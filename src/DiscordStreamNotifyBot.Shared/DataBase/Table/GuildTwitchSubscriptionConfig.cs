using System.ComponentModel.DataAnnotations;

namespace DiscordStreamNotifyBot.DataBase.Table
{
    public sealed class GuildTwitchSubscriptionConfig : DbEntity
    {
        public ulong GuildId { get; set; }

        [MaxLength(64)]
        public string BroadcasterId { get; set; } = "";

        [MaxLength(64)]
        public string BroadcasterLogin { get; set; } = "";

        [MaxLength(128)]
        public string BroadcasterDisplayName { get; set; } = "";

        public ulong SubscriberRoleId { get; set; }
        public ulong? PreviousSubscriberRoleId { get; set; }
        public bool DeletionPending { get; set; }
        public ulong Tier1RoleId { get; set; }
        public ulong Tier2RoleId { get; set; }
        public ulong Tier3RoleId { get; set; }
    }
}
