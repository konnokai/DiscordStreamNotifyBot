# CHZZK 錄影整合計畫

狀態：待實作。此文件供後續 session 接手，尚未修改程式或資料庫。

## 已確認的需求

- 修改 Bot 與同工作區的 `StreamRecordTools`；不擴充 Backend 或網站管理介面。
- Bot 提供類似 YouTube 的擁有者錄影指令：立即錄影、開啟／關閉頻道自動錄影、列出已開啟自動錄影的頻道。
- 新增 CHZZK 爬蟲後，發給 Bot 擁有者的私訊需要有可用的錄影切換按鈕。
- 自動錄影由 Scraper 判定新場次、發布開台通知的同一條流程委派，不由各 Discord 通知發送流程委派。
- 頻道只新增 `IsRecord` 開關，預設關閉；不新增 `RecordEnabledAtUtc`。
- Streamlink 沿用預設行為，使用 `best`，不另外控制 Time Machine 或回錄起點。
- 不新增全域錄影開關、停止單場錄影、即時狀態查詢、網站錄影設定或自動補送機制。

### 「只錄下一場」的最終語意

使用者已確認不必依真實開台時間嚴格排除當前直播，以爬蟲是否已處理開台為準：

| 情況 | 行為 |
|---|---|
| 已發過開台通知，才開啟自動錄影 | 不補錄；當場需使用立即錄影指令 |
| 已在直播，但爬蟲尚未偵測到開台，此時開啟自動錄影 | 下一輪發開台通知時，一併委派錄影 |
| 同場持續 OPEN、取消 PendingClose、重啟或重新接回既有場次 | 不新增自動錄影請求 |
| 關閉自動錄影 | 不停止已經執行的工作；之後新場次不再自動委派 |
| 當次錄影委派失敗或錄影端沒有上線 | 不自動補送，需使用立即錄影指令重試 |

先前討論中的啟用時間欄位、每輪 OPEN 補送與錄影端恢復後自動補錄，均已取消。不得依舊提案實作。

## 現況與入口

以下是規劃時讀取的程式入口；實作前須重讀目前工作樹與各儲存庫規範，不假設其他 session 沒有修改。

### Bot

- `src/DiscordStreamNotifyBot.Notifier/Command/Youtube/YoutubeStream.cs`：YouTube 前綴錄影指令、擁有者權限及委派回覆模式。
- `src/DiscordStreamNotifyBot.Notifier/SharedService/AdminSettings/CrawlerOwnerNotifier.cs`：爬蟲新增後私訊擁有者。CHZZK 分支目前沒有按鈕。
- `src/DiscordStreamNotifyBot.Notifier/Interaction/SpiderManagementComponent.cs`：既有 YouTube、Twitch、TwitCasting 管理按鈕與執行期擁有者檢查。
- `src/DiscordStreamNotifyBot.Scraper/Detection/Chzzk/ChzzkDetectionService.cs`：新場次、既有場次接回與開關台通知流程。
- `ChzzkSpider`：目前沒有錄影設定；比照 Twitch／TwitCasting 使用爬蟲本身的 `IsRecord`，不另建錄影頻道表。
- `ChzzkUrlParser`、`ChzzkUrls`、`ChzzkClient`：沿用網址解析、頻道網址及直播狀態查詢。
- `ChzzkStreamIdentity`：既有場次鍵為 `channelId:yyyyMMdd_HHmmss`，依平台開台時間建立；不可變更既有識別規則。
- `RedisChannels`、`RedisContractTests`：新增跨專案 channel 常數與契約測試。

### StreamRecordTools

- `StreamRecordTools/Program.cs`：CLI verb 與分派。
- `StreamRecordTools/Command/Subscribe.cs`：Redis 訂閱及 Docker、Linux 非 Docker、Windows 派工。
- `StreamRecordTools/Command/Record/Twitcasting.cs`：最接近 CHZZK 的 Streamlink 錄影流程。
- `StreamRecordTools/ToolConfig.cs`、`.env_sample`、`docker-compose.yml`：錄影路徑與掛載。
- 現有 Twitch／TwitCasting 流程沒有可靠的錄影執行互斥；不可把容器名稱碰撞視為完整去重。
- 現有 TwitCasting 流程沒有完整檢查 Streamlink 退出碼；新增 CHZZK 時不照抄這項缺口，也不順便重構其他平台。

## Bot 實作

### 指令

沿用 YouTube 的 `s!` 前綴、私訊與 Bot 擁有者權限。以下名稱為建議，實作時確認不與現有名稱及別名衝突：

| 建議指令 | 行為 |
|---|---|
| `s!ChzzkRecord <頻道網址或 ID>` | 查詢目前直播，確認 OPEN 且可建立場次鍵後，委派單次錄影 |
| `s!ChzzkAutoRecord <頻道網址或 ID> <on/off>` | 明確設定既有爬蟲的 `IsRecord` |
| `s!ChzzkRecordList` | 列出 `IsRecord = true` 的頻道名稱及 ID／連結 |

- 使用既有 `ChzzkUrlParser`，不另寫網址解析。
- 自動錄影設定要求爬蟲存在；不要隱含新增爬蟲或改變其 guild 歸屬。
- 立即錄影不要求已有爬蟲，也不改變自動錄影設定。
- 立即錄影遇到離線、未知狀態、API 失敗或無法建立場次鍵時，不發送請求，提供可理解的錯誤。
- Redis 發布沒有訂閱者時，明確回覆無法委派；有訂閱者僅回覆「已送出錄影請求」，不可宣稱正在錄影。
- 列表表示自動錄影設定，不表示目前有錄影程序執行。

### 爬蟲新增通知按鈕

- 在 `CrawlerOwnerNotifier` 的 CHZZK 分支新增「錄影頻道」欄位及切換按鈕；YouTube 分支同步改為切換按鈕，不再使用加入／移除按鈕。
- 明確說明：已處理開台的場次不會補錄，需使用立即錄影指令。
- custom ID 使用 `spider_chzzk:record:{channelId}`（切換語意，對齊 Twitch／TwitCasting；使用者已於 2026-09-18 決議覆蓋原「設定目標值」提案）；YouTube 沿用 `spider_youtube:trusted`／`spider_youtube:record` 並改為切換。
- 操作成功後更新訊息上的狀態及對應按鈕。
- 每次操作重新檢查 Bot 擁有者權限與爬蟲是否存在；舊訊息不得繞過權限或重建已刪除爬蟲。
- 指令與按鈕共用最小必要設定邏輯，避免兩邊規則分歧。
- 新增通知由共用服務發送，因此 Slash 與網站新增爬蟲都應收到按鈕，不需變更網站 JSON 契約。

### 資料庫與自動委派

- `ChzzkSpider` 新增 `IsRecord`，預設 `false`。
- 在 Bot 儲存庫建立 migration，同步 EF snapshot 與 `migrate_sql/all.sql`；不套用正式資料庫。
- 僅在建立新場次、發布 StartStream 通知的流程檢查 `IsRecord` 並發布錄影請求。
- 不放入 Notifier 的各 guild 通知處理，避免同頻道多個通知訂閱造成多份錄影。
- 不放入同場更新、既有場次接回或關台流程；不新增輪詢補送。
- 錄影發布失敗須獨立處理，不能阻擋原本的開台通知；通知發布失敗也不應阻止已符合條件的當次錄影嘗試。兩個副作用獨立記錄錯誤，不改寫既有通知重送政策。
- 不新增錄影啟用時間、錄影委派資料表、常態重試排程或自動委派成功標記。
- 不變更現有開關台狀態機、三分鐘關台確認或場次鍵格式。

## Redis 契約建議

新增 Literal channel `chzzk.record`，不修改其他平台既有契約。Bot 與工具須在同一輪實作中對齊。

建議 payload：

```json
{
  "channelId": "4de764d9dad3b25602284be6db3ac647",
  "streamKey": "4de764d9dad3b25602284be6db3ac647:20260918_120000"
}
```

此時間為格式示例，不代表使用者提供的頻道實際開台時間。場次鍵必須由 CHZZK API 開台資訊及既有轉換規則建立。

- 手動與自動錄影共用同一種 payload。
- 使用 Newtonsoft.Json 與既有序列化慣例。
- 工具端驗證 payload、頻道 ID、場次鍵及兩者一致性；不接受任意 URL、命令或路徑。
- 傳入場次鍵供檔名與日誌追蹤；Streamlink 使用的是頻道直播 URL，不保證 API 查詢與實際啟動之間不會換場。
- Pub/Sub 的訂閱者數不是 worker 確認，也不代表 Streamlink 已啟動。首版接受此限制，不新增 ACK、結果事件或持久化工作佇列。

### 同時執行防護

建議由 CHZZK 錄影 worker 在啟動 Streamlink 前取得頻道層級的 Redis 執行鎖，避免立即錄影與自動請求同時錄製同一頻道。

- 鎖須有擁有者識別、到期與續租；只可釋放自身持有的鎖。
- 正常結束釋放；崩潰後可到期恢復，不能永久卡住。
- 續租失敗後不可任由舊程序無限繼續錄影，需處理失去鎖時的程序終止與檔案保留。
- 不任意指定租期或重試次數；依實際執行與現有 Redis 使用方式決定並說明依據。
- 此為執行互斥，不是自動補送或永久禁止同場重錄。工作結束後，立即錄影仍可重新委派。
- 若 CLI 沿用 `--disable-redis`，需明確記載該模式不提供跨程序互斥。

## StreamRecordTools 實作

### 派工與 CLI

- 新增 `chzzk_once` verb，接收頻道 ID 與場次鍵，沿用 output、temp-path、disable-redis 等既有共通選項。
- `Subscribe.cs` 訂閱 `chzzk.record` 並建立 CHZZK 工作。
- 同步處理既有 Docker、Linux 非 Docker 與 Windows 啟動路徑，不只實作 Docker。
- 新增 `Command/Record/Chzzk.cs`，參考 TwitCasting，不抽出跨平台通用錄影框架。
- 增加 `ChzzkRecordPath`、設定範例與必要的 Compose 掛載；確認父容器傳給子容器的是正確主機路徑。
- 容器名稱與 label 標示 CHZZK、頻道及場次。

### 錄影與收尾

使用者已在本機成功執行：

```text
streamlink https://chzzk.naver.com/live/4de764d9dad3b25602284be6db3ac647 best -o test.ts
```

工具沿用相同行為，以受控輸出路徑取代 `test.ts`。不加入自訂 Time Machine、回錄起點或登入機制。

- 使用程序參數 API 傳值；若既有 tmux 路徑必須經 shell，須處理參數轉義，不能只套用一般字串插值。
- 檔名包含平台、頻道及安全格式的場次時間；`streamKey` 含冒號，不能直接作為 Windows 檔名。
- 重錄不能覆蓋原檔；沿用合適的唯一檔名方式。
- 先寫暫存目錄，結束後移到 CHZZK 保存目錄。
- 檢查程序啟動失敗、退出碼、檔案是否存在及是否為空；不能一律記錄為成功。
- 部分下載與搬檔失敗須保留已下載資料並留下錯誤資訊。
- 在適當的 finally／收尾路徑釋放執行鎖，完成資料保留後再沿用 `streamTools.removeById` 清理容器。
- 不順便修改 YouTube、Twitch 或 TwitCasting 的錄影行為。

## 測試與驗收

### Bot 自動化測試

使用現有測試專案，至少覆蓋以下行為；不為每一個簡單欄位另建框架：

- `IsRecord` 預設關閉；既有資料 migration 後不會自動開始錄影。
- 新場次且開啟設定才委派；關閉時不委派。
- 開台已處理後切換設定、同場 OPEN、接回既有場次、PendingClose 恢復均不補錄。
- 尚未處理開台前啟用，首次偵測可委派，不依啟用時間排除。
- 錄影委派失敗不阻擋通知，不產生下一輪錄影補送。
- 立即錄影不修改設定；API 非 OPEN 或資料不合法時拒絕。
- CHZZK 新增爬蟲通知的欄位、按鈕 ID、明確設定動作及權限防護。
- `chzzk.record` 名稱與兩端 JSON 欄位契約。

執行 Bot 完整 Release build／test，命令以 Bot `AGENTS.md` 為準。未修改 Slash metadata 就不新增 Slash 指令或無關 snapshot 變更。

### 錄影工具驗證

目前工具沒有既有測試專案。補最小可執行檢查，涵蓋新增的輸入解析、程序參數與失敗收尾；需要 Redis 的互斥行為使用隔離環境驗證。

- 手動與自動請求同時抵達時，同頻道只執行一份 Streamlink。
- 正常結束、程序失敗、鎖失效與搬檔失敗都保留正確檔案並釋放自身資源。
- 失敗後可再次使用立即錄影，不覆蓋前次檔案。
- CLI 與訂閱派工使用同一套實際錄影流程。

### 整合驗收

- 使用隔離 Redis 驗證 Bot payload 到工具工作啟動，不連正式 Redis 做測試。
- 驗證正式部署映像內的 Streamlink 版本與 CHZZK plugin；本機成功不等於映像版本已確認。
- 實際直播錄製與 Discord 互動需另取得適當環境及授權，確認檔案可播放、錄製起點與收尾行為。
- Time Machine log 只證明 plugin 使用相關來源，不代表一定從開台起點錄製，也不保證完整全場。
- 成人／登入限制頻道、跨場競態及平台 API／plugin 變動，首版不宣稱已支援或消除；實測結果另記。

## 交付與部署順序

1. 重讀兩個儲存庫的規範、相關程式與 git status，保留其他 session 的變更。
2. 對齊 Redis payload、CLI 與同時執行防護，先完成錄影工具及可執行檢查。
3. 完成 Bot migration、指令、按鈕與新場次自動委派，再跑受影響測試及完整建置。
4. 更新兩端 README 的新設定／指令／契約，以及 `docs/CHZZK_NOTIFICATION_PLAN.md` 原本「不含錄影」的範圍說明。
5. 檢查兩個儲存庫 diff 與 whitespace，回報尚未實測的外部整合項目。
6. 部署時先讓錄影端支援新契約，再於套用 migration 後部署 Bot；所有頻道初始自動錄影維持關閉。

本計畫不授權部署、正式 migration、啟動實際錄影或發送 Discord 訊息。不要自動提交或更新 graphify；程式實作完成後提醒使用者自行更新各儲存庫圖譜。
