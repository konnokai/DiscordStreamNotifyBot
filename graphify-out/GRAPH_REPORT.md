# Graph Report - DiscordStreamNotifyBot  (2026-07-08)

## Corpus Check
- 162 files · ~72,314 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1543 nodes · 3052 edges · 88 communities (71 shown, 17 thin omitted)
- Extraction: 93% EXTRACTED · 7% INFERRED · 0% AMBIGUOUS · INFERRED: 223 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `13b3e810`
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
- DiscordSocketClient
- EmbedBuilder
- ITextChannel
- IUserMessage
- Timer
- string
- ISubscriber
- TimeSpan

## God Nodes (most connected - your core abstractions)
1. `DiscordStreamNotifyBot.DataBase` - 40 edges
2. `DiscordStreamNotifyBot.DataBase.Table` - 40 edges
3. `MainDbContext` - 35 edges
4. `Video` - 33 edges
5. `MainDbService` - 31 edges
6. `DiscordStreamNotifyBot.Shared` - 27 edges
7. `YoutubeStreamService` - 26 edges
8. `YoutubeDetectionService` - 25 edges
9. `BotConfig` - 23 edges
10. `TwitchService` - 23 edges

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

## Communities (88 total, 17 thin omitted)

### Community 0 - "Admin Broadcast Commands"
Cohesion: 0.08
Nodes (37): ChannelInfo, ClusterQueryType, DiscordStreamNotifyBot.Command.Normal, Dictionary, GuildSnapshot, Replies, Responses, Alias (+29 more)

### Community 1 - "YouTube Stream Commands"
Cohesion: 0.07
Nodes (37): NowStreamingHost, Alias, ClusterQueryService, Command, CommandExample, DiscordSocketClient, IEnumerable, List (+29 more)

### Community 2 - "Twitch Commands"
Cohesion: 0.06
Nodes (46): DebouncedEventArgs, Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task (+38 more)

### Community 3 - "Twitcasting Service & DbContext"
Cohesion: 0.05
Nodes (24): DbContext, IDesignTimeDbContextFactory, Process, IEmote, Video, Extensions, DbSet, MainDbContext (+16 more)

### Community 4 - "Solution & Dependencies"
Cohesion: 0.04
Nodes (41): Microsoft.EntityFrameworkCore.Design (9.0.3), Microsoft.EntityFrameworkCore.Relational (9.0.3), Microsoft.EntityFrameworkCore.Tools (9.0.3), Microsoft.Extensions.DependencyInjection.Abstractions (10.0.1), System.Management (10.0.1), net8.0, Microsoft.NET.Sdk, net8.0 (+33 more)

### Community 5 - "Help & Owner Services"
Cohesion: 0.14
Nodes (15): DiscordStreamNotifyBot.Interaction.Utility.Service, IInteractionService, EmbedBuilder, SlashCommandInfo, HelpService, UtilityService, DefaultMemberPermissions, DiscordSocketClient (+7 more)

### Community 6 - "Notification Bus Consumer"
Cohesion: 0.08
Nodes (25): RedisValue, CancellationToken, IDatabase, int, StreamEntry, Task, TimeSpan, TwitcastingService (+17 more)

### Community 7 - "Help Autocomplete Handlers"
Cohesion: 0.33
Nodes (6): HelpService, InteractionService, IServiceProvider, SlashCommand, Task, Help

### Community 8 - "EF Migrations"
Cohesion: 0.05
Nodes (21): DiscordStreamNotifyBot.Migrations, Migration, ModelSnapshot, MigrationBuilder, ModelBuilder, RefactorDbContext, RefactorDbContext, MigrationBuilder (+13 more)

### Community 9 - "Precondition Attributes"
Cohesion: 0.05
Nodes (31): PreconditionAttribute, CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task, RequireGuildMemberCountAttribute, CommandInfo (+23 more)

### Community 10 - "Command Handler"
Cohesion: 0.18
Nodes (8): DiscordStreamNotifyBot.Command, SocketMessage, CommandService, DiscordSocketClient, IServiceProvider, Task, CommandHandler, ICommandService

### Community 11 - "Embed Builder Factory"
Cohesion: 0.12
Nodes (16): DateTime, EmbedBuilder, YTApiVideo, EmbedBuilderFactory, DateTime, Video, YTChannelType, DateTime (+8 more)

### Community 12 - "Scaling Architecture Docs"
Cohesion: 0.10
Nodes (37): CLAUDE.md (project instructions), DetectionHost (Scraper composition root), EmbedBuilderFactory (per-platform embeds), add-detection-platform skill, debug-detection-bus skill, ef-migration-baseline skill, docker-compose.yml (method A, fixed shards), Horizontal Scaling Plan (Redis Streams) (+29 more)

### Community 13 - "Interaction Extensions"
Cohesion: 0.11
Nodes (13): IDiscordInteraction, Assembly, DiscordSocketClient, EmbedBuilder, Func, IEnumerable, IInteractionContext, IMessage (+5 more)

### Community 14 - "Command Help Module"
Cohesion: 0.16
Nodes (12): DiscordStreamNotifyBot.Command.Help, Alias, Command, CommandService, IServiceProvider, string, Summary, Task (+4 more)

### Community 15 - "Video/Embed Extensions"
Cohesion: 0.10
Nodes (19): SocketCommandContext, Assembly, DateTime, DiscordSocketClient, EmbedBuilder, Func, ICommandContext, IEmote (+11 more)

### Community 16 - "SharedService Core"
Cohesion: 0.11
Nodes (14): DiscordStreamNotifyBot.Scraper.Detection.Youtube, DiscordStreamNotifyBot.Scraper.Detection.Twitch, DiscordStreamNotifyBot.SharedService.Twitch, DiscordStreamNotifyBot.SharedService.Youtube.Json, DiscordStreamNotifyBot.Shared.Messages, NoticeType, NoticeType, NowStreamingHost (+6 more)

### Community 17 - "YouTube Detection Service"
Cohesion: 0.13
Nodes (15): ConcurrentBag, bool, ConcurrentDictionary, HttpClient, IEnumerable, IHttpClientFactory, Task, Timer (+7 more)

### Community 18 - "YouTube Slash Commands"
Cohesion: 0.27
Nodes (12): CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, IChannel, NoticeType, RequireBotPermission, RequireContext (+4 more)

### Community 19 - "Bot Startup & Membership"
Cohesion: 0.16
Nodes (11): ButtonCheckData, DiscordStreamNotifyBot.Interaction.OwnerOnly.Service, SendAllPayload, bool, DiscordSocketClient, Embed, Task, ButtonCheckData (+3 more)

### Community 20 - "Auth / Token Crypto"
Cohesion: 0.14
Nodes (9): DiscordStreamNotifyBot.Auth, IDataStore, TokenCrypto, TokenManager, IDatabase, string, Task, Type (+1 more)

### Community 21 - "Bot Entry Points"
Cohesion: 0.13
Nodes (10): DiscordStreamNotifyBot.HttpClients, DiscordStreamNotifyBot.Scraper, DiscordStreamNotifyBot.Shared, DiscordStreamNotifyBot, DiscordStreamNotifyBot.Scraper.Detection.Twitcasting, BotPlayingStatus, Broadcaster, Movie (+2 more)

### Community 22 - "YouTube Reminder Scheduler"
Cohesion: 0.18
Nodes (9): GeneratedRegex, DateTime, DbSet, MainDbContext, Regex, Task, Video, YTChannelType (+1 more)

### Community 23 - "Interaction Handler"
Cohesion: 0.12
Nodes (14): Emote, IResult, SocketInteraction, SocketSlashCommandDataOption, IInteractionService, DiscordSocketClient, IInteractionContext, InteractionService (+6 more)

### Community 24 - "Command/Interaction Modules"
Cohesion: 0.09
Nodes (22): AutocompleteHandler, DiscordStreamNotifyBot.SharedService.Youtube, DiscordStreamNotifyBot.SharedService.YoutubeMember, DiscordStreamNotifyBot.Interaction.Utility, DiscordStreamNotifyBot.Interaction.Attribute, DiscordStreamNotifyBot.Interaction.YoutubeMember, DiscordStreamNotifyBot.Interaction.OwnerOnly, DiscordStreamNotifyBot.Interaction.TwitCasting (+14 more)

### Community 25 - "YouTube Member Service"
Cohesion: 0.12
Nodes (16): DiscordSocketClient, EmbedBuilder, GoogleAuthorizationCodeFlow, IDMChannel, ITextChannel, IUserMessage, MainDbService, SocketMessageComponent (+8 more)

### Community 26 - "Notice Cache & Messaging"
Cohesion: 0.20
Nodes (8): NoticeType, DateTime, Func, List, object, TimeSpan, NoticeCache, Embed

### Community 27 - "YouTube Reminder Timer"
Cohesion: 0.22
Nodes (7): DateTime, int, Task, YTApiVideo, YTChannelType, YoutubeDetectionService, Video

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
Cohesion: 0.15
Nodes (14): IRole, AutocompletionResult, CommandExample, CommandSummary, DiscordSocketClient, IAutocompleteInteraction, IInteractionContext, IParameterInfo (+6 more)

### Community 33 - "YouTube Spider Commands"
Cohesion: 0.50
Nodes (8): Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task, YoutubeStream

### Community 34 - "Twitch Channel Commands"
Cohesion: 0.26
Nodes (8): CommandExample, CommandSummary, DiscordSocketClient, IChannel, RequireBotPermission, SlashCommand, Task, Twitch

### Community 35 - "Twitcasting Detection"
Cohesion: 0.06
Nodes (33): DiscordStreamNotifyBot.HttpClients.Twitcasting.Model, DateTime, List, string, Task, TwitcastingDetectionService, DateTime, TwitcastingStream (+25 more)

### Community 36 - "Shared Extensions"
Cohesion: 0.17
Nodes (5): DateTime, EmbedBuilder, Video, YTChannelType, SharedExtensions

### Community 37 - "Bot State & Timers"
Cohesion: 0.05
Nodes (29): Action, Assembly, BotPlayingStatus, IDatabase, ISubscriber, ServiceProvider, ConnectionMultiplexer, DiscordSocketClient (+21 more)

### Community 38 - "Coordinator Entry/Shutdown"
Cohesion: 0.33
Nodes (4): CancellationTokenSource, CancellationToken, int, GracefulShutdown

### Community 39 - "YouTube Member Commands"
Cohesion: 0.10
Nodes (21): DiscordStreamNotifyBot.Command.YoutubeMember, ICommandService, IReadOnlyCollection, SocketGuild, DiscordSocketClient, Expected, ITextChannel, Responded (+13 more)

### Community 40 - "Twitcasting Commands"
Cohesion: 0.15
Nodes (14): AutocompletionResult, CommandExample, CommandSummary, DiscordSocketClient, IAutocompleteInteraction, IChannel, IInteractionContext, IParameterInfo (+6 more)

### Community 41 - "YouTube Member Interaction"
Cohesion: 0.49
Nodes (4): RequireContext, SlashCommand, Task, YoutubeMember

### Community 42 - "DB Query Extensions"
Cohesion: 0.13
Nodes (17): IDisposable, bool, Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task (+9 more)

### Community 43 - "Coordinator Service"
Cohesion: 0.24
Nodes (8): CancellationToken, IDatabase, int, PeriodicTimer, string, Task, CoordinatorService, IEnumerable

### Community 44 - "Twitcasting Spider Commands"
Cohesion: 0.29
Nodes (8): CommandExample, CommandSummary, DiscordSocketClient, RequireGuildMemberCount, SlashCommand, Task, TwitcastingService, TwitcastingSpider

### Community 45 - "Redis Channels"
Cohesion: 0.23
Nodes (9): Cluster, Member, Notifier, RedisChannels, SharedState, Twitcasting, Twitch, Youtube (+1 more)

### Community 46 - "Twitch Spider Commands"
Cohesion: 0.28
Nodes (8): CommandExample, CommandSummary, DiscordSocketClient, RequireGuildMemberCount, SlashCommand, Task, TwitchService, TwitchSpider

### Community 47 - "Nijisanji Stream JSON"
Cohesion: 0.33
Nodes (11): DateTime, List, Attributes, Data, Liver, NijisanjiStreamJson, Relationships, YoutubeChannel (+3 more)

### Community 48 - "Utility & Official Guilds"
Cohesion: 0.20
Nodes (5): HashSet, List, string, Task, Utility

### Community 49 - "Detection Host Bootstrap"
Cohesion: 0.18
Nodes (5): IEqualityComparer, Func, CommonEqualityComparer, Func, CommonEqualityComparer

### Community 50 - "YouTube Channel Spider"
Cohesion: 0.33
Nodes (7): CommandExample, CommandSummary, DiscordSocketClient, SlashCommand, Task, YoutubeStreamService, YoutubeChannelSpider

### Community 51 - "Twitcasting HTTP Client"
Cohesion: 0.25
Nodes (6): DiscordStreamNotifyBot.Interaction.Help, AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, HelpGetModulesAutocompleteHandler

### Community 52 - "Twitch Update Debounce"
Cohesion: 0.20
Nodes (7): ConcurrentQueue, DiscordStreamNotifyBot.Scraper.Detection.Twitch.Debounce, Debouncer, bool, string, DebounceChannelUpdateMessage, TwitchDetectionService

### Community 53 - "YouTube Member Modules"
Cohesion: 0.07
Nodes (21): DiscordStreamNotifyBot.SharedService.Twitcasting, DiscordStreamNotifyBot.DataBase.Table, DiscordStreamNotifyBot.SharedService, DiscordStreamNotifyBot.Interaction, EmbedBuilder, TwitcastingEmbedBuilderFactory, YoutubeMemberService, BannerChange (+13 more)

### Community 54 - "Uptime Kuma Client"
Cohesion: 0.20
Nodes (7): bool, DiscordSocketClient, HttpClient, string, Task, Timer, UptimeKumaClient

### Community 56 - "Twitcasting Channel Info"
Cohesion: 0.29
Nodes (4): DiscordStreamNotifyBot.Coordinator, BotRole, Task, Program

### Community 57 - "Scraper Service"
Cohesion: 0.39
Nodes (6): CancellationToken, PeriodicTimer, string, Task, TimeSpan, ScraperService

### Community 58 - "Twitcasting Backend Model"
Cohesion: 0.43
Nodes (4): ModuleBase, EmbedBuilder, Task, TopLevelModule

### Community 59 - "Startup Preflight"
Cohesion: 0.21
Nodes (7): Task, Program, BotRole, Func, Task, TimeSpan, StartupPreflight

### Community 60 - "Twitcasting Webhook Models"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 61 - "Broadcast Message Command"
Cohesion: 0.25
Nodes (7): SendMsgToAllGuildService, DefaultMemberPermissions, RequireOwner, SlashCommand, Task, SendMsgToAllGuild, TopLevelModule

### Community 62 - "TwitCasting Autocomplete"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 63 - "Twitch Autocomplete"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 64 - "YouTube Autocomplete"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 65 - "Notifier Program Entry"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 66 - "Periodic Runner"
Cohesion: 0.29
Nodes (5): CancellationToken, Func, Task, TimeSpan, PeriodicRunner

### Community 67 - "Interaction Base Module"
Cohesion: 0.39
Nodes (4): InteractionModuleBase, SocketInteractionContext, Task, TopLevelModule

### Community 68 - "TwitCasting DB Fix Command"
Cohesion: 0.15
Nodes (10): DiscordStreamNotifyBot.Command.TwitCasting, DbContextOptions, Alias, Command, RequireContext, RequireOwner, Task, TwitCasting (+2 more)

### Community 70 - "Redis Connection"
Cohesion: 0.33
Nodes (4): ConnectionMultiplexer, Lazy, string, RedisConnection

### Community 72 - "Twitcasting Categories JSON"
Cohesion: 0.50
Nodes (3): DateTime, YoutubePubSubNotification, YTNotificationType

### Community 73 - "Nijisanji Liver JSON"
Cohesion: 0.70
Nodes (4): Head, Images, NijisanjiLiverJson, SocialLinks

## Knowledge Gaps
- **79 isolated node(s):** `RedisChannels`, `net8.0`, `Microsoft.NET.Sdk`, `BotPlayingStatus`, `DiscordStreamNotifyBot.Command.Normal` (+74 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **17 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `DiscordStreamNotifyBot.DataBase` connect `Command/Interaction Modules` to `Admin Broadcast Commands`, `YouTube Stream Commands`, `Interaction Base Module`, `TwitCasting DB Fix Command`, `Twitcasting Service & DbContext`, `Shared Extensions`, `YouTube Member Commands`, `EF Migrations`, `SharedService Core`, `Bot Startup & Membership`, `YouTube Member Modules`, `Bot Entry Points`, `Startup Preflight`, `Command Attributes`?**
  _High betweenness centrality (0.172) - this node is a cross-community bridge._
- **Why does `MainDbService` connect `TwitCasting DB Fix Command` to `Admin Broadcast Commands`, `YouTube Stream Commands`, `Twitch Commands`, `Help & Owner Services`, `Notification Bus Consumer`, `SharedService Core`, `YouTube Detection Service`, `YouTube Slash Commands`, `Bot Startup & Membership`, `Notice Cache & Messaging`, `Member Check Settings`, `Twitch Channel Commands`, `Twitcasting Detection`, `Bot State & Timers`, `YouTube Member Commands`, `Twitcasting Commands`, `YouTube Member Interaction`, `Twitcasting Spider Commands`, `Twitch Spider Commands`, `YouTube Channel Spider`?**
  _High betweenness centrality (0.085) - this node is a cross-community bridge._
- **Why does `DiscordStreamNotifyBot.Shared` connect `Bot Entry Points` to `Admin Broadcast Commands`, `YouTube Stream Commands`, `Periodic Runner`, `Shared Extensions`, `Bot State & Timers`, `Coordinator Entry/Shutdown`, `Notification Bus Consumer`, `Redis Channels`, `SharedService Core`, `Bot Startup & Membership`, `Twitcasting Channel Info`, `Command/Interaction Modules`, `Startup Preflight`?**
  _High betweenness centrality (0.083) - this node is a cross-community bridge._
- **What connects `RedisChannels`, `net8.0`, `Microsoft.NET.Sdk` to the rest of the system?**
  _80 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Admin Broadcast Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.08397337429595494 - nodes in this community are weakly interconnected._
- **Should `YouTube Stream Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.07287093942054433 - nodes in this community are weakly interconnected._
- **Should `Twitch Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.056962025316455694 - nodes in this community are weakly interconnected._