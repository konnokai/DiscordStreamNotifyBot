using Discord.Commands;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.SharedService.Cluster;

namespace DiscordStreamNotifyBot.Command.Admin
{
    public class Administration : TopLevelModule<AdministrationService>
    {
        private readonly DiscordSocketClient _client;
        private readonly MainDbService _dbService;
        private readonly ClusterQueryService _clusterQuery;

        public Administration(DiscordSocketClient discordSocketClient, MainDbService dbService, ClusterQueryService clusterQuery)
        {
            _client = discordSocketClient;
            _dbService = dbService;
            _clusterQuery = clusterQuery;
        }

        // 暫時移除，ChangeStatus 現在並非 Static
        //[RequireContext(ContextType.DM)]
        //[Command("UpdateStatus")]
        //[Summary("更新機器人的狀態\n參數: Guild, Member, Stream, StreamCount, Info")]
        //[Alias("UpStats")]
        //[RequireOwner]
        //public async Task UpdateStatusAsync([Summary("狀態")] string stats)
        //{
        //    switch (stats.ToLowerInvariant())
        //    {
        //        case "guild":
        //            Bot.Status = Bot.BotPlayingStatus.Guild;
        //            break;
        //        case "member":
        //            Bot.Status = Bot.BotPlayingStatus.Member;
        //            break;
        //        case "stream":
        //            Bot.Status = Bot.BotPlayingStatus.Stream;
        //            break;
        //        case "streamcount":
        //            Bot.Status = Bot.BotPlayingStatus.StreamCount;
        //            break;
        //        case "info":
        //            Bot.Status = Bot.BotPlayingStatus.Info;
        //            break;
        //        default:
        //            await Context.Channel.SendConfirmAsync(string.Format("找不到 {0} 狀態", stats));
        //            return;
        //    }

        //    Bot.ChangeStatus();
        //}

        [RequireContext(ContextType.DM)]
        [Command("ListServer")]
        [Summary("顯示所有的伺服器")]
        [Alias("LS")]
        [RequireOwner]
        public async Task ListServerAsync([Summary("頁數")] int page = 0)
        {
            // 跨 shard：合併各 shard 的伺服器快照（B1）
            var guilds = await _clusterQuery.ReadMergedGuildsAsync();

            await Context.SendPaginatedConfirmAsync(page, (cur) =>
            {
                EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor().WithTitle("目前所在的伺服器有");

                foreach (var item in guilds.Skip(cur * 5).Take(5))
                {
                    embedBuilder.AddField(item.Name, "Id: " + item.Id +
                        "\nOwner Id: " + item.OwnerId +
                        "\n人數: " + item.MemberCount.ToString());
                }

                embedBuilder.WithFooter($"總數量: {guilds.Count}（{Bot.TotalShardCount} shard）");
                return embedBuilder;
            }, guilds.Count, 5);
        }

        [RequireContext(ContextType.DM)]
        [Command("SearchServer")]
        [Summary("查詢伺服器")]
        [Alias("SS")]
        [RequireOwner]
        public async Task SearchServerAsync([Summary("關鍵字")] string keyword = "", [Summary("頁數")] int page = 0)
        {
            // 跨 shard：合併各 shard 的伺服器快照（B1）
            var guilds = await _clusterQuery.ReadMergedGuildsAsync();

            if (ulong.TryParse(keyword, out ulong guildId))
            {
                var guild = guilds.FirstOrDefault((x) => x.Id == guildId);
                if (guild != null)
                {
                    var embed = new EmbedBuilder().WithOkColor().AddField(guild.Name,
                        $"Id: {guild.Id}\n" +
                        $"擁有者Id: {guild.OwnerId}\n" +
                        $"人數: {guild.MemberCount}\n").Build();

                    await Context.Channel.SendMessageAsync(embed: embed);
                    return;
                }
            }

            var list = guilds.Where((x) => x.Name.Contains(keyword, StringComparison.InvariantCultureIgnoreCase)).ToList();
            if (!list.Any())
            {
                await Context.Channel.SendErrorAsync("該關鍵字無伺服器");
                return;
            }

            await Context.SendPaginatedConfirmAsync(page, (cur) =>
            {
                EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor().WithTitle($"查詢 `{keyword}` 後的伺服器有");

                foreach (var item in list.Skip(cur * 5).Take(5))
                {
                    embedBuilder.AddField(item.Name,
                        $"Id: {item.Id}\n" +
                        $"擁有者Id: {item.OwnerId}\n" +
                        $"人數: {item.MemberCount}\n");
                }

                embedBuilder.WithFooter($"總數量: {list.Count}");

                return embedBuilder;
            }, list.Count, 5, false);
        }

        [RequireContext(ContextType.DM)]
        [Command("Die")]
        [Summary("關閉機器人")]
        [Alias("Bye")]
        [RequireOwner]
        public async Task DieAsync()
        {
            await Context.Channel.SendConfirmAsync("關閉中（已廣播至所有 shard）");
            // 廣播至所有 Notifier shard（含本 shard），各自收到後設 Bot.IsDisconnect = true
            await _service.BroadcastShutdownAsync();
        }

        [RequireContext(ContextType.DM)]
        [Command("Leave")]
        [Summary("讓機器人離開指定的伺服器")]
        [RequireOwner]
        public async Task LeaveAsync([Summary("伺服器Id")] ulong gid = 0)
        {
            if (gid == 0) { await Context.Channel.SendErrorAsync("伺服器Id為空"); return; }

            // 目標伺服器只在單一 shard，廣播讓持有它的 shard 離開
            await _service.BroadcastLeaveGuildAsync(gid);
            await Context.Channel.SendConfirmAsync($"已廣播離開伺服器 `{gid}` 的要求（由持有該伺服器的 shard 執行）");
        }

        [RequireContext(ContextType.DM)]
        [Command("GetInviteURL")]
        [Summary("取得伺服器的邀請連結")]
        [Alias("invite")]
        [RequireOwner]
        public async Task GetInviteURLAsync([Summary("伺服器Id")] ulong gid = 0, [Summary("頻道Id")] ulong cid = 0)
        {
            if (gid == 0)
            {
                await Context.Channel.SendErrorAsync("伺服器Id為空");
                return;
            }

            try
            {
                // 目標伺服器只在單一 shard，向持有它的 shard 請求建立邀請／頻道清單（B2）
                string arg = cid == 0 ? gid.ToString() : $"{gid}:{cid}";
                var (responses, _, _) = await _clusterQuery.RequestAsync<ClusterQueryService.InviteResponse>(ClusterQueryService.ClusterQueryType.GetInviteUrl, arg);

                var resp = responses.FirstOrDefault();
                if (resp == null)
                {
                    await Context.Channel.SendErrorAsync($"伺服器 {gid} 不存在或目前無 shard 在線持有");
                    return;
                }

                if (cid == 0)
                {
                    IReadOnlyList<ClusterQueryService.ChannelInfo> channels = resp.TextChannels ?? new List<ClusterQueryService.ChannelInfo>();

                    await Context.SendPaginatedConfirmAsync(0, (cur) =>
                    {
                        EmbedBuilder embedBuilder = new EmbedBuilder()
                           .WithOkColor()
                           .WithTitle($"以下為 {gid} 所有的文字頻道")
                           .WithDescription(string.Join('\n', channels.Skip(cur * 20).Take(20).Select((x) => x.Id + " / " + x.Name)));

                        return embedBuilder;
                    }, channels.Count, 20);
                }
                else
                {
                    if (resp.InviteUrl == "__NOPERM__")
                        await Context.Channel.SendErrorAsync("缺少邀請權限");
                    else if (string.IsNullOrEmpty(resp.InviteUrl))
                        await Context.Channel.SendErrorAsync("無法建立邀請（頻道不存在或建立失敗）");
                    else
                        await Context.Channel.SendConfirmAsync(resp.InviteUrl);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        [RequireContext(ContextType.DM)]
        [Command("GuildInfo")]
        [Summary("顯示伺服器資訊")]
        [Alias("ginfo")]
        [RequireOwner]
        public async Task GuildInfo(ulong gid = 0)
        {
            try
            {
                if (gid == 0)
                {
                    await Context.Channel.SendErrorAsync("GuildId 不可為空").ConfigureAwait(false);
                    return;
                }

                // 伺服器即時資訊（名稱/人數/頻道名）只有持有它的 shard 有，向該 shard 請求（B2）；DB 區塊維持本機查（共用 DB）
                var (infoResponses, _, _) = await _clusterQuery.RequestAsync<ClusterQueryService.GuildInfoResponse>(ClusterQueryService.ClusterQueryType.GuildInfo, gid.ToString());
                var info = infoResponses.FirstOrDefault();
                if (info == null)
                {
                    await Context.Channel.SendErrorAsync("找不到指定的伺服器（無 shard 在線持有）").ConfigureAwait(false);
                    return;
                }

                var channels = info.Channels ?? new Dictionary<ulong, string>();

                string result = $"伺服器名稱: **{info.Name}**\n" +
                            $"伺服器Id: {gid}\n" +
                            $"擁有者Id: {info.OwnerId}\n" +
                            $"人數: {info.MemberCount}\n";

                using (var db = _dbService.GetDbContext())
                {
                    var guildConfig = await db.GuildConfig.AsNoTracking().FirstOrDefaultAsync((x) => x.GuildId == gid);
                    if (guildConfig != null && guildConfig.LogMemberStatusChannelId != 0)
                    {
                        if (channels.TryGetValue(guildConfig.LogMemberStatusChannelId, out var logChannelName))
                            result += $"伺服器會限記錄頻道: {logChannelName} ({guildConfig.LogMemberStatusChannelId})\n";
                        else
                            result += $"伺服器會限記錄頻道: (不存在) {guildConfig.LogMemberStatusChannelId}\n";
                    }

                    var youtubeChannelSpiders = db.YoutubeChannelSpider.AsNoTracking().Where((x) => x.GuildId == gid);
                    if (youtubeChannelSpiders.Any())
                    {
                        bool isTooMany = youtubeChannelSpiders.Count() > 20;
                        if (isTooMany)
                        {
                            result += $"設定的 YouTube 爬蟲: \n```{string.Join('\n', youtubeChannelSpiders.Take(20).Select((x) => $"{x.ChannelTitle}: {x.ChannelId}"))}\n(還有 {youtubeChannelSpiders.Count() - 20} 個爬蟲...)```\n";
                        }
                        else
                        {
                            result += $"設定的 YouTube 爬蟲: \n```{string.Join('\n', youtubeChannelSpiders.Select((x) => $"{x.ChannelTitle}: {x.ChannelId}"))}```\n";
                        }
                    }

                    var youtubeChannelList = db.NoticeYoutubeStreamChannel.AsNoTracking().Where((x) => x.GuildId == gid);
                    if (youtubeChannelList.Any())
                    {
                        List<string> channelListResult = new List<string>();

                        foreach (var item in youtubeChannelList)
                        {
                            if (channels.TryGetValue(item.DiscordNoticeVideoChannelId, out var noticeChannelName))
                                channelListResult.Add($"{noticeChannelName}: {item.YouTubeChannelId}");
                            else
                                channelListResult.Add($"(不存在) {item.DiscordNoticeVideoChannelId}: {item.YouTubeChannelId}");
                        }

                        bool isTooMany = channelListResult.Count > 20;
                        if (isTooMany)
                        {
                            result += $"設定 YouTube 通知的頻道: \n```{string.Join('\n', channelListResult.Take(20))}\n(還有 {channelListResult.Count - 20} 個爬蟲...)```\n";
                        }
                        else
                        {
                            result += $"設定 YouTube 通知的頻道: \n```{string.Join('\n', channelListResult)}```\n";
                        }
                    }

                    var memberChcekList = db.GuildYoutubeMemberConfig.AsNoTracking().Where((x) => x.GuildId == gid);
                    if (memberChcekList.Any())
                    {
                        result += $"設定會限的頻道: \n```{string.Join('\n', memberChcekList.Select((x) => $"{x.MemberCheckChannelTitle}: {x.MemberCheckGrantRoleId}"))}```\n";
                    }

                    var twitchSpiders = db.TwitchSpider.AsNoTracking().Where((x) => x.GuildId == gid);
                    if (twitchSpiders.Any())
                    {
                        result += $"設定的 Twitch 爬蟲: \n```{string.Join('\n', twitchSpiders.Select((x) => $"{x.UserName}: {x.UserLogin}"))}```\n";
                    }

                    var noticeTwitchStreamChannels = db.NoticeTwitchStreamChannels.AsNoTracking().Where((x) => x.GuildId == gid);
                    if (noticeTwitchStreamChannels.Any())
                    {
                        List<string> channelListResult = new List<string>();

                        foreach (var item in noticeTwitchStreamChannels)
                        {
                            if (channels.TryGetValue(item.DiscordChannelId, out var noticeChannelName))
                                channelListResult.Add($"{noticeChannelName}: {item.NoticeTwitchUserId}");
                            else
                                channelListResult.Add($"(不存在) {item.DiscordChannelId}: {item.NoticeTwitchUserId}");
                        }

                        result += $"設定 Twitch 通知的頻道: \n```{string.Join('\n', channelListResult)}```\n";
                    }

                    await Context.Channel.SendConfirmAsync(result).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        [RequireContext(ContextType.DM)]
        [Command("UserInfo")]
        [Summary("顯示使用者資訊")]
        [Alias("uinfo")]
        [RequireOwner]
        public async Task UserInfo(ulong uid = 0)
        {
            try
            {
                if (uid == 0)
                {
                    await Context.Channel.SendErrorAsync("UserId 不可為空").ConfigureAwait(false);
                    return;
                }

                var user = await _client.Rest.GetUserAsync(uid);
                if (user == null)
                {
                    await Context.Channel.SendErrorAsync("找不到指定的使用者").ConfigureAwait(false);
                    return;
                }

                string result = $"使用者名稱: **{user.Username}**\n" +
                            $"使用者 Id: {user.Id}\n";

                // 使用者可能在任一 shard 的伺服器，需即時向各 shard 查詢（B2）
                var (responses, responded, expected) = await _clusterQuery.RequestAsync<ClusterQueryService.UserInfoResponse>(ClusterQueryService.ClusterQueryType.UserInfo, uid.ToString());
                var guildList = responses.SelectMany((r) => r.GuildNames).ToList();

                if (guildList.Any())
                {
                    result += $"共同的伺服器: \n```{string.Join('\n', guildList)}```";
                }

                result += $"\n（{responded}/{expected} shard 已回應）";

                await Context.Channel.SendConfirmAsync(result).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        [RequireContext(ContextType.DM)]
        [Command("AddOfficialList")]
        [Summary("新增官方伺服器白名單")]
        [Alias("aol")]
        [RequireOwner]
        public async Task AddOfficialListAsync(ulong guildId)
        {
            if (Utility.OfficialGuildList.Contains(guildId))
            {
                await Context.Channel.SendErrorAsync("此伺服器已存在於名單內");
                return;
            }

            Utility.OfficialGuildList.Add(guildId);

            if (await _service.SaveAndBroadcastOfficialGuildListAsync())
            {
                await Context.Channel.SendConfirmAsync($"已添加 {guildId} 至官方伺服器名單內");
            }
            else
            {
                await Context.Channel.SendErrorAsync($"添加 {guildId} 至官方伺服器名單內失敗");
            }
        }

        [RequireContext(ContextType.DM)]
        [Command("RemoveOfficialList")]
        [Summary("移除官方伺服器白名單")]
        [Alias("rol")]
        [RequireOwner]
        public async Task RemoveOfficialListAsync(ulong guildId)
        {
            if (!Utility.OfficialGuildList.Contains(guildId))
            {
                await Context.Channel.SendErrorAsync("此伺服器不存在於名單內");
                return;
            }

            Utility.OfficialGuildList.Remove(guildId);

            if (await _service.SaveAndBroadcastOfficialGuildListAsync())
            {
                await Context.Channel.SendConfirmAsync($"已從官方伺服器名單內移除 {guildId}");
            }
            else
            {
                await Context.Channel.SendErrorAsync($"從官方伺服器名單內移除 {guildId} 失敗");
            }
        }

        [RequireContext(ContextType.DM)]
        [Command("ListOfficialList")]
        [Summary("顯示官方伺服器白名單列表")]
        [Alias("lol")]
        [RequireOwner]
        public async Task ListOfficialListAsync(int page = 0)
        {
            if (Utility.OfficialGuildList.Count == 0)
            {
                await Context.Channel.SendErrorAsync("官方伺服器白名單為空");
                return;
            }

            // 白名單為全域（Redis），成員/名稱狀態需跨 shard 判定：任何 shard 都未持有者才標「已離開」（B1）
            var mergedGuilds = await _clusterQuery.ReadMergedGuildsAsync();
            var guildNameById = new Dictionary<ulong, string>();
            foreach (var g in mergedGuilds)
                guildNameById[g.Id] = g.Name;

            List<string> officialList = new();
            foreach (var item in Utility.OfficialGuildList)
            {
                if (guildNameById.TryGetValue(item, out var name))
                    officialList.Add($"{name} `({item})`");
                else
                    officialList.Add($"*已離開的伺服器* `({item})`");
            }

            if (page <= 0)
                page = 0;

            await Context.SendPaginatedConfirmAsync(page, (page) => (
                new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle("官方伺服器白名單清單")
                    .WithDescription(string.Join('\n', officialList.Skip(page * 20).Take(20)))),
                officialList.Count, 20);
        }

        [RequireContext(ContextType.DM)]
        [Command("ListNoNotifyGuild")]
        [Summary("顯示未設定通知的伺服器列表")]
        [Alias("lnng")]
        [RequireOwner]
        public async Task ListNoNotifyGuildAsync(int page = 0)
        {
            try
            {
                // 跨 shard：合併全叢集伺服器後過濾未設定通知者（B1 + DB）
                var merged = await _clusterQuery.ReadMergedGuildsAsync();
                var guilds = _clusterQuery.FilterNoNotifyGuilds(merged);

                File.WriteAllText(Utility.GetDataFilePath("NoNotifyGuildList.txt"), string.Join('\n', guilds.Select(g => $"{g.Name} | {g.Id} | {g.MemberCount} 人")));

                if (page <= 0)
                    page = 0;

                await Context.SendPaginatedConfirmAsync(page, (page) =>
                    new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle("未設定通知的伺服器列表")
                        .WithDescription(string.Join('\n', guilds.Skip(page * 20).Take(20).Select(g => $"{g.Name} | {g.Id} | {g.MemberCount} 人"))),
                    guilds.Count, 20);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "ListNoNotifyGuild Error");
                await Context.Channel.SendErrorAsync("取得未設定通知的伺服器列表失敗，請查看日誌").ConfigureAwait(false);
            }
        }


        [RequireContext(ContextType.DM)]
        [Command("LeaveNoNotifyGuild")]
        [Summary("離開未設定通知的伺服器")]
        [Alias("leavenng")]
        [RequireOwner]
        public async Task LeaveNoNotifyGuildAsync()
        {
            try
            {
                await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);

                // 各 shard 離開自己持有的未設定通知伺服器並回報數量（A 廣播 + 數量彙總）
                var (total, responded, expected) = await _service.BroadcastLeaveNoNotifyAsync();

                if (total == 0)
                {
                    await Context.Channel.SendErrorAsync("沒有未設定通知的伺服器");
                    return;
                }

                await Context.Channel.SendConfirmAsync($"已廣播離開 {total} 個未設定通知的伺服器（{responded}/{expected} shard 回應，實際離開於背景進行）").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "LeaveNoNotifyGuild Error");
                await Context.Channel.SendErrorAsync("離開未設定通知的伺服器失敗，請查看日誌").ConfigureAwait(false);
            }
        }
    }
}