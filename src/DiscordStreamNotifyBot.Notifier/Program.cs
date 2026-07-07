using DiscordStreamNotifyBot.Shared;
using System.Reflection;

namespace DiscordStreamNotifyBot
{
    public class Program
    {
        private const BotRole Role = BotRole.Notifier;

        public static string Version => GetLinkerTime(Assembly.GetEntryAssembly());

        static async Task Main(string[] args)
        {
            Log.Info(Version + " 初始化中");
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // 統一優雅關閉（SIGINT/SIGTERM，計畫 §9-1）：取消時橋接至既有的 Bot.IsDisconnect 關閉路徑
            GracefulShutdown.Init();
            GracefulShutdown.Token.Register(() => Bot.IsDisconnect = true);

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

            // Todo: 改 Shard 架構後需要同步清單給其他 Shard
            if (File.Exists(Utility.GetDataFilePath("OfficialList.json")))
            {
                try
                {
                    Utility.OfficialGuildList = JsonConvert.DeserializeObject<HashSet<ulong>>(File.ReadAllText(Utility.GetDataFilePath("OfficialList.json")));
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), "ReadOfficialListFile Error");
                    return;
                }
            }

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
            try
            {
                var preflightConfig = new BotConfig();
                preflightConfig.InitBotConfig();
                await StartupPreflight.EnsureAsync(Role, preflightConfig, TimeSpan.FromSeconds(60));
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "StartupPreflight 失敗");
                Environment.Exit(1);
            }

            var bot = new Bot(shardId, totalShards);
            bot.StartAndBlockAsync().GetAwaiter().GetResult();
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