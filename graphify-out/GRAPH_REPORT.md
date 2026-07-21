# Graph Report - DiscordStreamNotifyBot  (2026-07-21)

## Corpus Check
- 184 files · ~96,848 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 2079 nodes · 3995 edges · 148 communities (120 shown, 28 thin omitted)
- Extraction: 93% EXTRACTED · 7% INFERRED · 0% AMBIGUOUS · INFERRED: 269 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `310e78af`
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

## God Nodes (most connected - your core abstractions)
1. `TwitchDetectionService` - 53 edges
2. `DiscordStreamNotifyBot.DataBase` - 44 edges
3. `DiscordStreamNotifyBot.DataBase.Table` - 44 edges
4. `Log` - 41 edges
5. `MainDbContext` - 37 edges
6. `MainDbService` - 33 edges
7. `Video` - 33 edges
8. `DiscordStreamNotifyBot.Shared` - 32 edges
9. `TwitchApiService` - 31 edges
10. `YoutubeStreamService` - 27 edges

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

## Communities (148 total, 28 thin omitted)

### Community 0 - "Admin Broadcast Commands"
Cohesion: 0.10
Nodes (12): DiscordStreamNotifyBot.DataBase.Table, BannerChange, DateTime, DbEntity, GuildConfig, GuildYoutubeMemberConfig, NoticeTwitcastingStreamChannel, NoticeTwitchStreamChannel (+4 more)

### Community 1 - "YouTube Stream Commands"
Cohesion: 0.06
Nodes (37): BotPlayingStatus, ConnectionMultiplexer, DiscordSocketClient, IDatabase, int, ISubscriber, IUser, Task (+29 more)

### Community 2 - "Twitch Commands"
Cohesion: 0.08
Nodes (24): For /graphify add and --watch, For /graphify query, For the commit hook and native CLAUDE.md integration, For --update and --cluster-only, /graphify, Honesty Rules, Interpreter guard for subcommands, Part A - Structural extraction for code files (+16 more)

### Community 3 - "Twitcasting Service & DbContext"
Cohesion: 0.06
Nodes (49): ChannelInfo, ClusterQueryType, DiscordStreamNotifyBot.Command.Normal, Dictionary, IReadOnlyCollection, Replies, Responses, SocketGuild (+41 more)

### Community 4 - "Solution & Dependencies"
Cohesion: 0.08
Nodes (23): Microsoft.EntityFrameworkCore.Design (9.0.3), Microsoft.EntityFrameworkCore.Relational (9.0.3), Microsoft.EntityFrameworkCore.Tools (9.0.3), Serilog (4.4.0), Serilog.Sinks.Console (6.1.1), Serilog.Sinks.File (7.0.0), Serilog.Sinks.Grafana.Loki (9.0.1), net8.0 (+15 more)

### Community 5 - "Help & Owner Services"
Cohesion: 0.20
Nodes (6): IDatabase, IEnumerable, string, Task, TimeSpan, ClusterService

### Community 6 - "Notification Bus Consumer"
Cohesion: 0.27
Nodes (9): IRole, CommandExample, CommandSummary, DiscordSocketClient, ITextChannel, RequireGuildMemberCount, SlashCommand, Task (+1 more)

### Community 7 - "Help Autocomplete Handlers"
Cohesion: 0.13
Nodes (13): 1. Shared — 定義契約, 2. Scraper — 偵測並 publish, 3. Notifier — 消費並發送, 動工前先讀一個既有平台, 收尾檢查, 新增偵測平台 / 通知事件, 步驟（依相依順序，Shared → Scraper → Notifier）, 偵測 → 匯流排 → 發送 路徑除錯 (+5 more)

### Community 8 - "EF Migrations"
Cohesion: 0.26
Nodes (8): ServiceProvider, DetectionHost, CancellationToken, PeriodicTimer, string, Task, TimeSpan, ScraperService

### Community 9 - "Precondition Attributes"
Cohesion: 0.05
Nodes (31): PreconditionAttribute, CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task, RequireGuildMemberCountAttribute, CommandInfo (+23 more)

### Community 10 - "Command Handler"
Cohesion: 0.33
Nodes (7): CommandExample, CommandSummary, DiscordSocketClient, SlashCommand, Task, YoutubeStreamService, YoutubeChannelSpider

### Community 11 - "Embed Builder Factory"
Cohesion: 0.12
Nodes (17): DateTime, EmbedBuilder, YTApiVideo, EmbedBuilderFactory, DateTime, Video, DateTime, string (+9 more)

### Community 12 - "Scaling Architecture Docs"
Cohesion: 0.12
Nodes (12): DiscordStreamNotifyBot.SharedService.Twitcasting, DiscordStreamNotifyBot.SharedService, DiscordStreamNotifyBot.DataBase, DiscordStreamNotifyBot.Interaction, Emote, BotPlayingStatus, IInteractionService, DiscordSocketClient (+4 more)

### Community 13 - "Interaction Extensions"
Cohesion: 0.11
Nodes (12): IDiscordInteraction, Process, DiscordSocketClient, EmbedBuilder, IEmote, IInteractionContext, IMessage, IUserMessage (+4 more)

### Community 14 - "Command Help Module"
Cohesion: 0.18
Nodes (19): DiscordStreamNotifyBot.Command.YoutubeMember, ICommandService, Alias, Command, CommandExample, RequireContext, RequireOwner, Summary (+11 more)

### Community 15 - "Video/Embed Extensions"
Cohesion: 0.10
Nodes (19): SocketCommandContext, Assembly, DateTime, DiscordSocketClient, EmbedBuilder, Func, ICommandContext, IEmote (+11 more)

### Community 16 - "SharedService Core"
Cohesion: 0.32
Nodes (6): SocketMessage, CommandService, DiscordSocketClient, IServiceProvider, Task, CommandHandler

### Community 17 - "YouTube Detection Service"
Cohesion: 0.11
Nodes (18): Backend, Bot（本 repo）, MySQL（兩端都已連同一個庫）, 儲存層（現況為 Redis）, 加密與 blob 格式（兩端一致）, 加密金鑰處理, 影響檔案一覽, 待決策（給實作 session） (+10 more)

### Community 18 - "YouTube Slash Commands"
Cohesion: 0.08
Nodes (35): InteractionModuleBase, NowStreamingHost, SocketInteractionContext, Task, TopLevelModule, AutocompletionResult, CommandExample, CommandSummary (+27 more)

### Community 19 - "Bot Startup & Membership"
Cohesion: 0.06
Nodes (33): ButtonCheckData, DiscordStreamNotifyBot.Interaction.Utility.Service, DiscordStreamNotifyBot.Interaction.OwnerOnly.Service, IInteractionService, SendAllPayload, EmbedBuilder, SlashCommandInfo, HelpService (+25 more)

### Community 20 - "Auth / Token Crypto"
Cohesion: 0.12
Nodes (10): IDataStore, TokenCrypto, TokenManager, Task, ITokenDataStore, IDatabase, string, Task (+2 more)

### Community 21 - "Bot Entry Points"
Cohesion: 0.12
Nodes (13): DiscordSocketClient, Embed, HttpClient, IEnumerable, IHttpClientFactory, List, MessageComponent, NoticeCache (+5 more)

### Community 22 - "YouTube Reminder Scheduler"
Cohesion: 0.15
Nodes (14): AutocompletionResult, CommandExample, CommandSummary, DiscordSocketClient, IAutocompleteInteraction, IChannel, IInteractionContext, IParameterInfo (+6 more)

### Community 23 - "Interaction Handler"
Cohesion: 0.17
Nodes (10): IResult, SocketInteraction, SocketSlashCommandDataOption, DiscordSocketClient, IInteractionContext, InteractionService, IServiceProvider, SlashCommandInfo (+2 more)

### Community 24 - "Command/Interaction Modules"
Cohesion: 0.22
Nodes (5): DiscordStreamNotifyBot.SharedService.Youtube, DiscordStreamNotifyBot.Auth, DiscordStreamNotifyBot.SharedService.YoutubeMember, DiscordStreamNotifyBot.Interaction.YoutubeMember, DiscordStreamNotifyBot

### Community 25 - "YouTube Member Service"
Cohesion: 0.20
Nodes (7): Attribute, string, CommandExampleAttribute, string, CommandExampleAttribute, string, CommandSummaryAttribute

### Community 26 - "Notice Cache & Messaging"
Cohesion: 0.11
Nodes (17): 1. 背景與動機, 2. 新增跨 repo 契約, 3. A（小幫手）改動, 4. B（StreamRecordTools）改動, 5. 部署順序與相容性, 6. 驗證, 7. 影響範圍, A1. `Shared/RedisChannels.cs` (+9 more)

### Community 27 - "YouTube Reminder Timer"
Cohesion: 0.20
Nodes (10): 15. 預期修改檔案, 16. 完成定義, 1. 背景, 2. 目標, 3. 非目標, 4. 已確認的產品決策, 8.1 首次設定流程, 8.2 語系設定指令 (+2 more)

### Community 28 - "Command Attributes"
Cohesion: 0.20
Nodes (10): 10. 預期修改檔案, 11. 完成定義, 1. 背景, 2. 目標, 3. 非目標, 4. 技術選型, 6.1 例外事件, 6. Facade 相容契約 (+2 more)

### Community 29 - "Graphify Tooling Docs"
Cohesion: 0.22
Nodes (9): 12. 分階段執行, 階段 0：建立基準與字串清冊, 階段 1：Localization 基礎與繁中資源化, 階段 2：資料庫與語系設定, 階段 3：Slash command 註冊本地化, 階段 4：共用互動、Help 與首次設定, 階段 5：一般 Interaction 模組, 階段 6：背景通知與會限 DM (+1 more)

### Community 30 - "Logging"
Cohesion: 0.36
Nodes (5): RequireContext, SlashCommand, Task, YoutubeMember, YoutubeMemberService

### Community 31 - "Cluster Leader/Heartbeat"
Cohesion: 0.09
Nodes (18): DiscordStreamNotifyBot.Coordinator, Counter, Gauge, HashSet, StreamGroupInfo, string, CoordinatorMetrics, CancellationToken (+10 more)

### Community 32 - "Member Check Settings"
Cohesion: 0.10
Nodes (16): GoogleAuthorizationCodeFlow, IDMChannel, SocketGuildUser, SocketMessageComponent, Task, DiscordSocketClient, EmbedBuilder, ITextChannel (+8 more)

### Community 33 - "YouTube Spider Commands"
Cohesion: 0.06
Nodes (30): ConsoleColor, DelegatingHandler, HttpRequestMessage, HttpResponseMessage, ILogEventSink, ITextFormatter, LogEvent, LogEventLevel (+22 more)

### Community 34 - "Twitch Channel Commands"
Cohesion: 0.33
Nodes (6): Console 備援, Log 與 Loki, Loki 主動推送, Serilog Pipeline, 排障, 檔案路由

### Community 35 - "Twitcasting Detection"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 37 - "Bot State & Timers"
Cohesion: 0.29
Nodes (5): IDatabase, ISubscriber, Task, TimeSpan, RedisTokenKeyProvisioner

### Community 39 - "YouTube Member Commands"
Cohesion: 0.33
Nodes (6): HelpService, InteractionService, IServiceProvider, SlashCommand, Task, Help

### Community 40 - "Twitcasting Commands"
Cohesion: 0.29
Nodes (8): CommandExample, CommandSummary, DiscordSocketClient, RequireGuildMemberCount, SlashCommand, Task, TwitcastingService, TwitcastingSpider

### Community 41 - "YouTube Member Interaction"
Cohesion: 0.14
Nodes (14): ConcurrentBag, bool, ConcurrentDictionary, DateTime, HttpClient, IEnumerable, IHttpClientFactory, Task (+6 more)

### Community 42 - "DB Query Extensions"
Cohesion: 0.25
Nodes (6): DiscordStreamNotifyBot.Interaction.Help, AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, HelpGetModulesAutocompleteHandler

### Community 43 - "Coordinator Service"
Cohesion: 0.11
Nodes (12): DbContext, DbSet, ModelBuilder, Video, MainDbContext, HoloVideos, NijisanjiVideos, NonApprovedVideos (+4 more)

### Community 44 - "Twitcasting Spider Commands"
Cohesion: 0.31
Nodes (5): DateTime, int, Task, YTApiVideo, YoutubeDetectionService

### Community 45 - "Redis Channels"
Cohesion: 0.19
Nodes (9): string, Cluster, Member, Notifier, RedisChannels, SharedState, Twitcasting, Twitch (+1 more)

### Community 46 - "Twitch Spider Commands"
Cohesion: 0.25
Nodes (8): 13.1 編譯與靜態檢查, 13.2 Slash command 註冊, 13.3 Locale resolver, 13.4 首次設定, 13.5 通知, 13.6 YouTube 會限驗證, 13.7 範圍守衛, 13. 驗證矩陣

### Community 47 - "Nijisanji Stream JSON"
Cohesion: 0.32
Nodes (7): CommandExample, CommandSummary, DiscordSocketClient, SlashCommand, Task, TwitchService, TwitchSpider

### Community 48 - "Utility & Official Guilds"
Cohesion: 0.12
Nodes (11): DateTime, List, Channel, EventLiver, Liver, NijisanjiStreamJson, HashSet, List (+3 more)

### Community 49 - "Detection Host Bootstrap"
Cohesion: 0.16
Nodes (12): DiscordStreamNotifyBot.Command.Help, Alias, Command, CommandService, IServiceProvider, string, Summary, Task (+4 more)

### Community 50 - "YouTube Channel Spider"
Cohesion: 0.29
Nodes (5): CancellationToken, Func, Task, TimeSpan, PeriodicRunner

### Community 51 - "Twitcasting HTTP Client"
Cohesion: 0.05
Nodes (41): 10. 可優化項目（claude 分支已有成品，對應階段順手移植）, 11. 驗證清單（部署前全過）, 1. 目標架構, 2.1 `Shared`（共用 library）, 2.2 `Scraper`（爬蟲層，叢集唯一）, 2.3 `Notifier`（通知層 / shard，可多個）, 2.4 `Coordinator`（主控層，1 個）, 2.5 SharedService 逐服務拆分歸屬（判斷準則表） (+33 more)

### Community 52 - "Twitch Update Debounce"
Cohesion: 0.18
Nodes (5): DateTime, EmbedBuilder, Video, YTChannelType, SharedExtensions

### Community 53 - "YoutubeApiService"
Cohesion: 0.18
Nodes (8): IEnumerable, IHttpClientFactory, List, string, Task, YouTubeService, YTApiVideo, YoutubeApiService

### Community 54 - "TwitchDetectionService.cs"
Cohesion: 0.26
Nodes (6): DbContextOptions, string, MainDbService, string, Task, MySqlDataStore

### Community 55 - "Redis Token Provisioner"
Cohesion: 0.25
Nodes (8): 7. 分階段執行, 階段 0：建立基準, 階段 1：加入 Serilog 與 bootstrap logger, 階段 2：搬移 console 與檔案路由, 階段 3：切換 Loki sink, 階段 4：整理 facade 與 Discord.Net adapter, 階段 5：移除自製 sink 與更新文件, 階段 6：後續漸進式 structured logging（不阻擋本計畫完成）

### Community 56 - "TwitcastingService"
Cohesion: 0.29
Nodes (6): Broadcaster, DiscordSocketClient, EmojiService, NoticeCache, Task, TwitcastingService

### Community 57 - "CLAUDE.md"
Cohesion: 0.17
Nodes (11): Build & Run, Conventions, EF Core 鐵則, graphify, 制度條款, 外部契約（不可片面更改）, 指令文件, 架構要點（現行樹） (+3 more)

### Community 58 - "Twitcasting Backend Model"
Cohesion: 0.06
Nodes (39): HelixStream, IReadOnlyDictionary, ConcurrentDictionary, DateTime, EventSubSubscription, RedisValue, ScraperMetrics, SemaphoreSlim (+31 more)

### Community 59 - "Startup Preflight"
Cohesion: 0.24
Nodes (7): int, Program, BotRole, Func, Task, TimeSpan, StartupPreflight

### Community 60 - "Twitcasting Webhook Models"
Cohesion: 0.20
Nodes (8): DiscordStreamNotifyBot.Interaction.OwnerOnly, SendMsgToAllGuildService, DefaultMemberPermissions, RequireOwner, SlashCommand, Task, SendMsgToAllGuild, TopLevelModule

### Community 61 - "Broadcast Message Command"
Cohesion: 0.17
Nodes (8): ConcurrentQueue, DiscordStreamNotifyBot.Scraper.Detection.Twitch.Debounce, DebouncedEventArgs, Debouncer, bool, string, DebounceChannelUpdateMessage, TwitchDetectionService

### Community 62 - "TwitCasting Autocomplete"
Cohesion: 0.22
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 63 - "Twitch Autocomplete"
Cohesion: 0.29
Nodes (5): HttpClient, List, string, Task, TwitcastingClient

### Community 64 - "YouTube Autocomplete"
Cohesion: 0.12
Nodes (17): DiscordStreamNotifyBot.Scraper.Detection.Youtube, DiscordStreamNotifyBot.Scraper.Detection.Twitch, DiscordStreamNotifyBot.SharedService.Youtube.Json, DiscordStreamNotifyBot.Shared.Messages, NoticeType, NowStreamingHost, TwitchAuthorizationChangedPayload, TwitchReconcileRequestedPayload (+9 more)

### Community 65 - "Notifier Program Entry"
Cohesion: 0.22
Nodes (6): DateTime, List, Task, TwitcastingDetectionService, DateTime, TwitcastingStream

### Community 66 - "DiscordStreamNotifyBot.HttpClients.Twitcasting.Model"
Cohesion: 0.32
Nodes (5): List, Broadcaster, GetMovieInfoResponse, Movie, GetUserInfoResponse

### Community 67 - "Interaction Base Module"
Cohesion: 0.24
Nodes (7): Assembly, CancellationToken, Exception, int, PeriodicTimer, Task, Program

### Community 68 - "TwitCasting DB Fix Command"
Cohesion: 0.08
Nodes (13): Migration, MigrationBuilder, RefactorDbContext, MigrationBuilder, ModifyTwitCastingTable, MigrationBuilder, AddMaxSpiderCountSettingField, MigrationBuilder (+5 more)

### Community 70 - ".FixTCDbAsync"
Cohesion: 0.22
Nodes (7): DiscordStreamNotifyBot.Command.TwitCasting, Alias, Command, RequireContext, RequireOwner, Task, TwitCasting

### Community 71 - "DbContext Factory"
Cohesion: 0.25
Nodes (7): EF Core 遷移與基線化（本專案版）, 一次性基線化（舊的 EnsureCreated 正式庫）, 一般變更流程, 你必須先知道的三件專案特例, 啟動時不碰資料庫（重要）, 套用：本地/開發 vs 正式環境, 收尾

### Community 72 - "Twitcasting Categories JSON"
Cohesion: 0.18
Nodes (8): DiscordStreamNotifyBot.HttpClients, DiscordStreamNotifyBot.HttpClients.Twitcasting.Model, DiscordStreamNotifyBot.Scraper, DiscordStreamNotifyBot.Shared, DiscordStreamNotifyBot.Scraper.Detection.Twitcasting, Broadcaster, Movie, TwitCastingWebHookJson

### Community 73 - "Nijisanji Liver JSON"
Cohesion: 0.29
Nodes (7): 11.1 現況限制, 11.2 目標作法, 11.3 YouTube, 11.4 Twitch, 11.5 TwitCasting, 11.6 YouTube 會限驗證, 11. 通知與背景訊息

### Community 74 - "TwitCasting Webhook JSON"
Cohesion: 0.13
Nodes (17): IDisposable, bool, Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task (+9 more)

### Community 80 - "DiscordSocketClient"
Cohesion: 0.27
Nodes (14): Alias, ClusterQueryService, Command, CommandExample, DiscordSocketClient, IEnumerable, List, RequireContext (+6 more)

### Community 81 - ".PublishYoutubeNotificationAsync"
Cohesion: 0.29
Nodes (5): Assembly, Func, IEnumerable, IServiceCollection, Type

### Community 82 - "ITextChannel"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 83 - "IUserMessage"
Cohesion: 0.07
Nodes (16): DiscordStreamNotifyBot.Migrations, ModelSnapshot, ModelBuilder, RefactorDbContext, ModelBuilder, ModifyTwitCastingTable, ModelBuilder, AddMaxSpiderCountSettingField (+8 more)

### Community 84 - "TwitcastingDetectionService"
Cohesion: 0.22
Nodes (8): GeneratedRegex, YTChannelType, DateTime, Regex, Task, Video, YTChannelType, YoutubeDetectionService

### Community 85 - "string"
Cohesion: 0.33
Nodes (5): For /graphify explain, For /graphify path, graphify reference: query, path, explain, Step 0 — Constrained query expansion (REQUIRED before traversal), Step 1 — Traversal

### Community 86 - "TcBackendStreamData.cs"
Cohesion: 0.44
Nodes (8): App, BackendMovie, Fmp4, Hls, Llfmp4, Streams, TcBackendStreamData, Webrtc

### Community 87 - "SyncModelDrift"
Cohesion: 0.40
Nodes (3): DiscordStreamNotifyBot.Interaction.Utility, DiscordStreamNotifyBot.Command.Admin, DiscordStreamNotifyBot.SharedService.Cluster

### Community 88 - ".SendConfirmMessageAsync"
Cohesion: 0.40
Nodes (3): DiscordStreamNotifyBot.Command.Youtube, DiscordStreamNotifyBot.Command.Attribute, DiscordStreamNotifyBot.Command.Twitch

### Community 89 - "CommandTextEqualityComparer"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 90 - "YoutubePubSubNotification"
Cohesion: 0.33
Nodes (6): 5.1 支援值, 5.2 公開內容與背景通知, 5.3 私人即時回覆, 5.4 延遲會限驗證 DM, 5.5 併發安全, 5. 語系模型與解析規則

### Community 91 - "20250320095452_RefactorDbContext.Designer.cs"
Cohesion: 0.47
Nodes (4): ConcurrentDictionary, Task, TimeSpan, TwitchGuildEligibilityEvaluator

### Community 92 - "Program"
Cohesion: 0.40
Nodes (5): 10.1 共用回覆 API, 10.2 Precondition 與 handler 錯誤, 10.3 例外訊息, 10.4 第一階段模組, 10. 執行期互動本地化

### Community 93 - "20250620094111_AddMaxSpiderCountSettingField.Designer.cs"
Cohesion: 0.40
Nodes (5): 6.1 指令註冊資源, 6.2 執行期訊息資源, 6.3 Help 長文, 6.4 Localizer API, 6. 資源架構

### Community 94 - "20250320095452_RefactorDbContext.Designer.cs"
Cohesion: 0.40
Nodes (5): 5.1 Console, 5.2 非容器檔案, 5.3 Loki, 5.4 `LOKI_URL` 相容性, 5. 目標架構

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
Cohesion: 0.20
Nodes (5): IEqualityComparer, Func, CommonEqualityComparer, Func, CommonEqualityComparer

### Community 101 - "graphify"
Cohesion: 0.40
Nodes (4): Timer, Video, YTChannelType, ReminderItem

### Community 102 - "extraction-spec.md"
Cohesion: 0.33
Nodes (4): CancellationTokenSource, CancellationToken, int, GracefulShutdown

### Community 103 - "net8.0"
Cohesion: 0.40
Nodes (5): 8.1 編譯與靜態檢查, 8.2 Console 與檔案, 8.3 Loki, 8.4 生命週期, 8. 驗證矩陣

### Community 106 - "DiscordSocketClient"
Cohesion: 0.10
Nodes (18): AutocompleteHandler, DiscordStreamNotifyBot.Interaction.Attribute, DiscordStreamNotifyBot.Interaction.TwitCasting, DiscordStreamNotifyBot.Interaction.Help.Service, DiscordStreamNotifyBot.Interaction.Twitch, DiscordStreamNotifyBot.Interaction.Youtube, GuildNoticeTwitCastingChannelIdAutocompleteHandler, GuildTwitCastingSpiderAutocompleteHandler (+10 more)

### Community 113 - "NoticeCache"
Cohesion: 0.21
Nodes (8): HttpException, DateTime, Func, List, object, TimeSpan, NoticeCache, Embed

### Community 114 - "StreamGroupInfo"
Cohesion: 0.43
Nodes (4): ModuleBase, EmbedBuilder, Task, TopLevelModule

### Community 115 - "string"
Cohesion: 0.11
Nodes (18): Microsoft.Extensions.DependencyInjection.Abstractions (10.0.1), System.Management (10.0.1), net8.0, Ben.Demystifier (0.4.1), Discord.Net (3.19.1), Dorssel.Utilities.Debounce (3.0.0), EFCore.NamingConventions (9.0.0), Google.Apis.YouTube.v3 (1.73.0.3981) (+10 more)

### Community 116 - "CancellationToken"
Cohesion: 0.05
Nodes (54): Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task, TwitchService (+46 more)

### Community 119 - "RedisConnection"
Cohesion: 0.33
Nodes (4): ConnectionMultiplexer, Lazy, string, RedisConnection

### Community 120 - "string"
Cohesion: 0.14
Nodes (13): 0. 涉及專案, 10. Backend EventSub Webhook, 12. Frontend, 14. Grafana, 18. 建置與遷移, 19. 部署順序, 1. 不可偏離的決策, 20. 官方參考 (+5 more)

### Community 121 - "MainDbContextFactory"
Cohesion: 0.50
Nodes (4): 14.1 建議部署順序, 14.2 相容性, 14.3 回滾, 14. 部署與回滾

### Community 122 - "Category"
Cohesion: 0.70
Nodes (4): List, CategoriesJson, Category, SubCategory

### Community 123 - "TwitchApiResults.cs"
Cohesion: 0.50
Nodes (4): 7.1 `GuildConfig.Locale`, 7.2 `YoutubeMemberCheck.Locale`, 7.3 Migration 鐵則, 7. 資料庫變更

### Community 124 - "16. 執行階段"
Cohesion: 0.22
Nodes (9): 16. 執行階段, 階段 0：前置確認, 階段 1：資料模型與 Backend 設定, 階段 2：Google/Twitch OAuth 隔離, 階段 3：Frontend, 階段 4：Twitch add資格與授權清理, 階段 5：StreamOnline 與 EventSub reconcile, 階段 6：Prometheus 與 Grafana (+1 more)

### Community 125 - "Prometheus / Grafana 監控"
Cohesion: 0.22
Nodes (8): Backend 指標, Coordinator 指標, Endpoints, Grafana, Prometheus, Prometheus / Grafana 監控, Scraper 指標, 排障

### Community 126 - ".DispatchFromBusAsync"
Cohesion: 0.50
Nodes (4): 9.1 Discord.Net 設定, 9.2 指令名稱, 9.3 Command signature, 9. Slash Command Localization

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
Cohesion: 0.29
Nodes (3): net8.0, prometheus-net.AspNetCore (8.2.1), Microsoft.NET.Sdk

### Community 144 - "13. Prometheus"
Cohesion: 0.67
Nodes (3): 13.1 Backend 指標, 13.2 Scraper 指標, 13. Prometheus

### Community 145 - "4. 安全刪除狀態機"
Cohesion: 0.67
Nodes (3): 4.1 直播中授權失效, 4.2 關台後重新判斷, 4. 安全刪除狀態機

## Knowledge Gaps
- **347 isolated node(s):** `net8.0`, `prometheus-net.AspNetCore (8.2.1)`, `Microsoft.NET.Sdk`, `BotPlayingStatus`, `DiscordStreamNotifyBot.Command.Normal` (+342 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **28 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `DiscordStreamNotifyBot.DataBase` connect `Scaling Architecture Docs` to `YouTube Autocomplete`, `Member Check Settings`, `Admin Broadcast Commands`, `Twitcasting Service & DbContext`, `.FixTCDbAsync`, `Twitcasting Categories JSON`, `.LoadInteractionFrom`, `DiscordSocketClient`, `Command Help Module`, `Bot Startup & Membership`, `IUserMessage`, `Twitch Update Debounce`, `TwitchDetectionService.cs`, `SyncModelDrift`, `.SendConfirmMessageAsync`, `Command/Interaction Modules`, `YoutubeApiService`, `Startup Preflight`?**
  _High betweenness centrality (0.109) - this node is a cross-community bridge._
- **Why does `MainDbService` connect `TwitchDetectionService.cs` to `YouTube Stream Commands`, `Twitcasting Service & DbContext`, `Notification Bus Consumer`, `Command Handler`, `Command Help Module`, `YouTube Slash Commands`, `Bot Startup & Membership`, `Bot Entry Points`, `YouTube Reminder Scheduler`, `Logging`, `Member Check Settings`, `Twitcasting Commands`, `YouTube Member Interaction`, `Nijisanji Stream JSON`, `YoutubeApiService`, `TwitcastingService`, `Twitcasting Backend Model`, `YouTube Autocomplete`, `Notifier Program Entry`, `.FixTCDbAsync`, `DiscordSocketClient`, `NoticeCache`, `CancellationToken`?**
  _High betweenness centrality (0.070) - this node is a cross-community bridge._
- **Why does `DiscordStreamNotifyBot.Shared` connect `Twitcasting Categories JSON` to `Twitcasting Service & DbContext`, `Help & Owner Services`, `Scaling Architecture Docs`, `Bot Startup & Membership`, `Command/Interaction Modules`, `Cluster Leader/Heartbeat`, `Bot State & Timers`, `Redis Channels`, `Utility & Official Guilds`, `YouTube Channel Spider`, `Twitch Update Debounce`, `YoutubeApiService`, `Startup Preflight`, `YouTube Autocomplete`, `SyncModelDrift`, `.SendConfirmMessageAsync`, `extraction-spec.md`, `DiscordSocketClient`, `.EvaluateAsync`?**
  _High betweenness centrality (0.068) - this node is a cross-community bridge._
- **What connects `net8.0`, `prometheus-net.AspNetCore (8.2.1)`, `Microsoft.NET.Sdk` to the rest of the system?**
  _347 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Admin Broadcast Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.10153846153846154 - nodes in this community are weakly interconnected._
- **Should `YouTube Stream Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.055051421657592255 - nodes in this community are weakly interconnected._
- **Should `Twitch Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.08 - nodes in this community are weakly interconnected._