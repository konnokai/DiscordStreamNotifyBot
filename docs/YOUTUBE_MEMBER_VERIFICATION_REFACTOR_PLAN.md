# YouTube 會員驗證架構重構計畫

> 狀態：實作與隔離部署 rehearsal 已完成，待真實 Discord／Google manual acceptance
>
> 建立日期：2026-08-04
>
> Bot 基準 commit：`d060f08344e7383406976a53cafcf3ac8ca07812`

## 1. 範圍

本計畫會同步修改三個 repo：

| 元件 | 路徑 | 責任 |
|---|---|---|
| Bot | `DiscordStreamNotifyBot/` | 會員驗證、Discord role、Slash command、DB migration |
| Backend | `../DiscordStreamBotBackend/` | Google OAuth、共享 token、account-links API、unlink intent |
| Frontend | `../auto-discord-ytmember-checker/` | 帳號連結與待清理狀態顯示 |

目標是讓 YouTube member check 採用目前 Twitch member check 已驗證過的生命週期、操作協調、持久化清理與角色修復模式，同時保留 YouTube 特有的留言探測及 Scraper 影片探索流程。

## 2. 已定案決策

- Slash group `member` 改為 `youtube-member`。
- Slash group `member-set` 改為 `youtube-member-set`。
- Leaf command 名稱與既有一般使用者行為原則上維持不變。
- YouTube 與 Twitch 不允許新增跨平台 role 重疊。
- 既有跨平台 role 重疊不自動改 role；清理時必須保守保留其他平台仍需要的 entitlement。
- `GuildConfig.LogMemberStatusChannelId` 繼續由兩平台共用。
- DB migration 只由 Bot repo 產生與管理。
- 前端、後端與 Bot 可在同一維護窗口全部停止後更新。
- `member.revokeToken` 名稱及 payload 不變；Redis 僅作即時喚醒，MySQL 才是 durable truth。
- `youtube_member_access_token` 表名、密文格式及 `ProviderTokenEncryptionKey` 契約不變。
- 不把 YouTube 抽象成通用 provider framework，也不複製 Twitch Tier、gift、Helix 或 EventSub 邏輯。

## 3. 非目標

- 本階段不分拆 YouTube/Twitch 狀態 log channel。
- 本階段不重新設計完整 Google refresh-token rotation 或 durable provider revoke state machine。
- 本階段不修改 YouTube access-token 加密格式或 Redis 外部頻道。
- 本階段不改現有 YouTube channel/video ID 欄位型別、不新增跨表 foreign key。
- 本階段不自動重新指派、刪除或接管既有 Discord role object。
- 本階段不新增前端測試框架或大型 UI redesign。

## 4. 必須保留的現行行為

- YouTube 會員資格仍以使用者 Google credential 讀取設定的會員限定影片留言判斷。
- 會員限定影片探索仍在 Scraper，Notifier 不重複探索影片。
- 初次 YouTube 驗證仍先排入 check，背景週期處理；不改成 Twitch 的同步 Helix 查詢。
- `EnableGuildMembersIntent` 關閉時，不註冊 rejoin restore，也不下載完整 guild member 清單。
- 一般互動仍支援 `zh-TW`、`en-US`、`ja`。
- 管理員與營運 log 維持繁體中文。
- YouTube managed-role 防護及 Twitch select-menu 隔離測試不得退化。
- Scraper、Notifier、Coordinator 的既有 shard ownership 與 graceful shutdown 契約不得改壞。

## 5. 目前主要問題

- YouTube 使用 raw `System.Threading.Timer` 搭配 async callback，可能重入且無法在關閉時 drain。
- `YoutubeMemberService` 同時負責 OAuth、select menu、排程、角色、rejoin、orphan reconciliation 與 Redis 事件。
- 取消、unlink、非會員與設定刪除會在 Discord role 清理失敗時遺失 durable retry evidence。
- Role 更新沒有 previous-role checkpoint，已驗證成員不會可靠遷移。
- Config/check 沒有 natural-key unique constraint，並行互動可能建立重複資料。
- YouTube/Twitch 都只看自己平台的 role entitlement，跨平台 role 碰撞可能互相移除 role。
- YouTube raw select-menu handler 缺少完整 guild/config/value 驗證。
- 暫時性 Google/API/log channel 錯誤可能被當成 destructive failure。
- `CheckMemberShip` 的 credential error log 可能序列化 access/refresh token，必須移除。
- Backend unlink 成功後依賴非 durable Redis pub/sub 才通知 Bot 清理角色。
- Frontend 丟棄 unlink response body，無法區分「帳號已解除」與「Discord role 尚在背景清理」。

## 6. 目標架構

### 6.1 共用元件

新增或調整兩個真正跨平台的服務：

1. `MemberOperationCoordinator`
   - 由現有 `TwitchSubscriptionOperationCoordinator` 提升為共用 singleton。
   - User lock 與 guild lock 分開管理。
   - 同時需要兩種 lock 時，固定先 user、再 guild。
   - 跨 guild cleanup 在 user lock 下依 guild ID 排序，逐一取得並釋放 guild lock。
   - Config add/update/delete 與 orphan reconciliation 只取 guild lock。

2. `MemberRoleOwnershipService`
   - 查詢同 guild 的 YouTube current/previous role。
   - 查詢同 guild 的 Twitch subscriber/previous/Tier role。
   - 判斷某 user 是否仍由任一平台的 active verified check 擁有該 role entitlement。
   - 判斷某 Discord role object 是否仍被任一平台 config reference。
   - 所有角色移除與 Twitch Tier role object 刪除都必須經過這個服務。

不要建立 `IMemberProvider` 等泛化框架。YouTube/Twitch 的 provider semantics 必須維持分離。

### 6.2 YouTube 模組

保留 `SharedService/YoutubeMember/`，拆成以下責任：

- `YoutubeMemberService`
  - 生命週期、週期任務、shard filtering、驗證 orchestration。
  - 明確 `Start()` 與 `StopAsync()`。
  - 追蹤並 await 所有背景工作。

- `YoutubeMemberApiClient`
  - 封裝 YouTube membership probe。
  - 將 Google SDK/HTTP response 轉為 typed result。
  - 不直接改 DB 或 Discord role。

- `YoutubeMemberAuthorizationService`
  - 封裝現有 Google flow、credential load/refresh、linked account lookup 與 revoke。
  - 保留既有 MySQL token 表與加密契約。
  - 只有 conclusive provider invalidation 才可觸發全域 cleanup。

- `YoutubeMemberRoleService`
  - Grant/remove/repair role。
  - Config role migration、config deletion、pending cleanup。
  - Rejoin restore 與 orphan reconciliation 可留在 orchestration service，但所有 Discord role mutation 必須委派到 role service。

- `YoutubeMemberPolicies`
  - 純函式決定 provider result、DB transition、role action 與 retry action。
  - 供 focused unit tests 使用。

- `YoutubeMemberComponent`
  - 由 Discord InteractionService 處理 select menu。
  - 不再由 `YoutubeMemberService` 訂閱所有 `SelectMenuExecuted`。

## 7. 狀態機

### 7.1 Check state

| 狀態 | `IsChecked` | `PendingRoleRemoval` | 說明 |
|---|---:|---:|---|
| Queued | false | false | 尚未完成初次驗證，或新 role grant 尚待重試 |
| Verified | true | false | Provider 已確認且 Discord role 同步成功 |
| Removal pending | false | true | 已決定取消 entitlement，Discord role 尚待清理 |
| Terminal | row deleted | row deleted | Role 已移除或確認不存在 |

Transition 規則：

- 建立 queued row 後才能呼叫 provider。
- Role grant 成功後才能將 row 設成 verified。
- Cancel、confirmed non-member、unlink、authorization invalidation 先保存 removal pending，再呼叫 Discord。
- Discord role 移除成功，或 guild/user/role 已確定不存在，才能刪 check row。
- Temporary failure、quota、timeout、local config/decrypt fault 必須保留最後成功 entitlement。
- Pending row 不得被 rejoin restore、orphan valid-entitlement snapshot 或正常 reverification 當成 active。
- Provider call 前記住 config/check row ID、role ID 與狀態；provider call 後重新取得 lock 並 reload，只有同一筆 row 與狀態仍有效時才能套用結果。

### 7.2 Config state

| 狀態 | `DeletionPending` | `PreviousMemberCheckGrantRoleId` |
|---|---:|---|
| Active | false | null |
| Role migration pending | false | previous role ID |
| Deletion pending | true | null 或 previous role ID |
| Terminal | row deleted | row deleted |

Role update 順序：

1. 在 guild lock 內驗證權限、managed role、role hierarchy 與跨平台 ownership。
2. 若 previous role 已存在，只允許以目前 target role 重試修復，不允許第三次變更。
3. 先保存 current role 到 previous，並保存新 current role。
4. 對 verified members 補新 role。
5. 只有其他 entitlement 不需要時才移除 previous role。
6. 所有成員成功後才清除 previous checkpoint。

Config deletion 順序：

1. 保存 `DeletionPending=true`。
2. 將相關 check 全部標成 removal pending。
3. 清理 current 與 previous role entitlement。
4. 所有 check 完成後才刪 config。
5. 即使已沒有 check，週期工作仍必須重試 deletion-pending config。

## 8. DB Schema

### 8.1 Entity changes

`GuildYoutubeMemberConfig`：

```csharp
public ulong? PreviousMemberCheckGrantRoleId { get; set; }
public bool DeletionPending { get; set; }
```

`YoutubeMemberCheck`：

```csharp
public bool PendingRoleRemoval { get; set; }
```

### 8.2 Indexes

- Unique：`GuildYoutubeMemberConfig(GuildId, MemberCheckChannelId)`。
- Unique：`YoutubeMemberCheck(GuildId, UserId, CheckYTChannelId)`。
- Lookup：`GuildYoutubeMemberConfig(DeletionPending, GuildId)`。
- Lookup：`YoutubeMemberCheck(PendingRoleRemoval, GuildId)`。
- Backend unlink lookup：`YoutubeMemberCheck(UserId, PendingRoleRemoval)`。

本階段不新增 `(GuildId, RoleId)` unique index，避免未經確認就改變同平台共用 role 的既有行為。跨平台 role separation 由 guild lock 下的 application policy 保證。

### 8.3 Migration 規則

- Existing rows backfill：所有 bool 為 false，previous role 為 null。
- 不自動 deduplicate。
- 不自動修正跨平台 role collision。
- 不新增 composite FK。
- Migration 前先執行 read-only preflight；若有重複或孤兒資料，停止 deployment 並人工處理。
- 產生一份目標 migration 的增量冪等 SQL，並更新 `migrate_sql/all.sql`。
- 重新產生 SQL 後需逐檔比對 checked-in output。

### 8.4 Preflight 查詢

至少要報告：

- Duplicate config：`(guild_id, member_check_channel_id)`。
- Duplicate check：`(guild_id, user_id, check_yt_channel_id)`。
- Orphan check：找不到同 guild/channel config。
- Invalid role ID：current/previous role 為 0 或不合理狀態。
- Cross-provider collision：YouTube current/previous 與 Twitch subscriber/previous/Tier role ID 重疊。

Preflight 結果只能報告，不可在 migration 內猜測性刪除資料。

## 9. Role 隔離政策

### 9.1 新設定

- YouTube role 不得等於同 guild 任一 Twitch subscriber、previous 或 Tier role。
- Twitch subscriber role 不得等於同 guild 任一 YouTube current 或 previous role。
- Twitch 新建 Tier role 由 Discord 產生新 ID；刪除前仍要檢查 YouTube reference。
- YouTube managed role、`@everyone`、超過 Bot role hierarchy 的角色仍拒絕。

### 9.2 既有碰撞

- 不自動重新命名、重新指派或刪除 role。
- 移除 role entitlement 前，查詢 YouTube 與 Twitch verified check union。
- 任一平台仍需要該 role 時，保留 role 並只完成本平台 DB transition。
- Twitch Tier role object 只有在兩平台 config reference union 為空時才能刪除。
- Orphan reconciliation 必須先載入同 guild 的跨平台 entitlement snapshot，避免兩個 job 互相移除。
- 對既有 collision 寫營運 log，要求管理員後續改成不同 role。

## 10. Slash 與 Interaction Cutover

### 10.1 Command rename

- `/member` -> `/youtube-member`
- `/member-set` -> `/youtube-member-set`
- Leaf command 不改名。

必須更新：

- `Interaction/YoutubeMember/YoutubeMember.cs`
- `Interaction/YoutubeMember/YoutubeMemberSetting.cs`
- `InteractionCommands.resx`
- `InteractionCommands.en-US.resx`
- `InteractionCommands.ja.resx`
- `Help.CommandDetail.*` keys
- `CommandDisplayResolver.GetCommandPath(...)` 呼叫
- 背景訊息內的 command path
- XML 文件註解
- Interaction command snapshot 與 explicit contract tests
- Frontend 新增的 YouTube 使用說明

不要改：

- Runtime `Member.*` localization keys
- `member.revokeToken`
- DB table/entity 名稱
- Twitch command/group 名稱

### 10.2 Component ID

新 ID：

```text
youtube-member-check:{guildId}:{userId}
```

Handler 必須驗證：

- Component 實際 guild 與 embedded guild 相同。
- Component user 與 embedded user 相同。
- Selected channel 數量 1 到 25。
- 每個 selected channel 都是該 guild 當下 active、non-deletion-pending config。
- 不接受 Twitch custom ID。

舊 `member:check:{guildId}:{userId}` 屬於已存在的 Discord message。舊 handler 只回覆 localized expired message，要求重新執行 `/youtube-member check`，不得再變更 DB 或 role。

Multi-select 改用 diff：

- 新選擇新增 queued check。
- 保留的選擇不重建 row。
- 被取消的選擇標成 removal pending。
- 不再先刪除所有 check/role。

## 11. 排程與生命週期

- 以 `PeriodicRunner` 取代所有 YouTube `System.Threading.Timer`。
- New-check cycle 保留約 15 秒 initial delay 與 5 分鐘 interval。
- Old-check cycle 保留每日一次語意，正確計算下一個 04:00，而不是固定延後到隔天。
- Pending cleanup/deletion 必須每個週期優先處理，即使 YouTube API disabled。
- Orphan reconciliation 保留 initial delay 與每日 interval，僅在 `EnableGuildMembersIntent=true` 啟動。
- `Start()` 註冊 Redis 與 `UserJoined`；`StopAsync()` 取消 token、解除事件並 await tasks。
- Redis handler 不使用無追蹤 fire-and-forget；至少加入 lifecycle task tracking 與例外 log。
- Bot shutdown 先停止 YouTube/Twitch member services，再停止 token lifecycle 與 Discord client。

## 12. Provider Result 分類

`YoutubeMemberApiClient` 至少回傳：

- `Member`
- `NotMember`
- `AuthorizationInvalid`
- `ProbeVideoInvalid`
- `QuotaExceeded`
- `RateLimited`
- `TemporaryFailure`
- `LocalContractFailure`

規則：

- 只使用 HTTP status 與 Google error reason，避免依賴 exception message 文字。
- `NotMember` 必須是可確定的 provider response，不能把一般 403 全部視為非會員。
- `AuthorizationInvalid` 只接受 conclusive invalid/revoked credential。
- Quota、429、5xx、timeout、decrypt/config error 不得移除既有 role。
- Probe video invalid 只標記需要重新探索/管理員處理，不因 log channel 失效刪 config。
- 不得 log access token、refresh token、完整 credential 或序列化 token response。

## 13. Backend Contract

Backend 不產生 migration，只 mirror Bot-owned schema。

### 13.1 Entity/DTO

`YoutubeMemberCheck` mirror 新增：

```csharp
public bool PendingRoleRemoval { get; set; }
```

`GoogleMemberSubscription` response 新增：

```json
{
  "guildId": 123,
  "channelId": "UC...",
  "isChecked": false,
  "pendingRoleRemoval": true,
  "lastCheckedAt": "2026-08-04T12:00:00Z"
}
```

`GoogleAccountLink` 新增：

```json
{
  "cleanupPending": true
}
```

OAuth `status` 維持 `linked | unlinked | invalid`，不得把 Discord role cleanup 混入 OAuth status。

### 13.2 GET `/account-links`

- Linked：回傳所有現有 subscription rows。
- Unlinked/invalid：至少回傳 pending cleanup rows 與 `cleanupPending=true`。
- Pending cleanup 不可因 token row 已刪除而消失。
- 額外欄位必須使用明確的 Newtonsoft `JsonProperty` lower-camel 名稱。

### 13.3 DELETE `/account-links/google`

1. 驗證 Discord session。
2. 執行既有 Google provider revoke。
3. Provider revoke 失敗時保留 token/check，回傳 503。
4. Provider revoke 成功後，在 DB transaction 將該 user checks 設為 `IsChecked=false`、`PendingRoleRemoval=true`，並刪除 local token。
5. Commit 後 publish `member.revokeToken` wake-up hint。
6. 有 pending rows 時回傳 HTTP 202 與 typed body。
7. 沒有 pending rows 時回傳 HTTP 200。
8. 若 revoke 期間 token 已被新連結取代，保留新 token/check state 並回傳 HTTP 409 retryable response。

建議 response：

```json
{
  "status": "unlinked",
  "cleanupPending": true
}
```

Redis publish 失敗不得回滾已 durable 保存的 unlink intent；Bot 週期掃描負責恢復。

## 14. Frontend

### 14.1 TypeScript contract

更新 `src/lib/accountLinks.ts`：

- `GoogleAccountLink.channelId`
- `GoogleAccountLink.subscriptions`
- `GoogleAccountLink.cleanupPending`
- `GoogleMemberSubscription.pendingRoleRemoval`
- Typed unlink response，不再使用 `Promise<void>`。

不要把 member cleanup state 加入 `GoogleViewStatus`。

### 14.2 GoogleSection

- 保留現有 red/zinc card 與 responsive layout。
- 新增 `/youtube-member check` 使用提示。
- `cleanupPending=true` 時顯示持續存在的 amber inline status，不只顯示 toast。
- Google 已 unlinked 但 role 尚待清理時，header 顯示「未連結」，另顯示「身分組清理中」。
- 提供手動重新取得狀態動作；不新增無限 polling。
- 動態狀態使用可存取的 live region，不能只靠顏色區分。
- Pending cleanup 時不要顯示「已完成解除」或「身分組已移除」。

### 14.3 VerifyWindow

- Parse DELETE 200/202 body。
- Optimistic update 不得丟掉 subscription/pending metadata。
- 立即 refresh 失敗時保留 last-known cleanup state。
- 401 仍清除 Discord session；503 顯示 provider revoke 尚未完成。
- Network operation pending 與 durable cleanup pending 使用不同 state。

### 14.4 Copy/Privacy

- GoogleSection 說明改為「YouTube 頻道資料與會員限定影片留言」。
- Account 名稱與頭像標示為 YouTube channel，而不是 Google profile。
- Privacy 補上 membership check result、last-check time 與 pending role cleanup retention。
- Twitch `/twitch-member check` 文案不改。

## 15. 實作階段

每階段只有在該階段驗證完成後才能勾選。不得預先勾選。

### Phase 0：Baseline 與 characterization

- [x] 確認 Bot HEAD 以 `d060f08` 為基準，三個 repo status 已記錄。
- [x] 跑 Bot、Backend、Frontend baseline build/test/lint。
- [x] 新增 Slash rename、component route、DB transition、Backend JSON contract 的失敗測試或 characterization tests。
- [x] 確認現有 YouTube managed-role 與 Twitch select-menu isolation tests 保留。

驗證：三個 repo baseline command 全部成功；新增測試只因尚未實作目標行為而失敗。

### Phase 1：Schema 與 migration

- [x] 更新 Bot entities 與 `MainDbContext` indexes。
- [x] 產生 migration/designer/snapshot。
- [x] 產生增量冪等 SQL 並更新 `all.sql`。
- [x] 新增 real-MySQL migration/constraint tests。
- [x] 建立 production preflight SQL 或文件化查詢。
- [x] Backend entity mirror 新欄位，但不建立 migration。

驗證：EF 無 pending model changes；generated SQL 與 checked-in SQL 一致；MySQL component tests 通過。

### Phase 2：共用操作與 role ownership

- [x] 將 Twitch coordinator 提升為共用 service，保持現有 Twitch tests 通過。
- [x] 新增跨平台 role reference/entitlement service。
- [x] YouTube/Twitch config validation 都拒絕新跨平台 role collision。
- [x] Twitch Tier role deletion 加入 YouTube reference guard。
- [x] 新增 legacy collision tests。

驗證：Twitch 既有 4-role policy 不退化；YouTube/Twitch 各種 grant/remove/delete 組合均不互相影響。

### Phase 3：YouTube interaction 與 state machine

- [x] Rename Slash groups 與所有三語 metadata/help/snapshot。
- [x] 建立 `YoutubeMemberComponent`，移除 raw global select handler。
- [x] 實作 selection diff 與 legacy component expired response。
- [x] 實作 queued/verified/removal-pending transitions。
- [x] Cancel/unlink/non-member 在 Discord mutation 前保存 intent。
- [x] 以 row ID/state reload 防止 stale provider result。

驗證：command contract、localization、component authorization、wrong-guild/user/value、stale-result tests 通過。

### Phase 4：Role/config durability

- [x] 建立 YouTube role service。
- [x] 實作 previous-role repair 與 third-role rejection。
- [x] 實作 deletion-pending complete retry path。
- [x] Rejoin restore 排除 pending/deletion states。
- [x] Orphan reconciliation 使用跨平台 entitlement snapshot。
- [x] 缺少 log channel/permission 不刪 config。

驗證：注入 Discord role failure 後重啟可恢復；config deletion 即使 zero checks 仍完成；shared log failure 不造成資料損失。

### Phase 5：Provider 與 lifecycle

- [x] 將 provider call 封裝為 typed result client。
- [x] 移除 message-text destructive classification。
- [x] 移除 credential/token 序列化 log。
- [x] Raw timers 全部改為 tracked `PeriodicRunner` tasks。
- [x] Bot startup/shutdown 明確 start/stop YouTube service。
- [x] Redis/UserJoined subscriptions 可解除並 drain。

驗證：timer 不重入、shutdown 可取消、temporary/quota/local failures 保留 entitlement、disabled mode 仍處理 pending cleanup。

### Phase 6：Backend

- [x] Mirror pending field。
- [x] GET account-links 回傳 cleanup state。
- [x] Google unlink durable 保存 pending intent。
- [x] DELETE 回傳 typed 200/202/409 response。
- [x] Redis revoke 保持原 channel/payload，只作 hint。
- [x] 新增 Google JSON/unlink/DB transition tests。

驗證：linked、unlinked、invalid、pending cleanup、503 與 Redis publish failure cases 全部通過。

### Phase 7：Frontend

- [x] 更新 TypeScript contract。
- [x] 正確處理 unlink 200/202/401/409/503。
- [x] GoogleSection 顯示 `/youtube-member check` 與 cleanup pending。
- [x] Refresh failure 保留 last-known state。
- [x] 更新 Privacy 與 account label。
- [x] 檢查 mobile/desktop、keyboard、screen-reader live region。

驗證：build、ESLint、Stylelint、Prettier 全部通過；手動覆蓋 empty/linked/unlinked/invalid/pending/network failure。

### Phase 8：整合、文件與部署 rehearsal

- [x] 更新 Bot `AGENTS.md` 架構狀態。
- [x] 更新 Bot testing/token/member 文件。
- [x] 更新 Backend README account-links/shared-schema 契約。
- [x] 更新 Frontend README/Privacy。
- [x] 跑三個 repo 完整驗證。
- [x] 在 production-shaped DB rehearsal migration 與 rollback application binaries。
- [x] 驗證 Docker SIGTERM 與 stop grace period 足以 drain。
- [ ] 執行 Discord manual acceptance matrix。

驗證：全部 completion criteria 滿足後才可標記計畫完成。

驗證紀錄（2026-08-05）：Bot Release build 通過、548 passed／33 environment-gated skipped，EF 無 pending model；Backend Release build 通過、51 passed；Frontend build、ESLint、Stylelint、Prettier 通過。增量與完整 SQL 已由最終 migration 重產比對。另於 SSH 測試主機以隔離的 MariaDB 10.11、Redis 8.4 與 .NET 8 SDK container 實跑 component tests，33 passed／0 skipped，包含完整 migration、preflight、constraint、token store 與兩 context row-lock/CAS。Chrome DevTools 已覆蓋 desktop/mobile、長字串、keyboard focus、live region、pending refresh failure 與 DELETE 200/202/401/409/503 mock contract；最終 accessibility、best practices、SEO、agentic browsing 均為 100，無 console error 或水平 overflow。

同日 production-shaped rehearsal 由上一筆 migration 建立 MariaDB schema 並放入代表性舊資料；read-only preflight 無 blocker 後，checked-in 增量 SQL 連續執行兩次，migration history 維持單筆、舊資料 backfill 為 `false`／`null`、五個 indexes 正確建立且 duplicate natural key 被拒絕。Current Scraper 與基準 commit `d060f08` Scraper 均可在保留 additive schema 的情況下啟動，SIGTERM 後 exit 0，驗證優先回退 application binary 而不執行 schema Down。Notifier 另以不連 Discord 的 `Debug_DontRegisterCommand` rehearsal artifact 啟動完整 Redis consumer 與 YouTube/Twitch lifecycle，收到 `member.revokeToken` 後送 SIGTERM，在 1 秒內 drain 並 exit 0，低於 30 秒 stop grace。Rehearsal 期間發現 preflight 使用錯誤的 Twitch Tier 欄位名，已修正並加入直接執行 SQL 的 MySQL component regression test。真實 Discord／Google acceptance 尚未執行，因此計畫仍未完成。

## 16. 驗證命令

### 16.1 Bot

```powershell
dotnet build DiscordStreamNotifyBot.sln -c Release
dotnet test DiscordStreamNotifyBot.sln -c Release --no-build
dotnet ef migrations has-pending-model-changes --project src/DiscordStreamNotifyBot.Shared --configuration Release
git diff --check
```

有 MySQL/Redis component environment 時必須實跑，不可只接受 skip。

### 16.2 Backend

```powershell
dotnet build DiscordStreamBotBackend.sln -c Release
dotnet test DiscordStreamBotBackend.sln -c Release --no-build
git diff --check
```

### 16.3 Frontend

```powershell
pnpm build
pnpm lint:script
pnpm lint:style
pnpm exec prettier --check .
git diff --check
```

## 17. Manual Acceptance Matrix

- `/youtube-member` 與 `/youtube-member-set` 存在，舊 global `/member`、`/member-set` 已移除。
- 舊 guild-scoped command 不殘留。
- 新 component 只允許原 user、原 guild 與 active config。
- 舊 component 顯示 expired，不變更資料。
- Twitch component 不被 YouTube handler 承認。
- YouTube/Twitch 同 guild 使用不同 role 時，各自 grant/remove/rejoin/reconcile。
- 新設定嘗試跨平台使用同 role 時被拒絕。
- 既有跨平台 role collision 在任一平台取消時，不移除另一平台仍需要的 role。
- YouTube role update 中斷後可由重試完成。
- YouTube config deletion 中斷後可由週期工作完成。
- Discord 權限暫時失敗時保留 pending state。
- Google quota/timeout/5xx 不移除既有 role。
- Google conclusive invalidation 將 checks 轉為 pending，並持久重試角色清理。
- Backend unlink 在 Bot 離線時仍保存 pending state；Bot 恢復後完成清理。
- Frontend 在 Google 已 unlinked、role 尚待清理時顯示正確雙狀態。
- Shared log channel 缺失或無權限時只記錄 warning，不刪兩平台設定。
- Rejoin restore 與 orphan reconciliation 在 `EnableGuildMembersIntent` 開關兩種狀態都符合契約。
- 多 shard 僅由 owning Notifier 修改該 guild。
- SIGTERM 不留下未追蹤 timer/event task。

## 18. 停機部署順序

更新檔案可照使用者指定順序 Frontend -> Backend -> Bot，但所有服務在 migration 與檔案更新完成前保持停止。

1. Frontend 進維護模式，停止對外 OAuth/account-links 操作。
2. 停止 Backend、所有 Notifier、Scraper、Coordinator。
3. 確認沒有 DB writer 或 active provider operation。
4. 建立並驗證 DB backup。
5. 執行 duplicate/orphan/role-collision preflight。
6. 有無法安全判定的資料時停止部署，不自動修正。
7. 套用 reviewed idempotent migration SQL。
8. 驗證 columns、defaults、indexes、constraints 與 `__EFMigrationsHistory`。
9. 在停止狀態更新 Frontend、Backend、Bot artifacts。
10. 啟動 Backend 並確認 schema/API health。
11. 啟動 Bot stack，確認 shard ownership、pending cleanup 與 Slash registration。
12. 確認 shard 0 global overwrite 完成，並清除 stale guild command。
13. 啟用 Frontend，執行 account-link/unlink smoke test。
14. 完成 manual acceptance 後解除維護模式。

Rollback 原則：

- 優先回退 application binary，保留 additive schema。
- 任一 pending/deletion/previous-role state 未清空時，不得啟動不理解新欄位的舊 Bot。
- Incident 中不執行 Down migration。
- Schema rollback 只在所有 writer 停止、pending state 清空且有 verified backup 時考慮。

## 19. Completion Criteria

- 三個 repo build/test/lint 全部通過。
- Bot EF 無 pending model changes，增量與完整 migration SQL 可重現。
- MySQL/Redis component tests 在真實環境通過，不只是 skip。
- YouTube 所有 Discord role mutation 都經過 operation coordinator 與 role ownership policy。
- 所有 destructive transition 都先保存 durable intent。
- Temporary provider/log/Discord failure 不會遺失 entitlement 或 retry evidence。
- Slash metadata、三語資源、Help、snapshot、Frontend copy 完全使用新 group 名稱。
- Backend/Frontend 可表示 account unlinked 但 role cleanup pending。
- Legacy role collision 不會讓兩平台互相移除仍有效 role。
- Full-stop deployment rehearsal 與 Discord manual acceptance 完成。
- `AGENTS.md` 與相關文件反映實作後架構。

## 20. 新 Session 執行規則

- 先讀三個 repo 的工作規則與本文件，再開始修改。
- 以工作樹為真實來源，不假設本文件行號永遠正確。
- 先完成 Phase 0，逐 phase 實作與驗證；不得一次跨越多個未驗證 phase。
- 只修改本計畫要求的三個 repo，不碰 `StreamRecordTools` 或其他 sibling repo。
- 保留 Bot baseline commit `d060f08` 的 Twitch 行為與測試。
- 不 reset、checkout、clean、rebase 或覆寫使用者/其他 session 的改動。
- 未經使用者明確要求，不 commit、push 或建立 PR。
- 發現 production data 需要人工判斷時停止 destructive migration，回報 query 結果與選項。
- 不自動更新 `graphify-out`；完成後提醒使用者手動執行 graphify update。
