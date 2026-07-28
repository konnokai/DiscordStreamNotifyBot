using DiscordStreamNotifyBot.Scraper.Detection.Youtube;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class YoutubeMemberVideoPolicyTests
    {
        [Fact]
        public void ManualPinWithoutMissingTitleSkipsAutomaticDiscovery()
        {
            var decision = YoutubeMemberVideoPolicy.PlanChannel(
                new YoutubeMemberChannelFacts(false, false));

            Assert.False(decision.DiscoverVideo);
            Assert.False(decision.RefreshChannelTitle);
        }

        [Fact]
        public void ManualPinCanRefreshTitleWithoutReplacingVideo()
        {
            var decision = YoutubeMemberVideoPolicy.PlanChannel(
                new YoutubeMemberChannelFacts(false, true));

            Assert.False(decision.DiscoverVideo);
            Assert.True(decision.RefreshChannelTitle);
        }

        [Fact]
        public void AutomaticConfigWithoutVideoRunsCandidateDiscovery()
        {
            var decision = YoutubeMemberVideoPolicy.PlanChannel(
                new YoutubeMemberChannelFacts(true, false));

            Assert.True(decision.DiscoverVideo);
        }

        [Theory]
        [InlineData(true, null, null, 0)]
        [InlineData(false, 403, "forbidden", 3)]
        [InlineData(false, 400, "parameter has disabled comments", 1)]
        [InlineData(false, 404, "not found", 2)]
        [InlineData(false, 500, "backend error", 4)]
        public void CandidateResponseMapsToExplicitAction(
            bool succeeded,
            int? statusCode,
            string message,
            int expected)
        {
            Assert.Equal(
                (YoutubeMemberCandidateAction)expected,
                YoutubeMemberVideoPolicy.ClassifyCandidate(
                    new YoutubeMemberCandidateFacts(succeeded, statusCode, message)));
        }

        [Fact]
        public void DisabledCommentsTakePrecedenceOverForbiddenStatus()
        {
            Assert.Equal(
                YoutubeMemberCandidateAction.IgnoreCommentsDisabled,
                YoutubeMemberVideoPolicy.ClassifyCandidate(
                    new YoutubeMemberCandidateFacts(false, 403, "parameter has disabled comments")));
        }
    }
}
