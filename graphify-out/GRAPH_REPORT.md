# Graph Report - DiscordStreamNotifyBot  (2026-08-31)

## Corpus Check
- 333 files · ~173,480 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 4206 nodes · 9674 edges · 263 communities (222 shown, 41 thin omitted)
- Extraction: 92% EXTRACTED · 8% INFERRED · 0% AMBIGUOUS · INFERRED: 821 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `1fb09ce8`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- TwitchSubscriptionApiClient
- .GetLocaleAsync
- .Warn
- EmojiService
- DiscordStreamNotifyBot.Shared.csproj
- TwitchApiService
- InteractionHandler
- 偵測 → 匯流排 → 發送 路徑除錯
- AuthTokenTests
- YoutubeVideoIdParser
- YoutubeReminderPolicyTests
- .RunCoreAsync
- DiscordStreamNotifyBot.DataBase.Table
- Extensions
- TwitchSubscriptionService
- Extensions
- .ReconcileUserStateAsync
- 會限 OAuth Token 儲存改走 MySQL（去 Redis 依賴）計畫
- .AddChannel
- AdminSettingsMutationResult
- .CreateService
- YoutubeMemberAuthorizationService
- Log
- .RetryWithBackoffAsync
- Twitch 訂閱驗證實作計畫
- YoutubeMemberPolicies
- 新增 TwitCasting 錄影委派計畫（小幫手 ↔ StreamRecordTools）
- 多語系支援計畫
- Serilog Logging 遷移計畫
- 12. 分階段執行
- YoutubeDetectionService
- CoordinatorMetrics
- YoutubeMemberLifecycleTaskRegistry
- LocaleResolver
- AGENTS.md
- NotificationEmbedFactoryTests
- TwitchOAuthRefreshLockLease
- .Format
- .SendCrawlerResultAsync
- YoutubeMemberRoleService
- TwitchDetectionService
- Twitch
- ScraperMetrics
- GuildLocaleService
- TwitchChannelUpdateDecisionTests
- RedisChannels
- 13. 驗證矩陣
- MainDbContext
- 網頁管理設定：30 秒請求與背景清理實作計畫
- Administration
- 網頁管理設定中心：爬蟲與會員驗證實作計畫
- 水平擴展（三層拆分）計畫 — Redis Streams 版
- YoutubeMemberSetting
- .Get
- YoutubeVideoClaimCache
- .SetVerificationLogChannelAsync
- AdministrationService
- AGENTS.md
- TwitchRefreshRotationLifecycle
- TwitchSubscriptionRolePolicyTests
- DiscordStreamNotifyBot.Shared
- YoutubeStreamService
- graphify reference: extra exports and benchmark
- Bot
- DiscordStreamNotifyBot.SharedService.YoutubeMember
- TwitchReconcileDecisionTests
- .FilterNoNotifyGuilds
- .Main
- AddManualMemberCheckVideoFlag
- AdminSettingsService
- NotificationContractTests
- EF Core 遷移與基線化（本專案版）
- NotificationBusConsumer
- 11. 通知與背景訊息
- GoogleOAuthOperationLockLease
- TwitchStreamLifecycleDecisionTests
- FUNDING.yml (Patreon / ECPay / PayPal)
- Build workflow (SonarQube analysis)
- MIT License
- Notifier Bot Logo — interlocking chain-link icon, purple-to-magenta-to-red gradient on light grey circle; flat modern vector branding representing the linking/notification identity of the Discord stream-notify bot
- YoutubeStream
- TwitchService
- AdminSettingsContractTests
- AdminSettings.cs
- .PublishYoutubeNotificationAsync
- graphify reference: query, path, explain
- 自動化測試導入計畫
- YoutubeApiService
- DescriptionOnlyLocalizationManager
- BotLocalizerTests
- YoutubeMemberService
- .CreateAsyncClient
- .Get
- .ToLabel
- .AssertKeysAbsentAsync
- graphify reference: add a URL and watch a folder
- graphify reference: commit hook and native CLAUDE.md integration
- graphify reference: incremental update and cluster-only
- .ExecuteOnceAsync
- graphify reference: GitHub clone and cross-repo merge
- graphify reference: transcribe video and audio
- 網頁管理設定中心實作計畫
- BotConfig
- .Info
- .claude/CLAUDE.md (graphify trigger)
- DiscordWebhookClient
- DiscordStreamNotifyBot.Interaction.Attribute
- Confidence rubric (EXTRACTED/INFERRED/AMBIGUOUS)
- AST structural extraction (Part A)
- Community detection & clustering
- God nodes & surprising connections
- Knowledge graph (graph.json)
- Semantic extraction (parallel subagents)
- .CreateOrRepairConfigurationAsync
- .LoadSnapshotAsync
- DiscordStreamNotifyBot.Notifier.csproj
- .OnlyAffiliateAndPartnerCanBeConfigured
- Normal
- ClusterService
- TwitcastingService
- Twitch OAuth 與零成本 EventSub 實作計畫
- .FixTCDbAsync
- YoutubeMemberApiClient
- .Filter
- 16. 執行階段
- Prometheus / Grafana 監控
- TwitcastingClient
- DiscordStreamNotifyBot.Scraper.csproj
- DiscordStreamNotifyBot.Tests.csproj
- 17. 驗證矩陣
- 7. 分階段執行
- SendMsgToAllGuildService
- 7. OAuth API 與流程隔離
- TwitcastingLiveStartPlannerTests
- ClusterQueryService
- .Plan
- HelpDescription (bot feature summary)
- 20250320095452_RefactorDbContext.Designer.cs
- 11. Bot EventSub 與偵測
- 15. 預期修改檔案
- 2. 現況基線
- 5. Guild 資格與 OAuth 豁免
- DiscordStreamNotifyBot.sln
- CommandDisplayResolver
- 13. Prometheus
- 4. 安全刪除狀態機
- Help
- YoutubeMemberVideoLogMessageFormatterTests
- DiscordStreamNotifyBot.Migrations
- DiscordStreamNotifyBot.Command.Attribute
- RefactorDbContext
- ModifyTwitCastingTable
- AddMaxSpiderCountSettingField
- SyncModelDrift
- AddTwitchBroadcasterAuthorization
- AddLocalizationSettings
- 20250603065853_ModifyTwitCastingTable.Designer.cs
- 20260611015819_SyncModelDrift.Designer.cs
- 20260719142803_AddTwitchBroadcasterAuthorization.Designer.cs
- .Classify
- 20260721095646_AddLocalizationSettings.Designer.cs
- DiscordStreamNotifyBot.Tests
- 20260803141135_AddTwitchSubscriptionVerification.Designer.cs
- 20260803165758_AddTwitchSubscriptionDeletionPending.Designer.cs
- RedisComponentFixture
- 20260804173737_AddYoutubeMemberVerificationDurability.Designer.cs
- .CheckRequirementsAsync
- .NotifyAddedAsync
- 20260807045351_AddGoogleOAuthUnlinkIntent.Designer.cs
- 20260813032017_RenameVerificationLogChannel.Designer.cs
- DebounceChannelUpdateMessage
- .CheckMemberShipCore
- NotificationBusConsumerOptionsTests.cs
- TwitchGuildEligibilityEvaluator
- graphify.js
- .TryGetKey
- GuildTwitchSubscriptionConfig
- .SendStreamMessageAsync
- YoutubeStream
- YoutubeApiVideoPolicyTests
- .CheckMemberShipOnlyVideoIdAsync
- .ShutdownAsync
- .HandleStartLiveMessageAsync
- ReactionEventWrapper
- YoutubeDetectionService
- MigrationAndConstraintTests
- DiscordStreamNotifyBot.Command
- NonPersistentGoogleDataStore
- TwitchSubscriptionPolicies.cs
- .GetCommandHelp
- MySqlDataStore
- .ToMetricEvent
- Category
- .DecideAutomaticMutation
- .GenerateSuggestionsAsync
- Task
- NijisanjiStreamJson.cs
- UptimeKumaClient
- .GetDbContext
- .CheckPermissionsAsync
- TcBackendStreamData.cs
- AddTwitchSubscriptionVerification
- .GroupName
- RedisConnection
- InteractionErrorPolicyTests
- TopLevelModule
- .SendConfirmMessageAsync
- AddTwitchSubscriptionDeletionPending
- .CreateAsync
- .CheckRequirementsAsync
- .CheckRequirementsAsync
- .CheckPermissionsAsync
- Log 與 Loki
- .Resolve
- .GenerateSuggestionsAsync
- YouTube 會員驗證架構重構計畫
- .GenerateSuggestionsAsync
- NijisanjiLiverJson.cs
- NotifierMetrics
- TwitCastingWebHookJson.cs
- .GenerateSuggestionsAsync
- .SlashCommandExecuted
- TwitchStateDecisions.cs
- CommandTextEqualityComparer
- 5. 語系模型與解析規則
- .GenerateSuggestionsAsync
- 15. 實作階段
- Migration
- .GenerateSuggestionsAsync
- 10. 執行期互動本地化
- .LoadCommandFrom
- DiscordStreamNotifyBot.HttpClients.Twitcasting.Model
- .AuthorizationEventRequiresCurrentPersistedRevocation
- AddYoutubeMemberVerificationDurability
- opencode.json
- YouTube 會員驗證
- YoutubeReminderRegistryTests
- MigrationAndConstraintTests.cs
- 14. Frontend
- 8. DB Schema
- TwitchAccessTokenContractTests
- MySqlComponentFixture
- .TryGetPayload
- 13. Backend Contract
- 16. 驗證命令
- .LockGuildAsync
- .SameUserMutationsAreExclusiveAndOwnerReleaseRemovesTheKey
- 10. Slash 與 Interaction Cutover
- 6. 目標架構
- 7. 狀態機
- 9. Role 隔離政策
- AutocompleteHandler
- .SendLocalizedConfirmAsync
- RenameVerificationLogChannel
- TwitcastingSpider
- TwitchStream
- TwitchOAuthRefreshLockRedisComponentTests
- GracefulShutdown
- AdministrationComponent
- .SendMessageToAllGuildAsync
- 14. 部署與回滾
- .IsValidIdentity
- TopLevelModule

## God Nodes (most connected - your core abstractions)
1. `DiscordStreamNotifyBot.DataBase.Table` - 68 edges
2. `DiscordStreamNotifyBot.DataBase` - 64 edges
3. `DiscordStreamNotifyBot.Shared` - 61 edges
4. `TwitchDetectionService` - 59 edges
5. `BotLocalizer` - 53 edges
6. `MainDbContext` - 50 edges
7. `DiscordStreamNotifyBot.Tests` - 50 edges
8. `YoutubeStreamService` - 49 edges
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

## Communities (263 total, 41 thin omitted)

### Community 0 - "TwitchSubscriptionApiClient"
Cohesion: 0.16
Nodes (13): CancellationToken, DateTimeOffset, HttpResponseMessage, IHttpClientFactory, NotifierMetrics, string, Task, TwitchProviderResult (+5 more)

### Community 1 - ".GetLocaleAsync"
Cohesion: 0.17
Nodes (17): InteractionModuleBase, SocketInteractionContext, SlashCommand, Task, TopLevelModule, CommandExample, CommandSummary, DefaultMemberPermissions (+9 more)

### Community 2 - ".Warn"
Cohesion: 0.22
Nodes (11): PendingRefreshPersistence, CancellationToken, int, NotifierMetrics, object, string, Task, TimeSpan (+3 more)

### Community 3 - "EmojiService"
Cohesion: 0.29
Nodes (4): DiscordStreamNotifyBot.SharedService, Emote, DiscordSocketClient, EmojiService

### Community 4 - "DiscordStreamNotifyBot.Shared.csproj"
Cohesion: 0.08
Nodes (23): Microsoft.EntityFrameworkCore.Design (9.0.3), Microsoft.EntityFrameworkCore.Relational (9.0.3), Microsoft.EntityFrameworkCore.Tools (9.0.3), Serilog (4.4.0), Serilog.Sinks.Console (6.1.1), Serilog.Sinks.File (7.0.0), Serilog.Sinks.Grafana.Loki (9.0.1), net8.0 (+15 more)

### Community 5 - "TwitchApiService"
Cohesion: 0.06
Nodes (42): EventSubSubscription, IReadOnlyList, Stream, TwitchEventSubDeleteResult, TwitchEventSubDeleteStatus, TwitchEventSubEnsureMode, TwitchEventSubEnsureResult, TwitchEventSubSubscriptionsResult (+34 more)

### Community 6 - "InteractionHandler"
Cohesion: 0.10
Nodes (20): DisplayName, ISet, Dictionary, DiscordSocketClient, Func, HashSet, IDictionary, IEnumerable (+12 more)

### Community 7 - "偵測 → 匯流排 → 發送 路徑除錯"
Cohesion: 0.13
Nodes (13): 1. Shared — 定義契約, 2. Scraper — 偵測並 publish, 3. Notifier — 消費並發送, 動工前先讀一個既有平台, 收尾檢查, 新增偵測平台 / 通知事件, 步驟（依相依順序，Shared → Scraper → Notifier）, 偵測 → 匯流排 → 發送 路徑除錯 (+5 more)

### Community 8 - "AuthTokenTests"
Cohesion: 0.15
Nodes (8): TokenCrypto, TokenManager, Fact, InlineData, string, Theory, AuthTokenTests, TokenPayload

### Community 9 - "YoutubeVideoIdParser"
Cohesion: 0.20
Nodes (7): string, Uri, YoutubeVideoIdParser, InlineData, string, Theory, YoutubeVideoIdParserTests

### Community 10 - "YoutubeReminderPolicyTests"
Cohesion: 0.09
Nodes (19): DateTime, TimeSpan, YoutubeReminderApiAction, YoutubeReminderBatchChangeAction, YoutubeReminderBatchFacts, YoutubeReminderPolicy, YoutubeReminderReconciliationAction, YoutubeReminderStartAction (+11 more)

### Community 11 - ".RunCoreAsync"
Cohesion: 0.23
Nodes (9): CancellationToken, Func, Task, TimeProvider, TimeSpan, PeriodicRunner, Fact, Task (+1 more)

### Community 12 - "DiscordStreamNotifyBot.DataBase.Table"
Cohesion: 0.09
Nodes (14): DiscordStreamNotifyBot.SharedService.AdminSettings, DiscordStreamNotifyBot.Tests.Component.MySql, DiscordStreamNotifyBot.Interaction.ServerAdministration, DiscordStreamNotifyBot.SharedService.Youtube, DiscordStreamNotifyBot.SharedService.Twitcasting, DiscordStreamNotifyBot.Interaction.Utility.Service, DiscordStreamNotifyBot.DataBase.Table, DiscordStreamNotifyBot.Localization (+6 more)

### Community 13 - "Extensions"
Cohesion: 0.07
Nodes (21): Process, Assembly, DiscordSocketClient, Func, IEmote, IEnumerable, IMessage, IServiceCollection (+13 more)

### Community 14 - "TwitchSubscriptionService"
Cohesion: 0.15
Nodes (16): CancellationToken, CancellationTokenSource, ConcurrentDictionary, DateTimeOffset, DiscordSocketClient, int, NotifierMetrics, RedisValue (+8 more)

### Community 15 - "Extensions"
Cohesion: 0.16
Nodes (9): DateTime, EmbedBuilder, IEmote, IMessage, IMessageChannel, IUserMessage, Video, YTChannelType (+1 more)

### Community 16 - ".ReconcileUserStateAsync"
Cohesion: 0.17
Nodes (10): DateTime, TwitchSpiderRemovalMetricReason, TwitchUserState, DateTime, TwitchBroadcasterAuthorization, DateTime, TwitchSpider, TwitchEventSubCleanupDeferredMetricReason (+2 more)

### Community 17 - "會限 OAuth Token 儲存改走 MySQL（去 Redis 依賴）計畫"
Cohesion: 0.11
Nodes (18): Backend, Bot（本 repo）, MySQL（兩端都已連同一個庫）, 儲存層（現況為 Redis）, 加密與 blob 格式（兩端一致）, 加密金鑰處理, 影響檔案一覽, 待決策（給實作 session） (+10 more)

### Community 18 - ".AddChannel"
Cohesion: 0.28
Nodes (9): CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, IChannel, RequireBotPermission, SlashCommand, Task (+1 more)

### Community 19 - "AdminSettingsMutationResult"
Cohesion: 0.16
Nodes (10): DiscordSocketClient, SocketGuild, AdminSettingsChannelValidator, CancellationToken, SocketGuild, Task, SocketGuild, CancellationToken (+2 more)

### Community 20 - ".CreateService"
Cohesion: 0.29
Nodes (7): Fact, Func, IReadOnlyCollection, IReadOnlyDictionary, Task, TimeProvider, GuildLocaleServiceTests

### Community 21 - "YoutubeMemberAuthorizationService"
Cohesion: 0.22
Nodes (9): GoogleAuthorizationCodeFlow, CancellationToken, HttpClient, MySqlDataStore, string, Task, YoutubeMemberAuthorizationService, YoutubeMemberTokenSnapshot (+1 more)

### Community 22 - "Log"
Cohesion: 0.10
Nodes (16): ILogEventSink, ITextFormatter, LogEvent, LogEventLevel, Logger, LoggerConfiguration, bool, long (+8 more)

### Community 23 - ".RetryWithBackoffAsync"
Cohesion: 0.23
Nodes (9): Func, Task, TimeProvider, TimeSpan, StartupPreflight, DateTimeOffset, Fact, Task (+1 more)

### Community 24 - "Twitch 訂閱驗證實作計畫"
Cohesion: 0.05
Nodes (36): 10. Frontend 調整, 11. 安全與錯誤處理, 12.1 Backend, 12.2 Bot, 12.3 Frontend, 12. 自動化測試, 13. 手動驗收, 14. 實作順序 (+28 more)

### Community 25 - "YoutubeMemberPolicies"
Cohesion: 0.07
Nodes (18): ComponentInteraction, Task, YoutubeMemberComponent, IEnumerable, IReadOnlyCollection, IReadOnlyList, UserId, YoutubeMemberPolicies (+10 more)

### Community 26 - "新增 TwitCasting 錄影委派計畫（小幫手 ↔ StreamRecordTools）"
Cohesion: 0.11
Nodes (17): 1. 背景與動機, 2. 新增跨 repo 契約, 3. A（小幫手）改動, 4. B（StreamRecordTools）改動, 5. 部署順序與相容性, 6. 驗證, 7. 影響範圍, A1. `Shared/RedisChannels.cs` (+9 more)

### Community 27 - "多語系支援計畫"
Cohesion: 0.09
Nodes (23): 15. 預期修改檔案, 16. 完成定義, 1. 背景, 2. 目標, 3. 非目標, 4. 已確認的產品決策, 6.1 指令註冊資源, 6.2 執行期訊息資源 (+15 more)

### Community 28 - "Serilog Logging 遷移計畫"
Cohesion: 0.10
Nodes (20): 10. 預期修改檔案, 11. 完成定義, 1. 背景, 2. 目標, 3. 非目標, 4. 技術選型, 5.1 Console, 5.2 非容器檔案 (+12 more)

### Community 29 - "12. 分階段執行"
Cohesion: 0.22
Nodes (9): 12. 分階段執行, 階段 0：建立基準與字串清冊, 階段 1：Localization 基礎與繁中資源化, 階段 2：資料庫與語系設定, 階段 3：Slash command 註冊本地化, 階段 4：共用互動、Help 與首次設定, 階段 5：一般 Interaction 模組, 階段 6：背景通知與會限 DM (+1 more)

### Community 30 - "YoutubeDetectionService"
Cohesion: 0.14
Nodes (14): IsDeleted, int, Timer, Video, YTChannelType, ReminderItem, ConcurrentDictionary, DateTime (+6 more)

### Community 31 - "CoordinatorMetrics"
Cohesion: 0.08
Nodes (20): DiscordStreamNotifyBot.Coordinator, Counter, Gauge, HashSet, StreamGroupInfo, string, CoordinatorMetrics, CancellationToken (+12 more)

### Community 32 - "YoutubeMemberLifecycleTaskRegistry"
Cohesion: 0.12
Nodes (13): bool, ConcurrentDictionary, DateTime, IEnumerable, long, object, Task, TimeSpan (+5 more)

### Community 33 - "LocaleResolver"
Cohesion: 0.23
Nodes (5): LocaleResolver, InlineData, Theory, LocaleResolverTests, SupportedLocaleTests

### Community 35 - "NotificationEmbedFactoryTests"
Cohesion: 0.24
Nodes (8): Color, DateTime, Embed, Fact, InlineData, string, Theory, NotificationEmbedFactoryTests

### Community 36 - "TwitchOAuthRefreshLockLease"
Cohesion: 0.15
Nodes (17): CancellationToken, CancellationTokenSource, Exception, IDatabase, int, RedisKey, RedisValue, string (+9 more)

### Community 37 - ".Format"
Cohesion: 0.16
Nodes (11): EmbedBuilder, IDiscordInteraction, IServiceProvider, Task, DateTime, EmbedBuilder, IReadOnlyCollection, TimeSpan (+3 more)

### Community 38 - ".SendCrawlerResultAsync"
Cohesion: 0.17
Nodes (14): CommandExample, CommandSummary, DefaultMemberPermissions, SlashCommand, Task, TwitchService, TwitchSpider, CommandExample (+6 more)

### Community 39 - "YoutubeMemberRoleService"
Cohesion: 0.17
Nodes (12): YoutubeMemberRoleApplyResult, CancellationToken, DiscordSocketClient, IEnumerable, IRole, SocketGuild, Task, YoutubeMemberRoleConfigurationResult (+4 more)

### Community 40 - "TwitchDetectionService"
Cohesion: 0.11
Nodes (15): EventSubSubscription, IReadOnlyCollection, IReadOnlyDictionary, RedisValue, ScraperMetrics, SemaphoreSlim, Task, TimeSpan (+7 more)

### Community 41 - "Twitch"
Cohesion: 0.43
Nodes (9): Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task, TwitchService (+1 more)

### Community 42 - "ScraperMetrics"
Cohesion: 0.18
Nodes (11): Counter, Gauge, string, ScraperMetricResult, ScraperMetrics, TwitchAuthorizationChangeMetricResult, TwitchEventSubCleanupDeferredMetricReason, TwitchEventSubMetricStatus (+3 more)

### Community 43 - "GuildLocaleService"
Cohesion: 0.14
Nodes (16): Locale, CancellationToken, ConcurrentDictionary, Dictionary, Func, IEnumerable, IReadOnlyCollection, IReadOnlyDictionary (+8 more)

### Community 44 - "TwitchChannelUpdateDecisionTests"
Cohesion: 0.19
Nodes (10): IEnumerable, IReadOnlyList, TwitchChannelEventFacts, TwitchChannelStateFacts, TwitchChannelUpdateBatch, TwitchChannelUpdateChange, TwitchChannelUpdatePolicy, DateTime (+2 more)

### Community 45 - "RedisChannels"
Cohesion: 0.13
Nodes (12): int, string, AdminSettings, Cluster, Member, Notifier, OAuth, RedisChannels (+4 more)

### Community 46 - "13. 驗證矩陣"
Cohesion: 0.25
Nodes (8): 13.1 編譯與靜態檢查, 13.2 Slash command 註冊, 13.3 Locale resolver, 13.4 首次設定, 13.5 通知, 13.6 YouTube 會限驗證, 13.7 範圍守衛, 13. 驗證矩陣

### Community 47 - "MainDbContext"
Cohesion: 0.04
Nodes (39): DbContext, IDesignTimeDbContextFactory, CancellationToken, IEnumerable, List, Task, YoutubeMemberService, DbSet (+31 more)

### Community 48 - "網頁管理設定：30 秒請求與背景清理實作計畫"
Cohesion: 0.10
Nodes (19): 10. 實作順序, 11. 不在本次實作, 1. 目標, 2. 已確認決策, 3. 端點範圍與 deadline, 4. Cross-project contract, 5.1 Controller, 5.2 Redis bridge (+11 more)

### Community 49 - "Administration"
Cohesion: 0.43
Nodes (8): Alias, Command, DiscordSocketClient, RequireContext, RequireOwner, Summary, Task, Administration

### Community 50 - "網頁管理設定中心：爬蟲與會員驗證實作計畫"
Cohesion: 0.05
Nodes (40): 10.1 授權, 10.2 爬蟲, 10.3 YouTube 會員驗證, 10.4 Twitch 訂閱驗證, 10. 手動驗收矩陣, 11. 實作順序, 12. 完成閘門, 13. 新 Session 交接指令 (+32 more)

### Community 51 - "水平擴展（三層拆分）計畫 — Redis Streams 版"
Cohesion: 0.05
Nodes (41): 10. 可優化項目（claude 分支已有成品，對應階段順手移植）, 11. 驗證清單（部署前全過）, 1. 目標架構, 2.1 `Shared`（共用 library）, 2.2 `Scraper`（爬蟲層，叢集唯一）, 2.3 `Notifier`（通知層 / shard，可多個）, 2.4 `Coordinator`（主控層，1 個）, 2.5 SharedService 逐服務拆分歸屬（判斷準則表） (+33 more)

### Community 52 - "YoutubeMemberSetting"
Cohesion: 0.18
Nodes (15): DefaultMemberPermissions, IRole, SlashCommand, Task, TwitchSubscriptionSetting, CommandExample, CommandSummary, DefaultMemberPermissions (+7 more)

### Community 53 - ".Get"
Cohesion: 0.19
Nodes (10): DateTimeOffset, Func, List, object, TimeProvider, TimeSpan, NoticeCache, Fact (+2 more)

### Community 54 - "YoutubeVideoClaimCache"
Cohesion: 0.20
Nodes (8): ConcurrentDictionary, TimeProvider, TimeSpan, YoutubeVideoClaimCache, Fact, Task, TimeSpan, YoutubeVideoClaimCacheTests

### Community 55 - ".SetVerificationLogChannelAsync"
Cohesion: 0.35
Nodes (9): DefaultMemberPermissions, DiscordSocketClient, IChannel, ITextChannel, RequireContext, RequireUserPermission, SlashCommand, Task (+1 more)

### Community 56 - "AdministrationService"
Cohesion: 0.16
Nodes (10): DiscordSocketClient, Expected, IReadOnlyCollection, ITextChannel, Responded, SocketGuild, string, Task (+2 more)

### Community 57 - "AGENTS.md"
Cohesion: 0.17
Nodes (11): Build & Run, Conventions, EF Core 鐵則, graphify, 制度條款, 外部契約（不可片面更改）, 指令文件, 架構要點（現行樹） (+3 more)

### Community 58 - "TwitchRefreshRotationLifecycle"
Cohesion: 0.06
Nodes (27): IAsyncDisposable, IDisposable, List, SemaphoreSlim, ValueTask, Lease, LeaseGroup, Action (+19 more)

### Community 59 - "TwitchSubscriptionRolePolicyTests"
Cohesion: 0.15
Nodes (10): AddRoleIds, IReadOnlySet, RemoveRoleIds, Func, IReadOnlyList, TwitchSubscriptionRolePolicy, Fact, InlineData (+2 more)

### Community 60 - "DiscordStreamNotifyBot.Shared"
Cohesion: 0.08
Nodes (10): DiscordStreamNotifyBot.Tests.Component.Redis, DiscordStreamNotifyBot.HttpClients, DiscordStreamNotifyBot.Auth, DiscordStreamNotifyBot.Scraper, DiscordStreamNotifyBot.Shared, DiscordStreamNotifyBot.Interaction.OwnerOnly.Service, DiscordStreamNotifyBot.Command.TwitCasting, DiscordStreamNotifyBot (+2 more)

### Community 61 - "YoutubeStreamService"
Cohesion: 0.07
Nodes (22): NowStreamingHost, CrawlerPolicy, TwitchNotification, DiscordSocketClient, Embed, EmojiService, HttpClient, IEnumerable (+14 more)

### Community 62 - "graphify reference: extra exports and benchmark"
Cohesion: 0.22
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 63 - "Bot"
Cohesion: 0.14
Nodes (12): BotPlayingStatus, ConnectionMultiplexer, DiscordSocketClient, IDatabase, int, ISubscriber, IUser, Task (+4 more)

### Community 64 - "DiscordStreamNotifyBot.SharedService.YoutubeMember"
Cohesion: 0.10
Nodes (9): DiscordStreamNotifyBot.SharedService.YoutubeMember, DiscordStreamNotifyBot.Interaction.YoutubeMember, DiscordStreamNotifyBot.SharedService.Google, YoutubeMemberAuthorizationResult, YoutubeMemberAuthorizationStatus, Exception, YoutubeMemberSafeLogging, Fact (+1 more)

### Community 65 - "TwitchReconcileDecisionTests"
Cohesion: 0.19
Nodes (7): TwitchGuildEligibilityFacts, TwitchReconcileFacts, DateTime, Fact, InlineData, Theory, TwitchReconcileDecisionTests

### Community 66 - ".FilterNoNotifyGuilds"
Cohesion: 0.26
Nodes (7): IEnumerable, DateTime, List, GuildSnapshot, GuildSnapshotEnvelope, Fact, NoNotifyGuildFilterTests

### Community 67 - ".Main"
Cohesion: 0.12
Nodes (12): Assembly, CancellationToken, Exception, int, PeriodicTimer, Task, Program, HashSet (+4 more)

### Community 69 - "AdminSettingsService"
Cohesion: 0.11
Nodes (15): Id, Name, RequestRoute, CancellationToken, DiscordSocketClient, Func, IEnumerable, int (+7 more)

### Community 70 - "NotificationContractTests"
Cohesion: 0.33
Nodes (3): Fact, JObject, NotificationContractTests

### Community 71 - "EF Core 遷移與基線化（本專案版）"
Cohesion: 0.25
Nodes (7): EF Core 遷移與基線化（本專案版）, 一次性基線化（舊的 EnsureCreated 正式庫）, 一般變更流程, 你必須先知道的三件專案特例, 啟動時不碰資料庫（重要）, 套用：本地/開發 vs 正式環境, 收尾

### Community 72 - "NotificationBusConsumer"
Cohesion: 0.20
Nodes (11): CancellationToken, Func, IDatabase, int, StreamEntry, Task, TwitcastingService, TwitchService (+3 more)

### Community 73 - "11. 通知與背景訊息"
Cohesion: 0.29
Nodes (7): 11.1 現況限制, 11.2 目標作法, 11.3 YouTube, 11.4 Twitch, 11.5 TwitCasting, 11.6 YouTube 會限驗證, 11. 通知與背景訊息

### Community 74 - "GoogleOAuthOperationLockLease"
Cohesion: 0.15
Nodes (15): CancellationToken, CancellationTokenSource, IDatabase, int, RedisKey, RedisValue, string, Task (+7 more)

### Community 75 - "TwitchStreamLifecycleDecisionTests"
Cohesion: 0.17
Nodes (8): ConcurrentDictionary, TwitchStreamStartAction, TwitchStreamStartFacts, TwitchStreamStartPolicy, Fact, InlineData, Theory, TwitchStreamLifecycleDecisionTests

### Community 80 - "YoutubeStream"
Cohesion: 0.07
Nodes (34): Alias, ClusterQueryService, Command, CommandExample, DiscordSocketClient, IEnumerable, List, RequireContext (+26 more)

### Community 81 - "TwitchService"
Cohesion: 0.09
Nodes (15): IInteractionService, Clip, DateTime, DiscordSocketClient, EmojiService, EventSubSubscription, IReadOnlyList, NoticeCache (+7 more)

### Community 82 - "AdminSettingsContractTests"
Cohesion: 0.16
Nodes (6): Fact, InlineData, MessageComponent, string, Theory, AdminSettingsContractTests

### Community 83 - "AdminSettings.cs"
Cohesion: 0.09
Nodes (37): Dictionary, int, List, string, AdminProbeVideoPayload, AdminRemoveNotificationPayload, AdminSetChannelPayload, AdminSetLocalePayload (+29 more)

### Community 84 - ".PublishYoutubeNotificationAsync"
Cohesion: 0.17
Nodes (10): GeneratedRegex, YTChannelType, DateTime, DbSet, MainDbContext, Regex, Task, Video (+2 more)

### Community 85 - "graphify reference: query, path, explain"
Cohesion: 0.33
Nodes (5): For /graphify explain, For /graphify path, graphify reference: query, path, explain, Step 0 — Constrained query expansion (REQUIRED before traversal), Step 1 — Traversal

### Community 86 - "自動化測試導入計畫"
Cohesion: 0.17
Nodes (12): 10. 測試實作規則, 1. 目標, 2. 測試分類, 3. 不移除的啟動檢查, 4. 第一批：低耦合契約與格式化, 5. 第二批：小幅抽出純邏輯, 6. 第三批：時間與快取, 7. 第四批：Scraper 狀態機 (+4 more)

### Community 87 - "YoutubeApiService"
Cohesion: 0.20
Nodes (9): CancellationToken, IEnumerable, IHttpClientFactory, List, string, Task, YouTubeService, YTApiVideo (+1 more)

### Community 88 - "DescriptionOnlyLocalizationManager"
Cohesion: 0.29
Nodes (7): ILocalizationManager, ResxLocalizationManager, IDictionary, IList, LocalizationTarget, string, DescriptionOnlyLocalizationManager

### Community 89 - "BotLocalizerTests"
Cohesion: 0.13
Nodes (8): CultureInfo, IReadOnlyList, string, SupportedLocale, Fact, InlineData, Theory, BotLocalizerTests

### Community 90 - "YoutubeMemberService"
Cohesion: 0.11
Nodes (16): CheckId, Snapshot, SocketMessageComponent, CancellationToken, CancellationTokenSource, Func, int, MemberOperationCoordinator (+8 more)

### Community 91 - ".CreateAsyncClient"
Cohesion: 0.19
Nodes (15): HttpMessageHandler, HttpStatusCode, IHttpClientFactory, CancellationToken, Fact, Func, HttpClient, HttpRequestMessage (+7 more)

### Community 92 - ".Get"
Cohesion: 0.12
Nodes (19): IInteractionContext, Dictionary, Regex, ResourceManager, string, BotLocalizer, EmbedBuilder, TwitcastingEmbedBuilderFactory (+11 more)

### Community 93 - ".ToLabel"
Cohesion: 0.22
Nodes (12): NotificationBusMetricResult, NotificationDeliveryResult, TwitchSubscriptionProviderError, TwitchSubscriptionRoleOperation, TwitchSubscriptionRoleResult, TwitchTokenOperation, TwitchTokenOperationResult, YoutubeMemberCheckCycleResult (+4 more)

### Community 94 - ".AssertKeysAbsentAsync"
Cohesion: 0.34
Nodes (7): YTChannelType, YoutubeNotification, IDatabase, RedisComponentFact, StreamEntry, Task, NotificationBusConsumerRedisComponentTests

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
Cohesion: 0.12
Nodes (18): ConcurrentDictionary, Func, SemaphoreSlim, Task, YoutubeNoticeType, ClaimState, YoutubeTerminalEventAction, YoutubeTerminalEventDecision (+10 more)

### Community 101 - "網頁管理設定中心實作計畫"
Cohesion: 0.12
Nodes (16): 10. 首版完成閘門, 11. 驗證, 12. 實作順序, 1. 目標, 2. 已確認產品決策, 3. 系統邊界, 4.1 命令, 4.2 回應 (+8 more)

### Community 102 - "BotConfig"
Cohesion: 0.10
Nodes (12): ServiceProvider, DetectionHost, int, Task, Program, BotConfig, Action, BotRole (+4 more)

### Community 103 - ".Info"
Cohesion: 0.28
Nodes (4): ConsoleColor, LogFileRoute, LogLevel, Exception

### Community 105 - "DiscordWebhookClient"
Cohesion: 0.28
Nodes (6): CancellationToken, DiscordSocketClient, HttpClient, Task, DiscordWebhookClient, Message

### Community 106 - "DiscordStreamNotifyBot.Interaction.Attribute"
Cohesion: 0.11
Nodes (9): DiscordStreamNotifyBot.Interaction.Utility, DiscordStreamNotifyBot.Interaction.Attribute, DiscordStreamNotifyBot.Interaction.OwnerOnly, DiscordStreamNotifyBot.Interaction.TwitCasting, DiscordStreamNotifyBot.Command.Admin, DiscordStreamNotifyBot.Interaction.Help.Service, DiscordStreamNotifyBot.Interaction.Twitch, DiscordStreamNotifyBot.SharedService.Cluster (+1 more)

### Community 113 - ".CreateOrRepairConfigurationAsync"
Cohesion: 0.22
Nodes (12): IReadOnlyCollection, MemberRoleOwnershipSnapshot, CancellationToken, DiscordSocketClient, Exception, ICollection, IRole, NotifierMetrics (+4 more)

### Community 114 - ".LoadSnapshotAsync"
Cohesion: 0.24
Nodes (8): CancellationToken, ICollection, IEnumerable, Task, MemberEntitlementProvider, MemberRoleEntitlement, MemberRoleOwnershipPolicy, MemberRoleOwnershipService

### Community 115 - "DiscordStreamNotifyBot.Notifier.csproj"
Cohesion: 0.10
Nodes (19): Microsoft.Extensions.DependencyInjection.Abstractions (10.0.1), System.Management (10.0.1), net8.0, Ben.Demystifier (0.4.1), Discord.Net (3.20.1), Dorssel.Utilities.Debounce (3.0.0), EFCore.NamingConventions (9.0.0), Google.Apis.YouTube.v3 (1.73.0.3981) (+11 more)

### Community 116 - ".OnlyAffiliateAndPartnerCanBeConfigured"
Cohesion: 0.33
Nodes (3): InlineData, Theory, TwitchSubscriptionConfigurationPolicyTests

### Community 117 - "Normal"
Cohesion: 0.26
Nodes (8): DiscordStreamNotifyBot.Command.Normal, Alias, Command, DiscordSocketClient, DiscordWebhookClient, Summary, Task, Normal

### Community 118 - "ClusterService"
Cohesion: 0.15
Nodes (14): CancellationToken, PeriodicTimer, string, Task, TimeSpan, ScraperService, IDatabase, string (+6 more)

### Community 119 - "TwitcastingService"
Cohesion: 0.18
Nodes (11): Broadcaster, CancellationToken, DiscordSocketClient, EmojiService, NoticeCache, NotifierMetrics, SocketGuild, Task (+3 more)

### Community 120 - "Twitch OAuth 與零成本 EventSub 實作計畫"
Cohesion: 0.14
Nodes (13): 0. 涉及專案, 10. Backend EventSub Webhook, 12. Frontend, 14. Grafana, 18. 建置與遷移, 19. 部署順序, 1. 不可偏離的決策, 20. 官方參考 (+5 more)

### Community 121 - ".FixTCDbAsync"
Cohesion: 0.29
Nodes (6): Alias, Command, RequireContext, RequireOwner, Task, TwitCasting

### Community 122 - "YoutubeMemberApiClient"
Cohesion: 0.26
Nodes (7): GoogleApiException, GoogleCredential, CancellationToken, HashSet, Task, YoutubeMemberApiClient, YoutubeMemberProbeResult

### Community 123 - ".Filter"
Cohesion: 0.19
Nodes (7): IEnumerable, int, IReadOnlyList, AutocompleteCandidate, AutocompleteSearch, Fact, AutocompleteSearchTests

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

### Community 130 - "7. 分階段執行"
Cohesion: 0.25
Nodes (8): 7. 分階段執行, 階段 0：建立基準, 階段 1：加入 Serilog 與 bootstrap logger, 階段 2：搬移 console 與檔案路由, 階段 3：切換 Loki sink, 階段 4：整理 facade 與 Discord.Net adapter, 階段 5：移除自製 sink 與更新文件, 階段 6：後續漸進式 structured logging（不阻擋本計畫完成）

### Community 131 - "SendMsgToAllGuildService"
Cohesion: 0.17
Nodes (12): ButtonCheckData, IInteractionService, SendAllPayload, bool, DiscordSocketClient, Embed, Task, ButtonCheckData (+4 more)

### Community 132 - "7. OAuth API 與流程隔離"
Cohesion: 0.40
Nodes (5): 7.1 API, 7.2 State, 7.3 Callback, 7.4 Twitch scopes, 7. OAuth API 與流程隔離

### Community 133 - "TwitcastingLiveStartPlannerTests"
Cohesion: 0.21
Nodes (7): TwitcastingLiveStartEvent, TwitcastingWebhookParser, Fact, InlineData, string, Theory, TwitcastingLiveStartPlannerTests

### Community 134 - "ClusterQueryService"
Cohesion: 0.12
Nodes (22): ChannelInfo, ClusterQueryType, NotificationChannelIssue, Replies, Responses, Dictionary, DiscordSocketClient, Expected (+14 more)

### Community 135 - ".Plan"
Cohesion: 0.19
Nodes (10): HashSet, IEnumerable, IReadOnlyList, string, TwitcastingWebhookAction, TwitcastingWebhookActionKind, TwitcastingWebhookRegistration, TwitcastingWebhookRegistrationPlanner (+2 more)

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
Cohesion: 0.19
Nodes (8): IEnumerable, IReadOnlyList, ModuleInfo, ResourceManager, SlashCommandInfo, SlashCommandParameterInfo, string, CommandDisplayResolver

### Community 144 - "13. Prometheus"
Cohesion: 0.67
Nodes (3): 13.1 Backend 指標, 13.2 Scraper 指標, 13. Prometheus

### Community 145 - "4. 安全刪除狀態機"
Cohesion: 0.67
Nodes (3): 4.1 直播中授權失效, 4.2 關台後重新判斷, 4. 安全刪除狀態機

### Community 146 - "Help"
Cohesion: 0.25
Nodes (12): AutocompletionResult, HelpService, IAutocompleteInteraction, IInteractionContext, InteractionService, IParameterInfo, IReadOnlyList, IServiceProvider (+4 more)

### Community 147 - "YoutubeMemberVideoLogMessageFormatterTests"
Cohesion: 0.41
Nodes (5): YoutubeMemberVideoLogNotification, Fact, InlineData, Theory, YoutubeMemberVideoLogMessageFormatterTests

### Community 148 - "DiscordStreamNotifyBot.Migrations"
Cohesion: 0.14
Nodes (8): DiscordStreamNotifyBot.Migrations, ModelSnapshot, ModelBuilder, AddMaxSpiderCountSettingField, ModelBuilder, AddManualMemberCheckVideoFlag, ModelBuilder, MainDbContextModelSnapshot

### Community 149 - "DiscordStreamNotifyBot.Command.Attribute"
Cohesion: 0.13
Nodes (10): Attribute, DiscordStreamNotifyBot.Command.Youtube, DiscordStreamNotifyBot.Command.Attribute, DiscordStreamNotifyBot.Command.Twitch, string, CommandExampleAttribute, string, CommandExampleAttribute (+2 more)

### Community 159 - ".Classify"
Cohesion: 0.25
Nodes (6): IEnumerable, YoutubeMemberProbeResultKind, Fact, InlineData, Theory, YoutubeMemberApiClientTests

### Community 161 - "DiscordStreamNotifyBot.Tests"
Cohesion: 0.08
Nodes (13): DiscordStreamNotifyBot.Scraper.Detection.Youtube, DiscordStreamNotifyBot.Scraper.Detection.Twitch.Debounce, DiscordStreamNotifyBot.Tests, DiscordStreamNotifyBot.Scraper.Detection.Twitch, DiscordStreamNotifyBot.SharedService.Twitch, DiscordStreamNotifyBot.SharedService.Youtube.Json, DiscordStreamNotifyBot.Scraper.Detection.Twitcasting, DiscordStreamNotifyBot.Shared.Messages (+5 more)

### Community 164 - "RedisComponentFixture"
Cohesion: 0.16
Nodes (12): ConfigurationOptions, FactAttribute, string, MySqlComponentFactAttribute, ConnectionMultiplexer, IDatabase, RedisKey, string (+4 more)

### Community 166 - ".CheckRequirementsAsync"
Cohesion: 0.17
Nodes (8): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildMemberCountAttribute, string, InteractionErrorCodes

### Community 167 - ".NotifyAddedAsync"
Cohesion: 0.27
Nodes (7): Components, Embed, MessageComponent, SocketGuild, Task, CrawlerOwnerNotifier, CrawlerPlatform

### Community 170 - "DebounceChannelUpdateMessage"
Cohesion: 0.08
Nodes (26): CancellationTokenRegistration, DebouncedEventArgs, Debouncer, FakeTimeProvider, Func, int, IReadOnlyCollection, string (+18 more)

### Community 171 - ".CheckMemberShipCore"
Cohesion: 0.22
Nodes (11): SocketRole, SocketTextChannel, CancellationToken, SocketGuild, Task, YoutubeMemberNotMemberApplyResult, YoutubeMemberService, YoutubeMemberCheckStateSnapshot (+3 more)

### Community 173 - "TwitchGuildEligibilityEvaluator"
Cohesion: 0.20
Nodes (8): ConcurrentDictionary, DateTime, Task, TimeProvider, TimeSpan, TwitchGuildEligibilityEvaluator, TwitchGuildEligibilityStatus, TwitchGuildEligibilityDecision

### Community 175 - ".TryGetKey"
Cohesion: 0.27
Nodes (5): NotificationDedupPolicy, Fact, InlineData, Theory, NotificationDedupPolicyTests

### Community 176 - "GuildTwitchSubscriptionConfig"
Cohesion: 0.11
Nodes (11): IQueryable, DateTimeOffset, IEnumerable, int, IReadOnlyCollection, TwitchRateLimitPolicy, TwitchSubscriptionConfigurationPolicy, TwitchSubscriptionConfigurationQueries (+3 more)

### Community 177 - ".SendStreamMessageAsync"
Cohesion: 0.29
Nodes (5): Event, HttpException, Platform, NotificationMetricEvent, TwitchSpider

### Community 178 - "YoutubeStream"
Cohesion: 0.08
Nodes (28): DiscordStreamNotifyBot.Command.Help, ICommandService, IEqualityComparer, Func, CommonEqualityComparer, Alias, Command, CommandInfo (+20 more)

### Community 179 - "YoutubeApiVideoPolicyTests"
Cohesion: 0.20
Nodes (9): YoutubeApiVideoAction, YoutubeApiVideoDecision, YoutubeApiVideoFacts, YoutubeApiVideoPolicy, DateTime, Fact, InlineData, Theory (+1 more)

### Community 180 - ".CheckMemberShipOnlyVideoIdAsync"
Cohesion: 0.16
Nodes (11): Task, YoutubeDetectionService, YoutubeMemberCandidateAction, YoutubeMemberCandidateFacts, YoutubeMemberChannelDecision, YoutubeMemberChannelFacts, YoutubeMemberVideoPolicy, Fact (+3 more)

### Community 181 - ".ShutdownAsync"
Cohesion: 0.18
Nodes (9): DelegatingHandler, LogMessage, CancellationToken, HttpRequestMessage, HttpResponseMessage, int, Task, TimeSpan (+1 more)

### Community 182 - ".HandleStartLiveMessageAsync"
Cohesion: 0.19
Nodes (11): List, RedisValue, SemaphoreSlim, Task, TwitcastingDetectionService, TwitcastingLiveStartAction, TwitcastingLiveStartFacts, TwitcastingLiveStartPlan (+3 more)

### Community 183 - "ReactionEventWrapper"
Cohesion: 0.29
Nodes (8): bool, Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task, ReactionEventWrapper

### Community 184 - "YoutubeDetectionService"
Cohesion: 0.11
Nodes (16): ConcurrentBag, bool, ConcurrentDictionary, DateTime, HttpClient, IEnumerable, IHttpClientFactory, Task (+8 more)

### Community 185 - "MigrationAndConstraintTests"
Cohesion: 0.36
Nodes (3): MySqlComponentFact, Task, MigrationAndConstraintTests

### Community 186 - "DiscordStreamNotifyBot.Command"
Cohesion: 0.18
Nodes (8): DiscordStreamNotifyBot.Command, SocketMessage, CommandService, DiscordSocketClient, IServiceProvider, Task, CommandHandler, ICommandService

### Community 187 - "NonPersistentGoogleDataStore"
Cohesion: 0.21
Nodes (5): IDataStore, Task, ITokenDataStore, Task, NonPersistentGoogleDataStore

### Community 188 - "TwitchSubscriptionPolicies.cs"
Cohesion: 0.26
Nodes (4): TwitchAuthorizationLocalState, TwitchAuthorizationLocalStatePolicy, TwitchRefreshPersistenceDecision, TwitchRefreshPersistencePolicy

### Community 189 - ".GetCommandHelp"
Cohesion: 0.33
Nodes (6): RequireBotPermissionAttribute, RequireUserPermissionAttribute, EmbedBuilder, IEnumerable, SlashCommandInfo, HelpService

### Community 190 - "MySqlDataStore"
Cohesion: 0.31
Nodes (4): CancellationToken, string, Task, MySqlDataStore

### Community 191 - ".ToMetricEvent"
Cohesion: 0.29
Nodes (6): CollectorRegistry, TwitchNoticeType, InlineData, Task, Theory, NotifierMetricsTests

### Community 192 - "Category"
Cohesion: 0.43
Nodes (5): IEnumerable, List, CategoriesJson, Category, SubCategory

### Community 193 - ".DecideAutomaticMutation"
Cohesion: 0.31
Nodes (4): YoutubeMemberAutomaticMutationAction, YoutubeMemberManualPinPolicy, Fact, YoutubeMemberManualPinPolicyTests

### Community 194 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, ConfiguredBroadcasterAutocompleteHandler

### Community 195 - "Task"
Cohesion: 0.27
Nodes (6): SocketCommandContext, DiscordSocketClient, Func, ICommandContext, SocketReaction, Task

### Community 196 - "NijisanjiStreamJson.cs"
Cohesion: 0.43
Nodes (6): DateTime, List, Channel, EventLiver, Liver, NijisanjiStreamJson

### Community 197 - "UptimeKumaClient"
Cohesion: 0.24
Nodes (7): bool, DiscordSocketClient, HttpClient, string, Task, Timer, UptimeKumaClient

### Community 198 - ".GetDbContext"
Cohesion: 0.16
Nodes (16): DbContextOptions, ComponentInteraction, Task, SpiderManagementComponent, CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient (+8 more)

### Community 199 - ".CheckPermissionsAsync"
Cohesion: 0.22
Nodes (7): PreconditionAttribute, CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task, RequireGuildMemberCountAttribute

### Community 200 - "TcBackendStreamData.cs"
Cohesion: 0.44
Nodes (8): App, BackendMovie, Fmp4, Hls, Llfmp4, Streams, TcBackendStreamData, Webrtc

### Community 202 - ".GroupName"
Cohesion: 0.19
Nodes (13): IDatabase, int, RedisKey, RedisValue, StreamGroupInfo, string, Task, TimeSpan (+5 more)

### Community 203 - "RedisConnection"
Cohesion: 0.28
Nodes (5): ConnectionMultiplexer, Lazy, object, string, RedisConnection

### Community 204 - "InteractionErrorPolicyTests"
Cohesion: 0.33
Nodes (5): Fact, InlineData, InteractionCommandError, Theory, InteractionErrorPolicyTests

### Community 205 - "TopLevelModule"
Cohesion: 0.22
Nodes (10): SendMsgToAllGuildService, SendMsgToAllGuild, ComponentInteraction, GuildTwitchSubscriptionConfig, RequireContext, SlashCommand, Task, TwitchSubscription (+2 more)

### Community 206 - ".SendConfirmMessageAsync"
Cohesion: 0.36
Nodes (6): IDMChannel, DiscordSocketClient, EmbedBuilder, ITextChannel, IUserMessage, Ext

### Community 208 - ".CreateAsync"
Cohesion: 0.09
Nodes (21): GuildPermission, IServiceProvider, IServiceScope, IServiceScopeFactory, Fact, InlineData, SlashCommandParameterInfo, Task (+13 more)

### Community 209 - ".CheckRequirementsAsync"
Cohesion: 0.25
Nodes (6): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildAttribute

### Community 210 - ".CheckRequirementsAsync"
Cohesion: 0.25
Nodes (6): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildOwnerAttribute

### Community 211 - ".CheckPermissionsAsync"
Cohesion: 0.25
Nodes (6): CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task, RequireGuildOwnerAttribute

### Community 212 - "Log 與 Loki"
Cohesion: 0.29
Nodes (7): Console 備援, Grafana Dashboard, Log 與 Loki, Loki 主動推送, Serilog Pipeline, 排障, 檔案路由

### Community 213 - ".Resolve"
Cohesion: 0.57
Nodes (3): InteractionCommandError, InteractionErrorDescriptor, InteractionErrorPolicy

### Community 214 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildNoticeYoutubeChannelIdAutocompleteHandler

### Community 215 - "YouTube 會員驗證架構重構計畫"
Cohesion: 0.15
Nodes (12): 11. 排程與生命週期, 12. Provider Result 分類, 17. Manual Acceptance Matrix, 18. 停機部署順序, 19. Completion Criteria, 1. 範圍, 20. 新 Session 執行規則, 2. 已定案決策 (+4 more)

### Community 216 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildYoutubeChannelSpiderAutocompleteHandler

### Community 217 - "NijisanjiLiverJson.cs"
Cohesion: 0.70
Nodes (4): Head, Images, NijisanjiLiverJson, SocialLinks

### Community 218 - "NotifierMetrics"
Cohesion: 0.16
Nodes (8): Histogram, Counter, Gauge, string, TimeSpan, TwitchSubscriptionStatus, NotifierMetrics, Fact

### Community 219 - "TwitCastingWebHookJson.cs"
Cohesion: 0.83
Nodes (3): Broadcaster, Movie, TwitCastingWebHookJson

### Community 220 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildYoutubeMemberCheckChannelIdAutocompleteHandler

### Community 221 - ".SlashCommandExecuted"
Cohesion: 0.21
Nodes (7): IResult, SocketInteraction, SocketSlashCommandDataOption, IDiscordInteraction, IInteractionContext, SlashCommandInfo, Task

### Community 222 - "TwitchStateDecisions.cs"
Cohesion: 0.11
Nodes (17): DateTime, TimeSpan, TwitchChannelUpdateAction, TwitchChannelUpdateDecision, TwitchGuildEligibilityPolicy, TwitchMissingGuildObservation, TwitchMissingObservationAction, TwitchOfflineAction (+9 more)

### Community 223 - "CommandTextEqualityComparer"
Cohesion: 0.47
Nodes (3): DiscordStreamNotifyBot.Interaction.Help, SlashCommandInfo, CommandTextEqualityComparer

### Community 224 - "5. 語系模型與解析規則"
Cohesion: 0.33
Nodes (6): 5.1 支援值, 5.2 公開內容與背景通知, 5.3 私人即時回覆, 5.4 延遲會限驗證 DM, 5.5 併發安全, 5. 語系模型與解析規則

### Community 225 - ".GenerateSuggestionsAsync"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 226 - "15. 實作階段"
Cohesion: 0.20
Nodes (10): 15. 實作階段, Phase 0：Baseline 與 characterization, Phase 1：Schema 與 migration, Phase 2：共用操作與 role ownership, Phase 3：YouTube interaction 與 state machine, Phase 4：Role/config durability, Phase 5：Provider 與 lifecycle, Phase 6：Backend (+2 more)

### Community 227 - "Migration"
Cohesion: 0.40
Nodes (3): Migration, MigrationBuilder, AddGoogleOAuthUnlinkIntent

### Community 228 - ".GenerateSuggestionsAsync"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 229 - "10. 執行期互動本地化"
Cohesion: 0.40
Nodes (5): 10.1 共用回覆 API, 10.2 Precondition 與 handler 錯誤, 10.3 例外訊息, 10.4 第一階段模組, 10. 執行期互動本地化

### Community 230 - ".LoadCommandFrom"
Cohesion: 0.40
Nodes (4): Assembly, IEnumerable, IServiceCollection, Type

### Community 231 - "DiscordStreamNotifyBot.HttpClients.Twitcasting.Model"
Cohesion: 0.19
Nodes (9): DiscordStreamNotifyBot.HttpClients.Twitcasting.Model, List, GetAllRegistedWebHookJson, Webhook, List, Broadcaster, GetMovieInfoResponse, Movie (+1 more)

### Community 232 - ".AuthorizationEventRequiresCurrentPersistedRevocation"
Cohesion: 0.40
Nodes (3): TwitchAuthorizationEventPolicy, InlineData, Theory

### Community 234 - "opencode.json"
Cohesion: 0.50
Nodes (3): plugin, $schema, .opencode/plugins/graphify.js

### Community 235 - "YouTube 會員驗證"
Cohesion: 0.33
Nodes (5): Durable state, YouTube 會員驗證, 使用者契約, 服務邊界, 部署前驗證

### Community 238 - "14. Frontend"
Cohesion: 0.40
Nodes (5): 14.1 TypeScript contract, 14.2 GoogleSection, 14.3 VerifyWindow, 14.4 Copy/Privacy, 14. Frontend

### Community 239 - "8. DB Schema"
Cohesion: 0.40
Nodes (5): 8.1 Entity changes, 8.2 Indexes, 8.3 Migration 規則, 8.4 Preflight 查詢, 8. DB Schema

### Community 242 - "MySqlComponentFixture"
Cohesion: 0.15
Nodes (14): IAsyncLifetime, ICollectionFixture, string, Task, MySqlComponentCollection, MySqlComponentFixture, MySqlComponentFact, MySqlDataStore (+6 more)

### Community 243 - ".TryGetPayload"
Cohesion: 0.25
Nodes (5): StreamEntry, Fact, InlineData, Theory, RedisContractTests

### Community 244 - "13. Backend Contract"
Cohesion: 0.50
Nodes (4): 13.1 Entity/DTO, 13.2 GET `/account-links`, 13.3 DELETE `/account-links/google`, 13. Backend Contract

### Community 245 - "16. 驗證命令"
Cohesion: 0.50
Nodes (4): 16.1 Bot, 16.2 Backend, 16.3 Frontend, 16. 驗證命令

### Community 246 - ".LockGuildAsync"
Cohesion: 0.19
Nodes (13): LeaseGroup, CancellationToken, DiscordSocketClient, IServiceProvider, SocketGuild, Task, UtilityService, CancellationToken (+5 more)

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

### Community 252 - "AutocompleteHandler"
Cohesion: 0.12
Nodes (15): AutocompleteHandler, GuildNoticeTwitCastingChannelIdAutocompleteHandler, GuildTwitCastingSpiderAutocompleteHandler, AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider (+7 more)

### Community 253 - ".SendLocalizedConfirmAsync"
Cohesion: 0.20
Nodes (10): DiscordSocketClient, DiscordWebhookClient, SlashCommand, Task, Utility, RequireContext, SlashCommand, Task (+2 more)

### Community 257 - "TwitcastingSpider"
Cohesion: 0.33
Nodes (8): CommandExample, CommandSummary, DefaultMemberPermissions, RequireGuildMemberCount, SlashCommand, Task, TwitcastingService, TwitcastingSpider

### Community 262 - "TwitchStream"
Cohesion: 0.24
Nodes (5): HelixStream, TwitchStreamDataFacts, TwitchStreamNotificationFactory, DateTime, TwitchStream

### Community 268 - "TwitchOAuthRefreshLockRedisComponentTests"
Cohesion: 0.36
Nodes (4): IConnectionMultiplexer, RedisComponentFact, Task, TwitchOAuthRefreshLockRedisComponentTests

### Community 275 - "GracefulShutdown"
Cohesion: 0.33
Nodes (4): CancellationToken, CancellationTokenSource, int, GracefulShutdown

### Community 290 - "AdministrationComponent"
Cohesion: 0.50
Nodes (3): ComponentInteraction, Task, AdministrationComponent

### Community 292 - ".SendMessageToAllGuildAsync"
Cohesion: 0.40
Nodes (4): DefaultMemberPermissions, RequireOwner, SlashCommand, Task

### Community 295 - "14. 部署與回滾"
Cohesion: 0.50
Nodes (4): 14.1 建議部署順序, 14.2 相容性, 14.3 回滾, 14. 部署與回滾

### Community 297 - ".IsValidIdentity"
Cohesion: 0.40
Nodes (3): TwitchAccessTokenData, TwitchTokenErrorData, TwitchValidateTokenData

### Community 319 - "TopLevelModule"
Cohesion: 0.43
Nodes (4): ModuleBase, EmbedBuilder, Task, TopLevelModule

## Knowledge Gaps
- **510 isolated node(s):** `$schema`, `.opencode/plugins/graphify.js`, `net8.0`, `prometheus-net.AspNetCore (8.2.1)`, `Microsoft.NET.Sdk` (+505 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **41 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `DiscordStreamNotifyBot.DataBase` connect `DiscordStreamNotifyBot.DataBase.Table` to `EmojiService`, `20250320095452_RefactorDbContext.Designer.cs`, `DiscordStreamNotifyBot.Migrations`, `DiscordStreamNotifyBot.Command.Attribute`, `20250603065853_ModifyTwitCastingTable.Designer.cs`, `20260611015819_SyncModelDrift.Designer.cs`, `20260719142803_AddTwitchBroadcasterAuthorization.Designer.cs`, `20260721095646_AddLocalizationSettings.Designer.cs`, `DiscordStreamNotifyBot.Tests`, `20260803141135_AddTwitchSubscriptionVerification.Designer.cs`, `20260803165758_AddTwitchSubscriptionDeletionPending.Designer.cs`, `20260804173737_AddYoutubeMemberVerificationDurability.Designer.cs`, `20260807045351_AddGoogleOAuthUnlinkIntent.Designer.cs`, `20260813032017_RenameVerificationLogChannel.Designer.cs`, `MainDbContext`, `DiscordStreamNotifyBot.Shared`, `DiscordStreamNotifyBot.SharedService.YoutubeMember`, `.GetDbContext`, `DiscordStreamNotifyBot.Interaction.Attribute`, `.LoadSnapshotAsync`?**
  _High betweenness centrality (0.064) - this node is a cross-community bridge._
- **Why does `DiscordStreamNotifyBot.Shared` connect `DiscordStreamNotifyBot.Shared` to `DiscordStreamNotifyBot.SharedService.YoutubeMember`, `DiscordStreamNotifyBot.Tests`, `BotConfig`, `YoutubeVideoIdParser`, `DiscordStreamNotifyBot.Interaction.Attribute`, `.GroupName`, `DiscordStreamNotifyBot.DataBase.Table`, `.RunCoreAsync`, `RedisChannels`, `GracefulShutdown`, `DiscordStreamNotifyBot.Command.Attribute`, `ClusterService`?**
  _High betweenness centrality (0.060) - this node is a cross-community bridge._
- **Why does `MainDbService` connect `.GetDbContext` to `TwitcastingSpider`, `.GetLocaleAsync`, `SendMsgToAllGuildService`, `.Warn`, `ClusterQueryService`, `TwitchSubscriptionService`, `.AddChannel`, `YoutubeMemberAuthorizationService`, `YoutubeMemberPolicies`, `DiscordStreamNotifyBot.Tests`, `.SendCrawlerResultAsync`, `YoutubeMemberRoleService`, `TwitchDetectionService`, `Twitch`, `GuildLocaleService`, `Administration`, `YoutubeMemberSetting`, `.HandleStartLiveMessageAsync`, `.SetVerificationLogChannelAsync`, `YoutubeDetectionService`, `YoutubeStreamService`, `MySqlDataStore`, `Bot`, `AdminSettingsService`, `TopLevelModule`, `YoutubeStream`, `TwitchService`, `YoutubeApiService`, `YoutubeMemberService`, `.CreateOrRepairConfigurationAsync`, `.LoadSnapshotAsync`, `MySqlComponentFixture`, `.LockGuildAsync`, `TwitcastingService`, `.FixTCDbAsync`, `.SendLocalizedConfirmAsync`?**
  _High betweenness centrality (0.059) - this node is a cross-community bridge._
- **What connects `$schema`, `.opencode/plugins/graphify.js`, `net8.0` to the rest of the system?**
  _510 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `DiscordStreamNotifyBot.Shared.csproj` be split into smaller, more focused modules?**
  _Cohesion score 0.08333333333333333 - nodes in this community are weakly interconnected._
- **Should `TwitchApiService` be split into smaller, more focused modules?**
  _Cohesion score 0.05886708626434654 - nodes in this community are weakly interconnected._
- **Should `InteractionHandler` be split into smaller, more focused modules?**
  _Cohesion score 0.09747899159663866 - nodes in this community are weakly interconnected._