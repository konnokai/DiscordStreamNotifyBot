using System.Collections.Concurrent;

namespace DiscordStreamNotifyBot.SharedService.Member
{
    public sealed class MemberOperationCoordinator
    {
        // 僅序列化同一個 Notifier instance 的 Discord/DB 操作；跨程序 token refresh 另由 Redis lease 保護。
        // 同時需要兩把鎖時固定先 User、再 Guild，避免刪除、驗證與回補互相反向等待。
        private readonly ConcurrentDictionary<ulong, SemaphoreSlim> _userLocks = new();
        private readonly ConcurrentDictionary<ulong, SemaphoreSlim> _guildLocks = new();

        /// <summary>序列化同一使用者在此 Notifier 內的驗證、清理與角色回補。</summary>
        public Task<Lease> LockUserAsync(ulong discordUserId, CancellationToken cancellationToken)
            => LockAsync(_userLocks.GetOrAdd(discordUserId, _ => new SemaphoreSlim(1, 1)), cancellationToken);

        /// <summary>序列化同一 guild 在此 Notifier 內的設定、刪除與角色對帳。</summary>
        public Task<Lease> LockGuildAsync(ulong guildId, CancellationToken cancellationToken)
            => LockAsync(_guildLocks.GetOrAdd(guildId, _ => new SemaphoreSlim(1, 1)), cancellationToken);

        /// <summary>
        /// 同一使用者需要跨多個 guild 更新 durable state 時，固定以 guild id 排序取得所有鎖。
        /// 這讓 user→guild 鎖序不因資料庫回傳順序而產生反向等待。
        /// </summary>
        public async Task<LeaseGroup> LockGuildsAsync(IEnumerable<ulong> guildIds, CancellationToken cancellationToken)
        {
            var leases = new List<Lease>();
            try
            {
                foreach (ulong guildId in guildIds.Distinct().Order())
                    leases.Add(await LockGuildAsync(guildId, cancellationToken));
                return new LeaseGroup(leases);
            }
            catch
            {
                foreach (Lease lease in leases.AsEnumerable().Reverse())
                    lease.Dispose();
                throw;
            }
        }

        private static async Task<Lease> LockAsync(SemaphoreSlim semaphore, CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken);
            return new Lease(semaphore);
        }

        public sealed class Lease : IDisposable, IAsyncDisposable
        {
            private SemaphoreSlim _semaphore;

            internal Lease(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref _semaphore, null)?.Release();
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }

        public sealed class LeaseGroup : IDisposable, IAsyncDisposable
        {
            private List<Lease> _leases;

            internal LeaseGroup(List<Lease> leases)
            {
                _leases = leases;
            }

            public void Dispose()
            {
                List<Lease> leases = Interlocked.Exchange(ref _leases, null);
                if (leases == null)
                    return;
                foreach (Lease lease in leases.AsEnumerable().Reverse())
                    lease.Dispose();
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
