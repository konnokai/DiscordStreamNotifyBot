# DiscordStreamNotifyBot.Scraper

## 專案職責

三層架構的**爬蟲／偵測層**（exe，`net8.0`）。叢集中的**唯一偵測者**：以 leader 鎖保證單例，啟動 `Detection/` 下的輪詢 Timer、錄影 Redis 訂閱、PubSub/EventSub/WebHook 維護，偵測到開台/結束/改時間等事件後 **publish DTO 至 RabbitMQ `bot.notify`**，由 Notifier 消費發送。

> **完全不建立 `DiscordSocketClient`**：偵測路徑一律走匯流排，不觸碰 Discord gateway。發送、建立活動、換橫幅、會限身分組等需 Discord 的動作全在 Notifier。

## 相依

- **專案**：僅 `DiscordStreamNotifyBot.Shared`。
- **外部**：MySQL、Redis（leader 鎖/心跳 + 與 [YoutubeStreamRecord](https://github.com/konnokai/YoutubeStreamRecord) 的 IPC）、RabbitMQ（發布端）、YouTube/Twitch/TwitCasting API、後端 webhook（`Discord-Stream-Bot-Backend`）。

## 資料夾與檔案

### 根目錄

| 檔案 | 職責 |
|------|------|
| `Program.cs` | 進入點：寫死 `BotRole.Scraper`，前置檢查後執行 `ScraperService`。 |
| `ScraperService.cs` | 核心生命週期：取得 scraper leader 鎖 → 啟動 `DetectionHost` → 定期續租＋寫心跳；**失租即 exit(1)** 避免 split-brain（交 compose 重啟）；關閉前保存狀態並釋放鎖。 |
| `DetectionHost.cs` | 偵測宿主：設 `BotState.IsDetectionHost = true`，組裝 DI 並實體化三個偵測服務（建構子內即啟動 Timer/訂閱），不建立 Discord 連線。 |

### `Detection/` — 偵測服務（只有 Timer + Redis 訂閱 + publish DTO）

| 路徑 | 職責 |
|------|------|
| `Youtube/YoutubeDetectionService.cs` | YouTube 偵測核心（`partial`）：開台/結束/會限/429 等 Redis 訂閱與狀態維護。 |
| `Youtube/YoutubeDetectionService.Schedule.cs` | 排程爬取（Holo/彩虹社/其他頻道輪詢、PubSub 註冊）。 |
| `Youtube/YoutubeDetectionService.Reminder.cs` | 到點提醒（排程開台前/改時間）。 |
| `Twitch/TwitchDetectionService.cs` | Twitch 開台/離線偵測（輪詢 + EventSub）。 |
| `Twitch/Debounce/DebounceChannelUpdateMessage.cs` | Twitch `channel_update` 去抖動。 |
| `Twitcasting/TwitcastingDetectionService.cs` | TwitCasting 偵測（PubSub/WebHook）。 |
| `Twitcasting/TwitCastingWebHookJson.cs` | TwitCasting webhook 載荷模型。 |

## 維護注意

- 偵測服務**只依賴 Shared**（`*ApiService` / DB / `NotificationBusPublisher` / `RedisChannels`），新增偵測請勿引用任何 Discord gateway 型別。
- leader TTL 刻意設為續租間隔的 ≥3 倍，避免 GC 暫停誤失鎖（`ScraperService.cs`）。
- 待辦：多程序實測（計畫 §6.2）尚未完成；匯流排 at-least-once 去重未實作（影響重複通知，見 [`docs/CODE_REVIEW.md`](../../docs/CODE_REVIEW.md) 🟠-1/🟠-2）。
- `find_dead_code` 標記 `YoutubeDetectionService.cs` 的 `GetOrCreateNijisanjiLiverListAsync` 零引用，確認後可移除（CODE_REVIEW 💡）。
