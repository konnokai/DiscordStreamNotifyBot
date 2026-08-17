using Discord.Commands;
using Discord.Interactions;
using DiscordStreamNotifyBot.Command;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.HttpClients;
using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.Shared;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using System.Reflection;

namespace DiscordStreamNotifyBot
{
    public class Bot
    {
        public static Stopwatch StopWatch { get; private set; } = new Stopwatch();
        public static BotPlayingStatus Status { get; set; } = BotPlayingStatus.Guild;

        // 以下共用執行期狀態的真實來源已移至 Shared 的 BotState（階段 1），Bot 對應成員委派至此，
        // 舊有 Bot.XXX 呼叫端不需改動，Shared 內的工具（Utility 等）與後續偵測服務讀寫同一份狀態。
        public static ConnectionMultiplexer Redis { get => BotState.Redis; set => BotState.Redis = value; }
        public static ISubscriber RedisSub { get => BotState.RedisSub; set => BotState.RedisSub = value; }
        public static IDatabase RedisDb { get => BotState.RedisDb; set => BotState.RedisDb = value; }
        public static MainDbService DbService { get => BotState.DbService; private set => BotState.DbService = value; }

        public static IUser ApplicatonOwner { get => BotState.ApplicatonOwner; private set => BotState.ApplicatonOwner = value; }

        public static bool IsConnect { get => BotState.IsConnect; set => BotState.IsConnect = value; }
        public static bool IsDisconnect { get => BotState.IsDisconnect; set => BotState.IsDisconnect = value; }
        public static bool IsHoloChannelSpider { get => BotState.IsHoloChannelSpider; set => BotState.IsHoloChannelSpider = value; }
        public static bool IsNijisanjiChannelSpider { get => BotState.IsNijisanjiChannelSpider; set => BotState.IsNijisanjiChannelSpider = value; }
        public static bool IsOtherChannelSpider { get => BotState.IsOtherChannelSpider; set => BotState.IsOtherChannelSpider = value; }

        public static int ShardId { get => BotState.ShardId; private set => BotState.ShardId = value; }
        public static int TotalShardCount { get => BotState.TotalShardCount; private set => BotState.TotalShardCount = value; }

        public static bool IsServerOnThisShard(ulong guildId) => BotState.IsServerOnThisShard(guildId);

        public static bool ShouldDeleteMissingGuild(ulong guildId) => BotState.ShouldDeleteMissingGuild(guildId);

        private static DiscordSocketClient client;
        private static Timer timerUpdateStatus;
        private static NotificationBusConsumer _busConsumer;

        public enum BotPlayingStatus { Guild, Member, Stream, StreamCount, Info }

        private readonly static BotConfig _botConfig = new();
        private readonly int _shardId;
        private readonly int _totalShardCount;
        private readonly NotifierMetrics _metrics;

        internal Bot(int shardId, int totalShardCount, NotifierMetrics metrics)
        {
            _shardId = shardId;
            _totalShardCount = totalShardCount;
            _metrics = metrics;
            ShardId = shardId;
            TotalShardCount = totalShardCount;

            _botConfig.InitBotConfig();
            DbService = new MainDbService(_botConfig.MySqlConnectionString);
            timerUpdateStatus = new Timer(TimerHandler);

            Log.Info($"Shard {_shardId} / {_totalShardCount} 正在初始化⋯⋯");

            try
            {
                RedisConnection.Init(_botConfig.RedisOption);
                Redis = RedisConnection.Instance.ConnectionMultiplexer;
                RedisSub = Redis.GetSubscriber();
                RedisDb = Redis.GetDatabase();

                // 必須在 Discord 登入前訂閱，確保任一 shard 發現 token 失效時，其他仍在線的 shard 也會立即斷線。
                RedisSub.Subscribe(new RedisChannel(RedisChannels.Notifier.Shutdown, RedisChannel.PatternMode.Literal), (_, value) =>
                {
                    Log.Info($"收到關閉廣播，準備關閉本 shard: {value}");
                    IsDisconnect = true;
                });

                Log.Info("Redis 已連線");

                if (RedisSub.Publish(new RedisChannel("youtube.test", RedisChannel.PatternMode.Literal), "nope") != 0)
                {
                    Log.Info("Redis Sub 已存在");
                }
                else
                {
                    Log.Warn("Redis Sub 不存在，請開啟錄影工具");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Redis 連線錯誤，請確認伺服器是否已開啟");
                Log.Error(ex.Message);
                return;
            }

            // 不在啟動時建立/遷移資料庫：正式 DB 已基線化，禁用 EnsureCreated（會建立無遷移歷史的庫）；
            // 遷移一律用 Script-Migration 產生冪等 SQL、人工審核後於維護窗口手動套用（見 CLAUDE.md EF 鐵則）。
            // 本地/開發庫請自行 dotnet ef database update。
        }

        public async Task StartAndBlockAsync()
        {
            client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                ShardId = _shardId,
                TotalShards = _totalShardCount,
                LogLevel = Debugger.IsAttached ? LogSeverity.Debug : LogSeverity.Info,
                ConnectionTimeout = int.MaxValue,
                MessageCacheSize = 0,
                // 未使用邀請與排程活動事件，因此不請求 GuildInvites 與 GuildScheduledEvents intent。
                // 會員重加入即時回補 / 孤兒身分組對帳需要 GuildMembers 特權 intent，僅在設定啟用時才請求，
                // 否則未在 Discord 後台開特權會導致 login 4014 disallowed intent 連線失敗。
                GatewayIntents = GatewayIntents.AllUnprivileged & ~GatewayIntents.GuildInvites & ~GatewayIntents.GuildScheduledEvents
                    | (_botConfig.EnableGuildMembersIntent ? GatewayIntents.GuildMembers : GatewayIntents.None),
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

                // 寫入本 shard 伺服器快照，供跨 shard 讀取類指令彙總（B1，計畫 §7）
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

                        IEnumerable<TwitchSubscriptionCheck> twitchSubscriptionChecks;
                        if ((twitchSubscriptionChecks = db.TwitchSubscriptionCheck.Where(x => x.GuildId == guild.Id)).Any())
                            db.TwitchSubscriptionCheck.RemoveRange(twitchSubscriptionChecks);

                        IEnumerable<GuildTwitchSubscriptionConfig> guildTwitchSubscriptionConfigs;
                        if ((guildTwitchSubscriptionConfigs = db.GuildTwitchSubscriptionConfig.Where(x => x.GuildId == guild.Id)).Any())
                            db.GuildTwitchSubscriptionConfig.RemoveRange(guildTwitchSubscriptionConfigs);

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
            Log.Info("登入中⋯⋯");

            try
            {
                await client.LoginAsync(TokenType.Bot, _botConfig.DiscordToken);
                await client.StartAsync();
            }
            catch (Discord.Net.HttpException ex) when (TryShutdownOnDiscordAuthorizationFailure(ex, "Discord 登入"))
            {
                return;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "Discord 登入失敗！");
                return;
            }

            do { await Task.Delay(200); }
            while (!IsConnect);

            Log.Info("登入成功！");

            UptimeKumaClient.Init(_botConfig.UptimeKumaPushUrl, client);
#endif

            #region 初始化指令系統
            var commandLocalizationManager = new DescriptionOnlyLocalizationManager();
            var services = new ServiceCollection()
                .AddHttpClient()
                .AddSingleton(DbService)
                .AddSingleton<BotLocalizer>()
                .AddSingleton<CommandDisplayResolver>()
                .AddSingleton<LocaleResolver>()
                .AddSingleton<GuildLocaleService>()
                .AddSingleton(_metrics)
                .AddSingleton<Shared.YoutubeApiService>()
                .AddSingleton(SharedService.Google.GoogleOAuthOperationLock.Create(Redis))
                .AddSingleton<SharedService.EmojiService>()
                .AddSingleton<SharedService.Twitch.TwitchApiService>()
                .AddSingleton<SharedService.Twitch.TwitchService>()
                .AddSingleton<SharedService.TwitchSubscription.TwitchSubscriptionApiClient>()
                .AddSingleton<SharedService.Member.MemberOperationCoordinator>()
                .AddSingleton<SharedService.Member.MemberRoleOwnershipService>()
                .AddSingleton<SharedService.YoutubeMember.YoutubeMemberRoleService>()
                .AddSingleton<SharedService.YoutubeMember.YoutubeMemberApiClient>()
                .AddSingleton<SharedService.YoutubeMember.YoutubeMemberAuthorizationService>()
                .AddSingleton<SharedService.TwitchSubscription.TwitchAuthorizationTokenService>()
                .AddSingleton<SharedService.TwitchSubscription.TwitchSubscriptionRoleService>()
                .AddSingleton<SharedService.TwitchSubscription.TwitchSubscriptionService>()
                .AddSingleton<SharedService.Youtube.YoutubeStreamService>()
                .AddSingleton<SharedService.YoutubeMember.YoutubeMemberService>()
                .AddSingleton<SharedService.AdminSettings.AdminSettingsService>()
                .AddSingleton(client)
                .AddSingleton(_botConfig)
                .AddSingleton(new InteractionService(client, new InteractionServiceConfig()
                {
                    AutoServiceScopes = true,
                    UseCompiledLambda = true,
                    EnableAutocompleteHandlers = true,
                    DefaultRunMode = Discord.Interactions.RunMode.Async,
                    ExitOnMissingModalField = true,
                    LocalizationManager = commandLocalizationManager
                }))
                .AddSingleton(new CommandService(new CommandServiceConfig()
                {
                    CaseSensitiveCommands = false,
                    DefaultRunMode = Discord.Commands.RunMode.Async
                }));

            //https://blog.darkthread.net/blog/polly/
            //HandleTransientHttpError 包含 5xx 及 408 錯誤
            services.AddHttpClient<DiscordWebhookClient>();
            services.AddHttpClient(
                SharedService.TwitchSubscription.TwitchSubscriptionApiClient.HttpClientName,
                client => client.Timeout = TimeSpan.FromSeconds(30));
            services.AddHttpClient<TwitcastingClient>()
                .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .RetryAsync(3));

            services.LoadInteractionFrom(Assembly.GetAssembly(typeof(InteractionHandler)));
            services.LoadCommandFrom(Assembly.GetAssembly(typeof(CommandHandler)));

            IServiceProvider serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
            await serviceProvider.GetRequiredService<InteractionHandler>().InitializeAsync();
            await serviceProvider.GetRequiredService<CommandHandler>().InitializeAsync();
            var twitchSubscriptionService = serviceProvider
                .GetRequiredService<SharedService.TwitchSubscription.TwitchSubscriptionService>();
            twitchSubscriptionService.Start();
            serviceProvider.GetRequiredService<SharedService.YoutubeMember.YoutubeMemberService>().Start();
            serviceProvider.GetRequiredService<SharedService.AdminSettings.AdminSettingsService>().Start();
            #endregion

            #region 通知匯流排消費（Notifier 的通知一律來自 bot:notify Redis Stream；消費啟動失敗 = 無法服務，直接結束交由重啟）
            try
            {
                _busConsumer = new NotificationBusConsumer(
                    serviceProvider.GetService<SharedService.Youtube.YoutubeStreamService>(),
                    serviceProvider.GetService<SharedService.Twitch.TwitchService>(),
                    serviceProvider.GetService<SharedService.Twitcasting.TwitcastingService>(),
                    serviceProvider.GetService<SharedService.YoutubeMember.YoutubeMemberService>(),
                    _metrics);
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
                InteractionService interactionService = serviceProvider.GetService<InteractionService>();
                // 以「指令規格雜湊」判斷是否需重註冊：只要 Slash 指令的名稱/參數/型別等規格有變動，雜湊即改變（取代僅比對指令總數）
                InteractionHandler interactionHandler = serviceProvider.GetService<InteractionHandler>();
#if DEBUG
                string debugGuildSignature = string.Join(",", _botConfig.TestSlashCommandGuildIds.OrderBy(id => id));
                string localCommandSignature = $"{interactionHandler.DebugCommandSignature}:{_totalShardCount}:{debugGuildSignature}";
#else
                string localCommandSignature = interactionHandler.CommandSignature;
#endif
#if DEBUG
                // 雜湊鍵帶 shardId：多 shard 併跑時若共用同一鍵，先啟動的 shard 會把雜湊設成最新，其餘 shard 讀到相同值而整個略過註冊，
                // 導致自己持有的測試伺服器沒有指令（正是 shard 1 沒指令的原因）。每個 shard 各自維護雜湊才能各自註冊自己持有的伺服器。
                string commandSignatureKey = $"discord_stream_bot:command_signature:{_shardId}";
                var commandSignature = (await RedisDb.StringGetAsync(commandSignatureKey)).ToString();
                if (commandSignature != localCommandSignature)
                {
                    if (_botConfig.TestSlashCommandGuildIds.Length == 0)
                        Log.Warn("未設定測試 Slash 指令的伺服器，略過");
                    else
                    {
                        bool registrationSucceeded = true;
                        bool registeredAnyGuild = false;
                        foreach (var guildId in _botConfig.TestSlashCommandGuildIds)
                        {
                            // 只註冊本 shard 持有的伺服器；其餘交由持有它的 shard 註冊（非錯誤，不記警告避免各 shard 洗版）
                            if (client.GetGuild(guildId) == null)
                                continue;

                            registeredAnyGuild = true;
                            try
                            {
                                var result = await interactionService.RegisterCommandsToGuildAsync(guildId);
                                Log.Info($"已註冊指令 ({guildId}) : {string.Join(", ", result.Select((x) => x.Name))}");

                                result = await interactionService.AddModulesToGuildAsync(guildId, false, interactionService.Modules.Where((x) => x.DontAutoRegister).ToArray());
                                Log.Info($"已註冊指令 ({guildId}) : {string.Join(", ", result.Select((x) => x.Name))}");
                            }
                            catch (Exception ex)
                            {
                                registrationSucceeded = false;
                                Log.Error(ex, $"註冊伺服器專用 Slash 指令失敗 ({guildId})");
                            }
                        }

                        if (registrationSucceeded && registeredAnyGuild)
                            await RedisDb.StringSetAsync(commandSignatureKey, localCommandSignature);
                    }
                }
#elif RELEASE
                // 全球指令對所有伺服器生效、與 shard 無關，且註冊有速率限制、生效慢：只由 shard 0 在指令規格變更時重註冊
                if (_shardId == 0)
                {
                    try
                    {
                        const string commandSignatureKey = "discord_stream_bot:command_signature";
                        var commandSignature = (await RedisDb.StringGetAsync(commandSignatureKey)).ToString();
                        if (commandSignature != localCommandSignature)
                        {
                            await interactionService.RegisterCommandsGloballyAsync();
                            await RedisDb.StringSetAsync(commandSignatureKey, localCommandSignature);
                            Log.Info("已註冊全球指令");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "註冊全球 Slash 指令失敗，請確認 Redis 伺服器是否可以存取");
                        IsDisconnect = true;
                    }
                }

                // 伺服器專屬指令（RequireGuild / DontAutoRegister）需 GetGuild 取得該伺服器，只有「持有它的 shard」能註冊，與 shard 0 無關；
                // 故不走上面的全域 command_count 閘門（否則只有其中一個 shard 會執行），每次啟動時各 shard 自行處理自己持有的伺服器。
                try
                {
                    foreach (var guildId in _botConfig.TestSlashCommandGuildIds)
                    {
                        if (client.GetGuild(guildId) == null)
                            continue;

                        var result = await interactionService.RemoveModulesFromGuildAsync(guildId, interactionService.Modules.Where((x) => !x.DontAutoRegister).ToArray());
                        Log.Info($"({guildId}) 已移除測試指令，剩餘指令: {string.Join(", ", result.Select((x) => x.Name))}");
                    }

                    foreach (var item in interactionService.Modules.Where((x) => x.Preconditions.Any((x) => x is Interaction.Attribute.RequireGuildAttribute)))
                    {
                        var guildId = ((Interaction.Attribute.RequireGuildAttribute)item.Preconditions.Single((x) => x is Interaction.Attribute.RequireGuildAttribute)).GuildId;
                        var guild = client.GetGuild(guildId.Value);

                        if (guild == null)
                            continue; // 該伺服器不在本 shard，交由持有它的 shard 註冊（非錯誤，故不記警告避免每個 shard 洗版）

                        var result = await interactionService.AddModulesToGuildAsync(guild, false, item);
                        Log.Info($"已在 {guild.Name}({guild.Id}) 註冊指令: {string.Join(", ", item.SlashCommands.Select((x) => x.Name))}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "註冊伺服器專用 Slash 指令失敗");
                }
#endif
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "註冊 Slash 指令失敗，關閉中⋯⋯");
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

            Log.Info("已完成初始化！");

            do { await Task.Delay(1000); }
            while (!IsDisconnect);

            while (IsHoloChannelSpider || IsOtherChannelSpider)
            {
                List<string> str = new List<string>();

                if (IsHoloChannelSpider) str.Add("Holo");
                if (IsOtherChannelSpider) str.Add("Other");

                Log.Info($"等待 {string.Join(", ", str)} 完成");
                await Task.Delay(5000);
            }

            await serviceProvider.GetRequiredService<SharedService.YoutubeMember.YoutubeMemberService>().StopAsync();
            await twitchSubscriptionService.StopAsync();
            await serviceProvider
                .GetRequiredService<SharedService.TwitchSubscription.TwitchAuthorizationTokenService>()
                .StopAsync();
            await client.StopAsync();

            Redis.GetSubscriber().UnsubscribeAll();
            // 偵測資料庫保存改由 Scraper（DetectionHost.SaveStateBeforeShutdown）負責，Notifier 不再處理。
        }

        /// <summary>
        /// Discord token 被重設後，既有 Gateway 連線可能不會立刻中斷，只會在下一次 REST 呼叫收到授權錯誤。
        /// 一旦確認為 token 層級錯誤，立即廣播所有 Notifier shard 一起關閉，交由部署層重新啟動。
        /// </summary>
        public static bool TryShutdownOnDiscordAuthorizationFailure(Discord.Net.HttpException exception, string source)
        {
            bool isUnauthorized = exception.HttpCode == HttpStatusCode.Unauthorized;
            bool isUnclassifiedForbidden = exception.HttpCode == HttpStatusCode.Forbidden && !exception.DiscordCode.HasValue;
            if (!isUnauthorized && !isUnclassifiedForbidden)
                return false;

            Log.Error(exception.Demystify(), $"{source} 偵測到 Discord Bot Token 授權失效，廣播關閉所有 Notifier shard");
            IsDisconnect = true;

            try
            {
                RedisSub?.Publish(
                    new RedisChannel(RedisChannels.Notifier.Shutdown, RedisChannel.PatternMode.Literal),
                    $"Discord 授權失效，來源 shard {ShardId}");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "廣播 Discord 授權失效關閉訊號失敗");
            }

            return true;
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
                        await client.SetCustomStatusAsync($"看了 {Utility.GetDbStreamCount()} 個直播");
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
