using DiscordStreamNotifyBot.SharedService.YoutubeMember;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class YoutubeMemberManualPinPolicyTests
    {
        [Fact]
        public void AutomaticRemovalPreservesManualPin()
        {
            Assert.Equal(
                YoutubeMemberAutomaticMutationAction.PreserveManualPin,
                YoutubeMemberManualPinPolicy.DecideAutomaticMutation(isManualVideoId: true));
        }

        [Fact]
        public void AutomaticRemovalAppliesToAutoSelectedConfig()
        {
            Assert.Equal(
                YoutubeMemberAutomaticMutationAction.Apply,
                YoutubeMemberManualPinPolicy.DecideAutomaticMutation(isManualVideoId: false));
        }
    }
}
