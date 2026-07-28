using DiscordStreamNotifyBot.SharedService;
using Microsoft.Extensions.Time.Testing;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class NoticeCacheTests
    {
        [Fact]
        public void GetLoadsOnceWithinTtlAndReloadsAfterExpiry()
        {
            var timeProvider = new FakeTimeProvider();
            int loads = 0;
            var cache = new NoticeCache<int>(
                () => new List<int> { ++loads },
                timeProvider,
                TimeSpan.FromSeconds(30));

            List<int> first = cache.Get();
            timeProvider.Advance(TimeSpan.FromSeconds(30));
            List<int> atBoundary = cache.Get();
            timeProvider.Advance(TimeSpan.FromTicks(1));
            List<int> expired = cache.Get();

            Assert.Same(first, atBoundary);
            Assert.NotSame(first, expired);
            Assert.Equal(new[] { 1 }, first);
            Assert.Equal(new[] { 2 }, expired);
            Assert.Equal(2, loads);
        }

        [Fact]
        public void TtlStartsAfterLoaderCompletes()
        {
            var timeProvider = new FakeTimeProvider();
            int loads = 0;
            var cache = new NoticeCache<int>(
                () =>
                {
                    loads++;
                    timeProvider.Advance(TimeSpan.FromSeconds(20));
                    return new List<int> { loads };
                },
                timeProvider,
                TimeSpan.FromSeconds(30));

            List<int> first = cache.Get();
            timeProvider.Advance(TimeSpan.FromSeconds(30));

            Assert.Same(first, cache.Get());
            Assert.Equal(1, loads);
        }

        [Fact]
        public void InvalidateForcesNextGetToReload()
        {
            var timeProvider = new FakeTimeProvider();
            int loads = 0;
            var cache = new NoticeCache<int>(() => new List<int> { ++loads }, timeProvider);

            List<int> first = cache.Get();
            cache.Invalidate();
            List<int> second = cache.Get();

            Assert.NotSame(first, second);
            Assert.Equal(2, loads);
            Assert.Same(second, cache.Get());
        }

        [Fact]
        public async Task ConcurrentColdGetsUseSingleLoad()
        {
            var timeProvider = new FakeTimeProvider();
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var release = new ManualResetEventSlim();
            int loads = 0;
            var expected = new List<int> { 42 };
            var cache = new NoticeCache<int>(
                () =>
                {
                    Interlocked.Increment(ref loads);
                    entered.TrySetResult();
                    release.Wait();
                    return expected;
                },
                timeProvider);

            Task<List<int>>[] gets = Enumerable.Range(0, 8)
                .Select(_ => Task.Run(cache.Get))
                .ToArray();
            await entered.Task;
            release.Set();
            List<int>[] results = await Task.WhenAll(gets);

            Assert.Equal(1, loads);
            Assert.All(results, result => Assert.Same(expected, result));
        }

        [Fact]
        public void FailedLoadIsRetriedWithoutRefreshingCache()
        {
            var timeProvider = new FakeTimeProvider();
            int loads = 0;
            var cache = new NoticeCache<int>(
                () =>
                {
                    loads++;
                    if (loads == 1)
                        throw new InvalidOperationException("expected");
                    return new List<int> { loads };
                },
                timeProvider);

            Assert.Throws<InvalidOperationException>(() => cache.Get());

            Assert.Equal(new[] { 2 }, cache.Get());
            Assert.Equal(2, loads);
        }

        [Fact]
        public void NullSnapshotIsNotCached()
        {
            var timeProvider = new FakeTimeProvider();
            int loads = 0;
            var cache = new NoticeCache<int>(
                () =>
                {
                    loads++;
                    return null;
                },
                timeProvider);

            Assert.Null(cache.Get());
            Assert.Null(cache.Get());
            Assert.Equal(2, loads);
        }
    }
}
