# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## Project Overview

**「直播小幫手」(Discord Stream Notify Bot)** — 通知 Discord 伺服器 Vtuber 直播的機器人，支援 YouTube、Twitch、TwitCasting。  
以 Discord.Net 建構，.NET 8.0。**正在進行水平擴展（三層拆分）重構**，詳見 `HORIZONTAL_SCALING_PLAN.md`。

> **語言規範**：程式碼、註解、Log 訊息、使用者介面字串一律使用**繁體中文**。

### Solution 結構（`DiscordStreamNotifyBot.sln`）

```
src/
├─ DiscordStreamNotifyBot.Shared/       (classlib) 無狀態共用基礎：
│                                                   DataBase/Auth/HttpClients/Log/Redis/BotConfig/Utility、
│                                                   RedisChannels、ClusterService、RabbitMqService、Messages DTO、
│                                                   StartupPreflight、GracefulShutdown、MainDbContextFactory、
│                                                   YoutubeApiService、TwitchApiService（無狀態 API）、BotState、
│                                                   SharedExtensions、IInteractionService、SharedService/Youtube/Json（資料模型）
├─ DiscordStreamNotifyBot.Notifier/     (exe) 通知層：Discord 連線 + Interaction/Command 指令樹 + YoutubeMember 會限服務
│                                              （AssemblyName = DiscordStreamNotifyBot；消費匯流排重建 embed 發送）
│                                              SharedService/{Youtube,Twitch,Twitcasting}Service（指令支援+發送）、
│                                              EmojiService、*EmbedBuilderFactory
├─ DiscordStreamNotifyBot.Scraper/      (exe) 爬蟲層：leader 鎖 + 心跳 + DetectionHost（只參考 Shared，不參考 Notifier）
│                                              Detection/{Youtube,Twitch,Twitcasting}（偵測服務，只有 Timer + Redis 訂閱）
│                                              **完全不建立 DiscordSocketClient**，偵測到事件 publish DTO 至匯流排
└─ DiscordStreamNotifyBot.Coordinator/  (exe) 主控層：心跳監控、leader 觀察、TOTAL_SHARDS 公告
```

> **專案職責清楚（無循環/交叉參考）**：Notifier、Scraper、Coordinator 皆只參考 Shared。
> **偵測（Timer/Redis 訂閱/排程爬取/PubSub/EventSub/WebHook）位於 Scraper 的 `Detection/`**，不碰 Discord gateway，
> 偵測到事件 publish DTO 至匯流排。**指令支援 + 通知發送位於 Notifier 的 `SharedService/`**（持有 DiscordSocketClient
> /EmojiService/EmbedBuilderFactory，消費匯流排重建 embed 發送、建立活動、換橫幅）。
> 兩端共用的無狀態 API 收斂於 Shared 的 `YoutubeApiService` / `TwitchApiService`。`Bot`(Notifier) 的共用靜態成員委派至 `BotState`(Shared)。

> **架構（角色由執行檔決定，無模式旗標）**：
> Scraper＝唯一偵測者（leader 鎖單例；`BotState.IsDetectionHost` 由 DetectionHost 設定）→ publish DTO 到 RabbitMQ
> `bot.notify`；Notifier shard＝消費 `notify.shard.{id}` → `EmbedBuilderFactory` 重建 embed → 只發給自己持有的
> 伺服器（shard 守衛）。**Notifier 必須搭配 RabbitMQ + Scraper 才有通知**（指令系統獨立可用）。
> 會限（YoutubeMemberService）**不走匯流排**：按 shard 分區由各 Notifier 自行檢查。
> 少數 YouTube owner 控制（切換錄影 / 強制 SubscribePubSub / 手動 AddVideo）由 Notifier 指令發 `youtube.control.*`
> Redis 訊息，Scraper 偵測器訂閱後執行。
> 階段 5：官方伺服器白名單存 Redis SET（首啟由 OfficialList.json 播種）、狀態列計數跨 shard 彙總（Redis HASH）。
> **待辦**：§11-2 EF baseline、多程序實測（計畫 §6.2 驗證清單）。

### 相依的外部系統

| 系統 | 用途 | 通訊方式 |
|------|------|----------|
| [YoutubeStreamRecord](https://github.com/konnokai/YoutubeStreamRecord) | 錄影；實時偵測開台 | Redis pub/sub |
| [Discord-Stream-Bot-Backend](https://github.com/konnokai/Discord-Stream-Bot-Backend) | YouTube 會限 OAuth 驗證；接收 PubSubHubbub / Twitch EventSub webhook | HTTP (`ApiServerDomain`) |

---

## Build & Run

```powershell
# 建置整個方案（正式使用一律 Release — Debug 組態會跳過 Discord 登入、指令註冊等大量功能）
dotnet build DiscordStreamNotifyBot.sln -c Release

# 執行通知層（= 原本的 bot 主程式）
dotnet run -c Release --project src/DiscordStreamNotifyBot.Notifier
# 首次執行：若 bot_config.json 不存在，自動產生 bot_config_example.json 後退出
# 複製並填入 DiscordToken、WebHookUrl、GoogleApiKey、ApiServerDomain（必填）

# Notifier Sharding（啟動參數 [ShardId, TotalShards]，預設 0/1）
dotnet run -c Release --project src/DiscordStreamNotifyBot.Notifier -- 0 4

# 其他角色（重構中，偵測主邏輯尚未搬入）
dotnet run -c Release --project src/DiscordStreamNotifyBot.Scraper
dotnet run -c Release --project src/DiscordStreamNotifyBot.Coordinator
```

> **角色由執行哪個 exe 決定**（各 Program.cs 寫死 `BotRole`），無 `Role`/偵測/匯流排設定欄位。
> 設定可由環境變數覆寫（`TOTAL_SHARDS` / `MYSQL_CONNECTION_STRING` / `REDIS_OPTION` /
> `DISCORD_TOKEN` / `GOOGLE_API_KEY` / `RABBITMQ_*`，見計畫 §3）。
> Docker 部署見 `docker-compose.yml`。Notifier 的通知需 RabbitMQ + Scraper 運行。

> **無自動化測試**，此 repo 不含任何測試框架。重構驗證依賴多程序手動實測（計畫 §6.2 驗證清單）。

### 組態旗標（`#if` 改變行為，非單純最佳化）

| 組態 | 行為 |
|------|------|
| `Release` | 完整功能；全球註冊 Slash 指令 |
| `Debug` | 登入 Discord，但指令只註冊到 `TestSlashCommandGuildId` |
| `Debug_DontRegisterCommand` | 略過指令註冊（快速迭代非指令邏輯用）|
| `Debug_API` | 僅執行單次 YouTube API 呼叫後立即返回 |

修改程式碼時，**務必確認**周圍的 `#if` 區塊。

### EF Core Migration

EF 工具指向 **Shared** 專案（已提供 `MainDbContextFactory` 設計階段工廠，免 startup project）：

```powershell
dotnet ef migrations add <Name> --project src/DiscordStreamNotifyBot.Shared
dotnet ef database update --project src/DiscordStreamNotifyBot.Shared   # 僅本地/開發
```

> **套用：本地用 `database update`，正式環境用 Script-Migration 手動套用。**
> 正式環境**不**對正式 DB 直連 `database update`，改產生 SQL、人工審過後手動到對應資料庫執行：
> `dotnet ef migrations script --idempotent --project src/DiscordStreamNotifyBot.Shared -o migrate.sql`
> （冪等腳本會一併寫入 `__EFMigrationsHistory`，先手動跑完再部署 → shard 0 啟動的 `Migrate()` 視為無待處理＝no-op，不衝突）。

> **啟動時 DB 初始化**：Notifier shard 0 啟動呼叫 `InitializeDatabase()`（取代舊 EnsureCreated）：
> 全新空庫→`Migrate()` 建表並寫歷史；有遷移歷史→套用待處理遷移；
> 既有庫但無 `__EFMigrationsHistory`（舊 EnsureCreated 建立）→ 安全略過並提示先基線化。
>
> **既有正式 DB 基線化（一次性，計畫 §11-2）**：舊庫由 EnsureCreated 建立、無遷移歷史，
> 須先執行 `src/DiscordStreamNotifyBot.Shared/Migrations/_Baseline_ExistingDb.sql`（標記前置遷移為已套用）
> 再 `dotnet ef database update`（套用 `SyncModelDrift`，drop 已移除的 Twitter Space 舊表）。**務必先備份**。
> `has-pending-model-changes` 已無 drift。

---

## Architecture

### 啟動流程

```
Program.Main
  └─ Bot(shardId, totalShards)
       ├─ BotConfig.InitBotConfig()          # 讀 bot_config.json，auto-gen RedisTokenKey
       ├─ MainDbService(connectionString)
       ├─ RedisConnection.Init()             # 失敗立即 throw
       ├─ (shard 0) db.EnsureCreated()
       └─ StartAndBlockAsync()
            ├─ DiscordSocketClient 建立 & 事件綁定 (Ready / JoinedGuild / LeftGuild)
            ├─ Discord Login + 等待 IsConnect
            ├─ ServiceCollection 組裝 (見下)
            ├─ InteractionHandler / CommandHandler 初始化
            ├─ Slash 指令註冊 (Debug=test guild / Release=global)
            └─ 阻塞直到 IsDisconnect，再清理 spider flags & 儲存 DB
```

### 全域靜態狀態（`Bot` 類別）

| 成員 | 說明 |
|------|------|
| `Bot.Redis` / `Bot.RedisSub` / `Bot.RedisDb` | StackExchange.Redis 連線 |
| `Bot.DbService` | `MainDbService`（DbContextOptions 工廠） |
| `Bot.client` | `DiscordSocketClient` |
| `Bot.IsConnect` / `Bot.IsDisconnect` | 啟動 / 關閉旗標 |
| `Bot.IsHoloChannelSpider` / ... | Spider 執行中旗標（關閉前等待完成）|

### DI 自動載入（反射）

`Interaction/Extensions.cs` → `LoadInteractionFrom`  
`Command/Extensions.cs` → `LoadCommandFrom`

掃描 Assembly，找出所有實作 `IInteractionService` / `ICommandService` 的具體類別，自動註冊為 Singleton。**新增 SharedService 只需實作 `IInteractionService`，不需手動 DI 登記。**

### 雙指令系統

| | 前綴指令（Legacy） | Slash 指令（主要）|
|-|-------------------|------------------|
| 目錄 | `Command/` | `Interaction/` |
| 前綴 | `s!` | `/` |
| Handler | `CommandHandler` | `InteractionHandler` |
| 用途 | 擁有者/管理 | 一般使用者 |

兩個目錄結構完全對稱（`Youtube/`、`Twitch/`、`TwitCasting/`、`YoutubeMember/`、`Attribute/`、`Help/`）。

### 偵測服務（Scraper `Detection/`）與通知服務（Notifier `SharedService/`）

偵測（Timer + Redis pub/sub 訂閱）與發送（GetGuild 送訊息）**已拆分到不同專案**，不再共用一個 partial class：

| 平台 | Scraper `Detection/`（偵測→publish DTO） | Notifier `SharedService/`（指令支援+消費發送） | Shared（無狀態 API） |
|------|------|------|------|
| YouTube | `Youtube/YoutubeDetectionService{,.Schedule,.Reminder}.cs`（排程爬取、Redis 訂閱、PubSub、reminder） | `Youtube/YoutubeStreamService.cs`、`EmbedBuilderFactory.cs` | `YoutubeApiService` |
| Twitch | `Twitch/TwitchDetectionService.cs` + `Debounce/` | `Twitch/TwitchService.cs`、`TwitchEmbedBuilderFactory.cs` | `TwitchApiService` |
| Twitcasting | `Twitcasting/TwitcastingDetectionService.cs` | `Twitcasting/TwitcastingService.cs`、`TwitcastingEmbedBuilderFactory.cs` | – |
| 會限 | – | `YoutubeMember/YoutubeMemberService.cs`（按 shard，不走匯流排） | – |

> 偵測服務只依賴 Shared（`*ApiService` / DB / `NotificationBusPublisher` / `RedisChannels`），**不參考 Discord gateway**。
> Notifier 服務持有 `DiscordSocketClient` + `EmojiService`，消費匯流排重建 embed 後發送。
> `YoutubeDetectionService.cs` 為 `partial` class（核心 + `.Schedule` 排程爬取 + `.Reminder` 到點提醒）。

### 資料庫（`DataBase/`）

`MainDbContext` 透過 `MainDbService.GetDbContext()` 取得**短生命週期** context（`using var db = ...`），讀取一律加 `.AsNoTracking()`。

**YouTube 影片四表共同繼承 `Video` 基底類別：**

| Table | `YTChannelType` |
|-------|-----------------|
| `HoloVideos` | `Holo` |
| `NijisanjiVideos` | `Nijisanji` |
| `OtherVideos` | `Other` |
| `NonApprovedVideos` | `NonApproved` |

依 video id 查詢時需依序探查四張表（參見 `Interaction/Extensions.cs` 中的 helper 方法）。頻道歸屬可透過 `YoutubeChannelOwnedType` 覆寫。

### Redis Pub/Sub 頻道（與錄影工具 IPC）

| 分類 | 頻道 |
|------|------|
| YouTube | `youtube.startstream` `youtube.endstream` `youtube.addstream` `youtube.deletestream` `youtube.unarchived` `youtube.memberonly` `youtube.record` `youtube.429error` `youtube.test` `youtube.pubsub.{CreateOrUpdate,Deleted,NeedRegister}` |
| Twitch | `twitch.record` `twitch:channel_update` `twitch:stream_offline` |
| TwitCasting | `twitcasting.pubsub.startlive` |
| 會限 | `member.revokeToken` `member.syncRedisToken` |

### Auth（`Auth/`）

`TokenManager` + `TokenCrypto`：`AES-CBC 加密 + HMAC-SHA256 簽章`，格式 `iv.payload.signature`，使用 `RedisTokenKey` 作為金鑰，與後端共享用於 YouTube 會限 OAuth。

---

## Key Conventions

- **Log**：`Log.Info/Warn/Error`（靜態類別，彩色 Console 輸出）。例外一律 `.Demystify()` 後再 Log。
- **JSON**：`Newtonsoft.Json`（`JsonConvert`），**不使用** `System.Text.Json`。
- **Global Usings**（csproj 已宣告，不會出現在個別檔案）：`Discord`、`Discord.WebSocket`、`Newtonsoft.Json`、`StackExchange.Redis`、`Microsoft.EntityFrameworkCore`、`System.Diagnostics`、`Google.Apis.YouTube.v3.Data`
- **程式碼風格**：遵循根目錄 `.editorconfig`。
- **Embed 顏色**：`WithOkColor()`（綠）/ `WithErrorColor()`（深灰）/ `WithRecordColor()`（紅）。

---

## Command Documentation

各指令的權威使用說明維護在 Notion：<https://konnokai.notion.site/a4fff40bd95c4bec9edca5b78cdd5d37>  
CLAUDE.md 刻意不重複維護指令清單。指令行為請讀 `Interaction/` 或 `Command/` 下的模組（及 `Data/HelpDescription.txt`）。

---

## 專案內建 Skills（`.claude/skills/`）

本 repo 自帶的工作流 skill，會在符合情境時自動觸發（亦可手動 `/<name>`）：

| Skill | 使用時機 |
|-------|----------|
| `add-detection-platform` | 新增直播平台偵測，或為既有平台多一種通知事件（Shared DTO/routing key → Scraper 偵測 → Notifier 發送 三層配方） |
| `debug-detection-bus` | 「偵測到卻沒發通知 / 漏發 / 重複發」除錯，追蹤 偵測→RabbitMQ→shard 守衛 整條鏈 |
| `ef-migration-baseline` | EF schema 變更、新增/套用 migration、或處理舊 EnsureCreated 庫的基線化（本專案特例版，補足 `/migrate`） |

---

## Applicable dotnet-claude-kit Skills

| Skill | 使用時機 |
|-------|----------|
| `/migrate` | 新增或修改 EF Core 資料表欄位後 |
| `/health-check` | 定期程式碼品質審查 |
| `/code-review` | PR 或重構後的審閱 |
| `/security-scan` | Auth 模組或加密邏輯有變更時 |
| `/checkpoint` | 儲存進度、切換任務前 |
| `/de-sloppify` | PR 前的程式碼清理 |

> **不適用**：`/scaffold`（無 VSA/Clean Arch）、`/tdd`（無測試框架）、`/api-versioning`（無 Web API）、`/aspire`。
