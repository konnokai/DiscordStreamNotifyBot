using DiscordStreamNotifyBot.Shared;
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
                GoogleOAuthOperationLockAcquireResult first = await operationLock.TryAcquireAsync(discordUserId);
                GoogleOAuthOperationLockAcquireResult contender = await operationLock.TryAcquireAsync(discordUserId);

                Assert.Equal(GoogleOAuthOperationLockAcquireStatus.Acquired, first.Status);
                Assert.Equal(GoogleOAuthOperationLockAcquireStatus.Contended, contender.Status);
                Assert.Equal(
                    GoogleOAuthOperationLockOwnershipStatus.Owned,
                    await first.Lease.EnsureOwnedAsync());
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
