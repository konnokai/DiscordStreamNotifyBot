using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.Youtube;

namespace DiscordStreamNotifyBot
{
    /// <summary>
    /// 通知匯流排消費端（階段 3 cutover，opt-in）。
    /// <para>
    /// 由 <c>EnableNotificationBus</c> 旗標啟用；啟用後連上 RabbitMQ，消費本 shard 的 <c>notify.shard.{id}</c>，
    /// 依 routing key 解析 DTO 並交給對應服務發送。預設關閉時完全不啟用，維持單體行為。
    /// </para>
    /// <para>
    /// 目前僅接上 YouTube 路徑（<see cref="YoutubeStreamService.DispatchFromBusAsync"/>）；
    /// Twitch / Twitcasting / Banner / Member 待後續路徑逐一接線（需 broker 實測）。
    /// </para>
    /// </summary>
    public sealed class NotificationBusConsumer : IAsyncDisposable
    {
        private readonly BotConfig _botConfig;
        private readonly YoutubeStreamService _youtubeStreamService;
        private RabbitMqService _rabbitMq;
        private IAsyncDisposable _consumeChannel;

        public NotificationBusConsumer(BotConfig botConfig, YoutubeStreamService youtubeStreamService)
        {
            _botConfig = botConfig;
            _youtubeStreamService = youtubeStreamService;
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
                    var dto = JsonConvert.DeserializeObject<YoutubeNotification>(json);
                    if (dto == null) return true; // 壞訊息直接 ack 丟棄，避免卡佇列
                    await _youtubeStreamService.DispatchFromBusAsync(dto);
                    return true;

                // TODO(階段 3)：twitch / twitcasting / banner / member 路徑接線
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
