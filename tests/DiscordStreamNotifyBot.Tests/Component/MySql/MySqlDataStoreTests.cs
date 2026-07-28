using DiscordStreamNotifyBot.DataBase.Table;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DiscordStreamNotifyBot.Tests.Component.MySql
{
    [Collection(MySqlComponentCollection.Name)]
    [Trait("Category", "MySqlComponent")]
    public sealed class MySqlDataStoreTests
    {
        private readonly MySqlComponentFixture _fixture;

        public MySqlDataStoreTests(MySqlComponentFixture fixture)
        {
            _fixture = fixture;
        }

        [MySqlComponentFact]
        public async Task StoreAndGetRoundTripUsesEncryptedDatabaseValue()
        {
            var key = NextUserId().ToString();
            var payload = new StoredToken
            {
                AccessToken = "component-access-token",
                RefreshToken = "component-refresh-token"
            };
            var store = new MySqlDataStore(_fixture.DbService);

            await store.StoreAsync(key, payload);
            var result = await store.GetAsync<StoredToken>(key);

            Assert.Equal(payload.AccessToken, result.AccessToken);
            Assert.Equal(payload.RefreshToken, result.RefreshToken);
            await using var db = _fixture.DbService.GetDbContext();
            var databaseValue = await db.YoutubeMemberAccessToken.AsNoTracking()
                .Where(x => x.DiscordUserId == ulong.Parse(key))
                .Select(x => x.EncryptedAccessToken)
                .SingleAsync();
            Assert.NotEqual(JsonConvert.SerializeObject(payload), databaseValue);
            Assert.Equal(3, databaseValue.Split('.').Length);
        }

        [MySqlComponentFact]
        public async Task StoreUpdatesExistingRowAndDeleteRemovesIt()
        {
            var key = NextUserId().ToString();
            var store = new MySqlDataStore(_fixture.DbService);
            await store.StoreAsync(key, new StoredToken { AccessToken = "first" });

            string firstDatabaseValue;
            await using (var db = _fixture.DbService.GetDbContext())
            {
                firstDatabaseValue = await db.YoutubeMemberAccessToken.AsNoTracking()
                    .Where(x => x.DiscordUserId == ulong.Parse(key))
                    .Select(x => x.EncryptedAccessToken)
                    .SingleAsync();
            }

            await store.StoreAsync(key, new StoredToken { AccessToken = "second" });

            var updated = await store.GetAsync<StoredToken>(key);
            Assert.Equal("second", updated.AccessToken);
            await using (var db = _fixture.DbService.GetDbContext())
            {
                var rows = await db.YoutubeMemberAccessToken.AsNoTracking()
                    .Where(x => x.DiscordUserId == ulong.Parse(key))
                    .ToArrayAsync();
                Assert.Single(rows);
                Assert.NotEqual(firstDatabaseValue, rows[0].EncryptedAccessToken);
            }

            Assert.True(await store.IsExistUserTokenAsync<StoredToken>(key));
            await store.DeleteAsync<StoredToken>(key);
            Assert.False(await store.IsExistUserTokenAsync<StoredToken>(key));
            Assert.Null(await store.GetAsync<StoredToken>(key));
        }

        [MySqlComponentFact]
        public async Task GetReadsLegacyPlaintextJson()
        {
            var userId = NextUserId();
            var payload = new StoredToken
            {
                AccessToken = "legacy-access-token",
                RefreshToken = "legacy-refresh-token"
            };
            await using (var db = _fixture.DbService.GetDbContext())
            {
                db.YoutubeMemberAccessToken.Add(new YoutubeMemberAccessToken
                {
                    DiscordUserId = userId,
                    EncryptedAccessToken = JsonConvert.SerializeObject(payload)
                });
                await db.SaveChangesAsync();
            }

            var store = new MySqlDataStore(_fixture.DbService);
            var result = await store.GetAsync<StoredToken>(userId.ToString());

            Assert.Equal(payload.AccessToken, result.AccessToken);
            Assert.Equal(payload.RefreshToken, result.RefreshToken);
        }

        [MySqlComponentFact]
        public async Task ConcurrentFirstStoresForSameUserAreIdempotent()
        {
            var key = NextUserId().ToString();
            var payloads = Enumerable.Range(0, 16)
                .Select(i => new StoredToken
                {
                    AccessToken = $"concurrent-access-{i}",
                    RefreshToken = $"concurrent-refresh-{i}"
                })
                .ToArray();
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var stores = payloads.Select(async payload =>
            {
                await start.Task;
                await new MySqlDataStore(_fixture.DbService).StoreAsync(key, payload);
            }).ToArray();

            start.SetResult();
            await Task.WhenAll(stores);

            await using var db = _fixture.DbService.GetDbContext();
            var rows = await db.YoutubeMemberAccessToken.AsNoTracking()
                .Where(x => x.DiscordUserId == ulong.Parse(key))
                .ToArrayAsync();
            var restored = await new MySqlDataStore(_fixture.DbService).GetAsync<StoredToken>(key);

            Assert.Single(rows);
            Assert.Contains(payloads, payload =>
                payload.AccessToken == restored.AccessToken &&
                payload.RefreshToken == restored.RefreshToken);
        }

        private static ulong NextUserId()
            => (ulong)Random.Shared.NextInt64(1, long.MaxValue);

        private sealed class StoredToken
        {
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
        }
    }
}
