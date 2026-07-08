namespace DiscordStreamNotifyBot.Shared
{
    /// <summary>
    /// 叢集控制平面（Redis）封裝：心跳、scraper leader 鎖、shard 租約、TOTAL_SHARDS 公告（計畫 §4.2 / §5.2）。
    /// <para>所有鍵語義皆以 <c>SET NX EX</c> / TTL 為基礎；續租 / 釋放以 Lua 確保「只有持有者能操作」避免誤搶。</para>
    /// </summary>
    public class ClusterService
    {
        private readonly IDatabase _db;

        // 只有當鍵值等於預期持有者時才更新 TTL（續租）
        private const string RenewIfOwnerScript =
            "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('pexpire', KEYS[1], ARGV[2]) else return 0 end";

        // 只有當鍵值等於預期持有者時才刪除（釋放）
        private const string ReleaseIfOwnerScript =
            "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";

        public ClusterService(IDatabase db = null)
        {
            _db = db ?? RedisConnection.Instance.ConnectionMultiplexer.GetDatabase();
        }

        #region 心跳
        /// <summary>寫入 / 更新本程序心跳鍵（帶 TTL）。</summary>
        public Task WriteHeartbeatAsync(string role, string instanceId, TimeSpan ttl)
            => _db.StringSetAsync(RedisChannels.Cluster.Heartbeat(role, instanceId),
                DateTimeOffset.UtcNow.ToUnixTimeSeconds(), ttl);

        /// <summary>查詢某心跳鍵是否仍存活。</summary>
        public Task<bool> IsHeartbeatAliveAsync(string role, string instanceId)
            => _db.KeyExistsAsync(RedisChannels.Cluster.Heartbeat(role, instanceId));

        /// <summary>列出目前存活的心跳鍵（供 coordinator 監控）。</summary>
        public IEnumerable<string> ScanHeartbeatKeys()
        {
            var endpoints = _db.Multiplexer.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _db.Multiplexer.GetServer(endpoint);
                if (!server.IsConnected || server.IsReplica)
                    continue;

                foreach (var key in server.Keys(_db.Database, pattern: "cluster:heartbeat:*"))
                    yield return key.ToString();
            }
        }
        #endregion

        #region scraper leader 鎖
        /// <summary>嘗試取得 scraper leader 鎖（SET NX EX）。成功回傳 true。</summary>
        public Task<bool> TryAcquireScraperLeaderAsync(string instanceId, TimeSpan ttl)
            => _db.StringSetAsync(RedisChannels.Cluster.ScraperLeader, instanceId, ttl, When.NotExists);

        /// <summary>續租 scraper leader 鎖（僅持有者有效）。</summary>
        public async Task<bool> RenewScraperLeaderAsync(string instanceId, TimeSpan ttl)
        {
            var result = await _db.ScriptEvaluateAsync(RenewIfOwnerScript,
                new RedisKey[] { RedisChannels.Cluster.ScraperLeader },
                new RedisValue[] { instanceId, (long)ttl.TotalMilliseconds });
            return (long)result == 1;
        }

        /// <summary>釋放 scraper leader 鎖（僅持有者有效）。</summary>
        public async Task<bool> ReleaseScraperLeaderAsync(string instanceId)
        {
            var result = await _db.ScriptEvaluateAsync(ReleaseIfOwnerScript,
                new RedisKey[] { RedisChannels.Cluster.ScraperLeader },
                new RedisValue[] { instanceId });
            return (long)result == 1;
        }

        /// <summary>查詢目前 leader 持有者（無則 null）。</summary>
        public async Task<string> GetScraperLeaderAsync()
        {
            var value = await _db.StringGetAsync(RedisChannels.Cluster.ScraperLeader);
            return value.IsNullOrEmpty ? null : value.ToString();
        }
        #endregion

        #region TOTAL_SHARDS 公告
        public Task AnnounceTotalShardsAsync(int totalShards)
            => _db.StringSetAsync(RedisChannels.Cluster.TotalShards, totalShards);

        public async Task<int?> GetTotalShardsAsync()
        {
            var value = await _db.StringGetAsync(RedisChannels.Cluster.TotalShards);
            return value.IsNullOrEmpty ? null : (int)value;
        }
        #endregion

        #region shard 租約（方式 B，計畫 §6.2.1）
        /// <summary>嘗試取得指定 shard 的租約（SET NX EX）。</summary>
        public Task<bool> TryAcquireShardLeaseAsync(int shardId, string instanceId, TimeSpan ttl)
            => _db.StringSetAsync(RedisChannels.Cluster.ShardLease(shardId), instanceId, ttl, When.NotExists);

        /// <summary>續租指定 shard 租約（僅持有者有效）。</summary>
        public async Task<bool> RenewShardLeaseAsync(int shardId, string instanceId, TimeSpan ttl)
        {
            var result = await _db.ScriptEvaluateAsync(RenewIfOwnerScript,
                new RedisKey[] { RedisChannels.Cluster.ShardLease(shardId) },
                new RedisValue[] { instanceId, (long)ttl.TotalMilliseconds });
            return (long)result == 1;
        }

        /// <summary>
        /// 在 <c>[0, totalShards)</c> 範圍內搶第一個尚未被占用的 shard id；全部被占用時回傳 null（應待命重試）。
        /// </summary>
        public async Task<int?> TryClaimAnyShardAsync(int totalShards, string instanceId, TimeSpan ttl)
        {
            for (int i = 0; i < totalShards; i++)
            {
                if (await TryAcquireShardLeaseAsync(i, instanceId, ttl))
                    return i;
            }
            return null;
        }
        #endregion
    }
}
