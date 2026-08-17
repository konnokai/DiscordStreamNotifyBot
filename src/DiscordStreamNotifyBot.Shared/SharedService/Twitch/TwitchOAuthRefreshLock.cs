namespace DiscordStreamNotifyBot.SharedService.Twitch
{
    public enum TwitchOAuthRefreshLockAcquireStatus
    {
        Acquired,
        Contended,
        TemporaryFailure
    }

    public enum TwitchOAuthRefreshLockReleaseStatus
    {
        Released,
        OwnershipLost,
        TemporaryFailure
    }

    public enum TwitchOAuthRefreshLockOwnershipStatus
    {
        Owned,
        OwnershipLost,
        TemporaryFailure
    }

    public sealed class TwitchOAuthRefreshLockAcquireResult
    {
        public TwitchOAuthRefreshLockAcquireStatus Status { get; private init; }
        public TwitchOAuthRefreshLockLease Lease { get; private init; }
        public Exception Exception { get; private init; }

        public static TwitchOAuthRefreshLockAcquireResult Acquired(TwitchOAuthRefreshLockLease lease)
            => new() { Status = TwitchOAuthRefreshLockAcquireStatus.Acquired, Lease = lease };

        public static TwitchOAuthRefreshLockAcquireResult Contended()
            => new() { Status = TwitchOAuthRefreshLockAcquireStatus.Contended };

        public static TwitchOAuthRefreshLockAcquireResult TemporaryFailure(Exception exception)
            => new() { Status = TwitchOAuthRefreshLockAcquireStatus.TemporaryFailure, Exception = exception };
    }

    public sealed class TwitchOAuthRefreshLock
    {
        internal static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(10);
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
        public async Task<TwitchOAuthRefreshLockAcquireResult> TryAcquireAsync(
            string twitchUserId,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(twitchUserId);
            cancellationToken.ThrowIfCancellationRequested();
            RedisKey key = DiscordStreamNotifyBot.Shared.RedisChannels.OAuth.TwitchRefreshLock(twitchUserId);
            RedisValue owner = $"bot:{Environment.ProcessId}:{Guid.NewGuid():N}";

            try
            {
                return await _database.StringSetAsync(key, owner, DefaultTtl, When.NotExists)
                    ? TwitchOAuthRefreshLockAcquireResult.Acquired(
                        new TwitchOAuthRefreshLockLease(_database, key, owner, DefaultTtl))
                    : TwitchOAuthRefreshLockAcquireResult.Contended();
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException or InvalidOperationException)
            {
                return TwitchOAuthRefreshLockAcquireResult.TemporaryFailure(ex);
            }
        }
    }

    public sealed class TwitchOAuthRefreshLockLease
    {
        // TTL 到期後可能已有新 owner 接手；續租與釋放都必須在 Redis 內原子比對 owner。
        // 延遲的舊 lease 不得延長或刪除新 owner 的 lock。
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

        public TwitchOAuthRefreshLockLease(
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

        /// <summary>原子確認 owner 並延長 TTL；若 lease 已被接手則永久標記 ownership lost。</summary>
        public async Task<(TwitchOAuthRefreshLockOwnershipStatus Status, Exception Exception)> EnsureOwnedAsync(
            CancellationToken cancellationToken = default)
        {
            if (Volatile.Read(ref _ownershipLost) != 0)
                return (TwitchOAuthRefreshLockOwnershipStatus.OwnershipLost, null);

            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                RedisResult result = await _database.ScriptEvaluateAsync(
                    RenewScript,
                    [_key],
                    [_owner, (long)_ttl.TotalMilliseconds]);
                cancellationToken.ThrowIfCancellationRequested();
                if ((long)result == 1)
                    return (TwitchOAuthRefreshLockOwnershipStatus.Owned, null);

                Interlocked.Exchange(ref _ownershipLost, 1);
                return (TwitchOAuthRefreshLockOwnershipStatus.OwnershipLost, null);
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException or InvalidOperationException)
            {
                return (TwitchOAuthRefreshLockOwnershipStatus.TemporaryFailure, ex);
            }
        }

        /// <summary>停止續租並僅在 owner 相符時刪除 Redis lock，避免舊 lease 刪除新 owner。</summary>
        public async Task<(TwitchOAuthRefreshLockReleaseStatus Status, Exception Exception)> ReleaseAsync(
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _releaseStarted, 1) != 0)
                return (TwitchOAuthRefreshLockReleaseStatus.Released, null);

            await StopRenewalAsync();
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                RedisResult result = await _database.ScriptEvaluateAsync(ReleaseScript, [_key], [_owner]);
                return ((long)result == 1
                    ? TwitchOAuthRefreshLockReleaseStatus.Released
                    : TwitchOAuthRefreshLockReleaseStatus.OwnershipLost, null);
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException or InvalidOperationException)
            {
                return (TwitchOAuthRefreshLockReleaseStatus.TemporaryFailure, ex);
            }
        }

        /// <summary>停止續租但保留 lock，交由 TTL 清除；用於 refresh token 已輪替但尚未安全保存時。</summary>
        public async Task AbandonAsync()
        {
            if (Interlocked.Exchange(ref _releaseStarted, 1) != 0)
                return;
            await StopRenewalAsync();
        }

        private async Task RenewUntilReleasedAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_ttl.TotalMilliseconds / 3));
            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    var result = await EnsureOwnedAsync(cancellationToken);
                    if (result.Status == TwitchOAuthRefreshLockOwnershipStatus.OwnershipLost)
                        return;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
        }

        private async Task StopRenewalAsync()
        {
            if (!_renewalCancellation.IsCancellationRequested)
                await _renewalCancellation.CancelAsync();
            await _renewalTask;
            _renewalCancellation.Dispose();
        }
    }
}
