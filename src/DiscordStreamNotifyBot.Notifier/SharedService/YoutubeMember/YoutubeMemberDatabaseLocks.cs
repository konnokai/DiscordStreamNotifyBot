using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;

namespace DiscordStreamNotifyBot.SharedService.YoutubeMember
{
    public partial class YoutubeMemberService
    {
        // EF LINQ 的一般讀取不會帶 MySQL row lock。provider 結果跨程序抵達時，必須在 transaction
        // 中鎖定同一組 token/check/config，否則 Scraper 或另一個 Notifier 可在驗證與套用間覆寫設定。
        private static Task<YoutubeMemberAccessToken> LockTokenAsync(
            MainDbContext db,
            ulong userId,
            CancellationToken cancellationToken)
            => db.YoutubeMemberAccessToken.FromSqlInterpolated($"""
                SELECT * FROM `youtube_member_access_token`
                WHERE `discord_user_id` = {userId}
                FOR UPDATE
                """).SingleOrDefaultAsync(cancellationToken);

        private static Task<YoutubeMemberCheck> LockCheckAsync(
            MainDbContext db,
            int checkId,
            CancellationToken cancellationToken)
            => db.YoutubeMemberCheck.FromSqlInterpolated($"""
                SELECT * FROM `youtube_member_check`
                WHERE `id` = {checkId}
                FOR UPDATE
                """).SingleOrDefaultAsync(cancellationToken);

        private static Task<GuildYoutubeMemberConfig> LockConfigurationAsync(
            MainDbContext db,
            int configurationId,
            CancellationToken cancellationToken)
            => db.GuildYoutubeMemberConfig.FromSqlInterpolated($"""
                SELECT * FROM `guild_youtube_member_config`
                WHERE `id` = {configurationId}
                FOR UPDATE
                """).SingleOrDefaultAsync(cancellationToken);

        private static Task<List<YoutubeMemberCheck>> LockUserChecksAsync(
            MainDbContext db,
            ulong userId,
            CancellationToken cancellationToken)
            => db.YoutubeMemberCheck.FromSqlInterpolated($"""
                SELECT * FROM `youtube_member_check`
                WHERE `user_id` = {userId}
                FOR UPDATE
                """).ToListAsync(cancellationToken);

        private static async Task LockGuildConfigurationsAsync(
            MainDbContext db,
            IEnumerable<ulong> guildIds,
            CancellationToken cancellationToken)
        {
            foreach (ulong guildId in guildIds.Distinct().Order())
            {
                _ = await db.GuildYoutubeMemberConfig.FromSqlInterpolated($"""
                    SELECT * FROM `guild_youtube_member_config`
                    WHERE `guild_id` = {guildId}
                    FOR UPDATE
                    """).ToListAsync(cancellationToken);
            }
        }

        private async Task<ulong[]> GetUserCheckGuildIdsAsync(ulong userId, CancellationToken cancellationToken)
        {
            using var db = _dbService.GetDbContext();
            return await db.YoutubeMemberCheck.AsNoTracking().Where(x => x.UserId == userId)
                .Select(x => x.GuildId).Distinct().OrderBy(x => x).ToArrayAsync(cancellationToken);
        }
    }
}
