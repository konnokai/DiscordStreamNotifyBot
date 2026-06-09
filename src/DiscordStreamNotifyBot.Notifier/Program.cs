using DiscordStreamNotifyBot.Shared;

namespace DiscordStreamNotifyBot.Notifier
{
    internal class Program
    {
        private static Task Main(string[] args)
        {
            // 階段 0：空殼。後續階段 2 將搬入 Discord 連線、指令系統 (Interaction/Command) 與 shard 通知發送。
            Console.WriteLine($"[{BotRole.Notifier}] 尚未實作。");
            return Task.CompletedTask;
        }
    }
}
