using DiscordStreamNotifyBot.Shared;

namespace DiscordStreamNotifyBot.Scraper
{
    internal class Program
    {
        private const BotRole Role = BotRole.Scraper;
        private const int MetricsPort = 9465;

        private static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Log.RolePrefix = "scraper";
            GracefulShutdown.Init();

            var config = new BotConfig();
            config.InitBotConfig(Role);

            try
            {
                await StartupPreflight.EnsureAsync(Role, config, TimeSpan.FromSeconds(60));
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "StartupPreflight 失敗");
                return 1;
            }

            var metrics = new ScraperMetrics();
            using var metricServer = new Prometheus.KestrelMetricServer(port: MetricsPort);
            try
            {
                metricServer.Start();
                Log.Info($"Prometheus 指標已啟動：http://0.0.0.0:{MetricsPort}/metrics");

                var service = new ScraperService(config, metrics);
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
            finally
            {
                await metricServer.StopAsync();
            }
        }
    }
}
