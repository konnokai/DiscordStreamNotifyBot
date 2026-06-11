-- =============================================================================
-- 既有資料庫 EF Migration 基線化（計畫 §11-2）— 一次性，僅對「由 EnsureCreated 建立、
-- 無 __EFMigrationsHistory」的既有正式資料庫執行。全新空庫不需要本腳本（直接 database update 即可）。
--
-- 背景：舊版以 db.Database.EnsureCreated() 建表，沒有遷移歷史。若直接 dotnet ef database update，
-- EF 會誤以為沒有任何遷移已套用，從頭套用 RefactorDbContext（建立已存在的表）而失敗。
-- 解法：手動把「schema 已存在」的前置遷移標記為已套用，之後 database update 只會跑 SyncModelDrift。
--
-- ⚠ 執行前務必完整備份資料庫。
-- =============================================================================

-- 1) 建立 EF 遷移歷史表（欄位為 snake_case，因為本專案套用 UseSnakeCaseNamingConvention）
CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `migration_id` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `product_version` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `pk___ef_migrations_history` PRIMARY KEY (`migration_id`)
) CHARACTER SET=utf8mb4;

-- 2) 將 schema 已由 EnsureCreated 建立的前置遷移標記為已套用
--    （INSERT IGNORE：可安全重複執行；不含 SyncModelDrift —— 那一筆要留給 database update 實際執行）
INSERT IGNORE INTO `__EFMigrationsHistory` (`migration_id`, `product_version`) VALUES
    ('20250320095452_RefactorDbContext',           '9.0.3'),
    ('20250603065853_ModifyTwitCastingTable',      '9.0.3'),
    ('20250620094111_AddMaxSpiderCountSettingField','9.0.3');

-- 3) 完成後執行（於 repo 根目錄）：
--      dotnet ef database update --project src/DiscordStreamNotifyBot.Shared
--    這會套用 SyncModelDrift（DROP TABLE IF EXISTS 三張 Twitter Space 舊表；不存在則略過）。
--
-- 之後所有 schema 變更一律走 migration，不再使用 EnsureCreated。
