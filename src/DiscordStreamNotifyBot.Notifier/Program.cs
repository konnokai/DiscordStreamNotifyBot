using DiscordStreamNotifyBot.Shared;
using System.Reflection;

namespace DiscordStreamNotifyBot
{
    public class Program
    {
        public static string Version => GetLinkerTime(Assembly.GetEntryAssembly());

        static async Task Main(string[] args)
        {
            Log.Info(Version + " 初始化中");
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.CancelKeyPress += Console_CancelKeyPress;

            // https://stackoverflow.com/q/5710148/15800522
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Exception ex = (Exception)e.ExceptionObject;

                try
                {
                    if (!Debugger.IsAttached)
                    {
                        StreamWriter sw = new StreamWriter($"{DateTime.Now:yyyy-MM-dd hh-mm-ss}_crash.log");
                        sw.WriteLine("### Bot Crash ###");
                        sw.WriteLine(ex.Demystify().ToString());
                        sw.Close();
                    }

                    Log.Error(ex.Demystify(), "UnhandledException", true, false);
                }
                finally
                {
                    Environment.Exit(1);
                }
            };

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

            // 啟動連線檢查（計畫 §5.3）：進入主邏輯前先確認 MySQL / Redis 可連線，失敗印訊息後 Exit(1)
            try
            {
                var preflightConfig = new BotConfig();
                preflightConfig.InitBotConfig(BotRole.Notifier);
                await StartupPreflight.EnsureAsync(BotRole.Notifier, preflightConfig, TimeSpan.FromSeconds(60));
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "StartupPreflight 失敗");
                Environment.Exit(1);
            }

            // 官方伺服器白名單改存 Redis（階段 5：跨 shard 共享）；首次啟動由舊 OfficialList.json 播種
            try
            {
                var redisDb = RedisConnection.Instance.ConnectionMultiplexer.GetDatabase();
                if (!await redisDb.KeyExistsAsync(Shared.RedisChannels.SharedState.OfficialGuildList) &&
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

            var bot = new Bot(shardId, totalShards);
            bot.StartAndBlockAsync().GetAwaiter().GetResult();
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Bot.IsDisconnect = true;
            e.Cancel = true;
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