# Graph Report - DiscordStreamNotifyBot  (2026-07-11)

## Corpus Check
- 169 files · ~76,390 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1700 nodes · 3246 edges · 121 communities (94 shown, 27 thin omitted)
- Extraction: 93% EXTRACTED · 7% INFERRED · 0% AMBIGUOUS · INFERRED: 228 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `e17f5f04`
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
- YouTube Member Modules
- Uptime Kuma Client
- Redis Token Provisioner
- Twitcasting Channel Info
- Scraper Service
- Twitcasting Backend Model
- Startup Preflight
- Twitcasting Webhook Models
- Broadcast Message Command
- TwitCasting Autocomplete
- Twitch Autocomplete
- YouTube Autocomplete
- Notifier Program Entry
- Periodic Runner
- Interaction Base Module
- TwitCasting DB Fix Command
- Twitcasting Movie Info
- MainDbContextModelSnapshot.cs
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
- EmbedBuilder
- ITextChannel
- IUserMessage
- ModifyTwitCastingTable
- string
- AddMaxSpiderCountSettingField
- SyncModelDrift
- AddManualMemberCheckVideoFlag
- YoutubeChannelOwnedType
- 20250320095452_RefactorDbContext.Designer.cs
- 20250603065853_ModifyTwitCastingTable.Designer.cs
- 20250620094111_AddMaxSpiderCountSettingField.Designer.cs
- 20250320095452_RefactorDbContext.Designer.cs
- 20250603065853_ModifyTwitCastingTable.Designer.cs
- 20250620094111_AddMaxSpiderCountSettingField.Designer.cs
- TwitchSpider
- graphify reference: GitHub clone and cross-repo merge
- graphify reference: transcribe video and audio
- graphify
- extraction-spec.md
- .claude/CLAUDE.md (graphify trigger)
- DetectionHost (Scraper composition root)
- Confidence rubric (EXTRACTED/INFERRED/AMBIGUOUS)
- AST structural extraction (Part A)
- Community detection & clustering
- God nodes & surprising connections
- Knowledge graph (graph.json)
- Semantic extraction (parallel subagents)
- HelpDescription (bot feature summary)
- TcBackendStreamData.cs
- .PromptUserConfirmAsync
- TopLevelModule
- TwitcastingDetectionService
- .FixTCDbAsync
- .GenerateSuggestionsAsync
- CommonEqualityComparer
- ModifyTwitCastingTable
- GetAllRegistedWebHookJson.cs
- 20250620094111_AddMaxSpiderCountSettingField.Designer.cs
- 20260709091318_AddManualMemberCheckVideoFlag.Designer.cs

## God Nodes (most connected - your core abstractions)
1. `DiscordStreamNotifyBot.DataBase` - 43 edges
2. `DiscordStreamNotifyBot.DataBase.Table` - 42 edges
3. `MainDbContext` - 35 edges
4. `MainDbService` - 33 edges
5. `Video` - 33 edges
6. `DiscordStreamNotifyBot.Shared` - 28 edges
7. `YoutubeStreamService` - 27 edges
8. `YoutubeDetectionService` - 25 edges
9. `BotConfig` - 25 edges
10. `TwitchService` - 23 edges

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

## Communities (121 total, 27 thin omitted)

### Community 0 - "Admin Broadcast Commands"
Cohesion: 0.07
Nodes (21): DiscordStreamNotifyBot.DataBase.Table, BannerChange, DateTime, DbEntity, GuildConfig, GuildYoutubeMemberConfig, NoticeTwitcastingStreamChannel, NoticeTwitchStreamChannel (+13 more)

### Community 1 - "YouTube Stream Commands"
Cohesion: 0.06
Nodes (33): BotPlayingStatus, RedisValue, ConnectionMultiplexer, DiscordSocketClient, IDatabase, int, ISubscriber, IUser (+25 more)

### Community 2 - "Twitch Commands"
Cohesion: 0.06
Nodes (41): Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task, TwitchService (+33 more)

### Community 3 - "Twitcasting Service & DbContext"
Cohesion: 0.13
Nodes (7): Assembly, DateTime, IEnumerable, IServiceCollection, Type, Video, YTChannelType

### Community 4 - "Solution & Dependencies"
Cohesion: 0.04
Nodes (41): Microsoft.EntityFrameworkCore.Design (9.0.3), Microsoft.EntityFrameworkCore.Relational (9.0.3), Microsoft.EntityFrameworkCore.Tools (9.0.3), Microsoft.Extensions.DependencyInjection.Abstractions (10.0.1), System.Management (10.0.1), net8.0, Microsoft.NET.Sdk, net8.0 (+33 more)

### Community 5 - "Help & Owner Services"
Cohesion: 0.33
Nodes (11): DateTime, List, Attributes, Data, Liver, NijisanjiStreamJson, Relationships, YoutubeChannel (+3 more)

### Community 6 - "Notification Bus Consumer"
Cohesion: 0.14
Nodes (15): AutocompletionResult, CommandExample, CommandSummary, DiscordSocketClient, IAutocompleteInteraction, IChannel, IInteractionContext, IParameterInfo (+7 more)

### Community 7 - "Help Autocomplete Handlers"
Cohesion: 0.13
Nodes (13): 1. Shared — 定義契約, 2. Scraper — 偵測並 publish, 3. Notifier — 消費並發送, 動工前先讀一個既有平台, 收尾檢查, 新增偵測平台 / 通知事件, 步驟（依相依順序，Shared → Scraper → Notifier）, 偵測 → 匯流排 → 發送 路徑除錯 (+5 more)

### Community 9 - "Precondition Attributes"
Cohesion: 0.05
Nodes (31): PreconditionAttribute, CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task, RequireGuildMemberCountAttribute, CommandInfo (+23 more)

### Community 10 - "Command Handler"
Cohesion: 0.18
Nodes (8): DiscordStreamNotifyBot.Command, SocketMessage, CommandService, DiscordSocketClient, IServiceProvider, Task, CommandHandler, ICommandService

### Community 11 - "Embed Builder Factory"
Cohesion: 0.08
Nodes (21): DateTime, EmbedBuilder, YTApiVideo, EmbedBuilderFactory, HoloVideos, NijisanjiVideos, NonApprovedVideos, OtherVideos (+13 more)

### Community 12 - "Scaling Architecture Docs"
Cohesion: 0.13
Nodes (10): DiscordStreamNotifyBot.HttpClients, DiscordStreamNotifyBot.SharedService.Twitcasting, DiscordStreamNotifyBot.SharedService, DiscordStreamNotifyBot.Command.Youtube, DiscordStreamNotifyBot.Command.Attribute, DiscordStreamNotifyBot.DataBase, DiscordStreamNotifyBot.Command.Twitch, DiscordStreamNotifyBot.Interaction (+2 more)

### Community 13 - "Interaction Extensions"
Cohesion: 0.06
Nodes (25): IDiscordInteraction, Process, Assembly, DiscordSocketClient, EmbedBuilder, Func, IEmote, IEnumerable (+17 more)

### Community 14 - "Command Help Module"
Cohesion: 0.06
Nodes (47): ChannelInfo, ClusterQueryType, DiscordStreamNotifyBot.Command.Normal, Dictionary, GuildSnapshot, IReadOnlyCollection, Replies, Responses (+39 more)

### Community 15 - "Video/Embed Extensions"
Cohesion: 0.17
Nodes (12): SocketCommandContext, DiscordSocketClient, EmbedBuilder, Func, ICommandContext, IEmote, IMessage, IMessageChannel (+4 more)

### Community 16 - "SharedService Core"
Cohesion: 0.20
Nodes (8): NoticeType, DateTime, Func, List, object, TimeSpan, NoticeCache, Embed

### Community 17 - "YouTube Detection Service"
Cohesion: 0.11
Nodes (18): Backend, Bot（本 repo）, MySQL（兩端都已連同一個庫）, 儲存層（現況為 Redis）, 加密與 blob 格式（兩端一致）, 加密金鑰處理, 影響檔案一覽, 待決策（給實作 session） (+10 more)

### Community 18 - "YouTube Slash Commands"
Cohesion: 0.27
Nodes (12): CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, IChannel, NoticeType, RequireBotPermission, RequireContext (+4 more)

### Community 19 - "Bot Startup & Membership"
Cohesion: 0.07
Nodes (27): ButtonCheckData, DiscordStreamNotifyBot.Interaction.Utility.Service, DiscordStreamNotifyBot.Interaction.Help.Service, IInteractionService, SendAllPayload, EmbedBuilder, SlashCommandInfo, HelpService (+19 more)

### Community 20 - "Auth / Token Crypto"
Cohesion: 0.12
Nodes (10): IDataStore, TokenCrypto, TokenManager, Task, ITokenDataStore, IDatabase, string, Task (+2 more)

### Community 21 - "Bot Entry Points"
Cohesion: 0.16
Nodes (7): DiscordStreamNotifyBot.Auth, DiscordStreamNotifyBot.Scraper, DiscordStreamNotifyBot.Shared, DiscordStreamNotifyBot.Interaction.OwnerOnly.Service, DiscordStreamNotifyBot, NoticeType, SendAllPayload

### Community 22 - "YouTube Reminder Scheduler"
Cohesion: 0.17
Nodes (9): GeneratedRegex, DateTime, DbSet, MainDbContext, Regex, Task, Video, YTChannelType (+1 more)

### Community 23 - "Interaction Handler"
Cohesion: 0.12
Nodes (14): Emote, IResult, SocketInteraction, SocketSlashCommandDataOption, IInteractionService, DiscordSocketClient, IInteractionContext, InteractionService (+6 more)

### Community 24 - "Command/Interaction Modules"
Cohesion: 0.40
Nodes (3): DiscordStreamNotifyBot.Interaction.Utility, DiscordStreamNotifyBot.Command.Admin, DiscordStreamNotifyBot.SharedService.Cluster

### Community 25 - "YouTube Member Service"
Cohesion: 0.07
Nodes (30): ConcurrentBag, bool, ConcurrentDictionary, DateTime, HttpClient, IEnumerable, IHttpClientFactory, Task (+22 more)

### Community 26 - "Notice Cache & Messaging"
Cohesion: 0.11
Nodes (17): 1. 背景與動機, 2. 新增跨 repo 契約, 3. A（小幫手）改動, 4. B（StreamRecordTools）改動, 5. 部署順序與相容性, 6. 驗證, 7. 影響範圍, A1. `Shared/RedisChannels.cs` (+9 more)

### Community 27 - "YouTube Reminder Timer"
Cohesion: 0.19
Nodes (10): DbContext, DateTime, int, Task, YTApiVideo, YTChannelType, YoutubeDetectionService, DbSet (+2 more)

### Community 28 - "Command Attributes"
Cohesion: 0.12
Nodes (13): Attribute, DiscordStreamNotifyBot.Interaction.Attribute, DiscordStreamNotifyBot.Interaction.TwitCasting, DiscordStreamNotifyBot.SharedService.Twitch, DiscordStreamNotifyBot.Interaction.Twitch, string, CommandExampleAttribute, string (+5 more)

### Community 29 - "Graphify Tooling Docs"
Cohesion: 0.08
Nodes (24): For /graphify add and --watch, For /graphify query, For the commit hook and native CLAUDE.md integration, For --update and --cluster-only, /graphify, Honesty Rules, Interpreter guard for subcommands, Part A - Structural extraction for code files (+16 more)

### Community 30 - "Logging"
Cohesion: 0.18
Nodes (9): ConsoleColor, Exception, LogMessage, LogType, object, string, Task, Log (+1 more)

### Community 31 - "Cluster Leader/Heartbeat"
Cohesion: 0.26
Nodes (5): IDatabase, string, Task, TimeSpan, ClusterService

### Community 32 - "Member Check Settings"
Cohesion: 0.13
Nodes (15): GoogleAuthorizationCodeFlow, IDMChannel, SocketGuildUser, SocketMessageComponent, DiscordSocketClient, EmbedBuilder, ITextChannel, IUserMessage (+7 more)

### Community 33 - "YouTube Spider Commands"
Cohesion: 0.18
Nodes (19): DiscordStreamNotifyBot.Command.YoutubeMember, ICommandService, Alias, Command, CommandExample, RequireContext, RequireOwner, Summary (+11 more)

### Community 34 - "Twitch Channel Commands"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildNoticeTwitchChannelIdAutocompleteHandler

### Community 35 - "Twitcasting Detection"
Cohesion: 0.33
Nodes (7): CommandExample, CommandSummary, DiscordSocketClient, SlashCommand, Task, YoutubeStreamService, YoutubeChannelSpider

### Community 36 - "Shared Extensions"
Cohesion: 0.29
Nodes (4): Action, ServiceProvider, DetectionHost, BotConfig

### Community 37 - "Bot State & Timers"
Cohesion: 0.29
Nodes (5): IDatabase, ISubscriber, Task, TimeSpan, RedisTokenKeyProvisioner

### Community 38 - "Coordinator Entry/Shutdown"
Cohesion: 0.15
Nodes (8): CancellationTokenSource, DiscordStreamNotifyBot.Coordinator, BotRole, Task, Program, CancellationToken, int, GracefulShutdown

### Community 39 - "YouTube Member Commands"
Cohesion: 0.26
Nodes (6): DbContextOptions, string, MainDbService, string, Task, MySqlDataStore

### Community 40 - "Twitcasting Commands"
Cohesion: 0.29
Nodes (8): CommandExample, CommandSummary, DiscordSocketClient, RequireGuildMemberCount, SlashCommand, Task, TwitcastingService, TwitcastingSpider

### Community 41 - "YouTube Member Interaction"
Cohesion: 0.50
Nodes (4): RequireContext, SlashCommand, Task, YoutubeMember

### Community 42 - "DB Query Extensions"
Cohesion: 0.24
Nodes (8): CancellationToken, IDatabase, int, PeriodicTimer, string, Task, CoordinatorService, IEnumerable

### Community 43 - "Coordinator Service"
Cohesion: 0.39
Nodes (6): CancellationToken, PeriodicTimer, string, Task, TimeSpan, ScraperService

### Community 44 - "Twitcasting Spider Commands"
Cohesion: 0.15
Nodes (10): Task, Task, YoutubeMemberService, bool, DiscordSocketClient, HttpClient, string, Task (+2 more)

### Community 45 - "Redis Channels"
Cohesion: 0.23
Nodes (9): string, Cluster, Member, Notifier, RedisChannels, SharedState, Twitcasting, Twitch (+1 more)

### Community 46 - "Twitch Spider Commands"
Cohesion: 0.11
Nodes (29): NowStreamingHost, Alias, ClusterQueryService, Command, CommandExample, DiscordSocketClient, IEnumerable, List (+21 more)

### Community 47 - "Nijisanji Stream JSON"
Cohesion: 0.25
Nodes (6): Broadcaster, DiscordSocketClient, EmojiService, NoticeCache, Task, TwitcastingService

### Community 48 - "Utility & Official Guilds"
Cohesion: 0.18
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

### Community 53 - "YouTube Member Modules"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 54 - "Uptime Kuma Client"
Cohesion: 0.12
Nodes (13): AutocompleteHandler, DiscordStreamNotifyBot.SharedService.Youtube, DiscordStreamNotifyBot.SharedService.YoutubeMember, DiscordStreamNotifyBot.Interaction.YoutubeMember, DiscordStreamNotifyBot.Interaction.Youtube, AutocompletionResult, IAutocompleteInteraction, IInteractionContext (+5 more)

### Community 55 - "Redis Token Provisioner"
Cohesion: 0.47
Nodes (4): DiscordStreamNotifyBot.Scraper.Detection.Twitcasting, Broadcaster, Movie, TwitCastingWebHookJson

### Community 56 - "Twitcasting Channel Info"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 57 - "Scraper Service"
Cohesion: 0.28
Nodes (8): CommandExample, CommandSummary, DiscordSocketClient, RequireGuildMemberCount, SlashCommand, Task, TwitchService, TwitchSpider

### Community 58 - "Twitcasting Backend Model"
Cohesion: 0.20
Nodes (7): ConcurrentQueue, DiscordStreamNotifyBot.Scraper.Detection.Twitch.Debounce, Debouncer, bool, string, DebounceChannelUpdateMessage, TwitchDetectionService

### Community 59 - "Startup Preflight"
Cohesion: 0.42
Nodes (4): Func, Task, TimeSpan, StartupPreflight

### Community 60 - "Twitcasting Webhook Models"
Cohesion: 0.26
Nodes (8): CommandExample, CommandSummary, DiscordSocketClient, IChannel, RequireBotPermission, SlashCommand, Task, Twitch

### Community 61 - "Broadcast Message Command"
Cohesion: 0.17
Nodes (11): Build & Run, Conventions, EF Core 鐵則, graphify, 制度條款, 外部契約（不可片面更改）, 指令文件, 架構要點（現行樹） (+3 more)

### Community 62 - "TwitCasting Autocomplete"
Cohesion: 0.22
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 63 - "Twitch Autocomplete"
Cohesion: 0.31
Nodes (5): HttpClient, List, string, Task, TwitcastingClient

### Community 64 - "YouTube Autocomplete"
Cohesion: 0.13
Nodes (12): DiscordStreamNotifyBot.Scraper.Detection.Youtube, DiscordStreamNotifyBot.Scraper.Detection.Twitch, DiscordStreamNotifyBot.SharedService.Youtube.Json, DiscordStreamNotifyBot.Shared.Messages, NoticeType, NowStreamingHost, YTNotificationType, ConnectionMultiplexer (+4 more)

### Community 65 - "Notifier Program Entry"
Cohesion: 0.70
Nodes (4): List, CategoriesJson, Category, SubCategory

### Community 66 - "Periodic Runner"
Cohesion: 0.21
Nodes (7): DebouncedEventArgs, ConcurrentDictionary, HashSet, Task, TwitchDetectionService, DateTime, TwitchStream

### Community 67 - "Interaction Base Module"
Cohesion: 0.18
Nodes (8): Assembly, CancellationToken, PeriodicTimer, Task, Program, Task, Program, BotRole

### Community 69 - "Twitcasting Movie Info"
Cohesion: 0.22
Nodes (9): 8. 分階段實作步驟, 階段 0：止血 PR — shard 歸屬守衛, 階段 1：Solution 骨架 + Shared, 階段 2：Notifier 上線（先維持單 shard 行為）, 階段 3：Scraper 拆出 + Redis Streams 匯流排（完成，正確性待測試環境驗）, 階段 4：Coordinator（完成，正確性待測試環境驗）, 階段 5：跨 shard 指令與共享狀態（完成，正確性待測試環境驗）, 階段 6：Docker 化與部署驗證（檔案完成，實跑待測試環境） (+1 more)

### Community 70 - "MainDbContextModelSnapshot.cs"
Cohesion: 0.33
Nodes (4): ConnectionMultiplexer, Lazy, string, RedisConnection

### Community 71 - "DbContext Factory"
Cohesion: 0.25
Nodes (7): EF Core 遷移與基線化（本專案版）, 一次性基線化（舊的 EnsureCreated 正式庫）, 一般變更流程, 你必須先知道的三件專案特例, 啟動時不碰資料庫（重要）, 套用：本地/開發 vs 正式環境, 收尾

### Community 74 - "TwitCasting Webhook JSON"
Cohesion: 0.26
Nodes (9): IDisposable, bool, Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task (+1 more)

### Community 80 - "DiscordSocketClient"
Cohesion: 0.29
Nodes (5): 一、`claude` 分支是你最大的資產，也是最大的陷阱, 三、使用者已做的決策，不要重新辯論, 二、你在活的生產系統旁施工, 給未來 session 的信, 這套制度最可能的退化方式，與預防

### Community 81 - "EmbedBuilder"
Cohesion: 0.27
Nodes (6): DiscordStreamNotifyBot.HttpClients.Twitcasting.Model, List, Broadcaster, GetMovieInfoResponse, Movie, GetUserInfoResponse

### Community 82 - "ITextChannel"
Cohesion: 0.50
Nodes (3): Migration, MigrationBuilder, RefactorDbContext

### Community 83 - "IUserMessage"
Cohesion: 0.40
Nodes (3): ModelSnapshot, ModelBuilder, MainDbContextModelSnapshot

### Community 84 - "ModifyTwitCastingTable"
Cohesion: 0.20
Nodes (8): DiscordStreamNotifyBot.Interaction.OwnerOnly, SendMsgToAllGuildService, DefaultMemberPermissions, RequireOwner, SlashCommand, Task, SendMsgToAllGuild, TopLevelModule

### Community 85 - "string"
Cohesion: 0.33
Nodes (5): For /graphify explain, For /graphify path, graphify reference: query, path, explain, Step 0 — Constrained query expansion (REQUIRED before traversal), Step 1 — Traversal

### Community 88 - "AddManualMemberCheckVideoFlag"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 90 - "YoutubeChannelOwnedType"
Cohesion: 0.50
Nodes (3): DateTime, YTChannelType, YoutubeChannelOwnedType

### Community 91 - "20250320095452_RefactorDbContext.Designer.cs"
Cohesion: 0.33
Nodes (6): 2.1 `Shared`（共用 library）, 2.2 `Scraper`（爬蟲層，叢集唯一）, 2.3 `Notifier`（通知層 / shard，可多個）, 2.4 `Coordinator`（主控層，1 個）, 2.5 SharedService 逐服務拆分歸屬（判斷準則表）, 2. 專案拆分 (Solution Layout)

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

### Community 105 - "DetectionHost (Scraper composition root)"
Cohesion: 0.27
Nodes (9): IRole, CommandExample, CommandSummary, DiscordSocketClient, ITextChannel, RequireGuildMemberCount, SlashCommand, Task (+1 more)

### Community 137 - "TcBackendStreamData.cs"
Cohesion: 0.44
Nodes (8): App, BackendMovie, Fmp4, Hls, Llfmp4, Streams, TcBackendStreamData, Webrtc

### Community 138 - ".PromptUserConfirmAsync"
Cohesion: 0.48
Nodes (4): InteractionModuleBase, SocketInteractionContext, Task, TopLevelModule

### Community 139 - "TopLevelModule"
Cohesion: 0.43
Nodes (4): ModuleBase, EmbedBuilder, Task, TopLevelModule

### Community 140 - "TwitcastingDetectionService"
Cohesion: 0.16
Nodes (8): EmbedBuilder, TwitcastingEmbedBuilderFactory, DateTime, List, Task, TwitcastingDetectionService, DateTime, TwitcastingStream

### Community 141 - ".FixTCDbAsync"
Cohesion: 0.22
Nodes (7): DiscordStreamNotifyBot.Command.TwitCasting, Alias, Command, RequireContext, RequireOwner, Task, TwitCasting

### Community 142 - ".GenerateSuggestionsAsync"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 146 - "GetAllRegistedWebHookJson.cs"
Cohesion: 0.67
Nodes (3): List, GetAllRegistedWebHookJson, Webhook

### Community 147 - "20250620094111_AddMaxSpiderCountSettingField.Designer.cs"
Cohesion: 0.33
Nodes (3): DiscordStreamNotifyBot.Migrations, ModelBuilder, AddMaxSpiderCountSettingField

## Knowledge Gaps
- **207 isolated node(s):** `net8.0`, `Microsoft.NET.Sdk`, `BotPlayingStatus`, `DiscordStreamNotifyBot.Command.Normal`, `DiscordStreamNotifyBot.Command.TwitCasting` (+202 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **27 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `DiscordStreamNotifyBot.DataBase` connect `Scaling Architecture Docs` to `Admin Broadcast Commands`, `EF Migrations`, `.FixTCDbAsync`, `Command Help Module`, `20250620094111_AddMaxSpiderCountSettingField.Designer.cs`, `20260709091318_AddManualMemberCheckVideoFlag.Designer.cs`, `Bot Entry Points`, `Command/Interaction Modules`, `YouTube Member Service`, `Command Attributes`, `YouTube Spider Commands`, `YouTube Member Commands`, `Uptime Kuma Client`, `Redis Token Provisioner`, `Startup Preflight`, `YouTube Autocomplete`, `Twitcasting Categories JSON`, `IUserMessage`, `SyncModelDrift`, `20250603065853_ModifyTwitCastingTable.Designer.cs`?**
  _High betweenness centrality (0.131) - this node is a cross-community bridge._
- **Why does `MainDbService` connect `YouTube Member Commands` to `YouTube Stream Commands`, `Twitch Commands`, `Notification Bus Consumer`, `TwitcastingDetectionService`, `.FixTCDbAsync`, `Command Help Module`, `SharedService Core`, `YouTube Slash Commands`, `Bot Startup & Membership`, `YouTube Member Service`, `Member Check Settings`, `YouTube Spider Commands`, `Twitcasting Detection`, `Twitcasting Commands`, `YouTube Member Interaction`, `Twitch Spider Commands`, `Nijisanji Stream JSON`, `Scraper Service`, `Twitcasting Webhook Models`, `YouTube Autocomplete`, `Periodic Runner`, `DetectionHost (Scraper composition root)`?**
  _High betweenness centrality (0.114) - this node is a cross-community bridge._
- **Why does `DiscordStreamNotifyBot.Command` connect `Command Handler` to `TopLevelModule`, `Scaling Architecture Docs`, `Command Help Module`, `CommonEqualityComparer`?**
  _High betweenness centrality (0.055) - this node is a cross-community bridge._
- **What connects `net8.0`, `Microsoft.NET.Sdk`, `BotPlayingStatus` to the rest of the system?**
  _207 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Admin Broadcast Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.06984126984126984 - nodes in this community are weakly interconnected._
- **Should `YouTube Stream Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.05779220779220779 - nodes in this community are weakly interconnected._
- **Should `Twitch Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.06430745814307458 - nodes in this community are weakly interconnected._