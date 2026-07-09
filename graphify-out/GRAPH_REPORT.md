# Graph Report - DiscordStreamNotifyBot  (2026-07-09)

## Corpus Check
- 167 files · ~76,118 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1728 nodes · 3124 edges · 151 communities (92 shown, 59 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 195 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `dcb41fa5`
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
- 5. Shard 歸屬與生命週期
- YoutubeChannelOwnedType
- 20250320095452_RefactorDbContext.Designer.cs
- 20250603065853_ModifyTwitCastingTable.Designer.cs
- 20250620094111_AddMaxSpiderCountSettingField.Designer.cs
- 20250320095452_RefactorDbContext.Designer.cs
- 20250603065853_ModifyTwitCastingTable.Designer.cs
- 20250620094111_AddMaxSpiderCountSettingField.Designer.cs
- TwitchSpider
- 20260709091318_AddManualMemberCheckVideoFlag.Designer.cs
- graphify reference: GitHub clone and cross-repo merge
- graphify reference: transcribe video and audio
- graphify
- extraction-spec.md
- AutocompletionResult
- .claude/CLAUDE.md (graphify trigger)
- DetectionHost (Scraper composition root)
- EmbedBuilderFactory (per-platform embeds)
- Confidence rubric (EXTRACTED/INFERRED/AMBIGUOUS)
- AST structural extraction (Part A)
- Community detection & clustering
- God nodes & surprising connections
- Knowledge graph (graph.json)
- Semantic extraction (parallel subagents)
- IAutocompleteInteraction
- IInteractionContext
- IParameterInfo
- IServiceProvider
- RequireGuildMemberCount
- SlashCommand
- StreamEntry
- TimeSpan
- TwitcastingService
- TwitchService
- IUserMessage
- bool
- ConcurrentDictionary
- HttpClient
- IEnumerable
- IHttpClientFactory
- Video
- YoutubeApiService
- YouTubeService
- YTApiVideo
- string
- MigrationBuilder
- .LoadCommandFrom
- HelpDescription (bot feature summary)
- TcBackendStreamData.cs
- .PromptUserConfirmAsync
- TopLevelModule
- TwitcastingDetectionService
- .FixTCDbAsync
- .GenerateSuggestionsAsync
- CommonEqualityComparer
- Category
- ModifyTwitCastingTable
- GetAllRegistedWebHookJson.cs
- 20250620094111_AddMaxSpiderCountSettingField.Designer.cs
- 20260709091318_AddManualMemberCheckVideoFlag.Designer.cs
- RecordYoutubeChannel
- TwitchSpider

## God Nodes (most connected - your core abstractions)
1. `DiscordStreamNotifyBot.DataBase.Table` - 41 edges
2. `DiscordStreamNotifyBot.DataBase` - 36 edges
3. `MainDbContext` - 35 edges
4. `Video` - 31 edges
5. `MainDbService` - 28 edges
6. `YoutubeDetectionService` - 25 edges
7. `BotConfig` - 25 edges
8. `YoutubeStreamService` - 24 edges
9. `TwitchService` - 23 edges
10. `DiscordStreamNotifyBot.Shared` - 23 edges

## Surprising Connections (you probably didn't know these)
- `Bot` --references--> `BotConfig`  [EXTRACTED]
  src/DiscordStreamNotifyBot.Notifier/Bot.cs → src/DiscordStreamNotifyBot.Shared/BotConfig.cs
- `YoutubeMember` --references--> `YoutubeMemberService`  [EXTRACTED]
  src/DiscordStreamNotifyBot.Notifier/Interaction/YoutubeMember/YoutubeMember.cs → src/DiscordStreamNotifyBot.Notifier/SharedService/YoutubeMember/CheckMemberShip.cs
- `YoutubeMemberService` --references--> `BotConfig`  [EXTRACTED]
  src/DiscordStreamNotifyBot.Notifier/SharedService/YoutubeMember/YoutubeMemberService.cs → src/DiscordStreamNotifyBot.Shared/BotConfig.cs
- `YoutubeDetectionService` --references--> `BotConfig`  [EXTRACTED]
  src/DiscordStreamNotifyBot.Scraper/Detection/Youtube/YoutubeDetectionService.cs → src/DiscordStreamNotifyBot.Shared/BotConfig.cs
- `CoordinatorService` --references--> `BotConfig`  [EXTRACTED]
  src/DiscordStreamNotifyBot.Coordinator/CoordinatorService.cs → src/DiscordStreamNotifyBot.Shared/BotConfig.cs

## Import Cycles
- None detected.

## Communities (151 total, 59 thin omitted)

### Community 0 - "Admin Broadcast Commands"
Cohesion: 0.10
Nodes (16): DbContext, DbSet, MainDbContext, BannerChange, DateTime, DbEntity, GuildConfig, NoticeTwitcastingStreamChannel (+8 more)

### Community 1 - "YouTube Stream Commands"
Cohesion: 0.24
Nodes (8): IDatabase, int, StreamEntry, string, Task, TimeSpan, NotificationBus, StreamGroupInfo

### Community 2 - "Twitch Commands"
Cohesion: 0.06
Nodes (46): RedisValue, Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task (+38 more)

### Community 3 - "Twitcasting Service & DbContext"
Cohesion: 0.08
Nodes (24): BotPlayingStatus, CancellationToken, ConnectionMultiplexer, ISubscriber, IUser, DiscordSocketClient, IDatabase, int (+16 more)

### Community 4 - "Solution & Dependencies"
Cohesion: 0.04
Nodes (41): Microsoft.EntityFrameworkCore.Design (9.0.3), Microsoft.EntityFrameworkCore.Relational (9.0.3), Microsoft.EntityFrameworkCore.Tools (9.0.3), Microsoft.Extensions.DependencyInjection.Abstractions (10.0.1), System.Management (10.0.1), net8.0, Microsoft.NET.Sdk, net8.0 (+33 more)

### Community 5 - "Help & Owner Services"
Cohesion: 0.33
Nodes (11): DateTime, List, Attributes, Data, Liver, NijisanjiStreamJson, Relationships, YoutubeChannel (+3 more)

### Community 6 - "Notification Bus Consumer"
Cohesion: 0.15
Nodes (14): AutocompletionResult, CommandExample, CommandSummary, DiscordSocketClient, IAutocompleteInteraction, IChannel, IInteractionContext, IParameterInfo (+6 more)

### Community 7 - "Help Autocomplete Handlers"
Cohesion: 0.25
Nodes (7): 1. Shared — 定義契約, 2. Scraper — 偵測並 publish, 3. Notifier — 消費並發送, 動工前先讀一個既有平台, 收尾檢查, 新增偵測平台 / 通知事件, 步驟（依相依順序，Shared → Scraper → Notifier）

### Community 9 - "Precondition Attributes"
Cohesion: 0.05
Nodes (31): PreconditionAttribute, CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task, RequireGuildMemberCountAttribute, CommandInfo (+23 more)

### Community 10 - "Command Handler"
Cohesion: 0.18
Nodes (8): DiscordStreamNotifyBot.Command, SocketMessage, CommandService, DiscordSocketClient, IServiceProvider, Task, CommandHandler, ICommandService

### Community 11 - "Embed Builder Factory"
Cohesion: 0.23
Nodes (6): DateTime, EmbedBuilder, YTApiVideo, EmbedBuilderFactory, DateTime, Video

### Community 12 - "Scaling Architecture Docs"
Cohesion: 0.20
Nodes (6): DiscordStreamNotifyBot.SharedService.Youtube, DiscordStreamNotifyBot.SharedService.Twitcasting, DiscordStreamNotifyBot.Interaction, TwitcastingEmbedBuilderFactory, NoticeType, NowStreamingHost

### Community 13 - "Interaction Extensions"
Cohesion: 0.06
Nodes (26): IDiscordInteraction, IDisposable, Process, Assembly, DiscordSocketClient, EmbedBuilder, Func, IEmote (+18 more)

### Community 14 - "Command Help Module"
Cohesion: 0.06
Nodes (45): ChannelInfo, ClusterQueryType, DiscordStreamNotifyBot.Command.Normal, GuildSnapshot, IReadOnlyCollection, Replies, Responses, SocketGuild (+37 more)

### Community 15 - "Video/Embed Extensions"
Cohesion: 0.15
Nodes (13): SocketCommandContext, DiscordSocketClient, EmbedBuilder, Func, ICommandContext, IEmote, IMessage, IMessageChannel (+5 more)

### Community 16 - "SharedService Core"
Cohesion: 0.23
Nodes (5): DiscordStreamNotifyBot.Scraper.Detection.Youtube, DiscordStreamNotifyBot.Shared.Messages, YTNotificationType, Task, YoutubeDetectionService

### Community 17 - "YouTube Detection Service"
Cohesion: 0.11
Nodes (18): Backend, Bot（本 repo）, MySQL（兩端都已連同一個庫）, 儲存層（現況為 Redis）, 加密與 blob 格式（兩端一致）, 加密金鑰處理, 影響檔案一覽, 待決策（給實作 session） (+10 more)

### Community 18 - "YouTube Slash Commands"
Cohesion: 0.27
Nodes (12): CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, IChannel, NoticeType, RequireBotPermission, RequireContext (+4 more)

### Community 19 - "Bot Startup & Membership"
Cohesion: 0.12
Nodes (16): DiscordStreamNotifyBot.Interaction.Utility.Service, DiscordStreamNotifyBot.Interaction.Help.Service, IInteractionService, EmbedBuilder, SlashCommandInfo, HelpService, UtilityService, DefaultMemberPermissions (+8 more)

### Community 20 - "Auth / Token Crypto"
Cohesion: 0.14
Nodes (9): DiscordStreamNotifyBot.Auth, IDataStore, TokenCrypto, TokenManager, IDatabase, string, Task, Type (+1 more)

### Community 21 - "Bot Entry Points"
Cohesion: 0.18
Nodes (5): DiscordStreamNotifyBot.Scraper, DiscordStreamNotifyBot.Shared, DiscordStreamNotifyBot, ServiceProvider, DetectionHost

### Community 22 - "YouTube Reminder Scheduler"
Cohesion: 0.17
Nodes (9): GeneratedRegex, DateTime, DbSet, MainDbContext, Regex, Task, Video, YTChannelType (+1 more)

### Community 23 - "Interaction Handler"
Cohesion: 0.10
Nodes (15): DiscordStreamNotifyBot.SharedService, Emote, IResult, SocketInteraction, SocketSlashCommandDataOption, IInteractionService, DiscordSocketClient, IInteractionContext (+7 more)

### Community 24 - "Command/Interaction Modules"
Cohesion: 0.15
Nodes (15): AutocompleteHandler, DiscordStreamNotifyBot.Interaction.Utility, DiscordStreamNotifyBot.Interaction.Attribute, DiscordStreamNotifyBot.Interaction.TwitCasting, DiscordStreamNotifyBot.Command.Admin, DiscordStreamNotifyBot.Interaction.Twitch, DiscordStreamNotifyBot.SharedService.Cluster, DiscordStreamNotifyBot.Interaction.Youtube (+7 more)

### Community 25 - "YouTube Member Service"
Cohesion: 0.10
Nodes (17): bool, ConcurrentBag, ConcurrentDictionary, HttpClient, IEnumerable, IHttpClientFactory, NijisanjiLiverJson, MainDbService (+9 more)

### Community 26 - "Notice Cache & Messaging"
Cohesion: 0.11
Nodes (17): 1. 背景與動機, 2. 新增跨 repo 契約, 3. A（小幫手）改動, 4. B（StreamRecordTools）改動, 5. 部署順序與相容性, 6. 驗證, 7. 影響範圍, A1. `Shared/RedisChannels.cs` (+9 more)

### Community 27 - "YouTube Reminder Timer"
Cohesion: 0.21
Nodes (8): DateTime, int, Task, YTApiVideo, YTChannelType, YoutubeDetectionService, Video, TableVideo

### Community 28 - "Command Attributes"
Cohesion: 0.20
Nodes (7): Attribute, string, CommandExampleAttribute, string, CommandExampleAttribute, string, CommandSummaryAttribute

### Community 29 - "Graphify Tooling Docs"
Cohesion: 0.08
Nodes (24): For /graphify add and --watch, For /graphify query, For the commit hook and native CLAUDE.md integration, For --update and --cluster-only, /graphify, Honesty Rules, Interpreter guard for subcommands, Part A - Structural extraction for code files (+16 more)

### Community 30 - "Logging"
Cohesion: 0.11
Nodes (16): ConsoleColor, Exception, LogMessage, LogType, DateTime, Func, List, object (+8 more)

### Community 31 - "Cluster Leader/Heartbeat"
Cohesion: 0.26
Nodes (5): IDatabase, string, Task, TimeSpan, ClusterService

### Community 32 - "Member Check Settings"
Cohesion: 0.05
Nodes (38): AutocompletionResult, CommandExample, CommandSummary, DiscordStreamNotifyBot.SharedService.YoutubeMember, DiscordStreamNotifyBot.Interaction.YoutubeMember, EmbedBuilder, GoogleAuthorizationCodeFlow, IAutocompleteInteraction (+30 more)

### Community 33 - "YouTube Spider Commands"
Cohesion: 0.44
Nodes (9): ICommandService, Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task (+1 more)

### Community 34 - "Twitch Channel Commands"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 35 - "Twitcasting Detection"
Cohesion: 0.18
Nodes (12): AutocompletionResult, CommandExample, CommandSummary, DiscordSocketClient, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider (+4 more)

### Community 36 - "Shared Extensions"
Cohesion: 0.31
Nodes (3): Action, BotRole, BotConfig

### Community 37 - "Bot State & Timers"
Cohesion: 0.29
Nodes (5): IDatabase, ISubscriber, Task, TimeSpan, RedisTokenKeyProvisioner

### Community 38 - "Coordinator Entry/Shutdown"
Cohesion: 0.15
Nodes (8): CancellationTokenSource, DiscordStreamNotifyBot.Coordinator, BotRole, Task, Program, CancellationToken, int, GracefulShutdown

### Community 40 - "Twitcasting Commands"
Cohesion: 0.16
Nodes (13): AutocompletionResult, CommandExample, CommandSummary, DiscordSocketClient, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider (+5 more)

### Community 41 - "YouTube Member Interaction"
Cohesion: 0.28
Nodes (7): DbContextOptions, RequireContext, SlashCommand, Task, YoutubeMember, string, MainDbService

### Community 42 - "DB Query Extensions"
Cohesion: 0.24
Nodes (8): CancellationToken, IDatabase, int, PeriodicTimer, string, Task, CoordinatorService, IEnumerable

### Community 43 - "Coordinator Service"
Cohesion: 0.39
Nodes (6): CancellationToken, PeriodicTimer, string, Task, TimeSpan, ScraperService

### Community 44 - "Twitcasting Spider Commands"
Cohesion: 0.24
Nodes (7): bool, DiscordSocketClient, HttpClient, string, Task, Timer, UptimeKumaClient

### Community 45 - "Redis Channels"
Cohesion: 0.23
Nodes (9): string, Cluster, Member, Notifier, RedisChannels, SharedState, Twitcasting, Twitch (+1 more)

### Community 46 - "Twitch Spider Commands"
Cohesion: 0.06
Nodes (47): NowStreamingHost, Alias, ClusterQueryService, Command, CommandExample, DiscordSocketClient, IEnumerable, List (+39 more)

### Community 47 - "Nijisanji Stream JSON"
Cohesion: 0.22
Nodes (7): Broadcaster, DiscordSocketClient, EmojiService, NoticeCache, Task, TwitcastingService, TwitcastingNotification

### Community 48 - "Utility & Official Guilds"
Cohesion: 0.20
Nodes (5): HashSet, List, string, Task, Utility

### Community 49 - "Detection Host Bootstrap"
Cohesion: 0.06
Nodes (31): DiscordStreamNotifyBot.Command.Help, DiscordStreamNotifyBot.Interaction.Help, IEqualityComparer, Alias, Command, CommandInfo, CommandService, IServiceProvider (+23 more)

### Community 50 - "YouTube Channel Spider"
Cohesion: 0.16
Nodes (12): ButtonCheckData, DiscordStreamNotifyBot.Interaction.OwnerOnly.Service, SendAllPayload, bool, DiscordSocketClient, Embed, Task, ButtonCheckData (+4 more)

### Community 51 - "Twitcasting HTTP Client"
Cohesion: 0.05
Nodes (41): 10. 可優化項目（claude 分支已有成品，對應階段順手移植）, 11. 驗證清單（部署前全過）, 1. 目標架構, 2.1 `Shared`（共用 library）, 2.2 `Scraper`（爬蟲層，叢集唯一）, 2.3 `Notifier`（通知層 / shard，可多個）, 2.4 `Coordinator`（主控層，1 個）, 2.5 SharedService 逐服務拆分歸屬（判斷準則表） (+33 more)

### Community 52 - "Twitch Update Debounce"
Cohesion: 0.17
Nodes (5): DateTime, EmbedBuilder, Video, YTChannelType, SharedExtensions

### Community 53 - "YouTube Member Modules"
Cohesion: 0.09
Nodes (15): DiscordStreamNotifyBot.DataBase.Table, DiscordStreamNotifyBot.Command.Youtube, DiscordStreamNotifyBot.Command.Attribute, DiscordStreamNotifyBot.Command.Twitch, DbEntity, GuildYoutubeMemberConfig, HoloVideos, NijisanjiVideos (+7 more)

### Community 54 - "Uptime Kuma Client"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 55 - "Redis Token Provisioner"
Cohesion: 0.47
Nodes (4): DiscordStreamNotifyBot.Scraper.Detection.Twitcasting, Broadcaster, Movie, TwitCastingWebHookJson

### Community 56 - "Twitcasting Channel Info"
Cohesion: 0.40
Nodes (3): EmbedBuilder, DateTime, TwitcastingStream

### Community 57 - "Scraper Service"
Cohesion: 0.23
Nodes (10): Dictionary, CommandExample, CommandSummary, DiscordSocketClient, RequireGuildMemberCount, SlashCommand, Task, TwitchService (+2 more)

### Community 58 - "Twitcasting Backend Model"
Cohesion: 0.17
Nodes (8): ConcurrentQueue, DiscordStreamNotifyBot.Scraper.Detection.Twitch.Debounce, DebouncedEventArgs, Debouncer, bool, string, DebounceChannelUpdateMessage, TwitchDetectionService

### Community 59 - "Startup Preflight"
Cohesion: 0.26
Nodes (7): Task, Program, BotRole, Func, Task, TimeSpan, StartupPreflight

### Community 60 - "Twitcasting Webhook Models"
Cohesion: 0.28
Nodes (8): CommandExample, CommandSummary, DiscordSocketClient, IChannel, RequireBotPermission, SlashCommand, Task, Twitch

### Community 61 - "Broadcast Message Command"
Cohesion: 0.15
Nodes (11): Build & Run, Conventions, EF Core 鐵則, graphify, 制度條款, 外部契約（不可片面更改）, 指令文件, 架構要點（現行樹） (+3 more)

### Community 62 - "TwitCasting Autocomplete"
Cohesion: 0.22
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 63 - "Twitch Autocomplete"
Cohesion: 0.31
Nodes (5): HttpClient, List, string, Task, TwitcastingClient

### Community 64 - "YouTube Autocomplete"
Cohesion: 0.18
Nodes (8): DiscordStreamNotifyBot.Scraper.Detection.Twitch, DiscordStreamNotifyBot.SharedService.Twitch, NoticeType, ConnectionMultiplexer, IDatabase, ISubscriber, IUser, BotState

### Community 66 - "Periodic Runner"
Cohesion: 0.29
Nodes (5): CancellationToken, Func, Task, TimeSpan, PeriodicRunner

### Community 67 - "Interaction Base Module"
Cohesion: 0.36
Nodes (5): Assembly, CancellationToken, PeriodicTimer, Task, Program

### Community 68 - "TwitCasting DB Fix Command"
Cohesion: 0.27
Nodes (10): DiscordStreamNotifyBot.Command.YoutubeMember, Alias, Command, RequireContext, RequireOwner, Summary, Task, YoutubeMemberService (+2 more)

### Community 69 - "Twitcasting Movie Info"
Cohesion: 0.29
Nodes (6): 偵測 → 匯流排 → 發送 路徑除錯, 完整路徑（先在腦中對齊這條鏈）, 想確認訊息真的有進匯流排 / 有沒有堆積, 「沒收到通知」依序排查, 「重複通知」排查, 關鍵檔

### Community 70 - "MainDbContextModelSnapshot.cs"
Cohesion: 0.33
Nodes (4): ConnectionMultiplexer, Lazy, string, RedisConnection

### Community 71 - "DbContext Factory"
Cohesion: 0.25
Nodes (7): EF Core 遷移與基線化（本專案版）, 一次性基線化（舊的 EnsureCreated 正式庫）, 一般變更流程, 你必須先知道的三件專案特例, 啟動時不碰資料庫（重要）, 套用：本地/開發 vs 正式環境, 收尾

### Community 73 - "Nijisanji Liver JSON"
Cohesion: 0.53
Nodes (5): DiscordStreamNotifyBot.SharedService.Youtube.Json, Head, Images, NijisanjiLiverJson, SocialLinks

### Community 74 - "TwitCasting Webhook JSON"
Cohesion: 0.29
Nodes (8): bool, Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task, ReactionEventWrapper

### Community 80 - "DiscordSocketClient"
Cohesion: 0.50
Nodes (3): DateTime, YoutubePubSubNotification, YTNotificationType

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

### Community 89 - "5. Shard 歸屬與生命週期"
Cohesion: 0.22
Nodes (7): DiscordStreamNotifyBot.HttpClients, DiscordStreamNotifyBot.Command.TwitCasting, TwitCasting, DiscordSocketClient, HttpClient, DiscordWebhookClient, Message

### Community 90 - "YoutubeChannelOwnedType"
Cohesion: 0.50
Nodes (3): DateTime, YTChannelType, YoutubeChannelOwnedType

### Community 92 - "20250603065853_ModifyTwitCastingTable.Designer.cs"
Cohesion: 0.33
Nodes (3): DiscordStreamNotifyBot.Migrations, ModelBuilder, ModifyTwitCastingTable

### Community 95 - "20250603065853_ModifyTwitCastingTable.Designer.cs"
Cohesion: 0.50
Nodes (3): For /graphify add, For --watch, graphify reference: add a URL and watch a folder

### Community 96 - "20250620094111_AddMaxSpiderCountSettingField.Designer.cs"
Cohesion: 0.50
Nodes (3): For git commit hook, For native CLAUDE.md integration, graphify reference: commit hook and native CLAUDE.md integration

### Community 97 - "TwitchSpider"
Cohesion: 0.50
Nodes (3): For --cluster-only, For --update (incremental re-extraction), graphify reference: incremental update and cluster-only

### Community 135 - ".LoadCommandFrom"
Cohesion: 0.17
Nodes (6): Assembly, DateTime, IEnumerable, IServiceCollection, Type, Video

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
Cohesion: 0.29
Nodes (4): DateTime, List, Task, TwitcastingDetectionService

### Community 141 - ".FixTCDbAsync"
Cohesion: 0.33
Nodes (5): Alias, Command, RequireContext, RequireOwner, Task

### Community 142 - ".GenerateSuggestionsAsync"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 144 - "Category"
Cohesion: 0.70
Nodes (4): List, CategoriesJson, Category, SubCategory

### Community 146 - "GetAllRegistedWebHookJson.cs"
Cohesion: 0.67
Nodes (3): List, GetAllRegistedWebHookJson, Webhook

## Knowledge Gaps
- **207 isolated node(s):** `目的`, `範圍`, `加密與 blob 格式（兩端一致）`, `儲存層（現況為 Redis）`, `MySQL（兩端都已連同一個庫）` (+202 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **59 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `DiscordStreamNotifyBot.DataBase` connect `Command/Interaction Modules` to `Admin Broadcast Commands`, `EF Migrations`, `Scaling Architecture Docs`, `Command Help Module`, `SharedService Core`, `20250620094111_AddMaxSpiderCountSettingField.Designer.cs`, `Bot Entry Points`, `Interaction Handler`, `Member Check Settings`, `YouTube Member Interaction`, `Twitch Spider Commands`, `YouTube Channel Spider`, `Twitch Update Debounce`, `YouTube Member Modules`, `Redis Token Provisioner`, `YouTube Autocomplete`, `TwitCasting DB Fix Command`, `Twitcasting Categories JSON`, `5. Shard 歸屬與生命週期`, `20250320095452_RefactorDbContext.Designer.cs`, `20250603065853_ModifyTwitCastingTable.Designer.cs`?**
  _High betweenness centrality (0.134) - this node is a cross-community bridge._
- **Why does `DiscordStreamNotifyBot.Shared` connect `Bot Entry Points` to `YouTube Autocomplete`, `YouTube Stream Commands`, `Periodic Runner`, `Bot State & Timers`, `Coordinator Entry/Shutdown`, `Redis Channels`, `Command Help Module`, `Twitch Spider Commands`, `SharedService Core`, `YouTube Channel Spider`, `Twitch Update Debounce`, `Redis Token Provisioner`, `Command/Interaction Modules`, `Startup Preflight`?**
  _High betweenness centrality (0.086) - this node is a cross-community bridge._
- **Why does `DiscordStreamNotifyBot.DataBase.Table` connect `YouTube Member Modules` to `YouTube Autocomplete`, `Member Check Settings`, `Admin Broadcast Commands`, `Twitcasting Service & DbContext`, `YoutubeChannelOwnedType`, `Scaling Architecture Docs`, `SharedService Core`, `Twitch Update Debounce`, `RecordYoutubeChannel`, `TwitchSpider`, `Redis Token Provisioner`, `Command/Interaction Modules`, `Twitcasting Channel Info`?**
  _High betweenness centrality (0.086) - this node is a cross-community bridge._
- **What connects `目的`, `範圍`, `加密與 blob 格式（兩端一致）` to the rest of the system?**
  _207 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Admin Broadcast Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.09686609686609686 - nodes in this community are weakly interconnected._
- **Should `Twitch Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.056790123456790124 - nodes in this community are weakly interconnected._
- **Should `Twitcasting Service & DbContext` be split into smaller, more focused modules?**
  _Cohesion score 0.0784313725490196 - nodes in this community are weakly interconnected._