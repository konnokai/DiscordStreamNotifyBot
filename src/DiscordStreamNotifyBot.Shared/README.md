# DiscordStreamNotifyBot.Shared

## 專案職責

三層架構的**無狀態共用基礎層**（classlib，`net8.0`）。集中放置不綁定 Discord gateway 的所有共用元件：資料庫、認證、HTTP 用戶端、Redis/RabbitMQ 連線、設定、跨層全域狀態、無狀態 API（YouTube/Twitch）、以及匯流排 DTO。`Notifier` / `Scraper` / `Coordinator` 三個執行檔皆**只**參考本專案（已由 `get_project_graph` 驗證：三層僅依賴 Shared、零循環）。

> 架構全貌見根目錄 [`../../CLAUDE.md`](../../CLAUDE.md) 與 [`../../HORIZONTAL_SCALING_PLAN.md`](../../HORIZONTAL_SCALING_PLAN.md)。

## 相依

- **外部**：MySQL（EF Core / Pomelo）、Redis（StackExchange.Redis）、RabbitMQ（`RabbitMQ.Client`）、YouTube Data API、Twitch API、TwitCasting API。
- **專案**：無（葉節點）。

## 資料夾與檔案

### 根目錄（基礎設施與跨層狀態）

| 檔案 | 職責 |
|------|------|
| `BotConfig.cs` | `bot_config.json` 設定模型；含 env 覆寫（`TOTAL_SHARDS` / `MYSQL_*` / `REDIS_OPTION` / `DISCORD_TOKEN` / `GOOGLE_API_KEY` / `RABBITMQ_*`）與 `RabbitMqConfig`。 |
| `BotRole.cs` | 角色列舉（Notifier / Scraper / Coordinator），由各 `Program.cs` 寫死。 |
| `BotState.cs` | 跨層全域執行期狀態（DbService/Redis/Shard/旗標）。含 shard 歸屬公式 `IsServerOnThisShard` 與刪除守衛 `ShouldDeleteMissingGuild`。 |
| `ClusterService.cs` | 叢集協調：scraper leader 鎖、shard lease、心跳、`TOTAL_SHARDS` 公告（均存 Redis）。 |
| `GracefulShutdown.cs` | 統一 SIGINT/**SIGTERM** 攔截（§11-1），提供關閉用 `CancellationToken`。 |
| `PeriodicRunner.cs` | `PeriodicTimer` 背景輪詢工具（§12.1），天然無重入、吃 CancellationToken。 |
| `RabbitMqService.cs` | RabbitMQ 連線封裝：宣告 `bot.notify` topic exchange + DLX，publisher confirms，per-shard quorum queue（TTL/expires，§11-3）。 |
| `NotificationBusPublisher.cs` | 發布端共用單例（延遲初始化、執行緒安全），Scraper 偵測事件 publish DTO 用。 |
| `RedisConnection.cs` / `RedisDataStore.cs` | Redis 連線單例；型別化 key-value 存取。 |
| `RedisChannels.cs` | 所有 Redis pub/sub 頻道與叢集 key 常數（含 `cluster:*`、`youtube.*`、`member.*`…）。 |
| `Log.cs` | 靜態彩色 Console/檔案 Log（`Info/Warn/Error/New`）；`RolePrefix` 標記角色（§12.7）。 |
| `Utility.cs` / `SharedExtensions.cs` | 共用工具方法；Embed 顏色擴充與四表 video 查詢 helper（`GetStreamVideoByVideoId` 等）。 |
| `YoutubeApiService.cs` | 無狀態 YouTube Data API 用戶端（偵測與通知共用）。 |
| `StartupPreflight.cs` | 啟動前置檢查（Redis/RabbitMQ/MySQL/Discord 連線）。 |
| `IInteractionService.cs` | DI 自動載入用標記介面。 |
| `AssemblyInfo.cs` | 組件中介資料。 |

### `Auth/` — 會限 OAuth token 加解密（與後端共用契約）

| 檔案 | 職責 |
|------|------|
| `TokenCrypto.cs` | AES-CBC 加解密 + HMAC-SHA256 雜湊原語。 |
| `TokenManager.cs` | token 組裝/解析：格式 `iv.payload.signature`，Encrypt-then-MAC，先驗簽再解密。 |

### `DataBase/` — EF Core 資料層

| 檔案 | 職責 |
|------|------|
| `MainDbContext.cs` | 主 `DbContext`（YouTube 四表、Twitch、Twitcasting、GuildConfig、會限…）。 |
| `MainDbService.cs` | `PooledDbContextFactory` 包裝；`GetDbContext()` 取短生命週期 context（§12.8）。 |
| `MainDbContextFactory.cs` | 設計階段工廠（供 `dotnet ef`，免 startup project）。 |
| `Table/*.cs` | 實體模型（22 個）。YouTube 影片四表 `HoloVideo`/`NijisanjiVideo`/`OtherVideo`/`NonApprovedVideo` 共同繼承 `Video`（以 `YTChannelType` 區分）；其餘為通知頻道設定、爬蟲、會限、橫幅等。 |

### `Migrations/` — EF Core 遷移歷史

含 4 組遷移（`*_RefactorDbContext` / `*_ModifyTwitCastingTable` / `*_AddMaxSpiderCountSettingField` / `*_SyncModelDrift`）與 `MainDbContextModelSnapshot.cs`。`.Designer.cs` 與 snapshot 為自動產生。
正式 DB 基線化腳本見 `Migrations/_Baseline_ExistingDb.sql`（一次性，§11-2，上線前先備份）。

### `HttpClients/` — 外部 HTTP 用戶端

| 路徑 | 職責 |
|------|------|
| `DiscordWebhookClient.cs` | Discord webhook 發送。 |
| `Twitcasting/TwitcastingClient.cs` + `Twitcasting/Model/*.cs` | TwitCasting API 用戶端與 JSON 回應模型。 |

### `Messages/` 與 `SharedService/`

| 路徑 | 職責 |
|------|------|
| `Messages/Notifications.cs` | 匯流排 DTO（`YoutubeNotification` / `TwitchNotification` / `TwitcastingNotification` / `BannerChangeNotification`）與 `NotifyRoutingKeys`。 |
| `SharedService/Twitch/TwitchApiService.cs` | 無狀態 Twitch API 用戶端。 |
| `SharedService/Youtube/Json/*.cs` | 彩虹社 liver/stream JSON 模型。 |

## 維護注意

- **讀取一律 `.AsNoTracking()`**（CLAUDE.md 慣例）；`SharedExtensions.cs` 仍有少數遺漏，見 [`docs/CODE_REVIEW.md`](../../docs/CODE_REVIEW.md) 🟡-2。
- **Auth token 格式與 `Discord-Stream-Bot-Backend` 共用**，任何更動需兩端同步（見 CODE_REVIEW 🟡-1）。
- 修改實體欄位後請用 `/migrate` 產生遷移（EF 工具指向本專案）。
