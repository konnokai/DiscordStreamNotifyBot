# CLAUDE.md

**「直播小幫手」(Discord Stream Notify Bot)** — 通知 Discord 伺服器 Vtuber 直播的機器人（YouTube / Twitch / TwitCasting）。Discord.Net、.NET 8.0、MySQL (EF Core + Pomelo)、Redis (StackExchange.Redis)。

> **語言規範**：程式碼註解、Log 訊息、使用者介面字串、commit 訊息一律**繁體中文**。

---

## 目前狀態（架構變更時，與變更同一個 commit 更新本段）

- 程式碼 = 多專案（`DiscordStreamNotifyBot.sln`）：`src/DiscordStreamNotifyBot.Shared`（共用基礎，含 `DataBase/`+`Migrations/`、`Auth/`、`BotState`、`StartupPreflight`、`GracefulShutdown`、`RedisChannels`、`NotificationBus`(Redis Streams)、`Messages/` DTO、`*ApiService`、`ClusterService`、`SharedExtensions`）+ `src/DiscordStreamNotifyBot.Scraper`（叢集唯一偵測宿主：`Detection/` + leader 鎖，publish `bot:notify`）+ `src/DiscordStreamNotifyBot.Notifier`（連 Discord、指令系統、消費 `bot:notify` 發送，輸出 `DiscordStreamNotifyBot.dll`）+ `src/DiscordStreamNotifyBot.Coordinator`（exe 空殼）。
- 進行中：**三層拆分重構（Scraper / Notifier / Coordinator + Shared，Redis Streams 匯流排）**，權威設計與分階段步驟見 [docs/HORIZONTAL_SCALING_PLAN.md](docs/HORIZONTAL_SCALING_PLAN.md)，目前進度：**階段 3 完成（Scraper 偵測 → Streams → Notifier 消費發送，僅編譯驗證），階段 4（Coordinator）待做**。正確性（§11）待計畫完成後於測試環境驗。
- **`claude` 分支 = RabbitMQ 版三層拆分的完整參考實作。只用 `git show claude:<path>` 閱讀收割，永不合併、永不 checkout 到工作樹。**
- `nadekobot/` = 參考用外部專案（已 gitignore），非本專案程式碼。
- 開始任何重構工作前，先讀 [docs/LETTER_TO_FUTURE_SESSIONS.md](docs/LETTER_TO_FUTURE_SESSIONS.md)。

## Build & Run

```powershell
dotnet build DiscordStreamNotifyBot.sln -c Release   # commit 前必跑，0 error 才可提交
dotnet run -c Release --project src/DiscordStreamNotifyBot.Notifier        # 單 shard（現行有功能的 app）
dotnet run -c Release --project src/DiscordStreamNotifyBot.Notifier -- 0 4 # [ShardId, TotalShards]
```

- 首次執行若無 `bot_config.json` 會產生 `bot_config_example.json` 後退出；必填 DiscordToken、WebHookUrl、GoogleApiKey、ApiServerDomain。
- **一律建置整個 solution，不要只建單一專案**（拆分後跨專案相依多，單建會漏編譯錯誤）。
- **無自動化測試框架**；驗證靠 Release build + 手動實測（重構期間用計畫 §11 清單）。

### 組態旗標（`#if` 改變行為，不是最佳化）

| 組態 | 行為 |
|------|------|
| `Release` | 完整功能；全球註冊 Slash 指令 |
| `Debug` | 登入 Discord，指令只註冊到 `TestSlashCommandGuildId` |
| `Debug_DontRegisterCommand` | 略過指令註冊 |
| `Debug_API` | 單次 YouTube API 呼叫後立即返回 |

改程式碼時務必確認周圍的 `#if` 區塊；**正式行為只以 Release 為準**。

## EF Core 鐵則

```powershell
dotnet ef migrations add <Name> --project src/DiscordStreamNotifyBot.Shared
dotnet ef database update --project src/DiscordStreamNotifyBot.Shared    # 僅限本地/開發 DB
```

- **正式 DB 永遠不用 `database update` 直連**。改產冪等 SQL、人工審核後手動於維護窗口執行：
  `dotnet ef migrations script --idempotent --project src/DiscordStreamNotifyBot.Shared -o migrate.sql`
- 正式 DB **已完成基線化**（`__EFMigrationsHistory` 存在，2026-06），且已套用至 claude 分支的 `SyncModelDrift`。**禁用 `EnsureCreated`**。
- 重構搬遷 DataBase/ 時，migration 檔**只能照搬、不可重新生成**（ID 必須對上正式 DB 歷史，詳見計畫 §9-2）。

## 架構要點（現行樹）

- Notifier 進入點 `Program.cs` → `Bot.cs`：init 設定/DB/Redis → DiscordSocketClient（手動 shard 參數）→ 指令註冊 → 啟動 `NotificationBusConsumer`（消費 `bot:notify`）→ 阻塞至關閉。Scraper 進入點 `Program.cs` → `ScraperService`（搶 leader 鎖）→ `DetectionHost`（`Detection/` 偵測、publish DTO，不連 Discord）。
- 全域靜態狀態在 Shared 的 `BotState`（`Redis/RedisSub/RedisDb`、`DbService`、`IsConnect`、`ShardId/TotalShardCount`、`IsServerOnThisShard`/`ShouldDeleteMissingGuild`）；Notifier 的 `Bot` 靜態成員委派至此。
- **雙指令系統**（目錄結構對稱）：`Command/` = `s!` 前綴（擁有者/管理用）；`Interaction/` = Slash（一般使用者）。
- **DI 反射自動載入**：實作 `IInteractionService` / `ICommandService` 的類別自動註冊 Singleton（`Interaction|Command/Extensions.cs`），新增服務不需手動登記。
- DB：`MainDbService.GetDbContext()` 取短生命週期 context（`using var db = ...`），讀取一律 `.AsNoTracking()`。YouTube 影片四表（Holo/Nijisanji/Other/NonApproved）繼承 `Video`，依 videoId 查詢需依序探查四表。
- 偵測與發送已拆分（計畫階段 3）：偵測（Timer/排程爬取/webhook 訂閱）在 **Scraper** `Detection/`，publish DTO 到 `bot:notify`；發送（`_client.GetGuild` + embed）在 **Notifier** `SharedService/`，消費匯流排後 `DispatchFromBusAsync` 重建 embed。跨層 DTO 在 `Shared/Messages/`。

## 外部契約（不可片面更改）

以下 Redis pub/sub 頻道名是與外部 repo 的跨專案契約（[YoutubeStreamRecord](https://github.com/konnokai/YoutubeStreamRecord) 錄影工具、[Discord-Stream-Bot-Backend](https://github.com/konnokai/Discord-Stream-Bot-Backend) webhook 接收端）：

| 分類 | 頻道 |
|------|------|
| YouTube | `youtube.startstream` `youtube.endstream` `youtube.addstream` `youtube.deletestream` `youtube.unarchived` `youtube.memberonly` `youtube.record` `youtube.429error` `youtube.pubsub.{CreateOrUpdate,Deleted,NeedRegister}` |
| Twitch | `twitch.record` `twitch:channel_update` `twitch:stream_offline` |
| TwitCasting | `twitcasting.pubsub.startlive` |
| 會限 | `member.revokeToken` `member.syncRedisToken` |

改名 = 破壞另外兩個 repo。`Auth/`（TokenManager，AES-CBC+HMAC，金鑰 `RedisTokenKey`）與後端共享，同屬契約。

## Conventions

- Log：靜態 `Log.Info/Warn/Error`；例外一律 `.Demystify()` 後再記。
- JSON：`Newtonsoft.Json`（`JsonConvert`）；**不使用 `System.Text.Json`**。
- Global usings 已在 csproj 宣告：`Discord`、`Discord.WebSocket`、`Newtonsoft.Json`、`StackExchange.Redis`、`Microsoft.EntityFrameworkCore`、`System.Diagnostics`、`Google.Apis.YouTube.v3.Data`。
- Embed 顏色：`WithOkColor()`（綠）/ `WithErrorColor()`（深灰）/ `WithRecordColor()`（紅）。
- 風格遵循根目錄 `.editorconfig`。

## 指令文件

各指令的權威使用說明在 Notion：<https://konnokai.notion.site/a4fff40bd95c4bec9edca5b78cdd5d37>。本檔刻意不維護指令清單；查指令行為讀 `Interaction/`、`Command/` 模組與 `Data/HelpDescription.txt`。

## 制度條款

1. 架構或慣例變更，**同一個 commit** 更新 CLAUDE.md 對應段落（尤其「目前狀態」）。
2. 本檔上限 **150 行**：要加新規則，先刪或合併一條舊的；長內容放 `docs/` 用連結引用。
3. 狀態判讀的信任順序：**工作樹 > git 歷史 > memory > 文件**。文件與程式碼矛盾時，以程式碼為準並回頭修文件。
4. 重構每完成一個階段：勾計畫 checkbox + commit + 更新本檔「目前狀態」。進度必須存在 repo，不存在任何 session 的記憶裡。

## 適用 Skills

`/migrate`（EF 變更後）、`/code-review`（重構/PR 後）、`/security-scan`（Auth 或加密變更時）、`/health-check`、`/de-sloppify`、`/checkpoint`。不適用：`/scaffold`、`/tdd`（無測試框架）、`/api-versioning`、`/aspire`。
