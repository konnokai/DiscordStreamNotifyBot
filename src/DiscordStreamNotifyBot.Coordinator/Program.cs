using DiscordStreamNotifyBot.Shared;

namespace DiscordStreamNotifyBot.Coordinator
{
    internal class Program
    {
        private const BotRole Role = BotRole.Coordinator;

        private static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
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
            await service.RunAsync(GracefulShutdown.Token);
            return 0;
        }
    }
}
