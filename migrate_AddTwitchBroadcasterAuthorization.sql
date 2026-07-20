START TRANSACTION;
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

COMMIT;
