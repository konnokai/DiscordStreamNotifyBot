using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.Youtube.Json;
using Google.Apis.YouTube.v3;
using System.Collections.Concurrent;
using TableVideo = DiscordStreamNotifyBot.DataBase.Table.Video;
using YTApiVideo = Google.Apis.YouTube.v3.Data.Video;

using Bot = DiscordStreamNotifyBot.Shared.BotState;

namespace DiscordStreamNotifyBot.Scraper.Detection.Youtube
{
    /// <summary>
    /// YouTube 偵測服務（Scraper 專用）：排程爬取（Holo/Nijisanji/Other）、錄影程序 Redis 訂閱、
    /// PubSubHubbub 維護、到點提醒（reminder）排程，偵測到事件改 publish <see cref="YoutubeNotification"/> /
    /// <see cref="BannerChangeNotification"/> 至通知匯流排。YouTube API 一律經 Shared <see cref="Shared.YoutubeApiService"/>；
    /// 不碰 Discord gateway（發送、建立活動、換橫幅由 Notifier 消費匯流排後執行）。
    /// </summary>
    public partial class YoutubeDetectionService
    {
        public bool IsRecord { get; set; } = true;
        public ConcurrentBag<NijisanjiLiverJson> NijisanjiLiverContents { get; } = new ConcurrentBag<NijisanjiLiverJson>();
        public ConcurrentDictionary<string, ReminderItem> Reminders { get; } = new ConcurrentDictionary<string, ReminderItem>();

        public YouTubeService YouTubeService => _apiService.YouTubeService;

        private static ConcurrentDictionary<string, TableVideo> addNewStreamVideo = new();
        private static HashSet<string> newStreamList = new();

        private bool isSubscribing = false;
        private bool isFirstHolo = true, isFirst2434 = true, isFirstOther = true;
        private Timer holoSchedule, nijisanjiSchedule, otherSchedule, checkScheduleTime, saveDateBase, subscribePubSub, reScheduleTime;
        private Timer channelTitleCheckTimer;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _nijisanjiApiHttpClient;
        private readonly ConcurrentDictionary<string, byte> _endLiveBag = new();
        private readonly MainDbService _dbService;
        private readonly BotConfig _botConfig;
        private readonly Shared.YoutubeApiService _apiService;

        public YoutubeDetectionService(IHttpClientFactory httpClientFactory, BotConfig botConfig, MainDbService dbService, Shared.YoutubeApiService apiService)
        {
            _httpClientFactory = httpClientFactory;
            _dbService = dbService;
            _botConfig = botConfig;
            _apiService = apiService;

            _nijisanjiApiHttpClient = _httpClientFactory.CreateClient();
            _nijisanjiApiHttpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");

            // 錄影程序 Redis 訂閱：偵測到事件改 publish DTO（不直接送 Discord）
            Bot.RedisSub.Subscribe(new RedisChannel("youtube.startstream", RedisChannel.PatternMode.Literal), async (channel, videoData) =>
            {
                try
                {
                    string videoId = "";
                    bool isMemberOnly = false;
                    var tempData = videoData.ToString().Split(':');
                    if (tempData.Length != 2)
                    {
                        Log.Info($"{channel} - {videoData}: 資料數量不正確");
                        videoId = videoData.ToString().Substring(0, 11);
                    }
                    else
                    {
                        videoId = tempData[0];
                        isMemberOnly = tempData[1] == "1";
                    }

                    Log.Info($"{channel} - {videoId}");

                    var item = await GetVideoAsync(videoId).ConfigureAwait(false);
                    if (item == null)
                    {
                        Log.Warn($"{videoId} Delete");
                        await Bot.RedisSub.PublishAsync(new RedisChannel("youtube.deletestream", RedisChannel.PatternMode.Literal), videoId);
                        return;
                    }

                    DateTime startTime;
                    if (!string.IsNullOrEmpty(item.LiveStreamingDetails.ActualStartTimeRaw))
                        startTime = DateTime.Parse(item.LiveStreamingDetails.ActualStartTimeRaw);
                    else
                        startTime = DateTime.Parse(item.LiveStreamingDetails.ScheduledStartTimeRaw);

                    await PublishByVideoIdAsync(item.Id, YoutubeNoticeType.Start, actualStart: startTime, isMemberOnly: isMemberOnly, item: item).ConfigureAwait(false);
                    await PublishBannerAsync(item.Snippet.ChannelId, item.Id);
                }
                catch (Exception ex)
                {
                    Log.Error($"Record-StartStream {ex}");
                }
            });

            Bot.RedisSub.Subscribe(new RedisChannel("youtube.endstream", RedisChannel.PatternMode.Literal), async (channel, videoId) =>
            {
                try
                {
                    Log.Info($"{channel} - {videoId}");

                    if (_endLiveBag.ContainsKey(videoId))
                    {
                        Log.Warn("重複通知，略過");
                        return;
                    }

                    var item = await GetVideoAsync(videoId.ToString()).ConfigureAwait(false);
                    if (item == null)
                    {
                        Log.Warn($"{videoId} Delete");
                        await Bot.RedisSub.PublishAsync(new RedisChannel("youtube.deletestream", RedisChannel.PatternMode.Literal), videoId);
                        return;
                    }

                    if (string.IsNullOrEmpty(item.LiveStreamingDetails.ActualEndTimeRaw))
                    {
                        Log.Warn("還沒關台");
                        return;
                    }

                    _endLiveBag.TryAdd(videoId, 1);

                    var startTime = DateTime.Parse(item.LiveStreamingDetails.ActualStartTimeRaw);
                    var endTime = DateTime.Parse(item.LiveStreamingDetails.ActualEndTimeRaw);

                    await PublishByVideoIdAsync(item.Id, YoutubeNoticeType.End, actualStart: startTime, actualEnd: endTime, item: item).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Error($"Record-EndStream {ex}");
                }
            });

            Bot.RedisSub.Subscribe(new RedisChannel("youtube.memberonly", RedisChannel.PatternMode.Literal), async (channel, videoId) =>
            {
                Log.Info($"{channel} - {videoId}");

                try
                {
                    if (SharedExtensions.HasStreamVideoByVideoId(videoId))
                    {
                        var streamVideo = SharedExtensions.GetStreamVideoByVideoId(videoId);
                        var item = await GetVideoAsync(videoId).ConfigureAwait(false);

                        if (item == null)
                        {
                            Log.Warn($"{videoId} Delete");
                            await Bot.RedisSub.PublishAsync(new RedisChannel("youtube.deletestream", RedisChannel.PatternMode.Literal), videoId);
                            return;
                        }

                        if (string.IsNullOrEmpty(item.LiveStreamingDetails.ActualEndTimeRaw))
                        {
                            Log.Warn("還沒關台");
                            return;
                        }

                        _endLiveBag.TryAdd(videoId, 1);

                        var startTime = DateTime.Parse(item.LiveStreamingDetails.ActualStartTimeRaw);
                        var endTime = DateTime.Parse(item.LiveStreamingDetails.ActualEndTimeRaw);

                        await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.End, actualStart: startTime, actualEnd: endTime, isMemberOnly: true).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Record-MemberOnly {ex}");
                }
            });

            Bot.RedisSub.Subscribe(new RedisChannel("youtube.deletestream", RedisChannel.PatternMode.Literal), async (channel, videoId) =>
            {
                Log.Info($"{channel} - {videoId}");

                _endLiveBag.TryAdd(videoId, 1);

                try
                {
                    if (SharedExtensions.HasStreamVideoByVideoId(videoId))
                    {
                        var streamVideo = SharedExtensions.GetStreamVideoByVideoId(videoId);
                        await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.Delete).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Record-DeleteStream {ex}");
                }
            });

            Bot.RedisSub.Subscribe(new RedisChannel("youtube.unarchived", RedisChannel.PatternMode.Literal), async (channel, videoId) =>
            {
                Log.Info($"{channel} - {videoId}");

                _endLiveBag.TryAdd(videoId, 1);

                try
                {
                    if (SharedExtensions.HasStreamVideoByVideoId(videoId))
                    {
                        var streamVideo = SharedExtensions.GetStreamVideoByVideoId(videoId);
                        await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.Delete).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Record-UnArchived {ex}");
                }
            });

            Bot.RedisSub.Subscribe(new RedisChannel("youtube.429error", RedisChannel.PatternMode.Literal), async (channel, videoId) =>
            {
                Log.Info($"{channel} - {videoId}");
                IsRecord = false;

                try
                {
                    if (SharedExtensions.HasStreamVideoByVideoId(videoId))
                    {
                        var streamVideo = SharedExtensions.GetStreamVideoByVideoId(videoId);
                        await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.Start).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Record-429Error {ex}");
                }
            });

            Bot.RedisSub.Subscribe(new RedisChannel("youtube.addstream", RedisChannel.PatternMode.Literal), async (channel, videoId) =>
            {
                videoId = GetVideoId(videoId);
                Log.Info($"{channel} - (手動新增) {videoId}");

                try
                {
                    if (!addNewStreamVideo.ContainsKey(videoId) && !SharedExtensions.HasStreamVideoByVideoId(videoId))
                    {
                        var item = await GetVideoAsync(videoId).ConfigureAwait(false);
                        if (item == null)
                        {
                            Log.Warn($"{videoId} Delete");
                            return;
                        }

                        try
                        {
                            await AddOtherDataAsync(item);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.Demystify(), $"PubSub-AddStream: {item.Id}");
                        }
                    }
                    else
                    {
                        Log.Warn($"{videoId} 已存在，略過");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"PubSub-AddStream {ex}");
                }
            });

            Bot.RedisSub.Subscribe(new RedisChannel("youtube.pubsub.CreateOrUpdate", RedisChannel.PatternMode.Literal), async (channel, youtubeNotificationJson) =>
            {
                YoutubePubSubNotification youtubePubSubNotification = JsonConvert.DeserializeObject<YoutubePubSubNotification>(youtubeNotificationJson.ToString());

                try
                {
                    using (var db = _dbService.GetDbContext())
                    {
                        if (!addNewStreamVideo.ContainsKey(youtubePubSubNotification.VideoId) && !SharedExtensions.HasStreamVideoByVideoId(youtubePubSubNotification.VideoId))
                        {
                            Log.Info($"{channel} - (新影片) {youtubePubSubNotification.ChannelId}: {youtubePubSubNotification.VideoId}");

                            DataBase.Table.Video streamVideo;
                            var youtubeChannelSpider = db.YoutubeChannelSpider.FirstOrDefault((x) => x.ChannelId == youtubePubSubNotification.ChannelId);

                            if (db.RecordYoutubeChannel.Any((x) => x.YoutubeChannelId == youtubePubSubNotification.ChannelId) // 錄影頻道一律允許
                                || db.NijisanjiVideos.Any((x) => x.ChannelId == youtubePubSubNotification.ChannelId) || // 可能是 2434 的頻道，允許
                                (youtubeChannelSpider != null && youtubeChannelSpider.IsTrustedChannel)) // 否則就確認這是不是允許的爬蟲
                            {
                                var item = await GetVideoAsync(youtubePubSubNotification.VideoId).ConfigureAwait(false);
                                if (item == null)
                                {
                                    Log.Warn($"{youtubePubSubNotification.VideoId} Delete");
                                    return;
                                }

                                try
                                {
                                    await AddOtherDataAsync(item);
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex.Demystify(), $"PubSub_AddData_CreateOrUpdate: {item.Id}");
                                }
                            }
                            else
                            {
                                var videoContent = await GetVideoDurationAsync(youtubePubSubNotification.VideoId);
                                if (videoContent.ContentDetails.Duration == "PT15S")
                                {
                                    var isCommentDisabled = await GetCommentThreadsIsDisabledAsync(youtubePubSubNotification.VideoId);
                                    if (isCommentDisabled)
                                    {
                                        Log.Error($"(新偽裝貼文) | {db.GetNonApprovedChannelTitleByChannelId(youtubePubSubNotification.ChannelId)} ({youtubePubSubNotification.VideoId})");
                                        return;
                                    }
                                }

                                streamVideo = new DataBase.Table.Video()
                                {
                                    ChannelId = youtubePubSubNotification.ChannelId,
                                    ChannelTitle = db.GetNonApprovedChannelTitleByChannelId(youtubePubSubNotification.ChannelId),
                                    VideoId = youtubePubSubNotification.VideoId,
                                    VideoTitle = youtubePubSubNotification.Title,
                                    ScheduledStartTime = youtubePubSubNotification.Published,
                                    ChannelType = DataBase.Table.Video.YTChannelType.NonApproved
                                };

                                Log.New($"(非已認可的新影片) | {youtubePubSubNotification.Published} | {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({streamVideo.VideoId})");

                                if (addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo) && streamVideo.ScheduledStartTime > DateTime.Now.AddDays(-2))
                                    await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.NewVideo).ConfigureAwait(false);
                            }
                        }
                        else Log.Info($"{channel} - (編輯或關台) {youtubePubSubNotification.ChannelId}: {youtubePubSubNotification.VideoId}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"PubSub-CreateOrUpdate {ex}");
                }
            });

            Bot.RedisSub.Subscribe(new RedisChannel("youtube.pubsub.Deleted", RedisChannel.PatternMode.Literal), async (channel, youtubeNotificationJson) =>
            {
                YoutubePubSubNotification youtubePubSubNotification = JsonConvert.DeserializeObject<YoutubePubSubNotification>(youtubeNotificationJson.ToString());

                Log.Info($"{channel} - {youtubePubSubNotification.VideoId}");

                try
                {
                    if (SharedExtensions.HasStreamVideoByVideoId(youtubePubSubNotification.VideoId))
                    {
                        DataBase.Table.Video streamVideo = SharedExtensions.GetStreamVideoByVideoId(youtubePubSubNotification.VideoId);
                        if (streamVideo != null)
                        {
                            await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.Delete).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"PubSub-Deleted {ex}");
                }
            });

            Bot.RedisSub.Subscribe(new RedisChannel("youtube.pubsub.NeedRegister", RedisChannel.PatternMode.Literal), async (channel, channelId) =>
            {
                using (var db = _dbService.GetDbContext())
                {
                    if (db.YoutubeChannelSpider.Any((x) => x.ChannelId == channelId.ToString()))
                    {
                        var youtubeChannelSpider = db.YoutubeChannelSpider.Single((x) => x.ChannelId == channelId.ToString());

                        if (await PostSubscribeRequestAsync(channelId.ToString()))
                        {
                            Log.Info($"已重新註冊 YT PubSub: {youtubeChannelSpider.ChannelTitle} ({channelId})");
                            youtubeChannelSpider.LastSubscribeTime = DateTime.Now;
                            db.Update(youtubeChannelSpider);
                            db.SaveChanges();
                        }
                    }
                    else
                    {
                        Log.Error($"後端通知須重新註冊但資料庫無該 ChannelId 的資料: {channelId}");
                    }
                }
            });

            Log.Info("已建立 Redis 訂閱");

            // owner 控制（取代原 Notifier 指令）：切換錄影 / 強制重新訂閱 PubSub
            Bot.RedisSub.Subscribe(new RedisChannel("youtube.control.toggleRecord", RedisChannel.PatternMode.Literal), (channel, _) =>
            {
                IsRecord = !IsRecord;
                Log.Info($"[控制] 直播錄影已{(IsRecord ? "開啟" : "關閉")}");
            });

            Bot.RedisSub.Subscribe(new RedisChannel("youtube.control.subscribePubSub", RedisChannel.PatternMode.Literal), async (channel, _) =>
            {
                Log.Info("[控制] 收到強制重新註冊 PubSub 要求");
                await SubscribePubSubAsync();
            });

            Bot.RedisSub.Subscribe(new RedisChannel("youtube.control.addVideo", RedisChannel.PatternMode.Literal), async (channel, videoId) =>
            {
                try
                {
                    string id = GetVideoId(videoId);
                    if (addNewStreamVideo.ContainsKey(id) || SharedExtensions.HasStreamVideoByVideoId(id))
                    {
                        Log.Warn($"[控制] addVideo: {id} 已存在，略過");
                        return;
                    }

                    var item = await GetVideoAsync(id).ConfigureAwait(false);
                    if (item == null)
                    {
                        Log.Warn($"[控制] addVideo: {id} 不存在");
                        return;
                    }

                    await AddOtherDataAsync(item);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), $"[控制] addVideo: {videoId}");
                }
            });

            // 偵測 Timer
            reScheduleTime = new Timer((objState) => ReScheduleReminder(), null, TimeSpan.FromSeconds(5), TimeSpan.FromDays(1));
            holoSchedule = new Timer(async (objState) => await HoloScheduleAsync(), null, TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(5));
            nijisanjiSchedule = new Timer(async (objState) => await NijisanjiScheduleAsync(), null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(5));
            otherSchedule = new Timer(async (objState) => await OtherScheduleAsync(), null, TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(5));
            checkScheduleTime = new Timer(async (objState) => await CheckScheduleTime(), null, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15));
            saveDateBase = new Timer((objState) => SaveDateBase(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(3));

#if !RELEASE
            return;
#endif

            subscribePubSub = new Timer(async (objState) => await SubscribePubSubAsync(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(30));

            // 新增每日 00:00 定時檢查 YouTube 頻道名稱的 Timer
            var now = DateTime.Now;
            var nextMidnight = now.Date.AddDays(1);
            var dueTime = nextMidnight - now;
            channelTitleCheckTimer = new Timer(async _ => await CheckAndUpdateYoutubeChannelTitlesAsync(), null, dueTime, TimeSpan.FromDays(1));
        }

        #region 匯流排發布 helper
        /// <summary>偵測端：由 <see cref="TableVideo"/> 建立 DTO 並 publish 至通知匯流排。</summary>
        internal async Task PublishYoutubeNotificationAsync(TableVideo streamVideo, YoutubeNoticeType noticeType,
            DateTime? actualStart = null, DateTime? actualEnd = null, bool isMemberOnly = false)
        {
            try
            {
                var dto = new YoutubeNotification
                {
                    NoticeType = noticeType,
                    VideoId = streamVideo.VideoId,
                    ChannelId = streamVideo.ChannelId,
                    ChannelTitle = streamVideo.ChannelTitle,
                    VideoTitle = streamVideo.VideoTitle,
                    ScheduledStartTime = streamVideo.ScheduledStartTime,
                    ActualStartTime = actualStart,
                    ActualEndTime = actualEnd,
                    IsMemberOnly = isMemberOnly,
                    ChannelType = streamVideo.ChannelType,
                };
                await NotificationBusPublisher.PublishJsonAsync(_botConfig.RabbitMQ, NotifyRoutingKeys.Youtube, dto).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"PublishYoutubeNotificationAsync: {streamVideo.VideoId} / {noticeType}");
            }
        }

        /// <summary>偵測端：以 videoId 查找（DB / addNewStreamVideo / API item）後 publish。對應原 SendStreamMessageAsync(string,...) 行為。</summary>
        private async Task PublishByVideoIdAsync(string videoId, YoutubeNoticeType noticeType,
            DateTime? actualStart = null, DateTime? actualEnd = null, bool isMemberOnly = false, YTApiVideo item = null)
        {
            TableVideo streamVideo = SharedExtensions.GetStreamVideoByVideoId(videoId);

            if (streamVideo == null)
            {
                if (addNewStreamVideo.ContainsKey(videoId))
                {
                    streamVideo = addNewStreamVideo[videoId];
                }
                else
                {
                    try
                    {
                        item ??= await GetVideoAsync(videoId).ConfigureAwait(false);
                        if (item == null) return;

                        if (!DateTime.TryParse(item.LiveStreamingDetails?.ActualStartTimeRaw, out var startTime))
                        {
                            if (!DateTime.TryParse(item.LiveStreamingDetails?.ScheduledStartTimeRaw, out startTime))
                                return;
                        }

                        streamVideo = new TableVideo()
                        {
                            ChannelId = item.Snippet.ChannelId,
                            ChannelTitle = item.Snippet.ChannelTitle,
                            VideoId = item.Id,
                            VideoTitle = item.Snippet.Title,
                            ScheduledStartTime = startTime,
                            ChannelType = TableVideo.YTChannelType.Other
                        };

                        if (!addNewStreamVideo.TryAdd(streamVideo.VideoId, streamVideo))
                            return;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Demystify(), $"PublishByVideoIdAsync-GetVideoAsync: {videoId}");
                        return;
                    }
                }
            }

            await PublishYoutubeNotificationAsync(streamVideo, noticeType, actualStart, actualEnd, isMemberOnly).ConfigureAwait(false);
        }

        /// <summary>偵測端：publish 伺服器橫幅變更事件（由 Notifier 消費後執行 GetGuild + 換 banner）。</summary>
        private async Task PublishBannerAsync(string channelId, string videoId)
        {
            try
            {
                await NotificationBusPublisher.PublishJsonAsync(_botConfig.RabbitMQ,
                    NotifyRoutingKeys.Banner,
                    new BannerChangeNotification { ChannelId = channelId, VideoId = videoId }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"PublishBannerChange: {channelId} / {videoId}");
            }
        }
        #endregion

        #region API 委派（Shared.YoutubeApiService 單一來源）
        public Task<YTApiVideo> GetVideoAsync(string videoId) => _apiService.GetVideoAsync(videoId);
        private Task<IEnumerable<YTApiVideo>> GetVideosAsync(IEnumerable<string> videoIds) => _apiService.GetVideosAsync(videoIds);
        public Task<string> GetChannelIdAsync(string channelUrl) => _apiService.GetChannelIdAsync(channelUrl);
        public string GetVideoId(string videoUrl) => _apiService.GetVideoId(videoUrl);
        public Task<string> GetChannelTitle(string channelId) => _apiService.GetChannelTitle(channelId);
        public Task<bool> PostSubscribeRequestAsync(string channelId, bool subscribe = true) => _apiService.PostSubscribeRequestAsync(channelId, subscribe);
        #endregion

        private bool CanRecord(DataBase.Table.Video streamVideo)
        {
            using var db = _dbService.GetDbContext();
            return IsRecord && db.RecordYoutubeChannel.AsNoTracking().Any((x) => x.YoutubeChannelId.Trim() == streamVideo.ChannelId.Trim());
        }

        internal async Task SubscribePubSubAsync()
        {
            if (isSubscribing)
                return;

            isSubscribing = true;

            try
            {
                using var db = _dbService.GetDbContext();
                var list = await db.YoutubeChannelSpider
                    .AsNoTracking()
                    .Where(x => x.LastSubscribeTime < DateTime.Now.AddDays(-7))
                    .ToListAsync();

                if (list.Count != 0)
                {
                    int i = 0;
                    foreach (var item in list)
                    {
                        i++;
                        if (await PostSubscribeRequestAsync(item.ChannelId))
                        {
                            Log.Info($"已註冊 YT PubSub: {item.ChannelTitle} ({item.ChannelId}) ({i}/{list.Count})");
                        }
                        else
                        {
                            Log.Warn($"註冊 YT PubSub 失敗: {item.ChannelTitle} ({item.ChannelId}) ({i}/{list.Count})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "SubscribePubSubAsync Error");
            }
            finally
            {
                isSubscribing = false;
            }
        }

        /// <summary>每天 00:00 檢查所有 YoutubeChannelSpider 的頻道名稱，若有異動則自動更新。</summary>
        private async Task CheckAndUpdateYoutubeChannelTitlesAsync()
        {
            int updatedCount = 0;
            int totalCount = 0;
            try
            {
                using (var db = _dbService.GetDbContext())
                {
                    var allChannels = db.YoutubeChannelSpider.AsNoTracking().ToList();
                    var channelIdList = allChannels.Select(x => x.ChannelId).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
                    totalCount = channelIdList.Count;
                    var chunkSize = 50;
                    for (int i = 0; i < channelIdList.Count; i += chunkSize)
                    {
                        var chunk = channelIdList.Skip(i).Take(chunkSize).ToList();
                        try
                        {
                            var request = YouTubeService.Channels.List("snippet");
                            request.Id = string.Join(",", chunk);
                            var response = await request.ExecuteAsync();
                            var channelTitleDict = response.Items.ToDictionary(x => x.Id, x => x.Snippet.Title);

                            foreach (var channelId in chunk)
                            {
                                var spider = db.YoutubeChannelSpider.FirstOrDefault(x => x.ChannelId == channelId);
                                if (spider == null) continue;
                                if (channelTitleDict.TryGetValue(channelId, out var newTitle))
                                {
                                    if (spider.ChannelTitle != newTitle)
                                    {
                                        spider.ChannelTitle = newTitle;
                                        db.YoutubeChannelSpider.Update(spider);
                                        updatedCount++;
                                    }
                                }
                                else
                                {
                                    Log.Warn($"YouTube API 查無此頻道或回傳異常: {channelId}");
                                }
                            }
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            Log.Warn($"YouTube API 批次查詢失敗: {ex.Message}");
                        }
                    }
                }
                Log.Info($"YouTube 頻道名稱每日檢查：已更新 {updatedCount} / {totalCount} 個頻道");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"每日 YouTube 頻道名稱檢查任務失敗");
            }
        }

        private async Task GetOrCreateNijisanjiLiverListAsync(string affiliation, bool forceRefresh = false)
        {
            if (!forceRefresh)
            {
                try
                {
                    if (await Bot.RedisDb.KeyExistsAsync($"youtube.nijisanji.liver.{affiliation}"))
                    {
                        var liver = JsonConvert.DeserializeObject<List<NijisanjiLiverJson>>(await Bot.RedisDb.StringGetAsync($"youtube.nijisanji.liver.{affiliation}"));
                        foreach (var item in liver)
                        {
                            NijisanjiLiverContents.Add(item);
                        }
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), $"GetOrCreateNijisanjiLiverListAsync-GetRedisData-{affiliation}");
                }
            }

            try
            {
                var json = await _nijisanjiApiHttpClient.GetStringAsync($"https://www.nijisanji.jp/api/livers?limit=300&orderKey=subscriber_count&order=asc&affiliation={affiliation}&locale=ja&includeAll=true");
                var liver = JsonConvert.DeserializeObject<List<NijisanjiLiverJson>>(json);
                await Bot.RedisDb.StringSetAsync($"youtube.nijisanji.liver.{affiliation}", JsonConvert.SerializeObject(liver), TimeSpan.FromDays(1));
                foreach (var item in liver)
                {
                    NijisanjiLiverContents.Add(item);
                }
                Log.New($"GetOrCreateNijisanjiLiverListAsync: {affiliation} 已刷新");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"GetOrCreateNijisanjiLiverListAsync-GetLiver-{affiliation}");
            }
        }
    }

    public class ReminderItem
    {
        public DataBase.Table.Video StreamVideo { get; set; }
        public Timer Timer { get; set; }
        public DataBase.Table.Video.YTChannelType ChannelType { get; set; }
    }

    public class YoutubePubSubNotification
    {
        public enum YTNotificationType { CreateOrUpdated, Deleted }

        public YTNotificationType NotificationType { get; set; } = YTNotificationType.CreateOrUpdated;
        public string VideoId { get; set; }
        public string ChannelId { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public DateTime Published { get; set; }
        public DateTime Updated { get; set; }

        public override string ToString()
        {
            switch (NotificationType)
            {
                case YTNotificationType.CreateOrUpdated:
                    return $"({NotificationType} at {Updated}) {ChannelId} - {VideoId} | {Title}";
                case YTNotificationType.Deleted:
                    return $"({NotificationType} at {Published}) {ChannelId} - {VideoId}";
            }
            return "";
        }
    }

    public static class Ext
    {
        public static DateTime? ConvertDateTime(this string text)
        {
            try
            {
                return Convert.ToDateTime(text);
            }
            catch
            {
                return new DateTime();
            }
        }
    }
}
