# Graph Report - DiscordStreamNotifyBot  (2026-07-28)

## Corpus Check
- 259 files · ~123,447 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 3057 nodes · 6681 edges · 210 communities (173 shown, 37 thin omitted)
- Extraction: 92% EXTRACTED · 8% INFERRED · 0% AMBIGUOUS · INFERRED: 523 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `d00e0799`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Admin Broadcast Commands
- YouTube Stream Commands
- Twitch Commands
- Twitcasting Service & DbContext
- Solution & Dependencies
- Help & Owner Services
- Notification Bus Consumer
- Help Autocomplete Handlers
- EF Migrations
- Precondition Attributes
- Command Handler
- Embed Builder Factory
- Scaling Architecture Docs
- Interaction Extensions
- Command Help Module
- Video/Embed Extensions
- SharedService Core
- YouTube Detection Service
- YouTube Slash Commands
- Bot Startup & Membership
- Auth / Token Crypto
- Bot Entry Points
- YouTube Reminder Scheduler
- Interaction Handler
- Command/Interaction Modules
- YouTube Member Service
- Notice Cache & Messaging
- YouTube Reminder Timer
- Command Attributes
- Graphify Tooling Docs
- Logging
- Cluster Leader/Heartbeat
- Member Check Settings
- YouTube Spider Commands
- Twitch Channel Commands
- Twitcasting Detection
- Shared Extensions
- Bot State & Timers
- Coordinator Entry/Shutdown
- YouTube Member Commands
- Twitcasting Commands
- YouTube Member Interaction
- DB Query Extensions
- Coordinator Service
- Twitcasting Spider Commands
- Redis Channels
- Twitch Spider Commands
- Nijisanji Stream JSON
- Utility & Official Guilds
- Detection Host Bootstrap
- YouTube Channel Spider
- Twitcasting HTTP Client
- Twitch Update Debounce
- YoutubeApiService
- TwitchDetectionService.cs
- Redis Token Provisioner
- TwitcastingService
- CLAUDE.md
- Twitcasting Backend Model
- Startup Preflight
- Twitcasting Webhook Models
- Broadcast Message Command
- TwitCasting Autocomplete
- Twitch Autocomplete
- YouTube Autocomplete
- Notifier Program Entry
- DiscordStreamNotifyBot.HttpClients.Twitcasting.Model
- Interaction Base Module
- TwitCasting DB Fix Command
- Twitcasting Movie Info
- .FixTCDbAsync
- DbContext Factory
- Twitcasting Categories JSON
- Nijisanji Liver JSON
- TwitCasting Webhook JSON
- README & Help Docs
- Funding Config
- CI Build Workflow
- License
- Bot Logo Image
- DiscordSocketClient
- .PublishYoutubeNotificationAsync
- ITextChannel
- IUserMessage
- TwitcastingDetectionService
- string
- TcBackendStreamData.cs
- SyncModelDrift
- .SendConfirmMessageAsync
- CommandTextEqualityComparer
- YoutubePubSubNotification
- 20250320095452_RefactorDbContext.Designer.cs
- Program
- 20250620094111_AddMaxSpiderCountSettingField.Designer.cs
- 20250320095452_RefactorDbContext.Designer.cs
- 20250603065853_ModifyTwitCastingTable.Designer.cs
- 20250620094111_AddMaxSpiderCountSettingField.Designer.cs
- TwitchSpider
- RecordYoutubeChannel
- graphify reference: GitHub clone and cross-repo merge
- graphify reference: transcribe video and audio
- graphify
- extraction-spec.md
- net8.0
- .claude/CLAUDE.md (graphify trigger)
- .LoadInteractionFrom
- DiscordSocketClient
- Confidence rubric (EXTRACTED/INFERRED/AMBIGUOUS)
- AST structural extraction (Part A)
- Community detection & clustering
- God nodes & surprising connections
- Knowledge graph (graph.json)
- Semantic extraction (parallel subagents)
- NoticeCache
- StreamGroupInfo
- string
- CancellationToken
- .OnReaction
- .EvaluateAsync
- RedisConnection
- string
- MainDbContextFactory
- Category
- TwitchApiResults.cs
- 16. 執行階段
- Prometheus / Grafana 監控
- .DispatchFromBusAsync
- DiscordStreamNotifyBot.sln
- DiscordWebhookClient
- 17. 驗證矩陣
- YoutubeChannelOwnedType
- GetAllRegistedWebHookJson.cs
- 7. OAuth API 與流程隔離
- .LoadCommandFrom
- RecordYoutubeChannel
- AddTwitchBroadcasterAuthorization
- HelpDescription (bot feature summary)
- NijisanjiLiverJson.cs
- 11. Bot EventSub 與偵測
- 15. 預期修改檔案
- TwitcastingDetectionService
- 5. Guild 資格與 OAuth 豁免
- DiscordStreamNotifyBot.Coordinator.csproj
- 20260719142803_AddTwitchBroadcasterAuthorization.Designer.cs
- 13. Prometheus
- 4. 安全刪除狀態機
- CLAUDE.md
- 20250620094111_AddMaxSpiderCountSettingField.Designer.cs
- MainDbContextModelSnapshot.cs
- .LoadCommandFrom
- RefactorDbContext
- ModifyTwitCastingTable
- AddMaxSpiderCountSettingField
- SyncModelDrift
- AddTwitchBroadcasterAuthorization
- AddLocalizationSettings
- 5. Shard 歸屬與生命週期
- .SendPaginatedConfirmAsync
- .CheckMemberShipOnlyVideoIdAsync
- 20250603065853_ModifyTwitCastingTable.Designer.cs
- 20250620094111_AddMaxSpiderCountSettingField.Designer.cs
- 20260611015819_SyncModelDrift.Designer.cs
- 20260709091318_AddManualMemberCheckVideoFlag.Designer.cs
- 20260719142803_AddTwitchBroadcasterAuthorization.Designer.cs
- 20260721095646_AddLocalizationSettings.Designer.cs
- .GetStreamVideoByVideoId
- HoloVideos
- NonApprovedVideos
- InteractionErrorPolicyTests
- .Resolve
- .GuildMemberCountPreconditionMapsValuesAndContactPath
- .GenerateSuggestionsAsync
- .GenerateSuggestionsAsync
- .GenerateSuggestionsAsync
- 5. 目標架構
- 5. Shard 歸屬與生命週期
- 7. 資料庫變更
- 9. Slash Command Localization
- GetAllRegistedWebHookJson.cs
- LogEvent
- .PublishAsync
- .GetNowStreamingChannel
- .GuildMemberCountPreconditionMapsValuesAndContactPath
- MainDbContextFactory
- ReminderItem
- YoutubePubSubNotification
- .GetStreamVideoByVideoId
- YoutubeChannelOwnedType
- GetAllRegistedWebHookJson.cs
- .HandleStreamStartedAsync
- .GetCommandHelp
- .CreateStreamEnded
- TwitcastingLiveStartPlanner.cs
- Normal
- .FilterNoNotifyGuilds
- .AddChannelSpider
- RedisDataStore
- DiscordStreamNotifyBot.SharedService.Cluster
- .GetLocaleAsync
- .DecideAutomaticMutation
- TcBackendStreamData.cs
- GetMovieInfoResponse
- .FixTCDbAsync
- .CreateStreamStarted
- ITokenDataStore
- GuildSnapshot
- GetAllRegistedWebHookJson.cs
- .TokenManagerRejectsBadTokenFormat
- OtherVideos
- 9. Slash Command Localization

## God Nodes (most connected - your core abstractions)
1. `TwitchDetectionService` - 53 edges
2. `BotLocalizer` - 52 edges
3. `DiscordStreamNotifyBot.DataBase.Table` - 52 edges
4. `DiscordStreamNotifyBot.DataBase` - 47 edges
5. `DiscordStreamNotifyBot.Shared` - 44 edges
6. `Video` - 41 edges
7. `Log` - 41 edges
8. `MainDbContext` - 38 edges
9. `MainDbService` - 36 edges
10. `DiscordStreamNotifyBot.Tests` - 35 edges

## Surprising Connections (you probably didn't know these)
- `InteractionMetadataFixture` --references--> `InteractionHandler`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/InteractionMetadataFixture.cs → src/DiscordStreamNotifyBot.Notifier/Interaction/InteractionHandler.cs
- `BotLocalizerTests` --references--> `BotLocalizer`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/BotLocalizerTests.cs → src/DiscordStreamNotifyBot.Notifier/Localization/BotLocalizer.cs
- `NotificationEmbedFactoryTests` --references--> `BotLocalizer`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/NotificationEmbedFactoryTests.cs → src/DiscordStreamNotifyBot.Notifier/Localization/BotLocalizer.cs
- `YoutubeMemberVideoLogMessageFormatterTests` --references--> `BotLocalizer`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/YoutubeMemberVideoLogMessageFormatterTests.cs → src/DiscordStreamNotifyBot.Notifier/Localization/BotLocalizer.cs
- `YoutubeMemberVideoLogMessageFormatterTests` --references--> `CommandDisplayResolver`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/YoutubeMemberVideoLogMessageFormatterTests.cs → src/DiscordStreamNotifyBot.Notifier/Localization/CommandDisplayResolver.cs

## Import Cycles
- None detected.

## Communities (210 total, 37 thin omitted)

### Community 0 - "Admin Broadcast Commands"
Cohesion: 0.09
Nodes (12): BannerChange, DateTime, DbEntity, GuildConfig, GuildYoutubeMemberConfig, NoticeTwitcastingStreamChannel, NoticeTwitchStreamChannel, NoticeYoutubeStreamChannel (+4 more)

### Community 1 - "YouTube Stream Commands"
Cohesion: 0.19
Nodes (14): IDatabase, int, RedisKey, RedisValue, StreamEntry, StreamGroupInfo, string, Task (+6 more)

### Community 2 - "Twitch Commands"
Cohesion: 0.08
Nodes (24): For /graphify add and --watch, For /graphify query, For the commit hook and native CLAUDE.md integration, For --update and --cluster-only, /graphify, Honesty Rules, Interpreter guard for subcommands, Part A - Structural extraction for code files (+16 more)

### Community 3 - "Twitcasting Service & DbContext"
Cohesion: 0.20
Nodes (11): ConfigurationOptions, FactAttribute, ICollectionFixture, ConnectionMultiplexer, IDatabase, RedisKey, string, Task (+3 more)

### Community 4 - "Solution & Dependencies"
Cohesion: 0.08
Nodes (23): Microsoft.EntityFrameworkCore.Design (9.0.3), Microsoft.EntityFrameworkCore.Relational (9.0.3), Microsoft.EntityFrameworkCore.Tools (9.0.3), Serilog (4.4.0), Serilog.Sinks.Console (6.1.1), Serilog.Sinks.File (7.0.0), Serilog.Sinks.Grafana.Loki (9.0.1), net8.0 (+15 more)

### Community 5 - "Help & Owner Services"
Cohesion: 0.11
Nodes (17): Clip, DateTime, EventSubSubscription, HttpClient, IReadOnlyList, Lazy, Regex, SemaphoreSlim (+9 more)

### Community 6 - "Notification Bus Consumer"
Cohesion: 0.07
Nodes (27): DisplayName, IResult, ISet, SocketInteraction, SocketSlashCommandDataOption, Dictionary, DiscordSocketClient, Func (+19 more)

### Community 7 - "Help Autocomplete Handlers"
Cohesion: 0.13
Nodes (13): 1. Shared — 定義契約, 2. Scraper — 偵測並 publish, 3. Notifier — 消費並發送, 動工前先讀一個既有平台, 收尾檢查, 新增偵測平台 / 通知事件, 步驟（依相依順序，Shared → Scraper → Notifier）, 偵測 → 匯流排 → 發送 路徑除錯 (+5 more)

### Community 8 - "EF Migrations"
Cohesion: 0.19
Nodes (7): string, Uri, YoutubeVideoIdParser, InlineData, string, Theory, YoutubeVideoIdParserTests

### Community 9 - "Precondition Attributes"
Cohesion: 0.25
Nodes (6): CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task, RequireGuildMemberCountAttribute

### Community 10 - "Command Handler"
Cohesion: 0.09
Nodes (19): DateTime, TimeSpan, YoutubeReminderApiAction, YoutubeReminderBatchChangeAction, YoutubeReminderBatchFacts, YoutubeReminderPolicy, YoutubeReminderReconciliationAction, YoutubeReminderStartAction (+11 more)

### Community 11 - "Embed Builder Factory"
Cohesion: 0.17
Nodes (13): Dictionary, Regex, ResourceManager, string, BotLocalizer, DateTime, EmbedBuilder, TimeSpan (+5 more)

### Community 12 - "Scaling Architecture Docs"
Cohesion: 0.08
Nodes (18): DiscordStreamNotifyBot.SharedService.Youtube, DiscordStreamNotifyBot.SharedService.Twitcasting, DiscordStreamNotifyBot.SharedService.YoutubeMember, DiscordStreamNotifyBot.Localization, DiscordStreamNotifyBot.Interaction.YoutubeMember, DiscordStreamNotifyBot.SharedService.Twitch, DiscordStreamNotifyBot.DataBase, DiscordStreamNotifyBot.Interaction (+10 more)

### Community 13 - "Interaction Extensions"
Cohesion: 0.08
Nodes (18): Process, Assembly, DiscordSocketClient, EmbedBuilder, Func, IDiscordInteraction, IEmote, IEnumerable (+10 more)

### Community 14 - "Command Help Module"
Cohesion: 0.18
Nodes (19): DiscordStreamNotifyBot.Command.YoutubeMember, ICommandService, Alias, Command, CommandExample, RequireContext, RequireOwner, Summary (+11 more)

### Community 15 - "Video/Embed Extensions"
Cohesion: 0.14
Nodes (14): SocketCommandContext, DateTime, DiscordSocketClient, EmbedBuilder, Func, ICommandContext, IEmote, IMessage (+6 more)

### Community 16 - "SharedService Core"
Cohesion: 0.18
Nodes (8): DiscordStreamNotifyBot.Command, SocketMessage, CommandService, DiscordSocketClient, IServiceProvider, Task, CommandHandler, ICommandService

### Community 17 - "YouTube Detection Service"
Cohesion: 0.11
Nodes (18): Backend, Bot（本 repo）, MySQL（兩端都已連同一個庫）, 儲存層（現況為 Redis）, 加密與 blob 格式（兩端一致）, 加密金鑰處理, 影響檔案一覽, 待決策（給實作 session） (+10 more)

### Community 18 - "YouTube Slash Commands"
Cohesion: 0.28
Nodes (12): CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, IChannel, NoticeType, RequireBotPermission, RequireContext (+4 more)

### Community 19 - "Bot Startup & Membership"
Cohesion: 0.15
Nodes (12): ButtonCheckData, DiscordStreamNotifyBot.Interaction.OwnerOnly.Service, SendAllPayload, bool, DiscordSocketClient, Embed, Task, ButtonCheckData (+4 more)

### Community 20 - "Auth / Token Crypto"
Cohesion: 0.07
Nodes (15): DiscordStreamNotifyBot.Auth, DiscordStreamNotifyBot, TokenCrypto, TokenManager, HashSet, List, string, Task (+7 more)

### Community 21 - "Bot Entry Points"
Cohesion: 0.11
Nodes (17): MessageComponent, NowStreamingHost, DiscordSocketClient, Embed, EmojiService, HttpClient, IEnumerable, IHttpClientFactory (+9 more)

### Community 22 - "YouTube Reminder Scheduler"
Cohesion: 0.15
Nodes (8): IEnumerable, IHttpClientFactory, List, string, Task, YouTubeService, YTApiVideo, YoutubeApiService

### Community 23 - "Interaction Handler"
Cohesion: 0.21
Nodes (9): CancellationToken, Func, Task, TimeProvider, TimeSpan, PeriodicRunner, Fact, Task (+1 more)

### Community 24 - "Command/Interaction Modules"
Cohesion: 0.25
Nodes (5): CancellationTokenSource, Task, CancellationToken, int, GracefulShutdown

### Community 25 - "YouTube Member Service"
Cohesion: 0.09
Nodes (11): DiscordStreamNotifyBot.DataBase.Table, DiscordStreamNotifyBot.Command.Youtube, DiscordStreamNotifyBot.Command.Attribute, DiscordStreamNotifyBot.Command.Twitch, CacheEntry, GuildLocaleRequest, HoloVideos, NijisanjiVideos (+3 more)

### Community 26 - "Notice Cache & Messaging"
Cohesion: 0.11
Nodes (17): 1. 背景與動機, 2. 新增跨 repo 契約, 3. A（小幫手）改動, 4. B（StreamRecordTools）改動, 5. 部署順序與相容性, 6. 驗證, 7. 影響範圍, A1. `Shared/RedisChannels.cs` (+9 more)

### Community 27 - "YouTube Reminder Timer"
Cohesion: 0.20
Nodes (10): 15. 預期修改檔案, 16. 完成定義, 1. 背景, 2. 目標, 3. 非目標, 4. 已確認的產品決策, 8.1 首次設定流程, 8.2 語系設定指令 (+2 more)

### Community 28 - "Command Attributes"
Cohesion: 0.13
Nodes (15): 10. 預期修改檔案, 11. 完成定義, 1. 背景, 2. 目標, 3. 非目標, 4. 技術選型, 5.1 Console, 5.2 非容器檔案 (+7 more)

### Community 29 - "Graphify Tooling Docs"
Cohesion: 0.22
Nodes (9): 12. 分階段執行, 階段 0：建立基準與字串清冊, 階段 1：Localization 基礎與繁中資源化, 階段 2：資料庫與語系設定, 階段 3：Slash command 註冊本地化, 階段 4：共用互動、Help 與首次設定, 階段 5：一般 Interaction 模組, 階段 6：背景通知與會限 DM (+1 more)

### Community 30 - "Logging"
Cohesion: 0.17
Nodes (12): DbContext, IsDeleted, DateTime, Task, TimeSpan, Video, YTApiVideo, YoutubeDetectionService (+4 more)

### Community 31 - "Cluster Leader/Heartbeat"
Cohesion: 0.06
Nodes (33): DiscordStreamNotifyBot.Coordinator, Counter, Gauge, HashSet, StreamGroupInfo, string, CoordinatorMetrics, CancellationToken (+25 more)

### Community 32 - "Member Check Settings"
Cohesion: 0.11
Nodes (17): GoogleAuthorizationCodeFlow, IDMChannel, SocketGuildUser, SocketMessageComponent, Task, DiscordSocketClient, EmbedBuilder, IServiceProvider (+9 more)

### Community 33 - "YouTube Spider Commands"
Cohesion: 0.20
Nodes (10): DateTime, TwitchSpiderRemovalMetricReason, TwitchUserState, DateTime, TwitchBroadcasterAuthorization, DateTime, TwitchSpider, TwitchEventSubCleanupDeferredMetricReason (+2 more)

### Community 34 - "Twitch Channel Commands"
Cohesion: 0.22
Nodes (6): Console 備援, Log 與 Loki, Loki 主動推送, Serilog Pipeline, 排障, 檔案路由

### Community 35 - "Twitcasting Detection"
Cohesion: 0.44
Nodes (9): Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task, TwitchService (+1 more)

### Community 36 - "Shared Extensions"
Cohesion: 0.15
Nodes (12): IDataStore, Task, ITokenDataStore, IDatabase, string, Task, Type, RedisDataStore (+4 more)

### Community 37 - "Bot State & Timers"
Cohesion: 0.17
Nodes (7): Action, BotConfig, IDatabase, ISubscriber, Task, TimeSpan, RedisTokenKeyProvisioner

### Community 38 - "Coordinator Entry/Shutdown"
Cohesion: 0.14
Nodes (18): DbContextOptions, TwitCasting, CommandExample, CommandSummary, RequireGuildMemberCount, SlashCommand, Task, TwitcastingService (+10 more)

### Community 39 - "YouTube Member Commands"
Cohesion: 0.14
Nodes (9): ConsoleColor, LogEvent, LogFileRoute, Logger, LoggerConfiguration, LogLevel, long, Exception (+1 more)

### Community 40 - "Twitcasting Commands"
Cohesion: 0.12
Nodes (15): HelixStream, ConcurrentDictionary, IReadOnlyCollection, IReadOnlyDictionary, RedisValue, ScraperMetrics, SemaphoreSlim, Task (+7 more)

### Community 41 - "YouTube Member Interaction"
Cohesion: 0.14
Nodes (13): ConcurrentBag, bool, ConcurrentDictionary, DateTime, HttpClient, IHttpClientFactory, Task, YoutubeApiService (+5 more)

### Community 42 - "DB Query Extensions"
Cohesion: 0.13
Nodes (14): EventSubSubscription, Counter, Gauge, string, ScraperMetricResult, ScraperMetrics, TwitchAuthorizationChangeMetricResult, TwitchEventSubCleanupDeferredMetricReason (+6 more)

### Community 43 - "Coordinator Service"
Cohesion: 0.12
Nodes (17): CultureInfo, Locale, ConcurrentDictionary, Dictionary, Func, IEnumerable, IReadOnlyCollection, IReadOnlyDictionary (+9 more)

### Community 44 - "Twitcasting Spider Commands"
Cohesion: 0.21
Nodes (7): bool, InlineData, int, string, Theory, BotStateCollectionDefinition, BotStateTests

### Community 45 - "Redis Channels"
Cohesion: 0.19
Nodes (9): string, Cluster, Member, Notifier, RedisChannels, SharedState, Twitcasting, Twitch (+1 more)

### Community 46 - "Twitch Spider Commands"
Cohesion: 0.25
Nodes (8): 13.1 編譯與靜態檢查, 13.2 Slash command 註冊, 13.3 Locale resolver, 13.4 首次設定, 13.5 通知, 13.6 YouTube 會限驗證, 13.7 範圍守衛, 13. 驗證矩陣

### Community 47 - "Nijisanji Stream JSON"
Cohesion: 0.29
Nodes (7): Fact, Func, IReadOnlyCollection, IReadOnlyDictionary, Task, TimeProvider, GuildLocaleServiceTests

### Community 48 - "Utility & Official Guilds"
Cohesion: 0.23
Nodes (5): LocaleResolver, InlineData, Theory, LocaleResolverTests, SupportedLocaleTests

### Community 49 - "Detection Host Bootstrap"
Cohesion: 0.08
Nodes (19): DiscordStreamNotifyBot.Command.Help, IEqualityComparer, Func, CommonEqualityComparer, Alias, Command, CommandInfo, CommandService (+11 more)

### Community 50 - "YouTube Channel Spider"
Cohesion: 0.06
Nodes (27): IDisposable, IServiceProvider, IServiceScope, IServiceScopeFactory, bool, Cacheable, DiscordSocketClient, IMessageChannel (+19 more)

### Community 51 - "Twitcasting HTTP Client"
Cohesion: 0.13
Nodes (15): 10. 可優化項目（claude 分支已有成品，對應階段順手移植）, 11. 驗證清單（部署前全過）, 1. 目標架構, 3. 設定, 5.1 歸屬守衛（防多 shard 互刪設定，最高優先）, 5.2 心跳與重啟, 5.3 啟動連線檢查 (StartupPreflight), 5. Shard 歸屬與生命週期 (+7 more)

### Community 52 - "Twitch Update Debounce"
Cohesion: 0.18
Nodes (14): IRole, RequireContext, SlashCommand, Task, YoutubeMember, CommandExample, CommandSummary, DiscordSocketClient (+6 more)

### Community 53 - "YoutubeApiService"
Cohesion: 0.06
Nodes (32): DateTime, DateTime, TimeSpan, TwitchChannelUpdateAction, TwitchGuildEligibilityDecision, TwitchGuildEligibilityFacts, TwitchGuildEligibilityPolicy, TwitchMissingGuildObservation (+24 more)

### Community 54 - "TwitchDetectionService.cs"
Cohesion: 0.29
Nodes (9): DefaultMemberPermissions, DiscordSocketClient, DiscordWebhookClient, IChannel, RequireContext, RequireUserPermission, SlashCommand, Task (+1 more)

### Community 55 - "Redis Token Provisioner"
Cohesion: 0.25
Nodes (8): 7. 分階段執行, 階段 0：建立基準, 階段 1：加入 Serilog 與 bootstrap logger, 階段 2：搬移 console 與檔案路由, 階段 3：切換 Loki sink, 階段 4：整理 facade 與 Discord.Net adapter, 階段 5：移除自製 sink 與更新文件, 階段 6：後續漸進式 structured logging（不阻擋本計畫完成）

### Community 56 - "TwitcastingService"
Cohesion: 0.23
Nodes (7): Broadcaster, DiscordSocketClient, EmojiService, NoticeCache, Task, TwitcastingNotification, TwitcastingService

### Community 57 - "CLAUDE.md"
Cohesion: 0.17
Nodes (11): Build & Run, Conventions, EF Core 鐵則, graphify, 制度條款, 外部契約（不可片面更改）, 指令文件, 架構要點（現行樹） (+3 more)

### Community 58 - "Twitcasting Backend Model"
Cohesion: 0.39
Nodes (3): string, Task, MySqlDataStore

### Community 59 - "Startup Preflight"
Cohesion: 0.20
Nodes (9): Func, Task, TimeProvider, TimeSpan, StartupPreflight, DateTimeOffset, Fact, Task (+1 more)

### Community 60 - "Twitcasting Webhook Models"
Cohesion: 0.22
Nodes (7): DiscordStreamNotifyBot.Interaction.OwnerOnly, SendMsgToAllGuildService, DefaultMemberPermissions, RequireOwner, SlashCommand, Task, SendMsgToAllGuild

### Community 61 - "Broadcast Message Command"
Cohesion: 0.07
Nodes (32): CancellationTokenRegistration, DebouncedEventArgs, Debouncer, FakeTimeProvider, Func, int, IReadOnlyCollection, string (+24 more)

### Community 62 - "TwitCasting Autocomplete"
Cohesion: 0.22
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 63 - "Twitch Autocomplete"
Cohesion: 0.14
Nodes (12): BotPlayingStatus, HttpException, ConnectionMultiplexer, DiscordSocketClient, IDatabase, int, ISubscriber, IUser (+4 more)

### Community 64 - "YouTube Autocomplete"
Cohesion: 0.09
Nodes (12): DiscordStreamNotifyBot.Tests.Component.Redis, DiscordStreamNotifyBot.HttpClients, DiscordStreamNotifyBot.Scraper, DiscordStreamNotifyBot.Shared, DiscordStreamNotifyBot.Command.TwitCasting, ServiceProvider, DetectionHost, int (+4 more)

### Community 65 - "Notifier Program Entry"
Cohesion: 0.18
Nodes (9): DelegatingHandler, HttpRequestMessage, HttpResponseMessage, LogMessage, CancellationToken, int, Task, TimeSpan (+1 more)

### Community 66 - "DiscordStreamNotifyBot.HttpClients.Twitcasting.Model"
Cohesion: 0.25
Nodes (5): NotificationDedupPolicy, Fact, InlineData, Theory, NotificationDedupPolicyTests

### Community 67 - "Interaction Base Module"
Cohesion: 0.24
Nodes (7): Assembly, CancellationToken, Exception, int, PeriodicTimer, Task, Program

### Community 69 - "Twitcasting Movie Info"
Cohesion: 0.19
Nodes (8): IEnumerable, IReadOnlyList, ModuleInfo, ResourceManager, SlashCommandInfo, SlashCommandParameterInfo, string, CommandDisplayResolver

### Community 70 - ".FixTCDbAsync"
Cohesion: 0.33
Nodes (3): JObject, Fact, NotificationContractTests

### Community 71 - "DbContext Factory"
Cohesion: 0.25
Nodes (7): EF Core 遷移與基線化（本專案版）, 一次性基線化（舊的 EnsureCreated 正式庫）, 一般變更流程, 你必須先知道的三件專案特例, 啟動時不碰資料庫（重要）, 套用：本地/開發 vs 正式環境, 收尾

### Community 73 - "Nijisanji Liver JSON"
Cohesion: 0.29
Nodes (7): 11.1 現況限制, 11.2 目標作法, 11.3 YouTube, 11.4 Twitch, 11.5 TwitCasting, 11.6 YouTube 會限驗證, 11. 通知與背景訊息

### Community 74 - "TwitCasting Webhook JSON"
Cohesion: 0.08
Nodes (29): Color, EmbedBuilder, DateTime, EmbedBuilder, IReadOnlyCollection, TimeSpan, TwitchEmbedBuilderFactory, DateTime (+21 more)

### Community 80 - "DiscordSocketClient"
Cohesion: 0.27
Nodes (14): Alias, ClusterQueryService, Command, CommandExample, DiscordSocketClient, IEnumerable, List, RequireContext (+6 more)

### Community 81 - ".PublishYoutubeNotificationAsync"
Cohesion: 0.29
Nodes (8): bool, Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task, ReactionEventWrapper

### Community 82 - "ITextChannel"
Cohesion: 0.16
Nodes (11): ILogEventSink, ITextFormatter, LogEventLevel, bool, object, string, DeferredFileSink, LogFileRoute (+3 more)

### Community 83 - "IUserMessage"
Cohesion: 0.33
Nodes (3): DiscordStreamNotifyBot.Migrations, ModelBuilder, RefactorDbContext

### Community 84 - "TwitcastingDetectionService"
Cohesion: 0.16
Nodes (11): GeneratedRegex, IEnumerable, YTChannelType, DateTime, DbSet, MainDbContext, Regex, Task (+3 more)

### Community 85 - "string"
Cohesion: 0.33
Nodes (5): For /graphify explain, For /graphify path, graphify reference: query, path, explain, Step 0 — Constrained query expansion (REQUIRED before traversal), Step 1 — Traversal

### Community 86 - "TcBackendStreamData.cs"
Cohesion: 0.17
Nodes (12): 10. 測試實作規則, 1. 目標, 2. 測試分類, 3. 不移除的啟動檢查, 4. 第一批：低耦合契約與格式化, 5. 第二批：小幅抽出純邏輯, 6. 第三批：時間與快取, 7. 第四批：Scraper 狀態機 (+4 more)

### Community 87 - "SyncModelDrift"
Cohesion: 0.19
Nodes (4): DateTime, EmbedBuilder, YTChannelType, SharedExtensions

### Community 88 - ".SendConfirmMessageAsync"
Cohesion: 0.29
Nodes (7): ILocalizationManager, ResxLocalizationManager, IDictionary, IList, LocalizationTarget, string, DescriptionOnlyLocalizationManager

### Community 89 - "CommandTextEqualityComparer"
Cohesion: 0.18
Nodes (4): Fact, InlineData, Theory, BotLocalizerTests

### Community 90 - "YoutubePubSubNotification"
Cohesion: 0.33
Nodes (6): 5.1 支援值, 5.2 公開內容與背景通知, 5.3 私人即時回覆, 5.4 延遲會限驗證 DM, 5.5 併發安全, 5. 語系模型與解析規則

### Community 91 - "20250320095452_RefactorDbContext.Designer.cs"
Cohesion: 0.23
Nodes (6): Video, DateTime, MySqlComponentFact, Task, YTChannelType, VideoQueryAndConcurrencyTests

### Community 92 - "Program"
Cohesion: 0.40
Nodes (5): 10.1 共用回覆 API, 10.2 Precondition 與 handler 錯誤, 10.3 例外訊息, 10.4 第一階段模組, 10. 執行期互動本地化

### Community 93 - "20250620094111_AddMaxSpiderCountSettingField.Designer.cs"
Cohesion: 0.40
Nodes (5): 6.1 指令註冊資源, 6.2 執行期訊息資源, 6.3 Help 長文, 6.4 Localizer API, 6. 資源架構

### Community 94 - "20250320095452_RefactorDbContext.Designer.cs"
Cohesion: 0.36
Nodes (6): StreamEntry, IDatabase, RedisComponentFact, StreamEntry, Task, NotificationBusConsumerRedisComponentTests

### Community 95 - "20250603065853_ModifyTwitCastingTable.Designer.cs"
Cohesion: 0.50
Nodes (3): For /graphify add, For --watch, graphify reference: add a URL and watch a folder

### Community 96 - "20250620094111_AddMaxSpiderCountSettingField.Designer.cs"
Cohesion: 0.50
Nodes (3): For git commit hook, For native CLAUDE.md integration, graphify reference: commit hook and native CLAUDE.md integration

### Community 97 - "TwitchSpider"
Cohesion: 0.50
Nodes (3): For --cluster-only, For --update (incremental re-extraction), graphify reference: incremental update and cluster-only

### Community 98 - "RecordYoutubeChannel"
Cohesion: 0.12
Nodes (18): ConcurrentDictionary, Func, SemaphoreSlim, Task, YoutubeNoticeType, ClaimState, YoutubeTerminalEventAction, YoutubeTerminalEventDecision (+10 more)

### Community 101 - "graphify"
Cohesion: 0.20
Nodes (9): SlashCommand, CommandExample, CommandSummary, DiscordSocketClient, IChannel, RequireBotPermission, SlashCommand, Task (+1 more)

### Community 102 - "extraction-spec.md"
Cohesion: 0.20
Nodes (11): HashSet, IReadOnlyCollection, IReadOnlyList, TwitchEventSubCreateSpec, TwitchEventSubFact, TwitchEventSubFinalDecision, TwitchEventSubReconcilePlan, TwitchEventSubReconcilePolicy (+3 more)

### Community 103 - "net8.0"
Cohesion: 0.38
Nodes (6): CommandExample, CommandSummary, SlashCommand, Task, TwitchService, TwitchSpider

### Community 105 - ".LoadInteractionFrom"
Cohesion: 0.28
Nodes (6): CancellationToken, DiscordSocketClient, HttpClient, Task, DiscordWebhookClient, Message

### Community 106 - "DiscordSocketClient"
Cohesion: 0.09
Nodes (17): Attribute, DiscordStreamNotifyBot.Interaction.Attribute, DiscordStreamNotifyBot.Interaction.TwitCasting, DiscordStreamNotifyBot.Command.Admin, DiscordStreamNotifyBot.Interaction.Help.Service, DiscordStreamNotifyBot.Interaction.Twitch, DiscordStreamNotifyBot.SharedService.Cluster, DiscordStreamNotifyBot.Interaction.Youtube (+9 more)

### Community 113 - "NoticeCache"
Cohesion: 0.24
Nodes (6): int, Timer, Video, YTChannelType, ReminderItem, ConcurrentDictionary

### Community 114 - "StreamGroupInfo"
Cohesion: 0.43
Nodes (4): ModuleBase, EmbedBuilder, Task, TopLevelModule

### Community 115 - "string"
Cohesion: 0.11
Nodes (18): Microsoft.Extensions.DependencyInjection.Abstractions (10.0.1), System.Management (10.0.1), net8.0, Ben.Demystifier (0.4.1), Discord.Net (3.19.1), Dorssel.Utilities.Debounce (3.0.0), EFCore.NamingConventions (9.0.0), Google.Apis.YouTube.v3 (1.73.0.3981) (+10 more)

### Community 116 - "CancellationToken"
Cohesion: 0.11
Nodes (17): Clip, DateTime, DiscordSocketClient, EmojiService, EventSubSubscription, IReadOnlyList, Lazy, NoticeCache (+9 more)

### Community 117 - ".OnReaction"
Cohesion: 0.22
Nodes (5): 一、`claude` 分支是你最大的資產，也是最大的陷阱, 三、使用者已做的決策，不要重新辯論, 二、你在活的生產系統旁施工, 給未來 session 的信, 這套制度最可能的退化方式，與預防

### Community 118 - ".EvaluateAsync"
Cohesion: 0.83
Nodes (3): Broadcaster, Movie, TwitCastingWebHookJson

### Community 119 - "RedisConnection"
Cohesion: 0.31
Nodes (5): HttpClient, List, string, Task, TwitcastingClient

### Community 120 - "string"
Cohesion: 0.14
Nodes (13): 0. 涉及專案, 10. Backend EventSub Webhook, 12. Frontend, 14. Grafana, 18. 建置與遷移, 19. 部署順序, 1. 不可偏離的決策, 20. 官方參考 (+5 more)

### Community 121 - "MainDbContextFactory"
Cohesion: 0.50
Nodes (4): 14.1 建議部署順序, 14.2 相容性, 14.3 回滾, 14. 部署與回滾

### Community 122 - "Category"
Cohesion: 0.31
Nodes (9): EventSubSubscription, IReadOnlyList, Stream, TwitchEventSubDeleteResult, TwitchEventSubDeleteStatus, TwitchEventSubEnsureMode, TwitchEventSubEnsureResult, TwitchEventSubSubscriptionsResult (+1 more)

### Community 123 - "TwitchApiResults.cs"
Cohesion: 0.22
Nodes (7): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, Fact, AutocompleteSearchTests

### Community 124 - "16. 執行階段"
Cohesion: 0.22
Nodes (9): 16. 執行階段, 階段 0：前置確認, 階段 1：資料模型與 Backend 設定, 階段 2：Google/Twitch OAuth 隔離, 階段 3：Frontend, 階段 4：Twitch add資格與授權清理, 階段 5：StreamOnline 與 EventSub reconcile, 階段 6：Prometheus 與 Grafana (+1 more)

### Community 125 - "Prometheus / Grafana 監控"
Cohesion: 0.22
Nodes (8): Backend 指標, Coordinator 指標, Endpoints, Grafana, Prometheus, Prometheus / Grafana 監控, Scraper 指標, 排障

### Community 126 - ".DispatchFromBusAsync"
Cohesion: 0.19
Nodes (10): CancellationToken, Func, IDatabase, int, Task, TwitcastingService, TwitchService, YoutubeMemberService (+2 more)

### Community 127 - "DiscordStreamNotifyBot.sln"
Cohesion: 0.50
Nodes (3): net8.0, prometheus-net.AspNetCore (8.2.1), Microsoft.NET.Sdk

### Community 128 - "DiscordWebhookClient"
Cohesion: 0.25
Nodes (7): coverlet.collector (6.0.0), Microsoft.Extensions.TimeProvider.Testing (9.0.0), Microsoft.NET.Test.Sdk (17.8.0), xunit (2.5.3), xunit.runner.visualstudio (2.5.3), net8.0, Microsoft.NET.Sdk

### Community 129 - "17. 驗證矩陣"
Cohesion: 0.33
Nodes (6): 17.1 新增 spider, 17.2 EventSub, 17.3 授權失效, 17.4 OAuth, 17.5 Prometheus/Grafana, 17. 驗證矩陣

### Community 130 - "YoutubeChannelOwnedType"
Cohesion: 0.31
Nodes (5): IEnumerable, int, IReadOnlyList, AutocompleteCandidate, AutocompleteSearch

### Community 131 - "GetAllRegistedWebHookJson.cs"
Cohesion: 0.38
Nodes (5): IAsyncLifetime, string, Task, MySqlComponentCollection, MySqlComponentFixture

### Community 132 - "7. OAuth API 與流程隔離"
Cohesion: 0.40
Nodes (5): 7.1 API, 7.2 State, 7.3 Callback, 7.4 Twitch scopes, 7. OAuth API 與流程隔離

### Community 133 - ".LoadCommandFrom"
Cohesion: 0.20
Nodes (7): TwitcastingLiveStartEvent, TwitcastingWebhookParser, Fact, InlineData, string, Theory, TwitcastingLiveStartPlannerTests

### Community 134 - "RecordYoutubeChannel"
Cohesion: 0.05
Nodes (52): ChannelInfo, ClusterQueryType, DiscordStreamNotifyBot.Command.Normal, Replies, Responses, DiscordSocketClient, Expected, IReadOnlyCollection (+44 more)

### Community 135 - "AddTwitchBroadcasterAuthorization"
Cohesion: 0.19
Nodes (10): HashSet, IEnumerable, IReadOnlyList, string, TwitcastingWebhookAction, TwitcastingWebhookActionKind, TwitcastingWebhookRegistration, TwitcastingWebhookRegistrationPlanner (+2 more)

### Community 137 - "NijisanjiLiverJson.cs"
Cohesion: 0.70
Nodes (4): Head, Images, NijisanjiLiverJson, SocialLinks

### Community 138 - "11. Bot EventSub 與偵測"
Cohesion: 0.50
Nodes (4): 11.1 `TwitchApiService`, 11.2 `TwitchDetectionService`, 11.3 Reconcile, 11. Bot EventSub 與偵測

### Community 139 - "15. 預期修改檔案"
Cohesion: 0.50
Nodes (4): 15.1 Bot, 15.2 Backend, 15.3 Frontend, 15. 預期修改檔案

### Community 140 - "TwitcastingDetectionService"
Cohesion: 0.50
Nodes (4): 2.1 Bot, 2.2 Backend, 2.3 Frontend, 2. 現況基線

### Community 141 - "5. Guild 資格與 OAuth 豁免"
Cohesion: 0.50
Nodes (4): 5.1 一般 guild 資格, 5.2 新增 spider 的 OAuth 豁免, 5.3 授權失效時的 guild 查詢, 5. Guild 資格與 OAuth 豁免

### Community 142 - "DiscordStreamNotifyBot.Coordinator.csproj"
Cohesion: 0.25
Nodes (3): net8.0, prometheus-net.AspNetCore (8.2.1), Microsoft.NET.Sdk

### Community 143 - "20260719142803_AddTwitchBroadcasterAuthorization.Designer.cs"
Cohesion: 0.17
Nodes (15): DiscordStreamNotifyBot.Interaction.Help, AutocompletionResult, HelpService, IAutocompleteInteraction, IInteractionContext, InteractionService, IParameterInfo, IReadOnlyList (+7 more)

### Community 144 - "13. Prometheus"
Cohesion: 0.67
Nodes (3): 13.1 Backend 指標, 13.2 Scraper 指標, 13. Prometheus

### Community 145 - "4. 安全刪除狀態機"
Cohesion: 0.67
Nodes (3): 4.1 直播中授權失效, 4.2 關台後重新判斷, 4. 安全刪除狀態機

### Community 148 - "MainDbContextModelSnapshot.cs"
Cohesion: 0.40
Nodes (3): ModelSnapshot, ModelBuilder, MainDbContextModelSnapshot

### Community 149 - ".LoadCommandFrom"
Cohesion: 0.40
Nodes (4): Assembly, IEnumerable, IServiceCollection, Type

### Community 150 - "RefactorDbContext"
Cohesion: 0.50
Nodes (3): Migration, MigrationBuilder, RefactorDbContext

### Community 156 - "5. Shard 歸屬與生命週期"
Cohesion: 0.16
Nodes (10): DateTimeOffset, Func, List, object, TimeProvider, TimeSpan, NoticeCache, Fact (+2 more)

### Community 157 - ".SendPaginatedConfirmAsync"
Cohesion: 0.25
Nodes (6): CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task, RequireGuildOwnerAttribute

### Community 158 - ".CheckMemberShipOnlyVideoIdAsync"
Cohesion: 0.22
Nodes (7): PreconditionAttribute, ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildAttribute

### Community 165 - ".GetStreamVideoByVideoId"
Cohesion: 0.22
Nodes (5): DiscordStreamNotifyBot.SharedService, Emote, IInteractionService, DiscordSocketClient, EmojiService

### Community 166 - "HoloVideos"
Cohesion: 0.25
Nodes (6): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildMemberCountAttribute

### Community 167 - "NonApprovedVideos"
Cohesion: 0.25
Nodes (6): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildOwnerAttribute

### Community 168 - "InteractionErrorPolicyTests"
Cohesion: 0.39
Nodes (4): InlineData, InteractionCommandError, Theory, InteractionErrorPolicyTests

### Community 169 - ".Resolve"
Cohesion: 0.57
Nodes (3): InteractionCommandError, InteractionErrorDescriptor, InteractionErrorPolicy

### Community 170 - ".GuildMemberCountPreconditionMapsValuesAndContactPath"
Cohesion: 0.33
Nodes (3): string, InteractionErrorCodes, Fact

### Community 171 - ".GenerateSuggestionsAsync"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 172 - ".GenerateSuggestionsAsync"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 173 - ".GenerateSuggestionsAsync"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 174 - "5. 目標架構"
Cohesion: 0.38
Nodes (5): ConcurrentDictionary, Task, TimeProvider, TimeSpan, TwitchGuildEligibilityEvaluator

### Community 175 - "5. Shard 歸屬與生命週期"
Cohesion: 0.27
Nodes (4): Fact, InlineData, Theory, RedisContractTests

### Community 176 - "7. 資料庫變更"
Cohesion: 0.22
Nodes (9): 8. 分階段實作步驟, 階段 0：止血 PR — shard 歸屬守衛, 階段 1：Solution 骨架 + Shared, 階段 2：Notifier 上線（先維持單 shard 行為）, 階段 3：Scraper 拆出 + Redis Streams 匯流排（完成，正確性待測試環境驗）, 階段 4：Coordinator（完成，正確性待測試環境驗）, 階段 5：跨 shard 指令與共享狀態（完成，正確性待測試環境驗）, 階段 6：Docker 化與部署驗證（檔案完成，實跑待測試環境） (+1 more)

### Community 177 - "9. Slash Command Localization"
Cohesion: 0.29
Nodes (5): DiscordStreamNotifyBot.Interaction.Utility.Service, DiscordStreamNotifyBot.Interaction.Utility, IInteractionService, IServiceProvider, UtilityService

### Community 178 - "GetAllRegistedWebHookJson.cs"
Cohesion: 0.33
Nodes (5): Alias, Command, RequireContext, RequireOwner, Task

### Community 179 - "LogEvent"
Cohesion: 0.20
Nodes (9): YoutubeApiVideoAction, YoutubeApiVideoDecision, YoutubeApiVideoFacts, YoutubeApiVideoPolicy, DateTime, Fact, InlineData, Theory (+1 more)

### Community 180 - ".PublishAsync"
Cohesion: 0.18
Nodes (9): YoutubeMemberCandidateAction, YoutubeMemberCandidateFacts, YoutubeMemberChannelDecision, YoutubeMemberChannelFacts, YoutubeMemberVideoPolicy, Fact, InlineData, Theory (+1 more)

### Community 181 - ".GetNowStreamingChannel"
Cohesion: 0.29
Nodes (4): DiscordStreamNotifyBot.Tests.Component.MySql, string, MySqlComponentFactAttribute, StoredToken

### Community 182 - ".GuildMemberCountPreconditionMapsValuesAndContactPath"
Cohesion: 0.19
Nodes (11): List, RedisValue, SemaphoreSlim, Task, TwitcastingDetectionService, TwitcastingLiveStartAction, TwitcastingLiveStartFacts, TwitcastingLiveStartPlan (+3 more)

### Community 184 - "ReminderItem"
Cohesion: 0.12
Nodes (15): AutocompleteHandler, GuildNoticeTwitCastingChannelIdAutocompleteHandler, GuildNoticeTwitchChannelIdAutocompleteHandler, AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider (+7 more)

### Community 185 - "YoutubePubSubNotification"
Cohesion: 0.09
Nodes (17): DiscordStreamNotifyBot.Scraper.Detection.Youtube, DiscordStreamNotifyBot.Scraper.Detection.Twitch.Debounce, DiscordStreamNotifyBot.Tests, DiscordStreamNotifyBot.Scraper.Detection.Twitch, DiscordStreamNotifyBot.SharedService.Youtube.Json, DiscordStreamNotifyBot.Scraper.Detection.Twitcasting, DiscordStreamNotifyBot.Shared.Messages, TwitchAuthorizationChangedPayload (+9 more)

### Community 187 - "YoutubeChannelOwnedType"
Cohesion: 0.21
Nodes (8): ConnectionMultiplexer, Lazy, object, string, RedisConnection, RedisComponentFact, Task, RedisTokenKeyRedisComponentTests

### Community 188 - "GetAllRegistedWebHookJson.cs"
Cohesion: 0.24
Nodes (7): bool, DiscordSocketClient, HttpClient, string, Task, Timer, UptimeKumaClient

### Community 190 - ".GetCommandHelp"
Cohesion: 0.33
Nodes (6): RequireBotPermissionAttribute, RequireUserPermissionAttribute, EmbedBuilder, IEnumerable, SlashCommandInfo, HelpService

### Community 191 - ".CreateStreamEnded"
Cohesion: 0.50
Nodes (3): DateTime, YTChannelType, YoutubeChannelOwnedType

### Community 192 - "TwitcastingLiveStartPlanner.cs"
Cohesion: 0.43
Nodes (5): IEnumerable, List, CategoriesJson, Category, SubCategory

### Community 193 - "Normal"
Cohesion: 0.48
Nodes (3): MySqlComponentFact, Task, MigrationAndConstraintTests

### Community 194 - ".FilterNoNotifyGuilds"
Cohesion: 0.61
Nodes (3): MySqlComponentFact, Task, MySqlDataStoreTests

### Community 196 - "RedisDataStore"
Cohesion: 0.43
Nodes (6): DateTime, List, Channel, EventLiver, Liver, NijisanjiStreamJson

### Community 197 - "DiscordStreamNotifyBot.SharedService.Cluster"
Cohesion: 0.33
Nodes (6): 2.1 `Shared`（共用 library）, 2.2 `Scraper`（爬蟲層，叢集唯一）, 2.3 `Notifier`（通知層 / shard，可多個）, 2.4 `Coordinator`（主控層，1 個）, 2.5 SharedService 逐服務拆分歸屬（判斷準則表）, 2. 專案拆分 (Solution Layout)

### Community 198 - ".GetLocaleAsync"
Cohesion: 0.14
Nodes (16): ComponentInteraction, InteractionModuleBase, SocketInteractionContext, Task, SpiderManagementComponent, Task, TopLevelModule, CommandExample (+8 more)

### Community 199 - ".DecideAutomaticMutation"
Cohesion: 0.31
Nodes (4): YoutubeMemberAutomaticMutationAction, YoutubeMemberManualPinPolicy, Fact, YoutubeMemberManualPinPolicyTests

### Community 200 - "TcBackendStreamData.cs"
Cohesion: 0.44
Nodes (8): App, BackendMovie, Fmp4, Hls, Llfmp4, Streams, TcBackendStreamData, Webrtc

### Community 201 - "GetMovieInfoResponse"
Cohesion: 0.17
Nodes (9): DiscordStreamNotifyBot.HttpClients.Twitcasting.Model, List, GetAllRegistedWebHookJson, Webhook, List, Broadcaster, GetMovieInfoResponse, Movie (+1 more)

### Community 202 - ".FixTCDbAsync"
Cohesion: 0.33
Nodes (6): 4.1 拓撲, 4.2 DTO（`Shared/Messages/`）, 4.3 消費迴圈（Notifier）, 4.4 建群與 Preflight, 4.5 Redis 控制平面鍵（非 stream）, 4. 訊息契約：Redis Streams 通知匯流排

### Community 204 - "ITokenDataStore"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 206 - "GetAllRegistedWebHookJson.cs"
Cohesion: 0.40
Nodes (5): 8.1 編譯與靜態檢查, 8.2 Console 與檔案, 8.3 Loki, 8.4 生命週期, 8. 驗證矩陣

### Community 211 - "OtherVideos"
Cohesion: 0.50
Nodes (4): 7.1 `GuildConfig.Locale`, 7.2 `YoutubeMemberCheck.Locale`, 7.3 Migration 鐵則, 7. 資料庫變更

### Community 212 - "9. Slash Command Localization"
Cohesion: 0.50
Nodes (4): 9.1 Discord.Net 設定, 9.2 指令名稱, 9.3 Command signature, 9. Slash Command Localization

## Knowledge Gaps
- **378 isolated node(s):** `net8.0`, `prometheus-net.AspNetCore (8.2.1)`, `Microsoft.NET.Sdk`, `BotPlayingStatus`, `DiscordStreamNotifyBot.Command.Normal` (+373 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **37 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `DiscordStreamNotifyBot.DataBase` connect `Scaling Architecture Docs` to `GetAllRegistedWebHookJson.cs`, `RecordYoutubeChannel`, `Interaction Extensions`, `Command Help Module`, `Bot Startup & Membership`, `MainDbContextModelSnapshot.cs`, `Auth / Token Crypto`, `YouTube Reminder Scheduler`, `YouTube Member Service`, `20250603065853_ModifyTwitCastingTable.Designer.cs`, `20250620094111_AddMaxSpiderCountSettingField.Designer.cs`, `20260611015819_SyncModelDrift.Designer.cs`, `20260709091318_AddManualMemberCheckVideoFlag.Designer.cs`, `20260719142803_AddTwitchBroadcasterAuthorization.Designer.cs`, `20260721095646_AddLocalizationSettings.Designer.cs`, `.GetStreamVideoByVideoId`, `9. Slash Command Localization`, `MainDbContextFactory`, `YoutubePubSubNotification`, `Startup Preflight`, `YouTube Autocomplete`, `IUserMessage`, `SyncModelDrift`, `DiscordSocketClient`?**
  _High betweenness centrality (0.065) - this node is a cross-community bridge._
- **Why does `DiscordStreamNotifyBot.Shared` connect `YouTube Autocomplete` to `GetAllRegistedWebHookJson.cs`, `RecordYoutubeChannel`, `EF Migrations`, `Scaling Architecture Docs`, `Bot Startup & Membership`, `Auth / Token Crypto`, `YouTube Reminder Scheduler`, `Interaction Handler`, `Command/Interaction Modules`, `YouTube Member Service`, `Cluster Leader/Heartbeat`, `Bot State & Timers`, `Twitcasting Spider Commands`, `Redis Channels`, `5. Shard 歸屬與生命週期`, `YoutubePubSubNotification`, `Startup Preflight`, `Twitcasting Categories JSON`, `SyncModelDrift`, `DiscordSocketClient`, `.DispatchFromBusAsync`?**
  _High betweenness centrality (0.061) - this node is a cross-community bridge._
- **Why does `MainDbService` connect `Coordinator Entry/Shutdown` to `GetAllRegistedWebHookJson.cs`, `RecordYoutubeChannel`, `Scaling Architecture Docs`, `Command Help Module`, `YouTube Slash Commands`, `Bot Startup & Membership`, `Bot Entry Points`, `YouTube Reminder Scheduler`, `Member Check Settings`, `Twitcasting Detection`, `Twitcasting Commands`, `YouTube Member Interaction`, `Coordinator Service`, `Twitch Update Debounce`, `TwitchDetectionService.cs`, `.GuildMemberCountPreconditionMapsValuesAndContactPath`, `TwitcastingService`, `YoutubePubSubNotification`, `Twitcasting Backend Model`, `Twitch Autocomplete`, `.GetLocaleAsync`, `DiscordSocketClient`, `graphify`, `net8.0`, `CancellationToken`?**
  _High betweenness centrality (0.061) - this node is a cross-community bridge._
- **What connects `net8.0`, `prometheus-net.AspNetCore (8.2.1)`, `Microsoft.NET.Sdk` to the rest of the system?**
  _378 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Admin Broadcast Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.09090909090909091 - nodes in this community are weakly interconnected._
- **Should `Twitch Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.08 - nodes in this community are weakly interconnected._
- **Should `Solution & Dependencies` be split into smaller, more focused modules?**
  _Cohesion score 0.08333333333333333 - nodes in this community are weakly interconnected._