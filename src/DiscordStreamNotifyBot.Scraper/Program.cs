using DiscordStreamNotifyBot.Shared;

namespace DiscordStreamNotifyBot.Scraper
{
    internal class Program
    {
        private const BotRole Role = BotRole.Scraper;

        private static async Task<int> Main(string[] args)
        {
            var config = new BotConfig();
            config.InitBotConfig(Role);

            try
            {
                await StartupPreflight.EnsureAsync(Role, config, TimeSpan.FromSeconds(60));
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                return 1;
            }

            // 階段 0/1：空殼。後續階段 3 將搬入偵測 Timer、錄影 Redis 訂閱、PubSub 維護與 RabbitMQ publish。
            Log.Info($"[{Role}] 啟動連線檢查完成；主邏輯尚未實作。");
            return 0;
        }
    }
}
