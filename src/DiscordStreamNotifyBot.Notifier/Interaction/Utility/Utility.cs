using Discord.Interactions;
using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.SharedService.Cluster;
using System.Globalization;

namespace DiscordStreamNotifyBot.Interaction.Utility
{
    [Group("utility", "工具")]
    public class Utility : TopLevelModule
    {
        private readonly DiscordSocketClient _client;
        private readonly HttpClients.DiscordWebhookClient _discordWebhookClient;
        private readonly ClusterQueryService _clusterQuery;

        public Utility(
            DiscordSocketClient client,
            HttpClients.DiscordWebhookClient discordWebhookClient,
            ClusterQueryService clusterQuery)
        {
            _client = client;
            _discordWebhookClient = discordWebhookClient;
            _clusterQuery = clusterQuery;
        }

        [SlashCommand("ping", "延遲檢測")]
        public async Task PingAsync()
        {
            await SendLocalizedConfirmAsync("Utility.Ping", false, false, _client.Latency);
        }

        [SlashCommand("invite", "取得邀請連結")]
        public async Task InviteAsync()
        {
#if RELEASE
            if (Context.User.Id != Bot.ApplicatonOwner.Id)
            {
                _discordWebhookClient.SendMessageToDiscord($"[{Context.Guild.Name}-{Context.Channel.Name}] {Context.User.Username}:({Context.User.Id}) 使用了邀請指令");
            }
#endif     
            await SendLocalizedConfirmAsync("Utility.Invite", false, true,
                $"https://discordapp.com/api/oauth2/authorize?client_id={_client.CurrentUser.Id}&permissions=11006299201&scope=bot+applications.commands");
        }

        [SlashCommand("status", "顯示機器人目前的狀態")]
        public async Task StatusAsync()
        {
            string locale = await GetLocaleAsync(false);
            EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor();
            embedBuilder.WithTitle(BotLocalizer.Get("Utility.Status.Title", locale));

#if DEBUG || DEBUG_DONTREGISTERCOMMAND
            embedBuilder.Title += BotLocalizer.Get("Utility.Status.TestBuild", locale);
#endif

            embedBuilder.WithDescription(BotLocalizer.Format("Utility.Status.Build", locale, Program.Version));
            embedBuilder.AddField(BotLocalizer.Get("Utility.Status.Author", locale), "孤之界 (konnokai)", true);
            embedBuilder.AddField(BotLocalizer.Get("Utility.Status.Owner", locale), $"{Bot.ApplicatonOwner}", true);
            // 跨 shard：以合併快照（B1）彙總全叢集的伺服器數與成員數，而非只算本 shard
            var mergedGuilds = await _clusterQuery.ReadMergedGuildsAsync();
            embedBuilder.AddField(BotLocalizer.Get("Utility.Status.State", locale),
                BotLocalizer.Format("Utility.Status.StateValue", locale, mergedGuilds.Count, mergedGuilds.Sum(x => x.MemberCount)), false);
            embedBuilder.AddField(BotLocalizer.Get("Utility.Status.StreamCount", locale), DiscordStreamNotifyBot.Utility.GetDbStreamCount(), true);
            embedBuilder.AddField(BotLocalizer.Get("Utility.Status.Uptime", locale),
                BotLocalizer.Format("Utility.Status.UptimeValue", locale, Bot.StopWatch.Elapsed.Days,
                    Bot.StopWatch.Elapsed.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)), false);

            await RespondAsync(embed: embedBuilder.Build());
        }
    }
}
