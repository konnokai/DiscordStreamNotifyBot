-- Read-only preflight for AddYoutubeMemberVerificationDurability.
-- Any returned row blocks deployment until a human determines ownership and cleanup.

-- Duplicate YouTube member configurations.
SELECT guild_id, member_check_channel_id, COUNT(*) AS duplicate_count
FROM guild_youtube_member_config
GROUP BY guild_id, member_check_channel_id
HAVING COUNT(*) > 1;

-- Duplicate YouTube member checks.
SELECT guild_id, user_id, check_yt_channel_id, COUNT(*) AS duplicate_count
FROM youtube_member_check
GROUP BY guild_id, user_id, check_yt_channel_id
HAVING COUNT(*) > 1;

-- Checks without a matching guild/channel configuration.
SELECT c.id, c.guild_id, c.user_id, c.check_yt_channel_id
FROM youtube_member_check AS c
LEFT JOIN guild_youtube_member_config AS g
    ON g.guild_id = c.guild_id
   AND g.member_check_channel_id = c.check_yt_channel_id
WHERE g.id IS NULL;

-- Invalid current role IDs. Previous-role checks apply after the additive migration.
SELECT id, guild_id, member_check_grant_role_id
FROM guild_youtube_member_config
WHERE member_check_grant_role_id = 0;

-- IDs outside the existing YouTube channel contract cannot safely use the prefix indexes.
SELECT 'config' AS source_table, id, guild_id, member_check_channel_id AS channel_id
FROM guild_youtube_member_config
WHERE member_check_channel_id IS NULL
   OR CHAR_LENGTH(member_check_channel_id) = 0
   OR CHAR_LENGTH(member_check_channel_id) > 24
UNION ALL
SELECT 'check' AS source_table, id, guild_id, check_yt_channel_id AS channel_id
FROM youtube_member_check
WHERE check_yt_channel_id IS NULL
   OR CHAR_LENGTH(check_yt_channel_id) = 0
   OR CHAR_LENGTH(check_yt_channel_id) > 24;

-- Existing YouTube/Twitch role collisions. Report only; never reassign automatically.
SELECT
    y.id AS youtube_config_id,
    t.id AS twitch_config_id,
    y.guild_id,
    y.member_check_grant_role_id AS youtube_role_id,
    CASE
        WHEN y.member_check_grant_role_id = t.subscriber_role_id THEN 'subscriber'
        WHEN y.member_check_grant_role_id = t.previous_subscriber_role_id THEN 'previous_subscriber'
        WHEN y.member_check_grant_role_id = t.tier1role_id THEN 'tier1'
        WHEN y.member_check_grant_role_id = t.tier2role_id THEN 'tier2'
        WHEN y.member_check_grant_role_id = t.tier3role_id THEN 'tier3'
    END AS twitch_reference
FROM guild_youtube_member_config AS y
INNER JOIN guild_twitch_subscription_config AS t
    ON t.guild_id = y.guild_id
   AND y.member_check_grant_role_id IN (
       t.subscriber_role_id,
       COALESCE(t.previous_subscriber_role_id, 0),
       t.tier1role_id,
       t.tier2role_id,
       t.tier3role_id
   );
