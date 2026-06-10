using System.Text;

namespace DiscordStreamNotifyBot.Shared
{
    /// <summary>
    /// 通知匯流排發布端共用單例（階段 3 cutover）。
    /// <para>
    /// 同一程序內所有服務（YouTube/Twitch/Twitcasting/Banner…）共用一條 RabbitMQ 發布連線，
    /// 延遲初始化、執行緒安全。僅在 <c>EnableNotificationBus</c> 開啟的程式路徑被呼叫。
    /// </para>
    /// </summary>
    public static class NotificationBusPublisher
    {
        private static RabbitMqService _service;
        private static readonly SemaphoreSlim _initLock = new(1, 1);

        /// <summary>將物件序列化為 JSON 後發佈到 <c>bot.notify</c> exchange。</summary>
        public static async Task PublishJsonAsync(BotConfig.RabbitMqConfig config, string routingKey, object payload)
        {
            await EnsureInitializedAsync(config).ConfigureAwait(false);
            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));
            await _service.PublishAsync(routingKey, body).ConfigureAwait(false);
        }

        private static async Task EnsureInitializedAsync(BotConfig.RabbitMqConfig config)
        {
            if (_service != null) return;

            await _initLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_service == null)
                {
                    var service = new RabbitMqService(config);
                    await service.InitializeAsync().ConfigureAwait(false);
                    _service = service;
                }
            }
            finally
            {
                _initLock.Release();
            }
        }
    }
}
