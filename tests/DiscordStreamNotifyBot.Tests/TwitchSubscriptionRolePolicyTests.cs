using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.SharedService.TwitchSubscription;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class TwitchSubscriptionRolePolicyTests
    {
        private static readonly GuildTwitchSubscriptionConfig Config = new()
        {
            Tier1RoleId = 101,
            Tier2RoleId = 102,
            Tier3RoleId = 103
        };

        [Theory]
        [InlineData("1000", 101UL)]
        [InlineData("2000", 102UL)]
        [InlineData("3000", 103UL)]
        [InlineData("4000", 0UL)]
        public void TierMapsToOnlyItsConfiguredRole(string tier, ulong expected)
        {
            Assert.Equal(expected, TwitchSubscriptionRolePolicy.GetTierRoleId(Config, tier));
        }

        [Theory]
        [InlineData("1000", 102UL, 103UL)]
        [InlineData("2000", 101UL, 103UL)]
        [InlineData("3000", 101UL, 102UL)]
        public void TierReplacementRemovesTheOtherTwoRoles(string tier, ulong first, ulong second)
        {
            Assert.Equal([first, second], TwitchSubscriptionRolePolicy.GetOtherTierRoleIds(Config, tier));
        }

        [Theory]
        [InlineData("1000", "Subscriber Tier 1")]
        [InlineData("2000", "Subscriber Tier 2")]
        [InlineData("3000", "Subscriber Tier 3")]
        public void TierRoleNamesFollowSharedRoleName(string tier, string expected)
        {
            Assert.Equal(expected, TwitchSubscriptionRolePolicy.GetTierRoleName("Subscriber", tier));
        }

        [Fact]
        public void TierRoleNameStaysWithinDiscordLimitAndKeepsTierSuffix()
        {
            string roleName = TwitchSubscriptionRolePolicy.GetTierRoleName(new string('a', 100), "3000");

            Assert.Equal(100, roleName.Length);
            Assert.EndsWith(" Tier 3", roleName, StringComparison.Ordinal);
        }
    }
}
