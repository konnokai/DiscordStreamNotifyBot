using DiscordStreamNotifyBot.Shared;

namespace DiscordStreamNotifyBot.Notifier
{
    internal class Program
    {
        private const BotRole Role = BotRole.Notifier;

        private static async Task<int> Main(string[] args)
        {
            var config = new BotConfig();
            config.InitBotConfig(Role);

            // notifier 啟動參數 ["shardId", "totalShards"]（方式 A 固定 shard），優先於設定檔/環境變數
            if (args.Length >= 1 && int.TryParse(args[0], out var shardId))
                config.ShardId = shardId;
            if (args.Length >= 2 && int.TryParse(args[1], out var totalShards))
                config.TotalShards = totalShards;

            try
            {
                await StartupPreflight.EnsureAsync(Role, config, TimeSpan.FromSeconds(60));
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                return 1;
            }

            // 階段 0/1：空殼。後續階段 2 將搬入 Discord 連線、指令系統 (Interaction/Command) 與 shard 通知發送。
            Log.Info($"[{Role}] 啟動連線檢查完成（Shard {config.ShardId}/{config.TotalShards}）；主邏輯尚未實作。");
            return 0;
        }
    }
}
