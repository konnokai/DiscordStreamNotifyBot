---
name: ef-migration-baseline
description: >
  在「直播小幫手」做 EF Core schema 變更、新增/套用 migration、或處理「舊的 EnsureCreated 正式庫要基線化」時用。
  涵蓋本專案特有的眉角：EF 工具指向 Shared 專案（已有 MainDbContextFactory 設計階段工廠，免 startup project）、
  連線字串解析順序、啟動時自動套用 migration 與上線前的 SQL 審查、以及一次性基線化腳本 _Baseline_ExistingDb.sql。
  只要使用者說「加欄位/改表」「新增 migration」「update database」「schema 變更」「資料表」「基線化」
  「EnsureCreated 的舊庫」，就用這個 skill。注意：本專案的 dotnet-claude-kit `/migrate` 是引導式變體，
  需要逐步審 SQL 時可改用它。
---

# EF Core 遷移與基線化（本專案版）

## 你必須先知道的三件專案特例

1. **工具指向 Shared，不是 Notifier。** `src/DiscordStreamNotifyBot.Shared/DataBase/MainDbContextFactory.cs`
   是設計階段工廠，所以所有 `dotnet ef` 指令都加 `--project src/DiscordStreamNotifyBot.Shared`，**不需** startup project。
2. **連線字串解析順序**（factory 與啟動時一致）：環境變數 `MYSQL_CONNECTION_STRING`
   → 同目錄 `bot_config.json` 的 `MySqlConnectionString` → localhost 預設值。
   `migrations add`（離線）連不到 DB 也能跑（退回固定 ServerVersion）；`database update` 需連得到。
3. **命名規約是 snake_case**（`UseSnakeCaseNamingConvention`）。產生的 migration / 手寫 SQL 欄位都是 snake_case，
   factory 與 `MainDbService` 的設定必須一致，否則模型會對不上。

## 一般變更流程

```powershell
# 1) 改完 model（DataBase/Table 下的實體 / MainDbContext）後，先確認確實有 drift
dotnet ef migrations has-pending-model-changes --project src/DiscordStreamNotifyBot.Shared

# 2) 產生 migration（名稱用描述性 PascalCase）
dotnet ef migrations add <Name> --project src/DiscordStreamNotifyBot.Shared

# 3) 審 Migrations/<timestamp>_<Name>.cs 的 Up()/Down()——特別注意 DROP COLUMN / DROP TABLE 這種破壞性操作
#    破壞性變更務必先備份正式庫

# 4) 套用（依環境分兩條路，見下）
```

### 套用：本地/開發 vs 正式環境

- **本地 / 開發**：直接套用即可
  ```powershell
  dotnet ef database update --project src/DiscordStreamNotifyBot.Shared
  ```
- **正式環境**：服務啟動時會自動套用（見下節），所以**不需要**再手動 `database update`。
  上線前先產生冪等 SQL 審查這次會改什麼，確認沒有預料外的 `DROP`：
  ```powershell
  # 內含 __EFMigrationsHistory 判斷，重跑/不確定已套到哪都安全
  dotnet ef migrations script --idempotent --project src/DiscordStreamNotifyBot.Shared -o migrate.sql
  # 或只產生某個範圍： dotnet ef migrations script <FromMigration> <ToMigration> --idempotent ... -o migrate.sql
  ```
  含破壞性變更時先備份資料庫，也可以在維護窗口手動執行審過的 SQL，讓啟動時只剩「已是最新」。

> YouTube 影片四表（`HoloVideos` / `NijisanjiVideos` / `OtherVideos` / `NonApprovedVideos`）共同繼承
> `DataBase.Table.Video` 基底，加共用欄位時是改 base，會同時影響四張表——審 migration 時確認四張都有動到。

## 啟動時自動套用 migration（重要）

`Shared/StartupPreflight.cs` 在 MySQL 連線檢查通過後直接套用 migration（scraper 與 notifier 都是）。重點：

- **不碰 `EnsureCreated`**：正式 DB 已基線化（有 `__EFMigrationsHistory`），`EnsureCreated` 對全新庫會建出無遷移歷史的
  schema，之後所有 migration 都會失敗。只能用 migration。
- **併發由 Pomelo 內建鎖處理**：scraper 與各 shard 同時啟動時，EF 的 `MigrateAsync` 會用
  `GET_LOCK('__{database}_EFMigrationsLock', 259200)` 互斥並重讀歷史，不需要自己加鎖。
- **migration 的指令逾時放寬到 1 小時**：大表 `ALTER TABLE` 超過預設 30 秒會被取消，DDL 不可交易，會留下半套 schema。
- **資料庫本身要先存在**：程式不會建立 database。
- **migration 失敗＝程序不啟動**：notifier 的 compose `restart` 是 `no`，失敗後不會自己重啟，要看 log 修。

**流程**：改 model → `migrations add` → 審 `Up()/Down()` → 產冪等 SQL 審查（見上節）→ 部署，啟動時自動套用。

## 上線順序與回退

- migration 必須讓舊版 Bot 與 Backend 還能跑（先加欄位／表、之後才刪）。做不到就停掉所有服務再部署。
- 新版程序一啟動就套用 schema；Backend 在另一個 repo，不會跟著重啟。
- 回退 image 不會退 schema；要還原得自己執行 `Down()` 對應的 SQL。

## 一次性基線化（舊的 EnsureCreated 正式庫）

> 本專案的正式 DB **已經基線化過了**（見記憶 ef-baseline-done），平常不會再碰這段。
> 只有面對「另一個由舊 EnsureCreated 建立、無遷移歷史」的庫時才需要。

舊庫沒有遷移歷史，直接 `database update` 會從頭套 `RefactorDbContext`（建立已存在的表）而失敗。解法：
1. **完整備份資料庫**（腳本開頭也這樣警告）。
2. 執行 `src/DiscordStreamNotifyBot.Shared/Migrations/_Baseline_ExistingDb.sql`：建 `__EFMigrationsHistory`
   並把 schema 已存在的前置遷移（`RefactorDbContext` / `ModifyTwitCastingTable` / `AddMaxSpiderCountSettingField`）
   標記為已套用（`INSERT IGNORE`，可重複執行；不含 `SyncModelDrift`）。
3. `dotnet ef database update --project src/DiscordStreamNotifyBot.Shared`：只會實際跑 `SyncModelDrift`
   （`DROP TABLE IF EXISTS` 三張 Twitter Space 舊表，不存在則略過）。

之後一律走 migration，不再用 EnsureCreated。

## 收尾
- 建置驗證：`dotnet build DiscordStreamNotifyBot.sln -c Release`。
- 提交前確認 `Migrations/`（含 `.Designer.cs` 與 `MainDbContextModelSnapshot.cs`）一併納入 commit。
