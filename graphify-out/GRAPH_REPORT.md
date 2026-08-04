# Graph Report - DiscordStreamNotifyBot  (2026-08-04)

## Corpus Check
- 287 files · ~142,575 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 3471 nodes · 7880 edges · 225 communities (185 shown, 40 thin omitted)
- Extraction: 91% EXTRACTED · 9% INFERRED · 0% AMBIGUOUS · INFERRED: 679 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `ae31f345`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- MainDbContext
- .AssertKeysAbsentAsync
- TwitchAuthorizationTokenService
- DiscordStreamNotifyBot.DataBase.Table
- DiscordStreamNotifyBot.Shared.csproj
- TwitchApiService
- InteractionHandler
- 偵測 → 匯流排 → 發送 路徑除錯
- NotifierMetrics
- .CheckPermissionsAsync
- YoutubeReminderPolicyTests
- BotLocalizer
- DiscordStreamNotifyBot.Localization
- Extensions
- TwitchSubscriptionService
- Extensions
- DiscordStreamNotifyBot.Command
- 會限 OAuth Token 儲存改走 MySQL（去 Redis 依賴）計畫
- .SendLocalizedErrorAsync
- .SendStreamMessageAsync
- AuthTokenTests
- YoutubeStreamService
- Log
- .RunCoreAsync
- Twitch 訂閱驗證實作計畫
- SendMsgToAllGuildService
- 新增 TwitCasting 錄影委派計畫（小幫手 ↔ StreamRecordTools）
- 多語系支援計畫
- Serilog Logging 遷移計畫
- 12. 分階段執行
- YoutubeDetectionService
- CoordinatorMetrics
- YoutubeMemberService
- .ReconcileUserStateAsync
- Log 與 Loki
- DbEntity
- TwitchOAuthRefreshLockLease
- BotConfig
- .GetDbContext
- TwitchSubscriptionConfigurationPolicy
- TwitchDetectionService
- YoutubeDetectionService
- ScraperMetrics
- GuildLocaleService
- .AddUpdate
- RedisChannels.cs
- 13. 驗證矩陣
- .CreateService
- LocaleResolver
- YoutubeStream
- MetadataServiceProvider
- 水平擴展（三層拆分）計畫 — Redis Streams 版
- .SendLocalizedConfirmAsync
- TwitchStateDecisions.cs
- Utility
- 7. 分階段執行
- TwitcastingService
- AGENTS.md
- TwitchRefreshRotationLifecycle
- .RetryWithBackoffAsync
- .SendMessageToAllGuildAsync
- DebounceChannelUpdateMessage
- graphify reference: extra exports and benchmark
- Bot
- .RunAsync
- TwitchReconcileDecisionTests
- RedisComponentFixture
- .Main
- AddManualMemberCheckVideoFlag
- CommandDisplayResolver
- NotificationContractTests
- EF Core 遷移與基線化（本專案版）
- GuildTwitchSubscriptionConfig
- 11. 通知與背景訊息
- .GetLocaleAsync
- 直播小幫手 [點我邀請到你的 Discord 內](https://discordapp.com/api/oauth2/authorize?client_id=758222559392432160&permissions=2416143425&scope=bot%20applications.commands)
- FUNDING.yml (Patreon / ECPay / PayPal)
- Build workflow (SonarQube analysis)
- MIT License
- Notifier Bot Logo — interlocking chain-link icon, purple-to-magenta-to-red gradient on light grey circle; flat modern vector branding representing the linking/notification identity of the Discord stream-notify bot
- YoutubeStream
- ReactionEventWrapper
- TwitchSubscriptionApiClient
- DiscordStreamNotifyBot.Migrations
- .PublishYoutubeNotificationAsync
- graphify reference: query, path, explain
- 自動化測試導入計畫
- SharedExtensions
- DescriptionOnlyLocalizationManager
- .Get
- 5. 語系模型與解析規則
- TwitchSubscriptionApiClientTests
- 10. 執行期互動本地化
- 6. 資源架構
- .HandleStreamStartedAsync
- graphify reference: add a URL and watch a folder
- graphify reference: commit hook and native CLAUDE.md integration
- graphify reference: incremental update and cluster-only
- .ExecuteOnceAsync
- graphify reference: GitHub clone and cross-repo merge
- graphify reference: transcribe video and audio
- .Format
- .Plan
- .AddChannelSpider
- .claude/CLAUDE.md (graphify trigger)
- DiscordWebhookClient
- DiscordStreamNotifyBot.DataBase
- Confidence rubric (EXTRACTED/INFERRED/AMBIGUOUS)
- AST structural extraction (Part A)
- Community detection & clustering
- God nodes & surprising connections
- Knowledge graph (graph.json)
- Semantic extraction (parallel subagents)
- .CreateOrRepairConfigurationAsync
- TopLevelModule
- DiscordStreamNotifyBot.Notifier.csproj
- TwitchService
- TwitchChannelUpdateDecisionTests
- ClusterService
- DiscordStreamNotifyBot.HttpClients.Twitcasting.Model
- Twitch OAuth 與零成本 EventSub 實作計畫
- .RefreshTokenAsync
- .Warn
- .Filter
- 16. 執行階段
- Prometheus / Grafana 監控
- TwitcastingClient
- DiscordStreamNotifyBot.Scraper.csproj
- DiscordStreamNotifyBot.Tests.csproj
- 17. 驗證矩陣
- .MakeNamesUnique
- Notifications.cs
- 7. OAuth API 與流程隔離
- TwitcastingLiveStartPlannerTests
- ClusterQueryService
- .Plan
- HelpDescription (bot feature summary)
- DiscordStreamNotifyBot.Shared
- 11. Bot EventSub 與偵測
- 15. 預期修改檔案
- 2. 現況基線
- 5. Guild 資格與 OAuth 豁免
- DiscordStreamNotifyBot.sln
- Help
- 13. Prometheus
- 4. 安全刪除狀態機
- MainDbService
- .OnReaction
- MainDbContextModelSnapshot.cs
- .LoadCommandFrom
- RefactorDbContext
- Migration
- AddMaxSpiderCountSettingField
- SyncModelDrift
- AddTwitchBroadcasterAuthorization
- AddLocalizationSettings
- .Get
- .CheckPermissionsAsync
- .CheckRequirementsAsync
- 20250603065853_ModifyTwitCastingTable.Designer.cs
- 20260721095646_AddLocalizationSettings.Designer.cs
- 20260611015819_SyncModelDrift.Designer.cs
- 20260709091318_AddManualMemberCheckVideoFlag.Designer.cs
- 20260719142803_AddTwitchBroadcasterAuthorization.Designer.cs
- .SlashCommandExecuted
- .AuthorizationEventRequiresCurrentPersistedRevocation
- .CheckRequirementsAsync
- .CheckRequirementsAsync
- InteractionErrorPolicyTests
- .Resolve
- .GuildMemberCountPreconditionMapsValuesAndContactPath
- .GenerateSuggestionsAsync
- AutocompleteHandler
- .GenerateSuggestionsAsync
- TwitchGuildEligibilityEvaluator
- Program
- TwitchSubscriptionPolicies.cs
- .LockGuildAsync
- GracefulShutdown
- YoutubeApiVideoPolicyTests
- .CheckMemberShipOnlyVideoIdAsync
- DiscordStreamNotifyBot.Command.Attribute
- .HandleStartLiveMessageAsync
- MigrationAndConstraintTests
- .GenerateSuggestionsAsync
- MySqlComponentFixture
- .AddSubscriptionCheckAsync
- TwitchChannelUpdateInfo
- UptimeKumaClient
- YoutubeReminderRegistryTests
- Program
- .Decide
- TwitcastingLiveStartPlanner.cs
- NijisanjiLiverJson.cs
- .GenerateSuggestionsAsync
- DiscordStreamNotifyBot.Tests
- NijisanjiStreamJson.cs
- .GenerateSuggestionsAsync
- Twitcasting
- DiscordStreamNotifyBot.SharedService.YoutubeMember
- TcBackendStreamData.cs
- TwitCastingWebHookJson.cs
- .GenerateSuggestionsAsync
- 5. 目標架構
- .GenerateSuggestionsAsync
- CommandTextEqualityComparer
- AddTwitchSubscriptionVerification
- AddTwitchSubscriptionDeletionPending
- .OnlyAffiliateAndPartnerCanBeConfigured
- .WasAuthorizationRevokedDuringStream
- 20260803141135_AddTwitchSubscriptionVerification.Designer.cs
- 7. 資料庫變更
- RedisConnection
- NotificationBusConsumerOptionsTests.cs
- .CheckAsync
- .ValidateProviderTokenEncryptionKey
- DebounceFixture
- .FixTCDbAsync
- MainDbContextFactory
- TwitchAccessTokenContractTests.cs
- MySqlComponentFactAttribute
- YoutubeChannelOwnedType
- 20250320095452_RefactorDbContext.Designer.cs
- TwitchSubscriptionCheck
- .GetStreamVideoByVideoId

## God Nodes (most connected - your core abstractions)
1. `DiscordStreamNotifyBot.DataBase.Table` - 60 edges
2. `TwitchDetectionService` - 57 edges
3. `DiscordStreamNotifyBot.DataBase` - 54 edges
4. `BotLocalizer` - 53 edges
5. `DiscordStreamNotifyBot.Shared` - 47 edges
6. `Log` - 46 edges
7. `DiscordStreamNotifyBot.Tests` - 44 edges
8. `MainDbService` - 42 edges
9. `Video` - 42 edges
10. `MainDbContext` - 40 edges

## Surprising Connections (you probably didn't know these)
- `InteractionMetadataFixture` --references--> `InteractionHandler`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/InteractionMetadataFixture.cs → src/DiscordStreamNotifyBot.Notifier/Interaction/InteractionHandler.cs
- `BotLocalizerTests` --references--> `BotLocalizer`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/BotLocalizerTests.cs → src/DiscordStreamNotifyBot.Notifier/Localization/BotLocalizer.cs
- `YoutubeMemberVideoLogMessageFormatterTests` --references--> `BotLocalizer`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/YoutubeMemberVideoLogMessageFormatterTests.cs → src/DiscordStreamNotifyBot.Notifier/Localization/BotLocalizer.cs
- `MySqlComponentFixture` --references--> `MainDbService`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/Component/MySql/MySqlComponentFixture.cs → src/DiscordStreamNotifyBot.Shared/DataBase/MainDbService.cs
- `DebounceFixture` --references--> `TwitchChannelUpdateInfo`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/TwitchChannelUpdateDebounceTests.cs → src/DiscordStreamNotifyBot.Shared/Messages/Notifications.cs

## Import Cycles
- None detected.

## Communities (225 total, 40 thin omitted)

### Community 0 - "MainDbContext"
Cohesion: 0.09
Nodes (14): DbContext, DbSet, ModelBuilder, MainDbContext, HoloVideos, NijisanjiVideos, NonApprovedVideos, OtherVideos (+6 more)

### Community 1 - ".AssertKeysAbsentAsync"
Cohesion: 0.06
Nodes (41): CancellationToken, Func, IDatabase, int, StreamEntry, Task, TwitcastingService, TwitchService (+33 more)

### Community 2 - "TwitchAuthorizationTokenService"
Cohesion: 0.18
Nodes (13): PendingRefreshPersistence, CancellationToken, int, NotifierMetrics, object, string, Task, TimeSpan (+5 more)

### Community 3 - "DiscordStreamNotifyBot.DataBase.Table"
Cohesion: 0.13
Nodes (7): DiscordStreamNotifyBot.Tests.Component.MySql, DiscordStreamNotifyBot.Auth, DiscordStreamNotifyBot.Interaction.TwitchMember, DiscordStreamNotifyBot.DataBase.Table, DiscordStreamNotifyBot.SharedService.Twitch, DiscordStreamNotifyBot.SharedService.TwitchSubscription, TwitchApiServiceDisabledTests

### Community 4 - "DiscordStreamNotifyBot.Shared.csproj"
Cohesion: 0.08
Nodes (23): Microsoft.EntityFrameworkCore.Design (9.0.3), Microsoft.EntityFrameworkCore.Relational (9.0.3), Microsoft.EntityFrameworkCore.Tools (9.0.3), Serilog (4.4.0), Serilog.Sinks.Console (6.1.1), Serilog.Sinks.File (7.0.0), Serilog.Sinks.Grafana.Loki (9.0.1), net8.0 (+15 more)

### Community 5 - "TwitchApiService"
Cohesion: 0.10
Nodes (28): EventSubSubscription, IReadOnlyList, Stream, TwitchEventSubDeleteResult, TwitchEventSubDeleteStatus, TwitchEventSubEnsureMode, TwitchEventSubEnsureResult, TwitchEventSubSubscriptionsResult (+20 more)

### Community 6 - "InteractionHandler"
Cohesion: 0.10
Nodes (20): DisplayName, ISet, Dictionary, DiscordSocketClient, Func, HashSet, IDictionary, IEnumerable (+12 more)

### Community 7 - "偵測 → 匯流排 → 發送 路徑除錯"
Cohesion: 0.13
Nodes (13): 1. Shared — 定義契約, 2. Scraper — 偵測並 publish, 3. Notifier — 消費並發送, 動工前先讀一個既有平台, 收尾檢查, 新增偵測平台 / 通知事件, 步驟（依相依順序，Shared → Scraper → Notifier）, 偵測 → 匯流排 → 發送 路徑除錯 (+5 more)

### Community 8 - "NotifierMetrics"
Cohesion: 0.12
Nodes (20): Histogram, Counter, Gauge, string, TimeSpan, TwitchSubscriptionStatus, NotificationBusMetricResult, NotificationDeliveryResult (+12 more)

### Community 9 - ".CheckPermissionsAsync"
Cohesion: 0.25
Nodes (6): CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task, RequireGuildMemberCountAttribute

### Community 10 - "YoutubeReminderPolicyTests"
Cohesion: 0.09
Nodes (19): DateTime, TimeSpan, YoutubeReminderApiAction, YoutubeReminderBatchChangeAction, YoutubeReminderBatchFacts, YoutubeReminderPolicy, YoutubeReminderReconciliationAction, YoutubeReminderStartAction (+11 more)

### Community 11 - "BotLocalizer"
Cohesion: 0.07
Nodes (33): Color, MessageComponent, Dictionary, Regex, ResourceManager, string, BotLocalizer, EmbedBuilder (+25 more)

### Community 12 - "DiscordStreamNotifyBot.Localization"
Cohesion: 0.15
Nodes (6): DiscordStreamNotifyBot.SharedService.Youtube, DiscordStreamNotifyBot.SharedService.Twitcasting, DiscordStreamNotifyBot.Interaction.Utility.Service, DiscordStreamNotifyBot.Localization, DiscordStreamNotifyBot.Interaction.Help.Service, DiscordStreamNotifyBot.Interaction

### Community 13 - "Extensions"
Cohesion: 0.13
Nodes (9): Process, EmbedBuilder, IDiscordInteraction, IEmote, IInteractionContext, IServiceProvider, Task, Video (+1 more)

### Community 14 - "TwitchSubscriptionService"
Cohesion: 0.19
Nodes (12): CancellationToken, CancellationTokenSource, ConcurrentDictionary, DateTimeOffset, DiscordSocketClient, int, NotifierMetrics, RedisValue (+4 more)

### Community 15 - "Extensions"
Cohesion: 0.14
Nodes (14): SocketCommandContext, DateTime, DiscordSocketClient, EmbedBuilder, Func, ICommandContext, IEmote, IMessage (+6 more)

### Community 16 - "DiscordStreamNotifyBot.Command"
Cohesion: 0.18
Nodes (8): DiscordStreamNotifyBot.Command, SocketMessage, CommandService, DiscordSocketClient, IServiceProvider, Task, CommandHandler, ICommandService

### Community 17 - "會限 OAuth Token 儲存改走 MySQL（去 Redis 依賴）計畫"
Cohesion: 0.11
Nodes (18): Backend, Bot（本 repo）, MySQL（兩端都已連同一個庫）, 儲存層（現況為 Redis）, 加密與 blob 格式（兩端一致）, 加密金鑰處理, 影響檔案一覽, 待決策（給實作 session） (+10 more)

### Community 18 - ".SendLocalizedErrorAsync"
Cohesion: 0.28
Nodes (12): CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, IChannel, NoticeType, RequireBotPermission, RequireContext (+4 more)

### Community 19 - ".SendStreamMessageAsync"
Cohesion: 0.28
Nodes (4): Event, HttpException, Platform, NotificationMetricEvent

### Community 20 - "AuthTokenTests"
Cohesion: 0.07
Nodes (19): IDataStore, MySqlDataStore, TokenCrypto, TokenManager, Task, ITokenDataStore, string, Task (+11 more)

### Community 21 - "YoutubeStreamService"
Cohesion: 0.09
Nodes (20): NowStreamingHost, NoticeType, DiscordSocketClient, Embed, EmojiService, HttpClient, IEnumerable, IHttpClientFactory (+12 more)

### Community 22 - "Log"
Cohesion: 0.06
Nodes (30): ConsoleColor, DelegatingHandler, ILogEventSink, ITextFormatter, LogEvent, LogEventLevel, LogFileRoute, Logger (+22 more)

### Community 23 - ".RunCoreAsync"
Cohesion: 0.21
Nodes (9): CancellationToken, Func, Task, TimeProvider, TimeSpan, PeriodicRunner, Fact, Task (+1 more)

### Community 24 - "Twitch 訂閱驗證實作計畫"
Cohesion: 0.05
Nodes (36): 10. Frontend 調整, 11. 安全與錯誤處理, 12.1 Backend, 12.2 Bot, 12.3 Frontend, 12. 自動化測試, 13. 手動驗收, 14. 實作順序 (+28 more)

### Community 25 - "SendMsgToAllGuildService"
Cohesion: 0.16
Nodes (14): ButtonCheckData, DiscordStreamNotifyBot.Interaction.OwnerOnly.Service, IInteractionService, SendAllPayload, bool, DiscordSocketClient, Embed, Task (+6 more)

### Community 26 - "新增 TwitCasting 錄影委派計畫（小幫手 ↔ StreamRecordTools）"
Cohesion: 0.11
Nodes (17): 1. 背景與動機, 2. 新增跨 repo 契約, 3. A（小幫手）改動, 4. B（StreamRecordTools）改動, 5. 部署順序與相容性, 6. 驗證, 7. 影響範圍, A1. `Shared/RedisChannels.cs` (+9 more)

### Community 27 - "多語系支援計畫"
Cohesion: 0.11
Nodes (18): 14.1 建議部署順序, 14.2 相容性, 14.3 回滾, 14. 部署與回滾, 15. 預期修改檔案, 16. 完成定義, 1. 背景, 2. 目標 (+10 more)

### Community 28 - "Serilog Logging 遷移計畫"
Cohesion: 0.13
Nodes (15): 10. 預期修改檔案, 11. 完成定義, 1. 背景, 2. 目標, 3. 非目標, 4. 技術選型, 6.1 例外事件, 6. Facade 相容契約 (+7 more)

### Community 29 - "12. 分階段執行"
Cohesion: 0.22
Nodes (9): 12. 分階段執行, 階段 0：建立基準與字串清冊, 階段 1：Localization 基礎與繁中資源化, 階段 2：資料庫與語系設定, 階段 3：Slash command 註冊本地化, 階段 4：共用互動、Help 與首次設定, 階段 5：一般 Interaction 模組, 階段 6：背景通知與會限 DM (+1 more)

### Community 30 - "YoutubeDetectionService"
Cohesion: 0.13
Nodes (14): IsDeleted, int, Timer, Video, YTChannelType, ReminderItem, ConcurrentDictionary, DateTime (+6 more)

### Community 31 - "CoordinatorMetrics"
Cohesion: 0.10
Nodes (15): Counter, Gauge, HashSet, StreamGroupInfo, string, CoordinatorMetrics, CancellationToken, IDatabase (+7 more)

### Community 32 - "YoutubeMemberService"
Cohesion: 0.11
Nodes (17): GoogleAuthorizationCodeFlow, IDMChannel, SocketMessageComponent, DiscordSocketClient, EmbedBuilder, IServiceProvider, ITextChannel, IUserMessage (+9 more)

### Community 33 - ".ReconcileUserStateAsync"
Cohesion: 0.17
Nodes (10): DateTime, TwitchSpiderRemovalMetricReason, TwitchUserState, DateTime, TwitchBroadcasterAuthorization, DateTime, TwitchSpider, TwitchEventSubCleanupDeferredMetricReason (+2 more)

### Community 34 - "Log 與 Loki"
Cohesion: 0.18
Nodes (6): Console 備援, Log 與 Loki, Loki 主動推送, Serilog Pipeline, 排障, 檔案路由

### Community 35 - "DbEntity"
Cohesion: 0.09
Nodes (12): BannerChange, DateTime, DbEntity, GuildConfig, GuildYoutubeMemberConfig, NoticeTwitcastingStreamChannel, NoticeTwitchStreamChannel, NoticeYoutubeStreamChannel (+4 more)

### Community 36 - "TwitchOAuthRefreshLockLease"
Cohesion: 0.11
Nodes (21): IConnectionMultiplexer, CancellationToken, CancellationTokenSource, Exception, IDatabase, int, RedisKey, RedisValue (+13 more)

### Community 37 - "BotConfig"
Cohesion: 0.22
Nodes (4): ServiceProvider, DetectionHost, BotConfig, Action

### Community 38 - ".GetDbContext"
Cohesion: 0.19
Nodes (13): CommandExample, CommandSummary, RequireGuildMemberCount, SlashCommand, Task, TwitcastingService, TwitcastingSpider, CommandExample (+5 more)

### Community 39 - "TwitchSubscriptionConfigurationPolicy"
Cohesion: 0.14
Nodes (7): DateTimeOffset, IEnumerable, int, IReadOnlyCollection, TwitchRateLimitPolicy, TwitchSubscriptionConfigurationPolicy, TwitchSubscriptionPoliciesTests

### Community 40 - "TwitchDetectionService"
Cohesion: 0.10
Nodes (17): ConcurrentDictionary, EventSubSubscription, IReadOnlyCollection, IReadOnlyDictionary, RedisValue, ScraperMetrics, SemaphoreSlim, Task (+9 more)

### Community 41 - "YoutubeDetectionService"
Cohesion: 0.10
Nodes (19): ConcurrentBag, bool, ConcurrentDictionary, HttpClient, IEnumerable, IHttpClientFactory, Task, YoutubeApiService (+11 more)

### Community 42 - "ScraperMetrics"
Cohesion: 0.20
Nodes (11): Counter, Gauge, string, ScraperMetricResult, ScraperMetrics, TwitchAuthorizationChangeMetricResult, TwitchEventSubCleanupDeferredMetricReason, TwitchEventSubMetricStatus (+3 more)

### Community 43 - "GuildLocaleService"
Cohesion: 0.14
Nodes (15): Locale, ConcurrentDictionary, Dictionary, Func, IEnumerable, IReadOnlyCollection, IReadOnlyDictionary, SemaphoreSlim (+7 more)

### Community 44 - ".AddUpdate"
Cohesion: 0.47
Nodes (4): CancellationToken, Fact, Task, TwitchChannelUpdateDebounceTests

### Community 45 - "RedisChannels.cs"
Cohesion: 0.20
Nodes (11): int, string, Cluster, Member, Notifier, OAuth, RedisChannels, SharedState (+3 more)

### Community 46 - "13. 驗證矩陣"
Cohesion: 0.25
Nodes (8): 13.1 編譯與靜態檢查, 13.2 Slash command 註冊, 13.3 Locale resolver, 13.4 首次設定, 13.5 通知, 13.6 YouTube 會限驗證, 13.7 範圍守衛, 13. 驗證矩陣

### Community 47 - ".CreateService"
Cohesion: 0.29
Nodes (7): Fact, Func, IReadOnlyCollection, IReadOnlyDictionary, Task, TimeProvider, GuildLocaleServiceTests

### Community 48 - "LocaleResolver"
Cohesion: 0.23
Nodes (5): LocaleResolver, InlineData, Theory, LocaleResolverTests, SupportedLocaleTests

### Community 49 - "YoutubeStream"
Cohesion: 0.08
Nodes (28): DiscordStreamNotifyBot.Command.Help, ICommandService, IEqualityComparer, Func, CommonEqualityComparer, Alias, Command, CommandInfo (+20 more)

### Community 50 - "MetadataServiceProvider"
Cohesion: 0.10
Nodes (18): IServiceProvider, IServiceScope, IServiceScopeFactory, Fact, SlashCommandParameterInfo, Task, Type, InteractionCommandContractTests (+10 more)

### Community 51 - "水平擴展（三層拆分）計畫 — Redis Streams 版"
Cohesion: 0.05
Nodes (41): 10. 可優化項目（claude 分支已有成品，對應階段順手移植）, 11. 驗證清單（部署前全過）, 1. 目標架構, 2.1 `Shared`（共用 library）, 2.2 `Scraper`（爬蟲層，叢集唯一）, 2.3 `Notifier`（通知層 / shard，可多個）, 2.4 `Coordinator`（主控層，1 個）, 2.5 SharedService 逐服務拆分歸屬（判斷準則表） (+33 more)

### Community 52 - ".SendLocalizedConfirmAsync"
Cohesion: 0.27
Nodes (9): CommandExample, CommandSummary, DiscordSocketClient, IRole, ITextChannel, RequireGuildMemberCount, SlashCommand, Task (+1 more)

### Community 53 - "TwitchStateDecisions.cs"
Cohesion: 0.12
Nodes (18): TimeSpan, TwitchChannelUpdateAction, TwitchGuildEligibilityPolicy, TwitchMissingObservationAction, TwitchOfflineAction, TwitchOfflineFacts, TwitchOfflinePolicy, TwitchOfflineScheduleAction (+10 more)

### Community 54 - "Utility"
Cohesion: 0.29
Nodes (9): DefaultMemberPermissions, DiscordSocketClient, DiscordWebhookClient, IChannel, RequireContext, RequireUserPermission, SlashCommand, Task (+1 more)

### Community 55 - "7. 分階段執行"
Cohesion: 0.25
Nodes (8): 7. 分階段執行, 階段 0：建立基準, 階段 1：加入 Serilog 與 bootstrap logger, 階段 2：搬移 console 與檔案路由, 階段 3：切換 Loki sink, 階段 4：整理 facade 與 Discord.Net adapter, 階段 5：移除自製 sink 與更新文件, 階段 6：後續漸進式 structured logging（不阻擋本計畫完成）

### Community 56 - "TwitcastingService"
Cohesion: 0.12
Nodes (12): Emote, IInteractionService, DiscordSocketClient, EmojiService, DiscordSocketClient, EmojiService, NoticeCache, NotifierMetrics (+4 more)

### Community 57 - "AGENTS.md"
Cohesion: 0.17
Nodes (11): Build & Run, Conventions, EF Core 鐵則, graphify, 制度條款, 外部契約（不可片面更改）, 指令文件, 架構要點（現行樹） (+3 more)

### Community 58 - "TwitchRefreshRotationLifecycle"
Cohesion: 0.07
Nodes (28): IDisposable, bool, Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task (+20 more)

### Community 59 - ".RetryWithBackoffAsync"
Cohesion: 0.22
Nodes (9): Func, Task, TimeProvider, TimeSpan, StartupPreflight, DateTimeOffset, Fact, Task (+1 more)

### Community 60 - ".SendMessageToAllGuildAsync"
Cohesion: 0.22
Nodes (7): DiscordStreamNotifyBot.Interaction.OwnerOnly, SendMsgToAllGuildService, DefaultMemberPermissions, RequireOwner, SlashCommand, Task, SendMsgToAllGuild

### Community 61 - "DebounceChannelUpdateMessage"
Cohesion: 0.18
Nodes (9): CancellationTokenRegistration, DebouncedEventArgs, Debouncer, Func, int, IReadOnlyCollection, string, Task (+1 more)

### Community 62 - "graphify reference: extra exports and benchmark"
Cohesion: 0.22
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 63 - "Bot"
Cohesion: 0.14
Nodes (12): BotPlayingStatus, ConnectionMultiplexer, DiscordSocketClient, IDatabase, int, ISubscriber, IUser, Task (+4 more)

### Community 64 - ".RunAsync"
Cohesion: 0.39
Nodes (6): CancellationToken, PeriodicTimer, string, Task, TimeSpan, ScraperService

### Community 65 - "TwitchReconcileDecisionTests"
Cohesion: 0.22
Nodes (7): TwitchGuildEligibilityDecision, TwitchGuildEligibilityFacts, TwitchMissingGuildObservation, TwitchReconcileFacts, DateTime, Fact, TwitchReconcileDecisionTests

### Community 66 - "RedisComponentFixture"
Cohesion: 0.22
Nodes (10): ConfigurationOptions, ICollectionFixture, ConnectionMultiplexer, IDatabase, RedisKey, string, Task, RedisComponentCollection (+2 more)

### Community 67 - ".Main"
Cohesion: 0.21
Nodes (5): HashSet, List, string, Task, Utility

### Community 69 - "CommandDisplayResolver"
Cohesion: 0.10
Nodes (19): RequireBotPermissionAttribute, RequireUserPermissionAttribute, EmbedBuilder, IEnumerable, SlashCommandInfo, HelpService, IEnumerable, IReadOnlyList (+11 more)

### Community 70 - "NotificationContractTests"
Cohesion: 0.33
Nodes (3): JObject, Fact, NotificationContractTests

### Community 71 - "EF Core 遷移與基線化（本專案版）"
Cohesion: 0.25
Nodes (7): EF Core 遷移與基線化（本專案版）, 一次性基線化（舊的 EnsureCreated 正式庫）, 一般變更流程, 你必須先知道的三件專案特例, 啟動時不碰資料庫（重要）, 套用：本地/開發 vs 正式環境, 收尾

### Community 72 - "GuildTwitchSubscriptionConfig"
Cohesion: 0.16
Nodes (10): IQueryable, TwitchSubscriptionConfigurationQueries, IReadOnlyList, TwitchSubscriptionRolePolicy, TwitchRoleConfigurationResult, GuildTwitchSubscriptionConfig, Fact, InlineData (+2 more)

### Community 73 - "11. 通知與背景訊息"
Cohesion: 0.29
Nodes (7): 11.1 現況限制, 11.2 目標作法, 11.3 YouTube, 11.4 Twitch, 11.5 TwitCasting, 11.6 YouTube 會限驗證, 11. 通知與背景訊息

### Community 74 - ".GetLocaleAsync"
Cohesion: 0.26
Nodes (7): InteractionModuleBase, SocketInteractionContext, ComponentInteraction, Task, SpiderManagementComponent, Task, TopLevelModule

### Community 80 - "YoutubeStream"
Cohesion: 0.08
Nodes (32): DiscordStreamNotifyBot.Command.YoutubeMember, Alias, ClusterQueryService, Command, CommandExample, DiscordSocketClient, IEnumerable, List (+24 more)

### Community 81 - "ReactionEventWrapper"
Cohesion: 0.29
Nodes (8): bool, Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task, ReactionEventWrapper

### Community 82 - "TwitchSubscriptionApiClient"
Cohesion: 0.16
Nodes (11): DateTimeOffset, HttpResponseMessage, IHttpClientFactory, NotifierMetrics, string, TwitchSubscriptionApiClient, TwitchSubscriptionData, TwitchSubscriptionResponse (+3 more)

### Community 83 - "DiscordStreamNotifyBot.Migrations"
Cohesion: 0.20
Nodes (5): DiscordStreamNotifyBot.Migrations, ModelBuilder, AddMaxSpiderCountSettingField, ModelBuilder, AddTwitchSubscriptionDeletionPending

### Community 84 - ".PublishYoutubeNotificationAsync"
Cohesion: 0.17
Nodes (10): GeneratedRegex, YTChannelType, DateTime, DbSet, MainDbContext, Regex, Task, Video (+2 more)

### Community 85 - "graphify reference: query, path, explain"
Cohesion: 0.33
Nodes (5): For /graphify explain, For /graphify path, graphify reference: query, path, explain, Step 0 — Constrained query expansion (REQUIRED before traversal), Step 1 — Traversal

### Community 86 - "自動化測試導入計畫"
Cohesion: 0.17
Nodes (12): 10. 測試實作規則, 1. 目標, 2. 測試分類, 3. 不移除的啟動檢查, 4. 第一批：低耦合契約與格式化, 5. 第二批：小幅抽出純邏輯, 6. 第三批：時間與快取, 7. 第四批：Scraper 狀態機 (+4 more)

### Community 87 - "SharedExtensions"
Cohesion: 0.12
Nodes (10): DateTime, EmbedBuilder, Video, YTChannelType, SharedExtensions, DateTime, MySqlComponentFact, Task (+2 more)

### Community 88 - "DescriptionOnlyLocalizationManager"
Cohesion: 0.29
Nodes (7): ILocalizationManager, ResxLocalizationManager, IDictionary, IList, LocalizationTarget, string, DescriptionOnlyLocalizationManager

### Community 89 - ".Get"
Cohesion: 0.13
Nodes (8): CultureInfo, IReadOnlyList, string, SupportedLocale, Fact, InlineData, Theory, BotLocalizerTests

### Community 90 - "5. 語系模型與解析規則"
Cohesion: 0.33
Nodes (6): 5.1 支援值, 5.2 公開內容與背景通知, 5.3 私人即時回覆, 5.4 延遲會限驗證 DM, 5.5 併發安全, 5. 語系模型與解析規則

### Community 91 - "TwitchSubscriptionApiClientTests"
Cohesion: 0.19
Nodes (15): HttpMessageHandler, HttpStatusCode, IHttpClientFactory, CancellationToken, Fact, Func, HttpClient, HttpRequestMessage (+7 more)

### Community 92 - "10. 執行期互動本地化"
Cohesion: 0.40
Nodes (5): 10.1 共用回覆 API, 10.2 Precondition 與 handler 錯誤, 10.3 例外訊息, 10.4 第一階段模組, 10. 執行期互動本地化

### Community 93 - "6. 資源架構"
Cohesion: 0.40
Nodes (5): 6.1 指令註冊資源, 6.2 執行期訊息資源, 6.3 Help 長文, 6.4 Localizer API, 6. 資源架構

### Community 94 - ".HandleStreamStartedAsync"
Cohesion: 0.21
Nodes (5): HelixStream, TwitchStreamDataFacts, TwitchStreamNotificationFactory, DateTime, TwitchStream

### Community 95 - "graphify reference: add a URL and watch a folder"
Cohesion: 0.50
Nodes (3): For /graphify add, For --watch, graphify reference: add a URL and watch a folder

### Community 96 - "graphify reference: commit hook and native CLAUDE.md integration"
Cohesion: 0.50
Nodes (3): For git commit hook, For native CLAUDE.md integration, graphify reference: commit hook and native CLAUDE.md integration

### Community 97 - "graphify reference: incremental update and cluster-only"
Cohesion: 0.50
Nodes (3): For --cluster-only, For --update (incremental re-extraction), graphify reference: incremental update and cluster-only

### Community 98 - ".ExecuteOnceAsync"
Cohesion: 0.13
Nodes (17): ConcurrentDictionary, Func, SemaphoreSlim, Task, YoutubeNoticeType, ClaimState, YoutubeTerminalEventAction, YoutubeTerminalEventDecision (+9 more)

### Community 101 - ".Format"
Cohesion: 0.17
Nodes (10): SlashCommand, IChannel, CommandExample, CommandSummary, DiscordSocketClient, IChannel, RequireBotPermission, SlashCommand (+2 more)

### Community 102 - ".Plan"
Cohesion: 0.19
Nodes (11): HashSet, IReadOnlyCollection, IReadOnlyList, TwitchEventSubCreateSpec, TwitchEventSubFact, TwitchEventSubFinalDecision, TwitchEventSubReconcilePlan, TwitchEventSubReconcilePolicy (+3 more)

### Community 103 - ".AddChannelSpider"
Cohesion: 0.38
Nodes (6): CommandExample, CommandSummary, SlashCommand, Task, TwitchService, TwitchSpider

### Community 105 - "DiscordWebhookClient"
Cohesion: 0.31
Nodes (6): CancellationToken, DiscordSocketClient, HttpClient, Task, DiscordWebhookClient, Message

### Community 106 - "DiscordStreamNotifyBot.DataBase"
Cohesion: 0.13
Nodes (10): DiscordStreamNotifyBot.Interaction.Utility, DiscordStreamNotifyBot.Interaction.Attribute, DiscordStreamNotifyBot.Interaction.TwitCasting, DiscordStreamNotifyBot.Command.Admin, DiscordStreamNotifyBot.Interaction.Twitch, DiscordStreamNotifyBot.SharedService.Cluster, DiscordStreamNotifyBot.Interaction.Youtube, DiscordStreamNotifyBot.DataBase (+2 more)

### Community 113 - ".CreateOrRepairConfigurationAsync"
Cohesion: 0.27
Nodes (10): ICollection, CancellationToken, DiscordSocketClient, Exception, IRole, NotifierMetrics, SocketGuild, Task (+2 more)

### Community 114 - "TopLevelModule"
Cohesion: 0.43
Nodes (4): ModuleBase, EmbedBuilder, Task, TopLevelModule

### Community 115 - "DiscordStreamNotifyBot.Notifier.csproj"
Cohesion: 0.10
Nodes (19): Microsoft.Extensions.DependencyInjection.Abstractions (10.0.1), System.Management (10.0.1), net8.0, Ben.Demystifier (0.4.1), Discord.Net (3.19.1), Dorssel.Utilities.Debounce (3.0.0), EFCore.NamingConventions (9.0.0), Google.Apis.YouTube.v3 (1.73.0.3981) (+11 more)

### Community 116 - "TwitchService"
Cohesion: 0.11
Nodes (27): Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task, TwitchService (+19 more)

### Community 117 - "TwitchChannelUpdateDecisionTests"
Cohesion: 0.30
Nodes (6): TwitchChannelEventFacts, TwitchChannelStateFacts, TwitchChannelUpdateDecision, DateTime, Fact, TwitchChannelUpdateDecisionTests

### Community 118 - "ClusterService"
Cohesion: 0.21
Nodes (8): IDatabase, string, Task, TimeSpan, ClusterService, RedisComponentFact, Task, ClusterServiceRedisComponentTests

### Community 119 - "DiscordStreamNotifyBot.HttpClients.Twitcasting.Model"
Cohesion: 0.21
Nodes (9): DiscordStreamNotifyBot.HttpClients.Twitcasting.Model, List, GetAllRegistedWebHookJson, Webhook, List, Broadcaster, GetMovieInfoResponse, Movie (+1 more)

### Community 120 - "Twitch OAuth 與零成本 EventSub 實作計畫"
Cohesion: 0.14
Nodes (13): 0. 涉及專案, 10. Backend EventSub Webhook, 12. Frontend, 14. Grafana, 18. 建置與遷移, 19. 部署順序, 1. 不可偏離的決策, 20. 官方參考 (+5 more)

### Community 121 - ".RefreshTokenAsync"
Cohesion: 0.24
Nodes (7): CancellationToken, Task, TwitchProviderResult, TwitchProviderResultStatus, TwitchAccessTokenData, TwitchTokenErrorData, TwitchValidateTokenData

### Community 124 - "16. 執行階段"
Cohesion: 0.22
Nodes (9): 16. 執行階段, 階段 0：前置確認, 階段 1：資料模型與 Backend 設定, 階段 2：Google/Twitch OAuth 隔離, 階段 3：Frontend, 階段 4：Twitch add資格與授權清理, 階段 5：StreamOnline 與 EventSub reconcile, 階段 6：Prometheus 與 Grafana (+1 more)

### Community 125 - "Prometheus / Grafana 監控"
Cohesion: 0.20
Nodes (9): Backend 指標, Coordinator 指標, Endpoints, Grafana, Notifier 指標, Prometheus, Prometheus / Grafana 監控, Scraper 指標 (+1 more)

### Community 126 - "TwitcastingClient"
Cohesion: 0.22
Nodes (6): Broadcaster, HttpClient, List, string, Task, TwitcastingClient

### Community 127 - "DiscordStreamNotifyBot.Scraper.csproj"
Cohesion: 0.50
Nodes (3): net8.0, prometheus-net.AspNetCore (8.2.1), Microsoft.NET.Sdk

### Community 128 - "DiscordStreamNotifyBot.Tests.csproj"
Cohesion: 0.25
Nodes (7): coverlet.collector (6.0.0), Microsoft.Extensions.TimeProvider.Testing (9.0.0), Microsoft.NET.Test.Sdk (17.8.0), xunit (2.5.3), xunit.runner.visualstudio (2.5.3), net8.0, Microsoft.NET.Sdk

### Community 129 - "17. 驗證矩陣"
Cohesion: 0.33
Nodes (6): 17.1 新增 spider, 17.2 EventSub, 17.3 授權失效, 17.4 OAuth, 17.5 Prometheus/Grafana, 17. 驗證矩陣

### Community 130 - ".MakeNamesUnique"
Cohesion: 0.31
Nodes (5): IEnumerable, int, IReadOnlyList, AutocompleteCandidate, AutocompleteSearch

### Community 131 - "Notifications.cs"
Cohesion: 0.17
Nodes (12): CollectorRegistry, DateTime, List, string, NotifyType, TwitchNoticeType, TwitchNotification, YoutubeNoticeType (+4 more)

### Community 132 - "7. OAuth API 與流程隔離"
Cohesion: 0.40
Nodes (5): 7.1 API, 7.2 State, 7.3 Callback, 7.4 Twitch scopes, 7. OAuth API 與流程隔離

### Community 133 - "TwitcastingLiveStartPlannerTests"
Cohesion: 0.21
Nodes (7): TwitcastingLiveStartEvent, TwitcastingWebhookParser, Fact, InlineData, string, Theory, TwitcastingLiveStartPlannerTests

### Community 134 - "ClusterQueryService"
Cohesion: 0.05
Nodes (52): ChannelInfo, ClusterQueryType, DiscordStreamNotifyBot.Command.Normal, Replies, Responses, DiscordSocketClient, Expected, IReadOnlyCollection (+44 more)

### Community 135 - ".Plan"
Cohesion: 0.19
Nodes (10): HashSet, IEnumerable, IReadOnlyList, string, TwitcastingWebhookAction, TwitcastingWebhookActionKind, TwitcastingWebhookRegistration, TwitcastingWebhookRegistrationPlanner (+2 more)

### Community 137 - "DiscordStreamNotifyBot.Shared"
Cohesion: 0.11
Nodes (11): DiscordStreamNotifyBot.Tests.Component.Redis, DiscordStreamNotifyBot.HttpClients, DiscordStreamNotifyBot.Scraper, DiscordStreamNotifyBot.Shared, DiscordStreamNotifyBot.Command.TwitCasting, DiscordStreamNotifyBot, int, Program (+3 more)

### Community 138 - "11. Bot EventSub 與偵測"
Cohesion: 0.50
Nodes (4): 11.1 `TwitchApiService`, 11.2 `TwitchDetectionService`, 11.3 Reconcile, 11. Bot EventSub 與偵測

### Community 139 - "15. 預期修改檔案"
Cohesion: 0.50
Nodes (4): 15.1 Bot, 15.2 Backend, 15.3 Frontend, 15. 預期修改檔案

### Community 140 - "2. 現況基線"
Cohesion: 0.50
Nodes (4): 2.1 Bot, 2.2 Backend, 2.3 Frontend, 2. 現況基線

### Community 141 - "5. Guild 資格與 OAuth 豁免"
Cohesion: 0.50
Nodes (4): 5.1 一般 guild 資格, 5.2 新增 spider 的 OAuth 豁免, 5.3 授權失效時的 guild 查詢, 5. Guild 資格與 OAuth 豁免

### Community 142 - "DiscordStreamNotifyBot.sln"
Cohesion: 0.25
Nodes (3): net8.0, prometheus-net.AspNetCore (8.2.1), Microsoft.NET.Sdk

### Community 143 - "Help"
Cohesion: 0.22
Nodes (13): DiscordStreamNotifyBot.Interaction.Help, AutocompletionResult, HelpService, IAutocompleteInteraction, IInteractionContext, InteractionService, IParameterInfo, IReadOnlyList (+5 more)

### Community 144 - "13. Prometheus"
Cohesion: 0.67
Nodes (3): 13.1 Backend 指標, 13.2 Scraper 指標, 13. Prometheus

### Community 145 - "4. 安全刪除狀態機"
Cohesion: 0.67
Nodes (3): 4.1 直播中授權失效, 4.2 關台後重新判斷, 4. 安全刪除狀態機

### Community 146 - "MainDbService"
Cohesion: 0.19
Nodes (12): DbContextOptions, TwitCasting, ComponentInteraction, GuildTwitchSubscriptionConfig, RequireContext, SlashCommand, Task, TwitchMember (+4 more)

### Community 147 - ".OnReaction"
Cohesion: 0.15
Nodes (9): Assembly, DiscordSocketClient, Func, IEnumerable, IMessage, IServiceCollection, IUserMessage, SocketReaction (+1 more)

### Community 148 - "MainDbContextModelSnapshot.cs"
Cohesion: 0.40
Nodes (3): ModelSnapshot, ModelBuilder, MainDbContextModelSnapshot

### Community 149 - ".LoadCommandFrom"
Cohesion: 0.40
Nodes (4): Assembly, IEnumerable, IServiceCollection, Type

### Community 151 - "Migration"
Cohesion: 0.50
Nodes (3): Migration, MigrationBuilder, ModifyTwitCastingTable

### Community 156 - ".Get"
Cohesion: 0.16
Nodes (11): DiscordStreamNotifyBot.SharedService, DateTimeOffset, Func, List, object, TimeProvider, TimeSpan, NoticeCache (+3 more)

### Community 157 - ".CheckPermissionsAsync"
Cohesion: 0.25
Nodes (6): CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task, RequireGuildOwnerAttribute

### Community 158 - ".CheckRequirementsAsync"
Cohesion: 0.25
Nodes (6): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildAttribute

### Community 164 - ".SlashCommandExecuted"
Cohesion: 0.21
Nodes (7): IResult, SocketInteraction, SocketSlashCommandDataOption, IDiscordInteraction, IInteractionContext, SlashCommandInfo, Task

### Community 165 - ".AuthorizationEventRequiresCurrentPersistedRevocation"
Cohesion: 0.40
Nodes (3): TwitchAuthorizationEventPolicy, InlineData, Theory

### Community 166 - ".CheckRequirementsAsync"
Cohesion: 0.22
Nodes (7): PreconditionAttribute, ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildMemberCountAttribute

### Community 167 - ".CheckRequirementsAsync"
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

### Community 172 - "AutocompleteHandler"
Cohesion: 0.20
Nodes (9): AutocompleteHandler, GuildNoticeTwitchChannelIdAutocompleteHandler, AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildTwitchSpiderAutocompleteHandler (+1 more)

### Community 173 - ".GenerateSuggestionsAsync"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 174 - "TwitchGuildEligibilityEvaluator"
Cohesion: 0.27
Nodes (7): ConcurrentDictionary, DateTime, Task, TimeProvider, TimeSpan, TwitchGuildEligibilityEvaluator, TwitchGuildEligibilityStatus

### Community 175 - "Program"
Cohesion: 0.23
Nodes (7): Assembly, CancellationToken, Exception, int, PeriodicTimer, Task, Program

### Community 176 - "TwitchSubscriptionPolicies.cs"
Cohesion: 0.29
Nodes (3): TwitchAuthorizationLocalState, TwitchAuthorizationLocalStatePolicy, TwitchRefreshPersistencePolicy

### Community 177 - ".LockGuildAsync"
Cohesion: 0.20
Nodes (10): IAsyncDisposable, CancellationToken, ConcurrentDictionary, Lease, SemaphoreSlim, Task, Lease, TwitchSubscriptionOperationCoordinator (+2 more)

### Community 178 - "GracefulShutdown"
Cohesion: 0.33
Nodes (4): CancellationToken, CancellationTokenSource, int, GracefulShutdown

### Community 179 - "YoutubeApiVideoPolicyTests"
Cohesion: 0.20
Nodes (9): YoutubeApiVideoAction, YoutubeApiVideoDecision, YoutubeApiVideoFacts, YoutubeApiVideoPolicy, DateTime, Fact, InlineData, Theory (+1 more)

### Community 180 - ".CheckMemberShipOnlyVideoIdAsync"
Cohesion: 0.16
Nodes (11): Task, YoutubeDetectionService, YoutubeMemberCandidateAction, YoutubeMemberCandidateFacts, YoutubeMemberChannelDecision, YoutubeMemberChannelFacts, YoutubeMemberVideoPolicy, Fact (+3 more)

### Community 181 - "DiscordStreamNotifyBot.Command.Attribute"
Cohesion: 0.13
Nodes (10): Attribute, DiscordStreamNotifyBot.Command.Youtube, DiscordStreamNotifyBot.Command.Attribute, DiscordStreamNotifyBot.Command.Twitch, string, CommandExampleAttribute, string, CommandExampleAttribute (+2 more)

### Community 182 - ".HandleStartLiveMessageAsync"
Cohesion: 0.33
Nodes (6): List, RedisValue, SemaphoreSlim, Task, TwitcastingDetectionService, TwitcastingNotification

### Community 183 - "MigrationAndConstraintTests"
Cohesion: 0.38
Nodes (3): MySqlComponentFact, Task, MigrationAndConstraintTests

### Community 184 - ".GenerateSuggestionsAsync"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 185 - "MySqlComponentFixture"
Cohesion: 0.38
Nodes (5): IAsyncLifetime, string, Task, MySqlComponentCollection, MySqlComponentFixture

### Community 186 - ".AddSubscriptionCheckAsync"
Cohesion: 0.42
Nodes (5): DefaultMemberPermissions, IRole, SlashCommand, Task, TwitchMemberSetting

### Community 187 - "TwitchChannelUpdateInfo"
Cohesion: 0.27
Nodes (6): IEnumerable, IReadOnlyList, TwitchChannelUpdateBatch, TwitchChannelUpdateChange, TwitchChannelUpdatePolicy, TwitchChannelUpdateInfo

### Community 188 - "UptimeKumaClient"
Cohesion: 0.24
Nodes (7): bool, DiscordSocketClient, HttpClient, string, Task, Timer, UptimeKumaClient

### Community 190 - "Program"
Cohesion: 0.29
Nodes (4): DiscordStreamNotifyBot.Coordinator, BotRole, int, Program

### Community 191 - ".Decide"
Cohesion: 0.33
Nodes (4): TwitchSpiderRemovalFacts, TwitchSpiderRemovalPolicy, InlineData, Theory

### Community 192 - "TwitcastingLiveStartPlanner.cs"
Cohesion: 0.19
Nodes (10): IEnumerable, TwitcastingLiveStartAction, TwitcastingLiveStartFacts, TwitcastingLiveStartPlan, TwitcastingLiveStartPlanner, TwitcastingStreamData, List, CategoriesJson (+2 more)

### Community 193 - "NijisanjiLiverJson.cs"
Cohesion: 0.70
Nodes (4): Head, Images, NijisanjiLiverJson, SocialLinks

### Community 194 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, ConfiguredBroadcasterAutocompleteHandler

### Community 195 - "DiscordStreamNotifyBot.Tests"
Cohesion: 0.09
Nodes (16): DiscordStreamNotifyBot.Scraper.Detection.Youtube, DiscordStreamNotifyBot.Scraper.Detection.Twitch.Debounce, DiscordStreamNotifyBot.Tests, DiscordStreamNotifyBot.Scraper.Detection.Twitch, DiscordStreamNotifyBot.SharedService.Youtube.Json, DiscordStreamNotifyBot.Scraper.Detection.Twitcasting, DiscordStreamNotifyBot.Shared.Messages, DateTime (+8 more)

### Community 196 - "NijisanjiStreamJson.cs"
Cohesion: 0.43
Nodes (6): DateTime, List, Channel, EventLiver, Liver, NijisanjiStreamJson

### Community 197 - ".GenerateSuggestionsAsync"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 198 - "Twitcasting"
Cohesion: 0.27
Nodes (8): CommandExample, CommandSummary, DiscordSocketClient, RequireBotPermission, SlashCommand, Task, TwitcastingService, Twitcasting

### Community 199 - "DiscordStreamNotifyBot.SharedService.YoutubeMember"
Cohesion: 0.11
Nodes (10): DiscordStreamNotifyBot.SharedService.YoutubeMember, DiscordStreamNotifyBot.Interaction.YoutubeMember, YoutubeMemberAutomaticMutationAction, YoutubeMemberManualPinPolicy, YoutubeMemberVideoLogMessageFormatter, Fact, YoutubeMemberManualPinPolicyTests, InlineData (+2 more)

### Community 200 - "TcBackendStreamData.cs"
Cohesion: 0.44
Nodes (8): App, BackendMovie, Fmp4, Hls, Llfmp4, Streams, TcBackendStreamData, Webrtc

### Community 201 - "TwitCastingWebHookJson.cs"
Cohesion: 0.83
Nodes (3): Broadcaster, Movie, TwitCastingWebHookJson

### Community 202 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildYoutubeMemberCheckChannelIdAutocompleteHandler

### Community 203 - "5. 目標架構"
Cohesion: 0.40
Nodes (5): 5.1 Console, 5.2 非容器檔案, 5.3 Loki, 5.4 `LOKI_URL` 相容性, 5. 目標架構

### Community 204 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildNoticeTwitCastingChannelIdAutocompleteHandler

### Community 208 - ".OnlyAffiliateAndPartnerCanBeConfigured"
Cohesion: 0.40
Nodes (3): InlineData, Theory, TwitchSubscriptionConfigurationPolicyTests

### Community 211 - "7. 資料庫變更"
Cohesion: 0.50
Nodes (4): 7.1 `GuildConfig.Locale`, 7.2 `YoutubeMemberCheck.Locale`, 7.3 Migration 鐵則, 7. 資料庫變更

### Community 212 - "RedisConnection"
Cohesion: 0.28
Nodes (5): ConnectionMultiplexer, Lazy, object, string, RedisConnection

### Community 214 - ".CheckAsync"
Cohesion: 0.54
Nodes (4): RequireContext, SlashCommand, Task, YoutubeMember

### Community 215 - ".ValidateProviderTokenEncryptionKey"
Cohesion: 0.29
Nodes (4): Fact, InlineData, Theory, ProviderTokenEncryptionKeyTests

### Community 216 - "DebounceFixture"
Cohesion: 0.29
Nodes (7): FakeTimeProvider, IReadOnlyCollection, List, DebounceFixture, UserId, UserLogin, UserName

### Community 217 - ".FixTCDbAsync"
Cohesion: 0.33
Nodes (5): Alias, Command, RequireContext, RequireOwner, Task

### Community 220 - "MySqlComponentFactAttribute"
Cohesion: 0.50
Nodes (3): FactAttribute, string, MySqlComponentFactAttribute

### Community 221 - "YoutubeChannelOwnedType"
Cohesion: 0.50
Nodes (3): DateTime, YTChannelType, YoutubeChannelOwnedType

## Knowledge Gaps
- **365 isolated node(s):** `net8.0`, `prometheus-net.AspNetCore (8.2.1)`, `Microsoft.NET.Sdk`, `DiscordStreamNotifyBot.Command.Normal`, `DiscordStreamNotifyBot.Command.TwitCasting` (+360 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **40 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `DiscordStreamNotifyBot.Shared` connect `DiscordStreamNotifyBot.Shared` to `.AssertKeysAbsentAsync`, `DiscordStreamNotifyBot.DataBase.Table`, `DiscordStreamNotifyBot.Tests`, `BotConfig`, `ClusterQueryService`, `MySqlComponentFixture`, `DiscordStreamNotifyBot.DataBase`, `DiscordStreamNotifyBot.Localization`, `RedisChannels.cs`, `YoutubeStream`, `GracefulShutdown`, `DiscordStreamNotifyBot.Command.Attribute`, `ClusterService`, `.RunCoreAsync`, `SendMsgToAllGuildService`, `.RetryWithBackoffAsync`, `Program`?**
  _High betweenness centrality (0.072) - this node is a cross-community bridge._
- **Why does `DiscordStreamNotifyBot.DataBase.Table` connect `DiscordStreamNotifyBot.DataBase.Table` to `MainDbContext`, `DiscordStreamNotifyBot.Shared`, `BotLocalizer`, `DiscordStreamNotifyBot.Localization`, `AuthTokenTests`, `.ReconcileUserStateAsync`, `DbEntity`, `GuildLocaleService`, `TwitchSubscriptionPolicies.cs`, `DiscordStreamNotifyBot.Command.Attribute`, `TwitchStateDecisions.cs`, `TwitchRefreshRotationLifecycle`, `TwitcastingLiveStartPlanner.cs`, `DiscordStreamNotifyBot.Tests`, `DiscordStreamNotifyBot.SharedService.YoutubeMember`, `GuildTwitchSubscriptionConfig`, `YoutubeChannelOwnedType`, `.HandleStreamStartedAsync`, `TwitchSubscriptionCheck`, `DiscordStreamNotifyBot.DataBase`?**
  _High betweenness centrality (0.063) - this node is a cross-community bridge._
- **Why does `DiscordStreamNotifyBot.Tests` connect `DiscordStreamNotifyBot.Tests` to `.AssertKeysAbsentAsync`, `DiscordStreamNotifyBot.DataBase.Table`, `DiscordStreamNotifyBot.Shared`, `DiscordStreamNotifyBot.Localization`, `AuthTokenTests`, `.RunCoreAsync`, `.Get`, `InteractionErrorPolicyTests`, `LocaleResolver`, `MetadataServiceProvider`, `TwitchRefreshRotationLifecycle`, `.RetryWithBackoffAsync`, `DiscordStreamNotifyBot.SharedService.YoutubeMember`, `.OnlyAffiliateAndPartnerCanBeConfigured`, `YoutubeStream`, `NotificationBusConsumerOptionsTests.cs`, `.ValidateProviderTokenEncryptionKey`, `.Get`, `TwitchAccessTokenContractTests.cs`, `.Plan`, `DiscordStreamNotifyBot.DataBase`, `.Filter`?**
  _High betweenness centrality (0.063) - this node is a cross-community bridge._
- **What connects `net8.0`, `prometheus-net.AspNetCore (8.2.1)`, `Microsoft.NET.Sdk` to the rest of the system?**
  _365 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `MainDbContext` be split into smaller, more focused modules?**
  _Cohesion score 0.09090909090909091 - nodes in this community are weakly interconnected._
- **Should `.AssertKeysAbsentAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.06117353308364544 - nodes in this community are weakly interconnected._
- **Should `DiscordStreamNotifyBot.DataBase.Table` be split into smaller, more focused modules?**
  _Cohesion score 0.13043478260869565 - nodes in this community are weakly interconnected._