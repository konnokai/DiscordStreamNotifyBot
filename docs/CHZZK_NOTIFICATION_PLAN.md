# CHZZK 直播通知實作計畫

建立日期：2026-09-15。已實作，驗證現況見 §13，關台通知修正見 §14。

## 1. 目標與範圍

在「直播小幫手」加入 CHZZK 指定頻道的輪詢式開台、關台通知，沿用既有 Scraper、Redis Streams、Notifier、MySQL 架構。

使用者已指定：**streamKey 以 channelId + openDate 為準**。不以 liveId、chatChannelId、標題或分類代替。

計畫涵蓋 API 存取、場次狀態、通知去重與補送、Discord Slash 設定，以及網站設定中心的跨專案整合。網站部分可分階段交付，但不得顯示尚未接通的假功能。

首版不加入錄影、會員驗證、聊天、斗內、Drops、Discord 活動、橫幅變更或標題變更通知。不另外建立通用平台框架，也不改寫既有三平台。

使用者已決定基本對齊 Twitch 架構，但不加入 `IsWarningUser` 欄位、警告清單、狀態切換按鈕或相關指令。非 CHZZK API 的管理、通知及網站流程優先沿用 Twitch；本文件明訂的 CHZZK 特例優先。

## 2. 已確認資料來源

### 2.1 頻道資料

```http
GET https://api.chzzk.naver.com/service/v1/channels/{channelId}
```

已觀察到不帶應用憑證即可取得資料，成功回應包在 `content`：

- `channelId`、`channelName`、`channelImageUrl`。
- `openLive`、`followerCount` 等頻道資料。

用途為新增頻道驗證、名稱與頭像快取。不要在每次直播輪詢時重複抓取。

### 2.2 直播狀態

```http
GET https://api.chzzk.naver.com/polling/v3.1/channels/{channelId}/live-status?includePlayerRecommendContent=false
```

已觀察到不帶應用憑證即可取得資料。使用下列欄位：

| 欄位 | 用途與觀察 |
|---|---|
| `content.channelId` | 必須與請求頻道相符 |
| `content.status` | `OPEN`、`CLOSE`；生命週期判斷來源 |
| `content.openDate` | 場次識別；格式範例 `2026-09-15 13:41:52` |
| `content.closeDate` | OPEN 樣本為 null；CLOSE 樣本有時間 |
| `content.liveTitle` | 直播標題 |
| `content.liveCategoryValue` | 顯示用分類名稱 |
| `content.liveCategory` | 分類識別碼 |
| `content.tags` | 標籤，可選顯示 |
| `content.concurrentUserCount` | 當次快照人數，不是最高人數 |

OPEN 與 CLOSE 樣本均未包含 liveId 或直播縮圖。首版可使用頻道頭像，不必為縮圖增加另一個資料來源。

**已知陷阱：** CLOSE 樣本中的 `livePollingStatusJson` 仍為 `STARTED`、`isPublishing=true`、`PLAYABLE`。不得用該欄位判定直播。`callPeriodMilliSecond=10000` 也不是 Bot 的配額或輪詢契約。CLOSE 回應可能保留上一場標題及觀看人數，不得把資料存在視為仍在直播。

### 2.3 與官方 Open API 的差異

上述為 CHZZK 網站使用、未列入公開 Open API 文件的 endpoint，不是需要 Client ID/Secret 的 `openapi.chzzk.naver.com` API。

此方案不需要為直播查詢加入 CHZZK OAuth、token refresh 或 Client Secret。匿名可讀不代表官方承諾第三方使用、配額或穩定性；部署前需接受此依賴風險，並查核適用條款。若遭拒絕存取，不以代理輪替、偽造登入等方式繞過。

官方 Live API 只有全站直播清單，每頁最多 20 筆，不能當成同等成本的指定頻道 fallback。首版不實作自動 fallback。

## 3. 場次識別與時間

具體鍵格式：

```text
streamKey = channelId + ":" + 正規化 openDate
```

使用者於 2026-09-15 進一步指定 openDate 在鍵內的正規化方式：移除冒號與減號、空格改底線，例如 `2026-09-15 12:41:10` → `20260915_124110`，因此完整鍵形如 `{channelId}:20260915_124110`。實作以解析後（KST）的時間重新格式化，與逐字元取代結果一致且不受空白差異影響；DB 的 `OpenDateRaw` 與通知 DTO 仍保留 API 原始字串。分隔符只為清楚界定兩個欄位，識別來源仍只有使用者指定的 channelId 與 openDate。

- 缺少或無效的 channelId/openDate 時，不得產生開台事件或用目前時間補造場次。
- 相同鍵代表同一場，標題或分類變更不能觸發新的開台通知。
- 同一頻道的新 openDate 代表新場次，即使中間漏看 CLOSE，也要辨識為新場次。
- chatChannelId 不作為直播場次 ID。
- 時間字串沒有 offset。時區需驗證，不能直接假定為執行環境本地時間；在確認前不宣稱 UTC、精確時長或關台時間。
- 若平台在斷線恢復時改變 openDate，這套契約會辨識為新場次；不得暗中以冷卻時間合併不同鍵。若需合併，另外取得使用者決策。

## 4. 已確認生命週期契約

使用者已選擇首次 OPEN 立即通知、關台延遲 3 分鐘重新確認。場次仍嚴格依 streamKey 判斷，不照搬 Twitch 將不同 stream ID 的短暫重連合併的行為。

| 情境 | 行為 |
|---|---|
| 全新追蹤頻道，首次有效回應為 OPEN | 保存場次並立即發布開台，後續依 streamKey 去重 |
| 全新追蹤頻道，首次為 CLOSE | 建立離線基線，不發關台 |
| 已建立離線基線後收到 OPEN | 保存新場次並發布開台 |
| 已追蹤且收到相同 streamKey 的 OPEN | 更新快照，不重發 |
| 已追蹤且 OPEN 的 streamKey 改變 | 發布新場次開台；舊場標示已被取代，不捏造舊場關台時間 |
| 已知目前場次收到同 streamKey 的 CLOSE | 進入關台確認；等待 3 分鐘後重新查詢，成功確認同場 CLOSE 才發布一次關台 |
| 關台確認期間收到相同鍵 OPEN | 取消關台確認，不重發開台 |
| 收到不同舊場次的 CLOSE | 不得關閉目前場次 |
| API 失敗、未知 status、content 為 null 或必要欄位異常 | 本輪為未知，不更新為離線，不發通知、不刪設定 |
| 程式重啟或 Scraper leader 切換 | 沿用 Twitch 的狀態恢復、輪詢與去重設計，不刻意清除既有場次或通知標記；可靠性邊界見第 5 節 |

只對已觀察到 OPEN 的場次發布關台；首次查詢就是 CLOSE 時不得對上一場補發關台。

3 分鐘等待期間的重複 CLOSE 不重設等待起點，也不能因下一輪 CLOSE 提早通知。確認查詢失敗時維持未知，後續重試，不能當作已關台。相同鍵 OPEN 取消等待；不同鍵 OPEN 按新場處理並取消舊場工作，避免舊工作關閉新場。

## 5. 持久化、發布與去重

先讀現有平台的狀態及發布流程，沿用已能滿足下列要求的機制，避免重做通知基礎設施。

- 保存每個追蹤頻道的初始化狀態、目前 streamKey、最後有效 status、關台待確認狀態及最後 OPEN 快照。
- 場次快照保留名稱、標題、分類、openDate；關台通知以已保存的場次資料為主。
- 開台與關台各以 `(CHZZK, streamKey, eventType)` 去重，避免互相覆蓋。
- 使用者已選擇對齊 Twitch 的發布、錯誤處理、去重與補送設計，沿用其 NotificationBus 發布路徑；不強制改用 PublishOnceAsync。
- 不新增 outbox 表，也不要求 MySQL 狀態與待發布事件同交易。不要把場次存在視為 Discord 已送達，亦不可宣稱已消除 DB 到 Redis 之間程序中斷造成的漏送風險。
- 去重保留期及重試策略先核對 Twitch 的現行使用方式，不另創無依據的 TTL 或重試上限。
- Notifier 沿用 `NotificationDeliveryProgress`、PEL、ACK 與逐目標 checkpoint，不另外直接呼叫 Discord 繞過匯流排。
- 不宣稱 exactly-once：Discord 發送成功但 checkpoint 尚未保存時程序中斷，既有架構仍可能重複。

不可只用記憶體保存 lastStatus 或最近通知鍵，否則重啟會重發、漏發或誤發關台。

### 5.1 已確認新增的資料表

平台 Entity 名稱統一使用 `Chzzk`，沿用現有 `TwitchSpider`、`TwitchStream`、`NoticeTwitchStreamChannel` 的命名方式。實際資料表名稱、型別與 EF Core mapping 依專案慣例設定。

本次確認新增以下三張表，另擴充 GuildConfig。發布可靠性對齊 Twitch，不新增 `ChzzkNotificationOutbox`。

### 5.2 ChzzkSpider

每個 CHZZK 頻道一筆，保存追蹤來源、頻道快取及目前場次指標，不因多個 guild 設定通知而重複建立。

| 欄位 | 用途 |
|---|---|
| `ChannelId` | 主鍵，CHZZK 頻道 ID |
| `ChannelName` | 頻道名稱快取 |
| `ChannelImageUrl` | 頻道頭像快取 |
| `GuildId` | 登記／管理來源的 Discord guild；Bot Owner 選擇 Owner 歸屬時為 `0` |
| `DateAdded` | 加入追蹤時間，UTC |
| `InitializedAt` | 首次有效基線建立時間，UTC；null 表示尚未初始化 |
| `CurrentStreamKey` | 目前或最近確認的場次；尚無場次時可為 null |

**Bot Owner 機制：** Slash 新增時沿用 Twitch 詢問是否用於目前 guild。選是，使用目前 guild ID 並依該 guild 規則計數；選否，由伺服器端設定 `GuildId = 0`，不占操作 guild 名額也不受該 guild 上限限制。一般管理員只能使用經授權的實際 guild ID，不得藉由提交 `0` 取得 owner 來源權限。Web 維持 guild 設定入口，不自行加入 owner 模式；Owner 專用指定／修改歸屬與移除操作依 Twitch 權限模式提供。

`GuildId = 0` 代表 Bot Owner 管理，不代表無效或遺失 guild。guild 離開、刪除及孤兒清理流程不得因找不到 guild 0 而刪除該來源。既有來源的管理歸屬變更沿用現行 owner 權限流程，不因一般 guild 新增通知設定而覆寫。

`GuildId` 不是通知訂閱者清單；訂閱關係由 `NoticeChzzkStreamChannel` 表達。不另建頻道名稱／頭像表，也不加入首版未使用的錄影欄位。

### 5.3 ChzzkStream

每個 streamKey 一筆，保存場次快照及本地生命週期，支援重啟恢復、關台確認與舊場次辨識。

| 欄位 | 用途 |
|---|---|
| `Id` | 沿用 `DbEntity` 主鍵 |
| `StreamKey` | `channelId + ":" + 正規化 openDate`（如 `20260915_124110`），不可重複 |
| `ChannelId` | 所屬 CHZZK 頻道 |
| `OpenDateRaw` | API 原始開台時間，保留場次識別來源 |
| `CloseDateRaw` | API 回報的關台時間，可為 null |
| `StreamTitle` | 最近一次有效直播標題 |
| `CategoryName` | 顯示用分類名稱 |
| `Status` | 本地生命週期：`Open`、`PendingClose`、`Closed`、`Superseded` |
| `LastObservedAt` | 最近一次有效觀察時間，UTC |

建立 `StreamKey` 唯一索引，以及 `ChannelId` 一般索引。`Superseded` 表示已觀察到新場次，但未確認舊場的關台時間；不補造 closeDate。API 錯誤不改為 Closed，也不建立錯誤場次。

### 5.4 NoticeChzzkStreamChannel

每個 guild 對同一 CHZZK 頻道只能設定一個 Discord 通知目的地。

| 欄位 | 用途 |
|---|---|
| `Id` | 沿用 `DbEntity` 主鍵 |
| `GuildId` | 經授權的實際 Discord guild ID |
| `DiscordChannelId` | Discord 通知目的地 |
| `NoticeChzzkChannelId` | 要通知的 CHZZK 頻道 ID |
| `StartStreamMessage` | 開台自訂訊息 |
| `EndStreamMessage` | 關台自訂訊息 |

**資料庫唯一索引固定為 `(GuildId, NoticeChzzkChannelId)`。** 更換目的地時更新同一筆設定，不能因 DiscordChannelId 不同而新增第二筆。Slash 與 Web 的 upsert 查找條件必須一致，並以資料庫唯一約束防止併發新增產生重複。

不同 guild 可各自訂閱同一來源。`GuildId = 0` 的 owner 特例只適用於 `ChzzkSpider`，不適用於本表。不加入首版未使用的 `ChangeStreamDataMessage`，也不另建 guild 與 CHZZK 頻道關聯表。

### 5.5 GuildConfig 擴充與爬蟲上限

使用者已指定：在既有 `GuildConfig` 表新增 `MaxChzzkSpiderCount`，預設值為 `3`。這是既有表擴充，不另建配額表。

- 欄位型別、EF Core mapping 及設定取得方式沿用現有平台的 MaxSpiderCount 欄位慣例。Entity 預設值與 migration 的資料庫預設值均為 3，既有 guild 紀錄也須初始化為 3；保留其他平台欄位與既有資料。
- 伺服器管理員新增 CHZZK 爬蟲時，必須讀取該 guild 的 `GuildConfig.MaxChzzkSpiderCount` 作為當次上限，不得在服務、Slash 或 Web 內寫死 3。日後設定值調整應直接生效。
- 使用者已指定沿用 Twitch：欄位值大於 0 時使用設定值；值為 0 或沒有 GuildConfig 紀錄時回退預設 3。0 不是禁止新增，也不是無上限。
- 官方 guild 沿用 Twitch 豁免名額限制。snapshot 與前端上限判斷須沿用既有官方 guild 豁免的表示方式，不能讓前端誤擋伺服器端允許的新增。
- 計數使用 `ChzzkSpider.GuildId == 目前 guild ID` 的來源筆數，不以通知目的地數或 `NoticeChzzkStreamChannel` 筆數計算。
- 新增會增加來源筆數時，若目前數量已達上限，拒絕新增且不留下資料。已存在來源的重複新增不消耗新名額，也不得覆寫來源管理歸屬。
- 上限檢查與新增必須沿用現有可防止同 guild 併發超額的協調方式，不能只在前端判斷或先計數後無保護地寫入。Slash 與 Web 共用同一業務服務。
- Bot Owner 選擇 Owner 歸屬時使用 `GuildId = 0`，不計入操作 guild 名額，也不受該 guild 的 MaxChzzkSpiderCount 限制；選目前 guild 則依該 guild 規則。不要照抄 Twitch AddCrawlerAsync 中 owner 模式仍可能被操作 guild 名額擋住的判斷。
- 網頁爬蟲 snapshot 的 CHZZK `count` 與 `limit` 使用相同計數及設定來源，讓畫面顯示與伺服器端驗證一致。

## 6. API 存取與輪詢

- 使用既有 HttpClientFactory、Newtonsoft.Json、PeriodicRunner 與 GracefulShutdown，不新增 SDK 或排程框架。
- 僅由目前 Scraper leader 執行輪詢。同一 CHZZK 頻道即使被多個 guild 設定，也只查一次。
- 同一頻道的輪詢與狀態轉換不可重入，並尊重 leader 生命週期及取消訊號。
- API 成功需同時檢查 HTTP、body.code、content 與必要欄位。未知 status 視為未知，不用 `status != OPEN` 推導離線。
- 429 遵守有效 Retry-After；無提示時沿用專案可適用的退避規則，不立即密集重試。網路錯誤與 5xx 不得清除狀態。
- 認證要求改變、403、404、null content 不自動刪除使用者設定；記錄可辨識的停用或錯誤資訊。
- 固定 API origin，僅把驗證過的 channelId 放進路徑。使用者提供 URL 時只解析支援的 CHZZK 頻道/直播 URL，不任意抓取使用者網址，避免 SSRF。
- 只記錄診斷所需欄位，不保存完整 liveTokenList、原始 cookie 或其他可能敏感的回應資料。
- 直播連結可評估 `https://chzzk.naver.com/live/{channelId}`，需在實作驗證時確認正常導向。

輪詢頻率已由使用者決定：**每個頻道 30 秒查一次**（`ChzzkDetectionService.PollInterval` 常數，不做設定項）。同一輪循序查詢所有已追蹤頻道，查詢期間不重入；429 的 `Retry-After` 生效期間跳過輪詢。

使用者已確認接受未公開 endpoint 的維護、使用條款及可用性風險（2026-09-15 指示直接使用網站匿名 endpoint），並知悉此依賴可能隨時變更。

## 7. 專案接入位置

以下路徑以 `DiscordStreamNotifyBot/` 為基準；接手時以工作樹為準重新核對。

| 區域 | 既有入口與預期修改 |
|---|---|
| API client、資料存取 | `src/DiscordStreamNotifyBot.Shared/`；放入 CHZZK HTTP client、必要 DTO、資料表與 migration |
| 偵測 | `src/DiscordStreamNotifyBot.Scraper/Detection/` 新增 CHZZK 服務；`DetectionHost.cs` 註冊與啟動 |
| 通知契約 | `src/DiscordStreamNotifyBot.Shared/Messages/Notifications.cs` 新增 NotifyType 與 CHZZK DTO |
| 發送 | `src/DiscordStreamNotifyBot.Notifier/NotificationBusConsumer.cs` 加分流；平台發送服務放 `SharedService/` |
| 補送與去重 | 核對 `NotificationDedupPolicy`、`NotificationDeliveryProgress` 及相關既有政策的 CHZZK 支援 |
| 使用者設定 | `Interaction/` 加 Slash 群組；頻道增刪與通知設定共用同一業務服務 |
| 網站契約 | `Shared/Messages/AdminSettings.cs`、`Notifier/SharedService/AdminSettings/` 擴充 CHZZK capabilities、snapshot、desired-state actions |
| 本地化與營運 | zh-TW/en-US/ja 資源、Help、有限標籤 metrics、必要啟動設定與文件 |

通知 DTO 至少涵蓋：事件類型、channelId、channelName、channelImageUrl、streamKey、原始 openDate、可選 closeDate、轉換後的 UTC 開台／關台時間（`StreamStartAt`／`StreamEndAt`，顯示用；消費者已改為直接使用，不再自行解析原始字串）、liveTitle、category。保留語言中立，不傳序列化 Discord Embed；資料不足的欄位可省略，不能捏造。

只新增必要的啟用/輪詢設定，不新增不存在需求的憑證設定。缺乏直播縮圖時使用頭像或不放大圖。

## 8. 使用者與網站設定

沿用本專案「追蹤來源」與「guild 通知目的地」的既有分離模式，避免新增 CHZZK 特有操作習慣。Slash 與 Web 必須使用同一業務服務。

- 管理員可新增/移除 CHZZK 來源、指定 Discord 通知頻道，設定開台/關台訊息。
- 接受 channelId 及經驗證的 CHZZK URL。新增時驗證頻道，失敗不留下半套設定。
- 權限、guild ownership、訊息目的地與 Bot 發送權限沿用現行規則。
- 設定 snapshot 顯示 CHZZK 是否啟用與來源是否正在被偵測，不把「設定存在」當成「可正常通知」。
- CHZZK 爬蟲名額依第 5.5 節，預設 3、0 回退 3、官方 guild 不限名額。
- 新增爬蟲沿用 Twitch 至少 200 名伺服器成員的資格要求；Bot Owner 與官方 guild 豁免人數門檻。這不是 CHZZK 頻道粉絲數要求，不加入 OAuth bypass。
- 同來源已歸屬目前 guild 時回覆已存在；Owner 或仍在叢集內的其他 guild 來源不得被一般管理員接管。原 guild 已退出時，沿用跨 shard guild 快照判斷，在通過資格與名額檢查後允許接管；不得僅因目前 shard 找不到 guild 就視為已退出。
- 爬蟲原登記 guild 或 Bot Owner 可移除來源；移除爬蟲停止偵測，但保留其他 guild 的通知設定，snapshot 顯示 detectionEnabled=false。移除通知設定則不連帶刪除共用爬蟲、場次或匯流排內待補送事件。
- 通知設定不要求來源已有爬蟲；可以先設定並顯示尚未偵測。通知訊息沿用 Twitch：空字串只發 Embed，`-` 停用該類型通知，不另加 Enabled 欄位。
- 沿用 Twitch 的通知快取失效、guild 語言、shard 守衛、逐目標 checkpoint、官方公告頻道 crosspost 與既有非平台專屬通知按鈕。以已存來源 ID 刪除通知或爬蟲不應依賴 CHZZK API 可用性。
- 沿用爬蟲列表、自動完成、Owner 新增成功私訊與管理操作；不複製 IsWarningUser、警告清單、警告切換、錄影按鈕或 Twitch API 專用操作。
- 若全域已在追蹤某頻道，新 guild 加入通知設定不立即重播全域舊事件；若需目前直播補發，另外定義功能。

網站整合目錄：

- `../DiscordStreamBotBackend`：沿用 Discord 身分驗證、guild 授權、Redis request/reply。Backend 不直接寫 Bot 設定表。
- `../auto-discord-ytmember-checker`：SettingsPage 加 CHZZK 平台表單與正確的 capability/錯誤狀態，維持既有兩層導覽。
- action 命名與 payload 欄位先比照當前既有平台，再同步三個 repo 的型別、驗證與測試。不要只改前端。
- 開始 UI 工作前讀適用的 FRONTEND_AGENTS.md 與設計規則。驗證桌機、手機及實際互動。

## 9. 實作前待確認事項

以下不是尋找新 API 的必要前提，但會影響正式設定或顯示。除下列已解決項目外，接手 session 應完成可獨立驗證的部分，再向使用者集中詢問剩餘決策。

- 預計追蹤頻道量與可接受通知延遲，據此決定輪詢設定。**已解決**：使用者決定 30 秒，以程式常數實作（2026-09-15）。
- 確認接受未公開 endpoint 的維護、使用條款及可用性風險。**已解決**：使用者確認（2026-09-15）。
- 實測 openDate/closeDate 時區，確認前不顯示精確時長。**已解決**：使用者指定 UTC+9；2026-09-15 唯讀實測（見 §13）確認 openDate 不可能是 UTC，與 KST 一致。程式以固定 +09:00 轉 UTC 後用 Discord timestamp 顯示，並顯示時長。
- 觀察同一頻道的 OPEN/CLOSE/重開與斷線恢復；現有 OPEN、CLOSE 樣本來自不同頻道，不能證明同頻道轉換語意。**未完成（外部驗證）**：需要長時間觀察真實頻道轉換；狀態機已以 fixture 與決策表測試覆蓋，正式語意仍待上線後觀察。
- 首次 OPEN 立即通知、3 分鐘關台確認及發布可靠性對齊 Twitch 已決定，不再列為待選政策。

## 10. 分階段執行與驗證

狀態標記：`[x]` 程式與可離線驗證皆完成；`[~]` 程式完成但僅能做部分驗證；`[ ]` 未完成（多為需 Redis／Discord／MySQL／多 shard 的外部驗證）。詳見 §13。

### 階段 A：API 與狀態決策

- [x] 讀本專案 AGENTS.md、現有偵測/通知/設定程式碼及適用 graphify 查詢；任何重構前讀 `docs/LETTER_TO_FUTURE_SESSIONS.md`。
- [x] 建立只含必要欄位的 OPEN/CLOSE fixture，不放播放 token 或 cookie。`tests/DiscordStreamNotifyBot.Tests/Fixtures/Chzzk/`。
- [x] 實作 HTTP client、回應驗證、streamKey 與可測試的狀態轉換。`ChzzkClient`、`ChzzkStreamIdentity`、`ChzzkPollPolicy`。
- [x] 測試相同鍵不重發、新 openDate 辨識新場、舊場 CLOSE 不關閉新場、未知/失敗不轉離線。`ChzzkPollPolicyTests`。
- [x] 測試首次 OPEN 立即通知、首次 CLOSE 不通知、重啟去重、3 分鐘後重新確認、確認失敗重試及同鍵恢復 OPEN；不同鍵仍算新場，CLOSE 中巢狀 STARTED 不影響結果。`ChzzkPollPolicyTests`＋`ChzzkClientTests`（重啟恢復以 LastObservedAt 為等待起點，決策可離線驗證；實際跨程序重啟仍屬外部驗證）。

### 階段 B：可靠發布與 Discord

- [x] 實作三張新表、GuildConfig 擴充、狀態保存及 Twitch 同等發布、重試與去重，不新增 outbox。
- [x] 接入唯一 Scraper leader、bot:notify、Notifier dispatch 與既有補送 checkpoint。
- [~] 驗證 Redis 暫時失敗與重啟處理，記錄 DB 到 Redis 中斷風險；不以對齊 Twitch 宣稱發布前事件也有持久化補送保證。程式沿用既有 at-least-once 路徑；**Redis 實際中斷與補送尚未實測**。
- [x] 加入 Slash 設定、三語訊息、目的地/權限驗證與可用性顯示。`/chzzk`、`/chzzk-spider`、三語 resx、snapshot `detectionEnabled`。
- [ ] 驗證 Owner Slash 的歸屬詢問兩條路徑、Owner 來源 GuildId=0 且不受操作 guild 上限限制、一般管理員不能偽造 owner 來源、guild 清理不誤刪 owner 來源。**程式已實作（owner 新增不套用 guild 名額；GuildId=0 不計入計數），未經正式 Discord 實測**。
- [ ] 驗證 200 人資格門檻及 Owner/官方 guild 豁免、官方 guild 名額豁免、0 或缺設定回退 3、跨 shard 來源接管、移除爬蟲保留通知、空字串與 `-` 語意，以及 IsWarningUser 相關功能確實未加入。**程式已實作，未經正式 Discord 實測**；已知限制：Web 前端的「新增爬蟲」按鈕以 `count >= limit` 停用，官方 guild 的伺服器端豁免未反映在 snapshot，沿用 Twitch 現行表示方式。
- [ ] 驗證 GuildConfig 新舊紀錄的 MaxChzzkSpiderCount 預設為 3、讀取調整後的上限、達上限拒絕新增、重複新增不占名額、併發不超額，以及 owner 來源不計入 guild 名額；Slash/Web 與 snapshot 必須一致。**migration 的 `DEFAULT 3` 已人工審查；MySQL 併發與既有資料列升級未實測（本機無測試 DB）**。
- [ ] 驗證 `(GuildId, NoticeChzzkChannelId)` 唯一約束、變更目的地更新原紀錄、併發新增不重複，以及不同 guild 可訂閱同一來源。**唯一索引已在 migration 建立；未經 MySQL 實測**。
- [ ] 以測試環境驗證同頻道多 guild 只輪詢一次、各 shard 只發自己的 guild、部分目標失敗可補送。**未實測（需 Redis＋多 shard＋Discord）**。

### 階段 C：網站設定與交付

- [x] 同步 Bot、Backend、Frontend 的 capability、action、snapshot、表單及驗證。Bot 新增 capability/action/snapshot 區塊；Backend 為 pass-through 無需改動；Frontend 新增 CHZZK 表單、型別與 action。三 repo 檢查皆通過。
- [ ] 驗證 Slash/Web 共用行為、未授權存取、無效頻道、API 不可用與功能停用。**未實測（需實際 Backend＋Bot＋Discord 登入）**。
- [x] 按 repo 規則產生單次增量及完整冪等 migration SQL，人工審查；不可直連正式 DB 執行 database update。`migrate_sql/20260915073735_AddChzzkNotification.sql`、`migrate_sql/all.sql`（未執行）。
- [x] 執行 Bot 完整 Release build/test、Backend 與前端適用的既有檢查，以及相關 diff 檢查。Bot：build 0 警告/錯誤、681 passed／38 skipped（Redis/MySQL component tests 因環境跳過）；Backend：76 passed；Frontend：`vue-tsc`／eslint／stylelint／vite build 皆通過；三 repo `git diff --check` 乾淨。
- [ ] 經授權才在測試 Discord guild 傳訊息；外部 API、正式 Discord、正式 DB 或多 shard 尚未驗證者明列，不以 unit tests 宣稱已通過。
- [x] 更新必要文件與 AGENTS.md 狀態，不自動部署、commit、push 或 graphify update。

使用現有測試框架，不新增框架、不規定任意測試數量。Bot 命令：

```powershell
dotnet build DiscordStreamNotifyBot.sln -c Release
dotnet test DiscordStreamNotifyBot.sln -c Release
```

## 11. 交接提示詞

> 請依 `docs/CHZZK_NOTIFICATION_PLAN.md` 實作 CHZZK 直播通知。streamKey 使用 `channelId + ":" + 正規化 openDate`（移除冒號與減號、空格改底線，如 `20260915_124110`；見 §3）。使用網站匿名指定頻道 endpoint，不加入 CHZZK OAuth 或全站輪詢 fallback。先讀現行工作樹及規則，沿用 Scraper → bot:notify → Notifier、既有去重與逐目標補送，保留使用者及其他 session 的變更。先確認第 9 節未決產品/營運設定，完成可獨立進行的實作與驗證，不擅自填配額、人數門檻或輪詢秒數。每完成一階段更新本文件勾選項，明列未完成的外部驗證；不要自行 commit、push、部署、傳送正式 Discord 訊息或修改正式資料庫。

## 12. 參考來源

- 使用者提供的 channel、CLOSE、OPEN JSON 回應及本次匿名讀取觀察。
- [官方 Live API](https://chzzk.gitbook.io/chzzk/chzzk-api/live)
- [官方 Session API](https://chzzk.gitbook.io/chzzk/chzzk-api/session)
- [官方 Drops Webhook](https://chzzk.gitbook.io/chzzk/drops/webhook)
- [官方 API 參考事項](https://chzzk.gitbook.io/chzzk/chzzk-api/tips)
- 既有 `docs/WEB_ADMIN_SETTINGS_PLAN.md`、`docs/WEB_ADMIN_CRAWLER_VERIFICATION_PLAN.md` 與目前原始碼。

## 13. 實作與驗證現況（2026-09-15）

### 已實作的檔案（DiscordStreamNotifyBot repo）

| 區域 | 檔案 |
|---|---|
| HTTP client／模型 | `Shared/HttpClients/Chzzk/ChzzkClient.cs`、`ChzzkModels.cs` |
| streamKey／時間／URL | `Shared/SharedService/Chzzk/ChzzkStreamIdentity.cs`、`ChzzkUrlParser.cs` |
| 資料表 | `Shared/DataBase/Table/ChzzkSpider.cs`、`ChzzkStream.cs`、`NoticeChzzkStreamChannel.cs`；`GuildConfig.MaxChzzkSpiderCount` |
| Migration | `Shared/Migrations/20260915073735_AddChzzkNotification.cs`；`migrate_sql/20260915073735_AddChzzkNotification.sql`、`migrate_sql/all.sql` |
| 通知 DTO／去重 | `Shared/Messages/Notifications.cs`（`NotifyType.Chzzk`、`ChzzkNotification`）、`Notifier/NotificationDedupPolicy.cs` |
| 偵測 | `Scraper/Detection/Chzzk/ChzzkDetectionService.cs`、`ChzzkStateDecisions.cs`、`Scraper/DetectionHost.cs` |
| 發送 | `Notifier/SharedService/Chzzk/ChzzkService.cs`、`ChzzkEmbedBuilderFactory.cs`、`Notifier/NotificationBusConsumer.cs`、`Notifier/NotifierMetrics.cs` |
| 指令與三語 | `Notifier/Interaction/Chzzk/Chzzk.cs`、`ChzzkSpider.cs`、`BotMessages*.resx`、`InteractionCommands*.resx`、command contract snapshot |
| 網站契約 | `Shared/Messages/AdminSettings.cs`、`Notifier/SharedService/AdminSettings/AdminSettingsService.cs`、`CrawlerOwnerNotifier.cs`（`CrawlerPlatform.Chzzk`）、`CrawlerPolicy`（沿用） |
| 測試 | `tests/.../ChzzkPollPolicyTests.cs`、`ChzzkClientTests.cs`、`ChzzkStreamIdentityTests.cs`、`NotificationDedupPolicyTests.cs`、`Fixtures/Chzzk/*.json` |
| 前端 | `auto-discord-ytmember-checker/src/lib/adminSettings.ts`、`src/page/SettingsPage.vue` |
| 後端 | 無需修改：`AdminGuildsController` 以 pass-through 轉送 action/payload 與 snapshot JObject |

streamKey 的 openDate 正規化（使用者 2026-09-15 指定）：`2026-09-15 12:41:10` → `20260915_124110`（移除冒號與減號、空格改底線）。僅影響鍵本身，`ChzzkStream.OpenDateRaw` 與通知 DTO 的 `OpenDate`／`CloseDate` 仍保留 API 原始字串供識別與診斷；功能尚未上線，沒有既有鍵需要回填。通知 DTO 另帶偵測端轉換好的 UTC `StreamStartAt`／`StreamEndAt`，Notifier 的 embed 顯示直接使用，不再自行解析原始字串。

### 已完成的驗證

- `dotnet build DiscordStreamNotifyBot.sln -c Release`：0 警告、0 錯誤。
- `dotnet test DiscordStreamNotifyBot.sln -c Release`：681 passed、38 skipped（Redis／MySQL component tests 因本機無服務而跳過）、0 failed。
- Backend `dotnet test DiscordStreamBotBackend.sln -c Release`：76 passed、0 failed（未改動，確認基線）。
- Frontend `vue-tsc --noEmit`、`eslint`、`stylelint`、`pnpm build` 全數通過。
- 三 repo `git diff --check` 無錯誤。
- Migration SQL 人工審查：`ALTER TABLE guild_config ADD max_chzzk_spider_count int unsigned NOT NULL DEFAULT 3`、三張新表、`chzzk_streams.stream_key` 唯一索引、`notice_chzzk_stream_channels (guild_id, notice_chzzk_channel_id)` 唯一索引。

### 唯讀實測（2026-09-15，未寫入任何資料）

- `GET https://api.chzzk.naver.com/service/v1/channels/f39c3d74e33a81ab3080356b91bb8de5` → HTTP 200、`code=200`，`channelName`／`channelImageUrl`／`openLive` 可用，符合 DTO 必要欄位。
- `GET .../polling/v3.1/channels/{id}/live-status?includePlayerRecommendContent=false` → HTTP 200、`status=OPEN`、`openDate=2026-09-15 13:41:52`、`closeDate=null`，`livePollingStatusJson` 仍為 `STARTED`（維持原陷阱結論：只信任頂層 `status`）。
- 時區：實測當下 UTC 為 `2026-09-15 07:47:38`。openDate 若為 UTC 會落在未來約 6 小時，與 OPEN 矛盾；以 KST（UTC+9）解讀為 `04:41:52 UTC`（約 3 小時前）才合理。程式固定以 +09:00 轉 UTC。
- `https://chzzk.naver.com/{channelId}` 與 `https://chzzk.naver.com/live/{channelId}` 直接回 200 HTML（無 redirect），兩種連結皆可用。
- 未刻意觸發 429（不做壓測探測門檻）。

### 尚未完成的外部驗證（不宣稱已通過）

- 正式 Discord：Slash 指令實際註冊與互動、owner 歸屬兩條路徑、權限與 guild 清理、200 人門檻、官方 guild 豁免、空字串／`-` 語意、通知實際發送與 crosspost。
- Redis／MySQL：唯一約束、併發新增、GuildConfig 既有資料列升級、`XAUTOCLAIM` 補送、實際 DB→Redis 中斷情境。
- 多 shard：同頻道只輪詢一次、各 shard 只發自己的 guild。
- 真實頻道長時間觀察：同頻道 OPEN→CLOSE→重開、斷線恢復、平台是否變更 openDate。
- Web 端：實際 Backend＋Bot 連線下的表單操作與錯誤狀態顯示（未在瀏覽器實測）。
- 已知限制：Web 前端新增爬蟲按鈕以 `count >= limit` 停用，官方 guild 的伺服器端豁免未反映在 snapshot（沿用 Twitch 現行表示方式）；場次存在不等於 Discord 已送達，DB→Redis 程序中斷仍可能漏送；同鍵場次列已存在時（例如移除爬蟲後重新加入）接回既有場次且不重發。

## 14. 關台通知修正（2026-09-16）

- 根因：`ApplyObservationAsync` 原本僅在 OPEN 時建立 streamKey，CLOSE 一律帶 null 進入狀態機，被判定為不同場而忽略，無法進入 PendingClose。既有測試直接提供正確 facts，未涵蓋這段轉換。
- 修正：OPEN／CLOSE 共用原有正規化鍵；未知 status 先拒絕，不能落入 CLOSE 建立基線。保留首次 CLOSE 不補發、同場三分鐘確認、同鍵恢復取消、不同鍵為新場的政策。不改既有 DB 鍵或 schema。
- 關台發布對齊 Twitch：Redis 發布成功才保存 Closed；發布失敗保留 PendingClose，下一輪必須重新確認同場 CLOSE 才重試。不新增 outbox；發布成功但 DB 保存失敗仍可能重投，沿用 Notifier 既有去重與 checkpoint。開台的 DB→Redis 中斷漏送風險維持不變。
- 回歸測試使用使用者回報的頻道、時間與持久化鍵，經 HTTP stub 解析後走實際觀察轉換，覆蓋延遲邊界、還原待確認狀態、播放器 STARTED 不影響 CLOSE、通知 DTO／去重鍵、未知與無效資料、恢復與換場，以及發布失敗重試。
- 驗證：修正前回歸測試重現同場 CLOSE 被 Ignore；修正後完整 solution Release build 為 0 警告／0 錯誤，Release test 為 697 passed／38 skipped／0 failed。測試程序清空 MySQL／Redis component 連線環境變數以避免碰觸外部服務；`git diff --check` 通過，僅有 Git 換行轉換提示。
- 未執行部署、正式 DB 修改或 Discord 傳訊。真實 API 轉換、DB／Redis／多 shard／Discord 端到端行為仍需部署後驗證；離線測試不代表外部流程已驗收。
