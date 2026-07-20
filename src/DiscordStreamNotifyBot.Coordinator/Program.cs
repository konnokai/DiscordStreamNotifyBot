namespace DiscordStreamNotifyBot.Coordinator
{
    // 主控層進入點（計畫階段 4）：StartupPreflight + GracefulShutdown → CoordinatorService 監控迴圈。
    internal class Program
    {
        private const BotRole Role = BotRole.Coordinator;
        private const int MetricsPort = 9464;

        private static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Log.RolePrefix = "coordinator";
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

                var metrics = new CoordinatorMetrics();
                metrics.Start(config.TotalShards);

                using var metricServer = new Prometheus.KestrelMetricServer(port: MetricsPort);
                try
                {
                    metricServer.Start();
                    Log.Info($"Prometheus 指標已啟動：http://0.0.0.0:{MetricsPort}/metrics");

                    var service = new CoordinatorService(config, metrics);
                    await service.RunAsync(GracefulShutdown.Token);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), "Coordinator 執行失敗");
                    return 1;
                }
                finally
                {
                    metrics.Stop();
                    await metricServer.StopAsync();
                }

                Log.Info($"{Role} 已關閉");
                return 0;
            }
            finally
            {
                await Log.ShutdownAsync(TimeSpan.FromSeconds(3));
            }
        }
    }
}
