namespace DiscordStreamNotifyBot.Scraper
{
    // 階段 1 空殼：僅掛上 StartupPreflight + GracefulShutdown，驗證專案結構與 Shared 參考。
    // 偵測 Timer / 錄影 Redis 訂閱 / PubSub 維護 / leader 鎖 / Streams 發佈於階段 3 實作。
    internal class Program
    {
        private const BotRole Role = BotRole.Scraper;

        private static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            GracefulShutdown.Init();

            var config = new BotConfig();
            config.InitBotConfig();

            try
            {
                await StartupPreflight.EnsureAsync(Role, config, TimeSpan.FromSeconds(60));
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "StartupPreflight 失敗");
                return 1;
            }

            Log.Info($"{Role} 空殼啟動完成，等待關閉訊號（實際邏輯於後續階段實作）");
            try { await Task.Delay(Timeout.Infinite, GracefulShutdown.Token); }
            catch (OperationCanceledException) { }

            Log.Info($"{Role} 已關閉");
            return 0;
        }
    }
}
