# Graph Report - DiscordStreamNotifyBot  (2026-08-17)

## Corpus Check
- 326 files · ~170,184 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 4121 nodes · 9584 edges · 261 communities (220 shown, 41 thin omitted)
- Extraction: 91% EXTRACTED · 9% INFERRED · 0% AMBIGUOUS · INFERRED: 828 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `7dfac8e6`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- TwitchSubscriptionApiClient
- NotificationBus
- TwitchAuthorizationTokenService
- .OnlyAffiliateAndPartnerCanBeConfigured
- DiscordStreamNotifyBot.Shared.csproj
- TwitchApiService
- InteractionHandler
- 偵測 → 匯流排 → 發送 路徑除錯
- NotifierMetrics
- .CheckPermissionsAsync
- YoutubeReminderPolicyTests
- BotLocalizer
- DiscordStreamNotifyBot.Shared.Messages
- Extensions
- .GetDbContext
- Extensions
- DiscordStreamNotifyBot.Command
- 會限 OAuth Token 儲存改走 MySQL（去 Redis 依賴）計畫
- .GetLocaleAsync
- YoutubeMemberRolePoliciesTests
- AuthTokenTests
- YoutubeMemberAuthorizationService
- Log
- .RunCoreAsync
- Twitch 訂閱驗證實作計畫
- YoutubeMemberPolicies
- 新增 TwitCasting 錄影委派計畫（小幫手 ↔ StreamRecordTools）
- 多語系支援計畫
- Serilog Logging 遷移計畫
- 12. 分階段執行
- YoutubeDetectionService
- CoordinatorMetrics
- YoutubeMemberService
- TwitchGuildEligibilityEvaluator
- Log 與 Loki
- MainDbContext
- TwitchOAuthRefreshLockLease
- BotConfig
- MainDbService
- YoutubeMemberRoleService
- TwitchDetectionService
- Twitch
- ScraperMetrics
- GuildLocaleService
- .AddUpdate
- RedisChannels
- 13. 驗證矩陣
- MySqlComponentFixture
- .TryGetKey
- CommonEqualityComparer
- 網頁管理設定中心：爬蟲與會員驗證實作計畫
- 水平擴展（三層拆分）計畫 — Redis Streams 版
- YoutubeMemberSetting
- TwitchStateDecisions.cs
- Utility
- .PrepareMemberCheckCleanupAsync
- .ReconcileUserStateAsync
- AGENTS.md
- TwitchRefreshRotationLifecycle
- .RetryWithBackoffAsync
- DiscordStreamNotifyBot.DataBase
- .Warn
- graphify reference: extra exports and benchmark
- Bot
- DiscordStreamNotifyBot.DataBase.Table
- TwitchReconcileDecisionTests
- RedisComponentFixture
- .Main
- AddManualMemberCheckVideoFlag
- AdminSettingsService
- NotificationContractTests
- EF Core 遷移與基線化（本專案版）
- TwitchSubscriptionRolePolicyTests
- 11. 通知與背景訊息
- GoogleOAuthOperationLockLease
- LogEvent
- FUNDING.yml (Patreon / ECPay / PayPal)
- Build workflow (SonarQube analysis)
- MIT License
- Notifier Bot Logo — interlocking chain-link icon, purple-to-magenta-to-red gradient on light grey circle; flat modern vector branding representing the linking/notification identity of the Discord stream-notify bot
- YoutubeStream
- AdminSettingsMutationResult
- AdminSettingsContractTests
- AdminSettings.cs
- .PublishYoutubeNotificationAsync
- graphify reference: query, path, explain
- 自動化測試導入計畫
- YoutubeApiService
- DescriptionOnlyLocalizationManager
- .Get
- .CheckMemberShipCore
- TwitchSubscriptionApiClientTests
- .ShutdownAsync
- 6. 資源架構
- .AssertKeysAbsentAsync
- graphify reference: add a URL and watch a folder
- graphify reference: commit hook and native CLAUDE.md integration
- graphify reference: incremental update and cluster-only
- .ExecuteOnceAsync
- graphify reference: GitHub clone and cross-repo merge
- graphify reference: transcribe video and audio
- 網頁管理設定中心實作計畫
- .Plan
- .New
- .claude/CLAUDE.md (graphify trigger)
- DiscordWebhookClient
- DiscordStreamNotifyBot.Command.Attribute
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
- ReactionEventWrapper
- ClusterService
- DiscordStreamNotifyBot.Scraper.Detection.Twitcasting
- Twitch OAuth 與零成本 EventSub 實作計畫
- YoutubeMember
- YoutubeMemberApiClient
- .Filter
- 16. 執行階段
- Prometheus / Grafana 監控
- TwitcastingClient
- DiscordStreamNotifyBot.Scraper.csproj
- DiscordStreamNotifyBot.Tests.csproj
- 17. 驗證矩陣
- .CreateStreamEnded
- Help
- 7. OAuth API 與流程隔離
- TwitcastingLiveStartPlannerTests
- ClusterQueryService
- .Plan
- HelpDescription (bot feature summary)
- DiscordStreamNotifyBot
- 11. Bot EventSub 與偵測
- 15. 預期修改檔案
- 2. 現況基線
- 5. Guild 資格與 OAuth 豁免
- DiscordStreamNotifyBot.sln
- CommandDisplayResolver
- 13. Prometheus
- 4. 安全刪除狀態機
- .HandleTwitcastingAsync
- NotificationEmbedFactoryTests
- DiscordStreamNotifyBot.Migrations
- .SendStreamMessageAsync
- RefactorDbContext
- ModifyTwitCastingTable
- AddMaxSpiderCountSettingField
- SyncModelDrift
- AddTwitchBroadcasterAuthorization
- AddLocalizationSettings
- .Get
- SendMsgToAllGuildService
- .CheckRequirementsAsync
- .Classify
- DiscordStreamNotifyBot.HttpClients.Twitcasting.Model
- 20260611015819_SyncModelDrift.Designer.cs
- .MakeNamesUnique
- 20260719142803_AddTwitchBroadcasterAuthorization.Designer.cs
- Help
- .LoadSnapshotAsync
- .CheckRequirementsAsync
- .NotifyAddedAsync
- InteractionErrorPolicyTests
- .GetAvailableCommandsAsync
- YoutubeMemberLifecycleTaskRegistry
- .Decide
- YoutubeVideoIdParserTests
- CommandTextEqualityComparer
- TwitchDetectionService.cs
- NotificationBusConsumer
- GuildTwitchSubscriptionConfig
- HelpService
- YoutubeStream
- YoutubeApiVideoPolicyTests
- .PlanChannel
- TwitchChannelUpdateDecisionTests
- .HandleStartLiveMessageAsync
- DebounceChannelUpdateMessage
- YoutubeDetectionService
- YoutubeVideoIdParser
- 8. 分階段實作步驟
- .GenerateSuggestionsAsync
- .GenerateSuggestionsAsync
- .SetMessage
- .GenerateSuggestionsAsync
- YoutubeMemberVideoLogMessageFormatterTests
- Category
- DiscordStreamNotifyBot.SharedService.YoutubeMember
- TwitchSubscriptionSetting
- BotState
- NijisanjiStreamJson.cs
- .GenerateSuggestionsAsync
- .TryGetPayload
- TwitchChannelUpdateInfo
- TcBackendStreamData.cs
- 20260803141135_AddTwitchSubscriptionVerification.Designer.cs
- TwitchEventSubEnsureMode
- AGENTS.md
- .GenerateSuggestionsAsync
- TwitchSubscription
- AddTwitchSubscriptionVerification
- AddTwitchSubscriptionDeletionPending
- .CreateAsync
- 4. 訊息契約：Redis Streams 通知匯流排
- 5. 語系模型與解析規則
- .CheckPermissionsAsync
- .CheckRequirementsAsync
- .Main
- .Resolve
- YouTube 會員驗證架構重構計畫
- TwitchOAuthRefreshLockRedisComponentTests
- NijisanjiLiverJson.cs
- .SendMessageToAllGuildAsync
- .GuildMemberCountPreconditionMapsValuesAndContactPath
- .GenerateSuggestionsAsync
- .GenerateSuggestionsAsync
- 20250320095452_RefactorDbContext.Designer.cs
- 5. 目標架構
- DebounceFixture
- UtilityService
- 15. 實作階段
- Migration
- EmojiService
- 20250603065853_ModifyTwitCastingTable.Designer.cs
- .LoadCommandFrom
- YoutubeMemberRoleApplyResult
- BotRole
- 20260804173737_AddYoutubeMemberVerificationDurability.Designer.cs
- 20260807045351_AddGoogleOAuthUnlinkIntent.Designer.cs
- YouTube 會員驗證
- YoutubeReminderRegistryTests
- .GenerateSuggestionsAsync
- 14. Frontend
- 8. DB Schema
- AddYoutubeMemberVerificationDurability
- GetAllRegistedWebHookJson.cs
- .WasAuthorizationRevokedDuringStream
- 13. Backend Contract
- 16. 驗證命令
- 20260803165758_AddTwitchSubscriptionDeletionPending.Designer.cs
- .SameUserMutationsAreExclusiveAndOwnerReleaseRemovesTheKey
- 10. Slash 與 Interaction Cutover
- 6. 目標架構
- 7. 狀態機
- 9. Role 隔離政策
- TwitchSpider
- .CreateStreamStarted
- RenameVerificationLogChannel
- DiscordStreamNotifyBot.Tests
- .AuthorizationEventRequiresCurrentPersistedRevocation
- 20260721095646_AddLocalizationSettings.Designer.cs
- 20260813032017_RenameVerificationLogChannel.Designer.cs
- .BuildMessageComponent
- NonApprovedVideos
- OtherVideos

## God Nodes (most connected - your core abstractions)
1. `DiscordStreamNotifyBot.DataBase.Table` - 67 edges
2. `DiscordStreamNotifyBot.DataBase` - 64 edges
3. `DiscordStreamNotifyBot.Shared` - 61 edges
4. `TwitchDetectionService` - 57 edges
5. `BotLocalizer` - 53 edges
6. `MainDbContext` - 50 edges
7. `YoutubeStreamService` - 49 edges
8. `DiscordStreamNotifyBot.Tests` - 49 edges
9. `MainDbService` - 48 edges
10. `Log` - 46 edges

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

## Communities (261 total, 41 thin omitted)

### Community 0 - "TwitchSubscriptionApiClient"
Cohesion: 0.11
Nodes (18): CancellationToken, DateTimeOffset, HttpResponseMessage, IHttpClientFactory, NotifierMetrics, string, Task, TwitchProviderResult (+10 more)

### Community 1 - "NotificationBus"
Cohesion: 0.21
Nodes (10): IDatabase, int, RedisKey, RedisValue, StreamGroupInfo, string, Task, TimeSpan (+2 more)

### Community 2 - "TwitchAuthorizationTokenService"
Cohesion: 0.14
Nodes (16): PendingRefreshPersistence, CancellationToken, int, NotifierMetrics, object, string, Task, TimeSpan (+8 more)

### Community 3 - ".OnlyAffiliateAndPartnerCanBeConfigured"
Cohesion: 0.33
Nodes (3): InlineData, Theory, TwitchSubscriptionConfigurationPolicyTests

### Community 4 - "DiscordStreamNotifyBot.Shared.csproj"
Cohesion: 0.08
Nodes (23): Microsoft.EntityFrameworkCore.Design (9.0.3), Microsoft.EntityFrameworkCore.Relational (9.0.3), Microsoft.EntityFrameworkCore.Tools (9.0.3), Serilog (4.4.0), Serilog.Sinks.Console (6.1.1), Serilog.Sinks.File (7.0.0), Serilog.Sinks.Grafana.Loki (9.0.1), net8.0 (+15 more)

### Community 5 - "TwitchApiService"
Cohesion: 0.12
Nodes (19): Clip, DateTime, EventSubSubscription, HttpClient, IReadOnlyList, Lazy, Regex, SemaphoreSlim (+11 more)

### Community 6 - "InteractionHandler"
Cohesion: 0.07
Nodes (27): DisplayName, IResult, ISet, SocketInteraction, SocketSlashCommandDataOption, Dictionary, DiscordSocketClient, Func (+19 more)

### Community 7 - "偵測 → 匯流排 → 發送 路徑除錯"
Cohesion: 0.13
Nodes (13): 1. Shared — 定義契約, 2. Scraper — 偵測並 publish, 3. Notifier — 消費並發送, 動工前先讀一個既有平台, 收尾檢查, 新增偵測平台 / 通知事件, 步驟（依相依順序，Shared → Scraper → Notifier）, 偵測 → 匯流排 → 發送 路徑除錯 (+5 more)

### Community 8 - "NotifierMetrics"
Cohesion: 0.11
Nodes (22): CollectorRegistry, Histogram, Counter, Gauge, string, TimeSpan, TwitchSubscriptionStatus, NotificationBusMetricResult (+14 more)

### Community 9 - ".CheckPermissionsAsync"
Cohesion: 0.22
Nodes (7): PreconditionAttribute, CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task, RequireGuildMemberCountAttribute

### Community 10 - "YoutubeReminderPolicyTests"
Cohesion: 0.09
Nodes (19): DateTime, TimeSpan, YoutubeReminderApiAction, YoutubeReminderBatchChangeAction, YoutubeReminderBatchFacts, YoutubeReminderPolicy, YoutubeReminderReconciliationAction, YoutubeReminderStartAction (+11 more)

### Community 11 - "BotLocalizer"
Cohesion: 0.14
Nodes (16): Dictionary, Regex, ResourceManager, string, BotLocalizer, DateTime, EmbedBuilder, TimeSpan (+8 more)

### Community 12 - "DiscordStreamNotifyBot.Shared.Messages"
Cohesion: 0.11
Nodes (15): DiscordStreamNotifyBot.SharedService.AdminSettings, DiscordStreamNotifyBot.SharedService.Youtube, DiscordStreamNotifyBot.SharedService.Twitcasting, DiscordStreamNotifyBot.Localization, DiscordStreamNotifyBot.SharedService.Twitch, DiscordStreamNotifyBot.Shared.Messages, DiscordStreamNotifyBot.Interaction, TwitcastingNotificationVariant (+7 more)

### Community 13 - "Extensions"
Cohesion: 0.06
Nodes (26): Process, Assembly, DiscordSocketClient, EmbedBuilder, Func, IDiscordInteraction, IEmote, IEnumerable (+18 more)

### Community 14 - ".GetDbContext"
Cohesion: 0.13
Nodes (20): LeaseGroup, CancellationToken, ConcurrentDictionary, IEnumerable, Lease, Task, MemberOperationCoordinator, CancellationToken (+12 more)

### Community 15 - "Extensions"
Cohesion: 0.12
Nodes (15): SocketCommandContext, DateTime, DiscordSocketClient, EmbedBuilder, Func, ICommandContext, IEmote, IMessage (+7 more)

### Community 16 - "DiscordStreamNotifyBot.Command"
Cohesion: 0.18
Nodes (8): DiscordStreamNotifyBot.Command, SocketMessage, CommandService, DiscordSocketClient, IServiceProvider, Task, CommandHandler, ICommandService

### Community 17 - "會限 OAuth Token 儲存改走 MySQL（去 Redis 依賴）計畫"
Cohesion: 0.11
Nodes (18): Backend, Bot（本 repo）, MySQL（兩端都已連同一個庫）, 儲存層（現況為 Redis）, 加密與 blob 格式（兩端一致）, 加密金鑰處理, 影響檔案一覽, 待決策（給實作 session） (+10 more)

### Community 18 - ".GetLocaleAsync"
Cohesion: 0.13
Nodes (26): InteractionModuleBase, SocketInteractionContext, Task, TopLevelModule, CommandExample, CommandSummary, DiscordSocketClient, IChannel (+18 more)

### Community 19 - "YoutubeMemberRolePoliciesTests"
Cohesion: 0.21
Nodes (3): IEnumerable, Fact, YoutubeMemberRolePoliciesTests

### Community 20 - "AuthTokenTests"
Cohesion: 0.05
Nodes (25): DiscordStreamNotifyBot.Auth, IDataStore, TokenCrypto, TokenManager, Task, ITokenDataStore, CancellationToken, string (+17 more)

### Community 21 - "YoutubeMemberAuthorizationService"
Cohesion: 0.13
Nodes (14): GoogleAuthorizationCodeFlow, CancellationToken, HttpClient, MySqlDataStore, string, Task, YoutubeMemberAuthorizationResult, YoutubeMemberAuthorizationService (+6 more)

### Community 22 - "Log"
Cohesion: 0.16
Nodes (12): ILogEventSink, ITextFormatter, Logger, LoggerConfiguration, bool, long, object, string (+4 more)

### Community 23 - ".RunCoreAsync"
Cohesion: 0.21
Nodes (9): CancellationToken, Func, Task, TimeProvider, TimeSpan, PeriodicRunner, Fact, Task (+1 more)

### Community 24 - "Twitch 訂閱驗證實作計畫"
Cohesion: 0.05
Nodes (36): 10. Frontend 調整, 11. 安全與錯誤處理, 12.1 Backend, 12.2 Bot, 12.3 Frontend, 12. 自動化測試, 13. 手動驗收, 14. 實作順序 (+28 more)

### Community 25 - "YoutubeMemberPolicies"
Cohesion: 0.10
Nodes (13): ComponentInteraction, Task, IReadOnlyCollection, IReadOnlyList, UserId, YoutubeMemberPolicies, YoutubeMemberSelectionTransition, YoutubeMemberSingleConfigurationQueueAction (+5 more)

### Community 26 - "新增 TwitCasting 錄影委派計畫（小幫手 ↔ StreamRecordTools）"
Cohesion: 0.11
Nodes (17): 1. 背景與動機, 2. 新增跨 repo 契約, 3. A（小幫手）改動, 4. B（StreamRecordTools）改動, 5. 部署順序與相容性, 6. 驗證, 7. 影響範圍, A1. `Shared/RedisChannels.cs` (+9 more)

### Community 27 - "多語系支援計畫"
Cohesion: 0.07
Nodes (27): 10.1 共用回覆 API, 10.2 Precondition 與 handler 錯誤, 10.3 例外訊息, 10.4 第一階段模組, 10. 執行期互動本地化, 14.1 建議部署順序, 14.2 相容性, 14.3 回滾 (+19 more)

### Community 28 - "Serilog Logging 遷移計畫"
Cohesion: 0.09
Nodes (23): 10. 預期修改檔案, 11. 完成定義, 1. 背景, 2. 目標, 3. 非目標, 4. 技術選型, 6.1 例外事件, 6. Facade 相容契約 (+15 more)

### Community 29 - "12. 分階段執行"
Cohesion: 0.22
Nodes (9): 12. 分階段執行, 階段 0：建立基準與字串清冊, 階段 1：Localization 基礎與繁中資源化, 階段 2：資料庫與語系設定, 階段 3：Slash command 註冊本地化, 階段 4：共用互動、Help 與首次設定, 階段 5：一般 Interaction 模組, 階段 6：背景通知與會限 DM (+1 more)

### Community 30 - "YoutubeDetectionService"
Cohesion: 0.14
Nodes (14): IsDeleted, int, Timer, Video, YTChannelType, ReminderItem, ConcurrentDictionary, DateTime (+6 more)

### Community 31 - "CoordinatorMetrics"
Cohesion: 0.08
Nodes (20): DiscordStreamNotifyBot.Coordinator, Counter, Gauge, HashSet, StreamGroupInfo, string, CoordinatorMetrics, CancellationToken (+12 more)

### Community 32 - "YoutubeMemberService"
Cohesion: 0.10
Nodes (20): IDMChannel, SocketMessageComponent, CancellationToken, CancellationTokenSource, DiscordSocketClient, EmbedBuilder, Func, int (+12 more)

### Community 33 - "TwitchGuildEligibilityEvaluator"
Cohesion: 0.22
Nodes (8): ConcurrentDictionary, DateTime, Task, TimeProvider, TimeSpan, TwitchGuildEligibilityEvaluator, TwitchGuildEligibilityStatus, TwitchGuildEligibilityDecision

### Community 34 - "Log 與 Loki"
Cohesion: 0.20
Nodes (7): Console 備援, Grafana Dashboard, Log 與 Loki, Loki 主動推送, Serilog Pipeline, 排障, 檔案路由

### Community 35 - "MainDbContext"
Cohesion: 0.05
Nodes (28): DbContext, IDesignTimeDbContextFactory, DbSet, ModelBuilder, MainDbContext, MainDbContextFactory, BannerChange, DateTime (+20 more)

### Community 36 - "TwitchOAuthRefreshLockLease"
Cohesion: 0.15
Nodes (17): CancellationToken, CancellationTokenSource, Exception, IDatabase, int, RedisKey, RedisValue, string (+9 more)

### Community 37 - "BotConfig"
Cohesion: 0.16
Nodes (6): BotConfig, Action, Fact, InlineData, Theory, ProviderTokenEncryptionKeyTests

### Community 38 - "MainDbService"
Cohesion: 0.13
Nodes (20): DbContextOptions, TwitCasting, CommandExample, CommandSummary, RequireGuildMemberCount, SlashCommand, Task, TwitcastingService (+12 more)

### Community 39 - "YoutubeMemberRoleService"
Cohesion: 0.25
Nodes (9): CancellationToken, DiscordSocketClient, IEnumerable, IRole, SocketGuild, Task, YoutubeMemberRoleConfigurationResult, YoutubeMemberRoleService (+1 more)

### Community 40 - "TwitchDetectionService"
Cohesion: 0.08
Nodes (22): HelixStream, ConcurrentDictionary, EventSubSubscription, IReadOnlyCollection, IReadOnlyDictionary, RedisValue, ScraperMetrics, SemaphoreSlim (+14 more)

### Community 41 - "Twitch"
Cohesion: 0.24
Nodes (14): Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task, TwitchService (+6 more)

### Community 42 - "ScraperMetrics"
Cohesion: 0.20
Nodes (10): Counter, Gauge, string, ScraperMetricResult, ScraperMetrics, TwitchAuthorizationChangeMetricResult, TwitchEventSubCleanupDeferredMetricReason, TwitchEventSubMetricStatus (+2 more)

### Community 43 - "GuildLocaleService"
Cohesion: 0.07
Nodes (28): Locale, CancellationToken, ConcurrentDictionary, Dictionary, Func, IEnumerable, IReadOnlyCollection, IReadOnlyDictionary (+20 more)

### Community 44 - ".AddUpdate"
Cohesion: 0.47
Nodes (4): CancellationToken, Fact, Task, TwitchChannelUpdateDebounceTests

### Community 45 - "RedisChannels"
Cohesion: 0.16
Nodes (12): int, string, AdminSettings, Cluster, Member, Notifier, OAuth, RedisChannels (+4 more)

### Community 46 - "13. 驗證矩陣"
Cohesion: 0.25
Nodes (8): 13.1 編譯與靜態檢查, 13.2 Slash command 註冊, 13.3 Locale resolver, 13.4 首次設定, 13.5 通知, 13.6 YouTube 會限驗證, 13.7 範圍守衛, 13. 驗證矩陣

### Community 47 - "MySqlComponentFixture"
Cohesion: 0.07
Nodes (22): IAsyncLifetime, ICollectionFixture, DateTime, EmbedBuilder, Video, YTChannelType, SharedExtensions, MySqlComponentFact (+14 more)

### Community 48 - ".TryGetKey"
Cohesion: 0.23
Nodes (5): NotificationDedupPolicy, Fact, InlineData, Theory, NotificationDedupPolicyTests

### Community 49 - "CommonEqualityComparer"
Cohesion: 0.18
Nodes (5): IEqualityComparer, Func, CommonEqualityComparer, Func, CommonEqualityComparer

### Community 50 - "網頁管理設定中心：爬蟲與會員驗證實作計畫"
Cohesion: 0.05
Nodes (40): 10.1 授權, 10.2 爬蟲, 10.3 YouTube 會員驗證, 10.4 Twitch 訂閱驗證, 10. 手動驗收矩陣, 11. 實作順序, 12. 完成閘門, 13. 新 Session 交接指令 (+32 more)

### Community 51 - "水平擴展（三層拆分）計畫 — Redis Streams 版"
Cohesion: 0.10
Nodes (21): 10. 可優化項目（claude 分支已有成品，對應階段順手移植）, 11. 驗證清單（部署前全過）, 1. 目標架構, 2.1 `Shared`（共用 library）, 2.2 `Scraper`（爬蟲層，叢集唯一）, 2.3 `Notifier`（通知層 / shard，可多個）, 2.4 `Coordinator`（主控層，1 個）, 2.5 SharedService 逐服務拆分歸屬（判斷準則表） (+13 more)

### Community 52 - "YoutubeMemberSetting"
Cohesion: 0.25
Nodes (9): CommandExample, CommandSummary, DiscordSocketClient, GuildYoutubeMemberConfig, IRole, RequireGuildMemberCount, SlashCommand, Task (+1 more)

### Community 53 - "TwitchStateDecisions.cs"
Cohesion: 0.11
Nodes (19): TimeSpan, TwitchChannelUpdateAction, TwitchGuildEligibilityPolicy, TwitchMissingObservationAction, TwitchOfflineAction, TwitchOfflineFacts, TwitchOfflinePolicy, TwitchOfflineScheduleAction (+11 more)

### Community 54 - "Utility"
Cohesion: 0.27
Nodes (10): DefaultMemberPermissions, DiscordSocketClient, DiscordWebhookClient, IChannel, ITextChannel, RequireContext, RequireUserPermission, SlashCommand (+2 more)

### Community 55 - ".PrepareMemberCheckCleanupAsync"
Cohesion: 0.18
Nodes (13): CheckId, Snapshot, CancellationToken, CancellationToken, IEnumerable, List, Task, YoutubeMemberService (+5 more)

### Community 56 - ".ReconcileUserStateAsync"
Cohesion: 0.28
Nodes (6): DateTime, TwitchSpiderRemovalMetricReason, DateTime, TwitchSpider, TwitchEventSubCleanupDeferredMetricReason, TwitchUserState

### Community 57 - "AGENTS.md"
Cohesion: 0.17
Nodes (11): Build & Run, Conventions, EF Core 鐵則, graphify, 制度條款, 外部契約（不可片面更改）, 指令文件, 架構要點（現行樹） (+3 more)

### Community 58 - "TwitchRefreshRotationLifecycle"
Cohesion: 0.16
Nodes (13): Action, bool, Dictionary, Lease, long, object, Task, TaskCompletionSource (+5 more)

### Community 59 - ".RetryWithBackoffAsync"
Cohesion: 0.13
Nodes (14): ConnectionMultiplexer, Lazy, object, string, RedisConnection, Func, Task, TimeProvider (+6 more)

### Community 60 - "DiscordStreamNotifyBot.DataBase"
Cohesion: 0.11
Nodes (14): DiscordStreamNotifyBot.Tests.Component.Redis, DiscordStreamNotifyBot.Interaction.Utility.Service, DiscordStreamNotifyBot.Interaction.Utility, DiscordStreamNotifyBot.Interaction.Attribute, DiscordStreamNotifyBot.Interaction.TwitCasting, DiscordStreamNotifyBot.Shared, DiscordStreamNotifyBot.Command.YoutubeMember, DiscordStreamNotifyBot.Command.Admin (+6 more)

### Community 61 - ".Warn"
Cohesion: 0.08
Nodes (21): NowStreamingHost, DiscordSocketClient, Embed, EmojiService, HttpClient, IEnumerable, IHttpClientFactory, List (+13 more)

### Community 62 - "graphify reference: extra exports and benchmark"
Cohesion: 0.22
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 63 - "Bot"
Cohesion: 0.14
Nodes (12): BotPlayingStatus, ConnectionMultiplexer, DiscordSocketClient, IDatabase, int, ISubscriber, IUser, Task (+4 more)

### Community 64 - "DiscordStreamNotifyBot.DataBase.Table"
Cohesion: 0.10
Nodes (6): DiscordStreamNotifyBot.Tests.Component.MySql, DiscordStreamNotifyBot.DataBase.Table, DiscordStreamNotifyBot.Interaction.TwitchSubscription, DiscordStreamNotifyBot.SharedService.TwitchSubscription, Fact, YoutubeMembershipSchemaContractTests

### Community 65 - "TwitchReconcileDecisionTests"
Cohesion: 0.24
Nodes (6): TwitchGuildEligibilityFacts, TwitchMissingGuildObservation, TwitchReconcileFacts, DateTime, Fact, TwitchReconcileDecisionTests

### Community 66 - "RedisComponentFixture"
Cohesion: 0.17
Nodes (12): ConfigurationOptions, FactAttribute, string, MySqlComponentFactAttribute, ConnectionMultiplexer, IDatabase, RedisKey, string (+4 more)

### Community 67 - ".Main"
Cohesion: 0.12
Nodes (12): Assembly, CancellationToken, Exception, int, PeriodicTimer, Task, Program, HashSet (+4 more)

### Community 69 - "AdminSettingsService"
Cohesion: 0.11
Nodes (15): Id, Name, RequestRoute, CancellationToken, DiscordSocketClient, Func, IEnumerable, int (+7 more)

### Community 70 - "NotificationContractTests"
Cohesion: 0.30
Nodes (3): Fact, JObject, NotificationContractTests

### Community 71 - "EF Core 遷移與基線化（本專案版）"
Cohesion: 0.25
Nodes (7): EF Core 遷移與基線化（本專案版）, 一次性基線化（舊的 EnsureCreated 正式庫）, 一般變更流程, 你必須先知道的三件專案特例, 啟動時不碰資料庫（重要）, 套用：本地/開發 vs 正式環境, 收尾

### Community 72 - "TwitchSubscriptionRolePolicyTests"
Cohesion: 0.16
Nodes (10): AddRoleIds, IReadOnlySet, RemoveRoleIds, Func, IReadOnlyList, TwitchSubscriptionRolePolicy, Fact, InlineData (+2 more)

### Community 73 - "11. 通知與背景訊息"
Cohesion: 0.29
Nodes (7): 11.1 現況限制, 11.2 目標作法, 11.3 YouTube, 11.4 Twitch, 11.5 TwitCasting, 11.6 YouTube 會限驗證, 11. 通知與背景訊息

### Community 74 - "GoogleOAuthOperationLockLease"
Cohesion: 0.15
Nodes (15): CancellationToken, CancellationTokenSource, IDatabase, int, RedisKey, RedisValue, string, Task (+7 more)

### Community 75 - "LogEvent"
Cohesion: 0.23
Nodes (4): LogEvent, LogEventLevel, LogTextFormatter, TextWriter

### Community 80 - "YoutubeStream"
Cohesion: 0.15
Nodes (25): ICommandService, Alias, ClusterQueryService, Command, CommandExample, DiscordSocketClient, IEnumerable, List (+17 more)

### Community 81 - "AdminSettingsMutationResult"
Cohesion: 0.11
Nodes (19): IInteractionService, DiscordSocketClient, SocketGuild, AdminSettingsChannelValidator, Broadcaster, CancellationToken, DiscordSocketClient, EmojiService (+11 more)

### Community 82 - "AdminSettingsContractTests"
Cohesion: 0.12
Nodes (7): CrawlerPolicy, Fact, InlineData, MessageComponent, string, Theory, AdminSettingsContractTests

### Community 83 - "AdminSettings.cs"
Cohesion: 0.09
Nodes (37): Dictionary, int, List, string, AdminProbeVideoPayload, AdminRemoveNotificationPayload, AdminSetChannelPayload, AdminSetLocalePayload (+29 more)

### Community 84 - ".PublishYoutubeNotificationAsync"
Cohesion: 0.16
Nodes (10): GeneratedRegex, YTChannelType, DateTime, DbSet, MainDbContext, Regex, Task, Video (+2 more)

### Community 85 - "graphify reference: query, path, explain"
Cohesion: 0.33
Nodes (5): For /graphify explain, For /graphify path, graphify reference: query, path, explain, Step 0 — Constrained query expansion (REQUIRED before traversal), Step 1 — Traversal

### Community 86 - "自動化測試導入計畫"
Cohesion: 0.17
Nodes (12): 10. 測試實作規則, 1. 目標, 2. 測試分類, 3. 不移除的啟動檢查, 4. 第一批：低耦合契約與格式化, 5. 第二批：小幅抽出純邏輯, 6. 第三批：時間與快取, 7. 第四批：Scraper 狀態機 (+4 more)

### Community 87 - "YoutubeApiService"
Cohesion: 0.24
Nodes (8): IEnumerable, IHttpClientFactory, List, string, Task, YouTubeService, YTApiVideo, YoutubeApiService

### Community 88 - "DescriptionOnlyLocalizationManager"
Cohesion: 0.29
Nodes (7): ILocalizationManager, ResxLocalizationManager, IDictionary, IList, LocalizationTarget, string, DescriptionOnlyLocalizationManager

### Community 89 - ".Get"
Cohesion: 0.13
Nodes (8): CultureInfo, IReadOnlyList, string, SupportedLocale, Fact, InlineData, Theory, BotLocalizerTests

### Community 90 - ".CheckMemberShipCore"
Cohesion: 0.35
Nodes (6): SocketRole, SocketTextChannel, SocketGuild, Task, YoutubeMemberNotMemberApplyResult, YoutubeMemberService

### Community 91 - "TwitchSubscriptionApiClientTests"
Cohesion: 0.18
Nodes (15): HttpMessageHandler, HttpStatusCode, IHttpClientFactory, CancellationToken, Fact, Func, HttpClient, HttpRequestMessage (+7 more)

### Community 92 - ".ShutdownAsync"
Cohesion: 0.18
Nodes (9): DelegatingHandler, LogMessage, CancellationToken, HttpRequestMessage, HttpResponseMessage, int, Task, TimeSpan (+1 more)

### Community 93 - "6. 資源架構"
Cohesion: 0.40
Nodes (5): 6.1 指令註冊資源, 6.2 執行期訊息資源, 6.3 Help 長文, 6.4 Localizer API, 6. 資源架構

### Community 94 - ".AssertKeysAbsentAsync"
Cohesion: 0.27
Nodes (8): IDatabase, RedisComponentFact, StreamEntry, Task, NotificationBusConsumerRedisComponentTests, RedisComponentFact, Task, NotificationBusRedisComponentTests

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

### Community 101 - "網頁管理設定中心實作計畫"
Cohesion: 0.12
Nodes (16): 10. 首版完成閘門, 11. 驗證, 12. 實作順序, 1. 目標, 2. 已確認產品決策, 3. 系統邊界, 4.1 命令, 4.2 回應 (+8 more)

### Community 102 - ".Plan"
Cohesion: 0.19
Nodes (11): HashSet, IReadOnlyCollection, IReadOnlyList, TwitchEventSubCreateSpec, TwitchEventSubFact, TwitchEventSubFinalDecision, TwitchEventSubReconcilePlan, TwitchEventSubReconcilePolicy (+3 more)

### Community 103 - ".New"
Cohesion: 0.31
Nodes (4): ConsoleColor, LogFileRoute, LogLevel, Exception

### Community 105 - "DiscordWebhookClient"
Cohesion: 0.31
Nodes (6): CancellationToken, DiscordSocketClient, HttpClient, Task, DiscordWebhookClient, Message

### Community 106 - "DiscordStreamNotifyBot.Command.Attribute"
Cohesion: 0.14
Nodes (9): Attribute, DiscordStreamNotifyBot.Command.Attribute, DiscordStreamNotifyBot.Command.Twitch, string, CommandExampleAttribute, string, CommandExampleAttribute, string (+1 more)

### Community 113 - ".CreateOrRepairConfigurationAsync"
Cohesion: 0.25
Nodes (12): IReadOnlyCollection, MemberRoleOwnershipSnapshot, CancellationToken, DiscordSocketClient, Exception, ICollection, IRole, NotifierMetrics (+4 more)

### Community 114 - "TopLevelModule"
Cohesion: 0.43
Nodes (4): ModuleBase, EmbedBuilder, Task, TopLevelModule

### Community 115 - "DiscordStreamNotifyBot.Notifier.csproj"
Cohesion: 0.10
Nodes (19): Microsoft.Extensions.DependencyInjection.Abstractions (10.0.1), System.Management (10.0.1), net8.0, Ben.Demystifier (0.4.1), Discord.Net (3.19.1), Dorssel.Utilities.Debounce (3.0.0), EFCore.NamingConventions (9.0.0), Google.Apis.YouTube.v3 (1.73.0.3981) (+11 more)

### Community 116 - "TwitchService"
Cohesion: 0.16
Nodes (10): CancellationToken, DiscordSocketClient, EmojiService, NoticeCache, NotifierMetrics, SocketGuild, Task, User (+2 more)

### Community 117 - "ReactionEventWrapper"
Cohesion: 0.29
Nodes (8): bool, Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task, ReactionEventWrapper

### Community 118 - "ClusterService"
Cohesion: 0.15
Nodes (14): CancellationToken, PeriodicTimer, string, Task, TimeSpan, ScraperService, IDatabase, string (+6 more)

### Community 119 - "DiscordStreamNotifyBot.Scraper.Detection.Twitcasting"
Cohesion: 0.47
Nodes (4): DiscordStreamNotifyBot.Scraper.Detection.Twitcasting, Broadcaster, Movie, TwitCastingWebHookJson

### Community 120 - "Twitch OAuth 與零成本 EventSub 實作計畫"
Cohesion: 0.14
Nodes (13): 0. 涉及專案, 10. Backend EventSub Webhook, 12. Frontend, 14. Grafana, 18. 建置與遷移, 19. 部署順序, 1. 不可偏離的決策, 20. 官方參考 (+5 more)

### Community 121 - "YoutubeMember"
Cohesion: 0.36
Nodes (5): RequireContext, SlashCommand, Task, YoutubeMemberCheck, YoutubeMember

### Community 122 - "YoutubeMemberApiClient"
Cohesion: 0.26
Nodes (7): GoogleApiException, GoogleCredential, CancellationToken, HashSet, Task, YoutubeMemberApiClient, YoutubeMemberProbeResult

### Community 124 - "16. 執行階段"
Cohesion: 0.22
Nodes (9): 16. 執行階段, 階段 0：前置確認, 階段 1：資料模型與 Backend 設定, 階段 2：Google/Twitch OAuth 隔離, 階段 3：Frontend, 階段 4：Twitch add資格與授權清理, 階段 5：StreamOnline 與 EventSub reconcile, 階段 6：Prometheus 與 Grafana (+1 more)

### Community 125 - "Prometheus / Grafana 監控"
Cohesion: 0.20
Nodes (9): Backend 指標, Coordinator 指標, Endpoints, Grafana, Notifier 指標, Prometheus, Prometheus / Grafana 監控, Scraper 指標 (+1 more)

### Community 126 - "TwitcastingClient"
Cohesion: 0.29
Nodes (5): HttpClient, List, string, Task, TwitcastingClient

### Community 127 - "DiscordStreamNotifyBot.Scraper.csproj"
Cohesion: 0.50
Nodes (3): net8.0, prometheus-net.AspNetCore (8.2.1), Microsoft.NET.Sdk

### Community 128 - "DiscordStreamNotifyBot.Tests.csproj"
Cohesion: 0.25
Nodes (7): coverlet.collector (6.0.0), Microsoft.Extensions.TimeProvider.Testing (9.0.0), Microsoft.NET.Test.Sdk (17.8.0), xunit (2.5.3), xunit.runner.visualstudio (2.5.3), net8.0, Microsoft.NET.Sdk

### Community 129 - "17. 驗證矩陣"
Cohesion: 0.33
Nodes (6): 17.1 新增 spider, 17.2 EventSub, 17.3 授權失效, 17.4 OAuth, 17.5 Prometheus/Grafana, 17. 驗證矩陣

### Community 130 - ".CreateStreamEnded"
Cohesion: 0.32
Nodes (6): DateTime, EmbedBuilder, IReadOnlyCollection, TimeSpan, TwitchEmbedBuilderFactory, TwitchClipInfo

### Community 131 - "Help"
Cohesion: 0.31
Nodes (8): Alias, Command, CommandService, IServiceProvider, string, Summary, Task, Help

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

### Community 137 - "DiscordStreamNotifyBot"
Cohesion: 0.15
Nodes (5): DiscordStreamNotifyBot.HttpClients, DiscordStreamNotifyBot.Command.TwitCasting, DiscordStreamNotifyBot, ServiceProvider, DetectionHost

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

### Community 143 - "CommandDisplayResolver"
Cohesion: 0.10
Nodes (17): RequireBotPermissionAttribute, RequireUserPermissionAttribute, SlashCommand, SlashCommandInfo, CommandTextEqualityComparer, EmbedBuilder, IEnumerable, SlashCommandInfo (+9 more)

### Community 144 - "13. Prometheus"
Cohesion: 0.67
Nodes (3): 13.1 Backend 指標, 13.2 Scraper 指標, 13. Prometheus

### Community 145 - "4. 安全刪除狀態機"
Cohesion: 0.67
Nodes (3): 4.1 直播中授權失效, 4.2 關台後重新判斷, 4. 安全刪除狀態機

### Community 146 - ".HandleTwitcastingAsync"
Cohesion: 0.60
Nodes (3): ComponentInteraction, Task, SpiderManagementComponent

### Community 147 - "NotificationEmbedFactoryTests"
Cohesion: 0.24
Nodes (8): Color, DateTime, Embed, Fact, InlineData, string, Theory, NotificationEmbedFactoryTests

### Community 148 - "DiscordStreamNotifyBot.Migrations"
Cohesion: 0.14
Nodes (8): DiscordStreamNotifyBot.Migrations, ModelSnapshot, ModelBuilder, AddMaxSpiderCountSettingField, ModelBuilder, AddManualMemberCheckVideoFlag, ModelBuilder, MainDbContextModelSnapshot

### Community 149 - ".SendStreamMessageAsync"
Cohesion: 0.11
Nodes (20): Event, HttpException, Platform, NotificationMetricEvent, TwitchNotification, TwitchSpider, DateTime, List (+12 more)

### Community 156 - ".Get"
Cohesion: 0.19
Nodes (10): DateTimeOffset, Func, List, object, TimeProvider, TimeSpan, NoticeCache, Fact (+2 more)

### Community 157 - "SendMsgToAllGuildService"
Cohesion: 0.17
Nodes (12): ButtonCheckData, DiscordStreamNotifyBot.Interaction.OwnerOnly.Service, SendAllPayload, bool, DiscordSocketClient, Embed, Task, ButtonCheckData (+4 more)

### Community 158 - ".CheckRequirementsAsync"
Cohesion: 0.25
Nodes (6): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildAttribute

### Community 159 - ".Classify"
Cohesion: 0.23
Nodes (6): IEnumerable, YoutubeMemberProbeResultKind, Fact, InlineData, Theory, YoutubeMemberApiClientTests

### Community 160 - "DiscordStreamNotifyBot.HttpClients.Twitcasting.Model"
Cohesion: 0.15
Nodes (11): DiscordStreamNotifyBot.HttpClients.Twitcasting.Model, Alias, Command, RequireContext, RequireOwner, Task, List, Broadcaster (+3 more)

### Community 162 - ".MakeNamesUnique"
Cohesion: 0.31
Nodes (5): IEnumerable, int, IReadOnlyList, AutocompleteCandidate, AutocompleteSearch

### Community 164 - "Help"
Cohesion: 0.27
Nodes (9): AutocompleteHandler, DiscordStreamNotifyBot.Interaction.Help, HelpService, InteractionService, Help, HelpGetCommandsAutocompleteHandler, HelpGetModulesAutocompleteHandler, GuildNoticeTwitCastingChannelIdAutocompleteHandler (+1 more)

### Community 165 - ".LoadSnapshotAsync"
Cohesion: 0.17
Nodes (9): CancellationToken, ICollection, IEnumerable, Task, MemberEntitlementProvider, MemberRoleEntitlement, MemberRoleOwnershipPolicy, MemberRoleOwnershipService (+1 more)

### Community 166 - ".CheckRequirementsAsync"
Cohesion: 0.25
Nodes (6): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildMemberCountAttribute

### Community 167 - ".NotifyAddedAsync"
Cohesion: 0.27
Nodes (7): Components, Embed, MessageComponent, SocketGuild, Task, CrawlerOwnerNotifier, CrawlerPlatform

### Community 168 - "InteractionErrorPolicyTests"
Cohesion: 0.39
Nodes (4): InlineData, InteractionCommandError, Theory, InteractionErrorPolicyTests

### Community 169 - ".GetAvailableCommandsAsync"
Cohesion: 0.40
Nodes (7): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IReadOnlyList, IServiceProvider, Task

### Community 170 - "YoutubeMemberLifecycleTaskRegistry"
Cohesion: 0.11
Nodes (13): bool, ConcurrentDictionary, DateTime, IEnumerable, long, object, Task, TimeSpan (+5 more)

### Community 171 - ".Decide"
Cohesion: 0.36
Nodes (4): TwitchSpiderRemovalFacts, TwitchSpiderRemovalMetricReason, InlineData, Theory

### Community 172 - "YoutubeVideoIdParserTests"
Cohesion: 0.39
Nodes (4): InlineData, string, Theory, YoutubeVideoIdParserTests

### Community 173 - "CommandTextEqualityComparer"
Cohesion: 0.38
Nodes (3): DiscordStreamNotifyBot.Command.Help, CommandInfo, CommandTextEqualityComparer

### Community 174 - "TwitchDetectionService.cs"
Cohesion: 0.29
Nodes (4): DiscordStreamNotifyBot.Scraper.Detection.Twitch.Debounce, TwitchAuthorizationChangedPayload, TwitchReconcileRequestedPayload, TwitchStreamEventPayload

### Community 175 - "NotificationBusConsumer"
Cohesion: 0.19
Nodes (11): CancellationToken, Func, IDatabase, int, StreamEntry, Task, TwitcastingService, TwitchService (+3 more)

### Community 176 - "GuildTwitchSubscriptionConfig"
Cohesion: 0.11
Nodes (11): IQueryable, DateTimeOffset, IEnumerable, int, IReadOnlyCollection, TwitchRateLimitPolicy, TwitchSubscriptionConfigurationPolicy, TwitchSubscriptionConfigurationQueries (+3 more)

### Community 177 - "HelpService"
Cohesion: 0.57
Nodes (3): CommandInfo, EmbedBuilder, HelpService

### Community 178 - "YoutubeStream"
Cohesion: 0.40
Nodes (9): DiscordStreamNotifyBot.Command.Youtube, Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task (+1 more)

### Community 179 - "YoutubeApiVideoPolicyTests"
Cohesion: 0.20
Nodes (9): YoutubeApiVideoAction, YoutubeApiVideoDecision, YoutubeApiVideoFacts, YoutubeApiVideoPolicy, DateTime, Fact, InlineData, Theory (+1 more)

### Community 180 - ".PlanChannel"
Cohesion: 0.20
Nodes (9): YoutubeMemberCandidateAction, YoutubeMemberCandidateFacts, YoutubeMemberChannelDecision, YoutubeMemberChannelFacts, YoutubeMemberVideoPolicy, Fact, InlineData, Theory (+1 more)

### Community 181 - "TwitchChannelUpdateDecisionTests"
Cohesion: 0.30
Nodes (6): TwitchChannelEventFacts, TwitchChannelStateFacts, TwitchChannelUpdateDecision, DateTime, Fact, TwitchChannelUpdateDecisionTests

### Community 182 - ".HandleStartLiveMessageAsync"
Cohesion: 0.19
Nodes (11): List, RedisValue, SemaphoreSlim, Task, TwitcastingDetectionService, TwitcastingLiveStartAction, TwitcastingLiveStartFacts, TwitcastingLiveStartPlan (+3 more)

### Community 183 - "DebounceChannelUpdateMessage"
Cohesion: 0.18
Nodes (9): CancellationTokenRegistration, DebouncedEventArgs, Debouncer, Func, int, IReadOnlyCollection, string, Task (+1 more)

### Community 184 - "YoutubeDetectionService"
Cohesion: 0.16
Nodes (11): ConcurrentBag, bool, ConcurrentDictionary, HttpClient, IEnumerable, IHttpClientFactory, Task, YoutubeApiService (+3 more)

### Community 185 - "YoutubeVideoIdParser"
Cohesion: 0.26
Nodes (3): string, Uri, YoutubeVideoIdParser

### Community 186 - "8. 分階段實作步驟"
Cohesion: 0.22
Nodes (9): 8. 分階段實作步驟, 階段 0：止血 PR — shard 歸屬守衛, 階段 1：Solution 骨架 + Shared, 階段 2：Notifier 上線（先維持單 shard 行為）, 階段 3：Scraper 拆出 + Redis Streams 匯流排（完成，正確性待測試環境驗）, 階段 4：Coordinator（完成，正確性待測試環境驗）, 階段 5：跨 shard 指令與共享狀態（完成，正確性待測試環境驗）, 階段 6：Docker 化與部署驗證（檔案完成，實跑待測試環境） (+1 more)

### Community 187 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildTwitCastingSpiderAutocompleteHandler

### Community 188 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildNoticeTwitchChannelIdAutocompleteHandler

### Community 189 - ".SetMessage"
Cohesion: 0.29
Nodes (7): CommandExample, CommandSummary, DiscordSocketClient, RequireBotPermission, SlashCommand, Task, Twitch

### Community 190 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildTwitchSpiderAutocompleteHandler

### Community 191 - "YoutubeMemberVideoLogMessageFormatterTests"
Cohesion: 0.37
Nodes (5): YoutubeMemberVideoLogNotification, Fact, InlineData, Theory, YoutubeMemberVideoLogMessageFormatterTests

### Community 192 - "Category"
Cohesion: 0.43
Nodes (5): IEnumerable, List, CategoriesJson, Category, SubCategory

### Community 193 - "DiscordStreamNotifyBot.SharedService.YoutubeMember"
Cohesion: 0.16
Nodes (8): DiscordStreamNotifyBot.SharedService.YoutubeMember, DiscordStreamNotifyBot.Interaction.YoutubeMember, DiscordStreamNotifyBot.SharedService.Member, YoutubeMemberAutomaticMutationAction, YoutubeMemberManualPinPolicy, Fact, YoutubeMemberManualPinPolicyTests, YoutubeMemberSafeLoggingTests

### Community 194 - "TwitchSubscriptionSetting"
Cohesion: 0.46
Nodes (5): DefaultMemberPermissions, IRole, SlashCommand, Task, TwitchSubscriptionSetting

### Community 195 - "BotState"
Cohesion: 0.12
Nodes (12): DateTime, YoutubePubSubNotification, YTNotificationType, Task, YoutubeDetectionService, ConnectionMultiplexer, IDatabase, ISubscriber (+4 more)

### Community 196 - "NijisanjiStreamJson.cs"
Cohesion: 0.43
Nodes (6): DateTime, List, Channel, EventLiver, Liver, NijisanjiStreamJson

### Community 197 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildYoutubeChannelSpiderAutocompleteHandler

### Community 198 - ".TryGetPayload"
Cohesion: 0.23
Nodes (5): StreamEntry, Fact, InlineData, Theory, RedisContractTests

### Community 199 - "TwitchChannelUpdateInfo"
Cohesion: 0.27
Nodes (6): IEnumerable, IReadOnlyList, TwitchChannelUpdateBatch, TwitchChannelUpdateChange, TwitchChannelUpdatePolicy, TwitchChannelUpdateInfo

### Community 200 - "TcBackendStreamData.cs"
Cohesion: 0.44
Nodes (8): App, BackendMovie, Fmp4, Hls, Llfmp4, Streams, TcBackendStreamData, Webrtc

### Community 202 - "TwitchEventSubEnsureMode"
Cohesion: 0.31
Nodes (9): EventSubSubscription, IReadOnlyList, Stream, TwitchEventSubDeleteResult, TwitchEventSubDeleteStatus, TwitchEventSubEnsureMode, TwitchEventSubEnsureResult, TwitchEventSubSubscriptionsResult (+1 more)

### Community 203 - "AGENTS.md"
Cohesion: 0.14
Nodes (7): 一、`claude` 分支是你最大的資產，也是最大的陷阱, 三、使用者已做的決策，不要重新辯論, 二、你在活的生產系統旁施工, 給未來 session 的信, 這套制度最可能的退化方式，與預防, License, 直播小幫手 [點我邀請到你的 Discord 內](https://discordapp.com/api/oauth2/authorize?client_id=758222559392432160&permissions=2416143425&scope=bot%20applications.commands)

### Community 204 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildYoutubeMemberCheckChannelIdAutocompleteHandler

### Community 205 - "TwitchSubscription"
Cohesion: 0.31
Nodes (6): ComponentInteraction, GuildTwitchSubscriptionConfig, RequireContext, SlashCommand, Task, TwitchSubscription

### Community 208 - ".CreateAsync"
Cohesion: 0.05
Nodes (31): IAsyncDisposable, IDisposable, IServiceProvider, IServiceScope, IServiceScopeFactory, List, SemaphoreSlim, ValueTask (+23 more)

### Community 209 - "4. 訊息契約：Redis Streams 通知匯流排"
Cohesion: 0.33
Nodes (6): 4.1 拓撲, 4.2 DTO（`Shared/Messages/`）, 4.3 消費迴圈（Notifier）, 4.4 建群與 Preflight, 4.5 Redis 控制平面鍵（非 stream）, 4. 訊息契約：Redis Streams 通知匯流排

### Community 210 - "5. 語系模型與解析規則"
Cohesion: 0.33
Nodes (6): 5.1 支援值, 5.2 公開內容與背景通知, 5.3 私人即時回覆, 5.4 延遲會限驗證 DM, 5.5 併發安全, 5. 語系模型與解析規則

### Community 211 - ".CheckPermissionsAsync"
Cohesion: 0.25
Nodes (6): CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task, RequireGuildOwnerAttribute

### Community 212 - ".CheckRequirementsAsync"
Cohesion: 0.25
Nodes (6): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildOwnerAttribute

### Community 213 - ".Main"
Cohesion: 0.25
Nodes (5): Task, CancellationToken, CancellationTokenSource, int, GracefulShutdown

### Community 214 - ".Resolve"
Cohesion: 0.57
Nodes (3): InteractionCommandError, InteractionErrorDescriptor, InteractionErrorPolicy

### Community 215 - "YouTube 會員驗證架構重構計畫"
Cohesion: 0.15
Nodes (12): 11. 排程與生命週期, 12. Provider Result 分類, 17. Manual Acceptance Matrix, 18. 停機部署順序, 19. Completion Criteria, 1. 範圍, 20. 新 Session 執行規則, 2. 已定案決策 (+4 more)

### Community 216 - "TwitchOAuthRefreshLockRedisComponentTests"
Cohesion: 0.31
Nodes (4): IConnectionMultiplexer, RedisComponentFact, Task, TwitchOAuthRefreshLockRedisComponentTests

### Community 217 - "NijisanjiLiverJson.cs"
Cohesion: 0.53
Nodes (5): DiscordStreamNotifyBot.SharedService.Youtube.Json, Head, Images, NijisanjiLiverJson, SocialLinks

### Community 218 - ".SendMessageToAllGuildAsync"
Cohesion: 0.22
Nodes (7): DiscordStreamNotifyBot.Interaction.OwnerOnly, SendMsgToAllGuildService, DefaultMemberPermissions, RequireOwner, SlashCommand, Task, SendMsgToAllGuild

### Community 219 - ".GuildMemberCountPreconditionMapsValuesAndContactPath"
Cohesion: 0.33
Nodes (3): string, InteractionErrorCodes, Fact

### Community 220 - ".GenerateSuggestionsAsync"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 221 - ".GenerateSuggestionsAsync"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 223 - "5. 目標架構"
Cohesion: 0.40
Nodes (5): 5.1 Console, 5.2 非容器檔案, 5.3 Loki, 5.4 `LOKI_URL` 相容性, 5. 目標架構

### Community 224 - "DebounceFixture"
Cohesion: 0.29
Nodes (7): FakeTimeProvider, IReadOnlyCollection, List, UserId, DebounceFixture, UserLogin, UserName

### Community 225 - "UtilityService"
Cohesion: 0.38
Nodes (7): IInteractionService, CancellationToken, DiscordSocketClient, IServiceProvider, SocketGuild, Task, UtilityService

### Community 226 - "15. 實作階段"
Cohesion: 0.20
Nodes (10): 15. 實作階段, Phase 0：Baseline 與 characterization, Phase 1：Schema 與 migration, Phase 2：共用操作與 role ownership, Phase 3：YouTube interaction 與 state machine, Phase 4：Role/config durability, Phase 5：Provider 與 lifecycle, Phase 6：Backend (+2 more)

### Community 227 - "Migration"
Cohesion: 0.40
Nodes (3): Migration, MigrationBuilder, AddGoogleOAuthUnlinkIntent

### Community 228 - "EmojiService"
Cohesion: 0.29
Nodes (4): DiscordStreamNotifyBot.SharedService, Emote, DiscordSocketClient, EmojiService

### Community 230 - ".LoadCommandFrom"
Cohesion: 0.40
Nodes (4): Assembly, IEnumerable, IServiceCollection, Type

### Community 231 - "YoutubeMemberRoleApplyResult"
Cohesion: 0.50
Nodes (3): YoutubeMemberRoleApplyResult, InlineData, Theory

### Community 232 - "BotRole"
Cohesion: 0.40
Nodes (3): int, Program, BotRole

### Community 235 - "YouTube 會員驗證"
Cohesion: 0.33
Nodes (5): Durable state, YouTube 會員驗證, 使用者契約, 服務邊界, 部署前驗證

### Community 237 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildNoticeYoutubeChannelIdAutocompleteHandler

### Community 238 - "14. Frontend"
Cohesion: 0.40
Nodes (5): 14.1 TypeScript contract, 14.2 GoogleSection, 14.3 VerifyWindow, 14.4 Copy/Privacy, 14. Frontend

### Community 239 - "8. DB Schema"
Cohesion: 0.40
Nodes (5): 8.1 Entity changes, 8.2 Indexes, 8.3 Migration 規則, 8.4 Preflight 查詢, 8. DB Schema

### Community 241 - "GetAllRegistedWebHookJson.cs"
Cohesion: 0.67
Nodes (3): List, GetAllRegistedWebHookJson, Webhook

### Community 244 - "13. Backend Contract"
Cohesion: 0.50
Nodes (4): 13.1 Entity/DTO, 13.2 GET `/account-links`, 13.3 DELETE `/account-links/google`, 13. Backend Contract

### Community 245 - "16. 驗證命令"
Cohesion: 0.50
Nodes (4): 16.1 Bot, 16.2 Backend, 16.3 Frontend, 16. 驗證命令

### Community 247 - ".SameUserMutationsAreExclusiveAndOwnerReleaseRemovesTheKey"
Cohesion: 0.33
Nodes (4): IConnectionMultiplexer, RedisComponentFact, Task, GoogleOAuthOperationLockRedisComponentTests

### Community 248 - "10. Slash 與 Interaction Cutover"
Cohesion: 0.67
Nodes (3): 10.1 Command rename, 10.2 Component ID, 10. Slash 與 Interaction Cutover

### Community 249 - "6. 目標架構"
Cohesion: 0.67
Nodes (3): 6.1 共用元件, 6.2 YouTube 模組, 6. 目標架構

### Community 250 - "7. 狀態機"
Cohesion: 0.67
Nodes (3): 7.1 Check state, 7.2 Config state, 7. 狀態機

### Community 251 - "9. Role 隔離政策"
Cohesion: 0.67
Nodes (3): 9.1 新設定, 9.2 既有碰撞, 9. Role 隔離政策

### Community 252 - "TwitchSpider"
Cohesion: 0.35
Nodes (6): CommandExample, CommandSummary, SlashCommand, Task, TwitchService, TwitchSpider

### Community 253 - ".CreateStreamStarted"
Cohesion: 0.33
Nodes (4): EmbedBuilder, TwitcastingEmbedBuilderFactory, DateTime, TwitcastingStream

### Community 257 - "DiscordStreamNotifyBot.Tests"
Cohesion: 0.10
Nodes (9): DiscordStreamNotifyBot.Scraper.Detection.Youtube, DiscordStreamNotifyBot.Scraper, DiscordStreamNotifyBot.Tests, DiscordStreamNotifyBot.Scraper.Detection.Twitch, string, BotStateCollectionDefinition, Fact, NotificationBusConsumerOptionsTests (+1 more)

### Community 258 - ".AuthorizationEventRequiresCurrentPersistedRevocation"
Cohesion: 0.40
Nodes (3): TwitchAuthorizationEventPolicy, InlineData, Theory

## Knowledge Gaps
- **469 isolated node(s):** `net8.0`, `prometheus-net.AspNetCore (8.2.1)`, `Microsoft.NET.Sdk`, `DiscordStreamNotifyBot.Command.Normal`, `DiscordStreamNotifyBot.Command.TwitCasting` (+464 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **41 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MainDbService` connect `MainDbService` to `TwitchAuthorizationTokenService`, `ClusterQueryService`, `.GetDbContext`, `.HandleTwitcastingAsync`, `.GetLocaleAsync`, `AuthTokenTests`, `YoutubeMemberAuthorizationService`, `SendMsgToAllGuildService`, `YoutubeMemberService`, `.LoadSnapshotAsync`, `YoutubeMemberRoleService`, `TwitchDetectionService`, `Twitch`, `GuildLocaleService`, `MySqlComponentFixture`, `YoutubeMemberSetting`, `Utility`, `.HandleStartLiveMessageAsync`, `YoutubeDetectionService`, `DiscordStreamNotifyBot.DataBase`, `.SetMessage`, `.Warn`, `Bot`, `TwitchSubscriptionSetting`, `BotState`, `AdminSettingsService`, `TwitchSubscription`, `YoutubeStream`, `AdminSettingsMutationResult`, `YoutubeApiService`, `UtilityService`, `.CreateOrRepairConfigurationAsync`, `TwitchService`, `YoutubeMember`, `TwitchSpider`?**
  _High betweenness centrality (0.082) - this node is a cross-community bridge._
- **Why does `DiscordStreamNotifyBot.DataBase.Table` connect `DiscordStreamNotifyBot.DataBase.Table` to `DiscordStreamNotifyBot.Tests`, `TwitchAuthorizationTokenService`, `NonApprovedVideos`, `OtherVideos`, `BotLocalizer`, `DiscordStreamNotifyBot.Shared.Messages`, `AuthTokenTests`, `YoutubeMemberPolicies`, `MainDbContext`, `GuildLocaleService`, `TwitchDetectionService.cs`, `GuildTwitchSubscriptionConfig`, `YoutubeStream`, `TwitchStateDecisions.cs`, `.HandleStartLiveMessageAsync`, `DiscordStreamNotifyBot.DataBase`, `DiscordStreamNotifyBot.SharedService.YoutubeMember`, `NotificationContractTests`, `DiscordStreamNotifyBot.Command.Attribute`, `.CreateStreamStarted`?**
  _High betweenness centrality (0.069) - this node is a cross-community bridge._
- **Why does `DiscordStreamNotifyBot.Shared` connect `DiscordStreamNotifyBot.DataBase` to `DiscordStreamNotifyBot.Tests`, `NotificationBus`, `ClusterQueryService`, `DiscordStreamNotifyBot`, `DiscordStreamNotifyBot.Shared.Messages`, `.RunCoreAsync`, `SendMsgToAllGuildService`, `BotConfig`, `YoutubeVideoIdParserTests`, `RedisChannels`, `TwitchDetectionService.cs`, `NotificationBusConsumer`, `YoutubeVideoIdParser`, `.RetryWithBackoffAsync`, `DiscordStreamNotifyBot.DataBase.Table`, `DiscordStreamNotifyBot.SharedService.YoutubeMember`, `BotState`, `.TryGetPayload`, `.Main`, `TwitchOAuthRefreshLockRedisComponentTests`, `BotRole`, `DiscordStreamNotifyBot.Command.Attribute`, `ClusterService`?**
  _High betweenness centrality (0.066) - this node is a cross-community bridge._
- **What connects `net8.0`, `prometheus-net.AspNetCore (8.2.1)`, `Microsoft.NET.Sdk` to the rest of the system?**
  _469 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `TwitchSubscriptionApiClient` be split into smaller, more focused modules?**
  _Cohesion score 0.1053763440860215 - nodes in this community are weakly interconnected._
- **Should `TwitchAuthorizationTokenService` be split into smaller, more focused modules?**
  _Cohesion score 0.1378048780487805 - nodes in this community are weakly interconnected._
- **Should `DiscordStreamNotifyBot.Shared.csproj` be split into smaller, more focused modules?**
  _Cohesion score 0.08333333333333333 - nodes in this community are weakly interconnected._