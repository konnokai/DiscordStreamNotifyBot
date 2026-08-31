using DiscordStreamNotifyBot.Scraper.Detection.Youtube;
using Microsoft.Extensions.Time.Testing;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class YoutubeVideoClaimCacheTests
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

        [Fact]
        public async Task ConcurrentClaimsHaveSingleWinner()
        {
            var cache = new YoutubeVideoClaimCache(new FakeTimeProvider(), Ttl);

            bool[] claims = await Task.WhenAll(Enumerable.Range(0, 100)
                .Select(_ => Task.Run(() => cache.TryClaim("video-id"))));

            Assert.Single(claims.Where(claimed => claimed));
            Assert.Equal(1, cache.Count);
        }

        [Fact]
        public async Task ExpiredClaimHasSingleNewWinnerWithoutSlidingExtension()
        {
            var timeProvider = new FakeTimeProvider();
            var cache = new YoutubeVideoClaimCache(timeProvider, Ttl);

            Assert.True(cache.TryClaim("video-id"));
            timeProvider.Advance(Ttl - TimeSpan.FromTicks(1));
            Assert.False(cache.TryClaim("video-id"));
            timeProvider.Advance(TimeSpan.FromTicks(1));

            bool[] claims = await Task.WhenAll(Enumerable.Range(0, 100)
                .Select(_ => Task.Run(() => cache.TryClaim("video-id"))));

            Assert.Single(claims.Where(claimed => claimed));
            Assert.Equal(1, cache.Count);
        }

        [Fact]
        public void CleanupRemovesOnlyExpiredClaims()
        {
            var timeProvider = new FakeTimeProvider();
            var cache = new YoutubeVideoClaimCache(timeProvider, Ttl);

            Assert.True(cache.TryClaim("expired"));
            timeProvider.Advance(TimeSpan.FromHours(12));
            Assert.True(cache.TryClaim("active"));
            timeProvider.Advance(TimeSpan.FromHours(12));

            Assert.Equal(1, cache.RemoveExpired());
            Assert.Equal(1, cache.Count);
            Assert.True(cache.TryClaim("expired"));
            Assert.False(cache.TryClaim("active"));
        }

        [Fact]
        public void ReleasedClaimCanBeRetriedImmediately()
        {
            var cache = new YoutubeVideoClaimCache(new FakeTimeProvider(), Ttl);

            Assert.True(cache.TryClaim("video-id"));
            cache.Release("video-id");

            Assert.True(cache.TryClaim("video-id"));
        }
    }
}
