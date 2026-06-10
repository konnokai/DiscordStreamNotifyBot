using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.Youtube;

namespace DiscordStreamNotifyBot
{
    /// <summary>
    /// 通知匯流排消費端（階段 3 cutover，opt-in）。
    /// <para>
    /// Notifier 的通知一律來自匯流排：連上 RabbitMQ、消費本 shard 的 <c>notify.shard.{id}</c>，
    /// 依 routing key 解析 DTO 並交給對應服務發送（偵測與發布由 Scraper 負責）。
    /// </para>
    /// <para>
    /// 已接線：YouTube / Twitch / Twitcasting / Banner。
    /// Member（會限身分組）不走匯流排：會限檢查經 shard 守衛天然按 shard 分區，role 操作為 REST 不綁 gateway。
    /// </para>
    /// </summary>
    public sealed class NotificationBusConsumer : IAsyncDisposable
    {
        private readonly BotConfig _botConfig;
        private readonly YoutubeStreamService _youtubeStreamService;
        private readonly SharedService.Twitch.TwitchService _twitchService;
        private readonly SharedService.Twitcasting.TwitcastingService _twitcastingService;
        private RabbitMqService _rabbitMq;
        private IAsyncDisposable _consumeChannel;

        public NotificationBusConsumer(BotConfig botConfig, YoutubeStreamService youtubeStreamService,
            SharedService.Twitch.TwitchService twitchService, SharedService.Twitcasting.TwitcastingService twitcastingService)
        {
            _botConfig = botConfig;
            _youtubeStreamService = youtubeStreamService;
            _twitchService = twitchService;
            _twitcastingService = twitcastingService;
        }

        public async Task StartAsync(int shardId)
        {
            _rabbitMq = new RabbitMqService(_botConfig.RabbitMQ);
            await _rabbitMq.InitializeAsync();

            _consumeChannel = await _rabbitMq.ConsumeShardQueueAsync(shardId, HandleMessageAsync);
            Log.Info($"[NotificationBus] 已開始消費 notify.shard.{shardId}");
        }

        private async Task<bool> HandleMessageAsync(string routingKey, ReadOnlyMemory<byte> body)
        {
            var json = System.Text.Encoding.UTF8.GetString(body.Span);

            switch (routingKey)
            {
                case NotifyRoutingKeys.Youtube:
                    var youtubeDto = JsonConvert.DeserializeObject<YoutubeNotification>(json);
                    if (youtubeDto == null) return true; // 壞訊息直接 ack 丟棄，避免卡佇列
                    await _youtubeStreamService.DispatchFromBusAsync(youtubeDto);
                    return true;

                case NotifyRoutingKeys.Twitch:
                    var twitchDto = JsonConvert.DeserializeObject<TwitchNotification>(json);
                    if (twitchDto == null) return true;
                    await _twitchService.DispatchFromBusAsync(twitchDto);
                    return true;

                case NotifyRoutingKeys.Twitcasting:
                    var twitcastingDto = JsonConvert.DeserializeObject<TwitcastingNotification>(json);
                    if (twitcastingDto == null) return true;
                    await _twitcastingService.DispatchFromBusAsync(twitcastingDto);
                    return true;

                case NotifyRoutingKeys.Banner:
                    var bannerDto = JsonConvert.DeserializeObject<BannerChangeNotification>(json);
                    if (bannerDto == null) return true;
                    await _youtubeStreamService.DispatchBannerFromBusAsync(bannerDto);
                    return true;

                default:
                    Log.Warn($"[NotificationBus] 尚未接線的 routing key: {routingKey}，暫時 ack 略過");
                    return true;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_consumeChannel != null)
                await _consumeChannel.DisposeAsync();
            if (_rabbitMq != null)
                await _rabbitMq.DisposeAsync();
        }
    }
}
