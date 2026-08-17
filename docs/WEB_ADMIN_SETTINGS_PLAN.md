# 網頁管理設定中心實作計畫

## 1. 目標

在既有 `stream-bot.konnokai.me` 增加 Discord 伺服器管理設定中心。伺服器擁有者或具 `Administrator` 權限的管理員可直接讀取及修改《直播小幫手》設定，不必逐一執行 Slash 指令。

首版涵蓋：

- 共用設定：伺服器語言、Bot 擁有者公告頻道、YouTube 會員與 Twitch 訂閱驗證共用紀錄頻道。
- YouTube 通知：來源、直播通知頻道、影片通知頻道、通知訊息、建立 Discord 活動。
- Twitch 通知：來源、通知頻道、開台／關台／資料變更訊息。
- TwitCasting 通知：來源、通知頻道、開台訊息。

首版不修改爬蟲、YouTube 會員驗證或 Twitch 訂閱驗證設定，但必須顯示來源是否已有偵測能力，不得把尚未啟用爬蟲的通知標成可正常運作。

## 2. 已確認產品決策

- 網頁直接修改設定，不是只產生 Slash 指令。
- 只有伺服器擁有者或具 `Administrator` 權限者可使用。
- Backend 不直接寫 Bot 的設定資料表。
- 設定由持有該 guild 的 Notifier shard 執行，並重用 Slash 指令的業務服務。
- 每次只提交一項明確目標狀態，不提供整份設定批次覆寫。
- 不使用 `toggle` 類命令；布林設定傳送明確的 `true`／`false`。
- 首版沿用既有 Vue SPA、ASP.NET Backend、MySQL、Redis 與 Notifier，不新增框架或服務。

## 3. 系統邊界

```text
Vue /settings
    |
ASP.NET Backend
    |-- Discord OAuth identify + guilds
    |-- owner / Administrator 即時驗證
    |
Redis admin settings request/reply
    |
持有 guild 的 Notifier shard
    |-- Discord 即時物件與 Bot 權限檢查
    |-- 共用及平台設定服務
    |-- MySQL
```

Frontend 只傳輸管理設定 DTO。Backend 負責使用者身分與 guild 授權。Notifier 是設定業務規則、Discord 物件及 MySQL mutation 的唯一入口。

## 4. 跨專案契約

所有 Discord snowflake 在 JSON 中使用十進位字串，避免 JavaScript number 精度遺失。

### 4.1 命令

```json
{
  "contractVersion": 1,
  "correlationId": "32-char-hex",
  "guildId": "123456789012345678",
  "actorUserId": "123456789012345678",
  "action": "youtube-notification.upsert",
  "payload": {}
}
```

首版 action：

- `guild.set-locale`
- `guild.set-global-notice-channel`
- `guild.set-verification-log-channel`
- `youtube-notification.upsert`
- `youtube-notification.remove`
- `twitch-notification.upsert`
- `twitch-notification.remove`
- `twitcasting-notification.upsert`
- `twitcasting-notification.remove`

### 4.2 回應

```json
{
  "contractVersion": 1,
  "correlationId": "32-char-hex",
  "shardId": 0,
  "state": "applied",
  "code": "settings.updated",
  "arguments": {}
}
```

狀態：

- `applied`：已完成。
- `pending`：設定意圖已保存，但 Discord 修復或清理仍待背景工作完成。
- `rejected`：權限、輸入或業務規則拒絕。
- `unknown`：僅由 Backend 在等不到 owning shard 回覆時產生；Frontend 必須重新取得快照，不可自動重送 mutation。

### 4.3 快照

首版快照固定頂層形狀：

```json
{
  "contractVersion": 1,
  "capabilities": [],
  "guild": {},
  "health": {},
  "resources": { "channels": [] },
  "common": {},
  "notifications": {
    "youtube": [],
    "twitch": [],
    "twitcasting": []
  }
}
```

`capabilities` 由 owning shard 回報，Frontend 只顯示已支援功能。未來只以 additive 欄位加入 `resources.roles` 與 `verification`，不更換 envelope、OAuth 或 Redis 路由。

## 5. 授權與安全

- Discord OAuth scope 由 `identify` 擴充為 `identify guilds`。
- Backend 保留 Discord access token 於既有加密 session payload，session 到期時間不得晚於 provider access token 到期時間；首版不保存 refresh token。
- 每次讀取或修改前，Backend 以 Discord `/users/@me/guilds` 最新回應驗證 owner 或 `Administrator`。
- Backend guild 清單與 Bot guild snapshot 交集只用於 UI；Notifier 執行前仍須確認 `_client.GetGuild(guildId)` 與 shard ownership。
- Redis 是既有受信任內部控制平面，必須保持不公開且受 ACL／網路邊界保護。
- Backend 的管理命令不得使用現有會自動重送的通知發布佇列；逾時後延遲重送可能造成使用者不知情的 mutation。
- API 及快照使用 allowlist DTO，不回傳 OAuth token 或資料表 entity。

## 6. Notifier 實作

1. 新增明確啟動的管理設定服務，訂閱獨立 request channel 並回覆 correlation-specific reply channel。
2. 非 owning shard 不回覆、不寫入；owning shard 對不支援版本或 action 回覆 `rejected`。
3. 將 `Utility` 的三個共用 mutation 移入既有 `UtilityService`。
4. 將 YouTube、Twitch、TwitCasting 通知增刪改移入各既有平台服務。
5. Slash Interaction 改呼叫相同方法，只保留 Discord context、確認操作及本地化顯示。
6. 平台服務完成 mutation 後清除自己的通知快取。
7. 命令記錄 `correlationId`、guild、actor、action、state 與 code，不記錄 OAuth token。

首版使用 action switch，不建立 registry、factory 或 plugin framework。

## 7. Backend API

```http
GET  /admin/guilds
GET  /admin/guilds/{guildId}/settings
POST /admin/guilds/{guildId}/commands
```

- `GET /admin/guilds`：取得 owner／Administrator guild，標示 Bot 是否已加入。
- `GET /admin/guilds/{guildId}/settings`：授權後向 owning shard 取得快照。
- `POST /admin/guilds/{guildId}/commands`：授權、產生 correlationId、直接 publish、等待單一 owning shard 回覆。

Backend 不新增通知或驗證設定表的 mutation model。

## 8. Frontend

- 在既有 SPA 增加 `/settings`，不先導入 router dependency。
- 流程為 Discord 登入、選擇 guild、健康檢查、共用設定、三平台通知。
- 頻道選項顯示 Bot 是否具 View Channel、Send Messages、Embed Links；YouTube 活動另顯示 Manage Events。
- mutation 完成後重新取得快照，不做 optimistic update。
- `pending`、`rejected`、`unknown` 必須有不同顯示；`unknown` 只提供重新整理，不自動重試。
- 首版維持繁體中文；API 使用穩定 code，保留未來本地化能力。

## 9. 會限驗證下一階段銜接

下一階段新增：

- `resources.roles`：role ID、名稱、位置、managed、Bot 是否可管理。
- `verification.youtube` 與 `verification.twitch` 快照。
- YouTube add/update/remove、手動探測影片、恢復自動探測 action。
- Twitch add/update/remove action。
- Frontend 驗證設定區塊與 pending cleanup 顯示。

執行時直接呼叫：

- `YoutubeMemberRoleService.ConfigureRoleAsync`／`DeleteConfigurationAsync`。
- `TwitchSubscriptionRoleService.CreateOrRepairConfigurationAsync`／`DeleteConfigurationAsync`。
- `MemberOperationCoordinator` 的 guild lock。
- `MemberRoleOwnershipService` 的跨平台 role ownership 保護。

不得在 Backend 或 Web command handler 重寫角色階層、managed role、Tier role、跨平台碰撞或 durable cleanup 規則。

## 10. 首版完成閘門

- 非 owner／Administrator 無法取得或修改設定。
- 未知版本及 action 安全回覆 `rejected`，不寫 DB。
- 非 owning shard 不執行；多 shard 環境只執行一次。
- 相同目標狀態重送後結果一致。
- Redis reply 遺失不觸發背景重送，Frontend 能以重新取得快照確認結果。
- 三平台通知 mutation 後立即失效對應快取。
- 驗證紀錄頻道經網頁路徑設定後，既有 YouTube／Twitch 驗證指令可直接使用。
- `pending` 回應可由 Backend 與 Frontend 完整傳遞及呈現。
- Bot 與 Backend 以固定 JSON fixture 驗證 envelope、response 與 snowflake 字串契約。
- Backend 未取得 Bot 設定資料表的寫入能力。
- 新增未來 action 只需新增 payload、capability、action switch case 與 UI，不改認證、envelope 或路由。

## 11. 驗證

- Bot：Release build、Release tests、契約測試、平台設定 focused tests、跨 shard owning guard 測試。
- Backend：Release build、Release tests、Discord guild authorization、provider token expiry、request/reply timeout 與契約測試。
- Frontend：build、ESLint、Stylelint、Prettier。
- 手動：owner、Administrator、無權限使用者、Bot 未加入、Bot 缺頻道權限、三平台增刪改、無爬蟲來源、Notifier 離線與多 shard。

## 12. 實作順序

1. 固定 JSON 契約及三專案測試 fixture。
2. 實作 Notifier 快照與管理命令 request/reply。
3. 抽出共用與三平台通知 mutation，Slash 改共用服務。
4. 擴充 Backend Discord OAuth 與 guild authorization。
5. 實作 Backend 管理 API bridge。
6. 實作 Frontend `/settings`。
7. 完成自動化與手動驗收，通過首版完成閘門。
