START TRANSACTION;
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

COMMIT;
