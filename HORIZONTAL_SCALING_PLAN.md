# 水平擴展（三層拆分）計畫

> 目標：把現在的單一程序拆成 **爬蟲層 (Scraper)**、**通知層 (Notifier, 可水平擴展的 shard)**、**主控層 (Coordinator)** 三種角色，
> 各自獨立專案、共用一個 library，使用 Docker Compose 部署。
>
> 決策（已確認）：
> - 主控層 = **輕量協調者 (Coordinator)**：不負責 `Process.Start`，由 Docker Compose 拉起與重啟；主控層只做心跳監控、leader 選舉、shard 分配/租約、叢集狀態回報。
> - 程式碼 = **拆成多個專案 + 共用 library**。

---

## 1. 最終架構

```
                         ┌──────────────────────────────┐
                         │  Coordinator (主控層, 1 個)    │
                         │  - 心跳監控 (Redis heartbeat)   │
                         │  - scraper leader 選舉/鎖       │
                         │  - shard id 租約分配            │
                         │  - 叢集狀態 (Uptime Kuma/log)   │
                         └──────────────┬───────────────┘
                                        │ Redis (狀態/租約/心跳)
        外部 API / 錄影程序              │
   YouTube / Twitch / TwitCasting       │
            │  ▲                        │
            ▼  │ youtube.record 等       │
   ┌────────────────────┐  notify 事件   │   ┌────────────────────────────┐
   │ Scraper (爬蟲層,1個) │ ── notify ──▶ RabbitMQ ─▶│ Notifier shard 0..N-1 (多個) │
   │ - 所有輪詢 Timer     │  bot.notify   │   │ - 連 Discord (ShardId/Total) │
   │ - 錄影程序 Redis 訂閱 │  (durable)    │   │ - slash / prefix 指令         │
   │ - PubSub 訂閱維護     │               │   │ - 只發送給自己持有的伺服器     │
   │ - 偵測開台/關台/改時間 │               │   │ - Banner/活動/會員身分組       │
   │ - 不連 Discord       │               │   └────────────────────────────┘
   └────────────────────┘
              共用 MySQL + Redis(控制平面/錄影IPC) + RabbitMQ(通知匯流排)
```

### 訊息中介（Hybrid：RabbitMQ + Redis）
| 用途 | 中介 | 原因 |
|---|---|---|
| 內部通知匯流排 `bot.notify.*`（scraper → notifier） | **RabbitMQ** | durable queue + 手動 ack，shard 重啟期間不漏開台通知（at-least-once） |
| 錄影程序 IPC（`youtube.startstream/endstream/record` 等） | **Redis pub/sub** | 與外部 repo `YoutubeStreamRecord` 的既有契約，不可單方面更改 |
| 主控層控制平面（心跳/leader 鎖/shard 租約/total_shards） | **Redis** | `SET NX EX` / TTL 鍵語義，非 RabbitMQ 用途 |

> 替代方案：若引入 RabbitMQ 的唯一動機是持久化，**Redis Streams**（consumer group + ack）也能達成且無須新增基礎設施；RabbitMQ 的額外價值在 routing / DLQ / 管理 UI / backpressure。本計畫採 RabbitMQ。

核心原則：
- **抓取與偵測是叢集唯一 (singleton)** → 只有一個 scraper，API quota 與 `youtube.record` 不被乘以 shard 數。
- **通知發送按 shard 分散** → 每個 notifier 只處理自己持有的伺服器；任何 `GetGuild == null` 的刪除動作都要先過 shard 歸屬判斷。
- **scraper 完全不碰 Discord gateway**；所有需要 `_client.GetGuild(...)` 的動作（送訊息、建活動、改 banner、給會員身分組）都移到 notifier，由 RabbitMQ 通知事件觸發。

---

## 2. 專案拆分 (Solution Layout)

```
DiscordStreamNotifyBot.sln
├─ src/
│  ├─ DiscordStreamNotifyBot.Shared/      (classlib)   ← 共用基礎
│  ├─ DiscordStreamNotifyBot.Scraper/     (exe)        ← 爬蟲層
│  ├─ DiscordStreamNotifyBot.Notifier/    (exe)        ← 通知層 (shard)
│  └─ DiscordStreamNotifyBot.Coordinator/ (exe)        ← 主控層
└─ docker-compose.yml
```

### 2.1 `DiscordStreamNotifyBot.Shared`（共用 library）
從現有專案搬移、不含任何 Discord gateway 邏輯的部分：

| 來源 | 內容 |
|---|---|
| `BotConfig.cs` | 設定（新增 `Role`、`TotalShards`、`HeartbeatIntervalSeconds`、`HeartbeatTtlSeconds`、`RabbitMQ` 等欄位，見 §3） |
| `DataBase/` + `Migrations/` | `MainDbContext`、`MainDbService`、所有 `Table/`、EF 遷移（**EF 工具指向此專案**） |
| `Auth/` | `TokenManager`、`TokenCrypto` |
| `RedisConnection.cs`、`RedisDataStore.cs` | Redis 連線 |
| `Log.cs`、`Utility.cs`（非 Discord 部分） | 共用工具 |
| `HttpClients/Twitcasting`、Twitch API client | 純 HTTP 抓取用 |
| **新增** `RedisChannels.cs` | 集中 Redis 頻道字串常數（錄影 IPC / 控制平面） |
| **新增** `RabbitMqService.cs` | RabbitMQ 連線封裝（原生 `RabbitMQ.Client`）：宣告 `bot.notify` topic exchange + DLX、publish、per-shard queue 消費 |
| **新增** `Messages/` DTO | 跨層通知事件（見 §4.1） |
| **新增** `StartupPreflight.cs` | 啟動連線檢查（MySQL/Redis/RabbitMQ 依角色探測 + 重試，見 §5.3） |
| **新增** `YoutubeApiService` | 抽出**無狀態** YouTube API：`GetChannelIdAsync`、`GetChannelTitle`、`GetVideoId`、`GetVideoAsync`、`PostSubscribeRequestAsync` |

> `YoutubeApiService` 是關鍵：指令層 `/youtube-spider add` 用到的 `GetChannelIdAsync`/`GetChannelTitle`/`PostSubscribeRequestAsync` 都只是 YouTube API / HTTP POST，不需要 Discord，也不需要 Timer。抽到共用後，notifier 的指令可直接呼叫，**不必跨層 Redis 請求**。

### 2.2 `DiscordStreamNotifyBot.Scraper`（爬蟲層）
- 從 `SharedService/` 搬入所有 **Timer 輪詢與偵測**：`YoutubeStreamService` 的 `holoSchedule/nijisanjiSchedule/otherSchedule/checkScheduleTime/reScheduleTime/subscribePubSub/channelTitleCheckTimer`、`Schedule.cs`、`ReminderAction.cs` 的偵測部分、`TwitchService`、`TwitcastingService`、`YoutubeMemberService` 的檢查 Timer。
- 註冊**錄影程序 Redis 訂閱**（`youtube.startstream/endstream/memberonly/deletestream/unarchived/429error/addstream/pubsub.*`、`twitch.*`、`twitcasting.pubsub.startlive`）。
- 偵測到要通知時，**不直接送 Discord**，改 publish `bot.notify.*` 事件到 RabbitMQ（見 §4）。
- 發 `youtube.record` 給錄影程序（維持現狀，但只有此單一程序會發）。
- 啟動前先向主控層取得 **scraper leader 鎖**（Redis `SET NX EX`）；拿不到就退出或待命。
- **不建立 `DiscordSocketClient`**。

### 2.3 `DiscordStreamNotifyBot.Notifier`（通知層 / shard）
- 建立 `DiscordSocketClient`（`ShardId`/`TotalShards` 來自啟動參數或主控層租約）。
- 載入 `Interaction/`（slash）+ `Command/`（prefix）指令系統（從現專案搬入）。
- 訂閱 RabbitMQ `bot.notify.*` 事件 → 重建 embed → 查 DB 通知設定 → **只發給自己持有的伺服器**。
- 持有 `_client.GetGuild(...)` 的所有動作：送訊息、Crosspost、建立/修改活動、`ChangeGuildBanner`、會員身分組授予。
- `JoinedGuild`/`LeftGuild`/`Ready` 的 `GuildConfig` 生命週期維護（只處理本 shard 的伺服器）。
- **刪除設定前一律加 shard 歸屬判斷**（見 §5）。
- 全域指令註冊維持 Redis `command_count` CAS 協調（只會有一個 notifier 實際註冊）。

### 2.4 `DiscordStreamNotifyBot.Coordinator`（主控層）
- 監控所有角色的 Redis 心跳鍵；逾時即記錄/告警（Uptime Kuma、Discord owner DM、log）。
- 維護 scraper leader 鎖的續租監控（leader 心跳停了就釋放鎖，讓重啟的 scraper 能接手）。
- **shard id 租約分配**（選用，支援 `--scale`）：notifier 開機向主控層要一個 `[0, TOTAL_SHARDS)` 內未被占用的 shard id；釋放/逾時回收。
- 寫入並公告 `TOTAL_SHARDS`（叢集真實來源）。
- 提供叢集狀態查詢（新增到 /utility status 指令內）。

### 2.5 SharedService 各服務 tech stack 與拆分歸屬

現況五個服務全混在同一 process。拆分原則：**外部抓取/偵測 → Scraper**、**Discord 發送/互動 → Notifier**、**無狀態 API/工具 → Shared**。

| 服務 | 外部 API / 協定 | 主要函式庫 | Redis 頻道（錄影 IPC） | 計時器 |
|---|---|---|---|---|
| **YoutubeStreamService** (`Youtube/`) | YouTube Data API v3（ApiKey）、PubSubHubbub（HTTP POST）、Nijisanji API、hololive 排程頁（HTML 抓取） | `Google.Apis.YouTube.v3`、`HtmlAgilityPack`、`Polly`、`Newtonsoft.Json`、`Regex` | `youtube.startstream/endstream/memberonly/deletestream/unarchived/429error/addstream/pubsub.*` | holo/niji/other/checkScheduleTime/reScheduleTime/subscribePubSub/channelTitleCheck |
| **TwitchService** (`Twitch/`) | Twitch Helix API、Twitch EventSub（webhook，secret 存 Redis `twitch:webhook_secret`） | `TwitchLib.Api`、`Dorssel.Utilities.Debounce`、`Polly`、`Regex` | `twitch.record`、`twitch:channel_update`、`twitch:stream_offline` | 主輪詢 + 移除驗證失敗 webhook |
| **TwitcastingService** (`Twitcasting/`) | TwitCasting API、TwitCasting webhook（JSON） | `TwitcastingClient`（typed HttpClient + Polly）、`Newtonsoft.Json` | `twitcasting.pubsub.startlive` | 刷新分類 + 刷新 webhook |
| **YoutubeMemberService** (`YoutubeMember/`) | YouTube Data API v3 + **Google OAuth2**（`youtube.force-ssl`）會限驗證 | `Google.Apis.Auth.OAuth2`、`RedisDataStore`(OAuth token)、`Auth/TokenManager`(AES+HMAC)、`Polly` | `member.revokeToken`、`member.syncRedisToken` | checkMemberShipOnlyVideoId/checkOld/checkNewMemberStatus |
| **EmojiService** (`EmojiService.cs`) | 僅 Discord（Application Emote） | `Discord.Net` | – | – |

拆分後各服務的落點：

- **YoutubeStreamService**：偵測/抓取/PubSub/錄影訂閱 → **Scraper**；`GetChannelId/GetChannelTitle/GetVideoId/PostSubscribeRequest` 等無狀態 API → **Shared (`YoutubeApiService`)**；`SendStreamMessageAsync`/`ChangeGuildBanner`/建立活動 → **Notifier**。
- **TwitchService**：EventSub 訂閱維護 + 偵測 + Debounce 彙整 → **Scraper**；`SendStreamMessageAsync` → **Notifier**。注意 `twitch:webhook_secret` 由 Scraper 維護。
- **TwitcastingService**：webhook/分類輪詢 + 偵測 → **Scraper**；通知發送 → **Notifier**。
- **YoutubeMemberService**：會員狀態檢查 Timer + OAuth token（`RedisDataStore`）+ revoke 訂閱 → **Scraper**；**身分組授予/移除（需 `GetGuild`）→ Notifier**（由 RabbitMQ `bot.notify.member` 觸發）。OAuth/Token 邏輯 → **Shared**。
- **EmojiService**：只用 Discord client → **Notifier**（emote 用於組 embed 的 message component）。

> 共通點：五者都用 `Discord.Net`(發送) + `EF Core`(MainDbContext) + `System.Threading.Timer` + `StackExchange.Redis`。拆分時 **Timer/外部抓取留 Scraper、`GetGuild`/送訊息歸 Notifier、無狀態 API 抽 Shared**，是判斷每段程式碼歸屬的準則。

---

## 3. 三層共用設定

`bot_config.json` 增加下列欄位，且**全部可用環境變數覆寫**（正式環境/Compose 用 `.env` 注入，敏感值不入 image）：

```jsonc
{
  // 既有: DiscordToken, GoogleApiKey, MySqlConnectionString, RedisOption, ApiServerDomain ...
  "Role": "scraper | notifier | coordinator",   // 由啟動參數/env 指定
  "TotalShards": 4,                              // 叢集 shard 總數
  "ShardId": 0,                                  // notifier 專用；用租約時可省略
  "HeartbeatIntervalSeconds": 10,
  "HeartbeatTtlSeconds": 30,
  "RabbitMQ": {                                  // 通知匯流排
    "HostName": "rabbitmq",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  }
}
```

環境變數覆寫對應（`BotConfig.InitBotConfig()` 載入後套用，env 優先）：

| 設定 | 環境變數 |
|---|---|
| `MySqlConnectionString` | `MYSQL_CONNECTION_STRING` |
| `RedisOption` | `REDIS_OPTION` |
| `RabbitMQ.HostName/Port/UserName/Password/VirtualHost` | `RABBITMQ_HOST` / `RABBITMQ_PORT` / `RABBITMQ_USER` / `RABBITMQ_PASSWORD` / `RABBITMQ_VHOST` |
| `Role` | `ROLE` |
| `TotalShards` | `TOTAL_SHARDS`（亦可由 notifier 啟動參數 `["id","total"]` 提供） |
| ShardId 分配模式 | `SHARD_ASSIGNMENT`（`fixed` 預設＝方式 A，用 command/`ShardId`；`lease`＝方式 B 向主控層領，見 §6.2.1） |
| `DiscordToken` / `GoogleApiKey` | `DISCORD_TOKEN` / `GOOGLE_API_KEY` |

> MySQL / Redis / RabbitMQ 為**外部獨立服務**，本專案不負責啟動它們，只透過上述連線設定連入（見 §6）。

---

## 4. 訊息契約（新增）

### 4.1 RabbitMQ 通知匯流排（scraper → notifier）

- **Exchange**：`bot.notify`，type = `topic`，durable。
- **Routing key**：`youtube` / `twitch` / `twitcasting` / `banner` / `member`。
- **Queue（每個 shard 一條）**：`notify.shard.{shardId}`，durable / quorum；綁定 `bot.notify` exchange（廣播 + 各自過濾，見 §4.3）。
- **訊息**：persistent，body = JSON（Newtonsoft）。
- **消費**：手動 ack；發送成功才 ack，失敗 nack/requeue 或進 DLQ（`bot.notify.dlx`）。
- **函式庫**：原生 `RabbitMQ.Client`（不引入 MassTransit/Wolverine）。

Payload 以 **結構化資料**傳遞（不要序列化 `Embed` 物件），由 notifier 端用既有的 `EmbedBuilderFactory` 重建 embed：

```csharp
// DiscordStreamNotifyBot.Shared/Messages/YoutubeNotification.cs
public class YoutubeNotification
{
    public NoticeType NoticeType { get; set; }         // 既有 enum 移到 Shared
    public string VideoId { get; set; }
    public string ChannelId { get; set; }
    public string ChannelTitle { get; set; }
    public string VideoTitle { get; set; }
    public DateTime ScheduledStartTime { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
    public bool IsMemberOnly { get; set; }
    public Video.YTChannelType ChannelType { get; set; }
}
```

### 4.2 Redis 控制平面與錄影 IPC（維持 Redis）

| Key / 頻道 | 型別 | 用途 |
|---|---|---|
| `cluster:scraper:leader` | string (SET NX EX) | scraper leader 鎖；持有者定期續租 |
| `cluster:heartbeat:{role}:{id}` | string EX | 各程序心跳，TTL = HeartbeatTtl |
| `cluster:shard:lease:{shardId}` | string EX | notifier shard 租約（用 `--scale` 時） |
| `cluster:total_shards` | string | TOTAL_SHARDS 公告 |
| `youtube.startstream` / `endstream` / `record` / `pubsub.*` … | pub/sub | **既有錄影程序 IPC，維持不變** |

### 4.3 路由方式（起步採「廣播 + 各自過濾」）

scraper 不需要知道任何通知設定：每則事件廣播給所有 `notify.shard.{id}` queue，各 notifier 收到後用 `(guildId>>22)%total` 過濾出本 shard 的伺服器再發送。
- 優點：scraper 與通知設定零耦合；shard 重啟期間訊息留在自己 durable queue，不漏。
- 代價：每則事件送到 N 個 shard（直播通知量級可忽略）。
- 之後可選優化：scraper 查設定算出 shard，改用 routing key `shard.{id}` 精準投遞。
- **注意**：per-shard 固定 queue 名稱需要穩定 shard id → 部署採 §6「方式 A 固定 shard 服務」，租約式 `--scale` 之後再上。

---

## 5. Shard 歸屬與生命週期

### 5.1 歸屬公式（判斷「該不該刪設定」的關鍵）
Discord 官方公式：`(guildId >> 22) % totalShards == shardId`。
notifier 在 `GetGuild == null` 時：
- 若 `(guildId >> 22) % totalShards != _shardId` → **不是我的伺服器，靜默略過**（別刪）。
- 若等於本 shard 且 `Ready` 後仍找不到 → 才是真的離開了，可刪。

需要修正的 5 處（目前都會無條件刪）：
- `SharedService/Youtube/ReminderAction.cs:370`
- `SharedService/Twitch/TwitchService.cs:574`
- `SharedService/Twitcasting/TwitcastingService.cs:215`
- `SharedService/YoutubeMember/CheckMemberShip.cs:95`
- `SharedService/Youtube/ChangeGuildBanner.cs:25`

### 5.2 心跳與重啟
- 每個程序每 `HeartbeatInterval` 秒寫 `cluster:heartbeat:{role}:{id}`（帶 TTL）。
- scraper 開機 `SET cluster:scraper:leader NX EX`；成功才啟動 Timer，並定期續租。死亡後鎖自動過期 → Compose 重啟的新 scraper 接手。
- 主控層只觀察與告警；**實際重啟交給 Compose `restart: unless-stopped`**。

### 5.3 啟動連線檢查 (Preflight)
任何角色在進入主邏輯**之前**，先依角色檢查所需外部服務可連線；失敗就帶清楚訊息結束（非零退出碼），交給 Compose `restart` 重試。Shared 提供 `StartupPreflight`。

各角色需檢查的項目：

| 角色 | MySQL | Redis | RabbitMQ | Discord |
|---|:---:|:---:|:---:|:---:|
| coordinator | – | ✅ | – | – |
| scraper | ✅ | ✅ | ✅ (publish) | – |
| notifier | ✅ | ✅ | ✅ (consume) | ✅ 登入 |

檢查內容與策略：
- **MySQL**：開連線執行 `db.Database.CanConnectAsync()`。
- **Redis**：`PING`（現有 `RedisConnection.Init` 已會 throw，包進統一流程並補 PING 驗證）。
- **RabbitMQ**：建立連線並宣告 `bot.notify` exchange / 對應 queue（順便完成 topology 初始化）。
- **重試 + fail-fast 並行**：每項以指數退避重試（例如最多 ~60 秒）；逾時仍失敗 → 記錄「哪個服務、host:port、原因」後 `Environment.Exit(非0)`，由 `restart: unless-stopped` 重來。避免無限卡死在啟動。
- **訊息要可診斷**：明確印出目標 `host:port` 與例外原因（最常見：bind 在 127.0.0.1 容器連不到、防火牆、密碼錯）。

```csharp
// DiscordStreamNotifyBot.Shared/StartupPreflight.cs（示意）
public static class StartupPreflight
{
    public static async Task EnsureAsync(BotRole role, BotConfig cfg, TimeSpan timeout)
    {
        var checks = new List<(string name, Func<Task> probe)>();
        if (role is Scraper or Notifier)
            checks.Add(("MySQL", () => ProbeMySqlAsync(cfg.MySqlConnectionString)));
        checks.Add(("Redis", () => ProbeRedisAsync(cfg.RedisOption)));        // 全角色
        if (role is Scraper or Notifier)
            checks.Add(("RabbitMQ", () => ProbeRabbitAsync(cfg.RabbitMQ)));
        // Discord 由 notifier 既有登入流程驗證

        foreach (var (name, probe) in checks)
            await RetryWithBackoffAsync(name, probe, timeout);   // 失敗 → throw → Main 印訊息後 Exit(1)
    }
}
```

> 持續性健康監控（非啟動時）由 §5.2 主控層 Redis 心跳負責（可選再加 Docker `healthcheck` 讀心跳鍵）；Preflight 只管「開機能不能連上」。

---

## 6. Docker Compose（示意）

> **MySQL / Redis / RabbitMQ 由各自獨立的 compose stack 運行**，本專案的 compose **只跑應用程式**（coordinator / scraper / notifier），連線資訊全部來自 `.env`，不在此 stack 內建這三個服務。

> 連線方式：**主機埠口映射**。MySQL/Redis/RabbitMQ 已 `ports:` 映射到主機（並與其他服務共用），本 stack 的容器透過 `host.docker.internal` 連回主機埠口，**不需要共用 Docker network**。

### 6.1 連線設定 `.env`（與 compose 同目錄）
```dotenv
# 外部基礎設施 = 主機埠口；容器內用 host.docker.internal 連回主機
MYSQL_CONNECTION_STRING=server=host.docker.internal;port=3306;database=stream_bot;user=bot;password=***
REDIS_OPTION=host.docker.internal:6379,password=***
RABBITMQ_HOST=host.docker.internal
RABBITMQ_PORT=5672
RABBITMQ_USER=bot
RABBITMQ_PASSWORD=***
RABBITMQ_VHOST=/

# 叢集
TOTAL_SHARDS=4
DISCORD_TOKEN=***
GOOGLE_API_KEY=***
```
> 埠口（3306/6379/5672）換成你主機實際映射的對外埠口。

### 6.2 應用程式 compose（`docker-compose.yml`，不含基礎設施）
```yaml
# 讓所有容器都能用 host.docker.internal 連回主機（Linux 必要；Docker Desktop 可省）
x-host: &host-gateway
  extra_hosts:
    - "host.docker.internal:host-gateway"

services:
  coordinator:
    build: { context: ., dockerfile: src/DiscordStreamNotifyBot.Coordinator/Dockerfile }
    restart: unless-stopped
    env_file: .env
    environment: { ROLE: coordinator }
    <<: *host-gateway

  scraper:
    build: { context: ., dockerfile: src/DiscordStreamNotifyBot.Scraper/Dockerfile }
    restart: unless-stopped
    env_file: .env
    environment: { ROLE: scraper }
    depends_on: [coordinator]
    <<: *host-gateway

  # 方式 A：固定 shard（簡單、Discord 友善；對應 per-shard queue notify.shard.{id}）
  # 每個 shard 一個服務、ShardId 寫死在 command；見 §6.2.1 為另一種 --scale 做法
  notifier-0:
    build: { context: ., dockerfile: src/DiscordStreamNotifyBot.Notifier/Dockerfile }
    command: ["0", "4"]          # [ShardId, TotalShards]
    restart: unless-stopped
    env_file: .env
    environment: { ROLE: notifier }
    depends_on: [coordinator]
    <<: *host-gateway
  notifier-1: { /* command: ["1","4"] ... */ }
  notifier-2: { /* command: ["2","4"] ... */ }
  notifier-3: { /* command: ["3","4"] ... */ }
```

> 應用程式各角色透過 `env_file` 讀 `.env`；`BotConfig` 需支援以這些環境變數覆寫 `bot_config.json`（連線字串、Redis、RabbitMQ、TotalShards、Token 等）。

### 6.2.1 方式 B：`--scale` + 主控層 shard 租約（彈性，需租約機制）

方式 A 每個 shard 要寫一個服務、ShardId 寫死；方式 B 改成**單一 `notifier` 服務用 `--scale` 拉多個 replica**，每個 replica 開機時向主控層**領一個尚未被占用的 ShardId**（不寫死 command）。

```yaml
  # 方式 B：單一服務 + --scale；ShardId 由主控層租約分配（不寫死 command）
  notifier:
    build: { context: ., dockerfile: src/DiscordStreamNotifyBot.Notifier/Dockerfile }
    restart: unless-stopped
    env_file: .env               # 內含 TOTAL_SHARDS=4
    environment:
      ROLE: notifier
      SHARD_ASSIGNMENT: lease     # 啟用租約模式（不指定固定 ShardId）
    depends_on: [coordinator]
    <<: *host-gateway
    # 注意：不可設 container_name / 固定 hostname，否則無法 --scale
```

啟動（replica 數 = `TOTAL_SHARDS`）：
```powershell
docker compose up -d coordinator scraper
docker compose up -d --scale notifier=4 notifier
```

**租約運作流程（每個 replica 開機）**
1. 讀 `TOTAL_SHARDS`（來自 `.env` / Redis `cluster:total_shards`）。
2. 對 `i = 0..TOTAL_SHARDS-1` 嘗試 `SET cluster:shard:lease:{i} {instanceId} NX EX <ttl>`，搶到第一個成功的就當自己的 ShardId。
3. 用該 ShardId 連 Discord、消費 RabbitMQ queue `notify.shard.{i}`。
4. 定期續租（renew TTL）；程序死亡 → 租約過期 → 重啟/其他 replica 可重新搶占同一 id。

**方式 B 的注意事項（務必看）**
- **replica 數應等於 `TOTAL_SHARDS`**：
  - `--scale < TOTAL_SHARDS` → 部分 shard 無人認領，那些 shard 的伺服器收不到通知，其 `notify.shard.{i}` queue 會持續堆積。
  - `--scale > TOTAL_SHARDS` → 多出的 replica 領不到租約，應**待命並重試**（log 提示），不要 crash loop。
- **`TOTAL_SHARDS` 仍是固定值**：租約只決定「誰拿哪個 id」，不改變總數。真要改 shard 總數＝改 `TOTAL_SHARDS` 並讓全體 notifier 重連（同方式 A 的規劃性維運）。
- **避免雙重認領（split-brain）**：租約 TTL 要明顯大於續租間隔（例如 TTL 30s / 續租 10s），防止 GC 暫停導致租約過期被別人搶走、造成兩個 replica 同 ShardId。Discord 對相同 ShardId 的重複 identify 會拒絕，可作為最後防線；但仍應靠租約避免。
- **queue 名稱穩定**：ShardId 在 replica 生命週期內固定，故 `notify.shard.{id}` 穩定；replica 重啟期間訊息留在 durable queue，不漏。
- **縮容**：降低 `--scale` 會讓被釋放的 shard 暫時離線（其 queue 堆積），等補回 replica 才消化——縮容＝該 shard 暫停服務，非「自動重新分配通知到其他 shard」。

**何時選哪個**
- **方式 A**：部署直觀、可獨立重啟/觀察單一 shard、不需要租約程式碼 → **建議初期採用**。
- **方式 B**：想用單一服務定義 + `--scale` 彈性調整 replica → 需先完成主控層租約（階段 4），成熟後再切。

### 6.3 連線注意事項
- **`host.docker.internal`**：Docker Desktop（Windows/Mac）內建可用；**Linux 需** compose 加 `extra_hosts: ["host.docker.internal:host-gateway"]`（Docker 20.10+），上方 `x-host` 錨點已統一帶上。
- 替代填法：也可直接填**主機區網 IP**（例如 `192.168.x.x`），效果相同。
- 確認外部服務的綁定位址：若 MySQL/Redis/RabbitMQ 只 bind `127.0.0.1`，容器將連不到；需 bind `0.0.0.0` 或主機 IP，且防火牆放行對應埠口。
- **安全**：埠口對外時務必設定強密碼／限制來源；RabbitMQ 管理介面（15672）勿對公網開放。

> Discord identify 需固定 `TotalShards`，改變總數需所有 shard 重連。初期建議 **方式 A（固定 shard 服務）**；待主控層租約（階段 4）成熟再切 **方式 B（§6.2.1）**。

---

## 7. 分階段實作步驟

> 每階段都應可獨立建置、可回滾。本 repo 無自動化測試，每階段以多程序手動實測驗證。

### 階段 0：建立 Solution 骨架
- 建 `src/` 四個專案 + 參考關係；`.sln` 納入。
- 暫時讓 Scraper/Notifier/Coordinator 都先能編譯（空殼）。

### 階段 1：抽出 `Shared` library
- 搬移 `DataBase/`(+Migrations)、`Auth/`、`BotConfig`（加 env 覆寫）、`Redis*`、`Log`、`Utility`(非 Discord)、HTTP clients。
- 新增 `RedisChannels` 常數、`YoutubeApiService`（無狀態 YouTube API/PubSub）、`StartupPreflight`（§5.3）。
- 三個 exe 的 `Main` 開頭一律 `await StartupPreflight.EnsureAsync(role, cfg, timeout)`，失敗印訊息後 `Exit(1)`。
- EF 遷移工具改指向 Shared 專案，驗證 `dotnet ef` 仍可運作。

### 階段 2：通知層 (Notifier) 上線（先維持單 shard 行為）
- 把 `Bot.cs` 的 Discord 連線、指令系統、`Interaction/`、`Command/` 搬入 Notifier。
- 指令改用 `Shared.YoutubeApiService`。
- **先在此加上 §5.1 的 shard 歸屬守衛**（止血，立即讓多 shard 不互刪資料）。
- 此時 Notifier 仍可暫時內含偵測 Timer，確保功能不中斷。

### 階段 3：爬蟲層 (Scraper) 拆出 + RabbitMQ 通知匯流排
- 在 Shared 新增 RabbitMQ 連線封裝（`RabbitMqService`：宣告 `bot.notify` topic exchange / DLX、publish helper、per-shard queue 消費 helper），用原生 `RabbitMQ.Client`。
- 把偵測 Timer、錄影 Redis 訂閱、PubSub 維護搬到 Scraper。
- 偵測端改 publish 結構化 DTO 到 `bot.notify`（§4.1），移除直接 Discord 呼叫。
- Notifier 宣告/消費 `notify.shard.{id}` queue → 重建 embed → 發送（含 banner/活動/會員事件）；成功才 ack。
- 移除 Notifier 內殘留的偵測 Timer。
- 加 scraper leader 鎖（Redis）。

### 階段 4：主控層 (Coordinator)
- 心跳監控 + leader 續租觀察 + `cluster:total_shards` 公告。
- （選用）shard id 租約分配，支援 `--scale`。
- 叢集狀態輸出（Uptime Kuma / owner DM / log）。

### 階段 5：跨 shard 共享狀態
- `Utility.OfficialGuildList` 改存 Redis（解決 `Program.cs:41` TODO），由 scraper 維護、notifier 讀。
- 狀態列伺服器/成員總數：各 notifier 把計數寫 Redis，由其中一個彙總顯示（或維持各自顯示）。

### 階段 6：Docker 化與部署驗證
- 各 exe 專案加 Dockerfile；撰寫 `docker-compose.yml`（方式 A）。
- 驗證清單：
  - [ ] 同一則開台通知只發一次
  - [ ] YouTube/Twitch API quota 不隨 shard 數成長
  - [ ] 跨 shard 不再互刪通知設定
  - [ ] 重啟單一 notifier 不影響其他 shard
  - [ ] 殺掉 scraper 後 Compose 重啟可自動接手 leader
  - [ ] 錄影請求 `youtube.record` 只發一次
  - [ ] 重啟某個 notifier 期間的開台通知，重啟後仍從 RabbitMQ queue 補送（不漏）

---

## 8. 主要風險與取捨

- **Embed 跨層**：一律傳結構化資料、在 notifier 端用 `EmbedBuilderFactory` 重建，避免序列化 `Embed`。
- **指令的跨層副作用**：新增/移除爬蟲頻道的 PubSub 註冊是無狀態 HTTP，notifier 可直接做；**持續性的重新訂閱 Timer** 留在 scraper。少數真需要跨層的（例如要求 scraper 立即重掃）再用 Redis request/response。
- **scraper 單點**：狀態都在 DB/Redis，重啟即恢復；leader 鎖確保同時只有一個在跑。初期可接受短暫中斷，不需要熱備。
- **多一個基礎設施 (RabbitMQ)**：運維面新增一個需監控的元件。換來通知 at-least-once；若想避免新增元件，可改用 Redis Streams（§1 替代方案）。
- **at-least-once → 可能重複**：RabbitMQ 重送可能造成同一通知發兩次。notifier 端送出後可用 Redis 短期去重鍵（如 `notified:{videoId}:{noticeType}` EX 數分鐘）防重，沿用既有 `_endLiveBag` 的概念。
- **RabbitMQ 不可用時**：scraper publish 應有重試 + 本機暫存退化策略；queue 為 durable，broker 重啟後訊息不失。
- **TotalShards 變更成本**：改變 shard 總數需所有 notifier 重連並重算歸屬，屬於規劃性維運，非日常操作。
- **無自動化測試**：依賴多程序手動驗證，建議每階段保留可回滾的 commit。

---

## 9. 最低風險的先行項

若想先得到立即效益而不動大架構：**先單獨做階段 2 的「shard 歸屬守衛」（§5.1 的 5 處修正）**。這不改變單 shard 行為，但能立刻消除「多 shard 互刪設定」的災難，可作為獨立 PR 先合併。

---

## 10. 正式環境啟動與維運

### 10.1 前置基礎設施（外部、各自獨立運行）
- MySQL、Redis、RabbitMQ 由**各自獨立的 compose stack** 運行；本專案 compose 不含這三者，僅透過 `.env` 連入（見 §6）。
- 連線方式為**主機埠口映射**：容器透過 `host.docker.internal`（或主機 IP）連回主機埠口（§6）。先確認三者已啟動、bind 位址容器可達、防火牆放行。
- 錄影程序 [YoutubeStreamRecord](https://github.com/konnokai/YoutubeStreamRecord) 為獨立程式，靠 Redis 與 scraper 溝通；未啟動時 bot 仍可運作，僅缺實時偵測/錄影。

### 10.2 設定
- 所有角色共用同一份設定，連線資訊集中在 `.env`（§6.1）；**各角色差異只有 `ROLE` 與 notifier 的 `ShardId`**。
- 敏感值（DiscordToken / GoogleApiKey / 連線字串 / RabbitMQ 密碼）以 `.env` 環境變數注入，勿烤進 image。
- `.env` 加入 `.gitignore`，勿提交。

### 10.3 首次資料庫初始化（只做一次）
```powershell
dotnet ef database update --project src/DiscordStreamNotifyBot.Shared
```
> 拆分後不再由「shard 0 EnsureCreated」負責；改由此一次性遷移步驟避免多程序競爭建表。

### 10.4 啟動（Docker Compose 主路徑）
```powershell
# 前提：外部 MySQL/Redis/RabbitMQ 已啟動，且 .env 連線資訊正確
docker compose build          # 首次建置 image
docker compose up -d          # 啟動應用程式叢集（背景）
```
- 本 stack 只啟動應用程式；`depends_on` 順序：coordinator → scraper → notifier-0..N-1（外部基礎設施不在本 stack，請先確保可連線）。
- **shard 總數**＝ compose 內 `notifier-N` 服務數量；每個 `command: ["id","total"]` 的 `total` 必須一致、`id` 由 0 連續至 total-1。

### 10.5 啟動後健康驗證
- `docker compose ps`：全部 Up / healthy。
- `docker compose logs -f scraper`：Timer 開始輪詢、已建立 Redis 訂閱、取得 scraper leader 鎖。
- `docker compose logs -f notifier-0`：Discord 登入成功、開始消費 `notify.shard.0`。
- RabbitMQ 管理介面（`:15672`）：見 `bot.notify` exchange 與 `notify.shard.*` queue，無大量堆積。
- Coordinator log：所有角色心跳正常、無缺漏 shard。

### 10.6 常用維運操作
```powershell
docker compose logs -f notifier-1            # 單一 shard log
docker compose restart notifier-2            # 重啟單一 shard（期間通知留 queue 補送）
docker compose pull && docker compose up -d  # 更新版本滾動重啟
docker compose down                          # 停整個叢集
```

### 10.7 正式環境鐵則
1. **scraper 唯一**：勿設 replicas>1（leader 鎖會擋，但語意上就該唯一）。
2. **TotalShards 全叢集一致**，且等於 Redis `cluster:total_shards`；變更屬規劃性維運（需所有 notifier 重連）。
3. `restart: unless-stopped`：程序掛掉自動重啟；scraper 掛掉後新實例自動接手 leader。
4. RabbitMQ queue 用 durable/quorum：broker 或 notifier 重啟不漏通知；at-least-once 可能重複，靠 notifier 端短期去重鍵防重。

---

## 11. Review 補強：待修正的正確性問題

> 這些不是「可以更好」，而是不處理會在正式環境出錯，**應併入對應階段**。

1. **SIGTERM 優雅關閉（高）**：現況 `Console.CancelKeyPress` 只攔 SIGINT(Ctrl+C)；但 `docker stop` 送 **SIGTERM**，導致容器停止時 `SaveDateBase()` / `RedisSub.UnsubscribeAll()` 等清理**不會執行**，可能遺失未存檔資料。
   → 改用 .NET Generic Host（§12.1）原生處理 SIGTERM，以 `IHostApplicationLifetime` + `CancellationToken` 做優雅關閉；取代現有 `while(!IsDisconnect)` 輪詢與 `IsHoloChannelSpider` 等靜態旗標。
2. **EF 初始化與既有 DB 衝突（高）**：現有資料庫由 `EnsureCreated()` 建立，**沒有** `__EFMigrationsHistory`；直接 `dotnet ef database update`（§10.3）會與既存表衝突。
   → 一次性遷移流程：建立 initial migration → 用 `migrations add` 後以 `--no-build` 或手動將 baseline 標記為已套用（`dotnet ef migrations script` 比對 / 或 `INSERT __EFMigrationsHistory`），確認既有資料不被重建。**正式環境務必先備份**。
3. ~~**方式 A 縮容後孤兒 queue（中）**~~ **（已修正）**：`notify.shard.{id}` 為 durable，若直接刪除某 notifier 服務，該 queue 無人消費會無限堆積。
   → `RabbitMqService.ConsumeShardQueueAsync` 宣告時已加 `x-message-ttl`（1h，過期訊息丟棄）+ `x-expires`（24h 閒置自動刪 queue）；值均大於正常重啟窗口，確保 at-least-once 不被誤刪。**注意**：既有 queue 變更參數會 PRECONDITION_FAILED，需先刪 queue（pre-production 直接生效）。

> 已修正：§2.4 租約範圍改為半開區間 `[0, TOTAL_SHARDS)`；§2.1/§3 心跳欄位命名統一為 `HeartbeatIntervalSeconds` / `HeartbeatTtlSeconds`；§11-1 SIGTERM（GracefulShutdown）、§11-2 EF baseline、§11-3 孤兒 queue TTL 皆已完成。

---

## 12. 可優化項目（與重構一併或之後進行）

> 建議優先序：**12.1 / 12.2 重構時一併做**（順手且解掉 §11 問題）；**12.5 隨階段 3 做**；其餘可後續迭代。

### 12.1 排程與生命週期：Generic Host + PeriodicTimer +（必要時）cron — 回應「Timer → cronjob」
現況 `System.Threading.Timer` + 靜態旗標（`isRuning`/`isSubscribing`）混雜，重入要自己擋，且關閉處理不完整（§11-1）。重構為三層較清楚：

| 排程性質 | 例子 | 建議做法 |
|---|---|---|
| 固定間隔輪詢 | holo/niji/other(5min)、saveDb(3min)、subscribePubSub(30min) | `BackgroundService` + **`PeriodicTimer`**（await 友善、無重入、吃 `CancellationToken`，省掉 `isRuning` 旗標） |
| **日曆型排程** | 每日 00:00 頻道名稱檢查（現手算 `nextMidnight-now`） | **真正的 cron**：`Cronos`（輕量 cron 解析）或 `Quartz.NET`，比手算穩、處理重啟/DST |
| 一次性到點提醒 | `StartReminder` 每場直播一個 Timer | 維持「掃描即將到點清單」的 PeriodicTimer，或用 **Quartz 持久化 job** 取代，省掉重啟後 `ReScheduleReminder` 手動重排 |

推薦：**Generic Host + BackgroundService + PeriodicTimer**（低依賴、解 SIGTERM）；只有日曆型/持久化排程才上 **Quartz.NET**（其 clustering 也能當 scraper 單例保險，但與 leader 鎖重疊，非必要）。**不要把固定間隔輪詢硬改成 cron**——cron 的價值在「特定時刻」，間隔輪詢用 PeriodicTimer 更合適。

### 12.2 設定：改用 `Microsoft.Extensions.Configuration`
採 Generic Host 後，用內建 configuration 分層（json + 環境變數 + 命令列），env 以 `RabbitMQ__HostName` 自動綁定 → **可刪掉 §3 手寫的 env 覆寫對應程式碼**，少維護一份對照。

### 12.3 通知層：notice 設定記憶體快取
廣播 + 各自過濾下，每則事件 × N shard 都查一次 `NoticeYoutubeStreamChannel`。notifier 端做記憶體快取（定期刷新，或用 Redis pub/sub 發「設定已變更」失效通知），大幅降 MySQL 壓力。或改採 §4.3 的 shard-routed 路由，從源頭減少 fan-out。

### 12.4 YouTube quota：批次查詢
scraper 是唯一 quota 消費者，更要省。偵測/提醒路徑盡量用 `GetVideosAsync`（一次 50 筆）取代逐支 `GetVideoAsync`；錄影 pub/sub handler 若會連續處理多支也可彙整批次。

### 12.5 RabbitMQ 可靠性與吞吐（隨階段 3）
- ~~**Publisher confirms**~~ **（已完成）**：`RabbitMqService.InitializeAsync` 以 `CreateChannelOptions(publisherConfirmationsEnabled, publisherConfirmationTrackingEnabled)` 建立發布 channel，`BasicPublishAsync` 會等 broker 確認落地，未確認即丟例外（呼叫端 try/catch 記錄）。
- ~~**BasicQos prefetch**~~ **（已完成）**：`ConsumeShardQueueAsync` 以 `BasicQosAsync(prefetchCount: 20)` 限制同時處理量。
- **shard-routed routing key**：scraper 算出目標 shard 精準投遞，免廣播（§4.3 後續優化，尚未做）。

### 12.6 建置：單一 image 多角色
三個 exe 可用單一 multi-stage Dockerfile 產出**一個 image**，entrypoint 依 `ROLE` 選執行檔；省 build 時間/儲存、簡化 CI（取代 §6 的三個 Dockerfile）。

### 12.7 可觀測性：結構化日誌 + 角色/shard 標籤 **（已完成）**
`Log.RolePrefix` 由各程序啟動時設定（`scraper` / `notifier:{shardId}` / `coordinator`），於時間戳後輸出 `[{role}]` 標籤（console + 檔案），跨程序追蹤更容易。後續可再導入 Serilog + 集中 sink。

### 12.8 EF Core：Pooled DbContextFactory **（已完成）**
`MainDbService` 內改用 `PooledDbContextFactory<MainDbContext>`（`GetDbContext()` 由池取得、Dispose 歸還），降低 scraper/notifier 大量短生命週期 context 的配置成本。36 處皆 `using var db = GetDbContext()` 短用即還，且讀取普遍 `AsNoTracking`，契合物件池。
