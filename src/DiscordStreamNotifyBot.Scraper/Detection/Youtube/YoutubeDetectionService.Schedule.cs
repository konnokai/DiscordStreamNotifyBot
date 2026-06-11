using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.Youtube.Json;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Polly;
using System.Net;
using System.Text.RegularExpressions;
using Video = Google.Apis.YouTube.v3.Data.Video;

using Bot = DiscordStreamNotifyBot.Shared.BotState;

namespace DiscordStreamNotifyBot.Scraper.Detection.Youtube
{
    public partial class YoutubeDetectionService
    {
        private void ReScheduleReminder()
        {
            using (var db = _dbService.GetDbContext())
            {
                foreach (var streamVideo in db.HoloVideos.AsNoTracking().Where((x) => x.ScheduledStartTime > DateTime.Now && !x.IsPrivate))
                {
                    StartReminder(streamVideo, DataBase.Table.Video.YTChannelType.Holo);
                }

                foreach (var streamVideo in db.NijisanjiVideos.AsNoTracking().Where((x) => x.ScheduledStartTime > DateTime.Now && !x.IsPrivate))
                {
                    StartReminder(streamVideo, DataBase.Table.Video.YTChannelType.Nijisanji);
                }

                foreach (var streamVideo in db.OtherVideos.AsNoTracking().Where((x) => x.ScheduledStartTime > DateTime.Now && !x.IsPrivate))
                {
                    StartReminder(streamVideo, DataBase.Table.Video.YTChannelType.Other);
                }
            }
        }

        private async Task HoloScheduleAsync()
        {
            if (Bot.IsHoloChannelSpider || Bot.IsDisconnect) return;
            Bot.IsHoloChannelSpider = true;

            try
            {
                HtmlWeb htmlWeb = new HtmlWeb();
                HtmlDocument htmlDocument = await Policy.Handle<HttpRequestException>()
                    .Or<WebException>((ex) => ex.Message.Contains("unavailable"))
                    .Or<TaskCanceledException>((ex) => ex.Message.Contains("HttpClient.Timeout"))
                    .WaitAndRetryAsync(3, (retryAttempt) =>
                    {
                        var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                        Log.Warn($"HoloSchedule GET 失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                        return timeSpan;
                    })
                    .ExecuteAsync(async () =>
                    {
                        return await htmlWeb.LoadFromWebAsync("https://schedule.hololive.tv/simple");
                    });

                if (htmlDocument == null)
                {
                    Log.Warn("HoloSchedule htmlDocument 為空，放棄本次排程");
                    return;
                }

                var aList = htmlDocument.DocumentNode.Descendants().Where((x) => x.Name == "a");
                List<string> idList = new List<string>();
                foreach (var item in aList)
                {
                    string url = item.Attributes["href"].Value;
                    if (url.StartsWith("https://www.youtube.com/watch"))
                    {
                        string videoId = url.Split("?v=")[1].Trim();
                        if (!newStreamList.Contains(videoId) && !addNewStreamVideo.ContainsKey(videoId) && !SharedExtensions.HasStreamVideoByVideoId(videoId)) idList.Add(videoId);
                        newStreamList.Add(videoId);
                    }
                }

                if (idList.Count > 0)
                {
                    Log.New($"Holo Id: {string.Join(", ", idList)}");

                    for (int i = 0; i < idList.Count; i += 50)
                    {
                        var video = YouTubeService.Videos.List("snippet,liveStreamingDetails");
                        video.Id = string.Join(",", idList.Skip(i).Take(50));
                        var videoResult = await video.ExecuteAsync().ConfigureAwait(false);
                        foreach (var item in videoResult.Items)
                        {
                            if (item.LiveStreamingDetails == null) //上傳影片
                            {
                                var streamVideo = new DataBase.Table.Video()
                                {
                                    ChannelId = item.Snippet.ChannelId,
                                    ChannelTitle = item.Snippet.ChannelTitle,
                                    VideoId = item.Id,
                                    VideoTitle = item.Snippet.Title,
                                    ScheduledStartTime = DateTime.Parse(item.Snippet.PublishedAtRaw),
                                    ChannelType = DataBase.Table.Video.YTChannelType.Holo
                                };

                                Log.New($"(新影片) | {streamVideo.ScheduledStartTime} | {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({streamVideo.VideoId})");

                                if (addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo) && !isFirstHolo)
                                    await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.NewVideo).ConfigureAwait(false);
                            }
                            else if (!string.IsNullOrEmpty(item.LiveStreamingDetails.ActualStartTimeRaw)) //已開台直播
                            {
                                var startTime = DateTime.Parse(item.LiveStreamingDetails.ActualStartTimeRaw);
                                var streamVideo = new DataBase.Table.Video()
                                {
                                    ChannelId = item.Snippet.ChannelId,
                                    ChannelTitle = item.Snippet.ChannelTitle,
                                    VideoId = item.Id,
                                    VideoTitle = item.Snippet.Title,
                                    ScheduledStartTime = startTime,
                                    ChannelType = DataBase.Table.Video.YTChannelType.Holo
                                };

                                Log.New($"(已開台) | {streamVideo.ScheduledStartTime} | {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({streamVideo.VideoId})");

                                if (addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo) && item.Snippet.LiveBroadcastContent == "live")
                                    await ReminderTimerActionAsync(streamVideo);
                            }
                            else if (!string.IsNullOrEmpty(item.LiveStreamingDetails.ScheduledStartTimeRaw)) //尚未開台的直播
                            {
                                var startTime = DateTime.Parse(item.LiveStreamingDetails.ScheduledStartTimeRaw);
                                var streamVideo = new DataBase.Table.Video()
                                {
                                    ChannelId = item.Snippet.ChannelId,
                                    ChannelTitle = item.Snippet.ChannelTitle,
                                    VideoId = item.Id,
                                    VideoTitle = item.Snippet.Title,
                                    ScheduledStartTime = startTime,
                                    ChannelType = DataBase.Table.Video.YTChannelType.Holo
                                };

                                Log.New($"(新直播) | {streamVideo.ScheduledStartTime} | {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({streamVideo.VideoId})");

                                if (startTime > DateTime.Now && startTime < DateTime.Now.AddDays(14))
                                {
                                    if (addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo))
                                    {
                                        if (!isFirstHolo) await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.NewStream).ConfigureAwait(false);
                                        StartReminder(streamVideo, streamVideo.ChannelType);
                                    }
                                }
                                else if (startTime > DateTime.Now.AddMinutes(-10) || item.Snippet.LiveBroadcastContent == "live")
                                {
                                    if (addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo))
                                        StartReminder(streamVideo, streamVideo.ChannelType);
                                }
                                else addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("EOF or 0 bytes"))
                    Log.Error($"HoloStream: {ex}");
            }

            Bot.IsHoloChannelSpider = false; isFirstHolo = false;
        }

        private async Task NijisanjiScheduleAsync()
        {
            if (Bot.IsNijisanjiChannelSpider || Bot.IsDisconnect)
            {
                Log.Warn("彩虹社影片清單整理已取消");
                return;
            }

            try
            {
                Bot.IsNijisanjiChannelSpider = true;

                List<Data> datas = new List<Data>();
                NijisanjiStreamJson nijisanjiStreamJson = null;

                for (int i = -1; i <= 1; i++)
                {
                    try
                    {
                        string result = await _nijisanjiApiHttpClient.GetStringAsync($"https://www.nijisanji.jp/api/streams?day_offset={i}");
                        if (result.Contains("ERROR</h1>"))
                            continue;

                        nijisanjiStreamJson = JsonConvert.DeserializeObject<NijisanjiStreamJson>(result);
                        datas.AddRange(nijisanjiStreamJson.Included);
                        datas.AddRange(nijisanjiStreamJson.Data);
                    }
                    catch (Exception ex)
                    {
                        if (!ex.Message.Contains("EOF or 0 bytes") && !ex.Message.Contains("504") && !ex.Message.Contains("500"))
                            Log.Error(ex.Demystify(), $"NijisanjiScheduleAsync-GetData: {i}");
                        continue;
                    }
                }

                if (!datas.Any())
                {
                    Log.Warn("NijisanjiScheduleAsync: 直播清單無資料");
                    Bot.IsNijisanjiChannelSpider = false;
                    return;
                }

                foreach (var item in datas)
                {
                    if (item.Type != "youtube_event")
                        continue;

                    string videoId = item.Attributes.Url.Split("?v=")[1].Trim();
                    if (newStreamList.Contains(videoId) || addNewStreamVideo.ContainsKey(videoId) || SharedExtensions.HasStreamVideoByVideoId(videoId)) continue;
                    newStreamList.Add(videoId);

                    Log.Info($"Nijisanji Id: {videoId}");
                    var video = await GetVideoAsync(videoId);
                    if (video == null)
                    {
                        Log.Warn($"NijisanjiScheduleAsync: 取得直播資料失敗 {videoId}");
                        continue;
                    }

                    DataBase.Table.Video streamVideo = new DataBase.Table.Video()
                    {
                        ChannelId = video.Snippet.ChannelId,
                        ChannelTitle = video.Snippet.ChannelTitle,
                        VideoId = videoId,
                        VideoTitle = video.Snippet.Title,
                        ScheduledStartTime = item.Attributes.StartAt.Value,
                        ChannelType = DataBase.Table.Video.YTChannelType.Nijisanji
                    };

                    if (item.Attributes.Status == "on_air") // 已開台
                    {
                        Log.New($"(已開台) | {streamVideo.ScheduledStartTime} | {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({streamVideo.VideoId})");

                        if (addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo))
                            StartReminder(streamVideo, streamVideo.ChannelType);
                    }
                    else if (!item.Attributes.EndAt.HasValue) // 沒有關台時間但又沒開台就當是新的直播
                    {
                        try
                        {
                            Log.New($"(新直播) | {streamVideo.ScheduledStartTime} | {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({streamVideo.VideoId})");

                            if (addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo))
                            {
                                // 會遇到尚未開台但已過開始時間的情況，所以還是先判定開始時間大於現在時間後再傳送新直播通知
                                if (!isFirst2434 && item.Attributes.StartAt > DateTime.Now)
                                    await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.NewStream).ConfigureAwait(false);

                                StartReminder(streamVideo, streamVideo.ChannelType);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.Demystify(), $"NijisanjiScheduleAsync-New Stream: {streamVideo.VideoId}");
                        }
                    }
                    else
                    {
                        Log.New($"(已下播的新直播) | {streamVideo.ScheduledStartTime} | {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({streamVideo.VideoId})");
                        addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"NijisanjiScheduleAsync: {ex}");
            }

            Bot.IsNijisanjiChannelSpider = false; isFirst2434 = false;
        }

        private async Task OtherScheduleAsync()
        {
            if (Bot.IsOtherChannelSpider || Bot.IsDisconnect) return;

#if RELEASE
            try
            {
                if (Bot.RedisDb.KeyExists("youtube.otherStart"))
                {
                    var time = await Bot.RedisDb.KeyTimeToLiveAsync("youtube.otherStart");
                    Log.Warn($"已跑過突襲開台檢測爬蟲，剩餘 {time:mm\\:ss}");
                    isFirstOther = false;
                    return;
                }
            }
            catch
            {
                Log.Error("Redis 又死了zzz");
            }
#endif

            await Bot.RedisDb.StringSetAsync("youtube.otherStart", "0", TimeSpan.FromMinutes(4));
            Bot.IsOtherChannelSpider = true;
            Dictionary<string, List<string>> otherVideoDic = new Dictionary<string, List<string>>();
            var addVideoIdList = new List<string>();

            using (var db = _dbService.GetDbContext())
            {
                var channelList = db.YoutubeChannelSpider.Where((x) => db.RecordYoutubeChannel.Any((x2) => x.ChannelId == x2.YoutubeChannelId));
                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36");
                httpClient.DefaultRequestHeaders.Add("AcceptLanguage", "zh-TW");

                Log.Info($"突襲開台檢測開始: {channelList.Count()} 個頻道");
                foreach (var item in channelList)
                {
                    if (Bot.IsDisconnect) break;

                    try
                    {
                        if (item.ChannelTitle == null)
                        {
                            var ytChannel = YouTubeService.Channels.List("snippet");
                            ytChannel.Id = item.ChannelId;
                            item.ChannelTitle = (await ytChannel.ExecuteAsync().ConfigureAwait(false)).Items[0].Snippet.Title;
                            db.YoutubeChannelSpider.Update(item);
                            db.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Demystify(), $"OtherUpdateChannelTitle {item}");
                    }

                    string videoId = "";

                    foreach (var type in new string[] { "videos", "streams" })
                    {
                        try
                        {
                            var response = await Policy.Handle<HttpRequestException>()
                                .Or<WebException>((ex) => ex.Message.Contains("unavailable"))
                                .Or<TaskCanceledException>((ex) => ex.Message.Contains("HttpClient.Timeout"))
                                .WaitAndRetryAsync(3, (retryAttempt) =>
                                {
                                    var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                                    Log.Warn($"OtherSchedule {item.ChannelId} - {type}: GET 失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                                    return timeSpan;
                                })
                                .ExecuteAsync(async () =>
                                {
                                    return await httpClient.GetStringAsync($"https://www.youtube.com/channel/{item.ChannelId}/{type}");
                                });

                            if (string.IsNullOrEmpty(response))
                            {
                                Log.Warn($"OtherSchedule {item.ChannelId} - {type}: Response 為空，放棄本次排程");
                                continue;
                            }

                            Regex regex;
                            if (response.Contains("window[\"ytInitialData\"]"))
                                regex = OldYtInitialDataRegex();
                            else
                                regex = NewYtInitialDataRegex();

                            var group = regex.Match(response).Groups[1];
                            var jObject = JObject.Parse(group.Value);
                            var alerts = jObject["alerts"];

                            if (alerts != null)
                            {
                                foreach (var alert in alerts)
                                {
                                    var alertRenderer = alert["alertRenderer"];
                                    if (alertRenderer["type"].ToString() == "ERROR")
                                    {
                                        if (alertRenderer["text"]["simpleText"].ToString().Contains("未知的錯誤"))
                                        {
                                            Log.Warn($"{item.ChannelTitle} ({item.ChannelId}) 頻道錯誤: {alertRenderer["text"]["simpleText"]}，可能是暫時性的錯誤，跳過");
                                            continue;
                                        }

                                        // 偵測端無 Discord owner，僅記錄（原 owner 私訊改由維運監看 log）
                                        Log.Warn($"{item.ChannelTitle} ({item.ChannelId}) 頻道錯誤: {alertRenderer["text"]["simpleText"]}");
                                    }
                                }

                                break;
                            }

                            List<JToken> videoList =
                            [
                                .. jObject.Descendants().Where((x) => x.ToString().StartsWith("\"gridVideoRenderer")),
                                .. jObject.Descendants().Where((x) => x.ToString().StartsWith("\"videoRenderer")),
                            ];

                            if (!otherVideoDic.ContainsKey(item.ChannelId))
                            {
                                otherVideoDic.Add(item.ChannelId, new List<string>());
                            }

                            foreach (var item2 in videoList)
                            {
                                try
                                {
                                    videoId = JObject.Parse(item2.ToString().Substring(item2.ToString().IndexOf("{")))["videoId"].ToString();

                                    if (!otherVideoDic[item.ChannelId].Contains(videoId))
                                    {
                                        otherVideoDic[item.ChannelId].Add(videoId);
                                        if (!newStreamList.Contains(videoId) && !addNewStreamVideo.ContainsKey(videoId) && !SharedExtensions.HasStreamVideoByVideoId(videoId))
                                            addVideoIdList.Add(videoId);
                                        newStreamList.Add(videoId);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex.Demystify(), $"OtherSchedule {item.ChannelId} - {type}: GetVideoId");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            try { otherVideoDic[item.ChannelId].Remove(videoId); }
                            catch (Exception) { }
                            Log.Error(ex.Demystify(), $"OtherSchedule {item.ChannelId} - {type}: GetVideoList");
                        }
                    }
                }

                for (int i = 0; i < addVideoIdList.Count; i += 50)
                {
                    if (Bot.IsDisconnect) break;

                    IEnumerable<Video> videos;
                    try
                    {
                        videos = await GetVideosAsync(addVideoIdList.Skip(i).Take(50));
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"OtherSchedule-GetVideosAsync: {ex}");
                        Bot.IsOtherChannelSpider = false;
                        return;
                    }

                    foreach (var item in videos)
                    {
                        try
                        {
                            await AddOtherDataAsync(item);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.Demystify(), $"OtherAddSchedule {item.Id}");
                        }
                    }
                }
            }

            Bot.IsOtherChannelSpider = false; isFirstOther = false;
        }

        private async Task CheckScheduleTime()
        {
            using var db = _dbService.GetDbContext();
            try
            {
                foreach (var item in Reminders.Where((x) => x.Value.StreamVideo.ScheduledStartTime < DateTime.Now))
                {
                    Reminders.TryRemove(item);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"CheckScheduleTime-TryRemove");
            }

            int changeVideoNum = 0;
            for (int i = 0; i < Reminders.Count; i += 50)
            {
                try
                {
                    var remindersList = Reminders.Skip(i).Take(50);
                    if (!remindersList.Any())
                    {
                        Log.Error($"CheckScheduleTime-Any: {i} / {Reminders.Count}");
                        break;
                    }

                    var video = YouTubeService.Videos.List("snippet,liveStreamingDetails");
                    video.Id = string.Join(",", remindersList.Select((x) => x.Key));
                    var videoResult = await video.ExecuteAsync(); // 如果直播被刪除的話該直播 Id 不會回傳資訊，但 API 會返回 200 狀態

                    foreach (var reminder in remindersList)
                    {
                        try
                        {
                            // 如果 viderResult 內沒有該 VideoId 直播的話，則判定該直播已刪除
                            if (!videoResult.Items.Any((x) => x.Id == reminder.Key))
                            {
                                Log.Warn($"CheckScheduleTime-VideoResult-{reminder.Key}: 已刪除直播");

                                await PublishYoutubeNotificationAsync(reminder.Value.StreamVideo, YoutubeNoticeType.Delete).ConfigureAwait(false);
                                Reminders.TryRemove(reminder.Key, out var reminderItem);

                                reminder.Value.StreamVideo.IsPrivate = true;
                                db.UpdateAndSave(reminder.Value.StreamVideo);

                                continue;
                            }

                            var item = videoResult.Items.First((x) => x.Id == reminder.Key);

                            // 可能是有調整到排程導致 API 回傳無資料，很少見但真的會遇到
                            if (item.LiveStreamingDetails == null || string.IsNullOrEmpty(item.LiveStreamingDetails.ScheduledStartTimeRaw))
                            {
                                Reminders.TryRemove(reminder.Key, out var reminderItem);

                                await PublishYoutubeNotificationAsync(reminder.Value.StreamVideo, YoutubeNoticeType.Start).ConfigureAwait(false);
                                continue;
                            }

                            if (reminder.Value.StreamVideo.ScheduledStartTime != DateTime.Parse(item.LiveStreamingDetails.ScheduledStartTimeRaw))
                            {
                                changeVideoNum++;
                                try
                                {
                                    if (Reminders.TryRemove(reminder.Key, out var t))
                                    {
                                        t.Timer.Change(Timeout.Infinite, Timeout.Infinite);
                                        t.Timer.Dispose();
                                    }

                                    var startTime = DateTime.Parse(item.LiveStreamingDetails.ScheduledStartTimeRaw);
                                    var streamVideo = new DataBase.Table.Video()
                                    {
                                        ChannelId = item.Snippet.ChannelId,
                                        ChannelTitle = item.Snippet.ChannelTitle,
                                        VideoId = item.Id,
                                        VideoTitle = item.Snippet.Title,
                                        ScheduledStartTime = startTime,
                                        ChannelType = reminder.Value.StreamVideo.ChannelType
                                    };

                                    db.UpdateAndSave(reminder.Value.StreamVideo);

                                    Log.Info($"時間已更改 {streamVideo.ChannelTitle} - {streamVideo.VideoTitle}");

                                    if (startTime > DateTime.Now && startTime < DateTime.Now.AddDays(14))
                                    {
                                        await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.ChangeTime).ConfigureAwait(false);
                                        StartReminder(streamVideo, streamVideo.ChannelType);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($"CheckScheduleTime-HasValue: {ex}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"CheckScheduleTime-VideoResult-Items: {reminder.Key}");
                            Log.Error($"{ex}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"CheckScheduleTime: {ex}");
                }
            }

            if (changeVideoNum > 0)
            {
                Log.Info($"CheckScheduleTime-Done: {changeVideoNum} / {Reminders.Count}");
            }
        }

        public async Task AddOtherDataAsync(Video item, bool isFromRNRS = false)
        {
            if (item.LiveStreamingDetails == null)
            {
                var videoContent = await GetVideoDurationAsync(item.Id);
                if (videoContent.ContentDetails.Duration == "PT15S")
                {
                    var isCommentDisabled = await GetCommentThreadsIsDisabledAsync(item.Id);
                    if (isCommentDisabled)
                    {
                        Log.Error($"(新偽裝貼文) | {item.Snippet.ChannelTitle} ({item.Id})");
                        return;
                    }
                }

                var streamVideo = new DataBase.Table.Video()
                {
                    ChannelId = item.Snippet.ChannelId,
                    ChannelTitle = item.Snippet.ChannelTitle,
                    VideoId = item.Id,
                    VideoTitle = item.Snippet.Title,
                    ScheduledStartTime = DateTime.Parse(item.Snippet.PublishedAtRaw),
                    ChannelType = DataBase.Table.Video.YTChannelType.Other
                };

                streamVideo.ChannelType = streamVideo.GetProductionType();
                Log.New($"(新影片) | {streamVideo.ScheduledStartTime} | {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({streamVideo.VideoId})");

                if (addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo) && !isFirstOther && !isFromRNRS && streamVideo.ScheduledStartTime > DateTime.Now.AddDays(-2))
                    await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.NewVideo).ConfigureAwait(false);
            }
            else if (!string.IsNullOrEmpty(item.LiveStreamingDetails.ActualStartTimeRaw)) //已開台直播
            {
                var startTime = DateTime.Parse(item.LiveStreamingDetails.ActualStartTimeRaw);
                var streamVideo = new DataBase.Table.Video()
                {
                    ChannelId = item.Snippet.ChannelId,
                    ChannelTitle = item.Snippet.ChannelTitle,
                    VideoId = item.Id,
                    VideoTitle = item.Snippet.Title,
                    ScheduledStartTime = startTime,
                    ChannelType = DataBase.Table.Video.YTChannelType.Other
                };

                streamVideo.ChannelType = streamVideo.GetProductionType();
                Log.New($"(已開台) | {streamVideo.ScheduledStartTime} | {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({streamVideo.VideoId})");

                if (addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo) && item.Snippet.LiveBroadcastContent == "live" && !isFromRNRS)
                    await ReminderTimerActionAsync(streamVideo);
            }
            else if (!string.IsNullOrEmpty(item.LiveStreamingDetails.ScheduledStartTimeRaw)) // 尚未開台的直播
            {
                var startTime = DateTime.Parse(item.LiveStreamingDetails.ScheduledStartTimeRaw);
                var streamVideo = new DataBase.Table.Video()
                {
                    ChannelId = item.Snippet.ChannelId,
                    ChannelTitle = item.Snippet.ChannelTitle,
                    VideoId = item.Id,
                    VideoTitle = item.Snippet.Title,
                    ScheduledStartTime = startTime,
                    ChannelType = DataBase.Table.Video.YTChannelType.Other
                };

                streamVideo.ChannelType = streamVideo.GetProductionType();
                Log.New($"(新直播) | {streamVideo.ScheduledStartTime} | {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({streamVideo.VideoId})");

                if (startTime > DateTime.Now && startTime < DateTime.Now.AddDays(14))
                {
                    if (addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo) && !isFromRNRS)
                    {
                        if (!isFirstOther) await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.NewStream).ConfigureAwait(false);
                        StartReminder(streamVideo, streamVideo.ChannelType);
                    }
                }
                else if (startTime > DateTime.Now.AddMinutes(-10) || item.Snippet.LiveBroadcastContent == "live")
                {
                    if (addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo) && !isFromRNRS)
                        StartReminder(streamVideo, streamVideo.ChannelType);
                }
                else addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo);
            }
            else if (string.IsNullOrEmpty(item.LiveStreamingDetails.ActualStartTimeRaw) && item.LiveStreamingDetails.ActiveLiveChatId != null)
            {
                var streamVideo = new DataBase.Table.Video()
                {
                    ChannelId = item.Snippet.ChannelId,
                    ChannelTitle = item.Snippet.ChannelTitle,
                    VideoId = item.Id,
                    VideoTitle = item.Snippet.Title,
                    ScheduledStartTime = DateTime.Parse(item.Snippet.PublishedAtRaw),
                    ChannelType = DataBase.Table.Video.YTChannelType.Other
                };

                Log.New($"(一般路過的新直播室) {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({streamVideo.VideoId})");
                addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo);
            }
        }

        public static void SaveDateBase()
        {
            int saveNum = 0;

            try
            {
                using var db = Bot.DbService.GetDbContext();

                if (!Bot.IsHoloChannelSpider && addNewStreamVideo.Any((x) => x.Value.ChannelType == DataBase.Table.Video.YTChannelType.Holo))
                {
                    foreach (var item in addNewStreamVideo.Where((x) => x.Value.ChannelType == DataBase.Table.Video.YTChannelType.Holo))
                    {
                        if (!db.HoloVideos.AsNoTracking().Any((x) => x.VideoId == item.Key))
                        {
                            try
                            {
                                db.HoloVideos.Add(new()
                                {
                                    ChannelId = item.Value.ChannelId,
                                    ChannelTitle = item.Value.ChannelTitle,
                                    VideoId = item.Value.VideoId,
                                    VideoTitle = item.Value.VideoTitle,
                                    ScheduledStartTime = item.Value.ScheduledStartTime,
                                    ChannelType = item.Value.ChannelType,
                                    IsPrivate = item.Value.IsPrivate
                                });
                                saveNum++;
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.Demystify(), $"SaveHoloVideo: {item.Key}");
                            }
                        }

                        addNewStreamVideo.Remove(item.Key, out _);
                    }

                    Log.Info($"Holo 資料庫已儲存: {db.SaveChanges()} 筆");
                }

                if (!Bot.IsNijisanjiChannelSpider && addNewStreamVideo.Any((x) => x.Value.ChannelType == DataBase.Table.Video.YTChannelType.Nijisanji))
                {
                    foreach (var item in addNewStreamVideo.Where((x) => x.Value.ChannelType == DataBase.Table.Video.YTChannelType.Nijisanji))
                    {
                        if (!db.NijisanjiVideos.AsNoTracking().Any((x) => x.VideoId == item.Key))
                        {
                            try
                            {
                                db.NijisanjiVideos.Add(new()
                                {
                                    ChannelId = item.Value.ChannelId,
                                    ChannelTitle = item.Value.ChannelTitle,
                                    VideoId = item.Value.VideoId,
                                    VideoTitle = item.Value.VideoTitle,
                                    ScheduledStartTime = item.Value.ScheduledStartTime,
                                    ChannelType = item.Value.ChannelType,
                                    IsPrivate = item.Value.IsPrivate
                                });
                                saveNum++;
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.Demystify(), $"SaveNijisanjiVideo: {item.Key}");
                            }
                        }

                        addNewStreamVideo.Remove(item.Key, out _);
                    }

                    Log.Info($"2434 資料庫已儲存: {db.SaveChanges()} 筆");
                }

                if (!Bot.IsOtherChannelSpider && addNewStreamVideo.Any((x) => x.Value.ChannelType == DataBase.Table.Video.YTChannelType.Other))
                {
                    foreach (var item in addNewStreamVideo.Where((x) => x.Value.ChannelType == DataBase.Table.Video.YTChannelType.Other))
                    {
                        if (!db.OtherVideos.AsNoTracking().Any((x) => x.VideoId == item.Key))
                        {
                            try
                            {
                                db.OtherVideos.Add(new()
                                {
                                    ChannelId = item.Value.ChannelId,
                                    ChannelTitle = item.Value.ChannelTitle,
                                    VideoId = item.Value.VideoId,
                                    VideoTitle = item.Value.VideoTitle,
                                    ScheduledStartTime = item.Value.ScheduledStartTime,
                                    ChannelType = item.Value.ChannelType,
                                    IsPrivate = item.Value.IsPrivate
                                });
                                saveNum++;
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.Demystify(), $"SaveOtherVideo: {item.Key}");
                            }
                        }

                        addNewStreamVideo.Remove(item.Key, out _);
                    }

                    Log.Info($"Other 資料庫已儲存: {db.SaveChanges()} 筆");
                }

                if (addNewStreamVideo.Any((x) => x.Value.ChannelType == DataBase.Table.Video.YTChannelType.NonApproved))
                {
                    foreach (var item in addNewStreamVideo.Where((x) => x.Value.ChannelType == DataBase.Table.Video.YTChannelType.NonApproved))
                    {
                        if (!db.NonApprovedVideos.AsNoTracking().Any((x) => x.VideoId == item.Key))
                        {
                            try
                            {
                                db.NonApprovedVideos.Add(new()
                                {
                                    ChannelId = item.Value.ChannelId,
                                    ChannelTitle = item.Value.ChannelTitle,
                                    VideoId = item.Value.VideoId,
                                    VideoTitle = item.Value.VideoTitle,
                                    ScheduledStartTime = item.Value.ScheduledStartTime,
                                    ChannelType = item.Value.ChannelType,
                                    IsPrivate = item.Value.IsPrivate
                                });
                                saveNum++;
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.Demystify(), $"SaveNonApprovedVideo: {item.Key}");
                            }
                        }

                        addNewStreamVideo.Remove(item.Key, out _);
                    }

                    Log.Info($"NonApproved 資料庫已儲存: {db.SaveChanges()} 筆");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"SaveDateBase");
            }

            if (saveNum != 0)
                Log.Info($"資料庫已儲存完畢: {saveNum} 筆");
        }

        [GeneratedRegex("window\\[\"ytInitialData\"\\] = (.*);")]
        private static partial Regex OldYtInitialDataRegex();

        [GeneratedRegex(">var ytInitialData = (.*?);</script>")]
        private static partial Regex NewYtInitialDataRegex();
    }
}
