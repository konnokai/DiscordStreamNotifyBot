# Graph Report - .  (2026-07-08)

## Corpus Check
- 168 files · ~72,314 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1528 nodes · 3071 edges · 80 communities (74 shown, 6 thin omitted)
- Extraction: 93% EXTRACTED · 7% INFERRED · 0% AMBIGUOUS · INFERRED: 226 edges (avg confidence: 0.8)
- Token cost: 225,318 input · 0 output

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
- Redis Connection
- DbContext Factory
- Twitcasting Categories JSON
- Nijisanji Liver JSON
- TwitCasting Webhook JSON
- README & Help Docs
- Funding Config
- CI Build Workflow
- License
- Bot Logo Image

## God Nodes (most connected - your core abstractions)
1. `DiscordStreamNotifyBot.DataBase` - 41 edges
2. `DiscordStreamNotifyBot.DataBase.Table` - 41 edges
3. `MainDbContext` - 35 edges
4. `Video` - 33 edges
5. `MainDbService` - 32 edges
6. `YoutubeStreamService` - 27 edges
7. `DiscordStreamNotifyBot.Shared` - 27 edges
8. `YoutubeDetectionService` - 25 edges
9. `TwitchService` - 23 edges
10. `BotConfig` - 23 edges

## Surprising Connections (you probably didn't know these)
- `DetectionHost (Scraper composition root)` --conceptually_related_to--> `Scraper layer (detection host)`  [INFERRED]
  .claude/skills/add-detection-platform/SKILL.md → docs/HORIZONTAL_SCALING_PLAN.md
- `EmbedBuilderFactory (per-platform embeds)` --conceptually_related_to--> `Notifier layer (Discord shard)`  [INFERRED]
  .claude/skills/add-detection-platform/SKILL.md → docs/HORIZONTAL_SCALING_PLAN.md
- `CLAUDE.md (project instructions)` --references--> `ef-migration-baseline skill`  [EXTRACTED]
  CLAUDE.md → .claude/skills/ef-migration-baseline/SKILL.md
- `README (Discord Stream Notify Bot)` --conceptually_related_to--> `HelpDescription (bot feature summary)`  [INFERRED]
  README.md → src/DiscordStreamNotifyBot.Notifier/Data/HelpDescription.txt
- `docker-compose.yml (method A, fixed shards)` --implements--> `Docker Compose deployment (method A / B)`  [EXTRACTED]
  docker-compose.yml → docs/HORIZONTAL_SCALING_PLAN.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Three-layer cluster roles + Shared** — docs_horizontal_scaling_plan_scraper, docs_horizontal_scaling_plan_notifier, docs_horizontal_scaling_plan_coordinator, docs_horizontal_scaling_plan_shared [EXTRACTED 1.00]
- **Detection -> bus -> send flow** — docs_horizontal_scaling_plan_scraper, docs_horizontal_scaling_plan_notification_bus, docs_horizontal_scaling_plan_consumer_group, docs_horizontal_scaling_plan_notification_bus_consumer, docs_horizontal_scaling_plan_shard_ownership_guard [EXTRACTED 1.00]
- **graphify build pipeline** — claude_skills_graphify_skill_ast_extraction, claude_skills_graphify_skill_semantic_extraction, claude_skills_graphify_skill_community_detection, claude_skills_graphify_skill_god_nodes, claude_skills_graphify_skill_knowledge_graph [EXTRACTED 1.00]

## Communities (80 total, 6 thin omitted)

### Community 0 - "Admin Broadcast Commands"
Cohesion: 0.06
Nodes (47): ChannelInfo, ClusterQueryType, DiscordStreamNotifyBot.Command.Normal, Dictionary, GuildSnapshot, IReadOnlyCollection, Replies, Responses (+39 more)

### Community 1 - "YouTube Stream Commands"
Cohesion: 0.07
Nodes (37): NowStreamingHost, Alias, ClusterQueryService, Command, CommandExample, DiscordSocketClient, IEnumerable, List (+29 more)

### Community 2 - "Twitch Commands"
Cohesion: 0.07
Nodes (38): ICommandService, Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task (+30 more)

### Community 3 - "Twitcasting Service & DbContext"
Cohesion: 0.06
Nodes (32): DiscordStreamNotifyBot.SharedService.Twitcasting, DiscordStreamNotifyBot.DataBase.Table, DbContext, DbSet, MainDbContext, BannerChange, DateTime, DbEntity (+24 more)

### Community 4 - "Solution & Dependencies"
Cohesion: 0.04
Nodes (41): Microsoft.EntityFrameworkCore.Design (9.0.3), Microsoft.EntityFrameworkCore.Relational (9.0.3), Microsoft.EntityFrameworkCore.Tools (9.0.3), Microsoft.Extensions.DependencyInjection.Abstractions (10.0.1), System.Management (10.0.1), net8.0, Microsoft.NET.Sdk, net8.0 (+33 more)

### Community 5 - "Help & Owner Services"
Cohesion: 0.06
Nodes (31): ButtonCheckData, DiscordStreamNotifyBot.Interaction.Utility.Service, DiscordStreamNotifyBot.Interaction.OwnerOnly.Service, DiscordStreamNotifyBot.Interaction.Help.Service, IInteractionService, SendAllPayload, EmbedBuilder, SlashCommandInfo (+23 more)

### Community 6 - "Notification Bus Consumer"
Cohesion: 0.07
Nodes (25): RedisValue, CancellationToken, IDatabase, int, StreamEntry, Task, TimeSpan, TwitcastingService (+17 more)

### Community 7 - "Help Autocomplete Handlers"
Cohesion: 0.05
Nodes (37): AutocompleteHandler, DiscordStreamNotifyBot.Interaction.Help, AutocompletionResult, HelpService, IAutocompleteInteraction, IInteractionContext, InteractionService, IParameterInfo (+29 more)

### Community 8 - "EF Migrations"
Cohesion: 0.05
Nodes (21): DiscordStreamNotifyBot.Migrations, Migration, ModelSnapshot, MigrationBuilder, ModelBuilder, RefactorDbContext, RefactorDbContext, MigrationBuilder (+13 more)

### Community 9 - "Precondition Attributes"
Cohesion: 0.05
Nodes (31): PreconditionAttribute, CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task, RequireGuildMemberCountAttribute, CommandInfo (+23 more)

### Community 10 - "Command Handler"
Cohesion: 0.07
Nodes (22): DiscordStreamNotifyBot.Command, ModuleBase, SocketMessage, CommandService, DiscordSocketClient, IServiceProvider, Task, CommandHandler (+14 more)

### Community 11 - "Embed Builder Factory"
Cohesion: 0.12
Nodes (16): DateTime, EmbedBuilder, YTApiVideo, EmbedBuilderFactory, DateTime, Video, YTChannelType, DateTime (+8 more)

### Community 12 - "Scaling Architecture Docs"
Cohesion: 0.10
Nodes (37): CLAUDE.md (project instructions), DetectionHost (Scraper composition root), EmbedBuilderFactory (per-platform embeds), add-detection-platform skill, debug-detection-bus skill, ef-migration-baseline skill, docker-compose.yml (method A, fixed shards), Horizontal Scaling Plan (Redis Streams) (+29 more)

### Community 13 - "Interaction Extensions"
Cohesion: 0.08
Nodes (22): IDiscordInteraction, IDisposable, Assembly, DiscordSocketClient, EmbedBuilder, Func, IEnumerable, IInteractionContext (+14 more)

### Community 14 - "Command Help Module"
Cohesion: 0.09
Nodes (19): DiscordStreamNotifyBot.Command.Help, IEqualityComparer, Alias, Command, CommandInfo, CommandService, IServiceProvider, string (+11 more)

### Community 15 - "Video/Embed Extensions"
Cohesion: 0.10
Nodes (19): SocketCommandContext, Assembly, DateTime, DiscordSocketClient, EmbedBuilder, Func, ICommandContext, IEmote (+11 more)

### Community 16 - "SharedService Core"
Cohesion: 0.09
Nodes (16): DiscordStreamNotifyBot.Scraper.Detection.Youtube, DiscordStreamNotifyBot.SharedService, DiscordStreamNotifyBot.Scraper.Detection.Twitch, DiscordStreamNotifyBot.SharedService.Twitch, DiscordStreamNotifyBot.SharedService.Youtube.Json, DiscordStreamNotifyBot.Shared.Messages, DiscordStreamNotifyBot.Interaction, NoticeType (+8 more)

### Community 17 - "YouTube Detection Service"
Cohesion: 0.11
Nodes (17): ConcurrentBag, bool, ConcurrentDictionary, DateTime, HttpClient, IHttpClientFactory, Task, Timer (+9 more)

### Community 18 - "YouTube Slash Commands"
Cohesion: 0.27
Nodes (12): CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, IChannel, NoticeType, RequireBotPermission, RequireContext (+4 more)

### Community 19 - "Bot Startup & Membership"
Cohesion: 0.12
Nodes (11): DebouncedEventArgs, Task, Task, Task, ConcurrentDictionary, HashSet, Task, TwitchDetectionService (+3 more)

### Community 20 - "Auth / Token Crypto"
Cohesion: 0.14
Nodes (9): DiscordStreamNotifyBot.Auth, IDataStore, TokenCrypto, TokenManager, IDatabase, string, Task, Type (+1 more)

### Community 21 - "Bot Entry Points"
Cohesion: 0.13
Nodes (10): DiscordStreamNotifyBot.HttpClients, DiscordStreamNotifyBot.Scraper, DiscordStreamNotifyBot.Shared, DiscordStreamNotifyBot.Command.TwitCasting, DiscordStreamNotifyBot, DiscordStreamNotifyBot.Scraper.Detection.Twitcasting, BotPlayingStatus, TwitCasting (+2 more)

### Community 22 - "YouTube Reminder Scheduler"
Cohesion: 0.20
Nodes (8): GeneratedRegex, IEnumerable, YTChannelType, DateTime, Regex, Task, Video, YoutubeDetectionService

### Community 23 - "Interaction Handler"
Cohesion: 0.12
Nodes (14): Emote, IResult, SocketInteraction, SocketSlashCommandDataOption, IInteractionService, DiscordSocketClient, IInteractionContext, InteractionService (+6 more)

### Community 24 - "Command/Interaction Modules"
Cohesion: 0.19
Nodes (9): DiscordStreamNotifyBot.Interaction.Utility, DiscordStreamNotifyBot.Interaction.Attribute, DiscordStreamNotifyBot.Interaction.OwnerOnly, DiscordStreamNotifyBot.Interaction.TwitCasting, DiscordStreamNotifyBot.Command.Admin, DiscordStreamNotifyBot.Interaction.Twitch, DiscordStreamNotifyBot.SharedService.Cluster, DiscordStreamNotifyBot.Interaction.Youtube (+1 more)

### Community 25 - "YouTube Member Service"
Cohesion: 0.16
Nodes (13): GoogleAuthorizationCodeFlow, IDMChannel, SocketMessageComponent, DiscordSocketClient, EmbedBuilder, ITextChannel, IUserMessage, Task (+5 more)

### Community 26 - "Notice Cache & Messaging"
Cohesion: 0.19
Nodes (7): DateTime, Func, List, object, TimeSpan, NoticeCache, Embed

### Community 27 - "YouTube Reminder Timer"
Cohesion: 0.27
Nodes (6): DateTime, int, Task, YTApiVideo, YoutubeDetectionService, Video

### Community 28 - "Command Attributes"
Cohesion: 0.13
Nodes (10): Attribute, DiscordStreamNotifyBot.Command.Youtube, DiscordStreamNotifyBot.Command.Attribute, DiscordStreamNotifyBot.Command.Twitch, string, CommandExampleAttribute, string, CommandExampleAttribute (+2 more)

### Community 29 - "Graphify Tooling Docs"
Cohesion: 0.16
Nodes (16): .claude/CLAUDE.md (graphify trigger), add URL ingest & --watch, Exports (Neo4j/FalkorDB/wiki/SVG/GraphML/MCP), Extraction subagent prompt spec, Confidence rubric (EXTRACTED/INFERRED/AMBIGUOUS), GitHub clone & cross-repo merge, Post-commit hook & CLAUDE.md integration, query / path / explain traversal (+8 more)

### Community 30 - "Logging"
Cohesion: 0.18
Nodes (9): ConsoleColor, Exception, LogMessage, LogType, object, string, Task, Log (+1 more)

### Community 31 - "Cluster Leader/Heartbeat"
Cohesion: 0.26
Nodes (5): IDatabase, string, Task, TimeSpan, ClusterService

### Community 32 - "Member Check Settings"
Cohesion: 0.21
Nodes (10): IRole, CommandExample, CommandSummary, DiscordSocketClient, ITextChannel, RequireGuildMemberCount, SlashCommand, Task (+2 more)

### Community 33 - "YouTube Spider Commands"
Cohesion: 0.50
Nodes (8): Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task, YoutubeStream

### Community 34 - "Twitch Channel Commands"
Cohesion: 0.28
Nodes (8): CommandExample, CommandSummary, DiscordSocketClient, IChannel, RequireBotPermission, SlashCommand, Task, Twitch

### Community 35 - "Twitcasting Detection"
Cohesion: 0.14
Nodes (9): EmbedBuilder, TwitcastingEmbedBuilderFactory, DateTime, List, string, Task, TwitcastingDetectionService, DateTime (+1 more)

### Community 36 - "Shared Extensions"
Cohesion: 0.18
Nodes (5): DateTime, EmbedBuilder, Video, YTChannelType, SharedExtensions

### Community 37 - "Bot State & Timers"
Cohesion: 0.16
Nodes (11): BotPlayingStatus, ConnectionMultiplexer, DiscordSocketClient, IDatabase, int, ISubscriber, IUser, Task (+3 more)

### Community 38 - "Coordinator Entry/Shutdown"
Cohesion: 0.15
Nodes (8): CancellationTokenSource, DiscordStreamNotifyBot.Coordinator, BotRole, Task, Program, CancellationToken, int, GracefulShutdown

### Community 39 - "YouTube Member Commands"
Cohesion: 0.27
Nodes (10): DiscordStreamNotifyBot.Command.YoutubeMember, Alias, Command, RequireContext, RequireOwner, Summary, Task, YoutubeMemberService (+2 more)

### Community 40 - "Twitcasting Commands"
Cohesion: 0.25
Nodes (9): CommandExample, CommandSummary, DiscordSocketClient, IChannel, RequireBotPermission, SlashCommand, Task, TwitcastingService (+1 more)

### Community 41 - "YouTube Member Interaction"
Cohesion: 0.28
Nodes (7): DbContextOptions, RequireContext, SlashCommand, Task, YoutubeMember, string, MainDbService

### Community 42 - "DB Query Extensions"
Cohesion: 0.17
Nodes (4): Process, IEmote, Video, Extensions

### Community 43 - "Coordinator Service"
Cohesion: 0.24
Nodes (8): CancellationToken, IDatabase, int, PeriodicTimer, string, Task, CoordinatorService, IEnumerable

### Community 44 - "Twitcasting Spider Commands"
Cohesion: 0.28
Nodes (8): CommandExample, CommandSummary, DiscordSocketClient, RequireGuildMemberCount, SlashCommand, Task, TwitcastingService, TwitcastingSpider

### Community 45 - "Redis Channels"
Cohesion: 0.23
Nodes (9): string, Cluster, Member, Notifier, RedisChannels, SharedState, Twitcasting, Twitch (+1 more)

### Community 46 - "Twitch Spider Commands"
Cohesion: 0.29
Nodes (8): CommandExample, CommandSummary, DiscordSocketClient, RequireGuildMemberCount, SlashCommand, Task, TwitchService, TwitchSpider

### Community 47 - "Nijisanji Stream JSON"
Cohesion: 0.33
Nodes (11): DateTime, List, Attributes, Data, Liver, NijisanjiStreamJson, Relationships, YoutubeChannel (+3 more)

### Community 48 - "Utility & Official Guilds"
Cohesion: 0.20
Nodes (5): HashSet, List, string, Task, Utility

### Community 49 - "Detection Host Bootstrap"
Cohesion: 0.25
Nodes (4): Action, ServiceProvider, DetectionHost, BotConfig

### Community 50 - "YouTube Channel Spider"
Cohesion: 0.33
Nodes (7): CommandExample, CommandSummary, DiscordSocketClient, SlashCommand, Task, YoutubeStreamService, YoutubeChannelSpider

### Community 51 - "Twitcasting HTTP Client"
Cohesion: 0.31
Nodes (5): HttpClient, List, string, Task, TwitcastingClient

### Community 52 - "Twitch Update Debounce"
Cohesion: 0.20
Nodes (7): ConcurrentQueue, DiscordStreamNotifyBot.Scraper.Detection.Twitch.Debounce, Debouncer, bool, string, DebounceChannelUpdateMessage, TwitchDetectionService

### Community 53 - "YouTube Member Modules"
Cohesion: 0.24
Nodes (4): DiscordStreamNotifyBot.SharedService.Youtube, DiscordStreamNotifyBot.SharedService.YoutubeMember, DiscordStreamNotifyBot.Interaction.YoutubeMember, YoutubeMemberService

### Community 54 - "Uptime Kuma Client"
Cohesion: 0.24
Nodes (7): bool, DiscordSocketClient, HttpClient, string, Task, Timer, UptimeKumaClient

### Community 55 - "Redis Token Provisioner"
Cohesion: 0.33
Nodes (5): IDatabase, ISubscriber, Task, TimeSpan, RedisTokenKeyProvisioner

### Community 56 - "Twitcasting Channel Info"
Cohesion: 0.25
Nodes (6): Broadcaster, DiscordSocketClient, EmojiService, NoticeCache, Task, TwitcastingService

### Community 57 - "Scraper Service"
Cohesion: 0.39
Nodes (6): CancellationToken, PeriodicTimer, string, Task, TimeSpan, ScraperService

### Community 58 - "Twitcasting Backend Model"
Cohesion: 0.44
Nodes (8): App, BackendMovie, Fmp4, Hls, Llfmp4, Streams, TcBackendStreamData, Webrtc

### Community 59 - "Startup Preflight"
Cohesion: 0.42
Nodes (4): Func, Task, TimeSpan, StartupPreflight

### Community 60 - "Twitcasting Webhook Models"
Cohesion: 0.29
Nodes (5): DiscordStreamNotifyBot.HttpClients.Twitcasting.Model, List, GetAllRegistedWebHookJson, Webhook, GetUserInfoResponse

### Community 61 - "Broadcast Message Command"
Cohesion: 0.25
Nodes (7): SendMsgToAllGuildService, DefaultMemberPermissions, RequireOwner, SlashCommand, Task, SendMsgToAllGuild, TopLevelModule

### Community 62 - "TwitCasting Autocomplete"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildNoticeTwitCastingChannelIdAutocompleteHandler

### Community 63 - "Twitch Autocomplete"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildTwitchSpiderAutocompleteHandler

### Community 64 - "YouTube Autocomplete"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildYoutubeChannelSpiderAutocompleteHandler

### Community 65 - "Notifier Program Entry"
Cohesion: 0.33
Nodes (3): Task, Program, BotRole

### Community 66 - "Periodic Runner"
Cohesion: 0.29
Nodes (5): CancellationToken, Func, Task, TimeSpan, PeriodicRunner

### Community 67 - "Interaction Base Module"
Cohesion: 0.53
Nodes (4): InteractionModuleBase, SocketInteractionContext, Task, TopLevelModule

### Community 68 - "TwitCasting DB Fix Command"
Cohesion: 0.33
Nodes (5): Alias, Command, RequireContext, RequireOwner, Task

### Community 69 - "Twitcasting Movie Info"
Cohesion: 0.47
Nodes (4): List, Broadcaster, GetMovieInfoResponse, Movie

### Community 70 - "Redis Connection"
Cohesion: 0.33
Nodes (4): ConnectionMultiplexer, Lazy, string, RedisConnection

### Community 72 - "Twitcasting Categories JSON"
Cohesion: 0.70
Nodes (4): List, CategoriesJson, Category, SubCategory

### Community 73 - "Nijisanji Liver JSON"
Cohesion: 0.70
Nodes (4): Head, Images, NijisanjiLiverJson, SocialLinks

### Community 74 - "TwitCasting Webhook JSON"
Cohesion: 0.83
Nodes (3): Broadcaster, Movie, TwitCastingWebHookJson

## Knowledge Gaps
- **79 isolated node(s):** `net8.0`, `Microsoft.NET.Sdk`, `BotPlayingStatus`, `DiscordStreamNotifyBot.Command.Normal`, `DiscordStreamNotifyBot.Command.TwitCasting` (+74 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **6 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `DiscordStreamNotifyBot.DataBase` connect `Command/Interaction Modules` to `Admin Broadcast Commands`, `YouTube Stream Commands`, `Twitcasting Service & DbContext`, `Help & Owner Services`, `YouTube Member Commands`, `DbContext Factory`, `YouTube Member Interaction`, `DB Query Extensions`, `EF Migrations`, `SharedService Core`, `Bot Entry Points`, `YouTube Member Modules`, `Startup Preflight`, `Command Attributes`?**
  _High betweenness centrality (0.132) - this node is a cross-community bridge._
- **Why does `MainDbService` connect `YouTube Member Interaction` to `Admin Broadcast Commands`, `YouTube Stream Commands`, `Twitch Commands`, `Help & Owner Services`, `SharedService Core`, `YouTube Detection Service`, `YouTube Slash Commands`, `Bot Startup & Membership`, `Bot Entry Points`, `YouTube Member Service`, `Notice Cache & Messaging`, `Member Check Settings`, `Twitch Channel Commands`, `Twitcasting Detection`, `Bot State & Timers`, `YouTube Member Commands`, `Twitcasting Commands`, `Twitcasting Spider Commands`, `Twitch Spider Commands`, `YouTube Channel Spider`, `Twitcasting Channel Info`?**
  _High betweenness centrality (0.123) - this node is a cross-community bridge._
- **Why does `DiscordStreamNotifyBot.Shared` connect `Bot Entry Points` to `Admin Broadcast Commands`, `Notifier Program Entry`, `Periodic Runner`, `YouTube Stream Commands`, `Help & Owner Services`, `Coordinator Entry/Shutdown`, `Notification Bus Consumer`, `Redis Channels`, `SharedService Core`, `YouTube Member Modules`, `Redis Token Provisioner`, `Command/Interaction Modules`, `Startup Preflight`?**
  _High betweenness centrality (0.095) - this node is a cross-community bridge._
- **What connects `net8.0`, `Microsoft.NET.Sdk`, `BotPlayingStatus` to the rest of the system?**
  _80 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Admin Broadcast Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.05981012658227848 - nodes in this community are weakly interconnected._
- **Should `YouTube Stream Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.07287093942054433 - nodes in this community are weakly interconnected._
- **Should `Twitch Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.07319347319347319 - nodes in this community are weakly interconnected._