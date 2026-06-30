---
name: add-detection-platform
description: >
  在「直播小幫手」加入一個新的直播平台偵測（或為既有平台新增一種通知事件）時的跨專案配方。
  涵蓋 Shared（DTO / routing key / Redis 頻道 / 無狀態 API）→ Scraper/Detection（偵測 → publish DTO）
  → Notifier/SharedService（消費 → 重建 embed → 依 shard 守衛發送）三層該改哪些檔、為什麼這樣分。
  只要使用者說「加一個平台」「新增 Kick/Bilibili/X 偵測」「多一種開台/關台通知」「偵測到事件要發 Discord」
  「事件要過匯流排」，就用這個 skill，即使他沒講到「偵測服務」或「RabbitMQ」這些字。
---

# 新增偵測平台 / 通知事件

這個專案把「偵測」與「發送」拆到不同程序，靠 RabbitMQ 匯流排解耦。理由：偵測者只能有一個
（Scraper，leader 鎖單例），但發送要分散到各 Notifier shard（每個 shard 只持有部分伺服器）。
所以**偵測端不碰 Discord gateway**，只 publish 結構化 DTO；**發送端不做偵測**，只消費 DTO 重建 embed
發給自己持有的伺服器。新增平台就是照這條線把三層補齊。

> 反例提醒：會限（YoutubeMember）**不走匯流排**——它經 shard 守衛天然按 shard 分區，別照這個配方做。

## 動工前先讀一個既有平台

TwitCasting 是最小的端到端範例，先把這三個檔讀過再開始，照著抄最不會漏：
- `src/DiscordStreamNotifyBot.Scraper/Detection/Twitcasting/TwitcastingDetectionService.cs`（偵測 → publish）
- `src/DiscordStreamNotifyBot.Notifier/SharedService/Twitcasting/TwitcastingService.cs`（消費 → 發送）
- `src/DiscordStreamNotifyBot.Notifier/SharedService/Twitcasting/TwitcastingEmbedBuilderFactory.cs`

## 步驟（依相依順序，Shared → Scraper → Notifier）

### 1. Shared — 定義契約
- `Shared/Messages/Notifications.cs`：
  - 新增 `XxxNotification` DTO（純資料，**不要**塞 Embed 物件）。
  - 若事件有多種類型，新增 `XxxNoticeType` 線路列舉（與 UI 用的 `NoticeType` 分開，只當跨層契約）。
  - 在 `NotifyRoutingKeys` 加一個 routing key 常數，例如 `public const string Kick = "kick";`。
- `Shared/RedisChannels.cs`：**只有**和外部錄影工具/後端有 IPC（webhook、錄影請求）時才加頻道常數；
  這些字串是既有契約，新增可以、改既有的不行。
- 若兩端要共用無狀態 API 呼叫，放一個 `XxxApiService` 在 Shared（參考 `YoutubeApiService`）。

### 2. Scraper — 偵測並 publish
- 建 `Scraper/Detection/Xxx/XxxDetectionService.cs`：
  - **只 using Shared**，不得參考 Discord.WebSocket / Notifier。
  - 建構子內啟動偵測：輪詢用 `PeriodicRunner.RunAsync("Xxx-...", dueTime, period, Handler, GracefulShutdown.Token)`
    （PeriodicTimer，無重入、吃 CancellationToken）；事件式用 `Bot.RedisSub.Subscribe(...)`。
  - 偵測到事件 → `NotificationBusPublisher.PublishJsonAsync(_botConfig.RabbitMQ, NotifyRoutingKeys.Xxx, dto)`。
  - 錄影等副作用（Process.Start / Redis publish 給錄影工具）在偵測端完成，結果用 DTO 欄位（如 `IsRecord`）帶過去。
- `Scraper/DetectionHost.cs`：在 `Start()` 的 `ServiceCollection` 加 `.AddSingleton<Detection.Xxx.XxxDetectionService>()`，
  並在下方 `GetRequiredService<...>()` 實體化（建構子即啟動 Timer/訂閱，所以一定要主動取一次）。

### 3. Notifier — 消費並發送
- 建 `Notifier/SharedService/Xxx/XxxService.cs : IInteractionService`：
  - `DispatchFromBusAsync(XxxNotification dto)`：把 DTO 還原成 DB 實體或直接取值 → 用 EmbedBuilderFactory 重建 embed。
  - 發送時**逐一 `GetGuild(item.GuildId)`，null 就 `continue`**（非本 shard 持有），除非 `Bot.ShouldDeleteMissingGuild(guildId)`
    才真的刪設定——這個守衛避免多 shard 互刪彼此的通知設定。
  - 通知設定清單走記憶體快取（參考 `NoticeCache`，§12.3），不要每次打 DB。
  - 因為實作 `IInteractionService`，反射會自動註冊為 Singleton，**不需手動 DI**。
- 建 `Notifier/SharedService/Xxx/XxxEmbedBuilderFactory.cs`（顏色用 `WithOkColor()` / `WithErrorColor()` / `WithRecordColor()`）。
- `Notifier/NotificationBusConsumer.cs`：**這一步要手動**——建構子注入新的 `XxxService`，在 `HandleMessageAsync`
  的 switch 加 `case NotifyRoutingKeys.Xxx:` 反序列化 DTO 後呼叫 `DispatchFromBusAsync`。漏了這步，事件會被
  「尚未接線的 routing key」warn 後直接 ack 丟掉。

## 收尾檢查
- publisher 與 consumer 的 routing key 字串一致（用 `NotifyRoutingKeys` 常數，別寫字面字串）。
- 偵測服務沒有任何 `using Discord*`；發送服務沒有任何 Timer/偵測邏輯。
- 注意 `#if DEBUG`：多個偵測/發送方法在 Debug 會 `return` 短路（不 publish/不送），驗證通知要 Release。
- 全方案建置：`dotnet build DiscordStreamNotifyBot.sln -c Release`。
- 端到端驗證走 [debug-detection-bus](../debug-detection-bus/SKILL.md) 的路徑（需 Scraper + RabbitMQ + Notifier 同時跑）。
