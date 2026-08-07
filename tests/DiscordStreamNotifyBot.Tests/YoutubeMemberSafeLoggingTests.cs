using DiscordStreamNotifyBot.SharedService.YoutubeMember;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class YoutubeMemberSafeLoggingTests
    {
        [Fact]
        public void OAuthFailureDescriptionDoesNotContainExceptionMessageOrTokenValue()
        {
            const string token = "access-token-value-must-never-be-logged";
            string message = YoutubeMemberSafeLogging.DescribeFailure("刷新 YouTube 憑證",
                new InvalidOperationException(token));

            Assert.DoesNotContain(token, message);
            Assert.DoesNotContain("InvalidOperationException:", message);
            Assert.Contains(nameof(InvalidOperationException), message);
        }
    }
}
