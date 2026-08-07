START TRANSACTION;
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

COMMIT;
