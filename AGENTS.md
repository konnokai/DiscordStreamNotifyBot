# 直播小幫手 Bot 協作說明

這是 Discord 直播通知 Bot，使用 .NET 8、Discord.Net、MySQL／MariaDB、EF Core 與 Redis。支援 YouTube、Twitch、TwitCasting、CHZZK、YouTube 會員驗證、Twitch 訂閱驗證及錄影委派。一般使用者介面支援 `zh-TW`、`en-US`、`ja`；維護與營運訊息使用繁體中文。

## 建置與驗證

```powershell
dotnet build DiscordStreamNotifyBot.sln -c Release
dotnet test DiscordStreamNotifyBot.sln -c Release
```

- 修改後一律建置整個 solution，不要只建單一專案。
- 正式行為以 `Release` 為準。
- `Debug` 會登入 Discord，但只向 `TestSlashCommandGuildIds` 註冊指令。
- 自動化測試位於 `tests/DiscordStreamNotifyBot.Tests`。
- 外部 Discord、MySQL、Redis 與各直播平台整合無法由單元測試完整覆蓋；涉及整合流程時要明確標示未驗證部分。

### Component tests（Redis／MySQL）

需要真實 Redis／MySQL 的測試預設會被 skip。本機要實跑時，**只有在該機器存在 `tests/component.local.runsettings` 才加上 `--settings`**（該檔案在 `.gitignore` 內，不進版控，內容是該機器的服務位置與測試帳號）：

```powershell
if (Test-Path tests/component.local.runsettings) {
    dotnet test DiscordStreamNotifyBot.sln -c Release --settings tests/component.local.runsettings
} else {
    dotnet test DiscordStreamNotifyBot.sln -c Release   # 沒有本機設定，component tests 會 skip
}
```

- 這個檔案不是每個環境都有，不要當成固定步驟；不存在時硬加 `--settings` 會直接失敗（找不到設定檔），不是 skip。
- 檔案以 `RunConfiguration/EnvironmentVariables` 注入 `REDIS_COMPONENT_OPTION`（Redis component tests）與 `MYSQL_TEST_CONNECTION_STRING`（MySQL component tests）。
- Redis 必須指定 `defaultDatabase=2` 以上且該資料庫為空：fixture 會拒絕碰觸生產 DB 0 與 token DB 1，也拒絕覆寫既有資料。
- 有設定環境變數但服務不可連時，測試會以連線例外**失敗**（不是 skip）。
- MySQL component tests 會在該主機建立並刪除 `discord_stream_bot_component_<guid>` 測試資料庫，測試帳號需要 `CREATE`／`DROP` 權限；權限不足會出現 `Access denied for user ...`。
- 只跑其中一類時加 `--filter "FullyQualifiedName~Component.Redis"`（MySQL 用 `~Component.MySql`）。

## 執行方式

程式分成三個服務：

```powershell
dotnet run -c Release --project src/DiscordStreamNotifyBot.Coordinator
dotnet run -c Release --project src/DiscordStreamNotifyBot.Scraper
dotnet run -c Release --project src/DiscordStreamNotifyBot.Notifier
dotnet run -c Release --project src/DiscordStreamNotifyBot.Notifier -- <ShardId> <TotalShards>
```

- `Coordinator` 管理心跳、leader 與 shard 狀態。
- `Scraper` 偵測直播並發布通知事件，不連線 Discord。
- `Notifier` 連線 Discord、處理指令並消費通知事件。
- 三個服務共用 `bot_config.json`、MySQL 與 Redis。

## 設定與機密

- 第一次執行前，依 `bot_config_example.json` 建立 `bot_config.json`。
- `DiscordToken`、`WebHookUrl`、`GoogleApiKey` 與 `ApiServerDomain` 是目前啟動流程的必要設定。
- `ProviderTokenEncryptionKey` 至少 64 個字元，並與 Backend 使用相同值。
- 已加密資料存在時不可任意更換 `ProviderTokenEncryptionKey`。
- 不得提交 `bot_config.json`、`.env`、Token、API Key、Cookie 或 Webhook URL。
- `EnableGuildMembersIntent` 只有在 Discord Developer Portal 已啟用 Server Members Intent 時才可設為 `true`。

## 架構

- `src/DiscordStreamNotifyBot.Shared`：共用設定、資料庫、Redis、驗證、訊息 DTO 與 API service。
- `src/DiscordStreamNotifyBot.Scraper`：YouTube／Twitch／TwitCasting／CHZZK 偵測、leader lock 與通知事件發布。
- `src/DiscordStreamNotifyBot.Notifier`：Discord 連線、前綴指令、Slash 指令與通知發送。
- `src/DiscordStreamNotifyBot.Coordinator`：服務心跳、leader 與 Redis Streams pending 狀態監控。
- `Command/` 是 `s!` 前綴指令；`Interaction/` 是 Slash 指令。
- 實作 `IInteractionService` 或 `ICommandService` 的服務會由既有反射註冊流程加入 DI，不需另外建立註冊表。
- 共用跨服務 DTO 放在 `Shared/Messages/`，不要讓 Scraper 直接相依 Notifier 類別。
- 資料庫讀取使用短生命週期 `DbContext`；純查詢維持 `.AsNoTracking()`。

## 資料庫與 migration

MySQL schema 由本儲存庫管理。Backend 只映射既有資料表，不建立自己的 migration。

```powershell
dotnet ef migrations add <Name> --project src/DiscordStreamNotifyBot.Shared
dotnet ef migrations script --idempotent --project src/DiscordStreamNotifyBot.Shared -o migrate_sql\all.sql
```

- 新增 migration 後，同步更新 `migrate_sql/all.sql`。
- 既有 migration ID 是資料庫歷史的一部分，不可重新產生或改名。
- `database update` 只可用於本機或開發資料庫。正式環境使用審核過的 SQL。
- 禁止以 `EnsureCreated` 取代 migration。

## 跨專案契約

以下內容會影響 Backend、Frontend 或 StreamRecordTools，修改前必須搜尋所有使用端：

- Redis channel 名稱與 payload。
- 管理設定 request／reply JSON。
- OAuth token 加密格式與 `ProviderTokenEncryptionKey`。
- Discord guild、channel、user ID 的字串格式。

主要 Redis channel：

| 分類 | Channel |
|---|---|
| YouTube | `youtube.startstream`、`youtube.endstream`、`youtube.addstream`、`youtube.deletestream`、`youtube.unarchived`、`youtube.memberonly`、`youtube.record`、`youtube.429error`、`youtube.pubsub.*` |
| Twitch | `twitch.record`、`twitch:stream_online`、`twitch:channel_update`、`twitch:stream_offline`、`twitch:authorization_changed` |
| TwitCasting | `twitcasting.pubsub.startlive`、`twitcasting.record` |
| 會員驗證 | `member.revokeToken` |

## 程式慣例

- JSON 使用 `Newtonsoft.Json`，除非整個契約已明確改用其他序列化器。
- Log 使用既有 `Log.Info`、`Log.Warn`、`Log.Error` facade；例外先 `.Demystify()`。
- Discord embed 顏色沿用 `WithOkColor()`、`WithErrorColor()`、`WithRecordColor()`。
- Slash 權限同時保留 Discord command metadata 與執行期 `RequireUserPermission` 檢查。
- 修改 command metadata 時，同步更新 command contract snapshot。
- 遵循根目錄 `.editorconfig`。
- 程式碼註解、Log 與 commit 訊息使用繁體中文。

## 文件

- 公開安裝與部署方式寫在 `README.md`，內容必須能讓沒有既有環境的新使用者照著完成。
- 架構與跨專案契約見 `docs/`。
- 指令行為以 `Interaction/`、`Command/`、`Data/HelpDescription.txt` 與 [Notion 指令文件](https://konnokai.notion.site/a4fff40bd95c4bec9edca5b78cdd5d37) 為準。
- 架構、設定或公開部署流程變更時，同步更新相關文件，不要加入單次遷移日誌。

## graphify

- 程式碼問題先執行 `graphify query "<問題>"`。
- 關係查詢使用 `graphify path "<A>" "<B>"`，單一概念使用 `graphify explain "<概念>"`。
- 查詢不足時再讀 `graphify-out/wiki/index.md` 或 `graphify-out/GRAPH_REPORT.md`。
- 修改後不要自動執行 `graphify update .`；提醒使用者自行更新。
- `graphify-out/` 已列入 `.gitignore`，不進版控；提交時不需理會它的變更。
