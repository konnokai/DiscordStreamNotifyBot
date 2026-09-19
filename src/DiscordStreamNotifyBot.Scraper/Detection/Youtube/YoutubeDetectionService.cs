using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.Youtube;
using DiscordStreamNotifyBot.SharedService.Youtube.Json;
using Google.Apis.YouTube.v3;
using System.Collections.Concurrent;
using Bot = DiscordStreamNotifyBot.Shared.BotState;
using TableVideo = DiscordStreamNotifyBot.DataBase.Table.Video;
using YTApiVideo = Google.Apis.YouTube.v3.Data.Video;

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

        private static readonly TimeSpan NewStreamClaimTtl = TimeSpan.FromHours(24);
        private static ConcurrentDictionary<string, TableVideo> addNewStreamVideo = new();

        private readonly YoutubeVideoClaimCache _newStreamClaims = new(TimeProvider.System, NewStreamClaimTtl);

        private bool isFirstHolo = true, isFirst2434 = true, isFirstOther = true;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _nijisanjiApiHttpClient;
        private readonly YoutubeTerminalEventRegistry _terminalEvents = new();
        private readonly MainDbService _dbService;
        private readonly Shared.YoutubeApiService _apiService;

        public YoutubeDetectionService(IHttpClientFactory httpClientFactory, BotConfig botConfig, MainDbService dbService,
            Shared.YoutubeApiService apiService, SharedService.Youtube.YoutubeWebSubService webSubService,
            SharedService.Youtube.IYoutubeAtomValidatorStore atomValidators)
        {
            _httpClientFactory = httpClientFactory;
            _dbService = dbService;
            _apiService = apiService;
            _webSubService = webSubService;
            _atomFallback = new YoutubeAtomFallback(httpClientFactory, atomValidators, ListAtomChannelIdsAsync, ProcessAtomVideoIdsAsync);

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
                        Log.Info($"{channel} - {videoData}: 資料欄位數不正確");
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
                        Log.Warn($"找不到影片，發布刪除事件：{videoId}");
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

                    var item = await GetVideoAsync(videoId.ToString()).ConfigureAwait(false);
                    if (item == null)
                    {
                        Log.Warn($"找不到影片，發布刪除事件：{videoId}");
                        await Bot.RedisSub.PublishAsync(new RedisChannel("youtube.deletestream", RedisChannel.PatternMode.Literal), videoId);
                        return;
                    }

                    if (string.IsNullOrEmpty(item.LiveStreamingDetails.ActualEndTimeRaw))
                    {
                        Log.Warn("還沒關台");
                        return;
                    }

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
                            Log.Warn($"找不到影片，發布刪除事件：{videoId}");
                            await Bot.RedisSub.PublishAsync(new RedisChannel("youtube.deletestream", RedisChannel.PatternMode.Literal), videoId);
                            return;
                        }

                        if (string.IsNullOrEmpty(item.LiveStreamingDetails.ActualEndTimeRaw))
                        {
                            Log.Warn("還沒關台");
                            return;
                        }

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

                try
                {
                    if (SharedExtensions.HasStreamVideoByVideoId(videoId))
                    {
                        var streamVideo = SharedExtensions.GetStreamVideoByVideoId(videoId);
                        await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.Delete,
                            isUnarchived: true).ConfigureAwait(false);
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
                Log.Info($"{channel} - （手動新增） {videoId}");

                try
                {
                    if (!addNewStreamVideo.ContainsKey(videoId) && !SharedExtensions.HasStreamVideoByVideoId(videoId))
                    {
                        var item = await GetVideoAsync(videoId).ConfigureAwait(false);
                        if (item == null)
                        {
                            Log.Warn($"找不到影片：{videoId}");
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
                YoutubePubSubNotification youtubePubSubNotification;
                try
                {
                    youtubePubSubNotification = JsonConvert.DeserializeObject<YoutubePubSubNotification>(youtubeNotificationJson.ToString());
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), $"PubSub-CreateOrUpdate-Deserialize: {youtubeNotificationJson}");
                    return;
                }

                if (youtubePubSubNotification == null || string.IsNullOrWhiteSpace(youtubePubSubNotification.VideoId))
                {
                    Log.Warn($"{channel} - 收到沒有 videoId 的 PubSub payload");
                    return;
                }

                // 與 Atom fallback 共用同一組 claim 與同一個處置入口，兩邊不會各自查 API 後重複通知（計畫 §5.6、§9.4）。
                using var claims = _newStreamClaims.CreateBatch();

                try
                {
                    if (ClaimUnknownVideo(youtubePubSubNotification.VideoId, claims) != UnknownVideoClaim.Claimed)
                    {
                        Log.Info($"{channel} - （已知或處理中） {youtubePubSubNotification.ChannelId}：{youtubePubSubNotification.VideoId}");
                        return;
                    }

                    if (await ProcessDiscoveredVideoAsync(
                            youtubePubSubNotification.VideoId,
                            youtubePubSubNotification.ChannelId,
                            youtubePubSubNotification.Title,
                            youtubePubSubNotification.Published).ConfigureAwait(false))
                    {
                        claims.Complete(youtubePubSubNotification.VideoId);
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
                // 這個 callback 是 async void（StackExchange.Redis 的 Action overload）：任何例外都會逃到程序層，
                // 因此解析、DB 查詢與送出全部包在例外邊界內。
                try
                {
                    string id = channelId.ToString();

                    using (var db = _dbService.GetDbContext())
                    {
                        if (db.YoutubeChannelSpider.Any((x) => x.ChannelId == id))
                        {
                            // Backend 找不到 HMAC secret 時的補送；LastSubscribeTime 一律等 challenge 成功才由 Backend 更新。
                            var result = await RequestWebSubSubscribeAsync(id, force: false, renewDue: false);
                            LogWebSubResult(id, result, "NeedRegister");
                        }
                        else
                        {
                            Log.Error($"後端要求重新註冊，但資料庫中沒有 ChannelId 為 {id} 的資料。");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), $"PubSub-NeedRegister: {channelId}");
                }
            });

            Log.Info("已建立 Redis 訂閱");

            // 由 Bot 擁有者控制（取代原本的 Notifier 指令）：切換錄影 / 強制重新訂閱 PubSub
            Bot.RedisSub.Subscribe(new RedisChannel("youtube.control.toggleRecord", RedisChannel.PatternMode.Literal), (channel, _) =>
            {
                IsRecord = !IsRecord;
                Log.Info($"[控制] 直播錄影已{(IsRecord ? "開啟" : "關閉")}");
            });

            Bot.RedisSub.Subscribe(new RedisChannel("youtube.control.subscribePubSub", RedisChannel.PatternMode.Literal), async (channel, _) =>
            {
                Log.Info("[控制] 收到強制重新註冊 PubSub 要求");
                await SubscribePubSubAsync(force: true);
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

            // 偵測排程（計畫 §12.1）：PeriodicRunner 以背景輪詢執行，支援 await、避免重入，並使用 CancellationToken。
            var token = GracefulShutdown.Token;
            PeriodicRunner.RunAsync("YT-reSchedule", TimeSpan.FromSeconds(5), TimeSpan.FromDays(1), () => { ReScheduleReminder(); return Task.CompletedTask; }, token);
            PeriodicRunner.RunAsync("YT-holo", TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(5), HoloScheduleAsync, token);
            PeriodicRunner.RunAsync("YT-niji", TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(5), NijisanjiScheduleAsync, token);
            PeriodicRunner.RunAsync("YT-other", TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(5), OtherScheduleAsync, token);
            PeriodicRunner.RunAsync("YT-checkSchedule", TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15), CheckScheduleTime, token);
            PeriodicRunner.RunAsync("YT-saveDb", TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(3), () =>
            {
                SaveDateBase();
                _newStreamClaims.RemoveExpired();
                return Task.CompletedTask;
            }, token);

#if !RELEASE
            return;
#endif

            PeriodicRunner.RunAsync("YT-subscribePubSub", TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(30), () => SubscribePubSubAsync(), token);

            // Atom 補償輪詢（計畫 §6.4）：WebSub 仍是主要來源，這裡只補「續訂沒有被確認」的頻道，不另建分類或通知邏輯。
            PeriodicRunner.RunAsync("YT-atom", TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(15), AtomFallbackAsync, token);

            // 會限影片探索（原 Notifier 每 5 分鐘 Timer，搬來 Scraper 單例執行，避免多 shard 重複燒配額）
            PeriodicRunner.RunAsync("YT-memberVideoCheck", TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5), CheckMemberShipOnlyVideoIdAsync, token);

            // 每日 00:00 定時檢查 YouTube 頻道名稱
            var now = DateTime.Now;
            var dueTime = now.Date.AddDays(1) - now;
            PeriodicRunner.RunAsync("YT-channelTitleCheck", dueTime, TimeSpan.FromDays(1), CheckAndUpdateYoutubeChannelTitlesAsync, token);
        }

        #region 匯流排發布 helper
        /// <summary>
        /// 偵測端：由 <see cref="TableVideo"/> 建立 DTO 並 publish 至通知匯流排。
        /// 回傳 false 代表發布失敗（已記錄），呼叫端可據此保留補償用的狀態（例如 Atom validator）。
        /// </summary>
        internal async Task<bool> PublishYoutubeNotificationAsync(TableVideo streamVideo, YoutubeNoticeType noticeType,
            DateTime? actualStart = null, DateTime? actualEnd = null, bool isMemberOnly = false,
            DateTime? previousScheduledStartTime = null, bool isUnarchived = false)
        {
            var terminalKind = YoutubeTerminalEventRegistry.Classify(noticeType, isMemberOnly, isUnarchived);
            var dto = new YoutubeNotification
            {
                NoticeType = noticeType,
                VideoId = streamVideo.VideoId,
                ChannelId = streamVideo.ChannelId,
                ChannelTitle = streamVideo.ChannelTitle,
                VideoTitle = streamVideo.VideoTitle,
                ScheduledStartTime = streamVideo.ScheduledStartTime,
                PreviousScheduledStartTime = previousScheduledStartTime,
                ActualStartTime = actualStart,
                ActualEndTime = actualEnd,
                IsMemberOnly = isMemberOnly,
                IsUnarchived = isUnarchived,
                ChannelType = streamVideo.ChannelType,
            };

            try
            {
                if (terminalKind.HasValue)
                {
                    var decision = await _terminalEvents.ExecuteOnceAsync(
                        streamVideo.VideoId,
                        terminalKind.Value,
                        () => NotificationBus.PublishAsync(Bot.RedisDb, NotifyType.Youtube, dto)).ConfigureAwait(false);
                    if (decision.Action == YoutubeTerminalEventAction.IgnoreDuplicate)
                    {
                        Log.Warn($"YouTube 終止事件重複通知，略過：{streamVideo.VideoId} / {terminalKind}，已處理 {decision.ClaimedKind}");
                    }
                    return true;
                }

                await NotificationBus.PublishAsync(Bot.RedisDb, NotifyType.Youtube, dto).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"PublishYoutubeNotificationAsync: {streamVideo.VideoId} / {noticeType}");
                return false;
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

        /// <summary>偵測端：publish 伺服器橫幅變更事件（由 Notifier 消費後呼叫 GetGuild 並更新伺服器橫幅）。</summary>
        private async Task PublishBannerAsync(string channelId, string videoId)
        {
            try
            {
                await NotificationBus.PublishAsync(Bot.RedisDb,
                    NotifyType.Banner,
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
        private Task<IEnumerable<YTApiVideo>> GetVideosAsync(IEnumerable<string> videoIds, CancellationToken cancellationToken = default)
            => _apiService.GetVideosAsync(videoIds, cancellationToken);
        public Task<string> GetChannelIdAsync(string channelUrl) => _apiService.GetChannelIdAsync(channelUrl);
        public string GetVideoId(string videoUrl) => _apiService.GetVideoId(videoUrl);
        public Task<string> GetChannelTitle(string channelId) => _apiService.GetChannelTitle(channelId);

        /// <summary>對 Google Hub 送出單一頻道的 WebSub subscribe；<paramref name="force"/> 供 owner 強制重新訂閱（可清除 denied）。</summary>
        private Task<YoutubeWebSubRequestResult> RequestWebSubSubscribeAsync(string channelId, bool force, bool renewDue)
            => _webSubService.RequestAsync(channelId, subscribe: true, force: force, renewDue: renewDue);

        private static void LogWebSubResult(string channelId, YoutubeWebSubRequestResult result, string source)
        {
            switch (result.Outcome)
            {
                case YoutubeWebSubRequestOutcome.Accepted:
                    Log.Info($"已送出 YT WebSub 訂閱要求，等待 challenge：{channelId}（{source}）");
                    break;
                case YoutubeWebSubRequestOutcome.Suppressed:
                    Log.Warn($"YT WebSub 訂閱先前已被 Hub 拒絕，未重送：{channelId}（{source}）");
                    break;
                case YoutubeWebSubRequestOutcome.PermanentFailure:
                    Log.Error($"YT WebSub 訂閱被永久拒絕，略過該頻道：{channelId} / HTTP {(int?)result.StatusCode} / {result.DiagnosticSummary}（{source}）");
                    break;
                case YoutubeWebSubRequestOutcome.TransientFailure:
                    Log.Warn($"YT WebSub 訂閱暫時失敗：{channelId} / HTTP {(int?)result.StatusCode} / {result.DiagnosticSummary}（{source}）");
                    break;
            }
        }
        #endregion

        /// <summary>取得未知影片 claim 的結果；呼叫端必須區分「別人正在處理」與「已知」。</summary>
        private enum UnknownVideoClaim
        {
            /// <summary>已取得 claim，可以繼續查詢與處理。</summary>
            Claimed,

            /// <summary>已存在於待寫入集合或資料庫，不需要再處理。</summary>
            AlreadyKnown,

            /// <summary>其他排程持有 claim，結果尚未確定。</summary>
            Busy,
        }

        /// <summary>
        /// 依序使用程序內 claim、待寫入集合與資料庫判斷影片是否需要進一步處理。
        /// <para>
        /// 已完成的 claim 會留在快取直到 TTL 到期，因此「已知」必須先於 claim 判斷，否則剛處理過的影片
        /// 會被下一個排程當成「處理中」，讓 Atom 的 validator 整整一天無法前進（每輪重抓完整 feed）。
        /// </para>
        /// <para>
        /// WebSub 與 Atom 都必須經過這裡，才會收斂到同一條去重路徑（計畫 §5.6、§9.4）。
        /// </para>
        /// </summary>
        private UnknownVideoClaim ClaimUnknownVideo(string videoId, YoutubeVideoClaimCache.Batch claims)
        {
            if (addNewStreamVideo.ContainsKey(videoId) || SharedExtensions.HasStreamVideoByVideoId(videoId))
                return UnknownVideoClaim.AlreadyKnown;

            return claims.TryClaim(videoId) ? UnknownVideoClaim.Claimed : UnknownVideoClaim.Busy;
        }

        private bool TryClaimUnknownVideo(string videoId, YoutubeVideoClaimCache.Batch claims)
            => ClaimUnknownVideo(videoId, claims) == UnknownVideoClaim.Claimed;

        /// <summary>
        /// WebSub 與 Atom 共用的影片處置入口（計畫 §5.6）：先判斷頻道是否已認可
        /// （錄影頻道／2434／trusted crawler），已認可走既有分類 <see cref="AddOtherDataAsync"/>，
        /// 否則走非認可影片（偽裝貼文判定＋<c>NonApproved</c>）流程。
        /// </summary>
        /// <param name="prefetched">呼叫端已取得的 API 資料；Atom 批次查詢後直接沿用，避免重複查詢。</param>
        /// <returns>false 代表尚未成功處理，呼叫端必須釋放 claim 並保留補償狀態。</returns>
        private async Task<bool> ProcessDiscoveredVideoAsync(
            string videoId, string channelId, string title, DateTime published, YTApiVideo prefetched = null)
        {
            bool isApprovedChannel;
            using (var db = _dbService.GetDbContext())
            {
                isApprovedChannel = db.RecordYoutubeChannel.AsNoTracking().Any((x) => x.YoutubeChannelId == channelId) // 錄影頻道一律允許
                    || db.NijisanjiVideos.AsNoTracking().Any((x) => x.ChannelId == channelId) // 可能是 2434 的頻道，允許
                    || (db.YoutubeChannelSpider.AsNoTracking().FirstOrDefault((x) => x.ChannelId == channelId)?.IsTrustedChannel ?? false);
            }

            if (isApprovedChannel)
            {
                YTApiVideo item = prefetched ?? await GetVideoAsync(videoId).ConfigureAwait(false);
                if (item == null)
                {
                    Log.Warn($"找不到影片：{videoId}");
                    return false;
                }

                try
                {
                    return await AddOtherDataAsync(item).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), $"ProcessDiscoveredVideo: {videoId}");
                    return false;
                }
            }

            var videoContent = await GetVideoDurationAsync(videoId).ConfigureAwait(false);
            string duration = videoContent?.ContentDetails?.Duration;
            if (duration == null)
            {
                // API 沒回傳就不建立假資料；claim 由呼叫端釋放，之後可重試（計畫 §9.4）。
                Log.Warn($"取得影片長度失敗，暫不建立非認可影片：{videoId}");
                return false;
            }

            if (duration == "PT15S" && await GetCommentThreadsIsDisabledAsync(videoId).ConfigureAwait(false))
            {
                Log.Error($"（新偽裝貼文） | {GetNonApprovedChannelTitle(channelId)} ({videoId})");
                return true;
            }

            var streamVideo = new DataBase.Table.Video()
            {
                ChannelId = channelId,
                ChannelTitle = GetNonApprovedChannelTitle(channelId),
                VideoId = videoId,
                VideoTitle = title,
                ScheduledStartTime = published,
                ChannelType = DataBase.Table.Video.YTChannelType.NonApproved
            };

            Log.New($"（非已認可的新影片） | {published} | {streamVideo.ChannelTitle} - {streamVideo.VideoTitle} ({videoId})");

            if (!addNewStreamVideo.TryAdd(videoId, streamVideo) || published <= DateTime.Now.AddDays(-2))
                return true;

            if (await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.NewVideo).ConfigureAwait(false))
                return true;

            addNewStreamVideo.TryRemove(videoId, out _);
            return false;
        }

        private string GetNonApprovedChannelTitle(string channelId)
        {
            using var db = _dbService.GetDbContext();
            return db.GetNonApprovedChannelTitleByChannelId(channelId);
        }

        private bool CanRecord(DataBase.Table.Video streamVideo)
        {
            using var db = _dbService.GetDbContext();
            return IsRecord && db.RecordYoutubeChannel.AsNoTracking().Any((x) => x.YoutubeChannelId.Trim() == streamVideo.ChannelId.Trim());
        }

        /// <summary>
        /// 定期續訂（計畫 §8.3）：以既有 7 天政策加上「HMAC secret 缺失或 TTL 已接近緩衝」挑出需要續訂的頻道，
        /// 逐一送出 subscribe。暫時性失敗停止本輪（交給下一個週期），永久性失敗只略過該頻道。
        /// </summary>
        internal async Task SubscribePubSubAsync(bool force = false)
        {
            try
            {
                List<ChannelSubscriptionState> channels;
                using (var db = _dbService.GetDbContext())
                {
                    channels = await db.YoutubeChannelSpider
                        .AsNoTracking()
                        .Select((x) => new ChannelSubscriptionState(x.ChannelId, x.LastSubscribeTime))
                        .ToListAsync();
                }

                if (channels.Count == 0)
                    return;

                int renewed = 0;
                foreach (var item in channels)
                {
                    if (Bot.IsDisconnect)
                        break;

                    bool renewDue;
                    try
                    {
                        renewDue = force || await _webSubService.IsRenewalDueAsync(item.ChannelId, item.LastSubscribeTime, DateTime.Now);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Demystify(), $"檢查 YT WebSub 訂閱狀態失敗，略過該頻道：{item.ChannelId}");
                        continue;
                    }

                    if (!renewDue)
                        continue;

                    var result = await RequestWebSubSubscribeAsync(item.ChannelId, force, renewDue: true);
                    renewed++;
                    LogWebSubResult(item.ChannelId, result, "定期續訂");

                    // 429／5xx／網路錯誤代表 Hub 端有狀況，停止本輪避免無意義的重試。
                    if (result.Outcome == YoutubeWebSubRequestOutcome.TransientFailure)
                    {
                        Log.Warn($"YT WebSub 續訂暫時失敗，停止本輪（已處理 {renewed}/{channels.Count} 個頻道）");
                        break;
                    }
                }

                if (renewed != 0)
                    Log.Info($"YT WebSub 續訂本輪結束：已送出 {renewed} 個頻道（共 {channels.Count} 個）");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "SubscribePubSubAsync 發生錯誤");
            }
        }

        /// <summary>續訂篩選所需的最小欄位，避免把整個 entity 拉進記憶體。</summary>
        private sealed record ChannelSubscriptionState(string ChannelId, DateTime LastSubscribeTime);

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
                                    Log.Warn($"YouTube API 查無此頻道或回傳異常：{channelId}");
                                }
                            }
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            Log.Warn($"YouTube API 批次查詢失敗：{ex.Message}");
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
                Log.New($"GetOrCreateNijisanjiLiverListAsync: {affiliation} 已更新");
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
        public int RetryPending;
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
}
