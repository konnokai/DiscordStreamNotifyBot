using DiscordStreamNotifyBot.Shared;

namespace DiscordStreamNotifyBot.Scraper
{
    /// <summary>
    /// 爬蟲層核心邏輯（計畫 §2.2 / §5.2）。
    /// <para>
    /// 啟動時取得 scraper leader 鎖（叢集單例保證），取得後以 <see cref="DetectionHost"/>
    /// 啟動偵測服務（事件發布至通知匯流排），定期續租並寫心跳；關閉時保存狀態並釋放鎖。
    /// </para>
    /// <para>
    /// 失去 leader（續租失敗）視為致命錯誤立即結束程序 —— 偵測 Timer 啟動後無法收回，
    /// 若另一實例已接手會造成雙重偵測（split-brain），交由 Compose restart 重新走取鎖流程。
    /// </para>
    /// </summary>
    public class ScraperService
    {
        private readonly BotConfig _config;
        private readonly ClusterService _cluster;
        private readonly string _instanceId;
        private readonly TimeSpan _heartbeatInterval;
        private readonly TimeSpan _leaderTtl;

        public ScraperService(BotConfig config)
        {
            _config = config;
            _cluster = new ClusterService();
            _instanceId = $"{Environment.MachineName}:{Environment.ProcessId}";
            _heartbeatInterval = TimeSpan.FromSeconds(Math.Max(1, config.HeartbeatIntervalSeconds));
            // leader TTL 須明顯大於續租間隔，避免 GC 暫停導致誤失鎖（計畫 §6.2.1 同理）
            _leaderTtl = TimeSpan.FromSeconds(Math.Max(_config.HeartbeatTtlSeconds, config.HeartbeatIntervalSeconds * 3));
        }

        /// <returns>程序退出碼：0＝正常關閉；1＝失去 leader（需重啟重新取鎖）。</returns>
        public async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            // 取得 leader 鎖（叢集唯一），拿不到則待命重試
            await AcquireLeadershipAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return 0;

            Log.Info($"[Scraper] 已取得 leader 鎖（{_instanceId}）");

            // 取得 leader 後才啟動偵測（叢集單例保證：同時只有一個程序在偵測與發布）
            var detectionHost = new DetectionHost();
            detectionHost.Start(_config);

            int exitCode = 0;
            var heartbeatRole = BotRole.Scraper.ToString().ToLowerInvariant();
            using var timer = new PeriodicTimer(_heartbeatInterval);
            try
            {
                do
                {
                    await _cluster.WriteHeartbeatAsync(heartbeatRole, _instanceId, _leaderTtl);

                    if (!await _cluster.RenewScraperLeaderAsync(_instanceId, _leaderTtl))
                    {
                        // 偵測 Timer 啟動後無法收回；失去鎖代表可能已有別的實例接手 → 立即結束避免雙重偵測
                        Log.Error("[Scraper] leader 鎖續租失敗（可能已被其他實例接手），立即結束以避免雙重偵測");
                        exitCode = 1;
                        break;
                    }
                }
                while (await SafeWaitAsync(timer, cancellationToken));
            }
            finally
            {
                detectionHost.SaveStateBeforeShutdown();
                await _cluster.ReleaseScraperLeaderAsync(_instanceId);
                Log.Info("[Scraper] 已釋放 leader 鎖");
            }

            return exitCode;
        }

        private async Task AcquireLeadershipAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (await _cluster.TryAcquireScraperLeaderAsync(_instanceId, _leaderTtl))
                    return;

                var current = await _cluster.GetScraperLeaderAsync();
                Log.Info($"[Scraper] leader 由 {current} 持有，待命 {_heartbeatInterval.TotalSeconds:0} 秒後重試…");
                try { await Task.Delay(_heartbeatInterval, cancellationToken); }
                catch (OperationCanceledException) { return; }
            }
        }

        private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken cancellationToken)
        {
            try { return await timer.WaitForNextTickAsync(cancellationToken); }
            catch (OperationCanceledException) { return false; }
        }
    }
}
