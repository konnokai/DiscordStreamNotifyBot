using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService.Twitch;
using StackExchange.Redis;

namespace DiscordStreamNotifyBot.Tests.Component.Redis
{
    [Collection(RedisComponentCollection.Name)]
    [Trait("Category", "RedisComponent")]
    public sealed class TwitchOAuthRefreshLockRedisComponentTests
    {
        private readonly RedisComponentFixture _fixture;

        public TwitchOAuthRefreshLockRedisComponentTests(RedisComponentFixture fixture)
        {
            _fixture = fixture;
        }

        [RedisComponentFact]
        public void FactoryAlwaysSelectsBackendTokenDatabaseOne()
        {
            TwitchOAuthRefreshLock refreshLock = TwitchOAuthRefreshLock.Create(_fixture.Connection);

            Assert.Equal(RedisChannels.OAuth.DatabaseNumber, refreshLock.DatabaseNumber);
            Assert.Equal(1, refreshLock.DatabaseNumber);
        }

        [RedisComponentFact]
        public async Task LockUsesSharedKeySetNxTtlAndOwnerOnlyRelease()
        {
            string twitchUserId = $"component-{Guid.NewGuid():N}";
            RedisKey key = RedisChannels.OAuth.TwitchRefreshLock(twitchUserId);
            IDatabase db = _fixture.Database;
            await RedisComponentFixture.AssertKeysAbsentAsync(db, key);
            var refreshLock = new TwitchOAuthRefreshLock(db);

            try
            {
                TwitchOAuthRefreshLockAcquireResult first = await refreshLock.TryAcquireAsync(twitchUserId);
                TwitchOAuthRefreshLockAcquireResult contender = await refreshLock.TryAcquireAsync(twitchUserId);

                Assert.Equal(TwitchOAuthRefreshLockAcquireStatus.Acquired, first.Status);
                Assert.Equal(TwitchOAuthRefreshLockAcquireStatus.Contended, contender.Status);
                TimeSpan? ttl = await db.KeyTimeToLiveAsync(key);
                Assert.NotNull(ttl);
                Assert.InRange(ttl.Value, TimeSpan.FromMinutes(9), TimeSpan.FromMinutes(10));
                Assert.Equal(
                    TwitchOAuthRefreshLockOwnershipStatus.Owned,
                    (await first.Lease.EnsureOwnedAsync()).Status);
                Assert.Equal(TwitchOAuthRefreshLockReleaseStatus.Released, (await first.Lease.ReleaseAsync()).Status);
                Assert.Equal(TwitchOAuthRefreshLockReleaseStatus.Released, (await first.Lease.ReleaseAsync()).Status);
                Assert.False(await db.KeyExistsAsync(key));
            }
            finally
            {
                await db.KeyDeleteAsync(key);
            }
        }

        [RedisComponentFact]
        public async Task ReleaseDoesNotDeleteAReplacementOwner()
        {
            string twitchUserId = $"component-{Guid.NewGuid():N}";
            RedisKey key = RedisChannels.OAuth.TwitchRefreshLock(twitchUserId);
            IDatabase db = _fixture.Database;
            await RedisComponentFixture.AssertKeysAbsentAsync(db, key);
            var refreshLock = new TwitchOAuthRefreshLock(db);

            try
            {
                TwitchOAuthRefreshLockAcquireResult first = await refreshLock.TryAcquireAsync(twitchUserId);
                Assert.Equal(TwitchOAuthRefreshLockAcquireStatus.Acquired, first.Status);
                await db.StringSetAsync(key, "replacement-owner", TimeSpan.FromMinutes(10), When.Always);

                var ownership = await first.Lease.EnsureOwnedAsync();
                var release = await first.Lease.ReleaseAsync();

                Assert.Equal(TwitchOAuthRefreshLockOwnershipStatus.OwnershipLost, ownership.Status);
                Assert.Equal(TwitchOAuthRefreshLockReleaseStatus.OwnershipLost, release.Status);
                Assert.Equal("replacement-owner", (await db.StringGetAsync(key)).ToString());
            }
            finally
            {
                await db.KeyDeleteAsync(key);
            }
        }
    }
}
