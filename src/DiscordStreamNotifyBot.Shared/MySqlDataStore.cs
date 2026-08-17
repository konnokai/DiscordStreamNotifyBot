using DiscordStreamNotifyBot.Auth;
using DiscordStreamNotifyBot.DataBase;

namespace DiscordStreamNotifyBot
{
    /// <summary>
    /// 會限 OAuth token 的 MySQL 儲存後端（真實來源）。
    /// T 恆為 Google.Apis 的 TokenResponse、key 為 Discord userId 字串。
    /// </summary>
    public class MySqlDataStore : ITokenDataStore
    {
        private readonly MainDbService _dbService;
        private readonly string _key;

        public MySqlDataStore(MainDbService dbService, string providerTokenEncryptionKey)
        {
            _dbService = dbService;
            if (string.IsNullOrWhiteSpace(providerTokenEncryptionKey) || providerTokenEncryptionKey.Length < 64)
                throw new ArgumentException("Provider token 加密金鑰不得為空，且長度至少為 64 字元", nameof(providerTokenEncryptionKey));
            _key = providerTokenEncryptionKey;
        }

        public Task ClearAsync()
        {
            throw new NotImplementedException();
        }

        public async Task StoreAsync<T>(string key, T value)
        {
            var userId = ulong.Parse(key);
            var encValue = TokenManager.CreateToken(value, _key);
            var dateAdded = DateTime.UtcNow;

            using var db = _dbService.GetDbContext();
            // 同一使用者的 token 可能由多個 shard 同時更新，使用單一 upsert 避免讀取後插入的競爭條件。
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
                Log.Warn($"MySqlDataStore-GetAsync ({key}): 解密失敗，資料可能尚未加密。 {ex}");

                try
                {
                    return JsonConvert.DeserializeObject<T>(str);
                }
                catch (Exception ex2)
                {
                    Log.Error($"MySqlDataStore-GetAsync ({key}): JSON 反序列化失敗 {ex2}");
                    return default(T);
                }
            }
        }

        public async Task<bool> HasUnlinkIntentAsync(ulong discordUserId, CancellationToken cancellationToken)
        {
            using var db = _dbService.GetDbContext();
            return await db.GoogleOAuthUnlinkIntent.AsNoTracking()
                .AnyAsync(x => x.DiscordUserId == discordUserId, cancellationToken);
        }

        public async Task<bool> StoreRefreshIfCurrentAsync<T>(
            ulong discordUserId,
            string expectedEncryptedToken,
            T value,
            CancellationToken cancellationToken)
        {
            var encryptedValue = TokenManager.CreateToken(value, _key);
            var dateAdded = DateTime.UtcNow;
            using var db = _dbService.GetDbContext();
            var updated = await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE `youtube_member_access_token`
                SET `encrypted_access_token` = {encryptedValue},
                    `date_added` = {dateAdded}
                WHERE `discord_user_id` = {discordUserId}
                  AND BINARY `encrypted_access_token` = BINARY {expectedEncryptedToken}
                  AND NOT EXISTS (
                      SELECT 1 FROM `google_oauth_unlink_intent`
                      WHERE `discord_user_id` = {discordUserId});
                """, cancellationToken);
            return updated == 1;
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
