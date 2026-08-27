# Graph Report - DiscordStreamNotifyBot  (2026-08-27)

## Corpus Check
- cluster-only mode — file stats not available

## Summary
- 5326 nodes · 11165 edges · 351 communities (332 shown, 19 thin omitted)
- Extraction: 91% EXTRACTED · 9% INFERRED · 0% AMBIGUOUS · INFERRED: 1031 edges (avg confidence: 0.82)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `75636207`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- TwitchSubscriptionApiClient
- .GetLocaleAsync
- .Warn
- DiscordStreamNotifyBot.Tests
- DiscordStreamNotifyBot.Shared.csproj
- TwitchApiService
- InteractionHandler
- 偵測 → 匯流排 → 發送 路徑除錯
- AuthTokenTests
- .CheckPermissionsAsync
- YoutubeReminderPolicyTests
- NotificationEmbedFactoryTests
- DiscordStreamNotifyBot.Localization
- Extensions
- .GetDbContext
- Extensions
- .ReconcileUserStateAsync
- 會限 OAuth Token 儲存改走 MySQL（去 Redis 依賴）計畫
- .SetMessage
- AdminSettingsMutationResult
- MySqlDataStoreTests
- YoutubeMemberAuthorizationService
- Log
- FakeTimeProvider
- Twitch 訂閱驗證實作計畫
- YoutubeMemberCheck
- 新增 TwitCasting 錄影委派計畫（小幫手 ↔ StreamRecordTools）
- 多語系支援計畫
- Serilog Logging 遷移計畫
- 12. 分階段執行
- YoutubeDetectionService
- .Main
- YoutubeMemberLifecycleTaskRegistry
- TwitchSpider
- AGENTS.md
- YoutubeChannelOwnedType
- TwitchOAuthRefreshLockLease
- .MissingOrShortKeyIsRejected
- TopLevelModule
- YoutubeMemberRoleService
- TwitchDetectionService
- Twitch
- ScraperMetrics
- GuildLocaleService
- TwitchChannelUpdateChange
- RedisChannels
- 13. 驗證矩陣
- SharedExtensions
- 網頁管理設定：30 秒請求與背景清理實作計畫
- Administration
- 網頁管理設定中心：爬蟲與會員驗證實作計畫
- 水平擴展（三層拆分）計畫 — Redis Streams 版
- YoutubeMemberSetting
- TwitchStateDecisions.cs
- Utility
- .BuildVariant
- AdministrationService
- AGENTS.md
- TwitchRefreshRotationLifecycle
- MainDbContext
- DiscordStreamNotifyBot.Shared
- .Info
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
- GuildTwitchSubscriptionConfig
- 11. 通知與背景訊息
- GoogleOAuthOperationLockLease
- InteractionMetadataFixture
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
- Task
- DescriptionOnlyLocalizationManager
- .Get
- YoutubeMemberService
- .CreateAsyncClient
- Video
- 6. 資源架構
- NotificationBusConsumer
- graphify reference: add a URL and watch a folder
- graphify reference: commit hook and native CLAUDE.md integration
- graphify reference: incremental update and cluster-only
- YoutubeTerminalEventRegistry
- graphify reference: GitHub clone and cross-repo merge
- graphify reference: transcribe video and audio
- 網頁管理設定中心實作計畫
- BotConfig
- .New
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
- .SendStreamMessageAsync
- DiscordStreamNotifyBot.Notifier.csproj
- DiscordStreamNotifyBot.DataBase.Table
- Normal
- ClusterService
- Movie
- Twitch OAuth 與零成本 EventSub 實作計畫
- .StartAndBlockAsync
- .Classify
- AutocompleteCandidate
- 16. 執行階段
- Prometheus / Grafana 監控
- TwitcastingClient
- DiscordStreamNotifyBot.Scraper.csproj
- DiscordStreamNotifyBot.Tests.csproj
- 17. 驗證矩陣
- .Init
- TwitchEventSubEnsureResult
- 7. OAuth API 與流程隔離
- TwitcastingLiveStartPlannerTests
- ClusterQueryService
- .Plan
- HelpDescription (bot feature summary)
- .RunAsync
- 11. Bot EventSub 與偵測
- 15. 預期修改檔案
- 2. 現況基線
- 5. Guild 資格與 OAuth 豁免
- DiscordStreamNotifyBot.sln
- CommandDisplayResolver
- 13. Prometheus
- 4. 安全刪除狀態機
- YoutubeMemberPolicies
- YoutubeMemberVideoLogNotification
- MainDbContextModelSnapshot.cs
- DiscordStreamNotifyBot.Command.Attribute
- DiscordStreamNotifyBot.Migrations
- ModifyTwitCastingTable
- AddMaxSpiderCountSettingField
- Migration
- AddTwitchBroadcasterAuthorization
- AddLocalizationSettings
- IInteractionService
- SendMsgToAllGuildService
- .CheckRequirementsAsync
- YoutubeMemberApiClientTests
- AdminYoutubeMessagesPayload
- DiscordStreamNotifyBot.Shared.Messages
- .MakeNamesUnique
- Movie
- RedisComponentFixture
- MemberRoleOwnershipSnapshot
- .CheckRequirementsAsync
- .NotifyAddedAsync
- InteractionErrorPolicyTests
- TwitchNotification
- DebounceFixture
- .CheckMemberShipCore
- TwitchAccessTokenData
- TwitchGuildEligibilityStatus
- AdminSettingsCrawlerPlatform
- .TryGetKey
- TwitchSubscriptionPoliciesTests
- TwitchStream
- CommonEqualityComparer
- .Classify
- .CheckMemberShipOnlyVideoIdAsync
- .ShutdownAsync
- TwitcastingDetectionService
- .ValidateCommandLocalizationResources
- YoutubePubSubNotification
- YoutubeMemberRolePoliciesTests
- DiscordStreamNotifyBot.Command
- .GenerateSuggestionsAsync
- AutocompleteHandler
- TwitchSubscriptionStatus
- .GenerateSuggestionsAsync
- TwitchChannelUpdateInfo
- Category
- .DecideAutomaticMutation
- .GenerateSuggestionsAsync
- BotStateTests
- NijisanjiStreamJson
- .GenerateSuggestionsAsync
- .AddChannel
- AdminSettingsYoutubeMessages
- TcBackendStreamData.cs
- AddTwitchSubscriptionVerification
- .GroupName
- NotifierMetrics.cs
- .GenerateSuggestionsAsync
- MainDbService
- .Plan
- AddTwitchSubscriptionDeletionPending
- .CreateAsync
- .ToLabel
- TwitchSpiderRemovalAction
- .CheckPermissionsAsync
- .CheckRequirementsAsync
- .Format
- .Resolve
- YouTube 會員驗證架構重構計畫
- ReactionEventWrapper
- SocialLinks
- NotifierMetrics
- DiscordStreamNotifyBot.HttpClients.Twitcasting.Model
- .GenerateSuggestionsAsync
- .SlashCommandExecuted
- TwitchOfflineAction
- TwitcastingStream
- NoticeYoutubeStreamChannel
- .LockGuildsAsync
- 15. 實作階段
- AddGoogleOAuthUnlinkIntent
- NonPersistentGoogleDataStore
- .SendErrorMessageAsync
- ReactionEventWrapper
- Broadcaster
- YoutubeNotification
- AddYoutubeMemberVerificationDurability
- .SetMemberCheckVideoIdAsync
- YouTube 會員驗證
- ReminderItem
- .GenerateSuggestionsAsync
- 14. Frontend
- 8. DB Schema
- NijisanjiLiverJson
- AdminSettingsTwitchVerification
- TwitchBroadcasterAuthorization
- RedisContractTests
- 13. Backend Contract
- 16. 驗證命令
- UtilityService
- GoogleOAuthOperationLock
- 10. Slash 與 Interaction Cutover
- 6. 目標架構
- 7. 狀態機
- 9. Role 隔離政策
- TwitchSpider
- .SendLocalizedConfirmAsync
- RenameVerificationLogChannel
- GuildConfig
- AdminSettingsNotifications
- TwitcastingSpider
- AdminSettingsYoutubeVerification
- MySqlDataStore
- YoutubeMemberRoleApplyResult
- TwitchAuthorizationLocalState
- DbEntity
- YoutubeChannelSpider
- TwitchEventSubMetricStatus
- .LoadSnapshotAsync
- PreconditionAttribute
- YoutubeMemberVerificationResult
- TwitchOAuthRefreshLock
- GetAllRegistedWebHookJson
- AdminSettingsSnapshot
- GuildSnapshot
- TwitchSubscriptionRolePolicyTests
- .LoadInteractionFrom
- GuildInfoResponse
- GracefulShutdown
- AdminSettingsCommandReply
- .GetCommandHelp
- MySqlComponentFixture
- YoutubeMemberProbeResultKind
- NotificationMetricEvent
- MainDbContextFactory
- TwitchAuthorizationChangedPayload
- GoogleOAuthUnlinkIntent
- NoticeTwitcastingStreamChannel
- NotificationDeliveryResult
- YoutubeMemberRoleResult
- 10. 手動驗收矩陣
- TwitchReconcileAction
- .LoadCommandFrom
- AdministrationComponent
- .ToMetricEvent
- .SendMessageToAllGuildAsync
- MySqlComponentFixture.cs
- .AllRegisteredCommandsHaveDescriptionsInEverySupportedLocale
- 14. 部署與回滾
- Help
- TwitchValidateTokenData
- TwitchStreamEventPayload
- 8. 分階段實作步驟
- NotificationChannelIssue
- AdminTwitchMessagesPayload
- TwitchRefreshPersistenceDecision
- NoticeType
- .Start
- LogLevel
- BotPlayingStatus
- RequestRoute
- YoutubeMemberAccessToken
- 6. Bot 實作
- AdminSettingsRequestEnvelope
- NotificationBusMetricResult
- ClusterQueryType
- TwitchProviderResultStatus
- HelpService
- .SaveVideosByType
- RecordYoutubeChannel
- YTChannelType
- LogFileRoute
- TopLevelModule
- all.sql
- TwitchReconcileRequestedPayload
- _Baseline_ExistingDb.sql
- `guild_config`
- NoticeTwitchStreamChannel
- CommandTextEqualityComparer
- 給未來 session 的信
- MemberOperationCoordinator
- AdminSettingsRole
- 4. 訊息契約：Redis Streams 通知匯流排
- 5. 語系模型與解析規則
- .OnlyAffiliateAndPartnerCanBeConfigured
- 10. 執行期互動本地化
- 5. 目標架構
- 5. Contract v1 additive 擴充
- TwitchRoleConfigurationResult
- YoutubeMemberSingleConfigurationQueueAction
- YoutubeMemberTokenCleanupConcurrencyTests
- 7. 資料庫變更
- 9. Slash Command Localization
- TwitchAuthorizationChangedPayload
- YTChannelType

## God Nodes (most connected - your core abstractions)
1. `MainDbContext` - 75 edges
2. `DiscordStreamNotifyBot.DataBase.Table` - 67 edges
3. `YoutubeDetectionService` - 65 edges
4. `YoutubeMemberService` - 64 edges
5. `DiscordStreamNotifyBot.DataBase` - 64 edges
6. `DiscordStreamNotifyBot.Shared` - 61 edges
7. `TwitchDetectionService` - 60 edges
8. `BotConfig` - 58 edges
9. `MainDbService` - 51 edges
10. `YoutubeStreamService` - 51 edges

## Surprising Connections (you probably didn't know these)
- `DebounceFixture` --references--> `UserLogin`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/TwitchChannelUpdateDebounceTests.cs → src/DiscordStreamNotifyBot.Shared/Messages/Notifications.cs
- `DebounceFixture` --references--> `UserName`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/TwitchChannelUpdateDebounceTests.cs → src/DiscordStreamNotifyBot.Shared/Messages/Notifications.cs
- `NotificationEmbedFactoryTests` --references--> `BotLocalizer`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/NotificationEmbedFactoryTests.cs → src/DiscordStreamNotifyBot.Notifier/Localization/BotLocalizer.cs
- `YoutubeMemberVideoLogMessageFormatterTests` --references--> `CommandDisplayResolver`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/YoutubeMemberVideoLogMessageFormatterTests.cs → src/DiscordStreamNotifyBot.Notifier/Localization/CommandDisplayResolver.cs
- `DebounceFixture` --references--> `TwitchChannelUpdateInfo`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/TwitchChannelUpdateDebounceTests.cs → src/DiscordStreamNotifyBot.Shared/Messages/Notifications.cs

## Import Cycles
- None detected.

## Communities (351 total, 19 thin omitted)

### Community 0 - "TwitchSubscriptionApiClient"
Cohesion: 0.12
Nodes (18): CancellationToken, DateTimeOffset, HttpResponseMessage, IHttpClientFactory, NotifierMetrics, Task, TwitchProviderResult, Status (+10 more)

### Community 1 - ".GetLocaleAsync"
Cohesion: 0.22
Nodes (14): Task, IChannel, CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, IChannel, NoticeType (+6 more)

### Community 2 - ".Warn"
Cohesion: 0.17
Nodes (13): PendingRefreshPersistence, CancellationToken, NotifierMetrics, Task, TimeSpan, TwitchBroadcasterAuthorization, TwitchAuthorizationAccessResult, AccessToken (+5 more)

### Community 3 - "DiscordStreamNotifyBot.Tests"
Cohesion: 0.09
Nodes (7): DiscordStreamNotifyBot.Scraper.Detection.Youtube, DiscordStreamNotifyBot.Auth, DiscordStreamNotifyBot.SharedService, DiscordStreamNotifyBot.Tests, DiscordStreamNotifyBot.SharedService.Twitch, Fact, NotificationBusConsumerOptionsTests

### Community 4 - "DiscordStreamNotifyBot.Shared.csproj"
Cohesion: 0.08
Nodes (23): Microsoft.EntityFrameworkCore.Design (9.0.3), Microsoft.EntityFrameworkCore.Relational (9.0.3), Microsoft.EntityFrameworkCore.Tools (9.0.3), Serilog (4.4.0), Serilog.Sinks.Console (6.1.1), Serilog.Sinks.File (7.0.0), Serilog.Sinks.Grafana.Loki (9.0.1), net8.0 (+15 more)

### Community 5 - "TwitchApiService"
Cohesion: 0.08
Nodes (29): CancellationToken, Clip, DateTime, EventSubSubscription, HttpClient, IReadOnlyList, Lazy, Regex (+21 more)

### Community 6 - "InteractionHandler"
Cohesion: 0.10
Nodes (20): DisplayName, Value, BotLocalizer, ChoiceDisplayAttribute, CommandDisplayResolver, DiscordSocketClient, Func, GuildLocaleService (+12 more)

### Community 7 - "偵測 → 匯流排 → 發送 路徑除錯"
Cohesion: 0.13
Nodes (13): 1. Shared — 定義契約, 2. Scraper — 偵測並 publish, 3. Notifier — 消費並發送, 動工前先讀一個既有平台, 收尾檢查, 新增偵測平台 / 通知事件, 步驟（依相依順序，Shared → Scraper → Notifier）, 偵測 → 匯流排 → 發送 路徑除錯 (+5 more)

### Community 8 - "AuthTokenTests"
Cohesion: 0.15
Nodes (11): TokenCrypto, TokenManager, ArgumentException, Fact, InlineData, Theory, AuthTokenTests, TokenPayload (+3 more)

### Community 9 - ".CheckPermissionsAsync"
Cohesion: 0.33
Nodes (5): CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task

### Community 10 - "YoutubeReminderPolicyTests"
Cohesion: 0.06
Nodes (34): DateTime, TimeSpan, YoutubeReminderApiAction, TreatAsStarted, TreatAsTimeChanged, YoutubeReminderBatchChangeAction, PublishAndReplaceTimer, PublishAndRunImmediately (+26 more)

### Community 11 - "NotificationEmbedFactoryTests"
Cohesion: 0.24
Nodes (8): Color, DateTime, Embed, Fact, InlineData, TableVideo, Theory, NotificationEmbedFactoryTests

### Community 12 - "DiscordStreamNotifyBot.Localization"
Cohesion: 0.11
Nodes (7): DiscordStreamNotifyBot.SharedService.AdminSettings, DiscordStreamNotifyBot.SharedService.Youtube, DiscordStreamNotifyBot.SharedService.Twitcasting, DiscordStreamNotifyBot.Interaction.Utility.Service, DiscordStreamNotifyBot.Localization, DiscordStreamNotifyBot.Interaction, DiscordStreamNotifyBot.SharedService.Google

### Community 13 - "Extensions"
Cohesion: 0.09
Nodes (18): ManagementBaseObject, Process, BotLocalizer, DiscordSocketClient, EmbedBuilder, GuildLocaleService, IDiscordInteraction, IEmote (+10 more)

### Community 14 - ".GetDbContext"
Cohesion: 0.10
Nodes (30): CancellationToken, Lease, SemaphoreSlim, Task, BotLocalizer, CancellationToken, CancellationTokenSource, ConcurrentDictionary (+22 more)

### Community 15 - "Extensions"
Cohesion: 0.14
Nodes (14): DateTime, DiscordSocketClient, EmbedBuilder, Func, ICommandContext, IEmote, IMessage, IMessageChannel (+6 more)

### Community 16 - ".ReconcileUserStateAsync"
Cohesion: 0.19
Nodes (11): DateTime, TwitchBroadcasterAuthorization, TwitchSpider, TwitchSpiderRemovalMetricReason, TwitchUserState, Authorization, Spider, UserId (+3 more)

### Community 17 - "會限 OAuth Token 儲存改走 MySQL（去 Redis 依賴）計畫"
Cohesion: 0.11
Nodes (18): Backend, Bot（本 repo）, MySQL（兩端都已連同一個庫）, 儲存層（現況為 Redis）, 加密與 blob 格式（兩端一致）, 加密金鑰處理, 影響檔案一覽, 待決策（給實作 session） (+10 more)

### Community 18 - ".SetMessage"
Cohesion: 0.26
Nodes (9): CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, NoticeType, RequireBotPermission, SlashCommand, Task (+1 more)

### Community 19 - "AdminSettingsMutationResult"
Cohesion: 0.07
Nodes (29): DiscordSocketClient, SocketGuild, AdminSettingsChannelValidator, CrawlerPolicy, BotLocalizer, Broadcaster, CancellationToken, DiscordSocketClient (+21 more)

### Community 20 - "MySqlDataStoreTests"
Cohesion: 0.39
Nodes (8): StoredToken, MySqlComponentFact, MySqlDataStore, Task, MySqlDataStoreTests, StoredToken, AccessToken, RefreshToken

### Community 21 - "YoutubeMemberAuthorizationService"
Cohesion: 0.26
Nodes (9): GoogleAuthorizationCodeFlow, CancellationToken, HttpClient, MySqlDataStore, Task, YoutubeMemberAuthorizationService, IsConfigured, YoutubeMemberTokenSnapshot (+1 more)

### Community 22 - "Log"
Cohesion: 0.16
Nodes (9): ILogEventSink, ITextFormatter, LogEvent, Logger, LoggerConfiguration, DeferredFileSink, Log, IsRunningInContainer (+1 more)

### Community 23 - "FakeTimeProvider"
Cohesion: 0.05
Nodes (46): FakeTimeProvider, GuildLocaleRequest, DateTimeOffset, Func, List, TimeProvider, TimeSpan, NoticeCache (+38 more)

### Community 24 - "Twitch 訂閱驗證實作計畫"
Cohesion: 0.05
Nodes (36): 10. Frontend 調整, 11. 安全與錯誤處理, 12.1 Backend, 12.2 Bot, 12.3 Frontend, 12. 自動化測試, 13. 手動驗收, 14. 實作順序 (+28 more)

### Community 25 - "YoutubeMemberCheck"
Cohesion: 0.10
Nodes (16): ComponentInteraction, Task, YoutubeMemberComponent, IEnumerable, IReadOnlyCollection, IReadOnlyList, YoutubeMemberSelectionTransition, DateTime (+8 more)

### Community 26 - "新增 TwitCasting 錄影委派計畫（小幫手 ↔ StreamRecordTools）"
Cohesion: 0.11
Nodes (17): 1. 背景與動機, 2. 新增跨 repo 契約, 3. A（小幫手）改動, 4. B（StreamRecordTools）改動, 5. 部署順序與相容性, 6. 驗證, 7. 影響範圍, A1. `Shared/RedisChannels.cs` (+9 more)

### Community 27 - "多語系支援計畫"
Cohesion: 0.20
Nodes (10): 15. 預期修改檔案, 16. 完成定義, 1. 背景, 2. 目標, 3. 非目標, 4. 已確認的產品決策, 8.1 首次設定流程, 8.2 語系設定指令 (+2 more)

### Community 28 - "Serilog Logging 遷移計畫"
Cohesion: 0.09
Nodes (23): 10. 預期修改檔案, 11. 完成定義, 1. 背景, 2. 目標, 3. 非目標, 4. 技術選型, 6.1 例外事件, 6. Facade 相容契約 (+15 more)

### Community 29 - "12. 分階段執行"
Cohesion: 0.22
Nodes (9): 12. 分階段執行, 階段 0：建立基準與字串清冊, 階段 1：Localization 基礎與繁中資源化, 階段 2：資料庫與語系設定, 階段 3：Slash command 註冊本地化, 階段 4：共用互動、Help 與首次設定, 階段 5：一般 Interaction 模組, 階段 6：背景通知與會限 DM (+1 more)

### Community 30 - "YoutubeDetectionService"
Cohesion: 0.10
Nodes (19): ConcurrentBag, IsDeleted, ConcurrentDictionary, HttpClient, IHttpClientFactory, Video, YoutubeApiService, YouTubeService (+11 more)

### Community 31 - ".Main"
Cohesion: 0.10
Nodes (16): DiscordStreamNotifyBot.Coordinator, Counter, Gauge, HashSet, StreamGroupInfo, CoordinatorMetrics, CancellationToken, ClusterService (+8 more)

### Community 32 - "YoutubeMemberLifecycleTaskRegistry"
Cohesion: 0.14
Nodes (10): ConcurrentDictionary, DateTime, IEnumerable, Task, TimeSpan, YoutubeMemberLifecyclePolicy, YoutubeMemberLifecycleTaskRegistry, Fact (+2 more)

### Community 33 - "TwitchSpider"
Cohesion: 0.17
Nodes (11): DateTime, TwitchSpider, DateAdded, GuildId, IsRecord, IsWarningUser, OfflineImageUrl, ProfileImageUrl (+3 more)

### Community 34 - "AGENTS.md"
Cohesion: 0.12
Nodes (9): Console 備援, Grafana Dashboard, Log 與 Loki, Loki 主動推送, Serilog Pipeline, 排障, 檔案路由, License (+1 more)

### Community 35 - "YoutubeChannelOwnedType"
Cohesion: 0.25
Nodes (7): DateTime, YTChannelType, YoutubeChannelOwnedType, ChannelId, ChannelTitle, ChannelType, DateAdded

### Community 36 - "TwitchOAuthRefreshLockLease"
Cohesion: 0.10
Nodes (24): DateTime, PendingRefreshPersistence, CancellationToken, CancellationTokenSource, RedisKey, RedisValue, Task, TwitchOAuthRefreshLockAcquireResult (+16 more)

### Community 37 - ".MissingOrShortKeyIsRejected"
Cohesion: 0.25
Nodes (5): Fact, InlineData, InvalidOperationException, Theory, ProviderTokenEncryptionKeyTests

### Community 38 - "TopLevelModule"
Cohesion: 0.13
Nodes (18): InteractionModuleBase, BotLocalizer, CommandDisplayResolver, GuildLocaleService, LocaleResolver, TopLevelModule, BotLocalizer, CommandDisplayResolver (+10 more)

### Community 39 - "YoutubeMemberRoleService"
Cohesion: 0.21
Nodes (12): CancellationToken, DiscordSocketClient, GuildYoutubeMemberConfig, IEnumerable, IRole, SocketGuild, Task, YoutubeMemberRoleConfigurationResult (+4 more)

### Community 40 - "TwitchDetectionService"
Cohesion: 0.13
Nodes (15): ChannelUpdate, CancellationTokenSource, ConcurrentDictionary, IReadOnlyCollection, IReadOnlyDictionary, RedisValue, ScraperMetrics, SemaphoreSlim (+7 more)

### Community 41 - "Twitch"
Cohesion: 0.36
Nodes (10): Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task, TwitchService (+2 more)

### Community 42 - "ScraperMetrics"
Cohesion: 0.06
Nodes (34): EventSubSubscription, Counter, Gauge, ScraperMetricResult, Failure, Success, ScraperMetrics, TwitchAuthorizationChangeMetricResult (+26 more)

### Community 43 - "GuildLocaleService"
Cohesion: 0.07
Nodes (27): CacheEntry, CultureInfo, CancellationToken, ConcurrentDictionary, DateTimeOffset, Dictionary, Func, GuildConfig (+19 more)

### Community 44 - "TwitchChannelUpdateChange"
Cohesion: 0.18
Nodes (10): IEnumerable, IReadOnlyList, TwitchChannelEventFacts, TwitchChannelUpdateBatch, TwitchChannelUpdateChange, HasChanges, TwitchChannelUpdatePolicy, DateTime (+2 more)

### Community 45 - "RedisChannels"
Cohesion: 0.11
Nodes (10): AdminSettings, Cluster, Member, Notifier, OAuth, RedisChannels, SharedState, Twitcasting (+2 more)

### Community 46 - "13. 驗證矩陣"
Cohesion: 0.25
Nodes (8): 13.1 編譯與靜態檢查, 13.2 Slash command 註冊, 13.3 Locale resolver, 13.4 首次設定, 13.5 通知, 13.6 YouTube 會限驗證, 13.7 範圍守衛, 13. 驗證矩陣

### Community 47 - "SharedExtensions"
Cohesion: 0.09
Nodes (15): DbUpdateConcurrencyException, HoloVideos, NijisanjiVideos, NonApprovedVideos, OtherVideos, DateTime, EmbedBuilder, Video (+7 more)

### Community 48 - "網頁管理設定：30 秒請求與背景清理實作計畫"
Cohesion: 0.10
Nodes (19): 10. 實作順序, 11. 不在本次實作, 1. 目標, 2. 已確認決策, 3. 端點範圍與 deadline, 4. Cross-project contract, 5.1 Controller, 5.2 Redis bridge (+11 more)

### Community 49 - "Administration"
Cohesion: 0.32
Nodes (12): GuildInfoResponse, InviteResponse, Alias, Command, DiscordSocketClient, NotificationChannelCheckResponse, RequireContext, RequireOwner (+4 more)

### Community 50 - "網頁管理設定中心：爬蟲與會員驗證實作計畫"
Cohesion: 0.08
Nodes (25): 11. 實作順序, 12. 完成閘門, 13. 新 Session 交接指令, 1. 目標, 2.1 爬蟲, 2.2 YouTube 會員驗證, 2.3 Twitch 訂閱驗證, 2. 完成範圍 (+17 more)

### Community 51 - "水平擴展（三層拆分）計畫 — Redis Streams 版"
Cohesion: 0.10
Nodes (21): 10. 可優化項目（claude 分支已有成品，對應階段順手移植）, 11. 驗證清單（部署前全過）, 1. 目標架構, 2.1 `Shared`（共用 library）, 2.2 `Scraper`（爬蟲層，叢集唯一）, 2.3 `Notifier`（通知層 / shard，可多個）, 2.4 `Coordinator`（主控層，1 個）, 2.5 SharedService 逐服務拆分歸屬（判斷準則表） (+13 more)

### Community 52 - "YoutubeMemberSetting"
Cohesion: 0.27
Nodes (10): CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, GuildYoutubeMemberConfig, IRole, RequireGuildMemberCount, SlashCommand (+2 more)

### Community 53 - "TwitchStateDecisions.cs"
Cohesion: 0.11
Nodes (20): DateTime, TimeSpan, TwitchChannelStateFacts, TwitchChannelUpdateAction, Ignore, Queue, RefreshState, TwitchChannelUpdateDecision (+12 more)

### Community 54 - "Utility"
Cohesion: 0.27
Nodes (10): DefaultMemberPermissions, DiscordSocketClient, DiscordWebhookClient, IChannel, ITextChannel, RequireContext, RequireUserPermission, SlashCommand (+2 more)

### Community 55 - ".BuildVariant"
Cohesion: 0.15
Nodes (16): BotLocalizer, DateTime, EmbedBuilder, IReadOnlyCollection, TimeSpan, TwitchEmbedBuilderFactory, Embed, MessageComponent (+8 more)

### Community 56 - "AdministrationService"
Cohesion: 0.17
Nodes (9): DiscordSocketClient, Expected, IReadOnlyCollection, ITextChannel, Responded, SocketGuild, Task, AdministrationService (+1 more)

### Community 57 - "AGENTS.md"
Cohesion: 0.17
Nodes (11): Build & Run, Conventions, EF Core 鐵則, graphify, 制度條款, 外部契約（不可片面更改）, 指令文件, 架構要點（現行樹） (+3 more)

### Community 58 - "TwitchRefreshRotationLifecycle"
Cohesion: 0.18
Nodes (12): Action, Dictionary, Lease, Task, TaskCompletionSource, Lease, TwitchRefreshRotationLifecycle, ActiveOperationCount (+4 more)

### Community 59 - "MainDbContext"
Cohesion: 0.05
Nodes (48): BannerChange, DbContext, GoogleOAuthUnlinkIntent, RecordYoutubeChannel, DbSet, GuildConfig, GuildTwitchSubscriptionConfig, GuildYoutubeMemberConfig (+40 more)

### Community 60 - "DiscordStreamNotifyBot.Shared"
Cohesion: 0.09
Nodes (13): DiscordStreamNotifyBot.Tests.Component.Redis, DiscordStreamNotifyBot.HttpClients, DiscordStreamNotifyBot.Scraper, DiscordStreamNotifyBot.Shared, DiscordStreamNotifyBot.Command.YoutubeMember, DiscordStreamNotifyBot.Interaction.OwnerOnly.Service, DiscordStreamNotifyBot, Program (+5 more)

### Community 61 - ".Info"
Cohesion: 0.07
Nodes (24): NowStreamingHost, ComponentInteraction, Task, SpiderManagementComponent, BotLocalizer, CommandDisplayResolver, DiscordSocketClient, Embed (+16 more)

### Community 62 - "graphify reference: extra exports and benchmark"
Cohesion: 0.22
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 63 - "Bot"
Cohesion: 0.07
Nodes (25): BotPlayingStatus, ConnectionMultiplexer, DiscordSocketClient, IDatabase, ISubscriber, IUser, Task, Timer (+17 more)

### Community 64 - "DiscordStreamNotifyBot.SharedService.YoutubeMember"
Cohesion: 0.08
Nodes (13): DiscordStreamNotifyBot.SharedService.YoutubeMember, DiscordStreamNotifyBot.Interaction.YoutubeMember, GoogleCredential, YoutubeMemberAuthorizationResult, YoutubeMemberAuthorizationStatus, AuthorizationInvalid, LocalContractFailure, Ready (+5 more)

### Community 65 - "TwitchReconcileDecisionTests"
Cohesion: 0.21
Nodes (8): TwitchSpiderRemovalMetricReason, TwitchReconcileFacts, TwitchSpiderRemovalFacts, TwitchSpiderRemovalPolicy, Fact, InlineData, Theory, TwitchReconcileDecisionTests

### Community 66 - ".FilterNoNotifyGuilds"
Cohesion: 0.37
Nodes (4): IEnumerable, ArgumentNullException, Fact, NoNotifyGuildFilterTests

### Community 67 - ".Main"
Cohesion: 0.11
Nodes (13): AssemblyInformationalVersionAttribute, Assembly, CancellationToken, Exception, HashSet, PeriodicTimer, Task, Program (+5 more)

### Community 68 - "AddManualMemberCheckVideoFlag"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, AddManualMemberCheckVideoFlag

### Community 69 - "AdminSettingsService"
Cohesion: 0.16
Nodes (6): CancellationToken, DiscordSocketClient, GuildLocaleService, JObject, Task, AdminSettingsService

### Community 70 - "NotificationContractTests"
Cohesion: 0.28
Nodes (5): DateTime, Fact, JObject, YTChannelType, NotificationContractTests

### Community 71 - "EF Core 遷移與基線化（本專案版）"
Cohesion: 0.25
Nodes (7): EF Core 遷移與基線化（本專案版）, 一次性基線化（舊的 EnsureCreated 正式庫）, 一般變更流程, 你必須先知道的三件專案特例, 啟動時不碰資料庫（重要）, 套用：本地/開發 vs 正式環境, 收尾

### Community 72 - "GuildTwitchSubscriptionConfig"
Cohesion: 0.10
Nodes (19): AddRoleIds, IQueryable, IReadOnlySet, RemoveRoleIds, TwitchSubscriptionConfigurationQueries, Func, IReadOnlyList, TwitchSubscriptionRolePolicy (+11 more)

### Community 73 - "11. 通知與背景訊息"
Cohesion: 0.29
Nodes (7): 11.1 現況限制, 11.2 目標作法, 11.3 YouTube, 11.4 Twitch, 11.5 TwitCasting, 11.6 YouTube 會限驗證, 11. 通知與背景訊息

### Community 74 - "GoogleOAuthOperationLockLease"
Cohesion: 0.13
Nodes (17): CancellationToken, CancellationTokenSource, Exception, RedisKey, RedisValue, Task, ValueTask, GoogleOAuthOperationLockAcquireResult (+9 more)

### Community 75 - "InteractionMetadataFixture"
Cohesion: 0.13
Nodes (13): IServiceProvider, IServiceScope, IServiceScopeFactory, Dictionary, DiscordSocketClient, InteractionService, Type, InteractionMetadataFixture (+5 more)

### Community 80 - "YoutubeStream"
Cohesion: 0.10
Nodes (29): Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task, YoutubeStream (+21 more)

### Community 81 - "TwitchService"
Cohesion: 0.08
Nodes (28): SocketGuild, BotLocalizer, CancellationToken, Clip, DateTime, DiscordSocketClient, EmojiService, EventSubSubscription (+20 more)

### Community 82 - "AdminSettingsContractTests"
Cohesion: 0.15
Nodes (9): ActionRowComponent, ButtonComponent, RequestRoute, Func, Fact, InlineData, MessageComponent, Theory (+1 more)

### Community 83 - "AdminSettings.cs"
Cohesion: 0.07
Nodes (27): AdminProbeVideoPayload, SourceId, Video, AdminRemoveNotificationPayload, Source, AdminSetChannelPayload, ChannelId, AdminSetLocalePayload (+19 more)

### Community 84 - ".PublishYoutubeNotificationAsync"
Cohesion: 0.17
Nodes (10): GeneratedRegex, HttpRequestException, YTChannelType, DateTime, List, Regex, Task, Video (+2 more)

### Community 85 - "graphify reference: query, path, explain"
Cohesion: 0.33
Nodes (5): For /graphify explain, For /graphify path, graphify reference: query, path, explain, Step 0 — Constrained query expansion (REQUIRED before traversal), Step 1 — Traversal

### Community 86 - "自動化測試導入計畫"
Cohesion: 0.17
Nodes (12): 10. 測試實作規則, 1. 目標, 2. 測試分類, 3. 不移除的啟動檢查, 4. 第一批：低耦合契約與格式化, 5. 第二批：小幅抽出純邏輯, 6. 第三批：時間與快取, 7. 第四批：Scraper 狀態機 (+4 more)

### Community 87 - "Task"
Cohesion: 0.11
Nodes (14): DateTime, IEnumerable, TableVideo, Task, YTApiVideo, CancellationToken, Exception, IEnumerable (+6 more)

### Community 88 - "DescriptionOnlyLocalizationManager"
Cohesion: 0.33
Nodes (6): ILocalizationManager, ResxLocalizationManager, IDictionary, IList, LocalizationTarget, DescriptionOnlyLocalizationManager

### Community 89 - ".Get"
Cohesion: 0.11
Nodes (14): MissingManifestResourceException, Dictionary, DictionaryEntry, Regex, ResourceManager, BotLocalizer, YoutubeMemberVideoLogMessageFormatter, ArgumentException (+6 more)

### Community 90 - "YoutubeMemberService"
Cohesion: 0.09
Nodes (24): CheckId, Snapshot, SocketMessageComponent, YoutubeMemberNotMemberApplyResult, YoutubeMemberService, CancellationToken, IEnumerable, List (+16 more)

### Community 91 - ".CreateAsyncClient"
Cohesion: 0.21
Nodes (15): HttpMessageHandler, HttpStatusCode, IHttpClientFactory, CancellationToken, Fact, Func, HttpClient, HttpRequestMessage (+7 more)

### Community 92 - "Video"
Cohesion: 0.17
Nodes (16): BotLocalizer, DateTime, EmbedBuilder, TimeSpan, YTApiVideo, EmbedBuilderFactory, DateTime, Video (+8 more)

### Community 93 - "6. 資源架構"
Cohesion: 0.40
Nodes (5): 6.1 指令註冊資源, 6.2 執行期訊息資源, 6.3 Help 長文, 6.4 Localizer API, 6. 資源架構

### Community 94 - "NotificationBusConsumer"
Cohesion: 0.18
Nodes (17): CancellationToken, Func, IDatabase, StreamEntry, Task, TimeSpan, TwitcastingService, TwitchService (+9 more)

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
Cohesion: 0.07
Nodes (26): BotConfig, ApiServerDomain, DiscordToken, ECPayEmoteId, EnableGuildMembersIntent, GoogleApiKey, GoogleClientId, GoogleClientSecret (+18 more)

### Community 103 - ".New"
Cohesion: 0.31
Nodes (4): ConsoleColor, LogFileRoute, LogLevel, Exception

### Community 105 - "DiscordWebhookClient"
Cohesion: 0.21
Nodes (9): CancellationToken, DiscordSocketClient, HttpClient, Task, DiscordWebhookClient, Message, avatar_url, content (+1 more)

### Community 106 - "DiscordStreamNotifyBot.DataBase"
Cohesion: 0.13
Nodes (9): DiscordStreamNotifyBot.Interaction.Utility, DiscordStreamNotifyBot.Interaction.Attribute, DiscordStreamNotifyBot.Interaction.TwitCasting, DiscordStreamNotifyBot.Command.Admin, DiscordStreamNotifyBot.Interaction.Help.Service, DiscordStreamNotifyBot.Interaction.Twitch, DiscordStreamNotifyBot.SharedService.Cluster, DiscordStreamNotifyBot.Interaction.Youtube (+1 more)

### Community 113 - ".CreateOrRepairConfigurationAsync"
Cohesion: 0.23
Nodes (11): CancellationToken, DiscordSocketClient, Exception, GuildTwitchSubscriptionConfig, ICollection, IRole, NotifierMetrics, SocketGuild (+3 more)

### Community 114 - ".SendStreamMessageAsync"
Cohesion: 0.13
Nodes (14): ArgumentOutOfRangeException, Event, Platform, HttpException, Embed, HttpException, MessageComponent, TimeoutException (+6 more)

### Community 115 - "DiscordStreamNotifyBot.Notifier.csproj"
Cohesion: 0.10
Nodes (19): Microsoft.Extensions.DependencyInjection.Abstractions (10.0.1), System.Management (10.0.1), net8.0, Ben.Demystifier (0.4.1), Discord.Net (3.20.1), Dorssel.Utilities.Debounce (3.0.0), EFCore.NamingConventions (9.0.0), Google.Apis.YouTube.v3 (1.73.0.3981) (+11 more)

### Community 116 - "DiscordStreamNotifyBot.DataBase.Table"
Cohesion: 0.14
Nodes (7): DiscordStreamNotifyBot.Tests.Component.MySql, DiscordStreamNotifyBot.DataBase.Table, DiscordStreamNotifyBot.Interaction.TwitchSubscription, DiscordStreamNotifyBot.SharedService.Member, DiscordStreamNotifyBot.SharedService.TwitchSubscription, Fact, YoutubeMembershipSchemaContractTests

### Community 117 - "Normal"
Cohesion: 0.26
Nodes (8): DiscordStreamNotifyBot.Command.Normal, Alias, Command, DiscordSocketClient, DiscordWebhookClient, Summary, Task, Normal

### Community 118 - "ClusterService"
Cohesion: 0.23
Nodes (7): IDatabase, Task, TimeSpan, ClusterService, RedisComponentFact, Task, ClusterServiceRedisComponentTests

### Community 119 - "Movie"
Cohesion: 0.05
Nodes (39): Broadcaster, Created, Id, Image, IsLive, LastMovieId, Level, Name (+31 more)

### Community 120 - "Twitch OAuth 與零成本 EventSub 實作計畫"
Cohesion: 0.14
Nodes (13): 0. 涉及專案, 10. Backend EventSub Webhook, 12. Frontend, 14. Grafana, 18. 建置與遷移, 19. 部署順序, 1. 不可偏離的決策, 20. 官方參考 (+5 more)

### Community 121 - ".StartAndBlockAsync"
Cohesion: 0.09
Nodes (22): AdminSettingsService, BotLocalizer, CommandDisplayResolver, EmojiService, GuildLocaleService, InteractionService, LocaleResolver, MemberOperationCoordinator (+14 more)

### Community 122 - ".Classify"
Cohesion: 0.24
Nodes (8): GoogleApiException, YouTubeService, CancellationToken, GoogleCredential, HashSet, IEnumerable, Task, YoutubeMemberApiClient

### Community 123 - "AutocompleteCandidate"
Cohesion: 0.33
Nodes (5): AutocompleteCandidate, Name, SearchTerms, Fact, AutocompleteSearchTests

### Community 124 - "16. 執行階段"
Cohesion: 0.22
Nodes (9): 16. 執行階段, 階段 0：前置確認, 階段 1：資料模型與 Backend 設定, 階段 2：Google/Twitch OAuth 隔離, 階段 3：Frontend, 階段 4：Twitch add資格與授權清理, 階段 5：StreamOnline 與 EventSub reconcile, 階段 6：Prometheus 與 Grafana (+1 more)

### Community 125 - "Prometheus / Grafana 監控"
Cohesion: 0.20
Nodes (9): Backend 指標, Coordinator 指標, Endpoints, Grafana, Notifier 指標, Prometheus, Prometheus / Grafana 監控, Scraper 指標 (+1 more)

### Community 126 - "TwitcastingClient"
Cohesion: 0.14
Nodes (11): DiscordStreamNotifyBot.Command.TwitCasting, Alias, Command, RequireContext, RequireOwner, Task, TwitCasting, HttpClient (+3 more)

### Community 127 - "DiscordStreamNotifyBot.Scraper.csproj"
Cohesion: 0.50
Nodes (3): net8.0, prometheus-net.AspNetCore (8.2.1), Microsoft.NET.Sdk

### Community 128 - "DiscordStreamNotifyBot.Tests.csproj"
Cohesion: 0.25
Nodes (7): coverlet.collector (6.0.0), Microsoft.Extensions.TimeProvider.Testing (9.0.0), Microsoft.NET.Test.Sdk (17.8.0), xunit (2.5.3), xunit.runner.visualstudio (2.5.3), net8.0, Microsoft.NET.Sdk

### Community 129 - "17. 驗證矩陣"
Cohesion: 0.33
Nodes (6): 17.1 新增 spider, 17.2 EventSub, 17.3 授權失效, 17.4 OAuth, 17.5 Prometheus/Grafana, 17. 驗證矩陣

### Community 130 - ".Init"
Cohesion: 0.36
Nodes (5): DiscordSocketClient, HttpClient, Task, Timer, UptimeKumaClient

### Community 131 - "TwitchEventSubEnsureResult"
Cohesion: 0.08
Nodes (30): EventSubSubscription, IReadOnlyList, Stream, TwitchEventSubDeleteResult, DeletedSubscriptionIds, Status, TwitchEventSubDeleteStatus, ApiFailure (+22 more)

### Community 132 - "7. OAuth API 與流程隔離"
Cohesion: 0.40
Nodes (5): 7.1 API, 7.2 State, 7.3 Callback, 7.4 Twitch scopes, 7. OAuth API 與流程隔離

### Community 133 - "TwitcastingLiveStartPlannerTests"
Cohesion: 0.21
Nodes (8): TwitcastingLiveStartFacts, TwitcastingLiveStartPlanner, TwitcastingLiveStartEvent, TwitcastingWebhookParser, Fact, InlineData, Theory, TwitcastingLiveStartPlannerTests

### Community 134 - "ClusterQueryService"
Cohesion: 0.09
Nodes (31): ChannelInfo, ClusterQueryType, NotificationChannelIssue, QueryRequest, Replies, Responses, Expected, Func (+23 more)

### Community 135 - ".Plan"
Cohesion: 0.24
Nodes (11): HashSet, IEnumerable, IReadOnlyList, TwitcastingWebhookAction, TwitcastingWebhookActionKind, RegisterLiveStart, RemoveLiveStart, TwitcastingWebhookRegistration (+3 more)

### Community 137 - ".RunAsync"
Cohesion: 0.27
Nodes (8): ServiceProvider, DetectionHost, Task, CancellationToken, PeriodicTimer, Task, TimeSpan, ScraperService

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
Nodes (23): DiscordStreamNotifyBot.Interaction.Help, AutocompletionResult, HelpService, IAutocompleteInteraction, IInteractionContext, InteractionService, IParameterInfo, IReadOnlyList (+15 more)

### Community 144 - "13. Prometheus"
Cohesion: 0.67
Nodes (3): 13.1 Backend 指標, 13.2 Scraper 指標, 13. Prometheus

### Community 145 - "4. 安全刪除狀態機"
Cohesion: 0.67
Nodes (3): 4.1 直播中授權失效, 4.2 關台後重新判斷, 4. 安全刪除狀態機

### Community 146 - "YoutubeMemberPolicies"
Cohesion: 0.14
Nodes (11): CancellationToken, GuildYoutubeMemberConfig, YoutubeMemberAccessToken, YoutubeMemberCheckStateSnapshot, YoutubeMemberPolicies, YoutubeMemberProbeConfigurationSnapshot, Fact, InlineData (+3 more)

### Community 147 - "YoutubeMemberVideoLogNotification"
Cohesion: 0.20
Nodes (12): YoutubeMemberVideoLogNotification, BotOwnerMessage, CheckChannelId, IsNeedRemove, IsNeedSendToOwner, Message, MessageArguments, MessageCode (+4 more)

### Community 148 - "MainDbContextModelSnapshot.cs"
Cohesion: 0.33
Nodes (4): ModelSnapshot, DateTime, ModelBuilder, MainDbContextModelSnapshot

### Community 149 - "DiscordStreamNotifyBot.Command.Attribute"
Cohesion: 0.13
Nodes (10): Attribute, DiscordStreamNotifyBot.Command.Youtube, DiscordStreamNotifyBot.Command.Attribute, DiscordStreamNotifyBot.Command.Twitch, CommandExampleAttribute, ExpArray, CommandExampleAttribute, ExpArray (+2 more)

### Community 150 - "DiscordStreamNotifyBot.Migrations"
Cohesion: 0.22
Nodes (6): DiscordStreamNotifyBot.Migrations, DateTime, MigrationBuilder, DateTime, ModelBuilder, RefactorDbContext

### Community 151 - "ModifyTwitCastingTable"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, ModifyTwitCastingTable

### Community 152 - "AddMaxSpiderCountSettingField"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, AddMaxSpiderCountSettingField

### Community 153 - "Migration"
Cohesion: 0.20
Nodes (6): Migration, DateTime, MigrationBuilder, DateTime, ModelBuilder, SyncModelDrift

### Community 154 - "AddTwitchBroadcasterAuthorization"
Cohesion: 0.22
Nodes (5): DateTime, MigrationBuilder, DateTime, ModelBuilder, AddTwitchBroadcasterAuthorization

### Community 155 - "AddLocalizationSettings"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, AddLocalizationSettings

### Community 156 - "IInteractionService"
Cohesion: 0.25
Nodes (7): Emote, IInteractionService, DiscordSocketClient, EmojiService, ECPayEmote, PayPalEmote, YouTubeEmote

### Community 157 - "SendMsgToAllGuildService"
Cohesion: 0.08
Nodes (25): ButtonCheckData, IInteractionService, SendAllPayload, ChoiceDisplayAttribute, DiscordSocketClient, Embed, HttpException, Task (+17 more)

### Community 158 - ".CheckRequirementsAsync"
Cohesion: 0.33
Nodes (5): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task

### Community 159 - "YoutubeMemberApiClientTests"
Cohesion: 0.24
Nodes (5): Fact, InlineData, Theory, YoutubeMemberApiClientTests, TokenResponseException

### Community 160 - "AdminYoutubeMessagesPayload"
Cohesion: 0.13
Nodes (13): AdminYoutubeMessagesPayload, ChangeTime, Delete, End, NewStream, NewVideo, Start, AdminYoutubeUpsertPayload (+5 more)

### Community 161 - "DiscordStreamNotifyBot.Shared.Messages"
Cohesion: 0.07
Nodes (22): DiscordStreamNotifyBot.Scraper.Detection.Twitch.Debounce, DiscordStreamNotifyBot.Scraper.Detection.Twitch, DiscordStreamNotifyBot.SharedService.Youtube.Json, DiscordStreamNotifyBot.Shared.Messages, ConnectionMultiplexer, IDatabase, ISubscriber, IUser (+14 more)

### Community 162 - ".MakeNamesUnique"
Cohesion: 0.36
Nodes (3): IEnumerable, IReadOnlyList, AutocompleteSearch

### Community 163 - "Movie"
Cohesion: 0.09
Nodes (22): Movie, Category, CommentCount, Country, Created, CurrentViewCount, Duration, HlsUrl (+14 more)

### Community 164 - "RedisComponentFixture"
Cohesion: 0.20
Nodes (11): ConfigurationOptions, IAsyncLifetime, ConnectionMultiplexer, IDatabase, RedisKey, Task, RedisComponentFixture, Connection (+3 more)

### Community 165 - "MemberRoleOwnershipSnapshot"
Cohesion: 0.16
Nodes (12): IEnumerable, IReadOnlyCollection, MemberEntitlementProvider, Twitch, Youtube, MemberRoleEntitlement, MemberRoleOwnershipPolicy, MemberRoleOwnershipSnapshot (+4 more)

### Community 166 - ".CheckRequirementsAsync"
Cohesion: 0.15
Nodes (9): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildMemberCountAttribute, ErrorMessage, GuildMemberCount (+1 more)

### Community 167 - ".NotifyAddedAsync"
Cohesion: 0.20
Nodes (10): Components, Embed, MessageComponent, SocketGuild, Task, CrawlerOwnerNotifier, CrawlerPlatform, Twitcasting (+2 more)

### Community 168 - "InteractionErrorPolicyTests"
Cohesion: 0.33
Nodes (5): Fact, InlineData, InteractionCommandError, Theory, InteractionErrorPolicyTests

### Community 169 - "TwitchNotification"
Cohesion: 0.10
Nodes (21): List, TwitchNoticeType, ChangeStreamData, EndStream, StartStream, TwitchNotification, Clips, ClipsValue (+13 more)

### Community 170 - "DebounceFixture"
Cohesion: 0.27
Nodes (11): CancellationToken, Fact, IReadOnlyCollection, List, Task, UserId, DebounceFixture, Batches (+3 more)

### Community 171 - ".CheckMemberShipCore"
Cohesion: 0.16
Nodes (13): SocketRole, SocketTextChannel, SocketGuild, Task, GuildYoutubeMemberConfig, DeletionPending, GuildId, IsManualVideoId (+5 more)

### Community 172 - "TwitchAccessTokenData"
Cohesion: 0.22
Nodes (9): TwitchAccessTokenData, AccessToken, ExpiresIn, RefreshToken, Scopes, TokenType, TwitchUserId, Fact (+1 more)

### Community 173 - "TwitchGuildEligibilityStatus"
Cohesion: 0.13
Nodes (17): ConcurrentDictionary, DateTime, Task, TimeProvider, TimeSpan, TwitchGuildEligibilityEvaluator, TwitchGuildEligibilityStatus, Eligible (+9 more)

### Community 174 - "AdminSettingsCrawlerPlatform"
Cohesion: 0.14
Nodes (15): Name, IEnumerable, AdminSettingsCrawlerItem, SourceId, SourceName, AdminSettingsCrawlerPlatform, Count, Enabled (+7 more)

### Community 175 - ".TryGetKey"
Cohesion: 0.27
Nodes (5): NotificationDedupPolicy, Fact, InlineData, Theory, NotificationDedupPolicyTests

### Community 176 - "TwitchSubscriptionPoliciesTests"
Cohesion: 0.11
Nodes (10): DateTimeOffset, IEnumerable, IReadOnlyCollection, TwitchAuthorizationEventPolicy, TwitchRateLimitPolicy, TwitchSubscriptionConfigurationPolicy, GuildTwitchSubscriptionConfig, InlineData (+2 more)

### Community 177 - "TwitchStream"
Cohesion: 0.12
Nodes (14): HelixStream, TwitchStreamDataFacts, TwitchStreamNotificationFactory, DateTime, TwitchStream, GameName, StreamId, StreamStartAt (+6 more)

### Community 178 - "CommonEqualityComparer"
Cohesion: 0.18
Nodes (5): IEqualityComparer, Func, CommonEqualityComparer, Func, CommonEqualityComparer

### Community 179 - ".Classify"
Cohesion: 0.14
Nodes (16): DateTime, YoutubeApiVideoAction, ActiveChatOnly, Ignore, IgnoreFakePost, NewVideo, Scheduled, Started (+8 more)

### Community 180 - ".CheckMemberShipOnlyVideoIdAsync"
Cohesion: 0.14
Nodes (15): Task, YoutubeMemberCandidateAction, AbortDiscovery, IgnoreCommentsDisabled, IgnorePublicVideo, IgnoreUnavailable, SelectMemberOnlyVideo, YoutubeMemberCandidateFacts (+7 more)

### Community 181 - ".ShutdownAsync"
Cohesion: 0.19
Nodes (8): DelegatingHandler, LogMessage, CancellationToken, HttpRequestMessage, HttpResponseMessage, Task, TimeSpan, LokiHttpMessageHandler

### Community 182 - "TwitcastingDetectionService"
Cohesion: 0.21
Nodes (9): Category, List, RedisValue, SemaphoreSlim, Task, TwitcastingDetectionService, IsEnable, Category (+1 more)

### Community 183 - ".ValidateCommandLocalizationResources"
Cohesion: 0.18
Nodes (8): ISet, Dictionary, DictionaryEntry, HashSet, IDictionary, IList, LocalizationTarget, ModuleInfo

### Community 184 - "YoutubePubSubNotification"
Cohesion: 0.15
Nodes (12): YoutubePubSubNotification, ChannelId, Link, NotificationType, Published, Title, Updated, VideoId (+4 more)

### Community 186 - "DiscordStreamNotifyBot.Command"
Cohesion: 0.16
Nodes (9): DiscordStreamNotifyBot.Command, SocketCommandContext, SocketMessage, CommandService, DiscordSocketClient, IServiceProvider, Task, CommandHandler (+1 more)

### Community 187 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildTwitCastingSpiderAutocompleteHandler

### Community 188 - "AutocompleteHandler"
Cohesion: 0.20
Nodes (9): AutocompleteHandler, AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildNoticeTwitchChannelIdAutocompleteHandler, GuildYoutubeChannelSpiderAutocompleteHandler (+1 more)

### Community 189 - "TwitchSubscriptionStatus"
Cohesion: 0.25
Nodes (7): TwitchSubscriptionStatus, AuthorizationInvalid, AuthorizationMissing, BroadcasterUnavailable, NotSubscribed, Subscribed, TemporaryFailure

### Community 190 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildTwitchSpiderAutocompleteHandler

### Community 191 - "TwitchChannelUpdateInfo"
Cohesion: 0.14
Nodes (14): CancellationTokenRegistration, DebouncedEventArgs, Debouncer, ObjectDisposedException, Func, IReadOnlyCollection, Task, DebounceChannelUpdateMessage (+6 more)

### Community 192 - "Category"
Cohesion: 0.21
Nodes (11): List, CategoriesJson, Categories, Category, Id, Name, SubCategories, SubCategory (+3 more)

### Community 193 - ".DecideAutomaticMutation"
Cohesion: 0.24
Nodes (6): YoutubeMemberAutomaticMutationAction, Apply, PreserveManualPin, YoutubeMemberManualPinPolicy, Fact, YoutubeMemberManualPinPolicyTests

### Community 194 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, ConfiguredBroadcasterAutocompleteHandler

### Community 195 - "BotStateTests"
Cohesion: 0.38
Nodes (3): InlineData, Theory, BotStateTests

### Community 196 - "NijisanjiStreamJson"
Cohesion: 0.08
Nodes (27): DateTime, List, Channel, Id, Liver, Main, Name, ThumbnailUrl (+19 more)

### Community 197 - ".GenerateSuggestionsAsync"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 198 - ".AddChannel"
Cohesion: 0.27
Nodes (10): CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, IChannel, RequireBotPermission, SlashCommand, Task (+2 more)

### Community 199 - "AdminSettingsYoutubeMessages"
Cohesion: 0.13
Nodes (15): AdminSettingsYoutubeMessages, ChangeTime, Delete, End, NewStream, NewVideo, Start, AdminSettingsYoutubeNotification (+7 more)

### Community 200 - "TcBackendStreamData.cs"
Cohesion: 0.07
Nodes (29): App, Mode, Url, BackendMovie, Id, Live, Fmp4, Host (+21 more)

### Community 201 - "AddTwitchSubscriptionVerification"
Cohesion: 0.22
Nodes (5): DateTime, MigrationBuilder, DateTime, ModelBuilder, AddTwitchSubscriptionVerification

### Community 202 - ".GroupName"
Cohesion: 0.20
Nodes (12): IDatabase, RedisKey, RedisValue, StreamEntry, StreamGroupInfo, Task, TimeSpan, NotificationBus (+4 more)

### Community 203 - "NotifierMetrics.cs"
Cohesion: 0.10
Nodes (19): TwitchSubscriptionRoleOperation, Remove, Synchronize, TwitchSubscriptionRoleResult, DiscordError, MissingPermission, Success, UnknownError (+11 more)

### Community 204 - ".GenerateSuggestionsAsync"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 205 - "MainDbService"
Cohesion: 0.14
Nodes (15): DbContextOptions, ComponentInteraction, GuildTwitchSubscriptionConfig, RequireContext, SlashCommand, Task, TwitchSubscription, TwitchSubscriptionComponent (+7 more)

### Community 206 - ".Plan"
Cohesion: 0.21
Nodes (11): HashSet, IReadOnlyCollection, IReadOnlyList, TwitchEventSubCreateSpec, TwitchEventSubFact, TwitchEventSubFinalDecision, IsSuccess, TwitchEventSubReconcilePlan (+3 more)

### Community 207 - "AddTwitchSubscriptionDeletionPending"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, AddTwitchSubscriptionDeletionPending

### Community 208 - ".CreateAsync"
Cohesion: 0.24
Nodes (9): Fact, GuildPermission, InlineData, SlashCommandParameterInfo, Task, Theory, Type, InteractionCommandContractTests (+1 more)

### Community 209 - ".ToLabel"
Cohesion: 0.13
Nodes (13): TwitchSubscriptionStatus, TwitchSubscriptionProviderError, InvalidResponse, NetworkFailure, Provider4xx, Provider5xx, RateLimited, YoutubeMemberCheckCycleResult (+5 more)

### Community 210 - "TwitchSpiderRemovalAction"
Cohesion: 0.22
Nodes (9): TwitchSpiderRemovalAction, AlreadyRemoved, DeferApiFailure, DeferLive, DeferNotifier, DeferSnapshot, EvaluateEligibility, Remove (+1 more)

### Community 211 - ".CheckPermissionsAsync"
Cohesion: 0.22
Nodes (7): CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task, RequireGuildOwnerAttribute, ErrorMessage

### Community 212 - ".CheckRequirementsAsync"
Cohesion: 0.22
Nodes (7): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildOwnerAttribute, ErrorMessage

### Community 213 - ".Format"
Cohesion: 0.48
Nodes (3): LogEventLevel, LogTextFormatter, TextWriter

### Community 214 - ".Resolve"
Cohesion: 0.57
Nodes (3): InteractionCommandError, InteractionErrorDescriptor, InteractionErrorPolicy

### Community 215 - "YouTube 會員驗證架構重構計畫"
Cohesion: 0.15
Nodes (12): 11. 排程與生命週期, 12. Provider Result 分類, 17. Manual Acceptance Matrix, 18. 停機部署順序, 19. Completion Criteria, 1. 範圍, 20. 新 Session 執行規則, 2. 已定案決策 (+4 more)

### Community 216 - "ReactionEventWrapper"
Cohesion: 0.26
Nodes (8): Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task, ReactionEventWrapper, Message

### Community 217 - "SocialLinks"
Cohesion: 0.17
Nodes (12): Head, Height, Url, Width, Images, Head, SocialLinks, FieldId (+4 more)

### Community 218 - "NotifierMetrics"
Cohesion: 0.21
Nodes (6): Histogram, Counter, Gauge, TimeSpan, NotifierMetrics, Fact

### Community 219 - "DiscordStreamNotifyBot.HttpClients.Twitcasting.Model"
Cohesion: 0.18
Nodes (9): DiscordStreamNotifyBot.HttpClients.Twitcasting.Model, DiscordStreamNotifyBot.Scraper.Detection.Twitcasting, DateTime, TwitcastingLiveStartAction, IgnoreDuplicate, PersistAndNotify, PersistRequestRecordingAndNotify, TwitcastingLiveStartPlan (+1 more)

### Community 220 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildNoticeTwitCastingChannelIdAutocompleteHandler

### Community 221 - ".SlashCommandExecuted"
Cohesion: 0.19
Nodes (8): IResult, SocketInteraction, SocketInteractionContext, SocketSlashCommandDataOption, IDiscordInteraction, IInteractionContext, SlashCommandInfo, Task

### Community 222 - "TwitchOfflineAction"
Cohesion: 0.13
Nodes (17): TwitchOfflineAction, ClearState, Defer, Ignore, PublishEnd, ResumeStream, TwitchOfflineFacts, TwitchOfflinePolicy (+9 more)

### Community 223 - "TwitcastingStream"
Cohesion: 0.13
Nodes (13): BotLocalizer, EmbedBuilder, TwitcastingEmbedBuilderFactory, DateTime, TwitcastingStream, Category, ChannelId, ChannelTitle (+5 more)

### Community 224 - "NoticeYoutubeStreamChannel"
Cohesion: 0.15
Nodes (12): NoticeYoutubeStreamChannel, ChangeTimeMessage, DeleteMessage, DiscordNoticeStreamChannelId, DiscordNoticeVideoChannelId, EndMessage, GuildId, IsCreateEventForNewStream (+4 more)

### Community 225 - ".LockGuildsAsync"
Cohesion: 0.22
Nodes (8): IAsyncDisposable, IDisposable, LeaseGroup, IEnumerable, List, ValueTask, Lease, LeaseGroup

### Community 226 - "15. 實作階段"
Cohesion: 0.20
Nodes (10): 15. 實作階段, Phase 0：Baseline 與 characterization, Phase 1：Schema 與 migration, Phase 2：共用操作與 role ownership, Phase 3：YouTube interaction 與 state machine, Phase 4：Role/config durability, Phase 5：Provider 與 lifecycle, Phase 6：Backend (+2 more)

### Community 227 - "AddGoogleOAuthUnlinkIntent"
Cohesion: 0.22
Nodes (5): DateTime, MigrationBuilder, DateTime, ModelBuilder, AddGoogleOAuthUnlinkIntent

### Community 228 - "NonPersistentGoogleDataStore"
Cohesion: 0.21
Nodes (5): IDataStore, Task, ITokenDataStore, Task, NonPersistentGoogleDataStore

### Community 229 - ".SendErrorMessageAsync"
Cohesion: 0.29
Nodes (10): IDMChannel, KeyNotFoundException, BotLocalizer, DiscordSocketClient, EmbedBuilder, HttpException, ITextChannel, IUserMessage (+2 more)

### Community 230 - "ReactionEventWrapper"
Cohesion: 0.29
Nodes (8): Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task, ReactionEventWrapper, Message

### Community 231 - "Broadcaster"
Cohesion: 0.09
Nodes (21): List, Broadcaster, Created, Id, Image, IsLive, LastMovieId, Level (+13 more)

### Community 232 - "YoutubeNotification"
Cohesion: 0.06
Nodes (37): TableVideo, DateTime, YTChannelType, BannerChangeNotification, ChannelId, VideoId, NotifyType, TwitcastingNotification (+29 more)

### Community 233 - "AddYoutubeMemberVerificationDurability"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, AddYoutubeMemberVerificationDurability

### Community 234 - ".SetMemberCheckVideoIdAsync"
Cohesion: 0.35
Nodes (9): Alias, Command, RequireContext, RequireOwner, Summary, Task, YoutubeMemberService, YoutubeStreamService (+1 more)

### Community 235 - "YouTube 會員驗證"
Cohesion: 0.33
Nodes (5): Durable state, YouTube 會員驗證, 使用者契約, 服務邊界, 部署前驗證

### Community 236 - "ReminderItem"
Cohesion: 0.27
Nodes (8): YTChannelType, ReminderItem, ChannelType, StreamVideo, Timer, ConcurrentDictionary, Fact, YoutubeReminderRegistryTests

### Community 237 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildNoticeYoutubeChannelIdAutocompleteHandler

### Community 238 - "14. Frontend"
Cohesion: 0.40
Nodes (5): 14.1 TypeScript contract, 14.2 GoogleSection, 14.3 VerifyWindow, 14.4 Copy/Privacy, 14. Frontend

### Community 239 - "8. DB Schema"
Cohesion: 0.40
Nodes (5): 8.1 Entity changes, 8.2 Indexes, 8.3 Migration 規則, 8.4 Preflight 查詢, 8. DB Schema

### Community 240 - "NijisanjiLiverJson"
Cohesion: 0.18
Nodes (10): List, NijisanjiLiverJson, EnName, Hidden, Id, Images, Name, Slug (+2 more)

### Community 241 - "AdminSettingsTwitchVerification"
Cohesion: 0.18
Nodes (11): Dictionary, AdminSettingsTwitchVerification, DeletionPending, PendingRoleRemovalCount, PreviousSubscriberRoleId, SourceId, SourceLogin, SourceName (+3 more)

### Community 242 - "TwitchBroadcasterAuthorization"
Cohesion: 0.06
Nodes (32): DbUpdateException, DateTime, TwitchBroadcasterAuthorization, AuthorizedAt, ClientId, DateUpdated, DiscordUserId, DisplayName (+24 more)

### Community 243 - "RedisContractTests"
Cohesion: 0.31
Nodes (4): Fact, InlineData, Theory, RedisContractTests

### Community 244 - "13. Backend Contract"
Cohesion: 0.50
Nodes (4): 13.1 Entity/DTO, 13.2 GET `/account-links`, 13.3 DELETE `/account-links/google`, 13. Backend Contract

### Community 245 - "16. 驗證命令"
Cohesion: 0.50
Nodes (4): 16.1 Bot, 16.2 Backend, 16.3 Frontend, 16. 驗證命令

### Community 246 - "UtilityService"
Cohesion: 0.42
Nodes (6): CancellationToken, DiscordSocketClient, IServiceProvider, SocketGuild, Task, UtilityService

### Community 247 - "GoogleOAuthOperationLock"
Cohesion: 0.22
Nodes (8): IConnectionMultiplexer, IDatabase, TimeSpan, GoogleOAuthOperationLock, DatabaseNumber, RedisComponentFact, Task, GoogleOAuthOperationLockRedisComponentTests

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
Nodes (7): CommandExample, CommandSummary, DefaultMemberPermissions, SlashCommand, Task, TwitchService, TwitchSpider

### Community 253 - ".SendLocalizedConfirmAsync"
Cohesion: 0.37
Nodes (5): RequireContext, SlashCommand, Task, YoutubeMemberCheck, YoutubeMember

### Community 254 - "RenameVerificationLogChannel"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, RenameVerificationLogChannel

### Community 255 - "GuildConfig"
Cohesion: 0.18
Nodes (10): GuildConfig, GuildId, Locale, MaxTwitcastingSpiderCount, MaxTwitchSpiderCount, MaxTwitterSpaceSpiderCount, MaxYouTubeMemberCheckCount, MaxYouTubeSpiderCount (+2 more)

### Community 256 - "AdminSettingsNotifications"
Cohesion: 0.12
Nodes (16): AdminSettingsNotifications, Twitcasting, Twitch, Youtube, AdminSettingsTwitcastingNotification, ChannelId, DetectionEnabled, SourceId (+8 more)

### Community 257 - "TwitcastingSpider"
Cohesion: 0.33
Nodes (8): CommandExample, CommandSummary, DefaultMemberPermissions, RequireGuildMemberCount, SlashCommand, Task, TwitcastingService, TwitcastingSpider

### Community 258 - "AdminSettingsYoutubeVerification"
Cohesion: 0.20
Nodes (10): AdminSettingsYoutubeVerification, DeletionPending, PendingRoleRemovalCount, PreviousRoleId, ProbeMode, ProbeVideoId, RoleId, SourceId (+2 more)

### Community 259 - "MySqlDataStore"
Cohesion: 0.36
Nodes (3): CancellationToken, Task, MySqlDataStore

### Community 260 - "YoutubeMemberRoleApplyResult"
Cohesion: 0.29
Nodes (6): YoutubeMemberRoleApplyResult, Applied, Failed, UnknownMember, InlineData, Theory

### Community 261 - "TwitchAuthorizationLocalState"
Cohesion: 0.28
Nodes (6): TwitchAuthorizationLocalState, Active, Missing, PersistedInvalid, TemporaryFailure, TwitchAuthorizationLocalStatePolicy

### Community 262 - "DbEntity"
Cohesion: 0.09
Nodes (18): BannerChange, ChannelId, GuildId, LastChangeStreamId, DateTime, DbEntity, DateAdded, Id (+10 more)

### Community 263 - "YoutubeChannelSpider"
Cohesion: 0.22
Nodes (8): DateTime, YoutubeChannelSpider, ChannelId, ChannelTitle, DateAdded, GuildId, IsTrustedChannel, LastSubscribeTime

### Community 264 - "TwitchEventSubMetricStatus"
Cohesion: 0.14
Nodes (14): TwitchEventSubMetricStatus, AuthorizationRevoked, BetaMaintenance, Enabled, ModeratorRemoved, NotificationFailuresExceeded, Unknown, UserRemoved (+6 more)

### Community 265 - ".LoadSnapshotAsync"
Cohesion: 0.42
Nodes (4): CancellationToken, ICollection, Task, MemberRoleOwnershipService

### Community 266 - "PreconditionAttribute"
Cohesion: 0.25
Nodes (6): PreconditionAttribute, RequireGuildMemberCountAttribute, ErrorMessage, GuildMemberCount, RequireGuildAttribute, GuildId

### Community 267 - "YoutubeMemberVerificationResult"
Cohesion: 0.15
Nodes (13): YoutubeMemberVerificationResult, CommentsDisabled, CredentialExpired, Member, NotMember, Provider4xx, Provider5xx, QuotaExceeded (+5 more)

### Community 268 - "TwitchOAuthRefreshLock"
Cohesion: 0.24
Nodes (8): IConnectionMultiplexer, IDatabase, TimeSpan, TwitchOAuthRefreshLock, DatabaseNumber, RedisComponentFact, Task, TwitchOAuthRefreshLockRedisComponentTests

### Community 269 - "GetAllRegistedWebHookJson"
Cohesion: 0.29
Nodes (7): List, GetAllRegistedWebHookJson, AllCount, Webhooks, Webhook, Event, UserId

### Community 270 - "AdminSettingsSnapshot"
Cohesion: 0.08
Nodes (28): List, AdminSettingsChannel, CanEmbedLinks, CanManageEvents, CanSendMessages, CanView, Id, Name (+20 more)

### Community 271 - "GuildSnapshot"
Cohesion: 0.15
Nodes (13): DiscordSocketClient, DateTime, List, GuildSnapshot, Id, MemberCount, Name, OwnerId (+5 more)

### Community 272 - "TwitchSubscriptionRolePolicyTests"
Cohesion: 0.28
Nodes (5): Fact, GuildTwitchSubscriptionConfig, InlineData, Theory, TwitchSubscriptionRolePolicyTests

### Community 273 - ".LoadInteractionFrom"
Cohesion: 0.29
Nodes (5): Assembly, Func, IEnumerable, IServiceCollection, Type

### Community 274 - "GuildInfoResponse"
Cohesion: 0.29
Nodes (7): Dictionary, GuildInfoResponse, Channels, MemberCount, Name, OwnerId, ShardId

### Community 275 - "GracefulShutdown"
Cohesion: 0.33
Nodes (4): CancellationToken, CancellationTokenSource, GracefulShutdown, Token

### Community 276 - "AdminSettingsCommandReply"
Cohesion: 0.25
Nodes (7): AdminSettingsCommandReply, Arguments, Code, ContractVersion, CorrelationId, ShardId, State

### Community 277 - ".GetCommandHelp"
Cohesion: 0.30
Nodes (7): RequireBotPermissionAttribute, RequireUserPermissionAttribute, EmbedBuilder, GuildPermission, IEnumerable, SlashCommandInfo, HelpService

### Community 278 - "MySqlComponentFixture"
Cohesion: 0.57
Nodes (3): Task, MySqlComponentFixture, DbService

### Community 279 - "YoutubeMemberProbeResultKind"
Cohesion: 0.18
Nodes (11): YoutubeMemberProbeResult, PreservesEntitlement, YoutubeMemberProbeResultKind, AuthorizationInvalid, LocalContractFailure, Member, NotMember, ProbeVideoInvalid (+3 more)

### Community 280 - "NotificationMetricEvent"
Cohesion: 0.18
Nodes (11): NotificationMetricEvent, TwitcastingStart, TwitchChangeData, TwitchEnd, TwitchStart, YoutubeChangeTime, YoutubeDelete, YoutubeEnd (+3 more)

### Community 281 - "MainDbContextFactory"
Cohesion: 0.40
Nodes (3): IDesignTimeDbContextFactory, Version, MainDbContextFactory

### Community 282 - "TwitchAuthorizationChangedPayload"
Cohesion: 0.67
Nodes (3): TwitchAuthorizationChangedPayload, Status, TwitchUserId

### Community 283 - "GoogleOAuthUnlinkIntent"
Cohesion: 0.33
Nodes (5): DateTime, GoogleOAuthUnlinkIntent, DateAdded, DiscordUserId, ExpectedEncryptedToken

### Community 284 - "NoticeTwitcastingStreamChannel"
Cohesion: 0.33
Nodes (5): NoticeTwitcastingStreamChannel, DiscordChannelId, GuildId, ScreenId, StartStreamMessage

### Community 285 - "NotificationDeliveryResult"
Cohesion: 0.20
Nodes (10): NotificationDeliveryResult, AuthorizationFailure, Disabled, Discord5xx, MissingChannel, MissingGuild, MissingPermission, Sent (+2 more)

### Community 286 - "YoutubeMemberRoleResult"
Cohesion: 0.20
Nodes (9): YoutubeMemberRoleOperation, Add, Remove, YoutubeMemberRoleResult, DiscordError, MissingPermission, Success, UnknownError (+1 more)

### Community 287 - "10. 手動驗收矩陣"
Cohesion: 0.40
Nodes (5): 10.1 授權, 10.2 爬蟲, 10.3 YouTube 會員驗證, 10.4 Twitch 訂閱驗證, 10. 手動驗收矩陣

### Community 288 - "TwitchReconcileAction"
Cohesion: 0.20
Nodes (10): TwitchReconcileAction, DeferApiFailure, DeferLive, DeleteSubscriptions, DeleteSubscriptionsThenEvaluateGuild, EnsureFallbackSubscriptions, EnsurePermanentSubscriptions, KeepPollingWithoutSubscriptions (+2 more)

### Community 289 - ".LoadCommandFrom"
Cohesion: 0.40
Nodes (4): Assembly, IEnumerable, IServiceCollection, Type

### Community 290 - "AdministrationComponent"
Cohesion: 0.40
Nodes (4): ComponentInteraction, NotificationChannelCheckResponse, Task, AdministrationComponent

### Community 291 - ".ToMetricEvent"
Cohesion: 0.31
Nodes (5): CollectorRegistry, InlineData, Task, Theory, NotifierMetricsTests

### Community 292 - ".SendMessageToAllGuildAsync"
Cohesion: 0.22
Nodes (7): DiscordStreamNotifyBot.Interaction.OwnerOnly, SendMsgToAllGuildService, DefaultMemberPermissions, RequireOwner, SlashCommand, Task, SendMsgToAllGuild

### Community 293 - "MySqlComponentFixture.cs"
Cohesion: 0.22
Nodes (6): FactAttribute, ICollectionFixture, MySqlComponentFactAttribute, MySqlComponentCollection, RedisComponentCollection, RedisComponentFactAttribute

### Community 294 - ".AllRegisteredCommandsHaveDescriptionsInEverySupportedLocale"
Cohesion: 0.40
Nodes (3): Fact, Task, InteractionCommandLocalizationTests

### Community 295 - "14. 部署與回滾"
Cohesion: 0.50
Nodes (4): 14.1 建議部署順序, 14.2 相容性, 14.3 回滾, 14. 部署與回滾

### Community 296 - "Help"
Cohesion: 0.36
Nodes (7): Alias, Command, CommandService, IServiceProvider, Summary, Task, Help

### Community 297 - "TwitchValidateTokenData"
Cohesion: 0.20
Nodes (9): TwitchTokenErrorData, Error, Message, TwitchValidateTokenData, ClientId, ExpiresIn, Login, Scopes (+1 more)

### Community 298 - "TwitchStreamEventPayload"
Cohesion: 0.50
Nodes (4): TwitchStreamEventPayload, BroadcasterUserId, BroadcasterUserLogin, BroadcasterUserName

### Community 299 - "8. 分階段實作步驟"
Cohesion: 0.22
Nodes (9): 8. 分階段實作步驟, 階段 0：止血 PR — shard 歸屬守衛, 階段 1：Solution 骨架 + Shared, 階段 2：Notifier 上線（先維持單 shard 行為）, 階段 3：Scraper 拆出 + Redis Streams 匯流排（完成，正確性待測試環境驗）, 階段 4：Coordinator（完成，正確性待測試環境驗）, 階段 5：跨 shard 指令與共享狀態（完成，正確性待測試環境驗）, 階段 6：Docker 化與部署驗證（檔案完成，實跑待測試環境） (+1 more)

### Community 300 - "NotificationChannelIssue"
Cohesion: 0.25
Nodes (8): NotificationChannelIssue, ChannelId, ChannelName, GuildId, GuildName, MissingPermissions, Platform, Usages

### Community 301 - "AdminTwitchMessagesPayload"
Cohesion: 0.25
Nodes (8): AdminTwitchMessagesPayload, Change, End, Start, AdminTwitchUpsertPayload, ChannelId, Messages, Source

### Community 302 - "TwitchRefreshPersistenceDecision"
Cohesion: 0.29
Nodes (5): TwitchRefreshPersistenceDecision, AlreadyPersisted, Stale, WriteReplacement, TwitchRefreshPersistencePolicy

### Community 303 - "NoticeType"
Cohesion: 0.29
Nodes (7): NoticeType, ChangeTime, Delete, End, NewStream, NewVideo, Start

### Community 304 - ".Start"
Cohesion: 0.29
Nodes (5): TwitchApiService, YoutubeApiService, TwitcastingDetectionService, TwitchDetectionService, YoutubeDetectionService

### Community 305 - "LogLevel"
Cohesion: 0.29
Nodes (7): LogLevel, Critical, Debug, Error, Info, Trace, Warn

### Community 306 - "BotPlayingStatus"
Cohesion: 0.33
Nodes (6): BotPlayingStatus, Guild, Info, Member, Stream, StreamCount

### Community 307 - "RequestRoute"
Cohesion: 0.33
Nodes (6): RequestRoute, Command, Ignore, Snapshot, UnsupportedAction, UnsupportedVersion

### Community 308 - "YoutubeMemberAccessToken"
Cohesion: 0.33
Nodes (5): DateTime, YoutubeMemberAccessToken, DateAdded, DiscordUserId, EncryptedAccessToken

### Community 309 - "6. Bot 實作"
Cohesion: 0.40
Nodes (5): 6.1 先抽共用 crawler service 流程, 6.2 補 verification 管理入口, 6.3 擴充 AdminSettings contract 與快照, 6.4 併發與 cancellation, 6. Bot 實作

### Community 310 - "AdminSettingsRequestEnvelope"
Cohesion: 0.22
Nodes (8): AdminSettingsRequestEnvelope, Action, ActorUserId, ContractVersion, CorrelationId, DeadlineUnixMs, GuildId, Payload

### Community 311 - "NotificationBusMetricResult"
Cohesion: 0.40
Nodes (5): NotificationBusMetricResult, Deduplicated, Dispatched, DispatchFailed, InvalidPayload

### Community 312 - "ClusterQueryType"
Cohesion: 0.40
Nodes (5): ClusterQueryType, GetInviteUrl, GuildInfo, NotificationChannelCheck, UserInfo

### Community 313 - "TwitchProviderResultStatus"
Cohesion: 0.40
Nodes (5): TwitchProviderResultStatus, Failure, Invalid, Success, TemporaryFailure

### Community 314 - "HelpService"
Cohesion: 0.46
Nodes (4): ICommandService, CommandInfo, EmbedBuilder, HelpService

### Community 315 - ".SaveVideosByType"
Cohesion: 0.40
Nodes (3): DbSet, MainDbContext, YTChannelType

### Community 316 - "RecordYoutubeChannel"
Cohesion: 0.40
Nodes (4): DateTime, RecordYoutubeChannel, DateAdded, YoutubeChannelId

### Community 317 - "YTChannelType"
Cohesion: 0.40
Nodes (5): YTChannelType, Holo, Nijisanji, NonApproved, Other

### Community 318 - "LogFileRoute"
Cohesion: 0.40
Nodes (5): LogFileRoute, Error, General, None, Stream

### Community 319 - "TopLevelModule"
Cohesion: 0.32
Nodes (5): ModuleBase, EmbedBuilder, Task, TopLevelModule, _service

### Community 321 - "TwitchReconcileRequestedPayload"
Cohesion: 0.67
Nodes (3): TwitchReconcileRequestedPayload, Reason, TwitchUserId

### Community 333 - "NoticeTwitchStreamChannel"
Cohesion: 0.25
Nodes (7): NoticeTwitchStreamChannel, ChangeStreamDataMessage, DiscordChannelId, EndStreamMessage, GuildId, NoticeTwitchUserId, StartStreamMessage

### Community 334 - "CommandTextEqualityComparer"
Cohesion: 0.38
Nodes (3): DiscordStreamNotifyBot.Command.Help, CommandInfo, CommandTextEqualityComparer

### Community 335 - "給未來 session 的信"
Cohesion: 0.29
Nodes (5): 一、`claude` 分支是你最大的資產，也是最大的陷阱, 三、使用者已做的決策，不要重新辯論, 二、你在活的生產系統旁施工, 給未來 session 的信, 這套制度最可能的退化方式，與預防

### Community 336 - "MemberOperationCoordinator"
Cohesion: 0.38
Nodes (3): ConcurrentDictionary, MemberOperationCoordinator, MemberOperationCoordinatorTests

### Community 337 - "AdminSettingsRole"
Cohesion: 0.29
Nodes (7): AdminSettingsRole, BotCanManage, Everyone, Id, Managed, Name, Position

### Community 338 - "4. 訊息契約：Redis Streams 通知匯流排"
Cohesion: 0.33
Nodes (6): 4.1 拓撲, 4.2 DTO（`Shared/Messages/`）, 4.3 消費迴圈（Notifier）, 4.4 建群與 Preflight, 4.5 Redis 控制平面鍵（非 stream）, 4. 訊息契約：Redis Streams 通知匯流排

### Community 339 - "5. 語系模型與解析規則"
Cohesion: 0.33
Nodes (6): 5.1 支援值, 5.2 公開內容與背景通知, 5.3 私人即時回覆, 5.4 延遲會限驗證 DM, 5.5 併發安全, 5. 語系模型與解析規則

### Community 340 - ".OnlyAffiliateAndPartnerCanBeConfigured"
Cohesion: 0.33
Nodes (3): InlineData, Theory, TwitchSubscriptionConfigurationPolicyTests

### Community 341 - "10. 執行期互動本地化"
Cohesion: 0.40
Nodes (5): 10.1 共用回覆 API, 10.2 Precondition 與 handler 錯誤, 10.3 例外訊息, 10.4 第一階段模組, 10. 執行期互動本地化

### Community 342 - "5. 目標架構"
Cohesion: 0.40
Nodes (5): 5.1 Console, 5.2 非容器檔案, 5.3 Loki, 5.4 `LOKI_URL` 相容性, 5. 目標架構

### Community 343 - "5. Contract v1 additive 擴充"
Cohesion: 0.40
Nodes (5): 5.1 Capabilities, 5.2 新增 actions, 5.3 快照頂層, 5.4 回應碼, 5. Contract v1 additive 擴充

### Community 344 - "TwitchRoleConfigurationResult"
Cohesion: 0.40
Nodes (5): TwitchRoleConfigurationResult, Config, Error, IsNew, IsSuccess

### Community 345 - "YoutubeMemberSingleConfigurationQueueAction"
Cohesion: 0.40
Nodes (5): YoutubeMemberSingleConfigurationQueueAction, Add, PreserveQueued, PreserveVerified, RequeuePendingRoleRemoval

### Community 346 - "YoutubeMemberTokenCleanupConcurrencyTests"
Cohesion: 0.60
Nodes (3): MySqlComponentFact, Task, YoutubeMemberTokenCleanupConcurrencyTests

### Community 347 - "7. 資料庫變更"
Cohesion: 0.50
Nodes (4): 7.1 `GuildConfig.Locale`, 7.2 `YoutubeMemberCheck.Locale`, 7.3 Migration 鐵則, 7. 資料庫變更

### Community 348 - "9. Slash Command Localization"
Cohesion: 0.50
Nodes (4): 9.1 Discord.Net 設定, 9.2 指令名稱, 9.3 Command signature, 9. Slash Command Localization

### Community 349 - "TwitchAuthorizationChangedPayload"
Cohesion: 0.67
Nodes (3): TwitchAuthorizationChangedPayload, Status, TwitchUserId

## Knowledge Gaps
- **1511 isolated node(s):** `BotStateCollectionDefinition`, `NotifyType`, `Member`, `Notifier`, `SharedState` (+1506 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **19 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MainDbService` connect `MainDbService` to `TwitcastingSpider`, `.GetLocaleAsync`, `.Warn`, `MySqlDataStore`, `ClusterQueryService`, `.LoadSnapshotAsync`, `.GetDbContext`, `.SetMessage`, `AdminSettingsMutationResult`, `YoutubeMemberAuthorizationService`, `MySqlComponentFixture`, `FakeTimeProvider`, `YoutubeMemberCheck`, `SendMsgToAllGuildService`, `YoutubeDetectionService`, `DiscordStreamNotifyBot.Shared.Messages`, `TopLevelModule`, `YoutubeMemberRoleService`, `TwitchDetectionService`, `Twitch`, `GuildLocaleService`, `.Start`, `Administration`, `YoutubeMemberSetting`, `Utility`, `TwitcastingDetectionService`, `MainDbContext`, `.Info`, `Bot`, `AdminSettingsService`, `.AddChannel`, `YoutubeStream`, `TwitchService`, `Task`, `YoutubeMemberService`, `.SetMemberCheckVideoIdAsync`, `.CreateOrRepairConfigurationAsync`, `UtilityService`, `TwitchSpider`, `.SendLocalizedConfirmAsync`, `TwitcastingClient`?**
  _High betweenness centrality (0.089) - this node is a cross-community bridge._
- **Why does `DiscordStreamNotifyBot.Shared.Messages` connect `DiscordStreamNotifyBot.Shared.Messages` to `DiscordStreamNotifyBot.SharedService.YoutubeMember`, `DiscordStreamNotifyBot.Tests`, `YoutubeNotification`, `DiscordStreamNotifyBot.DataBase`, `NotifierMetrics.cs`, `DiscordStreamNotifyBot.Localization`, `GuildSnapshot`, `AdminSettings.cs`, `DiscordStreamNotifyBot.DataBase.Table`, `TwitchStateDecisions.cs`, `DiscordStreamNotifyBot.HttpClients.Twitcasting.Model`, `DiscordStreamNotifyBot.Shared`?**
  _High betweenness centrality (0.065) - this node is a cross-community bridge._
- **Why does `DiscordStreamNotifyBot.DataBase` connect `DiscordStreamNotifyBot.DataBase` to `DiscordStreamNotifyBot.Tests`, `DiscordStreamNotifyBot.Localization`, `MainDbContextModelSnapshot.cs`, `DiscordStreamNotifyBot.Command.Attribute`, `DiscordStreamNotifyBot.Migrations`, `ModifyTwitCastingTable`, `AddMaxSpiderCountSettingField`, `Migration`, `MainDbContextFactory`, `AddTwitchBroadcasterAuthorization`, `AddLocalizationSettings`, `DiscordStreamNotifyBot.Shared.Messages`, `MemberRoleOwnershipSnapshot`, `MySqlComponentFixture.cs`, `DiscordStreamNotifyBot.Shared`, `DiscordStreamNotifyBot.SharedService.YoutubeMember`, `AddManualMemberCheckVideoFlag`, `AddTwitchSubscriptionVerification`, `MainDbService`, `AddTwitchSubscriptionDeletionPending`, `AddGoogleOAuthUnlinkIntent`, `AddYoutubeMemberVerificationDurability`, `DiscordStreamNotifyBot.DataBase.Table`, `RenameVerificationLogChannel`, `TwitcastingClient`?**
  _High betweenness centrality (0.064) - this node is a cross-community bridge._
- **What connects `BotStateCollectionDefinition`, `NotifyType`, `Member` to the rest of the system?**
  _1511 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `TwitchSubscriptionApiClient` be split into smaller, more focused modules?**
  _Cohesion score 0.1168091168091168 - nodes in this community are weakly interconnected._
- **Should `DiscordStreamNotifyBot.Tests` be split into smaller, more focused modules?**
  _Cohesion score 0.08602150537634409 - nodes in this community are weakly interconnected._
- **Should `DiscordStreamNotifyBot.Shared.csproj` be split into smaller, more focused modules?**
  _Cohesion score 0.08333333333333333 - nodes in this community are weakly interconnected._