using DiscordStreamNotifyBot.DataBase;

namespace DiscordStreamNotifyBot.SharedService
{
    /// <summary>
    /// 通知設定記憶體快取（計畫 §12.3）。
    /// <para>
    /// 廣播 + 各自過濾下，每則開台事件 × N shard 都會查一次通知設定表，對 MySQL 壓力大。
    /// 本快取持有整張通知設定表的快照，TTL 內重用、逾時自動重載；發送路徑變更設定（移除失效伺服器/權限）後呼叫
    /// <see cref="Invalidate"/> 立即失效。通知設定屬「每伺服器」，且設定指令必在該伺服器所屬 shard 執行，
    /// 故本機快取 + 本機失效即足夠（跨 shard 不需共享）；TTL 為安全網，最大過時 ≤ TTL。
    /// </para>
    /// </summary>
    internal sealed class NoticeCache<T>
    {
        private readonly Func<List<T>> _load;
        private readonly TimeProvider _timeProvider;
        private readonly TimeSpan _ttl;
        private readonly object _lock = new();
        private List<T> _snapshot;
        private DateTimeOffset _loadedAt = DateTimeOffset.MinValue;

        public NoticeCache(MainDbService dbService, Func<MainDbContext, List<T>> load, TimeSpan? ttl = null)
            : this(
                () =>
                {
                    using var db = dbService.GetDbContext();
                    return load(db);
                },
                TimeProvider.System,
                ttl)
        {
        }

        internal NoticeCache(Func<List<T>> load, TimeProvider timeProvider, TimeSpan? ttl = null)
        {
            _load = load;
            _timeProvider = timeProvider;
            _ttl = ttl ?? TimeSpan.FromSeconds(30);
        }

        /// <summary>取得通知設定快照（TTL 內重用，逾時重載）。回傳的清單請勿就地修改。</summary>
        public List<T> Get()
        {
            lock (_lock)
            {
                if (_snapshot == null || _timeProvider.GetUtcNow() - _loadedAt > _ttl)
                {
                    _snapshot = _load();
                    _loadedAt = _timeProvider.GetUtcNow();
                }
                return _snapshot;
            }
        }

        /// <summary>使快取立即失效（下次 <see cref="Get"/> 會重載），於本機變更設定後呼叫。</summary>
        public void Invalidate()
        {
            lock (_lock)
            {
                _loadedAt = DateTimeOffset.MinValue;
            }
        }
    }
}
