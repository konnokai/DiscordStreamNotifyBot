using DiscordStreamNotifyBot.Shared;
using StackExchange.Redis;

namespace DiscordStreamNotifyBot.Tests.Component.Redis
{
    [Collection(RedisComponentCollection.Name)]
    [Trait("Category", "RedisComponent")]
    public sealed class ClusterServiceRedisComponentTests
    {
        private readonly RedisComponentFixture _fixture;

        public ClusterServiceRedisComponentTests(RedisComponentFixture fixture)
        {
            _fixture = fixture;
        }

        [RedisComponentFact]
        public async Task LeaderLockUsesSetNxTtlAndOwnerOnlyRenewAndRelease()
        {
            var db = _fixture.Database;
            var service = new ClusterService(db);
            var owner = $"component-owner-{Guid.NewGuid():N}";
            var contender = $"component-contender-{Guid.NewGuid():N}";
            RedisKey key = RedisChannels.Cluster.ScraperLeader;
            await RedisComponentFixture.AssertKeysAbsentAsync(db, key);

            try
            {
                Assert.True(await service.TryAcquireScraperLeaderAsync(owner, TimeSpan.FromSeconds(10)));
                Assert.False(await service.TryAcquireScraperLeaderAsync(contender, TimeSpan.FromSeconds(30)));
                Assert.Equal(owner, await service.GetScraperLeaderAsync());

                var initialTtl = await db.KeyTimeToLiveAsync(key);
                Assert.NotNull(initialTtl);
                Assert.InRange(initialTtl.Value, TimeSpan.Zero, TimeSpan.FromSeconds(10));

                Assert.False(await service.RenewScraperLeaderAsync(contender, TimeSpan.FromSeconds(30)));
                Assert.Equal(owner, (await db.StringGetAsync(key)).ToString());
                Assert.True((await db.KeyTimeToLiveAsync(key)) <= TimeSpan.FromSeconds(10));

                Assert.True(await service.RenewScraperLeaderAsync(owner, TimeSpan.FromSeconds(10)));
                Assert.True((await db.KeyTimeToLiveAsync(key)) > TimeSpan.FromSeconds(5));

                Assert.False(await service.ReleaseScraperLeaderAsync(contender));
                Assert.True(await db.KeyExistsAsync(key));
                Assert.True(await service.ReleaseScraperLeaderAsync(owner));
                Assert.False(await db.KeyExistsAsync(key));
            }
            finally
            {
                await RedisComponentFixture.DeleteStringIfOwnedAsync(db, key, owner, contender);
            }
        }
    }
}
