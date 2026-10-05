using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService;
using DiscordStreamNotifyBot.SharedService.Google;
using StackExchange.Redis;

namespace DiscordStreamNotifyBot.Tests.Component.Redis
{
    [Collection(RedisComponentCollection.Name)]
    [Trait("Category", "RedisComponent")]
    public sealed class GoogleOAuthOperationLockRedisComponentTests
    {
        private readonly RedisComponentFixture _fixture;

        public GoogleOAuthOperationLockRedisComponentTests(RedisComponentFixture fixture)
        {
            _fixture = fixture;
        }

        [RedisComponentFact]
        public void FactoryAlwaysSelectsSharedProviderDatabaseOne()
        {
            GoogleOAuthOperationLock operationLock = GoogleOAuthOperationLock.Create(_fixture.Connection);

            Assert.Equal(RedisChannels.OAuth.DatabaseNumber, operationLock.DatabaseNumber);
            Assert.Equal(1, operationLock.DatabaseNumber);
        }

        [RedisComponentFact]
        public async Task SameUserMutationsAreExclusiveAndOwnerReleaseRemovesTheKey()
        {
            ulong discordUserId = (ulong)Random.Shared.NextInt64(1, long.MaxValue);
            RedisKey key = RedisChannels.OAuth.GoogleOperationLock(discordUserId);
            IDatabase db = _fixture.Database;
            await RedisComponentFixture.AssertKeysAbsentAsync(db, key);
            var operationLock = new GoogleOAuthOperationLock(db);

            try
            {
                OAuthLeaseAcquireResult first = await operationLock.TryAcquireAsync(discordUserId);
                OAuthLeaseAcquireResult contender = await operationLock.TryAcquireAsync(discordUserId);

                Assert.Equal(OAuthLeaseAcquireStatus.Acquired, first.Status);
                Assert.Equal(OAuthLeaseAcquireStatus.Contended, contender.Status);
                Assert.Equal(
                    OAuthLeaseOwnershipStatus.Owned,
                    (await first.Lease.EnsureOwnedAsync()).Status);
                await first.Lease.DisposeAsync();
                Assert.False(await db.KeyExistsAsync(key));
            }
            finally
            {
                await db.KeyDeleteAsync(key);
            }
        }
    }
}
