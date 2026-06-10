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
├─ DiscordStreamNotifyBot.Shared/       (classlib) 共用基礎：DataBase/Auth/HttpClients/Log/Redis/BotConfig/Utility、
│                                                   RedisChannels、ClusterService、RabbitMqService、Messages DTO、
│                                                   StartupPreflight、GracefulShutdown、MainDbContextFactory(EF 設計工廠)
├─ DiscordStreamNotifyBot.Notifier/     (exe) 通知層：Discord 連線 + Interaction/Command 指令樹 + SharedService
│                                              （AssemblyName = DiscordStreamNotifyBot；偵測程式碼在此組件
│                                              但僅 Scraper 宿主會啟動；通知一律由匯流排消費而來）
├─ DiscordStreamNotifyBot.Scraper/      (exe) 爬蟲層：leader 鎖 + 心跳 + DetectionHost
│                                              （參考 Notifier 組件、無頭模式實體執行偵測服務並發布至匯流排）
└─ DiscordStreamNotifyBot.Coordinator/  (exe) 主控層：心跳監控、leader 觀察、TOTAL_SHARDS 公告
```

> **架構（角色由執行檔決定，無模式旗標）**：
> Scraper＝唯一偵測者（leader 鎖單例；`Bot.IsDetectionHost` 由 DetectionHost 設定）→ publish DTO 到 RabbitMQ
> `bot.notify`；Notifier shard＝消費 `notify.shard.{id}` → `EmbedBuilderFactory` 重建 embed → 只發給自己持有的
> 伺服器（shard 守衛 ×6 處）。**Notifier 必須搭配 RabbitMQ + Scraper 才有通知**（指令系統獨立可用）。
> 會限（YoutubeMemberService）**不走匯流排**：按 shard 分區由各 Notifier 自行檢查。
> 階段 5：官方伺服器白名單存 Redis SET（首啟由 OfficialList.json 播種）、狀態列計數跨 shard 彙總（Redis HASH）。
> **待辦**：YoutubeApiService 抽出、§11-2 EF baseline、多程序實測（計畫 §6.2 驗證清單）。

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
# dotnet ef database update --project src/DiscordStreamNotifyBot.Shared
#   ← 現況仍由 Notifier shard 0 啟動時呼叫 EnsureCreated()；
#     注意既有 DB 由 EnsureCreated 建立、無 __EFMigrationsHistory，直接 database update 會衝突（計畫 §11-2）。
#     has-pending-model-changes 目前回報 drift，屬既有狀況，Docker 化前需處理。
```

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

### SharedService（核心業務邏輯）

`SharedService/{Youtube,Twitch,Twitcasting,YoutubeMember}/` 是最重要的檔案。  
模式：Singleton + 多個 `System.Threading.Timer` + Redis pub/sub 訂閱。

`YoutubeStreamService` 是 `partial` class，分散在：

| 檔案 | 內容 |
|------|------|
| `YoutubeStreamService.cs` | 建構子、Redis 訂閱、YouTube API 存取 |
| `Schedule.cs` | 定時爬取排程 |
| `ReminderAction.cs` | 到點提醒動作 |
| `ChangeGuildBanner.cs` | 開台時更換伺服器橫幅 |
| `EmbedBuilderFactory.cs` | 建立通知 Embed |

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
