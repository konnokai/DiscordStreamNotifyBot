# DiscordStreamNotifyBot.Notifier

## 專案職責

三層架構的**通知層**（exe，`AssemblyName = DiscordStreamNotifyBot`，`net8.0`）。負責 Discord 連線、雙指令系統（Slash + 前綴）、以及**消費匯流排重建 embed 發送**。每個 Notifier 是一個 shard：消費自己的 `notify.shard.{id}`，依 shard 守衛只發給自己持有的伺服器。會限（YoutubeMemberService）**不走匯流排**，按 shard 分區由各 Notifier 自行檢查。

> **需搭配 RabbitMQ + Scraper 才有通知**（指令系統可獨立運作）。啟動參數 `[ShardId, TotalShards]`，預設 `0 1`。

## 相依

- **專案**：僅 `DiscordStreamNotifyBot.Shared`。
- **外部**：Discord gateway、MySQL、Redis、RabbitMQ（消費端）、YouTube/Twitch/TwitCasting API、Uptime Kuma、後端（會限 OAuth）。

## 資料夾與檔案

### 根目錄

| 檔案 | 職責 |
|------|------|
| `Program.cs` | 進入點：寫死 `BotRole.Notifier`，解析 shard 參數，前置檢查（shard 0 播種官方白名單），`bot.StartAndBlockAsync()`。 |
| `Bot.cs` | Discord 連線生命週期、DI 組裝、指令註冊、`Ready/JoinedGuild/LeftGuild` 事件、shard 守衛委派（`ShouldDeleteMissingGuild` → `BotState`）、DB 初始化。 |
| `NotificationBusConsumer.cs` | 匯流排消費端：消費 `notify.shard.{id}`，依 routing key 解析 DTO 並交對應 SharedService 發送。 |
| `UptimeKumaClient.cs` | Uptime Kuma 心跳推送。 |

### `Interaction/` — Slash 指令（主要，前綴 `/`，一般使用者）

`InteractionHandler.cs` 派發、`TopLevelModule.cs` 基底、`Extensions.cs` 共用 helper、`ReactionEventWrapper.cs`、`CommonEqualityComparer.cs`。
子目錄：`Attribute/`（指令屬性 5 個）、`Help/`、`OwnerOnly/`（`SendMsgToAllGuild` 全服廣播）、`Utility/`、`Youtube/`（`Youtube.cs` 為最大模組、`YoutubeChannelSpider.cs`）、`YoutubeMember/`、`Twitch/`、`Twitcasting/`。各平台多為 `指令.cs` + `Spider.cs` 對稱結構。

### `Command/` — 前綴指令（Legacy，前綴 `s!`，擁有者/管理）

與 `Interaction/` 結構對稱：`CommandHandler.cs` 派發、`ICommandService.cs`、`TopLevelModule.cs`、`Extensions.cs`、`ReactionEventWrapper.cs`、`CommonEqualityComparer.cs`。
子目錄：`Attribute/`、`Admin/`（`Administration` + `AdministraitonService`）、`Help/`、`Normal/`、`Youtube/`（`YoutubeStream` / `YoutubeChannelSpider`）、`YoutubeMember/`、`Twitch/`、`TwitCasting/`。

> 兩套指令系統皆透過反射自動 DI 載入（實作 `IInteractionService` / `ICommandService` 即註冊為 Singleton），新增模組不需手動登記。

### `SharedService/` — 通知發送與指令支援（持有 `DiscordSocketClient`）

| 路徑 | 職責 |
|------|------|
| `Youtube/YoutubeStreamService.cs` | 消費 YouTube DTO → 重建 embed → 發送、建立/改活動、換橫幅（`DispatchFromBusAsync` 入口）。 |
| `Youtube/EmbedBuilderFactory.cs` | YouTube 各通知類型 embed 工廠。 |
| `Twitch/TwitchService.cs` + `TwitchEmbedBuilderFactory.cs` | Twitch 通知發送與 embed。 |
| `Twitcasting/TwitcastingService.cs` + `TwitcastingEmbedBuilderFactory.cs` | TwitCasting 通知發送與 embed。 |
| `YoutubeMember/YoutubeMemberService.cs`、`CheckMemberShip.cs`、`CheckMemberShipOnlyVideoId.cs` | 會限身分組檢查（按 shard，不走匯流排）。 |
| `EmojiService.cs` | 應用程式 emoji 載入。 |
| `NoticeCache.cs` | 通知設定唯讀記憶體快取（TTL 30s，§12.3），降 fan-out 下的 MySQL 壓力。 |
| `Cluster/ClusterQueryService.cs` | 跨 shard 查詢（request-reply）與狀態彙總。 |

## 維護注意

- **發送點皆須套 shard 守衛** `Bot.ShouldDeleteMissingGuild`（已套於 6 處刪除點）；新增發送/刪除設定務必沿用，避免多 shard 互刪。
- 匯流排**重投會造成重複通知**（發送路徑無去重），且 dispatch 暫時性失敗會直接進 DLX 不重試——上線前須處理，詳見 [`docs/CODE_REVIEW.md`](../../docs/CODE_REVIEW.md) 🟠-1/🟠-2。
- `CheckMemberShipOnlyVideoId` 為 `async void`（會吞例外），建議改 `Task`（CODE_REVIEW 🟠-4）。
- `YoutubeStreamService.cs` 等檔的 CS0162 unreachable 警告源自 `#if DEBUG return;` 短路，**Release 無此問題**，屬良性（CODE_REVIEW 💡）。
- 指令權威使用說明維護於 Notion，不在此重複（見 CLAUDE.md）。
