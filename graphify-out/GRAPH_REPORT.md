# Graph Report - DiscordStreamNotifyBot  (2026-08-17)

## Corpus Check
- 325 files · ~169,985 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 4107 nodes · 9575 edges · 243 communities (202 shown, 41 thin omitted)
- Extraction: 91% EXTRACTED · 9% INFERRED · 0% AMBIGUOUS · INFERRED: 828 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `8b3f6bbc`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- .RefreshTokenAsync
- .PublishAsync
- TwitchAuthorizationTokenService
- DiscordStreamNotifyBot.DataBase.Table
- DiscordStreamNotifyBot.Shared.csproj
- TwitchApiService
- InteractionHandler
- 偵測 → 匯流排 → 發送 路徑除錯
- NotifierMetrics
- .CheckPermissionsAsync
- YoutubeReminderPolicyTests
- .Get
- DiscordStreamNotifyBot.Localization
- Extensions
- .GetDbContext
- Extensions
- CommandHandler
- 會限 OAuth Token 儲存改走 MySQL（去 Redis 依賴）計畫
- .SendLocalizedErrorAsync
- SharedExtensions
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
- .RemoveSpiderIfStillInvalidAsync
- AGENTS.md
- MainDbContext
- TwitchOAuthRefreshLockLease
- BotConfig
- .GetGuildNameMapAsync
- YoutubeMemberRoleService
- TwitchDetectionService
- Twitch
- ScraperMetrics
- GuildLocaleService
- DebounceChannelUpdateMessage
- RedisChannels
- 13. 驗證矩陣
- MySqlComponentFixture
- .TryGetKey
- .SetMemberCheckVideoIdAsync
- 網頁管理設定中心：爬蟲與會員驗證實作計畫
- 水平擴展（三層拆分）計畫 — Redis Streams 版
- YoutubeMemberSetting
- TwitchStateDecisions.cs
- Utility
- 7. 分階段執行
- .ToLabel
- AGENTS.md
- TwitchRefreshRotationLifecycle
- .RetryWithBackoffAsync
- DiscordStreamNotifyBot.DataBase
- .Warn
- graphify reference: extra exports and benchmark
- Bot
- DiscordStreamNotifyBot.SharedService.YoutubeMember
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
- LocaleResolver
- FUNDING.yml (Patreon / ECPay / PayPal)
- Build workflow (SonarQube analysis)
- MIT License
- Notifier Bot Logo — interlocking chain-link icon, purple-to-magenta-to-red gradient on light grey circle; flat modern vector branding representing the linking/notification identity of the Discord stream-notify bot
- YoutubeStream
- AdminSettingsMutationResult
- TwitchSubscriptionApiClient
- AdminSettings.cs
- .PublishYoutubeNotificationAsync
- graphify reference: query, path, explain
- 自動化測試導入計畫
- YoutubeDetectionService
- DescriptionOnlyLocalizationManager
- BotLocalizerTests
- .CreateService
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
- .SlashCommandExecuted
- YoutubeMemberApiClient
- .Filter
- 16. 執行階段
- Prometheus / Grafana 監控
- TwitcastingClient
- DiscordStreamNotifyBot.Scraper.csproj
- DiscordStreamNotifyBot.Tests.csproj
- 17. 驗證矩陣
- .Format
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
- CommandDisplayResolver
- 13. Prometheus
- 4. 安全刪除狀態機
- MySqlDataStore
- NotificationEmbedFactoryTests
- DiscordStreamNotifyBot.Migrations
- DiscordStreamNotifyBot.Command
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
- CommandTextEqualityComparer
- 20260719142803_AddTwitchBroadcasterAuthorization.Designer.cs
- .GetCulture
- .LoadSnapshotAsync
- .CheckRequirementsAsync
- .CheckMemberShipCore
- InteractionErrorPolicyTests
- .HandleTwitcastingAsync
- YoutubeMemberLifecycleTaskRegistry
- NonPersistentGoogleDataStore
- 10. 手動驗收矩陣
- 6. Bot 實作
- TwitchGuildEligibilityEvaluator
- NotificationBusConsumer
- GuildTwitchSubscriptionConfig
- TwitchSubscriptionPolicies.cs
- MainDbContextFactory
- YoutubeApiVideoPolicyTests
- .CheckMemberShipOnlyVideoIdAsync
- .DecideAutomaticMutation
- .HandleStartLiveMessageAsync
- YoutubeMemberRoleApplyResult
- NijisanjiLiverJson.cs
- YoutubeVideoIdParser
- 8. Frontend 實作
- YoutubeChannelOwnedType
- .ConvertDateTimeToDiscordMarkdown
- .GetStreamVideoByVideoId
- YTChannelType
- YoutubeMemberVideoLogMessageFormatterTests
- Category
- TwitchSpider
- DiscordStreamNotifyBot.Shared.Messages
- NijisanjiStreamJson.cs
- .TryGetPayload
- TcBackendStreamData.cs
- 20260803141135_AddTwitchSubscriptionVerification.Designer.cs
- MainDbService
- AddTwitchSubscriptionVerification
- AddTwitchSubscriptionDeletionPending
- .CreateAsync
- .HasOtherActiveEntitlement
- .CheckPermissionsAsync
- .CheckRequirementsAsync
- .Main
- .Resolve
- YouTube 會員驗證架構重構計畫
- TwitchOAuthRefreshLockRedisComponentTests
- .SendMessageToAllGuildAsync
- YoutubeMemberRolePoliciesTests
- .GetLocaleAsync
- 20250320095452_RefactorDbContext.Designer.cs
- .LoadCommandFrom
- UtilityService
- 15. 實作階段
- Migration
- EmojiService
- 20250603065853_ModifyTwitCastingTable.Designer.cs
- .SendConfirmMessageAsync
- 20260804173737_AddYoutubeMemberVerificationDurability.Designer.cs
- 20260807045351_AddGoogleOAuthUnlinkIntent.Designer.cs
- YouTube 會員驗證
- 14. Frontend
- 8. DB Schema
- AddYoutubeMemberVerificationDurability
- .ValidateProviderTokenEncryptionKey
- 14. 部署與回滾
- 13. Backend Contract
- 16. 驗證命令
- 20260803165758_AddTwitchSubscriptionDeletionPending.Designer.cs
- .SameUserMutationsAreExclusiveAndOwnerReleaseRemovesTheKey
- 10. Slash 與 Interaction Cutover
- 6. 目標架構
- 7. 狀態機
- 9. Role 隔離政策
- RenameVerificationLogChannel
- .OnlyAffiliateAndPartnerCanBeConfigured
- DiscordStreamNotifyBot.Tests
- .IsBlocked
- 20260721095646_AddLocalizationSettings.Designer.cs
- 20260813032017_RenameVerificationLogChannel.Designer.cs

## God Nodes (most connected - your core abstractions)
1. `DiscordStreamNotifyBot.DataBase.Table` - 70 edges
2. `DiscordStreamNotifyBot.DataBase` - 65 edges
3. `DiscordStreamNotifyBot.Shared` - 64 edges
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

## Communities (243 total, 41 thin omitted)

### Community 0 - ".RefreshTokenAsync"
Cohesion: 0.25
Nodes (7): CancellationToken, Task, TwitchProviderResult, TwitchProviderResultStatus, TwitchAccessTokenData, TwitchTokenErrorData, TwitchValidateTokenData

### Community 1 - ".PublishAsync"
Cohesion: 0.18
Nodes (14): IDatabase, int, RedisKey, RedisValue, StreamEntry, StreamGroupInfo, string, Task (+6 more)

### Community 2 - "TwitchAuthorizationTokenService"
Cohesion: 0.18
Nodes (11): PendingRefreshPersistence, CancellationToken, int, NotifierMetrics, object, string, Task, TimeSpan (+3 more)

### Community 3 - "DiscordStreamNotifyBot.DataBase.Table"
Cohesion: 0.13
Nodes (7): DiscordStreamNotifyBot.Tests.Component.MySql, DiscordStreamNotifyBot.DataBase.Table, DiscordStreamNotifyBot.SharedService.Twitch, DiscordStreamNotifyBot.Interaction.TwitchSubscription, DiscordStreamNotifyBot.SharedService.TwitchSubscription, Fact, YoutubeMembershipSchemaContractTests

### Community 4 - "DiscordStreamNotifyBot.Shared.csproj"
Cohesion: 0.08
Nodes (23): Microsoft.EntityFrameworkCore.Design (9.0.3), Microsoft.EntityFrameworkCore.Relational (9.0.3), Microsoft.EntityFrameworkCore.Tools (9.0.3), Serilog (4.4.0), Serilog.Sinks.Console (6.1.1), Serilog.Sinks.File (7.0.0), Serilog.Sinks.Grafana.Loki (9.0.1), net8.0 (+15 more)

### Community 5 - "TwitchApiService"
Cohesion: 0.09
Nodes (30): EventSubSubscription, IReadOnlyList, Stream, TwitchEventSubDeleteResult, TwitchEventSubDeleteStatus, TwitchEventSubEnsureMode, TwitchEventSubEnsureResult, TwitchEventSubSubscriptionsResult (+22 more)

### Community 6 - "InteractionHandler"
Cohesion: 0.10
Nodes (21): DisplayName, ISet, Dictionary, DiscordSocketClient, Func, HashSet, IDictionary, IEnumerable (+13 more)

### Community 7 - "偵測 → 匯流排 → 發送 路徑除錯"
Cohesion: 0.13
Nodes (13): 1. Shared — 定義契約, 2. Scraper — 偵測並 publish, 3. Notifier — 消費並發送, 動工前先讀一個既有平台, 收尾檢查, 新增偵測平台 / 通知事件, 步驟（依相依順序，Shared → Scraper → Notifier）, 偵測 → 匯流排 → 發送 路徑除錯 (+5 more)

### Community 8 - "NotifierMetrics"
Cohesion: 0.12
Nodes (13): CollectorRegistry, Event, Histogram, Platform, Counter, Gauge, string, TimeSpan (+5 more)

### Community 9 - ".CheckPermissionsAsync"
Cohesion: 0.22
Nodes (7): PreconditionAttribute, CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task, RequireGuildMemberCountAttribute

### Community 10 - "YoutubeReminderPolicyTests"
Cohesion: 0.09
Nodes (19): DateTime, TimeSpan, YoutubeReminderApiAction, YoutubeReminderBatchChangeAction, YoutubeReminderBatchFacts, YoutubeReminderPolicy, YoutubeReminderReconciliationAction, YoutubeReminderStartAction (+11 more)

### Community 11 - ".Get"
Cohesion: 0.14
Nodes (16): MessageComponent, IInteractionContext, Dictionary, Regex, ResourceManager, string, BotLocalizer, DateTime (+8 more)

### Community 12 - "DiscordStreamNotifyBot.Localization"
Cohesion: 0.11
Nodes (7): DiscordStreamNotifyBot.SharedService.Youtube, DiscordStreamNotifyBot.SharedService.Twitcasting, DiscordStreamNotifyBot.Interaction.Utility.Service, DiscordStreamNotifyBot.Localization, DiscordStreamNotifyBot.Interaction, DiscordStreamNotifyBot.SharedService.Google, TwitcastingNotificationVariant

### Community 13 - "Extensions"
Cohesion: 0.07
Nodes (21): Process, Assembly, DiscordSocketClient, Func, IEmote, IEnumerable, IMessage, IServiceCollection (+13 more)

### Community 14 - ".GetDbContext"
Cohesion: 0.14
Nodes (20): LeaseGroup, CancellationToken, ConcurrentDictionary, IEnumerable, Lease, Task, MemberOperationCoordinator, CancellationToken (+12 more)

### Community 15 - "Extensions"
Cohesion: 0.17
Nodes (12): SocketCommandContext, DiscordSocketClient, EmbedBuilder, Func, ICommandContext, IEmote, IMessage, IMessageChannel (+4 more)

### Community 16 - "CommandHandler"
Cohesion: 0.28
Nodes (6): SocketMessage, CommandService, DiscordSocketClient, IServiceProvider, Task, CommandHandler

### Community 17 - "會限 OAuth Token 儲存改走 MySQL（去 Redis 依賴）計畫"
Cohesion: 0.11
Nodes (18): Backend, Bot（本 repo）, MySQL（兩端都已連同一個庫）, 儲存層（現況為 Redis）, 加密與 blob 格式（兩端一致）, 加密金鑰處理, 影響檔案一覽, 待決策（給實作 session） (+10 more)

### Community 18 - ".SendLocalizedErrorAsync"
Cohesion: 0.28
Nodes (12): CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, IChannel, NoticeType, RequireBotPermission, RequireContext (+4 more)

### Community 19 - "SharedExtensions"
Cohesion: 0.12
Nodes (10): DateTime, EmbedBuilder, Video, YTChannelType, SharedExtensions, DateTime, MySqlComponentFact, Task (+2 more)

### Community 20 - "AuthTokenTests"
Cohesion: 0.11
Nodes (11): DiscordStreamNotifyBot.Auth, TokenCrypto, TokenManager, Fact, InlineData, string, Theory, AuthTokenTests (+3 more)

### Community 21 - "YoutubeMemberAuthorizationService"
Cohesion: 0.18
Nodes (11): GoogleAuthorizationCodeFlow, CancellationToken, HttpClient, MySqlDataStore, string, Task, YoutubeMemberAuthorizationResult, YoutubeMemberAuthorizationService (+3 more)

### Community 22 - "Log"
Cohesion: 0.11
Nodes (17): ILogEventSink, ITextFormatter, LogEvent, LogEventLevel, LogFileRoute, Logger, LoggerConfiguration, bool (+9 more)

### Community 23 - ".RunCoreAsync"
Cohesion: 0.23
Nodes (9): CancellationToken, Func, Task, TimeProvider, TimeSpan, PeriodicRunner, Fact, Task (+1 more)

### Community 24 - "Twitch 訂閱驗證實作計畫"
Cohesion: 0.05
Nodes (36): 10. Frontend 調整, 11. 安全與錯誤處理, 12.1 Backend, 12.2 Bot, 12.3 Frontend, 12. 自動化測試, 13. 手動驗收, 14. 實作順序 (+28 more)

### Community 25 - "YoutubeMemberPolicies"
Cohesion: 0.09
Nodes (18): ComponentInteraction, Task, IEnumerable, IReadOnlyCollection, IReadOnlyList, UserId, YoutubeMemberCheckStateSnapshot, YoutubeMemberPolicies (+10 more)

### Community 26 - "新增 TwitCasting 錄影委派計畫（小幫手 ↔ StreamRecordTools）"
Cohesion: 0.11
Nodes (17): 1. 背景與動機, 2. 新增跨 repo 契約, 3. A（小幫手）改動, 4. B（StreamRecordTools）改動, 5. 部署順序與相容性, 6. 驗證, 7. 影響範圍, A1. `Shared/RedisChannels.cs` (+9 more)

### Community 27 - "多語系支援計畫"
Cohesion: 0.07
Nodes (29): 10.1 共用回覆 API, 10.2 Precondition 與 handler 錯誤, 10.3 例外訊息, 10.4 第一階段模組, 10. 執行期互動本地化, 15. 預期修改檔案, 16. 完成定義, 1. 背景 (+21 more)

### Community 28 - "Serilog Logging 遷移計畫"
Cohesion: 0.10
Nodes (20): 10. 預期修改檔案, 11. 完成定義, 1. 背景, 2. 目標, 3. 非目標, 4. 技術選型, 5.1 Console, 5.2 非容器檔案 (+12 more)

### Community 29 - "12. 分階段執行"
Cohesion: 0.22
Nodes (9): 12. 分階段執行, 階段 0：建立基準與字串清冊, 階段 1：Localization 基礎與繁中資源化, 階段 2：資料庫與語系設定, 階段 3：Slash command 註冊本地化, 階段 4：共用互動、Help 與首次設定, 階段 5：一般 Interaction 模組, 階段 6：背景通知與會限 DM (+1 more)

### Community 30 - "YoutubeDetectionService"
Cohesion: 0.13
Nodes (14): IsDeleted, int, Timer, Video, YTChannelType, ReminderItem, ConcurrentDictionary, DateTime (+6 more)

### Community 31 - "CoordinatorMetrics"
Cohesion: 0.08
Nodes (19): DiscordStreamNotifyBot.Coordinator, Counter, Gauge, HashSet, StreamGroupInfo, string, CoordinatorMetrics, CancellationToken (+11 more)

### Community 32 - "YoutubeMemberService"
Cohesion: 0.11
Nodes (16): CheckId, Snapshot, SocketMessageComponent, CancellationToken, CancellationTokenSource, Func, int, MemberOperationCoordinator (+8 more)

### Community 33 - ".RemoveSpiderIfStillInvalidAsync"
Cohesion: 0.16
Nodes (10): DateTime, TwitchSpiderRemovalMetricReason, TwitchUserState, DateTime, TwitchReconcilePolicy, DateTime, TwitchBroadcasterAuthorization, DateTime (+2 more)

### Community 34 - "AGENTS.md"
Cohesion: 0.12
Nodes (9): Console 備援, Grafana Dashboard, Log 與 Loki, Loki 主動推送, Serilog Pipeline, 排障, 檔案路由, License (+1 more)

### Community 35 - "MainDbContext"
Cohesion: 0.05
Nodes (32): DbContext, CancellationToken, IEnumerable, List, Task, YoutubeMemberService, DbSet, ModelBuilder (+24 more)

### Community 36 - "TwitchOAuthRefreshLockLease"
Cohesion: 0.15
Nodes (17): CancellationToken, CancellationTokenSource, Exception, IDatabase, int, RedisKey, RedisValue, string (+9 more)

### Community 37 - "BotConfig"
Cohesion: 0.16
Nodes (10): ServiceProvider, DetectionHost, CancellationToken, PeriodicTimer, string, Task, TimeSpan, ScraperService (+2 more)

### Community 38 - ".GetGuildNameMapAsync"
Cohesion: 0.16
Nodes (14): CommandExample, CommandSummary, RequireGuildMemberCount, SlashCommand, Task, TwitcastingService, GuildTwitCastingSpiderAutocompleteHandler, TwitcastingSpider (+6 more)

### Community 39 - "YoutubeMemberRoleService"
Cohesion: 0.22
Nodes (9): CancellationToken, DiscordSocketClient, IEnumerable, IRole, SocketGuild, Task, YoutubeMemberRoleConfigurationResult, YoutubeMemberRoleService (+1 more)

### Community 40 - "TwitchDetectionService"
Cohesion: 0.13
Nodes (15): HelixStream, ConcurrentDictionary, IReadOnlyDictionary, RedisValue, ScraperMetrics, SemaphoreSlim, Task, TimeSpan (+7 more)

### Community 41 - "Twitch"
Cohesion: 0.34
Nodes (10): Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task, TwitchService (+2 more)

### Community 42 - "ScraperMetrics"
Cohesion: 0.12
Nodes (14): EventSubSubscription, Counter, Gauge, string, ScraperMetricResult, ScraperMetrics, TwitchAuthorizationChangeMetricResult, TwitchEventSubCleanupDeferredMetricReason (+6 more)

### Community 43 - "GuildLocaleService"
Cohesion: 0.13
Nodes (16): Locale, CancellationToken, ConcurrentDictionary, Dictionary, Func, IEnumerable, IReadOnlyCollection, IReadOnlyDictionary (+8 more)

### Community 44 - "DebounceChannelUpdateMessage"
Cohesion: 0.07
Nodes (32): CancellationTokenRegistration, DebouncedEventArgs, Debouncer, FakeTimeProvider, Func, int, IReadOnlyCollection, string (+24 more)

### Community 45 - "RedisChannels"
Cohesion: 0.16
Nodes (12): int, string, AdminSettings, Cluster, Member, Notifier, OAuth, RedisChannels (+4 more)

### Community 46 - "13. 驗證矩陣"
Cohesion: 0.25
Nodes (8): 13.1 編譯與靜態檢查, 13.2 Slash command 註冊, 13.3 Locale resolver, 13.4 首次設定, 13.5 通知, 13.6 YouTube 會限驗證, 13.7 範圍守衛, 13. 驗證矩陣

### Community 47 - "MySqlComponentFixture"
Cohesion: 0.10
Nodes (16): IAsyncLifetime, MySqlComponentFact, Task, MigrationAndConstraintTests, string, Task, MySqlComponentCollection, MySqlComponentFixture (+8 more)

### Community 48 - ".TryGetKey"
Cohesion: 0.27
Nodes (5): NotificationDedupPolicy, Fact, InlineData, Theory, NotificationDedupPolicyTests

### Community 49 - ".SetMemberCheckVideoIdAsync"
Cohesion: 0.10
Nodes (30): DiscordStreamNotifyBot.Command.Help, ICommandService, Alias, Command, CommandService, IServiceProvider, string, Summary (+22 more)

### Community 50 - "網頁管理設定中心：爬蟲與會員驗證實作計畫"
Cohesion: 0.08
Nodes (26): 11. 實作順序, 12. 完成閘門, 13. 新 Session 交接指令, 1. 目標, 2.1 爬蟲, 2.2 YouTube 會員驗證, 2.3 Twitch 訂閱驗證, 2. 完成範圍 (+18 more)

### Community 51 - "水平擴展（三層拆分）計畫 — Redis Streams 版"
Cohesion: 0.05
Nodes (41): 10. 可優化項目（claude 分支已有成品，對應階段順手移植）, 11. 驗證清單（部署前全過）, 1. 目標架構, 2.1 `Shared`（共用 library）, 2.2 `Scraper`（爬蟲層，叢集唯一）, 2.3 `Notifier`（通知層 / shard，可多個）, 2.4 `Coordinator`（主控層，1 個）, 2.5 SharedService 逐服務拆分歸屬（判斷準則表） (+33 more)

### Community 52 - "YoutubeMemberSetting"
Cohesion: 0.15
Nodes (15): AutocompletionResult, CommandExample, CommandSummary, DiscordSocketClient, GuildYoutubeMemberConfig, IAutocompleteInteraction, IInteractionContext, IParameterInfo (+7 more)

### Community 53 - "TwitchStateDecisions.cs"
Cohesion: 0.09
Nodes (23): TimeSpan, TwitchChannelUpdateAction, TwitchChannelUpdateDecision, TwitchGuildEligibilityPolicy, TwitchMissingGuildObservation, TwitchMissingObservationAction, TwitchOfflineAction, TwitchOfflineFacts (+15 more)

### Community 54 - "Utility"
Cohesion: 0.27
Nodes (10): DefaultMemberPermissions, DiscordSocketClient, DiscordWebhookClient, IChannel, ITextChannel, RequireContext, RequireUserPermission, SlashCommand (+2 more)

### Community 55 - "7. 分階段執行"
Cohesion: 0.25
Nodes (8): 7. 分階段執行, 階段 0：建立基準, 階段 1：加入 Serilog 與 bootstrap logger, 階段 2：搬移 console 與檔案路由, 階段 3：切換 Loki sink, 階段 4：整理 facade 與 Discord.Net adapter, 階段 5：移除自製 sink 與更新文件, 階段 6：後續漸進式 structured logging（不阻擋本計畫完成）

### Community 56 - ".ToLabel"
Cohesion: 0.22
Nodes (12): NotificationBusMetricResult, NotificationDeliveryResult, TwitchSubscriptionProviderError, TwitchSubscriptionRoleOperation, TwitchSubscriptionRoleResult, TwitchTokenOperation, TwitchTokenOperationResult, YoutubeMemberCheckCycleResult (+4 more)

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
Cohesion: 0.13
Nodes (9): DiscordStreamNotifyBot.Interaction.Utility, DiscordStreamNotifyBot.Interaction.Attribute, DiscordStreamNotifyBot.Interaction.TwitCasting, DiscordStreamNotifyBot.Command.Admin, DiscordStreamNotifyBot.Interaction.Help.Service, DiscordStreamNotifyBot.Interaction.Twitch, DiscordStreamNotifyBot.SharedService.Cluster, DiscordStreamNotifyBot.Interaction.Youtube (+1 more)

### Community 61 - ".Warn"
Cohesion: 0.07
Nodes (23): HttpException, NowStreamingHost, TwitcastingNotification, DiscordSocketClient, Embed, EmojiService, HttpClient, IEnumerable (+15 more)

### Community 62 - "graphify reference: extra exports and benchmark"
Cohesion: 0.22
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 63 - "Bot"
Cohesion: 0.14
Nodes (12): BotPlayingStatus, ConnectionMultiplexer, DiscordSocketClient, IDatabase, int, ISubscriber, IUser, Task (+4 more)

### Community 64 - "DiscordStreamNotifyBot.SharedService.YoutubeMember"
Cohesion: 0.10
Nodes (9): DiscordStreamNotifyBot.SharedService.YoutubeMember, DiscordStreamNotifyBot.Interaction.YoutubeMember, DiscordStreamNotifyBot.Command.YoutubeMember, DiscordStreamNotifyBot.SharedService.Member, Exception, YoutubeMemberSafeLogging, YoutubeMemberVideoLogMessageFormatter, Fact (+1 more)

### Community 65 - "TwitchReconcileDecisionTests"
Cohesion: 0.16
Nodes (10): TwitchGuildEligibilityFacts, TwitchReconcileFacts, TwitchSpiderRemovalAction, TwitchSpiderRemovalFacts, TwitchSpiderRemovalPolicy, DateTime, Fact, InlineData (+2 more)

### Community 66 - "RedisComponentFixture"
Cohesion: 0.16
Nodes (13): ConfigurationOptions, FactAttribute, ICollectionFixture, string, MySqlComponentFactAttribute, ConnectionMultiplexer, IDatabase, RedisKey (+5 more)

### Community 67 - ".Main"
Cohesion: 0.12
Nodes (12): Assembly, CancellationToken, Exception, int, PeriodicTimer, Task, Program, HashSet (+4 more)

### Community 69 - "AdminSettingsService"
Cohesion: 0.07
Nodes (21): Id, Name, RequestRoute, CancellationToken, DiscordSocketClient, Func, IEnumerable, int (+13 more)

### Community 70 - "NotificationContractTests"
Cohesion: 0.33
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

### Community 75 - "LocaleResolver"
Cohesion: 0.22
Nodes (5): LocaleResolver, InlineData, Theory, LocaleResolverTests, SupportedLocaleTests

### Community 80 - "YoutubeStream"
Cohesion: 0.24
Nodes (15): Alias, ClusterQueryService, Command, CommandExample, DiscordSocketClient, IEnumerable, List, RequireContext (+7 more)

### Community 81 - "AdminSettingsMutationResult"
Cohesion: 0.10
Nodes (21): IInteractionService, DiscordSocketClient, SocketGuild, AdminSettingsChannelValidator, Broadcaster, CancellationToken, DiscordSocketClient, EmojiService (+13 more)

### Community 82 - "TwitchSubscriptionApiClient"
Cohesion: 0.17
Nodes (12): DateTimeOffset, HttpResponseMessage, IHttpClientFactory, NotifierMetrics, string, TwitchSubscriptionApiClient, TwitchSubscriptionData, TwitchSubscriptionResponse (+4 more)

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

### Community 87 - "YoutubeDetectionService"
Cohesion: 0.10
Nodes (19): ConcurrentBag, bool, ConcurrentDictionary, HttpClient, IEnumerable, IHttpClientFactory, Task, YoutubeApiService (+11 more)

### Community 88 - "DescriptionOnlyLocalizationManager"
Cohesion: 0.29
Nodes (7): ILocalizationManager, ResxLocalizationManager, IDictionary, IList, LocalizationTarget, string, DescriptionOnlyLocalizationManager

### Community 89 - "BotLocalizerTests"
Cohesion: 0.18
Nodes (4): Fact, InlineData, Theory, BotLocalizerTests

### Community 90 - ".CreateService"
Cohesion: 0.29
Nodes (7): Fact, Func, IReadOnlyCollection, IReadOnlyDictionary, Task, TimeProvider, GuildLocaleServiceTests

### Community 91 - "TwitchSubscriptionApiClientTests"
Cohesion: 0.19
Nodes (15): HttpMessageHandler, HttpStatusCode, IHttpClientFactory, CancellationToken, Fact, Func, HttpClient, HttpRequestMessage (+7 more)

### Community 92 - ".ShutdownAsync"
Cohesion: 0.18
Nodes (9): DelegatingHandler, LogMessage, CancellationToken, HttpRequestMessage, HttpResponseMessage, int, Task, TimeSpan (+1 more)

### Community 93 - "6. 資源架構"
Cohesion: 0.40
Nodes (5): 6.1 指令註冊資源, 6.2 執行期訊息資源, 6.3 Help 長文, 6.4 Localizer API, 6. 資源架構

### Community 94 - ".AssertKeysAbsentAsync"
Cohesion: 0.42
Nodes (5): IDatabase, RedisComponentFact, StreamEntry, Task, NotificationBusConsumerRedisComponentTests

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
Cohesion: 0.20
Nodes (11): HashSet, IReadOnlyCollection, IReadOnlyList, TwitchEventSubCreateSpec, TwitchEventSubFact, TwitchEventSubFinalDecision, TwitchEventSubReconcilePlan, TwitchEventSubReconcilePolicy (+3 more)

### Community 103 - ".New"
Cohesion: 0.33
Nodes (3): ConsoleColor, LogLevel, Exception

### Community 105 - "DiscordWebhookClient"
Cohesion: 0.31
Nodes (6): CancellationToken, DiscordSocketClient, HttpClient, Task, DiscordWebhookClient, Message

### Community 106 - "DiscordStreamNotifyBot.Command.Attribute"
Cohesion: 0.13
Nodes (10): Attribute, DiscordStreamNotifyBot.Command.Youtube, DiscordStreamNotifyBot.Command.Attribute, DiscordStreamNotifyBot.Command.Twitch, string, CommandExampleAttribute, string, CommandExampleAttribute (+2 more)

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
Cohesion: 0.11
Nodes (14): Clip, DateTime, DiscordSocketClient, EmojiService, EventSubSubscription, IReadOnlyList, NoticeCache, NotifierMetrics (+6 more)

### Community 117 - "ReactionEventWrapper"
Cohesion: 0.29
Nodes (8): bool, Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task, ReactionEventWrapper

### Community 118 - "ClusterService"
Cohesion: 0.21
Nodes (8): IDatabase, string, Task, TimeSpan, ClusterService, RedisComponentFact, Task, ClusterServiceRedisComponentTests

### Community 119 - "DiscordStreamNotifyBot.Scraper.Detection.Twitcasting"
Cohesion: 0.38
Nodes (4): DiscordStreamNotifyBot.Scraper.Detection.Twitcasting, Broadcaster, Movie, TwitCastingWebHookJson

### Community 120 - "Twitch OAuth 與零成本 EventSub 實作計畫"
Cohesion: 0.14
Nodes (13): 0. 涉及專案, 10. Backend EventSub Webhook, 12. Frontend, 14. Grafana, 18. 建置與遷移, 19. 部署順序, 1. 不可偏離的決策, 20. 官方參考 (+5 more)

### Community 121 - ".SlashCommandExecuted"
Cohesion: 0.24
Nodes (6): IResult, SocketInteraction, SocketSlashCommandDataOption, IDiscordInteraction, IInteractionContext, Task

### Community 122 - "YoutubeMemberApiClient"
Cohesion: 0.26
Nodes (7): GoogleApiException, GoogleCredential, CancellationToken, HashSet, Task, YoutubeMemberApiClient, YoutubeMemberProbeResult

### Community 123 - ".Filter"
Cohesion: 0.05
Nodes (37): IEnumerable, int, IReadOnlyList, AutocompleteCandidate, AutocompleteSearch, AutocompletionResult, IAutocompleteInteraction, IInteractionContext (+29 more)

### Community 124 - "16. 執行階段"
Cohesion: 0.22
Nodes (9): 16. 執行階段, 階段 0：前置確認, 階段 1：資料模型與 Backend 設定, 階段 2：Google/Twitch OAuth 隔離, 階段 3：Frontend, 階段 4：Twitch add資格與授權清理, 階段 5：StreamOnline 與 EventSub reconcile, 階段 6：Prometheus 與 Grafana (+1 more)

### Community 125 - "Prometheus / Grafana 監控"
Cohesion: 0.20
Nodes (9): Backend 指標, Coordinator 指標, Endpoints, Grafana, Notifier 指標, Prometheus, Prometheus / Grafana 監控, Scraper 指標 (+1 more)

### Community 126 - "TwitcastingClient"
Cohesion: 0.21
Nodes (8): List, GetAllRegistedWebHookJson, Webhook, HttpClient, List, string, Task, TwitcastingClient

### Community 127 - "DiscordStreamNotifyBot.Scraper.csproj"
Cohesion: 0.50
Nodes (3): net8.0, prometheus-net.AspNetCore (8.2.1), Microsoft.NET.Sdk

### Community 128 - "DiscordStreamNotifyBot.Tests.csproj"
Cohesion: 0.25
Nodes (7): coverlet.collector (6.0.0), Microsoft.Extensions.TimeProvider.Testing (9.0.0), Microsoft.NET.Test.Sdk (17.8.0), xunit (2.5.3), xunit.runner.visualstudio (2.5.3), net8.0, Microsoft.NET.Sdk

### Community 129 - "17. 驗證矩陣"
Cohesion: 0.33
Nodes (6): 17.1 新增 spider, 17.2 EventSub, 17.3 授權失效, 17.4 OAuth, 17.5 Prometheus/Grafana, 17. 驗證矩陣

### Community 130 - ".Format"
Cohesion: 0.17
Nodes (10): EmbedBuilder, IDiscordInteraction, IServiceProvider, Task, DateTime, EmbedBuilder, IReadOnlyCollection, TimeSpan (+2 more)

### Community 131 - "Notifications.cs"
Cohesion: 0.18
Nodes (14): DateTime, List, string, YTChannelType, BannerChangeNotification, NotifyType, TwitcastingNotification, TwitchNoticeType (+6 more)

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
Cohesion: 0.14
Nodes (6): DiscordStreamNotifyBot.Tests.Component.Redis, DiscordStreamNotifyBot.HttpClients, DiscordStreamNotifyBot.Scraper, DiscordStreamNotifyBot.Shared, DiscordStreamNotifyBot.Command.TwitCasting, DiscordStreamNotifyBot

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
Cohesion: 0.08
Nodes (30): DiscordStreamNotifyBot.Interaction.Help, RequireBotPermissionAttribute, RequireUserPermissionAttribute, AutocompletionResult, HelpService, IAutocompleteInteraction, IInteractionContext, InteractionService (+22 more)

### Community 144 - "13. Prometheus"
Cohesion: 0.67
Nodes (3): 13.1 Backend 指標, 13.2 Scraper 指標, 13. Prometheus

### Community 145 - "4. 安全刪除狀態機"
Cohesion: 0.67
Nodes (3): 4.1 直播中授權失效, 4.2 關台後重新判斷, 4. 安全刪除狀態機

### Community 146 - "MySqlDataStore"
Cohesion: 0.31
Nodes (4): CancellationToken, string, Task, MySqlDataStore

### Community 147 - "NotificationEmbedFactoryTests"
Cohesion: 0.18
Nodes (12): Color, EmbedBuilder, TwitcastingEmbedBuilderFactory, DateTime, TwitcastingStream, DateTime, Embed, Fact (+4 more)

### Community 148 - "DiscordStreamNotifyBot.Migrations"
Cohesion: 0.14
Nodes (8): DiscordStreamNotifyBot.Migrations, ModelSnapshot, ModelBuilder, AddMaxSpiderCountSettingField, ModelBuilder, AddManualMemberCheckVideoFlag, ModelBuilder, MainDbContextModelSnapshot

### Community 149 - "DiscordStreamNotifyBot.Command"
Cohesion: 0.20
Nodes (4): DiscordStreamNotifyBot.Command, Func, CommonEqualityComparer, ICommandService

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
Cohesion: 0.25
Nodes (6): IEnumerable, YoutubeMemberProbeResultKind, Fact, InlineData, Theory, YoutubeMemberApiClientTests

### Community 160 - "DiscordStreamNotifyBot.HttpClients.Twitcasting.Model"
Cohesion: 0.14
Nodes (11): DiscordStreamNotifyBot.HttpClients.Twitcasting.Model, Alias, Command, RequireContext, RequireOwner, Task, List, Broadcaster (+3 more)

### Community 162 - "CommandTextEqualityComparer"
Cohesion: 0.24
Nodes (5): IEqualityComparer, CommandInfo, CommandTextEqualityComparer, Func, CommonEqualityComparer

### Community 164 - ".GetCulture"
Cohesion: 0.38
Nodes (4): CultureInfo, IReadOnlyList, string, SupportedLocale

### Community 165 - ".LoadSnapshotAsync"
Cohesion: 0.32
Nodes (6): CancellationToken, ICollection, Task, MemberEntitlementProvider, MemberRoleEntitlement, MemberRoleOwnershipService

### Community 166 - ".CheckRequirementsAsync"
Cohesion: 0.17
Nodes (8): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildMemberCountAttribute, string, InteractionErrorCodes

### Community 167 - ".CheckMemberShipCore"
Cohesion: 0.31
Nodes (8): SocketRole, SocketTextChannel, CancellationToken, SocketGuild, Task, YoutubeMemberNotMemberApplyResult, YoutubeMemberService, YoutubeMemberNotMemberApplyResult

### Community 168 - "InteractionErrorPolicyTests"
Cohesion: 0.33
Nodes (5): Fact, InlineData, InteractionCommandError, Theory, InteractionErrorPolicyTests

### Community 169 - ".HandleTwitcastingAsync"
Cohesion: 0.60
Nodes (3): ComponentInteraction, Task, SpiderManagementComponent

### Community 170 - "YoutubeMemberLifecycleTaskRegistry"
Cohesion: 0.12
Nodes (13): bool, ConcurrentDictionary, DateTime, IEnumerable, long, object, Task, TimeSpan (+5 more)

### Community 171 - "NonPersistentGoogleDataStore"
Cohesion: 0.21
Nodes (5): IDataStore, Task, ITokenDataStore, Task, NonPersistentGoogleDataStore

### Community 172 - "10. 手動驗收矩陣"
Cohesion: 0.40
Nodes (5): 10.1 授權, 10.2 爬蟲, 10.3 YouTube 會員驗證, 10.4 Twitch 訂閱驗證, 10. 手動驗收矩陣

### Community 173 - "6. Bot 實作"
Cohesion: 0.40
Nodes (5): 6.1 先抽共用 crawler service 流程, 6.2 補 verification 管理入口, 6.3 擴充 AdminSettings contract 與快照, 6.4 併發與 cancellation, 6. Bot 實作

### Community 174 - "TwitchGuildEligibilityEvaluator"
Cohesion: 0.24
Nodes (8): ConcurrentDictionary, DateTime, Task, TimeProvider, TimeSpan, TwitchGuildEligibilityEvaluator, TwitchGuildEligibilityStatus, TwitchGuildEligibilityDecision

### Community 175 - "NotificationBusConsumer"
Cohesion: 0.20
Nodes (11): CancellationToken, Func, IDatabase, int, StreamEntry, Task, TwitcastingService, TwitchService (+3 more)

### Community 176 - "GuildTwitchSubscriptionConfig"
Cohesion: 0.13
Nodes (9): IQueryable, IEnumerable, int, IReadOnlyCollection, TwitchSubscriptionConfigurationPolicy, TwitchSubscriptionConfigurationQueries, TwitchRoleConfigurationResult, GuildTwitchSubscriptionConfig (+1 more)

### Community 177 - "TwitchSubscriptionPolicies.cs"
Cohesion: 0.18
Nodes (7): TwitchAuthorizationEventPolicy, TwitchAuthorizationLocalState, TwitchAuthorizationLocalStatePolicy, TwitchRefreshPersistenceDecision, TwitchRefreshPersistencePolicy, InlineData, Theory

### Community 179 - "YoutubeApiVideoPolicyTests"
Cohesion: 0.20
Nodes (9): YoutubeApiVideoAction, YoutubeApiVideoDecision, YoutubeApiVideoFacts, YoutubeApiVideoPolicy, DateTime, Fact, InlineData, Theory (+1 more)

### Community 180 - ".CheckMemberShipOnlyVideoIdAsync"
Cohesion: 0.16
Nodes (11): Task, YoutubeDetectionService, YoutubeMemberCandidateAction, YoutubeMemberCandidateFacts, YoutubeMemberChannelDecision, YoutubeMemberChannelFacts, YoutubeMemberVideoPolicy, Fact (+3 more)

### Community 181 - ".DecideAutomaticMutation"
Cohesion: 0.31
Nodes (4): YoutubeMemberAutomaticMutationAction, YoutubeMemberManualPinPolicy, Fact, YoutubeMemberManualPinPolicyTests

### Community 182 - ".HandleStartLiveMessageAsync"
Cohesion: 0.17
Nodes (11): List, RedisValue, SemaphoreSlim, Task, TwitcastingDetectionService, IEnumerable, TwitcastingLiveStartAction, TwitcastingLiveStartFacts (+3 more)

### Community 183 - "YoutubeMemberRoleApplyResult"
Cohesion: 0.50
Nodes (3): YoutubeMemberRoleApplyResult, InlineData, Theory

### Community 184 - "NijisanjiLiverJson.cs"
Cohesion: 0.70
Nodes (4): Head, Images, NijisanjiLiverJson, SocialLinks

### Community 185 - "YoutubeVideoIdParser"
Cohesion: 0.20
Nodes (7): string, Uri, YoutubeVideoIdParser, InlineData, string, Theory, YoutubeVideoIdParserTests

### Community 186 - "8. Frontend 實作"
Cohesion: 0.50
Nodes (4): 8.1 爬蟲頁, 8.2 驗證頁, 8.3 前端狀態, 8. Frontend 實作

### Community 187 - "YoutubeChannelOwnedType"
Cohesion: 0.50
Nodes (3): DateTime, YTChannelType, YoutubeChannelOwnedType

### Community 191 - "YoutubeMemberVideoLogMessageFormatterTests"
Cohesion: 0.41
Nodes (5): YoutubeMemberVideoLogNotification, Fact, InlineData, Theory, YoutubeMemberVideoLogMessageFormatterTests

### Community 192 - "Category"
Cohesion: 0.70
Nodes (4): List, CategoriesJson, Category, SubCategory

### Community 194 - "TwitchSpider"
Cohesion: 0.12
Nodes (18): AutocompleteHandler, GuildNoticeTwitCastingChannelIdAutocompleteHandler, GuildNoticeTwitchChannelIdAutocompleteHandler, CommandExample, CommandSummary, SlashCommand, Task, TwitchService (+10 more)

### Community 195 - "DiscordStreamNotifyBot.Shared.Messages"
Cohesion: 0.08
Nodes (20): DiscordStreamNotifyBot.SharedService.AdminSettings, DiscordStreamNotifyBot.Scraper.Detection.Twitch.Debounce, DiscordStreamNotifyBot.Scraper.Detection.Twitch, DiscordStreamNotifyBot.SharedService.Youtube.Json, DiscordStreamNotifyBot.Shared.Messages, NoticeType, TwitchNotificationVariant, BannerDownloadResult (+12 more)

### Community 196 - "NijisanjiStreamJson.cs"
Cohesion: 0.43
Nodes (6): DateTime, List, Channel, EventLiver, Liver, NijisanjiStreamJson

### Community 198 - ".TryGetPayload"
Cohesion: 0.29
Nodes (4): Fact, InlineData, Theory, RedisContractTests

### Community 200 - "TcBackendStreamData.cs"
Cohesion: 0.44
Nodes (8): App, BackendMovie, Fmp4, Hls, Llfmp4, Streams, TcBackendStreamData, Webrtc

### Community 205 - "MainDbService"
Cohesion: 0.12
Nodes (18): DbContextOptions, TwitCasting, ComponentInteraction, GuildTwitchSubscriptionConfig, RequireContext, SlashCommand, Task, TwitchSubscription (+10 more)

### Community 208 - ".CreateAsync"
Cohesion: 0.05
Nodes (31): IAsyncDisposable, IDisposable, IServiceProvider, IServiceScope, IServiceScopeFactory, List, SemaphoreSlim, ValueTask (+23 more)

### Community 209 - ".HasOtherActiveEntitlement"
Cohesion: 0.29
Nodes (3): IEnumerable, MemberRoleOwnershipPolicy, MemberRoleOwnershipPolicyTests

### Community 211 - ".CheckPermissionsAsync"
Cohesion: 0.25
Nodes (6): CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task, RequireGuildOwnerAttribute

### Community 212 - ".CheckRequirementsAsync"
Cohesion: 0.25
Nodes (6): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildOwnerAttribute

### Community 213 - ".Main"
Cohesion: 0.13
Nodes (8): int, Task, Program, BotRole, CancellationToken, CancellationTokenSource, int, GracefulShutdown

### Community 214 - ".Resolve"
Cohesion: 0.57
Nodes (3): InteractionCommandError, InteractionErrorDescriptor, InteractionErrorPolicy

### Community 215 - "YouTube 會員驗證架構重構計畫"
Cohesion: 0.15
Nodes (12): 11. 排程與生命週期, 12. Provider Result 分類, 17. Manual Acceptance Matrix, 18. 停機部署順序, 19. Completion Criteria, 1. 範圍, 20. 新 Session 執行規則, 2. 已定案決策 (+4 more)

### Community 216 - "TwitchOAuthRefreshLockRedisComponentTests"
Cohesion: 0.36
Nodes (4): IConnectionMultiplexer, RedisComponentFact, Task, TwitchOAuthRefreshLockRedisComponentTests

### Community 218 - ".SendMessageToAllGuildAsync"
Cohesion: 0.22
Nodes (7): DiscordStreamNotifyBot.Interaction.OwnerOnly, SendMsgToAllGuildService, DefaultMemberPermissions, RequireOwner, SlashCommand, Task, SendMsgToAllGuild

### Community 220 - ".GetLocaleAsync"
Cohesion: 0.10
Nodes (26): InteractionModuleBase, SocketInteractionContext, Task, TopLevelModule, CommandExample, CommandSummary, DiscordSocketClient, IChannel (+18 more)

### Community 224 - ".LoadCommandFrom"
Cohesion: 0.40
Nodes (4): Assembly, IEnumerable, IServiceCollection, Type

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

### Community 231 - ".SendConfirmMessageAsync"
Cohesion: 0.36
Nodes (6): IDMChannel, DiscordSocketClient, EmbedBuilder, ITextChannel, IUserMessage, Ext

### Community 235 - "YouTube 會員驗證"
Cohesion: 0.33
Nodes (5): Durable state, YouTube 會員驗證, 使用者契約, 服務邊界, 部署前驗證

### Community 238 - "14. Frontend"
Cohesion: 0.40
Nodes (5): 14.1 TypeScript contract, 14.2 GoogleSection, 14.3 VerifyWindow, 14.4 Copy/Privacy, 14. Frontend

### Community 239 - "8. DB Schema"
Cohesion: 0.40
Nodes (5): 8.1 Entity changes, 8.2 Indexes, 8.3 Migration 規則, 8.4 Preflight 查詢, 8. DB Schema

### Community 241 - ".ValidateProviderTokenEncryptionKey"
Cohesion: 0.29
Nodes (4): Fact, InlineData, Theory, ProviderTokenEncryptionKeyTests

### Community 242 - "14. 部署與回滾"
Cohesion: 0.50
Nodes (4): 14.1 建議部署順序, 14.2 相容性, 14.3 回滾, 14. 部署與回滾

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

### Community 256 - ".OnlyAffiliateAndPartnerCanBeConfigured"
Cohesion: 0.33
Nodes (3): InlineData, Theory, TwitchSubscriptionConfigurationPolicyTests

### Community 257 - "DiscordStreamNotifyBot.Tests"
Cohesion: 0.09
Nodes (8): DiscordStreamNotifyBot.Scraper.Detection.Youtube, DiscordStreamNotifyBot.Tests, string, BotStateCollectionDefinition, Fact, NotificationBusConsumerOptionsTests, Fact, YoutubeReminderRegistryTests

## Knowledge Gaps
- **469 isolated node(s):** `net8.0`, `prometheus-net.AspNetCore (8.2.1)`, `Microsoft.NET.Sdk`, `DiscordStreamNotifyBot.Command.Normal`, `DiscordStreamNotifyBot.Command.TwitCasting` (+464 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **41 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `DiscordStreamNotifyBot.DataBase.Table` connect `DiscordStreamNotifyBot.DataBase.Table` to `DiscordStreamNotifyBot.Tests`, `DiscordStreamNotifyBot.Shared`, `.Get`, `DiscordStreamNotifyBot.Localization`, `NotificationEmbedFactoryTests`, `YoutubeMemberPolicies`, `.RemoveSpiderIfStillInvalidAsync`, `MainDbContext`, `YoutubeMemberRoleService`, `GuildLocaleService`, `MySqlComponentFixture`, `GuildTwitchSubscriptionConfig`, `TwitchSubscriptionPolicies.cs`, `TwitchStateDecisions.cs`, `.HandleStartLiveMessageAsync`, `YoutubeChannelOwnedType`, `DiscordStreamNotifyBot.DataBase`, `DiscordStreamNotifyBot.SharedService.YoutubeMember`, `DiscordStreamNotifyBot.Shared.Messages`, `DiscordStreamNotifyBot.Command.Attribute`?**
  _High betweenness centrality (0.076) - this node is a cross-community bridge._
- **Why does `MainDbService` connect `MainDbService` to `TwitchAuthorizationTokenService`, `ClusterQueryService`, `.GetDbContext`, `.SendLocalizedErrorAsync`, `MySqlDataStore`, `YoutubeMemberAuthorizationService`, `SendMsgToAllGuildService`, `YoutubeMemberService`, `.LoadSnapshotAsync`, `.GetGuildNameMapAsync`, `YoutubeMemberRoleService`, `TwitchDetectionService`, `Twitch`, `.HandleTwitcastingAsync`, `GuildLocaleService`, `MySqlComponentFixture`, `.SetMemberCheckVideoIdAsync`, `YoutubeMemberSetting`, `Utility`, `.HandleStartLiveMessageAsync`, `DiscordStreamNotifyBot.DataBase`, `.Warn`, `Bot`, `TwitchSpider`, `DiscordStreamNotifyBot.Shared.Messages`, `AdminSettingsService`, `YoutubeStream`, `AdminSettingsMutationResult`, `YoutubeDetectionService`, `.GetLocaleAsync`, `UtilityService`, `.CreateOrRepairConfigurationAsync`, `TwitchService`?**
  _High betweenness centrality (0.075) - this node is a cross-community bridge._
- **Why does `DiscordStreamNotifyBot.Shared` connect `DiscordStreamNotifyBot.Shared` to `DiscordStreamNotifyBot.Tests`, `.PublishAsync`, `DiscordStreamNotifyBot.DataBase.Table`, `ClusterQueryService`, `DiscordStreamNotifyBot.Localization`, `YoutubeMemberAuthorizationService`, `.RunCoreAsync`, `SendMsgToAllGuildService`, `CoordinatorMetrics`, `BotConfig`, `RedisChannels`, `MySqlComponentFixture`, `YoutubeVideoIdParser`, `DiscordStreamNotifyBot.DataBase`, `DiscordStreamNotifyBot.SharedService.YoutubeMember`, `DiscordStreamNotifyBot.Shared.Messages`, `.Main`, `DiscordStreamNotifyBot.Command.Attribute`, `ClusterService`?**
  _High betweenness centrality (0.054) - this node is a cross-community bridge._
- **What connects `net8.0`, `prometheus-net.AspNetCore (8.2.1)`, `Microsoft.NET.Sdk` to the rest of the system?**
  _469 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `DiscordStreamNotifyBot.DataBase.Table` be split into smaller, more focused modules?**
  _Cohesion score 0.13405797101449277 - nodes in this community are weakly interconnected._
- **Should `DiscordStreamNotifyBot.Shared.csproj` be split into smaller, more focused modules?**
  _Cohesion score 0.08333333333333333 - nodes in this community are weakly interconnected._
- **Should `TwitchApiService` be split into smaller, more focused modules?**
  _Cohesion score 0.08673469387755102 - nodes in this community are weakly interconnected._