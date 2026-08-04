# Twitch OAuth 與零成本 EventSub 實作計畫

> 狀態：三個 repo 程式碼、migration、文件與靜態建置已完成；第 0 階段正式環境確認及第 17 節執行期情境仍待部署環境驗證。
>
> 建立日期：2026-07-19
>
> 執行方式：新 session 從第一個未勾選的 checkbox 開始，完成後立即更新本文件。若當次使用者明確要求提交，再依階段建立 commit。

## 0. 涉及專案

| 角色 | 路徑 | 職責 |
|---|---|---|
| Bot | `W:\Discord\DiscordStreamNotifyBot` | Twitch spider、EventSub 註冊、Scraper 偵測、Discord 通知、Prometheus/Grafana |
| Backend | `W:\Discord\DiscordStreamBotBackend` | Discord/Google/Twitch OAuth、token 保存與驗證、EventSub Webhook 接收 |
| Frontend | `E:\repos\_konnokai\auto-discord-ytmember-checker` | Discord 登入後的 Google/Twitch 綁定介面 |

實際工作樹路徑目前分別對應：

```text
\\192.168.98.22\Projects\Discord\DiscordStreamNotifyBot
\\192.168.98.22\Projects\Discord\DiscordStreamBotBackend
E:\repos\_konnokai\auto-discord-ytmember-checker
```

## 1. 不可偏離的決策

1. EventSub Webhook 的建立、查詢、刪除一律使用 **App Access Token**，不可改用 broadcaster user token。
2. broadcaster OAuth 的用途是授權同一個 Twitch Client ID，讓 `stream.online`、`channel.update`、`stream.offline` 的 subscription cost 變成 0。
3. Backend 與 Bot 必須使用同一個 Twitch Application / Client ID。
4. 已授權且受監控的 broadcaster 永久註冊三種 EventSub。
5. 未授權 broadcaster 保留目前 30 秒輪詢，以及直播期間暫時建立 `channel.update`、`stream.offline` 的行為。
6. `DeleteEventSubSubscriptionAsync(userId)` 維持「刪除該 broadcaster 的全部 EventSub」語意，必須處理所有分頁，不限制 type。
7. **任何全刪 EventSub 前都必須先確認 broadcaster 已離線。直播中不得刪除，避免遺失 `channel.update` 與 `stream.offline`。**
8. 已授權的 `IsWarningUser` 視為一般已授權 broadcaster，使用永久三種 EventSub。
9. `IsWarningUser` 授權失效後回到目前行為：保留輪詢，不建立任何 EventSub。
10. 已授權使用者新增自己的 Twitch 頻道 spider 時，可豁免 200 人限制，但不可豁免 `MaxTwitchSpiderCount` 或其他限制。
11. 授權失效後，若 spider 持有 guild 不符合既有 200 人資格，等目前直播結束後自動移除 `TwitchSpider`。
12. 自動移除 `TwitchSpider` 時保留 `NoticeTwitchStreamChannel`，通知設定成為暫時不生效的 dormant 設定。
13. Twitch OAuth 為選用功能，不影響 Discord + Google 會員綁定完成條件。
14. Google 與 Twitch OAuth 必須使用不同的 start、state、callback，不可互相吃到對方的 authorization code。
15. 設定檔只填公開域名，固定 callback path 由程式組合。
16. Prometheus 採各服務獨立 endpoint，Grafana dashboard 整合多個 job。
17. 不新增新的基礎設施；沿用 MySQL、Redis、Prometheus、Grafana。

## 2. 現況基線

### 2.1 Bot

- `TwitchDetectionService` 每 30 秒輪詢全部 `TwitchSpider`。
- 發現開台後建立 `channel.update` v2 與 `stream.offline` v1。
- 目前沒有建立或消費 `stream.online`。
- `stream.offline` 三分鐘後呼叫 `DeleteEventSubSubscriptionAsync`，目前會刪除查到的全部 subscription。
- `IsWarningUser` 目前仍會輪詢並發開台通知，但不建立 EventSub。
- `AddChannelSpider` 目前使用 `[RequireGuildMemberCount(200)]`，precondition 會在解析 Twitch user ID 前執行。
- guild member count 的既有資格是 `MemberCount >= 200`；Bot 擁有者與官方 guild 豁免。
- `TwitchSpider` 對 Twitch user ID 是全域唯一，`GuildId` 代表 spider 的持有伺服器。

### 2.2 Backend

- 已有 `TwitchOAuthController`，但前端尚未接線。
- 舊版程式曾將 Twitch token 設計為存放於 Redis DB 1，但正式環境沒有需要遷移的既有資料。
- token 沒保存 Twitch user ID，Bot/Scraper 也不讀取此資料。
- Twitch OAuth 目前使用共用 `RedirectUrl`，scopes 為 `moderation:read` 與 `user:read:subscriptions`。
- Google callback 也使用相同 `RedirectUrl`。
- EventSub Webhook callback path 是 `/TwitchWebHooks`。
- Backend 只轉發 `channel.update`、`stream.offline` 到 Redis Pub/Sub。
- Webhook publish queue 容量只有 1，`TryAdd` 失敗會靜默丟事件。

### 2.3 Frontend

- Discord callback 由 `DiscordSection.vue` 處理。
- Google callback 由 `GoogleSection.vue` 處理。
- Google 目前把所有非 `state=discord` 的 OAuth code 視為 Google callback，直接新增 Twitch callback 會互相干擾。
- 正式前端部署於 Cloudflare Pages，網域是 `https://stream-bot.konnokai.me`。
- 正式 Backend API 網域是 `https://api.konnokai.me`。

## 3. 目標狀態矩陣

| 授權 | 監控 | `IsWarningUser` | 偵測模式 | EventSub | 關台後 |
|---|---|---:|---|---|---|
| 有效 | 有 | false | `stream.online` Webhook 為主，低頻補償 | 永久三種，預期 cost 0 | 保留全部 subscription |
| 有效 | 有 | true | 同一般已授權 broadcaster | 永久三種，預期 cost 0 | 保留全部 subscription |
| 有效 | 無 | 任意 | 不偵測 | 不建立 | 保存授權，待未來新增 spider |
| 無授權 | 有 | false | 現有 30 秒輪詢 | 開台後暫時 update/offline | 關台三分鐘後全刪 |
| 無授權 | 有 | true | 現有 30 秒輪詢 | 不建立 | 不處理 EventSub |
| 失效 | 有且 guild 合格 | false | 本場直播結束後回到輪詢 | 直播中保留；離線後全刪 | 保留 spider |
| 失效 | 有且 guild 合格 | true | 本場直播結束後回到目前 warning 行為 | 直播中保留；離線後全刪 | 保留 spider，未來不建 EventSub |
| 失效 | 有且 guild 不合格 | 任意 | 直播中暫時保留；離線後停止 | 直播中保留；離線後全刪 | 移除 spider |

## 4. 安全刪除狀態機

授權失效或使用者解除 Twitch 連結時，執行以下順序：

1. Backend 將授權標記為 revoked，不立即實體刪除 row，讓其他服務能觀察狀態。
2. Scraper 收到狀態變更提示，或在定期 reconcile 時讀到 revoked。
3. Scraper 先用 Helix `Get Streams` 確認 broadcaster 是否在線。
4. Redis `twitch:stream_data:{userId}` 只能當提示，不能單獨判定離線。
5. Twitch API 查詢失敗時採保守策略：不刪 EventSub、不刪 spider，標記 pending 並重試。
6. 若正在直播，保留現有全部 EventSub，將 broadcaster 加入補償輪詢與 pending cleanup。
7. 若已離線，才允許呼叫全刪 EventSub。
8. EventSub 全刪後，再依 guild 資格決定保留或移除 spider。
9. pending 期間若重新授權成功，取消 pending delete 與 pending spider removal。

### 4.1 直播中授權失效

- 不選擇性刪除 `stream.online`，因為刪除 helper 的既定語意是全刪。
- 暫時保留三種 subscription，即使 cost 已從 0 變成非零。
- 保留 `channel.update` 與 `stream.offline`，確保目前直播通知完整。
- 立即加入低頻或現有 30 秒補償輪詢，避免 offline callback 遺失後永遠不清理。
- 收到 `stream.offline` 後沿用三分鐘去抖動，再重新讀授權與 guild 狀態。
- 沒收到 offline callback 時，由補償輪詢確認已離線後執行相同清理。

### 4.2 關台後重新判斷

三分鐘去抖動完成後必須重新讀取最新狀態，不可沿用 callback 進來時的舊判斷：

| 最新狀態 | 動作 |
|---|---|
| 授權已恢復 | 不刪 EventSub，不移除 spider |
| 授權失效、guild 合格 | 全刪 EventSub，保留 spider並回到輪詢 |
| 授權失效、guild 不合格 | 全刪 EventSub，移除 spider |
| 從未授權的既有一般頻道 | 維持目前關台後全刪 EventSub |
| 從未授權的 warning 頻道 | 維持目前不建立 EventSub 的行為 |

## 5. Guild 資格與 OAuth 豁免

### 5.1 一般 guild 資格

以下任一成立即符合資格：

- 指令執行者是 Bot 擁有者。
- `TwitchSpider.GuildId == 0`，代表由 Bot 擁有者持有。
- guild 在官方伺服器清單。
- guild `MemberCount >= 200`。

### 5.2 新增 spider 的 OAuth 豁免

必須同時符合：

- 授權 row 有效且未 revoked。
- 授權 row 的 Twitch Client ID 等於 Bot 的 `TwitchClientId`。
- 授權 row 的 Discord user ID 等於 Slash 指令執行者 ID。
- 授權 row 的 Twitch user ID 等於欲新增的 Twitch 頻道 ID。

修改方式：

- 從 Twitch `AddChannelSpider` 移除 `[RequireGuildMemberCount(200)]`。
- 先呼叫 Twitch API 解析目標 user ID。
- 再於方法內判斷「一般 guild 資格 OR OAuth 豁免」。
- 只有 200 人條件可被 OAuth 豁免。
- `MaxTwitchSpiderCount`、已存在 spider、指令權限、Twitch API 驗證全部維持。

### 5.3 授權失效時的 guild 查詢

- Scraper 從 Redis `cluster:stats:guild_snapshot` 讀跨 shard guild member count。
- 應把 `GuildSnapshot` DTO 搬到 Shared 或新增 Shared 等價契約，避免 Scraper 參考 Notifier 專案。
- 找到 guild 且少於 200：若離線立即移除；若直播中標記關台後移除。
- 找到 guild 且至少 200：保留 spider。
- `GuildId == 0` 或官方 guild：保留 spider。
- 暫時查不到 guild：不立即移除。
- 至少跨過一次既有 15 分鐘 snapshot 更新後仍查不到，且 Notifier 心跳正常，再確認一次後視為 Bot 已退出該 guild並移除。
- Notifier 整體異常或 snapshot 讀取失敗時禁止自動刪除。

## 6. 設定與固定 URI

Backend 移除共用 `RedirectUrl`，改為只填公開域名：

```json
{
  "FrontendDomain": "https://stream-bot.konnokai.me",
  "ApiServerDomain": "https://api.konnokai.me"
}
```

規則：

- 必須包含 `https://`；本機開發可使用 `http://localhost`。
- 不包含結尾 `/`。
- 啟動時驗證為 absolute URI。
- 不從 HTTP `Host` header 動態產生 callback URL。
- CORS 只使用 `FrontendDomain`。

程式固定組合：

```text
Discord redirect:       {FrontendDomain}/
Google callback:        {ApiServerDomain}/oauth/google/callback
Twitch callback:        {ApiServerDomain}/oauth/twitch/callback
OAuth frontend return:  {FrontendDomain}/
```

正式環境 Provider Console 設定：

```text
Discord Developer Portal:
https://stream-bot.konnokai.me/

Google Cloud Console:
https://api.konnokai.me/oauth/google/callback

Twitch Developer Console:
https://api.konnokai.me/oauth/twitch/callback
```

開發環境建議：

```json
{
  "FrontendDomain": "http://localhost:3333",
  "ApiServerDomain": "https://dev-api.konnokai.me"
}
```

開發 Provider Console 另加入：

```text
Discord: http://localhost:3333/
Google:  https://dev-api.konnokai.me/oauth/google/callback
Twitch:  https://dev-api.konnokai.me/oauth/twitch/callback
```

Backend README 必須新增欄位表：

| 欄位 | 正式值 | 用途 |
|---|---|---|
| `FrontendDomain` | `https://stream-bot.konnokai.me` | Cloudflare Pages 前端公開網域、CORS、Discord callback、OAuth 完成返回頁面 |
| `ApiServerDomain` | `https://api.konnokai.me` | Backend 公開網域，產生 Google/Twitch callback URL |

README 同時列出上述三個 Provider Console 完整 URI，避免部署者只填 domain 卻忘記第三方白名單。

## 7. OAuth API 與流程隔離

### 7.1 API

建議固定路由：

```text
POST   /oauth/discord/callback
POST   /oauth/google/start
GET    /oauth/google/callback
POST   /oauth/twitch/start
GET    /oauth/twitch/callback
GET    /account-links
DELETE /account-links/google
DELETE /account-links/twitch
```

### 7.2 State

- start endpoint 使用 `Authorization: Bearer <DT>` 識別 Discord 使用者。
- 不再把 `DT` 放入 query string 或 OAuth state。
- state 使用密碼學安全亂數。
- state TTL 5 到 10 分鐘。
- Google 與 Twitch 使用不同 Redis key namespace。
- state 綁定 Discord user ID、provider、FrontendDomain。
- callback 原子消耗 state，避免 replay。

建議 Redis key：

```text
oauth:state:google:{nonce}
oauth:state:twitch:{nonce}
```

### 7.3 Callback

- Google authorization code 只會送到 `/oauth/google/callback`。
- Twitch authorization code 只會送到 `/oauth/twitch/callback`。
- Backend 完成交換、驗證、保存後，302 回前端。
- 前端只收到 `provider` 與 `result`，不收到 code、state、access token、refresh token。

成功範例：

```text
https://stream-bot.konnokai.me/?provider=google&result=success
https://stream-bot.konnokai.me/?provider=twitch&result=success
```

失敗範例：

```text
https://stream-bot.konnokai.me/?provider=twitch&result=error&reason=authorization_denied
```

`reason` 必須是固定低敏感錯誤碼，不能包含 provider 原始訊息、code、state 或 token。

### 7.4 Twitch scopes

- 移除未使用的 `moderation:read`。
- 第一版只保留 `user:read:subscriptions`，維持單一實際 OAuth scope 以取得 broadcaster authorization。
- 不呼叫不需要的 Twitch API，不將 subscription 資料用於其他用途。
- 前端與隱私權政策明確說明 Twitch 登入只用於 Twitch spider。

## 8. 授權資料模型

新增 `twitch_broadcaster_authorization`，migration 的唯一權威在 Bot Shared。

建議欄位：

| 欄位 | 用途 |
|---|---|
| `twitch_user_id` | 主鍵；broadcaster ID |
| `discord_user_id` | 綁定的 Discord 使用者，唯一索引 |
| `client_id` | 驗證是否與 Bot Twitch Application 相同 |
| `user_login` | Twitch login |
| `display_name` | 顯示名稱 |
| `profile_image_url` | 前端顯示 |
| `encrypted_access_token` | Backend 專用加密 token |
| `scopes` | JSON 字串或等價格式 |
| `token_expires_at` | access token 到期時間 |
| `last_validated_at` | 最近 Twitch validate 時間 |
| `authorized_at` | 最近授權時間 |
| `revoked_at` | 失效/解除時間，null 表示有效 |
| `date_updated` | 最後更新時間 |

約束：

- `twitch_user_id` 唯一。
- `discord_user_id` 唯一，一個 Discord 帳號只能管理一個 Twitch 授權。
- 同 Twitch + 同 Discord 重登採 upsert並 rotation refresh token。
- 同 Discord 改綁另一個 Twitch 時要求先解除舊連結。
- Bot/Scraper 不解密 provider token。
- Backend 使用既有 `Token:Redis` 相容加密契約保存 token。
- Backend 不建立自己的 migration；兩個 DbContext 映射同一張表。

## 9. Backend token 維護

- 新增 hosted service，在啟動時及每小時驗證 Twitch token。
- validate 回傳的 Client ID 必須與設定相同。
- access token 無效時嘗試 refresh。
- refresh 成功時保存新的 access token 與 refresh token。
- refresh 失敗、使用者 revoke、Client ID 不符才標記 revoked。
- access token 自然到期但 refresh 成功不算授權失效。
- 解除連結時先呼叫 Twitch revoke，再標記 revoked並清除 encrypted token。
- 授權狀態變更後 publish Redis 提示；資料庫仍是最終真實來源。

新增內部 Redis 契約：

```text
twitch:authorization_changed
```

payload 至少包含 Twitch user ID 與固定狀態碼，不包含 token。

## 10. Backend EventSub Webhook

修改 `EventSubHostedService`：

- 增加 `StreamOnline` handler。
- publish `twitch:stream_online`。
- 保留 `twitch:channel_update`、`twitch:stream_offline`。
- 增加 EventSub revocation handler 與 Prometheus counter。
- callback secret 繼續使用現有 Twitch WebHook secret，但啟動時驗證 Backend config 與 Redis DB 0 `twitch:webhook_secret` 一致。

修改 `RedisService`：

- 移除容量 1 且忽略 `TryAdd` 的靜默丟事件行為。
- 改為有 backpressure 的 async queue，或至少保證 enqueue 失敗會被觀察並計數。
- 不改既有 Redis channel 字串。

Shared 新增：

```csharp
RedisChannels.Twitch.StreamOnline = "twitch:stream_online";
RedisChannels.Twitch.AuthorizationChanged = "twitch:authorization_changed";
```

新增外部契約後同步更新 `AGENTS.md`。

## 11. Bot EventSub 與偵測

### 11.1 `TwitchApiService`

- EventSub CRUD 明確使用 App Access Token。
- 新增 `stream.online` v1。
- Get/Delete 全面支援 pagination。
- `DeleteEventSubSubscriptionAsync(userId)` 刪除該 broadcaster 查到的全部 subscription。
- 建立前精確比對 type、version、condition、callback、transport、status。
- 保留完整 response，取得 `Cost`、`TotalCost`、`MaxTotalCost`。
- 已授權建立三種永久 subscription並驗證 cost。
- 未授權沿用現有暫時 update/offline。
- 修正 `GetNowStreamsAsync` chunk loop 錯誤傳入完整 `twitchUserIds`，應傳入當前 `item`。

### 11.2 `TwitchDetectionService`

- 訂閱 `twitch:stream_online` 與 `twitch:authorization_changed`。
- 抽出 Webhook 與 polling 共用的直播開始處理流程。
- 收到 online callback 後呼叫 Helix `Get Streams` 補齊完整直播資料。
- 以 Stream ID、DB 紀錄、Redis state 去重。
- 已授權 broadcaster 排除於高頻一般輪詢，但保留低頻補償 reconcile。
- `IsWarningUser` 在授權有效時不再阻止 Redis stream state與 EventSub。
- 授權失效後按第 4 節安全狀態機處理。
- pending cleanup 必須可在 Scraper 重啟後從 DB authorization + TwitchSpider + Twitch live state重新推導。

### 11.3 Reconcile

執行時機：

- Scraper 啟動。
- 每 5 到 10 分鐘。
- 收到 authorization changed。
- 新增 spider 後。
- 刪除 spider 後。

工作內容：

- 授權有效且有 spider：確保永久三種 subscription。
- 授權有效但無 spider：不建立 subscription。
- 授權失效且離線：全刪並套用 guild 清理政策。
- 授權失效且在線：保留 subscription，標記 pending。
- 清理 callback failed、錯誤 version、錯誤 callback 或非 enabled 狀態。
- 記錄 cost、total cost、max cost。

## 12. Frontend

新增 `src/components/TwitchSection.vue`，Discord 登入後與 Google 一起顯示。

部署與路由：

- 部署於 Cloudflare Pages custom domain `stream-bot.konnokai.me`。
- Vite 只建置單一 `index.html`，根路徑 `/` 同時顯示服務介紹與帳號連結介面。
- Discord callback 與 Google/Twitch 完成返回都回到 `/`，不再使用 `/stream/` 或 `/login/`。
- `/privacy` 與 `/terms` 由同一個 Vue SPA entry 顯示；`public/_redirects` 提供 Pages SPA fallback。
- 舊路由不提供重新導向或相容頁面。

流程：

1. 使用者完成 Discord 登入。
2. 頁面同時查詢 Google 與 Twitch link status。
3. Google 維持會員驗證必要項目。
4. Twitch 明確標示為選用。
5. Google 成功後提示可繼續完成 Twitch，但不可未經點擊自動跳轉 provider。
6. Twitch 成功、失敗、取消、解除都不可改變 Google 狀態。
7. callback query 處理後重新查 `/account-links`並清除 provider/result query。

Twitch 區塊必須包含以下意思：

> Twitch 登入為選用功能，僅用於設定 Twitch 直播爬蟲，不會用於其他用途。完成授權後，即使伺服器未滿 200 人，也可以新增您自己的 Twitch 頻道爬蟲。若授權失效，系統會重新檢查設定該爬蟲的伺服器人數；未滿 200 人時將自動移除爬蟲，相關 Twitch 通知也會停止。若頻道當下正在直播，會等待該場直播結束後才移除，避免遺失直播更新及關台通知。

前端狀態：

- 未連結
- 正在授權
- 已連結
- 授權失效
- 已撤銷
- 發生錯誤

完成條件：

- Discord + Google 仍顯示「已完成綁定」。
- Twitch 未連結不阻止完成。
- 解除 Twitch 前顯示可能自動移除 spider 的警告。
- 更新隱私權政策，說明 Twitch profile、token 保存、用途、撤銷與 spider 清理規則。

## 13. Prometheus

採三個 scrape endpoint：

| 服務 | Endpoint |
|---|---|
| Coordinator | `:9464/metrics`，維持現況 |
| Scraper | 新增 `:9465/metrics` |
| Backend | ASP.NET 現有 HTTP server 的 `/metrics` |

Backend request middleware 必須排除 `/metrics` access log、錯誤計數與 rate limit，避免每 15 秒 scrape 製造大量 log。

### 13.1 Backend 指標

```text
discord_stream_notify_oauth_attempts_total{provider,result}
discord_stream_notify_oauth_linked_accounts{provider,status}
discord_stream_notify_oauth_token_validations_total{provider,result}
discord_stream_notify_oauth_token_refreshes_total{provider,result}
discord_stream_notify_twitch_webhook_events_total{type,result}
discord_stream_notify_twitch_webhook_queue_dropped_total
discord_stream_notify_twitch_webhook_last_received_unixtime{type}
```

### 13.2 Scraper 指標

```text
discord_stream_notify_twitch_spiders{mode}
discord_stream_notify_twitch_eventsub_subscriptions{type,mode,status}
discord_stream_notify_twitch_eventsub_total_cost
discord_stream_notify_twitch_eventsub_max_total_cost
discord_stream_notify_twitch_reconcile_total{result}
discord_stream_notify_twitch_reconcile_last_success_unixtime
discord_stream_notify_twitch_poll_cycles_total{result}
discord_stream_notify_twitch_authorization_changes_total{result}
discord_stream_notify_twitch_spider_removals_total{reason}
discord_stream_notify_twitch_spider_cleanup_pending
discord_stream_notify_twitch_eventsub_cleanup_deferred{reason}
discord_stream_notify_twitch_oauth_bypass_additions_total
```

固定低基數 `mode`：

```text
oauth
fallback
warning
unmonitored
```

不得把 Twitch user ID、Discord user ID、Guild ID、subscription ID 放入 Prometheus label。

## 14. Grafana

更新 `deploy/grafana/dashboards/coordinator-prometheus.json`，擴充為整體服務 dashboard。

新增 template variables：

- Coordinator job / instance
- Scraper job / instance
- Backend job / instance

新增 panels：

- Google/Twitch OAuth 成功與失敗率
- Twitch 授權有效、撤銷、失效帳號數
- Token validation與 refresh失敗
- Twitch spider mode 分布
- EventSub type / mode / status 數量
- EventSub total cost、max cost、使用率
- 最近一次 reconcile 距今
- Webhook 接收率、錯誤率、最後接收時間
- Webhook queue drop
- Polling 成功率
- 授權失效後自動移除 spider 數
- 等待 guild 資格確認的 cleanup 數
- 因直播中而延後 EventSub cleanup 的數量

同步更新：

- `docs/PROMETHEUS_GRAFANA.md`
- Bot `docker-compose.yml`，發布 Scraper 9465
- Prometheus 三個 scrape job 範例
- Dashboard description、tags、version

## 15. 預期修改檔案

### 15.1 Bot

- `src/DiscordStreamNotifyBot.Shared/DataBase/MainDbContext.cs`
- `src/DiscordStreamNotifyBot.Shared/DataBase/Table/TwitchBroadcasterAuthorization.cs`（新增）
- `src/DiscordStreamNotifyBot.Shared/Migrations/*`（新增 migration + snapshot）
- `src/DiscordStreamNotifyBot.Shared/RedisChannels.cs`
- `src/DiscordStreamNotifyBot.Shared/SharedService/Twitch/TwitchApiService.cs`
- `src/DiscordStreamNotifyBot.Scraper/Detection/Twitch/TwitchDetectionService.cs`
- `src/DiscordStreamNotifyBot.Scraper/Program.cs`
- `src/DiscordStreamNotifyBot.Scraper/DiscordStreamNotifyBot.Scraper.csproj`
- `src/DiscordStreamNotifyBot.Notifier/Interaction/Twitch/TwitchSpider.cs`
- Shared guild snapshot DTO / cluster contract相關檔案
- `src/DiscordStreamNotifyBot.Coordinator/CoordinatorMetrics.cs`（若需共同 dashboard 說明，不承載 Scraper 指標）
- `docker-compose.yml`
- `deploy/grafana/dashboards/coordinator-prometheus.json`
- `docs/PROMETHEUS_GRAFANA.md`
- `AGENTS.md`

### 15.2 Backend

- `DiscordStreamBotBackend/Controllers/TwitchOAuthController.cs`
- `DiscordStreamBotBackend/Controllers/YouTubeMemberController.cs` 或拆出的 Google OAuth controller
- 新增 account links controller / OAuth state service
- `DiscordStreamBotBackend/Services/EventSubHostedService.cs`
- `DiscordStreamBotBackend/Services/RedisService.cs`
- 新增 Twitch token validation hosted service
- `DiscordStreamBotBackend/DataBase/MainDbContext.cs`
- 新增對應 authorization entity
- `DiscordStreamBotBackend/Middleware/LogMiddleware.cs`
- `DiscordStreamBotBackend/Startup.cs`
- `DiscordStreamBotBackend/DiscordStreamBotBackend.csproj`
- `DiscordStreamBotBackend/appsettings.json`
- `config/appsettings.Production.example.json`
- `README.md`

### 15.3 Frontend

- `src/page/VerifyWindow.vue`
- `src/components/GoogleSection.vue`
- `src/components/TwitchSection.vue`（新增）
- 可選：共用 API client、account-link types、OAuth result helper
- `src/main.ts`
- `src/App.vue`
- `src/page/PrivacyPage.vue`
- `src/page/TermsPage.vue`
- `public/_redirects`
- `README.md`

## 16. 執行階段

### 階段 0：前置確認

- [ ] 確認 Backend 與 Bot Twitch Client ID 完全相同。
- [ ] 確認正式 Twitch WebHook secret 與 Redis DB 0 值相同。
- [x] 確認正式環境沒有需要遷移的 Redis Twitch token，不加入舊版相容流程。
- [x] 確認不保留舊 Twitch OAuth endpoint 相容期。
- [ ] 在 Google/Twitch/Discord Provider Console 加入第 6 節 URI。

完成定義：部署前提與既有資料量已確認，不依猜測加入相容程式碼。

> 2026-08-04 確認正式 Redis DB 1 沒有舊版 Twitch OAuth token，因此移除啟動期 SCAN、token migration、unlink tombstone 與 Discord 維度 coordination lock。MySQL 是 provider token 的唯一資料來源，Redis DB 1 僅保留跨 Bot／Backend 的 refresh lock。

### 階段 1：資料模型與 Backend 設定

- [x] Bot Shared 新增 authorization entity、DbSet、migration、snapshot。
- [x] Backend 映射相同 table。
- [x] `RedirectUrl` 改成 `FrontendDomain`、`ApiServerDomain`。
- [x] 更新 Backend appsettings sample 與 README。
- [x] 產生 idempotent migration SQL並人工審核。

完成定義：兩個 DbContext model一致，正式 SQL可安全套用既有 DB。

### 階段 2：Google/Twitch OAuth 隔離

- [x] 實作 provider-specific start/state/callback。
- [x] Google 不再用長效 `DT` 當 OAuth state。
- [x] Twitch callback validate Client ID 與 Twitch user ID。
- [x] 實作 `/account-links` 與 unlink endpoints。
- [x] 實作 Twitch hourly validation / refresh / revoke。
- [x] OAuth code/state/token 不進入 access log。
- [x] 授權狀態變更 publish Redis提示。
- [x] Discord 登入使用 browser-bound 隨機 state，code exchange 改為 `POST /oauth/discord/callback` JSON body，並移除舊 `/DiscordCallBack`。
- [x] 移除 `/GoogleCallBack`、`/GetGoogleData`、`/UnlinkGoogle`，不保留舊前端相容端點。

完成定義：Google 與 Twitch 可在同一次前端 session 依序完成，互不處理對方 callback。

### 階段 3：Frontend

- [x] 新增 Twitch optional UI。
- [x] 改用 account links status API。
- [x] 處理 provider/result，不處理 Google/Twitch code。
- [x] 加入授權失效與直播中延後移除提示。
- [x] 更新隱私權政策與 README。
- [x] 改為 Cloudflare Pages 單一 SPA entry，根路徑直接顯示帳號連結介面並移除 `/stream/`、`/login/`。
- [x] 正式前端網域改為 `https://stream-bot.konnokai.me`，Discord 與 provider 完成返回統一使用 `/`。
- [x] provider callback 結果在狀態查詢成功前暫存於 `sessionStorage`，避免 Discord session 失效或暫時 API 錯誤時遺失結果。

完成定義：Discord + Google完成條件不變，Twitch可跳過，三種 callback 狀態不互相污染。

> 2026-07-20 本機驗證：Frontend 已移至 `E:\repos\_konnokai\auto-discord-ytmember-checker`，使用專案指定的 pnpm 11.12.0 完成 frozen lockfile 安裝、production build、`lint:script` 與 `lint:style`，三項驗證均為 exit code 0 且 lint 無輸出。

### 階段 4：Twitch add資格與授權清理

- [x] Twitch add移除 precondition並改成方法內資格判斷。
- [x] 驗證 Discord actor與 Twitch target完全匹配才套 OAuth 豁免。
- [x] 保留 MaxTwitchSpiderCount。
- [x] Shared提供 guild snapshot contract給 Scraper。
- [x] 實作 snapshot missing重試與 Notifier健康守衛。
- [x] 自動移除只刪 TwitchSpider，不刪通知設定。

完成定義：未滿 200 人只有授權本人能新增自己的 Twitch 頻道；失效後不會因暫時 snapshot缺失誤刪。

### 階段 5：StreamOnline 與 EventSub reconcile

- [x] Backend接收並 publish `stream.online`。
- [x] 修正 Backend Webhook queue丟事件問題。
- [x] Shared新增 Redis channel constants。
- [x] Twitch API CRUD支援 pagination、cost、精確 ensure與全刪。
- [x] Scraper消費 stream online。
- [x] 已授權 warning頻道走永久 EventSub。
- [x] 實作安全刪除狀態機與補償輪詢。
- [x] 修正 Get Streams 100筆 chunk bug。

完成定義：已授權頻道以三種 0-cost Webhook運作；授權失效發生於直播中時不會遺失 update/offline。

### 階段 6：Prometheus 與 Grafana

- [x] Backend加入 `/metrics`。
- [x] Scraper加入 `:9465/metrics`。
- [x] 實作第 13 節低基數指標。
- [x] `/metrics` 不寫一般 access log。
- [x] Docker Compose發布 Scraper metrics port。
- [x] 更新 Grafana dashboard JSON。
- [x] 更新 Prometheus/Grafana文件與 scrape範例。

完成定義：三個 endpoint均可 scrape，dashboard顯示 OAuth、EventSub、spider cleanup與成本狀態。

### 階段 7：文件與整體驗證

- [x] 更新 `AGENTS.md` 架構狀態與 Redis外部契約。
- [x] 更新本文件所有已完成 checkbox。
- [x] 執行三 repo build/lint。
- [x] 執行 idempotent migration script檢查。
- [ ] 執行完整手動情境驗證。
- [x] Bot code修改後執行 `graphify update .`。

完成定義：文件、程式碼、部署設定、dashboard與實際行為一致。

## 17. 驗證矩陣

### 17.1 新增 spider

- [ ] 未滿 200 人且未授權：拒絕新增。
- [ ] 未滿 200 人，actor已授權相同 Twitch ID：允許新增。
- [ ] actor授權不同 Twitch ID：不得豁免。
- [ ] 其他 Discord使用者嘗試新增已授權 Twitch ID：不得使用授權者的豁免。
- [ ] OAuth豁免仍受 MaxTwitchSpiderCount限制。
- [ ] 官方 guild與 Bot擁有者原有豁免不變。

### 17.2 EventSub

- [ ] 已授權離線頻道存在三種 enabled subscription且 cost為 0。
- [ ] 已授權開台只發一次開始通知。
- [ ] channel update正常。
- [ ] stream offline正常。
- [ ] 已授權關台後三種 subscription仍存在。
- [ ] 未授權一般頻道仍由 30秒 polling發現開台。
- [ ] 未授權一般頻道直播期間只有暫時 update/offline。
- [ ] 未授權一般頻道關台後全刪 EventSub。
- [ ] 未授權 warning頻道不建立 EventSub。
- [ ] 已授權 warning頻道使用永久三種 EventSub。

### 17.3 授權失效

- [ ] 離線 + guild >=200：立即全刪，保留 spider並回退 polling。
- [ ] 離線 + guild <200：立即全刪並移除 spider。
- [ ] 直播中 + guild >=200：不刪 EventSub；關台後全刪並保留 spider。
- [ ] 直播中 + guild <200：不刪 EventSub；關台後全刪並移除 spider。
- [ ] 直播中 + warning：目前直播 update/offline不遺失；關台後不再建立 EventSub。
- [ ] Twitch API暫時失敗：不刪 EventSub或 spider。
- [ ] offline callback遺失：補償 polling仍能完成清理。
- [ ] pending期間重新授權：取消刪除與移除。
- [ ] snapshot暫時缺失：不立即移除。
- [ ] Notifier異常：不因 snapshot缺失移除。

### 17.4 OAuth

- [ ] Discord後可依序完成 Google與Twitch。
- [ ] Google callback不觸發 Twitch handler。
- [ ] Twitch callback不觸發 Google handler。
- [ ] Twitch取消不影響 Google狀態。
- [ ] 未登入 Twitch仍可完成 Discord + Google。
- [ ] state單次使用且過期不可重放。
- [ ] Client ID不符拒絕保存授權。
- [ ] refresh token rotation正確保存。
- [ ] unlink會 revoke並觸發安全清理流程。

### 17.5 Prometheus/Grafana

- [ ] Coordinator `:9464/metrics`正常。
- [ ] Scraper `:9465/metrics`正常。
- [ ] Backend `/metrics`正常。
- [ ] OAuth success/failure counter正常。
- [ ] EventSub cost/max cost正常。
- [ ] deferred cleanup gauge正常。
- [ ] auto-removal counter正常。
- [ ] Webhook queue drop維持0。
- [ ] Dashboard所有 panel有資料。
- [ ] 指標沒有 broadcaster/guild/user高基數 labels。

## 18. 建置與遷移

Bot：

```powershell
dotnet build DiscordStreamNotifyBot.sln -c Release
dotnet ef migrations script --idempotent --project src/DiscordStreamNotifyBot.Shared -o migrate.sql
graphify update .
```

Backend：

```powershell
dotnet build DiscordStreamBotBackend.sln -c Release
```

Frontend：

```powershell
pnpm build
pnpm lint:script
pnpm lint:style
```

正式 DB 禁止執行 `dotnet ef database update`。只允許人工審核後於維護窗口套用 idempotent SQL。

## 19. 部署順序

1. 備份並套用經審核的 DB migration SQL。
2. 部署 Backend的新 schema mapping、OAuth API、token validation、Webhook與 metrics。
3. 確認 MySQL `twitch_broadcaster_authorization` 是唯一 token 資料來源，Redis DB 1 僅用於 refresh lock。
4. 部署 Bot/Scraper的新 EventSub雙模式、guild policy與 metrics。
5. 驗證 Client ID、Webhook secret、FrontendDomain、ApiServerDomain。
6. 手動以測試 Twitch broadcaster完成授權，確認三筆 cost 0。
7. 於維護窗口同步啟用 Backend 新 `FrontendDomain`、Bot 新網站連結與 Cloudflare Pages production deployment，綁定 `stream-bot.konnokai.me`並驗證 SPA fallback。
8. 匯入更新後 Grafana dashboard。
9. 觀察至少一個完整直播週期及一次授權失效情境。

為避免前端先產生授權但 Scraper尚未支援，Frontend必須最後部署。

## 20. 官方參考

- Twitch Managing EventSub Subscriptions：<https://dev.twitch.tv/docs/eventsub/manage-subscriptions/>
- Twitch EventSub Subscription Types：<https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/>
- Twitch Validating Tokens：<https://dev.twitch.tv/docs/authentication/validate-tokens>
- Twitch OAuth Authorization Code Flow：<https://dev.twitch.tv/docs/authentication/getting-tokens-oauth>
- Cloudflare Pages Redirects：<https://developers.cloudflare.com/pages/configuration/redirects/>
- Cloudflare Pages Custom Domains：<https://developers.cloudflare.com/pages/configuration/custom-domains/>
- EventSub cost RFC：<https://discuss.dev.twitch.com/t/rfc-0014-eventsub-subscription-limit-changes/30312>
