# 網頁管理設定中心：爬蟲與會員驗證實作計畫

> 狀態：程式碼與自動化檢查已完成；待正式 Discord／Redis／MySQL／多 shard 手動驗收
>
> 前置：`WEB_ADMIN_SETTINGS_PLAN.md` 的 v1 管理設定中心、Backend API 與 Frontend 兩層選單已在三個 repo 的未提交工作樹中完成。

## 1. 目標

補齊 `/settings` 目前只有預覽畫面的兩組功能：

- 爬蟲：管理 YouTube、Twitch、TwitCasting 的偵測來源。
- 會員驗證：管理 YouTube 會員驗證與 Twitch 訂閱驗證設定。
- 快照顯示限制、角色、探測影片、Tier 角色及 durable cleanup 狀態。
- Web 與 Slash 指令共用同一組 Bot domain service，不建立第二套規則。

本計畫只擴充既有 contract v1、`GET /admin/guilds/{guildId}/settings` 與 `POST /admin/guilds/{guildId}/commands`。不新增服務、資料庫或前端框架。

## 2. 完成範圍

### 2.1 爬蟲

每個平台提供：

- 顯示目前由該 guild 擁有的爬蟲來源。
- 顯示目前數量與該 guild 的設定上限。
- 新增來源。
- 移除來源，移除前須二次確認。
- 平台 API 停用、來源重複、來源屬於其他 guild、未達使用門檻、超過上限等明確錯誤。
- 新增或移除後重新取得快照，使直播通知的 `detectionEnabled` 同步更新。

爬蟲頁不重複編輯直播通知頻道或通知訊息；這些資料仍由「直播通知」主選單管理。

### 2.2 YouTube 會員驗證

- 顯示頻道、授予身分組、舊身分組遷移狀態、探測影片模式及 durable cleanup 狀態。
- 新增或更新頻道與授予身分組。
- 移除設定。
- 手動指定探測影片。
- 清除手動影片，恢復自動探索。
- 顯示已驗證人數與待移除身分組人數。

### 2.3 Twitch 訂閱驗證

- 顯示 broadcaster、共用訂閱者身分組、舊身分組遷移狀態及三個 Tier 身分組。
- 新增或更新 broadcaster 與共用身分組。
- 移除設定。
- 顯示已驗證人數與待移除身分組人數。
- Tier 身分組由既有 Bot service 建立、修復、重新命名及定位；Web 不直接指定 Tier role。

## 3. 不在本階段處理

- 不提供一般成員的 YouTube／Twitch 驗證操作；成員仍使用既有 Slash 指令。
- 不把已驗證成員明細放進設定快照。快照只回傳統計數量；明細繼續使用既有 `list-checked-member`。若日後要放上 Web，另做有明確分頁的 read-only query 契約。
- 不讓 guild 管理員切換 YouTube trusted、Twitch／TwitCasting warning 或錄影狀態；這些是 Bot 擁有者維運功能。
- 不顯示或傳輸 Google／Twitch OAuth token、密文、scope、provider expiry 或 provider API 原始回應。
- 不修改既有會員數門檻、每 guild 上限、官方 guild 例外、Twitch OAuth bypass 或全域來源 ownership 規則。
- 不新增資料表或 migration；目前 crawler 與 verification schema 已足夠。如實作中確認需要 schema，先停下來更新本文件並取得 owner 決策。

## 4. 必須維持的產品規則

### 4.1 共通安全邊界

- Backend 每次讀寫前仍以 Discord `/users/@me/guilds` 即時確認 owner 或 `Administrator`。
- 持有 guild 的 Notifier shard 是 Discord 物件檢查及 MySQL mutation 的唯一入口。
- 所有 snowflake 在 JSON 中使用十進位字串。
- `unknown` 不得自動重送 mutation；Frontend 只能提示並重新取得快照。
- 每次命令只提交一個目標狀態，不提供整份快照覆寫或 toggle action。
- Notifier 必須重新驗證 guild、來源 ownership、Bot 權限、角色與業務限制，不能信任 Frontend 的 disabled state。

### 4.2 爬蟲規則

- YouTube 沿用兩大箱 managed channel 防護、official guild 例外與 `MaxYouTubeSpiderCount`。
- Twitch 沿用 200 人門檻；未滿門檻時，只有 actor 已用同一 Discord 帳號連結欲新增的 Twitch broadcaster 才可 bypass。檢查使用 request envelope 的 `actorUserId`，不得改用 guild owner 或其他管理員的授權。
- Twitch OAuth bypass 必須沿用目前 active authorization 判斷、client ID 約束與 reconcile publication。
- TwitCasting 沿用 500 人門檻、official guild 例外與 `MaxTwitcastingSpiderCount`。
- 三平台來源目前都是全域唯一，guild 只能移除自己擁有的 row。`GuildId == 0` 的 Bot owner row 不得由 Web 修改。
- 來源屬於仍存在的其他 guild 時拒絕接管；原 guild 已不存在時，沿用目前 Slash 的 orphan ownership 行為。
- 同來源跨 guild 併發新增不能靠記憶體 guild lock 保證；以資料庫唯一鍵為最後防線，constraint conflict 後 reload 並回傳確定結果。
- 移除爬蟲不得刪除直播通知設定；通知保留但 `detectionEnabled=false`。

目前 TwitCasting handler fallback 是 2，但 `GuildConfig.MaxTwitcastingSpiderCount` 預設值是 3。抽 service 前先用 characterization test 鎖住實際行為，不得順手統一數字。若測試證明會改變正式行為，停下來請 owner 決定。

### 4.3 驗證規則

- YouTube 沿用 250 人門檻、official guild 例外與 `MaxYouTubeMemberCheckCount`。
- 新增 YouTube／Twitch 驗證前必須已有有效 `VerificationLogChannelId`，且頻道仍存在。
- 角色必須不是 `@everyone`、不是 managed role，且位置低於 Bot 最高角色；Bot 必須有 `ManageRoles`。
- YouTube 與 Twitch 不得新增跨平台 role collision。
- 所有角色 mutation 與刪除都經 `MemberRoleOwnershipService` 保護其他平台 entitlement。
- YouTube role 更新沿用 `PreviousMemberCheckGrantRoleId` checkpoint；Twitch 沿用 `PreviousSubscriberRoleId` 與三個 Tier role repair。
- `DeletionPending`、`PendingRoleRemoval` 是 durable work，不得因 Web request 逾時或 Discord 暫時失敗而清除。
- operational failure 回 `pending` 或 `rejected` 時，不得假裝設定已完全刪除。
- YouTube quota、429、5xx、timeout 或 probe temporary failure 不得移除既有 entitlement。
- Twitch provider token 及訂閱資格查詢仍只存在 Bot 內，不進入管理設定 Redis envelope。

## 5. Contract v1 additive 擴充

保留 `contractVersion: 1`。只新增 action、capability 與快照欄位；不更換 OAuth、API route 或 Redis channel。

### 5.1 Capabilities

Notifier 依服務啟用狀態回報：

- `youtube-crawler`
- `twitch-crawler`
- `twitcasting-crawler`
- `youtube-verification`
- `twitch-verification`

Frontend 只顯示快照宣告的 capability。平台停用時仍可考慮保留 remove／cleanup 能力，但新增按鈕必須停用；具體由快照平台狀態表達，不能只靠 capability 隱藏既有設定。

### 5.2 新增 actions

```text
youtube-crawler.add
youtube-crawler.remove
twitch-crawler.add
twitch-crawler.remove
twitcasting-crawler.add
twitcasting-crawler.remove

youtube-verification.upsert
youtube-verification.remove
youtube-verification.set-probe-video
youtube-verification.use-automatic-probe
twitch-verification.upsert
twitch-verification.remove
```

Payload：

```json
{ "source": "平台網址、login 或 ID" }
{ "sourceId": "已解析並由快照回傳的 canonical ID" }
{ "source": "平台網址或 ID", "roleId": "123456789012345678" }
{ "sourceId": "canonical ID", "video": "YouTube 影片網址或 ID" }
```

規則：

- add／upsert 接受既有 Slash 已支援的輸入形式，由 Bot 解析 canonical ID。
- remove 與 probe mutation 使用快照回傳的 canonical `sourceId`，避免以顯示名稱刪錯資料。
- `roleId`、guild、actor 及 channel snowflake 都保持 JSON string。
- Payload 使用 allowlist DTO 及嚴格型別驗證；未知欄位可以忽略，但缺欄、空字串、numeric snowflake 或錯誤型別必須 `rejected`。

### 5.3 快照頂層

`AdminSettingsSnapshot` 新增：

```json
{
  "resources": {
    "channels": [],
    "roles": []
  },
  "crawlers": {
    "youtube": { "enabled": true, "count": 0, "limit": 3, "items": [] },
    "twitch": { "enabled": true, "count": 0, "limit": 3, "items": [] },
    "twitcasting": { "enabled": true, "count": 0, "limit": 2, "items": [] }
  },
  "verification": {
    "youtube": [],
    "twitch": []
  }
}
```

範例中的 limit 只示意欄位形狀，實際值必須來自現有 domain rule／`GuildConfig`，不得把範例數字寫成新的政策。

`resources.roles[]`：

```json
{
  "id": "123456789012345678",
  "name": "會員",
  "position": 10,
  "managed": false,
  "everyone": false,
  "botCanManage": true
}
```

Crawler item：

```json
{
  "sourceId": "canonical-platform-id",
  "sourceName": "display name"
}
```

YouTube verification item：

```json
{
  "sourceId": "UC...",
  "sourceName": "channel title",
  "roleId": "123456789012345678",
  "previousRoleId": null,
  "deletionPending": false,
  "probeMode": "automatic",
  "probeVideoId": "-",
  "verifiedMemberCount": 0,
  "pendingRoleRemovalCount": 0
}
```

Twitch verification item：

```json
{
  "sourceId": "broadcaster-id",
  "sourceLogin": "login",
  "sourceName": "display name",
  "subscriberRoleId": "123456789012345678",
  "previousSubscriberRoleId": null,
  "tierRoleIds": {
    "1000": "123456789012345679",
    "2000": "123456789012345680",
    "3000": "123456789012345681"
  },
  "deletionPending": false,
  "verifiedMemberCount": 0,
  "pendingRoleRemovalCount": 0
}
```

所有 nullable role ID 仍以 string 或 null 傳輸。統計以資料庫 aggregate query 取得，不載入完整成員列到記憶體。

### 5.4 回應碼

沿用 `applied | pending | rejected | unknown`。新增穩定 code，Frontend 以 code 顯示繁體中文，不直接顯示 provider exception：

```text
crawler.added
crawler.removed
crawler.already-exists
crawler.not-configured
crawler.not-owned
crawler.source-owned
crawler.source-ineligible
crawler.limit-reached
crawler.guild-member-requirement
crawler.oauth-eligibility-required
crawler.platform-disabled

verification.configured
verification.removed
verification.cleanup-pending
verification.not-configured
verification.limit-reached
verification.guild-member-requirement
verification.log-channel-required
verification.log-channel-missing
verification.manage-roles-required
verification.role-invalid
verification.role-too-high
verification.role-collision
verification.deletion-pending
verification.source-not-found
verification.source-ineligible
verification.probe-video-set
verification.probe-automatic
verification.probe-video-invalid
verification.platform-disabled
```

需要顯示上限、目前人數或來源名稱時放進 `arguments`，不得把 token、provider body 或完整 exception 放進去。

## 6. Bot 實作

### 6.1 先抽共用 crawler service 流程

目前三個 Slash handler 直接混合 URL 解析、平台 API、限制、全域 ownership、DB 與回覆。先把 add／remove 搬到現有平台 service：

- `YoutubeStreamService.AddCrawlerAsync`／`RemoveCrawlerAsync`
- `TwitchService.AddCrawlerAsync`／`RemoveCrawlerAsync`
- `TwitcastingService.AddCrawlerAsync`／`RemoveCrawlerAsync`

參數至少包含 guild、actor user ID、原始 source 與 cancellation token。回傳 domain result（state、code、arguments、canonical source），不得直接發 Discord 訊息。

Slash handlers 改成：defer／確認 -> 呼叫 service -> 將 code 映射為既有 localized response。`AdminSettingsService` 呼叫同一 service 並直接回 contract result。

抽取時必須保留：

- 平台 enable 檢查。
- member count／official guild／Bot owner／Twitch actor OAuth bypass。
- guild-specific max count。
- 全域 source uniqueness、orphan ownership 與 Bot owner row。
- YouTube managed channel 防護。
- Twitch reconcile publication。
- 現有營運 log；不得記錄 OAuth 資料。

不要新增 crawler registry、factory 或介面；只有三個既有平台，action switch 已足夠。

### 6.2 補 verification 管理入口

YouTube：在 `YoutubeMemberService` 新增管理設定方法，協調現有 `YoutubeMemberRoleService`、YouTube source／video 解析、限制及 `MemberOperationCoordinator` guild lock。

Twitch：在 `TwitchSubscriptionService` 新增管理設定方法，協調現有 `TwitchApiService` 與 `TwitchSubscriptionRoleService`。

Slash setting handlers 改呼叫這些方法，保留 Discord confirm 與本地化外殼。Web handler 不得複製以下邏輯：

- role hierarchy／managed／everyone 檢查。
- `VerificationLogChannelId` 檢查。
- 跨平台 ownership。
- previous-role migration。
- Tier role 建立與修復。
- `DeletionPending`／`PendingRoleRemoval` transition。
- probe video 驗證與 automatic pin policy。

服務自己取得 `MemberOperationCoordinator.LockGuildAsync`；caller 不重複持有同一把 lock。

### 6.3 擴充 AdminSettings contract 與快照

修改：

- `Shared/Messages/AdminSettings.cs`
- `SharedService/AdminSettings/AdminSettingsService.cs`
- 必要的 platform／verification service
- `tests/DiscordStreamNotifyBot.Tests/AdminSettingsContractTests.cs`

`BuildSnapshotAsync` 應：

- 用 allowlist DTO 回傳 guild-owned crawler rows。
- 回傳全部 Discord roles 的管理狀態，讓 UI 能解釋不可選原因；Notifier mutation 仍再次檢查。
- aggregate verification counts。
- 回傳 pending cleanup 與 previous role checkpoint。
- 對 provider API 名稱查詢採 best effort；API 停用或失敗時仍用 DB canonical ID，不能讓整份設定快照失敗。

### 6.4 併發與 cancellation

- 所有新 async API 接受 request cancellation／`GracefulShutdown.Token`。
- guild verification mutation 使用既有 guild lock。
- crawler source 全域唯一不能只用 guild lock；保存時處理 DB constraint race。
- Provider call 前後若資料可能改變，reload authoritative row，再決定是否套用結果。
- 不加入 retry loop 或新的 timeout；沿用現有 provider client 與 Backend 2.5 秒 request/reply budget。

## 7. Backend 實作

Backend 維持 thin transport：

- `AdminSettingsModels.cs` 的 snapshot reply 加上 `crawlers` 與 `verification`，`resources` 可繼續用 `JObject`，不得建立 Bot domain model。
- `AdminGuildsController` route、即時 guild authorization、correlation ID 與 timeout 行為不變。
- Command action allowlist 由 Notifier 決定；Backend 只驗證 envelope 基本形狀，不複製平台 payload 規則。
- `unknown` 回應不重送。
- 更新 Backend contract tests，確認新增頂層欄位不會在 deserialize／serialize 時消失。

不新增 crawler 或 verification 專用 HTTP controller。

## 8. Frontend 實作

修改：

- `src/lib/adminSettings.ts`
- `src/page/SettingsPage.vue`

### 8.1 爬蟲頁

- 以平台顯示目前 count／limit、來源清單與新增表單。
- Add 欄位標示可接受 URL、login 或 ID；送出後由 Bot canonicalize。
- Remove 使用快照 `sourceId`，採 inline 二次確認，不用 fake modal。
- API 停用或資格不足時保留既有設定與移除能力，只停用新增並顯示原因。
- 每次 `applied` 或 `pending` 後重新取得快照；`unknown` 只顯示重新載入，不重送。

### 8.2 驗證頁

- role select 顯示 role 名稱；`managed`、`everyone` 或 `botCanManage=false` 的 role 顯示原因並禁止選取。
- YouTube 卡片顯示 automatic／manual probe、video ID、previous role、verified count、pending count 及 deletion pending。
- Twitch 卡片顯示共用 role、Tier 1／2／3 roles、previous role、verified count、pending count 及 deletion pending。
- `DeletionPending=true` 時設定不可更新，只顯示「清理中」與重新載入。
- 移除設定採 inline 二次確認，並明確說明角色清理可能進入背景工作。
- 不提供「立即驗證所有成員」或「清空已驗證名單」按鈕。
- 移除目前的 `feature-preview` placeholder；沿用既有表單、按鈕、toast 與兩層選單視覺語言。

### 8.3 前端狀態

- 每個 mutation 使用既有 per-form pending key，防止同一項重複送出。
- guild 切換後，舊 request／reply 不得污染新 guild。
- `pending` 要保留設定卡並標示背景處理中。
- `rejected` 依 code 顯示可採取的修復方式。
- 桌機及手機都允許主／子選單水平捲動，頁面本身不得水平溢位。

## 9. 測試計畫

### 9.1 Characterization tests

抽 service 前先鎖住目前 Slash 行為：

- 三平台 member count／official guild／Bot owner 例外。
- Twitch actor OAuth bypass 只能用本人且同一 broadcaster。
- 各平台 max count，包括 TwitCasting fallback 與 `GuildConfig` 值。
- source 已存在、其他 guild ownership、orphan ownership、Bot owner row。
- YouTube managed channel rejection。
- remove 僅允許 owning guild。
- Twitch add／remove reconcile reason。

### 9.2 Bot unit／contract tests

- 每個新 action 都被 `IsSupportedAction` 接受，未知 action 仍 rejected。
- 每種 payload 的有效、缺欄、錯型別及 numeric snowflake。
- Expanded snapshot 維持 camelCase、string snowflake、nullable role ID。
- Role resource 的 `botCanManage` 判斷。
- Crawler constraint race 的確定結果。
- Verification log channel、role validation、cross-platform collision。
- YouTube manual／automatic probe transition。
- Twitch Tier role repair 與 deletion pending。
- `pending` reply 不遺失 code／arguments。

### 9.3 Component tests

沿用既有 xUnit／MySQL component environment，至少驗證：

- 三平台 crawler add／remove 的 DB row 與 ownership。
- remove crawler 不刪通知設定。
- YouTube／Twitch upsert、role migration checkpoint、deletion pending 與 pending role removal aggregate。
- 快照不包含 OAuth token 欄位或 entity 原始序列化。

### 9.4 Backend tests

- owner／Administrator／forbidden 行為不退化。
- expanded snapshot passthrough。
- command envelope 的 actor、guild、snowflake string。
- timeout 回 unknown 且 publish 只發生一次。

### 9.5 Frontend 驗證

- `pnpm build`
- `pnpm lint:script`
- `pnpm lint:style`
- `pnpm exec prettier . --check`
- Chrome 桌機與手機：所有主選單／平台子選單、空狀態、有資料、pending、rejected、unknown、長 guild／role／source 名稱。

## 10. 手動驗收矩陣

在正式 Discord、Redis、MySQL 及多 shard 環境驗收：

### 10.1 授權

- Guild owner 可讀寫。
- Administrator 可讀寫。
- 無權限成員得到 403，且 Redis 未 publish mutation。
- Bot 不在 guild、Notifier 離線、非 owning shard 與 reply timeout 行為正確。

### 10.2 爬蟲

- YouTube／Twitch／TwitCasting 各自新增、重複新增、移除及超過上限。
- 未達 member count 時拒絕；official guild 例外維持。
- Twitch 未滿 200 人：actor 未連結拒絕、連結其他 broadcaster 拒絕、連結同 broadcaster 成功。
- 來源屬於其他 active guild 時拒絕；orphan row 依既有規則處理。
- 移除後通知設定保留，`detectionEnabled=false`；重新新增後恢復 true。
- Twitch add／remove 觸發 reconcile。

### 10.3 YouTube 會員驗證

- 未設定／已刪除驗證紀錄頻道。
- 無 Manage Roles、everyone、managed、role 太高及 Twitch role collision。
- 新增、同 role repair、換 role migration、previous role cleanup。
- 手動影片合法、找不到、留言關閉、恢復 automatic。
- 移除成功及 Discord 暫時失敗進入 deletion pending。
- 背景週期完成 pending cleanup 後快照更新。

### 10.4 Twitch 訂閱驗證

- 平台停用、找不到 broadcaster、非 Affiliate／Partner。
- 新增、同 role repair、換 role migration、Tier role 遺失重建及重新命名。
- YouTube role collision。
- 移除成功及 Discord 暫時失敗進入 deletion pending。
- 背景週期完成共用／Tier role cleanup 後快照更新。

## 11. 實作順序

1. 跑三個 repo baseline build／test／lint，記錄既有失敗，不先修無關問題。
2. 新增 crawler characterization tests。
3. 抽三平台 crawler add／remove 到既有 platform service，讓 Slash 改用共用方法。
4. 為 YouTube／Twitch verification 補 domain management methods，讓 Slash setting handlers 改用共用方法。
5. 擴充 Bot contract DTO、capabilities、actions、payload validation 與 snapshot。
6. 擴充 Bot contract／policy／component tests。
7. 擴充 Backend snapshot passthrough 與 tests。
8. 擴充 Frontend TypeScript contract，實作 crawler UI。
9. 實作 YouTube／Twitch verification UI，移除 placeholder。
10. 跑三 repo 自動化驗證與 `git diff --check`。
11. 在 `localhost:3333/settings` 用真實登入資料驗證桌機與手機。
12. 在維護窗口執行正式 Discord／Redis／MySQL／多 shard 手動驗收。

每一階段都應保持 build green。Crawler 與 verification 可分兩批完成，但不得讓 Web 與 Slash 暫時走不同的業務規則後就宣告完成。

## 12. 完成閘門

- 三平台 crawler 可從 Web 安全新增及移除，所有現有門檻、上限、ownership 與 reconcile 規則不退化。
- YouTube 會員驗證可管理 config、role 與 probe mode。
- Twitch 訂閱驗證可管理 config、共用 role 與 Tier role lifecycle。
- Snapshot 正確顯示 roles、limits、counts、previous role、deletion pending 及 pending role removals。
- Slash 與 Web 呼叫相同 domain service。
- Backend 沒有 Bot domain mutation 或 provider token 邏輯。
- Frontend 對 `applied／pending／rejected／unknown` 都有正確行為，unknown 不重送。
- Contract、Bot、Backend、Frontend 自動化檢查通過。
- 正式環境手動矩陣完成；若未執行，交接報告必須明列為 blocker，不得稱為 fully complete。

## 13. 新 Session 交接指令

將以下內容交給新的開發 session：

```text
請依 `DiscordStreamNotifyBot/docs/WEB_ADMIN_CRAWLER_VERIFICATION_PLAN.md` 完整實作網頁管理設定中心的爬蟲與會員驗證功能。

工作目錄包含三個 sibling repo：
- Bot：DiscordStreamNotifyBot
- Backend：DiscordStreamBotBackend
- Frontend：auto-discord-ytmember-checker

先讀各 repo 的 AGENTS.md、`WEB_ADMIN_SETTINGS_PLAN.md`、本計畫，以及目前未提交 diff。現有管理設定中心與 Frontend 兩層選單都在 dirty worktree，禁止重置、覆蓋或重做既有變更。

依本計畫第 11 節順序執行。先以 characterization tests 鎖住 crawler 現況，再抽共用 domain service；Slash 與 Web 必須共用同一規則。不要在 Backend 或 AdminSettingsService 複製 role、OAuth、crawler ownership、limit 或 durable cleanup 邏輯。不要新增框架、registry、資料表或 migration，除非原始碼證明必要；遇到 schema 或產品規則缺口先停下來說明。

完成每個 repo 的 build／test／lint／format／diff check，並在 localhost:3333 以桌機與手機驗證。正式 Discord／Redis／MySQL／多 shard 手動驗收若環境不可用，明列未驗證項目，不得聲稱完成。不要 commit、push 或修改無關 dirty files。
```
