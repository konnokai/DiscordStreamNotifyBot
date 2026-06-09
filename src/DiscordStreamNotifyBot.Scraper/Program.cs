using DiscordStreamNotifyBot.Shared;

namespace DiscordStreamNotifyBot.Scraper
{
    internal class Program
    {
        private static Task Main(string[] args)
        {
            // 階段 0：空殼。後續階段 3 將搬入偵測 Timer、錄影 Redis 訂閱、PubSub 維護與 RabbitMQ publish。
            Console.WriteLine($"[{BotRole.Scraper}] 尚未實作。");
            return Task.CompletedTask;
        }
    }
}
