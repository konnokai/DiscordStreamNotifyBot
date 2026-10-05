using DiscordStreamNotifyBot.DataBase.Table;

namespace DiscordStreamNotifyBot.SharedService.TwitchSubscription
{
    internal static class TwitchSubscriptionRolePolicy
    {
        /// <summary>Tier 1→3 的處理順序。</summary>
        public static readonly IReadOnlyList<string> Tiers = ["1000", "2000", "3000"];

        public static ulong GetTierRoleId(GuildTwitchSubscriptionConfig config, string tier) => tier switch
        {
            "1000" => config.Tier1RoleId,
            "2000" => config.Tier2RoleId,
            "3000" => config.Tier3RoleId,
            _ => 0
        };

        public static void SetTierRoleId(GuildTwitchSubscriptionConfig config, string tier, ulong roleId)
        {
            switch (tier)
            {
                case "1000": config.Tier1RoleId = roleId; break;
                case "2000": config.Tier2RoleId = roleId; break;
                case "3000": config.Tier3RoleId = roleId; break;
                default: throw new ArgumentOutOfRangeException(nameof(tier), tier, null);
            }
        }

        /// <summary>標記為待移除角色；呼叫端須先保存此狀態再碰 Discord。</summary>
        public static void QueueRoleRemoval(TwitchSubscriptionCheck check)
        {
            check.IsChecked = false;
            check.PendingRoleRemoval = true;
        }

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

        public static bool HasMissingTierRole(
            GuildTwitchSubscriptionConfig config,
            Func<ulong, bool> roleExists)
            => new[] { config.Tier1RoleId, config.Tier2RoleId, config.Tier3RoleId }
                .Any(id => id == 0 || !roleExists(id));

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
