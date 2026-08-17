namespace DiscordStreamNotifyBot.SharedService.AdminSettings
{
    internal static class CrawlerPolicy
    {
        public static int ResolveLimit(uint? configured, int fallback)
            => configured is > 0 ? (int)configured.Value : fallback;

        public static bool HasGeneralEligibility(
            ulong actorUserId,
            ulong botOwnerId,
            bool officialGuild,
            int memberCount,
            int requiredMemberCount)
            => actorUserId == botOwnerId || officialGuild || memberCount >= requiredMemberCount;

        public static bool CanRemove(ulong ownerGuildId, ulong guildId, bool botOwner)
            => botOwner || ownerGuildId == guildId;
    }
}
