﻿using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.SharedService.Youtube.Json;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Polly;
using System.Collections.Concurrent;
using System.Data;
using System.Net;
using System.Text.RegularExpressions;
using Extensions = DiscordStreamNotifyBot.Interaction.Extensions;
using Video = Google.Apis.YouTube.v3.Data.Video;

namespace DiscordStreamNotifyBot.SharedService.Youtube
{
    public partial class YoutubeStreamService
    {
        private static ConcurrentDictionary<string, DataBase.Table.Video> addNewStreamVideo = new();
        private static HashSet<string> newStreamList = new();
        private bool isFirstHolo = true, isFirst2434 = true, isFirstOther = true;

        private void ReScheduleReminder()
        {
            List<string> recordChannelId = new();
            using (var db = _dbService.GetDbContext())
            {
                if (db.RecordYoutubeChannel.Any())
                    recordChannelId = db.RecordYoutubeChannel.AsNoTracking().Select((x) => x.YoutubeChannelId).ToList();

                foreach (var streamVideo in db.HoloVideos.AsNoTracking().Where((x) => x.ScheduledStartTime > DateTime.Now && !x.IsPrivate))
                {
                    StartReminder(streamVideo, DataBase.Table.Video.YTChannelType.Holo);
                }

                foreach (var streamVideo in db.NijisanjiVideos.AsNoTracking().Where((x) => x.ScheduledStartTime > DateTime.Now && !x.IsPrivate))
                {
                    StartReminder(streamVideo, DataBase.Table.Video.YTChannelType.Holo);
                }

                foreach (var streamVideo in db.OtherVideos.AsNoTracking().Where((x) => x.ScheduledStartTime > DateTime.Now && !x.IsPrivate))
                {
                    StartReminder(streamVideo, DataBase.Table.Video.YTChannelType.Holo);
                }
            }
        }

        private async Task HoloScheduleAsync()
        {
            if (Bot.IsHoloChannelSpider || Bot.IsDisconnect) return;
            //Log.Info("Holo影片清單整理開始");
            Bot.IsHoloChannelSpider = true;

            try
            {
                HtmlWeb htmlWeb = new HtmlWeb();
                HtmlDocument htmlDocument = await Policy.Handle<HttpRequestException>()
                    .Or<WebException>((ex) => ex.Message.Contains("unavailable")) // Resource temporarily unavailable
                    .Or<TaskCanceledException>((ex) => ex.Message.Contains("HttpClient.Timeout")) // The request was canceled due to the configured HttpClient.Timeout of 100 seconds elapsing
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
                        if (!newStreamList.Contains(videoId) && !addNewStreamVideo.ContainsKey(videoId) && !Extensions.HasStreamVideoByVideoId(videoId)) idList.Add(videoId);
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

                                EmbedBuilder embedBuilder = new EmbedBuilder();
                                embedBuilder.WithOkColor()
                                    .WithTitle(streamVideo.VideoTitle)
                                    .WithDescription(Format.Url(streamVideo.ChannelTitle, $"https://www.youtube.com/channel/{streamVideo.ChannelId}"))
                                    .WithImageUrl($"https://i.ytimg.com/vi/{streamVideo.VideoId}/maxresdefault.jpg")
                                    .WithUrl($"https://www.youtube.com/watch?v={streamVideo.VideoId}")
                                    .AddField("上傳時間", streamVideo.ScheduledStartTime.ConvertDateTimeToDiscordMarkdown());

                                if (addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo) && !isFirstHolo)
                                    await SendStreamMessageAsync(streamVideo, embedBuilder.Build(), NoticeType.NewVideo).ConfigureAwait(false);
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
                                    EmbedBuilder embedBuilder = new EmbedBuilder();
                                    embedBuilder.WithErrorColor()
                                    .WithTitle(streamVideo.VideoTitle)
                                    .WithDescription(Format.Url(streamVideo.ChannelTitle, $"https://www.youtube.com/channel/{streamVideo.ChannelId}"))
                                    .WithImageUrl($"https://i.ytimg.com/vi/{streamVideo.VideoId}/maxresdefault.jpg")
                                    .WithUrl($"https://www.youtube.com/watch?v={streamVideo.VideoId}")
                                    .AddField("直播狀態", "尚未開台")
                                    .AddField("排定開台時間", startTime.ConvertDateTimeToDiscordMarkdown());

                                    if (addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo))
                                    {
                                        if (!isFirstHolo) await SendStreamMessageAsync(streamVideo, embedBuilder.Build(), NoticeType.NewStream).ConfigureAwait(false);
                                        StartReminder(streamVideo, streamVideo.ChannelType);
                                    }
                                }
                                else if (startTime > DateTime.Now.AddMinutes(-10) || item.Snippet.LiveBroadcastContent == "live") // 如果開台時間在十分鐘內或已經開台
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
            //Log.Info("Holo影片清單整理完成");
        }

        // 2434官網直播表: https://www.nijisanji.jp/streams
        // 直播資料路徑會變動，~~必須要從上面的網址直接解析Json~~
        // 發現有API網址了: https://www.nijisanji.jp/api/streams?day_offset=-1
        // day_offset是必要參數，可以設定-3~6
        // 2434成員資料
        // JP: https://www.nijisanji.jp/api/livers?limit=300&offset=0&orderKey=subscriber_count&order=desc&affiliation=nijisanji&locale=ja&includeHidden=true
        // EN: https://www.nijisanji.jp/api/livers?limit=300&offset=0&orderKey=subscriber_count&order=desc&affiliation=nijisanjien&locale=ja&includeHidden=true
        // VR: https://www.nijisanji.jp/api/livers?limit=300&offset=0&orderKey=subscriber_count&order=desc&affiliation=virtuareal&locale=ja&includeHidden=true
        // KR跟ID沒看到網站有請求
        private async Task NijisanjiScheduleAsync()
        {
            if (Bot.IsNijisanjiChannelSpider || Bot.IsDisconnect)
            {
                Log.Warn("彩虹社影片清單整理已取消");
                return;
            }
            //Log.Info("彩虹社影片清單整理開始");

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

                    string videoId = item.Attributes.Url.Split("?v=")[1].Trim(), channelTitle = "", liverId = null, externalId = null;
                    if (newStreamList.Contains(videoId) || addNewStreamVideo.ContainsKey(videoId) || Extensions.HasStreamVideoByVideoId(videoId)) continue;
                    newStreamList.Add(videoId);

                    var youtubeChannelData = datas.FirstOrDefault((x) => x.Type == "youtube_channel" && x.Id == item.Relationships.YoutubeChannel.Data.Id);
                    if (youtubeChannelData != null)
                    {
                        channelTitle = youtubeChannelData.Attributes.Name;
                        liverId = youtubeChannelData.Relationships?.Liver?.Data?.Id;
                        externalId = datas.FirstOrDefault((x) => x.Type == "liver" && x.Id == liverId).Attributes.ExternalId;
                    }

                    DataBase.Table.Video streamVideo = null;
                    if (!string.IsNullOrEmpty(externalId))
                    {
                        var channelData = NijisanjiLiverContents.FirstOrDefault((x) => x.id == externalId);
                        if (channelData != null)
                        {
                            if (string.IsNullOrEmpty(channelTitle))
                                channelTitle = $"{channelData.name} / {channelData.enName}";

                            string channelId = "";
                            try
                            {
                                channelId = await GetChannelIdAsync(channelData.socialLinks.youtube);

                                streamVideo = new DataBase.Table.Video()
                                {
                                    ChannelId = channelId,
                                    ChannelTitle = channelTitle,
                                    VideoId = videoId,
                                    VideoTitle = item.Attributes.Title,
                                    ScheduledStartTime = item.Attributes.StartAt.Value,
                                    ChannelType = DataBase.Table.Video.YTChannelType.Nijisanji
                                };
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.Demystify(), $"channelData 解析失敗: {channelData.socialLinks.youtube}");
                            }
                        }
                    }

                    Log.Info($"Nijisanji Id: {videoId}");
                    if (streamVideo == null)
                    {
                        var video = await GetVideoAsync(videoId);
                        streamVideo = new DataBase.Table.Video()
                        {
                            ChannelId = video.Snippet.ChannelId,
                            ChannelTitle = video.Snippet.ChannelTitle,
                            VideoId = videoId,
                            VideoTitle = item.Attributes.Title,
                            ScheduledStartTime = item.Attributes.StartAt.Value,
                            ChannelType = DataBase.Table.Video.YTChannelType.Nijisanji
                        };

                        Log.Warn($"檢測到無 Liver 資料的頻道({videoId}): `{video.Snippet.ChannelTitle}` / {item.Attributes.Title}");
                        Log.Warn("重新刷新 Liver 資料清單");

                        NijisanjiLiverContents.Clear();
                        foreach (var affiliation in new string[] { "nijisanji", "nijisanjien", "virtuareal" })
                        {
                            await Task.Run(async () => await GetOrCreateNijisanjiLiverListAsync(affiliation, true));
                        }
                    }

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
                            EmbedBuilder embedBuilder = new EmbedBuilder();
                            embedBuilder.WithErrorColor()
                                .WithTitle(streamVideo.VideoTitle)
                                .WithDescription(Format.Url(streamVideo.ChannelTitle, $"https://www.youtube.com/channel/{streamVideo.ChannelId}"))
                                .WithImageUrl($"https://i.ytimg.com/vi/{streamVideo.VideoId}/maxresdefault.jpg")
                                .WithUrl($"https://www.youtube.com/watch?v={streamVideo.VideoId}")
                                .AddField("直播狀態", "尚未開台")
                                .AddField("排定開台時間", item.Attributes.StartAt.Value.ConvertDateTimeToDiscordMarkdown());

                            Log.New($"(新直播) | {streamVideo.ScheduledStartTime} | {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({streamVideo.VideoId})");

                            if (addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo))
                            {
                                // 會遇到尚未開台但已過開始時間的情況，所以還是先判定開始時間大於現在時間後再傳送新直播通知
                                if (!isFirst2434 && item.Attributes.StartAt > DateTime.Now)
                                    await SendStreamMessageAsync(streamVideo, embedBuilder.Build(), NoticeType.NewStream).ConfigureAwait(false);

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
            //Log.Info("彩虹社影片清單整理完成");
        }

        // Todo: BlockingCollection應用 (但還不知道要用甚麼)
        // 應該是不能用，為了降低API配額消耗，所以必須取得全部的VideoId後再一次性的跟API要資料
        // https://blog.darkthread.net/blog/blockingcollection/
        // https://docs.microsoft.com/en-us/dotnet/standard/collections/thread-safe/blockingcollection-overview
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
                                .Or<WebException>((ex) => ex.Message.Contains("unavailable")) // Resource temporarily unavailable
                                .Or<TaskCanceledException>((ex) => ex.Message.Contains("HttpClient.Timeout")) // The request was canceled due to the configured HttpClient.Timeout of 100 seconds elapsing
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
                                regex = new Regex("window\\[\"ytInitialData\"\\] = (.*);");
                            else
                                regex = new Regex(">var ytInitialData = (.*?);</script>");

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
                                        try
                                        {
                                            Log.Warn($"{item.ChannelTitle} ({item.ChannelId}) 頻道錯誤: {alertRenderer["text"]["simpleText"]}");

                                            await Bot.ApplicatonOwner.SendMessageAsync($"`{item.ChannelTitle}` ({item.ChannelId}) 頻道錯誤: {alertRenderer["text"]["simpleText"]}");

                                            db.YoutubeChannelSpider.Remove(item);
                                            db.SaveChanges();
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Error(ex.Demystify(), $"頻道被 Ban 了但移除爬蟲失敗: {item.ChannelTitle} ({item.ChannelId})");
                                        }
                                    }
                                }

                                break;
                            }

                            List<JToken> videoList = new List<JToken>();
                            videoList.AddRange(jObject.Descendants().Where((x) => x.ToString().StartsWith("\"gridVideoRenderer")));
                            videoList.AddRange(jObject.Descendants().Where((x) => x.ToString().StartsWith("\"videoRenderer")));

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
                                        if (!newStreamList.Contains(videoId) && !addNewStreamVideo.ContainsKey(videoId) && !Extensions.HasStreamVideoByVideoId(videoId))
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
            //Log.Info("其他勢影片清單整理完成");
        }

        private async Task CheckScheduleTime()
        {
            using var db = _dbService.GetDbContext();
            try
            {
                // Key原則上不會有null或空白的情況才對
                //var list = Reminders.Where((x) => string.IsNullOrEmpty(x.Key)).ToList();
                //list.AddRange(Reminders.Where((x) => x.Value.StreamVideo.ScheduledStartTime < DateTime.Now));
                foreach (var item in Reminders.Where((x) => x.Value.StreamVideo.ScheduledStartTime < DateTime.Now))
                {
                    Reminders.TryRemove(item);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"CheckScheduleTime-TryRemove");
            }

            List<string> recordChannelId = new();
            try
            {
                if (db.RecordYoutubeChannel.Any())
                    recordChannelId = db.RecordYoutubeChannel.Select((x) => x.YoutubeChannelId).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"CheckScheduleTime-GetRecordYoutubeChannel");
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

                    foreach (var reminder in remindersList) // 直接使用 Reminders 來做迴圈
                    {
                        try
                        {
                            // 如果 viderResult 內沒有該 VideoId 直播的話，則判定該直播已刪除
                            if (!videoResult.Items.Any((x) => x.Id == reminder.Key))
                            {
                                // 如果是錄影頻道的話則忽略
                                //if (recordChannelId.Any((x) => x == reminder.Value.StreamVideo.ChannelId))
                                //{
                                //    Log.Warn($"CheckScheduleTime-VideoResult-{reminder.Key}: 錄影頻道已刪除直播，略過");
                                //    continue;
                                //}

                                Log.Warn($"CheckScheduleTime-VideoResult-{reminder.Key}: 已刪除直播");

                                EmbedBuilder embedBuilder = new EmbedBuilder();
                                embedBuilder.WithErrorColor()
                                    .WithTitle(reminder.Value.StreamVideo.VideoTitle)
                                    .WithDescription(Format.Url(reminder.Value.StreamVideo.ChannelTitle, $"https://www.youtube.com/channel/{reminder.Value.StreamVideo.ChannelId}"))
                                    .WithUrl($"https://www.youtube.com/watch?v={reminder.Key}")
                                    .AddField("直播狀態", "已刪除直播")
                                    .AddField("排定開台時間", reminder.Value.StreamVideo.ScheduledStartTime.ConvertDateTimeToDiscordMarkdown(), true);

                                await SendStreamMessageAsync(reminder.Value.StreamVideo, embedBuilder.Build(), NoticeType.Delete).ConfigureAwait(false);
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

                                EmbedBuilder embedBuilder = new EmbedBuilder();
                                embedBuilder.WithTitle(reminder.Value.StreamVideo.VideoTitle)
                                    .WithOkColor()
                                    .WithDescription(Format.Url(reminder.Value.StreamVideo.ChannelTitle, $"https://www.youtube.com/channel/{reminder.Value.StreamVideo.ChannelId}"))
                                    .WithImageUrl($"https://i.ytimg.com/vi/{reminder.Key}/maxresdefault.jpg")
                                    .WithUrl($"https://www.youtube.com/watch?v={reminder.Key}")
                                    .AddField("直播狀態", "直播排程資料遺失")
                                    .AddField("原先預定開台時間", reminder.Value.StreamVideo.ScheduledStartTime.ConvertDateTimeToDiscordMarkdown());

                                if (Bot.ApplicatonOwner != null) await Bot.ApplicatonOwner.SendMessageAsync(null, false, embedBuilder.Build()).ConfigureAwait(false);
                                await SendStreamMessageAsync(reminder.Value.StreamVideo, embedBuilder.Build(), NoticeType.Start).ConfigureAwait(false);
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
                                        EmbedBuilder embedBuilder = new EmbedBuilder();
                                        embedBuilder.WithErrorColor()
                                            .WithTitle(streamVideo.VideoTitle)
                                            .WithDescription(Format.Url(streamVideo.ChannelTitle, $"https://www.youtube.com/channel/{streamVideo.ChannelId}"))
                                            .WithImageUrl($"https://i.ytimg.com/vi/{streamVideo.VideoId}/maxresdefault.jpg")
                                            .WithUrl($"https://www.youtube.com/watch?v={streamVideo.VideoId}")
                                            .AddField("直播狀態", "尚未開台(已更改時間)", true)
                                            .AddField("排定開台時間", reminder.Value.StreamVideo.ScheduledStartTime.ConvertDateTimeToDiscordMarkdown())
                                            .AddField("更改開台時間", streamVideo.ScheduledStartTime.ConvertDateTimeToDiscordMarkdown());

                                        await SendStreamMessageAsync(streamVideo, embedBuilder.Build(), NoticeType.ChangeTime).ConfigureAwait(false);
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

                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.WithOkColor()
                    .WithTitle(streamVideo.VideoTitle)
                    .WithDescription(Format.Url(streamVideo.ChannelTitle, $"https://www.youtube.com/channel/{streamVideo.ChannelId}"))
                    .WithImageUrl($"https://i.ytimg.com/vi/{streamVideo.VideoId}/maxresdefault.jpg")
                    .WithUrl($"https://www.youtube.com/watch?v={streamVideo.VideoId}")
                    .AddField("上傳時間", streamVideo.ScheduledStartTime.ConvertDateTimeToDiscordMarkdown());

                if (addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo) && !isFirstOther && !isFromRNRS && streamVideo.ScheduledStartTime > DateTime.Now.AddDays(-2))
                    await SendStreamMessageAsync(streamVideo, embedBuilder.Build(), NoticeType.NewVideo).ConfigureAwait(false);
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
                    EmbedBuilder embedBuilder = new EmbedBuilder();
                    embedBuilder.WithErrorColor()
                        .WithTitle(streamVideo.VideoTitle)
                        .WithDescription(Format.Url(streamVideo.ChannelTitle, $"https://www.youtube.com/channel/{streamVideo.ChannelId}"))
                        .WithImageUrl($"https://i.ytimg.com/vi/{streamVideo.VideoId}/maxresdefault.jpg")
                        .WithUrl($"https://www.youtube.com/watch?v={streamVideo.VideoId}")
                        .AddField("直播狀態", "尚未開台", true)
                        .AddField("排定開台時間", startTime.ConvertDateTimeToDiscordMarkdown());

                    if (addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo) && !isFromRNRS)
                    {
                        if (!isFirstOther) await SendStreamMessageAsync(streamVideo, embedBuilder.Build(), NoticeType.NewStream).ConfigureAwait(false);
                        StartReminder(streamVideo, streamVideo.ChannelType);
                    }
                }
                else if (startTime > DateTime.Now.AddMinutes(-10) || item.Snippet.LiveBroadcastContent == "live") // 如果開台時間在十分鐘內或已經開台
                {
                    if (addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo) && !isFromRNRS)
                        StartReminder(streamVideo, streamVideo.ChannelType);
                }
                else addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo);
            }
            // 好像從沒看過這個分類觸發過
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
    }
}