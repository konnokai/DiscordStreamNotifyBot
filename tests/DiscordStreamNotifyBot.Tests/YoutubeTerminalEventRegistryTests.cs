using DiscordStreamNotifyBot.Scraper.Detection.Youtube;
using DiscordStreamNotifyBot.Shared.Messages;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class YoutubeTerminalEventRegistryTests
    {
        [Fact]
        public async Task ConcurrentClaimsPublishExactlyOnce()
        {
            var registry = new YoutubeTerminalEventRegistry();
            int publishCount = 0;

            await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => registry.ExecuteOnceAsync(
                "video-id",
                YoutubeTerminalEventKind.End,
                () =>
                {
                    Interlocked.Increment(ref publishCount);
                    return Task.CompletedTask;
                })));

            Assert.Equal(1, publishCount);
        }

        [Fact]
        public async Task EndAndMemberOnlyShareTheSameClaim()
        {
            var registry = new YoutubeTerminalEventRegistry();

            var first = await registry.ExecuteOnceAsync(
                "video-id", YoutubeTerminalEventKind.MemberOnly, () => Task.CompletedTask);
            var duplicate = await registry.ExecuteOnceAsync(
                "video-id", YoutubeTerminalEventKind.End, () => Task.CompletedTask);

            Assert.Equal(YoutubeTerminalEventAction.Publish, first.Action);
            Assert.Equal(YoutubeTerminalEventAction.IgnoreDuplicate, duplicate.Action);
            Assert.Equal(YoutubeTerminalEventKind.MemberOnly, duplicate.ClaimedKind);
        }

        [Fact]
        public async Task DeleteAndUnarchivedUseIndependentClaims()
        {
            var registry = new YoutubeTerminalEventRegistry();

            var deleted = await registry.ExecuteOnceAsync(
                "video-id", YoutubeTerminalEventKind.Delete, () => Task.CompletedTask);
            var unarchived = await registry.ExecuteOnceAsync(
                "video-id", YoutubeTerminalEventKind.Unarchived, () => Task.CompletedTask);

            Assert.Equal(YoutubeTerminalEventAction.Publish, deleted.Action);
            Assert.Equal(YoutubeTerminalEventAction.Publish, unarchived.Action);
        }

        [Fact]
        public async Task FailedPublishAllowsWaitingSourceToRetry()
        {
            var registry = new YoutubeTerminalEventRegistry();
            int attempts = 0;

            var first = registry.ExecuteOnceAsync("video-id", YoutubeTerminalEventKind.End, () =>
            {
                Interlocked.Increment(ref attempts);
                throw new InvalidOperationException("publish failed");
            });
            var retry = registry.ExecuteOnceAsync("video-id", YoutubeTerminalEventKind.MemberOnly, () =>
            {
                Interlocked.Increment(ref attempts);
                return Task.CompletedTask;
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() => first);
            var retryDecision = await retry;

            Assert.Equal(2, attempts);
            Assert.Equal(YoutubeTerminalEventAction.Publish, retryDecision.Action);
        }

        [Theory]
        [InlineData(YoutubeNoticeType.End, false, false, 0)]
        [InlineData(YoutubeNoticeType.End, true, false, 1)]
        [InlineData(YoutubeNoticeType.Delete, false, false, 2)]
        [InlineData(YoutubeNoticeType.Delete, false, true, 3)]
        public void TerminalDtoFactsMapToExpectedKind(
            YoutubeNoticeType noticeType,
            bool isMemberOnly,
            bool isUnarchived,
            int expected)
        {
            Assert.Equal((YoutubeTerminalEventKind)expected,
                YoutubeTerminalEventRegistry.Classify(noticeType, isMemberOnly, isUnarchived));
        }

        [Fact]
        public void NonTerminalNoticeDoesNotClaimRegistry()
        {
            Assert.Null(YoutubeTerminalEventRegistry.Classify(YoutubeNoticeType.Start, false, false));
        }
    }
}
