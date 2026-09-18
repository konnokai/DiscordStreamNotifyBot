# 直播小幫手

[![邀請機器人](https://img.shields.io/badge/Discord-邀請機器人-5865F2)](https://discordapp.com/api/oauth2/authorize?client_id=758222559392432160&permissions=2416143425&scope=bot%20applications.commands)
[![網站](https://img.shields.io/website-up-down-green-red/https/stream-bot.konnokai.me.svg)](https://stream-bot.konnokai.me/)

直播小幫手會偵測 YouTube、Twitch、TwitCasting 與 CHZZK 直播，並將開台、更新及關台通知送到 Discord。這個儲存庫是 Bot 本體，包含直播偵測、Discord 通知、會員／訂閱驗證與叢集協調服務。

只想使用官方服務，可以直接點上方邀請連結，不需要自行部署。自行架設與開發方式請往下看。

## 服務組成

程式分成三個可獨立執行的服務：

- `DiscordStreamNotifyBot.Coordinator`：追蹤各服務與 Discord shard 的存活狀態。
- `DiscordStreamNotifyBot.Scraper`：偵測直播狀態，將通知事件寫入 Redis Streams。
- `DiscordStreamNotifyBot.Notifier`：連線 Discord，處理指令並送出通知。

三個服務共用 MySQL、Redis 與同一份 `bot_config.json`。

## 系統需求

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- MySQL 或 MariaDB
- Redis
- [Discord Application](https://discord.com/developers/applications) 的 Bot Token
- 已啟用 YouTube Data API v3 的 [Google API Key](https://console.cloud.google.com/apis/library/youtube.googleapis.com)
- Discord Webhook，用來接收程式紀錄
- 可公開連線的 Backend 主機名稱，供直播平台 webhook 使用
- 啟用對應功能時所需的 Google、Twitch 與 TwitCasting OAuth 應用程式

若要使用網站帳號連結功能，還需要部署 [DiscordStreamBotBackend](https://github.com/konnokai/DiscordStreamBotBackend) 與 [前端](https://github.com/konnokai/discord-stream-bot-frontend)。

## 第一次設定

1. 複製設定範例：

```powershell
Copy-Item bot_config_example.json bot_config.json
```

Linux 或 macOS：

```sh
cp bot_config_example.json bot_config.json
```

2. 編輯 `bot_config.json`。至少確認下列欄位：

| 欄位 | 說明 |
|---|---|
| `MySqlConnectionString` | Bot 使用的 MySQL 連線字串 |
| `RedisOption` | Redis 連線設定 |
| `ProviderTokenEncryptionKey` | 加密 OAuth token 的金鑰，至少 64 個字元；若有部署 Backend，兩邊必須相同 |
| `ApiServerDomain` | Backend 主機名稱，例如 `api.example.com`；不要加 `https://`、路徑或結尾 `/` |
| `DiscordToken` | Discord Bot Token |
| `WebHookUrl` | 接收程式紀錄的 Discord Webhook |
| `GoogleApiKey` | YouTube Data API v3 金鑰 |
| `GoogleClientId`、`GoogleClientSecret` | 啟用 YouTube 會員驗證時填入 |
| `TwitchClientId`、`TwitchClientSecret` | 啟用 Twitch 功能時填入 |
| `TwitCastingClientId`、`TwitCastingClientSecret` | 啟用 TwitCasting 功能時填入 |
| `TestSlashCommandGuildIds` | Debug 模式下註冊指令的測試伺服器 ID |

`bot_config.json` 可能包含機密，請勿提交到 Git。

3. 建立資料庫，並匯入完整 migration SQL：

```sh
mysql -u root -p discord_stream_bot < migrate_sql/all.sql
```

請依實際資料庫名稱與帳號調整指令。程式不會替正式環境自動建立或更新資料表。

## 本機建置與執行

先建置及測試整個 solution：

```powershell
dotnet build DiscordStreamNotifyBot.sln -c Release
dotnet test DiscordStreamNotifyBot.sln -c Release
```

單一 shard 環境要同時啟動三個服務。請分別在三個終端機執行：

```powershell
dotnet run -c Release --project src/DiscordStreamNotifyBot.Coordinator
dotnet run -c Release --project src/DiscordStreamNotifyBot.Scraper
dotnet run -c Release --project src/DiscordStreamNotifyBot.Notifier
```

多 shard 的 Notifier 啟動格式為：

```powershell
dotnet run -c Release --project src/DiscordStreamNotifyBot.Notifier -- <ShardId> <TotalShards>
```

正式環境請使用 `Release`。`Debug` 只會將 Slash 指令註冊到 `TestSlashCommandGuildIds`，方便開發時測試。

## Docker Compose 部署

Compose 只啟動 Bot 的三種服務，不會建立 MySQL、Redis 或資料表。

1. 準備可讓容器連線的 MySQL 與 Redis。
2. 建立並填好 `bot_config.json`。
3. 複製 `.env.example`：

```sh
cp .env.example .env
```

4. 確認 `.env` 的 `TOTAL_SHARDS` 與 `docker-compose.yml` 內的 `notifier-*` 服務數量相同。範例 Compose 目前提供兩個 shard。
5. 啟動服務：

```sh
docker compose up -d --build
docker compose logs -f
```

Linux 透過 Compose 的 `host-gateway` 使用 `host.docker.internal` 連回主機。若 MySQL 或 Redis 位於其他主機，請直接修改連線字串。

## 設定與安全提醒

- Discord Developer Portal 的 Bot 頁面必須啟用程式實際使用的 intents。
- `EnableGuildMembersIntent` 預設為 `false`。只有在 Developer Portal 已啟用 Server Members Intent 時才可開啟。
- `ProviderTokenEncryptionKey` 一旦用來加密 token 就不能任意更換，否則既有資料將無法解密。
- 不要提交 `bot_config.json`、`.env`、Token、API Key 或 Webhook URL。
- MySQL migration 由本儲存庫管理；Backend 不會另外建立相同資料表。
- 錄影委派需要另外部署 [StreamRecordTools](https://github.com/konnokai/StreamRecordTools)，並讓兩邊連線到相同的 Redis。

## CHZZK 錄影

CHZZK 錄影委派需要另外部署 [StreamRecordTools](https://github.com/konnokai/StreamRecordTools)，並讓兩邊連線到相同的 Redis。錄影端需要可執行 `streamlink` 且支援 CHZZK plugin。

- 在伺服器使用 `/chzzk-spider add` 新增爬蟲後，Bot 擁有者會收到私訊，附「切換自動錄影」按鈕。
- Bot 擁有者也可在私訊使用 `s!ChzzkAutoRecord <頻道網址或 ID> <on/off>` 設定，`s!ChzzkRecordList` 列出已開啟自動錄影的頻道。
- 需要補錄目前場次時使用 `s!ChzzkRecord <頻道網址或 ID>`。自動錄影只委派之後偵測到的新場次；已發過開台通知的場次不會補錄。
- `s!` 指令需要 Bot 擁有者權限，且只在私訊中有效。
- `s!ChzzkRecord` 送出請求不代表已經開始錄影，錄影端是否在線與 Streamlink 是否正常啟動需以錄影端紀錄為準。

## 相關文件

- [網站管理設定契約](docs/WEB_ADMIN_SETTINGS_PLAN.md)
- [測試說明](docs/TESTING_PLAN.md)
- [Log 與 Loki](docs/LOGGING.md)
- [CHZZK 通知設計](docs/CHZZK_NOTIFICATION_PLAN.md)
- [Discord 指令說明](https://konnokai.notion.site/a4fff40bd95c4bec9edca5b78cdd5d37)

## 授權

本專案採用 [MIT License](LICENSE.txt)。
