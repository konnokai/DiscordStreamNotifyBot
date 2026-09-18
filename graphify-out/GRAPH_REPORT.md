# Graph Report - DiscordStreamNotifyBot  (2026-09-18)

## Corpus Check
- 367 files · ~190,881 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 5891 nodes · 12465 edges · 342 communities (313 shown, 19 thin omitted)
- Extraction: 91% EXTRACTED · 9% INFERRED · 0% AMBIGUOUS · INFERRED: 1178 edges (avg confidence: 0.82)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `49ec66fb`
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
- DiscordStreamNotifyBot.Localization
- Extensions
- TwitchSubscriptionService
- Extensions
- AdminSettingsContractTests
- 會限 OAuth Token 儲存改走 MySQL（去 Redis 依賴）計畫
- .AddChannel
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
- NotificationDeliveryProgress
- 直播小幫手
- NotificationEmbedFactoryTests
- TwitchOAuthRefreshLockLease
- .CreateStreamEnded
- .SendCrawlerResultAsync
- YoutubeMemberRoleService
- TwitchDetectionService
- AdminSettingsMutationResult
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
- .OtherScheduleAsync
- .FilterNoNotifyGuilds
- Program
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
- NotificationBusConsumer
- Notifier Bot Logo — interlocking chain-link icon, purple-to-magenta-to-red gradient on light grey circle; flat modern vector branding representing the linking/notification identity of the Discord stream-notify bot
- YoutubeStream
- Broadcaster
- .BuildSnapshotAsync
- AdminSettingsChannel
- YoutubeDetectionService
- graphify reference: query, path, explain
- 自動化測試導入計畫
- Task
- .AutoRecordAsync
- .Get
- .GetDbContext
- .CreateAsyncClient
- EmbedBuilderFactory
- YoutubeNotification
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
- DiscordStreamNotifyBot.DataBase
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
- ClusterQueryService
- .Plan
- HelpDescription (bot feature summary)
- Migration
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
- DiscordStreamNotifyBot.Tests
- .StartAndBlockAsync
- ModifyTwitCastingTable
- AddMaxSpiderCountSettingField
- DiscordStreamNotifyBot.Migrations
- AddTwitchBroadcasterAuthorization
- AddLocalizationSettings
- .LoadSnapshotAsync
- .CreateService
- DbEntity
- NonPersistentGoogleDataStore
- Movie
- BotState
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
- BotStateTests
- .SetMemberCheckVideoIdAsync
- graphify.js
- .TryGetKey
- GuildTwitchSubscriptionConfig
- NotificationMetricEvent
- Twitch
- .Classify
- .CheckMemberShipOnlyVideoIdAsync
- TwitchStateDecisions.cs
- YoutubeMemberVideoLogNotification
- ReactionEventWrapper
- YoutubePubSubNotification
- MigrationAndConstraintTests
- DiscordStreamNotifyBot.Command
- TwitchOAuthRefreshLock
- CommonEqualityComparer
- .Start
- StubHandler
- TwitchReconcileDecisionTests
- Category
- .DecideAutomaticMutation
- .GenerateSuggestionsAsync
- SharedExtensions
- NijisanjiStreamJson
- .Init
- AdminYoutubeMessagesPayload
- .CheckPermissionsAsync
- TcBackendStreamData.cs
- ChzzkPollPolicyTests
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
- .PublishAsync
- YouTube 會員驗證架構重構計畫
- AdminSettingsYoutubeVerification
- TwitchAuthorizationLocalState
- NotifierMetrics
- Movie
- Utility
- .SlashCommandExecuted
- UtilityService
- TwitchValidateTokenData
- 5. 語系模型與解析規則
- DiscordStreamNotifyBot.Shared.Messages
- 15. 實作階段
- TwitcastingService
- AddChzzkAutoRecord
- AdminSettingsYoutubeNotification
- .LoadCommandFrom
- Broadcaster
- TwitchBroadcasterAuthorization
- AdminTwitchMessagesPayload
- opencode.json
- YouTube 會員驗證
- GuildSnapshotEnvelope
- CHZZK 直播通知實作計畫
- 14. Frontend
- 8. DB Schema
- TwitchAccessTokenData
- TwitcastingStream
- MySqlDataStoreTests
- RedisContractTests
- 13. Backend Contract
- 16. 驗證命令
- MemberOperationCoordinator
- .GenerateSuggestionsAsync
- 10. Slash 與 Interaction Cutover
- 6. 目標架構
- 7. 狀態機
- 9. Role 隔離政策
- NoticeType
- .GenerateSuggestionsAsync
- AdminSettingsNotifications
- .MissingOrShortKeyIsRejected
- .GenerateSuggestionsAsync
- AutocompleteHandler
- YoutubeMemberRoleApplyResult
- NoticeChzzkStreamChannel
- 13. 驗證矩陣
- ChzzkStreamIdentityTests
- TwitchStream
- YoutubeMemberVerificationResult
- NoticeYoutubeStreamChannel
- AdminSettings.cs
- 7. 分階段執行
- .GenerateSuggestionsAsync
- TwitchSpider
- TwitchSubscriptionCheck
- GuildConfig
- AdminSettingsTwitchVerification
- NotificationDeliveryResult
- TwitchReconcileAction
- DiscordStreamNotifyBot.Command.Attribute
- GracefulShutdown
- .GenerateSuggestionsAsync
- .AddChannel
- .GetCommandPath
- YoutubeChannelSpider
- MySqlComponentFixture
- PreconditionAttribute
- AdminChzzkMessagesPayload
- NotificationChannelIssue
- AdminSettingsRole
- NoticeTwitchStreamChannel
- YoutubeChannelOwnedType
- ChzzkStream
- AdminSettingsYoutubeMessages
- Stub
- AdministrationComponent
- ReactionEventWrapper
- YoutubeNoticeType
- .GenerateSuggestionsAsync
- GuildInfoResponse
- TwitchSubscriptionRolePolicyTests
- TwitchRefreshPersistenceDecision
- .GenerateSuggestionsAsync
- TwitchProviderResultStatus
- TwitcastingNotification
- AdminSettingsTwitcastingNotification
- MainDbContextFactory
- .ToLabel
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
- ClusterQueryType
- NoticeTwitcastingStreamChannel
- YoutubeMemberSingleConfigurationQueueAction
- Video
- .SendStreamMessageAsync
- .SaveVideosByType
- TopLevelModule
- .ChzzkAddedMessageShowsAutoRecordStateAndExplicitTargetButtons
- TwitchReconcileRequestedPayload
- .GetStreamVideoByVideoId
- TwitchStreamEventPayload
- all.sql
- TwitchAuthorizationChangedPayload
- _Baseline_ExistingDb.sql
- `guild_config`
- 5. 持久化、發布與去重
- YoutubeMemberTokenCleanupConcurrencyTests
- 14. 部署與回滾
- .ConvertDateTimeToDiscordMarkdown

## God Nodes (most connected - your core abstractions)
1. `MainDbContext` - 82 edges
2. `DiscordStreamNotifyBot.DataBase.Table` - 78 edges
3. `DiscordStreamNotifyBot.DataBase` - 72 edges
4. `DiscordStreamNotifyBot.Shared` - 68 edges
5. `YoutubeDetectionService` - 67 edges
6. `YoutubeMemberService` - 65 edges
7. `TwitchDetectionService` - 63 edges
8. `MainDbService` - 57 edges
9. `BotConfig` - 56 edges
10. `DiscordStreamNotifyBot.Tests` - 56 edges

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

## Communities (342 total, 19 thin omitted)

### Community 0 - "TwitchSubscriptionApiClient"
Cohesion: 0.12
Nodes (18): CancellationToken, DateTimeOffset, HttpResponseMessage, IHttpClientFactory, NotifierMetrics, Task, TwitchProviderResult, Status (+10 more)

### Community 1 - ".GetLocaleAsync"
Cohesion: 0.15
Nodes (24): InteractionModuleBase, BotLocalizer, CommandDisplayResolver, GuildLocaleService, LocaleResolver, Task, TopLevelModule, BotLocalizer (+16 more)

### Community 2 - ".Warn"
Cohesion: 0.18
Nodes (13): PendingRefreshPersistence, CancellationToken, NotifierMetrics, Task, TimeSpan, TwitchBroadcasterAuthorization, TwitchAuthorizationAccessResult, AccessToken (+5 more)

### Community 3 - "EmojiService"
Cohesion: 0.20
Nodes (7): DiscordStreamNotifyBot.SharedService, Emote, DiscordSocketClient, EmojiService, ECPayEmote, PayPalEmote, YouTubeEmote

### Community 4 - "DiscordStreamNotifyBot.Shared.csproj"
Cohesion: 0.08
Nodes (23): Microsoft.EntityFrameworkCore.Design (9.0.3), Microsoft.EntityFrameworkCore.Relational (9.0.3), Microsoft.EntityFrameworkCore.Tools (9.0.3), Serilog (4.4.0), Serilog.Sinks.Console (6.1.1), Serilog.Sinks.File (7.0.0), Serilog.Sinks.Grafana.Loki (9.0.1), net8.0 (+15 more)

### Community 5 - "TwitchApiService"
Cohesion: 0.09
Nodes (29): CancellationToken, Clip, DateTime, EventSubSubscription, HttpClient, IReadOnlyList, Lazy, Regex (+21 more)

### Community 6 - "InteractionHandler"
Cohesion: 0.08
Nodes (28): DisplayName, ISet, Value, BotLocalizer, ChoiceDisplayAttribute, CommandDisplayResolver, Dictionary, DictionaryEntry (+20 more)

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

### Community 11 - "FakeTimeProvider"
Cohesion: 0.24
Nodes (10): FakeTimeProvider, CancellationToken, Func, Task, TimeProvider, TimeSpan, PeriodicRunner, Fact (+2 more)

### Community 12 - "DiscordStreamNotifyBot.Localization"
Cohesion: 0.09
Nodes (9): DiscordStreamNotifyBot.Interaction.ServerAdministration, DiscordStreamNotifyBot.SharedService.Youtube, DiscordStreamNotifyBot.SharedService.Twitcasting, DiscordStreamNotifyBot.SharedService.YoutubeMember, DiscordStreamNotifyBot.Interaction.Utility.Service, DiscordStreamNotifyBot.Localization, DiscordStreamNotifyBot.Interaction.YoutubeMember, DiscordStreamNotifyBot.Interaction (+1 more)

### Community 13 - "Extensions"
Cohesion: 0.08
Nodes (23): ManagementBaseObject, Process, Assembly, BotLocalizer, DiscordSocketClient, EmbedBuilder, Func, GuildLocaleService (+15 more)

### Community 14 - "TwitchSubscriptionService"
Cohesion: 0.08
Nodes (35): BotLocalizer, CancellationToken, CancellationTokenSource, ConcurrentDictionary, DateTimeOffset, DiscordSocketClient, GuildLocaleService, NotifierMetrics (+27 more)

### Community 15 - "Extensions"
Cohesion: 0.18
Nodes (12): DiscordSocketClient, EmbedBuilder, Func, ICommandContext, IEmote, IMessage, IMessageChannel, IUserMessage (+4 more)

### Community 16 - "AdminSettingsContractTests"
Cohesion: 0.07
Nodes (24): ActionRowComponent, RequestRoute, Func, AdminSettingsCommandReply, Arguments, Code, ContractVersion, CorrelationId (+16 more)

### Community 17 - "會限 OAuth Token 儲存改走 MySQL（去 Redis 依賴）計畫"
Cohesion: 0.11
Nodes (18): Backend, Bot（本 repo）, MySQL（兩端都已連同一個庫）, 儲存層（現況為 Redis）, 加密與 blob 格式（兩端一致）, 加密金鑰處理, 影響檔案一覽, 待決策（給實作 session） (+10 more)

### Community 18 - ".AddChannel"
Cohesion: 0.25
Nodes (10): CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, IChannel, NoticeType, RequireBotPermission, SlashCommand (+2 more)

### Community 19 - "MainDbContext"
Cohesion: 0.04
Nodes (52): BannerChange, DbContext, GoogleOAuthUnlinkIntent, RecordYoutubeChannel, ChzzkSpider, DbSet, GuildConfig, GuildTwitchSubscriptionConfig (+44 more)

### Community 20 - ".CreateService"
Cohesion: 0.23
Nodes (11): GuildLocaleRequest, ArgumentException, Fact, Func, GuildLocaleService, InvalidOperationException, IReadOnlyCollection, IReadOnlyDictionary (+3 more)

### Community 21 - "YoutubeMemberAuthorizationService"
Cohesion: 0.09
Nodes (21): GoogleAuthorizationCodeFlow, CancellationToken, GoogleCredential, HttpClient, MySqlDataStore, Task, YoutubeMemberAuthorizationResult, YoutubeMemberAuthorizationService (+13 more)

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
Nodes (26): CheckId, Snapshot, ComponentInteraction, Task, IEnumerable, IReadOnlyCollection, IReadOnlyList, YoutubeMemberCheckStateSnapshot (+18 more)

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
Cohesion: 0.22
Nodes (9): Video, YTChannelType, ReminderItem, ChannelType, StreamVideo, Timer, ConcurrentDictionary, Fact (+1 more)

### Community 31 - ".Main"
Cohesion: 0.10
Nodes (16): DiscordStreamNotifyBot.Coordinator, Counter, Gauge, HashSet, StreamGroupInfo, CoordinatorMetrics, CancellationToken, ClusterService (+8 more)

### Community 32 - "YoutubeMemberLifecycleTaskRegistry"
Cohesion: 0.13
Nodes (12): ConcurrentDictionary, DateTime, IEnumerable, Task, TimeSpan, YoutubeMemberLifecyclePolicy, YoutubeMemberLifecycleTaskRegistry, Func (+4 more)

### Community 33 - "NotificationDeliveryProgress"
Cohesion: 0.13
Nodes (17): AggregateException, Dictionary, Exception, Func, IMessageChannel, IUserMessage, List, Task (+9 more)

### Community 34 - "直播小幫手"
Cohesion: 0.13
Nodes (11): MIT License, CHZZK 錄影, Docker Compose 部署, 授權, 服務組成, 本機建置與執行, 直播小幫手, 相關文件 (+3 more)

### Community 35 - "NotificationEmbedFactoryTests"
Cohesion: 0.24
Nodes (8): Color, DateTime, Embed, Fact, InlineData, TableVideo, Theory, NotificationEmbedFactoryTests

### Community 36 - "TwitchOAuthRefreshLockLease"
Cohesion: 0.10
Nodes (24): DateTime, PendingRefreshPersistence, CancellationToken, CancellationTokenSource, RedisKey, RedisValue, Task, TwitchOAuthRefreshLockAcquireResult (+16 more)

### Community 37 - ".CreateStreamEnded"
Cohesion: 0.38
Nodes (6): BotLocalizer, DateTime, EmbedBuilder, IReadOnlyCollection, TimeSpan, TwitchEmbedBuilderFactory

### Community 38 - ".SendCrawlerResultAsync"
Cohesion: 0.18
Nodes (14): ChzzkService, CommandExample, CommandSummary, DefaultMemberPermissions, SlashCommand, Task, ChzzkSpider, CommandExample (+6 more)

### Community 39 - "YoutubeMemberRoleService"
Cohesion: 0.22
Nodes (12): CancellationToken, DiscordSocketClient, GuildYoutubeMemberConfig, IEnumerable, IRole, SocketGuild, Task, YoutubeMemberRoleConfigurationResult (+4 more)

### Community 40 - "TwitchDetectionService"
Cohesion: 0.08
Nodes (30): ChannelUpdate, HelixStream, CancellationTokenSource, DateTime, EventSubSubscription, IReadOnlyCollection, IReadOnlyDictionary, RedisValue (+22 more)

### Community 41 - "AdminSettingsMutationResult"
Cohesion: 0.14
Nodes (10): SocketGuild, CancellationToken, IEnumerable, SocketGuild, Task, JObject, AdminSettingsMutationResult, Arguments (+2 more)

### Community 42 - "ScraperMetrics"
Cohesion: 0.05
Nodes (45): Counter, Gauge, ScraperMetricResult, Failure, Success, ScraperMetrics, TwitchAuthorizationChangeMetricResult, Authorized (+37 more)

### Community 43 - "GuildLocaleService"
Cohesion: 0.07
Nodes (28): CacheEntry, CultureInfo, CancellationToken, ConcurrentDictionary, DateTimeOffset, Dictionary, Func, GuildConfig (+20 more)

### Community 44 - "TwitchChannelUpdateInfo"
Cohesion: 0.13
Nodes (14): CancellationTokenRegistration, DebouncedEventArgs, Debouncer, ObjectDisposedException, Func, IReadOnlyCollection, Task, DebounceChannelUpdateMessage (+6 more)

### Community 45 - "RedisChannels"
Cohesion: 0.10
Nodes (11): AdminSettings, Chzzk, Cluster, Member, Notifier, OAuth, RedisChannels, SharedState (+3 more)

### Community 46 - "ChzzkService"
Cohesion: 0.13
Nodes (18): BotLocalizer, CancellationToken, DiscordSocketClient, EmojiService, GuildLocaleService, NoticeCache, NoticeChzzkStreamChannel, NotifierMetrics (+10 more)

### Community 47 - "YoutubeMemberService"
Cohesion: 0.10
Nodes (24): SocketRole, SocketTextChannel, CancellationToken, SocketGuild, Task, YoutubeMemberNotMemberApplyResult, YoutubeMemberService, CancellationToken (+16 more)

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
Cohesion: 0.18
Nodes (15): DefaultMemberPermissions, IRole, SlashCommand, Task, TwitchSubscriptionSetting, CommandExample, CommandSummary, DefaultMemberPermissions (+7 more)

### Community 53 - "NoticeCache"
Cohesion: 0.24
Nodes (10): DateTimeOffset, Func, List, TimeProvider, TimeSpan, NoticeCache, Fact, InvalidOperationException (+2 more)

### Community 54 - "YoutubeVideoClaimCache"
Cohesion: 0.16
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
Cohesion: 0.16
Nodes (13): Action, Dictionary, Lease, Task, TaskCompletionSource, Lease, TwitchRefreshRotationLifecycle, ActiveOperationCount (+5 more)

### Community 59 - "CHZZK 錄影整合計畫"
Cohesion: 0.10
Nodes (20): Bot, Bot 實作, Bot 自動化測試, CHZZK 錄影整合計畫, Redis 契約建議, StreamRecordTools, StreamRecordTools 實作, 交付與部署順序 (+12 more)

### Community 60 - "DiscordStreamNotifyBot.Shared"
Cohesion: 0.08
Nodes (12): DiscordStreamNotifyBot.Tests.Component.Redis, DiscordStreamNotifyBot.Scraper, DiscordStreamNotifyBot.Shared, DiscordStreamNotifyBot.Command.YoutubeMember, DiscordStreamNotifyBot.Interaction.OwnerOnly.Service, DiscordStreamNotifyBot, Program, BotRole (+4 more)

### Community 61 - "YoutubeStreamService"
Cohesion: 0.08
Nodes (22): NowStreamingHost, IInteractionService, BotLocalizer, CommandDisplayResolver, DiscordSocketClient, Embed, EmojiService, GuildLocaleService (+14 more)

### Community 62 - "graphify reference: extra exports and benchmark"
Cohesion: 0.22
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 63 - "Bot"
Cohesion: 0.07
Nodes (28): BotPlayingStatus, ConnectionMultiplexer, DiscordSocketClient, IDatabase, ISubscriber, IUser, Task, Timer (+20 more)

### Community 64 - "DiscordStreamNotifyBot.DataBase.Table"
Cohesion: 0.08
Nodes (16): DiscordStreamNotifyBot.Tests.Component.MySql, DiscordStreamNotifyBot.DataBase.Table, DiscordStreamNotifyBot.Interaction.TwitchSubscription, DiscordStreamNotifyBot.SharedService.Member, DiscordStreamNotifyBot.SharedService.TwitchSubscription, DateTime, GoogleOAuthUnlinkIntent, DateAdded (+8 more)

### Community 65 - ".OtherScheduleAsync"
Cohesion: 0.22
Nodes (6): GeneratedRegex, HttpRequestException, Batch, Regex, WebException, TaskCanceledException

### Community 66 - ".FilterNoNotifyGuilds"
Cohesion: 0.24
Nodes (9): IEnumerable, GuildSnapshot, Id, MemberCount, Name, OwnerId, ArgumentNullException, Fact (+1 more)

### Community 67 - "Program"
Cohesion: 0.24
Nodes (7): AssemblyInformationalVersionAttribute, Assembly, CancellationToken, Exception, PeriodicTimer, Task, Program

### Community 68 - "AddManualMemberCheckVideoFlag"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, AddManualMemberCheckVideoFlag

### Community 69 - ".VideoLookupSearchesAllFourTables"
Cohesion: 0.14
Nodes (11): DbUpdateConcurrencyException, HoloVideos, NijisanjiVideos, NonApprovedVideos, OtherVideos, Video, DateTime, MySqlComponentFact (+3 more)

### Community 70 - "NotificationContractTests"
Cohesion: 0.28
Nodes (5): DateTime, Fact, JObject, YTChannelType, NotificationContractTests

### Community 71 - "EF Core 遷移與基線化（本專案版）"
Cohesion: 0.25
Nodes (7): EF Core 遷移與基線化（本專案版）, 一次性基線化（舊的 EnsureCreated 正式庫）, 一般變更流程, 你必須先知道的三件專案特例, 啟動時不碰資料庫（重要）, 套用：本地/開發 vs 正式環境, 收尾

### Community 72 - "AdminSettingsSnapshot"
Cohesion: 0.11
Nodes (20): List, AdminSettingsCommon, GlobalNoticeChannelId, Locale, VerificationLogChannelId, AdminSettingsHealth, BotConnected, AdminSettingsSnapshot (+12 more)

### Community 73 - "11. 通知與背景訊息"
Cohesion: 0.29
Nodes (7): 11.1 現況限制, 11.2 目標作法, 11.3 YouTube, 11.4 Twitch, 11.5 TwitCasting, 11.6 YouTube 會限驗證, 11. 通知與背景訊息

### Community 74 - "GoogleOAuthOperationLockLease"
Cohesion: 0.09
Nodes (25): CancellationToken, CancellationTokenSource, Exception, IConnectionMultiplexer, IDatabase, RedisKey, RedisValue, Task (+17 more)

### Community 75 - "TwitchStreamLifecycleDecisionTests"
Cohesion: 0.08
Nodes (26): ConcurrentDictionary, TwitchOfflineAction, ClearState, Defer, Ignore, PublishEnd, ResumeStream, TwitchOfflineFacts (+18 more)

### Community 78 - "NotificationBusConsumer"
Cohesion: 0.19
Nodes (12): CancellationToken, ChzzkService, Func, IDatabase, RedisValue, StreamEntry, Task, TwitcastingService (+4 more)

### Community 80 - "YoutubeStream"
Cohesion: 0.16
Nodes (22): Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task, YoutubeStream (+14 more)

### Community 81 - "Broadcaster"
Cohesion: 0.11
Nodes (17): Broadcaster, Created, Id, Image, IsLive, LastMovieId, Level, Name (+9 more)

### Community 82 - ".BuildSnapshotAsync"
Cohesion: 0.10
Nodes (17): CancellationToken, DiscordSocketClient, GuildLocaleService, JObject, SocketGuild, Task, AdminSettingsService, RequestRoute (+9 more)

### Community 83 - "AdminSettingsChannel"
Cohesion: 0.14
Nodes (14): AdminSettingsChannel, CanEmbedLinks, CanManageEvents, CanSendMessages, CanView, Id, Name, Type (+6 more)

### Community 84 - "YoutubeDetectionService"
Cohesion: 0.10
Nodes (23): IsDeleted, ConcurrentDictionary, HttpClient, IHttpClientFactory, TimeSpan, YoutubeApiService, YouTubeService, IsRecord (+15 more)

### Community 85 - "graphify reference: query, path, explain"
Cohesion: 0.33
Nodes (5): For /graphify explain, For /graphify path, graphify reference: query, path, explain, Step 0 — Constrained query expansion (REQUIRED before traversal), Step 1 — Traversal

### Community 86 - "自動化測試導入計畫"
Cohesion: 0.17
Nodes (12): 10. 測試實作規則, 1. 目標, 2. 測試分類, 3. 不移除的啟動檢查, 4. 第一批：低耦合契約與格式化, 5. 第二批：小幅抽出純邏輯, 6. 第三批：時間與快取, 7. 第四批：Scraper 狀態機 (+4 more)

### Community 87 - "Task"
Cohesion: 0.10
Nodes (17): DateTime, IEnumerable, TableVideo, Task, YTApiVideo, BannerChangeNotification, ChannelId, VideoId (+9 more)

### Community 88 - ".AutoRecordAsync"
Cohesion: 0.27
Nodes (10): Alias, Command, CommandExample, RequireContext, RequireOwner, Summary, Task, ChzzkStream (+2 more)

### Community 89 - ".Get"
Cohesion: 0.08
Nodes (22): MissingManifestResourceException, CommandExample, CommandSummary, DefaultMemberPermissions, RequireGuildMemberCount, SlashCommand, Task, TwitcastingService (+14 more)

### Community 90 - ".GetDbContext"
Cohesion: 0.12
Nodes (13): ComponentInteraction, SocketMessageComponent, Task, SpiderManagementComponent, CancellationToken, EmbedBuilder, SocketGuild, SocketGuildUser (+5 more)

### Community 91 - ".CreateAsyncClient"
Cohesion: 0.21
Nodes (15): HttpMessageHandler, IHttpClientFactory, CancellationToken, Fact, Func, HttpClient, HttpRequestMessage, HttpResponseMessage (+7 more)

### Community 92 - "EmbedBuilderFactory"
Cohesion: 0.28
Nodes (7): BotLocalizer, DateTime, EmbedBuilder, TimeSpan, YTApiVideo, EmbedBuilderFactory, YoutubeNotificationVariant

### Community 93 - "YoutubeNotification"
Cohesion: 0.14
Nodes (14): YTChannelType, YoutubeNotification, ActualEndTime, ActualStartTime, ChannelId, ChannelTitle, ChannelType, IsMemberOnly (+6 more)

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

### Community 103 - "ChzzkClient"
Cohesion: 0.11
Nodes (19): HttpStatus, IsNotFound, IsSuccess, RetryAfter, CancellationToken, HttpClient, HttpResponseMessage, Task (+11 more)

### Community 105 - "DiscordWebhookClient"
Cohesion: 0.21
Nodes (9): CancellationToken, DiscordSocketClient, HttpClient, Task, DiscordWebhookClient, Message, avatar_url, content (+1 more)

### Community 106 - "DiscordStreamNotifyBot.DataBase"
Cohesion: 0.11
Nodes (12): DiscordStreamNotifyBot.HttpClients, DiscordStreamNotifyBot.Interaction.Chzzk, DiscordStreamNotifyBot.Interaction.Utility, DiscordStreamNotifyBot.Interaction.Attribute, DiscordStreamNotifyBot.Interaction.TwitCasting, DiscordStreamNotifyBot.Command.TwitCasting, DiscordStreamNotifyBot.Command.Admin, DiscordStreamNotifyBot.Interaction.Help.Service (+4 more)

### Community 113 - ".CreateOrRepairConfigurationAsync"
Cohesion: 0.17
Nodes (17): IReadOnlyCollection, MemberRoleOwnershipSnapshot, Entitlements, TwitchRoleReferences, YoutubeRoleReferences, CancellationToken, DiscordSocketClient, Exception (+9 more)

### Community 114 - "AdminSettingsCrawlerPlatform"
Cohesion: 0.13
Nodes (16): Name, IEnumerable, AdminSettingsCrawlerItem, SourceId, SourceName, AdminSettingsCrawlerPlatform, Count, Enabled (+8 more)

### Community 115 - "DiscordStreamNotifyBot.Notifier.csproj"
Cohesion: 0.10
Nodes (19): Microsoft.Extensions.DependencyInjection.Abstractions (10.0.1), System.Management (10.0.1), net8.0, Ben.Demystifier (0.4.1), Discord.Net (3.20.1), Dorssel.Utilities.Debounce (3.0.0), EFCore.NamingConventions (9.0.0), Google.Apis.YouTube.v3 (1.73.0.3981) (+11 more)

### Community 116 - "TwitchSpider"
Cohesion: 0.35
Nodes (7): CommandExample, CommandSummary, DefaultMemberPermissions, SlashCommand, Task, TwitchService, TwitchSpider

### Community 117 - "Normal"
Cohesion: 0.26
Nodes (8): DiscordStreamNotifyBot.Command.Normal, Alias, Command, DiscordSocketClient, DiscordWebhookClient, Summary, Task, Normal

### Community 118 - "ClusterService"
Cohesion: 0.21
Nodes (8): IDatabase, Task, TimeSpan, ClusterService, RedisComponentFact, Task, ClusterServiceRedisComponentTests, IDatabase

### Community 119 - "TwitchService"
Cohesion: 0.06
Nodes (34): BotLocalizer, CancellationToken, Clip, DateTime, DiscordSocketClient, Embed, EmojiService, EventSubSubscription (+26 more)

### Community 120 - "Twitch OAuth 與零成本 EventSub 實作計畫"
Cohesion: 0.14
Nodes (13): 0. 涉及專案, 10. Backend EventSub Webhook, 12. Frontend, 14. Grafana, 18. 建置與遷移, 19. 部署順序, 1. 不可偏離的決策, 20. 官方參考 (+5 more)

### Community 121 - "TwitchEventSubEnsureResult"
Cohesion: 0.08
Nodes (27): EventSubSubscription, IReadOnlyList, Stream, TwitchEventSubDeleteResult, DeletedSubscriptionIds, Status, TwitchEventSubDeleteStatus, ApiFailure (+19 more)

### Community 122 - "YoutubeMemberProbeResultKind"
Cohesion: 0.10
Nodes (23): GoogleApiException, YouTubeService, CancellationToken, GoogleCredential, HashSet, IEnumerable, Task, YoutubeMemberApiClient (+15 more)

### Community 123 - "AutocompleteCandidate"
Cohesion: 0.21
Nodes (8): IEnumerable, IReadOnlyList, AutocompleteCandidate, Name, SearchTerms, AutocompleteSearch, Fact, AutocompleteSearchTests

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
Cohesion: 0.09
Nodes (25): ChzzkNotificationVariant, BotLocalizer, EmbedBuilder, TimeSpan, ChzzkEmbedBuilderFactory, Embed, MessageComponent, ChzzkNotificationVariant (+17 more)

### Community 131 - "SendMsgToAllGuildService"
Cohesion: 0.08
Nodes (25): ButtonCheckData, IInteractionService, SendAllPayload, ChoiceDisplayAttribute, DiscordSocketClient, Embed, HttpException, Task (+17 more)

### Community 132 - "7. OAuth API 與流程隔離"
Cohesion: 0.40
Nodes (5): 7.1 API, 7.2 State, 7.3 Callback, 7.4 Twitch scopes, 7. OAuth API 與流程隔離

### Community 133 - "TwitcastingLiveStartPlannerTests"
Cohesion: 0.13
Nodes (17): Category, DateTime, IEnumerable, TwitcastingLiveStartAction, IgnoreDuplicate, PersistAndNotify, PersistRequestRecordingAndNotify, TwitcastingLiveStartFacts (+9 more)

### Community 134 - "ClusterQueryService"
Cohesion: 0.08
Nodes (33): ChannelInfo, ClusterQueryType, ConcurrentBag, NotificationChannelIssue, QueryRequest, Replies, Responses, DiscordSocketClient (+25 more)

### Community 135 - ".Plan"
Cohesion: 0.19
Nodes (12): DiscordStreamNotifyBot.Scraper.Detection.Twitcasting, HashSet, IEnumerable, IReadOnlyList, TwitcastingWebhookAction, TwitcastingWebhookActionKind, RegisterLiveStart, RemoveLiveStart (+4 more)

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

### Community 143 - "CommandDisplayResolver"
Cohesion: 0.08
Nodes (30): DiscordStreamNotifyBot.Interaction.Help, RequireBotPermissionAttribute, RequireUserPermissionAttribute, AutocompletionResult, HelpService, IAutocompleteInteraction, IInteractionContext, InteractionService (+22 more)

### Community 144 - "13. Prometheus"
Cohesion: 0.67
Nodes (3): 13.1 Backend 指標, 13.2 Scraper 指標, 13. Prometheus

### Community 145 - "4. 安全刪除狀態機"
Cohesion: 0.67
Nodes (3): 4.1 直播中授權失效, 4.2 關台後重新判斷, 4. 安全刪除狀態機

### Community 146 - "TwitchNotification"
Cohesion: 0.09
Nodes (23): List, NotifyType, TwitchClipInfo, CreatorName, Title, Url, ViewCount, TwitchNoticeType (+15 more)

### Community 147 - ".Plan"
Cohesion: 0.18
Nodes (14): TwitchEventSubEnsureMode, Fallback, PermanentOAuth, HashSet, IReadOnlyCollection, IReadOnlyList, TwitchEventSubCreateSpec, TwitchEventSubFact (+6 more)

### Community 148 - "MainDbContextModelSnapshot.cs"
Cohesion: 0.33
Nodes (4): ModelSnapshot, DateTime, ModelBuilder, MainDbContextModelSnapshot

### Community 149 - "DiscordStreamNotifyBot.Tests"
Cohesion: 0.08
Nodes (9): DiscordStreamNotifyBot.Scraper.Detection.Youtube, DiscordStreamNotifyBot.Auth, DiscordStreamNotifyBot.Tests, DiscordStreamNotifyBot.SharedService.Twitch, Fact, NotificationBusConsumerOptionsTests, InlineData, Theory (+1 more)

### Community 150 - ".StartAndBlockAsync"
Cohesion: 0.08
Nodes (25): AdminSettingsService, BotLocalizer, ChzzkClient, ChzzkRecordService, ChzzkService, CommandDisplayResolver, EmojiService, GuildLocaleService (+17 more)

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

### Community 156 - ".LoadSnapshotAsync"
Cohesion: 0.16
Nodes (11): CancellationToken, ICollection, IEnumerable, Task, MemberEntitlementProvider, Twitch, Youtube, MemberRoleEntitlement (+3 more)

### Community 157 - ".CreateService"
Cohesion: 0.32
Nodes (6): OperationCanceledException, CollectorRegistry, DiscordSocketClient, MySqlComponentFact, Task, YoutubeMemberCleanupPersistenceTests

### Community 158 - "DbEntity"
Cohesion: 0.09
Nodes (18): BannerChange, ChannelId, GuildId, LastChangeStreamId, DateTime, DbEntity, DateAdded, Id (+10 more)

### Community 159 - "NonPersistentGoogleDataStore"
Cohesion: 0.21
Nodes (5): IDataStore, Task, ITokenDataStore, Task, NonPersistentGoogleDataStore

### Community 160 - "Movie"
Cohesion: 0.09
Nodes (22): Movie, Category, CommentCount, Country, Created, CurrentViewCount, Duration, HlsUrl (+14 more)

### Community 161 - "BotState"
Cohesion: 0.09
Nodes (18): DiscordStreamNotifyBot.SharedService.Youtube.Json, ConnectionMultiplexer, IDatabase, ISubscriber, IUser, BotState, ApplicatonOwner, DbService (+10 more)

### Community 162 - "AddTwitchSubscriptionVerification"
Cohesion: 0.22
Nodes (5): DateTime, MigrationBuilder, DateTime, ModelBuilder, AddTwitchSubscriptionVerification

### Community 163 - "AddTwitchSubscriptionDeletionPending"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, AddTwitchSubscriptionDeletionPending

### Community 164 - "RedisComponentFixture"
Cohesion: 0.12
Nodes (15): ConfigurationOptions, FactAttribute, ICollectionFixture, MySqlComponentFactAttribute, MySqlComponentCollection, ConnectionMultiplexer, RedisKey, Task (+7 more)

### Community 165 - "AddYoutubeMemberVerificationDurability"
Cohesion: 0.25
Nodes (4): MigrationBuilder, DateTime, ModelBuilder, AddYoutubeMemberVerificationDurability

### Community 166 - ".CheckRequirementsAsync"
Cohesion: 0.15
Nodes (9): ICommandInfo, IInteractionContext, IServiceProvider, PreconditionResult, Task, RequireGuildMemberCountAttribute, ErrorMessage, GuildMemberCount (+1 more)

### Community 167 - ".NotifyAddedAsync"
Cohesion: 0.18
Nodes (11): Components, Embed, MessageComponent, SocketGuild, Task, CrawlerOwnerNotifier, CrawlerPlatform, Chzzk (+3 more)

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
Cohesion: 0.12
Nodes (11): GuildYoutubeMemberConfig, DeletionPending, GuildId, IsManualVideoId, MemberCheckChannelId, MemberCheckChannelTitle, MemberCheckGrantRoleId, MemberCheckVideoId (+3 more)

### Community 172 - "BotStateTests"
Cohesion: 0.38
Nodes (3): InlineData, Theory, BotStateTests

### Community 173 - ".SetMemberCheckVideoIdAsync"
Cohesion: 0.35
Nodes (9): Alias, Command, RequireContext, RequireOwner, Summary, Task, YoutubeMemberService, YoutubeStreamService (+1 more)

### Community 175 - ".TryGetKey"
Cohesion: 0.25
Nodes (5): NotificationDedupPolicy, Fact, InlineData, Theory, NotificationDedupPolicyTests

### Community 176 - "GuildTwitchSubscriptionConfig"
Cohesion: 0.07
Nodes (23): IQueryable, DateTimeOffset, IEnumerable, IReadOnlyCollection, TwitchAuthorizationEventPolicy, TwitchRateLimitPolicy, TwitchSubscriptionConfigurationPolicy, TwitchSubscriptionConfigurationQueries (+15 more)

### Community 177 - "NotificationMetricEvent"
Cohesion: 0.12
Nodes (16): ArgumentOutOfRangeException, NotificationMetricEvent, ChzzkEnd, ChzzkStart, TwitcastingStart, TwitchChangeData, TwitchEnd, TwitchStart (+8 more)

### Community 178 - "Twitch"
Cohesion: 0.11
Nodes (24): DiscordStreamNotifyBot.Command.Help, ICommandService, Alias, Command, CommandInfo, CommandService, IServiceProvider, Summary (+16 more)

### Community 179 - ".Classify"
Cohesion: 0.14
Nodes (16): DateTime, YoutubeApiVideoAction, ActiveChatOnly, Ignore, IgnoreFakePost, NewVideo, Scheduled, Started (+8 more)

### Community 180 - ".CheckMemberShipOnlyVideoIdAsync"
Cohesion: 0.14
Nodes (15): Task, YoutubeMemberCandidateAction, AbortDiscovery, IgnoreCommentsDisabled, IgnorePublicVideo, IgnoreUnavailable, SelectMemberOnlyVideo, YoutubeMemberCandidateFacts (+7 more)

### Community 181 - "TwitchStateDecisions.cs"
Cohesion: 0.10
Nodes (24): DateTime, IEnumerable, List, TimeSpan, TwitchChannelEventFacts, TwitchChannelStateFacts, TwitchChannelUpdateAction, Ignore (+16 more)

### Community 182 - "YoutubeMemberVideoLogNotification"
Cohesion: 0.20
Nodes (12): YoutubeMemberVideoLogNotification, BotOwnerMessage, CheckChannelId, IsNeedRemove, IsNeedSendToOwner, MessageArguments, MessageCode, ArgumentException (+4 more)

### Community 183 - "ReactionEventWrapper"
Cohesion: 0.29
Nodes (8): Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task, ReactionEventWrapper, Message

### Community 184 - "YoutubePubSubNotification"
Cohesion: 0.15
Nodes (12): YoutubePubSubNotification, ChannelId, Link, NotificationType, Published, Title, Updated, VideoId (+4 more)

### Community 185 - "MigrationAndConstraintTests"
Cohesion: 0.29
Nodes (5): DbUpdateException, MySqlComponentFact, Task, TwitchBroadcasterAuthorization, MigrationAndConstraintTests

### Community 186 - "DiscordStreamNotifyBot.Command"
Cohesion: 0.16
Nodes (9): DiscordStreamNotifyBot.Command, SocketCommandContext, SocketMessage, CommandService, DiscordSocketClient, IServiceProvider, Task, CommandHandler (+1 more)

### Community 187 - "TwitchOAuthRefreshLock"
Cohesion: 0.24
Nodes (8): IConnectionMultiplexer, IDatabase, TimeSpan, TwitchOAuthRefreshLock, DatabaseNumber, RedisComponentFact, Task, TwitchOAuthRefreshLockRedisComponentTests

### Community 188 - "CommonEqualityComparer"
Cohesion: 0.18
Nodes (5): IEqualityComparer, Func, CommonEqualityComparer, Func, CommonEqualityComparer

### Community 189 - ".Start"
Cohesion: 0.14
Nodes (14): ChzzkDetectionService, ServiceProvider, TwitchApiService, YoutubeApiService, DetectionHost, Task, CancellationToken, PeriodicTimer (+6 more)

### Community 190 - "StubHandler"
Cohesion: 0.21
Nodes (14): StubHandler, CancellationToken, Fact, Func, HttpRequestMessage, HttpResponseMessage, HttpStatusCode, InlineData (+6 more)

### Community 191 - "TwitchReconcileDecisionTests"
Cohesion: 0.09
Nodes (24): ConcurrentDictionary, DateTime, Task, TimeProvider, TimeSpan, TwitchGuildEligibilityEvaluator, TwitchGuildEligibilityStatus, Eligible (+16 more)

### Community 192 - "Category"
Cohesion: 0.21
Nodes (11): List, CategoriesJson, Categories, Category, Id, Name, SubCategories, SubCategory (+3 more)

### Community 193 - ".DecideAutomaticMutation"
Cohesion: 0.24
Nodes (6): YoutubeMemberAutomaticMutationAction, Apply, PreserveManualPin, YoutubeMemberManualPinPolicy, Fact, YoutubeMemberManualPinPolicyTests

### Community 194 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, ConfiguredBroadcasterAutocompleteHandler

### Community 195 - "SharedExtensions"
Cohesion: 0.21
Nodes (4): DateTime, EmbedBuilder, YTChannelType, SharedExtensions

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
Cohesion: 0.15
Nodes (12): DateTime, TimeSpan, ChzzkPollFacts, ChzzkPollPolicy, ChzzkStreamStatus, Closed, PendingClose, Superseded (+4 more)

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
Nodes (22): DbContextOptions, TwitCasting, ComponentInteraction, GuildTwitchSubscriptionConfig, RequireContext, SlashCommand, Task, TwitchSubscription (+14 more)

### Community 206 - ".SendErrorMessageAsync"
Cohesion: 0.31
Nodes (9): IDMChannel, KeyNotFoundException, BotLocalizer, DiscordSocketClient, HttpException, ITextChannel, IUserMessage, TimeoutException (+1 more)

### Community 207 - "TwitchSpiderRemovalAction"
Cohesion: 0.22
Nodes (9): TwitchSpiderRemovalAction, AlreadyRemoved, DeferApiFailure, DeferLive, DeferNotifier, DeferSnapshot, EvaluateEligibility, Remove (+1 more)

### Community 208 - ".CreateAsync"
Cohesion: 0.07
Nodes (31): ILocalizationManager, IServiceProvider, IServiceScope, IServiceScopeFactory, ResxLocalizationManager, IDictionary, IList, LocalizationTarget (+23 more)

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
Cohesion: 0.20
Nodes (7): Task, ISubscriber, Task, ChzzkRecordBus, ChzzkRecordRequest, ChannelId, StreamKey

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
Cohesion: 0.13
Nodes (11): Histogram, Counter, Gauge, TimeSpan, TwitchSubscriptionStatus, NotifierMetrics, HashSet, CollectorRegistry (+3 more)

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
Cohesion: 0.24
Nodes (9): CancellationToken, DiscordSocketClient, IServiceProvider, SocketGuild, Task, UtilityService, DiscordSocketClient, SocketGuild (+1 more)

### Community 223 - "TwitchValidateTokenData"
Cohesion: 0.20
Nodes (9): TwitchTokenErrorData, Error, Message, TwitchValidateTokenData, ClientId, ExpiresIn, Login, Scopes (+1 more)

### Community 224 - "5. 語系模型與解析規則"
Cohesion: 0.33
Nodes (6): 5.1 支援值, 5.2 公開內容與背景通知, 5.3 私人即時回覆, 5.4 延遲會限驗證 DM, 5.5 併發安全, 5. 語系模型與解析規則

### Community 225 - "DiscordStreamNotifyBot.Shared.Messages"
Cohesion: 0.11
Nodes (9): DiscordStreamNotifyBot.SharedService.AdminSettings, DiscordStreamNotifyBot.SharedService.Chzzk, DiscordStreamNotifyBot.HttpClients.Chzzk.Model, DiscordStreamNotifyBot.Scraper.Detection.Chzzk, DiscordStreamNotifyBot.Scraper.Detection.Twitch.Debounce, DiscordStreamNotifyBot.HttpClients.Chzzk, DiscordStreamNotifyBot.Scraper.Detection.Twitch, DiscordStreamNotifyBot.Shared.Messages (+1 more)

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
Cohesion: 0.24
Nodes (5): Assembly, IEnumerable, IServiceCollection, Type, YTChannelType

### Community 231 - "Broadcaster"
Cohesion: 0.17
Nodes (12): Broadcaster, Created, Id, Image, IsLive, LastMovieId, Level, Name (+4 more)

### Community 232 - "TwitchBroadcasterAuthorization"
Cohesion: 0.12
Nodes (16): DateTime, TwitchBroadcasterAuthorization, AuthorizedAt, ClientId, DateUpdated, DiscordUserId, DisplayName, EncryptedAccessToken (+8 more)

### Community 233 - "AdminTwitchMessagesPayload"
Cohesion: 0.25
Nodes (8): AdminTwitchMessagesPayload, Change, End, Start, AdminTwitchUpsertPayload, ChannelId, Messages, Source

### Community 234 - "opencode.json"
Cohesion: 0.50
Nodes (3): plugin, $schema, .opencode/plugins/graphify.js

### Community 235 - "YouTube 會員驗證"
Cohesion: 0.33
Nodes (5): Durable state, YouTube 會員驗證, 使用者契約, 服務邊界, 部署前驗證

### Community 236 - "GuildSnapshotEnvelope"
Cohesion: 0.25
Nodes (7): DateTime, List, GuildSnapshotEnvelope, Guilds, IsConnected, ShardId, UpdatedAtUtc

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
Cohesion: 0.22
Nodes (9): TwitchAccessTokenData, AccessToken, ExpiresIn, RefreshToken, Scopes, TokenType, TwitchUserId, Fact (+1 more)

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

### Community 246 - "MemberOperationCoordinator"
Cohesion: 0.17
Nodes (14): IAsyncDisposable, IDisposable, LeaseGroup, CancellationToken, ConcurrentDictionary, IEnumerable, Lease, List (+6 more)

### Community 247 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildNoticeTwitCastingChannelIdAutocompleteHandler

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

### Community 252 - "NoticeType"
Cohesion: 0.29
Nodes (7): NoticeType, ChangeTime, Delete, End, NewStream, NewVideo, Start

### Community 253 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildNoticeTwitchChannelIdAutocompleteHandler

### Community 254 - "AdminSettingsNotifications"
Cohesion: 0.12
Nodes (17): AdminSettingsChzzkNotification, ChannelId, DetectionEnabled, Messages, SourceId, SourceName, AdminSettingsNotifications, Chzzk (+9 more)

### Community 255 - ".MissingOrShortKeyIsRejected"
Cohesion: 0.25
Nodes (5): Fact, InlineData, InvalidOperationException, Theory, ProviderTokenEncryptionKeyTests

### Community 256 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildTwitchSpiderAutocompleteHandler

### Community 257 - "AutocompleteHandler"
Cohesion: 0.12
Nodes (15): AutocompleteHandler, GuildNoticeChzzkChannelIdAutocompleteHandler, AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildChzzkSpiderAutocompleteHandler (+7 more)

### Community 258 - "YoutubeMemberRoleApplyResult"
Cohesion: 0.29
Nodes (6): YoutubeMemberRoleApplyResult, Applied, Failed, UnknownMember, InlineData, Theory

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

### Community 267 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildNoticeYoutubeChannelIdAutocompleteHandler

### Community 268 - "TwitchSpider"
Cohesion: 0.18
Nodes (10): DateTime, TwitchSpider, DateAdded, GuildId, IsRecord, IsWarningUser, OfflineImageUrl, ProfileImageUrl (+2 more)

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

### Community 274 - "DiscordStreamNotifyBot.Command.Attribute"
Cohesion: 0.12
Nodes (11): Attribute, DiscordStreamNotifyBot.Command.Youtube, DiscordStreamNotifyBot.Command.Attribute, DiscordStreamNotifyBot.Command.Chzzk, DiscordStreamNotifyBot.Command.Twitch, CommandExampleAttribute, ExpArray, CommandExampleAttribute (+3 more)

### Community 275 - "GracefulShutdown"
Cohesion: 0.33
Nodes (4): CancellationToken, CancellationTokenSource, GracefulShutdown, Token

### Community 276 - ".GenerateSuggestionsAsync"
Cohesion: 0.29
Nodes (6): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider, GuildYoutubeMemberCheckChannelIdAutocompleteHandler

### Community 277 - ".AddChannel"
Cohesion: 0.25
Nodes (10): CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, IChannel, NoticeType, RequireBotPermission, SlashCommand (+2 more)

### Community 278 - ".GetCommandPath"
Cohesion: 0.26
Nodes (10): CommandExample, CommandSummary, DefaultMemberPermissions, DiscordSocketClient, IChannel, RequireBotPermission, SlashCommand, Task (+2 more)

### Community 279 - "YoutubeChannelSpider"
Cohesion: 0.22
Nodes (8): DateTime, YoutubeChannelSpider, ChannelId, ChannelTitle, DateAdded, GuildId, IsTrustedChannel, LastSubscribeTime

### Community 280 - "MySqlComponentFixture"
Cohesion: 0.46
Nodes (4): IAsyncLifetime, Task, MySqlComponentFixture, DbService

### Community 281 - "PreconditionAttribute"
Cohesion: 0.25
Nodes (6): PreconditionAttribute, RequireGuildMemberCountAttribute, ErrorMessage, GuildMemberCount, RequireGuildAttribute, GuildId

### Community 282 - "AdminChzzkMessagesPayload"
Cohesion: 0.29
Nodes (7): AdminChzzkMessagesPayload, End, Start, AdminChzzkUpsertPayload, ChannelId, Messages, Source

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
Cohesion: 0.08
Nodes (28): ChzzkSpider, ChzzkStream, ConcurrentDictionary, DateTime, Func, ScraperMetrics, Task, TimeSpan (+20 more)

### Community 288 - "AdminSettingsYoutubeMessages"
Cohesion: 0.29
Nodes (7): AdminSettingsYoutubeMessages, ChangeTime, Delete, End, NewStream, NewVideo, Start

### Community 289 - "Stub"
Cohesion: 0.43
Nodes (5): DispatchProxy, MethodInfo, Stub, Func, Stub

### Community 290 - "AdministrationComponent"
Cohesion: 0.40
Nodes (4): ComponentInteraction, NotificationChannelCheckResponse, Task, AdministrationComponent

### Community 291 - "ReactionEventWrapper"
Cohesion: 0.26
Nodes (8): Cacheable, DiscordSocketClient, IMessageChannel, IUserMessage, SocketReaction, Task, ReactionEventWrapper, Message

### Community 292 - "YoutubeNoticeType"
Cohesion: 0.29
Nodes (7): YoutubeNoticeType, ChangeTime, Delete, End, NewStream, NewVideo, Start

### Community 293 - ".GenerateSuggestionsAsync"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 294 - "GuildInfoResponse"
Cohesion: 0.29
Nodes (7): Dictionary, GuildInfoResponse, Channels, MemberCount, Name, OwnerId, ShardId

### Community 295 - "TwitchSubscriptionRolePolicyTests"
Cohesion: 0.14
Nodes (11): AddRoleIds, IReadOnlySet, RemoveRoleIds, Func, IReadOnlyList, TwitchSubscriptionRolePolicy, Fact, GuildTwitchSubscriptionConfig (+3 more)

### Community 296 - "TwitchRefreshPersistenceDecision"
Cohesion: 0.29
Nodes (5): TwitchRefreshPersistenceDecision, AlreadyPersisted, Stale, WriteReplacement, TwitchRefreshPersistencePolicy

### Community 297 - ".GenerateSuggestionsAsync"
Cohesion: 0.33
Nodes (5): AutocompletionResult, IAutocompleteInteraction, IInteractionContext, IParameterInfo, IServiceProvider

### Community 298 - "TwitchProviderResultStatus"
Cohesion: 0.40
Nodes (5): TwitchProviderResultStatus, Failure, Invalid, Success, TemporaryFailure

### Community 299 - "TwitcastingNotification"
Cohesion: 0.11
Nodes (19): Category, List, RedisValue, SemaphoreSlim, Task, TwitcastingDetectionService, IsEnable, DateTime (+11 more)

### Community 300 - "AdminSettingsTwitcastingNotification"
Cohesion: 0.33
Nodes (6): AdminSettingsTwitcastingNotification, ChannelId, DetectionEnabled, SourceId, SourceName, StartMessage

### Community 301 - "MainDbContextFactory"
Cohesion: 0.40
Nodes (3): IDesignTimeDbContextFactory, Version, MainDbContextFactory

### Community 302 - ".ToLabel"
Cohesion: 0.05
Nodes (45): NotificationBusMetricResult, Deduplicated, Dispatched, DispatchFailed, InvalidPayload, TwitchSubscriptionProviderError, InvalidResponse, NetworkFailure (+37 more)

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
Cohesion: 0.13
Nodes (19): IOException, Func, ChzzkPollAction, BaselineOffline, CancelPendingClose, ConfirmClose, Ignore, RefreshObserved (+11 more)

### Community 310 - "ChzzkSpider"
Cohesion: 0.18
Nodes (10): DateTime, ChzzkSpider, ChannelId, ChannelImageUrl, ChannelName, CurrentStreamKey, DateAdded, GuildId (+2 more)

### Community 311 - "AddChzzkNotification"
Cohesion: 0.22
Nodes (5): DateTime, MigrationBuilder, DateTime, ModelBuilder, AddChzzkNotification

### Community 312 - ".SendMessageToAllGuildAsync"
Cohesion: 0.22
Nodes (7): DiscordStreamNotifyBot.Interaction.OwnerOnly, SendMsgToAllGuildService, DefaultMemberPermissions, RequireOwner, SlashCommand, Task, SendMsgToAllGuild

### Community 313 - "ClusterQueryType"
Cohesion: 0.40
Nodes (5): ClusterQueryType, GetInviteUrl, GuildInfo, NotificationChannelCheck, UserInfo

### Community 314 - "NoticeTwitcastingStreamChannel"
Cohesion: 0.33
Nodes (5): NoticeTwitcastingStreamChannel, DiscordChannelId, GuildId, ScreenId, StartStreamMessage

### Community 315 - "YoutubeMemberSingleConfigurationQueueAction"
Cohesion: 0.40
Nodes (5): YoutubeMemberSingleConfigurationQueueAction, Add, PreserveQueued, PreserveVerified, RequeuePendingRoleRemoval

### Community 316 - "Video"
Cohesion: 0.12
Nodes (13): DateTime, Video, ChannelId, ChannelTitle, ChannelType, IsPrivate, ScheduledStartTime, VideoTitle (+5 more)

### Community 317 - ".SendStreamMessageAsync"
Cohesion: 0.11
Nodes (15): Event, Platform, HttpException, HttpException, NotificationDeliveryProgress, TimeoutException, Embed, HttpException (+7 more)

### Community 318 - ".SaveVideosByType"
Cohesion: 0.40
Nodes (3): DbSet, MainDbContext, YTChannelType

### Community 319 - "TopLevelModule"
Cohesion: 0.32
Nodes (5): ModuleBase, EmbedBuilder, Task, TopLevelModule, _service

### Community 320 - ".ChzzkAddedMessageShowsAutoRecordStateAndExplicitTargetButtons"
Cohesion: 0.40
Nodes (3): ButtonComponent, Fact, CrawlerOwnerNotifierTests

### Community 321 - "TwitchReconcileRequestedPayload"
Cohesion: 0.67
Nodes (3): TwitchReconcileRequestedPayload, Reason, TwitchUserId

### Community 323 - "TwitchStreamEventPayload"
Cohesion: 0.50
Nodes (4): TwitchStreamEventPayload, BroadcasterUserId, BroadcasterUserLogin, BroadcasterUserName

### Community 325 - "TwitchAuthorizationChangedPayload"
Cohesion: 0.67
Nodes (3): TwitchAuthorizationChangedPayload, Status, TwitchUserId

### Community 341 - "5. 持久化、發布與去重"
Cohesion: 0.33
Nodes (6): 5.1 已確認新增的資料表, 5.2 ChzzkSpider, 5.3 ChzzkStream, 5.4 NoticeChzzkStreamChannel, 5.5 GuildConfig 擴充與爬蟲上限, 5. 持久化、發布與去重

### Community 347 - "YoutubeMemberTokenCleanupConcurrencyTests"
Cohesion: 0.60
Nodes (3): MySqlComponentFact, Task, YoutubeMemberTokenCleanupConcurrencyTests

### Community 348 - "14. 部署與回滾"
Cohesion: 0.50
Nodes (4): 14.1 建議部署順序, 14.2 相容性, 14.3 回滾, 14. 部署與回滾

## Knowledge Gaps
- **1643 isolated node(s):** `$schema`, `.opencode/plugins/graphify.js`, ``__EFMigrationsHistory``, `net8.0`, `prometheus-net.AspNetCore (8.2.1)` (+1638 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 2525 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **19 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MainDbService` connect `MainDbService` to `.GetLocaleAsync`, `.Warn`, `SendMsgToAllGuildService`, `ClusterQueryService`, `TwitchSubscriptionService`, `.AddChannel`, `MainDbContext`, `.AddChannel`, `.GetCommandPath`, `YoutubeMemberAuthorizationService`, `.RetryWithBackoffAsync`, `MySqlComponentFixture`, `.LoadSnapshotAsync`, `ChzzkStream`, `BotState`, `.SendCrawlerResultAsync`, `YoutubeMemberRoleService`, `TwitchDetectionService`, `GuildLocaleService`, `TwitcastingNotification`, `.SetMemberCheckVideoIdAsync`, `ChzzkService`, `YoutubeMemberService`, `Administration`, `Twitch`, `YoutubeMemberSetting`, `.SetVerificationLogChannelAsync`, `YoutubeStreamService`, `.Start`, `Bot`, `YoutubeStream`, `.BuildSnapshotAsync`, `YoutubeDetectionService`, `Task`, `.AutoRecordAsync`, `.Get`, `.GetDbContext`, `UtilityService`, `TwitcastingService`, `.CreateOrRepairConfigurationAsync`, `TwitchSpider`, `TwitchService`?**
  _High betweenness centrality (0.084) - this node is a cross-community bridge._
- **Why does `DiscordStreamNotifyBot.DataBase.Table` connect `DiscordStreamNotifyBot.DataBase.Table` to `NoticeChzzkStreamChannel`, `TwitcastingLiveStartPlannerTests`, `TwitchStream`, `NoticeYoutubeStreamChannel`, `DiscordStreamNotifyBot.Localization`, `TwitchSpider`, `GuildConfig`, `TwitchSubscriptionCheck`, `DiscordStreamNotifyBot.Command.Attribute`, `DiscordStreamNotifyBot.Tests`, `YoutubeChannelSpider`, `YoutubeMemberCheck`, `NoticeTwitchStreamChannel`, `DbEntity`, `YoutubeChannelOwnedType`, `GuildYoutubeMemberConfig`, `GuildTwitchSubscriptionConfig`, `YoutubeMemberAccessToken`, `TwitchStateDecisions.cs`, `ChzzkSpider`, `NoticeTwitcastingStreamChannel`, `Video`, `.VideoLookupSearchesAllFourTables`, `ChzzkPollPolicyTests`, `DiscordStreamNotifyBot.Shared.Messages`, `TwitchBroadcasterAuthorization`, `DiscordStreamNotifyBot.DataBase`, `TwitcastingStream`?**
  _High betweenness centrality (0.060) - this node is a cross-community bridge._
- **Why does `MainDbContext` connect `MainDbContext` to `.GetLocaleAsync`, `NoticeChzzkStreamChannel`, `TwitchStream`, `Extensions`, `NoticeTwitchStreamChannel`, `ChzzkStream`, `AdminSettingsMutationResult`, `GuildLocaleService`, `MainDbContextFactory`, `ChzzkService`, `YoutubeMemberService`, `NoticeTwitcastingStreamChannel`, `DiscordStreamNotifyBot.DataBase.Table`, `SharedExtensions`, `MainDbService`, `.BuildSnapshotAsync`, `YoutubeDetectionService`, `.GetDbContext`, `TwitcastingService`, `TwitcastingStream`, `TwitchService`?**
  _High betweenness centrality (0.059) - this node is a cross-community bridge._
- **What connects `$schema`, `.opencode/plugins/graphify.js`, ``__EFMigrationsHistory`` to the rest of the system?**
  _1643 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `TwitchSubscriptionApiClient` be split into smaller, more focused modules?**
  _Cohesion score 0.1168091168091168 - nodes in this community are weakly interconnected._
- **Should `.GetLocaleAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.1461794019933555 - nodes in this community are weakly interconnected._
- **Should `DiscordStreamNotifyBot.Shared.csproj` be split into smaller, more focused modules?**
  _Cohesion score 0.08333333333333333 - nodes in this community are weakly interconnected._