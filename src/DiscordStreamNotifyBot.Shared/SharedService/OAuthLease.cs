namespace DiscordStreamNotifyBot.SharedService
{
    public enum OAuthLeaseAcquireStatus
    {
        Acquired,
        Contended,
        TemporaryFailure
    }

    public enum OAuthLeaseOwnershipStatus
    {
        Owned,
        OwnershipLost,
        TemporaryFailure
    }

    public enum OAuthLeaseReleaseStatus
    {
        Released,
        OwnershipLost,
        TemporaryFailure
    }

    public readonly record struct OAuthLeaseAcquireResult(
        OAuthLeaseAcquireStatus Status,
        OAuthLease Lease,
        Exception Exception);

    /// <summary>
    /// Bot 與 Backend 共用的 provider OAuth 跨程序租約（Twitch refresh lock、Google operation lock）。
    /// 鍵名、owner 格式、TTL 與 Lua 皆為與 Backend 共用的 Redis 契約，不可單方面修改。
    /// </summary>
    public sealed class OAuthLease : IAsyncDisposable
    {
        internal static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(10);

        // TTL 到期後可能已有新 owner 接手；續租與釋放都必須在 Redis 內原子比對 owner。
        // 延遲的舊 lease 不得延長或刪除新 owner 的 lock。
        internal const string RenewScript = "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('pexpire', KEYS[1], ARGV[2]) else return 0 end";
        internal const string ReleaseScript = "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";
        private readonly IDatabase _database;
        private readonly RedisKey _key;
        private readonly RedisValue _owner;
        private readonly TimeSpan _ttl;
        private readonly CancellationTokenSource _renewalCancellation = new();
        private readonly Task _renewalTask;
        private int _ownershipLost;
        private int _releaseStarted;

        private OAuthLease(
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

        /// <summary>以 SET NX 與帶 Bot 前綴的 owner token 嘗試取得可續租的 lease。</summary>
        internal static async Task<OAuthLeaseAcquireResult> TryAcquireAsync(
            IDatabase database,
            RedisKey key,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RedisValue owner = $"bot:{Environment.ProcessId}:{Guid.NewGuid():N}";

            try
            {
                return await database.StringSetAsync(key, owner, DefaultTtl, When.NotExists)
                    ? new OAuthLeaseAcquireResult(
                        OAuthLeaseAcquireStatus.Acquired,
                        new OAuthLease(database, key, owner, DefaultTtl),
                        null)
                    : new OAuthLeaseAcquireResult(OAuthLeaseAcquireStatus.Contended, null, null);
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException or InvalidOperationException)
            {
                return new OAuthLeaseAcquireResult(OAuthLeaseAcquireStatus.TemporaryFailure, null, ex);
            }
        }

        /// <summary>原子確認 owner 並延長 TTL；若 lease 已被接手則永久標記 ownership lost。</summary>
        public async Task<(OAuthLeaseOwnershipStatus Status, Exception Exception)> EnsureOwnedAsync(
            CancellationToken cancellationToken = default)
        {
            if (Volatile.Read(ref _ownershipLost) != 0)
                return (OAuthLeaseOwnershipStatus.OwnershipLost, null);

            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                RedisResult result = await _database.ScriptEvaluateAsync(
                    RenewScript,
                    [_key],
                    [_owner, (long)_ttl.TotalMilliseconds]);
                cancellationToken.ThrowIfCancellationRequested();
                if ((long)result == 1)
                    return (OAuthLeaseOwnershipStatus.Owned, null);

                Interlocked.Exchange(ref _ownershipLost, 1);
                return (OAuthLeaseOwnershipStatus.OwnershipLost, null);
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException or InvalidOperationException)
            {
                return (OAuthLeaseOwnershipStatus.TemporaryFailure, ex);
            }
        }

        /// <summary>停止續租並僅在 owner 相符時刪除 Redis lock，避免舊 lease 刪除新 owner。</summary>
        public async Task<(OAuthLeaseReleaseStatus Status, Exception Exception)> ReleaseAsync(
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _releaseStarted, 1) != 0)
                return (OAuthLeaseReleaseStatus.Released, null);

            await StopRenewalAsync();
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                RedisResult result = await _database.ScriptEvaluateAsync(ReleaseScript, [_key], [_owner]);
                return ((long)result == 1
                    ? OAuthLeaseReleaseStatus.Released
                    : OAuthLeaseReleaseStatus.OwnershipLost, null);
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException or InvalidOperationException)
            {
                return (OAuthLeaseReleaseStatus.TemporaryFailure, ex);
            }
        }

        /// <summary>供 <c>await using</c> 使用：與 <see cref="ReleaseAsync"/> 相同的 owner 比對釋放，但吞下所有錯誤。</summary>
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
                // 交由 TTL 到期後自動清理；不可無條件刪除可能已被其他程序接手的 key。
            }
            try { _renewalCancellation.Dispose(); } catch (ObjectDisposedException) { }
        }

        private async Task RenewUntilReleasedAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_ttl.TotalMilliseconds / 3));
            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    var result = await EnsureOwnedAsync(cancellationToken);
                    if (result.Status == OAuthLeaseOwnershipStatus.OwnershipLost)
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
