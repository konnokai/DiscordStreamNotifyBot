# Graph Report - DiscordStreamNotifyBot  (2026-09-06)

## Corpus Check
- 336 files · ~174,396 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 5416 nodes · 11368 edges · 339 communities (311 shown, 19 thin omitted)
- Extraction: 90% EXTRACTED · 10% INFERRED · 0% AMBIGUOUS · INFERRED: 1094 edges (avg confidence: 0.82)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `9527b6f6`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- TwitchSubscriptionApiClient
- .SendLocalizedErrorAsync
- .Warn
- EmojiService
- DiscordStreamNotifyBot.Shared.csproj
- TwitchApiService
- InteractionHandler
- 偵測 → 匯流排 → 發送 路徑除錯
- AuthTokenTests
- .TryParse
- YoutubeReminderPolicyTests
- .RunCoreAsync
- DiscordStreamNotifyBot.DataBase
- Extensions
- .GetDbContext
- Extensions
- .ReconcileUserStateAsync
- 會限 OAuth Token 儲存改走 MySQL（去 Redis 依賴）計畫
- .SetMessage
- MainDbContext
- FakeTimeProvider
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
- .SendStreamMessageAsync
- AGENTS.md
- NotificationEmbedFactoryTests
- TwitchOAuthRefreshLockLease
- .BuildVariant
- TwitchSpider
- YoutubeMemberRoleService
- TwitchDetectionService
- Twitch
- ScraperMetrics
- GuildLocaleService
- .Decide
- RedisChannels
- 13. 驗證矩陣
- .PrepareMemberCheckCleanupAsync
- 網頁管理設定：30 秒請求與背景清理實作計畫
- Administration
- 網頁管理設定中心：爬蟲與會員驗證實作計畫
- 水平擴展（三層拆分）計畫 — Redis Streams 版
- YoutubeMemberSetting
- NoticeCache
- YoutubeVideoClaimCache
- .SetVerificationLogChannelAsync
- AdministrationService
- AGENTS.md
- TwitchRefreshRotationLifecycle
- GuildTwitchSubscriptionConfig
- DiscordStreamNotifyBot.Shared
- YoutubeStreamService
- graphify reference: extra exports and benchmark
- Bot
- DiscordStreamNotifyBot.SharedService.YoutubeMember
- TwitchReconcileDecisionTests
- .FilterNoNotifyGuilds
- .Main
- AddManualMemberCheckVideoFlag
- .BuildSnapshotAsync
- YoutubeNoticeType
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
- YoutubeDetectionService
- graphify reference: query, path, explain
- 自動化測試導入計畫
- Task
- DescriptionOnlyLocalizationManager
- .Get
- YoutubeMemberService
- .CreateAsyncClient
- Video
- NotifierMetrics.cs
- .AssertKeysAbsentAsync
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
- AdminSettingsMutationResult
- Twitch OAuth 與零成本 EventSub 實作計畫
- TwitchEventSubEnsureResult
- .Classify
- AutocompleteCandidate
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
- DiscordStreamNotifyBot.Migrations
- 11. Bot EventSub 與偵測
- 15. 預期修改檔案
- 2. 現況基線
- 5. Guild 資格與 OAuth 豁免
- DiscordStreamNotifyBot.sln
- CommandDisplayResolver
- 13. Prometheus
- 4. 安全刪除狀態機
- TwitchNotification
- .Plan
- MainDbContextModelSnapshot.cs
- DiscordStreamNotifyBot.DataBase.Table
- .StartAndBlockAsync
- ModifyTwitCastingTable
- AddMaxSpiderCountSettingField
- Migration
- AddTwitchBroadcasterAuthorization
- AddLocalizationSettings
- MemberRoleOwnershipSnapshot
- .CreateService
- DbEntity
- YoutubeMemberApiClientTests
- Movie
- DiscordStreamNotifyBot.Shared.Messages
- AddTwitchSubscriptionVerification
- AddTwitchSubscriptionDeletionPending
- RedisComponentFixture
- AddYoutubeMemberVerificationDurability
- .CheckRequirementsAsync
- .NotifyAddedAsync
- AddGoogleOAuthUnlinkIntent
- RenameVerificationLogChannel
- DebounceFixture
- GuildYoutubeMemberConfig
- DiscordStreamNotifyBot.Tests
- TwitchGuildEligibilityStatus
- graphify.js
- .TryGetKey
- TwitchSubscriptionPoliciesTests
- NotificationMetricEvent
- Help
- .Classify
- YoutubeMemberVideoLogNotification
- TwitchChannelUpdateInfo
- .HandleStartLiveMessageAsync
- ReactionEventWrapper
- YoutubePubSubNotification
- MigrationAndConstraintTests
- DiscordStreamNotifyBot.Command
- DiscordStreamNotifyBot
- TwitchAuthorizationLocalState
- .RunAsync
- MySqlDataStore
- ArgumentOutOfRangeException
- Category
- .DecideAutomaticMutation
- .GenerateSuggestionsAsync
- .OnReaction
- NijisanjiStreamJson
- .Init
- .GetLocaleAsync
- .CheckPermissionsAsync
- TcBackendStreamData.cs
- AdminSettingsSnapshot
- .GroupName
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
- .GenerateSuggestionsAsync
- YouTube 會員驗證架構重構計畫
- .GenerateSuggestionsAsync
- NijisanjiLiverJson
- NotifierMetrics
- Movie
- .GenerateSuggestionsAsync
- .SlashCommandExecuted
- TwitchStateDecisions.cs
- InteractionMetadataFixture
- 5. 語系模型與解析規則
- .GenerateSuggestionsAsync
- 15. 實作階段
- AdminSettingsCrawlerPlatform
- AutocompleteHandler
- 10. 執行期互動本地化
- .LoadCommandFrom
- Broadcaster
- TwitchBroadcasterAuthorization
- .ValidateCommandLocalizationResources
- opencode.json
- YouTube 會員驗證
- GuildSnapshot
- AdminSettingsNotifications
- 14. Frontend
- 8. DB Schema
- TwitchAccessTokenData
- TwitcastingStream
- MySqlDataStoreTests
- RedisContractTests
- 13. Backend Contract
- 16. 驗證命令
- .LockGuildAsync
- GoogleOAuthOperationLock
- 10. Slash 與 Interaction Cutover
- 6. 目標架構
- 7. 狀態機
- 9. Role 隔離政策
- .GenerateSuggestionsAsync
- .SendLocalizedConfirmAsync
- AdminSettingsYoutubeMessages
- DiscordStreamNotifyBot.HttpClients.Twitcasting.Model
- TwitchSubscriptionStatus
- TwitcastingSpider
- YoutubeMemberPolicies.cs
- TwitchEventSubMetricStatus
- AdminSettingsYoutubeVerification
- .FailedActionRemainsRetryableAndReportedFailuresPreventAcknowledgment
- TwitchStream
- YoutubeMemberVerificationResult
- NoticeYoutubeStreamChannel
- AdminYoutubeMessagesPayload
- YoutubeNotification
- YoutubeMemberProbeResultKind
- TwitchSpider
- TwitchSubscriptionCheck
- GuildConfig
- AdminSettingsTwitchVerification
- NotificationDeliveryResult
- TwitchReconcileAction
- AdminSettingsResources
- GracefulShutdown
- 8. 分階段實作步驟
- .Info
- .MissingOrShortKeyIsRejected
- YoutubeChannelSpider
- MySqlComponentFixture
- PreconditionAttribute
- .MakeNamesUnique
- NotificationChannelIssue
- YoutubeMemberAuthorizationService.cs
- NoticeTwitchStreamChannel
- YoutubeChannelOwnedType
- GetAllRegistedWebHookJson
- AdminSettingsChannel
- Stub
- AdministrationComponent
- .Format
- .LoadInteractionFrom
- .GenerateSuggestionsAsync
- GuildInfoResponse
- 14. 部署與回滾
- TwitchRefreshPersistenceDecision
- LogLevel
- 2. 專案拆分 (Solution Layout)
- 4. 訊息契約：Redis Streams 通知匯流排
- 9. 測試計畫
- MainDbContextFactory
- YoutubeMemberRoleResult
- TwitchOfflineAction
- TwitchStreamStartAction
- GoogleOAuthUnlinkIntent
- NoticeTwitcastingStreamChannel
- YoutubeMemberAccessToken
- 6. 資源架構
- 5. 目標架構
- 8. 驗證矩陣
- 10. 手動驗收矩陣
- 5. Contract v1 additive 擴充
- ClusterQueryType
- TwitchProviderResultStatus
- TwitchRoleConfigurationResult
- YTChannelType
- LogFileRoute
- YoutubeMemberTokenCleanupConcurrencyTests
- TopLevelModule
- .AllRegisteredCommandsHaveDescriptionsInEverySupportedLocale
- 7. 資料庫變更
- 8. Frontend 實作
- TwitchStreamEventPayload
- all.sql
- TwitchAuthorizationChangedPayload
- TwitchReconcileRequestedPayload
- .ConvertDateTimeToDiscordMarkdown
- _Baseline_ExistingDb.sql
- `guild_config`

## God Nodes (most connected - your core abstractions)
1. `MainDbContext` - 75 edges
2. `DiscordStreamNotifyBot.DataBase.Table` - 69 edges
3. `YoutubeDetectionService` - 67 edges
4. `YoutubeMemberService` - 65 edges
5. `DiscordStreamNotifyBot.DataBase` - 64 edges
6. `TwitchDetectionService` - 63 edges
7. `DiscordStreamNotifyBot.Shared` - 62 edges
8. `BotConfig` - 56 edges
9. `YoutubeStreamService` - 51 edges
10. `MainDbService` - 51 edges

## Surprising Connections (you probably didn't know these)
- `InteractionMetadataFixture` --references--> `InteractionHandler`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/InteractionMetadataFixture.cs → src/DiscordStreamNotifyBot.Notifier/Interaction/InteractionHandler.cs
- `NotificationEmbedFactoryTests` --references--> `BotLocalizer`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/NotificationEmbedFactoryTests.cs → src/DiscordStreamNotifyBot.Notifier/Localization/BotLocalizer.cs
- `YoutubeMemberVideoLogMessageFormatterTests` --references--> `BotLocalizer`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/YoutubeMemberVideoLogMessageFormatterTests.cs → src/DiscordStreamNotifyBot.Notifier/Localization/BotLocalizer.cs
- `YoutubeMemberVideoLogMessageFormatterTests` --references--> `CommandDisplayResolver`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/YoutubeMemberVideoLogMessageFormatterTests.cs → src/DiscordStreamNotifyBot.Notifier/Localization/CommandDisplayResolver.cs
- `MySqlComponentFixture` --references--> `MainDbService`  [EXTRACTED]
  tests/DiscordStreamNotifyBot.Tests/Component/MySql/MySqlComponentFixture.cs → src/DiscordStreamNotifyBot.Shared/DataBase/MainDbService.cs

## Import Cycles
- None detected.

## Communities (339 total, 19 thin omitted)

### Community 0 - "TwitchSubscriptionApiClient"
Cohesion: 0.12
Nodes (18): CancellationToken, DateTimeOffset, HttpResponseMessage, IHttpClientFactory, NotifierMetrics, Task, TwitchProviderResult, Status (+10 more)

### Community 1 - ".SendLocalizedErrorAsync"
Cohesion: 0.28
Nodes (12): CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, IChannel, NoticeType, RequireBotPermission, RequireContext (+4 more)

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

### Community 9 - ".TryParse"
Cohesion: 0.21
Nodes (7): Uri, YoutubeVideoIdParser, ArgumentNullException, InlineData, Theory, YoutubeVideoIdParserTests, UriFormatException

### Community 10 - "YoutubeReminderPolicyTests"
Cohesion: 0.06
Nodes (34): DateTime, TimeSpan, YoutubeReminderApiAction, TreatAsStarted, TreatAsTimeChanged, YoutubeReminderBatchChangeAction, PublishAndReplaceTimer, PublishAndRunImmediately (+26 more)

### Community 11 - ".RunCoreAsync"
Cohesion: 0.23
Nodes (9): CancellationToken, Func, Task, TimeProvider, TimeSpan, PeriodicRunner, Fact, Task (+1 more)

### Community 12 - "DiscordStreamNotifyBot.DataBase"
Cohesion: 0.11
Nodes (9): DiscordStreamNotifyBot.SharedService.AdminSettings, DiscordStreamNotifyBot.HttpClients, DiscordStreamNotifyBot.Interaction.ServerAdministration, DiscordStreamNotifyBot.SharedService.Youtube, DiscordStreamNotifyBot.SharedService.Twitcasting, DiscordStreamNotifyBot.Interaction.Utility.Service, DiscordStreamNotifyBot.Localization, DiscordStreamNotifyBot.DataBase (+1 more)

### Community 13 - "Extensions"
Cohesion: 0.09
Nodes (18): ManagementBaseObject, Process, BotLocalizer, DiscordSocketClient, EmbedBuilder, GuildLocaleService, IDiscordInteraction, IEmote (+10 more)

### Community 14 - ".GetDbContext"
Cohesion: 0.09
Nodes (30): BotLocalizer, CancellationToken, CancellationTokenSource, ConcurrentDictionary, DateTimeOffset, DiscordSocketClient, GuildLocaleService, NotifierMetrics (+22 more)

### Community 15 - "Extensions"
Cohesion: 0.19
Nodes (9): EmbedBuilder, IEmote, IMessage, IMessageChannel, IUserMessage, Task, Video, YTChannelType (+1 more)

### Community 16 - ".ReconcileUserStateAsync"
Cohesion: 0.19
Nodes (11): DateTime, TwitchBroadcasterAuthorization, TwitchSpider, TwitchSpiderRemovalMetricReason, TwitchUserState, Authorization, Spider, UserId (+3 more)

### Community 17 - "會限 OAuth Token 儲存改走 MySQL（去 Redis 依賴）計畫"
Cohesion: 0.11
Nodes (18): Backend, Bot（本 repo）, MySQL（兩端都已連同一個庫）, 儲存層（現況為 Redis）, 加密與 blob 格式（兩端一致）, 加密金鑰處理, 影響檔案一覽, 待決策（給實作 session） (+10 more)

### Community 18 - ".SetMessage"
Cohesion: 0.26
Nodes (9): CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, NoticeType, RequireBotPermission, SlashCommand, Task (+1 more)

### Community 19 - "MainDbContext"
Cohesion: 0.05
Nodes (48): BannerChange, DbContext, GoogleOAuthUnlinkIntent, RecordYoutubeChannel, DbSet, GuildConfig, GuildTwitchSubscriptionConfig, GuildYoutubeMemberConfig (+40 more)

### Community 20 - "FakeTimeProvider"
Cohesion: 0.25
Nodes (12): FakeTimeProvider, GuildLocaleRequest, ArgumentException, Fact, Func, GuildLocaleService, InvalidOperationException, IReadOnlyCollection (+4 more)

### Community 21 - "YoutubeMemberAuthorizationService"
Cohesion: 0.26
Nodes (9): GoogleAuthorizationCodeFlow, CancellationToken, HttpClient, MySqlDataStore, Task, YoutubeMemberAuthorizationService, IsConfigured, YoutubeMemberTokenSnapshot (+1 more)

### Community 22 - "Log"
Cohesion: 0.10
Nodes (17): DelegatingHandler, ILogEventSink, ITextFormatter, LogEvent, Logger, LoggerConfiguration, LogMessage, CancellationToken (+9 more)

### Community 23 - ".RetryWithBackoffAsync"
Cohesion: 0.22
Nodes (11): Func, Task, TimeProvider, TimeSpan, StartupPreflight, DateTimeOffset, Fact, InvalidOperationException (+3 more)

### Community 24 - "Twitch 訂閱驗證實作計畫"
Cohesion: 0.05
Nodes (36): 10. Frontend 調整, 11. 安全與錯誤處理, 12.1 Backend, 12.2 Bot, 12.3 Frontend, 12. 自動化測試, 13. 手動驗收, 14. 實作順序 (+28 more)

### Community 25 - "YoutubeMemberCheck"
Cohesion: 0.08
Nodes (19): ComponentInteraction, Task, YoutubeMemberComponent, IEnumerable, IReadOnlyCollection, IReadOnlyList, YoutubeMemberPolicies, YoutubeMemberSelectionTransition (+11 more)

### Community 26 - "新增 TwitCasting 錄影委派計畫（小幫手 ↔ StreamRecordTools）"
Cohesion: 0.11
Nodes (17): 1. 背景與動機, 2. 新增跨 repo 契約, 3. A（小幫手）改動, 4. B（StreamRecordTools）改動, 5. 部署順序與相容性, 6. 驗證, 7. 影響範圍, A1. `Shared/RedisChannels.cs` (+9 more)

### Community 27 - "多語系支援計畫"
Cohesion: 0.14
Nodes (14): 15. 預期修改檔案, 16. 完成定義, 1. 背景, 2. 目標, 3. 非目標, 4. 已確認的產品決策, 8.1 首次設定流程, 8.2 語系設定指令 (+6 more)

### Community 28 - "Serilog Logging 遷移計畫"
Cohesion: 0.20
Nodes (10): 10. 預期修改檔案, 11. 完成定義, 1. 背景, 2. 目標, 3. 非目標, 4. 技術選型, 6.1 例外事件, 6. Facade 相容契約 (+2 more)

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
Cohesion: 0.16
Nodes (10): ConcurrentDictionary, DateTime, IEnumerable, Task, TimeSpan, YoutubeMemberLifecyclePolicy, YoutubeMemberLifecycleTaskRegistry, Fact (+2 more)

### Community 33 - ".SendStreamMessageAsync"
Cohesion: 0.09
Nodes (25): HttpException, Dictionary, Exception, Func, IMessageChannel, IUserMessage, List, Task (+17 more)

### Community 34 - "AGENTS.md"
Cohesion: 0.14
Nodes (7): 一、`claude` 分支是你最大的資產，也是最大的陷阱, 三、使用者已做的決策，不要重新辯論, 二、你在活的生產系統旁施工, 給未來 session 的信, 這套制度最可能的退化方式，與預防, License, 直播小幫手 [點我邀請到你的 Discord 內](https://discordapp.com/api/oauth2/authorize?client_id=758222559392432160&permissions=2416143425&scope=bot%20applications.commands)

### Community 35 - "NotificationEmbedFactoryTests"
Cohesion: 0.24
Nodes (8): Color, DateTime, Embed, Fact, InlineData, TableVideo, Theory, NotificationEmbedFactoryTests

### Community 36 - "TwitchOAuthRefreshLockLease"
Cohesion: 0.08
Nodes (30): CancellationToken, CancellationTokenSource, IConnectionMultiplexer, IDatabase, RedisKey, RedisValue, Task, TimeSpan (+22 more)

### Community 37 - ".BuildVariant"
Cohesion: 0.15
Nodes (16): BotLocalizer, DateTime, EmbedBuilder, IReadOnlyCollection, TimeSpan, TwitchEmbedBuilderFactory, Embed, MessageComponent (+8 more)

### Community 38 - "TwitchSpider"
Cohesion: 0.35
Nodes (7): CommandExample, CommandSummary, DefaultMemberPermissions, SlashCommand, Task, TwitchService, TwitchSpider

### Community 39 - "YoutubeMemberRoleService"
Cohesion: 0.21
Nodes (12): CancellationToken, DiscordSocketClient, GuildYoutubeMemberConfig, IEnumerable, IRole, SocketGuild, Task, YoutubeMemberRoleConfigurationResult (+4 more)

### Community 40 - "TwitchDetectionService"
Cohesion: 0.12
Nodes (16): ChannelUpdate, CancellationTokenSource, ConcurrentDictionary, IReadOnlyCollection, IReadOnlyDictionary, RedisValue, ScraperMetrics, SemaphoreSlim (+8 more)

### Community 41 - "Twitch"
Cohesion: 0.36
Nodes (10): Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task, TwitchService (+2 more)

### Community 42 - "ScraperMetrics"
Cohesion: 0.06
Nodes (34): EventSubSubscription, Counter, Gauge, ScraperMetricResult, Failure, Success, ScraperMetrics, TwitchAuthorizationChangeMetricResult (+26 more)

### Community 43 - "GuildLocaleService"
Cohesion: 0.07
Nodes (27): CacheEntry, CultureInfo, CancellationToken, ConcurrentDictionary, DateTimeOffset, Dictionary, Func, GuildConfig (+19 more)

### Community 44 - ".Decide"
Cohesion: 0.27
Nodes (7): IEnumerable, List, TwitchChannelEventFacts, TwitchChannelUpdatePolicy, DateTime, Fact, TwitchChannelUpdateDecisionTests

### Community 45 - "RedisChannels"
Cohesion: 0.11
Nodes (10): AdminSettings, Cluster, Member, Notifier, OAuth, RedisChannels, SharedState, Twitcasting (+2 more)

### Community 46 - "13. 驗證矩陣"
Cohesion: 0.25
Nodes (8): 13.1 編譯與靜態檢查, 13.2 Slash command 註冊, 13.3 Locale resolver, 13.4 首次設定, 13.5 通知, 13.6 YouTube 會限驗證, 13.7 範圍守衛, 13. 驗證矩陣

### Community 47 - ".PrepareMemberCheckCleanupAsync"
Cohesion: 0.16
Nodes (13): CheckId, Snapshot, CancellationToken, CancellationToken, GuildYoutubeMemberConfig, IEnumerable, List, Task (+5 more)

### Community 48 - "網頁管理設定：30 秒請求與背景清理實作計畫"
Cohesion: 0.10
Nodes (19): 10. 實作順序, 11. 不在本次實作, 1. 目標, 2. 已確認決策, 3. 端點範圍與 deadline, 4. Cross-project contract, 5.1 Controller, 5.2 Redis bridge (+11 more)

### Community 49 - "Administration"
Cohesion: 0.32
Nodes (12): GuildInfoResponse, InviteResponse, Alias, Command, DiscordSocketClient, NotificationChannelCheckResponse, RequireContext, RequireOwner (+4 more)

### Community 50 - "網頁管理設定中心：爬蟲與會員驗證實作計畫"
Cohesion: 0.10
Nodes (20): 11. 實作順序, 12. 完成閘門, 13. 新 Session 交接指令, 1. 目標, 2.1 爬蟲, 2.2 YouTube 會員驗證, 2.3 Twitch 訂閱驗證, 2. 完成範圍 (+12 more)

### Community 51 - "水平擴展（三層拆分）計畫 — Redis Streams 版"
Cohesion: 0.13
Nodes (15): 10. 可優化項目（claude 分支已有成品，對應階段順手移植）, 11. 驗證清單（部署前全過）, 1. 目標架構, 3. 設定, 5.1 歸屬守衛（防多 shard 互刪設定，最高優先）, 5.2 心跳與重啟, 5.3 啟動連線檢查 (StartupPreflight), 5. Shard 歸屬與生命週期 (+7 more)

### Community 52 - "YoutubeMemberSetting"
Cohesion: 0.27
Nodes (10): CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, GuildYoutubeMemberConfig, IRole, RequireGuildMemberCount, SlashCommand (+2 more)

### Community 53 - "NoticeCache"
Cohesion: 0.24
Nodes (10): DateTimeOffset, Func, List, TimeProvider, TimeSpan, NoticeCache, Fact, InvalidOperationException (+2 more)

### Community 54 - "YoutubeVideoClaimCache"
Cohesion: 0.14
Nodes (13): Batch, ConcurrentDictionary, DateTimeOffset, Dictionary, TimeProvider, TimeSpan, Batch, YoutubeVideoClaimCache (+5 more)

### Community 55 - ".SetVerificationLogChannelAsync"
Cohesion: 0.35
Nodes (9): DefaultMemberPermissions, DiscordSocketClient, IChannel, ITextChannel, RequireContext, RequireUserPermission, SlashCommand, Task (+1 more)

### Community 56 - "AdministrationService"
Cohesion: 0.17
Nodes (9): DiscordSocketClient, Expected, IReadOnlyCollection, ITextChannel, Responded, SocketGuild, Task, AdministrationService (+1 more)

### Community 57 - "AGENTS.md"
Cohesion: 0.17
Nodes (11): Build & Run, Conventions, EF Core 鐵則, graphify, 制度條款, 外部契約（不可片面更改）, 指令文件, 架構要點（現行樹） (+3 more)

### Community 58 - "TwitchRefreshRotationLifecycle"
Cohesion: 0.08
Nodes (23): IDisposable, Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task, ReactionEventWrapper (+15 more)

### Community 59 - "GuildTwitchSubscriptionConfig"
Cohesion: 0.08
Nodes (24): AddRoleIds, IQueryable, IReadOnlySet, RemoveRoleIds, TwitchSubscriptionConfigurationQueries, Func, IReadOnlyList, TwitchSubscriptionRolePolicy (+16 more)

### Community 60 - "DiscordStreamNotifyBot.Shared"
Cohesion: 0.09
Nodes (10): DiscordStreamNotifyBot.Tests.Component.Redis, DiscordStreamNotifyBot.Scraper, DiscordStreamNotifyBot.Shared, DiscordStreamNotifyBot.Interaction.OwnerOnly.Service, DiscordStreamNotifyBot.SharedService.Google, Program, BotRole, Coordinator (+2 more)

### Community 61 - "YoutubeStreamService"
Cohesion: 0.06
Nodes (33): NowStreamingHost, CrawlerPolicy, BotLocalizer, CancellationToken, CommandDisplayResolver, DiscordSocketClient, Embed, EmojiService (+25 more)

### Community 62 - "graphify reference: extra exports and benchmark"
Cohesion: 0.22
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 63 - "Bot"
Cohesion: 0.07
Nodes (28): BotPlayingStatus, ConnectionMultiplexer, DiscordSocketClient, IDatabase, ISubscriber, IUser, Task, Timer (+20 more)

### Community 64 - "DiscordStreamNotifyBot.SharedService.YoutubeMember"
Cohesion: 0.11
Nodes (8): DiscordStreamNotifyBot.SharedService.YoutubeMember, DiscordStreamNotifyBot.Interaction.YoutubeMember, DiscordStreamNotifyBot.Command.YoutubeMember, DiscordStreamNotifyBot.SharedService.Member, Exception, YoutubeMemberSafeLogging, Fact, YoutubeMemberSafeLoggingTests

### Community 65 - "TwitchReconcileDecisionTests"
Cohesion: 0.31
Nodes (4): TwitchReconcileFacts, DateTime, Fact, TwitchReconcileDecisionTests

### Community 66 - ".FilterNoNotifyGuilds"
Cohesion: 0.37
Nodes (4): IEnumerable, ArgumentNullException, Fact, NoNotifyGuildFilterTests

### Community 67 - ".Main"
Cohesion: 0.11
Nodes (13): AssemblyInformationalVersionAttribute, Assembly, CancellationToken, Exception, HashSet, PeriodicTimer, Task, Program (+5 more)

### Community 68 - "AddManualMemberCheckVideoFlag"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, AddManualMemberCheckVideoFlag

### Community 69 - ".BuildSnapshotAsync"
Cohesion: 0.10
Nodes (21): CancellationToken, DiscordSocketClient, GuildLocaleService, JObject, SocketGuild, Task, AdminSettingsService, RequestRoute (+13 more)

### Community 70 - "YoutubeNoticeType"
Cohesion: 0.16
Nodes (12): YoutubeNoticeType, ChangeTime, Delete, End, NewStream, NewVideo, Start, DateTime (+4 more)

### Community 71 - "EF Core 遷移與基線化（本專案版）"
Cohesion: 0.25
Nodes (7): EF Core 遷移與基線化（本專案版）, 一次性基線化（舊的 EnsureCreated 正式庫）, 一般變更流程, 你必須先知道的三件專案特例, 啟動時不碰資料庫（重要）, 套用：本地/開發 vs 正式環境, 收尾

### Community 72 - "NotificationBusConsumer"
Cohesion: 0.21
Nodes (11): CancellationToken, Func, IDatabase, RedisValue, StreamEntry, Task, TwitcastingService, TwitchService (+3 more)

### Community 73 - "11. 通知與背景訊息"
Cohesion: 0.29
Nodes (7): 11.1 現況限制, 11.2 目標作法, 11.3 YouTube, 11.4 Twitch, 11.5 TwitCasting, 11.6 YouTube 會限驗證, 11. 通知與背景訊息

### Community 74 - "GoogleOAuthOperationLockLease"
Cohesion: 0.13
Nodes (17): CancellationToken, CancellationTokenSource, Exception, RedisKey, RedisValue, Task, ValueTask, GoogleOAuthOperationLockAcquireResult (+9 more)

### Community 75 - "TwitchStreamLifecycleDecisionTests"
Cohesion: 0.21
Nodes (7): TwitchOfflineFacts, TwitchOfflinePolicy, TwitchStreamStartFacts, Fact, InlineData, Theory, TwitchStreamLifecycleDecisionTests

### Community 80 - "YoutubeStream"
Cohesion: 0.06
Nodes (47): DbUpdateConcurrencyException, ICommandService, Alias, Command, CommandExample, RequireContext, RequireOwner, Summary (+39 more)

### Community 81 - "TwitchService"
Cohesion: 0.10
Nodes (22): BotLocalizer, CancellationToken, Clip, DateTime, DiscordSocketClient, EmojiService, EventSubSubscription, GuildLocaleService (+14 more)

### Community 82 - "AdminSettingsContractTests"
Cohesion: 0.07
Nodes (25): ActionRowComponent, ButtonComponent, RequestRoute, Func, JObject, AdminSettingsCommandReply, Arguments, Code (+17 more)

### Community 83 - "AdminSettings.cs"
Cohesion: 0.08
Nodes (25): AdminProbeVideoPayload, SourceId, Video, AdminRemoveNotificationPayload, Source, AdminSetChannelPayload, ChannelId, AdminSetLocalePayload (+17 more)

### Community 84 - "YoutubeDetectionService"
Cohesion: 0.07
Nodes (34): ConcurrentBag, GeneratedRegex, HttpRequestException, IsDeleted, Batch, ConcurrentDictionary, HttpClient, IHttpClientFactory (+26 more)

### Community 85 - "graphify reference: query, path, explain"
Cohesion: 0.33
Nodes (5): For /graphify explain, For /graphify path, graphify reference: query, path, explain, Step 0 — Constrained query expansion (REQUIRED before traversal), Step 1 — Traversal

### Community 86 - "自動化測試導入計畫"
Cohesion: 0.17
Nodes (12): 10. 測試實作規則, 1. 目標, 2. 測試分類, 3. 不移除的啟動檢查, 4. 第一批：低耦合契約與格式化, 5. 第二批：小幅抽出純邏輯, 6. 第三批：時間與快取, 7. 第四批：Scraper 狀態機 (+4 more)

### Community 87 - "Task"
Cohesion: 0.10
Nodes (17): DateTime, IEnumerable, TableVideo, Task, YTApiVideo, BannerChangeNotification, ChannelId, VideoId (+9 more)

### Community 88 - "DescriptionOnlyLocalizationManager"
Cohesion: 0.33
Nodes (6): ILocalizationManager, ResxLocalizationManager, IDictionary, IList, LocalizationTarget, DescriptionOnlyLocalizationManager

### Community 89 - ".Get"
Cohesion: 0.12
Nodes (13): MissingManifestResourceException, Dictionary, DictionaryEntry, Regex, ResourceManager, BotLocalizer, ArgumentException, ArgumentNullException (+5 more)

### Community 90 - "YoutubeMemberService"
Cohesion: 0.12
Nodes (17): SocketMessageComponent, YoutubeMemberNotMemberApplyResult, YoutubeMemberService, CancellationToken, CancellationTokenSource, CommandDisplayResolver, Func, GuildLocaleService (+9 more)

### Community 91 - ".CreateAsyncClient"
Cohesion: 0.21
Nodes (15): HttpMessageHandler, HttpStatusCode, IHttpClientFactory, CancellationToken, Fact, Func, HttpClient, HttpRequestMessage (+7 more)

### Community 92 - "Video"
Cohesion: 0.17
Nodes (16): BotLocalizer, DateTime, EmbedBuilder, TimeSpan, YTApiVideo, EmbedBuilderFactory, DateTime, Video (+8 more)

### Community 93 - "NotifierMetrics.cs"
Cohesion: 0.08
Nodes (24): NotificationBusMetricResult, Deduplicated, Dispatched, DispatchFailed, InvalidPayload, TwitchSubscriptionRoleOperation, Remove, Synchronize (+16 more)

### Community 94 - ".AssertKeysAbsentAsync"
Cohesion: 0.31
Nodes (8): TimeSpan, NotificationBusConsumerOptions, Default, IDatabase, RedisComponentFact, StreamEntry, Task, NotificationBusConsumerRedisComponentTests

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

### Community 106 - "DiscordStreamNotifyBot.Interaction.Attribute"
Cohesion: 0.08
Nodes (16): Attribute, DiscordStreamNotifyBot.Interaction.Utility, DiscordStreamNotifyBot.Interaction.Attribute, DiscordStreamNotifyBot.Interaction.OwnerOnly, DiscordStreamNotifyBot.Interaction.TwitCasting, DiscordStreamNotifyBot.Command.Admin, DiscordStreamNotifyBot.Interaction.Help.Service, DiscordStreamNotifyBot.Interaction.Twitch (+8 more)

### Community 113 - ".CreateOrRepairConfigurationAsync"
Cohesion: 0.26
Nodes (11): CancellationToken, DiscordSocketClient, Exception, GuildTwitchSubscriptionConfig, ICollection, IRole, NotifierMetrics, SocketGuild (+3 more)

### Community 114 - ".LoadSnapshotAsync"
Cohesion: 0.42
Nodes (4): CancellationToken, ICollection, Task, MemberRoleOwnershipService

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
Cohesion: 0.23
Nodes (7): IDatabase, Task, TimeSpan, ClusterService, RedisComponentFact, Task, ClusterServiceRedisComponentTests

### Community 119 - "AdminSettingsMutationResult"
Cohesion: 0.07
Nodes (28): IInteractionService, CancellationToken, DiscordSocketClient, IServiceProvider, SocketGuild, Task, UtilityService, DiscordSocketClient (+20 more)

### Community 120 - "Twitch OAuth 與零成本 EventSub 實作計畫"
Cohesion: 0.14
Nodes (13): 0. 涉及專案, 10. Backend EventSub Webhook, 12. Frontend, 14. Grafana, 18. 建置與遷移, 19. 部署順序, 1. 不可偏離的決策, 20. 官方參考 (+5 more)

### Community 121 - "TwitchEventSubEnsureResult"
Cohesion: 0.08
Nodes (30): EventSubSubscription, IReadOnlyList, Stream, TwitchEventSubDeleteResult, DeletedSubscriptionIds, Status, TwitchEventSubDeleteStatus, ApiFailure (+22 more)

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

### Community 130 - "7. 分階段執行"
Cohesion: 0.25
Nodes (8): 7. 分階段執行, 階段 0：建立基準, 階段 1：加入 Serilog 與 bootstrap logger, 階段 2：搬移 console 與檔案路由, 階段 3：切換 Loki sink, 階段 4：整理 facade 與 Discord.Net adapter, 階段 5：移除自製 sink 與更新文件, 階段 6：後續漸進式 structured logging（不阻擋本計畫完成）

### Community 131 - "SendMsgToAllGuildService"
Cohesion: 0.08
Nodes (25): ButtonCheckData, IInteractionService, SendAllPayload, ChoiceDisplayAttribute, DiscordSocketClient, Embed, HttpException, Task (+17 more)

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

### Community 137 - "DiscordStreamNotifyBot.Migrations"
Cohesion: 0.22
Nodes (6): DiscordStreamNotifyBot.Migrations, DateTime, MigrationBuilder, DateTime, ModelBuilder, RefactorDbContext

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
Nodes (29): DiscordStreamNotifyBot.Interaction.Help, RequireBotPermissionAttribute, RequireUserPermissionAttribute, AutocompletionResult, HelpService, IAutocompleteInteraction, IInteractionContext, InteractionService (+21 more)

### Community 144 - "13. Prometheus"
Cohesion: 0.67
Nodes (3): 13.1 Backend 指標, 13.2 Scraper 指標, 13. Prometheus

### Community 145 - "4. 安全刪除狀態機"
Cohesion: 0.67
Nodes (3): 4.1 直播中授權失效, 4.2 關台後重新判斷, 4. 安全刪除狀態機

### Community 146 - "TwitchNotification"
Cohesion: 0.07
Nodes (30): DateTime, List, NotifyType, TwitcastingNotification, Category, ChannelId, ChannelTitle, IsPrivate (+22 more)

### Community 147 - ".Plan"
Cohesion: 0.21
Nodes (11): HashSet, IReadOnlyCollection, IReadOnlyList, TwitchEventSubCreateSpec, TwitchEventSubFact, TwitchEventSubFinalDecision, IsSuccess, TwitchEventSubReconcilePlan (+3 more)

### Community 148 - "MainDbContextModelSnapshot.cs"
Cohesion: 0.33
Nodes (4): ModelSnapshot, DateTime, ModelBuilder, MainDbContextModelSnapshot

### Community 149 - "DiscordStreamNotifyBot.DataBase.Table"
Cohesion: 0.09
Nodes (13): DiscordStreamNotifyBot.Tests.Component.MySql, DiscordStreamNotifyBot.DataBase.Table, DiscordStreamNotifyBot.Command.Youtube, DiscordStreamNotifyBot.Command.Attribute, DiscordStreamNotifyBot.Interaction.TwitchSubscription, DiscordStreamNotifyBot.SharedService.TwitchSubscription, DiscordStreamNotifyBot.Command.Twitch, DateTime (+5 more)

### Community 150 - ".StartAndBlockAsync"
Cohesion: 0.09
Nodes (22): AdminSettingsService, BotLocalizer, CommandDisplayResolver, EmojiService, GuildLocaleService, InteractionService, LocaleResolver, MemberOperationCoordinator (+14 more)

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

### Community 156 - "MemberRoleOwnershipSnapshot"
Cohesion: 0.17
Nodes (13): IEnumerable, IReadOnlyCollection, MemberEntitlementProvider, Twitch, Youtube, MemberRoleEntitlement, MemberRoleOwnershipPolicy, MemberRoleOwnershipSnapshot (+5 more)

### Community 157 - ".CreateService"
Cohesion: 0.13
Nodes (10): OperationCanceledException, CollectorRegistry, DiscordSocketClient, MySqlComponentFact, Task, YoutubeMemberCleanupPersistenceTests, Fact, InlineData (+2 more)

### Community 158 - "DbEntity"
Cohesion: 0.09
Nodes (18): BannerChange, ChannelId, GuildId, LastChangeStreamId, DateTime, DbEntity, DateAdded, Id (+10 more)

### Community 159 - "YoutubeMemberApiClientTests"
Cohesion: 0.24
Nodes (5): Fact, InlineData, Theory, YoutubeMemberApiClientTests, TokenResponseException

### Community 160 - "Movie"
Cohesion: 0.09
Nodes (22): Movie, Category, CommentCount, Country, Created, CurrentViewCount, Duration, HlsUrl (+14 more)

### Community 161 - "DiscordStreamNotifyBot.Shared.Messages"
Cohesion: 0.08
Nodes (21): DiscordStreamNotifyBot.Scraper.Detection.Twitch.Debounce, DiscordStreamNotifyBot.Scraper.Detection.Twitch, DiscordStreamNotifyBot.SharedService.Youtube.Json, DiscordStreamNotifyBot.Shared.Messages, ConnectionMultiplexer, IDatabase, ISubscriber, IUser (+13 more)

### Community 162 - "AddTwitchSubscriptionVerification"
Cohesion: 0.22
Nodes (5): DateTime, MigrationBuilder, DateTime, ModelBuilder, AddTwitchSubscriptionVerification

### Community 163 - "AddTwitchSubscriptionDeletionPending"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, AddTwitchSubscriptionDeletionPending

### Community 164 - "RedisComponentFixture"
Cohesion: 0.13
Nodes (16): ConfigurationOptions, FactAttribute, ICollectionFixture, MySqlComponentFactAttribute, MySqlComponentCollection, ConnectionMultiplexer, IDatabase, RedisKey (+8 more)

### Community 165 - "AddYoutubeMemberVerificationDurability"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, AddYoutubeMemberVerificationDurability

### Community 166 - ".CheckRequirementsAsync"
Cohesion: 0.15
Nodes (9): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildMemberCountAttribute, ErrorMessage, GuildMemberCount (+1 more)

### Community 167 - ".NotifyAddedAsync"
Cohesion: 0.20
Nodes (10): Components, Embed, MessageComponent, SocketGuild, Task, CrawlerOwnerNotifier, CrawlerPlatform, Twitcasting (+2 more)

### Community 168 - "AddGoogleOAuthUnlinkIntent"
Cohesion: 0.22
Nodes (5): DateTime, MigrationBuilder, DateTime, ModelBuilder, AddGoogleOAuthUnlinkIntent

### Community 169 - "RenameVerificationLogChannel"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, RenameVerificationLogChannel

### Community 170 - "DebounceFixture"
Cohesion: 0.23
Nodes (13): UserLogin, UserName, CancellationToken, Fact, IReadOnlyCollection, List, Task, UserId (+5 more)

### Community 171 - "GuildYoutubeMemberConfig"
Cohesion: 0.15
Nodes (13): SocketRole, SocketTextChannel, SocketGuild, Task, GuildYoutubeMemberConfig, DeletionPending, GuildId, IsManualVideoId (+5 more)

### Community 172 - "DiscordStreamNotifyBot.Tests"
Cohesion: 0.11
Nodes (7): DiscordStreamNotifyBot.Scraper.Detection.Youtube, DiscordStreamNotifyBot.Auth, DiscordStreamNotifyBot.Tests, DiscordStreamNotifyBot.SharedService.Twitch, BotStateCollectionDefinition, Fact, NotificationBusConsumerOptionsTests

### Community 173 - "TwitchGuildEligibilityStatus"
Cohesion: 0.15
Nodes (16): ConcurrentDictionary, DateTime, Task, TimeProvider, TimeSpan, TwitchGuildEligibilityEvaluator, TwitchGuildEligibilityStatus, Eligible (+8 more)

### Community 175 - ".TryGetKey"
Cohesion: 0.27
Nodes (5): NotificationDedupPolicy, Fact, InlineData, Theory, NotificationDedupPolicyTests

### Community 176 - "TwitchSubscriptionPoliciesTests"
Cohesion: 0.11
Nodes (10): DateTimeOffset, IEnumerable, IReadOnlyCollection, TwitchAuthorizationEventPolicy, TwitchRateLimitPolicy, TwitchSubscriptionConfigurationPolicy, GuildTwitchSubscriptionConfig, InlineData (+2 more)

### Community 177 - "NotificationMetricEvent"
Cohesion: 0.14
Nodes (13): Event, Platform, NotificationMetricEvent, TwitcastingStart, TwitchChangeData, TwitchEnd, TwitchStart, YoutubeChangeTime (+5 more)

### Community 178 - "Help"
Cohesion: 0.09
Nodes (18): DiscordStreamNotifyBot.Command.Help, IEqualityComparer, Func, CommonEqualityComparer, Alias, Command, CommandInfo, CommandService (+10 more)

### Community 179 - ".Classify"
Cohesion: 0.14
Nodes (16): DateTime, YoutubeApiVideoAction, ActiveChatOnly, Ignore, IgnoreFakePost, NewVideo, Scheduled, Started (+8 more)

### Community 180 - "YoutubeMemberVideoLogNotification"
Cohesion: 0.08
Nodes (27): Task, YoutubeMemberCandidateAction, AbortDiscovery, IgnoreCommentsDisabled, IgnorePublicVideo, IgnoreUnavailable, SelectMemberOnlyVideo, YoutubeMemberCandidateFacts (+19 more)

### Community 181 - "TwitchChannelUpdateInfo"
Cohesion: 0.13
Nodes (14): CancellationTokenRegistration, DebouncedEventArgs, Debouncer, ObjectDisposedException, Func, IReadOnlyCollection, Task, DebounceChannelUpdateMessage (+6 more)

### Community 182 - ".HandleStartLiveMessageAsync"
Cohesion: 0.21
Nodes (9): Category, List, RedisValue, SemaphoreSlim, Task, TwitcastingDetectionService, IsEnable, Category (+1 more)

### Community 183 - "ReactionEventWrapper"
Cohesion: 0.29
Nodes (8): Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task, ReactionEventWrapper, Message

### Community 184 - "YoutubePubSubNotification"
Cohesion: 0.15
Nodes (12): YoutubePubSubNotification, ChannelId, Link, NotificationType, Published, Title, Updated, VideoId (+4 more)

### Community 185 - "MigrationAndConstraintTests"
Cohesion: 0.30
Nodes (5): DbUpdateException, MySqlComponentFact, Task, TwitchBroadcasterAuthorization, MigrationAndConstraintTests

### Community 186 - "DiscordStreamNotifyBot.Command"
Cohesion: 0.15
Nodes (9): DiscordStreamNotifyBot.Command, SocketCommandContext, SocketMessage, CommandService, DiscordSocketClient, IServiceProvider, Task, CommandHandler (+1 more)

### Community 187 - "DiscordStreamNotifyBot"
Cohesion: 0.15
Nodes (6): DiscordStreamNotifyBot, IDataStore, Task, ITokenDataStore, Task, NonPersistentGoogleDataStore

### Community 188 - "TwitchAuthorizationLocalState"
Cohesion: 0.28
Nodes (6): TwitchAuthorizationLocalState, Active, Missing, PersistedInvalid, TemporaryFailure, TwitchAuthorizationLocalStatePolicy

### Community 189 - ".RunAsync"
Cohesion: 0.15
Nodes (13): ServiceProvider, TwitchApiService, YoutubeApiService, DetectionHost, Task, CancellationToken, PeriodicTimer, Task (+5 more)

### Community 190 - "MySqlDataStore"
Cohesion: 0.36
Nodes (3): CancellationToken, Task, MySqlDataStore

### Community 191 - "ArgumentOutOfRangeException"
Cohesion: 0.27
Nodes (6): ArgumentOutOfRangeException, CollectorRegistry, InlineData, Task, Theory, NotifierMetricsTests

### Community 192 - "Category"
Cohesion: 0.21
Nodes (11): List, CategoriesJson, Categories, Category, Id, Name, SubCategories, SubCategory (+3 more)

### Community 193 - ".DecideAutomaticMutation"
Cohesion: 0.24
Nodes (6): YoutubeMemberAutomaticMutationAction, Apply, PreserveManualPin, YoutubeMemberManualPinPolicy, Fact, YoutubeMemberManualPinPolicyTests

### Community 194 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, ConfiguredBroadcasterAutocompleteHandler

### Community 195 - ".OnReaction"
Cohesion: 0.29
Nodes (5): DiscordSocketClient, Func, ICommandContext, ReactionEventWrapper, SocketReaction

### Community 196 - "NijisanjiStreamJson"
Cohesion: 0.08
Nodes (27): DateTime, List, Channel, Id, Liver, Main, Name, ThumbnailUrl (+19 more)

### Community 197 - ".Init"
Cohesion: 0.36
Nodes (5): DiscordSocketClient, HttpClient, Task, Timer, UptimeKumaClient

### Community 198 - ".GetLocaleAsync"
Cohesion: 0.09
Nodes (30): InteractionModuleBase, BotLocalizer, CommandDisplayResolver, GuildLocaleService, LocaleResolver, Task, TopLevelModule, BotLocalizer (+22 more)

### Community 199 - ".CheckPermissionsAsync"
Cohesion: 0.33
Nodes (5): CommandInfo, ICommandContext, IServiceProvider, PreconditionResult, Task

### Community 200 - "TcBackendStreamData.cs"
Cohesion: 0.07
Nodes (29): App, Mode, Url, BackendMovie, Id, Live, Fmp4, Host (+21 more)

### Community 201 - "AdminSettingsSnapshot"
Cohesion: 0.11
Nodes (19): AdminSettingsCommon, GlobalNoticeChannelId, Locale, VerificationLogChannelId, AdminSettingsGuild, MemberCount, Name, AdminSettingsHealth (+11 more)

### Community 202 - ".GroupName"
Cohesion: 0.20
Nodes (12): IDatabase, RedisKey, RedisValue, StreamEntry, StreamGroupInfo, Task, TimeSpan, NotificationBus (+4 more)

### Community 203 - "RedisConnection"
Cohesion: 0.32
Nodes (4): ConnectionMultiplexer, Lazy, RedisConnection, Instance

### Community 204 - "InteractionErrorPolicyTests"
Cohesion: 0.33
Nodes (5): Fact, InlineData, InteractionCommandError, Theory, InteractionErrorPolicyTests

### Community 205 - "MainDbService"
Cohesion: 0.10
Nodes (21): DbContextOptions, SendMsgToAllGuildService, DefaultMemberPermissions, RequireOwner, SlashCommand, Task, SendMsgToAllGuild, ComponentInteraction (+13 more)

### Community 206 - ".SendErrorMessageAsync"
Cohesion: 0.29
Nodes (10): IDMChannel, KeyNotFoundException, BotLocalizer, DiscordSocketClient, EmbedBuilder, HttpException, ITextChannel, IUserMessage (+2 more)

### Community 207 - "TwitchSpiderRemovalAction"
Cohesion: 0.15
Nodes (13): TwitchSpiderRemovalMetricReason, TwitchSpiderRemovalAction, AlreadyRemoved, DeferApiFailure, DeferLive, DeferNotifier, DeferSnapshot, EvaluateEligibility (+5 more)

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

### Community 214 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildNoticeYoutubeChannelIdAutocompleteHandler

### Community 215 - "YouTube 會員驗證架構重構計畫"
Cohesion: 0.15
Nodes (12): 11. 排程與生命週期, 12. Provider Result 分類, 17. Manual Acceptance Matrix, 18. 停機部署順序, 19. Completion Criteria, 1. 範圍, 20. 新 Session 執行規則, 2. 已定案決策 (+4 more)

### Community 216 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildYoutubeChannelSpiderAutocompleteHandler

### Community 217 - "NijisanjiLiverJson"
Cohesion: 0.09
Nodes (22): List, Head, Height, Url, Width, Images, Head, NijisanjiLiverJson (+14 more)

### Community 218 - "NotifierMetrics"
Cohesion: 0.09
Nodes (22): Histogram, Counter, Gauge, TimeSpan, TwitchSubscriptionStatus, NotifierMetrics, TwitchSubscriptionProviderError, InvalidResponse (+14 more)

### Community 219 - "Movie"
Cohesion: 0.05
Nodes (39): Broadcaster, Created, Id, Image, IsLive, LastMovieId, Level, Name (+31 more)

### Community 220 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildYoutubeMemberCheckChannelIdAutocompleteHandler

### Community 221 - ".SlashCommandExecuted"
Cohesion: 0.19
Nodes (8): IResult, SocketInteraction, SocketInteractionContext, SocketSlashCommandDataOption, IDiscordInteraction, IInteractionContext, SlashCommandInfo, Task

### Community 222 - "TwitchStateDecisions.cs"
Cohesion: 0.09
Nodes (23): DateTime, TimeSpan, TwitchChannelStateFacts, TwitchChannelUpdateAction, Ignore, Queue, RefreshState, TwitchChannelUpdateChange (+15 more)

### Community 223 - "InteractionMetadataFixture"
Cohesion: 0.13
Nodes (13): IServiceProvider, IServiceScope, IServiceScopeFactory, Dictionary, DiscordSocketClient, InteractionService, Type, InteractionMetadataFixture (+5 more)

### Community 224 - "5. 語系模型與解析規則"
Cohesion: 0.33
Nodes (6): 5.1 支援值, 5.2 公開內容與背景通知, 5.3 私人即時回覆, 5.4 延遲會限驗證 DM, 5.5 併發安全, 5. 語系模型與解析規則

### Community 225 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildNoticeTwitCastingChannelIdAutocompleteHandler

### Community 226 - "15. 實作階段"
Cohesion: 0.20
Nodes (10): 15. 實作階段, Phase 0：Baseline 與 characterization, Phase 1：Schema 與 migration, Phase 2：共用操作與 role ownership, Phase 3：YouTube interaction 與 state machine, Phase 4：Role/config durability, Phase 5：Provider 與 lifecycle, Phase 6：Backend (+2 more)

### Community 227 - "AdminSettingsCrawlerPlatform"
Cohesion: 0.14
Nodes (15): Name, IEnumerable, AdminSettingsCrawlerItem, SourceId, SourceName, AdminSettingsCrawlerPlatform, Count, Enabled (+7 more)

### Community 228 - "AutocompleteHandler"
Cohesion: 0.20
Nodes (9): AutocompleteHandler, HelpGetCommandsAutocompleteHandler, HelpGetModulesAutocompleteHandler, AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider (+1 more)

### Community 229 - "10. 執行期互動本地化"
Cohesion: 0.40
Nodes (5): 10.1 共用回覆 API, 10.2 Precondition 與 handler 錯誤, 10.3 例外訊息, 10.4 第一階段模組, 10. 執行期互動本地化

### Community 230 - ".LoadCommandFrom"
Cohesion: 0.40
Nodes (4): Assembly, IEnumerable, IServiceCollection, Type

### Community 231 - "Broadcaster"
Cohesion: 0.09
Nodes (21): List, Broadcaster, Created, Id, Image, IsLive, LastMovieId, Level (+13 more)

### Community 232 - "TwitchBroadcasterAuthorization"
Cohesion: 0.12
Nodes (16): DateTime, TwitchBroadcasterAuthorization, AuthorizedAt, ClientId, DateUpdated, DiscordUserId, DisplayName, EncryptedAccessToken (+8 more)

### Community 233 - ".ValidateCommandLocalizationResources"
Cohesion: 0.18
Nodes (8): ISet, Dictionary, DictionaryEntry, HashSet, IDictionary, IList, LocalizationTarget, ModuleInfo

### Community 234 - "opencode.json"
Cohesion: 0.50
Nodes (3): plugin, $schema, .opencode/plugins/graphify.js

### Community 235 - "YouTube 會員驗證"
Cohesion: 0.33
Nodes (5): Durable state, YouTube 會員驗證, 使用者契約, 服務邊界, 部署前驗證

### Community 236 - "GuildSnapshot"
Cohesion: 0.15
Nodes (13): DiscordSocketClient, DateTime, List, GuildSnapshot, Id, MemberCount, Name, OwnerId (+5 more)

### Community 237 - "AdminSettingsNotifications"
Cohesion: 0.12
Nodes (16): AdminSettingsNotifications, Twitcasting, Twitch, Youtube, AdminSettingsTwitcastingNotification, ChannelId, DetectionEnabled, SourceId (+8 more)

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
Cohesion: 0.17
Nodes (14): IAsyncDisposable, LeaseGroup, CancellationToken, ConcurrentDictionary, IEnumerable, Lease, List, SemaphoreSlim (+6 more)

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

### Community 252 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildNoticeTwitchChannelIdAutocompleteHandler

### Community 253 - ".SendLocalizedConfirmAsync"
Cohesion: 0.20
Nodes (10): DiscordSocketClient, DiscordWebhookClient, SlashCommand, Task, Utility, RequireContext, SlashCommand, Task (+2 more)

### Community 254 - "AdminSettingsYoutubeMessages"
Cohesion: 0.13
Nodes (15): AdminSettingsYoutubeMessages, ChangeTime, Delete, End, NewStream, NewVideo, Start, AdminSettingsYoutubeNotification (+7 more)

### Community 255 - "DiscordStreamNotifyBot.HttpClients.Twitcasting.Model"
Cohesion: 0.18
Nodes (9): DiscordStreamNotifyBot.HttpClients.Twitcasting.Model, DiscordStreamNotifyBot.Scraper.Detection.Twitcasting, DateTime, TwitcastingLiveStartAction, IgnoreDuplicate, PersistAndNotify, PersistRequestRecordingAndNotify, TwitcastingLiveStartPlan (+1 more)

### Community 256 - "TwitchSubscriptionStatus"
Cohesion: 0.14
Nodes (13): TwitchAuthorizationAccessResult, AccessToken, DisplayName, Status, TwitchUserId, UserLogin, TwitchSubscriptionStatus, AuthorizationInvalid (+5 more)

### Community 257 - "TwitcastingSpider"
Cohesion: 0.33
Nodes (8): CommandExample, CommandSummary, DefaultMemberPermissions, RequireGuildMemberCount, SlashCommand, Task, TwitcastingService, TwitcastingSpider

### Community 258 - "YoutubeMemberPolicies.cs"
Cohesion: 0.15
Nodes (11): YoutubeMemberRoleApplyResult, Applied, Failed, UnknownMember, YoutubeMemberSingleConfigurationQueueAction, Add, PreserveQueued, PreserveVerified (+3 more)

### Community 259 - "TwitchEventSubMetricStatus"
Cohesion: 0.14
Nodes (14): TwitchEventSubMetricStatus, AuthorizationRevoked, BetaMaintenance, Enabled, ModeratorRemoved, NotificationFailuresExceeded, Unknown, UserRemoved (+6 more)

### Community 260 - "AdminSettingsYoutubeVerification"
Cohesion: 0.14
Nodes (14): List, AdminSettingsVerification, Twitch, Youtube, AdminSettingsYoutubeVerification, DeletionPending, PendingRoleRemovalCount, PreviousRoleId (+6 more)

### Community 261 - ".FailedActionRemainsRetryableAndReportedFailuresPreventAcknowledgment"
Cohesion: 0.23
Nodes (8): AggregateException, Fact, IMessage, IMessageChannel, IUserMessage, Task, TimeoutException, NotificationDeliveryProgressTests

### Community 262 - "TwitchStream"
Cohesion: 0.12
Nodes (14): HelixStream, TwitchStreamDataFacts, TwitchStreamNotificationFactory, DateTime, TwitchStream, GameName, StreamEndAt, StreamId (+6 more)

### Community 263 - "YoutubeMemberVerificationResult"
Cohesion: 0.15
Nodes (13): YoutubeMemberVerificationResult, CommentsDisabled, CredentialExpired, Member, NotMember, Provider4xx, Provider5xx, QuotaExceeded (+5 more)

### Community 264 - "NoticeYoutubeStreamChannel"
Cohesion: 0.15
Nodes (12): NoticeYoutubeStreamChannel, ChangeTimeMessage, DeleteMessage, DiscordNoticeStreamChannelId, DiscordNoticeVideoChannelId, EndMessage, GuildId, IsCreateEventForNewStream (+4 more)

### Community 265 - "AdminYoutubeMessagesPayload"
Cohesion: 0.15
Nodes (13): AdminYoutubeMessagesPayload, ChangeTime, Delete, End, NewStream, NewVideo, Start, AdminYoutubeUpsertPayload (+5 more)

### Community 266 - "YoutubeNotification"
Cohesion: 0.15
Nodes (13): YTChannelType, YoutubeNotification, ActualEndTime, ActualStartTime, ChannelId, ChannelTitle, ChannelType, IsMemberOnly (+5 more)

### Community 267 - "YoutubeMemberProbeResultKind"
Cohesion: 0.18
Nodes (11): YoutubeMemberProbeResult, PreservesEntitlement, YoutubeMemberProbeResultKind, AuthorizationInvalid, LocalContractFailure, Member, NotMember, ProbeVideoInvalid (+3 more)

### Community 268 - "TwitchSpider"
Cohesion: 0.17
Nodes (11): DateTime, TwitchSpider, DateAdded, GuildId, IsRecord, IsWarningUser, OfflineImageUrl, ProfileImageUrl (+3 more)

### Community 269 - "TwitchSubscriptionCheck"
Cohesion: 0.17
Nodes (11): DateTime, TwitchSubscriptionCheck, BroadcasterId, DiscordUserId, GuildId, IsChecked, IsGift, LastCheckTime (+3 more)

### Community 270 - "GuildConfig"
Cohesion: 0.18
Nodes (10): GuildConfig, GuildId, Locale, MaxTwitcastingSpiderCount, MaxTwitchSpiderCount, MaxTwitterSpaceSpiderCount, MaxYouTubeMemberCheckCount, MaxYouTubeSpiderCount (+2 more)

### Community 271 - "AdminSettingsTwitchVerification"
Cohesion: 0.18
Nodes (11): Dictionary, AdminSettingsTwitchVerification, DeletionPending, PendingRoleRemovalCount, PreviousSubscriberRoleId, SourceId, SourceLogin, SourceName (+3 more)

### Community 272 - "NotificationDeliveryResult"
Cohesion: 0.20
Nodes (10): NotificationDeliveryResult, AuthorizationFailure, Disabled, Discord5xx, MissingChannel, MissingGuild, MissingPermission, Sent (+2 more)

### Community 273 - "TwitchReconcileAction"
Cohesion: 0.20
Nodes (10): TwitchReconcileAction, DeferApiFailure, DeferLive, DeleteSubscriptions, DeleteSubscriptionsThenEvaluateGuild, EnsureFallbackSubscriptions, EnsurePermanentSubscriptions, KeepPollingWithoutSubscriptions (+2 more)

### Community 274 - "AdminSettingsResources"
Cohesion: 0.20
Nodes (10): AdminSettingsResources, Channels, Roles, AdminSettingsRole, BotCanManage, Everyone, Id, Managed (+2 more)

### Community 275 - "GracefulShutdown"
Cohesion: 0.33
Nodes (4): CancellationToken, CancellationTokenSource, GracefulShutdown, Token

### Community 276 - "8. 分階段實作步驟"
Cohesion: 0.22
Nodes (9): 8. 分階段實作步驟, 階段 0：止血 PR — shard 歸屬守衛, 階段 1：Solution 骨架 + Shared, 階段 2：Notifier 上線（先維持單 shard 行為）, 階段 3：Scraper 拆出 + Redis Streams 匯流排（完成，正確性待測試環境驗）, 階段 4：Coordinator（完成，正確性待測試環境驗）, 階段 5：跨 shard 指令與共享狀態（完成，正確性待測試環境驗）, 階段 6：Docker 化與部署驗證（檔案完成，實跑待測試環境） (+1 more)

### Community 277 - ".Info"
Cohesion: 0.39
Nodes (3): ComponentInteraction, Task, SpiderManagementComponent

### Community 278 - ".MissingOrShortKeyIsRejected"
Cohesion: 0.25
Nodes (5): Fact, InlineData, InvalidOperationException, Theory, ProviderTokenEncryptionKeyTests

### Community 279 - "YoutubeChannelSpider"
Cohesion: 0.22
Nodes (8): DateTime, YoutubeChannelSpider, ChannelId, ChannelTitle, DateAdded, GuildId, IsTrustedChannel, LastSubscribeTime

### Community 280 - "MySqlComponentFixture"
Cohesion: 0.46
Nodes (4): IAsyncLifetime, Task, MySqlComponentFixture, DbService

### Community 281 - "PreconditionAttribute"
Cohesion: 0.25
Nodes (6): PreconditionAttribute, RequireGuildMemberCountAttribute, ErrorMessage, GuildMemberCount, RequireGuildAttribute, GuildId

### Community 282 - ".MakeNamesUnique"
Cohesion: 0.36
Nodes (3): IEnumerable, IReadOnlyList, AutocompleteSearch

### Community 283 - "NotificationChannelIssue"
Cohesion: 0.25
Nodes (8): NotificationChannelIssue, ChannelId, ChannelName, GuildId, GuildName, MissingPermissions, Platform, Usages

### Community 284 - "YoutubeMemberAuthorizationService.cs"
Cohesion: 0.29
Nodes (7): GoogleCredential, YoutubeMemberAuthorizationResult, YoutubeMemberAuthorizationStatus, AuthorizationInvalid, LocalContractFailure, Ready, TemporaryFailure

### Community 285 - "NoticeTwitchStreamChannel"
Cohesion: 0.25
Nodes (7): NoticeTwitchStreamChannel, ChangeStreamDataMessage, DiscordChannelId, EndStreamMessage, GuildId, NoticeTwitchUserId, StartStreamMessage

### Community 286 - "YoutubeChannelOwnedType"
Cohesion: 0.25
Nodes (7): DateTime, YTChannelType, YoutubeChannelOwnedType, ChannelId, ChannelTitle, ChannelType, DateAdded

### Community 287 - "GetAllRegistedWebHookJson"
Cohesion: 0.29
Nodes (7): List, GetAllRegistedWebHookJson, AllCount, Webhooks, Webhook, Event, UserId

### Community 288 - "AdminSettingsChannel"
Cohesion: 0.25
Nodes (8): AdminSettingsChannel, CanEmbedLinks, CanManageEvents, CanSendMessages, CanView, Id, Name, Type

### Community 289 - "Stub"
Cohesion: 0.43
Nodes (5): DispatchProxy, MethodInfo, Stub, Func, Stub

### Community 290 - "AdministrationComponent"
Cohesion: 0.40
Nodes (4): ComponentInteraction, NotificationChannelCheckResponse, Task, AdministrationComponent

### Community 291 - ".Format"
Cohesion: 0.48
Nodes (3): LogEventLevel, LogTextFormatter, TextWriter

### Community 292 - ".LoadInteractionFrom"
Cohesion: 0.29
Nodes (5): Assembly, Func, IEnumerable, IServiceCollection, Type

### Community 293 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildTwitchSpiderAutocompleteHandler

### Community 294 - "GuildInfoResponse"
Cohesion: 0.29
Nodes (7): Dictionary, GuildInfoResponse, Channels, MemberCount, Name, OwnerId, ShardId

### Community 295 - "14. 部署與回滾"
Cohesion: 0.50
Nodes (4): 14.1 建議部署順序, 14.2 相容性, 14.3 回滾, 14. 部署與回滾

### Community 296 - "TwitchRefreshPersistenceDecision"
Cohesion: 0.29
Nodes (5): TwitchRefreshPersistenceDecision, AlreadyPersisted, Stale, WriteReplacement, TwitchRefreshPersistencePolicy

### Community 297 - "LogLevel"
Cohesion: 0.29
Nodes (7): LogLevel, Critical, Debug, Error, Info, Trace, Warn

### Community 298 - "2. 專案拆分 (Solution Layout)"
Cohesion: 0.33
Nodes (6): 2.1 `Shared`（共用 library）, 2.2 `Scraper`（爬蟲層，叢集唯一）, 2.3 `Notifier`（通知層 / shard，可多個）, 2.4 `Coordinator`（主控層，1 個）, 2.5 SharedService 逐服務拆分歸屬（判斷準則表）, 2. 專案拆分 (Solution Layout)

### Community 299 - "4. 訊息契約：Redis Streams 通知匯流排"
Cohesion: 0.33
Nodes (6): 4.1 拓撲, 4.2 DTO（`Shared/Messages/`）, 4.3 消費迴圈（Notifier）, 4.4 建群與 Preflight, 4.5 Redis 控制平面鍵（非 stream）, 4. 訊息契約：Redis Streams 通知匯流排

### Community 300 - "9. 測試計畫"
Cohesion: 0.33
Nodes (6): 9.1 Characterization tests, 9.2 Bot unit／contract tests, 9.3 Component tests, 9.4 Backend tests, 9.5 Frontend 驗證, 9. 測試計畫

### Community 301 - "MainDbContextFactory"
Cohesion: 0.40
Nodes (3): IDesignTimeDbContextFactory, Version, MainDbContextFactory

### Community 302 - "YoutubeMemberRoleResult"
Cohesion: 0.33
Nodes (6): YoutubeMemberRoleResult, DiscordError, MissingPermission, Success, UnknownError, UserMissing

### Community 303 - "TwitchOfflineAction"
Cohesion: 0.33
Nodes (6): TwitchOfflineAction, ClearState, Defer, Ignore, PublishEnd, ResumeStream

### Community 304 - "TwitchStreamStartAction"
Cohesion: 0.33
Nodes (6): TwitchStreamStartAction, IgnoreInvalid, IgnoreMissingSpider, PersistStreamAndRefreshState, PublishStart, RefreshStateOnly

### Community 305 - "GoogleOAuthUnlinkIntent"
Cohesion: 0.33
Nodes (5): DateTime, GoogleOAuthUnlinkIntent, DateAdded, DiscordUserId, ExpectedEncryptedToken

### Community 306 - "NoticeTwitcastingStreamChannel"
Cohesion: 0.33
Nodes (5): NoticeTwitcastingStreamChannel, DiscordChannelId, GuildId, ScreenId, StartStreamMessage

### Community 307 - "YoutubeMemberAccessToken"
Cohesion: 0.33
Nodes (5): DateTime, YoutubeMemberAccessToken, DateAdded, DiscordUserId, EncryptedAccessToken

### Community 308 - "6. 資源架構"
Cohesion: 0.40
Nodes (5): 6.1 指令註冊資源, 6.2 執行期訊息資源, 6.3 Help 長文, 6.4 Localizer API, 6. 資源架構

### Community 309 - "5. 目標架構"
Cohesion: 0.40
Nodes (5): 5.1 Console, 5.2 非容器檔案, 5.3 Loki, 5.4 `LOKI_URL` 相容性, 5. 目標架構

### Community 310 - "8. 驗證矩陣"
Cohesion: 0.40
Nodes (5): 8.1 編譯與靜態檢查, 8.2 Console 與檔案, 8.3 Loki, 8.4 生命週期, 8. 驗證矩陣

### Community 311 - "10. 手動驗收矩陣"
Cohesion: 0.40
Nodes (5): 10.1 授權, 10.2 爬蟲, 10.3 YouTube 會員驗證, 10.4 Twitch 訂閱驗證, 10. 手動驗收矩陣

### Community 312 - "5. Contract v1 additive 擴充"
Cohesion: 0.40
Nodes (5): 5.1 Capabilities, 5.2 新增 actions, 5.3 快照頂層, 5.4 回應碼, 5. Contract v1 additive 擴充

### Community 313 - "ClusterQueryType"
Cohesion: 0.40
Nodes (5): ClusterQueryType, GetInviteUrl, GuildInfo, NotificationChannelCheck, UserInfo

### Community 314 - "TwitchProviderResultStatus"
Cohesion: 0.40
Nodes (5): TwitchProviderResultStatus, Failure, Invalid, Success, TemporaryFailure

### Community 315 - "TwitchRoleConfigurationResult"
Cohesion: 0.40
Nodes (5): TwitchRoleConfigurationResult, Config, Error, IsNew, IsSuccess

### Community 316 - "YTChannelType"
Cohesion: 0.40
Nodes (5): YTChannelType, Holo, Nijisanji, NonApproved, Other

### Community 317 - "LogFileRoute"
Cohesion: 0.40
Nodes (5): LogFileRoute, Error, General, None, Stream

### Community 318 - "YoutubeMemberTokenCleanupConcurrencyTests"
Cohesion: 0.60
Nodes (3): MySqlComponentFact, Task, YoutubeMemberTokenCleanupConcurrencyTests

### Community 319 - "TopLevelModule"
Cohesion: 0.32
Nodes (5): ModuleBase, EmbedBuilder, Task, TopLevelModule, _service

### Community 320 - ".AllRegisteredCommandsHaveDescriptionsInEverySupportedLocale"
Cohesion: 0.40
Nodes (3): Fact, Task, InteractionCommandLocalizationTests

### Community 321 - "7. 資料庫變更"
Cohesion: 0.50
Nodes (4): 7.1 `GuildConfig.Locale`, 7.2 `YoutubeMemberCheck.Locale`, 7.3 Migration 鐵則, 7. 資料庫變更

### Community 322 - "8. Frontend 實作"
Cohesion: 0.50
Nodes (4): 8.1 爬蟲頁, 8.2 驗證頁, 8.3 前端狀態, 8. Frontend 實作

### Community 323 - "TwitchStreamEventPayload"
Cohesion: 0.50
Nodes (4): TwitchStreamEventPayload, BroadcasterUserId, BroadcasterUserLogin, BroadcasterUserName

### Community 325 - "TwitchAuthorizationChangedPayload"
Cohesion: 0.67
Nodes (3): TwitchAuthorizationChangedPayload, Status, TwitchUserId

### Community 326 - "TwitchReconcileRequestedPayload"
Cohesion: 0.67
Nodes (3): TwitchReconcileRequestedPayload, Reason, TwitchUserId

## Knowledge Gaps
- **1511 isolated node(s):** `$schema`, `.opencode/plugins/graphify.js`, ``__EFMigrationsHistory``, `net8.0`, `prometheus-net.AspNetCore (8.2.1)` (+1506 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 2319 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **19 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MainDbService` connect `MainDbService` to `TwitcastingSpider`, `.SendLocalizedErrorAsync`, `SendMsgToAllGuildService`, `.Warn`, `ClusterQueryService`, `.GetDbContext`, `.SetMessage`, `MainDbContext`, `.Info`, `YoutubeMemberAuthorizationService`, `.RetryWithBackoffAsync`, `MySqlComponentFixture`, `YoutubeMemberCheck`, `DiscordStreamNotifyBot.Shared.Messages`, `TwitchSpider`, `YoutubeMemberRoleService`, `TwitchDetectionService`, `Twitch`, `GuildLocaleService`, `Administration`, `YoutubeMemberSetting`, `.HandleStartLiveMessageAsync`, `.SetVerificationLogChannelAsync`, `YoutubeStreamService`, `.RunAsync`, `Bot`, `MySqlDataStore`, `.BuildSnapshotAsync`, `.GetLocaleAsync`, `YoutubeStream`, `TwitchService`, `YoutubeDetectionService`, `Task`, `YoutubeMemberService`, `.CreateOrRepairConfigurationAsync`, `.LoadSnapshotAsync`, `AdminSettingsMutationResult`, `.SendLocalizedConfirmAsync`, `TwitcastingClient`?**
  _High betweenness centrality (0.095) - this node is a cross-community bridge._
- **Why does `DiscordStreamNotifyBot.DataBase.Table` connect `DiscordStreamNotifyBot.DataBase.Table` to `YoutubeMemberPolicies.cs`, `TwitchStream`, `NoticeYoutubeStreamChannel`, `DiscordStreamNotifyBot.DataBase`, `TwitchSpider`, `GuildConfig`, `TwitchSubscriptionCheck`, `YoutubeChannelSpider`, `YoutubeMemberCheck`, `NoticeTwitchStreamChannel`, `DbEntity`, `YoutubeChannelOwnedType`, `DiscordStreamNotifyBot.Shared.Messages`, `GuildYoutubeMemberConfig`, `DiscordStreamNotifyBot.Tests`, `GoogleOAuthUnlinkIntent`, `NoticeTwitcastingStreamChannel`, `YoutubeMemberAccessToken`, `GuildTwitchSubscriptionConfig`, `DiscordStreamNotifyBot`, `DiscordStreamNotifyBot.Shared`, `DiscordStreamNotifyBot.SharedService.YoutubeMember`, `YoutubeStream`, `Video`, `TwitchStateDecisions.cs`, `TwitchBroadcasterAuthorization`, `DiscordStreamNotifyBot.Interaction.Attribute`, `TwitcastingStream`, `DiscordStreamNotifyBot.HttpClients.Twitcasting.Model`?**
  _High betweenness centrality (0.067) - this node is a cross-community bridge._
- **Why does `DiscordStreamNotifyBot.Shared.Messages` connect `DiscordStreamNotifyBot.Shared.Messages` to `DiscordStreamNotifyBot.SharedService.YoutubeMember`, `DiscordStreamNotifyBot.Interaction.Attribute`, `DiscordStreamNotifyBot.Tests`, `DiscordStreamNotifyBot.DataBase`, `GuildSnapshot`, `TwitchNotification`, `AdminSettings.cs`, `DiscordStreamNotifyBot.DataBase.Table`, `DiscordStreamNotifyBot`, `DiscordStreamNotifyBot.Shared`, `NotifierMetrics.cs`, `TwitchStateDecisions.cs`, `DiscordStreamNotifyBot.HttpClients.Twitcasting.Model`?**
  _High betweenness centrality (0.058) - this node is a cross-community bridge._
- **What connects `$schema`, `.opencode/plugins/graphify.js`, ``__EFMigrationsHistory`` to the rest of the system?**
  _1511 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `TwitchSubscriptionApiClient` be split into smaller, more focused modules?**
  _Cohesion score 0.1168091168091168 - nodes in this community are weakly interconnected._
- **Should `DiscordStreamNotifyBot.Shared.csproj` be split into smaller, more focused modules?**
  _Cohesion score 0.08333333333333333 - nodes in this community are weakly interconnected._
- **Should `TwitchApiService` be split into smaller, more focused modules?**
  _Cohesion score 0.08325624421831637 - nodes in this community are weakly interconnected._