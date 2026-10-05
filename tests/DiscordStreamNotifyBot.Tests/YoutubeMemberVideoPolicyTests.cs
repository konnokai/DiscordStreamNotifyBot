using DiscordStreamNotifyBot.Scraper.Detection.Youtube;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class YoutubeMemberVideoPolicyTests
    {
        [Theory]
        [InlineData(403, "forbidden", 1)]
        [InlineData(400, "parameter has disabled comments", 0)]
        [InlineData(404, "not found", 0)]
        [InlineData(500, "backend error", 2)]
        public void CandidateErrorMapsToExplicitAction(
            int? statusCode,
            string message,
            int expected)
        {
            Assert.Equal(
                (YoutubeDetectionService.MemberCandidateAction)expected,
                YoutubeDetectionService.ClassifyMemberCandidateError(statusCode, message));
        }

        [Fact]
        public void DisabledCommentsTakePrecedenceOverForbiddenStatus()
        {
            Assert.Equal(
                YoutubeDetectionService.MemberCandidateAction.Skip,
                YoutubeDetectionService.ClassifyMemberCandidateError(403, "parameter has disabled comments"));
        }
    }
}
