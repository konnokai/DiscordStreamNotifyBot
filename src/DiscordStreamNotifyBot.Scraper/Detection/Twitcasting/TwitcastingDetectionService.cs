using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.HttpClients;
using DiscordStreamNotifyBot.HttpClients.Twitcasting.Model;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;

using Bot = DiscordStreamNotifyBot.Shared.BotState;

namespace DiscordStreamNotifyBot.Scraper.Detection.Twitcasting
{
    /// <summary>
    /// TwitCasting 偵測服務（Scraper 專用）：分類 / WebHook 輪詢 Timer、開台 Redis 訂閱、錄影，
    /// 偵測到開台時 publish <see cref="TwitcastingNotification"/> 至通知匯流排（不碰 Discord gateway）。
    /// 指令支援與通知發送由 Notifier 端的 TwitcastingService 負責。
    /// </summary>
    public class TwitcastingDetectionService
    {
        public bool IsEnable { get; private set; } = true;

        private readonly TwitcastingClient _twitcastingClient;
        private readonly MainDbService _dbService;
        private readonly BotConfig _botConfig;
        private readonly SemaphoreSlim _startLiveLock = new(1, 1);

        private List<Category> categories;

        public TwitcastingDetectionService(TwitcastingClient twitcastingClient, BotConfig botConfig, MainDbService dbService)
        {
            if (string.IsNullOrEmpty(botConfig.TwitCastingClientId) || string.IsNullOrEmpty(botConfig.TwitCastingClientSecret))
            {
                Log.Warn($"{nameof(botConfig.TwitCastingClientId)} 或 {nameof(botConfig.TwitCastingClientSecret)} 遺失，無法運行 TwitCasting 偵測");
                IsEnable = false;
                return;
            }

            _twitcastingClient = twitcastingClient;
            _botConfig = botConfig;
            _dbService = dbService;

            // 偵測排程（計畫 §12.1）：PeriodicTimer 背景輪詢，await 友善、無重入、吃 CancellationToken
            var token = GracefulShutdown.Token;
            PeriodicRunner.RunAsync("TwitCasting-categories", TimeSpan.FromSeconds(3), TimeSpan.FromMinutes(30), async () =>
            {
                categories = await _twitcastingClient.GetCategoriesAsync();
            }, token);

            PeriodicRunner.RunAsync("TwitCasting-webhook", TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(15), ReconcileWebhooksAsync, token);

            Bot.RedisSub.Subscribe(
                new RedisChannel(RedisChannels.Twitcasting.PubSubStartLive, RedisChannel.PatternMode.Literal),
                async (_, message) => await HandleStartLiveMessageAsync(message));
        }

        private async Task HandleStartLiveMessageAsync(RedisValue message)
        {
            if (!TwitcastingWebhookParser.TryParseLiveStart(message, out var startEvent))
            {
                Log.Error("TwitCasting WebHook JSON 無效或不是 livestart 事件");
                return;
            }

            await _startLiveLock.WaitAsync(GracefulShutdown.Token);
            try
            {
                using var db = _dbService.GetDbContext();
                bool streamAlreadyExists = await db.TwitcastingStreams.AsNoTracking()
                    .AnyAsync(item => item.StreamId == startEvent.StreamId);
                bool isRecordingEnabled = await db.TwitcastingSpider.AsNoTracking()
                    .Where(item => item.ChannelId == startEvent.UserId)
                    .Select(item => item.IsRecord)
                    .FirstOrDefaultAsync();
                var plan = TwitcastingLiveStartPlanner.Plan(new TwitcastingLiveStartFacts(
                    startEvent,
                    streamAlreadyExists,
                    isRecordingEnabled,
                    TwitcastingLiveStartPlanner.ResolveCategoryName(startEvent.CategoryId, categories)));

                if (plan.Action == TwitcastingLiveStartAction.IgnoreDuplicate)
                {
                    Log.Warn($"TwitCasting 重複開台通知: {startEvent.StreamId} - {startEvent.StreamTitle}");
                    return;
                }

                bool recordingDelegated = false;
                if (plan.Action == TwitcastingLiveStartAction.PersistRequestRecordingAndNotify)
                    recordingDelegated = await RecordTwitCastingAsync(plan.Stream);

                var notification = TwitcastingLiveStartPlanner.CreateNotification(plan, recordingDelegated);
                if (!await PublishStartLiveWithRetryAsync(notification))
                    return;

                await db.TwitcastingStreams.AddAsync(TwitcastingLiveStartPlanner.ToEntity(plan.Stream));
                await db.SaveChangesAsync();
            }
            finally
            {
                _startLiveLock.Release();
            }
        }

        private async Task ReconcileWebhooksAsync()
        {
#if DEBUG
            return;
#endif

            // PeriodicTimer 保證單一迴圈不重疊，無需 isRuning 重入旗標（§12.1）
            using var db = _dbService.GetDbContext();
            var spiderList = db.TwitcastingSpider.AsNoTracking().ToList();

            try
            {
                // 取得所有已註冊的 webhook
                var registeredWebhooks = await _twitcastingClient.GetAllRegistedWebHookAsync();
                if (registeredWebhooks == null)
                {
                    Log.Error("TwitCastingService-Timer: 無法獲取已註冊的 Webhook 列表，請檢查 TwitCasting API 設定是否正確。");
                    return;
                }
                var plan = TwitcastingWebhookRegistrationPlanner.Plan(
                    spiderList.Select(item => item.ChannelId),
                    registeredWebhooks.Select(item => new TwitcastingWebhookRegistration(item.UserId, item.Event)));
                foreach (var action in plan)
                {
                    if (action.Kind == TwitcastingWebhookActionKind.RegisterLiveStart)
                    {
                        await _twitcastingClient.RegisterWebHookAsync(action.UserId);
                        Log.Info($"註冊 TwitCasting Webhook: {action.UserId}");
                    }
                    else
                    {
                        await _twitcastingClient.RemoveWebHookAsync(action.UserId);
                        Log.Info($"移除 TwitCasting Webhook: {action.UserId}");
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Demystify(), "TwitCastingService-Timer"); }

            await db.SaveChangesAsync();
        }

        /// <summary>偵測到開台：publish DTO 至通知匯流排（取代直接送 Discord）。</summary>
        private async Task<bool> PublishStartLiveAsync(TwitcastingNotification notification)
        {
#if DEBUG
            Log.New($"TwitCasting 開台通知: {notification.ChannelTitle} - {notification.StreamTitle} (isPrivate: {notification.IsPrivate})");
            return true;
#else
            try
            {
                await NotificationBus.PublishOnceAsync(
                    Bot.RedisDb,
                    $"twitcasting:notification_published:{notification.StreamId}",
                    TimeSpan.FromDays(30),
                    NotifyType.Twitcasting,
                    notification).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"PublishTwitcastingStartLive: {notification.ChannelId} / {notification.StreamId}");
                return false;
            }
#endif
        }

        private async Task<bool> PublishStartLiveWithRetryAsync(TwitcastingNotification notification)
        {
            var delay = TimeSpan.FromSeconds(1);
            while (!GracefulShutdown.Token.IsCancellationRequested)
            {
                if (await PublishStartLiveAsync(notification))
                    return true;

                try
                {
                    await Task.Delay(delay, GracefulShutdown.Token);
                }
                catch (OperationCanceledException)
                {
                    return false;
                }

                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 30));
            }

            return false;
        }

        /// <summary>
        /// 錄影委派：比照 Twitch，publish <see cref="RedisChannels.Twitcasting.Record"/> 給錄影工具執行，
        /// 不再於 Scraper 進程內本機 streamlink 錄影。回傳 subscriber 數判斷錄影端是否在線。
        /// </summary>
        private async Task<bool> RecordTwitCastingAsync(TwitcastingStreamData stream)
        {
            Log.Info($"{stream.ChannelTitle} ({stream.StreamId}): {stream.StreamTitle}");

            if (Bot.Redis == null)
                return false;

            try
            {
                const string script = """
                    local existing = redis.call('GET', KEYS[1])
                    if existing then
                        return tonumber(existing)
                    end
                    local subscribers = redis.call('PUBLISH', ARGV[1], ARGV[2])
                    if subscribers > 0 then
                        redis.call('SET', KEYS[1], subscribers, 'EX', ARGV[3])
                    end
                    return subscribers
                    """;
                var result = await Bot.RedisDb.ScriptEvaluateAsync(
                    script,
                    [$"twitcasting:recording_delegated:{stream.StreamId}"],
                    [RedisChannels.Twitcasting.Record, stream.ChannelId, (long)TimeSpan.FromDays(30).TotalSeconds]);
                if ((long)result != 0)
                {
                    Log.Info($"已發送 TwitCasting 錄影請求: {stream.ChannelId}");
                    return true;
                }

                Log.Warn($"Redis Sub 頻道不存在，請開啟錄影工具: {stream.ChannelId}");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"TwitCasting 錄影請求失敗: {stream.ChannelId} / {stream.StreamId}");
                return false;
            }
        }
    }
}
