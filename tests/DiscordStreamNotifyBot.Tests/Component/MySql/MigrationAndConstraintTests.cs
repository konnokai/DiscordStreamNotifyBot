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

        [MySqlComponentFact]
        public async Task TwitchSubscriptionTablesEnforceUniqueKeysAndTierConstraint()
        {
            ulong guildId = NextUserId();
            ulong discordUserId = NextUserId();
            await using (var db = _fixture.DbService.GetDbContext())
            {
                db.GuildTwitchSubscriptionConfig.Add(CreateSubscriptionConfig(guildId, "broadcaster-1"));
                db.TwitchSubscriptionCheck.Add(CreateSubscriptionCheck(guildId, discordUserId, "broadcaster-1", "1000"));
                await db.SaveChangesAsync();
            }

            await using (var duplicateConfigDb = _fixture.DbService.GetDbContext())
            {
                duplicateConfigDb.GuildTwitchSubscriptionConfig.Add(CreateSubscriptionConfig(guildId, "broadcaster-1"));
                await Assert.ThrowsAsync<DbUpdateException>(() => duplicateConfigDb.SaveChangesAsync());
            }

            await using (var duplicateCheckDb = _fixture.DbService.GetDbContext())
            {
                duplicateCheckDb.TwitchSubscriptionCheck.Add(CreateSubscriptionCheck(guildId, discordUserId, "broadcaster-1", "2000"));
                await Assert.ThrowsAsync<DbUpdateException>(() => duplicateCheckDb.SaveChangesAsync());
            }

            await using var invalidTierDb = _fixture.DbService.GetDbContext();
            invalidTierDb.TwitchSubscriptionCheck.Add(CreateSubscriptionCheck(guildId, NextUserId(), "broadcaster-1", "4000"));
            await Assert.ThrowsAsync<DbUpdateException>(() => invalidTierDb.SaveChangesAsync());
        }

        [MySqlComponentFact]
        public async Task TwitchSubscriptionConfigurationHasDurableDeletionPendingColumn()
        {
            await using var db = _fixture.DbService.GetDbContext();

            int matchingColumns = await db.Database.SqlQueryRaw<int>(
                """
                SELECT COUNT(*) AS `Value`
                FROM information_schema.columns
                WHERE table_schema = DATABASE()
                  AND table_name = 'guild_twitch_subscription_config'
                  AND column_name = 'deletion_pending'
                  AND is_nullable = 'NO'
                  AND data_type = 'tinyint'
                  AND column_default = '0'
                """).SingleAsync();

            Assert.Equal(1, matchingColumns);
        }

        [MySqlComponentFact]
        public async Task YoutubeMembershipTablesPersistDurableStateAndRejectDuplicateNaturalKeys()
        {
            ulong guildId = NextUserId();
            ulong userId = NextUserId();
            string channelId = $"UC{Guid.NewGuid():N}"[..24];

            await using (var db = _fixture.DbService.GetDbContext())
            {
                db.GuildYoutubeMemberConfig.Add(new GuildYoutubeMemberConfig
                {
                    GuildId = guildId,
                    MemberCheckChannelId = channelId,
                    MemberCheckChannelTitle = "Component Channel",
                    MemberCheckVideoId = "abcdefghijk",
                    MemberCheckGrantRoleId = 100,
                    PreviousMemberCheckGrantRoleId = 99,
                    DeletionPending = true
                });
                db.YoutubeMemberCheck.Add(new YoutubeMemberCheck
                {
                    GuildId = guildId,
                    UserId = userId,
                    CheckYTChannelId = channelId,
                    Locale = "zh-TW",
                    IsChecked = false,
                    PendingRoleRemoval = true,
                    LastCheckTime = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }

            await using (var readDb = _fixture.DbService.GetDbContext())
            {
                var config = await readDb.GuildYoutubeMemberConfig.AsNoTracking()
                    .SingleAsync(x => x.GuildId == guildId && x.MemberCheckChannelId == channelId);
                var check = await readDb.YoutubeMemberCheck.AsNoTracking()
                    .SingleAsync(x => x.GuildId == guildId && x.UserId == userId);

                Assert.Equal((ulong)99, config.PreviousMemberCheckGrantRoleId);
                Assert.True(config.DeletionPending);
                Assert.False(check.IsChecked);
                Assert.True(check.PendingRoleRemoval);
            }

            await using (var schemaDb = _fixture.DbService.GetDbContext())
            {
                int requiredNaturalKeys = await schemaDb.Database.SqlQueryRaw<int>(
                    """
                    SELECT COUNT(*) AS `Value`
                    FROM information_schema.columns
                    WHERE table_schema = DATABASE()
                      AND data_type = 'longtext'
                      AND is_nullable = 'NO'
                      AND ((table_name = 'guild_youtube_member_config' AND column_name = 'member_check_channel_id')
                        OR (table_name = 'youtube_member_check' AND column_name = 'check_yt_channel_id'))
                    """).SingleAsync();
                Assert.Equal(2, requiredNaturalKeys);
            }

            await using (var duplicateConfigDb = _fixture.DbService.GetDbContext())
            {
                duplicateConfigDb.GuildYoutubeMemberConfig.Add(new GuildYoutubeMemberConfig
                {
                    GuildId = guildId,
                    MemberCheckChannelId = channelId,
                    MemberCheckChannelTitle = "Duplicate",
                    MemberCheckGrantRoleId = 101
                });
                await Assert.ThrowsAsync<DbUpdateException>(() => duplicateConfigDb.SaveChangesAsync());
            }

            await using var duplicateCheckDb = _fixture.DbService.GetDbContext();
            duplicateCheckDb.YoutubeMemberCheck.Add(new YoutubeMemberCheck
            {
                GuildId = guildId,
                UserId = userId,
                CheckYTChannelId = channelId,
                Locale = "zh-TW"
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => duplicateCheckDb.SaveChangesAsync());
        }

        [MySqlComponentFact]
        public async Task YoutubeMembershipPreflightSqlExecutesAgainstCurrentSchema()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "YOUTUBE_MEMBER_VERIFICATION_PREFLIGHT.sql");
            string sql = await File.ReadAllTextAsync(path);
            string executableSql = string.Join('\n', sql.Split('\n')
                .Where(line => !line.TrimStart().StartsWith("--", StringComparison.Ordinal)));

            await using var db = _fixture.DbService.GetDbContext();
            foreach (string statement in executableSql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                await db.Database.ExecuteSqlRawAsync(statement);
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

        private static GuildTwitchSubscriptionConfig CreateSubscriptionConfig(ulong guildId, string broadcasterId)
            => new()
            {
                GuildId = guildId,
                BroadcasterId = broadcasterId,
                BroadcasterLogin = "component_channel",
                BroadcasterDisplayName = "Component Channel",
                SubscriberRoleId = 100,
                Tier1RoleId = 101,
                Tier2RoleId = 102,
                Tier3RoleId = 103,
                DateAdded = DateTime.UtcNow
            };

        private static TwitchSubscriptionCheck CreateSubscriptionCheck(
            ulong guildId,
            ulong discordUserId,
            string broadcasterId,
            string tier)
            => new()
            {
                GuildId = guildId,
                DiscordUserId = discordUserId,
                BroadcasterId = broadcasterId,
                Locale = "zh-TW",
                IsChecked = true,
                Tier = tier,
                LastCheckTime = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow
            };

        private static ulong NextUserId()
            => (ulong)Random.Shared.NextInt64(1, long.MaxValue);
    }

    public sealed class YoutubeMembershipSchemaContractTests
    {
        [Fact]
        public void EntitiesExposeDurableTransitionState()
        {
            Assert.Equal(typeof(ulong?), typeof(GuildYoutubeMemberConfig)
                .GetProperty("PreviousMemberCheckGrantRoleId")?.PropertyType);
            Assert.Equal(typeof(bool), typeof(GuildYoutubeMemberConfig)
                .GetProperty("DeletionPending")?.PropertyType);
            Assert.Equal(typeof(bool), typeof(YoutubeMemberCheck)
                .GetProperty("PendingRoleRemoval")?.PropertyType);
            Assert.Equal(typeof(string), typeof(GoogleOAuthUnlinkIntent)
                .GetProperty("ExpectedEncryptedToken")?.PropertyType);
        }
    }
}
