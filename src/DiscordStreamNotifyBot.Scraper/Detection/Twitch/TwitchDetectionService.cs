using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Scraper.Detection.Twitch.Debounce;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.Twitch;
using System.Collections.Concurrent;
using EventSubSubscription = TwitchLib.Api.Helix.Models.EventSub.EventSubSubscription;
using HelixStream = TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream;

using Bot = DiscordStreamNotifyBot.Shared.BotState;

namespace DiscordStreamNotifyBot.Scraper.Detection.Twitch
{
    /// <summary>
    /// Twitch 偵測服務（Scraper 專用）：EventSub callback、雙頻率補償輪詢、subscription reconcile、錄影，
    /// 偵測到事件後 publish <see cref="TwitchNotification"/> 至通知匯流排。
    /// </summary>
    public class TwitchDetectionService
    {
        private static readonly TimeSpan OfflineDebounce = TimeSpan.FromMinutes(3);

        private readonly TwitchApiService _apiService;
        private readonly MainDbService _dbService;
        private readonly BotConfig _botConfig;
        private readonly ScraperMetrics _metrics;
        private readonly TwitchGuildEligibilityEvaluator _guildEligibility;
        // 程序內去重搭配 Redis 去重鍵：前者擋同程序重複 callback，後者涵蓋重啟與多來源事件。
        private readonly ConcurrentDictionary<string, byte> _handledStreamIds = new(StringComparer.Ordinal);
        // 同一頻道短時間內的標題與分類變更會先彙整，避免連續發布多則通知。
        private readonly ConcurrentDictionary<string, DebounceChannelUpdateMessage> _debounceChannelUpdateMessage = new(StringComparer.Ordinal);
        // 關台事件先延遲確認；若頻道在等待期間恢復直播，會取消對應工作。
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _streamOfflineReminders = new(StringComparer.Ordinal);
        // EventSub callback、輪詢與 reconcile 可能同時處理同一 broadcaster，必須依使用者序列化狀態變更。
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _userLocks = new(StringComparer.Ordinal);
        // 尚未完成安全清理的頻道會加入高頻輪詢，直到能確認直播與授權狀態。
        private readonly ConcurrentDictionary<string, byte> _pendingCleanup = new(StringComparer.Ordinal);
        // 延後清理原因只供 metrics 分類；是否待重試以 _pendingCleanup 為準。
        private readonly ConcurrentDictionary<string, TwitchEventSubCleanupDeferredMetricReason> _deferredCleanup = new(StringComparer.Ordinal);
        // 防止高低頻輪詢或兩次全量 reconcile 彼此重入。
        private readonly SemaphoreSlim _pollLock = new(1, 1);
        private readonly SemaphoreSlim _fullReconcileLock = new(1, 1);

        public TwitchDetectionService(TwitchApiService apiService, BotConfig botConfig, MainDbService dbService,
            ScraperMetrics metrics, ClusterService clusterService)
        {
            _apiService = apiService;
            _botConfig = botConfig;
            _dbService = dbService;
            _metrics = metrics;
            _guildEligibility = new TwitchGuildEligibilityEvaluator(clusterService);

            if (!_apiService.IsEnable)
            {
                Log.Warn("Twitch API 未啟用，Twitch 偵測不啟動");
                return;
            }

            SubscribeRedisEvents();

            var token = GracefulShutdown.Token;
            PeriodicRunner.RunAsync("Twitch-high-frequency-poll", TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30),
                () => RunPollCycleAsync(pollAllSpiders: false), token);
            PeriodicRunner.RunAsync("Twitch-low-frequency-reconcile", TimeSpan.Zero, TimeSpan.FromMinutes(5),
                RunLowFrequencyCycleAsync, token);
        }

        private void SubscribeRedisEvents()
        {
            // Backend 將 Twitch webhook 轉送到 Redis Pub/Sub；直播通知則另走 NotificationBus Redis Stream。
            Bot.RedisSub.Subscribe(new RedisChannel(RedisChannels.Twitch.StreamOnline, RedisChannel.PatternMode.Literal),
                (channel, value) => _ = HandleStreamOnlineMessageAsync(value));
            Bot.RedisSub.Subscribe(new RedisChannel(RedisChannels.Twitch.ChannelUpdate, RedisChannel.PatternMode.Literal),
                (channel, value) => _ = HandleChannelUpdateMessageAsync(value));
            Bot.RedisSub.Subscribe(new RedisChannel(RedisChannels.Twitch.StreamOffline, RedisChannel.PatternMode.Literal),
                (_, value) => HandleStreamOfflineMessage(value));
            Bot.RedisSub.Subscribe(new RedisChannel(RedisChannels.Twitch.AuthorizationChanged, RedisChannel.PatternMode.Literal),
                (channel, value) => _ = HandleAuthorizationChangedMessageAsync(value));
            Bot.RedisSub.Subscribe(new RedisChannel(RedisChannels.Twitch.ReconcileRequested, RedisChannel.PatternMode.Literal),
                (channel, value) => _ = HandleReconcileRequestedMessageAsync(value));
        }

        private async Task HandleStreamOnlineMessageAsync(RedisValue value)
        {
            try
            {
                var payload = JsonConvert.DeserializeObject<TwitchStreamEventPayload>(value!);
                if (string.IsNullOrWhiteSpace(payload?.BroadcasterUserId))
                {
                    Log.Warn("收到缺少 TwitchUserId 的 stream_online payload，已忽略");
                    return;
                }

                var streams = await _apiService.GetNowStreamsResultAsync(payload.BroadcasterUserId);
                if (!streams.IsSuccess)
                    return;

                var stream = streams.Streams.FirstOrDefault(x => x.UserId == payload.BroadcasterUserId);
                if (stream == null)
                {
                    Log.Warn($"收到 Twitch stream_online，但 Helix 尚未查到直播: {payload.BroadcasterUserId}");
                    return;
                }

                await HandleStreamStartedAsync(stream);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "處理 Twitch stream_online 失敗");
            }
        }

        private async Task HandleAuthorizationChangedMessageAsync(RedisValue value)
        {
            TwitchAuthorizationChangedPayload payload;
            try
            {
                payload = JsonConvert.DeserializeObject<TwitchAuthorizationChangedPayload>(value!);
                if (string.IsNullOrWhiteSpace(payload?.TwitchUserId))
                    throw new InvalidOperationException("payload 缺少 TwitchUserId");
            }
            catch (Exception ex)
            {
                _metrics.RecordAuthorizationChange(TwitchAuthorizationChangeMetricResult.Failure);
                Log.Error(ex.Demystify(), "解析 Twitch authorization_changed 失敗");
                return;
            }

            _metrics.RecordAuthorizationChange(ParseAuthorizationChangeResult(payload.Status));
            await ReconcileUserAsync(payload.TwitchUserId, recordMetric: true, refreshMetrics: true);
        }

        private async Task HandleReconcileRequestedMessageAsync(RedisValue value)
        {
            try
            {
                var payload = JsonConvert.DeserializeObject<TwitchReconcileRequestedPayload>(value!);
                if (string.IsNullOrWhiteSpace(payload?.TwitchUserId))
                {
                    Log.Warn("收到缺少 TwitchUserId 的 Twitch reconcile request，已忽略");
                    return;
                }

                if (string.Equals(payload.Reason, "oauth_bypass_addition", StringComparison.OrdinalIgnoreCase))
                    _metrics.RecordOAuthBypassAddition();

                Log.Info($"收到 Twitch 單頻道 reconcile: {payload.TwitchUserId}（{payload.Reason ?? "未提供原因"}）");
                await ReconcileUserAsync(payload.TwitchUserId, recordMetric: true, refreshMetrics: true);
            }
            catch (Exception ex)
            {
                _metrics.RecordReconcile(ScraperMetricResult.Failure);
                Log.Error(ex.Demystify(), "處理 Twitch reconcile request 失敗");
            }
        }

        private void HandleStreamOfflineMessage(RedisValue value)
        {
            try
            {
                var payload = JsonConvert.DeserializeObject<TwitchStreamEventPayload>(value!);
                if (string.IsNullOrWhiteSpace(payload?.BroadcasterUserId))
                {
                    Log.Warn("收到缺少 TwitchUserId 的 stream_offline payload，已忽略");
                    return;
                }

                Log.Info($"Twitch 直播離線: {payload.BroadcasterUserLogin} ({payload.BroadcasterUserId})，等待三分鐘後確認");
                ScheduleOfflineCleanup(payload.BroadcasterUserId, payload.BroadcasterUserLogin,
                    payload.BroadcasterUserName, replaceExisting: true);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "處理 Twitch stream_offline 失敗");
            }
        }

        private async Task HandleChannelUpdateMessageAsync(RedisValue value)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<TwitchLib.EventSub.Core.SubscriptionTypes.Channel.ChannelUpdate>(value!)!;
                if (string.IsNullOrWhiteSpace(data?.BroadcasterUserId))
                    return;

                var userLock = GetUserLock(data.BroadcasterUserId);
                await userLock.WaitAsync();
                try
                {
                    Log.Info($"Twitch 頻道更新: {data.BroadcasterUserName} - {data.Title} ({data.CategoryName})");
                    var twitchStream = await GetStreamStateAsync(data.BroadcasterUserId);
                    if (twitchStream == null)
                    {
                        Log.Warn($"Redis 找不到 Twitch 頻道資料，忽略: {data.BroadcasterUserName}");
                        return;
                    }

                    bool isChangeTitle = twitchStream.StreamTitle != data.Title;
                    bool isChangeCategory = twitchStream.GameName != data.CategoryName;
                    if (!isChangeTitle && !isChangeCategory)
                    {
                        Log.Warn($"Twitch 頻道更新資料相同，忽略: {data.BroadcasterUserName}");
                        return;
                    }

                    string message = $"`{DateTime.UtcNow.Subtract(twitchStream.StreamStartAt):hh':'mm':'ss}`";
                    if (isChangeTitle)
                        message += $"\n標題變更 `{twitchStream.StreamTitle}` => `{data.Title}`";
                    if (isChangeCategory)
                    {
                        message += $"\n分類變更 `{(string.IsNullOrEmpty(twitchStream.GameName) ? "無" : twitchStream.GameName)}`" +
                            $" => `{(string.IsNullOrEmpty(data.CategoryName) ? "無" : data.CategoryName)}`";
                    }

                    _debounceChannelUpdateMessage.AddOrUpdate(data.BroadcasterUserId,
                        _ =>
                        {
                            var debounce = new DebounceChannelUpdateMessage(this, data.BroadcasterUserName,
                                data.BroadcasterUserLogin, data.BroadcasterUserId);
                            debounce.AddMessage(message);
                            return debounce;
                        },
                        (_, debounce) =>
                        {
                            debounce.AddMessage(message);
                            return debounce;
                        });

                    twitchStream.StreamTitle = data.Title;
                    twitchStream.GameName = data.CategoryName;
                    twitchStream.UserLogin = data.BroadcasterUserLogin;
                    twitchStream.UserName = data.BroadcasterUserName;
                    await SetStreamStateAsync(twitchStream);
                }
                finally
                {
                    userLock.Release();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "處理 Twitch channel_update 失敗");
            }
        }

        private async Task RunLowFrequencyCycleAsync()
        {
            // 低頻週期校正所有 DB、EventSub 與直播狀態，再補輪詢所有 spider，修復 callback 漏失。
            await ReconcileAllAsync();
            await RunPollCycleAsync(pollAllSpiders: true);
        }

        /// <summary>
        /// 執行單一輪詢週期。高頻模式只輪詢無有效 OAuth 與待清理頻道；低頻模式輪詢全部 spider。
        /// </summary>
        private async Task RunPollCycleAsync(bool pollAllSpiders)
        {
            if (!await _pollLock.WaitAsync(0))
                return;

            bool success = false;
            try
            {
                success = await PollSpidersAsync(pollAllSpiders);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), pollAllSpiders ? "Twitch 低頻補償輪詢失敗" : "Twitch 高頻輪詢失敗");
            }
            finally
            {
                _metrics.RecordPollCycle(success ? ScraperMetricResult.Success : ScraperMetricResult.Failure);
                _pollLock.Release();
            }
        }

        private async Task<bool> PollSpidersAsync(bool pollAllSpiders)
        {
            List<TwitchSpider> spiders;
            Dictionary<string, TwitchBroadcasterAuthorization> authorizations;
            using (var db = _dbService.GetDbContext())
            {
                spiders = await db.TwitchSpider.AsNoTracking().ToListAsync();
                authorizations = await db.TwitchBroadcasterAuthorization.AsNoTracking()
                    .ToDictionaryAsync(x => x.TwitchUserId, StringComparer.Ordinal);
            }

            var ids = spiders
                // 有效 OAuth 頻道平時依賴永久 EventSub，只在低頻補償週期重新確認。
                .Where(x => pollAllSpiders || !IsValidAuthorization(GetAuthorization(authorizations, x.UserId)))
                .Select(x => x.UserId)
                // 待清理頻道即使 spider 已不存在，仍需持續確認是否已離線，才能安全刪除殘留 EventSub。
                .Concat(pollAllSpiders ? Array.Empty<string>() : _pendingCleanup.Keys)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (ids.Length == 0)
                return true;

            var streams = await _apiService.GetNowStreamsResultAsync(ids);
            if (!streams.IsSuccess)
                return false;

            var liveIds = streams.Streams.Select(x => x.UserId).ToHashSet(StringComparer.Ordinal);
            foreach (var stream in streams.Streams)
                await HandleStreamStartedAsync(stream);

            foreach (string userId in ids.Where(x => !liveIds.Contains(x)))
            {
                var state = await GetStreamStateAsync(userId);
                if (state != null)
                {
                    ScheduleOfflineCleanup(userId, state.UserLogin, state.UserName);
                }
                else if (_pendingCleanup.ContainsKey(userId))
                {
                    await ReconcileUserAsync(userId, recordMetric: false, refreshMetrics: false);
                }
            }

            return true;
        }

        private async Task HandleStreamStartedAsync(HelixStream stream)
        {
            if (stream == null || string.IsNullOrWhiteSpace(stream.Id) || string.IsNullOrWhiteSpace(stream.UserId))
                return;

            var userLock = GetUserLock(stream.UserId);
            await userLock.WaitAsync();
            try
            {
                using var db = _dbService.GetDbContext();
                var spider = await db.TwitchSpider.SingleOrDefaultAsync(x => x.UserId == stream.UserId);
                if (spider == null)
                {
                    Log.Warn($"Twitch 開台事件沒有對應 spider，交由 reconcile 清理: {stream.UserId}");
                    return;
                }

                var authorization = await db.TwitchBroadcasterAuthorization.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.TwitchUserId == stream.UserId);
                var twitchStream = CreateTwitchStream(stream);
                CancelOfflineReminder(stream.UserId);
                bool databaseDuplicate = await db.TwitchStreams.AsNoTracking().AnyAsync(x => x.StreamId == stream.Id);
                bool notificationPublished = _handledStreamIds.ContainsKey(stream.Id) ||
                    await IsStreamNotificationPublishedAsync(stream.Id);

                if (notificationPublished)
                {
                    // 重複開台事件仍要刷新直播快取與 EventSub，但不可再次發布通知或啟動錄影。
                    await SetStreamStateAsync(twitchStream);
                    await MaintainLiveSubscriptionsAsync(spider, authorization, stream.StartedAt);
                    return;
                }

                var userData = await _apiService.GetUserAsync(twitchUserId: spider.UserId);
                if (userData != null)
                {
                    spider.OfflineImageUrl = userData.OfflineImageUrl ?? string.Empty;
                    spider.ProfileImageUrl = userData.ProfileImageUrl ?? string.Empty;
                    spider.UserName = userData.DisplayName ?? spider.UserName;
                    spider.UserLogin = userData.Login ?? spider.UserLogin;
                }

                if (!databaseDuplicate)
                    db.TwitchStreams.Add(twitchStream);
                await db.SaveChangesAsync();

                await SetStreamStateAsync(twitchStream);
                await MaintainLiveSubscriptionsAsync(spider, authorization, stream.StartedAt);

                bool isRecord = spider.IsRecord && await RecordTwitchAsync(twitchStream);
                RedisValue messageId = await NotificationBus.PublishAsync(Bot.RedisDb, NotifyType.Twitch, new TwitchNotification
                {
                    NoticeType = TwitchNoticeType.StartStream,
                    UserId = twitchStream.UserId,
                    StreamId = twitchStream.StreamId,
                    UserLogin = twitchStream.UserLogin,
                    UserName = twitchStream.UserName,
                    StreamTitle = twitchStream.StreamTitle,
                    GameName = twitchStream.GameName,
                    ThumbnailUrl = twitchStream.ThumbnailUrl,
                    StreamStartAt = twitchStream.StreamStartAt,
                    IsRecord = isRecord,
                });
                await MarkStreamNotificationPublishedAsync(stream.Id, messageId);
                _handledStreamIds[stream.Id] = 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"處理 Twitch 開台失敗: {stream.UserId}");
            }
            finally
            {
                userLock.Release();
            }
        }

        private async Task MaintainLiveSubscriptionsAsync(TwitchSpider spider,
            TwitchBroadcasterAuthorization authorization, DateTime streamStartedAt)
        {
            // ClientId 不一致代表授權不屬於目前應用程式；此時不得替外部應用程式調整訂閱。
            if (HasClientIdMismatch(authorization))
            {
                SetPending(spider.UserId, null);
                Log.Error($"Twitch broadcaster {spider.UserId} 的授權 ClientId 與目前設定不符，禁止自動調整 EventSub");
                return;
            }

            if (IsValidAuthorization(authorization))
            {
                await EnsureSubscriptionsAsync(spider.UserId, TwitchEventSubEnsureMode.PermanentOAuth);
                return;
            }

            if (WasLiveWhenAuthorizationRevoked(authorization, streamStartedAt))
            {
                // 授權是在本次直播開始後才失效；保留既有訂閱到確認關台，避免直播中斷偵測。
                SetPending(spider.UserId, TwitchEventSubCleanupDeferredMetricReason.StreamLive);
                return;
            }

            if (spider.IsWarningUser)
            {
                ClearPending(spider.UserId);
                return;
            }

            await EnsureSubscriptionsAsync(spider.UserId, TwitchEventSubEnsureMode.Fallback);
        }

        /// <summary>
        /// 以 spider、OAuth 授權、EventSub 訂閱、待清理集合的聯集為準，校正每個 broadcaster 的最終狀態。
        /// </summary>
        private async Task ReconcileAllAsync()
        {
            if (!await _fullReconcileLock.WaitAsync(0))
                return;

            bool success = true;
            try
            {
                List<TwitchSpider> spiders;
                List<TwitchBroadcasterAuthorization> authorizations;
                using (var db = _dbService.GetDbContext())
                {
                    spiders = await db.TwitchSpider.AsNoTracking().ToListAsync();
                    authorizations = await db.TwitchBroadcasterAuthorization.AsNoTracking().ToListAsync();
                }

                var subscriptions = await _apiService.GetEventSubSubscriptionsResultAsync();
                if (!subscriptions.IsSuccess)
                {
                    success = false;
                    return;
                }
                _metrics.UpdateEventSubCosts(subscriptions.TotalCost, subscriptions.MaxTotalCost);

                var spiderById = spiders.ToDictionary(x => x.UserId, StringComparer.Ordinal);
                var authorizationById = authorizations.ToDictionary(x => x.TwitchUserId, StringComparer.Ordinal);
                var userIds = spiderById.Keys.Concat(authorizationById.Keys)
                    .Concat(subscriptions.Subscriptions.Select(GetBroadcasterUserId))
                    .Concat(_pendingCleanup.Keys)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

                var streams = userIds.Length == 0
                    ? new TwitchStreamsResult { IsSuccess = true }
                    : await _apiService.GetNowStreamsResultAsync(userIds);
                if (!streams.IsSuccess)
                    success = false;
                var liveById = streams.Streams.ToDictionary(x => x.UserId, StringComparer.Ordinal);

                foreach (string userId in userIds)
                {
                    var state = new TwitchUserState(
                        spiderById.GetValueOrDefault(userId),
                        authorizationById.GetValueOrDefault(userId), userId);
                    bool itemSuccess = await ReconcileUserStateAsync(state, streams.IsSuccess,
                        liveById.GetValueOrDefault(userId));
                    success &= itemSuccess;
                }

                success &= await RefreshMetricsAsync();
            }
            catch (Exception ex)
            {
                success = false;
                Log.Error(ex.Demystify(), "Twitch 全量 reconcile 失敗");
            }
            finally
            {
                _metrics.RecordReconcile(success ? ScraperMetricResult.Success : ScraperMetricResult.Failure);
                _fullReconcileLock.Release();
            }
        }

        private async Task ReconcileUserAsync(string userId, bool recordMetric, bool refreshMetrics)
        {
            bool success = false;
            try
            {
                var state = await LoadUserStateAsync(userId);
                var streams = await _apiService.GetNowStreamsResultAsync(userId);
                success = await ReconcileUserStateAsync(state, streams.IsSuccess,
                    streams.Streams.FirstOrDefault(x => x.UserId == userId));
                if (refreshMetrics)
                    success &= await RefreshMetricsAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"Twitch 單頻道 reconcile 失敗: {userId}");
            }
            finally
            {
                if (recordMetric)
                    _metrics.RecordReconcile(success ? ScraperMetricResult.Success : ScraperMetricResult.Failure);
            }
        }

        /// <summary>
        /// Twitch 單頻道狀態機。只有在直播狀態可信、確認離線且訂閱刪除成功後，才會評估是否移除 spider。
        /// </summary>
        private async Task<bool> ReconcileUserStateAsync(TwitchUserState state, bool liveStateKnown, HelixStream liveStream)
        {
            string userId = state.UserId;
            if (string.IsNullOrWhiteSpace(userId))
                return true;

            var userLock = GetUserLock(userId);
            await userLock.WaitAsync();
            try
            {
                if (HasClientIdMismatch(state.Authorization))
                {
                    SetPending(userId, null);
                    Log.Error($"Twitch broadcaster {userId} 的授權 ClientId 與目前設定不符，禁止自動刪除 EventSub 或 spider");
                    return false;
                }

                bool hasValidAuthorization = IsValidAuthorization(state.Authorization);
                if (hasValidAuthorization && state.Spider != null)
                    return await EnsureSubscriptionsAsync(userId, TwitchEventSubEnsureMode.PermanentOAuth);

                if (!liveStateKnown)
                {
                    if (state.Authorization == null && state.Spider?.IsWarningUser == true)
                    {
                        ClearPending(userId);
                        return true;
                    }

                    SetPending(userId, TwitchEventSubCleanupDeferredMetricReason.TwitchApiFailure);
                    return false;
                }

                if (liveStream != null)
                {
                    // 直播中只能建立或保留偵測能力，不進行 EventSub 或 spider 清理。
                    if (state.Authorization == null && state.Spider != null && !state.Spider.IsWarningUser)
                        return await EnsureSubscriptionsAsync(userId, TwitchEventSubEnsureMode.Fallback);

                    if (state.Authorization == null && state.Spider?.IsWarningUser == true)
                    {
                        ClearPending(userId);
                        return true;
                    }

                    if (WasLiveWhenAuthorizationRevoked(state.Authorization, liveStream.StartedAt))
                    {
                        SetPending(userId, TwitchEventSubCleanupDeferredMetricReason.StreamLive);
                        return true;
                    }

                    if (state.Spider?.IsWarningUser == true)
                    {
                        ClearPending(userId);
                        return true;
                    }

                    if (state.Spider != null)
                        return await EnsureSubscriptionsAsync(userId, TwitchEventSubEnsureMode.Fallback);

                    SetPending(userId, TwitchEventSubCleanupDeferredMetricReason.StreamLive);
                    return true;
                }

                if (state.Authorization == null && state.Spider?.IsWarningUser == true)
                {
                    ClearPending(userId);
                    return true;
                }

                var streamState = await GetStreamStateAsync(userId);
                if (streamState != null)
                {
                    // Helix 已顯示離線，但本地仍有直播狀態；先走關台去抖動，避免瞬斷造成誤刪與誤通知。
                    ScheduleOfflineCleanup(userId, streamState.UserLogin, streamState.UserName);
                    SetPending(userId, TwitchEventSubCleanupDeferredMetricReason.StreamLive);
                    return true;
                }

                var deleteResult = await DeleteSubscriptionsAsync(userId);
                if (deleteResult is TwitchEventSubDeleteStatus.ApiFailure)
                    return false;
                if (deleteResult is TwitchEventSubDeleteStatus.DeferredLive)
                    return true;

                if (state.Authorization != null && state.Spider != null)
                    return await ApplyRevokedAuthorizationGuildPolicyAsync(state.Spider);

                ClearPending(userId);
                return true;
            }
            finally
            {
                userLock.Release();
            }
        }

        private async Task<bool> EnsureSubscriptionsAsync(string userId, TwitchEventSubEnsureMode mode)
        {
            var result = await _apiService.EnsureEventSubSubscriptionsAsync(userId, mode);
            if (result.Subscriptions.IsSuccess)
                _metrics.UpdateEventSubCosts(result.Subscriptions.TotalCost, result.Subscriptions.MaxTotalCost);
            if (result.IsSuccess)
            {
                ClearPending(userId);
                return true;
            }

            SetPending(userId, TwitchEventSubCleanupDeferredMetricReason.TwitchApiFailure);
            return false;
        }

        private async Task<TwitchEventSubDeleteStatus> DeleteSubscriptionsAsync(string userId)
        {
            var result = await _apiService.DeleteEventSubSubscriptionResultAsync(userId);
            switch (result.Status)
            {
                case TwitchEventSubDeleteStatus.Deleted:
                case TwitchEventSubDeleteStatus.NoSubscriptions:
                    ClearPending(userId);
                    break;
                case TwitchEventSubDeleteStatus.DeferredLive:
                    SetPending(userId, TwitchEventSubCleanupDeferredMetricReason.StreamLive);
                    break;
                case TwitchEventSubDeleteStatus.ApiFailure:
                    SetPending(userId, TwitchEventSubCleanupDeferredMetricReason.TwitchApiFailure);
                    break;
            }

            return result.Status;
        }

        private async Task<bool> ApplyRevokedAuthorizationGuildPolicyAsync(TwitchSpider spider)
        {
            // 授權失效不等於立即刪除通知設定；只有 guild 明確不符合資格時才移除偵測 spider。
            var eligibility = await _guildEligibility.EvaluateAsync(spider);
            switch (eligibility)
            {
                case TwitchGuildEligibilityStatus.Eligible:
                    ClearPending(spider.UserId);
                    return true;
                case TwitchGuildEligibilityStatus.Ineligible:
                    return await RemoveSpiderIfStillInvalidAsync(spider,
                        TwitchSpiderRemovalMetricReason.GuildIneligible);
                case TwitchGuildEligibilityStatus.MissingConfirmed:
                    return await RemoveSpiderIfStillInvalidAsync(spider,
                        TwitchSpiderRemovalMetricReason.GuildMissing);
                case TwitchGuildEligibilityStatus.NotifierUnavailable:
                    SetPending(spider.UserId, TwitchEventSubCleanupDeferredMetricReason.NotifierUnavailable);
                    return true;
                case TwitchGuildEligibilityStatus.PendingSnapshot:
                case TwitchGuildEligibilityStatus.SnapshotUnavailable:
                    SetPending(spider.UserId, TwitchEventSubCleanupDeferredMetricReason.GuildSnapshotUnavailable);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 執行 spider 刪除前的最後一道防線：重新確認離線、授權、guild 綁定與資格，避免競態誤刪。
        /// </summary>
        private async Task<bool> RemoveSpiderIfStillInvalidAsync(TwitchSpider expectedSpider,
            TwitchSpiderRemovalMetricReason reason)
        {
            var streams = await _apiService.GetNowStreamsResultAsync(expectedSpider.UserId);
            if (!streams.IsSuccess)
            {
                SetPending(expectedSpider.UserId, TwitchEventSubCleanupDeferredMetricReason.TwitchApiFailure);
                return true;
            }
            if (streams.Streams.Any(x => x.UserId == expectedSpider.UserId))
            {
                SetPending(expectedSpider.UserId, TwitchEventSubCleanupDeferredMetricReason.StreamLive);
                return true;
            }

            using var db = _dbService.GetDbContext();
            var currentSpider = await db.TwitchSpider.SingleOrDefaultAsync(x => x.UserId == expectedSpider.UserId);
            var currentAuthorization = await db.TwitchBroadcasterAuthorization.AsNoTracking()
                .SingleOrDefaultAsync(x => x.TwitchUserId == expectedSpider.UserId);
            if (currentSpider == null)
            {
                ClearPending(expectedSpider.UserId);
                return true;
            }

            if (currentSpider.GuildId != expectedSpider.GuildId || IsValidAuthorization(currentAuthorization) ||
                HasClientIdMismatch(currentAuthorization))
            {
                // 評估期間資料已變更，放棄本次刪除並等待下一輪以新狀態重新判斷。
                SetPending(expectedSpider.UserId, TwitchEventSubCleanupDeferredMetricReason.GuildSnapshotUnavailable);
                return true;
            }

            var latestEligibility = await _guildEligibility.EvaluateAsync(currentSpider);
            bool removalStillAllowed = reason switch
            {
                TwitchSpiderRemovalMetricReason.GuildIneligible => latestEligibility == TwitchGuildEligibilityStatus.Ineligible,
                TwitchSpiderRemovalMetricReason.GuildMissing => latestEligibility == TwitchGuildEligibilityStatus.MissingConfirmed,
                _ => false
            };
            if (!removalStillAllowed)
            {
                SetPending(expectedSpider.UserId,
                    latestEligibility == TwitchGuildEligibilityStatus.NotifierUnavailable
                        ? TwitchEventSubCleanupDeferredMetricReason.NotifierUnavailable
                        : TwitchEventSubCleanupDeferredMetricReason.GuildSnapshotUnavailable);
                return true;
            }

            db.TwitchSpider.Remove(currentSpider);
            await db.SaveChangesAsync();
            _metrics.RecordSpiderRemoval(reason);
            ClearPending(expectedSpider.UserId);
            Log.Warn($"已移除授權失效且 guild 不符合資格的 Twitch spider: {expectedSpider.UserId}（{reason}）");
            return true;
        }

        /// <summary>
        /// 排程關台去抖動。EventSub offline 可取代既有工作；輪詢只在尚未排程時建立工作。
        /// </summary>
        private void ScheduleOfflineCleanup(string userId, string userLogin, string userName, bool replaceExisting = false)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;

            var cancellation = CancellationTokenSource.CreateLinkedTokenSource(GracefulShutdown.Token);
            if (!replaceExisting)
            {
                if (!_streamOfflineReminders.TryAdd(userId, cancellation))
                {
                    cancellation.Dispose();
                    return;
                }
            }
            else
            {
                if (_streamOfflineReminders.TryRemove(userId, out var previous))
                {
                    previous.Cancel();
                    previous.Dispose();
                }
                _streamOfflineReminders[userId] = cancellation;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(OfflineDebounce, cancellation.Token);
                    await HandleStreamEndedAsync(userId, userLogin, userName, DateTime.UtcNow - OfflineDebounce);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), $"Twitch 關台去抖動處理失敗: {userId}");
                }
                finally
                {
                    if (_streamOfflineReminders.TryGetValue(userId, out var current) && ReferenceEquals(current, cancellation))
                        _streamOfflineReminders.TryRemove(userId, out _);
                    cancellation.Dispose();
                }
            });
        }

        /// <summary>
        /// 去抖動後再次向 Helix 確認；若已恢復直播則回到開台流程，否則校正訂閱並發布關台通知。
        /// </summary>
        private async Task HandleStreamEndedAsync(string userId, string userLogin, string userName, DateTime endAtUtc)
        {
            HelixStream resumedStream = null;
            TwitchStream twitchStream = null;
            bool shouldPublishEnd = false;
            var userLock = GetUserLock(userId);
            await userLock.WaitAsync();
            try
            {
                var streams = await _apiService.GetNowStreamsResultAsync(userId);
                if (!streams.IsSuccess)
                {
                    SetPending(userId, TwitchEventSubCleanupDeferredMetricReason.TwitchApiFailure);
                    return;
                }

                resumedStream = streams.Streams.FirstOrDefault(x => x.UserId == userId);
                if (resumedStream == null)
                {
                    twitchStream = await GetStreamStateAsync(userId);
                    var state = await LoadUserStateAsync(userId);
                    // 先處理 EventSub 與授權失效清理，再決定是否能安全發布關台通知。
                    await ReconcileOfflineStateCoreAsync(state);
                    if (_deferredCleanup.TryGetValue(userId, out var reason) &&
                        reason == TwitchEventSubCleanupDeferredMetricReason.StreamLive)
                        return;

                    shouldPublishEnd = twitchStream != null || state.Spider != null;
                }
            }
            finally
            {
                userLock.Release();
            }

            if (resumedStream != null)
            {
                await HandleStreamStartedAsync(resumedStream);
                return;
            }
            if (!shouldPublishEnd)
                return;

            string clipsValue = string.Empty;
            var video = await _apiService.GetLatestVODAsync(userId);
            if (video == null)
            {
                Log.Warn($"找不到對應的 Vod 紀錄資料: {userLogin} ({userId})");
            }
            else
            {
                DateTime createAt = DateTime.Parse(video.CreatedAt);
                var clips = await _apiService.GetClipsAsync(userId, createAt,
                    createAt + _apiService.ParseToTimeSpan(video.Duration));
                if (clips != null && clips.Any(x => x.VideoId == video.Id))
                {
                    int i = 0;
                    clipsValue = string.Join('\n', clips.Where(x => x.VideoId == video.Id)
                        .Select(x => $"{i++}. [{x.Title}]({x.Url}) By `{x.CreatorName}` (`{x.ViewCount}` 次觀看)"));
                }
            }

            if (twitchStream == null && video != null)
            {
                twitchStream = new TwitchStream
                {
                    UserId = userId,
                    UserLogin = userLogin,
                    UserName = userName,
                    StreamTitle = video.Title,
                    StreamStartAt = DateTime.Parse(video.CreatedAt)
                };
            }

            try
            {
                await NotificationBus.PublishAsync(Bot.RedisDb, NotifyType.Twitch, new TwitchNotification
                {
                    NoticeType = TwitchNoticeType.EndStream,
                    UserId = userId,
                    StreamId = twitchStream?.StreamId,
                    UserLogin = twitchStream?.UserLogin ?? userLogin,
                    UserName = twitchStream?.UserName ?? userName,
                    StreamTitle = twitchStream?.StreamTitle,
                    StreamStartAt = twitchStream?.StreamStartAt,
                    StreamEndAt = endAtUtc,
                    ClipsValue = clipsValue,
                });

                await Bot.RedisDb.KeyDeleteAsync(RedisChannels.Twitch.StreamData(userId));
                if (!string.IsNullOrEmpty(twitchStream?.StreamId))
                    _handledStreamIds.TryRemove(twitchStream.StreamId, out _);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"發布 Twitch 關台通知失敗: {userId}");
            }
        }

        /// <summary>已確認離線時的精簡 reconcile；仍沿用 ClientId、授權與 guild 資格安全條件。</summary>
        private async Task<bool> ReconcileOfflineStateCoreAsync(TwitchUserState state)
        {
            string userId = state.UserId;
            if (HasClientIdMismatch(state.Authorization))
            {
                SetPending(userId, null);
                Log.Error($"Twitch broadcaster {userId} 的授權 ClientId 與目前設定不符，關台後禁止自動刪除 EventSub 或 spider");
                return false;
            }

            if (IsValidAuthorization(state.Authorization) && state.Spider != null)
                return await EnsureSubscriptionsAsync(userId, TwitchEventSubEnsureMode.PermanentOAuth);

            if (state.Authorization == null && state.Spider?.IsWarningUser == true)
            {
                ClearPending(userId);
                return true;
            }

            var deleteResult = await DeleteSubscriptionsAsync(userId);
            if (deleteResult == TwitchEventSubDeleteStatus.ApiFailure)
                return false;
            if (deleteResult == TwitchEventSubDeleteStatus.DeferredLive)
                return true;

            if (state.Authorization != null && state.Spider != null)
                return await ApplyRevokedAuthorizationGuildPolicyAsync(state.Spider);

            ClearPending(userId);
            return true;
        }

        private async Task<TwitchUserState> LoadUserStateAsync(string userId)
        {
            using var db = _dbService.GetDbContext();
            var spider = await db.TwitchSpider.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId);
            var authorization = await db.TwitchBroadcasterAuthorization.AsNoTracking()
                .SingleOrDefaultAsync(x => x.TwitchUserId == userId);
            return new TwitchUserState(spider, authorization, userId);
        }

        private async Task<bool> RefreshMetricsAsync()
        {
            List<TwitchSpider> spiders;
            List<TwitchBroadcasterAuthorization> authorizations;
            using (var db = _dbService.GetDbContext())
            {
                spiders = await db.TwitchSpider.AsNoTracking().ToListAsync();
                authorizations = await db.TwitchBroadcasterAuthorization.AsNoTracking().ToListAsync();
            }

            var authorizationById = authorizations.ToDictionary(x => x.TwitchUserId, StringComparer.Ordinal);
            foreach (TwitchSpiderMetricMode mode in Enum.GetValues<TwitchSpiderMetricMode>())
                _metrics.SetSpiderCount(mode, spiders.Count(x => GetMetricMode(x, GetAuthorization(authorizationById, x.UserId)) == mode));

            var subscriptions = await _apiService.GetEventSubSubscriptionsResultAsync();
            if (!subscriptions.IsSuccess)
                return false;
            _metrics.UpdateEventSubCosts(subscriptions.TotalCost, subscriptions.MaxTotalCost);

            foreach (TwitchEventSubMetricType type in Enum.GetValues<TwitchEventSubMetricType>())
            foreach (TwitchSpiderMetricMode mode in Enum.GetValues<TwitchSpiderMetricMode>())
            foreach (TwitchEventSubMetricStatus status in Enum.GetValues<TwitchEventSubMetricStatus>())
                _metrics.SetEventSubSubscriptionCount(type, mode, status, 0);

            var spiderById = spiders.ToDictionary(x => x.UserId, StringComparer.Ordinal);
            var counts = new Dictionary<(TwitchEventSubMetricType, TwitchSpiderMetricMode, TwitchEventSubMetricStatus), int>();
            foreach (var subscription in subscriptions.Subscriptions)
            {
                if (!TryGetMetricType(subscription.Type, out var type))
                    continue;

                string userId = GetBroadcasterUserId(subscription);
                var spider = !string.IsNullOrEmpty(userId) ? spiderById.GetValueOrDefault(userId) : null;
                var authorization = !string.IsNullOrEmpty(userId) ? GetAuthorization(authorizationById, userId) : null;
                var key = (type, spider == null ? TwitchSpiderMetricMode.Unmonitored : GetMetricMode(spider, authorization),
                    ParseEventSubStatus(subscription.Status));
                counts[key] = counts.GetValueOrDefault(key) + 1;
            }

            foreach (var item in counts)
                _metrics.SetEventSubSubscriptionCount(item.Key.Item1, item.Key.Item2, item.Key.Item3, item.Value);
            return true;
        }

        private void SetPending(string userId, TwitchEventSubCleanupDeferredMetricReason? reason)
        {
            // 不持久化此集合；服務重啟後全量 reconcile 會從 DB 與現有 EventSub 重新建立待處理項目。
            _pendingCleanup[userId] = 0;
            if (reason.HasValue)
                _deferredCleanup[userId] = reason.Value;
            else
                _deferredCleanup.TryRemove(userId, out _);
            RefreshPendingMetrics();
        }

        private void ClearPending(string userId)
        {
            _pendingCleanup.TryRemove(userId, out _);
            _deferredCleanup.TryRemove(userId, out _);
            RefreshPendingMetrics();
        }

        private void RefreshPendingMetrics()
        {
            _metrics.SetSpiderCleanupPendingCount(_pendingCleanup.Count);
            foreach (TwitchEventSubCleanupDeferredMetricReason reason in Enum.GetValues<TwitchEventSubCleanupDeferredMetricReason>())
                _metrics.SetEventSubCleanupDeferredCount(reason, _deferredCleanup.Count(x => x.Value == reason));
        }

        private bool CancelOfflineReminder(string userId)
        {
            if (!_streamOfflineReminders.TryRemove(userId, out var cancellation))
                return false;

            cancellation.Cancel();
            cancellation.Dispose();
            return true;
        }

        private SemaphoreSlim GetUserLock(string userId) => _userLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));

        private async Task<TwitchStream> GetStreamStateAsync(string userId)
        {
            try
            {
                RedisValue json = await Bot.RedisDb.StringGetAsync(RedisChannels.Twitch.StreamData(userId));
                return json.IsNullOrEmpty ? null : JsonConvert.DeserializeObject<TwitchStream>(json!);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"讀取 Twitch Redis 直播狀態失敗: {userId}");
                return null;
            }
        }

        private async Task SetStreamStateAsync(TwitchStream twitchStream)
        {
            try
            {
                await Bot.RedisDb.StringSetAsync(RedisChannels.Twitch.StreamData(twitchStream.UserId),
                    JsonConvert.SerializeObject(twitchStream));
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"寫入 Twitch Redis 直播狀態失敗: {twitchStream.UserId}");
            }
        }

        private async Task<bool> IsStreamNotificationPublishedAsync(string streamId)
        {
            try
            {
                return await Bot.RedisDb.KeyExistsAsync(RedisChannels.Twitch.StreamNotification(streamId));
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"讀取 Twitch 開台通知去重狀態失敗: {streamId}");
                return false;
            }
        }

        private async Task MarkStreamNotificationPublishedAsync(string streamId, RedisValue messageId)
        {
            try
            {
                // 保留 30 天可涵蓋服務重啟與延遲 callback，值同時記錄匯流排 message id 供追查。
                await Bot.RedisDb.StringSetAsync(RedisChannels.Twitch.StreamNotification(streamId), messageId,
                    TimeSpan.FromDays(30));
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"寫入 Twitch 開台通知去重狀態失敗，可能造成重複通知: {streamId}");
            }
        }

        private static TwitchStream CreateTwitchStream(HelixStream stream) => new()
        {
            StreamId = stream.Id,
            StreamTitle = stream.Title,
            GameName = stream.GameName,
            ThumbnailUrl = (stream.ThumbnailUrl ?? string.Empty).Replace("{width}", "854").Replace("{height}", "480"),
            UserId = stream.UserId,
            UserLogin = stream.UserLogin,
            UserName = stream.UserName,
            StreamStartAt = stream.StartedAt
        };

        private bool IsValidAuthorization(TwitchBroadcasterAuthorization authorization) =>
            authorization != null && authorization.RevokedAt == null && !HasClientIdMismatch(authorization);

        private bool HasClientIdMismatch(TwitchBroadcasterAuthorization authorization) =>
            authorization != null && !string.Equals(authorization.ClientId, _botConfig.TwitchClientId, StringComparison.Ordinal);

        private static TwitchBroadcasterAuthorization GetAuthorization(
            IReadOnlyDictionary<string, TwitchBroadcasterAuthorization> authorizations, string userId) =>
            authorizations.GetValueOrDefault(userId);

        private TwitchSpiderMetricMode GetMetricMode(TwitchSpider spider, TwitchBroadcasterAuthorization authorization)
        {
            if (IsValidAuthorization(authorization))
                return TwitchSpiderMetricMode.OAuth;
            return spider.IsWarningUser ? TwitchSpiderMetricMode.Warning : TwitchSpiderMetricMode.Fallback;
        }

        private static bool WasLiveWhenAuthorizationRevoked(
            TwitchBroadcasterAuthorization authorization, DateTime streamStartedAt)
        {
            if (authorization?.RevokedAt == null)
                return false;

            DateTime startedAtUtc = streamStartedAt.Kind == DateTimeKind.Utc
                ? streamStartedAt
                : DateTime.SpecifyKind(streamStartedAt, DateTimeKind.Utc);
            DateTime revokedAtUtc = authorization.RevokedAt.Value.Kind == DateTimeKind.Utc
                ? authorization.RevokedAt.Value
                : DateTime.SpecifyKind(authorization.RevokedAt.Value, DateTimeKind.Utc);
            // 開台時間早於或等於撤銷時間，表示撤銷發生時該場直播已在進行。
            return startedAtUtc <= revokedAtUtc;
        }

        private static string GetBroadcasterUserId(EventSubSubscription subscription)
        {
            if (subscription?.Condition != null &&
                subscription.Condition.TryGetValue("broadcaster_user_id", out string userId))
                return userId;
            return null;
        }

        private static bool TryGetMetricType(string type, out TwitchEventSubMetricType metricType)
        {
            switch (type)
            {
                case "stream.online":
                    metricType = TwitchEventSubMetricType.StreamOnline;
                    return true;
                case "channel.update":
                    metricType = TwitchEventSubMetricType.ChannelUpdate;
                    return true;
                case "stream.offline":
                    metricType = TwitchEventSubMetricType.StreamOffline;
                    return true;
                default:
                    metricType = default;
                    return false;
            }
        }

        private static TwitchEventSubMetricStatus ParseEventSubStatus(string status) => status switch
        {
            "enabled" => TwitchEventSubMetricStatus.Enabled,
            "webhook_callback_verification_pending" => TwitchEventSubMetricStatus.WebhookCallbackVerificationPending,
            "webhook_callback_verification_failed" => TwitchEventSubMetricStatus.WebhookCallbackVerificationFailed,
            "notification_failures_exceeded" => TwitchEventSubMetricStatus.NotificationFailuresExceeded,
            "authorization_revoked" => TwitchEventSubMetricStatus.AuthorizationRevoked,
            "moderator_removed" => TwitchEventSubMetricStatus.ModeratorRemoved,
            "user_removed" => TwitchEventSubMetricStatus.UserRemoved,
            "version_removed" => TwitchEventSubMetricStatus.VersionRemoved,
            "beta_maintenance" => TwitchEventSubMetricStatus.BetaMaintenance,
            "websocket_disconnected" => TwitchEventSubMetricStatus.WebsocketDisconnected,
            "websocket_failed_ping_pong" => TwitchEventSubMetricStatus.WebsocketFailedPingPong,
            "websocket_received_inbound_traffic" => TwitchEventSubMetricStatus.WebsocketReceivedInboundTraffic,
            _ => TwitchEventSubMetricStatus.Unknown
        };

        private static TwitchAuthorizationChangeMetricResult ParseAuthorizationChangeResult(string status) =>
            status?.Trim().ToLowerInvariant() switch
            {
                "authorized" => TwitchAuthorizationChangeMetricResult.Authorized,
                "reauthorized" => TwitchAuthorizationChangeMetricResult.Reauthorized,
                "revoked" => TwitchAuthorizationChangeMetricResult.Revoked,
                "invalid" => TwitchAuthorizationChangeMetricResult.Invalid,
                _ => TwitchAuthorizationChangeMetricResult.Failure
            };

        /// <summary>
        /// 直播資料更新通知的發布入口。由 <see cref="DebounceChannelUpdateMessage"/> 彙整後送入通知匯流排。
        /// </summary>
        internal async Task PublishChannelUpdateAsync(string userId, string userName, string userLogin, string description)
        {
            try
            {
                await NotificationBus.PublishAsync(Bot.RedisDb, NotifyType.Twitch, new TwitchNotification
                {
                    NoticeType = TwitchNoticeType.ChangeStreamData,
                    UserId = userId,
                    UserLogin = userLogin,
                    UserName = userName,
                    Description = description,
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"發布 Twitch 頻道更新通知失敗: {userId}");
            }
        }

        private async Task<bool> RecordTwitchAsync(TwitchStream twitchStream)
        {
            Log.Info($"{twitchStream.UserName} ({twitchStream.StreamId}): {twitchStream.StreamTitle}");
            // 錄影工具是外部服務，沿用既有 Redis Pub/Sub 契約並以訂閱者數量判斷是否已接收。
            if (Bot.Redis != null && await Bot.RedisSub.PublishAsync(
                new RedisChannel(RedisChannels.Twitch.Record, RedisChannel.PatternMode.Literal), twitchStream.UserLogin) != 0)
            {
                Log.Info($"已發送 Twitch 錄影請求: {twitchStream.UserLogin}");
                return true;
            }

            Log.Warn($"Redis Sub 頻道不存在，請開啟錄影工具: {twitchStream.UserLogin}");
            return false;
        }

        private sealed class TwitchAuthorizationChangedPayload
        {
            public string TwitchUserId { get; set; }
            public string Status { get; set; }
        }

        private sealed class TwitchReconcileRequestedPayload
        {
            public string TwitchUserId { get; set; }
            public string Reason { get; set; }
        }

        private sealed class TwitchStreamEventPayload
        {
            public string BroadcasterUserId { get; set; }
            public string BroadcasterUserLogin { get; set; }
            public string BroadcasterUserName { get; set; }
        }

        /// <summary>彙整單一 broadcaster 在 DB 中可獨立存在的 spider 與 OAuth 授權紀錄。</summary>
        private sealed class TwitchUserState
        {
            public TwitchUserState(TwitchSpider spider, TwitchBroadcasterAuthorization authorization, string userId = null)
            {
                Spider = spider;
                Authorization = authorization;
                UserId = spider?.UserId ?? authorization?.TwitchUserId ?? userId;
            }

            public string UserId { get; }
            public TwitchSpider Spider { get; }
            public TwitchBroadcasterAuthorization Authorization { get; }
        }
    }
}
