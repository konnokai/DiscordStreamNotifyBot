# Graph Report - DiscordStreamNotifyBot  (2026-08-21)

## Corpus Check
- 327 files · ~171,445 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 4147 nodes · 9626 edges · 232 communities (195 shown, 37 thin omitted)
- Extraction: 91% EXTRACTED · 9% INFERRED · 0% AMBIGUOUS · INFERRED: 832 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `8c93aaad`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- TwitchSubscriptionApiClient
- .GetDbContext
- .Warn
- DiscordStreamNotifyBot.SharedService.Twitch
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
- TwitchSubscriptionService
- Extensions
- DiscordStreamNotifyBot.Command
- 會限 OAuth Token 儲存改走 MySQL（去 Redis 依賴）計畫
- .GetLocaleAsync
- TwitcastingService
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
- DiscordStreamNotifyBot.DataBase.Table
- TwitchOAuthRefreshLockLease
- .Main
- DiscordStreamNotifyBot.Interaction.Attribute
- YoutubeMemberRoleService
- TwitchDetectionService
- Twitch
- ScraperMetrics
- GuildLocaleService
- DebounceChannelUpdateMessage
- RedisChannels
- 13. 驗證矩陣
- MySqlComponentFixture
- 網頁管理設定：30 秒請求與背景清理實作計畫
- Administration
- 網頁管理設定中心：爬蟲與會員驗證實作計畫
- 水平擴展（三層拆分）計畫 — Redis Streams 版
- YoutubeMemberSetting
- TwitchStateDecisions.cs
- Utility
- .PrepareMemberCheckCleanupAsync
- AdministrationService
- AGENTS.md
- TwitchRefreshRotationLifecycle
- .RetryWithBackoffAsync
- DiscordStreamNotifyBot.Shared
- YoutubeStreamService
- graphify reference: extra exports and benchmark
- Bot
- DiscordStreamNotifyBot.DataBase
- TwitchReconcileDecisionTests
- .FilterNoNotifyGuilds
- .Main
- AddManualMemberCheckVideoFlag
- AdminSettingsService
- NotificationContractTests
- EF Core 遷移與基線化（本專案版）
- TwitchSubscriptionRolePolicyTests
- 11. 通知與背景訊息
- GoogleOAuthOperationLockLease
- MetadataServiceProvider
- FUNDING.yml (Patreon / ECPay / PayPal)
- Build workflow (SonarQube analysis)
- MIT License
- Notifier Bot Logo — interlocking chain-link icon, purple-to-magenta-to-red gradient on light grey circle; flat modern vector branding representing the linking/notification identity of the Discord stream-notify bot
- YoutubeStream
- AdminSettingsMutationResult
- AdminSettingsContractTests
- AdminSettings.cs
- .Info
- graphify reference: query, path, explain
- 自動化測試導入計畫
- YoutubeApiService
- DescriptionOnlyLocalizationManager
- .Get
- .SendStreamMessageAsync
- TwitchSubscriptionApiClientTests
- .GetGuildNameMapAsync
- 6. 資源架構
- .AssertKeysAbsentAsync
- graphify reference: add a URL and watch a folder
- graphify reference: commit hook and native CLAUDE.md integration
- graphify reference: incremental update and cluster-only
- .ExecuteOnceAsync
- graphify reference: GitHub clone and cross-repo merge
- graphify reference: transcribe video and audio
- 網頁管理設定中心實作計畫
- YoutubeMemberSelectMenuTests
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
- MigrationAndConstraintTests
- Normal
- ClusterService
- DiscordStreamNotifyBot.HttpClients.Twitcasting.Model
- Twitch OAuth 與零成本 EventSub 實作計畫
- .SendLocalizedConfirmAsync
- YoutubeMemberApiClient
- .Filter
- 16. 執行階段
- Prometheus / Grafana 監控
- TwitcastingClient
- DiscordStreamNotifyBot.Scraper.csproj
- DiscordStreamNotifyBot.Tests.csproj
- 17. 驗證矩陣
- UptimeKumaClient
- .RefreshMetricsAsync
- 7. OAuth API 與流程隔離
- TwitcastingLiveStartPlannerTests
- ClusterQueryService
- .Plan
- HelpDescription (bot feature summary)
- BotConfig
- 11. Bot EventSub 與偵測
- 15. 預期修改檔案
- 2. 現況基線
- 5. Guild 資格與 OAuth 豁免
- DiscordStreamNotifyBot.sln
- CommandDisplayResolver
- 13. Prometheus
- 4. 安全刪除狀態機
- .HandleTwitcastingAsync
- RedisConnection
- DiscordStreamNotifyBot.Migrations
- Notifications.cs
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
- .FixTCDbAsync
- 20260611015819_SyncModelDrift.Designer.cs
- .MakeNamesUnique
- 20260719142803_AddTwitchBroadcasterAuthorization.Designer.cs
- DiscordStreamNotifyBot.Tests.Component.MySql
- .LoadSnapshotAsync
- .CheckRequirementsAsync
- .NotifyAddedAsync
- InteractionErrorPolicyTests
- TwitchSubscriptionPolicies.cs
- .SetMemberCheckVideoIdAsync
- YoutubeApiVideoPolicyTests
- .CheckMemberShipOnlyVideoIdAsync
- .HandleStartLiveMessageAsync
- YoutubeDetectionService
- YoutubeVideoIdParser
- 8. 分階段實作步驟
- .GenerateSuggestionsAsync
- .GenerateSuggestionsAsync
- .GenerateSuggestionsAsync
- YoutubeMemberVideoLogMessageFormatterTests
- Category
- .DecideAutomaticMutation
- TwitchSubscriptionSetting
- BotState
- YoutubeDetectionService.cs
- .GenerateSuggestionsAsync
- TcBackendStreamData.cs
- 20260803141135_AddTwitchSubscriptionVerification.Designer.cs
- AGENTS.md
- .GenerateSuggestionsAsync
- MainDbService
- AddTwitchSubscriptionVerification
- AddTwitchSubscriptionDeletionPending
- .CreateAsync
- 4. 訊息契約：Redis Streams 通知匯流排
- 5. 語系模型與解析規則
- .CheckPermissionsAsync
- .CheckRequirementsAsync
- .Resolve
- YouTube 會員驗證架構重構計畫
- NijisanjiLiverJson.cs
- .GuildMemberCountPreconditionMapsValuesAndContactPath
- .GenerateSuggestionsAsync
- 20250320095452_RefactorDbContext.Designer.cs
- 5. 目標架構
- .LockGuildAsync
- 15. 實作階段
- Migration
- 20250603065853_ModifyTwitCastingTable.Designer.cs
- 20260804173737_AddYoutubeMemberVerificationDurability.Designer.cs
- 20260807045351_AddGoogleOAuthUnlinkIntent.Designer.cs
- YouTube 會員驗證
- YoutubeReminderRegistryTests
- .GenerateSuggestionsAsync
- 14. Frontend
- 8. DB Schema
- AddYoutubeMemberVerificationDurability
- TwitchBroadcasterAuthorization
- 13. Backend Contract
- 16. 驗證命令
- 20260803165758_AddTwitchSubscriptionDeletionPending.Designer.cs
- .SameUserMutationsAreExclusiveAndOwnerReleaseRemovesTheKey
- 10. Slash 與 Interaction Cutover
- 6. 目標架構
- 7. 狀態機
- 9. Role 隔離政策
- TwitchSpider
- RenameVerificationLogChannel
- DiscordStreamNotifyBot.Tests
- 20260721095646_AddLocalizationSettings.Designer.cs
- 20260813032017_RenameVerificationLogChannel.Designer.cs

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
- `YoutubeMemberVideoLogMessageFormatterTests` --references--> `BotLocalizer`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/YoutubeMemberVideoLogMessageFormatterTests.cs → src/DiscordStreamNotifyBot.Notifier/Localization/BotLocalizer.cs
- `YoutubeMemberVideoLogMessageFormatterTests` --references--> `CommandDisplayResolver`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/YoutubeMemberVideoLogMessageFormatterTests.cs → src/DiscordStreamNotifyBot.Notifier/Localization/CommandDisplayResolver.cs
- `MySqlComponentFixture` --references--> `MainDbService`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/Component/MySql/MySqlComponentFixture.cs → src/DiscordStreamNotifyBot.Shared/DataBase/MainDbService.cs

## Import Cycles
- None detected.

## Communities (232 total, 37 thin omitted)

### Community 0 - "TwitchSubscriptionApiClient"
Cohesion: 0.14
Nodes (15): CancellationToken, DateTimeOffset, HttpResponseMessage, IHttpClientFactory, NotifierMetrics, string, Task, TwitchProviderResult (+7 more)

### Community 1 - ".GetDbContext"
Cohesion: 0.23
Nodes (14): DiscordStreamNotifyBot.Interaction.Youtube, CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, IChannel, NoticeType, RequireBotPermission (+6 more)

### Community 2 - ".Warn"
Cohesion: 0.13
Nodes (18): PendingRefreshPersistence, CancellationToken, int, NotifierMetrics, object, string, Task, TimeSpan (+10 more)

### Community 3 - "DiscordStreamNotifyBot.SharedService.Twitch"
Cohesion: 0.12
Nodes (8): DiscordStreamNotifyBot.SharedService.Twitch, DiscordStreamNotifyBot.Interaction.TwitchSubscription, DiscordStreamNotifyBot.SharedService.TwitchSubscription, TwitchAppAccessTokenResponse, TwitchApiServiceDisabledTests, InlineData, Theory, TwitchSubscriptionConfigurationPolicyTests

### Community 4 - "DiscordStreamNotifyBot.Shared.csproj"
Cohesion: 0.08
Nodes (23): Microsoft.EntityFrameworkCore.Design (9.0.3), Microsoft.EntityFrameworkCore.Relational (9.0.3), Microsoft.EntityFrameworkCore.Tools (9.0.3), Serilog (4.4.0), Serilog.Sinks.Console (6.1.1), Serilog.Sinks.File (7.0.0), Serilog.Sinks.Grafana.Loki (9.0.1), net8.0 (+15 more)

### Community 5 - "TwitchApiService"
Cohesion: 0.07
Nodes (39): EventSubSubscription, IReadOnlyList, Stream, TwitchEventSubDeleteResult, TwitchEventSubDeleteStatus, TwitchEventSubEnsureMode, TwitchEventSubEnsureResult, TwitchEventSubSubscriptionsResult (+31 more)

### Community 6 - "InteractionHandler"
Cohesion: 0.07
Nodes (27): DisplayName, IResult, ISet, SocketInteraction, SocketSlashCommandDataOption, Dictionary, DiscordSocketClient, Func (+19 more)

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
Cohesion: 0.06
Nodes (39): Color, IDMChannel, Dictionary, Regex, ResourceManager, string, BotLocalizer, EmbedBuilder (+31 more)

### Community 12 - "DiscordStreamNotifyBot.Shared.Messages"
Cohesion: 0.08
Nodes (19): DiscordStreamNotifyBot.SharedService.AdminSettings, DiscordStreamNotifyBot.SharedService.Youtube, DiscordStreamNotifyBot.SharedService.Twitcasting, DiscordStreamNotifyBot.Interaction.Utility.Service, DiscordStreamNotifyBot.Localization, DiscordStreamNotifyBot.Interaction.Utility, DiscordStreamNotifyBot.Command.Admin, DiscordStreamNotifyBot.SharedService.Cluster (+11 more)

### Community 13 - "Extensions"
Cohesion: 0.06
Nodes (26): Process, Assembly, DiscordSocketClient, EmbedBuilder, Func, IDiscordInteraction, IEmote, IEnumerable (+18 more)

### Community 14 - "TwitchSubscriptionService"
Cohesion: 0.16
Nodes (13): CancellationToken, CancellationTokenSource, ConcurrentDictionary, DateTimeOffset, DiscordSocketClient, int, NotifierMetrics, RedisValue (+5 more)

### Community 15 - "Extensions"
Cohesion: 0.05
Nodes (35): IAsyncDisposable, IDisposable, SocketCommandContext, Assembly, DateTime, DiscordSocketClient, EmbedBuilder, Func (+27 more)

### Community 16 - "DiscordStreamNotifyBot.Command"
Cohesion: 0.18
Nodes (8): DiscordStreamNotifyBot.Command, SocketMessage, CommandService, DiscordSocketClient, IServiceProvider, Task, CommandHandler, ICommandService

### Community 17 - "會限 OAuth Token 儲存改走 MySQL（去 Redis 依賴）計畫"
Cohesion: 0.11
Nodes (18): Backend, Bot（本 repo）, MySQL（兩端都已連同一個庫）, 儲存層（現況為 Redis）, 加密與 blob 格式（兩端一致）, 加密金鑰處理, 影響檔案一覽, 待決策（給實作 session） (+10 more)

### Community 18 - ".GetLocaleAsync"
Cohesion: 0.13
Nodes (21): InteractionModuleBase, SocketInteractionContext, Task, TopLevelModule, CommandExample, CommandSummary, DiscordSocketClient, IChannel (+13 more)

### Community 19 - "TwitcastingService"
Cohesion: 0.18
Nodes (10): Broadcaster, CancellationToken, DiscordSocketClient, EmojiService, NoticeCache, NotifierMetrics, SocketGuild, Task (+2 more)

### Community 20 - "AuthTokenTests"
Cohesion: 0.05
Nodes (26): DiscordStreamNotifyBot.Auth, DiscordStreamNotifyBot, IDataStore, TokenCrypto, TokenManager, Task, ITokenDataStore, CancellationToken (+18 more)

### Community 21 - "YoutubeMemberAuthorizationService"
Cohesion: 0.13
Nodes (14): GoogleAuthorizationCodeFlow, CancellationToken, HttpClient, MySqlDataStore, string, Task, YoutubeMemberAuthorizationResult, YoutubeMemberAuthorizationService (+6 more)

### Community 22 - "Log"
Cohesion: 0.07
Nodes (26): DelegatingHandler, ILogEventSink, ITextFormatter, LogEvent, LogEventLevel, LogFileRoute, Logger, LoggerConfiguration (+18 more)

### Community 23 - ".RunCoreAsync"
Cohesion: 0.21
Nodes (9): CancellationToken, Func, Task, TimeProvider, TimeSpan, PeriodicRunner, Fact, Task (+1 more)

### Community 24 - "Twitch 訂閱驗證實作計畫"
Cohesion: 0.05
Nodes (36): 10. Frontend 調整, 11. 安全與錯誤處理, 12.1 Backend, 12.2 Bot, 12.3 Frontend, 12. 自動化測試, 13. 手動驗收, 14. 實作順序 (+28 more)

### Community 25 - "YoutubeMemberPolicies"
Cohesion: 0.09
Nodes (13): ComponentInteraction, Task, IEnumerable, IReadOnlyCollection, IReadOnlyList, YoutubeMemberPolicies, YoutubeMemberRoleApplyResult, YoutubeMemberSelectionTransition (+5 more)

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
Cohesion: 0.06
Nodes (27): SocketMessageComponent, bool, ConcurrentDictionary, DateTime, IEnumerable, long, object, Task (+19 more)

### Community 33 - "TwitchGuildEligibilityEvaluator"
Cohesion: 0.25
Nodes (7): ConcurrentDictionary, DateTime, Task, TimeProvider, TimeSpan, TwitchGuildEligibilityEvaluator, TwitchGuildEligibilityDecision

### Community 34 - "Log 與 Loki"
Cohesion: 0.20
Nodes (7): Console 備援, Grafana Dashboard, Log 與 Loki, Loki 主動推送, Serilog Pipeline, 排障, 檔案路由

### Community 35 - "DiscordStreamNotifyBot.DataBase.Table"
Cohesion: 0.04
Nodes (35): DiscordStreamNotifyBot.DataBase.Table, DbContext, IDesignTimeDbContextFactory, DbSet, ModelBuilder, MainDbContext, MainDbContextFactory, BannerChange (+27 more)

### Community 36 - "TwitchOAuthRefreshLockLease"
Cohesion: 0.11
Nodes (21): CancellationToken, CancellationTokenSource, Exception, IConnectionMultiplexer, IDatabase, int, RedisKey, RedisValue (+13 more)

### Community 37 - ".Main"
Cohesion: 0.10
Nodes (12): int, Task, Program, BotRole, CancellationToken, CancellationTokenSource, int, GracefulShutdown (+4 more)

### Community 38 - "DiscordStreamNotifyBot.Interaction.Attribute"
Cohesion: 0.10
Nodes (19): DiscordStreamNotifyBot.Interaction.Attribute, DiscordStreamNotifyBot.Interaction.OwnerOnly, DiscordStreamNotifyBot.Interaction.TwitCasting, DiscordStreamNotifyBot.Interaction.Help.Service, SendMsgToAllGuildService, DefaultMemberPermissions, RequireOwner, SlashCommand (+11 more)

### Community 39 - "YoutubeMemberRoleService"
Cohesion: 0.23
Nodes (9): CancellationToken, DiscordSocketClient, IEnumerable, IRole, SocketGuild, Task, YoutubeMemberRoleConfigurationResult, YoutubeMemberRoleService (+1 more)

### Community 40 - "TwitchDetectionService"
Cohesion: 0.12
Nodes (14): HelixStream, ConcurrentDictionary, DateTime, IReadOnlyCollection, RedisValue, ScraperMetrics, SemaphoreSlim, Task (+6 more)

### Community 41 - "Twitch"
Cohesion: 0.29
Nodes (11): Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task, TwitchService (+3 more)

### Community 42 - "ScraperMetrics"
Cohesion: 0.20
Nodes (11): Counter, Gauge, string, ScraperMetricResult, ScraperMetrics, TwitchAuthorizationChangeMetricResult, TwitchEventSubCleanupDeferredMetricReason, TwitchEventSubMetricStatus (+3 more)

### Community 43 - "GuildLocaleService"
Cohesion: 0.07
Nodes (28): CultureInfo, CancellationToken, ConcurrentDictionary, Dictionary, Func, IEnumerable, SemaphoreSlim, SocketGuild (+20 more)

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
Cohesion: 0.08
Nodes (18): IAsyncLifetime, DateTime, EmbedBuilder, Video, YTChannelType, SharedExtensions, string, Task (+10 more)

### Community 48 - "網頁管理設定：30 秒請求與背景清理實作計畫"
Cohesion: 0.10
Nodes (19): 10. 實作順序, 11. 不在本次實作, 1. 目標, 2. 已確認決策, 3. 端點範圍與 deadline, 4. Cross-project contract, 5.1 Controller, 5.2 Redis bridge (+11 more)

### Community 49 - "Administration"
Cohesion: 0.44
Nodes (8): Alias, Command, DiscordSocketClient, RequireContext, RequireOwner, Summary, Task, Administration

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
Cohesion: 0.09
Nodes (21): TimeSpan, TwitchChannelUpdateAction, TwitchGuildEligibilityPolicy, TwitchMissingObservationAction, TwitchOfflineAction, TwitchOfflineFacts, TwitchOfflinePolicy, TwitchOfflineScheduleAction (+13 more)

### Community 54 - "Utility"
Cohesion: 0.27
Nodes (10): DefaultMemberPermissions, DiscordSocketClient, DiscordWebhookClient, IChannel, ITextChannel, RequireContext, RequireUserPermission, SlashCommand (+2 more)

### Community 55 - ".PrepareMemberCheckCleanupAsync"
Cohesion: 0.13
Nodes (19): CheckId, Snapshot, SocketRole, SocketTextChannel, CancellationToken, SocketGuild, Task, YoutubeMemberNotMemberApplyResult (+11 more)

### Community 56 - "AdministrationService"
Cohesion: 0.16
Nodes (10): DiscordSocketClient, Expected, IReadOnlyCollection, ITextChannel, Responded, SocketGuild, string, Task (+2 more)

### Community 57 - "AGENTS.md"
Cohesion: 0.17
Nodes (11): Build & Run, Conventions, EF Core 鐵則, graphify, 制度條款, 外部契約（不可片面更改）, 指令文件, 架構要點（現行樹） (+3 more)

### Community 58 - "TwitchRefreshRotationLifecycle"
Cohesion: 0.13
Nodes (14): Action, bool, Dictionary, Lease, long, object, Task, TaskCompletionSource (+6 more)

### Community 59 - ".RetryWithBackoffAsync"
Cohesion: 0.20
Nodes (9): Func, Task, TimeProvider, TimeSpan, StartupPreflight, DateTimeOffset, Fact, Task (+1 more)

### Community 60 - "DiscordStreamNotifyBot.Shared"
Cohesion: 0.11
Nodes (10): DiscordStreamNotifyBot.Tests.Component.Redis, DiscordStreamNotifyBot.Scraper, DiscordStreamNotifyBot.Shared, DiscordStreamNotifyBot.Scraper.Detection.Twitch.Debounce, DiscordStreamNotifyBot.SharedService.Google, TwitchAuthorizationChangedPayload, TwitchReconcileRequestedPayload, TwitchStreamEventPayload (+2 more)

### Community 61 - "YoutubeStreamService"
Cohesion: 0.10
Nodes (16): NowStreamingHost, CancellationToken, DiscordSocketClient, Embed, EmojiService, HttpClient, IEnumerable, IHttpClientFactory (+8 more)

### Community 62 - "graphify reference: extra exports and benchmark"
Cohesion: 0.22
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 63 - "Bot"
Cohesion: 0.14
Nodes (12): BotPlayingStatus, ConnectionMultiplexer, DiscordSocketClient, IDatabase, int, ISubscriber, IUser, Task (+4 more)

### Community 64 - "DiscordStreamNotifyBot.DataBase"
Cohesion: 0.18
Nodes (6): DiscordStreamNotifyBot.SharedService.YoutubeMember, DiscordStreamNotifyBot.Interaction.YoutubeMember, DiscordStreamNotifyBot.Command.YoutubeMember, DiscordStreamNotifyBot.DataBase, DiscordStreamNotifyBot.SharedService.Member, GuildYoutubeMemberCheckChannelIdAutocompleteHandler

### Community 65 - "TwitchReconcileDecisionTests"
Cohesion: 0.15
Nodes (11): TwitchGuildEligibilityFacts, TwitchMissingGuildObservation, TwitchReconcileFacts, TwitchSpiderRemovalAction, TwitchSpiderRemovalFacts, TwitchSpiderRemovalPolicy, DateTime, Fact (+3 more)

### Community 66 - ".FilterNoNotifyGuilds"
Cohesion: 0.26
Nodes (7): IEnumerable, DateTime, List, GuildSnapshot, GuildSnapshotEnvelope, Fact, NoNotifyGuildFilterTests

### Community 67 - ".Main"
Cohesion: 0.11
Nodes (12): Assembly, CancellationToken, Exception, int, PeriodicTimer, Task, Program, HashSet (+4 more)

### Community 69 - "AdminSettingsService"
Cohesion: 0.12
Nodes (15): Id, Name, RequestRoute, CancellationToken, DiscordSocketClient, Func, IEnumerable, int (+7 more)

### Community 70 - "NotificationContractTests"
Cohesion: 0.33
Nodes (3): Fact, JObject, NotificationContractTests

### Community 71 - "EF Core 遷移與基線化（本專案版）"
Cohesion: 0.25
Nodes (7): EF Core 遷移與基線化（本專案版）, 一次性基線化（舊的 EnsureCreated 正式庫）, 一般變更流程, 你必須先知道的三件專案特例, 啟動時不碰資料庫（重要）, 套用：本地/開發 vs 正式環境, 收尾

### Community 72 - "TwitchSubscriptionRolePolicyTests"
Cohesion: 0.32
Nodes (4): Fact, InlineData, Theory, TwitchSubscriptionRolePolicyTests

### Community 73 - "11. 通知與背景訊息"
Cohesion: 0.29
Nodes (7): 11.1 現況限制, 11.2 目標作法, 11.3 YouTube, 11.4 Twitch, 11.5 TwitCasting, 11.6 YouTube 會限驗證, 11. 通知與背景訊息

### Community 74 - "GoogleOAuthOperationLockLease"
Cohesion: 0.15
Nodes (15): CancellationToken, CancellationTokenSource, IDatabase, int, RedisKey, RedisValue, string, Task (+7 more)

### Community 75 - "MetadataServiceProvider"
Cohesion: 0.16
Nodes (9): IServiceProvider, IServiceScope, IServiceScopeFactory, Dictionary, DiscordSocketClient, InteractionService, Type, InteractionMetadataFixture (+1 more)

### Community 80 - "YoutubeStream"
Cohesion: 0.27
Nodes (14): Alias, ClusterQueryService, Command, CommandExample, DiscordSocketClient, IEnumerable, List, RequireContext (+6 more)

### Community 81 - "AdminSettingsMutationResult"
Cohesion: 0.09
Nodes (21): IInteractionService, DiscordSocketClient, SocketGuild, AdminSettingsChannelValidator, CancellationToken, Clip, DateTime, DiscordSocketClient (+13 more)

### Community 82 - "AdminSettingsContractTests"
Cohesion: 0.13
Nodes (7): CrawlerPolicy, Fact, InlineData, MessageComponent, string, Theory, AdminSettingsContractTests

### Community 83 - "AdminSettings.cs"
Cohesion: 0.09
Nodes (37): Dictionary, int, List, string, AdminProbeVideoPayload, AdminRemoveNotificationPayload, AdminSetChannelPayload, AdminSetLocalePayload (+29 more)

### Community 84 - ".Info"
Cohesion: 0.17
Nodes (10): GeneratedRegex, YTChannelType, DateTime, DbSet, MainDbContext, Regex, Task, Video (+2 more)

### Community 85 - "graphify reference: query, path, explain"
Cohesion: 0.33
Nodes (5): For /graphify explain, For /graphify path, graphify reference: query, path, explain, Step 0 — Constrained query expansion (REQUIRED before traversal), Step 1 — Traversal

### Community 86 - "自動化測試導入計畫"
Cohesion: 0.17
Nodes (12): 10. 測試實作規則, 1. 目標, 2. 測試分類, 3. 不移除的啟動檢查, 4. 第一批：低耦合契約與格式化, 5. 第二批：小幅抽出純邏輯, 6. 第三批：時間與快取, 7. 第四批：Scraper 狀態機 (+4 more)

### Community 87 - "YoutubeApiService"
Cohesion: 0.15
Nodes (10): YTApiVideo, CancellationToken, IEnumerable, IHttpClientFactory, List, string, Task, YouTubeService (+2 more)

### Community 88 - "DescriptionOnlyLocalizationManager"
Cohesion: 0.29
Nodes (7): ILocalizationManager, ResxLocalizationManager, IDictionary, IList, LocalizationTarget, string, DescriptionOnlyLocalizationManager

### Community 89 - ".Get"
Cohesion: 0.16
Nodes (4): Fact, InlineData, Theory, BotLocalizerTests

### Community 90 - ".SendStreamMessageAsync"
Cohesion: 0.32
Nodes (4): Event, HttpException, Platform, NotificationMetricEvent

### Community 91 - "TwitchSubscriptionApiClientTests"
Cohesion: 0.19
Nodes (15): HttpMessageHandler, HttpStatusCode, IHttpClientFactory, CancellationToken, Fact, Func, HttpClient, HttpRequestMessage (+7 more)

### Community 92 - ".GetGuildNameMapAsync"
Cohesion: 0.28
Nodes (7): CommandExample, CommandSummary, SlashCommand, Task, YoutubeStreamService, GuildYoutubeChannelSpiderAutocompleteHandler, YoutubeChannelSpider

### Community 93 - "6. 資源架構"
Cohesion: 0.40
Nodes (5): 6.1 指令註冊資源, 6.2 執行期訊息資源, 6.3 Help 長文, 6.4 Localizer API, 6. 資源架構

### Community 94 - ".AssertKeysAbsentAsync"
Cohesion: 0.05
Nodes (50): ConfigurationOptions, FactAttribute, ICollectionFixture, CancellationToken, Func, IDatabase, int, StreamEntry (+42 more)

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

### Community 102 - "YoutubeMemberSelectMenuTests"
Cohesion: 0.20
Nodes (6): UserId, Fact, InlineData, Theory, YoutubeMemberSelectMenuTests, VideoId

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
Cohesion: 0.14
Nodes (20): AddRoleIds, IReadOnlySet, RemoveRoleIds, IReadOnlyCollection, MemberRoleOwnershipSnapshot, Func, IReadOnlyList, TwitchSubscriptionRolePolicy (+12 more)

### Community 114 - "TopLevelModule"
Cohesion: 0.43
Nodes (4): ModuleBase, EmbedBuilder, Task, TopLevelModule

### Community 115 - "DiscordStreamNotifyBot.Notifier.csproj"
Cohesion: 0.10
Nodes (19): Microsoft.Extensions.DependencyInjection.Abstractions (10.0.1), System.Management (10.0.1), net8.0, Ben.Demystifier (0.4.1), Discord.Net (3.19.1), Dorssel.Utilities.Debounce (3.0.0), EFCore.NamingConventions (9.0.0), Google.Apis.YouTube.v3 (1.73.0.3981) (+11 more)

### Community 116 - "MigrationAndConstraintTests"
Cohesion: 0.36
Nodes (3): MySqlComponentFact, Task, MigrationAndConstraintTests

### Community 117 - "Normal"
Cohesion: 0.26
Nodes (8): DiscordStreamNotifyBot.Command.Normal, Alias, Command, DiscordSocketClient, DiscordWebhookClient, Summary, Task, Normal

### Community 118 - "ClusterService"
Cohesion: 0.21
Nodes (8): IDatabase, string, Task, TimeSpan, ClusterService, RedisComponentFact, Task, ClusterServiceRedisComponentTests

### Community 119 - "DiscordStreamNotifyBot.HttpClients.Twitcasting.Model"
Cohesion: 0.15
Nodes (10): DiscordStreamNotifyBot.HttpClients, DiscordStreamNotifyBot.HttpClients.Twitcasting.Model, DiscordStreamNotifyBot.Command.TwitCasting, DiscordStreamNotifyBot.Scraper.Detection.Twitcasting, Broadcaster, Movie, TwitCastingWebHookJson, List (+2 more)

### Community 120 - "Twitch OAuth 與零成本 EventSub 實作計畫"
Cohesion: 0.14
Nodes (13): 0. 涉及專案, 10. Backend EventSub Webhook, 12. Frontend, 14. Grafana, 18. 建置與遷移, 19. 部署順序, 1. 不可偏離的決策, 20. 官方參考 (+5 more)

### Community 121 - ".SendLocalizedConfirmAsync"
Cohesion: 0.37
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
Cohesion: 0.16
Nodes (10): List, Broadcaster, GetMovieInfoResponse, Movie, GetUserInfoResponse, HttpClient, List, string (+2 more)

### Community 127 - "DiscordStreamNotifyBot.Scraper.csproj"
Cohesion: 0.50
Nodes (3): net8.0, prometheus-net.AspNetCore (8.2.1), Microsoft.NET.Sdk

### Community 128 - "DiscordStreamNotifyBot.Tests.csproj"
Cohesion: 0.25
Nodes (7): coverlet.collector (6.0.0), Microsoft.Extensions.TimeProvider.Testing (9.0.0), Microsoft.NET.Test.Sdk (17.8.0), xunit (2.5.3), xunit.runner.visualstudio (2.5.3), net8.0, Microsoft.NET.Sdk

### Community 129 - "17. 驗證矩陣"
Cohesion: 0.33
Nodes (6): 17.1 新增 spider, 17.2 EventSub, 17.3 授權失效, 17.4 OAuth, 17.5 Prometheus/Grafana, 17. 驗證矩陣

### Community 130 - "UptimeKumaClient"
Cohesion: 0.24
Nodes (7): bool, DiscordSocketClient, HttpClient, string, Task, Timer, UptimeKumaClient

### Community 131 - ".RefreshMetricsAsync"
Cohesion: 0.22
Nodes (4): EventSubSubscription, IReadOnlyDictionary, TwitchEventSubMetricStatus, TwitchEventSubMetricType

### Community 132 - "7. OAuth API 與流程隔離"
Cohesion: 0.40
Nodes (5): 7.1 API, 7.2 State, 7.3 Callback, 7.4 Twitch scopes, 7. OAuth API 與流程隔離

### Community 133 - "TwitcastingLiveStartPlannerTests"
Cohesion: 0.21
Nodes (7): TwitcastingLiveStartEvent, TwitcastingWebhookParser, Fact, InlineData, string, Theory, TwitcastingLiveStartPlannerTests

### Community 134 - "ClusterQueryService"
Cohesion: 0.15
Nodes (19): ChannelInfo, ClusterQueryType, Replies, Responses, Dictionary, DiscordSocketClient, Expected, Func (+11 more)

### Community 135 - ".Plan"
Cohesion: 0.19
Nodes (10): HashSet, IEnumerable, IReadOnlyList, string, TwitcastingWebhookAction, TwitcastingWebhookActionKind, TwitcastingWebhookRegistration, TwitcastingWebhookRegistrationPlanner (+2 more)

### Community 137 - "BotConfig"
Cohesion: 0.16
Nodes (10): ServiceProvider, DetectionHost, CancellationToken, PeriodicTimer, string, Task, TimeSpan, ScraperService (+2 more)

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
Nodes (31): AutocompleteHandler, DiscordStreamNotifyBot.Interaction.Help, RequireBotPermissionAttribute, RequireUserPermissionAttribute, AutocompletionResult, HelpService, IAutocompleteInteraction, IInteractionContext (+23 more)

### Community 144 - "13. Prometheus"
Cohesion: 0.67
Nodes (3): 13.1 Backend 指標, 13.2 Scraper 指標, 13. Prometheus

### Community 145 - "4. 安全刪除狀態機"
Cohesion: 0.67
Nodes (3): 4.1 直播中授權失效, 4.2 關台後重新判斷, 4. 安全刪除狀態機

### Community 146 - ".HandleTwitcastingAsync"
Cohesion: 0.60
Nodes (3): ComponentInteraction, Task, SpiderManagementComponent

### Community 147 - "RedisConnection"
Cohesion: 0.28
Nodes (5): ConnectionMultiplexer, Lazy, object, string, RedisConnection

### Community 148 - "DiscordStreamNotifyBot.Migrations"
Cohesion: 0.14
Nodes (8): DiscordStreamNotifyBot.Migrations, ModelSnapshot, ModelBuilder, AddMaxSpiderCountSettingField, ModelBuilder, AddManualMemberCheckVideoFlag, ModelBuilder, MainDbContextModelSnapshot

### Community 149 - "Notifications.cs"
Cohesion: 0.15
Nodes (15): CollectorRegistry, DateTime, List, string, YTChannelType, BannerChangeNotification, NotifyType, TwitchNoticeType (+7 more)

### Community 156 - ".Get"
Cohesion: 0.12
Nodes (14): DiscordStreamNotifyBot.SharedService, Emote, DiscordSocketClient, EmojiService, DateTimeOffset, Func, List, object (+6 more)

### Community 157 - "SendMsgToAllGuildService"
Cohesion: 0.17
Nodes (13): ButtonCheckData, DiscordStreamNotifyBot.Interaction.OwnerOnly.Service, IInteractionService, SendAllPayload, bool, DiscordSocketClient, Embed, Task (+5 more)

### Community 158 - ".CheckRequirementsAsync"
Cohesion: 0.22
Nodes (7): PreconditionAttribute, ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildAttribute

### Community 159 - ".Classify"
Cohesion: 0.23
Nodes (6): IEnumerable, YoutubeMemberProbeResultKind, Fact, InlineData, Theory, YoutubeMemberApiClientTests

### Community 160 - ".FixTCDbAsync"
Cohesion: 0.33
Nodes (5): Alias, Command, RequireContext, RequireOwner, Task

### Community 162 - ".MakeNamesUnique"
Cohesion: 0.31
Nodes (5): IEnumerable, int, IReadOnlyList, AutocompleteCandidate, AutocompleteSearch

### Community 164 - "DiscordStreamNotifyBot.Tests.Component.MySql"
Cohesion: 0.20
Nodes (5): DiscordStreamNotifyBot.Tests.Component.MySql, Fact, YoutubeMembershipSchemaContractTests, string, MySqlComponentFactAttribute

### Community 165 - ".LoadSnapshotAsync"
Cohesion: 0.24
Nodes (8): CancellationToken, ICollection, IEnumerable, Task, MemberEntitlementProvider, MemberRoleEntitlement, MemberRoleOwnershipPolicy, MemberRoleOwnershipService

### Community 166 - ".CheckRequirementsAsync"
Cohesion: 0.25
Nodes (6): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildMemberCountAttribute

### Community 167 - ".NotifyAddedAsync"
Cohesion: 0.27
Nodes (7): Components, Embed, MessageComponent, SocketGuild, Task, CrawlerOwnerNotifier, CrawlerPlatform

### Community 168 - "InteractionErrorPolicyTests"
Cohesion: 0.39
Nodes (4): InlineData, InteractionCommandError, Theory, InteractionErrorPolicyTests

### Community 176 - "TwitchSubscriptionPolicies.cs"
Cohesion: 0.08
Nodes (13): IQueryable, DateTimeOffset, IEnumerable, int, IReadOnlyCollection, TwitchAuthorizationEventPolicy, TwitchRateLimitPolicy, TwitchRefreshPersistencePolicy (+5 more)

### Community 178 - ".SetMemberCheckVideoIdAsync"
Cohesion: 0.06
Nodes (37): DiscordStreamNotifyBot.Command.Help, ICommandService, IEqualityComparer, Func, CommonEqualityComparer, Alias, Command, CommandInfo (+29 more)

### Community 179 - "YoutubeApiVideoPolicyTests"
Cohesion: 0.20
Nodes (9): YoutubeApiVideoAction, YoutubeApiVideoDecision, YoutubeApiVideoFacts, YoutubeApiVideoPolicy, DateTime, Fact, InlineData, Theory (+1 more)

### Community 180 - ".CheckMemberShipOnlyVideoIdAsync"
Cohesion: 0.15
Nodes (11): Task, YoutubeDetectionService, YoutubeMemberCandidateAction, YoutubeMemberCandidateFacts, YoutubeMemberChannelDecision, YoutubeMemberChannelFacts, YoutubeMemberVideoPolicy, Fact (+3 more)

### Community 182 - ".HandleStartLiveMessageAsync"
Cohesion: 0.17
Nodes (12): List, RedisValue, SemaphoreSlim, Task, TwitcastingDetectionService, IEnumerable, TwitcastingLiveStartAction, TwitcastingLiveStartFacts (+4 more)

### Community 184 - "YoutubeDetectionService"
Cohesion: 0.16
Nodes (12): ConcurrentBag, bool, ConcurrentDictionary, DateTime, HttpClient, IEnumerable, IHttpClientFactory, Task (+4 more)

### Community 185 - "YoutubeVideoIdParser"
Cohesion: 0.16
Nodes (7): string, Uri, YoutubeVideoIdParser, InlineData, string, Theory, YoutubeVideoIdParserTests

### Community 186 - "8. 分階段實作步驟"
Cohesion: 0.22
Nodes (9): 8. 分階段實作步驟, 階段 0：止血 PR — shard 歸屬守衛, 階段 1：Solution 骨架 + Shared, 階段 2：Notifier 上線（先維持單 shard 行為）, 階段 3：Scraper 拆出 + Redis Streams 匯流排（完成，正確性待測試環境驗）, 階段 4：Coordinator（完成，正確性待測試環境驗）, 階段 5：跨 shard 指令與共享狀態（完成，正確性待測試環境驗）, 階段 6：Docker 化與部署驗證（檔案完成，實跑待測試環境） (+1 more)

### Community 187 - ".GenerateSuggestionsAsync"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 188 - ".GenerateSuggestionsAsync"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 190 - ".GenerateSuggestionsAsync"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 191 - "YoutubeMemberVideoLogMessageFormatterTests"
Cohesion: 0.41
Nodes (5): YoutubeMemberVideoLogNotification, Fact, InlineData, Theory, YoutubeMemberVideoLogMessageFormatterTests

### Community 192 - "Category"
Cohesion: 0.70
Nodes (4): List, CategoriesJson, Category, SubCategory

### Community 193 - ".DecideAutomaticMutation"
Cohesion: 0.31
Nodes (4): YoutubeMemberAutomaticMutationAction, YoutubeMemberManualPinPolicy, Fact, YoutubeMemberManualPinPolicyTests

### Community 194 - "TwitchSubscriptionSetting"
Cohesion: 0.20
Nodes (11): AutocompletionResult, DefaultMemberPermissions, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IRole, IServiceProvider, SlashCommand (+3 more)

### Community 195 - "BotState"
Cohesion: 0.14
Nodes (11): TwitchGuildEligibilityStatus, ConnectionMultiplexer, IDatabase, ISubscriber, IUser, BotState, bool, InlineData (+3 more)

### Community 196 - "YoutubeDetectionService.cs"
Cohesion: 0.19
Nodes (10): DiscordStreamNotifyBot.SharedService.Youtube.Json, YoutubePubSubNotification, YTNotificationType, DateTime, List, Channel, EventLiver, Liver (+2 more)

### Community 197 - ".GenerateSuggestionsAsync"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 200 - "TcBackendStreamData.cs"
Cohesion: 0.44
Nodes (8): App, BackendMovie, Fmp4, Hls, Llfmp4, Streams, TcBackendStreamData, Webrtc

### Community 203 - "AGENTS.md"
Cohesion: 0.14
Nodes (7): 一、`claude` 分支是你最大的資產，也是最大的陷阱, 三、使用者已做的決策，不要重新辯論, 二、你在活的生產系統旁施工, 給未來 session 的信, 這套制度最可能的退化方式，與預防, License, 直播小幫手 [點我邀請到你的 Discord 內](https://discordapp.com/api/oauth2/authorize?client_id=758222559392432160&permissions=2416143425&scope=bot%20applications.commands)

### Community 204 - ".GenerateSuggestionsAsync"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 205 - "MainDbService"
Cohesion: 0.12
Nodes (17): DbContextOptions, Locale, TwitCasting, ComponentInteraction, GuildTwitchSubscriptionConfig, RequireContext, SlashCommand, Task (+9 more)

### Community 208 - ".CreateAsync"
Cohesion: 0.21
Nodes (9): Fact, SlashCommandParameterInfo, Task, Type, InteractionCommandContractTests, Fact, Task, InteractionCommandLocalizationTests (+1 more)

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

### Community 214 - ".Resolve"
Cohesion: 0.57
Nodes (3): InteractionCommandError, InteractionErrorDescriptor, InteractionErrorPolicy

### Community 215 - "YouTube 會員驗證架構重構計畫"
Cohesion: 0.15
Nodes (12): 11. 排程與生命週期, 12. Provider Result 分類, 17. Manual Acceptance Matrix, 18. 停機部署順序, 19. Completion Criteria, 1. 範圍, 20. 新 Session 執行規則, 2. 已定案決策 (+4 more)

### Community 217 - "NijisanjiLiverJson.cs"
Cohesion: 0.70
Nodes (4): Head, Images, NijisanjiLiverJson, SocialLinks

### Community 219 - ".GuildMemberCountPreconditionMapsValuesAndContactPath"
Cohesion: 0.33
Nodes (3): string, InteractionErrorCodes, Fact

### Community 220 - ".GenerateSuggestionsAsync"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 223 - "5. 目標架構"
Cohesion: 0.40
Nodes (5): 5.1 Console, 5.2 非容器檔案, 5.3 Loki, 5.4 `LOKI_URL` 相容性, 5. 目標架構

### Community 225 - ".LockGuildAsync"
Cohesion: 0.20
Nodes (13): LeaseGroup, CancellationToken, DiscordSocketClient, IServiceProvider, SocketGuild, Task, UtilityService, CancellationToken (+5 more)

### Community 226 - "15. 實作階段"
Cohesion: 0.20
Nodes (10): 15. 實作階段, Phase 0：Baseline 與 characterization, Phase 1：Schema 與 migration, Phase 2：共用操作與 role ownership, Phase 3：YouTube interaction 與 state machine, Phase 4：Role/config durability, Phase 5：Provider 與 lifecycle, Phase 6：Backend (+2 more)

### Community 227 - "Migration"
Cohesion: 0.40
Nodes (3): Migration, MigrationBuilder, AddGoogleOAuthUnlinkIntent

### Community 235 - "YouTube 會員驗證"
Cohesion: 0.33
Nodes (5): Durable state, YouTube 會員驗證, 使用者契約, 服務邊界, 部署前驗證

### Community 237 - ".GenerateSuggestionsAsync"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 238 - "14. Frontend"
Cohesion: 0.40
Nodes (5): 14.1 TypeScript contract, 14.2 GoogleSection, 14.3 VerifyWindow, 14.4 Copy/Privacy, 14. Frontend

### Community 239 - "8. DB Schema"
Cohesion: 0.40
Nodes (5): 8.1 Entity changes, 8.2 Indexes, 8.3 Migration 規則, 8.4 Preflight 查詢, 8. DB Schema

### Community 242 - "TwitchBroadcasterAuthorization"
Cohesion: 0.23
Nodes (8): TwitchUserState, DateTime, TwitchReconcilePolicy, DateTime, TwitchBroadcasterAuthorization, DateTime, TwitchSpider, TwitchSpiderMetricMode

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
Cohesion: 0.21
Nodes (9): DiscordStreamNotifyBot.Interaction.Twitch, GuildNoticeTwitchChannelIdAutocompleteHandler, CommandExample, CommandSummary, SlashCommand, Task, TwitchService, GuildTwitchSpiderAutocompleteHandler (+1 more)

### Community 257 - "DiscordStreamNotifyBot.Tests"
Cohesion: 0.15
Nodes (6): DiscordStreamNotifyBot.Scraper.Detection.Youtube, DiscordStreamNotifyBot.Tests, DiscordStreamNotifyBot.Scraper.Detection.Twitch, Fact, NotificationBusConsumerOptionsTests, YoutubeMemberSafeLoggingTests

## Knowledge Gaps
- **484 isolated node(s):** `net8.0`, `prometheus-net.AspNetCore (8.2.1)`, `Microsoft.NET.Sdk`, `DiscordStreamNotifyBot.Command.Normal`, `DiscordStreamNotifyBot.Command.TwitCasting` (+479 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **37 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `DiscordStreamNotifyBot.DataBase.Table` connect `DiscordStreamNotifyBot.DataBase.Table` to `.GetDbContext`, `.Warn`, `DiscordStreamNotifyBot.SharedService.Twitch`, `DiscordStreamNotifyBot.Tests`, `BotLocalizer`, `DiscordStreamNotifyBot.Shared.Messages`, `Extensions`, `TwitchSubscriptionService`, `AuthTokenTests`, `YoutubeMemberPolicies`, `DiscordStreamNotifyBot.Tests.Component.MySql`, `YoutubeMemberRoleService`, `GuildLocaleService`, `MySqlComponentFixture`, `TwitchSubscriptionPolicies.cs`, `TwitchStateDecisions.cs`, `.HandleStartLiveMessageAsync`, `TwitchRefreshRotationLifecycle`, `DiscordStreamNotifyBot.Shared`, `Bot`, `DiscordStreamNotifyBot.DataBase`, `BotState`, `DiscordStreamNotifyBot.Command.Attribute`, `.CreateOrRepairConfigurationAsync`?**
  _High betweenness centrality (0.076) - this node is a cross-community bridge._
- **Why does `MainDbService` connect `MainDbService` to `.GetDbContext`, `.Warn`, `ClusterQueryService`, `TwitchSubscriptionService`, `.HandleTwitcastingAsync`, `.GetLocaleAsync`, `TwitcastingService`, `YoutubeMemberAuthorizationService`, `AuthTokenTests`, `SendMsgToAllGuildService`, `YoutubeMemberService`, `.LoadSnapshotAsync`, `DiscordStreamNotifyBot.Interaction.Attribute`, `YoutubeMemberRoleService`, `TwitchDetectionService`, `Twitch`, `MySqlComponentFixture`, `Administration`, `.SetMemberCheckVideoIdAsync`, `YoutubeMemberSetting`, `Utility`, `.HandleStartLiveMessageAsync`, `YoutubeDetectionService`, `YoutubeStreamService`, `Bot`, `DiscordStreamNotifyBot.DataBase`, `TwitchSubscriptionSetting`, `BotState`, `AdminSettingsService`, `YoutubeStream`, `AdminSettingsMutationResult`, `YoutubeApiService`, `.GetGuildNameMapAsync`, `.LockGuildAsync`, `.CreateOrRepairConfigurationAsync`, `.SendLocalizedConfirmAsync`, `TwitchSpider`?**
  _High betweenness centrality (0.064) - this node is a cross-community bridge._
- **Why does `DiscordStreamNotifyBot.DataBase` connect `DiscordStreamNotifyBot.DataBase` to `.GetDbContext`, `.Warn`, `DiscordStreamNotifyBot.SharedService.Twitch`, `20260721095646_AddLocalizationSettings.Designer.cs`, `20260813032017_RenameVerificationLogChannel.Designer.cs`, `ClusterQueryService`, `DiscordStreamNotifyBot.Shared.Messages`, `Extensions`, `TwitchSubscriptionService`, `DiscordStreamNotifyBot.Migrations`, `YoutubeMemberAuthorizationService`, `AuthTokenTests`, `.Get`, `SendMsgToAllGuildService`, `20260611015819_SyncModelDrift.Designer.cs`, `DiscordStreamNotifyBot.DataBase.Table`, `20260719142803_AddTwitchBroadcasterAuthorization.Designer.cs`, `.LoadSnapshotAsync`, `DiscordStreamNotifyBot.Interaction.Attribute`, `YoutubeMemberRoleService`, `GuildLocaleService`, `MySqlComponentFixture`, `.RetryWithBackoffAsync`, `DiscordStreamNotifyBot.Shared`, `Bot`, `BotState`, `YoutubeDetectionService.cs`, `20260803141135_AddTwitchSubscriptionVerification.Designer.cs`, `YoutubeApiService`, `.GetGuildNameMapAsync`, `20250320095452_RefactorDbContext.Designer.cs`, `20250603065853_ModifyTwitCastingTable.Designer.cs`, `20260804173737_AddYoutubeMemberVerificationDurability.Designer.cs`, `DiscordStreamNotifyBot.Command.Attribute`, `20260807045351_AddGoogleOAuthUnlinkIntent.Designer.cs`, `.CreateOrRepairConfigurationAsync`, `20260803165758_AddTwitchSubscriptionDeletionPending.Designer.cs`, `DiscordStreamNotifyBot.HttpClients.Twitcasting.Model`, `TwitchSpider`?**
  _High betweenness centrality (0.059) - this node is a cross-community bridge._
- **What connects `net8.0`, `prometheus-net.AspNetCore (8.2.1)`, `Microsoft.NET.Sdk` to the rest of the system?**
  _484 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `TwitchSubscriptionApiClient` be split into smaller, more focused modules?**
  _Cohesion score 0.14 - nodes in this community are weakly interconnected._
- **Should `.Warn` be split into smaller, more focused modules?**
  _Cohesion score 0.12727272727272726 - nodes in this community are weakly interconnected._
- **Should `DiscordStreamNotifyBot.SharedService.Twitch` be split into smaller, more focused modules?**
  _Cohesion score 0.11578947368421053 - nodes in this community are weakly interconnected._