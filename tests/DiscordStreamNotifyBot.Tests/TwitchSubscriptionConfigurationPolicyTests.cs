using DiscordStreamNotifyBot.Interaction.TwitchSubscription;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class TwitchSubscriptionConfigurationPolicyTests
    {
        [Theory]
        [InlineData("affiliate", true)]
        [InlineData("AFFILIATE", true)]
        [InlineData("partner", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void OnlyAffiliateAndPartnerCanBeConfigured(string broadcasterType, bool expected)
        {
            Assert.Equal(expected, TwitchSubscriptionSetting.IsEligibleBroadcaster(broadcasterType));
        }
    }
}
