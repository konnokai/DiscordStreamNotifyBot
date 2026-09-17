using DiscordStreamNotifyBot.Shared;

namespace DiscordStreamNotifyBot.Scraper
{
    internal class Program
    {
        private const BotRole Role = BotRole.Scraper;

        private static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Log.RolePrefix = "scraper";
            GracefulShutdown.Init();

            var config = new BotConfig();
            config.InitBotConfig(Role);
            Log.ConfigureLoki(config.LokiUrl);

            try
            {
                try
                {
                    await StartupPreflight.EnsureAsync(Role, config, TimeSpan.FromSeconds(60));
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), "StartupPreflight 失敗");
                    return 1;
                }

                try
                {
                    var service = new ScraperService(config);
                    return await service.RunAsync(GracefulShutdown.Token);
                }
                catch (OperationCanceledException)
                {
                    return 0;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), "Scraper 執行失敗");
                    return 1;
                }
            }
            finally
            {
                await Log.ShutdownAsync(TimeSpan.FromSeconds(3));
            }
        }
    }
}
