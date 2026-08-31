using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.Youtube.Json;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Polly;
using System.Net;
using System.Text.RegularExpressions;
using Bot = DiscordStreamNotifyBot.Shared.BotState;
using Video = Google.Apis.YouTube.v3.Data.Video;

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
                        Log.Warn($"HoloSchedule GET 失敗，將於 {timeSpan.TotalSeconds} 秒後重試（第 {retryAttempt} 次重試）");
                        return timeSpan;
                    })
                    .ExecuteAsync(async () =>
                    {
                        return await htmlWeb.LoadFromWebAsync("https://schedule.hololive.tv/simple");
                    });

                if (htmlDocument == null)
                {
                    Log.Warn("HoloSchedule htmlDocument 為空，放棄本次排程");
                    Bot.IsHoloChannelSpider = false;
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
                        if (TryClaimUnknownVideo(videoId))
                            idList.Add(videoId);
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
                            var decision = await ClassifyApiVideoAsync(item, probeFakePost: false);
                            if (decision.Action == YoutubeApiVideoAction.NewVideo)
                            {
                                var streamVideo = BuildStreamVideo(item, decision.EventTime.Value, DataBase.Table.Video.YTChannelType.Holo);

                                Log.New($"（新影片） | {streamVideo.ScheduledStartTime} | {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({streamVideo.VideoId})");

                                if (addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo) && !isFirstHolo)
                                    await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.NewVideo).ConfigureAwait(false);
                            }
                            else if (decision.Action == YoutubeApiVideoAction.Started)
                            {
                                var streamVideo = BuildStreamVideo(item, decision.EventTime.Value, DataBase.Table.Video.YTChannelType.Holo);

                                Log.New($"（已開台） | {streamVideo.ScheduledStartTime} | {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({streamVideo.VideoId})");

                                if (addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo) && item.Snippet.LiveBroadcastContent == "live")
                                    await ReminderTimerActionAsync(streamVideo);
                            }
                            else if (decision.Action == YoutubeApiVideoAction.Scheduled)
                            {
                                var startTime = decision.EventTime.Value;
                                var streamVideo = BuildStreamVideo(item, startTime, DataBase.Table.Video.YTChannelType.Holo);

                                Log.New($"（新直播） | {streamVideo.ScheduledStartTime} | {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({streamVideo.VideoId})");

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
                            else if (decision.Action == YoutubeApiVideoAction.ActiveChatOnly)
                            {
                                var streamVideo = BuildStreamVideo(item, decision.EventTime.Value, DataBase.Table.Video.YTChannelType.Holo);
                                Log.New($"（僅偵測到直播聊天室的影片） {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({streamVideo.VideoId})");
                                addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo);
                            }
                            else if (decision.Action == YoutubeApiVideoAction.IgnoreFakePost)
                            {
                                Log.Error($"（新偽裝貼文） | {item.Snippet.ChannelTitle} ({item.Id})");
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
            finally
            {
                Bot.IsHoloChannelSpider = false; isFirstHolo = false;
            }
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

                var nijisanjiStreamList = new List<NijisanjiStreamJson>();

                for (int i = -1; i <= 1; i++)
                {
                    try
                    {
                        string result = await _nijisanjiApiHttpClient.GetStringAsync($"https://www.nijisanji.jp/api/streams?day_offset={i}");
                        if (result.Contains("ERROR</h1>"))
                            continue;

                        var nijisanjiStreamJson = JsonConvert.DeserializeObject<List<NijisanjiStreamJson>>(result);
                        nijisanjiStreamList.AddRange(nijisanjiStreamJson);
                    }
                    catch (Exception ex)
                    {
                        if (!ex.Message.Contains("EOF or 0 bytes") && !ex.Message.Contains("504") && !ex.Message.Contains("500"))
                            Log.Error(ex.Demystify(), $"NijisanjiScheduleAsync-GetData: {i}");
                        continue;
                    }
                }

                if (!nijisanjiStreamList.Any())
                {
                    Log.Warn("NijisanjiScheduleAsync: 直播清單無資料");
                    Bot.IsNijisanjiChannelSpider = false;
                    return;
                }

                // 先收集本次要查的 (item, videoId)，再批次查 YouTube API（一次 50 筆省 quota，§12.4）
                var pendingItems = new List<(NijisanjiStreamJson item, string videoId)>();
                foreach (var item in nijisanjiStreamList)
                {
                    if (item.Platform != "youtube")
                        continue;

                    string videoId = item.Url.Split("?v=")[1].Trim();
                    if (!TryClaimUnknownVideo(videoId))
                        continue;
                    pendingItems.Add((item, videoId));
                }

                var videoDict = new Dictionary<string, Video>();
                for (int i = 0; i < pendingItems.Count; i += 50)
                {
                    var idChunk = pendingItems.Skip(i).Take(50).Select((x) => x.videoId);
                    try
                    {
                        foreach (var v in await GetVideosAsync(idChunk))
                        {
                            if (!string.IsNullOrEmpty(v?.Id)) videoDict[v.Id] = v;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Demystify(), "NijisanjiScheduleAsync-GetVideos");
                    }
                }

                foreach (var (item, videoId) in pendingItems)
                {
                    if (!videoDict.TryGetValue(videoId, out var video) || video == null)
                    {
                        Log.Warn($"NijisanjiScheduleAsync: 取得直播資料失敗 {videoId}");
                        continue;
                    }

                    Log.Info($"Nijisanji Id: {videoId}");
                    DataBase.Table.Video streamVideo = BuildStreamVideo(video, item.StartAt.Value, DataBase.Table.Video.YTChannelType.Nijisanji);

                    if (item.Status == "on_air") // 已開台
                    {
                        Log.New($"（已開台） | {streamVideo.ScheduledStartTime} | {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({streamVideo.VideoId})");

                        if (addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo))
                            StartReminder(streamVideo, streamVideo.ChannelType);
                    }
                    else if (!item.EndAt.HasValue) // 沒有關台時間但又沒開台就當是新的直播
                    {
                        try
                        {
                            Log.New($"（新直播） | {streamVideo.ScheduledStartTime} | {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({streamVideo.VideoId})");

                            if (addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo))
                            {
                                // 會遇到尚未開台但已過開始時間的情況，所以還是先判定開始時間大於現在時間後再傳送新直播通知
                                if (!isFirst2434 && item.StartAt > DateTime.Now)
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
                        Log.New($"（已下播的新直播） | {streamVideo.ScheduledStartTime} | {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({streamVideo.VideoId})");
                        addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"NijisanjiScheduleAsync: {ex}");
            }
            finally
            {
                Bot.IsNijisanjiChannelSpider = false; isFirst2434 = false;
            }
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
                Log.Error("檢查 Redis 突襲開台鍵失敗");
            }
#endif

            await Bot.RedisDb.StringSetAsync("youtube.otherStart", "0", TimeSpan.FromMinutes(4));
            Bot.IsOtherChannelSpider = true;
            Dictionary<string, List<string>> otherVideoDic = new Dictionary<string, List<string>>();
            var addVideoIdList = new List<string>();

            using (var db = _dbService.GetDbContext())
            {
                var channelList = db.YoutubeChannelSpider.Where((x) => db.RecordYoutubeChannel.Any((x2) => x.ChannelId == x2.YoutubeChannelId)).ToList();
                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36");
                httpClient.DefaultRequestHeaders.Add("AcceptLanguage", "zh-TW");

                Log.Info($"突襲開台檢測開始：{channelList.Count()} 個頻道");
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
                            using var responseMessage = await Policy.Handle<HttpRequestException>()
                                .Or<WebException>((ex) => ex.Message.Contains("unavailable"))
                                .Or<TaskCanceledException>((ex) => ex.Message.Contains("HttpClient.Timeout"))
                                .WaitAndRetryAsync(3, (retryAttempt) =>
                                {
                                    var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                                    Log.Warn($"OtherSchedule {item.ChannelId} - {type}: GET 失敗，將於 {timeSpan.TotalSeconds} 秒後重試（第 {retryAttempt} 次重試）");
                                    return timeSpan;
                                })
                                .ExecuteAsync(async () =>
                                {
                                    var message = await httpClient.GetAsync($"https://www.youtube.com/channel/{item.ChannelId}/{type}");
                                    if (!message.IsSuccessStatusCode)
                                    {
                                        Log.Warn($"OtherSchedule {item.ChannelId} - {type}: HTTP {(int)message.StatusCode} {message.StatusCode}");
                                        try
                                        {
                                            message.EnsureSuccessStatusCode();
                                        }
                                        catch
                                        {
                                            message.Dispose();
                                            throw;
                                        }
                                    }

                                    return message;
                                });

                            var responseStatus = $"HTTP {(int)responseMessage.StatusCode} {responseMessage.StatusCode}";
                            var response = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

                            if (string.IsNullOrEmpty(response))
                            {
                                Log.Warn($"OtherSchedule {item.ChannelId} - {type}: {responseStatus}，回應為空，放棄本次排程");
                                continue;
                            }

                            Regex regex;
                            if (response.Contains("window[\"ytInitialData\"]"))
                                regex = OldYtInitialDataRegex();
                            else
                                regex = NewYtInitialDataRegex();

                            var match = regex.Match(response);
                            if (!match.Success || string.IsNullOrWhiteSpace(match.Groups[1].Value))
                            {
                                Log.Warn($"OtherSchedule {item.ChannelId} - {type}: {responseStatus}，ytInitialData regex 未命中，回應長度 {response.Length}");
                                continue;
                            }

                            var jObject = JObject.Parse(match.Groups[1].Value);
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
                                            Log.Warn($"{item.ChannelTitle} ({item.ChannelId}) 頻道錯誤：{alertRenderer["text"]["simpleText"]}，可能是暫時性的錯誤，跳過");
                                            continue;
                                        }

                                        // 偵測端無 Discord owner，僅記錄（原 owner 私訊改由維運監看 log）
                                        Log.Warn($"{item.ChannelTitle} ({item.ChannelId}) 頻道錯誤：{alertRenderer["text"]["simpleText"]}");
                                    }
                                }

                                break;
                            }

                            List<JToken> videoList =
                            [
                                .. jObject.Descendants().Where((x) => x.ToString().StartsWith("\"gridVideoRenderer")),
                                .. jObject.Descendants().Where((x) => x.ToString().StartsWith("\"videoRenderer")),
                                .. jObject.SelectTokens("$..richItemRenderer..watchEndpoint"),
                            ];

                            if (!otherVideoDic.ContainsKey(item.ChannelId))
                            {
                                otherVideoDic.Add(item.ChannelId, new List<string>());
                            }

                            foreach (var item2 in videoList)
                            {
                                try
                                {
                                    if (item2 is JObject videoRenderer)
                                    {
                                        videoId = videoRenderer.Value<string>("videoId");
                                    }
                                    else
                                    {
                                        var itemJson = item2.ToString();
                                        var objectStart = itemJson.IndexOf("{");
                                        if (objectStart < 0) continue;
                                        videoId = JObject.Parse(itemJson.Substring(objectStart)).Value<string>("videoId");
                                    }

                                    if (string.IsNullOrEmpty(videoId)) continue;

                                    if (!otherVideoDic[item.ChannelId].Contains(videoId))
                                    {
                                        otherVideoDic[item.ChannelId].Add(videoId);
                                        if (TryClaimUnknownVideo(videoId))
                                            addVideoIdList.Add(videoId);
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
                foreach (var item in Reminders.Where((x) =>
                    x.Value.StreamVideo.ScheduledStartTime < DateTime.Now &&
                    Volatile.Read(ref x.Value.RetryPending) == 0))
                {
                    RemoveReminder(item.Key, item.Value.StreamVideo);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"CheckScheduleTime-TryRemove");
            }

            int changeVideoNum = 0;
            var reminderSnapshot = Reminders.ToArray();
            for (int i = 0; i < reminderSnapshot.Length; i += 50)
            {
                try
                {
                    var remindersList = reminderSnapshot.Skip(i).Take(50).ToArray();

                    var video = YouTubeService.Videos.List("snippet,liveStreamingDetails");
                    video.Id = string.Join(",", remindersList.Select((x) => x.Key));
                    var videoResult = await video.ExecuteAsync(); // 若直播已刪除，該直播 ID 不會出現在回應中，但 API 仍會回傳 200 狀態。
                    var videosById = videoResult.Items.ToDictionary((x) => x.Id);

                    foreach (var reminder in remindersList)
                    {
                        try
                        {
                            videosById.TryGetValue(reminder.Key, out var item);
                            string scheduledStartTimeRaw = item?.LiveStreamingDetails?.ScheduledStartTimeRaw;
                            DateTime? startTime = null;
                            if (!string.IsNullOrEmpty(scheduledStartTimeRaw))
                            {
                                if (DateTime.TryParse(scheduledStartTimeRaw, out var parsedStartTime))
                                    startTime = parsedStartTime;
                                else
                                    Log.Error($"CheckScheduleTime-Parse: {reminder.Key} / {scheduledStartTimeRaw}");
                            }

                            var action = YoutubeReminderPolicy.ReconcileBatch(new YoutubeReminderBatchFacts(
                                item != null,
                                item?.LiveStreamingDetails != null,
                                !string.IsNullOrEmpty(scheduledStartTimeRaw),
                                startTime,
                                reminder.Value.StreamVideo.ScheduledStartTime,
                                DateTime.Now));

                            if (action == YoutubeReminderReconciliationAction.KeepExisting)
                                continue;

                            if (!RemoveReminder(reminder.Key, reminder.Value.StreamVideo, reminder.Value))
                                continue;

                            if (action == YoutubeReminderReconciliationAction.PublishDeleteAndRemove)
                            {
                                Log.Warn($"CheckScheduleTime-VideoResult-{reminder.Key}: 已刪除直播");
                                await PublishYoutubeNotificationAsync(reminder.Value.StreamVideo, YoutubeNoticeType.Delete).ConfigureAwait(false);

                                reminder.Value.StreamVideo.IsPrivate = true;
                                db.UpdateAndSave(reminder.Value.StreamVideo);
                                continue;
                            }

                            if (action == YoutubeReminderReconciliationAction.PublishStartAndRemove)
                            {
                                // 可能是影片已開播或排程資訊不完整，因此移除提醒並發布開台通知。
                                await PublishYoutubeNotificationAsync(reminder.Value.StreamVideo, YoutubeNoticeType.Start).ConfigureAwait(false);
                                continue;
                            }

                            var previousScheduledStartTime = reminder.Value.StreamVideo.ScheduledStartTime;
                            changeVideoNum++;
                            try
                            {
                                var streamVideo = BuildStreamVideo(item, startTime.Value, reminder.Value.StreamVideo.ChannelType);

                                var persistedVideo = GetDbVideoByType(db, reminder.Value.StreamVideo);
                                if (persistedVideo != null)
                                {
                                    persistedVideo.ChannelTitle = streamVideo.ChannelTitle;
                                    persistedVideo.VideoTitle = streamVideo.VideoTitle;
                                    persistedVideo.ScheduledStartTime = streamVideo.ScheduledStartTime;
                                    db.UpdateAndSave(persistedVideo);
                                }
                                else if (addNewStreamVideo.ContainsKey(streamVideo.VideoId))
                                {
                                    addNewStreamVideo[streamVideo.VideoId] = streamVideo;
                                }
                                else
                                {
                                    Log.Error($"({streamVideo.ChannelType}) 直播時間變更儲存失敗，找不到資料：{streamVideo.VideoId}");
                                }

                                Log.Info($"直播時間已變更 {streamVideo.ChannelTitle} - {streamVideo.VideoTitle}：{previousScheduledStartTime:O} -> {startTime:O}");

                                if (action is YoutubeReminderReconciliationAction.PublishChangeAndRunImmediately or
                                    YoutubeReminderReconciliationAction.PublishChangeAndReplaceTimer)
                                {
                                    await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.ChangeTime,
                                        previousScheduledStartTime: previousScheduledStartTime).ConfigureAwait(false);
                                    StartReminder(streamVideo, streamVideo.ChannelType);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"CheckScheduleTime-HasValue: {ex}");
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
            var decision = await ClassifyApiVideoAsync(item);
            if (decision.Action == YoutubeApiVideoAction.IgnoreFakePost)
            {
                Log.Error($"（新偽裝貼文） | {item.Snippet.ChannelTitle} ({item.Id})");
                return;
            }

            if (decision.Action == YoutubeApiVideoAction.NewVideo)
            {
                var streamVideo = BuildStreamVideo(item, decision.EventTime.Value, DataBase.Table.Video.YTChannelType.Other);

                streamVideo.ChannelType = streamVideo.GetProductionType();
                Log.New($"（新影片） | {streamVideo.ScheduledStartTime} | {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({streamVideo.VideoId})");

                if (addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo) && !isFirstOther && !isFromRNRS && streamVideo.ScheduledStartTime > DateTime.Now.AddDays(-2))
                    await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.NewVideo).ConfigureAwait(false);
            }
            else if (decision.Action == YoutubeApiVideoAction.Started)
            {
                var streamVideo = BuildStreamVideo(item, decision.EventTime.Value, DataBase.Table.Video.YTChannelType.Other);

                streamVideo.ChannelType = streamVideo.GetProductionType();
                Log.New($"（已開台） | {streamVideo.ScheduledStartTime} | {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({streamVideo.VideoId})");

                if (addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo) && item.Snippet.LiveBroadcastContent == "live" && !isFromRNRS)
                    await ReminderTimerActionAsync(streamVideo);
            }
            else if (decision.Action == YoutubeApiVideoAction.Scheduled)
            {
                var startTime = decision.EventTime.Value;
                var streamVideo = BuildStreamVideo(item, startTime, DataBase.Table.Video.YTChannelType.Other);

                streamVideo.ChannelType = streamVideo.GetProductionType();
                Log.New($"（新直播） | {streamVideo.ScheduledStartTime} | {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({streamVideo.VideoId})");

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
            else if (decision.Action == YoutubeApiVideoAction.ActiveChatOnly)
            {
                var streamVideo = BuildStreamVideo(item, decision.EventTime.Value, DataBase.Table.Video.YTChannelType.Other);

                Log.New($"（僅偵測到直播聊天室的影片） {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({streamVideo.VideoId})");
                addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo);
            }
        }

        private async Task<YoutubeApiVideoDecision> ClassifyApiVideoAsync(Video item, bool probeFakePost = true)
        {
            bool isFifteenSecondUpload = false;
            bool commentsDisabled = false;
            if (probeFakePost && item.LiveStreamingDetails == null)
            {
                var videoContent = await GetVideoDurationAsync(item.Id);
                isFifteenSecondUpload = videoContent?.ContentDetails?.Duration == "PT15S";
                if (isFifteenSecondUpload)
                    commentsDisabled = await GetCommentThreadsIsDisabledAsync(item.Id);
            }

            return YoutubeApiVideoPolicy.Classify(new YoutubeApiVideoFacts(
                item.LiveStreamingDetails != null,
                DateTime.Parse(item.Snippet.PublishedAtRaw),
                ParseApiTime(item.LiveStreamingDetails?.ActualStartTimeRaw),
                ParseApiTime(item.LiveStreamingDetails?.ScheduledStartTimeRaw),
                !string.IsNullOrEmpty(item.LiveStreamingDetails?.ActiveLiveChatId),
                isFifteenSecondUpload,
                commentsDisabled));
        }

        private static DateTime? ParseApiTime(string value)
            => string.IsNullOrEmpty(value) ? null : DateTime.Parse(value);

        public static void SaveDateBase()
        {
            int saveNum = 0;

            try
            {
                using var db = Bot.DbService.GetDbContext();

                if (!Bot.IsHoloChannelSpider)
                    saveNum += SaveVideosByType(db, db.HoloVideos, DataBase.Table.Video.YTChannelType.Holo, "Holo");

                if (!Bot.IsNijisanjiChannelSpider)
                    saveNum += SaveVideosByType(db, db.NijisanjiVideos, DataBase.Table.Video.YTChannelType.Nijisanji, "2434");

                if (!Bot.IsOtherChannelSpider)
                    saveNum += SaveVideosByType(db, db.OtherVideos, DataBase.Table.Video.YTChannelType.Other, "Other");

                saveNum += SaveVideosByType(db, db.NonApprovedVideos, DataBase.Table.Video.YTChannelType.NonApproved, "NonApproved");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"SaveDateBase");
            }

            if (saveNum != 0)
                Log.Info($"資料庫已儲存完畢：{saveNum} 筆");
        }

        /// <summary>由 YouTube API 影片資料建立 <see cref="DataBase.Table.Video"/>，收斂各排程重複的物件初始化。</summary>
        private static DataBase.Table.Video BuildStreamVideo(Video item, DateTime scheduledStartTime, DataBase.Table.Video.YTChannelType channelType)
            => new()
            {
                ChannelId = item.Snippet.ChannelId,
                ChannelTitle = item.Snippet.ChannelTitle,
                VideoId = item.Id,
                VideoTitle = item.Snippet.Title,
                ScheduledStartTime = scheduledStartTime,
                ChannelType = channelType
            };

        /// <summary>將 <see cref="addNewStreamVideo"/> 中指定 <paramref name="channelType"/> 的影片寫入資料庫後移除，收斂 SaveDateBase 的四段重複。</summary>
        private static int SaveVideosByType<T>(DataBase.MainDbContext db, DbSet<T> dbSet,
            DataBase.Table.Video.YTChannelType channelType, string logName) where T : DataBase.Table.Video, new()
        {
            if (!addNewStreamVideo.Any((x) => x.Value.ChannelType == channelType))
                return 0;

            int saved = 0;
            foreach (var item in addNewStreamVideo.Where((x) => x.Value.ChannelType == channelType))
            {
                if (!dbSet.AsNoTracking().Any((x) => x.VideoId == item.Key))
                {
                    try
                    {
                        dbSet.Add(new T
                        {
                            ChannelId = item.Value.ChannelId,
                            ChannelTitle = item.Value.ChannelTitle,
                            VideoId = item.Value.VideoId,
                            VideoTitle = item.Value.VideoTitle,
                            ScheduledStartTime = item.Value.ScheduledStartTime,
                            ChannelType = item.Value.ChannelType,
                            IsPrivate = item.Value.IsPrivate
                        });
                        saved++;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Demystify(), $"Save{logName}Video: {item.Key}");
                    }
                }

                addNewStreamVideo.Remove(item.Key, out _);
            }

            Log.Info($"{logName} 資料庫已儲存：{db.SaveChanges()} 筆");
            return saved;
        }

        [GeneratedRegex("window\\[\"ytInitialData\"\\] = (.*);")]
        private static partial Regex OldYtInitialDataRegex();

        [GeneratedRegex(">var ytInitialData = (.*?);</script>")]
        private static partial Regex NewYtInitialDataRegex();
    }
}
