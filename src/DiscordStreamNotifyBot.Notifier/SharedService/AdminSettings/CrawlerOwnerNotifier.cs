using DiscordStreamNotifyBot.Interaction;

namespace DiscordStreamNotifyBot.SharedService.AdminSettings
{
    internal enum CrawlerPlatform
    {
        Youtube,
        Twitch,
        Twitcasting
    }

    /// <summary>在爬蟲成功新增後私訊 Bot 擁有者，提供與 Slash 指令相同的維運按鈕。</summary>
    internal static class CrawlerOwnerNotifier
    {
        public static async Task NotifyAddedAsync(
            CrawlerPlatform platform,
            SocketGuild guild,
            ulong actorUserId,
            string sourceId,
            string sourceName,
            string sourcePath,
            bool addForBotOwner,
            bool oauthBypass = false)
        {
            try
            {
                SocketGuildUser actor = guild.GetUser(actorUserId);
                string actorText = actor == null
                    ? actorUserId.ToString()
                    : $"{actor.GlobalName ?? actor.Username} ({actor} / {actorUserId})";
                var message = BuildAddedMessage(
                    platform,
                    sourceId,
                    sourceName,
                    sourcePath,
                    addForBotOwner ? "擁有者" : $"{guild.Name} ({guild.Id})",
                    actorText,
                    oauthBypass);
                await Bot.ApplicatonOwner.SendMessageAsync(embed: message.Embed, components: message.Components);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"發送 {platform} 爬蟲新增通知給 Bot 擁有者時失敗");
            }
        }

        internal static (Embed Embed, MessageComponent Components) BuildAddedMessage(
            CrawlerPlatform platform,
            string sourceId,
            string sourceName,
            string sourcePath,
            string guild,
            string actor,
            bool oauthBypass = false)
        {
            string platformName = platform switch
            {
                CrawlerPlatform.Youtube => "YouTube",
                CrawlerPlatform.Twitch => "Twitch",
                _ => "TwitCasting"
            };
            string sourceUrl = platform switch
            {
                CrawlerPlatform.Youtube => $"https://www.youtube.com/channel/{sourcePath}",
                CrawlerPlatform.Twitch => $"https://twitch.tv/{sourcePath}",
                _ => $"https://twitcasting.tv/{sourcePath}"
            };
            var embed = new EmbedBuilder()
                .WithOkColor()
                .WithTitle($"已新增 {platformName} 頻道爬蟲")
                .AddField("頻道", Format.Url(sourceName, sourceUrl), false)
                .AddField("伺服器", guild, false)
                .AddField("執行者", actor, false);
            var components = new ComponentBuilder();

            if (platform == CrawlerPlatform.Youtube)
            {
                embed.AddField("認可頻道", "否", true)
                    .AddField("錄影頻道", "否", true);
                components
                    .WithButton("加入認可頻道", $"spider_youtube:trusted:{sourceId}", ButtonStyle.Success)
                    .WithButton("移除認可頻道", $"spider_youtube:untrusted:{sourceId}", ButtonStyle.Danger)
                    .WithButton("加入錄影頻道", $"spider_youtube:record:{sourceId}", ButtonStyle.Success, row: 1)
                    .WithButton("移除錄影頻道", $"spider_youtube:unrecord:{sourceId}", ButtonStyle.Danger, row: 1);
            }
            else
            {
                string buttonPrefix = platform == CrawlerPlatform.Twitch ? "spider_twitch" : "spider_tc";
                if (platform == CrawlerPlatform.Twitch)
                    embed.AddField("是否使用 OAuth 忽略人數要求", oauthBypass ? "是" : "否", false);
                embed.AddField("頻道狀態", "普通", true)
                    .AddField("頻道錄影", "關閉", true);
                components
                    .WithButton("切換頻道狀態", $"{buttonPrefix}:warning:{sourceId}", ButtonStyle.Danger)
                    .WithButton("切換頻道錄影", $"{buttonPrefix}:record:{sourceId}", ButtonStyle.Success);
            }

            return (embed.Build(), components.Build());
        }
    }
}
