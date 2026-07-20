# Graph Report - DiscordStreamNotifyBot  (2026-07-20)

## Corpus Check
- 182 files · ~91,142 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1931 nodes · 3772 edges · 150 communities (116 shown, 34 thin omitted)
- Extraction: 93% EXTRACTED · 7% INFERRED · 0% AMBIGUOUS · INFERRED: 269 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `d86294e4`
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
- YoutubeChannelSpider
- 20250620094111_AddMaxSpiderCountSettingField.Designer.cs
- 20260709091318_AddManualMemberCheckVideoFlag.Designer.cs
- .GetStreamVideoByVideoId

## God Nodes (most connected - your core abstractions)
1. `TwitchDetectionService` - 53 edges
2. `DiscordStreamNotifyBot.DataBase` - 44 edges
3. `DiscordStreamNotifyBot.DataBase.Table` - 44 edges
4. `MainDbContext` - 37 edges
5. `MainDbService` - 33 edges
6. `Video` - 33 edges
7. `DiscordStreamNotifyBot.Shared` - 32 edges
8. `TwitchApiService` - 31 edges
9. `YoutubeStreamService` - 27 edges
10. `BotConfig` - 27 edges

## Surprising Connections (you probably didn't know these)
- `CoordinatorService` --references--> `BotConfig`  [EXTRACTED]
  src/DiscordStreamNotifyBot.Coordinator/CoordinatorService.cs → src/DiscordStreamNotifyBot.Shared/BotConfig.cs
- `CoordinatorService` --references--> `ClusterService`  [EXTRACTED]
  src/DiscordStreamNotifyBot.Coordinator/CoordinatorService.cs → src/DiscordStreamNotifyBot.Shared/ClusterService.cs
- `Bot` --references--> `BotConfig`  [EXTRACTED]
  src/DiscordStreamNotifyBot.Notifier/Bot.cs → src/DiscordStreamNotifyBot.Shared/BotConfig.cs
- `Bot` --references--> `MainDbService`  [EXTRACTED]
  src/DiscordStreamNotifyBot.Notifier/Bot.cs → src/DiscordStreamNotifyBot.Shared/DataBase/MainDbService.cs
- `AdministrationService` --references--> `MainDbService`  [EXTRACTED]
  src/DiscordStreamNotifyBot.Notifier/Command/Admin/AdministraitonService.cs → src/DiscordStreamNotifyBot.Shared/DataBase/MainDbService.cs

## Import Cycles
- None detected.

## Communities (150 total, 34 thin omitted)

### Community 0 - "Admin Broadcast Commands"
Cohesion: 0.09
Nodes (12): BannerChange, DateTime, DbEntity, GuildConfig, GuildYoutubeMemberConfig, NoticeTwitcastingStreamChannel, NoticeTwitchStreamChannel, NoticeYoutubeStreamChannel (+4 more)

### Community 1 - "YouTube Stream Commands"
Cohesion: 0.05
Nodes (37): BotPlayingStatus, HttpException, ConnectionMultiplexer, DiscordSocketClient, IDatabase, int, ISubscriber, IUser (+29 more)

### Community 2 - "Twitch Commands"
Cohesion: 0.11
Nodes (16): ServiceProvider, EventSubSubscription, DetectionHost, Counter, Gauge, string, ScraperMetricResult, ScraperMetrics (+8 more)

### Community 3 - "Twitcasting Service & DbContext"
Cohesion: 0.06
Nodes (44): ChannelInfo, ClusterQueryType, DiscordStreamNotifyBot.Command.Normal, IReadOnlyCollection, Replies, Responses, SocketGuild, DiscordSocketClient (+36 more)

### Community 4 - "Solution & Dependencies"
Cohesion: 0.10
Nodes (19): Microsoft.EntityFrameworkCore.Design (9.0.3), Microsoft.EntityFrameworkCore.Relational (9.0.3), Microsoft.EntityFrameworkCore.Tools (9.0.3), net8.0, Ben.Demystifier (0.4.1), Discord.Net (3.19.1), Dorssel.Utilities.Debounce (3.0.0), EFCore.NamingConventions (9.0.0) (+11 more)

### Community 5 - "Help & Owner Services"
Cohesion: 0.43
Nodes (6): DateTime, List, Channel, EventLiver, Liver, NijisanjiStreamJson

### Community 6 - "Notification Bus Consumer"
Cohesion: 0.16
Nodes (12): InteractionModuleBase, SocketInteractionContext, Task, TopLevelModule, CommandExample, CommandSummary, DiscordSocketClient, IChannel (+4 more)

### Community 7 - "Help Autocomplete Handlers"
Cohesion: 0.13
Nodes (13): 1. Shared — 定義契約, 2. Scraper — 偵測並 publish, 3. Notifier — 消費並發送, 動工前先讀一個既有平台, 收尾檢查, 新增偵測平台 / 通知事件, 步驟（依相依順序，Shared → Scraper → Notifier）, 偵測 → 匯流排 → 發送 路徑除錯 (+5 more)

### Community 9 - "Precondition Attributes"
Cohesion: 0.05
Nodes (31): PreconditionAttribute, CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task, RequireGuildMemberCountAttribute, CommandInfo (+23 more)

### Community 10 - "Command Handler"
Cohesion: 0.18
Nodes (12): AutocompletionResult, CommandExample, CommandSummary, DiscordSocketClient, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider (+4 more)

### Community 11 - "Embed Builder Factory"
Cohesion: 0.06
Nodes (31): NowStreamingHost, DateTime, EmbedBuilder, YTApiVideo, EmbedBuilderFactory, DiscordSocketClient, Embed, HttpClient (+23 more)

### Community 12 - "Scaling Architecture Docs"
Cohesion: 0.13
Nodes (9): DiscordStreamNotifyBot.SharedService.Twitcasting, DiscordStreamNotifyBot.DataBase.Table, DiscordStreamNotifyBot.SharedService, DiscordStreamNotifyBot.Command.Youtube, DiscordStreamNotifyBot.Command.Attribute, DiscordStreamNotifyBot.Command.Twitch, DiscordStreamNotifyBot.Interaction, EmbedBuilder (+1 more)

### Community 13 - "Interaction Extensions"
Cohesion: 0.13
Nodes (8): IDiscordInteraction, Process, EmbedBuilder, IEmote, IInteractionContext, Task, Video, Extensions

### Community 14 - "Command Help Module"
Cohesion: 0.16
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
Cohesion: 0.27
Nodes (12): CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, IChannel, NoticeType, RequireBotPermission, RequireContext (+4 more)

### Community 19 - "Bot Startup & Membership"
Cohesion: 0.07
Nodes (28): ButtonCheckData, DiscordStreamNotifyBot.Interaction.Utility.Service, DiscordStreamNotifyBot.Interaction.OwnerOnly.Service, DiscordStreamNotifyBot.Interaction.Help.Service, IInteractionService, SendAllPayload, EmbedBuilder, SlashCommandInfo (+20 more)

### Community 20 - "Auth / Token Crypto"
Cohesion: 0.12
Nodes (10): IDataStore, TokenCrypto, TokenManager, Task, ITokenDataStore, IDatabase, string, Task (+2 more)

### Community 21 - "Bot Entry Points"
Cohesion: 0.39
Nodes (6): CancellationToken, PeriodicTimer, string, Task, TimeSpan, ScraperService

### Community 22 - "YouTube Reminder Scheduler"
Cohesion: 0.25
Nodes (9): CommandExample, CommandSummary, DiscordSocketClient, IChannel, RequireBotPermission, SlashCommand, Task, TwitcastingService (+1 more)

### Community 23 - "Interaction Handler"
Cohesion: 0.12
Nodes (14): Emote, IResult, SocketInteraction, SocketSlashCommandDataOption, IInteractionService, DiscordSocketClient, IInteractionContext, InteractionService (+6 more)

### Community 24 - "Command/Interaction Modules"
Cohesion: 0.22
Nodes (6): IDatabase, IEnumerable, string, Task, TimeSpan, ClusterService

### Community 25 - "YouTube Member Service"
Cohesion: 0.20
Nodes (7): Attribute, string, CommandExampleAttribute, string, CommandExampleAttribute, string, CommandSummaryAttribute

### Community 26 - "Notice Cache & Messaging"
Cohesion: 0.11
Nodes (17): 1. 背景與動機, 2. 新增跨 repo 契約, 3. A（小幫手）改動, 4. B（StreamRecordTools）改動, 5. 部署順序與相容性, 6. 驗證, 7. 影響範圍, A1. `Shared/RedisChannels.cs` (+9 more)

### Community 27 - "YouTube Reminder Timer"
Cohesion: 0.24
Nodes (9): IDisposable, bool, Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task (+1 more)

### Community 28 - "Command Attributes"
Cohesion: 0.19
Nodes (11): IReadOnlyDictionary, DateTime, TwitchUserState, DateTime, TwitchBroadcasterAuthorization, DateTime, TwitchSpider, TwitchEventSubCleanupDeferredMetricReason (+3 more)

### Community 29 - "Graphify Tooling Docs"
Cohesion: 0.08
Nodes (24): For /graphify add and --watch, For /graphify query, For the commit hook and native CLAUDE.md integration, For --update and --cluster-only, /graphify, Honesty Rules, Interpreter guard for subcommands, Part A - Structural extraction for code files (+16 more)

### Community 30 - "Logging"
Cohesion: 0.19
Nodes (10): DbContextOptions, RequireContext, SlashCommand, Task, YoutubeMember, string, MainDbService, string (+2 more)

### Community 31 - "Cluster Leader/Heartbeat"
Cohesion: 0.12
Nodes (14): Counter, Gauge, HashSet, StreamGroupInfo, string, CoordinatorMetrics, CancellationToken, IDatabase (+6 more)

### Community 32 - "Member Check Settings"
Cohesion: 0.17
Nodes (10): GoogleAuthorizationCodeFlow, SocketGuildUser, SocketMessageComponent, Task, Task, Timer, YoutubeMemberService, TokenResponse (+2 more)

### Community 33 - "YouTube Spider Commands"
Cohesion: 0.15
Nodes (11): ConsoleColor, LogFileType, LogLevel, LogMessage, Exception, object, string, Task (+3 more)

### Community 34 - "Twitch Channel Commands"
Cohesion: 0.18
Nodes (6): 一、`claude` 分支是你最大的資產，也是最大的陷阱, 三、使用者已做的決策，不要重新辯論, 二、你在活的生產系統旁施工, 給未來 session 的信, 這套制度最可能的退化方式，與預防, Log 格式與 Level Patterns

### Community 35 - "Twitcasting Detection"
Cohesion: 0.06
Nodes (27): AutocompleteHandler, AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildNoticeTwitCastingChannelIdAutocompleteHandler, AutocompletionResult (+19 more)

### Community 37 - "Bot State & Timers"
Cohesion: 0.23
Nodes (6): BotRole, IDatabase, ISubscriber, Task, TimeSpan, RedisTokenKeyProvisioner

### Community 38 - "Coordinator Entry/Shutdown"
Cohesion: 0.25
Nodes (5): CancellationTokenSource, Task, CancellationToken, int, GracefulShutdown

### Community 39 - "YouTube Member Commands"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 40 - "Twitcasting Commands"
Cohesion: 0.23
Nodes (10): Dictionary, CommandExample, CommandSummary, DiscordSocketClient, RequireGuildMemberCount, SlashCommand, Task, TwitcastingService (+2 more)

### Community 41 - "YouTube Member Interaction"
Cohesion: 0.13
Nodes (13): ConcurrentBag, bool, ConcurrentDictionary, HttpClient, IHttpClientFactory, Task, Timer, Video (+5 more)

### Community 42 - "DB Query Extensions"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 43 - "Coordinator Service"
Cohesion: 0.12
Nodes (10): DbContext, DbSet, ModelBuilder, MainDbContext, HoloVideos, NijisanjiVideos, NonApprovedVideos, OtherVideos (+2 more)

### Community 44 - "Twitcasting Spider Commands"
Cohesion: 0.26
Nodes (6): DateTime, int, Task, YTApiVideo, YoutubeDetectionService, Video

### Community 45 - "Redis Channels"
Cohesion: 0.19
Nodes (9): string, Cluster, Member, Notifier, RedisChannels, SharedState, Twitcasting, Twitch (+1 more)

### Community 47 - "Nijisanji Stream JSON"
Cohesion: 0.33
Nodes (7): CommandExample, CommandSummary, DiscordSocketClient, SlashCommand, Task, TwitchService, TwitchSpider

### Community 48 - "Utility & Official Guilds"
Cohesion: 0.20
Nodes (5): HashSet, List, string, Task, Utility

### Community 49 - "Detection Host Bootstrap"
Cohesion: 0.05
Nodes (33): DiscordStreamNotifyBot.Command.Help, DiscordStreamNotifyBot.Interaction.Help, IEqualityComparer, Func, CommonEqualityComparer, Alias, Command, CommandInfo (+25 more)

### Community 50 - "YouTube Channel Spider"
Cohesion: 0.29
Nodes (5): CancellationToken, Func, Task, TimeSpan, PeriodicRunner

### Community 51 - "Twitcasting HTTP Client"
Cohesion: 0.18
Nodes (11): 10. 可優化項目（claude 分支已有成品，對應階段順手移植）, 11. 驗證清單（部署前全過）, 1. 目標架構, 3. 設定, 6.1 方式 A：固定 shard 服務（初期採用）, 6.2 方式 B：`--scale` + shard 租約（主控層租約成熟後再切）, 6. Docker Compose, 7. 跨 shard 指令（Redis 三機制） (+3 more)

### Community 52 - "Twitch Update Debounce"
Cohesion: 0.18
Nodes (5): DateTime, EmbedBuilder, Video, YTChannelType, SharedExtensions

### Community 53 - "YoutubeApiService"
Cohesion: 0.20
Nodes (8): IEnumerable, IHttpClientFactory, List, string, Task, YouTubeService, YTApiVideo, YoutubeApiService

### Community 54 - "TwitchDetectionService.cs"
Cohesion: 0.14
Nodes (9): DiscordStreamNotifyBot.Scraper.Detection.Twitch, DiscordStreamNotifyBot.SharedService.Twitch, NoticeType, TwitchAuthorizationChangedPayload, TwitchReconcileRequestedPayload, TwitchStreamEventPayload, MissingGuildGeneration, TwitchGuildEligibilityStatus (+1 more)

### Community 55 - "Redis Token Provisioner"
Cohesion: 0.47
Nodes (4): DiscordStreamNotifyBot.Scraper.Detection.Twitcasting, Broadcaster, Movie, TwitCastingWebHookJson

### Community 56 - "TwitcastingService"
Cohesion: 0.21
Nodes (7): Broadcaster, DiscordSocketClient, EmojiService, NoticeCache, Task, TwitcastingService, TwitcastingNotification

### Community 57 - "CLAUDE.md"
Cohesion: 0.17
Nodes (11): Build & Run, Conventions, EF Core 鐵則, graphify, 制度條款, 外部契約（不可片面更改）, 指令文件, 架構要點（現行樹） (+3 more)

### Community 58 - "Twitcasting Backend Model"
Cohesion: 0.15
Nodes (12): HelixStream, ConcurrentDictionary, RedisValue, ScraperMetrics, SemaphoreSlim, Task, TimeSpan, TwitchDetectionService (+4 more)

### Community 59 - "Startup Preflight"
Cohesion: 0.42
Nodes (4): Func, Task, TimeSpan, StartupPreflight

### Community 60 - "Twitcasting Webhook Models"
Cohesion: 0.20
Nodes (8): DiscordStreamNotifyBot.Interaction.OwnerOnly, SendMsgToAllGuildService, DefaultMemberPermissions, RequireOwner, SlashCommand, Task, SendMsgToAllGuild, TopLevelModule

### Community 61 - "Broadcast Message Command"
Cohesion: 0.18
Nodes (8): ConcurrentQueue, DiscordStreamNotifyBot.Scraper.Detection.Twitch.Debounce, DebouncedEventArgs, Debouncer, bool, string, DebounceChannelUpdateMessage, TwitchDetectionService

### Community 62 - "TwitCasting Autocomplete"
Cohesion: 0.22
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 63 - "Twitch Autocomplete"
Cohesion: 0.29
Nodes (5): HttpClient, List, string, Task, TwitcastingClient

### Community 64 - "YouTube Autocomplete"
Cohesion: 0.15
Nodes (11): DiscordStreamNotifyBot.Scraper.Detection.Youtube, DiscordStreamNotifyBot.SharedService.Youtube.Json, DiscordStreamNotifyBot.Shared.Messages, NoticeType, NowStreamingHost, YTNotificationType, ConnectionMultiplexer, IDatabase (+3 more)

### Community 65 - "Notifier Program Entry"
Cohesion: 0.22
Nodes (6): DateTime, List, Task, TwitcastingDetectionService, DateTime, TwitcastingStream

### Community 66 - "DiscordStreamNotifyBot.HttpClients.Twitcasting.Model"
Cohesion: 0.31
Nodes (6): DiscordStreamNotifyBot.HttpClients.Twitcasting.Model, List, Broadcaster, GetMovieInfoResponse, Movie, GetUserInfoResponse

### Community 67 - "Interaction Base Module"
Cohesion: 0.24
Nodes (7): Assembly, CancellationToken, Exception, int, PeriodicTimer, Task, Program

### Community 69 - "Twitcasting Movie Info"
Cohesion: 0.22
Nodes (9): 8. 分階段實作步驟, 階段 0：止血 PR — shard 歸屬守衛, 階段 1：Solution 骨架 + Shared, 階段 2：Notifier 上線（先維持單 shard 行為）, 階段 3：Scraper 拆出 + Redis Streams 匯流排（完成，正確性待測試環境驗）, 階段 4：Coordinator（完成，正確性待測試環境驗）, 階段 5：跨 shard 指令與共享狀態（完成，正確性待測試環境驗）, 階段 6：Docker 化與部署驗證（檔案完成，實跑待測試環境） (+1 more)

### Community 70 - ".FixTCDbAsync"
Cohesion: 0.22
Nodes (7): DiscordStreamNotifyBot.Command.TwitCasting, Alias, Command, RequireContext, RequireOwner, Task, TwitCasting

### Community 71 - "DbContext Factory"
Cohesion: 0.25
Nodes (7): EF Core 遷移與基線化（本專案版）, 一次性基線化（舊的 EnsureCreated 正式庫）, 一般變更流程, 你必須先知道的三件專案特例, 啟動時不碰資料庫（重要）, 套用：本地/開發 vs 正式環境, 收尾

### Community 72 - "Twitcasting Categories JSON"
Cohesion: 0.16
Nodes (8): DiscordStreamNotifyBot.HttpClients, DiscordStreamNotifyBot.Auth, DiscordStreamNotifyBot.Scraper, DiscordStreamNotifyBot.Shared, DiscordStreamNotifyBot, BotPlayingStatus, int, Program

### Community 73 - "Nijisanji Liver JSON"
Cohesion: 0.12
Nodes (17): Clip, DateTime, EventSubSubscription, HttpClient, IReadOnlyList, Lazy, Regex, SemaphoreSlim (+9 more)

### Community 74 - "TwitCasting Webhook JSON"
Cohesion: 0.29
Nodes (8): bool, Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task, ReactionEventWrapper

### Community 80 - "DiscordSocketClient"
Cohesion: 0.15
Nodes (23): IRole, Alias, ClusterQueryService, Command, CommandExample, DiscordSocketClient, IEnumerable, List (+15 more)

### Community 81 - ".PublishYoutubeNotificationAsync"
Cohesion: 0.31
Nodes (5): DateTime, YTApiVideo, YoutubePubSubNotification, YoutubeNoticeType, YTNotificationType

### Community 83 - "IUserMessage"
Cohesion: 0.40
Nodes (3): ModelSnapshot, ModelBuilder, MainDbContextModelSnapshot

### Community 84 - "TwitcastingDetectionService"
Cohesion: 0.16
Nodes (11): GeneratedRegex, IEnumerable, YTChannelType, DateTime, DbSet, MainDbContext, Regex, Task (+3 more)

### Community 85 - "string"
Cohesion: 0.33
Nodes (5): For /graphify explain, For /graphify path, graphify reference: query, path, explain, Step 0 — Constrained query expansion (REQUIRED before traversal), Step 1 — Traversal

### Community 86 - "TcBackendStreamData.cs"
Cohesion: 0.44
Nodes (8): App, BackendMovie, Fmp4, Hls, Llfmp4, Streams, TcBackendStreamData, Webrtc

### Community 87 - "SyncModelDrift"
Cohesion: 0.33
Nodes (3): DiscordStreamNotifyBot.Migrations, ModelBuilder, RefactorDbContext

### Community 88 - ".SendConfirmMessageAsync"
Cohesion: 0.36
Nodes (6): IDMChannel, DiscordSocketClient, EmbedBuilder, ITextChannel, IUserMessage, Ext

### Community 91 - "20250320095452_RefactorDbContext.Designer.cs"
Cohesion: 0.33
Nodes (6): 2.1 `Shared`（共用 library）, 2.2 `Scraper`（爬蟲層，叢集唯一）, 2.3 `Notifier`（通知層 / shard，可多個）, 2.4 `Coordinator`（主控層，1 個）, 2.5 SharedService 逐服務拆分歸屬（判斷準則表）, 2. 專案拆分 (Solution Layout)

### Community 92 - "Program"
Cohesion: 0.29
Nodes (4): DiscordStreamNotifyBot.Coordinator, BotRole, int, Program

### Community 93 - "20250620094111_AddMaxSpiderCountSettingField.Designer.cs"
Cohesion: 0.33
Nodes (6): 4.1 拓撲, 4.2 DTO（`Shared/Messages/`）, 4.3 消費迴圈（Notifier）, 4.4 建群與 Preflight, 4.5 Redis 控制平面鍵（非 stream）, 4. 訊息契約：Redis Streams 通知匯流排

### Community 94 - "20250320095452_RefactorDbContext.Designer.cs"
Cohesion: 0.50
Nodes (4): 5.1 歸屬守衛（防多 shard 互刪設定，最高優先）, 5.2 心跳與重啟, 5.3 啟動連線檢查 (StartupPreflight), 5. Shard 歸屬與生命週期

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
Cohesion: 0.50
Nodes (3): Migration, MigrationBuilder, AddManualMemberCheckVideoFlag

### Community 105 - ".LoadInteractionFrom"
Cohesion: 0.29
Nodes (5): Assembly, Func, IEnumerable, IServiceCollection, Type

### Community 106 - "DiscordSocketClient"
Cohesion: 0.11
Nodes (16): DiscordStreamNotifyBot.SharedService.Youtube, DiscordStreamNotifyBot.SharedService.YoutubeMember, DiscordStreamNotifyBot.Interaction.Utility, DiscordStreamNotifyBot.Interaction.Attribute, DiscordStreamNotifyBot.Interaction.YoutubeMember, DiscordStreamNotifyBot.Interaction.TwitCasting, DiscordStreamNotifyBot.Command.Admin, DiscordStreamNotifyBot.Interaction.Twitch (+8 more)

### Community 113 - "NoticeCache"
Cohesion: 0.33
Nodes (6): DateTime, Func, List, object, TimeSpan, NoticeCache

### Community 114 - "StreamGroupInfo"
Cohesion: 0.43
Nodes (4): ModuleBase, EmbedBuilder, Task, TopLevelModule

### Community 115 - "string"
Cohesion: 0.11
Nodes (18): Microsoft.Extensions.DependencyInjection.Abstractions (10.0.1), System.Management (10.0.1), net8.0, Ben.Demystifier (0.4.1), Discord.Net (3.19.1), Dorssel.Utilities.Debounce (3.0.0), EFCore.NamingConventions (9.0.0), Google.Apis.YouTube.v3 (1.73.0.3981) (+10 more)

### Community 116 - "CancellationToken"
Cohesion: 0.10
Nodes (29): Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task, TwitchService (+21 more)

### Community 117 - ".OnReaction"
Cohesion: 0.33
Nodes (4): DiscordSocketClient, IMessage, IUserMessage, SocketReaction

### Community 118 - ".EvaluateAsync"
Cohesion: 0.47
Nodes (4): ConcurrentDictionary, Task, TimeSpan, TwitchGuildEligibilityEvaluator

### Community 119 - "RedisConnection"
Cohesion: 0.33
Nodes (4): ConnectionMultiplexer, Lazy, string, RedisConnection

### Community 120 - "string"
Cohesion: 0.15
Nodes (13): 0. 涉及專案, 10. Backend EventSub Webhook, 12. Frontend, 14. Grafana, 18. 建置與遷移, 19. 部署順序, 1. 不可偏離的決策, 20. 官方參考 (+5 more)

### Community 122 - "Category"
Cohesion: 0.70
Nodes (4): List, CategoriesJson, Category, SubCategory

### Community 123 - "TwitchApiResults.cs"
Cohesion: 0.31
Nodes (9): EventSubSubscription, IReadOnlyList, Stream, TwitchEventSubDeleteResult, TwitchEventSubDeleteStatus, TwitchEventSubEnsureMode, TwitchEventSubEnsureResult, TwitchEventSubSubscriptionsResult (+1 more)

### Community 124 - "16. 執行階段"
Cohesion: 0.22
Nodes (9): 16. 執行階段, 階段 0：前置確認, 階段 1：資料模型與 Backend 設定, 階段 2：Google/Twitch OAuth 隔離, 階段 3：Frontend, 階段 4：Twitch add資格與授權清理, 階段 5：StreamOnline 與 EventSub reconcile, 階段 6：Prometheus 與 Grafana (+1 more)

### Community 125 - "Prometheus / Grafana 監控"
Cohesion: 0.25
Nodes (8): Backend 指標, Coordinator 指標, Endpoints, Grafana, Prometheus, Prometheus / Grafana 監控, Scraper 指標, 排障

### Community 127 - "DiscordStreamNotifyBot.sln"
Cohesion: 0.29
Nodes (3): net8.0, prometheus-net.AspNetCore (8.2.1), Microsoft.NET.Sdk

### Community 128 - "DiscordWebhookClient"
Cohesion: 0.28
Nodes (6): CancellationToken, DiscordSocketClient, HttpClient, Task, DiscordWebhookClient, Message

### Community 129 - "17. 驗證矩陣"
Cohesion: 0.33
Nodes (6): 17.1 新增 spider, 17.2 EventSub, 17.3 授權失效, 17.4 OAuth, 17.5 Prometheus/Grafana, 17. 驗證矩陣

### Community 130 - "YoutubeChannelOwnedType"
Cohesion: 0.50
Nodes (3): DateTime, YTChannelType, YoutubeChannelOwnedType

### Community 131 - "GetAllRegistedWebHookJson.cs"
Cohesion: 0.67
Nodes (3): List, GetAllRegistedWebHookJson, Webhook

### Community 132 - "7. OAuth API 與流程隔離"
Cohesion: 0.40
Nodes (5): 7.1 API, 7.2 State, 7.3 Callback, 7.4 Twitch scopes, 7. OAuth API 與流程隔離

### Community 133 - ".LoadCommandFrom"
Cohesion: 0.40
Nodes (4): Assembly, IEnumerable, IServiceCollection, Type

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
Cohesion: 0.50
Nodes (3): net8.0, prometheus-net.AspNetCore (8.2.1), Microsoft.NET.Sdk

### Community 144 - "13. Prometheus"
Cohesion: 0.67
Nodes (3): 13.1 Backend 指標, 13.2 Scraper 指標, 13. Prometheus

### Community 145 - "4. 安全刪除狀態機"
Cohesion: 0.67
Nodes (3): 4.1 直播中授權失效, 4.2 關台後重新判斷, 4. 安全刪除狀態機

## Knowledge Gaps
- **267 isolated node(s):** `DiscordStreamNotifyBot.Coordinator`, `net8.0`, `prometheus-net.AspNetCore (8.2.1)`, `Microsoft.NET.Sdk`, `BotPlayingStatus` (+262 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **34 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `DiscordStreamNotifyBot.DataBase` connect `DiscordSocketClient` to `Notification Bus Consumer`, `EF Migrations`, `Scaling Architecture Docs`, `Command Help Module`, `20260719142803_AddTwitchBroadcasterAuthorization.Designer.cs`, `Bot Startup & Membership`, `20250620094111_AddMaxSpiderCountSettingField.Designer.cs`, `20260709091318_AddManualMemberCheckVideoFlag.Designer.cs`, `Logging`, `Coordinator Service`, `YoutubeApiService`, `TwitchDetectionService.cs`, `Redis Token Provisioner`, `Startup Preflight`, `YouTube Autocomplete`, `.FixTCDbAsync`, `Twitcasting Categories JSON`, `IUserMessage`, `SyncModelDrift`, `net8.0`, `MainDbContextFactory`?**
  _High betweenness centrality (0.128) - this node is a cross-community bridge._
- **Why does `DiscordStreamNotifyBot.Shared` connect `Twitcasting Categories JSON` to `YouTube Autocomplete`, `YouTube Stream Commands`, `Bot State & Timers`, `Coordinator Entry/Shutdown`, `DiscordSocketClient`, `Scaling Architecture Docs`, `Redis Channels`, `YouTube Channel Spider`, `Bot Startup & Membership`, `YoutubeApiService`, `TwitchDetectionService.cs`, `Redis Token Provisioner`, `Startup Preflight`, `Program`?**
  _High betweenness centrality (0.084) - this node is a cross-community bridge._
- **Why does `MainDbService` connect `Logging` to `YouTube Stream Commands`, `Twitcasting Service & DbContext`, `Notification Bus Consumer`, `Command Handler`, `Embed Builder Factory`, `Command Help Module`, `YouTube Slash Commands`, `Bot Startup & Membership`, `YouTube Reminder Scheduler`, `Member Check Settings`, `Twitcasting Commands`, `YouTube Member Interaction`, `Nijisanji Stream JSON`, `YoutubeApiService`, `TwitcastingService`, `Twitcasting Backend Model`, `YouTube Autocomplete`, `Notifier Program Entry`, `.FixTCDbAsync`, `DiscordSocketClient`, `NoticeCache`, `CancellationToken`?**
  _High betweenness centrality (0.083) - this node is a cross-community bridge._
- **What connects `DiscordStreamNotifyBot.Coordinator`, `net8.0`, `prometheus-net.AspNetCore (8.2.1)` to the rest of the system?**
  _267 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Admin Broadcast Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.09090909090909091 - nodes in this community are weakly interconnected._
- **Should `YouTube Stream Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.0544464609800363 - nodes in this community are weakly interconnected._
- **Should `Twitch Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.10793650793650794 - nodes in this community are weakly interconnected._