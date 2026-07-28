using Newtonsoft.Json.Linq;

namespace DiscordStreamNotifyBot.Shared
{
    /// <summary>
    /// RedisTokenKey（Auth 加解密金鑰，與後端共用）叢集佈建（在 Redis 連線後執行）。
    /// <para>
    /// 原本各程序在 <see cref="BotConfig.InitBotConfig"/> 各自產生金鑰，會造成每個 shard / scraper / coordinator
    /// 產生「不同」金鑰而彼此與後端對不上（且 6 個容器共用同一份 bot_config.json 互相覆寫）。
    /// 改由本類統一佈建：以 Redis 鍵 <see cref="RedisChannels.Cluster.RedisTokenKey"/> 為單一真實來源，
    /// <b>僅 notifier shard 0 有權建立</b>，其餘 shard 一律自 Redis 取用；建立後同步至後端（<c>member.syncRedisToken</c>）。
    /// </para>
    /// <para>只有 notifier 需要此金鑰（<see cref="RedisDataStore"/> 加解密會限 OAuth token）；scraper / coordinator 不呼叫本類。</para>
    /// </summary>
    public static class RedisTokenKeyProvisioner
    {
        /// <param name="role">程序角色。</param>
        /// <param name="shardId">notifier 的 shard 身分（shard 0 為建立權威）。</param>
        /// <param name="cfg">已載入並套用 env 覆寫的設定；本類會就地更新其 <see cref="BotConfig.RedisTokenKey"/>。</param>
        /// <param name="waitTimeout">非 shard 0 等待 shard 0 建立金鑰的時限。</param>
        public static async Task EnsureAsync(BotRole role, int shardId, BotConfig cfg, TimeSpan waitTimeout)
        {
            var mux = RedisConnection.Instance.ConnectionMultiplexer;
            var db = mux.GetDatabase();
            var sub = mux.GetSubscriber();
            bool isAuthority = role == BotRole.Notifier && shardId == 0;
            const string rkKey = RedisChannels.Cluster.RedisTokenKey;

            // 1) 只有 shard 0 能以設定檔金鑰更新叢集真實來源；其餘 shard 即使帶到舊設定也只能採用 Redis 值。
            if (isAuthority && !string.IsNullOrWhiteSpace(cfg.RedisTokenKey))
            {
                await db.StringSetAsync(rkKey, cfg.RedisTokenKey);
                Adopt(cfg, cfg.RedisTokenKey);
                await PublishToBackendAsync(sub, cfg.RedisTokenKey);
                return;
            }

            // 2) 設定檔無金鑰 → 先看 Redis 是否已由 shard 0 建立。
            var existing = await db.StringGetAsync(rkKey);
            if (!existing.IsNullOrEmpty)
            {
                Adopt(cfg, existing.ToString());
                return;
            }

            // 3) Redis 也沒有：只有 shard 0 能建立，其餘 shard 等待 shard 0 建立後取用。
            if (isAuthority)
            {
                var key = BotConfig.GenRandomKey();
                // SET NX：即使理論上不該有競爭者，仍以原子寫入 + 讀回勝出者確保全叢集一致。
                await db.StringSetAsync(rkKey, key, when: When.NotExists);
                key = (await db.StringGetAsync(rkKey)).ToString();
                Adopt(cfg, key);
                await PublishToBackendAsync(sub, key);
                Log.Info($"已由 shard 0 建立 {nameof(BotConfig.RedisTokenKey)} 並同步至 Redis 與後端");
            }
            else
            {
                Log.Warn($"{nameof(BotConfig.RedisTokenKey)} 尚未建立，等待 notifier shard 0 建立中…");
                var key = await WaitForKeyAsync(db, rkKey, waitTimeout);
                if (string.IsNullOrEmpty(key))
                    throw new InvalidOperationException(
                        $"等待 shard 0 建立 {nameof(BotConfig.RedisTokenKey)} 逾時（{waitTimeout.TotalSeconds:0} 秒）。" +
                        "請確認 notifier shard 0 已正常啟動。");
                Adopt(cfg, key);
                Log.Info($"已自 Redis 取得由 shard 0 建立的 {nameof(BotConfig.RedisTokenKey)}");
            }
        }

        // 採用金鑰：設定 Utility.RedisKey（RedisDataStore / 後端同步的真實來源）並就地更新 cfg。
        private static void Adopt(BotConfig cfg, string key)
        {
            Utility.RedisKey = key;
            cfg.RedisTokenKey = key;
        }

        private static async Task PublishToBackendAsync(ISubscriber sub, string key)
        {
            await sub.PublishAsync(
                new RedisChannel(RedisChannels.Member.SyncRedisToken, RedisChannel.PatternMode.Literal), key);
        }

        private static async Task<string> WaitForKeyAsync(IDatabase db, string rkKey, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                var v = await db.StringGetAsync(rkKey);
                if (!v.IsNullOrEmpty)
                    return v.ToString();
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
            return null;
        }
    }
}
