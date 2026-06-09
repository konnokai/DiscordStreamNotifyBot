using RabbitMQ.Client;
using RabbitMQ.Client.Events;
// 與全域 using Discord 的 IConnection/IChannel 區隔
using IConnection = RabbitMQ.Client.IConnection;
using IChannel = RabbitMQ.Client.IChannel;

namespace DiscordStreamNotifyBot.Shared
{
    /// <summary>
    /// RabbitMQ 通知匯流排連線封裝（原生 <c>RabbitMQ.Client</c>，計畫 §2.1 / §4.1）。
    /// <para>
    /// 宣告 <c>bot.notify</c> topic exchange + DLX，提供 scraper 端 publish 與 notifier 端 per-shard queue 消費。
    /// 起步採「廣播 + 各自過濾」路由：每個 shard queue 以 <c>#</c> 綁定 exchange，收到後由 notifier 端依
    /// shard 歸屬過濾（計畫 §4.3）。
    /// </para>
    /// <para>
    /// 注意：本類別為階段 3 的連線基礎，目前尚未接上偵測→publish / 消費→送出的實際流程，
    /// 仍需在有 RabbitMQ broker 的環境下做多程序驗證。
    /// </para>
    /// </summary>
    public sealed class RabbitMqService : IAsyncDisposable
    {
        /// <summary>主通知 exchange（topic、durable）。</summary>
        public const string NotifyExchange = "bot.notify";

        /// <summary>死信 exchange（fanout、durable）。</summary>
        public const string NotifyDlx = "bot.notify.dlx";

        /// <summary>死信佇列。</summary>
        public const string DeadLetterQueue = "notify.dlq";

        private readonly ConnectionFactory _factory;
        private IConnection _connection;
        private IChannel _publishChannel;

        public RabbitMqService(BotConfig.RabbitMqConfig config)
        {
            _factory = new ConnectionFactory
            {
                HostName = config.HostName,
                Port = config.Port,
                UserName = config.UserName,
                Password = config.Password,
                VirtualHost = config.VirtualHost,
            };
        }

        /// <summary>
        /// 建立連線並宣告 topology（exchange + DLX + 死信佇列）。Publish / Consume 前需先呼叫。
        /// </summary>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            _connection = await _factory.CreateConnectionAsync(cancellationToken);
            _publishChannel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await DeclareTopologyAsync(_publishChannel, cancellationToken);
        }

        private static async Task DeclareTopologyAsync(IChannel channel, CancellationToken cancellationToken)
        {
            // 主 exchange：topic、durable
            await channel.ExchangeDeclareAsync(NotifyExchange, ExchangeType.Topic, durable: true, autoDelete: false,
                cancellationToken: cancellationToken);

            // 死信 exchange + 佇列：消費失敗（nack 不 requeue）的訊息流入此處
            await channel.ExchangeDeclareAsync(NotifyDlx, ExchangeType.Fanout, durable: true, autoDelete: false,
                cancellationToken: cancellationToken);
            await channel.QueueDeclareAsync(DeadLetterQueue, durable: true, exclusive: false, autoDelete: false,
                cancellationToken: cancellationToken);
            await channel.QueueBindAsync(DeadLetterQueue, NotifyDlx, routingKey: string.Empty,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 發佈一則通知事件到 <c>bot.notify</c> exchange。訊息為 persistent。
        /// </summary>
        /// <param name="routingKey">routing key：youtube / twitch / twitcasting / banner / member（§4.1）。</param>
        /// <param name="body">訊息內容（建議為 Newtonsoft JSON 的 UTF-8 bytes）。</param>
        public async Task PublishAsync(string routingKey, ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default)
        {
            if (_publishChannel is null)
                throw new InvalidOperationException("尚未呼叫 InitializeAsync()");

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
            };

            await _publishChannel.BasicPublishAsync(
                exchange: NotifyExchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 宣告並消費本 shard 的 durable 佇列 <c>notify.shard.{shardId}</c>（手動 ack）。
        /// handler 回傳 <c>true</c> 才 ack；回傳 <c>false</c> 或拋例外則 nack 不 requeue（進 DLX）。
        /// </summary>
        /// <returns>消費用的 channel（呼叫端負責保留參考；Dispose 時關閉）。</returns>
        public async Task<IChannel> ConsumeShardQueueAsync(
            int shardId,
            Func<string, ReadOnlyMemory<byte>, Task<bool>> handler,
            ushort prefetchCount = 20,
            CancellationToken cancellationToken = default)
        {
            if (_connection is null)
                throw new InvalidOperationException("尚未呼叫 InitializeAsync()");

            var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await DeclareTopologyAsync(channel, cancellationToken);

            string queueName = $"notify.shard.{shardId}";
            var queueArgs = new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = NotifyDlx,
                ["x-queue-type"] = "quorum",
            };

            await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false,
                arguments: queueArgs, cancellationToken: cancellationToken);

            // 廣播 + 各自過濾：以 # 綁定接收所有通知（§4.3）
            await channel.QueueBindAsync(queueName, NotifyExchange, routingKey: "#", cancellationToken: cancellationToken);

            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: prefetchCount, global: false, cancellationToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                bool acked = false;
                try
                {
                    bool success = await handler(ea.RoutingKey, ea.Body);
                    if (success)
                    {
                        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken);
                        acked = true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), $"RabbitMQ 消費 notify.shard.{shardId} 失敗");
                }

                if (!acked)
                {
                    // 失敗：不 requeue，交給 DLX，避免無限重送同一壞訊息
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken);
                }
            };

            await channel.BasicConsumeAsync(queueName, autoAck: false, consumer, cancellationToken);
            return channel;
        }

        public async ValueTask DisposeAsync()
        {
            if (_publishChannel is not null)
                await _publishChannel.DisposeAsync();
            if (_connection is not null)
                await _connection.DisposeAsync();
        }
    }
}
