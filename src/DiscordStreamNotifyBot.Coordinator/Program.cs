using DiscordStreamNotifyBot.Shared;

namespace DiscordStreamNotifyBot.Coordinator
{
    internal class Program
    {
        private static Task Main(string[] args)
        {
            // 階段 0：空殼。後續階段 4 將實作心跳監控、leader 續租觀察、shard 租約分配與叢集狀態回報。
            Console.WriteLine($"[{BotRole.Coordinator}] 尚未實作。");
            return Task.CompletedTask;
        }
    }
}
