CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `migration_id` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `product_version` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `pk___ef_migrations_history` PRIMARY KEY (`migration_id`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;
DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    ALTER DATABASE CHARACTER SET utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    CREATE TABLE `banner_change` (
        `id` int NOT NULL AUTO_INCREMENT,
        `guild_id` bigint unsigned NOT NULL,
        `channel_id` longtext CHARACTER SET utf8mb4 NULL,
        `last_change_stream_id` longtext CHARACTER SET utf8mb4 NULL,
        `date_added` datetime(6) NULL,
        CONSTRAINT `pk_banner_change` PRIMARY KEY (`id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    CREATE TABLE `guild_config` (
        `id` int NOT NULL AUTO_INCREMENT,
        `guild_id` bigint unsigned NOT NULL,
        `log_member_status_channel_id` bigint unsigned NOT NULL,
        `notice_channel_id` bigint unsigned NOT NULL,
        `date_added` datetime(6) NULL,
        CONSTRAINT `pk_guild_config` PRIMARY KEY (`id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    CREATE TABLE `guild_youtube_member_config` (
        `id` int NOT NULL AUTO_INCREMENT,
        `guild_id` bigint unsigned NOT NULL,
        `member_check_channel_id` longtext CHARACTER SET utf8mb4 NULL,
        `member_check_channel_title` longtext CHARACTER SET utf8mb4 NULL,
        `member_check_video_id` longtext CHARACTER SET utf8mb4 NULL,
        `member_check_grant_role_id` bigint unsigned NOT NULL,
        `date_added` datetime(6) NULL,
        CONSTRAINT `pk_guild_youtube_member_config` PRIMARY KEY (`id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    CREATE TABLE `holo_videos` (
        `video_id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `channel_id` longtext CHARACTER SET utf8mb4 NULL,
        `channel_title` longtext CHARACTER SET utf8mb4 NULL,
        `video_title` longtext CHARACTER SET utf8mb4 NULL,
        `scheduled_start_time` datetime(6) NOT NULL,
        `channel_type` int NOT NULL,
        `is_private` tinyint(1) NOT NULL,
        CONSTRAINT `pk_holo_videos` PRIMARY KEY (`video_id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    CREATE TABLE `nijisanji_videos` (
        `video_id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `channel_id` longtext CHARACTER SET utf8mb4 NULL,
        `channel_title` longtext CHARACTER SET utf8mb4 NULL,
        `video_title` longtext CHARACTER SET utf8mb4 NULL,
        `scheduled_start_time` datetime(6) NOT NULL,
        `channel_type` int NOT NULL,
        `is_private` tinyint(1) NOT NULL,
        CONSTRAINT `pk_nijisanji_videos` PRIMARY KEY (`video_id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    CREATE TABLE `non_approved_videos` (
        `video_id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `channel_id` longtext CHARACTER SET utf8mb4 NULL,
        `channel_title` longtext CHARACTER SET utf8mb4 NULL,
        `video_title` longtext CHARACTER SET utf8mb4 NULL,
        `scheduled_start_time` datetime(6) NOT NULL,
        `channel_type` int NOT NULL,
        `is_private` tinyint(1) NOT NULL,
        CONSTRAINT `pk_non_approved_videos` PRIMARY KEY (`video_id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    CREATE TABLE `notice_twitcasting_stream_channels` (
        `id` int NOT NULL AUTO_INCREMENT,
        `guild_id` bigint unsigned NOT NULL,
        `discord_channel_id` bigint unsigned NOT NULL,
        `channel_id` longtext CHARACTER SET utf8mb4 NULL,
        `start_stream_message` longtext CHARACTER SET utf8mb4 NULL,
        `date_added` datetime(6) NULL,
        CONSTRAINT `pk_notice_twitcasting_stream_channels` PRIMARY KEY (`id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    CREATE TABLE `notice_twitch_stream_channels` (
        `id` int NOT NULL AUTO_INCREMENT,
        `guild_id` bigint unsigned NOT NULL,
        `discord_channel_id` bigint unsigned NOT NULL,
        `notice_twitch_user_id` longtext CHARACTER SET utf8mb4 NULL,
        `start_stream_message` longtext CHARACTER SET utf8mb4 NULL,
        `end_stream_message` longtext CHARACTER SET utf8mb4 NULL,
        `change_stream_data_message` longtext CHARACTER SET utf8mb4 NULL,
        `date_added` datetime(6) NULL,
        CONSTRAINT `pk_notice_twitch_stream_channels` PRIMARY KEY (`id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    CREATE TABLE `notice_twitter_space_channel` (
        `id` int NOT NULL AUTO_INCREMENT,
        `guild_id` bigint unsigned NOT NULL,
        `discord_channel_id` bigint unsigned NOT NULL,
        `notice_twitter_space_user_id` longtext CHARACTER SET utf8mb4 NULL,
        `notice_twitter_space_user_screen_name` longtext CHARACTER SET utf8mb4 NULL,
        `strat_twitter_space_message` longtext CHARACTER SET utf8mb4 NULL,
        `date_added` datetime(6) NULL,
        CONSTRAINT `pk_notice_twitter_space_channel` PRIMARY KEY (`id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    CREATE TABLE `notice_youtube_stream_channel` (
        `id` int NOT NULL AUTO_INCREMENT,
        `guild_id` bigint unsigned NOT NULL,
        `discord_notice_video_channel_id` bigint unsigned NOT NULL,
        `discord_notice_stream_channel_id` bigint unsigned NOT NULL,
        `is_create_event_for_new_stream` tinyint(1) NOT NULL,
        `you_tube_channel_id` longtext CHARACTER SET utf8mb4 NULL,
        `new_stream_message` longtext CHARACTER SET utf8mb4 NULL,
        `new_video_message` longtext CHARACTER SET utf8mb4 NULL,
        `strat_message` longtext CHARACTER SET utf8mb4 NULL,
        `end_message` longtext CHARACTER SET utf8mb4 NULL,
        `change_time_message` longtext CHARACTER SET utf8mb4 NULL,
        `delete_message` longtext CHARACTER SET utf8mb4 NULL,
        `date_added` datetime(6) NULL,
        CONSTRAINT `pk_notice_youtube_stream_channel` PRIMARY KEY (`id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    CREATE TABLE `other_videos` (
        `video_id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `channel_id` longtext CHARACTER SET utf8mb4 NULL,
        `channel_title` longtext CHARACTER SET utf8mb4 NULL,
        `video_title` longtext CHARACTER SET utf8mb4 NULL,
        `scheduled_start_time` datetime(6) NOT NULL,
        `channel_type` int NOT NULL,
        `is_private` tinyint(1) NOT NULL,
        CONSTRAINT `pk_other_videos` PRIMARY KEY (`video_id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    CREATE TABLE `record_youtube_channel` (
        `youtube_channel_id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `date_added` datetime(6) NULL,
        CONSTRAINT `pk_record_youtube_channel` PRIMARY KEY (`youtube_channel_id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    CREATE TABLE `twitcasting_spider` (
        `id` int NOT NULL AUTO_INCREMENT,
        `guild_id` bigint unsigned NOT NULL,
        `channel_title` longtext CHARACTER SET utf8mb4 NULL,
        `channel_id` longtext CHARACTER SET utf8mb4 NULL,
        `is_warning_user` tinyint(1) NOT NULL,
        `is_record` tinyint(1) NOT NULL,
        `date_added` datetime(6) NULL,
        CONSTRAINT `pk_twitcasting_spider` PRIMARY KEY (`id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    CREATE TABLE `twitcasting_streams` (
        `id` int NOT NULL AUTO_INCREMENT,
        `channel_id` longtext CHARACTER SET utf8mb4 NULL,
        `channel_title` longtext CHARACTER SET utf8mb4 NULL,
        `stream_id` int NOT NULL,
        `stream_title` longtext CHARACTER SET utf8mb4 NULL,
        `stream_sub_title` longtext CHARACTER SET utf8mb4 NULL,
        `category` longtext CHARACTER SET utf8mb4 NULL,
        `thumbnail_url` longtext CHARACTER SET utf8mb4 NULL,
        `stream_start_at` datetime(6) NOT NULL,
        `date_added` datetime(6) NULL,
        CONSTRAINT `pk_twitcasting_streams` PRIMARY KEY (`id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    CREATE TABLE `twitch_spider` (
        `user_id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `user_login` longtext CHARACTER SET utf8mb4 NULL,
        `user_name` longtext CHARACTER SET utf8mb4 NULL,
        `profile_image_url` longtext CHARACTER SET utf8mb4 NULL,
        `offline_image_url` longtext CHARACTER SET utf8mb4 NULL,
        `guild_id` bigint unsigned NOT NULL,
        `is_warning_user` tinyint(1) NOT NULL,
        `is_record` tinyint(1) NOT NULL,
        `date_added` datetime(6) NULL,
        CONSTRAINT `pk_twitch_spider` PRIMARY KEY (`user_id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    CREATE TABLE `twitch_streams` (
        `id` int NOT NULL AUTO_INCREMENT,
        `stream_id` longtext CHARACTER SET utf8mb4 NULL,
        `stream_title` longtext CHARACTER SET utf8mb4 NULL,
        `stream_start_at` datetime(6) NOT NULL,
        `user_id` longtext CHARACTER SET utf8mb4 NULL,
        `user_login` longtext CHARACTER SET utf8mb4 NULL,
        `user_name` longtext CHARACTER SET utf8mb4 NULL,
        `game_name` longtext CHARACTER SET utf8mb4 NULL,
        `thumbnail_url` longtext CHARACTER SET utf8mb4 NULL,
        `date_added` datetime(6) NULL,
        CONSTRAINT `pk_twitch_streams` PRIMARY KEY (`id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    CREATE TABLE `twitter_space` (
        `id` int NOT NULL AUTO_INCREMENT,
        `user_id` longtext CHARACTER SET utf8mb4 NULL,
        `user_screen_name` longtext CHARACTER SET utf8mb4 NULL,
        `user_name` longtext CHARACTER SET utf8mb4 NULL,
        `spaec_id` longtext CHARACTER SET utf8mb4 NULL,
        `spaec_title` longtext CHARACTER SET utf8mb4 NULL,
        `spaec_actual_start_time` datetime(6) NOT NULL,
        `spaec_master_playlist_url` longtext CHARACTER SET utf8mb4 NULL,
        `date_added` datetime(6) NULL,
        CONSTRAINT `pk_twitter_space` PRIMARY KEY (`id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    CREATE TABLE `twitter_space_spider` (
        `user_id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `user_screen_name` longtext CHARACTER SET utf8mb4 NULL,
        `user_name` longtext CHARACTER SET utf8mb4 NULL,
        `guild_id` bigint unsigned NOT NULL,
        `is_warning_user` tinyint(1) NOT NULL,
        `is_record` tinyint(1) NOT NULL,
        `date_added` datetime(6) NULL,
        CONSTRAINT `pk_twitter_space_spider` PRIMARY KEY (`user_id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    CREATE TABLE `youtube_channel_name_to_id` (
        `id` int NOT NULL AUTO_INCREMENT,
        `channel_name` longtext CHARACTER SET utf8mb4 NULL,
        `channel_id` longtext CHARACTER SET utf8mb4 NULL,
        `date_added` datetime(6) NULL,
        CONSTRAINT `pk_youtube_channel_name_to_id` PRIMARY KEY (`id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    CREATE TABLE `youtube_channel_owned_type` (
        `channel_id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `channel_title` longtext CHARACTER SET utf8mb4 NULL,
        `channel_type` int NOT NULL,
        `date_added` datetime(6) NULL,
        CONSTRAINT `pk_youtube_channel_owned_type` PRIMARY KEY (`channel_id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    CREATE TABLE `youtube_channel_spider` (
        `channel_id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `channel_title` longtext CHARACTER SET utf8mb4 NULL,
        `guild_id` bigint unsigned NOT NULL,
        `is_trusted_channel` tinyint(1) NOT NULL,
        `last_subscribe_time` datetime(6) NOT NULL,
        `date_added` datetime(6) NULL,
        CONSTRAINT `pk_youtube_channel_spider` PRIMARY KEY (`channel_id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    CREATE TABLE `youtube_member_access_token` (
        `discord_user_id` bigint unsigned NOT NULL AUTO_INCREMENT,
        `encrypted_access_token` longtext CHARACTER SET utf8mb4 NULL,
        `date_added` datetime(6) NULL,
        CONSTRAINT `pk_youtube_member_access_token` PRIMARY KEY (`discord_user_id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    CREATE TABLE `youtube_member_check` (
        `id` int NOT NULL AUTO_INCREMENT,
        `guild_id` bigint unsigned NOT NULL,
        `user_id` bigint unsigned NOT NULL,
        `check_yt_channel_id` longtext CHARACTER SET utf8mb4 NULL,
        `last_check_time` datetime(6) NOT NULL,
        `is_checked` tinyint(1) NOT NULL,
        `date_added` datetime(6) NULL,
        CONSTRAINT `pk_youtube_member_check` PRIMARY KEY (`id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250320095452_RefactorDbContext') THEN

    INSERT INTO `__EFMigrationsHistory` (`migration_id`, `product_version`)
    VALUES ('20250320095452_RefactorDbContext', '9.0.3');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250603065853_ModifyTwitCastingTable') THEN

    ALTER TABLE `notice_twitcasting_stream_channels` RENAME COLUMN `channel_id` TO `screen_id`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250603065853_ModifyTwitCastingTable') THEN

    ALTER TABLE `twitcasting_spider` ADD `screen_id` longtext CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250603065853_ModifyTwitCastingTable') THEN

    INSERT INTO `__EFMigrationsHistory` (`migration_id`, `product_version`)
    VALUES ('20250603065853_ModifyTwitCastingTable', '9.0.3');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250620094111_AddMaxSpiderCountSettingField') THEN

    ALTER TABLE `guild_config` ADD `max_twitcasting_spider_count` int unsigned NOT NULL DEFAULT 3;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250620094111_AddMaxSpiderCountSettingField') THEN

    ALTER TABLE `guild_config` ADD `max_twitch_spider_count` int unsigned NOT NULL DEFAULT 3;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250620094111_AddMaxSpiderCountSettingField') THEN

    ALTER TABLE `guild_config` ADD `max_twitter_space_spider_count` int unsigned NOT NULL DEFAULT 3;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250620094111_AddMaxSpiderCountSettingField') THEN

    ALTER TABLE `guild_config` ADD `max_you_tube_member_check_count` int unsigned NOT NULL DEFAULT 5;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250620094111_AddMaxSpiderCountSettingField') THEN

    ALTER TABLE `guild_config` ADD `max_you_tube_spider_count` int unsigned NOT NULL DEFAULT 3;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20250620094111_AddMaxSpiderCountSettingField') THEN

    INSERT INTO `__EFMigrationsHistory` (`migration_id`, `product_version`)
    VALUES ('20250620094111_AddMaxSpiderCountSettingField', '9.0.3');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260611015819_SyncModelDrift') THEN

    DROP TABLE IF EXISTS `notice_twitter_space_channel`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260611015819_SyncModelDrift') THEN

    DROP TABLE IF EXISTS `twitter_space`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260611015819_SyncModelDrift') THEN

    DROP TABLE IF EXISTS `twitter_space_spider`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260611015819_SyncModelDrift') THEN

    INSERT INTO `__EFMigrationsHistory` (`migration_id`, `product_version`)
    VALUES ('20260611015819_SyncModelDrift', '9.0.3');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260709091318_AddManualMemberCheckVideoFlag') THEN

    ALTER TABLE `guild_youtube_member_config` ADD `is_manual_video_id` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260709091318_AddManualMemberCheckVideoFlag') THEN

    INSERT INTO `__EFMigrationsHistory` (`migration_id`, `product_version`)
    VALUES ('20260709091318_AddManualMemberCheckVideoFlag', '9.0.3');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260719142803_AddTwitchBroadcasterAuthorization') THEN

    CREATE TABLE `twitch_broadcaster_authorization` (
        `twitch_user_id` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
        `discord_user_id` bigint unsigned NOT NULL,
        `client_id` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
        `user_login` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
        `display_name` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
        `profile_image_url` varchar(512) CHARACTER SET utf8mb4 NOT NULL,
        `encrypted_access_token` longtext CHARACTER SET utf8mb4 NULL,
        `scopes` longtext CHARACTER SET utf8mb4 NOT NULL,
        `token_expires_at` datetime(6) NULL,
        `last_validated_at` datetime(6) NULL,
        `authorized_at` datetime(6) NOT NULL,
        `revoked_at` datetime(6) NULL,
        `revocation_reason` varchar(64) CHARACTER SET utf8mb4 NULL,
        `date_updated` datetime(6) NOT NULL,
        CONSTRAINT `pk_twitch_broadcaster_authorization` PRIMARY KEY (`twitch_user_id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260719142803_AddTwitchBroadcasterAuthorization') THEN

    CREATE UNIQUE INDEX `ix_twitch_broadcaster_authorization_discord_user_id` ON `twitch_broadcaster_authorization` (`discord_user_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260719142803_AddTwitchBroadcasterAuthorization') THEN

    INSERT INTO `__EFMigrationsHistory` (`migration_id`, `product_version`)
    VALUES ('20260719142803_AddTwitchBroadcasterAuthorization', '9.0.3');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260721095646_AddLocalizationSettings') THEN

    ALTER TABLE `youtube_member_check` ADD `locale` varchar(16) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260721095646_AddLocalizationSettings') THEN

    ALTER TABLE `guild_config` ADD `locale` varchar(16) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260721095646_AddLocalizationSettings') THEN

    INSERT INTO `__EFMigrationsHistory` (`migration_id`, `product_version`)
    VALUES ('20260721095646_AddLocalizationSettings', '9.0.3');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260803141135_AddTwitchSubscriptionVerification') THEN

    CREATE TABLE `guild_twitch_subscription_config` (
        `id` int NOT NULL AUTO_INCREMENT,
        `guild_id` bigint unsigned NOT NULL,
        `broadcaster_id` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
        `broadcaster_login` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
        `broadcaster_display_name` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
        `subscriber_role_id` bigint unsigned NOT NULL,
        `previous_subscriber_role_id` bigint unsigned NULL,
        `tier1role_id` bigint unsigned NOT NULL,
        `tier2role_id` bigint unsigned NOT NULL,
        `tier3role_id` bigint unsigned NOT NULL,
        `date_added` datetime(6) NULL,
        CONSTRAINT `pk_guild_twitch_subscription_config` PRIMARY KEY (`id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260803141135_AddTwitchSubscriptionVerification') THEN

    CREATE TABLE `twitch_subscription_check` (
        `id` int NOT NULL AUTO_INCREMENT,
        `guild_id` bigint unsigned NOT NULL,
        `discord_user_id` bigint unsigned NOT NULL,
        `broadcaster_id` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
        `locale` varchar(16) CHARACTER SET utf8mb4 NULL,
        `is_checked` tinyint(1) NOT NULL,
        `pending_role_removal` tinyint(1) NOT NULL,
        `tier` varchar(4) CHARACTER SET utf8mb4 NULL,
        `is_gift` tinyint(1) NOT NULL,
        `last_check_time` datetime(6) NOT NULL,
        `date_added` datetime(6) NULL,
        CONSTRAINT `pk_twitch_subscription_check` PRIMARY KEY (`id`),
        CONSTRAINT `ck_twitch_subscription_check_tier` CHECK (`tier` IS NULL OR `tier` IN ('1000', '2000', '3000'))
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260803141135_AddTwitchSubscriptionVerification') THEN

    CREATE UNIQUE INDEX `ix_guild_twitch_subscription_config_guild_id_broadcaster_id` ON `guild_twitch_subscription_config` (`guild_id`, `broadcaster_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260803141135_AddTwitchSubscriptionVerification') THEN

    CREATE UNIQUE INDEX `ix_twitch_subscription_check_guild_id_discord_user_id_broadcast` ON `twitch_subscription_check` (`guild_id`, `discord_user_id`, `broadcaster_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260803141135_AddTwitchSubscriptionVerification') THEN

    INSERT INTO `__EFMigrationsHistory` (`migration_id`, `product_version`)
    VALUES ('20260803141135_AddTwitchSubscriptionVerification', '9.0.3');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260803165758_AddTwitchSubscriptionDeletionPending') THEN

    ALTER TABLE `guild_twitch_subscription_config` ADD `deletion_pending` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260803165758_AddTwitchSubscriptionDeletionPending') THEN

    INSERT INTO `__EFMigrationsHistory` (`migration_id`, `product_version`)
    VALUES ('20260803165758_AddTwitchSubscriptionDeletionPending', '9.0.3');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260804173737_AddYoutubeMemberVerificationDurability') THEN

    ALTER TABLE `youtube_member_check` MODIFY COLUMN `check_yt_channel_id` longtext CHARACTER SET utf8mb4 NOT NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260804173737_AddYoutubeMemberVerificationDurability') THEN

    ALTER TABLE `youtube_member_check` ADD `pending_role_removal` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260804173737_AddYoutubeMemberVerificationDurability') THEN

    ALTER TABLE `guild_youtube_member_config` MODIFY COLUMN `member_check_channel_id` longtext CHARACTER SET utf8mb4 NOT NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260804173737_AddYoutubeMemberVerificationDurability') THEN

    ALTER TABLE `guild_youtube_member_config` ADD `deletion_pending` tinyint(1) NOT NULL DEFAULT FALSE;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260804173737_AddYoutubeMemberVerificationDurability') THEN

    ALTER TABLE `guild_youtube_member_config` ADD `previous_member_check_grant_role_id` bigint unsigned NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260804173737_AddYoutubeMemberVerificationDurability') THEN

    CREATE UNIQUE INDEX `ix_youtube_member_check_guild_id_user_id_check_yt_channel_id` ON `youtube_member_check` (`guild_id`, `user_id`, `check_yt_channel_id`(24));

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260804173737_AddYoutubeMemberVerificationDurability') THEN

    CREATE INDEX `ix_youtube_member_check_pending_role_removal_guild_id` ON `youtube_member_check` (`pending_role_removal`, `guild_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260804173737_AddYoutubeMemberVerificationDurability') THEN

    CREATE INDEX `ix_youtube_member_check_user_id_pending_role_removal` ON `youtube_member_check` (`user_id`, `pending_role_removal`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260804173737_AddYoutubeMemberVerificationDurability') THEN

    CREATE INDEX `ix_guild_youtube_member_config_deletion_pending_guild_id` ON `guild_youtube_member_config` (`deletion_pending`, `guild_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260804173737_AddYoutubeMemberVerificationDurability') THEN

    CREATE UNIQUE INDEX `ix_guild_youtube_member_config_guild_id_member_check_channel_id` ON `guild_youtube_member_config` (`guild_id`, `member_check_channel_id`(24));

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260804173737_AddYoutubeMemberVerificationDurability') THEN

    INSERT INTO `__EFMigrationsHistory` (`migration_id`, `product_version`)
    VALUES ('20260804173737_AddYoutubeMemberVerificationDurability', '9.0.3');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260807045351_AddGoogleOAuthUnlinkIntent') THEN

    CREATE TABLE `google_oauth_unlink_intent` (
        `discord_user_id` bigint unsigned NOT NULL AUTO_INCREMENT,
        `expected_encrypted_token` longtext CHARACTER SET utf8mb4 NULL,
        `date_added` datetime(6) NOT NULL,
        CONSTRAINT `pk_google_oauth_unlink_intent` PRIMARY KEY (`discord_user_id`)
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260807045351_AddGoogleOAuthUnlinkIntent') THEN

    INSERT INTO `__EFMigrationsHistory` (`migration_id`, `product_version`)
    VALUES ('20260807045351_AddGoogleOAuthUnlinkIntent', '9.0.3');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260813032017_RenameVerificationLogChannel') THEN

    ALTER TABLE `guild_config` RENAME COLUMN `log_member_status_channel_id` TO `verification_log_channel_id`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260813032017_RenameVerificationLogChannel') THEN

    INSERT INTO `__EFMigrationsHistory` (`migration_id`, `product_version`)
    VALUES ('20260813032017_RenameVerificationLogChannel', '9.0.3');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;
