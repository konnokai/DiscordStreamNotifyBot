using StackExchange.Redis;

namespace DiscordStreamNotifyBot.Coordinator
{
    /// <summary>
    /// 主控層核心邏輯（計畫 §2.4 / §5.2）：
    /// 公告 TOTAL_SHARDS、寫入自身心跳、監控各角色心跳與 scraper leader、監控匯流排 pending 堆積，狀態改變時輸出 log。
    /// <para>不負責 <c>Process.Start</c>；實際重啟交給 Docker Compose <c>restart: unless-stopped</c>。</para>
    /// </summary>
    public class CoordinatorService
    {
        // pending 堆積告警門檻：單一消費者群組的未確認訊息數達此值即警告。
        // 2026/10/14～10/20 Prometheus 原始資料的 pending 峰值是 21，門檻 20 一週會被正常高峰觸發約 33 次，所以留一點空間。
        // ponytail: 固定門檻，若日後通知量級改變再調成可設定
        private const int PendingBacklogWarnThreshold = 30;

        private readonly BotConfig _config;
        private readonly ClusterService _cluster;
        private readonly IDatabase _db;
        private readonly CoordinatorMetrics _metrics;
        private readonly string _instanceId;

        // 監控迴圈跟著心跳間隔跑，每輪都寫 log 會洗版；只在狀態改變時寫，數值變化看 Prometheus。
        private string _lastClusterState;
        private bool? _lastBusEmpty;
        private readonly Dictionary<string, string> _groupIssues = [];

        public CoordinatorService(BotConfig config, CoordinatorMetrics metrics)
        {
            _config = config;
            _cluster = new ClusterService();
            _db = RedisConnection.Instance.ConnectionMultiplexer.GetDatabase();
            _metrics = metrics;
            _instanceId = $"{Environment.MachineName}:{Environment.ProcessId}";
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            // 公告叢集真實 shard 總數
            await _cluster.AnnounceTotalShardsAsync(_config.TotalShards);
            Log.Info($"已公告 TOTAL_SHARDS = {_config.TotalShards}");

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
                    await ReportBusBacklogAsync();
                    _metrics.RecordCycleSuccess();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _metrics.RecordCycleFailure();
                    Log.Error(ex.Demystify(), "監控迴圈發生錯誤");
                }
            }
            while (await SafeWaitAsync(timer, cancellationToken));

            Log.Info("已停止監控迴圈");
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

            // 檢查每個 notifier shard 是否有人認領（依心跳鍵中的 id 難以直接對應 shardId，故以數量粗略判斷）
            int aliveNotifiers = aliveKeys.Count(k => k.Contains(":notifier:"));
            int aliveScrapers = aliveKeys.Count(k => k.Contains(":scraper:"));
            int aliveCoordinators = aliveKeys.Count(k => k.Contains(":coordinator:"));
            bool scraperAlive = aliveScrapers > 0;

            _metrics.UpdateCluster(_config.TotalShards, aliveCoordinators, aliveScrapers,
                aliveNotifiers, leader is not null);

            var missingHint = aliveNotifiers < _config.TotalShards
                ? $"（注意：存活 notifier {aliveNotifiers} < TOTAL_SHARDS {_config.TotalShards}，可能有 shard 未認領）"
                : "";

            string clusterState = $"leader={leaderText} | scraper存活={scraperAlive} | " +
                                  $"notifier存活={aliveNotifiers}/{_config.TotalShards} {missingHint}";
            if (clusterState == _lastClusterState)
                return;

            _lastClusterState = clusterState;
            Log.Info($"叢集狀態 | {clusterState} | 心跳鍵數={aliveKeys.Count}");
        }

        /// <summary>
        /// 監控匯流排各 consumer group 的 pending 堆積（計畫 §4.4 與 §9 第 3 點）：
        /// pending 過高代表消費端落後；consumer 數為 0（例如縮容後留下的 group）代表訊息無人處理，兩者皆告警。
        /// </summary>
        private async Task ReportBusBacklogAsync()
        {
            var groups = await NotificationBus.GetGroupsAsync(_db);
            _metrics.UpdateBus(groups, PendingBacklogWarnThreshold);

            bool busEmpty = groups.Length == 0;
            if (busEmpty != _lastBusEmpty)
            {
                _lastBusEmpty = busEmpty;
                if (busEmpty)
                    Log.Info($"匯流排 {NotificationBus.StreamKey} 尚無 consumer group（notifier 未啟動或 stream 未建立）");
                else
                    Log.Info($"匯流排 {NotificationBus.StreamKey} 有 {groups.Length} 個 consumer group");
            }

            var currentGroups = new HashSet<string>();
            foreach (var group in groups)
            {
                string name = group.Name;
                currentGroups.Add(name);

                // 只記異常種類，pending 數字變動不算狀態改變
                string issue = group.ConsumerCount == 0 ? "no_consumer"
                    : group.PendingMessageCount >= PendingBacklogWarnThreshold ? "backlog"
                    : null;
                _groupIssues.TryGetValue(name, out string lastIssue);
                if (issue == lastIssue)
                    continue;

                if (issue is null)
                {
                    _groupIssues.Remove(name);
                    Log.Info($"匯流排 group {name} 已恢復，pending={group.PendingMessageCount}");
                    continue;
                }

                _groupIssues[name] = issue;
                if (issue == "no_consumer")
                    Log.Warn($"匯流排 group {name} 無 consumer，pending={group.PendingMessageCount}（訊息無人處理）");
                else
                    Log.Warn($"匯流排 group {name} pending={group.PendingMessageCount} 已達門檻 {PendingBacklogWarnThreshold}，消費端可能落後");
            }

            foreach (string staleGroup in _groupIssues.Keys.Except(currentGroups).ToArray())
            {
                _groupIssues.Remove(staleGroup);
                Log.Info($"匯流排 group {staleGroup} 已移除");
            }
        }
    }
}
