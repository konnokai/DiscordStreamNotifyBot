# 水平擴展（三層拆分）計畫 — Redis Streams 版

> 目標：把現在的單一程序拆成 **爬蟲層 (Scraper)**、**通知層 (Notifier, 可水平擴展的 shard)**、**主控層 (Coordinator)** 三種角色，各自獨立專案、共用一個 library，使用 Docker Compose 部署。
>
> 決策（使用者已確認，2026-07-07，**不要重新辯論**）：
> - 主控層 = **輕量協調者**：不負責 `Process.Start`，由 Docker Compose 拉起與重啟；只做心跳監控、leader 選舉、shard 分配/租約、叢集狀態回報。
> - 程式碼 = 拆成多個專案 + 共用 library。
> - Scraper → Notifier 匯流排 = **Redis Streams**（不引入 RabbitMQ 等額外套件與服務）。
> - **從 master 重做**；`claude` 分支上的 RabbitMQ 版完整實作**只作參考、永不合併**。
>
> 本文件是重做的權威設計。各階段的「參考實作」欄位指向 `claude` 分支檔案，用
> `git show claude:<路徑>` 閱讀。收割它的判斷與程式碼片段，但匯流排層一律改為本文件 §4 的 Redis Streams 契約。

---

## 1. 目標架構

```
                         ┌──────────────────────────────┐
                         │  Coordinator (主控層, 1 個)    │
                         │  - 心跳監控 (Redis heartbeat)   │
                         │  - scraper leader 鎖觀察        │
                         │  - shard id 租約分配 (選用)      │
                         │  - 叢集狀態回報 (Uptime Kuma/log)│
                         └──────────────┬───────────────┘
                                        │ Redis (心跳/鎖/租約/狀態)
        外部 API / 錄影程序              │
   YouTube / Twitch / TwitCasting       │
            │  ▲                        ▼
            ▼  │ youtube.record 等
   ┌────────────────────┐   XADD bot:notify    ┌────────────────────────────┐
   │ Scraper (爬蟲層,1個) │ ──────────────────▶ │ Notifier shard 0..N-1 (多個) │
   │ - 所有輪詢 Timer     │   Redis Stream      │ - 連 Discord (ShardId/Total) │
   │ - 錄影程序 Redis 訂閱 │   (MAXLEN 修剪)     │ - slash / prefix 指令         │
   │ - PubSub/EventSub    │   consumer group    │ - 只發給自己持有的伺服器       │
   │ - 偵測開台/關台/改時間 │   per shard         │ - Banner/活動/會限身分組       │
   │ - 不連 Discord       │                     └────────────────────────────┘
   └────────────────────┘
                    共用 MySQL + Redis（匯流排 / 控制平面 / 錄影 IPC）
```

### 訊息中介（全 Redis，零新套件）

| 用途 | 機制 | 原因 |
|---|---|---|
| 內部通知匯流排（scraper → notifier） | **Redis Streams**（`XADD` + consumer group + `XACK`） | at-least-once、shard 重啟期間不漏通知；StackExchange.Redis 內建，不加套件不加服務 |
| 錄影程序 IPC（`youtube.startstream/record` 等） | Redis pub/sub | 與外部 repo `YoutubeStreamRecord` 的既有契約，**不可片面更改** |
| 主控層控制平面（心跳/leader 鎖/shard 租約） | Redis `SET NX EX` / TTL 鍵 | 天然的鎖與租約語義 |
| 跨 shard 指令（§7） | Redis pub/sub + HASH | 廣播與快照用途，不需持久化 |

核心原則：
- **抓取與偵測是叢集唯一 (singleton)**：只有一個 scraper，API quota 與 `youtube.record` 不被乘以 shard 數。
- **通知發送按 shard 分散**：每個 notifier 只處理 `(guildId >> 22) % totalShards == shardId` 的伺服器。
- **Scraper 完全不碰 Discord gateway**：所有需要 `_client.GetGuild(...)` 的動作（送訊息、建活動、改 banner、給會限身分組）都在 notifier。
- **會限（YoutubeMemberService）不走匯流排**：會限檢查經 shard 守衛後天然按 shard 分區（各 shard 只檢查自己伺服器的成員，OAuth quota 自動分攤），身分組操作是 REST 不綁 gateway，且多種檢查結果各對應不同 log/私訊內容，DTO 化高風險零收益。

---

## 2. 專案拆分 (Solution Layout)

```
DiscordStreamNotifyBot.sln
├─ src/
│  ├─ DiscordStreamNotifyBot.Shared/      (classlib)   共用基礎
│  ├─ DiscordStreamNotifyBot.Scraper/     (exe)        爬蟲層
│  ├─ DiscordStreamNotifyBot.Notifier/    (exe)        通知層 (shard)
│  └─ DiscordStreamNotifyBot.Coordinator/ (exe)        主控層
├─ Dockerfile          （單一 multi-stage image，$ROLE 選執行檔，見 §6）
└─ docker-compose.yml
```

**參考規則（無循環/交叉參考）**：Notifier、Scraper、Coordinator 皆只參考 Shared。角色由執行檔決定（各 Program.cs 寫死 `BotRole`），沒有模式旗標。

### 2.1 `Shared`（共用 library）

從現有單一專案搬移、不含任何 Discord gateway 邏輯的部分：

| 來源 | 內容 |
|---|---|
| `BotConfig.cs` | 設定（新增 `TotalShards`、心跳欄位等，全部可 env 覆寫，見 §3） |
| `DataBase/` | `MainDbContext`、`MainDbService`、所有 `Table/`；**Migrations/ 整批照搬 claude 分支**（見 §9-2，不可重新生成） |
| `Auth/` | `TokenManager`、`TokenCrypto` |
| `RedisConnection.cs`、`RedisDataStore.cs` | Redis 連線 |
| `Log.cs`、`Utility.cs`（非 Discord 部分） | 共用工具 |
| `HttpClients/` | TwitCasting / Twitch 純 HTTP client |
| 新增 `RedisChannels.cs` | 集中所有 Redis 頻道/鍵字串常數（錄影 IPC / 控制平面 / 跨 shard） |
| 新增 `NotificationBus`（publisher + consumer helper） | Redis Streams 匯流排封裝（§4） |
| 新增 `Messages/` DTO | 跨層通知事件（§4.2） |
| 新增 `StartupPreflight.cs` | 啟動連線檢查（依角色探測 + 指數退避重試，失敗 `Exit(1)` 交給 Compose restart） |
| 新增 `GracefulShutdown.cs` | SIGTERM/SIGINT 統一處理，提供 `CancellationToken`（§9-1） |
| 新增 `YoutubeApiService` / `TwitchApiService` | **無狀態** API（`GetChannelIdAsync`、`GetVideoAsync`、`PostSubscribeRequestAsync` 等），指令層直接呼叫、不必跨層請求 |
| 新增 `BotState.cs` | 原 `Bot` 靜態成員的共用落點（含 `IsServerOnThisShard`） |

參考實作（照搬程度高）：`git show claude:src/DiscordStreamNotifyBot.Shared/RedisChannels.cs`、`.../YoutubeApiService.cs`、`.../StartupPreflight.cs`（RabbitMQ 檢查段改 §4.4）、`.../GracefulShutdown.cs`、`.../BotState.cs`、`.../DataBase/MainDbContextFactory.cs`。

### 2.2 `Scraper`（爬蟲層，叢集唯一）

- 搬入所有 **Timer 輪詢與偵測**：YouTube 排程爬取（holo/niji/other/checkSchedule/reSchedule/subscribePubSub/channelTitleCheck）、Twitch 輪詢與 EventSub 維護、TwitCasting 輪詢與 webhook。
- 註冊**錄影程序 Redis 訂閱**（`youtube.*`、`twitch.*`、`twitcasting.pubsub.startlive`）。
- 偵測到事件 → **publish 結構化 DTO 到 `bot:notify` stream**（§4），不直接碰 Discord。
- 發 `youtube.record` 給錄影程序（僅此單一程序會發）。
- 啟動先搶 **scraper leader 鎖**（`SET cluster:scraper:leader NX EX`），拿不到就待命重試。
- **不建立 `DiscordSocketClient`**。
- 訂閱 `youtube.control.*`（Notifier 端少數 owner 控制指令：切換錄影/強制 SubscribePubSub/手動 AddVideo）。

參考實作：`git show claude:src/DiscordStreamNotifyBot.Scraper/Detection/Youtube/YoutubeDetectionService.cs`（+ `.Schedule` / `.Reminder` partial）、`.../Twitch/TwitchDetectionService.cs`（含 Debounce/）、`.../Twitcasting/TwitcastingDetectionService.cs`。

### 2.3 `Notifier`（通知層 / shard，可多個）

- 建立 `DiscordSocketClient`（`ShardId`/`TotalShards` 來自啟動參數或租約）。
- 載入 `Interaction/`（slash）+ `Command/`（prefix）指令系統。
- 消費 `bot:notify` stream 自己的 consumer group → `EmbedBuilderFactory` 重建 embed → 查 DB 通知設定 → **只發給自己持有的伺服器**，成功才 `XACK`。
- 持有所有 `GetGuild` 動作：送訊息、Crosspost、建活動、換 banner、會限身分組。
- `JoinedGuild`/`LeftGuild`/`Ready` 的 `GuildConfig` 維護（只處理本 shard）。
- **刪設定前一律過 shard 歸屬守衛**（§5.1）。
- 全域 slash 指令註冊維持 Redis `command_count` CAS（只有一個 notifier 實際註冊）。
- 會限檢查（YoutubeMemberService）按 shard 分區自行執行，不走匯流排。

參考實作：`git show claude:src/DiscordStreamNotifyBot.Notifier/NotificationBusConsumer.cs`（消費語意；傳輸層改 streams）、`.../SharedService/Youtube/EmbedBuilderFactory.cs`、`.../SharedService/NoticeCache.cs`。

### 2.4 `Coordinator`（主控層，1 個）

- 監控所有角色的心跳鍵，逾時記錄/告警（Uptime Kuma、owner DM、log）。
- 觀察 scraper leader 鎖續租；鎖過期即記錄（實際重啟交給 Compose）。
- 監控 `bot:notify` 各 group 的 pending 數（`XPENDING`），堆積異常告警（取代 RabbitMQ 管理 UI 的 queue 深度監控）。
- （選用，後期）shard id 租約分配，支援 `--scale`（§6.2）。
- 寫入/公告 `cluster:total_shards`。

參考實作：`git show claude:src/DiscordStreamNotifyBot.Coordinator/CoordinatorService.cs`。

### 2.5 SharedService 逐服務拆分歸屬（判斷準則表）

判斷每段程式碼落點的準則：**Timer/外部抓取 → Scraper；`GetGuild`/發送 → Notifier；無狀態 API/工具 → Shared**。

| 現有服務 | → Scraper（偵測） | → Notifier（發送/互動） | → Shared（無狀態） |
|---|---|---|---|
| `YoutubeStreamService` | 排程爬取 Timer 群、Redis 錄影訂閱、PubSubHubbub 維護、reminder 偵測 | `SendStreamMessageAsync`、換 banner、建活動 | `YoutubeApiService`（GetChannelId/GetVideo/PostSubscribeRequest 等） |
| `TwitchService` | EventSub 維護 + 輪詢 + Debounce 彙整（`twitch:webhook_secret` 由 Scraper 維護） | `SendStreamMessageAsync` | `TwitchApiService` |
| `TwitcastingService` | webhook/分類輪詢 + 偵測 | 通知發送 | – |
| `YoutubeMemberService` | –（整組留 Notifier） | 會限檢查 Timer + 身分組授予/移除（按 shard 分區） | OAuth/TokenManager 邏輯 |
| `EmojiService` | – | 整組（組 embed 用 Application Emote） | – |

> claude 分支曾把會限檢查放 Scraper 再發 member 事件，最終版改為整組留 Notifier（理由見 §1 核心原則）。重做時直接採最終版。

---

## 3. 設定

`bot_config.json` 新增欄位，且**全部可用環境變數覆寫**（Compose 用 `.env` 注入，敏感值不烤進 image）：

```jsonc
{
  // 既有: DiscordToken, GoogleApiKey, MySqlConnectionString, RedisOption, ApiServerDomain ...
  "TotalShards": 4,
  "ShardId": 0,                    // notifier 專用；租約模式可省略
  "HeartbeatIntervalSeconds": 10,
  "HeartbeatTtlSeconds": 30
}
```

| 設定 | 環境變數 |
|---|---|
| `MySqlConnectionString` | `MYSQL_CONNECTION_STRING` |
| `RedisOption` | `REDIS_OPTION` |
| `TotalShards` | `TOTAL_SHARDS`（notifier 亦可用啟動參數 `["id","total"]`） |
| ShardId 分配模式 | `SHARD_ASSIGNMENT`（`fixed` 預設；`lease` 向主控層領，§6.2） |
| `DiscordToken` / `GoogleApiKey` | `DISCORD_TOKEN` / `GOOGLE_API_KEY` |

> Redis Streams 不需要任何新設定 — 匯流排就在既有 `RedisOption` 指向的 Redis 上。
> MySQL / Redis 為外部獨立服務，本專案不負責啟動，只連入（§6）。

---

## 4. 訊息契約：Redis Streams 通知匯流排

### 4.1 拓撲

- **單一 stream**：`bot:notify`。
- **每個 shard 一個 consumer group**：`shard-{shardId}`，各 group 都會收到全部訊息（= 廣播 + 各自過濾，scraper 與通知設定零耦合）。
- **每個 group 只有一個 consumer**（名稱固定用 `notifier-{shardId}`；重啟後同名接手自己的 pending）。
- **訊息欄位**：`type` = `youtube` / `twitch` / `twitcasting` / `banner`；`payload` = DTO 的 JSON（Newtonsoft）。
- **修剪**：`XADD bot:notify MAXLEN ~ 10000 ...` — 上限修剪取代 RabbitMQ 的 x-message-ttl，防無人消費時無限堆積（正常通知量遠低於此）。
- **函式庫**：StackExchange.Redis 內建（`StreamAdd` / `StreamReadGroup` / `StreamAcknowledge` / `StreamAutoClaim`），**零新套件**。

### 4.2 DTO（`Shared/Messages/`）

直接照搬 claude 分支的最終版欄位設計（`git show claude:src/DiscordStreamNotifyBot.Shared/Messages/Notifications.cs`）：

- `YoutubeNotification`（`YoutubeNoticeType`: NewStream/NewVideo/Start/End/ChangeTime/Delete + videoId/channelId/標題/時間/IsMemberOnly/ChannelType）
- `TwitchNotification`（StartStream/EndStream/ChangeStreamData + 標題/分類/縮圖/起訖時間/IsRecord/Clips/Description；Profile 圖由消費端查 DB）
- `TwitcastingNotification`（欄位對應 `TwitcastingStream` 表）
- `BannerChangeNotification`（channelId + videoId）

原則：**傳結構化資料，不序列化 `Embed`**；線路 enum 與 UI enum（帶 `[ChoiceDisplay]`）分離，成員順序須對應。

### 4.3 消費迴圈（Notifier）

```
loop（吃 GracefulShutdown.Token）:
  entries = StreamReadGroup("bot:notify", "shard-{id}", "notifier-{id}", ">", count: 20)
  若空 → Task.Delay(1~2s) 後重讀        # 見下方「不可用 BLOCK」
  逐則:
    反序列化 type/payload → 查通知設定 → 過濾本 shard 伺服器 → 重建 embed → 發送
    全部目標處理完（成功或已記錄失敗）→ XACK
    例外 → 不 ack（留在 PEL），log 後續由 XAUTOCLAIM 補救
啟動時與每 5 分鐘:
  StreamAutoClaim(minIdle: 5min) 認領自己 PEL 中逾時的訊息重新處理   # 崩潰恢復
```

- **StackExchange.Redis 不支援 blocking read**（`XREADGROUP ... BLOCK` 與多工連線模型不相容）→ 一律用**短輪詢**（1–2 秒），通知延遲可忽略。不要嘗試 BLOCK，也不要為此換函式庫。
- **at-least-once → 可能重複**：發送成功後寫 Redis 短期去重鍵 `notified:{videoId}:{noticeType}` EX 數分鐘，重複訊息直接 ack 略過。
- **毒訊息**：同一訊息被 XAUTOCLAIM 認領超過 N 次（`delivery-count` 可得）→ log 完整 payload 後強制 ack 丟棄（等效 DLQ 的最簡版）。

### 4.4 建群與 Preflight

- Notifier 啟動：`XGROUP CREATE bot:notify shard-{id} 0 MKSTREAM`（**從 `0` 建群**：首次部署不漏既有訊息，重複建群回 BUSYGROUP 視為成功；歷史重播由去重鍵吸收）。
- Scraper Preflight：對 `bot:notify` XADD 一則 `type=test` 訊息驗證可寫（消費端忽略 test）。
- Coordinator 監控：`XPENDING bot:notify shard-{id}` 各 group pending 數；`XINFO GROUPS` 檢查 group 齊全。

### 4.5 Redis 控制平面鍵（非 stream）

| Key / 頻道 | 型別 | 用途 |
|---|---|---|
| `cluster:scraper:leader` | string（SET NX EX） | scraper leader 鎖，持有者定期續租 |
| `cluster:heartbeat:{role}:{id}` | string EX | 各程序心跳，TTL = HeartbeatTtl |
| `cluster:shard:lease:{shardId}` | string EX | notifier shard 租約（§6.2 才用） |
| `cluster:total_shards` | string | shard 總數公告 |
| `youtube.startstream` / `record` / `pubsub.*` 等 | pub/sub | **錄影程序 IPC，外部契約，維持不變** |
| `youtube.control.*` | pub/sub | Notifier → Scraper 的少數 owner 控制 |

---

## 5. Shard 歸屬與生命週期

### 5.1 歸屬守衛（防多 shard 互刪設定，最高優先）

Discord 公式：`(guildId >> 22) % totalShards == shardId`（放 `BotState.IsServerOnThisShard`）。
Notifier 在 `GetGuild == null` 時：
- 不屬於本 shard → **靜默略過，別刪設定**。
- 屬於本 shard 且 `Ready` 後仍找不到 → 才是真的離開，可刪。

**現行程式碼需修 5 處**（皆無條件刪，路徑為現行樹、行號已驗證）：

- `DiscordStreamNotifyBot/SharedService/Youtube/ReminderAction.cs:370`
- `DiscordStreamNotifyBot/SharedService/Twitch/TwitchService.cs:574`
- `DiscordStreamNotifyBot/SharedService/Twitcasting/TwitcastingService.cs:215`
- `DiscordStreamNotifyBot/SharedService/YoutubeMember/CheckMemberShip.cs:95`
- `DiscordStreamNotifyBot/SharedService/Youtube/ChangeGuildBanner.cs:25`

> 這 5 處守衛**不依賴任何架構拆分**，可作為獨立止血 PR 先行（原計畫 §9 的建議，仍成立）。

### 5.2 心跳與重啟

- 每程序每 `HeartbeatIntervalSeconds` 寫 `cluster:heartbeat:{role}:{id}`（帶 TTL）。
- Scraper 死亡 → leader 鎖 TTL 過期 → Compose `restart: unless-stopped` 拉起的新實例接手。
- 主控層只觀察與告警，**不負責重啟任何程序**。

### 5.3 啟動連線檢查 (StartupPreflight)

進主邏輯前依角色探測外部服務，指數退避重試（上限 ~60s），仍失敗 → 印出「哪個服務、host:port、原因」後 `Exit(1)`，交給 Compose restart：

| 角色 | MySQL | Redis | Stream 建群/可寫 | Discord |
|---|:---:|:---:|:---:|:---:|
| coordinator | – | ✅ | – | – |
| scraper | ✅ | ✅ | ✅ XADD test | – |
| notifier | ✅ | ✅ | ✅ XGROUP CREATE | ✅ 登入 |

---

## 6. Docker Compose

> MySQL / Redis 由**各自獨立的 compose stack** 運行；本專案 compose 只跑應用程式，容器經 `host.docker.internal` 連回主機埠口（Linux 需 `extra_hosts: ["host.docker.internal:host-gateway"]`）。確認外部服務 bind 位址容器可達、防火牆放行。

**單一 image、多角色**：根目錄一個 multi-stage `Dockerfile`，三個 exe publish 至 `/app/{scraper,notifier,coordinator}`，entrypoint 依 `$ROLE` 選執行檔（Notifier 額外 forward `[ShardId, TotalShards]`）。不要走「三個 Dockerfile」的彎路。
參考實作：`git show claude:Dockerfile`、`git show claude:docker-compose.yml`、`git show claude:.env.example`。

### 6.1 方式 A：固定 shard 服務（初期採用）

每個 shard 一個服務、`command: ["id","total"]` 寫死；直觀、可獨立重啟觀察單一 shard、免租約程式碼。

```yaml
services:
  coordinator: { image: dsnb, environment: { ROLE: coordinator }, restart: unless-stopped, env_file: .env }
  scraper:     { image: dsnb, environment: { ROLE: scraper },     restart: unless-stopped, env_file: .env, depends_on: [coordinator] }
  notifier-0:  { image: dsnb, environment: { ROLE: notifier },    command: ["0","4"], restart: unless-stopped, env_file: .env, depends_on: [coordinator] }
  # notifier-1..3 同上，id 遞增；total 必須全體一致
```

### 6.2 方式 B：`--scale` + shard 租約（主控層租約成熟後再切）

單一 `notifier` 服務 + `SHARD_ASSIGNMENT=lease`；replica 開機對 `i = 0..TOTAL-1` 嘗試 `SET cluster:shard:lease:{i} {instanceId} NX EX`，搶到即為自己的 ShardId，定期續租。注意事項（原計畫已驗證的判斷，照抄適用）：
- replica 數應等於 `TOTAL_SHARDS`；少了該 shard 無人服務、多了待命重試（勿 crash loop）。
- 租約 TTL 明顯大於續租間隔（30s/10s），防 GC 暫停造成同 id 雙認領；Discord 拒絕重複 identify 是最後防線。
- 不可設 `container_name`/固定 hostname，否則無法 `--scale`。
- 縮容 = 該 shard 暫停服務（訊息留在其 group 的 stream 裡），不會自動轉給其他 shard。

> Discord identify 需固定 `TotalShards`；改 shard 總數 = 全體 notifier 重連的規劃性維運，非日常操作。

---

## 7. 跨 shard 指令（Redis 三機制）

背景約束：**Discord 把所有 DM 路由到 shard 0**，且每個 notifier 的 `_client.Guilds` 只含自己持有的伺服器 → owner/admin 指令原生只在單一 shard 生效。以 Redis 三機制解決（不引入 gRPC/服務發現）：

| 機制 | 指令 | 作法 |
|---|---|---|
| **A. 廣播動作** | `die` / `Leave` / `LeaveNoNotifyGuild` / `/send-message` | pub/sub 廣播結構化參數（如 `SendAllPayload`），各 shard 對**自己持有的伺服器**執行、各端重建 embed。`LeaveNoNotifyGuild` 各 shard 回報數量、實際離開背景進行 |
| **B1. 共享快照** | `ListServer` / `SearchServer` / `ListNoNotifyGuild` / `ListOfficialList` | HASH（field = shardId）：各 shard 在 Ready/Joined/Left/15min timer 寫自己的 guild 清單；請求端合併（本 shard 即時、其餘讀快照，只採 `shardId < TotalShards`） |
| **B2. request/reply** | `UserInfo` / `GuildInfo` / `GetInviteURL` | correlationId + 2.5s 收集視窗、部分結果可接受、單 shard 命中即短路（需即時打到持有/在線的 shard） |

已知取捨（刻意，不要「修好」它們）：
- `/send-message` 不做即時發送數量彙總（發送耗時數分鐘，2.5s 視窗抓不到），只回「已廣播」、各 shard 自行 log。
- 快照在 shard 離線期間是陳舊資料，下次 Ready 重寫（管理用途可接受）。
- 本來就正確、無需改：`Add/RemoveOfficialList`（已有 Redis reload 廣播）、`/member unlink`（`member.revokeToken`）、所有以共用 DB 為準的 per-guild 設定指令。

參考實作（完整可抄，改動小）：`git show claude:src/DiscordStreamNotifyBot.Notifier/SharedService/Cluster/ClusterQueryService.cs`（B1+B2 低階 helper）、`git show claude:src/DiscordStreamNotifyBot.Notifier/Command/Admin/AdministraitonService.cs`（A 的訂閱+廣播）。相依方向：AdministrationService → ClusterQueryService（單向）。

---

## 8. 分階段實作步驟

> 每階段：可獨立建置、可回滾、完成即勾 checkbox + commit + 更新 CLAUDE.md 狀態橫幅。
> 無自動化測試 — 每階段以多程序手動實測驗證（§11）。commit 前一律 `dotnet build DiscordStreamNotifyBot.sln -c Release`。

### 階段 0：止血 PR — shard 歸屬守衛
- [x] 在現行單一專案直接修 §5.1 的 5 處 + `BotState.IsServerOnThisShard` 等效 helper。不改變單 shard 行為，立即消除未來多 shard 互刪設定的災難。
      （helper 暫置於 `Bot`：`Bot.IsServerOnThisShard` / `Bot.ShouldDeleteMissingGuild`，階段 1 拆分時移入 `BotState`。）

### 階段 1：Solution 骨架 + Shared
- [ ] 建 `src/` 四專案 + 參考關係，全部可編譯（exe 先空殼）。
- [ ] 搬移 §2.1 清單；EF 工具指向 Shared（含設計階段工廠）；**Migrations/ 從 claude 分支整批照搬**（§9-2）。
- [ ] 三個 exe 的 Main 開頭掛 `StartupPreflight` + `GracefulShutdown`。
- 參考：§2.1 列出的 `git show claude:...` 檔案。

### 階段 2：Notifier 上線（先維持單 shard 行為）
- [ ] `Bot.cs`、指令系統、`Interaction/`、`Command/` 搬入 Notifier；指令改用 Shared 的 `*ApiService`。
- [ ] 偵測 Timer **暫留** Notifier，功能不中斷。
- 參考：`git show claude:src/DiscordStreamNotifyBot.Notifier/Bot.cs`、`.../Program.cs`。

### 階段 3：Scraper 拆出 + Redis Streams 匯流排
- [ ] Shared 加 `Messages/` DTO 與 streams publisher/consumer helper（§4）。
- [ ] 偵測 Timer、錄影訂閱、PubSub/EventSub 維護搬到 Scraper；偵測端 publish DTO、移除 Discord 呼叫。
- [ ] Notifier 消費 group → 重建 embed → 發送（含 banner/活動）→ XACK；移除殘留偵測 Timer。
- [ ] scraper leader 鎖。
- 參考：`git show claude:src/DiscordStreamNotifyBot.Scraper/Detection/...`（偵測邏輯照搬）、`.../Shared/NotificationBusPublisher.cs` 與 `.../Notifier/NotificationBusConsumer.cs`（語意參考，傳輸層改 §4）、`.../Shared/RabbitMqService.cs`（**只看不抄** — 它是被否決的傳輸層）。

### 階段 4：Coordinator
- [ ] 心跳監控 + leader 鎖觀察 + `cluster:total_shards` 公告 + XPENDING 堆積監控。
- [ ] （選用）shard 租約，支援方式 B。
- 參考：`git show claude:src/DiscordStreamNotifyBot.Coordinator/CoordinatorService.cs`。

### 階段 5：跨 shard 指令與共享狀態
- [ ] §7 三機制（ClusterQueryService + AdministrationService 改造）。
- [ ] `Utility.OfficialGuildList` 改存 Redis SET（解 `Program.cs:41` TODO，首啟由 OfficialList.json 播種）。
- [ ] 狀態列伺服器/成員計數跨 shard 彙總（Redis HASH）。

### 階段 6：Docker 化與部署驗證
- [ ] 單一 multi-stage Dockerfile + compose（方式 A）+ `.env.example`。
- [ ] 跑完 §11 驗證清單。
- 參考：`git show claude:Dockerfile`、`claude:docker-compose.yml`、`claude:.env.example`。

### 階段 7（收尾）：制度回填
- [ ] 更新 CLAUDE.md（架構段改為四專案版，收割 claude 分支 CLAUDE.md 的對應段落）。
- [ ] 收割 claude 分支的 `.claude/skills/`（`add-detection-platform`、`debug-detection-bus`、`ef-migration-baseline`），把 RabbitMQ 字眼改成 streams 後放入本分支。

---

## 9. 正確性必辦（不做會在正式環境出錯）

1. **SIGTERM 優雅關閉**：`docker stop` 送 SIGTERM，`Console.CancelKeyPress` 只攔 SIGINT → 現行清理（存 DB、Unsubscribe）不會執行。用 `AppDomain`/`PosixSignalRegistration` 統一處理（參考 `claude:src/DiscordStreamNotifyBot.Shared/GracefulShutdown.cs`），全部長迴圈吃它的 `CancellationToken`。
2. **EF Migrations 不可重新生成**：正式 DB 的 `__EFMigrationsHistory` **已記錄 claude 分支的 migration ID**（`20250320095452_RefactorDbContext` … `20260611015819_SyncModelDrift`，基線化已於 2026-06 完成）。重做時把 `claude:src/DiscordStreamNotifyBot.Shared/Migrations/` **整個資料夾照搬**（含 ModelSnapshot），否則新生成的 migration ID 對不上正式 DB，`Migrate()` 會嘗試重建既有表。（master 樹已含前三個 migration，缺 `SyncModelDrift` 與 baseline SQL — 照搬整個資料夾即補齊。）搬完後跑 `dotnet ef migrations has-pending-model-changes --project src/DiscordStreamNotifyBot.Shared` 確認無 drift。**禁用 EnsureCreated**；正式環境套用一律 Script-Migration（見 CLAUDE.md EF 鐵則）。
3. **Streams 堆積上限**：XADD 一律帶 `MAXLEN ~`；毒訊息按 §4.3 丟棄機制處理。無人消費的 group（例如縮容後）pending 會留著 — Coordinator 的 XPENDING 監控要涵蓋「group 存在但無 consumer」。

---

## 10. 可優化項目（claude 分支已有成品，對應階段順手移植）

| 項目 | 內容 | 參考 |
|---|---|---|
| PeriodicTimer 輪詢 | 固定間隔輪詢改 `PeriodicRunner`（無重入、吃 CancellationToken）；一次性到點提醒維持 `System.Threading.Timer` | `claude:src/DiscordStreamNotifyBot.Shared/PeriodicRunner.cs` |
| Notice 設定快取 | `NoticeCache<T>`（TTL 30s + 變更 Invalidate），降廣播 fan-out 的 MySQL 壓力 | `claude:src/DiscordStreamNotifyBot.Notifier/SharedService/NoticeCache.cs` |
| YouTube 批次查詢 | Nijisanji 排程改收集 videoId 批次 `GetVideosAsync`（一次 50 筆）省 quota | claude 分支 commit `66ac33e` |
| Pooled DbContextFactory | `MainDbService` 用 `PooledDbContextFactory` | claude 分支 commit `8043f4f` |
| Log 角色標籤 | `Log.RolePrefix` = `scraper` / `notifier:{id}` / `coordinator`，跨程序追蹤 | claude 分支 commit `9c74fa4` |
| Generic Host 設定重構 | **維持暫緩**：會變更部署 env 命名（`RabbitMQ__HostName` 式）、觸及啟動關鍵路徑，淨效益低 | 原計畫 §12.2 的暫緩分析 |

---

## 11. 驗證清單（部署前全過）

- [ ] 同一則開台通知只發一次
- [ ] YouTube/Twitch API quota 不隨 shard 數成長
- [ ] 跨 shard 不互刪通知設定
- [ ] 重啟單一 notifier 不影響其他 shard
- [ ] 殺掉 scraper 後 Compose 重啟自動接手 leader
- [ ] `youtube.record` 只發一次
- [ ] 重啟某 notifier 期間的開台通知，重啟後由該 group 的 pending/未讀訊息補送（不漏）
- [ ] `docker stop` 任一容器可觀察到優雅關閉 log（SIGTERM 生效）
- [ ] §7 三機制：DM 下 `ListServer`/`UserInfo`/`/send-message` 能覆蓋全部 shard 的伺服器
