using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Interaction.TwitchSubscription;
using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.SharedService.TwitchSubscription;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class TwitchSubscriptionRolePolicyTests
    {
        private static readonly GuildTwitchSubscriptionConfig Config = new()
        {
            SubscriberRoleId = 100,
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

        [Fact]
        public void SynchronizationDiffOnlyReturnsRequiredRoleChanges()
        {
            var changed = TwitchSubscriptionRolePolicy.GetSynchronizationDiff(
                Config, "2000", new HashSet<ulong> { 100, 101 });
            var synchronized = TwitchSubscriptionRolePolicy.GetSynchronizationDiff(
                Config, "2000", new HashSet<ulong> { 100, 102 });

            Assert.Equal([102UL], changed.AddRoleIds);
            Assert.Equal([101UL], changed.RemoveRoleIds);
            Assert.Empty(synchronized.AddRoleIds);
            Assert.Empty(synchronized.RemoveRoleIds);
        }

        [Theory]
        [InlineData("1000", "Subscriber Tier 1")]
        [InlineData("2000", "Subscriber Tier 2")]
        [InlineData("3000", "Subscriber Tier 3")]
        public void TierRoleNamesFollowSharedRoleName(string tier, string expected)
        {
            Assert.Equal(expected, TwitchSubscriptionRolePolicy.GetTierRoleName("Subscriber", tier));
        }

        [Theory]
        [InlineData("1000", "Tier 1")]
        [InlineData("2000", "Tier 2")]
        [InlineData("3000", "Tier 3")]
        public void TierDisplayDoesNotExposeProviderCodes(string tier, string expected)
        {
            Assert.Equal(expected, TwitchSubscription.FormatTier(tier));
        }

        [Theory]
        [InlineData("zh-TW")]
        [InlineData("en-US")]
        [InlineData("ja")]
        public void VerifiedStatusLogUsesReadableTierName(string locale)
        {
            string message = new BotLocalizer().Format(
                "TwitchMember.StatusLog.Verified", locale, 123UL, "broadcaster", TwitchSubscription.FormatTier("1000"));

            Assert.Contains("`Tier 1`", message, StringComparison.Ordinal);
            Assert.DoesNotContain("1000", message, StringComparison.Ordinal);
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
