# 自動化測試導入計畫

> 狀態：**第五批進行中**。Redis／MySQL component tests 已實跑通過；多 shard guild ownership 與外部 API request contract 待完成。
>
> 原則：單元測試負責提早攔截可重現的程式與契約錯誤；便宜的啟動 fail-fast 仍保留，外部服務真實行為則由 component／integration test 驗證。

## 1. 目標

1. 讓不需 Discord、MySQL、Redis 或外部 API 的邏輯能在本機與 CI 快速驗證。
2. 固定跨專案契約，包括 Auth token、Redis channel/key、NotificationBus DTO 與 Slash command metadata。
3. 把時間、狀態機與資料轉換邏輯從外部副作用中拆出，降低 Scraper／Notifier 回歸風險。
4. 保留必要的啟動檢查，避免未經測試或資源不完整的部署產物進入服務狀態。
5. 明確區分 unit、component、integration 與手動 Discord 驗證，避免大量 mock 產生虛假的安全感。

## 2. 測試分類

| 類型 | 範圍 | 外部依賴 | 預設執行 |
|---|---|---|---|
| Unit | 純函式、formatter、parser、decision、contract | 無 | `dotnet test DiscordStreamNotifyBot.sln -c Release` |
| Component | 單一基礎設施元件與真實 client/provider | Redis／MySQL container 擇一 | 後續獨立 category |
| Integration | 多程序、Redis Streams、DB、外部 API sandbox | 多個真實依賴 | 部署前或排程執行 |
| Manual Discord | 指令註冊、權限、interaction lifecycle、UI 顯示 | Discord 測試 guild | 發版前清單 |

## 3. 不移除的啟動檢查

- `InteractionHandler.ValidateCommandLocalizationResources()`：保留 metadata／資源 fail-fast，單元測試另行提早攔截。
- `BotLocalizer.ValidateResources()`：保留三語 key 與 placeholder fail-fast。
- `StartupPreflight.EnsureAsync()`：保留 MySQL／Redis 可連線檢查。
- `ProviderTokenEncryptionKey`：Notifier 啟動時驗證部署 secret 已提供且至少 64 字元。
- `NotificationBus.EnsureConsumerGroupAsync()`：保留 Redis Streams consumer group 建立。
- Discord login、Slash command registration 與 NotificationBus consumer 啟動失敗仍應直接終止 Notifier。

## 4. 第一批：低耦合契約與格式化

完成條件：不連外部服務；整個 solution Release build 0 error；Release test 全數通過。

- [x] 建立 `tests/DiscordStreamNotifyBot.Tests` 並加入 solution。
- [x] InteractionCommands 三語 description／canonical metadata 啟動契約。
- [x] `SupportedLocale` 與 `LocaleResolver` normalize／fallback precedence。
- [x] `BotLocalizer` 的 BotMessages 三語 key、placeholder、format 與 fallback。
- [x] `TokenCrypto`／`TokenManager` AES、HMAC、round-trip 與竄改偵測。
- [x] Notification DTO JSON 欄位、enum 與預設值契約。
- [x] `RedisChannels`、`NotificationBus` 常數、動態 key 與 `TryGetPayload`。
- [x] `BotState` shard 歸屬與 missing guild 刪除守衛。
- [x] YouTube／Twitch／TwitCasting Embed factory 的顏色、欄位、URL、時間與三語輸出。
- [x] Slash command signature、global/debug module 邊界、參數型別、required 與 choice value contract。

## 5. 第二批：小幅抽出純邏輯

完成條件：production 改動限於 internal pure helper 或可注入 seam；不改外部行為。

- [x] 將 `NotificationBusConsumer.TryGetDedupKey` 抽成 internal policy，測 shard 隔離與各 DTO 主鍵。
- [x] 將 interaction error/precondition code 對應抽成 resource descriptor。
- [x] 將 YouTube video URL／ID 解析抽成獨立 parser。
- [x] 將 autocomplete 排序、搜尋、去重與 25 筆限制抽成共用 helper。
- [x] 將會限影片 log message code formatting 抽成純 formatter。
- [x] 將無通知 guild 篩選拆成輸入集合／輸出集合的 pure overload。
- [x] 建立可讀的 Slash command contract snapshot，避免只靠 opaque hash。

## 6. 第三批：時間與快取

完成條件：使用 `TimeProvider` 或明確的 clock／delay abstraction；測試不得使用秒級真實等待。

- [x] `StartupPreflight` 指數退避、30 秒上限與 timeout。
- [x] `PeriodicRunner` 首次執行、取消、例外與不重入。
- [x] `NoticeCache` TTL、invalidate 與 single-load。
- [x] `GuildLocaleService` cache、single-flight、batch 與 invalidate。
- [x] Twitch channel update debounce quiet window、timeout、取消與 dispose。
- [x] YouTube reminder 14 天範圍、改時與 timer replacement decision。

## 7. 第四批：Scraper 狀態機

完成條件：狀態判斷輸入為 immutable facts，輸出為明確 action；API、Redis、DB 只留在 orchestration adapter。

- [x] Twitch OAuth／EventSub reconcile decision。
- [x] Twitch guild eligibility 二階段缺失確認。
- [x] Twitch 授權失效後最終刪除防線。
- [x] Twitch 開台去重、Start DTO 與關台 debounce／恢復直播 decision。
- [x] Twitch channel update diff、聚合與 legacy formatting。
- [x] YouTube API Video 分類：new、started、scheduled、active-chat-only、ignore fake post。
- [x] YouTube reminder 與 API batch reconciliation。
- [x] YouTube endstream／memberonly／delete／unarchived 跨來源原子去重。
- [x] YouTube 會限影片候選分類與 manual pin 保護。
- [x] TwitCasting webhook parser、stream mapping、notification DTO 與 recording decision。
- [x] TwitCasting webhook 註冊差集 planner。

## 8. 第五批：Component 與 Integration

完成條件：測試 category 與一般 unit suite 分離；本機沒有 container 時可明確略過，不得靜默通過。

- [x] Redis Streams：XGROUP、XACK、PEL、XAUTOCLAIM、MKSTREAM 與 consumer restart。
- [x] Redis leader lock／TTL／Lua owner check 與 shard lease。
- [x] Twitch OAuth refresh lock `SET NX`、TTL 與 Lua owner-only release。
- [x] MySQL：migration model、唯一鍵、concurrency、四影片表查詢與 token store。
- [x] NotificationBus Scraper publish → Notifier consume／dedup／ack。
- [ ] 多 shard guild ownership 與不互刪設定。
- [ ] 外部 API request contract：YouTube、Twitch EventSub、TwitCasting、Discord webhook。
- [x] Twitch 訂閱 Helix 200/401/404/429/5xx、Tier/gift、refresh 表單與共用 token JSON 契約。
- [x] Twitch 訂閱設定 deletion-pending／shared-role 修復 policy、refresh rotation graceful-shutdown drain lifecycle 與 migration 欄位 assertion。
- [x] YouTube 會員 Slash/component contract、queued/verified/removal-pending state、跨平台 role ownership、provider 分類、lifecycle drain 與 migration constraint assertion。

### Component tests 執行方式

Component tests 只讀測試專用環境變數，不會 fallback 到 production 設定。未設定時會顯示明確的 `Skipped` 原因；設定後連線或 migration 失敗則視為測試失敗。

```powershell
docker compose -f tests/docker-compose.component.yml up -d

$env:REDIS_COMPONENT_OPTION = "127.0.0.1:6380,defaultDatabase=15,abortConnect=true,connectTimeout=2000,syncTimeout=5000"
$env:MYSQL_TEST_CONNECTION_STRING = "Server=127.0.0.1;Port=3307;User ID=root;Password=component-root;SslMode=None"

dotnet test tests/DiscordStreamNotifyBot.Tests/DiscordStreamNotifyBot.Tests.csproj -c Release --filter "Category=RedisComponent"
dotnet test tests/DiscordStreamNotifyBot.Tests/DiscordStreamNotifyBot.Tests.csproj -c Release --filter "Category=MySqlComponent"

docker compose -f tests/docker-compose.component.yml down -v
```

Redis component tests 強制使用明確指定且編號至少為 2 的空白 DB，避免碰觸 production DB 0 與其他既有用途。

額外恢復與競爭案例已覆蓋：XAUTOCLAIM cursor 必須越過 poison head、dedup marker 保留時間需長於 reclaim 門檻、租約到期接手／併發搶 shard，以及同一使用者 token 首次併發寫入。

## 9. 手動 Discord 驗證

- [ ] 三個 client locale 顯示英文 canonical command name 與對應語言 description。
- [ ] global 與 Debug guild command registration 差異符合預期。
- [ ] guild／user／bot permission、channel overwrite 與 owner-only precondition。
- [ ] Respond／Followup／ephemeral／Modal／autocomplete lifecycle。
- [ ] 文字／公告頻道通知、crosspost、排程活動與缺權限錯誤。
- [ ] YouTube／Twitch／TwitCasting／會限通知在三語 guild 的實際輸出。

## 10. 測試實作規則

- 純邏輯優先抽成 internal helper，透過 `InternalsVisibleTo` 測試；不以 reflection 呼叫 private method。
- 只有 interface boundary 或 interaction routing 需要 mock；具體狀態判斷優先改成 pure decision，不 mock 整個 service。
- 涉及 static state、環境變數或 process culture 的測試必須保存並還原狀態，必要時關閉該 collection 平行執行。
- JSON contract 使用固定 JObject／fixture 逐欄斷言，不只做同型別 round-trip。
- 時間測試使用固定 UTC `DateTimeOffset`／`TimeProvider`，不得依賴執行機時區或真實等待。
- 跨 repo 的字串、enum value、payload 欄位與 token 格式可使用明確 golden contract；一般內部實作不建立脆弱 snapshot。
- 每批完成後更新本文件 checkbox，並執行 `dotnet build DiscordStreamNotifyBot.sln -c Release` 與 `dotnet test DiscordStreamNotifyBot.sln -c Release`。
