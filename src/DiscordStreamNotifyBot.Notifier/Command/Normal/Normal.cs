using Discord.Commands;
using DiscordStreamNotifyBot.SharedService.Cluster;

namespace DiscordStreamNotifyBot.Command.Normal
{
    public class Normal : TopLevelModule
    {
        private readonly DiscordSocketClient _client;
        private readonly HttpClients.DiscordWebhookClient _discordWebhookClient;
        private readonly ClusterQueryService _clusterQuery;

        public Normal(DiscordSocketClient client, HttpClients.DiscordWebhookClient discordWebhookClient, ClusterQueryService clusterQuery)
        {
            _client = client;
            _discordWebhookClient = discordWebhookClient;
            _clusterQuery = clusterQuery;
        }

        [Command("Ping")]
        [Summary("延遲檢測")]
        public async Task PingAsync()
        {
            await Context.Channel.SendConfirmAsync(":ping_pong: " + _client.Latency.ToString() + "ms");
        }


        [Command("Invite")]
        [Summary("取得邀請連結")]
        public async Task InviteAsync()
        {
            try
            {
                await (await Context.Message.Author.CreateDMChannelAsync())
                    .SendConfirmAsync("<https://discordapp.com/api/oauth2/authorize?client_id=" + _client.CurrentUser.Id + "&permissions=2416143425&scope=bot%20applications.commands>");
            }
            catch (Exception) { await Context.Channel.SendErrorAsync("無法私訊，請確認已開啟伺服器內成員私訊許可"); }
        }

        [Command("Status")]
        [Summary("顯示機器人目前的狀態")]
        [Alias("Stats")]
        public async Task StatusAsync()
        {
            EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor();
            embedBuilder.WithTitle("直播小幫手");
#if DEBUG || DEBUG_DONTREGISTERCOMMAND
            embedBuilder.Title += " (測試版)";
#endif

            embedBuilder.WithDescription($"建置版本 {Program.Version}");
            embedBuilder.AddField("作者", "孤之界#1121", true);
            embedBuilder.AddField("擁有者", $"{Bot.ApplicatonOwner.Username}#{Bot.ApplicatonOwner.Discriminator}", true);
            // 跨 shard：以合併快照（B1）彙總全叢集的伺服器數與成員數，而非只算本 shard
            var mergedGuilds = await _clusterQuery.ReadMergedGuildsAsync();
            embedBuilder.AddField("狀態", $"伺服器 {mergedGuilds.Count}\n服務成員數 {mergedGuilds.Sum((x) => x.MemberCount)}", false);
            embedBuilder.AddField("看過的直播數量", Utility.GetDbStreamCount(), true);
            embedBuilder.AddField("上線時間", $"{Bot.StopWatch.Elapsed:d\\天\\ hh\\:mm\\:ss}", false);

            await ReplyAsync(null, false, embedBuilder.Build());
        }
    }
}
