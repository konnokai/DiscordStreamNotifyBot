namespace DiscordStreamNotifyBot.Interaction
{
    internal static class InteractionErrorCodes
    {
        public const string GuildOnly = "precondition.guild-only";
        public const string GuildUnavailable = "precondition.guild-unavailable";
        public const string GuildOwnerOnly = "precondition.guild-owner-only";
        public const string GuildMemberCountPrefix = "precondition.guild-member-count:";

        public static string GuildMemberCount(uint required, int actual)
            => $"{GuildMemberCountPrefix}{required}:{actual}";
    }
}
