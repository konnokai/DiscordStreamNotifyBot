using DiscordStreamNotifyBot.DataBase.Table;
using Microsoft.EntityFrameworkCore;

namespace DiscordStreamNotifyBot.Tests.Component.MySql
{
    [Collection(MySqlComponentCollection.Name)]
    [Trait("Category", "MySqlComponent")]
    public sealed class YoutubeMemberTokenCleanupConcurrencyTests
    {
        private readonly MySqlComponentFixture _fixture;

        public YoutubeMemberTokenCleanupConcurrencyTests(MySqlComponentFixture fixture)
        {
            _fixture = fixture;
        }

        [MySqlComponentFact]
        public async Task ConditionalTokenDeleteIsByteExactAndWaitsForTheLockedSnapshot()
        {
            var userId = (ulong)Random.Shared.NextInt64(1, long.MaxValue);
            const string payload = "Case-Sensitive-Encrypted-Payload";
            await using (var seedDb = _fixture.DbService.GetDbContext())
            {
                seedDb.YoutubeMemberAccessToken.Add(new YoutubeMemberAccessToken
                {
                    DiscordUserId = userId,
                    EncryptedAccessToken = payload
                });
                await seedDb.SaveChangesAsync();
            }
            Assert.Equal(0, await DeleteIfCurrentAsync(userId, payload.ToLowerInvariant()));

            await using var lockDb = _fixture.DbService.GetDbContext();
            await using var transaction = await lockDb.Database.BeginTransactionAsync();
            var locked = await lockDb.YoutubeMemberAccessToken.FromSqlInterpolated($"""
                SELECT * FROM `youtube_member_access_token`
                WHERE `discord_user_id` = {userId}
                FOR UPDATE
                """).SingleAsync();
            Assert.Equal(payload, locked.EncryptedAccessToken);

            Task<int> conditionalDelete = DeleteIfCurrentAsync(userId, payload);
            await Task.Delay(TimeSpan.FromMilliseconds(150));
            Assert.False(conditionalDelete.IsCompleted);

            await transaction.CommitAsync();
            Assert.Equal(1, await conditionalDelete);
        }

        private async Task<int> DeleteIfCurrentAsync(ulong userId, string expectedPayload)
        {
            await using var db = _fixture.DbService.GetDbContext();
            return await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM `youtube_member_access_token`
                WHERE `discord_user_id` = {userId}
                  AND BINARY `encrypted_access_token` = BINARY {expectedPayload}
                """);
        }
    }
}
