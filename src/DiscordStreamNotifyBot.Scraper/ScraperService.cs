using DiscordStreamNotifyBot.Shared;

namespace DiscordStreamNotifyBot.Scraper
{
    /// <summary>
    /// 爬蟲層核心邏輯（計畫 §2.2 / §5.2）。
    /// <para>
    /// 啟動時取得 scraper leader 鎖（叢集單例保證），定期續租並寫心跳；關閉時釋放鎖。
    /// </para>
    /// <para>
    /// TODO(階段 3 核心)：取得 leader 後，於此啟動所有偵測 Timer、錄影程序 Redis 訂閱、PubSub 維護，
    /// 並將偵測結果改以 <see cref="RabbitMqService"/> publish 結構化 DTO（取代直接呼叫 Discord）。
    /// 該段為行為改動，需在有 RabbitMQ broker 的環境多程序驗證。
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

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            // 取得 leader 鎖（叢集唯一），拿不到則待命重試
            await AcquireLeadershipAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;

            Log.Info($"[Scraper] 已取得 leader 鎖（{_instanceId}）");

            // TODO(階段 3 核心)：StartDetectionTimers(); SubscribeRecordingRedis(); MaintainPubSub();
            Log.Warn("[Scraper] 偵測主邏輯尚未搬入（階段 3 核心）；目前僅維持 leader 鎖與心跳。");

            var heartbeatRole = BotRole.Scraper.ToString().ToLowerInvariant();
            using var timer = new PeriodicTimer(_heartbeatInterval);
            try
            {
                do
                {
                    await _cluster.WriteHeartbeatAsync(heartbeatRole, _instanceId, _leaderTtl);

                    if (!await _cluster.RenewScraperLeaderAsync(_instanceId, _leaderTtl))
                    {
                        // 失去 leader（理論上不該發生）；記錄並嘗試重新取得
                        Log.Warn("[Scraper] leader 鎖續租失敗，嘗試重新取得…");
                        await AcquireLeadershipAsync(cancellationToken);
                    }
                }
                while (await SafeWaitAsync(timer, cancellationToken));
            }
            finally
            {
                await _cluster.ReleaseScraperLeaderAsync(_instanceId);
                Log.Info("[Scraper] 已釋放 leader 鎖");
            }
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
