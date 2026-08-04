START TRANSACTION;
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

COMMIT;
