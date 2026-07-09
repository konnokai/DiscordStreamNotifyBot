using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.Youtube;
using Newtonsoft.Json.Linq;

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
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1.5);
        private static readonly TimeSpan AutoClaimMinIdle = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan DedupTtl = TimeSpan.FromMinutes(5);
        // 空輪詢約 200 次（~5 分鐘）觸發一次 XAUTOCLAIM，回收本 consumer PEL 中逾時未 ack 的訊息
        private const int AutoClaimEveryEmptyPolls = 200;

        private readonly YoutubeStreamService _youtubeStreamService;
        private readonly SharedService.Twitch.TwitchService _twitchService;
        private readonly SharedService.Twitcasting.TwitcastingService _twitcastingService;

        private int _shardId;

        public NotificationBusConsumer(YoutubeStreamService youtubeStreamService,
            SharedService.Twitch.TwitchService twitchService,
            SharedService.Twitcasting.TwitcastingService twitcastingService)
        {
            _youtubeStreamService = youtubeStreamService;
            _twitchService = twitchService;
            _twitcastingService = twitcastingService;
        }

        /// <summary>建立本 shard 的 consumer group 並於背景啟動消費迴圈（吃 GracefulShutdown.Token）。</summary>
        public async Task StartAsync(int shardId)
        {
            _shardId = shardId;
            await NotificationBus.EnsureConsumerGroupAsync(BotState.RedisDb, shardId);
            _ = Task.Run(() => ConsumeLoopAsync(GracefulShutdown.Token));
            Log.Info($"[NotificationBus] 已開始消費 {NotificationBus.StreamKey}（group {NotificationBus.GroupName(shardId)}）");
        }

        private async Task ConsumeLoopAsync(CancellationToken ct)
        {
            var db = BotState.RedisDb;
            int emptyPolls = 0;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var entries = await NotificationBus.ReadNewAsync(db, _shardId, 20);

                    if (entries.Length == 0)
                    {
                        if (++emptyPolls >= AutoClaimEveryEmptyPolls)
                        {
                            emptyPolls = 0;
                            var claimed = await NotificationBus.AutoClaimAsync(db, _shardId, AutoClaimMinIdle, 20);
                            foreach (var entry in claimed)
                                await ProcessEntryAsync(db, entry);
                        }

                        await Task.Delay(PollInterval, ct);
                        continue;
                    }

                    emptyPolls = 0;
                    foreach (var entry in entries)
                        await ProcessEntryAsync(db, entry);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), "[NotificationBus] 消費迴圈例外，稍後重試");
                    try { await Task.Delay(PollInterval, ct); } catch (OperationCanceledException) { break; }
                }
            }

            Log.Info("[NotificationBus] 消費迴圈已停止");
        }

        private async Task ProcessEntryAsync(IDatabase db, StreamEntry entry)
        {
            try
            {
                if (!NotificationBus.TryGetPayload(entry, out var type, out var payload))
                {
                    // 壞訊息：欄位缺失，直接 ack 丟棄避免卡佇列
                    Log.Warn($"[NotificationBus] 壞訊息（缺 type/payload），丟棄: {entry.Id}");
                    await NotificationBus.AckAsync(db, _shardId, entry.Id);
                    return;
                }

                var dedupKey = TryGetDedupKey(_shardId, type, payload);

                // 送出成功但 ack 失敗 → XAUTOCLAIM 重投時，去重鍵已存在 → 直接 ack 略過（避免重複發送）
                if (dedupKey != null && await db.KeyExistsAsync(dedupKey))
                {
                    await NotificationBus.AckAsync(db, _shardId, entry.Id);
                    return;
                }

                await DispatchAsync(type, payload);

                if (dedupKey != null)
                    await db.StringSetAsync(dedupKey, "1", DedupTtl);

                await NotificationBus.AckAsync(db, _shardId, entry.Id);
            }
            catch (Exception ex)
            {
                // 不 ack：留在 PEL，交由 XAUTOCLAIM 於逾時後補救重投
                Log.Error(ex.Demystify(), $"[NotificationBus] 處理訊息失敗（不 ack，留待 XAUTOCLAIM）: {entry.Id}");
            }
        }

        private async Task DispatchAsync(string type, string json)
        {
            switch (type)
            {
                case NotifyType.Youtube:
                    var youtubeDto = JsonConvert.DeserializeObject<YoutubeNotification>(json);
                    if (youtubeDto != null) await _youtubeStreamService.DispatchFromBusAsync(youtubeDto);
                    break;

                case NotifyType.Twitch:
                    var twitchDto = JsonConvert.DeserializeObject<TwitchNotification>(json);
                    if (twitchDto != null) await _twitchService.DispatchFromBusAsync(twitchDto);
                    break;

                case NotifyType.Twitcasting:
                    var twitcastingDto = JsonConvert.DeserializeObject<TwitcastingNotification>(json);
                    if (twitcastingDto != null) await _twitcastingService.DispatchFromBusAsync(twitcastingDto);
                    break;

                case NotifyType.Banner:
                    var bannerDto = JsonConvert.DeserializeObject<BannerChangeNotification>(json);
                    if (bannerDto != null) await _youtubeStreamService.DispatchBannerFromBusAsync(bannerDto);
                    break;

                default:
                    Log.Warn($"[NotificationBus] 尚未接線的 type: {type}，暫時 ack 略過");
                    break;
            }
        }

        /// <summary>
        /// 依 DTO 主鍵與通知類型組去重鍵（§4.3）。解析失敗回傳 null＝不做去重（仍靠 XACK）。
        /// <para>
        /// 鍵必須帶 shardId：<c>bot:notify</c> 是廣播，每個 shard group 都會收到同一則訊息並各自對自己持有的伺服器發送。
        /// 若去重鍵不分 shard，先處理的 shard 設鍵後，其餘 shard 會誤判為重複而整個略過發送（其伺服器永遠收不到通知）。
        /// 去重只為吸收「同一 shard 送出成功但 ack 失敗，XAUTOCLAIM 重投」的重複，本就該按 shard 隔離。
        /// </para>
        /// </summary>
        private static string TryGetDedupKey(int shardId, string type, string json)
        {
            try
            {
                var jo = JObject.Parse(json);
                return type switch
                {
                    NotifyType.Youtube => $"notified:{shardId}:yt:{jo.Value<string>("VideoId")}:{jo.Value<int?>("NoticeType")}",
                    // 以直播「場次」StreamId（非 UserId）為單位去重：避免同一實況主 5 分鐘內的新場次被舊場次去重鍵誤擋。
                    // 保留 NoticeType：同場次的 Start/End 共用同一 StreamId，少了它會讓 End 被當成 Start 的重複吃掉。
                    // StreamId 為空（如 ChangeStreamData 不帶、或 EndStream 抓不到 Redis 資料）＝不去重（回 null，仍靠 XACK），
                    // 避免不同實況主因空 StreamId 互撞。
                    NotifyType.Twitch => string.IsNullOrEmpty(jo.Value<string>("StreamId"))
                        ? null
                        : $"notified:{shardId}:tw:{jo.Value<string>("StreamId")}:{jo.Value<int?>("NoticeType")}",
                    NotifyType.Twitcasting => $"notified:{shardId}:tc:{jo.Value<string>("ChannelId")}:{jo.Value<int?>("StreamId")}",
                    NotifyType.Banner => $"notified:{shardId}:banner:{jo.Value<string>("ChannelId")}:{jo.Value<string>("VideoId")}",
                    _ => null,
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
