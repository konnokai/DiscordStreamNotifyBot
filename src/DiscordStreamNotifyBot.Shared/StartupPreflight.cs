using DiscordStreamNotifyBot.DataBase;

namespace DiscordStreamNotifyBot.Shared
{
    /// <summary>
    /// 啟動連線檢查（Preflight，計畫 §5.3）。任何角色在進入主邏輯之前，先依角色檢查所需外部服務可連線；
    /// 每項以指數退避重試，逾時仍失敗則拋出帶診斷訊息的例外，由呼叫端輸出後以非 0 結束碼結束程序，
    /// 避免啟動時無限卡住（scraper 由 Compose 重啟；notifier 是 <c>restart: "no"</c>，保持停止待人工處理）。
    /// MySQL 連線確認後會套用尚未執行的 EF migration，schema 版本跟著程式走。
    /// </summary>
    public static class StartupPreflight
    {
        /// <summary>依角色執行啟動連線檢查。</summary>
        /// <param name="role">程序角色。</param>
        /// <param name="cfg">已載入並套用 env 覆寫的設定。</param>
        /// <param name="timeout">單一檢查項目的重試總時限。</param>
        public static async Task EnsureAsync(BotRole role, BotConfig cfg, TimeSpan timeout)
        {
            // MySQL：scraper / notifier 需要；連線確認後直接套用尚未執行的 migration
            if (role is BotRole.Scraper or BotRole.Notifier)
            {
                await RetryWithBackoffAsync("MySQL", () => ProbeMySqlAsync(cfg.MySqlConnectionString), timeout, TimeProvider.System);
                await MigrateDatabaseAsync(cfg.MySqlConnectionString);
            }

            // Redis：全角色需要（控制平面 / 錄影 IPC / 匯流排）
            await RetryWithBackoffAsync("Redis", () => ProbeRedisAsync(cfg.RedisOption), timeout, TimeProvider.System);

            // Discord 由 notifier 既有登入流程驗證，不在此處理

            Log.Info($"啟動連線檢查通過（角色：{role}）");
        }

        private static async Task ProbeMySqlAsync(string connectionString)
        {
            var dbService = new MainDbService(connectionString);
            using var db = dbService.GetDbContext();
            if (!await db.Database.CanConnectAsync())
                throw new InvalidOperationException("CanConnectAsync 回傳 false");
        }

        /// <summary>
        /// 套用尚未執行的 EF migration。scraper 與各 notifier shard 同時啟動時，
        /// 由 Pomelo 內建的 <c>__{database}_EFMigrationsLock</c> 具名鎖互斥，不另外加鎖。
        /// </summary>
        private static async Task MigrateDatabaseAsync(string connectionString)
        {
            using var db = new MainDbService(connectionString).GetDbContext();
            // 大表的 ALTER TABLE 可能超過預設 30 秒；DDL 被中斷會留下半套 schema
            db.Database.SetCommandTimeout(3600);

            var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count == 0)
            {
                Log.Info("[Preflight] 資料庫 schema 已是最新");
                return;
            }

            Log.Info($"[Preflight] 套用資料庫 migration（{pending.Count} 個）：{string.Join(", ", pending)}");
            await db.Database.MigrateAsync();
            Log.Info("[Preflight] 資料庫 migration 完成");
        }

        private static async Task ProbeRedisAsync(string redisOption)
        {
            RedisConnection.ResetForRetry(redisOption);
            var db = RedisConnection.Instance.ConnectionMultiplexer.GetDatabase();
            await db.PingAsync();
        }

        private static async Task RetryWithBackoffAsync(
            string name,
            Func<Task> probe,
            TimeSpan timeout,
            TimeProvider timeProvider)
        {
            var deadline = timeProvider.GetUtcNow() + timeout;
            int attempt = 0;
            Exception lastException = null;

            while (timeProvider.GetUtcNow() < deadline)
            {
                attempt++;
                try
                {
                    TimeSpan probeTimeout = deadline - timeProvider.GetUtcNow();
                    await Task.Run(probe).WaitAsync(probeTimeout, timeProvider).ConfigureAwait(false);
                    Log.Info($"[Preflight] {name} 連線成功（第 {attempt} 次嘗試）");
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    var delaySeconds = Math.Min(30, Math.Pow(2, Math.Min(attempt, 5)));
                    var remaining = deadline - timeProvider.GetUtcNow();
                    if (remaining <= TimeSpan.Zero)
                        break;

                    var delay = TimeSpan.FromSeconds(delaySeconds);
                    if (delay > remaining)
                        delay = remaining;

                    Log.Warn($"[Preflight] {name} 連線失敗（第 {attempt} 次）：{ex.Message}；{delay.TotalSeconds:0} 秒後重試");
                    await Task.Delay(delay, timeProvider).ConfigureAwait(false);
                }
            }

            throw new InvalidOperationException(
                $"啟動連線檢查失敗：無法在 {timeout.TotalSeconds:0} 秒內連上 {name}。" +
                $"請確認目標位址（host:port）可達、密碼正確、防火牆放行。最後錯誤：{lastException?.Message}",
                lastException);
        }
    }
}
