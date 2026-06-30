---
name: debug-detection-bus
description: >
  追蹤「直播小幫手」一則直播通知從偵測到送進 Discord 的完整路徑，並定位「偵測到了卻沒發通知 / 漏發 / 重複發」
  這類問題。涵蓋 Scraper 偵測 → NotificationBusPublisher → RabbitMQ bot.notify(topic) → notify.shard.{id} 佇列
  → NotificationBusConsumer → DispatchFromBusAsync → GetGuild shard 守衛 這條鏈，以及 Redis IPC 與匯流排的差別。
  只要使用者說「沒收到通知」「通知沒發出去」「某個伺服器收不到開台」「事件有偵測到但 Discord 沒動靜」
  「routing key」「notify.shard」「為什麼重複通知」，就用這個 skill，即使他沒指名是哪一層出問題。
---

# 偵測 → 匯流排 → 發送 路徑除錯

## 完整路徑（先在腦中對齊這條鏈）

```
[Scraper 程序]  Detection/Xxx 偵測到事件
   → NotificationBusPublisher.PublishJsonAsync(config, NotifyRoutingKeys.Xxx, dto)
   → RabbitMQ exchange "bot.notify" (topic)  ──routing key=Xxx──▶  佇列 notify.shard.{id}（每個 Notifier shard 一條）
[Notifier shard]  NotificationBusConsumer.HandleMessageAsync(routingKey, body)
   → switch(routingKey) → XxxService.DispatchFromBusAsync(dto)
   → SendStreamMessageAsync → 逐伺服器 GetGuild(guildId) → 送 embed
```

兩個獨立通道，別搞混：
- **RabbitMQ 匯流排**＝偵測→發送的內部通知（YouTube/Twitch/Twitcasting/Banner）。
- **Redis pub/sub**＝和外部錄影工具 / 後端的 IPC（`youtube.startstream`、`youtube.record`、`twitch.record`、
  `youtube.pubsub.*` 等，見 `Shared/RedisChannels.cs`）。錄影工具偵測到開台是走 Redis 進來，再由 Scraper 轉成匯流排 DTO。

## 「沒收到通知」依序排查

由便宜到貴，照順序刷掉：

1. **Scraper 有在跑且是 leader 嗎？** 偵測只在 Scraper 程序執行（`BotState.IsDetectionHost`），且 leader 鎖單例。
   啟動 log 應有 `[Scraper] 偵測服務已啟動（YouTube / Twitch / Twitcasting）`。沒有 Scraper＝完全不會有通知。
2. **是 Debug build 嗎？** 多個 publish/send 方法有 `#if DEBUG return;` 短路。驗證通知一律用 Release。
3. **RabbitMQ 起來了嗎？** Notifier 的通知**必須** RabbitMQ + Scraper 同時在。consumer 啟動 log：
   `[NotificationBus] 已開始消費 notify.shard.{id}`。沒這行＝consumer 沒連上。
4. **routing key 接線了嗎？** 看 `NotificationBusConsumer.HandleMessageAsync` 的 switch 有沒有對應 `case`。
   未接線會印 `尚未接線的 routing key: {key}，暫時 ack 略過`——事件被丟棄但不報錯，最容易漏。
5. **是 shard 守衛擋掉的嗎？**（漏發給特定伺服器時最常見）發送端 `GetGuild(guildId) == null` 會**靜默 continue**，
   因為該伺服器不在這個 shard。確認那個 guild 該由哪個 shard 持有（`guildId % TOTAL_SHARDS`），去看那個 shard 的 log。
6. **通知設定為空？** 發送端從 `NoticeCache`（§12.3）取該頻道的通知清單，清單空＝沒有發送目標。查 DB 對應的
   `NoticeXxxStreamChannel` 表有沒有該頻道 + 該伺服器的列。
7. **DTO 反序列化失敗？** consumer 對 null DTO 直接 `return true`（ack 丟棄，避免卡佇列），不會重試。
   若懷疑壞訊息，在 `HandleMessageAsync` 反序列化後加 log 看 json 內容。

## 「重複通知」排查
- 偵測端通常有去重旗標/集合（如 YouTube 的 `_endLiveBag`、`addNewStreamVideo`、`newStreamList`，
  Twitcasting 的 `TwitcastingStreams` 既有列檢查）。確認去重的 key 與時機正確。
- RabbitMQ 重投：consumer 回傳 `true` 才 ack；若 handler 丟例外未被吞，訊息會重投造成重複。確認 handler 內例外有被接住。

## 想確認訊息真的有進匯流排
- 偵測端在 `PublishJsonAsync` 前後加暫時 log，或在 consumer `HandleMessageAsync` 入口 log routingKey。
- 直接看 RabbitMQ management UI 的 `bot.notify` exchange 與 `notify.shard.{id}` 佇列流量。

## 關鍵檔
- `Shared/NotificationBusPublisher.cs`、`Shared/Messages/Notifications.cs`（DTO + `NotifyRoutingKeys`）
- `Notifier/NotificationBusConsumer.cs`（switch 接線處）
- `Scraper/DetectionHost.cs`（偵測服務啟動）、`Scraper/Detection/*/`（各平台偵測）
- `Notifier/SharedService/*/`（各平台發送 + shard 守衛 `Bot.ShouldDeleteMissingGuild`）
- `Shared/RedisChannels.cs`（Redis IPC 頻道清單）
