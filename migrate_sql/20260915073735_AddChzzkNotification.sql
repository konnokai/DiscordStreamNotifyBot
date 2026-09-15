START TRANSACTION;
DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260915073735_AddChzzkNotification') THEN

    ALTER TABLE `guild_config` ADD `max_chzzk_spider_count` int unsigned NOT NULL DEFAULT 3;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260915073735_AddChzzkNotification') THEN

    CREATE TABLE `chzzk_spider` (
        `channel_id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `channel_name` varchar(128) CHARACTER SET utf8mb4 NULL,
        `channel_image_url` varchar(512) CHARACTER SET utf8mb4 NULL,
        `guild_id` bigint unsigned NOT NULL,
        `date_added` datetime(6) NULL,
        `initialized_at` datetime(6) NULL,
        `current_stream_key` varchar(128) CHARACTER SET utf8mb4 NULL,
        CONSTRAINT `pk_chzzk_spider` PRIMARY KEY (`channel_id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260915073735_AddChzzkNotification') THEN

    CREATE TABLE `chzzk_streams` (
        `id` int NOT NULL AUTO_INCREMENT,
        `stream_key` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
        `channel_id` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
        `open_date_raw` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
        `close_date_raw` varchar(64) CHARACTER SET utf8mb4 NULL,
        `stream_title` varchar(256) CHARACTER SET utf8mb4 NULL,
        `category_name` varchar(128) CHARACTER SET utf8mb4 NULL,
        `status` int NOT NULL,
        `last_observed_at` datetime(6) NOT NULL,
        `date_added` datetime(6) NULL,
        CONSTRAINT `pk_chzzk_streams` PRIMARY KEY (`id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260915073735_AddChzzkNotification') THEN

    CREATE TABLE `notice_chzzk_stream_channels` (
        `id` int NOT NULL AUTO_INCREMENT,
        `guild_id` bigint unsigned NOT NULL,
        `discord_channel_id` bigint unsigned NOT NULL,
        `notice_chzzk_channel_id` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
        `start_stream_message` longtext CHARACTER SET utf8mb4 NULL,
        `end_stream_message` longtext CHARACTER SET utf8mb4 NULL,
        `date_added` datetime(6) NULL,
        CONSTRAINT `pk_notice_chzzk_stream_channels` PRIMARY KEY (`id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260915073735_AddChzzkNotification') THEN

    CREATE INDEX `ix_chzzk_streams_channel_id` ON `chzzk_streams` (`channel_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260915073735_AddChzzkNotification') THEN

    CREATE UNIQUE INDEX `ix_chzzk_streams_stream_key` ON `chzzk_streams` (`stream_key`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260915073735_AddChzzkNotification') THEN

    CREATE UNIQUE INDEX `ix_notice_chzzk_stream_channels_guild_id_notice_chzzk_channel_id` ON `notice_chzzk_stream_channels` (`guild_id`, `notice_chzzk_channel_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260915073735_AddChzzkNotification') THEN

    INSERT INTO `__EFMigrationsHistory` (`migration_id`, `product_version`)
    VALUES ('20260915073735_AddChzzkNotification', '9.0.3');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;
