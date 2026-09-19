# YouTube WebSub 修正與 Atom fallback 實作計畫

> 狀態：程式碼已完成並通過兩邊建置與單元／契約測試；真實 Google Hub 與跨服務整合項目標示為未驗證（見第 17 節）。
>
> 建立日期：2026-09-19
>
> 執行方式：新 session 先重新讀取兩個儲存庫的 `AGENTS.md`、執行各自的 `graphify query`，再從第 10 節第一個未勾選項目開始。完成一個階段後立即更新本文件。未經使用者要求不要 commit、push 或部署。

## 1. 目標

修正目前 YouTube PubSubHubbub／WebSub 訂閱流程的規格與相容性問題，並加入不使用 `search.list` 的 Atom feed 補償輪詢。

完成後應符合下列結果：

- WebSub 仍是近即時通知的主要來源。
- 訂閱使用 YouTube 官方目前文件指定的 canonical topic。
- Google Hub 暫時回傳 5xx、漏送事件或 Redis 重啟時，Atom fallback 可發現近期新影片及公開排定直播。
- Atom fallback 不呼叫 `search.list`，只在發現未知 video ID 後沿用既有 `videos.list` 批次查詢。
- WebSub 與 Atom 同時發現同一支影片時，不會重複建立影片或重複發送通知。
- Backend 不再依賴 PubSubHubbub 0.4 未定義的 `hub.verify_token` 保存 HMAC secret。
- Backend 能處理 Hub 依 0.4 送出的 `hub.mode=denied` callback，不再把拒絕通知當成未知 mode。
- WebSub 通知使用原始 HTTP body bytes 驗證 HMAC，合法的 Content-Type 參數不會造成誤拒絕。
- YouTube 刪除事件會轉發到既有 `youtube.pubsub.Deleted` Redis channel。

## 2. 不在範圍

- 不使用 `search.list`。
- 不把現有 `/videos`、`/streams` HTML 爬取擴大到全部 `YoutubeChannelSpider`。
- 不自建 WebSub Hub，也不導入第三方 Hub 服務。YouTube publisher 沒有通知該 Hub 時，Hub 最後仍只能輪詢。
- 不新增 NuGet 套件、排程框架、資料表或 migration。
- 不重寫既有 YouTube 影片分類、提醒、通知匯流排與 Discord 發送流程。
- 不保證 Atom 能補回 feed 最近項目之外的長時間缺口，也不以 Atom 偵測刪除事件。
- 不在本次新增管理介面、Slash 指令或使用者可調整的輪詢設定。

## 3. 涉及專案

| 專案 | 目錄 | 本次職責 |
|---|---|---|
| Bot | `DiscordStreamNotifyBot/` | WebSub 訂閱請求、Redis pending state、續訂、Atom fallback、video ID 去重與批次查詢、測試 |
| Backend | `DiscordStreamBotBackend/` | WebSub challenge、HMAC secret、HMAC 驗證、Atom payload 解析、Redis 事件轉發、測試 |

MySQL schema 仍由 Bot 儲存庫管理。本計畫不需要 schema 變更。

## 4. 現況基線與已確認問題

### 4.1 Bot 訂閱流程

- `src/DiscordStreamNotifyBot.Shared/YoutubeApiService.cs`
  - `PostSubscribeRequestAsync` POST 至 `https://pubsubhubbub.appspot.com/subscribe`。
  - topic 仍為已過時的 `https://www.youtube.com/xml/feeds/videos.xml?channel_id=...`。
  - 只把 HTTP 202 視為受理。
  - 同一個 GUID 同時當作 `hub.secret` 與 `hub.verify_token`。
- `src/DiscordStreamNotifyBot.Scraper/Detection/Youtube/YoutubeDetectionService.cs`
  - `SubscribePubSubAsync` 每 30 分鐘執行。
  - 目前以 `LastSubscribeTime < 現在 - 7 天` 選擇續訂頻道。
  - 任一頻道失敗就停止本輪，未區分暫時性 5xx 與單一頻道 4xx。
  - `youtube.pubsub.NeedRegister` 可在定期續訂之外直接觸發另一個訂閱請求。

### 4.2 Backend callback

- `DiscordStreamBotBackend/Controllers/YouTubeNotificationsController.cs`
  - challenge GET 強制要求 `User-Agent` 以 `FeedFetcher-Google;` 開頭；規格沒有這項驗證契約。
  - 依賴 callback 的 `hub.verify_token` 保存 HMAC secret。
  - 只檢查 DB 內是否存在頻道，沒有確認這次 callback 對應本系統實際送出的 pending action。
  - POST 強制要求 Content-Type 完全等於 `application/atom+xml`，會拒絕合法的 charset 等參數。
  - HMAC 以 `StreamReader` 讀成字串後重新編碼計算，不是規格要求的原始 request body bytes。
  - HMAC 無效或缺少 secret 時回 400；應忽略 payload，並以 2xx 快速確認，避免 Hub 反覆重送無效內容。
  - CreateOrUpdated 目前以舊 `/xml/feeds/videos.xml` 字串辨識 payload，可能拒絕官方目前 `/feeds/videos.xml` 格式。
  - `Deleted` payload 完成解析後沒有呼叫 `AddYouTubePubMessage`，所以未轉發刪除事件。
- `DiscordStreamBotBackend/Middleware/LogMiddleware.cs`（實作時另外發現）：以每 IP 每小時 5 次非 atom Content-Type 的固定門檻對請求回 429，連 Hub 的 challenge GET 也算在內。遷移期連續 404（舊格式 callback）或一次大量續訂會讓授權正確的 challenge 被擋在 controller 之前，且 429 本身會延長計數 TTL 造成持續失敗。

### 4.3 現有 fallback

- `src/DiscordStreamNotifyBot.Scraper/Detection/Youtube/YoutubeDetectionService.Schedule.cs`
  - `OtherScheduleAsync` 每 5 分鐘抓 YouTube `/videos` 與 `/streams` HTML。
  - 目前只處理同時存在於 `RecordYoutubeChannel` 的頻道。
  - 找到 video ID 後，已會使用 `GetVideosAsync` 每 50 筆呼叫 `videos.list`，再交給 `AddOtherDataAsync`。
- 此流程保留原狀，不擴大到所有通知頻道。Atom fallback 負責低成本補償。

## 5. 不可偏離的設計決策

1. WebSub 是主要來源，Atom 是一直啟用的低頻 reconciliation，不等到確認故障才啟動。
2. Atom 輪詢涵蓋 **WebSub 訂閱沒有在續訂週期內被 challenge 確認的** `YoutubeChannelSpider`（`LastSubscribeTime < 現在 − 7 天`，含從未成功者），同一 YouTube channel 全域只抓一次，不依 guild 或通知目的地重複抓取。
   - 2026-09-19 使用者決策變更：原設計是「涵蓋全部 crawler」，但實測 feed 端點不支援條件式 GET（見 §17.3），全頻道輪詢等於每輪全量下載，因此改為只補疑似失效的頻道、間隔由 5 分鐘拉長為 15 分鐘。
   - 已知代價：Hub 靜默停止送通知但訂閱仍被續訂確認的頻道不會被 Atom 覆蓋；這類故障需要靠 WebSub 端（Hub 通知延遲、NeedRegister、log）與營運監控發現。
3. Atom 使用 `https://www.youtube.com/feeds/videos.xml?channel_id={channelId}`。
4. WebSub topic 使用與 Atom 相同的 canonical URL，不再使用 `/xml/feeds/`。
5. 不呼叫 `search.list`。未知 ID 的詳細資料只使用既有 `videos.list`，每次最多 50 個 ID。
6. Atom 沿用 `TryClaimUnknownVideo`、`SharedExtensions.HasStreamVideoByVideoId`、`GetVideosAsync` 與 `AddOtherDataAsync`，不建立第二套影片分類或通知邏輯。
7. WebSub 訂閱 POST 收到任一 2xx 只表示 Hub 已受理；只有 challenge 成功才可更新 `LastSubscribeTime` 與 HMAC secret 的實際 lease TTL。
8. pending action 與 HMAC secret 放在 Bot、Backend 已共用的 Redis logical DB 1。不得放入 Pub/Sub payload、log 或 DB 0。
9. Atom HTTP validator 放在 Redis DB 0。Redis 資料遺失只會造成下一輪重新下載完整 feed，不影響正確性。
10. Backend 必須同時支援部署前沒有 `channelId` query 的既有 callback POST，以及部署後每頻道固定且不可猜測的 callback URL；既有訂閱自然到期期間可能重複送達，交由既有 video ID 去重處理。
11. 不以 User-Agent 當 callback 授權。授權依 pending action、topic、channelId 與 HMAC 完成。
12. 任何 HTTP、XML、Redis 或 YouTube API 暫時錯誤都不得刪除 `YoutubeChannelSpider` 或把未知狀態當成影片刪除。
13. 以 Google Hub 宣告支援的 PubSubHubbub 0.4 為互通基線；`hub.verify`／`hub.verify_token` 只屬 Google 同時支援的 0.3 相容欄位，不得成為必要契約。

## 6. 目標流程

### 6.1 WebSub 訂閱

```text
Scraper／Notifier
  -> 取得或建立該頻道固定的 cryptographically random HMAC secret
  -> Redis DB 1 寫入 pending action
  -> POST Google Hub
  -> 2xx：等待 callback，不更新 LastSubscribeTime
  -> 非 2xx／例外：記錄可判斷的失敗類型

Google Hub
  -> GET Backend callback?channelId=...&token=...

Backend
  -> 驗證 mode、topic、channelId、challenge、lease_seconds
  -> 讀 Redis DB 1 pending action
  -> topic/mode/channelId 必須完全相符
  -> 確認該頻道 HMAC secret 存在，TTL 更新為 Hub 實際 lease_seconds
  -> 更新 YoutubeChannelSpider.LastSubscribeTime
  -> 回傳 challenge 與 2xx
```

HMAC secret 每個頻道固定一份，續訂重用，不因每次 request 旋轉。PubSubHubbub 0.4 challenge 沒有 request ID；固定 secret 可避免延遲 callback 無法判斷應啟用哪一份 secret。新的訂閱若只收到 POST 2xx、但 challenge 未到或驗證失敗，`LastSubscribeTime` 不得變更。

### 6.2 WebSub 通知

```text
Google Hub POST callback
  -> 讀取原始 body bytes
  -> 從 X-Hub-Signature 解析演算法與 signature
  -> 使用該頻道 HMAC secret 對原始 bytes 計算 HMAC
  -> 無效：不處理 payload，回 2xx
  -> 有效：用同一份 bytes 安全解析 Atom XML
  -> CreateOrUpdated / Deleted 發布到既有 Redis channel
  -> 回 2xx
```

回應 Hub 只代表已接收，不等待 Discord 發送。Redis 發布仍使用 Backend 現有佇列與 retry 行為。

### 6.3 Hub 拒絕訂閱

```text
Google Hub GET callback
  -> hub.mode=denied
  -> hub.topic=...
  -> hub.reason=<optional>

Backend
  -> 驗證 callback token、channelId、topic
  -> 將 pending 標為 denied，不更新 LastSubscribeTime 或 HMAC lease
  -> 回 2xx

Scraper
  -> Atom fallback 繼續運作
  -> 自動續訂不再每 30 分鐘重送相同被拒絕要求
  -> owner 強制重新訂閱或訂閱契約變更時才建立新 pending
```

`hub.reason` 是外部輸入，只能保存／記錄安全摘要，不得直接進入使用者訊息或 metric label。

### 6.4 Atom fallback

```text
Scraper leader 每 15 分鐘執行一次、不重入
  -> 讀取 LastSubscribeTime 超過續訂週期（7 天）或從未確認的 YoutubeChannelSpider
  -> 每個 channel GET canonical Atom feed
     -> 帶 If-None-Match / If-Modified-Since（若已有；實測端點不提供 validator，見 §17.3）
     -> 304：結束該頻道
     -> 200：安全解析 feed
  -> 收集未知 video ID
  -> 全輪去重後每 50 筆 videos.list
  -> 與 WebSub 共用的 ProcessDiscoveredVideoAsync
  -> 本輪處理成功後才保存新的 ETag / Last-Modified
```

首次執行可能發現 DB 尚未保存的近期項目。通知仍遵守既有分類規則，例如一般影片的兩天限制與排定直播的既有時間範圍，不另外補發更舊內容。

## 7. Redis 契約

### 7.1 Database 1：WebSub 共享狀態

Bot 與 Backend 必須使用完全相同的 key 名稱與 JSON 欄位。兩個 repo 各自加入契約測試，避免字串漂移。

```text
youtube:websub:pending:{channelId}
youtube.pubsub.HMACSecret:{channelId}
```

pending payload 最少包含：

```json
{
  "version": 1,
  "channelId": "UC...",
  "mode": "subscribe",
  "topic": "https://www.youtube.com/feeds/videos.xml?channel_id=UC...",
  "callbackToken": "<base64url>",
  "requestedAtUtc": "2026-09-19T00:00:00Z",
  "confirmedAtUtc": null,
  "deniedAtUtc": null
}
```

規則：

- `mode` 只允許 `subscribe` 或 `unsubscribe`。
- `channelId` 必須是已驗證的 24 字元 YouTube channel ID。
- HMAC secret 使用安全隨機值，每個頻道固定一份，沿用目前已存在的 `youtube.pubsub.HMACSecret:{channelId}` key，避免切換時讓既有訂閱立即失效。
- Bot 發出 subscribe 前先讀取既有 HMAC secret；不存在時使用 `SET NX` 建立，再讀回實際值送給 Hub。
- 同一 mode 的重試重用既有 pending action 與 HMAC secret，不因重試旋轉 secret。
- 未確認的同一 mode request 重用 pending；mode 改變，或前一筆已有 `confirmedAtUtc` 且已到續訂時間時，才替換 pending action。
- 同一頻道不得同時發出兩個 in-flight request。
- pending TTL 使用目前既有的 10 天 requested lease；重試可延長但不得縮短較長 TTL。
- challenge 成功時，Backend 必須確認 pending 仍與 mode、topic、channelId 相符。
- challenge 完成 DB 與 Redis 更新後寫入 `confirmedAtUtc`，並保留 pending 到 TTL 到期，讓 Hub 在 callback response 遺失時可安全重試同一 challenge。
- 已確認 pending 的重複 challenge 必須是 idempotent，不得再次旋轉 secret 或建立第二份狀態。
- 收到合法 `hub.mode=denied` 時寫入 `deniedAtUtc`；不更新 `confirmedAtUtc`、`LastSubscribeTime` 或 HMAC secret lease。
- denied pending 不自動週期重送。既有 owner 強制重新訂閱可清除 denied 狀態並建立新 pending。
- HMAC secret TTL 只在「建立新 secret」時設為目前請求的 lease，既有 secret 不因再次送出要求而延長（否則 Hub 回 5xx 或 challenge 未完成時，續訂篩選會被延長過的 TTL 誤導而略過該頻道）；challenge 成功後一律改用 callback 的實際 `hub.lease_seconds`。
- unsubscribe challenge 成功後刪除 HMAC secret。
- 不在任何 log、exception message 或 metric label 輸出 secret。

Bot 已有 `RedisChannels.OAuth.DatabaseNumber == 1`。實作可沿用同一 logical database number，但 YouTube key 必須有自己的命名空間，不可放進 OAuth key。

### 7.2 Database 0：Atom validator

```text
youtube:atom:etag:{channelId}
youtube:atom:last-modified:{channelId}
```

規則：

- response 沒有 validator 時不建立對應 key。
- 只有 XML 成功解析，且本輪未知 ID 已成功完成處理後，才更新 validator。
- 解析或 API 查詢失敗時保留舊 validator，讓下輪可重新取得相同內容。
- 移除 `YoutubeChannelSpider` 時可順手刪除 validator；刪除失敗不阻擋主要移除流程。

## 8. WebSub 詳細修正

### 8.1 Topic 與 callback

訂閱 request 固定 POST 至 Google 表單實際使用的 endpoint：

```text
https://pubsubhubbub.appspot.com/subscribe
```

Hub 首頁公告的 `https://pubsubhubbub.appspot.com/` 是 publisher discovery 與 publish endpoint；Google 的 subscriber 表單 action 是 `/subscribe`。本整合保留目前 `/subscribe`，不改成 root URL。

訂閱表單固定為：

```text
hub.mode=subscribe|unsubscribe
hub.topic=https://www.youtube.com/feeds/videos.xml?channel_id={channelId}
hub.callback=https://{ApiServerDomain}/NotificationCallback?channelId={channelId}&token={callbackToken}
hub.secret={per-channel HMAC secret}   # subscribe 才需要
hub.lease_seconds=864000               # 保留目前 10 天請求值
```

- 移除 `hub.verify` 與 `hub.verify_token`。
- Google 表單仍顯示這兩個 0.3 相容欄位，但 Hub 同時宣告符合 0.4；本實作可省略它們，重點是不得依賴 `hub.verify_token` callback。
- Google 表單 HTML 的 lease input 名稱是 `hub.lease_numbers`，與 0.4 規格不符。程式必須維持正確的 `hub.lease_seconds`，不可照抄表單欄位名稱。
- `hub.secret` 必須少於 200 bytes。
- `callbackToken` 由 HMAC secret 以 HMAC-SHA256 和固定用途字串衍生，再以 base64url 表示；不得直接把 HMAC secret 放進 URL。token 同時保存於 pending，讓 unsubscribe 刪除 secret 後仍可安全處理 Hub 的 challenge 重試。
- callback query 的 `channelId` 與穩定 token 讓每個頻道有固定、不同且不可猜測的 callback URL，續訂仍覆寫同一 `(topic, callback)` subscription。
- Backend 以相同方式重新計算 callback token，並以 constant-time API 比較。
- `ApiServerDomain` 仍由既有設定提供，不從 request Host 動態產生。
- URL 使用 `Uri`／query builder 等安全組合方式，不直接接受外部 topic 或 callback。

### 8.2 訂閱結果

不要再回傳單一 `bool` 隱藏失敗原因。使用最小必要的結果型別，至少區分：

| 結果 | HTTP／情境 | 批次行為 |
|---|---|---|
| Accepted | 任一 2xx | 等待 challenge；繼續下一頻道 |
| TransientFailure | 429、5xx、網路錯誤、timeout | 記錄狀態；停止本輪，交給既有週期重試 |
| PermanentFailure | 其他 4xx | 記錄頻道與回應摘要；略過該頻道並繼續，避免阻塞全部續訂 |

- 若回應包含有效 `Retry-After`，後續請求不得早於該時間；不要在目前批次內長時間 sleep。
- Google `/subscribe` 表單明示成功提交回 HTTP 204，PubSubHubbub 0.4 則規定 HTTP 202；因此 provider client 接受任一 2xx，再以 challenge 決定是否真正成立。
- response body 只記錄有長度限制的診斷摘要，避免 Hub 回傳大量內容污染 log。
- 取消訂閱失敗不能阻止使用者移除本地 crawler；保留目前 best-effort 語意。

### 8.3 續訂判斷

- 保留目前「請求 10 天 lease、約 7 天後續訂」的既有政策。
- 另外檢查 Redis DB 1 HMAC secret 是否不存在或 TTL 已接近目前既有三天續訂緩衝；Redis 重啟後可提早補建訂閱。
- `LastSubscribeTime` 只在 Backend challenge 成功後更新，不在訂閱 POST 2xx 時更新。
- `youtube.pubsub.NeedRegister` 與定期續訂必須共用同一個 per-channel in-flight 防重機制。

### 8.4 Challenge GET

Backend 必須：

- 驗證 `hub.mode`、`hub.topic`、`hub.challenge` 與 subscribe 時的 `hub.lease_seconds`。
- query `channelId`、topic 內 channel ID、pending payload channel ID 三者一致。
- callback token 必須與 pending payload 一致；subscribe 時若 HMAC secret 存在，再核對其衍生值。缺少或不符時回 404。
- subscribe 時確認 DB 仍存在該 `YoutubeChannelSpider`；不存在回 404。
- unsubscribe 時確認 pending action 確實為 unsubscribe，不以 DB row 已存在為前提。
- 找不到相符 pending action 時回 404，不接受未發出的訂閱或取消要求。
- Redis 或 MySQL 更新失敗時回 5xx 並保留未確認 pending，讓 Hub 重試；不可只完成一半後回 2xx。
- 不因 User-Agent 不符而拒絕；可留下不含敏感資料的診斷 log。
- 成功回應 body 必須逐字等於 challenge。
- 使用安全 Content-Type，並加入 `X-Content-Type-Options: nosniff`。
- 不把完整 challenge、secret 或 callback query 寫入一般資訊 log。

### 8.5 Denied GET

- `hub.mode=denied` 沒有 `hub.challenge`，不可走 subscribe／unsubscribe challenge 分支。
- 驗證 callback token、channelId 與 topic 後，將 pending 標為 denied 並回 2xx。
- 不更新 `LastSubscribeTime`、HMAC secret TTL 或 confirmed 狀態。
- 不刪除 crawler；Atom fallback 繼續運作。
- owner 強制重新訂閱可清除 denied 狀態。一般週期不得持續重送相同被拒絕要求。

### 8.6 Notification POST

- Controller action 改為 async，先把 body 讀成 byte array，再用同一份 bytes 驗 HMAC 與解析 XML。
- 新 callback POST 必須驗證 channel ID 與 callback token；部署前的舊 callback POST 可在沒有 query token 時以有效 body HMAC 通過遷移期驗證。
- callback token 無效時忽略內容並回 2xx，避免 Hub 對不可能成功的通知持續重送。
- Content-Type 以 media type 判斷，接受 `application/atom+xml` 的合法參數。
- `X-Hub-Signature` 解析為 `algorithm=hex`；至少支援 Google Hub 使用的 `sha1`，比較使用 constant-time API。
- 缺少 HMAC secret、缺少／無效 signature、未知演算法或 channel 不一致時，忽略內容並回 2xx。
- HMAC secret 缺失時可發布既有 `youtube.pubsub.NeedRegister`；已有 secret 但 signature 無效時不得觸發重新訂閱，避免外部請求放大 Hub 流量。
- XML reader 禁止 DTD，`XmlResolver = null`，並限制在既有 Atom／YouTube namespace 中取值。
- 有效的 CreateOrUpdated 與 Deleted 都呼叫 `RedisService.AddYouTubePubMessage`。
- payload 成功接收後快速回 2xx；Redis 發布失敗沿用現有 queue/retry，不讓 Hub request 等待 Discord。

## 9. Atom fallback 詳細設計

### 9.1 排程與涵蓋範圍

- 在 `YoutubeDetectionService` 使用既有 `PeriodicRunner` 與 `GracefulShutdown.Token`。
- 間隔為 15 分鐘（2026-09-19 由 5 分鐘調整，見 §5.2），不新增設定項。
- 只由 Scraper leader 執行；`PeriodicRunner` 不重入。
- 查詢 `YoutubeChannelSpider.AsNoTracking()` 中 `LastSubscribeTime` 超過續訂週期或從未確認的頻道，以 channel ID 去重。
- 單一頻道失敗不得中止其他頻道；全域 YouTube／網路故障時避免無限制立即重試。
- 不改動 `OtherScheduleAsync` 的 `RecordYoutubeChannel` 篩選與 HTML 頁面用途。

### 9.2 HTTP 行為

- 使用 `IHttpClientFactory`，不建立長期靜態 `HttpClient`。
- 有 ETag 時送 `If-None-Match`；有 Last-Modified 時送 `If-Modified-Since`。
- 304 不解析 body、不呼叫 YouTube API。
- 200 才解析 Atom。
- 404、410、429、5xx 與 timeout 都只記錄診斷，不刪 crawler、不清除既有影片狀態。
- 429 遵守有效 `Retry-After`，不以密集重試繞過限制。
- 不下載或記錄影片描述、縮圖等 fallback 不需要的內容。

### 9.3 Atom 解析

新增一個小型純 parser，輸入 XML bytes，輸出 feed channel ID 與 entry 清單。不要把 HTTP、Redis 或 DB 放進 parser。

每個 entry 最少解析：

- `yt:videoId`
- `yt:channelId`
- `published`
- `updated`

規則：

- feed 本身（根節點 `yt:channelId` 或 self link）必須與請求 channel 相符；不符時整份視為無效。
- entry 的 `yt:channelId` 若屬其他頻道（實測 YouTube 會夾帶合作／翻唱影片，見 §17.3）逐筆略過並記錄，**不因此丟棄整份 feed**；只有該頻道自己的 entry 進入後續處理。
- 缺少或格式錯誤的單一 entry 可略過並記錄，不因一筆壞資料放棄其他合法 entry。
- 同一 feed 重複 video ID 只保留一次。
- 禁止 DTD 與外部 entity。
- parser 不判斷直播、一般影片或偽裝貼文；分類仍由 `videos.list` 與現有 policy 負責。

### 9.4 去重、批次與通知

- 對解析出的 ID 先使用 `TryClaimUnknownVideo`；已在記憶體或 MySQL 的 ID 不再查 API。
- 跨頻道收集後再次依 video ID 去重。
- 每 50 個 ID 呼叫既有 `GetVideosAsync`。
- API 沒回傳的 ID 不建立假資料，並讓 claim 可在後續輪次重試。
- API 回傳後逐筆呼叫 `AddOtherDataAsync`，沿用既有：
  - 一般新影片通知。
  - 排定直播通知與 reminder。
  - 已開台處理。
  - 15 秒且停用留言的偽裝貼文判定。
  - `addNewStreamVideo` 與 DB 去重。
- WebSub 與 Atom 的競態由同一 `TryClaimUnknownVideo`／`addNewStreamVideo` 路徑收斂，不另建第二個通知 dedup key。

### 9.5 已知限制

- YouTube Atom feed 只保留有限的近期項目。若 Hub 故障時間很長且頻道在兩次成功輪詢間發布超過 feed 可見範圍，仍可能漏掉較舊項目。
- Atom 不提供可靠的 deleted-entry 歷史，刪除事件仍依 WebSub 與既有 reminder/API reconciliation。
- 私人、會員限定或未出現在公開 feed 的影片不能靠此 fallback 完整發現。
- Atom 發現延遲上限由 15 分鐘排程、單輪處理時間、YouTube 回應時間與其 15 分鐘伺服器快取（§17.3）共同決定，不宣稱即時。
- 只補「續訂沒有被 challenge 確認」的頻道；Hub 靜默停止送通知但訂閱仍確認成功的頻道不會被覆蓋。

## 10. 實作階段

### 階段 0：重新確認基線

- [x] 在 Bot 執行 `graphify query "YouTube WebSub 訂閱、Atom fallback、影片去重與 AddOtherDataAsync 的完整流程"`。
- [x] 在 Backend 執行 `graphify query "NotificationCallback challenge、HMAC 驗證與 YouTube Redis 轉發流程"`。
- [x] 檢查兩個 repo 的 `git status`，保留所有既有變更，不修改無關檔案。（Backend 只有既有 `graphify-out/` 異動。）
- [x] 重新開啟官方 YouTube push 文件、Google Hub 頁面與 PubSubHubbub 0.4／WebSub 規格，確認外部契約沒有再改變。
  - YouTube 文件（2026-09-16 更新）仍指定 topic 為 `https://www.youtube.com/feeds/videos.xml?channel_id=CHANNEL_ID`。
  - Hub 表單 action 仍為 `/subscribe`，成功提交回 HTTP 204，`hub.lease_numbers` 仍是表單筆誤。
  - PubSubHubbub 0.4：`hub.lease_seconds`、`hub.mode=denied`、`X-Hub-Signature: sha1=<hex>`、HMAC 對 request body 計算、`hub.secret` < 200 bytes。

### 階段 1：Bot WebSub request 與共享狀態

- [x] 在 Bot 集中定義 canonical topic、callback 與 Redis DB 1 key helper。（`Shared/RedisChannels.cs`、`Shared/SharedService/Youtube/YoutubeWebSubContract.cs`）
- [x] 新增最小 pending action DTO 與 Redis 存取；沿用或建立每頻道固定 HMAC secret。（`YoutubeWebSubService.cs`）
- [x] 移除 `hub.verify`、`hub.verify_token` 與舊 `/xml/feeds/` topic。
- [x] callback 改為每頻道固定 query URL。
- [x] 訂閱結果區分 Accepted、TransientFailure、PermanentFailure（另加 Suppressed：Hub 已拒絕且未強制重訂）。
- [x] 定期續訂與 `NeedRegister` 共用 per-channel in-flight 防重。
- [x] 續訂判斷納入 HMAC secret 缺失／TTL。
- [x] 新增 Bot 單元與 Redis component tests。

### 階段 2：Backend callback 修正

- [x] 在 Backend 定義與 Bot 完全一致的 Redis DB 1 key／pending payload 契約。
- [x] GET challenge 改為 pending action 驗證，不再依賴 `hub.verify_token` 或 User-Agent。
- [x] 加入 `hub.mode=denied` 處理與 denied pending 狀態。
- [x] challenge 成功後驗證 pending、更新 HMAC secret 實際 TTL，並更新 `LastSubscribeTime`。
- [x] POST 使用原始 bytes 驗 HMAC，Content-Type 接受合法參數（`LogMiddleware` 的 bad request 計數也改為 media type）。
- [x] XML parsing 禁止 DTD／外部 entity。
- [x] 無效通知忽略並回 2xx；有效通知快速確認。
- [x] 修正 Deleted 事件 Redis 轉發。
- [x] 新增 Backend 契約與決策測試（controller 的 Redis／MySQL 存取層無 component test 基礎設施，列為未驗證）。

### 階段 3：Atom fallback

- [x] 新增純 Atom parser 與 fixture tests。
- [x] 新增 Atom HTTP 輪詢，支援 ETag、Last-Modified、304、Retry-After。
- [x] 查詢範圍為全部 `YoutubeChannelSpider`，但每個 channel 只抓一次。
- [x] 未知 ID 經既有 claim 後，每 50 筆呼叫 `GetVideosAsync`。
- [x] 沿用 `AddOtherDataAsync`，不新增影片分類分支。
- [x] 只有整個頻道處理成功後更新 validator。
- [x] 加入排程且確認不重入、可取消、只有 Scraper leader 執行。

### 階段 4：整合與文件

- [ ] 驗證 WebSub 與 Atom 同時送入相同 ID 只處理一次。（需要真實環境；設計上收斂於既有 `TryClaimUnknownVideo`／`addNewStreamVideo`）
- [ ] 驗證 Backend 暫停時 Atom 仍能發現新 ID。（需要真實環境）
- [ ] 驗證 Redis DB 1 清空後，Scraper 會補送訂閱而不刪 crawler。（需要真實環境）
- [x] 驗證既有無 query callback 的舊通知在遷移期不造成 5xx 重送風暴。（決策層測試：舊格式 POST 只回 Publish／Ignore，兩者皆 2xx）
- [x] 更新 README 或營運文件中實際需要公開的設定／故障行為；不要寫入單次事故時間線。（README「設定與安全提醒」新增 Redis DB 1 與 Backend 版本需求）
- [x] 更新本文件狀態與實際驗證結果。

## 11. 實際修改位置

### 11.1 Bot

| 路徑 | 實際修改 |
|---|---|
| `src/DiscordStreamNotifyBot.Shared/SharedService/Youtube/YoutubeWebSubContract.cs` | 新增：canonical topic、callback URL、callback token 衍生、Hub 表單、結果分類、pending DTO 與重用政策 |
| `src/DiscordStreamNotifyBot.Shared/SharedService/Youtube/YoutubeWebSubService.cs` | 新增：DB 1 pending／secret 存取（`YoutubeWebSubState`）與訂閱要求（`YoutubeWebSubService`，含 per-channel in-flight） |
| `src/DiscordStreamNotifyBot.Shared/SharedService/Youtube/YoutubeAtomValidatorStore.cs` | 新增：DB 0 ETag／Last-Modified 存取 |
| `src/DiscordStreamNotifyBot.Shared/RedisChannels.cs` | 新增 `YoutubeWebSub` key helper 與 DB 契約 |
| `src/DiscordStreamNotifyBot.Shared/YoutubeApiService.cs` | 移除 `PostSubscribeRequestAsync`（改由 WebSub 服務負責），移除不再使用的 HttpClientFactory 相依 |
| `src/DiscordStreamNotifyBot.Scraper/Detection/Youtube/YoutubeDetectionService.cs` | 續訂選擇（7 天政策＋secret 缺失／TTL）、NeedRegister 補送、owner 強制重訂、Atom 排程註冊、WebSub 與 Atom 共用的 claim 與影片處置入口 |
| `src/DiscordStreamNotifyBot.Scraper/Detection/Youtube/YoutubeDetectionService.Atom.cs` | 新增：Atom 排程、頻道清單、批次 claim 與共用處置入口串接 |
| `src/DiscordStreamNotifyBot.Scraper/Detection/Youtube/YoutubeDetectionService.Schedule.cs` | `AddOtherDataAsync` 回傳發布結果；OtherSchedule 只在成功時完成 claim |
| `src/DiscordStreamNotifyBot.Scraper/Detection/Youtube/YoutubeDetectionService.Reminder.cs` | `ReminderTimerActionAsync`／`HandleStreamStartAsync`／`HandleStreamTimeChangedAsync` 回傳發布結果 |
| `src/DiscordStreamNotifyBot.Scraper/Detection/Youtube/YoutubeAtomFeedParser.cs` | 新增：純 Atom parser |
| `src/DiscordStreamNotifyBot.Scraper/Detection/Youtube/YoutubeAtomFallback.cs` | 新增：條件式 GET、304／429 處理、validator 更新時機 |
| `src/DiscordStreamNotifyBot.Scraper/DetectionHost.cs`、`src/DiscordStreamNotifyBot.Notifier/Bot.cs` | DI 註冊 WebSub 服務與 Atom validator store |
| `src/DiscordStreamNotifyBot.Notifier/SharedService/Youtube/YoutubeStreamService.cs` | `PostSubscribeRequestAsync` 改走 WebSub 服務；unsubscribe 時清除 Atom validator |
| `tests/DiscordStreamNotifyBot.Tests/` | `YoutubeWebSubContractTests`、`YoutubeAtomFeedParserTests`、`YoutubeAtomFallbackTests`、`YoutubeVideoClaimCacheTests`、`Component/Redis/YoutubeWebSubStateRedisComponentTests` |

`YoutubeApiService` 保持無狀態；WebSub pending state 由獨立的小型服務負責。影片分類與通知流程未重寫，只在 `AddOtherDataAsync` 增回傳值並抽出 WebSub／Atom 共用的處置入口。

### 11.2 Backend

| 路徑 | 實際修改 |
|---|---|
| `DiscordStreamBotBackend/YoutubeWebSub/YoutubeWebSubContract.cs` | 新增：topic 解析、callback token 衍生、Content-Type、constant-time 比較、pending DTO |
| `DiscordStreamBotBackend/YoutubeWebSub/YoutubeWebSubCallbackPolicy.cs` | 新增：challenge／denied／通知 POST 的決策、X-Hub-Signature 解析、raw-body HMAC、安全 Atom 解析 |
| `DiscordStreamBotBackend/YoutubeWebSub/YoutubeWebSubStateStore.cs` | 新增：DB 1 pending 與 secret 存取（含 confirmed／denied 標記、lease TTL） |
| `DiscordStreamBotBackend/YoutubeWebSub/YoutubePubSubNotification.cs` | 由 controller 移出，payload 形狀不變 |
| `DiscordStreamBotBackend/Controllers/YouTubeNotificationsController.cs` | 改為 async 薄轉接層：HTTP 狀態碼與 Redis／MySQL 存取 |
| `DiscordStreamBotBackend/Services/RedisService.cs` | `AddYouTubePubMessage` 改為 async 版本，保留既有 publish queue 與 retry |
| `DiscordStreamBotBackend/Middleware/LogMiddleware.cs` | Content-Type 改以 media type 判斷；`/NotificationCallback` 不套用通用 bad request 節流，避免正確的 Hub challenge 被 429 |
| `DiscordStreamBotBackend/RedisChannels.cs`、`Startup.cs` | WebSub key helper 與 state store DI 註冊 |
| `tests/DiscordStreamBotBackend.Tests/YoutubeWebSubContractTests.cs` | 契約、簽章、Atom 解析與決策測試 |

## 12. 測試矩陣

### 12.1 Bot 單元測試

- canonical topic 不含 `/xml/`，channel ID 正確 URL encode。
- callback 包含相同 channel ID。
- callback token 可由兩個 repo 以相同輸入得到相同結果，且 URL 不包含原始 secret。
- request 不再含 `hub.verify`／`hub.verify_token`。
- 200、202、204 都為 Accepted。
- request 使用 `/subscribe`，lease 欄位名稱固定為 `hub.lease_seconds`。
- HMAC secret 少於 200 bytes。
- 429、500、503、timeout 為 TransientFailure。
- 其他 4xx 為 PermanentFailure，且不阻塞後續頻道。
- 同頻道定期續訂與 NeedRegister 同時發生時只送一個 request。
- 未確認 pending 的重試重用相同 secret/token；已確認且到期續訂時才建立新 pending。
- Atom parser 正常解析目前 YouTube feed namespace。
- Atom parser 拒絕 channel mismatch、DTD 與無效 XML。
- 重複 entry 去重，壞 entry 不影響其他合法 entry。
- 304 不呼叫 parser 或 `videos.list`。
- 已知 video ID 不呼叫 `videos.list`。
- 未知 ID 依 50 筆切批，並交給既有 `AddOtherDataAsync`。
- 處理失敗時不更新 ETag／Last-Modified。

### 12.2 Backend 測試

- 有效 pending subscribe 回 2xx 與原 challenge，更新 HMAC secret TTL 與 `LastSubscribeTime`。
- pending 缺失、mode/topic/channel mismatch 回 404，且不改 HMAC secret TTL。
- callback token 缺失或錯誤回 404。
- 合法 denied callback 回 2xx、標記 denied，且不更新 lease／`LastSubscribeTime`。
- denied pending 不由一般週期自動重送，owner 強制重新訂閱可解除。
- challenge 失敗時保留既有 HMAC secret。
- callback response 重試時，同一 confirmed pending 可重複回傳 challenge，不重複建立狀態。
- Redis 或 MySQL 更新失敗回 5xx，pending 保持未確認。
- unsubscribe 成功刪除 HMAC secret，即使 DB row 已先移除。
- User-Agent 缺少或改變不影響有效 challenge。
- `application/atom+xml; charset=utf-8` 可接受。
- HMAC 對原始 bytes 計算，body 任一 byte 改變即失敗。
- signature 比較不使用一般字串相等。
- 無效 signature 不 publish，仍回 2xx。
- secret 缺失會要求重新訂閱；signature 錯誤不會要求重新訂閱。
- CreateOrUpdated 發布 `youtube.pubsub.CreateOrUpdate`。
- Deleted 發布 `youtube.pubsub.Deleted`。
- DTD／外部 entity 不會被解析。

### 12.3 必跑驗證

Bot：

```powershell
dotnet build DiscordStreamNotifyBot.sln -c Release
dotnet test DiscordStreamNotifyBot.sln -c Release
git diff --check
```

Backend：

```powershell
dotnet build DiscordStreamBotBackend.sln -c Release
dotnet test DiscordStreamBotBackend.sln -c Release
git diff --check
```

若 Backend solution 或測試命令與目前工作樹不同，以該 repo 現有入口為準並在本文件記錄實際命令。需要 Redis 的 component tests 若環境不可用，必須明確列出未驗證項目，不得用單元測試結果宣稱真實 Google Hub 已驗證。

## 13. 手動整合驗證

部署前使用測試頻道完成以下驗證，不使用正式大量頻道壓測：

1. 直接 GET canonical Atom feed，確認 channel ID、entry 與 validator 行為。
2. 發出單一 WebSub subscribe，確認 Hub 2xx 後 Backend 收到 challenge。
3. 確認 Redis DB 1 pending 驗證成功，HMAC secret TTL 改為 Hub lease。
4. 確認 MySQL `LastSubscribeTime` 只在 challenge 成功後變更。
5. 發布或修改測試影片，確認有效 WebSub payload 只發布一次 Redis event。
6. 暫停 WebSub callback 或以 fixture 模擬漏送，確認 Atom 下一輪能發現未知 ID。
7. 同一 ID 同時經 WebSub 與 Atom 到達，確認 MySQL、記憶體與 Discord 通知沒有重複。
8. 模擬 503 與 429，確認不密集重試、不清除 crawler。
9. 模擬 Redis DB 1 HMAC secret 遺失，確認定期維護會重新訂閱並建立新 secret。

Google Hub 目前若仍全面 503，可先完成本機與 fixture 驗證，但以下項目必須標示未驗證：真實 challenge、真實 HMAC header、實際 lease、真實通知延遲。

## 14. 上線順序與回復

### 14.1 上線順序

1. 先部署 Backend，使其可接受新 callback query、pending state 與舊 callback POST。
2. 再部署 Bot，開始送出 canonical topic、新 callback 與 pending state。
3. 確認新訂閱 challenge 成功後，再觀察 Atom fallback 與重複事件。
4. 舊 `(legacy topic, common callback)` subscription 不強制大量 unsubscribe，讓其依 Hub lease 自然到期。

不可先部署只會送新格式的 Bot，再保留不認識 pending state 的舊 Backend，否則 Hub challenge 會失敗。

### 14.2 回復策略

- Atom fallback 可先停用其排程而不移除 Redis validator；WebSub 不受影響。
- Backend 回復前要確認 Bot 是否仍在送新 callback query／pending contract，兩端不可退回不相容版本。
- 不以清空全部 Redis 或刪除 `YoutubeChannelSpider` 當回復手段。
- 若新 WebSub 流程有問題，保持 Atom fallback 運作，修正兩端契約後重新訂閱。

## 15. 完成定義

以下全部成立才可把本文件標為已實作：

- 兩個 repo 的 Release build 與適用測試通過。
- Bot 不再送出 `/xml/feeds/`、`hub.verify` 或 `hub.verify_token`。
- Backend challenge 只接受相符 pending action。
- Backend 正確處理 `hub.mode=denied`，不把拒絕通知當成錯誤 challenge。
- HMAC secret 不依賴 `hub.verify_token` 傳遞，challenge 成功後使用實際 lease TTL。
- HMAC 以原始 bytes 驗證，Content-Type 參數不造成誤拒絕。
- Deleted 事件可到達既有 Bot Redis subscriber。
- Atom fallback 涵蓋全部 crawler，支援 HTTP validator，且不呼叫 `search.list`。
- WebSub／Atom 同 ID 的競態不重複通知。
- 真實 Google Hub 未能驗證的項目已清楚記錄。
- 本文件勾選狀態、實際修改路徑與驗證結果已更新。

## 16. 參考資料

- YouTube Push Notifications：<https://developers.google.com/youtube/v3/guides/push_notifications>
- PubSubHubbub Core 0.4：<https://pubsubhubbub.github.io/PubSubHubbub/pubsubhubbub-core-0.4.html>
- W3C WebSub：<https://www.w3.org/TR/websub/>
- Google PubSubHubbub Hub：<https://pubsubhubbub.appspot.com/>
- Google Hub Subscribe 表單：<https://pubsubhubbub.appspot.com/subscribe>
- YouTube Atom topic：`https://www.youtube.com/feeds/videos.xml?channel_id=CHANNEL_ID`
- `videos.list`：<https://developers.google.com/youtube/v3/docs/videos/list>

## 17. 實際驗證結果

### 17.1 已執行命令與結果

Bot（`DiscordStreamNotifyBot/`）：

```powershell
dotnet build DiscordStreamNotifyBot.sln -c Release   # 0 warning / 0 error
dotnet test DiscordStreamNotifyBot.sln -c Release    # 771 passed, 0 failed, 46 skipped（component tests 需環境變數）
$env:REDIS_COMPONENT_OPTION='127.0.0.1:6379,defaultDatabase=10'
dotnet test DiscordStreamNotifyBot.sln -c Release    # 800 passed, 0 failed, 17 skipped（Redis component 全部實跑；
                                                     # MySQL component 另需 MYSQL_TEST_CONNECTION_STRING，未在此設定）
                                                     # 含 YoutubeWebSubStateRedisComponentTests（state 往返、CAS、
                                                     # secret 只建立一次且既有 TTL 不提早延長、相同要求只送一次 Hub、
                                                     # denied 需強制才重送、失敗送出後仍為續訂 due）
git diff --check                                      # 無輸出（僅既有 LF/CRLF 提示）
```

Backend（`DiscordStreamBotBackend/`）：

```powershell
dotnet build DiscordStreamBotBackend.sln -c Release  # 0 warning / 0 error
dotnet test DiscordStreamBotBackend.sln -c Release   # 124 passed, 0 failed
git diff --check                                     # 無輸出（僅既有 LF/CRLF 提示）
```

測試涵蓋：canonical topic／callback／token 衍生向量、request 表單欄位、狀態碼分類、pending JSON 與重用政策、Atom parser（含 DTD、壞 entry、重複 ID 與去重前的 channel 核對）、Atom 輪詢（304、跨頻道去重、單一頻道 5xx 隔離、失敗保留 validator、429 Retry-After）、Backend challenge／denied／POST 決策（含 raw-body HMAC、未知演算法、channel 不符、channel 與 token 成對、缺 secret 才要求重新訂閱、舊格式 callback、僅在帶 query 時先驗簽再解析）。

### 17.2 未驗證項目（需要真實環境或 Redis／MySQL component 基礎設施）

- 真實 Google Hub 的訂閱 challenge、`X-Hub-Signature` header、實際 lease 值與通知延遲。
- Backend controller 的 Redis／MySQL 存取順序與 5xx 行為；本次以決策層測試覆蓋，未以 component test 驗證。
- WebSub 與 Atom 同時送入同一 video ID 時的端到端去重。
- Backend 停止時 Atom fallback 的實際發現時間。
- 清空 Redis DB 1 後 Scraper 的補訂閱行為。
- Backend 專案沒有 Redis component test 基礎設施，`YoutubeWebSubStateStore` 的實際 Redis 行為未以測試驗證（Bot 端的同構 `YoutubeWebSubState` 已驗證）。

手動整合驗證步驟見第 13 節；部署順序與回復方式見第 14 節。

### 17.3 2026-09-19 真實 Atom feed 實測（`UCYxLMfeX1CbMBll9MsGlzmw`）

用與 Bot 相同的 User-Agent 直接抓 canonical feed，結果兩個外部契約與 §9.2／§9.3 的假設不同：

- **端點不支援條件式 GET**：回應只有 `Content-Type: text/xml; charset=UTF-8`、`Date`、`Expires`、`Cache-Control: public, max-age=900`、`Server: YouTube RSS Feeds server`，**沒有 `ETag` 也沒有 `Last-Modified`**；帶 `If-None-Match`（正確值或亂數值）都回 200。因此 validator 永遠不會被寫入，每輪都會無條件下載完整 feed（實測該頻道 25,720 bytes、15 筆 entry）。以 300 個頻道、每 5 分鐘一輪估算，約每天 2.2 GB 與 86,400 次請求；這是頻寬與對外流量，不消耗 YouTube Data API 配額。**這也是 2026-09-19 改為「只補疑似失效頻道 + 15 分鐘間隔」的依據（§5.2）。**
- **伺服器端快取 15 分鐘**：`max-age=900` 代表新影片出現在 feed 的時間可能比上傳晚最多約 15 分鐘，Atom 的實際發現延遲上限因此高於「5 分鐘排程」。
- **根節點 `yt:channelId` 省略 `UC` 前綴**：實測值為 `YxLMfeX1CbMBll9MsGlzmw`（22 字元），而 `self` link、`<id>yt:channel:...`、author uri 與 **entry 的 `yt:channelId`** 都是完整的 24 字元 `UCYxLMfeX1CbMBll9MsGlzmw`。parser 只在根節點是完整 channel ID 時採用，否則改用 self link。
- **頻道 feed 會夾帶其他頻道的 entry**（由實際 log `feed channel 不符` 追查）：`UCqe0-vqZwAvZUb22wCMu1fA` 的 feed 有 15 筆 entry，其中 2 筆（`QlaGDL69HjY`、`CjXemr360js`）的 `yt:channelId` 是 `UCsGWiDe1iLkhvbBGurUP2tg`（合作／翻唱），且這兩筆也存在於該頻道自己的 feed。因此 feed 本身的 channel 必須相符，但 entry 必須逐筆過濾；「任一 entry 不符就整份丟棄」會讓這類頻道的 Atom 永久失效（validator 永遠無法前進、每輪全量重抓）。
- feed 其餘結構與 parser 預期一致（`yt:videoId`、entry `published`／`updated` 帶 `+00:00`、根節點 `link rel="self"`）。

新增測試：`YoutubeAtomFeedParserTests.IgnoresFeedLevelChannelIdWithoutUcPrefix`、`ParsesRealYoutubeFeedShapeWithUnprefixedRootChannelId`、`YoutubeAtomFallbackTests.SkipsEntriesOwnedByAnotherChannelButKeepsItsOwn`。

尚未實測：其他頻道是否都缺 `ETag`／`Last-Modified`（只測了兩個頻道）、以及 validator 程式碼在端點未來支援時的行為。

## 18. 實作後審查與修正

第一輪實作完成後做了一次程式碼審查，以下缺陷已修正（依原嚴重度排序）：

1. **pending 併發覆寫**：兩邊都改成以原始 JSON 做 compare-and-set。Bot 端 `YoutubeWebSubState.TryWritePendingAsync`（含重讀重試）與 Backend 的 `TryMarkConfirmedAsync`／`TryMarkDeniedAsync`／`TrySetSecretTtlAsync` 都只在自己讀到的那筆 pending 仍是當前值時才寫入；unsubscribe 的 secret 刪除與 confirmed 標記放在同一個 transaction，舊 challenge 不會刪掉新訂閱的 secret。
2. **WebSub 未共用 claim**：`youtube.pubsub.CreateOrUpdate` 現在與 Atom 一樣先取得 `TryClaimUnknownVideo` 的 claim，成功才處理、失敗才釋放（`YoutubeDetectionService.cs`）。
3. **Atom 把「其他排程處理中」當成完成**：`ClaimUnknownVideo` 回傳 `Claimed`／`AlreadyKnown`／`Busy`，`Busy` 會讓該頻道保留舊 validator，避免影片永久被跳過。
4. **Atom 看不到發布失敗**：`PublishYoutubeNotificationAsync`、`ReminderTimerActionAsync`、`HandleStreamStartAsync`／`HandleStreamTimeChangedAsync`、`AddOtherDataAsync` 都回傳是否成功；Atom 只在成功時 complete claim 與更新 validator，失敗時把影片從 `addNewStreamVideo` 移除。發布失敗但影片已被 `YT-saveDb` 寫入 MySQL 的情況仍無法補償（見 §18.2 已知不修）。
5. **提前延長 secret TTL 抑制續訂**：`GetOrCreateSecretAsync` 只在建立新 secret 時設 TTL，既有 secret 不提早延長；續訂資格改由 `IsRenewalDueAsync` 判斷（7 天政策、secret 缺失／TTL 低於緩衝、或送出後仍未被 challenge 確認）。
6. **challenge 錯誤處理**：順序改為 secret TTL → MySQL → confirmed；secret TTL 更新失敗（pending 已變更或 secret 消失）回 5xx、MySQL 例外不再被當成 404、DB 更新改在 confirmed 之前且由 `AlreadyConfirmed` 重試補上。
7. **NeedRegister 例外逃出 async void**：整個 callback 包在例外邊界內。
8. **`RedisService.SavePendingMessage` 誤判 payload**：只有 CreateOrUpdate／Deleted 才解析通知 JSON，`NeedRegister` 的裸 channel ID 用訊息雜湊當識別，JSON 解析失敗也不會讓發布工作停止。

計畫誤差一併修正：

- Atom 只有 429／有效 `Retry-After` 才停止本輪，其餘 5xx 與單一頻道例外只記錄並繼續下一個頻道。
- Atom 改用預設 completion mode，讓 `HttpClient.Timeout` 涵蓋 body 讀取；`GetVideosAsync` 接受 `CancellationToken` 且取消不重試，批次間與逐筆處理前都檢查取消。
- 帶 query 的 POST 必須同時提供 `channelId` 與 `token`（只有兩者皆無才算舊格式），且先用 query channel 驗 token／HMAC 再解析 XML；GET 的 challenge 一律要求 `channelId` 與 topic 一致。
- feed 本身的 channel 必須相符，但 entry 逐筆過濾（見 §17.3 的實測）：會夾帶其他頻道影片的 feed 不再被整份丟棄。
- Atom validator store 固定使用 Redis DB 0，不再跟著連線的 `defaultDatabase`。
- Bot 送出要求的診斷摘要會先移除 secret／token（含 URL-encoded 形式）再截短。
- README 對 Redis DB 1 的說明改為實際用途與影響。

仍有已知限制（未修改）：`AddOtherDataAsync` 的通知發布失敗仍不會進入重試佇列（沿用既有匯流排行為，本次只讓 Atom 不誤判為成功）。

### 18.1 第二輪審查後的追加修正

- **WebSub 與 Atom 共用影片處置入口**：新增 `ProcessDiscoveredVideoAsync`，兩邊都先判斷頻道是否已認可（錄影頻道／2434／trusted crawler）再決定走 `AddOtherDataAsync` 或非認可影片流程（偽裝貼文判定＋`NonApproved`）。原先 Atom 直接呼叫 `AddOtherDataAsync`，會讓非認可頻道的影片落到 `OtherVideos` 而非 `NonApprovedVideos`。
- **claim 判斷順序**：`ClaimUnknownVideo` 改為先判斷影片是否已知（`addNewStreamVideo`／DB）再搶 claim。已完成的 claim 會留在快取 24 小時，先搶 claim 會讓 Atom 把剛處理過的影片當成「處理中」，validator 整整一天無法前進、每 5 分鐘重抓完整 feed。
- **claim 只在處理成功時完成**：WebSub 訂閱者、Atom 與 OtherSchedule 都改為 `ProcessDiscoveredVideoAsync`／`AddOtherDataAsync` 回傳 true 才 `claims.Complete`，失敗則交給 batch 釋放以便下輪重試。
- 新增測試：`YoutubeVideoClaimCacheTests.CompletedClaimStillBlocksLaterBatchesUntilTtl`（記錄「已完成 claim 仍在快取」這個必須先判斷已知的原因）。

### 18.2 第二輪審查後的追加修正（連線失敗以外的缺陷）

以下項目與 MySQL／Redis 連線失敗無關，已修正：

- **永久失敗也遵守 `Retry-After`**：`YoutubeWebSubService.SendAsync` 先解析並設定全域退避，再區分 Permanent／Transient（計畫 §8.2）。
- **Polly 不再把 HTTP timeout 當成取消**：只有自己的 `CancellationToken` 被取消才不重試（`YoutubeApiService.GetVideosAsync`）；先前寫法連 `TaskCanceledException`（timeout）都不重試。
- **Atom parser 讀取 feed 根節點的 `yt:channelId`**：優先於 self link，兩者矛盾時整份視為無效（計畫 §9.3）。
- **損壞的 pending 可被取代**：`GetPendingSnapshotAsync` 保留原始 JSON、`Action` 為 null，決策上視為沒有 pending，但 CAS 仍以原始值為條件，因此下一次要求能覆寫損壞資料，不再需要人工清 Redis。
- **`AlreadyConfirmed` 完全冪等**：Hub 重試同一 challenge 時不再改寫 `LastSubscribeTime`（第一次確認的順序是 secret TTL → DB → confirmed，重試改寫只會讓訂閱時間漂移、延後續訂）。
- **denied 的 CAS 失敗會重讀重試**：pending 真的被換掉才視為 superseded；仍同一筆要求時再標記一次。
- **`hub.reason` 與 pending 解析錯誤不再可能帶出 token**：denied 記錄前移除 callback token（含 URL-encoded 形式），`TryParse` 的錯誤訊息不再夾帶 `JsonException.Message`。
- **待重試訊息的識別不再用雜湊**：改用 `channel|payload`，不同頻道的 `NeedRegister` 不會互相覆蓋。
- **Atom 只補疑似失效的頻道、間隔 15 分鐘**（2026-09-19 使用者決策）：候選條件為 `LastSubscribeTime < 現在 − 7 天`（含從未成功者），與續訂判斷共用 `YoutubeWebSubContract.RenewAfter`；沒有候選頻道時整輪不發任何 HTTP。測試：`YoutubeAtomFallbackTests.NoCandidateChannelsMakesNoRequest`。

已知但不修（觸發條件是 Redis 發布失敗或連線異常，影響可接受）：

- 通知發布失敗時，影片若已被 `YT-saveDb` 寫入 MySQL，該則通知不會重試（N2）：匯流排本身沒有 outbox，重試機制不屬本次範圍。
- reminder 的延後重試在稍後發布失敗時不會再重排（N3）：同上，只有 Redis 發布失敗才會發生。
- 跨程序送出鎖的 TTL 是兩分鐘，前置 Redis 作業超過 20 秒時可能與 HTTP 重疊（N6）：需要 Redis 回應極慢才會發生，且 pending CAS 仍保證狀態一致。
- Backend 標記狀態時用的是 transaction 外讀到的 pending TTL，可能把 Bot 剛延長的值縮回（N8）：兩邊的 pending TTL 上限都是 10 天，差異只有毫秒級。
