# Twitch 訂閱驗證實作計畫

> 狀態：程式實作完成；待正式 DB migration、component environment 與 Twitch/Discord 手動驗收
>
> 建立日期：2026-08-03

## 1. 目標

新增完整的 Twitch 訂閱驗證流程：

1. 使用者在帳號連結網站完成 Discord 與 Twitch 綁定。
2. Discord 伺服器管理員設定要驗證的 Twitch 頻道及共用訂閱者身分組。
3. Bot 依管理員指定的身分組名稱，自動建立 Tier 1、Tier 2、Tier 3 身分組。
4. 使用者執行獨立的 Twitch 訂閱驗證指令。
5. 系統向 Twitch 查詢目前訂閱狀態，依結果新增、替換或移除 Discord 身分組。
6. 系統定期重新驗證，處理取消訂閱、Prime、贈送訂閱及 Tier 升降級。

YouTube 會員驗證的指令與資料不變。Twitch 使用獨立指令、資料表及背景服務。

## 2. 涉及專案

| 專案 | 路徑 | 職責 |
|---|---|---|
| Bot | `DiscordStreamNotifyBot` | 管理員設定、Slash 指令、token 讀取與刷新、Helix 訂閱查詢、資料表、身分組同步 |
| Backend | `DiscordStreamBotBackend` | Twitch OAuth 登入、token 保存與定期驗證、授權狀態通知 |
| Frontend | `auto-discord-ytmember-checker` | Twitch OAuth 入口、帳號狀態及授權用途說明 |

## 3. 已確認的產品規則

1. 功能範圍是完整 Discord 驗證，不只在網站顯示訂閱狀態。
2. 使用獨立的 `/twitch-member` 與 `/twitch-member-set` 指令，不改現有 `/member` 與 `/member-set` 契約。
3. Twitch 訂閱驗證與既有 Twitch EventSub broadcaster 授權共用同一個 Twitch 帳號連結及 token。
4. 現有 OAuth scope `user:read:subscriptions` 保留，不新增第二套 Twitch OAuth 流程。
5. 管理員新增設定時，參數比照 YouTube `add-member-check`：指定 Twitch 頻道及一個既有 Discord 身分組。
6. 管理員指定的身分組是所有有效訂閱者都會取得的共用身分組。
7. 系統依共用身分組名稱自動建立三個身分組：
   - `{共用身分組名稱} Tier 1`
   - `{共用身分組名稱} Tier 2`
   - `{共用身分組名稱} Tier 3`
8. Tier 身分組只作為等級標示，不複製共用身分組的 Discord 權限。
9. 有效訂閱者取得「共用身分組 + 目前 Tier 身分組」。升降級時只替換 Tier 身分組。
10. Prime 訂閱依 Twitch 回傳值視為 Tier 1；贈送訂閱依實際 Tier 通過驗證。
11. Twitch `Check User Subscription` 不回傳 `plan_name`，因此不依 Twitch 訂閱方案名稱建立角色。

## 4. Twitch API 契約

使用以下 Helix API：

```http
GET https://api.twitch.tv/helix/subscriptions/user
    ?broadcaster_id={broadcasterId}
    &user_id={linkedTwitchUserId}
Authorization: Bearer {userAccessToken}
Client-Id: {clientId}
```

要求：

- 使用 User Access Token。
- token 必須包含 `user:read:subscriptions`。
- `user_id` 必須等於 token 所代表的 Twitch 使用者。
- `broadcaster_id` 必須是 Twitch Affiliate 或 Partner。

回應分類：

| Twitch 回應 | 系統結果 | Bot 行為 |
|---|---|---|
| `200`，`tier=1000` | Tier 1 | 新增共用及 Tier 1 身分組，移除 Tier 2、3 |
| `200`，`tier=2000` | Tier 2 | 新增共用及 Tier 2 身分組，移除 Tier 1、3 |
| `200`，`tier=3000` | Tier 3 | 新增共用及 Tier 3 身分組，移除 Tier 1、2 |
| `404` | 未訂閱 | 移除共用及三個 Tier 身分組 |
| `401` | token 或 scope 失效 | Bot 刷新 token 後重試一次；仍失敗則要求重新連結 |
| `429` | Twitch rate limit | 保留現有身分組，依 reset 時間稍後重試 |
| `5xx` 或網路錯誤 | 暫時失敗 | 保留現有身分組，稍後重試 |
| 其他 `4xx` | 請求或設定錯誤 | 不把使用者判為未訂閱，記錄錯誤並通知管理員 |

## 5. Backend 責任

### 5.1 共用現有 Twitch 授權

沿用：

- `TwitchOAuthController`
- `TwitchAuthorizationService`
- `TwitchBroadcasterAuthorization`
- `TwitchTokenValidationHostedService`
- `twitch_broadcaster_authorization`
- `twitch:authorization_changed`

不建立第二筆 subscriber token，也不變更 Discord 與 Twitch 帳號的一對一限制。實體資料表名稱暫時保留，避免為功能新增不必要的 rename migration。

### 5.2 維持登入與定期驗證

Backend 維持以下責任：

1. 建立 Twitch OAuth URL、交換 authorization code，並驗證 Client ID、Twitch user ID 及 `user:read:subscriptions`。
2. 將 Access Token 與 Refresh Token 加密後保存至 `twitch_broadcaster_authorization`。
3. 由 `TwitchTokenValidationHostedService` 定期驗證 token；需要時刷新並保存 rotation 後的新 token。
4. token 確定無法刷新時標記授權失效，並發布 `twitch:authorization_changed`。
5. 處理使用者解除連結、provider revoke 及 account conflict。

Backend 不新增 Twitch 訂閱查詢 API，也不接收 Bot 的訂閱驗證請求。訂閱資格、Tier 判斷及 Discord 身分組操作全部留在 Bot。

### 5.3 與 Bot 共用 token 契約

Bot 與 Backend 沿用 YouTube 會員驗證現有模式，共用 MySQL token 資料及同一把 provider token 加密金鑰。兩端的 Twitch token model、JSON 欄位與加解密格式必須保持一致。

現有正式環境的金鑰值必須原樣保留，否則 MySQL 內既有 Google 與 Twitch token 將無法解密。本功能不旋轉金鑰，也不要求重加密既有資料；只調整金鑰的命名、注入方式及保存位置：

1. Bot 將 `BotConfig.RedisTokenKey` 改名為 `ProviderTokenEncryptionKey`。
2. Backend 將 `Token:Redis` 改為語意一致的 provider token encryption key 設定。
3. Bot 與 Backend 皆由部署 secret 明確提供相同值，啟動時驗證不得為空且至少 64 字元，不再自動產生金鑰。
4. `MySqlDataStore` 透過 constructor 或 options 明確取得金鑰，不再讀取全域靜態 `Utility.RedisKey`。
5. 金鑰不得保存至 Redis，也不得透過 Redis pub/sub 同步。

Twitch refresh token 可能在刷新後 rotation。為避免 Backend hourly validator 與 Bot 訂閱檢查同時刷新同一筆 token，兩端刷新前都必須：

1. 取得同一個 Redis 分散式鎖，例如 `twitch:oauth:refresh-lock:{twitchUserId}`。
2. lock value 使用每次取得時產生的唯一 owner，設定 TTL 並定期續租；寫入前以 owner-aware Lua 確認仍持有 lock，釋放時使用 compare-and-delete。
3. 取得鎖後重新讀取 MySQL row，不使用鎖外讀到的 token。
4. 重新驗證最新 Access Token；只有仍需刷新時才呼叫 refresh endpoint。
5. 先保存新的加密 token，再釋放鎖。
6. 鎖逾時、owner 已變更或暫時無法取得時，不移除角色，將本次驗證視為暫時失敗。

不得新增 Bot-to-Backend service token，也不得使用 HTTP 讓 Bot 向 Backend 查詢訂閱狀態。

### 5.4 移除舊 Redis token storage 與金鑰同步

MySQL 已是 provider token 的唯一 datastore，因此移除 Bot 與 Backend 內沒有 production 呼叫點的 `RedisDataStore` 及其專用測試。這不代表移除 Redis；Redis 仍用於 refresh lock、授權狀態通知、Notification Bus 及既有叢集協調。

一併移除只服務舊金鑰同步流程的項目：

- `Utility.RedisKey`
- `RedisTokenKeyProvisioner`
- `BotConfig.GenRandomKey` 中只供該流程使用的邏輯
- `cluster:redis_token_key`
- `member.syncRedisToken`
- 上述 channel、provisioner 與 datastore 的契約測試

刪除前須再次搜尋三個 repo 及外部部署設定，確認沒有仍在運行的 consumer。現行 Backend 沒有 `member.syncRedisToken` subscriber；若部署環境仍混跑舊版服務，應先完成版本切換，再移除 publication。

## 6. Bot 實作

### 6.1 Bot token 與 Helix 查詢

Bot 新增 Twitch 訂閱驗證服務，直接：

1. 依 Discord user ID 讀取 `TwitchBroadcasterAuthorization`。
2. 確認授權未撤銷、Client ID 正確、token 密文存在，且保存的 scope 包含 `user:read:subscriptions`。
3. 使用 `TokenManager.GetTokenResponseValue<TwitchAccessTokenData>` 與注入的 provider token encryption key 解密 token。
4. 直接呼叫 Twitch `GET /helix/subscriptions/user`。
5. Access Token 過期或 API 回傳 401 時，依第 5.3 節的共用鎖規則刷新、保存並重試一次。
6. refresh token 確定失效時更新授權 row，發布 `twitch:authorization_changed`，並把結果分類為 `AuthorizationInvalid`。

訂閱查詢服務回傳明確結果型別，指令與角色服務不得解析 Twitch 錯誤訊息文字：

```text
Subscribed
NotSubscribed
AuthorizationMissing
AuthorizationInvalid
BroadcasterUnavailable
TemporaryFailure
```

`Subscribed` 另外帶回 `Tier`、`IsGift`、Twitch user ID 及 broadcaster ID。Access Token、Refresh Token 及密文不得進入指令結果或角色同步 DTO；明文 token 只能存在於目前正在執行 OAuth、驗證、刷新或 Helix 查詢的服務程序內。

### 6.2 GuildTwitchSubscriptionConfig

每個 guild 與 Twitch broadcaster 一筆設定：

```text
Id
GuildId
BroadcasterId
BroadcasterLogin
BroadcasterDisplayName
SubscriberRoleId
PreviousSubscriberRoleId（共用身分組更新尚未同步完成時保留舊 ID，完成後清空）
DeletionPending（設定刪除及 Discord 身分組清理尚未完成）
Tier1RoleId
Tier2RoleId
Tier3RoleId
DateAdded
```

建立 `(GuildId, BroadcasterId)` 唯一索引。

### 6.3 TwitchSubscriptionCheck

保存使用者參加的驗證及最後結果：

```text
Id
GuildId
DiscordUserId
BroadcasterId
Locale
IsChecked
PendingRoleRemoval
Tier
IsGift
LastCheckTime
DateAdded
```

建立 `(GuildId, DiscordUserId, BroadcasterId)` 唯一索引。

`Tier` 只接受 `1000`、`2000`、`3000` 或 null。`IsChecked=false` 代表尚未完整驗證成功，不代表 Twitch 已確認未訂閱。`PendingRoleRemoval=true` 代表 Twitch 已明確回報未訂閱、使用者已取消或授權已失效，但 Discord 角色清理暫時失敗；背景週期只重試角色清理，不會重新授予角色。

### 6.4 EF Core migration

由 `src/DiscordStreamNotifyBot.Shared` 管理 migration：

1. 新增兩個 entity 與 `DbSet`。
2. 在 `MainDbContext.OnModelCreating` 設定唯一索引及欄位長度。
3. 產生 migration。
4. 產生上一版至新版的冪等增量 SQL。
5. 更新 `migrate_sql/all.sql`。
6. Backend 不映射這兩張表，因為訂閱驗證狀態與 Discord 身分組只由 Bot 管理。

## 7. 自動角色管理

### 7.1 新增設定

指令參數比照 YouTube `add-member-check`：

```text
/twitch-member-set add-subscription-check channel-url:<Twitch 頻道> role:<共用訂閱者身分組>
```

流程：

1. 解析 Twitch URL 或 login，透過 Twitch API 取得 broadcaster ID 與 display name。
2. 確認 broadcaster 是 Affiliate 或 Partner；若 API 無法直接確認，允許建立設定，但首次驗證若回傳 broadcaster 不可用，須通知管理員。
3. 檢查 Bot 具有 `ManageRoles`。
4. 拒絕 `@everyone`、managed role、位置高於或等於 Bot 最高角色的共用身分組。
5. 以管理員指定角色的當下名稱建立三個 Tier 身分組。
6. Tier 身分組不帶權限、不 hoist、不可 mention，並放在共用身分組附近且低於 Bot 最高角色。
7. 保存共用角色與三個 Tier 角色 ID。

重複設定同一個 broadcaster 時，視為更新：

- 更新共用身分組 ID。
- 依新的共用身分組名稱重新命名既有 Tier 身分組。
- Tier 身分組遺失時重建並保存新 ID。
- 不依角色名稱判斷角色歸屬，角色 ID 才是真實來源。
- `DeletionPending=true` 時拒絕更新，且不在使用者可驗證頻道、選單或清單中顯示。
- `PreviousSubscriberRoleId` 尚未清空時，只允許使用目前保存的共用身分組重跑修復；不得直接改成第三個角色而遺失中間角色的清理狀態。

### 7.2 驗證成功

任何 Tier 的訂閱者都必須持有共用身分組，並只持有一個目前 Tier 身分組：

| Twitch Tier | 應持有 | 應移除 |
|---|---|---|
| `1000` | 共用、Tier 1 | Tier 2、Tier 3 |
| `2000` | 共用、Tier 2 | Tier 1、Tier 3 |
| `3000` | 共用、Tier 3 | Tier 1、Tier 2 |

新增與移除角色皆採 idempotent 操作。API 驗證成功但 Discord 角色操作失敗時，不得把該次記錄標成完整成功。

### 7.3 未訂閱與授權失效

- Twitch 明確回傳 `404`：移除共用及三個 Tier 身分組，刪除或停用該驗證記錄。
- 使用者解除 Twitch 連結：收到 `twitch:authorization_changed` 後，各 shard 清除自己持有 guild 的驗證記錄及角色。
- token 確定失效且無法刷新：行為同解除連結，並通知使用者重新登入網站綁定。
- 429、5xx、timeout：保留角色與既有成功記錄，稍後重試。

### 7.4 移除設定

移除設定前要求管理員確認。確認後：

1. 移除該 broadcaster 的所有驗證記錄。
2. 從成員移除共用身分組及三個 Tier 身分組。
3. 刪除系統建立的三個 Tier 身分組。
4. 不刪管理員指定的共用身分組，因為該角色可能仍供其他功能使用。
5. 刪除 `GuildTwitchSubscriptionConfig`。

開始 Discord 清理前必須先保存 `DeletionPending=true`。每小時背景週期會重試完整刪除流程，即使驗證記錄已全部清除，仍須繼續刪除 Tier 身分組及設定列。

## 8. Slash 指令

### 8.1 使用者指令 `/twitch-member`

| 指令 | 行為 |
|---|---|
| `check` | 顯示本 guild 可驗證的 Twitch 頻道；單一頻道直接驗證，多個頻道使用選單 |
| `cancel-subscription-check` | 取消本 guild 的 Twitch 訂閱驗證並移除相關角色 |
| `show-my-twitch-account` | 顯示目前連結的 Twitch 帳號；未連結或失效時導向網站 |
| `list-can-check-channel` | 列出本 guild 已設定的 Twitch 頻道與共用身分組 |

首次驗證直接由 Bot 呼叫 Twitch，不等待背景排程。互動先 `DeferAsync(true)`，並只允許原發起者操作選單。

### 8.2 管理員指令 `/twitch-member-set`

| 指令 | 行為 |
|---|---|
| `add-subscription-check` | 指定 Twitch 頻道及共用角色，建立或更新三個 Tier 角色 |
| `remove-subscription-check` | 移除指定 Twitch 頻道的驗證設定 |
| `list-checked-member` | 分頁顯示已驗證使用者、頻道及目前 Tier |

驗證狀態紀錄沿用 `GuildConfig.LogMemberStatusChannelId`，不再新增第二個 log channel 設定指令。

所有一般使用者訊息需提供 `zh-TW`、`en-US`、`ja`；管理員及營運 log 維持繁體中文。

## 9. 背景驗證與 shard 行為

1. 首次驗證由 Slash 指令立即執行。
2. 已驗證記錄每小時重新確認一次。
3. 每個 Notifier 只處理自己持有的 guild，沿用 `Bot.IsServerOnThisShard` 與 `Bot.ShouldDeleteMissingGuild` 規則。
4. 同一 Discord 使用者可能在多個 guild 驗證同一 broadcaster；單次排程可快取該使用者與 broadcaster 的 Twitch 查詢結果，避免重複呼叫 Twitch。
5. 429 必須讀取 Twitch response header 的 reset 資訊，不可立即重試迴圈。
6. Twitch refresh endpoint 已接受 rotation 後，保存重試不再受一般關閉 cancellation 中止；Notifier 優雅關閉必須續租並等待 replacement token 保存或確認狀態已 stale，不能把唯一有效 token 留在記憶體後回報關閉完成。
7. 開啟 `EnableGuildMembersIntent` 時：
   - 使用者重新加入 guild，依最後有效記錄補回共用及 Tier 身分組。
   - 定期移除沒有有效驗證記錄的孤兒 Twitch 訂閱角色。
8. 未開啟 `EnableGuildMembersIntent` 時，不訂閱 member join，也不下載完整成員清單；定期 Twitch API 驗證及明確的角色新增、移除仍正常運作。

### 9.1 Bot metrics

在 Notifier 新增低基數指標：

- 訂閱查詢結果：`subscribed`、`not_subscribed`、`authorization_invalid`、`temporary_failure`。
- Tier：`1000`、`2000`、`3000`、`unknown`。
- token 解密、驗證及刷新結果。
- 已接受但尚未保存的 refresh rotation 數量，以及關閉等待狀態與耗時。
- Twitch 429 及 provider error。

不得把 Discord user ID、Twitch user ID、guild ID 或 broadcaster ID 放入 Prometheus label。

## 10. Frontend 調整

Frontend 不新增驗證 API，也不在網站判斷特定 guild 的訂閱資格。網站只負責帳號綁定。

修改項目：

1. `TwitchSection.vue`
   - 標題與說明改為同時支援 Twitch 訂閱驗證及自己的直播爬蟲。
   - 說明解除連結會停止訂閱身分組同步，也可能影響未滿 200 人伺服器的直播爬蟲。
2. `VerifyWindow.vue`
   - Twitch OAuth 成功訊息加入訂閱驗證用途。
   - Twitch 解除連結訊息加入訂閱身分組清理提示。
3. `HomePage.vue`
   - 說明 Discord + Twitch 綁定後，需回 Discord 執行 `/twitch-member check`。
4. `PrivacyPage.vue`
   - 揭露會保存 Twitch user ID、加密 token、授權 scope、驗證結果與最後檢查時間。
   - 說明會呼叫 Twitch API 檢查使用者對管理員設定頻道的訂閱狀態及 Tier。
5. README
   - 更新 OAuth 用途及完整流程。

`accountLinks.ts`、OAuth callback query 及 `/account-links` 既有 JSON 契約不需修改。

## 11. 安全與錯誤處理

1. Twitch token 與 YouTube 會員 token 一樣，由 Bot 直接從共用 MySQL 讀取、解密及在需要時刷新；Backend 不提供訂閱查詢 API。
2. Bot 與 Backend 只能使用部署 secret 提供的 provider token encryption key 解密 provider token；金鑰、密文及明文 token 都不得傳入 Discord interaction、Redis message 或 HTTP API。
3. Bot 與 Backend 刷新 Twitch token 時必須使用相同的分散式鎖及鎖後重讀規則，避免 refresh token rotation 競態。
4. 記錄 log 時不得輸出 Access Token、Refresh Token、加密 token 或完整 Authorization header。
5. `404` 才能判定未訂閱；timeout、429、5xx 不得移除角色。
6. role ID 是角色操作的真實來源，角色名稱只用於初次建立及管理員更新設定時重新命名。
7. 所有自動角色操作前重新確認 Bot 的 `ManageRoles` 及角色階層。
8. 不把 user ID、guild ID 或 broadcaster ID 放入 Prometheus label。

## 12. 自動化測試

### 12.1 Backend

- OAuth URL 必須包含 `user:read:subscriptions`。
- OAuth callback 保存的 Twitch token 可由 Bot 共用 model 與金鑰解密。
- 啟動時缺少 provider token encryption key、兩端金鑰不一致或金鑰長度不足時明確失敗。
- hourly validator 刷新 token 時遵守共用 Redis 鎖及鎖後重讀規則。
- token 無效、refresh rotation、revoke 與 `twitch:authorization_changed` 行為維持正確。
- 確認沒有新增 Bot-to-Backend 訂閱查詢端點。

### 12.2 Bot

- Tier 1/2/3 各自得到共用角色及正確 Tier 角色。
- Twitch token 密文可正確解密，Client ID、user ID、scope 不符時拒絕查詢。
- `MySqlDataStore` 使用注入的 provider token encryption key，不依賴 `Utility.RedisKey` 或 Redis key provisioning。
- 200 的 `1000`、`2000`、`3000` 正確映射。
- `is_gift=true` 不影響通過結果。
- 401 刷新 token 後只重試一次，並保存 rotation 後 token。
- Bot 與 Backend validator 同時要求刷新時只執行一次有效 rotation。
- 升級、降級時替換 Tier 角色。
- Prime 映射 Tier 1，贈送訂閱依 tier 通過。
- 404 移除四個角色。
- 暫時失敗保留全部現況。
- OAuth 解除連結清除驗證資料與角色。
- 重複設定不重複建立角色。
- 共用角色改名後，重新設定會同步三個 Tier 角色名稱。
- Tier 角色被手動刪除後可重建。
- 移除設定只刪系統建立角色，不刪共用角色。
- 缺少 Manage Roles、角色高於 Bot、`@everyone`、managed role 的拒絕路徑。
- migration 與唯一索引 component tests。
- 刪除待處理狀態查詢、零驗證記錄刪除重試、shared-role 中間狀態與 refresh rotation 關閉 drain policy。
- Slash command contract 及三語本地化測試。
- 多 shard 只處理所屬 guild。

### 12.3 Frontend

```powershell
pnpm build
pnpm lint:script
pnpm lint:style
```

## 13. 手動驗收

至少準備一個 Twitch Affiliate／Partner 測試頻道及以下帳號狀態：

1. 未連結 Twitch。
2. 已連結但未訂閱。
3. Prime 或 Tier 1。
4. Tier 2。
5. Tier 3。
6. 贈送訂閱。
7. 升級及降級。
8. 取消訂閱。
9. Twitch token 過期後成功刷新。
10. 使用者解除 Twitch 連結。

另驗證：

- 新增設定後角色名稱、位置及權限正確。
- 重複設定不產生重複角色。
- 共用角色不會在移除設定時被刪除。
- Bot 缺少 Manage Roles 時不會留下半套設定或部分角色。
- 429、Twitch 5xx、timeout 時不會誤刪角色。
- 使用者離開及重新加入 guild 的行為。
- 至少兩個 shard 的 guild ownership。
- Twitch EventSub crawler 不受訂閱驗證功能影響。

## 14. 實作順序

1. 定義 Bot 與 Backend 共用的 Twitch token model、加密格式及 Redis refresh lock 契約。
2. 保留既有金鑰值，改用 provider token encryption key 設定與明確注入。
3. 移除 `RedisDataStore`、`Utility.RedisKey`、Redis 金鑰 provisioning 及 `member.syncRedisToken`。
4. Backend hourly validator 套用共用鎖與鎖後重讀規則。
5. Bot token 讀取、解密、刷新、Helix 查詢及純結果分類 policy。
6. Bot entity、DbContext、migration 與 SQL。
7. Bot 自動角色建立、更新、移除。
8. `/twitch-member-set` 管理員指令。
9. `/twitch-member` 使用者指令。
10. 每小時複驗、OAuth 失效事件及 GuildMembers intent 補償流程。
11. 三語本地化、指令契約、metrics 及測試。
12. Frontend 文案、隱私權及 README。
13. 三個 repo 建置與手動整合驗收。
14. 更新本計畫狀態、`AGENTS.md` 架構摘要及部署文件。

## 15. 驗證指令

Backend：

```powershell
dotnet build DiscordStreamBotBackend.sln -c Release
dotnet test DiscordStreamBotBackend.sln -c Release
```

Bot：

```powershell
dotnet build DiscordStreamNotifyBot.sln -c Release
dotnet test DiscordStreamNotifyBot.sln -c Release
```

Frontend：

```powershell
pnpm build
pnpm lint:script
pnpm lint:style
```

## 16. 部署順序

1. 在 Bot 與 Backend 部署設定預先加入新的 provider token encryption key 設定，值必須等於目前正式環境使用中的金鑰；切勿產生新值。
2. 產生並人工審核 Bot repo 的 migration SQL。
3. 在維護窗口套用增量 SQL。
4. 部署 Backend，確認可解密既有 token、token validation 與共用 refresh lock 正常。
5. 部署 Bot，確認可解密既有 token、查詢 Twitch，且新 Slash 指令已在每個 shard 正常註冊。
6. 確認沒有舊版 `member.syncRedisToken` consumer 後移除舊設定與 publication。
7. 部署 Frontend 說明文字。
8. 執行一輪未訂閱、Tier 1、Tier 2、Tier 3 及解除連結驗收。
9. 觀察 Backend token validation 指標、Notifier Twitch 訂閱查詢與角色操作指標及錯誤 log。

若共用 refresh lock 需要修改 Backend，應先部署 Backend，再部署 Bot，避免兩端使用不同的 token rotation 規則。Frontend 可以最後部署，因 OAuth scope 及 callback 契約沒有改變。

## 17. 官方文件

- [Check User Subscription](https://dev.twitch.tv/docs/api/reference/#check-user-subscription)
- [Twitch OAuth Scopes](https://dev.twitch.tv/docs/authentication/scopes/)
- [Twitch API Rate Limits](https://dev.twitch.tv/docs/api/guide/#twitch-rate-limits)
