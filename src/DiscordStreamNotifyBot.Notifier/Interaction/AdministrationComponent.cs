using Discord.Interactions;
using DiscordStreamNotifyBot.SharedService.Cluster;
using System.Globalization;

namespace DiscordStreamNotifyBot.Interaction
{
    /// <summary>處理管理員通知頻道檢查結果的 SQL 按鈕。</summary>
    public sealed class AdministrationComponent : TopLevelModule
    {
        private readonly ClusterQueryService _clusterQuery;

        public AdministrationComponent(ClusterQueryService clusterQuery)
        {
            _clusterQuery = clusterQuery;
        }

        [ComponentInteraction("admin-notify-sql:*", true)]
        public async Task SendDeleteSqlAsync(string platform)
        {
            var button = (SocketMessageComponent)Context.Interaction;
            if (Context.User.Id != Bot.ApplicatonOwner.Id)
            {
                await button.RespondAsync("只有 Bot owner 可以使用此按鈕", ephemeral: true);
                return;
            }

            platform = platform.ToLowerInvariant();
            if (platform is not ("youtube" or "twitch" or "twitcasting"))
            {
                await button.RespondAsync("無效的平台", ephemeral: true);
                return;
            }

            try
            {
                await button.RespondAsync("處理中，正在重新檢查通知頻道...", ephemeral: true);
                var (responses, responded, expected) = await _clusterQuery.RequestAsync<ClusterQueryService.NotificationChannelCheckResponse>(
                    ClusterQueryService.ClusterQueryType.NotificationChannelCheck, "",
                    ClusterQueryService.NotificationChannelCheckTimeout);
                var issues = responses
                    .SelectMany(response => response.Issues)
                    .Where(issue => issue.Platform.Equals(platform, StringComparison.OrdinalIgnoreCase))
                    .GroupBy(issue => (issue.GuildId, issue.ChannelId))
                    .Select(group => group.First())
                    .OrderBy(issue => issue.GuildId)
                    .ThenBy(issue => issue.ChannelId)
                    .ToList();

                if (responded < expected && issues.Count == 0)
                {
                    await button.ModifyOriginalResponseAsync(message => message.Content =
                        $"通知頻道檢查未完成（{responded}/{expected} shard 回應），目前無法產生完整 SQL");
                    return;
                }

                if (issues.Count == 0)
                {
                    await button.ModifyOriginalResponseAsync(message => message.Content = "目前沒有需要刪除的通知資料");
                    return;
                }

                string sql = string.Join(Environment.NewLine, issues.Select(issue => platform switch
                {
                    "youtube" => $"DELETE FROM `notice_youtube_stream_channel` WHERE `guild_id` = {issue.GuildId.ToString(CultureInfo.InvariantCulture)} AND (`discord_notice_stream_channel_id` = {issue.ChannelId.ToString(CultureInfo.InvariantCulture)} OR `discord_notice_video_channel_id` = {issue.ChannelId.ToString(CultureInfo.InvariantCulture)});",
                    "twitch" => $"DELETE FROM `notice_twitch_stream_channels` WHERE `guild_id` = {issue.GuildId.ToString(CultureInfo.InvariantCulture)} AND `discord_channel_id` = {issue.ChannelId.ToString(CultureInfo.InvariantCulture)};",
                    _ => $"DELETE FROM `notice_twitcasting_stream_channels` WHERE `guild_id` = {issue.GuildId.ToString(CultureInfo.InvariantCulture)} AND `discord_channel_id` = {issue.ChannelId.ToString(CultureInfo.InvariantCulture)};"
                }));

                if (responded < expected)
                    sql += $"{Environment.NewLine}{Environment.NewLine}-- 注意：只收到 {responded}/{expected} shard 回應，SQL 可能不完整";
                await button.ModifyOriginalResponseAsync(message => message.Content = $"```sql\n{sql}\n```");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "Get notification delete SQL Error");
                if (Context.Interaction.HasResponded)
                    await button.ModifyOriginalResponseAsync(message => message.Content = "產生 DELETE SQL 失敗，請查看日誌");
                else
                    await button.RespondAsync("產生 DELETE SQL 失敗，請查看日誌", ephemeral: true);
            }
        }
    }
}
