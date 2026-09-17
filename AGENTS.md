# AGENTS.md

**「直播小幫手」(Discord Stream Notify Bot)** — 通知 Discord 伺服器 Vtuber 直播的機器人（YouTube / Twitch）。Discord.Net、.NET 8.0、MySQL (EF Core + Pomelo)、Redis (StackExchange.Redis)。

> **本分支（`feat/bloodk1n`）為特定伺服器特規版**：已移除 TwitCasting／CHZZK 平台、YouTube 會員驗證、Twitch 訂閱驗證、Holo・Nijisanji 分類、YouTube 錄影委派、全服廣播與 Prometheus metrics 等功能與模組；主分支新增的同類功能不回收。

> **語言規範**：程式碼註解、Log 訊息與 commit 訊息使用**繁體中文**；一般使用者介面透過資源支援 `zh-TW`、`en-US`、`ja`，owner/admin 與營運訊息維持繁體中文。

---

## 目前狀態（架構變更時，與變更同一個 commit 更新本段）

- 通知補送：三平台共用 `NotificationDeliveryProgress`，以 `notification:delivery:{shardId}:{entryId}` HASH 保存逐目標訊息／crosspost checkpoint；暫時失敗保留 PEL，ACK 與 checkpoint 刪除使用同一段 Lua。checkpoint 不設 TTL，已修剪事件於 XAUTOCLAIM 回報刪除時清理；仍是 at-least-once，Discord 成功與 Redis 保存之間的程序中斷可能重複。已移除舊版通知 payload fallback，不支援混版部署。
- 程式碼 = 多專案（`DiscordStreamNotifyBot.sln`）：`src/DiscordStreamNotifyBot.Shared`（共用基礎，含 `DataBase/`+`Migrations/`、`Auth/`、`BotState`、`StartupPreflight`、`GracefulShutdown`、`RedisChannels`、`NotificationBus`(Redis Streams)、`Messages/` DTO、`*ApiService`、`ClusterService`、`SharedExtensions`）+ `src/DiscordStreamNotifyBot.Scraper`（叢集唯一偵測宿主：`Detection/` + leader 鎖，publish `bot:notify`）+ `src/DiscordStreamNotifyBot.Notifier`（連 Discord、指令系統、消費 `bot:notify` 發送，輸出 `DiscordStreamNotifyBot.dll`）+ `src/DiscordStreamNotifyBot.Coordinator`（主控層：`CoordinatorService` 心跳/leader/TOTAL_SHARDS 公告/匯流排 pending 監控）。
- 本地化第一階段的程式實作已完成，待手動 Discord 驗證：Slash group／command／parameter／choice 名稱固定使用英文 canonical，description 支援 `zh-TW`／`en-US`／`ja`；一般互動、Help 與 YouTube／Twitch 背景通知維持三語，通知事件對本 shard guild 批次讀取 locale，通知 DTO 不攜帶 locale；`/utility` 僅保留一般工具，共用管理指令集中於 `/server-admin`。
- 自動化測試第一至四批已完成，第五批 Redis／MySQL component tests 已實跑通過；多 shard guild ownership 與外部 API request contract 待完成。細節見 [docs/TESTING_PLAN.md](docs/TESTING_PLAN.md)。
- 網頁管理設定 Bot 端已實作：Notifier owning shard 透過固定 Redis request/reply 契約提供 guild/common 與 YouTube／Twitch 通知快照，並由既有 Utility/YouTube/Twitch 服務執行明確 desired-state mutation；跨專案契約見 [docs/WEB_ADMIN_SETTINGS_PLAN.md](docs/WEB_ADMIN_SETTINGS_PLAN.md)。
- 網頁管理設定的 YouTube／Twitch 爬蟲已接上共用 domain service、expanded snapshot 與 Web 表單；正式 Discord／Redis／MySQL／多 shard 驗收仍依 [docs/WEB_ADMIN_CRAWLER_VERIFICATION_PLAN.md](docs/WEB_ADMIN_CRAWLER_VERIFICATION_PLAN.md) 執行。
- 網頁管理設定 latency follow-up 已完成：Backend 三個 endpoint 共用 30 秒 absolute deadline，Bot envelope 傳遞 `deadlineUnixMs`，Redis reply／unavailable／deadline exceeded 分流；正式整合驗收仍待執行。
- 特規版精簡（`feat/bloodk1n`）：移除 TwitCasting／CHZZK 平台、YouTube 會員驗證（YoutubeMember）、Twitch 訂閱驗證（TwitchSubscription）、Holo・Nijisanji 分類與影片表（`YTChannelType` 僅剩 `Other`/`NonApproved`）、YouTube 錄影委派（RecordYoutubeChannel）、`/youtube now-streaming` 與 `/youtube list-record-channel`、全服廣播與 `/server-admin set-verification-log-channel`、Prometheus metrics 與 `deploy/grafana`。EF 實體與 `GuildConfig` 欄位已自模型移除但**未產生新 migration**（比照舊特規版；正式 DB 變更交由維護窗口處理），MySQL component test `FullMigrationSetIsAppliedAndModelHasNoPendingChanges` 因此會回報 pending model changes。
- 開始任何重構工作前，先讀 [docs/LETTER_TO_FUTURE_SESSIONS.md](docs/LETTER_TO_FUTURE_SESSIONS.md)。

## Build & Run

```powershell
dotnet build DiscordStreamNotifyBot.sln -c Release   # commit 前必跑，0 error 才可提交
dotnet test DiscordStreamNotifyBot.sln -c Release    # 執行 tests/ 下的自動化測試
dotnet run -c Release --project src/DiscordStreamNotifyBot.Notifier        # 單 shard（現行有功能的 app）
dotnet run -c Release --project src/DiscordStreamNotifyBot.Notifier -- 0 4 # [ShardId, TotalShards]
```

- 首次執行若無 `bot_config.json` 會產生 `bot_config_example.json` 後退出；必填 DiscordToken、WebHookUrl、GoogleApiKey、ApiServerDomain。
- **一律建置整個 solution，不要只建單一專案**（拆分後跨專案相依多，單建會漏編譯錯誤）。
- 自動化測試放在 `tests/`；驗證需跑 Release build、Release test，整合行為仍依計畫 §11 清單手動實測。

### 組態旗標（`#if` 改變行為，不是最佳化）

| 組態 | 行為 |
|------|------|
| `Release` | 完整功能；全球註冊 Slash 指令 |
| `Debug` | 登入 Discord，指令只註冊到 `TestSlashCommandGuildIds`（各 shard 只註冊自己持有的伺服器） |
| `Debug_DontRegisterCommand` | 略過指令註冊 |
| `Debug_API` | 單次 YouTube API 呼叫後立即返回 |

改程式碼時務必確認周圍的 `#if` 區塊；**正式行為只以 Release 為準**。

## EF Core 鐵則

```powershell
dotnet ef migrations add <Name> --project src/DiscordStreamNotifyBot.Shared
dotnet ef database update --project src/DiscordStreamNotifyBot.Shared    # 僅限本地/開發 DB
```

- **正式 DB 永遠不用 `database update` 直連**。每次 migration 必須產生兩份冪等 SQL：單次增量檔指定上一筆 migration 為 `from`、目標 migration 為 `to`，人工審核後手動於維護窗口執行；另產生涵蓋全部 migrations 的完整檔，供全新環境匯入：
  `dotnet ef migrations script 20260709091318_AddManualMemberCheckVideoFlag 20260719142803_AddTwitchBroadcasterAuthorization --idempotent --project src/DiscordStreamNotifyBot.Shared -o migrate_sql\20260719142803_AddTwitchBroadcasterAuthorization.sql`
  `dotnet ef migrations script --idempotent --project src/DiscordStreamNotifyBot.Shared -o migrate_sql\all.sql`
- 生成的搬遷 SQL 一律放在 `migrate_sql/`；單次增量檔以目標 migration 命名為 `<MigrationCreateTime>_<MigrationName>.sql`，完整檔固定命名為 `all.sql` 並於每次 migration 後更新。
- 正式 DB **已完成基線化**（`__EFMigrationsHistory` 存在，2026-06），且已套用至 claude 分支的 `SyncModelDrift`。**禁用 `EnsureCreated`**。
- 重構搬遷 DataBase/ 時，migration 檔**只能照搬、不可重新生成**（ID 必須對上正式 DB 歷史，詳見計畫 §9-2）。


## 架構要點（現行樹）

- Notifier 進入點 `Program.cs` → `Bot.cs`：init 設定/DB/Redis → DiscordSocketClient（手動 shard 參數）→ DI 後明確啟動 `AdminSettingsService` → 指令註冊 → 啟動 `NotificationBusConsumer`（消費 `bot:notify`）→ 阻塞至關閉。Scraper 進入點 `Program.cs` → `ScraperService`（搶 leader 鎖）→ `DetectionHost`（`Detection/` 偵測、publish DTO，不連 Discord）。
- 全域靜態狀態在 Shared 的 `BotState`（`Redis/RedisSub/RedisDb`、`DbService`、`IsConnect`、`ShardId/TotalShardCount`、`IsServerOnThisShard`/`ShouldDeleteMissingGuild`）；Notifier 的 `Bot` 靜態成員委派至此。
- **雙指令系統**（目錄結構對稱）：`Command/` = `s!` 前綴（擁有者/管理用）；`Interaction/` = Slash（一般使用者）。
- **DI 反射自動載入**：實作 `IInteractionService` / `ICommandService` 的類別自動註冊 Singleton（`Interaction|Command/Extensions.cs`），新增服務不需手動登記。
- DB：`MainDbService.GetDbContext()` 取短生命週期 context（`using var db = ...`），讀取一律 `.AsNoTracking()`。YouTube 影片兩表（Other/NonApproved）繼承 `Video`，依 videoId 查詢需依序探查兩表。
- 偵測與發送已拆分（計畫階段 3）：偵測（Timer/排程爬取/webhook 訂閱）在 **Scraper** `Detection/`，publish DTO 到 `bot:notify`；發送（`_client.GetGuild` + embed）在 **Notifier** `SharedService/`，消費匯流排後 `DispatchFromBusAsync` 重建 embed。跨層 DTO 在 `Shared/Messages/`。
- Twitch 偵測採雙模式：有效 broadcaster OAuth 由 Scraper 永久維持 `stream.online`/`channel.update`/`stream.offline` 三種 EventSub並低頻補償；未授權頻道維持 30 秒 polling、直播期間暫時 update/offline。授權失效時先以 Helix確認離線，直播中禁止刪 EventSub，離線後依 Shared guild snapshot/Notifier健康守衛決定保留或移除 spider；通知設定不隨 spider 自動刪除。
- **Coordinator**（階段 4）：`CoordinatorService` 心跳/leader 觀察/`TOTAL_SHARDS` 公告/`XINFO GROUPS` pending 監控，不負責重啟（交 Compose）。**跨 shard 指令**（階段 5，計畫 §7）：`Notifier/SharedService/Cluster/ClusterQueryService`（合併快照 + request-reply）+ `AdministrationService` 廣播；`OfficialGuildList` 存 Redis SET；狀態列計數走 `cluster:stats:*` HASH 彙總。部署見根目錄 `Dockerfile`/`docker-compose.yml`（方式 A）。
- Logging：Shared 的 Serilog pipeline 接管 console、非容器 general/error/stream 檔案與 Grafana Loki 主動推送；既有靜態 `Log` facade、四個低 cardinality labels 與有限時間 flush 契約維持不變。

## 外部契約（不可片面更改）

以下 Redis pub/sub 頻道名是與外部 repo 的跨專案契約（[YoutubeStreamRecord](https://github.com/konnokai/YoutubeStreamRecord) 錄影工具、[Discord-Stream-Bot-Backend](https://github.com/konnokai/Discord-Stream-Bot-Backend) webhook 接收端）：

| 分類 | 頻道 |
|------|------|
| YouTube | `youtube.startstream` `youtube.endstream` `youtube.addstream` `youtube.deletestream` `youtube.unarchived` `youtube.memberonly` `youtube.429error` `youtube.pubsub.{CreateOrUpdate,Deleted,NeedRegister}` |
| Twitch | `twitch.record` `twitch:stream_online` `twitch:channel_update` `twitch:stream_offline` `twitch:authorization_changed` |

本特規版已移除 TwitCasting（`twitcasting.*`）、會限（`member.revokeToken`）與 `youtube.record`（錄影委派）相關頻道。改名 = 破壞另外兩個 repo。`Auth/`（TokenManager，AES-CBC+HMAC，金鑰 `ProviderTokenEncryptionKey`）與後端共享，同屬契約；金鑰只由部署 secret 提供，不透過 Redis 傳輸。

## Conventions

- Log：靜態 `Log.Info/Warn/Error` facade（底層 Serilog）；例外一律 `.Demystify()` 後再記；輸出格式與 Loki patterns 見 [docs/LOGGING.md](docs/LOGGING.md)。
- JSON：`Newtonsoft.Json`（`JsonConvert`）；**不使用 `System.Text.Json`**。
- Global usings 已在 csproj 宣告：`Discord`、`Discord.WebSocket`、`Newtonsoft.Json`、`StackExchange.Redis`、`Microsoft.EntityFrameworkCore`、`System.Diagnostics`、`Google.Apis.YouTube.v3.Data`。
- Embed 顏色：`WithOkColor()`（綠）/ `WithErrorColor()`（深灰）/ `WithRecordColor()`（紅）。
- Discord.Net 維持最新穩定版，`Shared` 與 `Notifier` 的版本必須一致；升級後執行完整 Release build 與 test。
- Slash 權限使用 `DefaultMemberPermissions`，不使用已淘汰的 `DefaultPermission`。有 `RequireUserPermission` 的 method 必須標註相同權限；整組共用權限時 module 也必須標註。method metadata 用於契約與 review，Discord 實際只對頂層 application command 套用預設權限，子指令仍以 `RequireUserPermission` 作為執行期安全邊界。
- `DefaultMemberPermissions` 異動必須納入 `CommandSignature` 並更新 command contract snapshot，確保 shard 0 重新註冊。
- 風格遵循根目錄 `.editorconfig`。
- Commit：訊息繁中；多行訊息用 Bash tool 的 heredoc（`git commit -F - <<'EOF' … EOF`），**勿用 PowerShell here-string `@'…'@`**（Bash tool 是 POSIX sh，`@'` 會漏字進標題）。

## 指令文件

各指令的權威使用說明在 Notion：<https://konnokai.notion.site/a4fff40bd95c4bec9edca5b78cdd5d37>。本檔刻意不維護指令清單；查指令行為讀 `Interaction/`、`Command/` 模組與 `Data/HelpDescription.txt`。

## 制度條款

1. 架構或慣例變更，**同一個 commit** 更新 AGENTS.md 對應段落（尤其「目前狀態」）。
2. 本檔上限 **150 行**：要加新規則，先刪或合併一條舊的；長內容放 `docs/` 用連結引用。
3. 狀態判讀的信任順序：**工作樹 > git 歷史 > memory > 文件**。文件與程式碼矛盾時，以程式碼為準並回頭修文件。
4. 重構每完成一個階段：勾計畫 checkbox + commit + 更新本檔「目前狀態」。進度必須存在 repo，不存在任何 session 的記憶裡。

## 適用 Skills

- 本專案 skills（`.claude/skills/`）：`add-detection-platform`（新增平台/通知事件）、`debug-detection-bus`（偵測→匯流排→發送除錯）、`ef-migration-baseline`（EF 遷移/基線化）。
- 外掛 skills：`/migrate`（EF 變更後）、`/code-review`（重構/PR 後）、`/security-scan`（Auth 或加密變更時）、`/health-check`、`/de-sloppify`、`/checkpoint`。不適用：`/scaffold`、`/api-versioning`、`/aspire`。

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- Do not automatically run `graphify update .` after modifying code. Remind the user to run it manually to keep the graph current.
- When the user requests a commit, always include any changes under `graphify-out/` in that commit.
