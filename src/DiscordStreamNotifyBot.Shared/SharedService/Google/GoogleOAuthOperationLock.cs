namespace DiscordStreamNotifyBot.SharedService.Google
{
    public enum GoogleOAuthOperationLockAcquireStatus
    {
        Acquired,
        Contended,
        TemporaryFailure
    }

    public enum GoogleOAuthOperationLockOwnershipStatus
    {
        Owned,
        OwnershipLost,
        TemporaryFailure
    }

    public readonly record struct GoogleOAuthOperationLockAcquireResult(
        GoogleOAuthOperationLockAcquireStatus Status,
        GoogleOAuthOperationLockLease Lease,
        Exception Exception);

    /// <summary>
    /// Google OAuth token mutation 的跨 Bot／Backend lease，與 Backend 共用 Redis DB1 key contract。
    /// </summary>
    public sealed class GoogleOAuthOperationLock
    {
        internal static readonly TimeSpan DefaultTtl =
            DiscordStreamNotifyBot.SharedService.Twitch.TwitchOAuthRefreshLock.DefaultTtl;
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

        internal int DatabaseNumber => _database.Database;

        public async Task<GoogleOAuthOperationLockAcquireResult> TryAcquireAsync(
            ulong discordUserId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RedisKey key = DiscordStreamNotifyBot.Shared.RedisChannels.OAuth.GoogleOperationLock(discordUserId);
            RedisValue owner = $"bot:{Environment.ProcessId}:{Guid.NewGuid():N}";

            try
            {
                if (await _database.StringSetAsync(key, owner, DefaultTtl, When.NotExists))
                {
                    return new GoogleOAuthOperationLockAcquireResult(
                        GoogleOAuthOperationLockAcquireStatus.Acquired,
                        new GoogleOAuthOperationLockLease(_database, key, owner, DefaultTtl),
                        null);
                }
                return new GoogleOAuthOperationLockAcquireResult(
                    GoogleOAuthOperationLockAcquireStatus.Contended, null, null);
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException or InvalidOperationException)
            {
                return new GoogleOAuthOperationLockAcquireResult(
                    GoogleOAuthOperationLockAcquireStatus.TemporaryFailure, null, ex);
            }
        }
    }

    public sealed class GoogleOAuthOperationLockLease : IAsyncDisposable
    {
        private const string RenewScript = "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('pexpire', KEYS[1], ARGV[2]) else return 0 end";
        private const string ReleaseScript = "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";
        private readonly IDatabase _database;
        private readonly RedisKey _key;
        private readonly RedisValue _owner;
        private readonly TimeSpan _ttl;
        private readonly CancellationTokenSource _renewalCancellation = new();
        private readonly Task _renewalTask;
        private int _ownershipLost;
        private int _releaseStarted;

        public GoogleOAuthOperationLockLease(
            IDatabase database,
            RedisKey key,
            RedisValue owner,
            TimeSpan ttl)
        {
            _database = database;
            _key = key;
            _owner = owner;
            _ttl = ttl;
            _renewalTask = RenewUntilReleasedAsync(_renewalCancellation.Token);
        }

        public async Task<GoogleOAuthOperationLockOwnershipStatus> EnsureOwnedAsync(
            CancellationToken cancellationToken = default)
        {
            if (Volatile.Read(ref _ownershipLost) != 0)
                return GoogleOAuthOperationLockOwnershipStatus.OwnershipLost;

            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                RedisResult result = await _database.ScriptEvaluateAsync(
                    RenewScript,
                    [_key],
                    [_owner, (long)_ttl.TotalMilliseconds]);
                cancellationToken.ThrowIfCancellationRequested();
                if ((long)result == 1)
                    return GoogleOAuthOperationLockOwnershipStatus.Owned;

                Interlocked.Exchange(ref _ownershipLost, 1);
                return GoogleOAuthOperationLockOwnershipStatus.OwnershipLost;
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException or InvalidOperationException)
            {
                return GoogleOAuthOperationLockOwnershipStatus.TemporaryFailure;
            }
        }

        private async Task RenewUntilReleasedAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_ttl.TotalMilliseconds / 3));
            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    if (await EnsureOwnedAsync(cancellationToken) == GoogleOAuthOperationLockOwnershipStatus.OwnershipLost)
                        return;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _releaseStarted, 1) != 0)
                return;

            await _renewalCancellation.CancelAsync();
            try { await _renewalTask; } catch (Exception) { }
            try
            {
                await _database.ScriptEvaluateAsync(ReleaseScript, [_key], [_owner]);
            }
            catch (Exception)
            {
                // 由 TTL 收斂；不可無條件刪除可能已被其他程序接手的 key。
            }
            try { _renewalCancellation.Dispose(); } catch (ObjectDisposedException) { }
        }
    }
}
