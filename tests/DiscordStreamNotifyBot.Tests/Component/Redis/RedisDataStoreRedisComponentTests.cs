using Newtonsoft.Json;
using StackExchange.Redis;

namespace DiscordStreamNotifyBot.Tests.Component.Redis
{
    [Collection(RedisComponentCollection.Name)]
    [Trait("Category", "RedisComponent")]
    public sealed class RedisDataStoreRedisComponentTests
    {
        private const string EncryptionKey =
            "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

        private readonly RedisComponentFixture _fixture;

        public RedisDataStoreRedisComponentTests(RedisComponentFixture fixture)
        {
            _fixture = fixture;
        }

        [RedisComponentFact]
        public async Task StoreAndGetRoundTripUsesEncryptedRedisValue()
        {
            var db = _fixture.Database;
            var logicalKey = $"component-encrypted-{Guid.NewGuid():N}";
            RedisKey redisKey = RedisDataStore.GenerateStoredKey(logicalKey, typeof(StoredPayload));
            var originalRedisKey = Utility.RedisKey;
            await RedisComponentFixture.AssertKeysAbsentAsync(db, redisKey);

            try
            {
                Utility.RedisKey = EncryptionKey;
                var store = new RedisDataStore(_fixture.Connection, _fixture.DatabaseNumber);
                var payload = new StoredPayload { UserId = 42, AccessToken = "component-secret" };

                await store.StoreAsync(logicalKey, payload);

                var raw = (await db.StringGetAsync(redisKey)).ToString();
                Assert.NotEmpty(raw);
                Assert.DoesNotContain(payload.AccessToken, raw);
                Assert.Equal(3, raw.Split('.').Length);

                var restored = await store.GetAsync<StoredPayload>(logicalKey);
                Assert.Equal(payload.UserId, restored.UserId);
                Assert.Equal(payload.AccessToken, restored.AccessToken);
            }
            finally
            {
                Utility.RedisKey = originalRedisKey;
                await db.KeyDeleteAsync(redisKey);
            }
        }

        [RedisComponentFact]
        public async Task DeleteRemovesOnlyTheGeneratedStoredKey()
        {
            var db = _fixture.Database;
            var logicalKey = $"component-delete-{Guid.NewGuid():N}";
            RedisKey redisKey = RedisDataStore.GenerateStoredKey(logicalKey, typeof(StoredPayload));
            var originalRedisKey = Utility.RedisKey;
            await RedisComponentFixture.AssertKeysAbsentAsync(db, redisKey);

            try
            {
                Utility.RedisKey = EncryptionKey;
                var store = new RedisDataStore(_fixture.Connection, _fixture.DatabaseNumber);
                await store.StoreAsync(logicalKey, new StoredPayload { UserId = 7, AccessToken = "delete-me" });

                Assert.True(await db.KeyExistsAsync(redisKey));
                await store.DeleteAsync<StoredPayload>(logicalKey);
                Assert.False(await db.KeyExistsAsync(redisKey));
                Assert.Null(await store.GetAsync<StoredPayload>(logicalKey));
            }
            finally
            {
                Utility.RedisKey = originalRedisKey;
                await db.KeyDeleteAsync(redisKey);
            }
        }

        [RedisComponentFact]
        public async Task GetAcceptsLegacyPlaintextJson()
        {
            var db = _fixture.Database;
            var logicalKey = $"component-legacy-{Guid.NewGuid():N}";
            RedisKey redisKey = RedisDataStore.GenerateStoredKey(logicalKey, typeof(StoredPayload));
            var originalRedisKey = Utility.RedisKey;
            await RedisComponentFixture.AssertKeysAbsentAsync(db, redisKey);

            try
            {
                Utility.RedisKey = EncryptionKey;
                var expected = new StoredPayload { UserId = 99, AccessToken = "legacy-plaintext" };
                await db.StringSetAsync(redisKey, JsonConvert.SerializeObject(expected));

                var restored = await new RedisDataStore(
                    _fixture.Connection, _fixture.DatabaseNumber).GetAsync<StoredPayload>(logicalKey);

                Assert.Equal(expected.UserId, restored.UserId);
                Assert.Equal(expected.AccessToken, restored.AccessToken);
            }
            finally
            {
                Utility.RedisKey = originalRedisKey;
                await db.KeyDeleteAsync(redisKey);
            }
        }

        private sealed class StoredPayload
        {
            public int UserId { get; set; }

            public string AccessToken { get; set; }
        }
    }
}
