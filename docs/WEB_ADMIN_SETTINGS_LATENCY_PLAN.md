# 網頁管理設定：30 秒請求與背景清理實作計畫

> 狀態：程式碼與自動化驗證已完成；正式 Discord／Redis／MySQL／多 shard 行為仍待依既有清單手動驗收。

## 1. 目標

縮短網頁管理設定的等待時間，讓管理設定相關端點的單次 request flow 在 30 秒內完成並回覆前端；驗證設定刪除則先保存 durable cleanup intent，立即回覆 `pending`，Discord role 清理交由既有背景週期處理。

本文件是 `WEB_ADMIN_SETTINGS_PLAN.md` 與 `WEB_ADMIN_CRAWLER_VERIFICATION_PLAN.md` 的 follow-up。若與兩份文件的舊描述衝突，以本文件下列已確認決策為準。

## 2. 已確認決策

- 30 秒是管理設定 request 的端到端 deadline，不是 Redis reply 的獨立等待時間。
- Backend timeout 回傳 HTTP `504`，穩定 code 為 `settings.timeout`，並保留 `correlationId`。
- Redis 無 subscriber、Redis 連線錯誤或 Bot 無法提供服務，仍回 HTTP `503`、`settings.unavailable`；不可誤報成 timeout。
- snapshot 讀取不恢復每次 owner／Administrator 的 Discord guild 重驗證。`/admin/guilds` 的 manageable guild 清單是前端選擇入口；snapshot 路徑只保留 session、guild 存在與 owning-shard 檢查。
- mutation 仍保留 Backend 的即時 owner／Administrator 驗證，因為 mutation 會寫入設定或觸發 Discord 變更。
- 驗證設定刪除先寫入 `DeletionPending`、`PendingRoleRemoval` 與 active-state fence，之後才處理 Discord role；背景清理完成後才物理刪除資料列。
- 不新增 queue、服務或資料表；沿用現有 YouTube／Twitch pending cleanup 與週期工作。
- YouTube 管理員選定的授予 role 不自動刪除。Twitch Bot 建立的 Tier roles 可沿用既有 cross-platform ownership 與 protected-config 檢查後刪除。
- 不因 Backend 30 秒 timeout 而重送 mutation，也不因 timeout 清除 durable pending state。

## 3. 端點範圍與 deadline

下列三個端點都必須使用同一種 30 秒總預算：

```http
GET  /admin/guilds
GET  /admin/guilds/{guildId}/settings
POST /admin/guilds/{guildId}/commands
```

要求：

1. deadline 在 controller action 入口建立，涵蓋 Discord authorization、Redis subscribe、Redis publish、Bot 執行、reply 解析與 response 產生。
2. 所有下游呼叫使用同一個 linked cancellation token；不得讓 authorization、Redis wait、Bot request 各自重新取得 30 秒。
3. 使用獨立的 timeout token 判斷 504，避免把 client disconnect 誤判為正常 timeout。
4. request 到期時，Backend 必須在 30 秒內回傳 504；若 Bot 已經保存 durable mutation intent，不能回滾或刪除該 intent。
5. Bot request envelope 傳遞 absolute deadline，讓 Bot 使用同一個剩餘時間限制 snapshot 與同步 mutation，而不是從收到 Redis 訊息時重新計算完整 30 秒。

## 4. Cross-project contract

保留 `contractVersion: 1`，以 additive 欄位擴充 request envelope：

```json
{
  "contractVersion": 1,
  "correlationId": "32-char-hex",
  "guildId": "123456789012345678",
  "actorUserId": "123456789012345678",
  "deadlineUnixMs": 1780000000000,
  "action": "settings.snapshot",
  "payload": {}
}
```

- `deadlineUnixMs` 使用 UTC Unix milliseconds，避免時區與序列化格式差異。
- Backend 與 Bot 的 envelope DTO、strict parser、JSON fixture 必須同步更新。
- deadline 已過期的 request 不得開始新的 provider 或 Discord mutation。
- Backend transport error 使用既有 allowlist reply shape，`state` 為 `timeout` 或 `unknown`，並包含 `code`、`correlationId`、`arguments`；timeout 不冒充 Bot 的 domain mutation result。

## 5. Backend 實作

### 5.1 Controller

- 在三個 action 入口建立 30 秒 deadline CTS。
- `GetGuilds` 維持既有 cached guild authorization，並把 Redis guild snapshot hash 讀取納入同一 deadline。
- `GetSettings` 保留目前不重新呼叫 Discord guild authorization 的產品決策，只驗證 bearer session 後發送 snapshot request。
- `SendCommand` 維持目前 uncached Discord authorization 與 guild membership check，然後使用同一 deadline 發送 command。
- 對 deadline cancellation 回傳 504；對 Redis null／無 subscriber／Redis exception 回傳 503。
- 504 body 必須包含 `settings.timeout` 與 request correlation ID，讓 Frontend 不把它當成 `unknown mutation`。

### 5.2 Redis bridge

- `PublishAndWaitAsync` 使用呼叫端傳入的剩餘 deadline，不再有獨立固定 reply timeout。
- Redis operation 需要區分三種結果：reply、unavailable、deadline exceeded。
- deadline exceeded 要讓 controller 能辨識並回 504；不可在 service 內把所有 cancellation 吞成 null。
- client disconnect 仍遵守 ASP.NET cancellation，不為已斷線的 client 虛構 response。

## 6. Notifier 實作

### 6.1 Request deadline

- `AdminSettingsService` 解析 envelope 的 `deadlineUnixMs`。
- 以 `GracefulShutdown.Token` 為基礎建立 request-scoped linked token，所有 snapshot／command domain calls 使用該 token。
- request deadline 到期時停止尚未完成的同步工作；不得影響既有週期 cleanup 或已保存的 durable state。
- 不為 deadline timeout 自動 publish mutation retry；Backend 會負責回 504。
- 保留目前 snapshot elapsed log，供後續辨識真正慢的 DB 或 provider fallback，而不是先做無證據的全面平行化。

### 6.2 驗證設定刪除

將目前同步刪除流程拆成兩個明確階段：

1. `Mark...DeletionPendingAsync`：在 guild lock 內載入設定與 checks，將設定標記 `DeletionPending`、將 checks 設為 `IsChecked=false` 與 `PendingRoleRemoval=true`，一次 `SaveChangesAsync` 後回傳可立即完成的 pending result。
2. `ProcessPending...DeletionAsync`：只處理已存在的 pending state；載入 ownership snapshot，逐一清除成員 role，清除成功的 check，最後依平台規則刪除可安全刪除的 system role 並刪除 config。

必要條件：

- YouTube 與 Twitch Web／Slash remove caller 都不能在 pending mark 後繼續等待所有 Discord role mutation。
- 既有週期 retry 必須改呼叫 process 方法，不能再次呼叫只會重新 mark 的入口。
- YouTube 與 Twitch 的 retry、ownership、guild lock、unknown member handling、role hierarchy handling 必須沿用既有 domain policy。
- snapshot 在 cleanup 完成前仍可看見設定的 `deletionPending` 與 pending count；這是 durable truth，不可為了讓 UI 消失而提前刪 row。
- 若 background cleanup 失敗，保留 pending state 供下一輪重試並記錄可搜尋的 guild、source、correlation 或 cleanup context。

## 7. Frontend 實作

- `AdminReplyState` 新增 `timeout`，HTTP 504 解析為 timeout，而非 `unknown`。
- `AdminSettingsApiError` 保留 response code，使 snapshot GET timeout 能顯示專用訊息。
- 504 時清除該 mutation loading，保留現有 snapshot，不自動重送 mutation。
- 驗證 remove 收到 `pending` 時，立即把現有項目顯示為 `deletionPending`，不要再追加完整 `GET /settings`；保留現有「清理中，暫時不能更新」提示。
- 已知可由 response arguments 更新的簡單 mutation，優先直接更新本地狀態；需要 canonical source name 或 detection state 的項目才保留後續 refresh。
- timeout、pending、rejected、unknown 仍維持不同 UI 語意；unknown 只允許使用者手動重新載入。

## 8. 可能修改的檔案

### Bot

- `src/DiscordStreamNotifyBot.Shared/Messages/AdminSettings.cs`
- `src/DiscordStreamNotifyBot.Notifier/SharedService/AdminSettings/AdminSettingsService.cs`
- `src/DiscordStreamNotifyBot.Notifier/SharedService/YoutubeMember/YoutubeMemberRoleService.cs`
- `src/DiscordStreamNotifyBot.Notifier/SharedService/YoutubeMember/YoutubeMemberService.cs`
- `src/DiscordStreamNotifyBot.Notifier/SharedService/TwitchSubscription/TwitchSubscriptionRoleService.cs`
- `src/DiscordStreamNotifyBot.Notifier/SharedService/TwitchSubscription/TwitchSubscriptionService.cs`
- 既有 Admin Settings、YouTube role、Twitch role focused tests

### Backend

- `DiscordStreamBotBackend/Controllers/AdminGuildsController.cs`
- `DiscordStreamBotBackend/Services/AdminSettingsRedisService.cs`
- `DiscordStreamBotBackend/Model/AdminSettingsModels.cs`
- Backend Admin Settings contract tests

### Frontend

- `src/lib/adminSettings.ts`
- `src/page/SettingsPage.vue`
- Frontend tests or existing lint/build verification files only when required by current project setup

## 9. 驗收條件

- 三個管理設定 endpoint 在正常完成、unavailable 或 timeout 時都能在 30 秒內回應。
- 30 秒 deadline 從 controller 入口開始計算，沒有串接出 30 秒加 30 秒的 hidden budget。
- Backend 能穩定區分 503 unavailable 與 504 timeout。
- timeout 不會自動重送 mutation，也不會清除 `DeletionPending` 或 `PendingRoleRemoval`。
- 驗證 remove 在 DB checkpoint 成功後可快速回 `pending`，不等待每位成員的 Discord role API。
- YouTube active entitlement 會在 checkpoint 後立即失效；Twitch active subscription verification 也會立即停止。
- 背景 retry 能從既有 pending rows 完成成員 role cleanup，成功後刪除 config/check rows。
- Twitch Tier roles 只在既有 protected-role／ownership 條件允許時刪除；YouTube 管理員選定 role 不被自動刪除。
- snapshot 不重新呼叫 Discord guild authorization；mutation 仍保留即時 authorization。
- Frontend timeout 不顯示為 unknown，不自動重送，pending 顯示為清理中。
- Bot Release build、Backend Release build、Frontend build／lint／format 與相關 tests 通過。

## 10. 實作順序

1. 先更新三端 contract DTO、deadline fixture 與 Backend 503/504 result model。
2. 實作 Backend 30 秒 endpoint deadline 與 Redis result distinction。
3. 實作 Bot envelope deadline propagation 與 request-scoped cancellation。
4. 拆分 YouTube／Twitch pending mark 與 background process cleanup，修正既有週期 caller。
5. 更新 Frontend timeout parsing、pending local state 與 mutation reload 行為。
6. 執行 focused tests，再執行三端 Release/build/lint/format 驗證。
7. 更新本文件狀態、`AGENTS.md` 目前狀態與必要測試文件；不更新 graphify，除非專案流程明確要求。

## 11. 不在本次實作

- 不恢復 snapshot 每次 Discord owner／Administrator API 驗證。
- 不新增 Redis Streams、queue、worker service 或資料表。
- 不新增任意未經需求指定的 timeout、retry、parallelism 或 cache TTL。
- 不把 YouTube 管理員選定 role 當成 Bot-owned role 刪除。
- 不修改既有通知、爬蟲、OAuth 或跨平台 ownership business rules。
