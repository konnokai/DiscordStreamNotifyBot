using DiscordStreamNotifyBot.Scraper.Detection.Twitch;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class TwitchReconcileDecisionTests
    {
        [Fact]
        public void RevocationDuringCurrentStreamTreatsUnspecifiedKindAsUtc()
        {
            Assert.True(TwitchDetectionService.WasAuthorizationRevokedDuringStream(
                new DateTime(2026, 7, 27, 12, 5, 0),
                new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc)));
            Assert.False(TwitchDetectionService.WasAuthorizationRevokedDuringStream(
                new DateTime(2026, 7, 27, 11, 55, 0),
                new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc)));
            Assert.False(TwitchDetectionService.WasAuthorizationRevokedDuringStream(
                null,
                new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc)));
        }
    }
}
