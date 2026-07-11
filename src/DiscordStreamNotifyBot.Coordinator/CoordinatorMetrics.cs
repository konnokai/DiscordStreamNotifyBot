using Prometheus;
using StackExchange.Redis;

namespace DiscordStreamNotifyBot.Coordinator
{
    /// <summary>Coordinator 的 Prometheus 指標；所有值由監控迴圈更新，scrape 不直接查詢 Redis。</summary>
    public sealed class CoordinatorMetrics
    {
        private const string Prefix = "discord_stream_notify_";

        private readonly Gauge _coordinatorUp = Metrics.CreateGauge(
            Prefix + "coordinator_up", "Coordinator 程序是否已啟動並提供監控服務。");
        private readonly Counter _monitorCycles = Metrics.CreateCounter(
            Prefix + "coordinator_monitor_cycles_total", "Coordinator 監控迴圈執行次數。",
            new CounterConfiguration { LabelNames = ["result"] });
        private readonly Gauge _lastSuccessUnixTime = Metrics.CreateGauge(
            Prefix + "coordinator_last_success_unixtime", "最近一次成功完成監控迴圈的 Unix timestamp。");
        private readonly Gauge _totalShards = Metrics.CreateGauge(
            Prefix + "cluster_total_shards", "Coordinator 公告的 Discord shard 總數。");
        private readonly Gauge _aliveInstances = Metrics.CreateGauge(
            Prefix + "cluster_alive_instances", "各角色目前存在心跳的 instance 數。",
            new GaugeConfiguration { LabelNames = ["role"] });
        private readonly Gauge _scraperLeaderPresent = Metrics.CreateGauge(
            Prefix + "scraper_leader_present", "目前是否存在 Scraper leader。1 代表存在。");
        private readonly Gauge _busGroups = Metrics.CreateGauge(
            Prefix + "bus_groups", "通知匯流排目前的 consumer group 數。");
        private readonly Gauge _busPendingMessages = Metrics.CreateGauge(
            Prefix + "bus_pending_messages", "通知匯流排各 consumer group 的 pending 訊息數。",
            new GaugeConfiguration { LabelNames = ["group"] });
        private readonly Gauge _busConsumers = Metrics.CreateGauge(
            Prefix + "bus_consumers", "通知匯流排各 consumer group 的 consumer 數。",
            new GaugeConfiguration { LabelNames = ["group"] });
        private readonly Gauge _busGroupUnhealthy = Metrics.CreateGauge(
            Prefix + "bus_group_unhealthy", "通知匯流排 consumer group 是否異常。1 代表符合該原因。",
            new GaugeConfiguration { LabelNames = ["group", "reason"] });

        private readonly HashSet<string> _knownGroups = [];

        public void Start(int totalShards)
        {
            _coordinatorUp.Set(1);
            _totalShards.Set(totalShards);
        }

        public void Stop() => _coordinatorUp.Set(0);

        public void RecordCycleSuccess()
        {
            _monitorCycles.WithLabels("success").Inc();
            _lastSuccessUnixTime.Set(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        public void RecordCycleFailure() => _monitorCycles.WithLabels("failure").Inc();

        public void UpdateCluster(int totalShards, int aliveCoordinators, int aliveScrapers,
            int aliveNotifiers, bool scraperLeaderPresent)
        {
            _totalShards.Set(totalShards);
            _aliveInstances.WithLabels("coordinator").Set(aliveCoordinators);
            _aliveInstances.WithLabels("scraper").Set(aliveScrapers);
            _aliveInstances.WithLabels("notifier").Set(aliveNotifiers);
            _scraperLeaderPresent.Set(scraperLeaderPresent ? 1 : 0);
        }

        public void UpdateBus(StreamGroupInfo[] groups, int pendingBacklogWarnThreshold)
        {
            _busGroups.Set(groups.Length);

            var currentGroups = groups.Select(group => group.Name.ToString()).ToHashSet();
            foreach (string staleGroup in _knownGroups.Except(currentGroups).ToArray())
            {
                _busPendingMessages.RemoveLabelled(staleGroup);
                _busConsumers.RemoveLabelled(staleGroup);
                _busGroupUnhealthy.RemoveLabelled(staleGroup, "no_consumer");
                _busGroupUnhealthy.RemoveLabelled(staleGroup, "backlog");
                _knownGroups.Remove(staleGroup);
            }

            foreach (var group in groups)
            {
                string groupName = group.Name;
                _knownGroups.Add(groupName);
                _busPendingMessages.WithLabels(groupName).Set(group.PendingMessageCount);
                _busConsumers.WithLabels(groupName).Set(group.ConsumerCount);
                _busGroupUnhealthy.WithLabels(groupName, "no_consumer").Set(group.ConsumerCount == 0 ? 1 : 0);
                _busGroupUnhealthy.WithLabels(groupName, "backlog")
                    .Set(group.PendingMessageCount >= pendingBacklogWarnThreshold ? 1 : 0);
            }
        }
    }
}
