namespace DiscordStreamNotifyBot.SharedService.Twitch
{
    /// <summary>以 Twitch user ID 為鍵、與 Backend 共用的 refresh lease（Redis DB1）。</summary>
    public sealed class TwitchOAuthRefreshLock
    {
        private readonly IDatabase _database;

        public TwitchOAuthRefreshLock(IDatabase database)
        {
            _database = database;
        }

        public static TwitchOAuthRefreshLock Create(IConnectionMultiplexer connection)
        {
            ArgumentNullException.ThrowIfNull(connection);
            return new TwitchOAuthRefreshLock(
                connection.GetDatabase(DiscordStreamNotifyBot.Shared.RedisChannels.OAuth.DatabaseNumber));
        }

        internal int DatabaseNumber => _database.Database;

        /// <summary>以固定 Redis DB 與 owner token 嘗試取得可續租的 Twitch refresh lease。</summary>
        public async Task<OAuthLeaseAcquireResult> TryAcquireAsync(
            string twitchUserId,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(twitchUserId);
            return await OAuthLease.TryAcquireAsync(
                _database,
                DiscordStreamNotifyBot.Shared.RedisChannels.OAuth.TwitchRefreshLock(twitchUserId),
                cancellationToken);
        }
    }
}
