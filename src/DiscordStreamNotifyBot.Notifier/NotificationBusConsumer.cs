using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.Youtube;

namespace DiscordStreamNotifyBot
{
    /// <summary>
    /// 通知匯流排消費端（階段 3-4）。Notifier 的通知一律來自 <c>bot:notify</c> Redis Stream：
    /// 消費本 shard 的 consumer group <c>shard-{id}</c>，依 <c>type</c> 反序列化 DTO 交給對應服務發送
    /// （偵測與發布由 Scraper 負責）。
    /// <para>
    /// StackExchange.Redis 不支援 blocking read → 短輪詢（§4.3）。at-least-once：發送成功才 XACK；
    /// 例外則不 ack，留在 PEL 由 XAUTOCLAIM 補救；以短期去重鍵吸收「送出成功但 ack 失敗」的重複。
    /// </para>
    /// <para>
    /// Member（會限身分組）不走匯流排：會限檢查經 shard 守衛天然按 shard 分區，role 操作為 REST 不綁 gateway。
    /// </para>
    /// </summary>
    public sealed class NotificationBusConsumer
    {
        private readonly YoutubeStreamService _youtubeStreamService;
        private readonly SharedService.Twitch.TwitchService _twitchService;
        private readonly SharedService.Twitcasting.TwitcastingService _twitcastingService;
        private readonly SharedService.YoutubeMember.YoutubeMemberService _youtubeMemberService;
        private readonly NotifierMetrics _metrics;
        private readonly Func<string, string, Task<bool>> _dispatchAsync;
        private readonly NotificationBusConsumerOptions _options;

        private int _shardId;

        internal NotificationBusConsumer(YoutubeStreamService youtubeStreamService,
            SharedService.Twitch.TwitchService twitchService,
            SharedService.Twitcasting.TwitcastingService twitcastingService,
            SharedService.YoutubeMember.YoutubeMemberService youtubeMemberService,
            NotifierMetrics metrics)
        {
            _youtubeStreamService = youtubeStreamService;
            _twitchService = twitchService;
            _twitcastingService = twitcastingService;
            _youtubeMemberService = youtubeMemberService;
            _metrics = metrics;
            _dispatchAsync = DispatchAsync;
            _options = NotificationBusConsumerOptions.Default;
        }

        internal NotificationBusConsumer(
            Func<string, string, Task> dispatchAsync,
            NotificationBusConsumerOptions options = null,
            NotifierMetrics metrics = null)
        {
            ArgumentNullException.ThrowIfNull(dispatchAsync);
            _dispatchAsync = async (type, payload) =>
            {
                await dispatchAsync(type, payload);
                return true;
            };
            _options = options ?? NotificationBusConsumerOptions.Default;
            _metrics = metrics;
        }

        /// <summary>建立本 shard 的 consumer group 並於背景啟動消費迴圈（吃 GracefulShutdown.Token）。</summary>
        public async Task StartAsync(int shardId)
        {
            _shardId = shardId;
            await NotificationBus.EnsureConsumerGroupAsync(BotState.RedisDb, shardId);
            _ = Task.Run(() => ConsumeLoopAsync(BotState.RedisDb, shardId, GracefulShutdown.Token));
            Log.Info($"[NotificationBus] 已開始消費 {NotificationBus.StreamKey}（group {NotificationBus.GroupName(shardId)}）");
        }

        internal async Task ConsumeLoopAsync(IDatabase db, int shardId, CancellationToken ct)
        {
            int pollsSinceAutoClaim = 0;
            RedisValue autoClaimStartId = "0-0";

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var entries = await NotificationBus.ReadNewAsync(db, shardId, _options.BatchSize);

                    foreach (var entry in entries)
                        await ProcessEntryAsync(db, shardId, entry);

                    if (++pollsSinceAutoClaim >= _options.AutoClaimEveryPolls)
                    {
                        pollsSinceAutoClaim = 0;
                        var claimPage = await NotificationBus.AutoClaimPageAsync(
                            db,
                            shardId,
                            _options.AutoClaimMinIdle,
                            autoClaimStartId,
                            _options.BatchSize);
                        autoClaimStartId = claimPage.NextStartId;
                        foreach (var entry in claimPage.ClaimedEntries)
                            await ProcessEntryAsync(db, shardId, entry);
                    }

                    if (entries.Length == 0)
                    {
                        await Task.Delay(_options.PollInterval, ct);
                        continue;
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), "[NotificationBus] 消費迴圈例外，稍後重試");
                    try { await Task.Delay(_options.PollInterval, ct); } catch (OperationCanceledException) { break; }
                }
            }

            Log.Info("[NotificationBus] 消費迴圈已停止");
        }

        internal async Task ProcessEntryAsync(IDatabase db, int shardId, StreamEntry entry)
        {
            string metricType = null;
            try
            {
                if (!NotificationBus.TryGetPayload(entry, out var type, out var payload))
                {
                    // 格式錯誤訊息：缺少欄位，直接 ACK 丟棄，避免卡住佇列。
                    Log.Warn($"[NotificationBus] 格式錯誤訊息（缺 type/payload），已丟棄：{entry.Id}");
                    await NotificationBus.AckAsync(db, shardId, entry.Id);
                    _metrics?.RecordNotificationBusMessage(null, NotificationBusMetricResult.InvalidPayload);
                    return;
                }

                metricType = type;

                var dedupKey = NotificationDedupPolicy.TryGetKey(shardId, type, payload);

                // 送出成功但 ack 失敗 → XAUTOCLAIM 重投時，去重鍵已存在 → 直接 ack 略過（避免重複發送）
                if (dedupKey != null && await db.KeyExistsAsync(dedupKey))
                {
                    await NotificationBus.AckAsync(db, shardId, entry.Id);
                    _metrics?.RecordNotificationBusMessage(type, NotificationBusMetricResult.Deduplicated);
                    return;
                }

                if (!await _dispatchAsync(type, payload))
                {
                    await NotificationBus.AckAsync(db, shardId, entry.Id);
                    _metrics?.RecordNotificationBusMessage(type, NotificationBusMetricResult.InvalidPayload);
                    return;
                }

                if (dedupKey != null)
                    await db.StringSetAsync(dedupKey, "1", _options.DedupTtl);

                await NotificationBus.AckAsync(db, shardId, entry.Id);
                _metrics?.RecordNotificationBusMessage(type, NotificationBusMetricResult.Dispatched);
            }
            catch (Exception ex)
            {
                _metrics?.RecordNotificationBusMessage(metricType, NotificationBusMetricResult.DispatchFailed);
                // 不 ack：留在 PEL，交由 XAUTOCLAIM 於逾時後補救重投
                Log.Error(ex.Demystify(), $"[NotificationBus] 處理訊息失敗（不 ack，留待 XAUTOCLAIM）: {entry.Id}");
            }
        }

        private async Task<bool> DispatchAsync(string type, string json)
        {
            switch (type)
            {
                case NotifyType.Youtube:
                    var youtubeDto = JsonConvert.DeserializeObject<YoutubeNotification>(json);
                    if (youtubeDto == null) return false;
                    await _youtubeStreamService.DispatchFromBusAsync(youtubeDto);
                    return true;

                case NotifyType.Twitch:
                    var twitchDto = JsonConvert.DeserializeObject<TwitchNotification>(json);
                    if (twitchDto == null) return false;
                    await _twitchService.DispatchFromBusAsync(twitchDto);
                    return true;

                case NotifyType.Twitcasting:
                    var twitcastingDto = JsonConvert.DeserializeObject<TwitcastingNotification>(json);
                    if (twitcastingDto == null) return false;
                    await _twitcastingService.DispatchFromBusAsync(twitcastingDto);
                    return true;

                case NotifyType.Banner:
                    var bannerDto = JsonConvert.DeserializeObject<BannerChangeNotification>(json);
                    if (bannerDto == null) return false;
                    await _youtubeStreamService.DispatchBannerFromBusAsync(bannerDto);
                    return true;

                case NotifyType.YoutubeMemberVideoLog:
                    var memberVideoLogDto = JsonConvert.DeserializeObject<YoutubeMemberVideoLogNotification>(json);
                    if (memberVideoLogDto == null) return false;
                    await _youtubeMemberService.DispatchMemberVideoLogFromBusAsync(memberVideoLogDto);
                    return true;

                default:
                    Log.Warn($"[NotificationBus] 尚未處理的通知類型：{type}，暫時 ACK 後略過。");
                    return false;
            }
        }
    }

    internal sealed record NotificationBusConsumerOptions(
        TimeSpan PollInterval,
        TimeSpan AutoClaimMinIdle,
        TimeSpan DedupTtl,
        int AutoClaimEveryPolls,
        int BatchSize)
    {
        internal static NotificationBusConsumerOptions Default { get; } = new(
            TimeSpan.FromSeconds(1.5),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(30),
            200,
            20);
    }
}
