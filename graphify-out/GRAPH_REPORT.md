# Graph Report - DiscordStreamNotifyBot  (2026-09-19)

## Corpus Check
- 379 files · ~201,355 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 6225 nodes · 13213 edges · 358 communities (329 shown, 18 thin omitted)
- Extraction: 91% EXTRACTED · 9% INFERRED · 0% AMBIGUOUS · INFERRED: 1233 edges (avg confidence: 0.82)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `9a1309c3`
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
- .TryParse
- YoutubeReminderPolicyTests
- FakeTimeProvider
- DiscordStreamNotifyBot.Shared.Messages
- Extensions
- TwitchSubscriptionService
- Extensions
- .BuildSnapshotAsync
- 會限 OAuth Token 儲存改走 MySQL（去 Redis 依賴）計畫
- InMemoryValidatorStore
- MainDbContext
- .CreateService
- YoutubeMemberAuthorizationService
- Log
- .RetryWithBackoffAsync
- Twitch 訂閱驗證實作計畫
- YoutubeMemberCheck
- 新增 TwitCasting 錄影委派計畫（小幫手 ↔ StreamRecordTools）
- 多語系支援計畫
- Serilog Logging 遷移計畫
- 12. 分階段執行
- ReminderItem
- .Main
- YoutubeMemberLifecycleTaskRegistry
- .FailedActionRemainsRetryableAndReportedFailuresPreventAcknowledgment
- 直播小幫手
- NotificationEmbedFactoryTests
- TwitchOAuthRefreshLockLease
- .BuildVariant
- ClusterQueryService
- YoutubeMemberRoleService
- TwitchDetectionService
- .GetDbContext
- ScraperMetrics
- GuildLocaleService
- TwitchChannelUpdateInfo
- RedisChannels
- ChzzkService
- YoutubeMemberService
- 網頁管理設定：30 秒請求與背景清理實作計畫
- Administration
- 網頁管理設定中心：爬蟲與會員驗證實作計畫
- 水平擴展（三層拆分）計畫 — Redis Streams 版
- YoutubeMemberSetting
- NoticeCache
- YoutubeVideoClaimCache
- .SetVerificationLogChannelAsync
- AdministrationService
- 直播小幫手 Bot 協作說明
- TwitchRefreshRotationLifecycle
- CHZZK 錄影整合計畫
- DiscordStreamNotifyBot.Shared
- YoutubeStreamService
- graphify reference: extra exports and benchmark
- Bot
- DiscordStreamNotifyBot.DataBase.Table
- .HoloScheduleAsync
- .FilterNoNotifyGuilds
- .Main
- AddManualMemberCheckVideoFlag
- .VideoLookupSearchesAllFourTables
- NotificationContractTests
- EF Core 遷移與基線化（本專案版）
- AdminSettingsSnapshot
- 11. 通知與背景訊息
- GoogleOAuthOperationLockLease
- TwitchStreamLifecycleDecisionTests
- FUNDING.yml (Patreon / ECPay / PayPal)
- Build workflow (SonarQube analysis)
- YoutubeWebSubContractTests
- Notifier Bot Logo — interlocking chain-link icon, purple-to-magenta-to-red gradient on light grey circle; flat modern vector branding representing the linking/notification identity of the Discord stream-notify bot
- YoutubeStream
- Broadcaster
- .RequestCoreAsync
- AdminSettingsChannel
- YoutubeDetectionService
- graphify reference: query, path, explain
- 自動化測試導入計畫
- YoutubeApiService
- ChzzkRecordService
- .Get
- .Info
- .CreateAsyncClient
- EmbedBuilderFactory
- YoutubeWebSubRequestResult
- .AssertKeysAbsentAsync
- graphify reference: add a URL and watch a folder
- graphify reference: commit hook and native CLAUDE.md integration
- graphify reference: incremental update and cluster-only
- YoutubeTerminalEventRegistry
- graphify reference: GitHub clone and cross-repo merge
- graphify reference: transcribe video and audio
- 網頁管理設定中心實作計畫
- BotConfig
- ChzzkClient
- .claude/CLAUDE.md (graphify trigger)
- DiscordWebhookClient
- YoutubeAtomFallback
- Confidence rubric (EXTRACTED/INFERRED/AMBIGUOUS)
- AST structural extraction (Part A)
- Community detection & clustering
- God nodes & surprising connections
- Knowledge graph (graph.json)
- Semantic extraction (parallel subagents)
- .CreateOrRepairConfigurationAsync
- AdminSettingsCrawlerPlatform
- DiscordStreamNotifyBot.Notifier.csproj
- TwitchSpider
- Normal
- ClusterService
- TwitchService
- Twitch OAuth 與零成本 EventSub 實作計畫
- TwitchEventSubEnsureResult
- YoutubeMemberProbeResultKind
- AutocompleteCandidate
- 16. 執行階段
- Prometheus / Grafana 監控
- TwitcastingClient
- DiscordStreamNotifyBot.Scraper.csproj
- DiscordStreamNotifyBot.Tests.csproj
- 17. 驗證矩陣
- ChzzkNotification
- SendMsgToAllGuildService
- 7. OAuth API 與流程隔離
- TwitcastingLiveStartPlannerTests
- .BuildResponseJsonAsync
- .Plan
- HelpDescription (bot feature summary)
- Migration
- 11. Bot EventSub 與偵測
- 15. 預期修改檔案
- 2. 現況基線
- 5. Guild 資格與 OAuth 豁免
- DiscordStreamNotifyBot.sln
- .GetCommandPath
- 13. Prometheus
- 4. 安全刪除狀態機
- TwitchNotification
- .Plan
- MainDbContextModelSnapshot.cs
- DiscordStreamNotifyBot.Tests
- .StartAndBlockAsync
- ModifyTwitCastingTable
- AddMaxSpiderCountSettingField
- DiscordStreamNotifyBot.Migrations
- AddTwitchBroadcasterAuthorization
- AddLocalizationSettings
- MemberRoleOwnershipSnapshot
- .Reconcile
- DbEntity
- NonPersistentGoogleDataStore
- Movie
- BotState
- AddTwitchSubscriptionVerification
- AddTwitchSubscriptionDeletionPending
- RedisComponentFixture
- AddYoutubeMemberVerificationDurability
- .CheckRequirementsAsync
- .BuildAddedMessage
- AddGoogleOAuthUnlinkIntent
- RenameVerificationLogChannel
- DebounceFixture
- GuildYoutubeMemberConfig
- .FailedSendKeepsRenewalDueUntilChallengeConfirms
- .Parse
- graphify.js
- YoutubeNotification
- Fact
- NotificationMetricEvent
- Twitch
- .Classify
- .CheckMemberShipOnlyVideoIdAsync
- TwitchStateDecisions.cs
- YoutubeMemberVideoLogNotification
- YoutubeWebSubPendingAction
- YoutubePubSubNotification
- MigrationAndConstraintTests
- InteractionMetadataFixture
- YoutubeAtomValidatorStore
- CommonEqualityComparer
- .Start
- StubHandler
- TwitchReconcileDecisionTests
- Category
- DiscordStreamNotifyBot.SharedService.YoutubeMember
- .GenerateSuggestionsAsync
- SharedExtensions
- NijisanjiStreamJson
- .Init
- AdminYoutubeMessagesPayload
- .CheckPermissionsAsync
- TcBackendStreamData.cs
- ChzzkPollPolicyTests
- .PublishAsync
- RedisConnection
- InteractionErrorPolicyTests
- MainDbService
- .SendErrorMessageAsync
- TwitchSpiderRemovalAction
- .CreateAsync
- .CheckRequirementsAsync
- .CheckRequirementsAsync
- .CheckPermissionsAsync
- Log 與 Loki
- .Resolve
- .PublishAsync
- YouTube 會員驗證架構重構計畫
- AdminSettingsYoutubeVerification
- TwitchAuthorizationLocalState
- NotifierMetrics
- Movie
- Utility
- .SlashCommandExecuted
- UtilityService
- .ValidateCommandLocalizationResources
- 5. 語系模型與解析規則
- DiscordStreamNotifyBot.SharedService.Chzzk
- 15. 實作階段
- TwitcastingService
- AddChzzkAutoRecord
- AdminSettingsYoutubeNotification
- .LoadCommandFrom
- Broadcaster
- TwitchBroadcasterAuthorization
- .Valid
- opencode.json
- YouTube 會員驗證
- GuildSnapshot
- CHZZK 直播通知實作計畫
- 14. Frontend
- 8. DB Schema
- TwitchAccessTokenData
- TwitcastingStream
- MySqlDataStoreTests
- RedisContractTests
- 13. Backend Contract
- 16. 驗證命令
- .LockGuildAsync
- TwitchSubscriptionStatus
- 10. Slash 與 Interaction Cutover
- 6. 目標架構
- 7. 狀態機
- 9. Role 隔離政策
- TwitchEventSubMetricStatus
- DiscordStreamNotifyBot.Scraper.Detection.Twitcasting
- AdminSettingsNotifications
- BotRole
- YoutubeMemberService.cs
- TwitcastingSpider
- YoutubeMemberPolicies.cs
- NoticeChzzkStreamChannel
- 13. 驗證矩陣
- ChzzkStreamIdentityTests
- TwitchStream
- YoutubeMemberVerificationResult
- NoticeYoutubeStreamChannel
- AdminSettings.cs
- 7. 分階段執行
- YouTube WebSub 修正與 Atom fallback 實作計畫
- TwitchSpider
- TwitchSubscriptionCheck
- GuildConfig
- AdminSettingsTwitchVerification
- NotificationDeliveryResult
- TwitchReconcileAction
- DiscordStreamNotifyBot.DataBase
- GracefulShutdown
- ChzzkPollAction
- .AddChannel
- .AddChannel
- YoutubeChannelSpider
- MySqlComponentFixture
- PreconditionAttribute
- YoutubeAtomParseResult
- NotificationChannelIssue
- AdminSettingsRole
- NoticeTwitchStreamChannel
- YoutubeChannelOwnedType
- ChzzkStream
- AdminSettingsYoutubeMessages
- MySqlDataStore
- MySqlComponentFixture.cs
- ReactionEventWrapper
- YoutubeNoticeType
- DescriptionOnlyLocalizationManager
- GuildInfoResponse
- GuildTwitchSubscriptionConfig
- TwitchRefreshPersistenceDecision
- 8. WebSub 詳細修正
- TwitchProviderResultStatus
- .HandleStartLiveMessageAsync
- AdminSettingsTwitcastingNotification
- MainDbContextFactory
- NotifierMetrics.cs
- TwitchRoleConfigurationResult
- NijisanjiLiverJson
- YoutubeMemberAccessToken
- .FixTCDbAsync
- 13. 實作與驗證現況（2026-09-15）
- 6. 資源架構
- .PublishRecordRequestAsync
- ChzzkSpider
- AddChzzkNotification
- .SendMessageToAllGuildAsync
- AdminSettingsCommandReply
- 10. 實作階段
- 9. Atom fallback 詳細設計
- Video
- .SendStreamMessageAsync
- .SaveVideosByType
- BotPlayingStatus
- .OnlyAffiliateAndPartnerCanBeConfigured
- TwitchReconcileRequestedPayload
- YoutubeMemberCandidateAction
- TwitchStreamEventPayload
- all.sql
- TwitchAuthorizationChangedPayload
- GoogleOAuthUnlinkIntent
- InviteResponse
- _Baseline_ExistingDb.sql
- `guild_config`
- 6. 目標流程
- .ParseToTimeSpan
- 5. 持久化、發布與去重
- .AllRegisteredCommandsHaveDescriptionsInEverySupportedLocale
- 12. 測試矩陣
- 4. 現況基線與已確認問題
- AdminSettingsCommon
- 11. 實際修改位置
- YoutubeMemberTokenCleanupConcurrencyTests
- 14. 部署與回滾
- 14. 上線順序與回復
- 17. 實際驗證結果
- 7. Redis 契約
- TwitchAuthorizationChangedPayload
- TwitchAppAccessTokenResponse
- RecordedRequest
- .CreateClient

## God Nodes (most connected - your core abstractions)
1. `MainDbContext` - 83 edges
2. `DiscordStreamNotifyBot.DataBase.Table` - 79 edges
3. `YoutubeDetectionService` - 77 edges
4. `DiscordStreamNotifyBot.Shared` - 75 edges
5. `DiscordStreamNotifyBot.DataBase` - 72 edges
6. `YoutubeMemberService` - 65 edges
7. `TwitchDetectionService` - 63 edges
8. `BotConfig` - 60 edges
9. `DiscordStreamNotifyBot.Tests` - 59 edges
10. `MainDbService` - 57 edges

## Surprising Connections (you probably didn't know these)
- `DebounceFixture` --references--> `UserLogin`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/TwitchChannelUpdateDebounceTests.cs → src/DiscordStreamNotifyBot.Shared/Messages/Notifications.cs
- `DebounceFixture` --references--> `UserName`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/TwitchChannelUpdateDebounceTests.cs → src/DiscordStreamNotifyBot.Shared/Messages/Notifications.cs
- `InteractionMetadataFixture` --references--> `InteractionHandler`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/InteractionMetadataFixture.cs → src/DiscordStreamNotifyBot.Notifier/Interaction/InteractionHandler.cs
- `NotificationEmbedFactoryTests` --references--> `BotLocalizer`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/NotificationEmbedFactoryTests.cs → src/DiscordStreamNotifyBot.Notifier/Localization/BotLocalizer.cs
- `YoutubeMemberVideoLogMessageFormatterTests` --references--> `BotLocalizer`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/YoutubeMemberVideoLogMessageFormatterTests.cs → src/DiscordStreamNotifyBot.Notifier/Localization/BotLocalizer.cs

## Import Cycles
- None detected.

## Communities (358 total, 18 thin omitted)

### Community 0 - "TwitchSubscriptionApiClient"
Cohesion: 0.12
Nodes (18): CancellationToken, DateTimeOffset, HttpResponseMessage, IHttpClientFactory, NotifierMetrics, Task, TwitchProviderResult, Status (+10 more)

### Community 1 - ".GetLocaleAsync"
Cohesion: 0.15
Nodes (24): InteractionModuleBase, BotLocalizer, CommandDisplayResolver, GuildLocaleService, LocaleResolver, Task, TopLevelModule, BotLocalizer (+16 more)

### Community 2 - ".Warn"
Cohesion: 0.21
Nodes (9): PendingRefreshPersistence, CancellationToken, DateTime, NotifierMetrics, Task, TimeSpan, TwitchBroadcasterAuthorization, PendingRefreshPersistence (+1 more)

### Community 3 - "EmojiService"
Cohesion: 0.20
Nodes (7): DiscordStreamNotifyBot.SharedService, Emote, DiscordSocketClient, EmojiService, ECPayEmote, PayPalEmote, YouTubeEmote

### Community 4 - "DiscordStreamNotifyBot.Shared.csproj"
Cohesion: 0.08
Nodes (23): Microsoft.EntityFrameworkCore.Design (9.0.3), Microsoft.EntityFrameworkCore.Relational (9.0.3), Microsoft.EntityFrameworkCore.Tools (9.0.3), Serilog (4.4.0), Serilog.Sinks.Console (6.1.1), Serilog.Sinks.File (7.0.0), Serilog.Sinks.Grafana.Loki (9.0.1), net8.0 (+15 more)

### Community 5 - "TwitchApiService"
Cohesion: 0.10
Nodes (24): CancellationToken, Clip, DateTime, EventSubSubscription, HttpClient, IReadOnlyList, Lazy, SemaphoreSlim (+16 more)

### Community 6 - "InteractionHandler"
Cohesion: 0.10
Nodes (20): DisplayName, Value, BotLocalizer, ChoiceDisplayAttribute, CommandDisplayResolver, DiscordSocketClient, Func, GuildLocaleService (+12 more)

### Community 7 - "偵測 → 匯流排 → 發送 路徑除錯"
Cohesion: 0.13
Nodes (13): 1. Shared — 定義契約, 2. Scraper — 偵測並 publish, 3. Notifier — 消費並發送, 動工前先讀一個既有平台, 收尾檢查, 新增偵測平台 / 通知事件, 步驟（依相依順序，Shared → Scraper → Notifier）, 偵測 → 匯流排 → 發送 路徑除錯 (+5 more)

### Community 8 - "AuthTokenTests"
Cohesion: 0.15
Nodes (11): TokenCrypto, TokenManager, ArgumentException, Fact, InlineData, Theory, AuthTokenTests, TokenPayload (+3 more)

### Community 9 - ".TryParse"
Cohesion: 0.21
Nodes (7): Uri, YoutubeVideoIdParser, ArgumentNullException, InlineData, Theory, YoutubeVideoIdParserTests, UriFormatException

### Community 10 - "YoutubeReminderPolicyTests"
Cohesion: 0.10
Nodes (21): DateTime, TimeSpan, YoutubeReminderApiAction, TreatAsStarted, TreatAsTimeChanged, YoutubeReminderBatchChangeAction, PublishAndReplaceTimer, PublishAndRunImmediately (+13 more)

### Community 11 - "FakeTimeProvider"
Cohesion: 0.24
Nodes (10): FakeTimeProvider, CancellationToken, Func, Task, TimeProvider, TimeSpan, PeriodicRunner, Fact (+2 more)

### Community 12 - "DiscordStreamNotifyBot.Shared.Messages"
Cohesion: 0.11
Nodes (11): DiscordStreamNotifyBot.SharedService.AdminSettings, DiscordStreamNotifyBot.Interaction.ServerAdministration, DiscordStreamNotifyBot.SharedService.Twitcasting, DiscordStreamNotifyBot.Interaction.Utility.Service, DiscordStreamNotifyBot.Localization, DiscordStreamNotifyBot.Interaction.Utility, DiscordStreamNotifyBot.Command.Admin, DiscordStreamNotifyBot.SharedService.Cluster (+3 more)

### Community 13 - "Extensions"
Cohesion: 0.09
Nodes (20): ManagementBaseObject, Process, BotLocalizer, DiscordSocketClient, EmbedBuilder, Func, GuildLocaleService, IDiscordInteraction (+12 more)

### Community 14 - "TwitchSubscriptionService"
Cohesion: 0.10
Nodes (26): BotLocalizer, CancellationToken, CancellationTokenSource, ConcurrentDictionary, DateTimeOffset, DiscordSocketClient, GuildLocaleService, NotifierMetrics (+18 more)

### Community 15 - "Extensions"
Cohesion: 0.05
Nodes (37): DiscordStreamNotifyBot.Command, ModuleBase, SocketCommandContext, SocketMessage, CommandService, DiscordSocketClient, IServiceProvider, Task (+29 more)

### Community 16 - ".BuildSnapshotAsync"
Cohesion: 0.07
Nodes (24): ActionRowComponent, RequestRoute, CancellationToken, DiscordSocketClient, Func, GuildLocaleService, JObject, SocketGuild (+16 more)

### Community 17 - "會限 OAuth Token 儲存改走 MySQL（去 Redis 依賴）計畫"
Cohesion: 0.11
Nodes (18): Backend, Bot（本 repo）, MySQL（兩端都已連同一個庫）, 儲存層（現況為 Redis）, 加密與 blob 格式（兩端一致）, 加密金鑰處理, 影響檔案一覽, 待決策（給實作 session） (+10 more)

### Community 18 - "InMemoryValidatorStore"
Cohesion: 0.19
Nodes (20): InMemoryValidatorStore, RecordedRequest, StubHandler, CancellationToken, Dictionary, ETag, Fact, Func (+12 more)

### Community 19 - "MainDbContext"
Cohesion: 0.04
Nodes (52): BannerChange, DbContext, GoogleOAuthUnlinkIntent, RecordYoutubeChannel, ChzzkSpider, DbSet, GuildConfig, GuildTwitchSubscriptionConfig (+44 more)

### Community 20 - ".CreateService"
Cohesion: 0.23
Nodes (11): GuildLocaleRequest, ArgumentException, Fact, Func, GuildLocaleService, InvalidOperationException, IReadOnlyCollection, IReadOnlyDictionary (+3 more)

### Community 21 - "YoutubeMemberAuthorizationService"
Cohesion: 0.18
Nodes (10): GoogleAuthorizationCodeFlow, CancellationToken, HttpClient, MySqlDataStore, Task, YoutubeMemberAuthorizationService, IsConfigured, YoutubeMemberTokenSnapshot (+2 more)

### Community 22 - "Log"
Cohesion: 0.05
Nodes (36): ConsoleColor, DelegatingHandler, ILogEventSink, ITextFormatter, LogEvent, LogEventLevel, LogFileRoute, Logger (+28 more)

### Community 23 - ".RetryWithBackoffAsync"
Cohesion: 0.22
Nodes (11): Func, Task, TimeProvider, TimeSpan, StartupPreflight, DateTimeOffset, Fact, InvalidOperationException (+3 more)

### Community 24 - "Twitch 訂閱驗證實作計畫"
Cohesion: 0.05
Nodes (36): 10. Frontend 調整, 11. 安全與錯誤處理, 12.1 Backend, 12.2 Bot, 12.3 Frontend, 12. 自動化測試, 13. 手動驗收, 14. 實作順序 (+28 more)

### Community 25 - "YoutubeMemberCheck"
Cohesion: 0.07
Nodes (22): ComponentInteraction, Task, IEnumerable, IReadOnlyCollection, IReadOnlyList, YoutubeMemberPolicies, YoutubeMemberSelectionTransition, DateTime (+14 more)

### Community 26 - "新增 TwitCasting 錄影委派計畫（小幫手 ↔ StreamRecordTools）"
Cohesion: 0.11
Nodes (17): 1. 背景與動機, 2. 新增跨 repo 契約, 3. A（小幫手）改動, 4. B（StreamRecordTools）改動, 5. 部署順序與相容性, 6. 驗證, 7. 影響範圍, A1. `Shared/RedisChannels.cs` (+9 more)

### Community 27 - "多語系支援計畫"
Cohesion: 0.09
Nodes (23): 10.1 共用回覆 API, 10.2 Precondition 與 handler 錯誤, 10.3 例外訊息, 10.4 第一階段模組, 10. 執行期互動本地化, 15. 預期修改檔案, 16. 完成定義, 1. 背景 (+15 more)

### Community 28 - "Serilog Logging 遷移計畫"
Cohesion: 0.10
Nodes (20): 10. 預期修改檔案, 11. 完成定義, 1. 背景, 2. 目標, 3. 非目標, 4. 技術選型, 5.1 Console, 5.2 非容器檔案 (+12 more)

### Community 29 - "12. 分階段執行"
Cohesion: 0.22
Nodes (9): 12. 分階段執行, 階段 0：建立基準與字串清冊, 階段 1：Localization 基礎與繁中資源化, 階段 2：資料庫與語系設定, 階段 3：Slash command 註冊本地化, 階段 4：共用互動、Help 與首次設定, 階段 5：一般 Interaction 模組, 階段 6：背景通知與會限 DM (+1 more)

### Community 30 - "ReminderItem"
Cohesion: 0.27
Nodes (8): YTChannelType, ReminderItem, ChannelType, StreamVideo, Timer, ConcurrentDictionary, Fact, YoutubeReminderRegistryTests

### Community 31 - ".Main"
Cohesion: 0.10
Nodes (16): DiscordStreamNotifyBot.Coordinator, Counter, Gauge, HashSet, StreamGroupInfo, CoordinatorMetrics, CancellationToken, ClusterService (+8 more)

### Community 32 - "YoutubeMemberLifecycleTaskRegistry"
Cohesion: 0.13
Nodes (12): ConcurrentDictionary, DateTime, IEnumerable, Task, TimeSpan, YoutubeMemberLifecyclePolicy, YoutubeMemberLifecycleTaskRegistry, Func (+4 more)

### Community 33 - ".FailedActionRemainsRetryableAndReportedFailuresPreventAcknowledgment"
Cohesion: 0.16
Nodes (13): DispatchProxy, MethodInfo, Open, Stub, Fact, Func, IMessage, IMessageChannel (+5 more)

### Community 34 - "直播小幫手"
Cohesion: 0.13
Nodes (11): MIT License, CHZZK 錄影, Docker Compose 部署, 授權, 服務組成, 本機建置與執行, 直播小幫手, 相關文件 (+3 more)

### Community 35 - "NotificationEmbedFactoryTests"
Cohesion: 0.24
Nodes (8): Color, DateTime, Embed, Fact, InlineData, TableVideo, Theory, NotificationEmbedFactoryTests

### Community 36 - "TwitchOAuthRefreshLockLease"
Cohesion: 0.08
Nodes (30): CancellationToken, CancellationTokenSource, IConnectionMultiplexer, IDatabase, RedisKey, RedisValue, Task, TimeSpan (+22 more)

### Community 37 - ".BuildVariant"
Cohesion: 0.16
Nodes (15): BotLocalizer, DateTime, EmbedBuilder, IReadOnlyCollection, TimeSpan, TwitchEmbedBuilderFactory, Embed, MessageComponent (+7 more)

### Community 38 - "ClusterQueryService"
Cohesion: 0.07
Nodes (36): ComponentInteraction, NotificationChannelCheckResponse, Task, AdministrationComponent, AutocompletionResult, ChzzkService, CommandExample, CommandSummary (+28 more)

### Community 39 - "YoutubeMemberRoleService"
Cohesion: 0.21
Nodes (12): CancellationToken, DiscordSocketClient, GuildYoutubeMemberConfig, IEnumerable, IRole, SocketGuild, Task, YoutubeMemberRoleConfigurationResult (+4 more)

### Community 40 - "TwitchDetectionService"
Cohesion: 0.08
Nodes (30): ChannelUpdate, HelixStream, CancellationTokenSource, DateTime, EventSubSubscription, IReadOnlyCollection, IReadOnlyDictionary, RedisValue (+22 more)

### Community 41 - ".GetDbContext"
Cohesion: 0.11
Nodes (17): SocketGuild, CancellationToken, IEnumerable, SocketGuild, Task, YoutubeWebSubRequestResult, CancellationToken, EmbedBuilder (+9 more)

### Community 42 - "ScraperMetrics"
Cohesion: 0.07
Nodes (31): Counter, Gauge, ScraperMetricResult, Failure, Success, ScraperMetrics, TwitchAuthorizationChangeMetricResult, Authorized (+23 more)

### Community 43 - "GuildLocaleService"
Cohesion: 0.07
Nodes (28): CacheEntry, CultureInfo, CancellationToken, ConcurrentDictionary, DateTimeOffset, Dictionary, Func, GuildConfig (+20 more)

### Community 44 - "TwitchChannelUpdateInfo"
Cohesion: 0.13
Nodes (14): CancellationTokenRegistration, DebouncedEventArgs, Debouncer, ObjectDisposedException, Func, IReadOnlyCollection, Task, DebounceChannelUpdateMessage (+6 more)

### Community 45 - "RedisChannels"
Cohesion: 0.08
Nodes (12): AdminSettings, Chzzk, Cluster, Member, Notifier, OAuth, RedisChannels, SharedState (+4 more)

### Community 46 - "ChzzkService"
Cohesion: 0.11
Nodes (20): SocketGuild, Task, BotLocalizer, CancellationToken, DiscordSocketClient, EmojiService, GuildLocaleService, NoticeCache (+12 more)

### Community 47 - "YoutubeMemberService"
Cohesion: 0.10
Nodes (27): CheckId, Snapshot, SocketRole, SocketTextChannel, CancellationToken, SocketGuild, Task, YoutubeMemberNotMemberApplyResult (+19 more)

### Community 48 - "網頁管理設定：30 秒請求與背景清理實作計畫"
Cohesion: 0.10
Nodes (19): 10. 實作順序, 11. 不在本次實作, 1. 目標, 2. 已確認決策, 3. 端點範圍與 deadline, 4. Cross-project contract, 5.1 Controller, 5.2 Redis bridge (+11 more)

### Community 49 - "Administration"
Cohesion: 0.32
Nodes (12): GuildInfoResponse, InviteResponse, Alias, Command, DiscordSocketClient, NotificationChannelCheckResponse, RequireContext, RequireOwner (+4 more)

### Community 50 - "網頁管理設定中心：爬蟲與會員驗證實作計畫"
Cohesion: 0.05
Nodes (40): 10.1 授權, 10.2 爬蟲, 10.3 YouTube 會員驗證, 10.4 Twitch 訂閱驗證, 10. 手動驗收矩陣, 11. 實作順序, 12. 完成閘門, 13. 新 Session 交接指令 (+32 more)

### Community 51 - "水平擴展（三層拆分）計畫 — Redis Streams 版"
Cohesion: 0.05
Nodes (41): 10. 可優化項目（claude 分支已有成品，對應階段順手移植）, 11. 驗證清單（部署前全過）, 1. 目標架構, 2.1 `Shared`（共用 library）, 2.2 `Scraper`（爬蟲層，叢集唯一）, 2.3 `Notifier`（通知層 / shard，可多個）, 2.4 `Coordinator`（主控層，1 個）, 2.5 SharedService 逐服務拆分歸屬（判斷準則表） (+33 more)

### Community 52 - "YoutubeMemberSetting"
Cohesion: 0.17
Nodes (16): AutocompletionResult, CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, GuildYoutubeMemberConfig, IAutocompleteInteraction, IInteractionContext (+8 more)

### Community 53 - "NoticeCache"
Cohesion: 0.21
Nodes (10): DateTimeOffset, Func, List, TimeProvider, TimeSpan, NoticeCache, Fact, InvalidOperationException (+2 more)

### Community 54 - "YoutubeVideoClaimCache"
Cohesion: 0.15
Nodes (13): Batch, ConcurrentDictionary, DateTimeOffset, Dictionary, TimeProvider, TimeSpan, Batch, YoutubeVideoClaimCache (+5 more)

### Community 55 - ".SetVerificationLogChannelAsync"
Cohesion: 0.35
Nodes (9): DefaultMemberPermissions, DiscordSocketClient, IChannel, ITextChannel, RequireContext, RequireUserPermission, SlashCommand, Task (+1 more)

### Community 56 - "AdministrationService"
Cohesion: 0.17
Nodes (9): DiscordSocketClient, Expected, IReadOnlyCollection, ITextChannel, Responded, SocketGuild, Task, AdministrationService (+1 more)

### Community 57 - "直播小幫手 Bot 協作說明"
Cohesion: 0.18
Nodes (10): graphify, 執行方式, 建置與驗證, 文件, 架構, 直播小幫手 Bot 協作說明, 程式慣例, 設定與機密 (+2 more)

### Community 58 - "TwitchRefreshRotationLifecycle"
Cohesion: 0.17
Nodes (12): IDisposable, Action, Dictionary, Lease, Task, TaskCompletionSource, Lease, TwitchRefreshRotationLifecycle (+4 more)

### Community 59 - "CHZZK 錄影整合計畫"
Cohesion: 0.10
Nodes (20): Bot, Bot 實作, Bot 自動化測試, CHZZK 錄影整合計畫, Redis 契約建議, StreamRecordTools, StreamRecordTools 實作, 交付與部署順序 (+12 more)

### Community 60 - "DiscordStreamNotifyBot.Shared"
Cohesion: 0.08
Nodes (8): DiscordStreamNotifyBot.Tests.Component.Redis, DiscordStreamNotifyBot.HttpClients, DiscordStreamNotifyBot.Scraper, DiscordStreamNotifyBot.Shared, DiscordStreamNotifyBot.Scraper.Detection.Twitch.Debounce, DiscordStreamNotifyBot.Command.TwitCasting, DiscordStreamNotifyBot.Scraper.Detection.Twitch, DiscordStreamNotifyBot

### Community 61 - "YoutubeStreamService"
Cohesion: 0.06
Nodes (31): NowStreamingHost, IInteractionService, BotLocalizer, CommandDisplayResolver, DiscordSocketClient, Embed, EmojiService, GuildLocaleService (+23 more)

### Community 62 - "graphify reference: extra exports and benchmark"
Cohesion: 0.22
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 63 - "Bot"
Cohesion: 0.08
Nodes (22): BotPlayingStatus, ConnectionMultiplexer, DiscordSocketClient, IDatabase, ISubscriber, IUser, Task, Timer (+14 more)

### Community 64 - "DiscordStreamNotifyBot.DataBase.Table"
Cohesion: 0.08
Nodes (12): DiscordStreamNotifyBot.Tests.Component.MySql, DiscordStreamNotifyBot.Auth, DiscordStreamNotifyBot.DataBase.Table, DiscordStreamNotifyBot.SharedService.Twitch, DiscordStreamNotifyBot.Interaction.TwitchSubscription, DiscordStreamNotifyBot.SharedService.TwitchSubscription, DateTime, RecordYoutubeChannel (+4 more)

### Community 65 - ".HoloScheduleAsync"
Cohesion: 0.09
Nodes (18): GeneratedRegex, HttpRequestException, CancellationToken, IReadOnlyCollection, IReadOnlyList, Task, Batch, CancellationToken (+10 more)

### Community 66 - ".FilterNoNotifyGuilds"
Cohesion: 0.37
Nodes (4): IEnumerable, ArgumentNullException, Fact, NoNotifyGuildFilterTests

### Community 67 - ".Main"
Cohesion: 0.19
Nodes (9): AssemblyInformationalVersionAttribute, Assembly, CancellationToken, Exception, HashSet, PeriodicTimer, Task, Program (+1 more)

### Community 68 - "AddManualMemberCheckVideoFlag"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, AddManualMemberCheckVideoFlag

### Community 69 - ".VideoLookupSearchesAllFourTables"
Cohesion: 0.16
Nodes (10): DbUpdateConcurrencyException, HoloVideos, NijisanjiVideos, NonApprovedVideos, OtherVideos, DateTime, MySqlComponentFact, Task (+2 more)

### Community 70 - "NotificationContractTests"
Cohesion: 0.28
Nodes (5): DateTime, Fact, JObject, YTChannelType, NotificationContractTests

### Community 71 - "EF Core 遷移與基線化（本專案版）"
Cohesion: 0.25
Nodes (7): EF Core 遷移與基線化（本專案版）, 一次性基線化（舊的 EnsureCreated 正式庫）, 一般變更流程, 你必須先知道的三件專案特例, 啟動時不碰資料庫（重要）, 套用：本地/開發 vs 正式環境, 收尾

### Community 72 - "AdminSettingsSnapshot"
Cohesion: 0.10
Nodes (22): List, AdminSettingsGuild, MemberCount, Name, AdminSettingsHealth, BotConnected, AdminSettingsResources, Channels (+14 more)

### Community 73 - "11. 通知與背景訊息"
Cohesion: 0.29
Nodes (7): 11.1 現況限制, 11.2 目標作法, 11.3 YouTube, 11.4 Twitch, 11.5 TwitCasting, 11.6 YouTube 會限驗證, 11. 通知與背景訊息

### Community 74 - "GoogleOAuthOperationLockLease"
Cohesion: 0.09
Nodes (26): IAsyncDisposable, CancellationToken, CancellationTokenSource, Exception, IConnectionMultiplexer, IDatabase, RedisKey, RedisValue (+18 more)

### Community 75 - "TwitchStreamLifecycleDecisionTests"
Cohesion: 0.08
Nodes (25): ConcurrentDictionary, TwitchOfflineAction, ClearState, Defer, Ignore, PublishEnd, ResumeStream, TwitchOfflineFacts (+17 more)

### Community 78 - "YoutubeWebSubContractTests"
Cohesion: 0.14
Nodes (6): IReadOnlyList, KeyValuePair, YoutubeWebSubContract, ArgumentException, Fact, YoutubeWebSubContractTests

### Community 80 - "YoutubeStream"
Cohesion: 0.07
Nodes (45): DiscordStreamNotifyBot.Command.Help, ICommandService, Alias, Command, CommandInfo, CommandService, IServiceProvider, Summary (+37 more)

### Community 81 - "Broadcaster"
Cohesion: 0.17
Nodes (12): Broadcaster, Created, Id, Image, IsLive, LastMovieId, Level, Name (+4 more)

### Community 82 - ".RequestCoreAsync"
Cohesion: 0.17
Nodes (13): CancellationToken, ConcurrentDictionary, DateTime, IConnectionMultiplexer, IDatabase, IHttpClientFactory, RetryConditionHeaderValue, SemaphoreSlim (+5 more)

### Community 83 - "AdminSettingsChannel"
Cohesion: 0.25
Nodes (8): AdminSettingsChannel, CanEmbedLinks, CanManageEvents, CanSendMessages, CanView, Id, Name, Type

### Community 84 - "YoutubeDetectionService"
Cohesion: 0.08
Nodes (28): IsDeleted, YoutubeDetectionService, ConcurrentDictionary, DateTime, HttpClient, IHttpClientFactory, TableVideo, Task (+20 more)

### Community 85 - "graphify reference: query, path, explain"
Cohesion: 0.33
Nodes (5): For /graphify explain, For /graphify path, graphify reference: query, path, explain, Step 0 — Constrained query expansion (REQUIRED before traversal), Step 1 — Traversal

### Community 86 - "自動化測試導入計畫"
Cohesion: 0.17
Nodes (12): 10. 測試實作規則, 1. 目標, 2. 測試分類, 3. 不移除的啟動檢查, 4. 第一批：低耦合契約與格式化, 5. 第二批：小幅抽出純邏輯, 6. 第三批：時間與快取, 7. 第四批：Scraper 狀態機 (+4 more)

### Community 87 - "YoutubeApiService"
Cohesion: 0.20
Nodes (8): CancellationToken, Exception, IEnumerable, List, Task, YTApiVideo, YoutubeApiService, YouTubeService

### Community 88 - "ChzzkRecordService"
Cohesion: 0.21
Nodes (12): Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task, ChzzkStream (+4 more)

### Community 89 - ".Get"
Cohesion: 0.08
Nodes (24): MissingManifestResourceException, CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, IChannel, NoticeType, RequireBotPermission (+16 more)

### Community 90 - ".Info"
Cohesion: 0.38
Nodes (4): ComponentInteraction, SocketMessageComponent, Task, SpiderManagementComponent

### Community 91 - ".CreateAsyncClient"
Cohesion: 0.22
Nodes (14): IHttpClientFactory, CancellationToken, Fact, Func, HttpClient, HttpRequestMessage, HttpResponseMessage, HttpStatusCode (+6 more)

### Community 92 - "EmbedBuilderFactory"
Cohesion: 0.28
Nodes (7): BotLocalizer, DateTime, EmbedBuilder, TimeSpan, YTApiVideo, EmbedBuilderFactory, YoutubeNotificationVariant

### Community 93 - "YoutubeWebSubRequestResult"
Cohesion: 0.12
Nodes (17): HttpStatusCode, TimeSpan, YoutubeWebSubRequestOutcome, Accepted, PermanentFailure, Suppressed, TransientFailure, YoutubeWebSubRequestResult (+9 more)

### Community 94 - ".AssertKeysAbsentAsync"
Cohesion: 0.16
Nodes (20): CancellationToken, ChzzkService, Func, IDatabase, RedisValue, StreamEntry, Task, TimeSpan (+12 more)

### Community 95 - "graphify reference: add a URL and watch a folder"
Cohesion: 0.50
Nodes (3): For /graphify add, For --watch, graphify reference: add a URL and watch a folder

### Community 96 - "graphify reference: commit hook and native CLAUDE.md integration"
Cohesion: 0.50
Nodes (3): For git commit hook, For native CLAUDE.md integration, graphify reference: commit hook and native CLAUDE.md integration

### Community 97 - "graphify reference: incremental update and cluster-only"
Cohesion: 0.50
Nodes (3): For --cluster-only, For --update (incremental re-extraction), graphify reference: incremental update and cluster-only

### Community 98 - "YoutubeTerminalEventRegistry"
Cohesion: 0.08
Nodes (31): ClaimState, ConcurrentDictionary, Func, SemaphoreSlim, Task, YoutubeNoticeType, ClaimState, ClaimedKind (+23 more)

### Community 101 - "網頁管理設定中心實作計畫"
Cohesion: 0.12
Nodes (16): 10. 首版完成閘門, 11. 驗證, 12. 實作順序, 1. 目標, 2. 已確認產品決策, 3. 系統邊界, 4.1 命令, 4.2 回應 (+8 more)

### Community 102 - "BotConfig"
Cohesion: 0.08
Nodes (26): BotConfig, ApiServerDomain, DiscordToken, ECPayEmoteId, EnableGuildMembersIntent, GoogleApiKey, GoogleClientId, GoogleClientSecret (+18 more)

### Community 103 - "ChzzkClient"
Cohesion: 0.11
Nodes (19): HttpStatus, IsNotFound, IsSuccess, RetryAfter, CancellationToken, HttpClient, HttpResponseMessage, Task (+11 more)

### Community 105 - "DiscordWebhookClient"
Cohesion: 0.21
Nodes (9): CancellationToken, DiscordSocketClient, HttpClient, Task, DiscordWebhookClient, Message, avatar_url, content (+1 more)

### Community 106 - "YoutubeAtomFallback"
Cohesion: 0.14
Nodes (18): ChannelFetch, ChannelFetchKind, CancellationToken, Func, HttpClient, IHttpClientFactory, IReadOnlyCollection, IReadOnlyList (+10 more)

### Community 113 - ".CreateOrRepairConfigurationAsync"
Cohesion: 0.26
Nodes (11): CancellationToken, DiscordSocketClient, Exception, GuildTwitchSubscriptionConfig, ICollection, IRole, NotifierMetrics, SocketGuild (+3 more)

### Community 114 - "AdminSettingsCrawlerPlatform"
Cohesion: 0.13
Nodes (16): Name, IEnumerable, AdminSettingsCrawlerItem, SourceId, SourceName, AdminSettingsCrawlerPlatform, Count, Enabled (+8 more)

### Community 115 - "DiscordStreamNotifyBot.Notifier.csproj"
Cohesion: 0.10
Nodes (19): Microsoft.Extensions.DependencyInjection.Abstractions (10.0.1), System.Management (10.0.1), net8.0, Ben.Demystifier (0.4.1), Discord.Net (3.20.1), Dorssel.Utilities.Debounce (3.0.0), EFCore.NamingConventions (9.0.0), Google.Apis.YouTube.v3 (1.73.0.3981) (+11 more)

### Community 116 - "TwitchSpider"
Cohesion: 0.18
Nodes (13): AutocompletionResult, CommandExample, CommandSummary, DefaultMemberPermissions, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider (+5 more)

### Community 117 - "Normal"
Cohesion: 0.26
Nodes (8): DiscordStreamNotifyBot.Command.Normal, Alias, Command, DiscordSocketClient, DiscordWebhookClient, Summary, Task, Normal

### Community 118 - "ClusterService"
Cohesion: 0.23
Nodes (7): IDatabase, Task, TimeSpan, ClusterService, RedisComponentFact, Task, ClusterServiceRedisComponentTests

### Community 119 - "TwitchService"
Cohesion: 0.08
Nodes (26): BotLocalizer, CancellationToken, Clip, DateTime, DiscordSocketClient, EmojiService, EventSubSubscription, GuildLocaleService (+18 more)

### Community 120 - "Twitch OAuth 與零成本 EventSub 實作計畫"
Cohesion: 0.14
Nodes (13): 0. 涉及專案, 10. Backend EventSub Webhook, 12. Frontend, 14. Grafana, 18. 建置與遷移, 19. 部署順序, 1. 不可偏離的決策, 20. 官方參考 (+5 more)

### Community 121 - "TwitchEventSubEnsureResult"
Cohesion: 0.08
Nodes (30): EventSubSubscription, IReadOnlyList, Stream, TwitchEventSubDeleteResult, DeletedSubscriptionIds, Status, TwitchEventSubDeleteStatus, ApiFailure (+22 more)

### Community 122 - "YoutubeMemberProbeResultKind"
Cohesion: 0.10
Nodes (23): GoogleApiException, YouTubeService, CancellationToken, GoogleCredential, HashSet, IEnumerable, Task, YoutubeMemberApiClient (+15 more)

### Community 123 - "AutocompleteCandidate"
Cohesion: 0.11
Nodes (20): IEnumerable, IReadOnlyList, AutocompleteCandidate, Name, SearchTerms, AutocompleteSearch, AutocompletionResult, IAutocompleteInteraction (+12 more)

### Community 124 - "16. 執行階段"
Cohesion: 0.22
Nodes (9): 16. 執行階段, 階段 0：前置確認, 階段 1：資料模型與 Backend 設定, 階段 2：Google/Twitch OAuth 隔離, 階段 3：Frontend, 階段 4：Twitch add資格與授權清理, 階段 5：StreamOnline 與 EventSub reconcile, 階段 6：Prometheus 與 Grafana (+1 more)

### Community 125 - "Prometheus / Grafana 監控"
Cohesion: 0.18
Nodes (10): Backend 指標, Coordinator 指標, Endpoints, Grafana, Notifier 指標, Prometheus, Prometheus / Grafana 監控, Scraper 指標 (+2 more)

### Community 126 - "TwitcastingClient"
Cohesion: 0.09
Nodes (21): DiscordStreamNotifyBot.HttpClients.Twitcasting.Model, List, GetAllRegistedWebHookJson, AllCount, Webhooks, Webhook, Event, UserId (+13 more)

### Community 127 - "DiscordStreamNotifyBot.Scraper.csproj"
Cohesion: 0.50
Nodes (3): net8.0, prometheus-net.AspNetCore (8.2.1), Microsoft.NET.Sdk

### Community 128 - "DiscordStreamNotifyBot.Tests.csproj"
Cohesion: 0.25
Nodes (7): coverlet.collector (6.0.0), Microsoft.Extensions.TimeProvider.Testing (9.0.0), Microsoft.NET.Test.Sdk (17.8.0), xunit (2.5.3), xunit.runner.visualstudio (2.5.3), net8.0, Microsoft.NET.Sdk

### Community 129 - "17. 驗證矩陣"
Cohesion: 0.33
Nodes (6): 17.1 新增 spider, 17.2 EventSub, 17.3 授權失效, 17.4 OAuth, 17.5 Prometheus/Grafana, 17. 驗證矩陣

### Community 130 - "ChzzkNotification"
Cohesion: 0.10
Nodes (22): ChzzkNotificationVariant, BotLocalizer, EmbedBuilder, TimeSpan, ChzzkEmbedBuilderFactory, Embed, MessageComponent, ChzzkNotificationVariant (+14 more)

### Community 131 - "SendMsgToAllGuildService"
Cohesion: 0.09
Nodes (24): ButtonCheckData, SendAllPayload, ChoiceDisplayAttribute, DiscordSocketClient, Embed, HttpException, Task, TimeoutException (+16 more)

### Community 132 - "7. OAuth API 與流程隔離"
Cohesion: 0.40
Nodes (5): 7.1 API, 7.2 State, 7.3 Callback, 7.4 Twitch scopes, 7. OAuth API 與流程隔離

### Community 133 - "TwitcastingLiveStartPlannerTests"
Cohesion: 0.16
Nodes (14): DateTime, TwitcastingLiveStartAction, IgnoreDuplicate, PersistAndNotify, PersistRequestRecordingAndNotify, TwitcastingLiveStartFacts, TwitcastingLiveStartPlan, TwitcastingLiveStartPlanner (+6 more)

### Community 134 - ".BuildResponseJsonAsync"
Cohesion: 0.09
Nodes (26): ClusterQueryType, ConcurrentBag, NotificationChannelIssue, QueryRequest, Replies, Responses, Expected, Func (+18 more)

### Community 135 - ".Plan"
Cohesion: 0.24
Nodes (11): HashSet, IEnumerable, IReadOnlyList, TwitcastingWebhookAction, TwitcastingWebhookActionKind, RegisterLiveStart, RemoveLiveStart, TwitcastingWebhookRegistration (+3 more)

### Community 137 - "Migration"
Cohesion: 0.20
Nodes (6): Migration, DateTime, MigrationBuilder, DateTime, ModelBuilder, RefactorDbContext

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

### Community 143 - ".GetCommandPath"
Cohesion: 0.08
Nodes (31): AutocompleteHandler, DiscordStreamNotifyBot.Interaction.Help, RequireBotPermissionAttribute, RequireUserPermissionAttribute, AutocompletionResult, HelpService, IAutocompleteInteraction, IInteractionContext (+23 more)

### Community 144 - "13. Prometheus"
Cohesion: 0.67
Nodes (3): 13.1 Backend 指標, 13.2 Scraper 指標, 13. Prometheus

### Community 145 - "4. 安全刪除狀態機"
Cohesion: 0.67
Nodes (3): 4.1 直播中授權失效, 4.2 關台後重新判斷, 4. 安全刪除狀態機

### Community 146 - "TwitchNotification"
Cohesion: 0.07
Nodes (27): DateTime, List, TwitcastingNotification, Category, ChannelId, ChannelTitle, IsPrivate, IsRecord (+19 more)

### Community 147 - ".Plan"
Cohesion: 0.21
Nodes (11): HashSet, IReadOnlyCollection, IReadOnlyList, TwitchEventSubCreateSpec, TwitchEventSubFact, TwitchEventSubFinalDecision, IsSuccess, TwitchEventSubReconcilePlan (+3 more)

### Community 148 - "MainDbContextModelSnapshot.cs"
Cohesion: 0.33
Nodes (4): ModelSnapshot, DateTime, ModelBuilder, MainDbContextModelSnapshot

### Community 149 - "DiscordStreamNotifyBot.Tests"
Cohesion: 0.11
Nodes (6): DiscordStreamNotifyBot.Scraper.Detection.Youtube, DiscordStreamNotifyBot.SharedService.Youtube, DiscordStreamNotifyBot.Tests, DiscordStreamNotifyBot.SharedService.Youtube.Json, Fact, NotificationBusConsumerOptionsTests

### Community 150 - ".StartAndBlockAsync"
Cohesion: 0.07
Nodes (27): AdminSettingsService, BotLocalizer, ChzzkClient, ChzzkRecordService, ChzzkService, CommandDisplayResolver, EmojiService, GuildLocaleService (+19 more)

### Community 151 - "ModifyTwitCastingTable"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, ModifyTwitCastingTable

### Community 152 - "AddMaxSpiderCountSettingField"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, AddMaxSpiderCountSettingField

### Community 153 - "DiscordStreamNotifyBot.Migrations"
Cohesion: 0.22
Nodes (6): DiscordStreamNotifyBot.Migrations, DateTime, MigrationBuilder, DateTime, ModelBuilder, SyncModelDrift

### Community 154 - "AddTwitchBroadcasterAuthorization"
Cohesion: 0.22
Nodes (5): DateTime, MigrationBuilder, DateTime, ModelBuilder, AddTwitchBroadcasterAuthorization

### Community 155 - "AddLocalizationSettings"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, AddLocalizationSettings

### Community 156 - "MemberRoleOwnershipSnapshot"
Cohesion: 0.15
Nodes (15): CancellationToken, ICollection, IEnumerable, IReadOnlyCollection, Task, MemberEntitlementProvider, Twitch, Youtube (+7 more)

### Community 157 - ".Reconcile"
Cohesion: 0.16
Nodes (13): YoutubeReminderBatchFacts, YoutubeReminderReconciliationAction, KeepExisting, PublishChangeAndReplaceTimer, PublishChangeAndRunImmediately, PublishDeleteAndRemove, PublishStartAndRemove, RemoveWithoutReplacement (+5 more)

### Community 158 - "DbEntity"
Cohesion: 0.07
Nodes (23): BannerChange, ChannelId, GuildId, LastChangeStreamId, DateTime, DbEntity, DateAdded, Id (+15 more)

### Community 159 - "NonPersistentGoogleDataStore"
Cohesion: 0.21
Nodes (5): IDataStore, Task, ITokenDataStore, Task, NonPersistentGoogleDataStore

### Community 160 - "Movie"
Cohesion: 0.09
Nodes (22): Movie, Category, CommentCount, Country, Created, CurrentViewCount, Duration, HlsUrl (+14 more)

### Community 161 - "BotState"
Cohesion: 0.08
Nodes (21): ConnectionMultiplexer, IDatabase, ISubscriber, IUser, BotState, ApplicatonOwner, DbService, IsConnect (+13 more)

### Community 162 - "AddTwitchSubscriptionVerification"
Cohesion: 0.22
Nodes (5): DateTime, MigrationBuilder, DateTime, ModelBuilder, AddTwitchSubscriptionVerification

### Community 163 - "AddTwitchSubscriptionDeletionPending"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, AddTwitchSubscriptionDeletionPending

### Community 164 - "RedisComponentFixture"
Cohesion: 0.20
Nodes (11): ConfigurationOptions, IAsyncLifetime, ConnectionMultiplexer, IDatabase, RedisKey, Task, RedisComponentFixture, Connection (+3 more)

### Community 165 - "AddYoutubeMemberVerificationDurability"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, AddYoutubeMemberVerificationDurability

### Community 166 - ".CheckRequirementsAsync"
Cohesion: 0.15
Nodes (9): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildMemberCountAttribute, ErrorMessage, GuildMemberCount (+1 more)

### Community 167 - ".BuildAddedMessage"
Cohesion: 0.14
Nodes (14): Components, Embed, MessageComponent, CrawlerOwnerNotifier, CrawlerPlatform, Chzzk, Twitcasting, Twitch (+6 more)

### Community 168 - "AddGoogleOAuthUnlinkIntent"
Cohesion: 0.22
Nodes (5): DateTime, MigrationBuilder, DateTime, ModelBuilder, AddGoogleOAuthUnlinkIntent

### Community 169 - "RenameVerificationLogChannel"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, RenameVerificationLogChannel

### Community 170 - "DebounceFixture"
Cohesion: 0.27
Nodes (11): CancellationToken, Fact, IReadOnlyCollection, List, Task, UserId, DebounceFixture, Batches (+3 more)

### Community 171 - "GuildYoutubeMemberConfig"
Cohesion: 0.11
Nodes (15): OperationCanceledException, GuildYoutubeMemberConfig, DeletionPending, GuildId, IsManualVideoId, MemberCheckChannelId, MemberCheckChannelTitle, MemberCheckGrantRoleId (+7 more)

### Community 172 - ".FailedSendKeepsRenewalDueUntilChallengeConfirms"
Cohesion: 0.18
Nodes (13): HttpMessageHandler, CancellationToken, HttpClient, HttpRequestMessage, HttpResponseMessage, HttpStatusCode, RedisComponentFact, Task (+5 more)

### Community 173 - ".Parse"
Cohesion: 0.24
Nodes (6): DateTime, YoutubeAtomEntry, YoutubeAtomFeedParser, Fact, YoutubeAtomFeedParserTests, XNamespace

### Community 175 - "YoutubeNotification"
Cohesion: 0.11
Nodes (19): NotificationDedupPolicy, YTChannelType, YoutubeNotification, ActualEndTime, ActualStartTime, ChannelId, ChannelTitle, ChannelType (+11 more)

### Community 176 - "Fact"
Cohesion: 0.10
Nodes (12): DateTimeOffset, IEnumerable, IReadOnlyCollection, TwitchAuthorizationEventPolicy, TwitchRateLimitPolicy, TwitchSubscriptionConfigurationPolicy, Fact, GuildTwitchSubscriptionConfig (+4 more)

### Community 177 - "NotificationMetricEvent"
Cohesion: 0.15
Nodes (13): NotificationMetricEvent, ChzzkEnd, ChzzkStart, TwitcastingStart, TwitchChangeData, TwitchEnd, TwitchStart, YoutubeChangeTime (+5 more)

### Community 178 - "Twitch"
Cohesion: 0.43
Nodes (9): Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task, TwitchService (+1 more)

### Community 179 - ".Classify"
Cohesion: 0.14
Nodes (16): DateTime, YoutubeApiVideoAction, ActiveChatOnly, Ignore, IgnoreFakePost, NewVideo, Scheduled, Started (+8 more)

### Community 180 - ".CheckMemberShipOnlyVideoIdAsync"
Cohesion: 0.21
Nodes (9): Task, YoutubeMemberCandidateFacts, YoutubeMemberChannelDecision, YoutubeMemberChannelFacts, YoutubeMemberVideoPolicy, Fact, InlineData, Theory (+1 more)

### Community 181 - "TwitchStateDecisions.cs"
Cohesion: 0.10
Nodes (24): DateTime, IEnumerable, List, TimeSpan, TwitchChannelEventFacts, TwitchChannelStateFacts, TwitchChannelUpdateAction, Ignore (+16 more)

### Community 182 - "YoutubeMemberVideoLogNotification"
Cohesion: 0.20
Nodes (12): YoutubeMemberVideoLogNotification, BotOwnerMessage, CheckChannelId, IsNeedRemove, IsNeedSendToOwner, MessageArguments, MessageCode, ArgumentException (+4 more)

### Community 183 - "YoutubeWebSubPendingAction"
Cohesion: 0.11
Nodes (17): DateTime, YoutubeWebSubPendingAction, CallbackToken, ChannelId, ConfirmedAtUtc, DeniedAtUtc, Mode, RequestedAtUtc (+9 more)

### Community 184 - "YoutubePubSubNotification"
Cohesion: 0.15
Nodes (12): YoutubePubSubNotification, ChannelId, Link, NotificationType, Published, Title, Updated, VideoId (+4 more)

### Community 185 - "MigrationAndConstraintTests"
Cohesion: 0.29
Nodes (5): DbUpdateException, MySqlComponentFact, Task, TwitchBroadcasterAuthorization, MigrationAndConstraintTests

### Community 186 - "InteractionMetadataFixture"
Cohesion: 0.13
Nodes (13): IServiceProvider, IServiceScope, IServiceScopeFactory, Dictionary, DiscordSocketClient, InteractionService, Type, InteractionMetadataFixture (+5 more)

### Community 187 - "YoutubeAtomValidatorStore"
Cohesion: 0.21
Nodes (9): CancellationToken, ETag, IConnectionMultiplexer, IDatabase, LastModified, Task, IYoutubeAtomValidatorStore, YoutubeAtomValidatorStore (+1 more)

### Community 188 - "CommonEqualityComparer"
Cohesion: 0.18
Nodes (5): IEqualityComparer, Func, CommonEqualityComparer, Func, CommonEqualityComparer

### Community 189 - ".Start"
Cohesion: 0.13
Nodes (16): ChzzkDetectionService, ServiceProvider, IHttpClientFactory, IYoutubeAtomValidatorStore, TwitchApiService, YoutubeApiService, DetectionHost, Task (+8 more)

### Community 190 - "StubHandler"
Cohesion: 0.22
Nodes (13): CancellationToken, Fact, Func, HttpRequestMessage, HttpResponseMessage, HttpStatusCode, InlineData, Task (+5 more)

### Community 191 - "TwitchReconcileDecisionTests"
Cohesion: 0.11
Nodes (19): DateTime, TwitchGuildEligibilityStatus, Eligible, Ineligible, MissingConfirmed, NotifierUnavailable, PendingSnapshot, SnapshotUnavailable (+11 more)

### Community 192 - "Category"
Cohesion: 0.21
Nodes (11): List, CategoriesJson, Categories, Category, Id, Name, SubCategories, SubCategory (+3 more)

### Community 193 - "DiscordStreamNotifyBot.SharedService.YoutubeMember"
Cohesion: 0.08
Nodes (12): DiscordStreamNotifyBot.SharedService.YoutubeMember, DiscordStreamNotifyBot.Interaction.YoutubeMember, YoutubeMemberAutomaticMutationAction, Apply, PreserveManualPin, YoutubeMemberManualPinPolicy, Exception, YoutubeMemberSafeLogging (+4 more)

### Community 194 - ".GenerateSuggestionsAsync"
Cohesion: 0.20
Nodes (11): AutocompletionResult, DefaultMemberPermissions, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IRole, IServiceProvider, SlashCommand (+3 more)

### Community 195 - "SharedExtensions"
Cohesion: 0.18
Nodes (5): DateTime, EmbedBuilder, Video, YTChannelType, SharedExtensions

### Community 196 - "NijisanjiStreamJson"
Cohesion: 0.08
Nodes (27): DateTime, List, Channel, Id, Liver, Main, Name, ThumbnailUrl (+19 more)

### Community 197 - ".Init"
Cohesion: 0.36
Nodes (5): DiscordSocketClient, HttpClient, Task, Timer, UptimeKumaClient

### Community 198 - "AdminYoutubeMessagesPayload"
Cohesion: 0.15
Nodes (13): AdminYoutubeMessagesPayload, ChangeTime, Delete, End, NewStream, NewVideo, Start, AdminYoutubeUpsertPayload (+5 more)

### Community 199 - ".CheckPermissionsAsync"
Cohesion: 0.33
Nodes (5): CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task

### Community 200 - "TcBackendStreamData.cs"
Cohesion: 0.07
Nodes (29): App, Mode, Url, BackendMovie, Id, Live, Fmp4, Host (+21 more)

### Community 201 - "ChzzkPollPolicyTests"
Cohesion: 0.16
Nodes (11): DateTime, TimeSpan, ChzzkPollFacts, ChzzkPollPolicy, ChzzkStreamStatus, Closed, PendingClose, Superseded (+3 more)

### Community 202 - ".PublishAsync"
Cohesion: 0.19
Nodes (12): IDatabase, RedisKey, RedisValue, StreamEntry, StreamGroupInfo, Task, TimeSpan, NotificationBus (+4 more)

### Community 203 - "RedisConnection"
Cohesion: 0.32
Nodes (4): ConnectionMultiplexer, Lazy, RedisConnection, Instance

### Community 204 - "InteractionErrorPolicyTests"
Cohesion: 0.33
Nodes (5): Fact, InlineData, InteractionCommandError, Theory, InteractionErrorPolicyTests

### Community 205 - "MainDbService"
Cohesion: 0.10
Nodes (22): DbContextOptions, TwitCasting, ComponentInteraction, GuildTwitchSubscriptionConfig, RequireContext, SlashCommand, Task, TwitchSubscription (+14 more)

### Community 206 - ".SendErrorMessageAsync"
Cohesion: 0.31
Nodes (9): IDMChannel, KeyNotFoundException, BotLocalizer, DiscordSocketClient, HttpException, ITextChannel, IUserMessage, TimeoutException (+1 more)

### Community 207 - "TwitchSpiderRemovalAction"
Cohesion: 0.22
Nodes (9): TwitchSpiderRemovalAction, AlreadyRemoved, DeferApiFailure, DeferLive, DeferNotifier, DeferSnapshot, EvaluateEligibility, Remove (+1 more)

### Community 208 - ".CreateAsync"
Cohesion: 0.24
Nodes (9): Fact, GuildPermission, InlineData, SlashCommandParameterInfo, Task, Theory, Type, InteractionCommandContractTests (+1 more)

### Community 209 - ".CheckRequirementsAsync"
Cohesion: 0.33
Nodes (5): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task

### Community 210 - ".CheckRequirementsAsync"
Cohesion: 0.22
Nodes (7): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildOwnerAttribute, ErrorMessage

### Community 211 - ".CheckPermissionsAsync"
Cohesion: 0.22
Nodes (7): CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task, RequireGuildOwnerAttribute, ErrorMessage

### Community 212 - "Log 與 Loki"
Cohesion: 0.20
Nodes (7): Console 備援, Grafana Dashboard, Log 與 Loki, Loki 主動推送, Serilog Pipeline, 排障, 檔案路由

### Community 213 - ".Resolve"
Cohesion: 0.57
Nodes (3): InteractionCommandError, InteractionErrorDescriptor, InteractionErrorPolicy

### Community 214 - ".PublishAsync"
Cohesion: 0.25
Nodes (6): ISubscriber, Task, ChzzkRecordBus, ChzzkRecordRequest, ChannelId, StreamKey

### Community 215 - "YouTube 會員驗證架構重構計畫"
Cohesion: 0.15
Nodes (12): 11. 排程與生命週期, 12. Provider Result 分類, 17. Manual Acceptance Matrix, 18. 停機部署順序, 19. Completion Criteria, 1. 範圍, 20. 新 Session 執行規則, 2. 已定案決策 (+4 more)

### Community 216 - "AdminSettingsYoutubeVerification"
Cohesion: 0.20
Nodes (10): AdminSettingsYoutubeVerification, DeletionPending, PendingRoleRemovalCount, PreviousRoleId, ProbeMode, ProbeVideoId, RoleId, SourceId (+2 more)

### Community 217 - "TwitchAuthorizationLocalState"
Cohesion: 0.28
Nodes (6): TwitchAuthorizationLocalState, Active, Missing, PersistedInvalid, TemporaryFailure, TwitchAuthorizationLocalStatePolicy

### Community 218 - "NotifierMetrics"
Cohesion: 0.10
Nodes (19): ArgumentOutOfRangeException, Event, Histogram, Platform, Counter, Gauge, TimeSpan, TwitchSubscriptionStatus (+11 more)

### Community 219 - "Movie"
Cohesion: 0.09
Nodes (22): Movie, Category, CommentCount, Country, Created, CurrentViewCount, Duration, HlsUrl (+14 more)

### Community 220 - "Utility"
Cohesion: 0.22
Nodes (5): HashSet, List, Task, Utility, OfficialGuildList

### Community 221 - ".SlashCommandExecuted"
Cohesion: 0.19
Nodes (8): IResult, SocketInteraction, SocketInteractionContext, SocketSlashCommandDataOption, IDiscordInteraction, IInteractionContext, SlashCommandInfo, Task

### Community 222 - "UtilityService"
Cohesion: 0.22
Nodes (10): IInteractionService, CancellationToken, DiscordSocketClient, IServiceProvider, SocketGuild, Task, UtilityService, DiscordSocketClient (+2 more)

### Community 223 - ".ValidateCommandLocalizationResources"
Cohesion: 0.18
Nodes (8): ISet, Dictionary, DictionaryEntry, HashSet, IDictionary, IList, LocalizationTarget, ModuleInfo

### Community 224 - "5. 語系模型與解析規則"
Cohesion: 0.33
Nodes (6): 5.1 支援值, 5.2 公開內容與背景通知, 5.3 私人即時回覆, 5.4 延遲會限驗證 DM, 5.5 併發安全, 5. 語系模型與解析規則

### Community 225 - "DiscordStreamNotifyBot.SharedService.Chzzk"
Cohesion: 0.16
Nodes (6): DiscordStreamNotifyBot.SharedService.Chzzk, DiscordStreamNotifyBot.Interaction.Chzzk, DiscordStreamNotifyBot.HttpClients.Chzzk.Model, DiscordStreamNotifyBot.Scraper.Detection.Chzzk, DiscordStreamNotifyBot.HttpClients.Chzzk, ChzzkLiveStatusValues

### Community 226 - "15. 實作階段"
Cohesion: 0.20
Nodes (10): 15. 實作階段, Phase 0：Baseline 與 characterization, Phase 1：Schema 與 migration, Phase 2：共用操作與 role ownership, Phase 3：YouTube interaction 與 state machine, Phase 4：Role/config durability, Phase 5：Provider 與 lifecycle, Phase 6：Backend (+2 more)

### Community 227 - "TwitcastingService"
Cohesion: 0.13
Nodes (14): CrawlerPolicy, BotLocalizer, Broadcaster, CancellationToken, DiscordSocketClient, EmojiService, GuildLocaleService, NoticeCache (+6 more)

### Community 228 - "AddChzzkAutoRecord"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, AddChzzkAutoRecord

### Community 229 - "AdminSettingsYoutubeNotification"
Cohesion: 0.25
Nodes (8): AdminSettingsYoutubeNotification, CreateEvent, DetectionEnabled, Messages, SourceId, SourceName, StreamChannelId, VideoChannelId

### Community 230 - ".LoadCommandFrom"
Cohesion: 0.20
Nodes (8): Assembly, IEnumerable, IServiceCollection, Type, Assembly, IServiceCollection, Type, Queue

### Community 231 - "Broadcaster"
Cohesion: 0.17
Nodes (12): Broadcaster, Created, Id, Image, IsLive, LastMovieId, Level, Name (+4 more)

### Community 232 - "TwitchBroadcasterAuthorization"
Cohesion: 0.12
Nodes (16): DateTime, TwitchBroadcasterAuthorization, AuthorizedAt, ClientId, DateUpdated, DiscordUserId, DisplayName, EncryptedAccessToken (+8 more)

### Community 233 - ".Valid"
Cohesion: 0.12
Nodes (15): AdminChzzkMessagesPayload, End, Start, AdminChzzkUpsertPayload, ChannelId, Messages, Source, AdminTwitchMessagesPayload (+7 more)

### Community 234 - "opencode.json"
Cohesion: 0.50
Nodes (3): plugin, $schema, .opencode/plugins/graphify.js

### Community 235 - "YouTube 會員驗證"
Cohesion: 0.33
Nodes (5): Durable state, YouTube 會員驗證, 使用者契約, 服務邊界, 部署前驗證

### Community 236 - "GuildSnapshot"
Cohesion: 0.15
Nodes (13): DiscordSocketClient, DateTime, List, GuildSnapshot, Id, MemberCount, Name, OwnerId (+5 more)

### Community 237 - "CHZZK 直播通知實作計畫"
Cohesion: 0.11
Nodes (19): 10. 分階段執行與驗證, 11. 交接提示詞, 12. 參考來源, 14. 關台通知修正（2026-09-16）, 1. 目標與範圍, 2.1 頻道資料, 2.2 直播狀態, 2.3 與官方 Open API 的差異 (+11 more)

### Community 238 - "14. Frontend"
Cohesion: 0.40
Nodes (5): 14.1 TypeScript contract, 14.2 GoogleSection, 14.3 VerifyWindow, 14.4 Copy/Privacy, 14. Frontend

### Community 239 - "8. DB Schema"
Cohesion: 0.40
Nodes (5): 8.1 Entity changes, 8.2 Indexes, 8.3 Migration 規則, 8.4 Preflight 查詢, 8. DB Schema

### Community 240 - "TwitchAccessTokenData"
Cohesion: 0.10
Nodes (18): TwitchAccessTokenData, AccessToken, ExpiresIn, RefreshToken, Scopes, TokenType, TwitchUserId, TwitchTokenErrorData (+10 more)

### Community 241 - "TwitcastingStream"
Cohesion: 0.13
Nodes (13): BotLocalizer, EmbedBuilder, TwitcastingEmbedBuilderFactory, DateTime, TwitcastingStream, Category, ChannelId, ChannelTitle (+5 more)

### Community 242 - "MySqlDataStoreTests"
Cohesion: 0.39
Nodes (8): StoredToken, MySqlComponentFact, MySqlDataStore, Task, MySqlDataStoreTests, StoredToken, AccessToken, RefreshToken

### Community 243 - "RedisContractTests"
Cohesion: 0.31
Nodes (4): Fact, InlineData, Theory, RedisContractTests

### Community 244 - "13. Backend Contract"
Cohesion: 0.50
Nodes (4): 13.1 Entity/DTO, 13.2 GET `/account-links`, 13.3 DELETE `/account-links/google`, 13. Backend Contract

### Community 245 - "16. 驗證命令"
Cohesion: 0.50
Nodes (4): 16.1 Bot, 16.2 Backend, 16.3 Frontend, 16. 驗證命令

### Community 246 - ".LockGuildAsync"
Cohesion: 0.15
Nodes (14): LeaseGroup, CancellationToken, ConcurrentDictionary, IEnumerable, Lease, List, SemaphoreSlim, Task (+6 more)

### Community 247 - "TwitchSubscriptionStatus"
Cohesion: 0.14
Nodes (13): TwitchAuthorizationAccessResult, AccessToken, DisplayName, Status, TwitchUserId, UserLogin, TwitchSubscriptionStatus, AuthorizationInvalid (+5 more)

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

### Community 252 - "TwitchEventSubMetricStatus"
Cohesion: 0.14
Nodes (14): TwitchEventSubMetricStatus, AuthorizationRevoked, BetaMaintenance, Enabled, ModeratorRemoved, NotificationFailuresExceeded, Unknown, UserRemoved (+6 more)

### Community 253 - "DiscordStreamNotifyBot.Scraper.Detection.Twitcasting"
Cohesion: 0.18
Nodes (7): DiscordStreamNotifyBot.Scraper.Detection.Twitcasting, TwitCastingWebHookJson, Broadcaster, Event, Movie, Signature, TwitcastingWebhookParser

### Community 254 - "AdminSettingsNotifications"
Cohesion: 0.12
Nodes (17): AdminSettingsChzzkNotification, ChannelId, DetectionEnabled, Messages, SourceId, SourceName, AdminSettingsNotifications, Chzzk (+9 more)

### Community 255 - "BotRole"
Cohesion: 0.12
Nodes (10): Program, BotRole, Coordinator, Notifier, Scraper, Fact, InlineData, InvalidOperationException (+2 more)

### Community 256 - "YoutubeMemberService.cs"
Cohesion: 0.22
Nodes (8): DiscordStreamNotifyBot.SharedService.Google, GoogleCredential, YoutubeMemberAuthorizationResult, YoutubeMemberAuthorizationStatus, AuthorizationInvalid, LocalContractFailure, Ready, TemporaryFailure

### Community 257 - "TwitcastingSpider"
Cohesion: 0.18
Nodes (14): AutocompletionResult, CommandExample, CommandSummary, DefaultMemberPermissions, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider (+6 more)

### Community 258 - "YoutubeMemberPolicies.cs"
Cohesion: 0.15
Nodes (11): YoutubeMemberRoleApplyResult, Applied, Failed, UnknownMember, YoutubeMemberSingleConfigurationQueueAction, Add, PreserveQueued, PreserveVerified (+3 more)

### Community 259 - "NoticeChzzkStreamChannel"
Cohesion: 0.29
Nodes (6): NoticeChzzkStreamChannel, DiscordChannelId, EndStreamMessage, GuildId, NoticeChzzkChannelId, StartStreamMessage

### Community 260 - "13. 驗證矩陣"
Cohesion: 0.25
Nodes (8): 13.1 編譯與靜態檢查, 13.2 Slash command 註冊, 13.3 Locale resolver, 13.4 首次設定, 13.5 通知, 13.6 YouTube 會限驗證, 13.7 範圍守衛, 13. 驗證矩陣

### Community 261 - "ChzzkStreamIdentityTests"
Cohesion: 0.18
Nodes (8): DateTime, TimeSpan, ChzzkStreamIdentity, ChzzkTime, Fact, InlineData, Theory, ChzzkStreamIdentityTests

### Community 262 - "TwitchStream"
Cohesion: 0.14
Nodes (13): TwitchStreamDataFacts, TwitchStreamNotificationFactory, DateTime, TwitchStream, GameName, StreamEndAt, StreamId, StreamStartAt (+5 more)

### Community 263 - "YoutubeMemberVerificationResult"
Cohesion: 0.15
Nodes (13): YoutubeMemberVerificationResult, CommentsDisabled, CredentialExpired, Member, NotMember, Provider4xx, Provider5xx, QuotaExceeded (+5 more)

### Community 264 - "NoticeYoutubeStreamChannel"
Cohesion: 0.15
Nodes (12): NoticeYoutubeStreamChannel, ChangeTimeMessage, DeleteMessage, DiscordNoticeStreamChannelId, DiscordNoticeVideoChannelId, EndMessage, GuildId, IsCreateEventForNewStream (+4 more)

### Community 265 - "AdminSettings.cs"
Cohesion: 0.09
Nodes (21): AdminProbeVideoPayload, SourceId, Video, AdminRemoveNotificationPayload, Source, AdminSetChannelPayload, ChannelId, AdminSetLocalePayload (+13 more)

### Community 266 - "7. 分階段執行"
Cohesion: 0.25
Nodes (8): 7. 分階段執行, 階段 0：建立基準, 階段 1：加入 Serilog 與 bootstrap logger, 階段 2：搬移 console 與檔案路由, 階段 3：切換 Loki sink, 階段 4：整理 facade 與 Discord.Net adapter, 階段 5：移除自製 sink 與更新文件, 階段 6：後續漸進式 structured logging（不阻擋本計畫完成）

### Community 267 - "YouTube WebSub 修正與 Atom fallback 實作計畫"
Cohesion: 0.20
Nodes (9): 13. 手動整合驗證, 15. 完成定義, 16. 參考資料, 18. 實作後審查與修正, 1. 目標, 2. 不在範圍, 3. 涉及專案, 5. 不可偏離的設計決策 (+1 more)

### Community 268 - "TwitchSpider"
Cohesion: 0.12
Nodes (16): ConcurrentDictionary, Task, TimeProvider, TimeSpan, TwitchGuildEligibilityEvaluator, DateTime, TwitchSpider, DateAdded (+8 more)

### Community 269 - "TwitchSubscriptionCheck"
Cohesion: 0.17
Nodes (11): DateTime, TwitchSubscriptionCheck, BroadcasterId, DiscordUserId, GuildId, IsChecked, IsGift, LastCheckTime (+3 more)

### Community 270 - "GuildConfig"
Cohesion: 0.18
Nodes (10): GuildConfig, GuildId, MaxChzzkSpiderCount, MaxTwitcastingSpiderCount, MaxTwitchSpiderCount, MaxTwitterSpaceSpiderCount, MaxYouTubeMemberCheckCount, MaxYouTubeSpiderCount (+2 more)

### Community 271 - "AdminSettingsTwitchVerification"
Cohesion: 0.18
Nodes (11): Dictionary, AdminSettingsTwitchVerification, DeletionPending, PendingRoleRemovalCount, PreviousSubscriberRoleId, SourceId, SourceLogin, SourceName (+3 more)

### Community 272 - "NotificationDeliveryResult"
Cohesion: 0.20
Nodes (10): NotificationDeliveryResult, AuthorizationFailure, Disabled, Discord5xx, MissingChannel, MissingGuild, MissingPermission, Sent (+2 more)

### Community 273 - "TwitchReconcileAction"
Cohesion: 0.20
Nodes (10): TwitchReconcileAction, DeferApiFailure, DeferLive, DeleteSubscriptions, DeleteSubscriptionsThenEvaluateGuild, EnsureFallbackSubscriptions, EnsurePermanentSubscriptions, KeepPollingWithoutSubscriptions (+2 more)

### Community 274 - "DiscordStreamNotifyBot.DataBase"
Cohesion: 0.07
Nodes (19): Attribute, DiscordStreamNotifyBot.Interaction.Attribute, DiscordStreamNotifyBot.Interaction.TwitCasting, DiscordStreamNotifyBot.Command.YoutubeMember, DiscordStreamNotifyBot.Command.Youtube, DiscordStreamNotifyBot.Interaction.OwnerOnly.Service, DiscordStreamNotifyBot.Command.Attribute, DiscordStreamNotifyBot.Command.Chzzk (+11 more)

### Community 275 - "GracefulShutdown"
Cohesion: 0.33
Nodes (4): CancellationToken, CancellationTokenSource, GracefulShutdown, Token

### Community 276 - "ChzzkPollAction"
Cohesion: 0.20
Nodes (10): ChzzkPollAction, BaselineOffline, CancelPendingClose, ConfirmClose, Ignore, RefreshObserved, StartPendingClose, SupersedeAndTrack (+2 more)

### Community 277 - ".AddChannel"
Cohesion: 0.15
Nodes (16): AutocompletionResult, CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, IAutocompleteInteraction, IChannel, IInteractionContext (+8 more)

### Community 278 - ".AddChannel"
Cohesion: 0.16
Nodes (16): AutocompletionResult, CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, IAutocompleteInteraction, IChannel, IInteractionContext (+8 more)

### Community 279 - "YoutubeChannelSpider"
Cohesion: 0.22
Nodes (8): DateTime, YoutubeChannelSpider, ChannelId, ChannelTitle, DateAdded, GuildId, IsTrustedChannel, LastSubscribeTime

### Community 280 - "MySqlComponentFixture"
Cohesion: 0.57
Nodes (3): Task, MySqlComponentFixture, DbService

### Community 281 - "PreconditionAttribute"
Cohesion: 0.25
Nodes (6): PreconditionAttribute, RequireGuildMemberCountAttribute, ErrorMessage, GuildMemberCount, RequireGuildAttribute, GuildId

### Community 282 - "YoutubeAtomParseResult"
Cohesion: 0.20
Nodes (9): IReadOnlyList, IReadOnlySet, YoutubeAtomParseResult, ChannelIds, Entries, Error, FeedChannelId, SkippedEntryCount (+1 more)

### Community 283 - "NotificationChannelIssue"
Cohesion: 0.25
Nodes (8): NotificationChannelIssue, ChannelId, ChannelName, GuildId, GuildName, MissingPermissions, Platform, Usages

### Community 284 - "AdminSettingsRole"
Cohesion: 0.29
Nodes (7): AdminSettingsRole, BotCanManage, Everyone, Id, Managed, Name, Position

### Community 285 - "NoticeTwitchStreamChannel"
Cohesion: 0.25
Nodes (7): NoticeTwitchStreamChannel, ChangeStreamDataMessage, DiscordChannelId, EndStreamMessage, GuildId, NoticeTwitchUserId, StartStreamMessage

### Community 286 - "YoutubeChannelOwnedType"
Cohesion: 0.25
Nodes (7): DateTime, YTChannelType, YoutubeChannelOwnedType, ChannelId, ChannelTitle, ChannelType, DateAdded

### Community 287 - "ChzzkStream"
Cohesion: 0.09
Nodes (29): ChzzkSpider, ChzzkStream, ConcurrentDictionary, DateTime, Func, ScraperMetrics, Task, TimeSpan (+21 more)

### Community 288 - "AdminSettingsYoutubeMessages"
Cohesion: 0.14
Nodes (13): RequestRoute, Command, Ignore, Snapshot, UnsupportedAction, UnsupportedVersion, AdminSettingsYoutubeMessages, ChangeTime (+5 more)

### Community 289 - "MySqlDataStore"
Cohesion: 0.36
Nodes (3): CancellationToken, Task, MySqlDataStore

### Community 290 - "MySqlComponentFixture.cs"
Cohesion: 0.22
Nodes (6): FactAttribute, ICollectionFixture, MySqlComponentFactAttribute, MySqlComponentCollection, RedisComponentCollection, RedisComponentFactAttribute

### Community 291 - "ReactionEventWrapper"
Cohesion: 0.26
Nodes (8): Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task, ReactionEventWrapper, Message

### Community 292 - "YoutubeNoticeType"
Cohesion: 0.10
Nodes (20): ChzzkNoticeType, EndStream, StartStream, NotifyType, TwitchNoticeType, ChangeStreamData, EndStream, StartStream (+12 more)

### Community 293 - "DescriptionOnlyLocalizationManager"
Cohesion: 0.33
Nodes (6): ILocalizationManager, ResxLocalizationManager, IDictionary, IList, LocalizationTarget, DescriptionOnlyLocalizationManager

### Community 294 - "GuildInfoResponse"
Cohesion: 0.29
Nodes (7): Dictionary, GuildInfoResponse, Channels, MemberCount, Name, OwnerId, ShardId

### Community 295 - "GuildTwitchSubscriptionConfig"
Cohesion: 0.08
Nodes (24): AddRoleIds, IQueryable, RemoveRoleIds, TwitchSubscriptionConfigurationQueries, Func, IReadOnlyList, IReadOnlySet, TwitchSubscriptionRolePolicy (+16 more)

### Community 296 - "TwitchRefreshPersistenceDecision"
Cohesion: 0.29
Nodes (5): TwitchRefreshPersistenceDecision, AlreadyPersisted, Stale, WriteReplacement, TwitchRefreshPersistencePolicy

### Community 297 - "8. WebSub 詳細修正"
Cohesion: 0.29
Nodes (7): 8.1 Topic 與 callback, 8.2 訂閱結果, 8.3 續訂判斷, 8.4 Challenge GET, 8.5 Denied GET, 8.6 Notification POST, 8. WebSub 詳細修正

### Community 298 - "TwitchProviderResultStatus"
Cohesion: 0.40
Nodes (5): TwitchProviderResultStatus, Failure, Invalid, Success, TemporaryFailure

### Community 299 - ".HandleStartLiveMessageAsync"
Cohesion: 0.21
Nodes (9): Category, List, RedisValue, SemaphoreSlim, Task, TwitcastingDetectionService, IsEnable, Category (+1 more)

### Community 300 - "AdminSettingsTwitcastingNotification"
Cohesion: 0.33
Nodes (6): AdminSettingsTwitcastingNotification, ChannelId, DetectionEnabled, SourceId, SourceName, StartMessage

### Community 301 - "MainDbContextFactory"
Cohesion: 0.40
Nodes (3): IDesignTimeDbContextFactory, MainDbContextFactory, Version

### Community 302 - "NotifierMetrics.cs"
Cohesion: 0.05
Nodes (36): NotificationBusMetricResult, Deduplicated, Dispatched, DispatchFailed, InvalidPayload, TwitchSubscriptionProviderError, InvalidResponse, NetworkFailure (+28 more)

### Community 303 - "TwitchRoleConfigurationResult"
Cohesion: 0.40
Nodes (5): TwitchRoleConfigurationResult, Config, Error, IsNew, IsSuccess

### Community 304 - "NijisanjiLiverJson"
Cohesion: 0.09
Nodes (22): List, Head, Height, Url, Width, Images, Head, NijisanjiLiverJson (+14 more)

### Community 305 - "YoutubeMemberAccessToken"
Cohesion: 0.33
Nodes (5): DateTime, YoutubeMemberAccessToken, DateAdded, DiscordUserId, EncryptedAccessToken

### Community 306 - ".FixTCDbAsync"
Cohesion: 0.33
Nodes (5): Alias, Command, RequireContext, RequireOwner, Task

### Community 307 - "13. 實作與驗證現況（2026-09-15）"
Cohesion: 0.40
Nodes (5): 13. 實作與驗證現況（2026-09-15）, 唯讀實測（2026-09-15，未寫入任何資料）, 尚未完成的外部驗證（不宣稱已通過）, 已完成的驗證, 已實作的檔案（DiscordStreamNotifyBot repo）

### Community 308 - "6. 資源架構"
Cohesion: 0.40
Nodes (5): 6.1 指令註冊資源, 6.2 執行期訊息資源, 6.3 Help 長文, 6.4 Localizer API, 6. 資源架構

### Community 309 - ".PublishRecordRequestAsync"
Cohesion: 0.22
Nodes (9): IOException, Func, ChzzkLiveStatusResult, ChzzkSpider, Fact, InlineData, Task, Theory (+1 more)

### Community 310 - "ChzzkSpider"
Cohesion: 0.18
Nodes (10): DateTime, ChzzkSpider, ChannelId, ChannelImageUrl, ChannelName, CurrentStreamKey, DateAdded, GuildId (+2 more)

### Community 311 - "AddChzzkNotification"
Cohesion: 0.22
Nodes (5): DateTime, MigrationBuilder, DateTime, ModelBuilder, AddChzzkNotification

### Community 312 - ".SendMessageToAllGuildAsync"
Cohesion: 0.22
Nodes (7): DiscordStreamNotifyBot.Interaction.OwnerOnly, SendMsgToAllGuildService, DefaultMemberPermissions, RequireOwner, SlashCommand, Task, SendMsgToAllGuild

### Community 313 - "AdminSettingsCommandReply"
Cohesion: 0.29
Nodes (7): AdminSettingsCommandReply, Arguments, Code, ContractVersion, CorrelationId, ShardId, State

### Community 314 - "10. 實作階段"
Cohesion: 0.33
Nodes (6): 10. 實作階段, 階段 0：重新確認基線, 階段 1：Bot WebSub request 與共享狀態, 階段 2：Backend callback 修正, 階段 3：Atom fallback, 階段 4：整合與文件

### Community 315 - "9. Atom fallback 詳細設計"
Cohesion: 0.33
Nodes (6): 9.1 排程與涵蓋範圍, 9.2 HTTP 行為, 9.3 Atom 解析, 9.4 去重、批次與通知, 9.5 已知限制, 9. Atom fallback 詳細設計

### Community 316 - "Video"
Cohesion: 0.12
Nodes (14): DateTime, Video, ChannelId, ChannelTitle, ChannelType, IsPrivate, ScheduledStartTime, VideoId (+6 more)

### Community 317 - ".SendStreamMessageAsync"
Cohesion: 0.07
Nodes (33): AggregateException, HttpException, Dictionary, Exception, Func, IMessageChannel, IUserMessage, List (+25 more)

### Community 318 - ".SaveVideosByType"
Cohesion: 0.40
Nodes (3): DbSet, MainDbContext, YTChannelType

### Community 319 - "BotPlayingStatus"
Cohesion: 0.33
Nodes (6): BotPlayingStatus, Guild, Info, Member, Stream, StreamCount

### Community 320 - ".OnlyAffiliateAndPartnerCanBeConfigured"
Cohesion: 0.33
Nodes (3): InlineData, Theory, TwitchSubscriptionConfigurationPolicyTests

### Community 321 - "TwitchReconcileRequestedPayload"
Cohesion: 0.67
Nodes (3): TwitchReconcileRequestedPayload, Reason, TwitchUserId

### Community 322 - "YoutubeMemberCandidateAction"
Cohesion: 0.33
Nodes (6): YoutubeMemberCandidateAction, AbortDiscovery, IgnoreCommentsDisabled, IgnorePublicVideo, IgnoreUnavailable, SelectMemberOnlyVideo

### Community 323 - "TwitchStreamEventPayload"
Cohesion: 0.50
Nodes (4): TwitchStreamEventPayload, BroadcasterUserId, BroadcasterUserLogin, BroadcasterUserName

### Community 325 - "TwitchAuthorizationChangedPayload"
Cohesion: 0.67
Nodes (3): TwitchAuthorizationChangedPayload, Status, TwitchUserId

### Community 326 - "GoogleOAuthUnlinkIntent"
Cohesion: 0.33
Nodes (5): DateTime, GoogleOAuthUnlinkIntent, DateAdded, DiscordUserId, ExpectedEncryptedToken

### Community 335 - "InviteResponse"
Cohesion: 0.40
Nodes (5): ChannelInfo, InviteResponse, InviteUrl, ShardId, TextChannels

### Community 339 - "6. 目標流程"
Cohesion: 0.40
Nodes (5): 6.1 WebSub 訂閱, 6.2 WebSub 通知, 6.3 Hub 拒絕訂閱, 6.4 Atom fallback, 6. 目標流程

### Community 340 - ".ParseToTimeSpan"
Cohesion: 0.40
Nodes (3): TimeSpan, Regex, TimeSpan

### Community 341 - "5. 持久化、發布與去重"
Cohesion: 0.33
Nodes (6): 5.1 已確認新增的資料表, 5.2 ChzzkSpider, 5.3 ChzzkStream, 5.4 NoticeChzzkStreamChannel, 5.5 GuildConfig 擴充與爬蟲上限, 5. 持久化、發布與去重

### Community 342 - ".AllRegisteredCommandsHaveDescriptionsInEverySupportedLocale"
Cohesion: 0.40
Nodes (3): Fact, Task, InteractionCommandLocalizationTests

### Community 343 - "12. 測試矩陣"
Cohesion: 0.50
Nodes (4): 12.1 Bot 單元測試, 12.2 Backend 測試, 12.3 必跑驗證, 12. 測試矩陣

### Community 344 - "4. 現況基線與已確認問題"
Cohesion: 0.50
Nodes (4): 4.1 Bot 訂閱流程, 4.2 Backend callback, 4.3 現有 fallback, 4. 現況基線與已確認問題

### Community 345 - "AdminSettingsCommon"
Cohesion: 0.50
Nodes (4): AdminSettingsCommon, GlobalNoticeChannelId, Locale, VerificationLogChannelId

### Community 346 - "11. 實際修改位置"
Cohesion: 0.67
Nodes (3): 11.1 Bot, 11.2 Backend, 11. 實際修改位置

### Community 347 - "YoutubeMemberTokenCleanupConcurrencyTests"
Cohesion: 0.60
Nodes (3): MySqlComponentFact, Task, YoutubeMemberTokenCleanupConcurrencyTests

### Community 348 - "14. 部署與回滾"
Cohesion: 0.50
Nodes (4): 14.1 建議部署順序, 14.2 相容性, 14.3 回滾, 14. 部署與回滾

### Community 349 - "14. 上線順序與回復"
Cohesion: 0.67
Nodes (3): 14.1 上線順序, 14.2 回復策略, 14. 上線順序與回復

### Community 351 - "17. 實際驗證結果"
Cohesion: 0.67
Nodes (3): 17.1 已執行命令與結果, 17.2 未驗證項目（需要真實環境或 Redis／MySQL component 基礎設施）, 17. 實際驗證結果

### Community 352 - "7. Redis 契約"
Cohesion: 0.67
Nodes (3): 7.1 Database 1：WebSub 共享狀態, 7.2 Database 0：Atom validator, 7. Redis 契約

### Community 353 - "TwitchAuthorizationChangedPayload"
Cohesion: 0.67
Nodes (3): TwitchAuthorizationChangedPayload, Status, TwitchUserId

### Community 354 - "TwitchAppAccessTokenResponse"
Cohesion: 0.67
Nodes (3): TwitchAppAccessTokenResponse, AccessToken, ExpiresIn

### Community 355 - "RecordedRequest"
Cohesion: 0.67
Nodes (3): IReadOnlyDictionary, Uri, RecordedRequest

## Knowledge Gaps
- **1722 isolated node(s):** `$schema`, `.opencode/plugins/graphify.js`, ``__EFMigrationsHistory``, `net8.0`, `prometheus-net.AspNetCore (8.2.1)` (+1717 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 2653 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **18 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MainDbService` connect `MainDbService` to `TwitcastingSpider`, `.GetLocaleAsync`, `SendMsgToAllGuildService`, `.Warn`, `TwitchSubscriptionService`, `.BuildSnapshotAsync`, `MainDbContext`, `.AddChannel`, `.AddChannel`, `YoutubeMemberAuthorizationService`, `.RetryWithBackoffAsync`, `MySqlComponentFixture`, `MemberRoleOwnershipSnapshot`, `ChzzkStream`, `BotState`, `MySqlDataStore`, `ClusterQueryService`, `YoutubeMemberRoleService`, `TwitchDetectionService`, `.GetDbContext`, `GuildLocaleService`, `.HandleStartLiveMessageAsync`, `ChzzkService`, `YoutubeMemberService`, `Administration`, `Twitch`, `YoutubeMemberSetting`, `.SetVerificationLogChannelAsync`, `YoutubeStreamService`, `.Start`, `Bot`, `.GenerateSuggestionsAsync`, `YoutubeStream`, `YoutubeDetectionService`, `YoutubeApiService`, `ChzzkRecordService`, `.Get`, `.Info`, `UtilityService`, `TwitcastingService`, `.CreateOrRepairConfigurationAsync`, `TwitchSpider`, `TwitchService`?**
  _High betweenness centrality (0.074) - this node is a cross-community bridge._
- **Why does `DiscordStreamNotifyBot.DataBase.Table` connect `DiscordStreamNotifyBot.DataBase.Table` to `YoutubeMemberService.cs`, `YoutubeMemberPolicies.cs`, `NoticeChzzkStreamChannel`, `TwitcastingLiveStartPlannerTests`, `TwitchStream`, `NoticeYoutubeStreamChannel`, `DiscordStreamNotifyBot.Shared.Messages`, `TwitchSpider`, `GuildConfig`, `TwitchSubscriptionCheck`, `DiscordStreamNotifyBot.DataBase`, `DiscordStreamNotifyBot.Tests`, `YoutubeChannelSpider`, `YoutubeMemberCheck`, `NoticeTwitchStreamChannel`, `DbEntity`, `YoutubeChannelOwnedType`, `GuildTwitchSubscriptionConfig`, `GuildYoutubeMemberConfig`, `YoutubeMemberAccessToken`, `TwitchStateDecisions.cs`, `ChzzkSpider`, `DiscordStreamNotifyBot.Shared`, `Video`, `DiscordStreamNotifyBot.SharedService.YoutubeMember`, `.VideoLookupSearchesAllFourTables`, `GoogleOAuthUnlinkIntent`, `ChzzkPollPolicyTests`, `DiscordStreamNotifyBot.SharedService.Chzzk`, `TwitchBroadcasterAuthorization`, `TwitcastingStream`?**
  _High betweenness centrality (0.070) - this node is a cross-community bridge._
- **Why does `DiscordStreamNotifyBot.Shared.Messages` connect `DiscordStreamNotifyBot.Shared.Messages` to `DiscordStreamNotifyBot.DataBase.Table`, `DiscordStreamNotifyBot.SharedService.Chzzk`, `YoutubeMemberService.cs`, `DiscordStreamNotifyBot.SharedService.YoutubeMember`, `BotState`, `TwitcastingLiveStartPlannerTests`, `YoutubeNoticeType`, `AdminSettings.cs`, `GuildSnapshot`, `NotifierMetrics.cs`, `DiscordStreamNotifyBot.DataBase`, `TwitchStateDecisions.cs`, `DiscordStreamNotifyBot.Tests`, `.PublishAsync`, `DiscordStreamNotifyBot.Shared`?**
  _High betweenness centrality (0.050) - this node is a cross-community bridge._
- **What connects `$schema`, `.opencode/plugins/graphify.js`, ``__EFMigrationsHistory`` to the rest of the system?**
  _1722 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `TwitchSubscriptionApiClient` be split into smaller, more focused modules?**
  _Cohesion score 0.1168091168091168 - nodes in this community are weakly interconnected._
- **Should `.GetLocaleAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.1461794019933555 - nodes in this community are weakly interconnected._
- **Should `DiscordStreamNotifyBot.Shared.csproj` be split into smaller, more focused modules?**
  _Cohesion score 0.08333333333333333 - nodes in this community are weakly interconnected._