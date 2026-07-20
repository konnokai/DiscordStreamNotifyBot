# Graph Report - DiscordStreamNotifyBot  (2026-07-20)

## Corpus Check
- 183 files · ~92,363 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1952 nodes · 3836 edges · 148 communities (118 shown, 30 thin omitted)
- Extraction: 93% EXTRACTED · 7% INFERRED · 0% AMBIGUOUS · INFERRED: 270 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `4e90f6f9`
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
- RedisConnection
- string
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
- 20250620094111_AddMaxSpiderCountSettingField.Designer.cs
- 20260709091318_AddManualMemberCheckVideoFlag.Designer.cs

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
- `Bot` --references--> `NotificationBusConsumer`  [EXTRACTED]
  src/DiscordStreamNotifyBot.Notifier/Bot.cs → src/DiscordStreamNotifyBot.Notifier/NotificationBusConsumer.cs
- `Bot` --references--> `BotConfig`  [EXTRACTED]
  src/DiscordStreamNotifyBot.Notifier/Bot.cs → src/DiscordStreamNotifyBot.Shared/BotConfig.cs
- `Bot` --references--> `MainDbService`  [EXTRACTED]
  src/DiscordStreamNotifyBot.Notifier/Bot.cs → src/DiscordStreamNotifyBot.Shared/DataBase/MainDbService.cs
- `AdministrationService` --references--> `MainDbService`  [EXTRACTED]
  src/DiscordStreamNotifyBot.Notifier/Command/Admin/AdministraitonService.cs → src/DiscordStreamNotifyBot.Shared/DataBase/MainDbService.cs

## Import Cycles
- None detected.

## Communities (148 total, 30 thin omitted)

### Community 0 - "Admin Broadcast Commands"
Cohesion: 0.05
Nodes (26): DiscordStreamNotifyBot.DataBase.Table, BannerChange, DateTime, DbEntity, GuildConfig, GuildYoutubeMemberConfig, NijisanjiVideos, NonApprovedVideos (+18 more)

### Community 1 - "YouTube Stream Commands"
Cohesion: 0.24
Nodes (8): IDatabase, int, StreamEntry, StreamGroupInfo, string, Task, TimeSpan, NotificationBus

### Community 2 - "Twitch Commands"
Cohesion: 0.11
Nodes (16): LokiLogEntry, long, Queue, Action, CancellationToken, CancellationTokenSource, HttpClient, int (+8 more)

### Community 3 - "Twitcasting Service & DbContext"
Cohesion: 0.06
Nodes (49): ChannelInfo, ClusterQueryType, DiscordStreamNotifyBot.Command.Normal, Dictionary, IReadOnlyCollection, Replies, Responses, SocketGuild (+41 more)

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
Cohesion: 0.33
Nodes (7): CommandExample, CommandSummary, DiscordSocketClient, SlashCommand, Task, YoutubeStreamService, YoutubeChannelSpider

### Community 11 - "Embed Builder Factory"
Cohesion: 0.20
Nodes (7): DateTime, EmbedBuilder, YTApiVideo, EmbedBuilderFactory, HoloVideos, DateTime, Video

### Community 12 - "Scaling Architecture Docs"
Cohesion: 0.12
Nodes (9): DiscordStreamNotifyBot.SharedService.Youtube, DiscordStreamNotifyBot.SharedService.Twitcasting, DiscordStreamNotifyBot.SharedService.Twitch, DiscordStreamNotifyBot.Interaction, TwitcastingEmbedBuilderFactory, NoticeType, NoticeType, NowStreamingHost (+1 more)

### Community 13 - "Interaction Extensions"
Cohesion: 0.06
Nodes (26): IDiscordInteraction, IDisposable, Process, Assembly, DiscordSocketClient, EmbedBuilder, Func, IEmote (+18 more)

### Community 14 - "Command Help Module"
Cohesion: 0.18
Nodes (19): DiscordStreamNotifyBot.Command.YoutubeMember, ICommandService, Alias, Command, CommandExample, RequireContext, RequireOwner, Summary (+11 more)

### Community 15 - "Video/Embed Extensions"
Cohesion: 0.15
Nodes (13): SocketCommandContext, DiscordSocketClient, EmbedBuilder, Func, ICommandContext, IEmote, IMessage, IMessageChannel (+5 more)

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
Cohesion: 0.09
Nodes (21): DiscordStreamNotifyBot.Interaction.Utility.Service, IInteractionService, EmbedBuilder, SlashCommandInfo, HelpService, UtilityService, DefaultMemberPermissions, DiscordSocketClient (+13 more)

### Community 20 - "Auth / Token Crypto"
Cohesion: 0.11
Nodes (11): DiscordStreamNotifyBot.Auth, IDataStore, TokenCrypto, TokenManager, Task, ITokenDataStore, IDatabase, string (+3 more)

### Community 21 - "Bot Entry Points"
Cohesion: 0.12
Nodes (15): NowStreamingHost, DiscordSocketClient, Embed, HttpClient, IEnumerable, IHttpClientFactory, List, MessageComponent (+7 more)

### Community 22 - "YouTube Reminder Scheduler"
Cohesion: 0.15
Nodes (14): AutocompletionResult, CommandExample, CommandSummary, DiscordSocketClient, IAutocompleteInteraction, IChannel, IInteractionContext, IParameterInfo (+6 more)

### Community 23 - "Interaction Handler"
Cohesion: 0.10
Nodes (15): DiscordStreamNotifyBot.SharedService, Emote, IResult, SocketInteraction, SocketSlashCommandDataOption, IInteractionService, DiscordSocketClient, IInteractionContext (+7 more)

### Community 24 - "Command/Interaction Modules"
Cohesion: 0.16
Nodes (11): BotPlayingStatus, ConnectionMultiplexer, DiscordSocketClient, IDatabase, int, ISubscriber, IUser, Task (+3 more)

### Community 25 - "YouTube Member Service"
Cohesion: 0.20
Nodes (7): Attribute, string, CommandExampleAttribute, string, CommandExampleAttribute, string, CommandSummaryAttribute

### Community 26 - "Notice Cache & Messaging"
Cohesion: 0.11
Nodes (17): 1. 背景與動機, 2. 新增跨 repo 契約, 3. A（小幫手）改動, 4. B（StreamRecordTools）改動, 5. 部署順序與相容性, 6. 驗證, 7. 影響範圍, A1. `Shared/RedisChannels.cs` (+9 more)

### Community 27 - "YouTube Reminder Timer"
Cohesion: 0.20
Nodes (10): CancellationToken, IDatabase, int, StreamEntry, Task, TimeSpan, TwitcastingService, TwitchService (+2 more)

### Community 28 - "Command Attributes"
Cohesion: 0.19
Nodes (10): ButtonCheckData, DiscordStreamNotifyBot.Interaction.OwnerOnly.Service, SendAllPayload, bool, DiscordSocketClient, Embed, Task, ButtonCheckData (+2 more)

### Community 29 - "Graphify Tooling Docs"
Cohesion: 0.24
Nodes (7): bool, DiscordSocketClient, HttpClient, string, Task, Timer, UptimeKumaClient

### Community 30 - "Logging"
Cohesion: 0.50
Nodes (4): RequireContext, SlashCommand, Task, YoutubeMember

### Community 31 - "Cluster Leader/Heartbeat"
Cohesion: 0.05
Nodes (33): DiscordStreamNotifyBot.Coordinator, Counter, Gauge, HashSet, StreamGroupInfo, string, CoordinatorMetrics, CancellationToken (+25 more)

### Community 32 - "Member Check Settings"
Cohesion: 0.10
Nodes (17): GoogleAuthorizationCodeFlow, IDMChannel, SocketGuildUser, SocketMessageComponent, Task, YoutubeMemberService, DiscordSocketClient, EmbedBuilder (+9 more)

### Community 33 - "YouTube Spider Commands"
Cohesion: 0.23
Nodes (9): ConsoleColor, LogFileType, LogLevel, LogMessage, Exception, int, object, string (+1 more)

### Community 34 - "Twitch Channel Commands"
Cohesion: 0.22
Nodes (4): Console 備援, Log 與 Loki, 主動推送, 排障

### Community 35 - "Twitcasting Detection"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildTwitchSpiderAutocompleteHandler

### Community 36 - "Shared Extensions"
Cohesion: 0.23
Nodes (4): ServiceProvider, DetectionHost, BotConfig, Action

### Community 37 - "Bot State & Timers"
Cohesion: 0.33
Nodes (5): IDatabase, ISubscriber, Task, TimeSpan, RedisTokenKeyProvisioner

### Community 38 - "Coordinator Entry/Shutdown"
Cohesion: 0.27
Nodes (4): Task, Task, Task, TimeSpan

### Community 39 - "YouTube Member Commands"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 40 - "Twitcasting Commands"
Cohesion: 0.28
Nodes (8): CommandExample, CommandSummary, DiscordSocketClient, RequireGuildMemberCount, SlashCommand, Task, TwitcastingService, TwitcastingSpider

### Community 41 - "YouTube Member Interaction"
Cohesion: 0.13
Nodes (13): ConcurrentBag, bool, ConcurrentDictionary, DateTime, HttpClient, IHttpClientFactory, Task, YoutubeApiService (+5 more)

### Community 42 - "DB Query Extensions"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 43 - "Coordinator Service"
Cohesion: 0.24
Nodes (6): DbContext, IDesignTimeDbContextFactory, DbSet, ModelBuilder, MainDbContext, MainDbContextFactory

### Community 44 - "Twitcasting Spider Commands"
Cohesion: 0.21
Nodes (7): DateTime, int, Task, YTApiVideo, YTChannelType, YoutubeDetectionService, Video

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
Cohesion: 0.06
Nodes (31): DiscordStreamNotifyBot.Command.Help, DiscordStreamNotifyBot.Interaction.Help, IEqualityComparer, Alias, Command, CommandInfo, CommandService, IServiceProvider (+23 more)

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
Cohesion: 0.16
Nodes (8): IEnumerable, IHttpClientFactory, List, string, Task, YouTubeService, YTApiVideo, YoutubeApiService

### Community 54 - "TwitchDetectionService.cs"
Cohesion: 0.42
Nodes (3): string, Task, MySqlDataStore

### Community 55 - "Redis Token Provisioner"
Cohesion: 0.83
Nodes (3): Broadcaster, Movie, TwitCastingWebHookJson

### Community 56 - "TwitcastingService"
Cohesion: 0.19
Nodes (9): EmbedBuilder, DiscordSocketClient, EmojiService, NoticeCache, Task, TwitcastingService, DateTime, TwitcastingStream (+1 more)

### Community 57 - "CLAUDE.md"
Cohesion: 0.17
Nodes (11): Build & Run, Conventions, EF Core 鐵則, graphify, 制度條款, 外部契約（不可片面更改）, 指令文件, 架構要點（現行樹） (+3 more)

### Community 58 - "Twitcasting Backend Model"
Cohesion: 0.06
Nodes (37): HelixStream, IReadOnlyDictionary, ConcurrentDictionary, DateTime, EventSubSubscription, RedisValue, ScraperMetrics, SemaphoreSlim (+29 more)

### Community 59 - "Startup Preflight"
Cohesion: 0.42
Nodes (4): Func, Task, TimeSpan, StartupPreflight

### Community 60 - "Twitcasting Webhook Models"
Cohesion: 0.13
Nodes (12): DiscordStreamNotifyBot.Command.TwitCasting, DbContextOptions, SendMsgToAllGuildService, TwitCasting, DefaultMemberPermissions, RequireOwner, SlashCommand, Task (+4 more)

### Community 61 - "Broadcast Message Command"
Cohesion: 0.17
Nodes (8): ConcurrentQueue, DiscordStreamNotifyBot.Scraper.Detection.Twitch.Debounce, DebouncedEventArgs, Debouncer, bool, string, DebounceChannelUpdateMessage, TwitchDetectionService

### Community 62 - "TwitCasting Autocomplete"
Cohesion: 0.22
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 63 - "Twitch Autocomplete"
Cohesion: 0.31
Nodes (5): HttpClient, List, string, Task, TwitcastingClient

### Community 64 - "YouTube Autocomplete"
Cohesion: 0.12
Nodes (15): DiscordStreamNotifyBot.Scraper.Detection.Youtube, DiscordStreamNotifyBot.Scraper.Detection.Twitch, DiscordStreamNotifyBot.SharedService.Youtube.Json, DiscordStreamNotifyBot.Shared.Messages, TwitchAuthorizationChangedPayload, TwitchReconcileRequestedPayload, TwitchStreamEventPayload, MissingGuildGeneration (+7 more)

### Community 65 - "Notifier Program Entry"
Cohesion: 0.29
Nodes (4): DateTime, List, Task, TwitcastingDetectionService

### Community 66 - "DiscordStreamNotifyBot.HttpClients.Twitcasting.Model"
Cohesion: 0.27
Nodes (6): DiscordStreamNotifyBot.HttpClients.Twitcasting.Model, List, Broadcaster, GetMovieInfoResponse, Movie, GetUserInfoResponse

### Community 67 - "Interaction Base Module"
Cohesion: 0.16
Nodes (10): Assembly, CancellationToken, Exception, int, PeriodicTimer, Task, Program, int (+2 more)

### Community 68 - "TwitCasting DB Fix Command"
Cohesion: 0.10
Nodes (12): DiscordStreamNotifyBot.Migrations, Migration, MigrationBuilder, RefactorDbContext, MigrationBuilder, ModifyTwitCastingTable, MigrationBuilder, AddMaxSpiderCountSettingField (+4 more)

### Community 69 - "Twitcasting Movie Info"
Cohesion: 0.22
Nodes (9): 8. 分階段實作步驟, 階段 0：止血 PR — shard 歸屬守衛, 階段 1：Solution 骨架 + Shared, 階段 2：Notifier 上線（先維持單 shard 行為）, 階段 3：Scraper 拆出 + Redis Streams 匯流排（完成，正確性待測試環境驗）, 階段 4：Coordinator（完成，正確性待測試環境驗）, 階段 5：跨 shard 指令與共享狀態（完成，正確性待測試環境驗）, 階段 6：Docker 化與部署驗證（檔案完成，實跑待測試環境） (+1 more)

### Community 70 - ".FixTCDbAsync"
Cohesion: 0.22
Nodes (6): Alias, Command, RequireContext, RequireOwner, Task, Broadcaster

### Community 71 - "DbContext Factory"
Cohesion: 0.25
Nodes (7): EF Core 遷移與基線化（本專案版）, 一次性基線化（舊的 EnsureCreated 正式庫）, 一般變更流程, 你必須先知道的三件專案特例, 啟動時不碰資料庫（重要）, 套用：本地/開發 vs 正式環境, 收尾

### Community 72 - "Twitcasting Categories JSON"
Cohesion: 0.23
Nodes (6): DiscordStreamNotifyBot.HttpClients, DiscordStreamNotifyBot.Scraper, DiscordStreamNotifyBot.Shared, DiscordStreamNotifyBot, DiscordStreamNotifyBot.Scraper.Detection.Twitcasting, BotPlayingStatus

### Community 73 - "Nijisanji Liver JSON"
Cohesion: 0.39
Nodes (4): DateTime, EmbedBuilder, TwitchEmbedBuilderFactory, TwitchNotification

### Community 74 - "TwitCasting Webhook JSON"
Cohesion: 0.29
Nodes (8): bool, Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task, ReactionEventWrapper

### Community 80 - "DiscordSocketClient"
Cohesion: 0.15
Nodes (23): IRole, Alias, ClusterQueryService, Command, CommandExample, DiscordSocketClient, IEnumerable, List (+15 more)

### Community 81 - ".PublishYoutubeNotificationAsync"
Cohesion: 0.22
Nodes (10): DateTime, string, YTChannelType, NotifyType, TwitcastingNotification, TwitchNoticeType, TwitchNotification, YoutubeMemberVideoLogNotification (+2 more)

### Community 82 - "ITextChannel"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildYoutubeChannelSpiderAutocompleteHandler

### Community 83 - "IUserMessage"
Cohesion: 0.40
Nodes (3): ModelSnapshot, ModelBuilder, MainDbContextModelSnapshot

### Community 84 - "TwitcastingDetectionService"
Cohesion: 0.17
Nodes (10): GeneratedRegex, IEnumerable, DateTime, DbSet, MainDbContext, Regex, Task, Video (+2 more)

### Community 85 - "string"
Cohesion: 0.33
Nodes (5): For /graphify explain, For /graphify path, graphify reference: query, path, explain, Step 0 — Constrained query expansion (REQUIRED before traversal), Step 1 — Traversal

### Community 86 - "TcBackendStreamData.cs"
Cohesion: 0.44
Nodes (8): App, BackendMovie, Fmp4, Hls, Llfmp4, Streams, TcBackendStreamData, Webrtc

### Community 88 - ".SendConfirmMessageAsync"
Cohesion: 0.40
Nodes (3): DiscordStreamNotifyBot.Command.Youtube, DiscordStreamNotifyBot.Command.Attribute, DiscordStreamNotifyBot.Command.Twitch

### Community 89 - "CommandTextEqualityComparer"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 90 - "YoutubePubSubNotification"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 91 - "20250320095452_RefactorDbContext.Designer.cs"
Cohesion: 0.33
Nodes (6): 2.1 `Shared`（共用 library）, 2.2 `Scraper`（爬蟲層，叢集唯一）, 2.3 `Notifier`（通知層 / shard，可多個）, 2.4 `Coordinator`（主控層，1 個）, 2.5 SharedService 逐服務拆分歸屬（判斷準則表）, 2. 專案拆分 (Solution Layout)

### Community 92 - "Program"
Cohesion: 0.40
Nodes (5): 一、`claude` 分支是你最大的資產，也是最大的陷阱, 三、使用者已做的決策，不要重新辯論, 二、你在活的生產系統旁施工, 給未來 session 的信, 這套制度最可能的退化方式，與預防

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

### Community 101 - "graphify"
Cohesion: 0.40
Nodes (4): Timer, Video, YTChannelType, ReminderItem

### Community 102 - "extraction-spec.md"
Cohesion: 0.40
Nodes (4): CancellationToken, CancellationTokenSource, int, GracefulShutdown

### Community 106 - "DiscordSocketClient"
Cohesion: 0.11
Nodes (18): AutocompleteHandler, DiscordStreamNotifyBot.SharedService.YoutubeMember, DiscordStreamNotifyBot.Interaction.Utility, DiscordStreamNotifyBot.Interaction.Attribute, DiscordStreamNotifyBot.Interaction.YoutubeMember, DiscordStreamNotifyBot.Interaction.OwnerOnly, DiscordStreamNotifyBot.Interaction.TwitCasting, DiscordStreamNotifyBot.Command.Admin (+10 more)

### Community 113 - "NoticeCache"
Cohesion: 0.17
Nodes (10): HttpException, SendAllPayload, NoticeType, DateTime, Func, List, object, TimeSpan (+2 more)

### Community 114 - "StreamGroupInfo"
Cohesion: 0.43
Nodes (4): ModuleBase, EmbedBuilder, Task, TopLevelModule

### Community 115 - "string"
Cohesion: 0.11
Nodes (18): Microsoft.Extensions.DependencyInjection.Abstractions (10.0.1), System.Management (10.0.1), net8.0, Ben.Demystifier (0.4.1), Discord.Net (3.19.1), Dorssel.Utilities.Debounce (3.0.0), EFCore.NamingConventions (9.0.0), Google.Apis.YouTube.v3 (1.73.0.3981) (+10 more)

### Community 116 - "CancellationToken"
Cohesion: 0.06
Nodes (50): Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task, TwitchService (+42 more)

### Community 119 - "RedisConnection"
Cohesion: 0.33
Nodes (4): ConnectionMultiplexer, Lazy, string, RedisConnection

### Community 120 - "string"
Cohesion: 0.15
Nodes (13): 0. 涉及專案, 10. Backend EventSub Webhook, 12. Frontend, 14. Grafana, 18. 建置與遷移, 19. 部署順序, 1. 不可偏離的決策, 20. 官方參考 (+5 more)

### Community 122 - "Category"
Cohesion: 0.70
Nodes (4): List, CategoriesJson, Category, SubCategory

### Community 124 - "16. 執行階段"
Cohesion: 0.22
Nodes (9): 16. 執行階段, 階段 0：前置確認, 階段 1：資料模型與 Backend 設定, 階段 2：Google/Twitch OAuth 隔離, 階段 3：Frontend, 階段 4：Twitch add資格與授權清理, 階段 5：StreamOnline 與 EventSub reconcile, 階段 6：Prometheus 與 Grafana (+1 more)

### Community 125 - "Prometheus / Grafana 監控"
Cohesion: 0.25
Nodes (8): Backend 指標, Coordinator 指標, Endpoints, Grafana, Prometheus, Prometheus / Grafana 監控, Scraper 指標, 排障

### Community 127 - "DiscordStreamNotifyBot.sln"
Cohesion: 0.50
Nodes (3): net8.0, prometheus-net.AspNetCore (8.2.1), Microsoft.NET.Sdk

### Community 129 - "17. 驗證矩陣"
Cohesion: 0.33
Nodes (6): 17.1 新增 spider, 17.2 EventSub, 17.3 授權失效, 17.4 OAuth, 17.5 Prometheus/Grafana, 17. 驗證矩陣

### Community 131 - "GetAllRegistedWebHookJson.cs"
Cohesion: 0.67
Nodes (3): List, GetAllRegistedWebHookJson, Webhook

### Community 132 - "7. OAuth API 與流程隔離"
Cohesion: 0.40
Nodes (5): 7.1 API, 7.2 State, 7.3 Callback, 7.4 Twitch scopes, 7. OAuth API 與流程隔離

### Community 133 - ".LoadCommandFrom"
Cohesion: 0.17
Nodes (6): Assembly, DateTime, IEnumerable, IServiceCollection, Type, Video

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
- **251 isolated node(s):** `DiscordStreamNotifyBot.Shared.csproj`, `DiscordStreamNotifyBot.Scraper`, `DiscordStreamNotifyBot.Notifier`, `DiscordStreamNotifyBot.Coordinator`, `net8.0` (+246 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **30 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `DiscordStreamNotifyBot.DataBase` connect `DiscordSocketClient` to `Admin Broadcast Commands`, `Twitcasting Service & DbContext`, `Notification Bus Consumer`, `EF Migrations`, `Scaling Architecture Docs`, `Command Help Module`, `20260719142803_AddTwitchBroadcasterAuthorization.Designer.cs`, `20250620094111_AddMaxSpiderCountSettingField.Designer.cs`, `20260709091318_AddManualMemberCheckVideoFlag.Designer.cs`, `Auth / Token Crypto`, `Interaction Handler`, `Command Attributes`, `Member Check Settings`, `Coordinator Service`, `YoutubeApiService`, `Startup Preflight`, `Twitcasting Webhook Models`, `YouTube Autocomplete`, `Twitcasting Categories JSON`, `IUserMessage`, `SyncModelDrift`, `.SendConfirmMessageAsync`, `net8.0`?**
  _High betweenness centrality (0.103) - this node is a cross-community bridge._
- **Why does `MainDbService` connect `Twitcasting Webhook Models` to `Twitcasting Service & DbContext`, `Notification Bus Consumer`, `Command Handler`, `Command Help Module`, `YouTube Slash Commands`, `Bot Startup & Membership`, `Bot Entry Points`, `YouTube Reminder Scheduler`, `Command/Interaction Modules`, `Command Attributes`, `Logging`, `Member Check Settings`, `Twitcasting Commands`, `YouTube Member Interaction`, `Nijisanji Stream JSON`, `YoutubeApiService`, `TwitchDetectionService.cs`, `TwitcastingService`, `Twitcasting Backend Model`, `YouTube Autocomplete`, `Notifier Program Entry`, `DiscordSocketClient`, `NoticeCache`, `CancellationToken`?**
  _High betweenness centrality (0.085) - this node is a cross-community bridge._
- **Why does `BotConfig` connect `Shared Extensions` to `Member Check Settings`, `Notifier Program Entry`, `Interaction Base Module`, `Twitcasting Backend Model`, `Bot State & Timers`, `Coordinator Entry/Shutdown`, `Twitcasting Categories JSON`, `YouTube Member Interaction`, `Nijisanji Stream JSON`, `Bot Startup & Membership`, `CancellationToken`, `Bot Entry Points`, `Command/Interaction Modules`, `TwitcastingService`, `Startup Preflight`, `Cluster Leader/Heartbeat`?**
  _High betweenness centrality (0.072) - this node is a cross-community bridge._
- **What connects `DiscordStreamNotifyBot.Shared.csproj`, `DiscordStreamNotifyBot.Scraper`, `DiscordStreamNotifyBot.Notifier` to the rest of the system?**
  _251 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Admin Broadcast Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.04964539007092199 - nodes in this community are weakly interconnected._
- **Should `Twitch Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.11397849462365592 - nodes in this community are weakly interconnected._
- **Should `Twitcasting Service & DbContext` be split into smaller, more focused modules?**
  _Cohesion score 0.05651176133103844 - nodes in this community are weakly interconnected._