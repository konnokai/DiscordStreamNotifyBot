using System.ComponentModel.DataAnnotations;

namespace DiscordStreamNotifyBot.DataBase.Table
{
    public sealed class TwitchSubscriptionCheck : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong DiscordUserId { get; set; }

        [MaxLength(64)]
        public string BroadcasterId { get; set; } = "";

        [MaxLength(16)]
        public string Locale { get; set; }

        public bool IsChecked { get; set; }
        public bool PendingRoleRemoval { get; set; }

        [MaxLength(4)]
        public string Tier { get; set; }

        public bool IsGift { get; set; }
        public DateTime LastCheckTime { get; set; } = DateTime.UtcNow;
    }
}
