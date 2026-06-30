# DiscordStreamNotifyBot.Coordinator

## 專案職責

三層架構的**主控／觀察層**（exe，`net8.0`）。最輕量的角色：向 Redis 公告 `TOTAL_SHARDS`、定期重新公告、並監控各角色心跳（scraper leader 與 notifier shard 存活數），在存活 notifier 少於 `TOTAL_SHARDS` 時輸出提示。**不偵測、不發送、不連 Discord gateway**。

## 相依

- **專案**：僅 `DiscordStreamNotifyBot.Shared`。
- **外部**：Redis（公告 `TOTAL_SHARDS`、讀心跳鍵）。

## 資料夾與檔案

| 檔案 | 職責 |
|------|------|
| `Program.cs` | 進入點：寫死 `BotRole.Coordinator`，前置檢查後執行 `CoordinatorService`。 |
| `CoordinatorService.cs` | 啟動即 `AnnounceTotalShardsAsync(config.TotalShards)`，之後迴圈定期重新公告並彙整心跳，回報 `notifier存活=N/TOTAL_SHARDS` 與 leader 狀態。 |

## 維護注意

- `TOTAL_SHARDS` 以本服務公告為單一真相來源；Notifier 的 `ClusterQueryService` 以此為準、fallback 本機設定。
- 變更 shard 數屬規劃性維運（需協調式重部署，計畫已記錄）。
- 詳細審閱見 [`docs/CODE_REVIEW.md`](../../docs/CODE_REVIEW.md)。
