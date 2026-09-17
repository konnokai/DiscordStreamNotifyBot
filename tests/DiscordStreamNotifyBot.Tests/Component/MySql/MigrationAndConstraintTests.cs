using DiscordStreamNotifyBot.DataBase.Table;
using Microsoft.EntityFrameworkCore;

namespace DiscordStreamNotifyBot.Tests.Component.MySql
{
    [Collection(MySqlComponentCollection.Name)]
    [Trait("Category", "MySqlComponent")]
    public sealed class MigrationAndConstraintTests
    {
        private readonly MySqlComponentFixture _fixture;

        public MigrationAndConstraintTests(MySqlComponentFixture fixture)
        {
            _fixture = fixture;
        }

        [MySqlComponentFact]
        public async Task FullMigrationSetIsAppliedAndModelHasNoPendingChanges()
        {
            await using var db = _fixture.DbService.GetDbContext();

            var migrations = db.Database.GetMigrations().ToArray();
            var appliedMigrations = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
            var pendingMigrations = (await db.Database.GetPendingMigrationsAsync()).ToArray();

            Assert.NotEmpty(migrations);
            Assert.Equal(migrations, appliedMigrations);
            Assert.Empty(pendingMigrations);
            Assert.False(db.Database.HasPendingModelChanges());
        }

        [MySqlComponentFact]
        public async Task TwitchAuthorizationRejectsDuplicateDiscordUserId()
        {
            var discordUserId = NextUserId();
            await using (var db = _fixture.DbService.GetDbContext())
            {
                db.TwitchBroadcasterAuthorization.Add(CreateAuthorization("twitch-a", discordUserId));
                await db.SaveChangesAsync();
            }

            await using var duplicateDb = _fixture.DbService.GetDbContext();
            duplicateDb.TwitchBroadcasterAuthorization.Add(CreateAuthorization("twitch-b", discordUserId));

            await Assert.ThrowsAsync<DbUpdateException>(() => duplicateDb.SaveChangesAsync());
        }

        private static TwitchBroadcasterAuthorization CreateAuthorization(string suffix, ulong discordUserId)
        {
            var now = DateTime.UtcNow;
            return new TwitchBroadcasterAuthorization
            {
                TwitchUserId = $"{suffix}-{Guid.NewGuid():N}",
                DiscordUserId = discordUserId,
                ClientId = "component-client",
                UserLogin = "component-user",
                DisplayName = "Component User",
                ProfileImageUrl = "https://example.invalid/profile.png",
                EncryptedAccessToken = "encrypted-token",
                Scopes = "user:read:email",
                AuthorizedAt = now,
                DateUpdated = now
            };
        }

        private static ulong NextUserId()
            => (ulong)Random.Shared.NextInt64(1, long.MaxValue);
    }
}
