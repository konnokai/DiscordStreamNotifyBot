using DiscordStreamNotifyBot.Localization;
using Microsoft.Extensions.Time.Testing;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class GuildLocaleServiceTests
    {
        [Fact]
        public async Task GetCachesConfiguredLocaleUntilExactExpiry()
        {
            var timeProvider = new FakeTimeProvider();
            int loads = 0;
            var service = CreateService(
                timeProvider,
                loadOne: _ => Task.FromResult(++loads == 1 ? "en-GB" : "ja"));

            Assert.Equal("en-US", await service.GetCoreAsync(1, "zh-TW"));
            timeProvider.Advance(TimeSpan.FromMinutes(5) - TimeSpan.FromTicks(1));
            Assert.Equal("en-US", await service.GetCoreAsync(1, "zh-TW"));
            timeProvider.Advance(TimeSpan.FromTicks(1));
            Assert.Equal("ja", await service.GetCoreAsync(1, "zh-TW"));
            Assert.Equal(2, loads);
        }

        [Fact]
        public async Task CachedNullLocaleUsesCurrentPreferredLocale()
        {
            var timeProvider = new FakeTimeProvider();
            int loads = 0;
            var service = CreateService(
                timeProvider,
                loadOne: _ =>
                {
                    loads++;
                    return Task.FromResult<string>(null);
                });

            Assert.Equal("en-US", await service.GetCoreAsync(1, "en-GB"));
            Assert.Equal("ja", await service.GetCoreAsync(1, "ja-JP"));
            Assert.Equal(1, loads);
        }

        [Fact]
        public async Task InvalidateReloadsOnlyTargetGuild()
        {
            var timeProvider = new FakeTimeProvider();
            var loads = new Dictionary<ulong, int>();
            var service = CreateService(
                timeProvider,
                loadOne: guildId =>
                {
                    loads[guildId] = loads.GetValueOrDefault(guildId) + 1;
                    return Task.FromResult("en-US");
                });

            await service.GetCoreAsync(1, null);
            await service.GetCoreAsync(2, null);
            service.Invalidate(1);
            await service.GetCoreAsync(1, null);
            await service.GetCoreAsync(2, null);

            Assert.Equal(2, loads[1]);
            Assert.Equal(1, loads[2]);
        }

        [Fact]
        public async Task ConcurrentGetsForSameGuildUseSingleFlight()
        {
            var timeProvider = new FakeTimeProvider();
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            int loads = 0;
            var service = CreateService(
                timeProvider,
                loadOne: guildId =>
                {
                    Interlocked.Increment(ref loads);
                    entered.TrySetResult();
                    return release.Task;
                });

            Task<string>[] gets = Enumerable.Range(0, 8)
                .Select(_ => service.GetCoreAsync(1, "zh-TW"))
                .ToArray();
            await entered.Task;
            Assert.Equal(1, loads);

            release.TrySetResult("ja-JP");
            string[] locales = await Task.WhenAll(gets);

            Assert.All(locales, locale => Assert.Equal("ja", locale));
            Assert.Equal(1, loads);
        }

        [Fact]
        public async Task DifferentGuildsCanLoadConcurrently()
        {
            var timeProvider = new FakeTimeProvider();
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            int active = 0;
            var service = CreateService(
                timeProvider,
                loadOne: async guildId =>
                {
                    if (Interlocked.Increment(ref active) == 2)
                        entered.TrySetResult();
                    try
                    {
                        return await release.Task;
                    }
                    finally
                    {
                        Interlocked.Decrement(ref active);
                    }
                });

            Task<string> first = service.GetCoreAsync(1, null);
            Task<string> second = service.GetCoreAsync(2, null);
            await entered.Task;
            release.TrySetResult("en-US");

            Assert.Equal("en-US", await first);
            Assert.Equal("en-US", await second);
        }

        [Fact]
        public async Task BatchDeduplicatesIdsAndUsesFirstPreferredLocale()
        {
            var timeProvider = new FakeTimeProvider();
            IReadOnlyCollection<ulong> loadedIds = null;
            var service = CreateService(
                timeProvider,
                loadMany: ids =>
                {
                    loadedIds = ids.ToArray();
                    return Task.FromResult<IReadOnlyDictionary<ulong, string>>(
                        new Dictionary<ulong, string> { [2] = "ja-JP" });
                });

            Dictionary<ulong, string> locales = await service.GetManyCoreAsync(new[]
            {
                new GuildLocaleRequest(1, "en-GB"),
                new GuildLocaleRequest(1, "ja-JP"),
                new GuildLocaleRequest(2, "zh-TW"),
            });

            Assert.Equal(new ulong[] { 1, 2 }, loadedIds.OrderBy(id => id));
            Assert.Equal("en-US", locales[1]);
            Assert.Equal("ja", locales[2]);
        }

        [Fact]
        public async Task RepeatedBatchCachesMissingRowsAndLoadsOnlyInvalidatedIds()
        {
            var timeProvider = new FakeTimeProvider();
            var batches = new List<ulong[]>();
            var service = CreateService(
                timeProvider,
                loadMany: ids =>
                {
                    batches.Add(ids.OrderBy(id => id).ToArray());
                    return Task.FromResult<IReadOnlyDictionary<ulong, string>>(
                        new Dictionary<ulong, string> { [1] = "en-US" });
                });
            var requests = new[]
            {
                new GuildLocaleRequest(1, "zh-TW"),
                new GuildLocaleRequest(2, "ja-JP"),
            };

            Dictionary<ulong, string> first = await service.GetManyCoreAsync(requests);
            Dictionary<ulong, string> cached = await service.GetManyCoreAsync(requests);
            service.Invalidate(2);
            Dictionary<ulong, string> invalidated = await service.GetManyCoreAsync(requests);

            Assert.Equal("en-US", first[1]);
            Assert.Equal("ja", first[2]);
            Assert.Equal(first, cached);
            Assert.Equal(first, invalidated);
            Assert.Equal(2, batches.Count);
            Assert.Equal(new ulong[] { 1, 2 }, batches[0]);
            Assert.Equal(new ulong[] { 2 }, batches[1]);
        }

        [Fact]
        public async Task PartialCacheBatchLoadsOnlyMissingIds()
        {
            var timeProvider = new FakeTimeProvider();
            IReadOnlyCollection<ulong> loadedIds = null;
            var service = CreateService(
                timeProvider,
                loadOne: _ => Task.FromResult("en-US"),
                loadMany: ids =>
                {
                    loadedIds = ids.ToArray();
                    return Task.FromResult<IReadOnlyDictionary<ulong, string>>(
                        new Dictionary<ulong, string> { [2] = "ja" });
                });

            await service.GetCoreAsync(1, null);
            Dictionary<ulong, string> locales = await service.GetManyCoreAsync(new[]
            {
                new GuildLocaleRequest(1, null),
                new GuildLocaleRequest(2, null),
            });

            Assert.Equal(new ulong[] { 2 }, loadedIds);
            Assert.Equal("en-US", locales[1]);
            Assert.Equal("ja", locales[2]);
        }

        [Fact]
        public async Task SetWarmsCacheAndUnsupportedLocaleDoesNotSave()
        {
            var timeProvider = new FakeTimeProvider();
            int reads = 0;
            var saves = new List<(ulong GuildId, string Locale)>();
            var service = CreateService(
                timeProvider,
                loadOne: _ =>
                {
                    reads++;
                    return Task.FromResult("zh-TW");
                },
                save: (guildId, locale) =>
                {
                    saves.Add((guildId, locale));
                    return Task.CompletedTask;
                });

            Assert.Equal("en-US", await service.SetAsync(1, "en-GB"));
            Assert.Equal("en-US", await service.GetCoreAsync(1, "ja"));
            await Assert.ThrowsAsync<ArgumentException>(() => service.SetAsync(2, "fr-FR"));

            Assert.Equal(0, reads);
            Assert.Equal(new[] { (1UL, "en-US") }, saves);
        }

        [Fact]
        public async Task FailedLoadReleasesGuildLockForRetry()
        {
            var timeProvider = new FakeTimeProvider();
            int loads = 0;
            var service = CreateService(
                timeProvider,
                loadOne: _ => ++loads == 1
                    ? Task.FromException<string>(new InvalidOperationException("expected"))
                    : Task.FromResult("ja"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetCoreAsync(1, null));

            Assert.Equal("ja", await service.GetCoreAsync(1, null));
            Assert.Equal(2, loads);
        }

        [Fact]
        public async Task InvalidateDuringLoadPreventsStaleResultFromBeingCached()
        {
            var timeProvider = new FakeTimeProvider();
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            int loads = 0;
            var service = CreateService(
                timeProvider,
                loadOne: _ =>
                {
                    loads++;
                    if (loads == 1)
                    {
                        entered.TrySetResult();
                        return release.Task;
                    }
                    return Task.FromResult("ja");
                });

            Task<string> inFlight = service.GetCoreAsync(1, null);
            await entered.Task;
            service.Invalidate(1);
            release.TrySetResult("en-US");

            Assert.Equal("en-US", await inFlight);
            Assert.Equal("ja", await service.GetCoreAsync(1, null));
            Assert.Equal(2, loads);
        }

        private static GuildLocaleService CreateService(
            TimeProvider timeProvider,
            Func<ulong, Task<string>> loadOne = null,
            Func<IReadOnlyCollection<ulong>, Task<IReadOnlyDictionary<ulong, string>>> loadMany = null,
            Func<ulong, string, Task> save = null)
        {
            return new GuildLocaleService(
                new LocaleResolver(),
                loadOne ?? (_ => Task.FromResult<string>(null)),
                loadMany ?? (_ => Task.FromResult<IReadOnlyDictionary<ulong, string>>(new Dictionary<ulong, string>())),
                save ?? ((_, _) => Task.CompletedTask),
                timeProvider);
        }
    }
}
