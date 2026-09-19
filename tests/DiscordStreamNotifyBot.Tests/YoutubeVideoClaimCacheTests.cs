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
        public void BatchReleasesFailedOrOmittedVideosButRetainsCompletedVideos()
        {
            var cache = new YoutubeVideoClaimCache(new FakeTimeProvider(), Ttl);
            using (var batch = cache.CreateBatch())
            {
                Assert.True(batch.TryClaim("completed"));
                Assert.True(batch.TryClaim("omitted"));
                Assert.True(batch.TryClaim("failed"));
                batch.Complete("completed");
                Assert.False(batch.TryClaim("failed"));
            }

            Assert.False(cache.TryClaim("completed"));
            Assert.True(cache.TryClaim("omitted"));
            Assert.True(cache.TryClaim("failed"));
        }

        [Fact]
        public void DisposingExpiredBatchDoesNotReleaseReplacementClaim()
        {
            var clock = new FakeTimeProvider();
            var cache = new YoutubeVideoClaimCache(clock, Ttl);
            var previous = cache.CreateBatch();
            Assert.True(previous.TryClaim("video"));
            clock.Advance(Ttl);
            using var replacement = cache.CreateBatch();
            Assert.True(replacement.TryClaim("video"));
            clock.Advance(Ttl);
            Assert.True(replacement.TryClaim("video"));

            previous.Dispose();

            Assert.False(cache.TryClaim("video"));
            replacement.Dispose();
            Assert.True(cache.TryClaim("video"));
        }

        [Fact]
        public void ReleasedClaimCanBeRetriedImmediately()
        {
            var cache = new YoutubeVideoClaimCache(new FakeTimeProvider(), Ttl);

            Assert.True(cache.TryClaim("video-id"));
            cache.Release("video-id");

            Assert.True(cache.TryClaim("video-id"));
        }

        /// <summary>
        /// 已完成的 claim 會留在快取直到 TTL 到期，後續排程只看 claim 會把它當成「處理中」。
        /// 這是呼叫端必須先判斷影片是否已知、再搶 claim 的原因（避免 Atom validator 卡住一天）。
        /// </summary>
        [Fact]
        public void CompletedClaimStillBlocksLaterBatchesUntilTtl()
        {
            var timeProvider = new FakeTimeProvider();
            var cache = new YoutubeVideoClaimCache(timeProvider, Ttl);

            using (var batch = cache.CreateBatch())
            {
                Assert.True(batch.TryClaim("completed"));
                batch.Complete("completed");
            }

            using (var laterBatch = cache.CreateBatch())
            {
                Assert.False(laterBatch.TryClaim("completed"));
            }

            timeProvider.Advance(Ttl);
            using (var expiredBatch = cache.CreateBatch())
            {
                Assert.True(expiredBatch.TryClaim("completed"));
            }
        }
    }
}
