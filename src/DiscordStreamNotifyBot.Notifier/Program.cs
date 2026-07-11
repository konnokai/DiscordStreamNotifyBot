using DiscordStreamNotifyBot.HttpClients;
using DiscordStreamNotifyBot.Shared;
using System.Reflection;

namespace DiscordStreamNotifyBot
{
    public class Program
    {
        private const BotRole Role = BotRole.Notifier;
        private static int _isHandlingUnhandledException;

        public static string Version => GetLinkerTime(Assembly.GetEntryAssembly());

        static async Task Main(string[] args)
        {
            Log.Info(Version + " 初始化中");
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // 統一優雅關閉（SIGINT/SIGTERM，計畫 §9-1）：取消時橋接至既有的 Bot.IsDisconnect 關閉路徑
            GracefulShutdown.Init();
            GracefulShutdown.Token.Register(() => Bot.IsDisconnect = true);

            if (!Directory.Exists(Path.GetDirectoryName(Utility.GetDataFilePath(""))))
                Directory.CreateDirectory(Path.GetDirectoryName(Utility.GetDataFilePath("")));

            int shardId = 0;
            int totalShards = 1;
            if (args.Length > 0 && args[0] != "run")
            {
                if (!int.TryParse(args[0], out shardId))
                {
                    Console.Error.WriteLine("Invalid first argument (shard id): {0}", args[0]);
                    return;
                }

                if (args.Length > 1)
                {
                    if (!int.TryParse(args[1], out var shardCount))
                    {
                        Console.Error.WriteLine("Invalid second argument (total shards): {0}", args[1]);
                        return;
                    }

                    totalShards = shardCount;
                }
            }

            // 啟動連線檢查（計畫 §5.3）：進入主邏輯前確認 MySQL / Redis 可連線，失敗印訊息後 Exit(1) 交給 Compose restart
            var preflightConfig = new BotConfig();
            try
            {
                preflightConfig.InitBotConfig();
                await StartupPreflight.EnsureAsync(Role, preflightConfig, TimeSpan.FromSeconds(60));
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "StartupPreflight 失敗");
                Environment.Exit(1);
            }

            RegisterUnhandledExceptionHandler(preflightConfig, shardId, totalShards);

            // RedisTokenKey 叢集佈建（僅 shard 0 有權建立，以 Redis 為單一真實來源同步至各 shard 與後端）。
            // 必須在建立 Bot（其 YoutubeMemberService/RedisDataStore 會捕捉 Utility.RedisKey）之前完成。
            try
            {
                await RedisTokenKeyProvisioner.EnsureAsync(Role, shardId, preflightConfig, TimeSpan.FromSeconds(60));
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "RedisTokenKey 佈建失敗");
                Environment.Exit(1);
            }

            // 官方伺服器白名單改存 Redis（階段 5：跨 shard 共享）；首次啟動由舊 OfficialList.json 播種
            try
            {
                var redisDb = RedisConnection.Instance.ConnectionMultiplexer.GetDatabase();
                if (!await redisDb.KeyExistsAsync(RedisChannels.SharedState.OfficialGuildList) &&
                    File.Exists(Utility.GetDataFilePath("OfficialList.json")))
                {
                    Utility.OfficialGuildList = JsonConvert.DeserializeObject<HashSet<ulong>>(File.ReadAllText(Utility.GetDataFilePath("OfficialList.json")));
                    await Utility.SaveOfficialGuildListToRedisAsync();
                    Log.Info($"已將 OfficialList.json（{Utility.OfficialGuildList.Count} 筆）播種至 Redis");
                }

                await Utility.LoadOfficialGuildListFromRedisAsync();
                Log.Info($"官方伺服器白名單已自 Redis 載入（{Utility.OfficialGuildList.Count} 筆）");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "載入官方伺服器白名單失敗");
            }

            // 寫入 notifier 心跳（計畫 §5.2）：coordinator 依 cluster:heartbeat:notifier:* 的數量判斷有幾個 shard 有人認領。
            // scraper / coordinator 已各自寫心跳，唯獨 notifier 先前漏寫，導致 coordinator 永遠顯示 notifier 存活=0。
            // id 以 shard 為鍵（非 machine:pid）：同一 shard 被多個程序重複認領時會共用同一鍵，數量才等於「實際被涵蓋的 shard 數」。
            _ = Task.Run(() => RunHeartbeatLoopAsync(preflightConfig, shardId, GracefulShutdown.Token));

            var bot = new Bot(shardId, totalShards);
            bot.StartAndBlockAsync().GetAwaiter().GetResult();
        }

        private static void RegisterUnhandledExceptionHandler(BotConfig config, int shardId, int totalShards)
        {
            // https://stackoverflow.com/q/5710148/15800522
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (Interlocked.Exchange(ref _isHandlingUnhandledException, 1) != 0)
                    return;

                var exception = e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString() ?? "未知的未處理例外");
                var demystified = exception.Demystify();

                try
                {
                    if (!Debugger.IsAttached && !Log.IsRunningInContainer)
                    {
                        using var writer = new StreamWriter($"{DateTime.Now:yyyy-MM-dd HH-mm-ss}_crash.log");
                        writer.WriteLine("### Bot Crash ###");
                        writer.WriteLine(demystified.ToString());
                    }

                    Log.Error(demystified, "UnhandledException", true, false);

                    try
                    {
                        string content = BuildUnhandledExceptionWebhookMessage(demystified, shardId, totalShards);
                        DiscordWebhookClient.SendMessageToDiscordAsync(
                            config.WebHookUrl,
                            content,
                            "直播小幫手 Crash Monitor").GetAwaiter().GetResult();
                    }
                    catch (Exception webhookException)
                    {
                        Log.Error(webhookException.Demystify(), "UnhandledException webhook 發送失敗");
                    }
                }
                finally
                {
                    Environment.Exit(1);
                }
            };
        }

        private static string BuildUnhandledExceptionWebhookMessage(Exception exception, int shardId, int totalShards)
        {
            string header = $"**Notifier 發生未處理例外，Container 將保持停止**\n" +
                $"版本: `{Version}`\n" +
                $"Shard: `{shardId} / {totalShards}`\n" +
                $"主機: `{Environment.MachineName}`\n" +
                $"時間: `{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}`";
            string details = exception.ToString().Replace("```", "'''");
            string prefix = $"{header}\n```text\n";
            const string suffix = "\n```";
            int maxDetailsLength = Math.Max(0, 2000 - prefix.Length - suffix.Length);
            if (details.Length > maxDetailsLength)
                details = details[..Math.Max(0, maxDetailsLength - 3)] + "...";

            return prefix + details + suffix;
        }

        /// <summary>背景寫入本 notifier 程序的心跳鍵（帶 TTL），供 coordinator 監控存活。</summary>
        private static async Task RunHeartbeatLoopAsync(BotConfig config, int shardId, CancellationToken cancellationToken)
        {
            var cluster = new ClusterService();
            var instanceId = $"shard{shardId}";
            var role = BotRole.Notifier.ToString().ToLowerInvariant();
            var interval = TimeSpan.FromSeconds(Math.Max(1, config.HeartbeatIntervalSeconds));
            // TTL 須明顯大於間隔，避免 GC 暫停導致誤判離線（與 scraper 同規則）
            var ttl = TimeSpan.FromSeconds(Math.Max(config.HeartbeatTtlSeconds, config.HeartbeatIntervalSeconds * 3));

            using var timer = new PeriodicTimer(interval);
            do
            {
                try { await cluster.WriteHeartbeatAsync(role, instanceId, ttl); }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { Log.Error(ex.Demystify(), "[Notifier] 寫入心跳失敗"); }
            }
            while (await SafeWaitAsync(timer, cancellationToken));
        }

        private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken cancellationToken)
        {
            try { return await timer.WaitForNextTickAsync(cancellationToken); }
            catch (OperationCanceledException) { return false; }
        }

        public static string GetLinkerTime(Assembly assembly)
        {
            const string BuildVersionMetadataPrefix = "+build";

            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (attribute?.InformationalVersion != null)
            {
                var value = attribute.InformationalVersion;
                var index = value.IndexOf(BuildVersionMetadataPrefix);
                if (index > 0)
                {
                    value = value[(index + BuildVersionMetadataPrefix.Length)..];
                    return value;
                }
            }
            return default;
        }
    }
}
