using DiscordStreamNotifyBot.Auth;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;

namespace DiscordStreamNotifyBot
{
    /// <summary>
    /// 會限 OAuth token 的 MySQL 儲存後端（真實來源）。
    /// T 恆為 Google.Apis 的 TokenResponse、key 為 Discord userId 字串；密文格式與 <see cref="RedisDataStore"/> 相同，兩端可互相解密。
    /// </summary>
    public class MySqlDataStore : ITokenDataStore
    {
        private readonly MainDbService _dbService;
        private readonly string _key = Utility.RedisKey;

        public MySqlDataStore(MainDbService dbService)
        {
            _dbService = dbService;
        }

        public Task ClearAsync()
        {
            throw new NotImplementedException();
        }

        public async Task StoreAsync<T>(string key, T value)
        {
            var userId = ulong.Parse(key);
            var encValue = TokenManager.CreateToken(value, _key);
            var dateAdded = DateTime.Now;

            using var db = _dbService.GetDbContext();
            // 同一使用者可能由多個 shard 同時刷新 token，使用單一 upsert 避免 read-then-insert 競爭。
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO `youtube_member_access_token`
                    (`discord_user_id`, `encrypted_access_token`, `date_added`)
                VALUES ({userId}, {encValue}, {dateAdded})
                ON DUPLICATE KEY UPDATE
                    `encrypted_access_token` = {encValue},
                    `date_added` = {dateAdded};
                """);
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var userId = ulong.Parse(key);

            using var db = _dbService.GetDbContext();
            var str = await db.YoutubeMemberAccessToken.AsNoTracking()
                .Where((x) => x.DiscordUserId == userId)
                .Select((x) => x.EncryptedAccessToken)
                .FirstOrDefaultAsync();

            if (str == null)
                return default(T);

            try
            {
                return TokenManager.GetTokenResponseValue<T>(str, _key);
            }
            catch (Exception ex)
            {
                Log.Warn($"MySqlDataStore-GetAsync ({key}): 解密失敗，也許還沒加密? {ex}");

                try
                {
                    return JsonConvert.DeserializeObject<T>(str);
                }
                catch (Exception ex2)
                {
                    Log.Error($"MySqlDataStore-GetAsync ({key}): JsonDes失敗 {ex2}");
                    return default(T);
                }
            }
        }

        public async Task DeleteAsync<T>(string key)
        {
            var userId = ulong.Parse(key);

            using var db = _dbService.GetDbContext();
            var entity = await db.YoutubeMemberAccessToken.SingleOrDefaultAsync((x) => x.DiscordUserId == userId);
            if (entity != null)
            {
                db.YoutubeMemberAccessToken.Remove(entity);
                await db.SaveChangesAsync();
            }
        }

        public async Task<bool> IsExistUserTokenAsync<T>(string key)
        {
            var userId = ulong.Parse(key);

            using var db = _dbService.GetDbContext();
            return await db.YoutubeMemberAccessToken.AsNoTracking().AnyAsync((x) => x.DiscordUserId == userId);
        }
    }
}
