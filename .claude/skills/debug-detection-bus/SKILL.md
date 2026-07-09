---
name: debug-detection-bus
description: >
  追蹤「直播小幫手」一則直播通知從偵測到送進 Discord 的完整路徑，並定位「偵測到了卻沒發通知 / 漏發 / 重複發」
  這類問題。涵蓋 Scraper 偵測 → NotificationBus.PublishAsync → Redis Stream bot:notify → consumer group shard-{id}
  → NotificationBusConsumer → DispatchAsync → GetGuild shard 守衛 這條鏈，以及 Redis IPC 與匯流排的差別。
  只要使用者說「沒收到通知」「通知沒發出去」「某個伺服器收不到開台」「事件有偵測到但 Discord 沒動靜」
  「NotifyType」「pending 堆積」「為什麼重複通知」，就用這個 skill，即使他沒指名是哪一層出問題。
---

# 偵測 → 匯流排 → 發送 路徑除錯

## 完整路徑（先在腦中對齊這條鏈）

```
[Scraper 程序]  Detection/Xxx 偵測到事件
   → NotificationBus.PublishAsync(Bot.RedisDb, NotifyType.Xxx, dto)   // XADD bot:notify（MAXLEN ~ 修剪）
[Redis Stream]  bot:notify  ──每個 shard 一個 consumer group shard-{id}（各 group 收到全部訊息＝廣播）──▶
[Notifier shard]  NotificationBusConsumer  StreamReadGroup(">") 短輪詢讀新訊息（1~2s；StackExchange.Redis 不支援 BLOCK）
   → ProcessEntryAsync → DispatchAsync(type, json) → switch(type) → XxxService.DispatchFromBusAsync(dto)
   → 逐伺服器 GetGuild(guildId) → 送 embed → 成功才 XACK
```

兩個獨立通道，別搞混：
- **Redis Streams 匯流排**（`bot:notify`）＝偵測→發送的內部通知（YouTube/Twitch/Twitcasting/Banner）。
- **Redis pub/sub**＝和外部錄影工具 / 後端的 IPC（`youtube.startstream`、`youtube.record`、`twitch.record`、`twitcasting.record`、
  `youtube.pubsub.*` 等，見 `Shared/RedisChannels.cs`）。錄影工具偵測到開台是走 Redis pub/sub 進來，再由 Scraper 轉成匯流排 DTO。

## 「沒收到通知」依序排查

由便宜到貴，照順序刷掉：

1. **Scraper 有在跑且是 leader 嗎？** 偵測只在 Scraper 程序執行（`BotState.IsDetectionHost`），且 leader 鎖單例。
   啟動 log 應有 `[Scraper] 偵測服務已啟動（YouTube / Twitch / Twitcasting）...`。沒有 Scraper＝完全不會有通知。
2. **是 Debug build 嗎？** 多個 publish/send 方法有 `#if DEBUG return;` 短路。驗證通知一律用 Release。
3. **Notifier consumer 起來了嗎？** 啟動 log：`[NotificationBus] 已開始消費 bot:notify（group shard-{id}）`。
   沒這行＝consumer 沒起。匯流排就在既有 Redis 上，不需額外服務。
4. **type 接線了嗎？** 看 `NotificationBusConsumer.DispatchAsync` 的 switch 有沒有對應 `case NotifyType.Xxx`。
   未接線會印 `尚未接線的 type: {type}，暫時 ack 略過`——事件被丟棄但不報錯，最容易漏。
5. **是 shard 守衛擋掉的嗎？**（漏發給特定伺服器時最常見）發送端 `GetGuild(guildId) == null` 會**靜默 continue**，
   因為該伺服器不在這個 shard。確認那個 guild 該由哪個 shard 持有——Discord 公式
   `(guildId >> 22) % totalShards == shardId`（`BotState.IsServerOnThisShard`），去看那個 shard 的 log。
6. **通知設定為空？** 發送端從 `NoticeCache` 取該頻道的通知清單，清單空＝沒有發送目標。查 DB 對應的
   `NoticeXxxStreamChannel` 表有沒有該頻道 + 該伺服器的列。
7. **DTO 反序列化失敗？** consumer 對缺 type/payload 的壞訊息直接 `XACK` 丟棄（`壞訊息（缺 type/payload），丟棄`），
   反序列化為 null 也不會呼叫發送。若懷疑壞訊息，在 `ProcessEntryAsync` 加 log 看 json 內容。

## 「重複通知」排查
- **匯流排本質是 at-least-once**：發送成功後寫短期去重鍵 `notified:{shardId}:{...}`（`NotificationBusConsumer.TryGetDedupKey`
  依 shardId + DTO 主鍵 + 類型組鍵），XAUTOCLAIM 重投時去重鍵已存在＝直接 ack 略過。確認去重鍵組法涵蓋該事件。
- **去重鍵漏帶 shardId ⇒ 「完全沒發、無 log」**：`bot:notify` 是廣播，每個 shard 都會讀到同一則；去重鍵若不分 shard，
  先處理的 shard 設鍵後其餘 shard 會在 `DispatchAsync` 前就 ack 略過（連 `發送 XXX 通知` log 都不會印），
  其伺服器永遠收不到。症狀：兩個 group 的 last-delivered-id 都已到最新（代表有讀到），但沒有任何發送 log。
- 偵測端通常也有去重旗標/集合（如 YouTube 的 `_endLiveBag`、`newStreamList`，Twitcasting 既有列檢查）。確認去重的 key 與時機正確。
- **未 ack 會重投**：`ProcessEntryAsync` 例外時**不 ack**（留在 PEL），交由 XAUTOCLAIM 逾時補救——這是設計，
  但若發送已成功卻在 ack 前丟例外，會靠去重鍵擋掉重複。確認去重鍵有寫入。

## 想確認訊息真的有進匯流排 / 有沒有堆積
- 偵測端在 `PublishAsync` 前後加暫時 log，或在 consumer `ProcessEntryAsync` 入口 log type。
- 用 `redis-cli`：`XLEN bot:notify`、`XINFO GROUPS bot:notify`（看各 group 的 pending 與 consumer 數）、
  `XPENDING bot:notify shard-{id}`。Coordinator 也會定期 log 匯流排 pending 堆積與無 consumer 的殘留 group。

## 關鍵檔
- `Shared/NotificationBus.cs`（XADD/XREADGROUP/XACK/XAUTOCLAIM 封裝）、`Shared/Messages/Notifications.cs`（DTO + `NotifyType`）
- `Notifier/NotificationBusConsumer.cs`（短輪詢迴圈 + `DispatchAsync` switch 接線處 + 去重鍵）
- `Scraper/DetectionHost.cs`（偵測服務啟動）、`Scraper/Detection/*/`（各平台偵測）
- `Notifier/SharedService/*/`（各平台發送 + shard 守衛 `Bot.ShouldDeleteMissingGuild`）
- `Shared/RedisChannels.cs`（Redis IPC 頻道清單）
