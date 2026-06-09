using DiscordStreamNotifyBot.Shared;

namespace DiscordStreamNotifyBot.Coordinator
{
    internal class Program
    {
        private const BotRole Role = BotRole.Coordinator;

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

            // 階段 0/1：空殼。後續階段 4 將實作心跳監控、leader 續租觀察、shard 租約分配與叢集狀態回報。
            Log.Info($"[{Role}] 啟動連線檢查完成；主邏輯尚未實作。");
            return 0;
        }
    }
}
