using Discord.Commands;
using Discord.Interactions;
using DiscordStreamNotifyBot.Command;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.HttpClients;
using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Shared;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using System.Reflection;

namespace DiscordStreamNotifyBot
{
    public class Bot
    {
        public static Stopwatch StopWatch { get; private set; } = new Stopwatch();

        // 以下共用狀態委派至 Shared.BotState（單一來源），讓移入 Shared 的偵測服務與本層共享同一份狀態
        public static ConnectionMultiplexer Redis { get => BotState.Redis; set => BotState.Redis = value; }
        public static ISubscriber RedisSub { get => BotState.RedisSub; set => BotState.RedisSub = value; }
        public static IDatabase RedisDb { get => BotState.RedisDb; set => BotState.RedisDb = value; }
        public static MainDbService DbService { get => BotState.DbService; private set => BotState.DbService = value; }

        public static IUser ApplicatonOwner { get => BotState.ApplicatonOwner; private set => BotState.ApplicatonOwner = value; }
        public static BotPlayingStatus Status { get; set; } = BotPlayingStatus.Guild;

        public static bool IsConnect { get => BotState.IsConnect; set => BotState.IsConnect = value; }
        public static bool IsDisconnect { get => BotState.IsDisconnect; set => BotState.IsDisconnect = value; }

        /// <summary>本程序負責的 Shard Id</summary>
        public static int ShardId { get => BotState.ShardId; private set => BotState.ShardId = value; }
        /// <summary>叢集的 Shard 總數</summary>
        public static int TotalShardCount { get => BotState.TotalShardCount; private set => BotState.TotalShardCount = value; }

        private static DiscordSocketClient client;
        private static Timer timerUpdateStatus;
        private static NotificationBusConsumer _busConsumer;

        public enum BotPlayingStatus { Guild, Member, Stream, StreamCount, Info }

        private readonly static BotConfig _botConfig = new();
        private readonly int _shardId;
        private readonly int _totalShardCount;

        public Bot(int shardId, int totalShardCount)
        {
            _shardId = shardId;
            _totalShardCount = totalShardCount;
            ShardId = shardId;
            TotalShardCount = totalShardCount;

            _botConfig.InitBotConfig();
            DbService = new MainDbService(_botConfig.MySqlConnectionString);
            timerUpdateStatus = new Timer(TimerHandler);

            Log.Info($"Shard {_shardId} / {_totalShardCount} 正在初始化...");

            try
            {
                RedisConnection.Init(_botConfig.RedisOption);
                Redis = RedisConnection.Instance.ConnectionMultiplexer;
                RedisSub = Redis.GetSubscriber();
                RedisDb = Redis.GetDatabase();

                Log.Info("Redis已連線");

                if (RedisSub.Publish(new RedisChannel("youtube.test", RedisChannel.PatternMode.Literal), "nope") != 0)
                {
                    Log.Info("Redis Sub已存在");
                }
                else
                {
                    Log.Warn("Redis Sub不存在，請開啟錄影工具");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Redis連線錯誤，請確認伺服器是否已開啟");
                Log.Error(ex.Message);
                return;
            }

            if (_shardId == 0)
                InitializeDatabase();
        }

        /// <summary>
        /// shard 0 啟動時的資料庫初始化（計畫 §11-2）。取代舊有 EnsureCreated（會建立無遷移歷史的庫）：
        /// <list type="bullet">
        /// <item>全新空庫 → <c>Migrate()</c> 依遷移建表並寫入歷史。</item>
        /// <item>既有庫但無 <c>__EFMigrationsHistory</c>（舊 EnsureCreated 建立）→ 不自動遷移（與舊行為一樣安全 no-op），
        /// 記錄提示請先執行 <c>Migrations/_Baseline_ExistingDb.sql</c> 基線化後再手動 <c>dotnet ef database update</c>。</item>
        /// <item>已有遷移歷史 → <c>Migrate()</c> 套用待處理遷移。</item>
        /// </list>
        /// </summary>
        private static void InitializeDatabase()
        {
            try
            {
                using var db = DbService.GetDbContext();
                var creator = (Microsoft.EntityFrameworkCore.Storage.RelationalDatabaseCreator)db.Database.GetService<Microsoft.EntityFrameworkCore.Storage.IDatabaseCreator>();

                if (!creator.HasTables())
                {
                    Log.Info("偵測到全新資料庫，依 EF 遷移建立 schema...");
                    db.Database.Migrate();
                    return;
                }

                if (!db.Database.GetAppliedMigrations().Any())
                {
                    Log.Warn("既有資料庫無 __EFMigrationsHistory（疑似舊版 EnsureCreated 建立）；本次略過自動遷移。");
                    Log.Warn("請先執行 src/DiscordStreamNotifyBot.Shared/Migrations/_Baseline_ExistingDb.sql 基線化後，再手動 dotnet ef database update（計畫 §11-2）。");
                    return;
                }

                db.Database.Migrate();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "InitializeDatabase 失敗");
            }
        }

        /// <summary>
        /// 在 <c>GetGuild(guildId) == null</c> 時判斷是否「真的」該刪除此伺服器的設定（委派 <see cref="BotState"/>）：
        /// 僅「歸屬本 Shard」且「已 Ready」才回傳 true，避免多 Shard 互刪設定。
        /// </summary>
        public static bool ShouldDeleteMissingGuild(ulong guildId) => BotState.ShouldDeleteMissingGuild(guildId);

        public async Task StartAndBlockAsync()
        {
            client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                ShardId = _shardId,
                TotalShards = _totalShardCount,
                LogLevel = LogSeverity.Verbose,
                ConnectionTimeout = int.MaxValue,
                MessageCacheSize = 0,
                // 因為沒有註冊事件，Discord .NET 建議可移除這兩個沒用到的特權
                // https://dotblogs.com.tw/yc421206/2015/10/20/c_scharp_enum_of_flags
                GatewayIntents = GatewayIntents.AllUnprivileged & ~GatewayIntents.GuildInvites & ~GatewayIntents.GuildScheduledEvents,
                AlwaysDownloadDefaultStickers = false,
                AlwaysResolveStickers = false,
                FormatUsersInBidirectionalUnicode = false,
                LogGatewayIntentWarnings = false,
            });

            #region 初始化Discord設定與事件
            client.Log += Log.LogMsg;

            client.Ready += async () =>
            {
                StopWatch.Start();
                timerUpdateStatus.Change(0, 15 * 60 * 1000);

                ApplicatonOwner = (await client.GetApplicationInfoAsync()).Owner;
                IsConnect = true;

                using (var db = DbService.GetDbContext())
                {
                    foreach (var guild in client.Guilds)
                    {
                        if (!await db.GuildConfig.AnyAsync(x => x.GuildId == guild.Id))
                        {
                            db.GuildConfig.Add(new GuildConfig() { GuildId = guild.Id });
                            await db.SaveChangesAsync();
                        }
                    }
                }

                // 寫入本 shard 伺服器快照，供跨 shard 讀取類指令彙總（B1）
                await SharedService.Cluster.ClusterQueryService.WriteGuildSnapshotAsync(client);
            };

            client.LeftGuild += (guild) =>
            {
                try
                {
                    Log.Info($"離開伺服器: {guild.Name}");

                    using (var db = DbService.GetDbContext())
                    {
                        GuildConfig guildConfig;
                        if ((guildConfig = db.GuildConfig.FirstOrDefault(x => x.GuildId == guild.Id)) != null)
                            db.GuildConfig.Remove(guildConfig);

                        IEnumerable<GuildYoutubeMemberConfig> guildYoutubeMemberConfigs;
                        if ((guildYoutubeMemberConfigs = db.GuildYoutubeMemberConfig.Where(x => x.GuildId == guild.Id)).Any())
                            db.GuildYoutubeMemberConfig.RemoveRange(guildYoutubeMemberConfigs);

                        IEnumerable<BannerChange> bannerChange;
                        if ((bannerChange = db.BannerChange.Where(x => x.GuildId == guild.Id)).Any())
                            db.BannerChange.RemoveRange(bannerChange);

                        IEnumerable<NoticeTwitcastingStreamChannel> noticeTwitCastingStreamChannels;
                        if ((noticeTwitCastingStreamChannels = db.NoticeTwitcastingStreamChannels.Where(x => x.GuildId == guild.Id)).Any())
                            db.NoticeTwitcastingStreamChannels.RemoveRange(noticeTwitCastingStreamChannels);

                        IEnumerable<NoticeTwitchStreamChannel> NoticeTwitchStreamChannels;
                        if ((NoticeTwitchStreamChannels = db.NoticeTwitchStreamChannels.Where(x => x.GuildId == guild.Id)).Any())
                            db.NoticeTwitchStreamChannels.RemoveRange(NoticeTwitchStreamChannels);

                        IEnumerable<NoticeYoutubeStreamChannel> noticeYoutubeStreamChannels;
                        if ((noticeYoutubeStreamChannels = db.NoticeYoutubeStreamChannel.Where(x => x.GuildId == guild.Id)).Any())
                            db.NoticeYoutubeStreamChannel.RemoveRange(noticeYoutubeStreamChannels);

                        IEnumerable<YoutubeMemberCheck> youtubeMemberChecks;
                        if ((youtubeMemberChecks = db.YoutubeMemberCheck.Where(x => x.GuildId == guild.Id)).Any())
                            db.YoutubeMemberCheck.RemoveRange(youtubeMemberChecks);

                        var saveTime = DateTime.Now;
                        bool saveFailed;

                        do
                        {
                            saveFailed = false;
                            try
                            {
                                db.SaveChanges();
                            }
                            catch (DbUpdateConcurrencyException ex)
                            {
                                saveFailed = true;
                                foreach (var item in ex.Entries)
                                {
                                    try
                                    {
                                        item.Reload();
                                    }
                                    catch (Exception ex2)
                                    {
                                        Log.Error($"LeftGuild-SaveChanges-Reload-{guild}");
                                        Log.Error(item.DebugView.ToString());
                                        Log.Error(ex2.ToString());
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"LeftGuild-SaveChanges-{guild}: {ex}");
                                Log.Error(db.ChangeTracker.DebugView.LongView);
                            }
                        } while (saveFailed && DateTime.Now.Subtract(saveTime) <= TimeSpan.FromMinutes(1));
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), $"LeftGuild-{guild}");
                }

                // 更新本 shard 伺服器快照（B1）
                _ = SharedService.Cluster.ClusterQueryService.WriteGuildSnapshotAsync(client);
                return Task.CompletedTask;
            };
            #endregion

#if DEBUG || RELEASE
            Log.Info("登入中...");

            try
            {
                await client.LoginAsync(TokenType.Bot, _botConfig.DiscordToken);
                await client.StartAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "Discord 登入失敗!");
                return;
            }

            do { await Task.Delay(200); }
            while (!IsConnect);

            Log.Info("登入成功!");

            UptimeKumaClient.Init(_botConfig.UptimeKumaPushUrl, client);
#endif

            #region 初始化指令系統
            var services = new ServiceCollection()
                .AddHttpClient()
                .AddSingleton(DbService)
                .AddSingleton<Shared.YoutubeApiService>()
                .AddSingleton<SharedService.EmojiService>()
                .AddSingleton<SharedService.Twitch.TwitchApiService>()
                .AddSingleton<SharedService.Twitch.TwitchService>()
                .AddSingleton<SharedService.Youtube.YoutubeStreamService>()
                .AddSingleton<SharedService.YoutubeMember.YoutubeMemberService>()
                .AddSingleton(client)
                .AddSingleton(_botConfig)
                .AddSingleton(new InteractionService(client, new InteractionServiceConfig()
                {
                    AutoServiceScopes = true,
                    UseCompiledLambda = true,
                    EnableAutocompleteHandlers = true,
                    DefaultRunMode = Discord.Interactions.RunMode.Async,
                    ExitOnMissingModalField = true
                }))
                .AddSingleton(new CommandService(new CommandServiceConfig()
                {
                    CaseSensitiveCommands = false,
                    DefaultRunMode = Discord.Commands.RunMode.Async
                }));

            //https://blog.darkthread.net/blog/polly/
            //HandleTransientHttpError 包含 5xx 及 408 錯誤
            services.AddHttpClient<DiscordWebhookClient>();
            services.AddHttpClient<TwitcastingClient>()
                .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .RetryAsync(3));

            services.LoadInteractionFrom(Assembly.GetAssembly(typeof(InteractionHandler)));
            services.LoadCommandFrom(Assembly.GetAssembly(typeof(CommandHandler)));

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            await serviceProvider.GetService<InteractionHandler>().InitializeAsync();
            await serviceProvider.GetService<CommandHandler>().InitializeAsync();
            #endregion

            #region 通知匯流排消費（Notifier 的通知一律來自 RabbitMQ；消費失敗 = 無法服務，直接結束交由重啟）
            try
            {
                _busConsumer = new NotificationBusConsumer(_botConfig,
                    serviceProvider.GetService<SharedService.Youtube.YoutubeStreamService>(),
                    serviceProvider.GetService<SharedService.Twitch.TwitchService>(),
                    serviceProvider.GetService<SharedService.Twitcasting.TwitcastingService>());
                await _busConsumer.StartAsync(_shardId);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "通知匯流排消費啟動失敗，Notifier 無法在沒有匯流排的情況下服務");
                Environment.Exit(1);
            }
            #endregion

            #region 註冊互動指令
            try
            {
                var commandCount = (await RedisDb.StringGetSetAsync("discord_stream_bot:command_count", serviceProvider.GetService<InteractionHandler>().CommandCount)).ToString();
                if (commandCount != serviceProvider.GetService<InteractionHandler>().CommandCount.ToString())
                {
                    InteractionService interactionService = serviceProvider.GetService<InteractionService>();
#if DEBUG
                    if (_botConfig.TestSlashCommandGuildId == 0 || client.GetGuild(_botConfig.TestSlashCommandGuildId) == null)
                        Log.Warn("未設定測試 Slash 指令的伺服器或伺服器不存在，略過");
                    else
                    {
                        try
                        {
                            var result = await interactionService.RegisterCommandsToGuildAsync(_botConfig.TestSlashCommandGuildId);
                            Log.Info($"已註冊指令 ({_botConfig.TestSlashCommandGuildId}) : {string.Join(", ", result.Select((x) => x.Name))}");

                            result = await interactionService.AddModulesToGuildAsync(_botConfig.TestSlashCommandGuildId, false, interactionService.Modules.Where((x) => x.DontAutoRegister).ToArray());
                            Log.Info($"已註冊指令 ({_botConfig.TestSlashCommandGuildId}) : {string.Join(", ", result.Select((x) => x.Name))}");
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "註冊伺服器專用 Slash 指令失敗");
                        }
                    }
#elif RELEASE
                    try
                    {
                        if (_botConfig.TestSlashCommandGuildId != 0 && client.GetGuild(_botConfig.TestSlashCommandGuildId) != null)
                        {
                            var result = await interactionService.RemoveModulesFromGuildAsync(_botConfig.TestSlashCommandGuildId, interactionService.Modules.Where((x) => !x.DontAutoRegister).ToArray());
                            Log.Info($"({_botConfig.TestSlashCommandGuildId}) 已移除測試指令，剩餘指令: {string.Join(", ", result.Select((x) => x.Name))}");
                        }
                        try
                        {
                            foreach (var item in interactionService.Modules.Where((x) => x.Preconditions.Any((x) => x is Interaction.Attribute.RequireGuildAttribute)))
                            {
                                var guildId = ((Interaction.Attribute.RequireGuildAttribute)item.Preconditions.Single((x) => x is Interaction.Attribute.RequireGuildAttribute)).GuildId;
                                var guild = client.GetGuild(guildId.Value);

                                if (guild == null)
                                {
                                    Log.Warn($"{item.Name} 註冊失敗，伺服器 {guildId} 不存在");
                                    continue;
                                }

                                var result = await interactionService.AddModulesToGuildAsync(guild, false, item);
                                Log.Info($"已在 {guild.Name}({guild.Id}) 註冊指令: {string.Join(", ", item.SlashCommands.Select((x) => x.Name))}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "註冊伺服器專用 Slash 指令失敗");
                        }

                        await interactionService.RegisterCommandsGloballyAsync();
                        Log.Info("已註冊全球指令");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "取得指令數量失敗，請確認 Redis 伺服器是否可以存取");
                        IsDisconnect = true;
                    }
#endif
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "註冊 Slash 指令失敗，關閉中...");
                IsDisconnect = true;
            }
            #endregion

            // 因為會用到 DiscordWebhookClient Service，所以沒辦法往上移動到 Region 內
            client.JoinedGuild += (guild) =>
            {
                Log.Info($"加入伺服器: {guild.Name}");

                var hasInvitePermission = guild.GetUser(client.CurrentUser.Id)?.GuildPermissions.CreateInstantInvite ?? false;
                if (!hasInvitePermission)
                {
                    //serviceProvider.GetService<DiscordWebhookClient>().SendMessageToDiscord($"加入 {guild.Name} ({guild.Id})\n" +
                    //    $"擁有者: {guild.OwnerId}\n" +
                    //    $"未開放邀請權限，已離開");
                    guild.LeaveAsync().GetAwaiter().GetResult();
                    return Task.CompletedTask;
                }

                serviceProvider.GetService<DiscordWebhookClient>().SendMessageToDiscord($"加入 {guild.Name}({guild.Id})\n" +
                    $"擁有者: {guild.OwnerId}");

                using (var db = DbService.GetDbContext())
                {
                    if (!db.GuildConfig.Any(x => x.GuildId == guild.Id))
                    {
                        db.GuildConfig.Add(new GuildConfig() { GuildId = guild.Id });
                        db.SaveChanges();
                    }
                }

                // 更新本 shard 伺服器快照（B1）
                _ = SharedService.Cluster.ClusterQueryService.WriteGuildSnapshotAsync(client);
                return Task.CompletedTask;
            };

            Log.Info("已初始化完成!");

            do { await Task.Delay(1000); }
            while (!IsDisconnect);

            await client.StopAsync();

            Redis.GetSubscriber().UnsubscribeAll();
        }

        private void TimerHandler(object state)
        {
            if (IsDisconnect) return;

            ChangeStatus();

            // 週期重寫本 shard 伺服器快照（B1；容忍 memberCount 漂移，管理用途足夠）
            _ = SharedService.Cluster.ClusterQueryService.WriteGuildSnapshotAsync(client);
        }

        /// <summary>
        /// 跨 shard 計數彙總（階段 5）：將本 shard 計數寫入 Redis HASH（field = shardId），
        /// 多 shard 時回傳全 shard 加總（僅計入 <c>[0, TotalShardCount)</c> 的欄位，避免縮容殘留干擾）；
        /// 單 shard 或 Redis 失敗時退回本機計數。
        /// </summary>
        private async Task<long> GetAggregatedShardCountAsync(string hashKey, long ownCount)
        {
            try
            {
                await RedisDb.HashSetAsync(hashKey, _shardId, ownCount);

                if (_totalShardCount <= 1)
                    return ownCount;

                long total = 0;
                foreach (var entry in await RedisDb.HashGetAllAsync(hashKey))
                {
                    if (int.TryParse(entry.Name, out int entryShardId) && entryShardId < _totalShardCount &&
                        entry.Value.TryParse(out long value))
                        total += value;
                }

                return total;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "GetAggregatedShardCountAsync");
                return ownCount;
            }
        }

        public void ChangeStatus()
        {
            Task.Run(async () =>
            {
                switch (Status)
                {
                    case BotPlayingStatus.Guild:
                        await client.SetCustomStatusAsync($"在 {await GetAggregatedShardCountAsync(Shared.RedisChannels.SharedState.GuildCountHash, client.Guilds.Count)} 個伺服器");
                        Status = BotPlayingStatus.Member;
                        break;
                    case BotPlayingStatus.Member:
                        try
                        {
                            await client.SetCustomStatusAsync($"服務 {await GetAggregatedShardCountAsync(Shared.RedisChannels.SharedState.MemberCountHash, client.Guilds.Sum((x) => x.MemberCount))} 個成員");
                            Status = BotPlayingStatus.Info;
                        }
                        catch (Exception) { Status = BotPlayingStatus.Stream; ChangeStatus(); }
                        break;
                    case BotPlayingStatus.Stream:
                        Status = BotPlayingStatus.StreamCount;
                        try
                        {
                            using var db = DbService.GetDbContext();

                            List<DataBase.Table.Video> list = null;
                            switch (new Random().Next(0, 2))
                            {
                                case 0:
                                    list = db.HoloVideos.AsNoTracking().Cast<DataBase.Table.Video>().ToList();
                                    break;
                                case 1:
                                    list = db.NijisanjiVideos.AsNoTracking().Cast<DataBase.Table.Video>().ToList();
                                    break;
                                case 2:
                                    list = db.OtherVideos.AsNoTracking().Cast<DataBase.Table.Video>().ToList();
                                    break;
                            }

                            var item = list[new Random().Next(0, list.Count)];
                            await client.SetGameAsync(item.VideoTitle, $"https://www.youtube.com/watch?v={item.VideoId}", ActivityType.Streaming);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.Demystify(), "ChangeStatus");
                            ChangeStatus();
                        }
                        break;
                    case BotPlayingStatus.StreamCount:
                        Status = BotPlayingStatus.Info;
                        await client.SetCustomStatusAsync($"看了 {Utility.GetDbStreamCount(DbService)} 個直播");
                        break;
                    case BotPlayingStatus.Info:
                        await client.SetCustomStatusAsync("去看你的直播啦");
                        Status = BotPlayingStatus.Guild;
                        break;
                }
            });
        }

    }
}
