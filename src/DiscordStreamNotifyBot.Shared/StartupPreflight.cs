using DiscordStreamNotifyBot.DataBase;

namespace DiscordStreamNotifyBot.Shared
{
    /// <summary>
    /// 啟動連線檢查（Preflight，計畫 §5.3）。任何角色在進入主邏輯之前，先依角色檢查所需外部服務可連線；
    /// 每項以指數退避重試，逾時仍失敗則拋出帶診斷訊息的例外，交由呼叫端印出後 <c>Environment.Exit(非0)</c>，
    /// 再由 Docker <c>restart: unless-stopped</c> 重來，避免無限卡死在啟動。
    /// </summary>
    public static class StartupPreflight
    {
        /// <summary>
        /// 依角色執行啟動連線檢查。
        /// </summary>
        /// <param name="role">程序角色。</param>
        /// <param name="cfg">已載入並套用 env 覆寫的設定。</param>
        /// <param name="timeout">單一檢查項目的重試總時限。</param>
        public static async Task EnsureAsync(BotRole role, BotConfig cfg, TimeSpan timeout)
        {
            var checks = new List<(string name, Func<Task> probe)>();

            // MySQL：scraper / notifier 需要
            if (role is BotRole.Scraper or BotRole.Notifier)
                checks.Add(("MySQL", () => ProbeMySqlAsync(cfg.MySqlConnectionString)));

            // Redis：全角色需要（控制平面 / 錄影 IPC）
            checks.Add(("Redis", () => ProbeRedisAsync(cfg.RedisOption)));

            // RabbitMQ：scraper(publish) / notifier(consume) 需要
            // TODO(階段 3)：RabbitMqService 上線後改為實際建立連線並宣告 bot.notify exchange / queue
            if (role is BotRole.Scraper or BotRole.Notifier)
                checks.Add(("RabbitMQ", () => ProbeRabbitMqAsync(cfg.RabbitMQ)));

            // Discord 由 notifier 既有登入流程驗證，不在此處理

            foreach (var (name, probe) in checks)
                await RetryWithBackoffAsync(name, probe, timeout);

            Log.Info($"啟動連線檢查通過（角色: {role}）");
        }

        private static async Task ProbeMySqlAsync(string connectionString)
        {
            var dbService = new MainDbService(connectionString);
            using var db = dbService.GetDbContext();
            if (!await db.Database.CanConnectAsync())
                throw new InvalidOperationException("CanConnectAsync 回傳 false");
        }

        private static async Task ProbeRedisAsync(string redisOption)
        {
            RedisConnection.Init(redisOption);
            var db = RedisConnection.Instance.ConnectionMultiplexer.GetDatabase();
            await db.PingAsync();
        }

        private static Task ProbeRabbitMqAsync(BotConfig.RabbitMqConfig _)
        {
            // 佔位：階段 3 引入 RabbitMQ.Client 後，於此建立連線並完成 topology 初始化。
            // 目前尚未引入 RabbitMQ 基礎設施，故先視為通過，避免阻擋階段 1/2 的啟動流程。
            return Task.CompletedTask;
        }

        private static async Task RetryWithBackoffAsync(string name, Func<Task> probe, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            int attempt = 0;
            Exception lastException = null;

            while (DateTime.UtcNow < deadline)
            {
                attempt++;
                try
                {
                    await probe();
                    Log.Info($"[Preflight] {name} 連線成功（第 {attempt} 次嘗試）");
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    var delaySeconds = Math.Min(30, Math.Pow(2, Math.Min(attempt, 5)));
                    var remaining = deadline - DateTime.UtcNow;
                    if (remaining <= TimeSpan.Zero)
                        break;

                    var delay = TimeSpan.FromSeconds(delaySeconds);
                    if (delay > remaining)
                        delay = remaining;

                    Log.Warn($"[Preflight] {name} 連線失敗（第 {attempt} 次）：{ex.Message}；{delay.TotalSeconds:0} 秒後重試");
                    await Task.Delay(delay);
                }
            }

            throw new InvalidOperationException(
                $"啟動連線檢查失敗：無法在 {timeout.TotalSeconds:0} 秒內連上 {name}。" +
                $"請確認目標位址（host:port）可達、密碼正確、防火牆放行。最後錯誤：{lastException?.Message}",
                lastException);
        }
    }
}
