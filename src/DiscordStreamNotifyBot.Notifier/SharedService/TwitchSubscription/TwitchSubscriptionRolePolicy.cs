using DiscordStreamNotifyBot.DataBase.Table;

namespace DiscordStreamNotifyBot.SharedService.TwitchSubscription
{
    internal static class TwitchSubscriptionRolePolicy
    {
        public static ulong GetTierRoleId(GuildTwitchSubscriptionConfig config, string tier) => tier switch
        {
            "1000" => config.Tier1RoleId,
            "2000" => config.Tier2RoleId,
            "3000" => config.Tier3RoleId,
            _ => 0
        };

        public static IReadOnlyList<ulong> GetOtherTierRoleIds(
            GuildTwitchSubscriptionConfig config,
            string tier)
        {
            ulong desired = GetTierRoleId(config, tier);
            return new[] { config.Tier1RoleId, config.Tier2RoleId, config.Tier3RoleId }
                .Where(id => id != 0 && id != desired)
                .Distinct()
                .ToArray();
        }

        public static (ulong[] AddRoleIds, ulong[] RemoveRoleIds) GetSynchronizationDiff(
            GuildTwitchSubscriptionConfig config,
            string tier,
            IReadOnlySet<ulong> currentRoleIds)
        {
            ulong tierRoleId = GetTierRoleId(config, tier);
            ulong[] addRoleIds = new[] { config.SubscriberRoleId, tierRoleId }
                .Where(x => x != 0 && !currentRoleIds.Contains(x))
                .ToArray();
            ulong[] removeRoleIds = GetOtherTierRoleIds(config, tier)
                .Where(currentRoleIds.Contains)
                .ToArray();
            return (addRoleIds, removeRoleIds);
        }

        public static string GetTierRoleName(string subscriberRoleName, string tier)
        {
            string suffix = tier switch
            {
                "1000" => " Tier 1",
                "2000" => " Tier 2",
                "3000" => " Tier 3",
                _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null)
            };
            const int maximumRoleNameLength = 100;
            int prefixLength = Math.Max(0, maximumRoleNameLength - suffix.Length);
            string prefix = subscriberRoleName.Length <= prefixLength
                ? subscriberRoleName
                : subscriberRoleName[..prefixLength];
            return prefix + suffix;
        }
    }
}
