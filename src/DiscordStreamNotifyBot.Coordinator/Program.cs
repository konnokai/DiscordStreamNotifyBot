namespace DiscordStreamNotifyBot.Coordinator
{
    // 主控層進入點（計畫階段 4）：StartupPreflight + GracefulShutdown → CoordinatorService 監控迴圈。
    internal class Program
    {
        private const BotRole Role = BotRole.Coordinator;

        private static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Log.RolePrefix = "coordinator";
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

            var service = new CoordinatorService(config);
            try { await service.RunAsync(GracefulShutdown.Token); }
            catch (OperationCanceledException) { }

            Log.Info($"{Role} 已關閉");
            return 0;
        }
    }
}
