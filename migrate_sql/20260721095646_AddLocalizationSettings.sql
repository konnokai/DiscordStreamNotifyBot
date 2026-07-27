START TRANSACTION;
DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `migration_id` = '20260721095646_AddLocalizationSettings') THEN
        IF NOT EXISTS(
            SELECT 1 FROM `information_schema`.`columns`
            WHERE `table_schema` = DATABASE()
                AND `table_name` = 'youtube_member_check'
                AND `column_name` = 'locale'
        ) THEN
            ALTER TABLE `youtube_member_check` ADD `locale` varchar(16) CHARACTER SET utf8mb4 NULL;
        END IF;

        IF NOT EXISTS(
            SELECT 1 FROM `information_schema`.`columns`
            WHERE `table_schema` = DATABASE()
                AND `table_name` = 'guild_config'
                AND `column_name` = 'locale'
        ) THEN
            ALTER TABLE `guild_config` ADD `locale` varchar(16) CHARACTER SET utf8mb4 NULL;
        END IF;

        IF EXISTS(
            SELECT 1 FROM `information_schema`.`columns`
            WHERE `table_schema` = DATABASE()
                AND `table_name` = 'youtube_member_check'
                AND `column_name` = 'locale'
        ) AND EXISTS(
            SELECT 1 FROM `information_schema`.`columns`
            WHERE `table_schema` = DATABASE()
                AND `table_name` = 'guild_config'
                AND `column_name` = 'locale'
        ) THEN
            INSERT INTO `__EFMigrationsHistory` (`migration_id`, `product_version`)
            VALUES ('20260721095646_AddLocalizationSettings', '9.0.3');
        END IF;
    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;
