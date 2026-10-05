using Polly;
using Polly.Retry;

namespace DiscordStreamNotifyBot.SharedService
{
    /// <summary>Discord 發送共用的重試策略：逾時或 Discord 5xx 時最多重試 3 次，等待 2、4、8 秒。</summary>
    internal static class DiscordRetryPolicy
    {
        /// <param name="onRetry">每次重試前以 (第幾次重試, 等待時間) 呼叫，供呼叫端記錄各自的 log 與 metric。</param>
        /// <param name="retryKeyNotFound">是否也重試 <see cref="KeyNotFoundException"/>（目前只有會限頻道訊息需要）。</param>
        public static AsyncRetryPolicy Create(Action<int, TimeSpan> onRetry, bool retryKeyNotFound = false)
        {
            PolicyBuilder builder = Policy.Handle<TimeoutException>();
            if (retryKeyNotFound)
                builder = builder.Or<KeyNotFoundException>();

            return builder
                .Or<Discord.Net.HttpException>((httpEx) => ((int)httpEx.HttpCode).ToString().StartsWith("50"))
                .WaitAndRetryAsync(3, (retryAttempt) =>
                {
                    var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                    onRetry(retryAttempt, timeSpan);
                    return timeSpan;
                });
        }
    }
}
