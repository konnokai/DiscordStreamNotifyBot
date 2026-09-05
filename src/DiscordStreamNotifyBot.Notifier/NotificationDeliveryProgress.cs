namespace DiscordStreamNotifyBot
{
    /// <summary>
    /// 單一匯流排事件的逐目標 checkpoint。成功步驟寫入 Redis，重播只續跑未完成部分；
    /// 不設定 TTL，避免長時間失敗後重送成功目標，資料由 consumer 在 ACK 時原子清除。
    /// Discord 成功與 Redis 寫入之間仍不是跨系統交易，程序在此間中斷時維持 at-least-once 語意。
    /// </summary>
    internal sealed class NotificationDeliveryProgress(
        Dictionary<string, string> completed,
        Func<string, string, Task> saveAsync)
    {
        private readonly List<Exception> _failures = [];

        internal static string Key(int shardId, RedisValue entryId) => $"notification:delivery:{shardId}:{entryId}";

        internal bool IsComplete(string target) => completed.ContainsKey($"done:{target}");

        internal Task CompleteAsync(string target) => RunAsync($"done:{target}", () => Task.FromResult("1"));

        internal void Fail(Exception exception) => _failures.Add(exception);

        internal void ThrowIfFailed()
        {
            if (_failures.Count != 0)
                throw new AggregateException("通知仍有未完成的接收目標，保留事件等待重試。", _failures);
        }

        /// <summary>只執行未保存的步驟；message ID 可供重播時重試 crosspost 而不另發一則訊息。</summary>
        internal async Task<string> RunAsync(string step, Func<Task<string>> action)
        {
            if (!completed.TryGetValue(step, out string result))
            {
                result = await action().ConfigureAwait(false);
                // 先保留 Discord 已完成的結果；Redis 暫時失敗時，同一輪重試只補寫 checkpoint。
                completed.Add(step, result);
            }
            await saveAsync(step, result).ConfigureAwait(false);
            return result;
        }

        /// <summary>發送與 crosspost 分開保存，外層沿用各平台的重試與錯誤分類。</summary>
        internal async Task SendAsync(string target, IMessageChannel channel, Func<Task<IUserMessage>> send, bool crosspost)
        {
            IUserMessage message = null;
            string messageId = await RunAsync($"message:{target}", async () =>
            {
                message = await send().ConfigureAwait(false);
                return message.Id.ToString();
            }).ConfigureAwait(false);
            if (!crosspost)
                return;

            await RunAsync($"crosspost:{target}", async () =>
            {
                message ??= await channel.GetMessageAsync(ulong.Parse(messageId)).ConfigureAwait(false) as IUserMessage
                    ?? throw new InvalidOperationException("找不到待發布的 Discord 通知訊息。");
                try
                {
                    await message.CrosspostAsync().ConfigureAwait(false);
                }
                catch (Discord.Net.HttpException ex) when (ex.DiscordCode == DiscordErrorCode.MessageAlreadyCrossposted)
                {
                    // 上次發布成功但 checkpoint 尚未寫入。
                }
                return "1";
            }).ConfigureAwait(false);
        }
    }
}
