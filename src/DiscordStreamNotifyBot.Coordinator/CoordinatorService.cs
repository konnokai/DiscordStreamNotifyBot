using DiscordStreamNotifyBot.Shared;

namespace DiscordStreamNotifyBot.Coordinator
{
    /// <summary>
    /// 主控層核心邏輯（計畫 §2.4 / §5.2）：
    /// 公告 TOTAL_SHARDS、寫入自身心跳、監控各角色心跳與 scraper leader，並定期輸出叢集狀態。
    /// <para>不負責 <c>Process.Start</c>；實際重啟交給 Docker Compose <c>restart: unless-stopped</c>。</para>
    /// </summary>
    public class CoordinatorService
    {
        private readonly BotConfig _config;
        private readonly ClusterService _cluster;
        private readonly string _instanceId;

        public CoordinatorService(BotConfig config)
        {
            _config = config;
            _cluster = new ClusterService();
            _instanceId = $"{Environment.MachineName}:{Environment.ProcessId}";
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            // 公告叢集真實 shard 總數
            await _cluster.AnnounceTotalShardsAsync(_config.TotalShards);
            Log.Info($"[Coordinator] 已公告 TOTAL_SHARDS = {_config.TotalShards}");

            var heartbeatInterval = TimeSpan.FromSeconds(Math.Max(1, _config.HeartbeatIntervalSeconds));
            var heartbeatTtl = TimeSpan.FromSeconds(Math.Max(2, _config.HeartbeatTtlSeconds));

            using var timer = new PeriodicTimer(heartbeatInterval);
            do
            {
                try
                {
                    // 自身心跳 + 持續公告（避免被清掉）
                    await _cluster.WriteHeartbeatAsync(BotRole.Coordinator.ToString().ToLowerInvariant(), _instanceId, heartbeatTtl);
                    await _cluster.AnnounceTotalShardsAsync(_config.TotalShards);

                    await ReportClusterStatusAsync();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), "[Coordinator] 監控迴圈發生錯誤");
                }
            }
            while (await SafeWaitAsync(timer, cancellationToken));

            Log.Info("[Coordinator] 已停止監控迴圈");
        }

        private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken cancellationToken)
        {
            try { return await timer.WaitForNextTickAsync(cancellationToken); }
            catch (OperationCanceledException) { return false; }
        }

        private async Task ReportClusterStatusAsync()
        {
            // scraper leader
            var leader = await _cluster.GetScraperLeaderAsync();
            string leaderText = leader is null ? "（無，等待接手）" : leader;

            // 存活心跳清單
            var aliveKeys = _cluster.ScanHeartbeatKeys().ToList();

            // 檢查每個 notifier shard 是否有人認領（依心跳鍵中的 id 難以直接對應 shardId，故以租約鍵 + 數量粗略判斷）
            int aliveNotifiers = aliveKeys.Count(k => k.Contains(":notifier:"));
            bool scraperAlive = aliveKeys.Any(k => k.Contains(":scraper:"));

            var missingHint = aliveNotifiers < _config.TotalShards
                ? $"（注意：存活 notifier {aliveNotifiers} < TOTAL_SHARDS {_config.TotalShards}，可能有 shard 未認領）"
                : "";

            Log.Info($"[Coordinator] 叢集狀態 | leader={leaderText} | scraper存活={scraperAlive} | " +
                     $"notifier存活={aliveNotifiers}/{_config.TotalShards} {missingHint} | 心跳鍵數={aliveKeys.Count}");
        }
    }
}
