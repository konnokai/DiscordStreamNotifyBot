namespace DiscordStreamNotifyBot.SharedService.Google
{
    /// <summary>
    /// 用於跨 Bot 與 Backend 協調 Google OAuth token 異動的租約，並與 Backend 共用 Redis DB1 鍵名契約。
    /// </summary>
    public sealed class GoogleOAuthOperationLock
    {
        private readonly IDatabase _database;

        public GoogleOAuthOperationLock(IDatabase database)
        {
            _database = database;
        }

        public static GoogleOAuthOperationLock Create(IConnectionMultiplexer connection)
        {
            ArgumentNullException.ThrowIfNull(connection);
            return new GoogleOAuthOperationLock(
                connection.GetDatabase(DiscordStreamNotifyBot.Shared.RedisChannels.OAuth.DatabaseNumber));
        }

        public Task<OAuthLeaseAcquireResult> TryAcquireAsync(
            ulong discordUserId,
            CancellationToken cancellationToken = default)
            => OAuthLease.TryAcquireAsync(
                _database,
                DiscordStreamNotifyBot.Shared.RedisChannels.OAuth.GoogleOperationLock(discordUserId),
                cancellationToken);
    }
}
