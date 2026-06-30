---
name: ef-migration-baseline
description: >
  在「直播小幫手」做 EF Core schema 變更、新增/套用 migration、或處理「舊的 EnsureCreated 正式庫要基線化」時用。
  涵蓋本專案特有的眉角：EF 工具指向 Shared 專案（已有 MainDbContextFactory 設計階段工廠，免 startup project）、
  連線字串解析順序、shard 0 啟動的 InitializeDatabase 三種分支、以及一次性基線化腳本 _Baseline_ExistingDb.sql。
  只要使用者說「加欄位/改表」「新增 migration」「update database」「schema 變更」「資料表」「基線化」
  「EnsureCreated 的舊庫」，就用這個 skill。注意：本專案的 dotnet-claude-kit `/migrate` 是引導式變體，
  需要逐步審 SQL 時可改用它。
---

# EF Core 遷移與基線化（本專案版）

## 你必須先知道的三件專案特例

1. **工具指向 Shared，不是 Notifier。** `Shared/DataBase/MainDbContextFactory.cs` 是設計階段工廠，
   所以所有 `dotnet ef` 指令都加 `--project src/DiscordStreamNotifyBot.Shared`，**不需** startup project。
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
- **正式環境**：**不**對正式 DB 直接 `database update`。改用 Script-Migration 產生 SQL、人工審過後，
  手動連到對應資料庫執行：
  ```powershell
  # 產生「冪等」腳本（內含 __EFMigrationsHistory 判斷，重跑/不確定已套到哪都安全）
  dotnet ef migrations script --idempotent --project src/DiscordStreamNotifyBot.Shared -o migrate.sql
  # 或只產生某個範圍： dotnet ef migrations script <FromMigration> <ToMigration> --idempotent ... -o migrate.sql
  ```
  審完 `migrate.sql` 再到正式 DB 上跑。理由：正式庫變更要可審、可控、可在維護窗口手動執行，
  不讓工具直連正式庫。

> **與啟動時 `InitializeDatabase` 的關係**：腳本（EF 產生的）會一併寫入 `__EFMigrationsHistory`，
> 所以正式環境**先手動跑完 SQL 再部署新版**；之後 shard 0 啟動的 `Migrate()` 看到沒有待處理遷移＝no-op，
> 不會重複套用，兩者一致不衝突。

> YouTube 影片四表（`HoloVideos` / `NijisanjiVideos` / `OtherVideos` / `NonApprovedVideos`）共同繼承
> `DataBase.Table.Video` 基底，加共用欄位時是改 base，會同時影響四張表——審 migration 時確認四張都有動到。

## 啟動時的自動初始化（shard 0，`Bot.InitializeDatabase`）

不用手動跑也會在 Notifier shard 0 啟動時判斷（理解它，遇到「啟動沒建表/沒遷移」才知道是哪個分支）：
- **全新空庫**（無任何表）→ `Migrate()` 建表並寫遷移歷史。
- **既有庫但無 `__EFMigrationsHistory`**（舊版 EnsureCreated 建立）→ **不自動遷移**，只 warn 提示要先基線化。
- **已有遷移歷史** → `Migrate()` 套用待處理遷移。

## 一次性基線化（舊的 EnsureCreated 正式庫）

> 本專案的正式 DB **已經基線化過了**（見記憶 ef-baseline-done），平常不會再碰這段。
> 只有面對「另一個由舊 EnsureCreated 建立、無遷移歷史」的庫時才需要。

舊庫沒有遷移歷史，直接 `database update` 會從頭套 `RefactorDbContext`（建立已存在的表）而失敗。解法：
1. **完整備份資料庫**（腳本開頭也這樣警告）。
2. 執行 `src/DiscordStreamNotifyBot.Shared/Migrations/_Baseline_ExistingDb.sql`：建 `__EFMigrationsHistory`
   並把 schema 已存在的前置遷移（RefactorDbContext / ModifyTwitCastingTable / AddMaxSpiderCountSettingField）
   標記為已套用（`INSERT IGNORE`，可重複執行；不含 `SyncModelDrift`）。
3. `dotnet ef database update --project src/DiscordStreamNotifyBot.Shared`：只會實際跑 `SyncModelDrift`
   （`DROP TABLE IF EXISTS` 三張 Twitter Space 舊表，不存在則略過）。

之後一律走 migration，不再用 EnsureCreated。

## 收尾
- 建置驗證：`dotnet build DiscordStreamNotifyBot.sln -c Release`。
- 提交前確認 `Migrations/`（含 `.Designer.cs` 與 `MainDbContextModelSnapshot.cs`）一併納入 commit。
