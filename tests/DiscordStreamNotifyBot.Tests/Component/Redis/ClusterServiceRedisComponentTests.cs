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

        [RedisComponentFact]
        public async Task ShardLeasesUseSetNxOwnerRenewAndClaimLowestFreeShard()
        {
            var db = _fixture.Database;
            var service = new ClusterService(db);
            var firstOwner = $"component-shard-owner-{Guid.NewGuid():N}";
            var claimant = $"component-shard-claimant-{Guid.NewGuid():N}";
            RedisKey[] keys =
            [
                RedisChannels.Cluster.ShardLease(0),
                RedisChannels.Cluster.ShardLease(1),
                RedisChannels.Cluster.ShardLease(2),
            ];
            await RedisComponentFixture.AssertKeysAbsentAsync(db, keys);

            try
            {
                Assert.True(await service.TryAcquireShardLeaseAsync(0, firstOwner, TimeSpan.FromSeconds(5)));
                Assert.False(await service.TryAcquireShardLeaseAsync(0, claimant, TimeSpan.FromSeconds(30)));
                Assert.False(await service.RenewShardLeaseAsync(0, claimant, TimeSpan.FromSeconds(30)));
                Assert.True(await service.RenewShardLeaseAsync(0, firstOwner, TimeSpan.FromSeconds(10)));
                Assert.True((await db.KeyTimeToLiveAsync(keys[0])) > TimeSpan.FromSeconds(5));

                Assert.Equal(1, await service.TryClaimAnyShardAsync(3, claimant, TimeSpan.FromSeconds(10)));
                Assert.Equal(2, await service.TryClaimAnyShardAsync(3, claimant, TimeSpan.FromSeconds(10)));
                Assert.Null(await service.TryClaimAnyShardAsync(3, claimant, TimeSpan.FromSeconds(10)));
                Assert.Equal(firstOwner, (await db.StringGetAsync(keys[0])).ToString());
                Assert.Equal(claimant, (await db.StringGetAsync(keys[1])).ToString());
                Assert.Equal(claimant, (await db.StringGetAsync(keys[2])).ToString());
            }
            finally
            {
                foreach (var key in keys)
                    await RedisComponentFixture.DeleteStringIfOwnedAsync(db, key, firstOwner, claimant);
            }
        }

        [RedisComponentFact]
        public async Task ExpiredLeaseCanBeClaimedAndFormerOwnerCannotMutateNewLease()
        {
            var db = _fixture.Database;
            var service = new ClusterService(db);
            const int shardId = 9106;
            var formerOwner = $"component-former-owner-{Guid.NewGuid():N}";
            var newOwner = $"component-new-owner-{Guid.NewGuid():N}";
            RedisKey key = RedisChannels.Cluster.ShardLease(shardId);
            await RedisComponentFixture.AssertKeysAbsentAsync(db, key);

            try
            {
                Assert.True(await service.TryAcquireShardLeaseAsync(
                    shardId, formerOwner, TimeSpan.FromMilliseconds(200)));
                await Task.Delay(TimeSpan.FromMilliseconds(500));

                Assert.True(await service.TryAcquireShardLeaseAsync(
                    shardId, newOwner, TimeSpan.FromSeconds(5)));
                Assert.False(await service.RenewShardLeaseAsync(
                    shardId, formerOwner, TimeSpan.FromSeconds(30)));
                Assert.Equal(newOwner, (await db.StringGetAsync(key)).ToString());
            }
            finally
            {
                await RedisComponentFixture.DeleteStringIfOwnedAsync(db, key, formerOwner, newOwner);
            }
        }

        [RedisComponentFact]
        public async Task ConcurrentClaimantsNeverReceiveTheSameShard()
        {
            var db = _fixture.Database;
            var service = new ClusterService(db);
            var owners = Enumerable.Range(0, 12)
                .Select(_ => $"component-claimant-{Guid.NewGuid():N}")
                .ToArray();
            RedisKey[] keys = Enumerable.Range(0, 3)
                .Select(RedisChannels.Cluster.ShardLease)
                .Select(x => (RedisKey)x)
                .ToArray();
            await RedisComponentFixture.AssertKeysAbsentAsync(db, keys);

            try
            {
                var claims = await Task.WhenAll(owners.Select(owner =>
                    service.TryClaimAnyShardAsync(3, owner, TimeSpan.FromSeconds(5))));
                var winners = claims.Where(x => x.HasValue).Select(x => x.Value).ToArray();

                Assert.Equal(3, winners.Length);
                Assert.Equal(3, winners.Distinct().Count());
            }
            finally
            {
                var ownerValues = owners.Select(x => (RedisValue)x).ToArray();
                foreach (var key in keys)
                    await RedisComponentFixture.DeleteStringIfOwnedAsync(db, key, ownerValues);
            }
        }
    }
}
