using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Interaction.Attribute;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService.Cluster;

namespace DiscordStreamNotifyBot.Interaction.Twitch
{
    [RequireContext(ContextType.Guild)]
    [Group("twitch-spider", "Twitch 爬蟲設定")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    public class TwitchSpider : TopLevelModule<SharedService.Twitch.TwitchService>
    {
        private readonly MainDbService _dbService;
        private readonly ClusterQueryService _clusterQuery;
        private readonly BotConfig _botConfig;
        public class GuildTwitchSpiderAutocompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                return await Task.Run(async () =>
                {
                    using var db = Bot.DbService.GetDbContext();
                    IQueryable<DataBase.Table.TwitchSpider> channelList;

                    if (autocompleteInteraction.User.Id == Bot.ApplicatonOwner.Id)
                    {
                        channelList = db.TwitchSpider;
                    }
                    else
                    {
                        if (!await db.TwitchSpider.AsNoTracking().AnyAsync((x) => x.GuildId == autocompleteInteraction.GuildId))
                            return AutocompletionResult.FromSuccess();

                        channelList = db.TwitchSpider.AsNoTracking().Where((x) => x.GuildId == autocompleteInteraction.GuildId);
                    }

                    try
                    {
                        string value = autocompleteInteraction.Data.Current.Value?.ToString();
                        var candidates = channelList.Select(item =>
                            new AutocompleteCandidate(item.UserName, item.UserId, item.UserLogin));
                        var results = AutocompleteSearch.Filter(candidates, value)
                            .Select(item => new AutocompleteResult(item.Name, item.Value));
                        return AutocompletionResult.FromSuccess(results);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"GuildTwitchSpiderAutocompleteHandler - {ex}");
                        return AutocompletionResult.FromSuccess();
                    }
                });
            }
        }

        public TwitchSpider(MainDbService dbService, ClusterQueryService clusterQuery, BotConfig botConfig)
        {
            _dbService = dbService;
            _clusterQuery = clusterQuery;
            _botConfig = botConfig;
        }

        [CommandSummary("新增 Twitch 頻道爬蟲\n" +
           "伺服器需大於 200 人才可使用\n" +
           "未來會根據情況增減可新增的頻道數量\n" +
           "如有任何需要請向擁有者詢問")]
        [CommandExample("998rrr", "https://twitch.tv/998rrr")]
        [SlashCommand("add", "新增 Twitch 頻道爬蟲")]
        public async Task AddChannelSpider([Summary("channel", "頻道網址")] string twitchUrl)
        {
            if (!_service.IsEnable)
            {
                await SendLocalizedErrorAsync("Errors.FeatureDisabled").ConfigureAwait(false);
                return;
            }

            await DeferAsync(true).ConfigureAwait(false);

            var userData = await _service.GetUserAsync(twitchUserLogin: _service.GetUserLoginByUrl(twitchUrl));
            if (userData == null)
            {
                await SendLocalizedErrorAsync("Twitch.Errors.UserNotFound", true);
                return;
            }

            using (var db = _dbService.GetDbContext())
            {
                bool hasGeneralEligibility = Context.User.Id == Bot.ApplicatonOwner.Id ||
                    DiscordStreamNotifyBot.Utility.OfficialGuildContains(Context.Guild.Id) ||
                    Context.Guild.MemberCount >= 200;
                bool hasOAuthEligibility = await db.TwitchBroadcasterAuthorization.AsNoTracking().AnyAsync(x =>
                    x.RevokedAt == null &&
                    x.ClientId == _botConfig.TwitchClientId &&
                    x.DiscordUserId == Context.User.Id &&
                    x.TwitchUserId == userData.Id);
                if (!hasGeneralEligibility && !hasOAuthEligibility)
                {
                    string locale = await GetLocaleAsync(true);
                    string contactPath = CommandDisplayResolver.GetCommandPath(locale, "utility", "send-message-to-bot-owner");
                    await SendLocalizedErrorAsync("TwitchSpider.MemberRequirement", true, true,
                        200, Context.Guild.MemberCount, contactPath).ConfigureAwait(false);
                    return;
                }

                bool usedOAuthBypass = !hasGeneralEligibility && hasOAuthEligibility;
                var guildConfig = db.GuildConfig.AsNoTracking().FirstOrDefault((x) => x.GuildId == Context.Guild.Id);
                int maxCount = guildConfig != null && guildConfig.MaxTwitchSpiderCount > 0
                    ? (int)guildConfig.MaxTwitchSpiderCount
                    : 3;
                bool reachedSpiderLimit = !DiscordStreamNotifyBot.Utility.OfficialGuildContains(Context.Guild.Id) &&
                    db.TwitchSpider.AsNoTracking().Count((x) => x.GuildId == Context.Guild.Id) >= maxCount;

                if (db.TwitchSpider.Any((x) => x.UserId == userData.Id))
                {
                    var item = db.TwitchSpider.FirstOrDefault((x) => x.UserId == userData.Id);
                    bool isGuildExist = true;
                    string guild = "";
                    string existingResponseLocale = await GetLocaleAsync(false);

                    // 跨 shard：用合併快照（B1）判定原持有伺服器是否仍在叢集，避免把別 shard 持有的伺服器誤判為已退出而搶走爬蟲
                    if (item.GuildId == 0)
                    {
                        guild = BotLocalizer.Get("Common.BotOwner", existingResponseLocale);
                    }
                    else
                    {
                        var guildMap = await _clusterQuery.GetGuildNameMapAsync();
                        if (guildMap.TryGetValue(item.GuildId, out var ownerName))
                        {
                            guild = ownerName;
                        }
                        else
                        {
                            if (reachedSpiderLimit)
                            {
                                await SendLocalizedErrorAsync("Spider.LimitReachedShort", true, true, maxCount, "Twitch")
                                    .ConfigureAwait(false);
                                return;
                            }

                            isGuildExist = false;

                            ulong originalGuildId = item.GuildId;
                            item.GuildId = Context.Guild.Id;
                            db.TwitchSpider.Update(item);
                            db.SaveChanges();
                            await PublishReconcileRequestedAsync(item.UserId, "spider_owner_changed");

                            try
                            {
                                await (await Bot.ApplicatonOwner.CreateDMChannelAsync())
                                    .SendMessageAsync(embed: new EmbedBuilder()
                                        .WithOkColor()
                                        .WithTitle("已更新 Twitch 爬蟲的持有伺服器")
                                        .AddField("頻道", Format.Url(item.UserName, $"https://twitch.tv/{userData.Login}"), false)
                                        .AddField("原伺服器", originalGuildId, false)
                                        .AddField("新伺服器", $"{Context.Guild.Name} ({Context.Guild.Id})", false).Build());
                            }
                            catch (Exception ex) { Log.Error(ex.Demystify(), "Update Twitch Spider GuildId Error"); }
                        }
                    }

                    string addPath = CommandDisplayResolver.GetCommandPath(existingResponseLocale, "twitch", "add");
                    await SendLocalizedConfirmAsync("Spider.AlreadyExists", true, false,
                        userData.DisplayName, addPath, userData.Login,
                        isGuildExist ? BotLocalizer.Format("Spider.OwnerHint", existingResponseLocale, guild) : "").ConfigureAwait(false);
                    return;
                }

                if (reachedSpiderLimit)
                {
                    string locale = await GetLocaleAsync(true);
                    string contactPath = CommandDisplayResolver.GetCommandPath(locale, "utility", "send-message-to-bot-owner");
                    await SendLocalizedErrorAsync("Spider.LimitReached", true, true,
                        maxCount, "Twitch", contactPath).ConfigureAwait(false);
                    return;
                }

                var spider = new DataBase.Table.TwitchSpider()
                {
                    GuildId = Context.Guild.Id,
                    UserId = userData.Id,
                    UserLogin = userData.Login,
                    UserName = userData.DisplayName,
                    ProfileImageUrl = userData.ProfileImageUrl,
                    OfflineImageUrl = userData.OfflineImageUrl
                };

                if (Context.User.Id == Bot.ApplicatonOwner.Id && !await PromptUserConfirmAsync("Spider.UseForCurrentGuildPrompt"))
                    spider.GuildId = 0;

                db.TwitchSpider.Add(spider);
                db.SaveChanges();
                await PublishReconcileRequestedAsync(spider.UserId,
                    usedOAuthBypass ? "oauth_bypass_addition" : "spider_added");

                string responseLocale = await GetLocaleAsync(true);
                string notificationPath = CommandDisplayResolver.GetCommandPath(responseLocale, "twitch", "add");
                await SendLocalizedConfirmAsync("Spider.Added", true, true,
                    userData.DisplayName, notificationPath, userData.Login).ConfigureAwait(false);

                try
                {
                    await (await Bot.ApplicatonOwner.CreateDMChannelAsync()).SendMessageAsync(embed: new EmbedBuilder()
                            .WithOkColor()
                            .WithTitle("已新增 Twitch 頻道爬蟲")
                            .AddField("頻道", Format.Url(userData.DisplayName, $"https://twitch.tv/{userData.Login}"), false)
                            .AddField("伺服器", spider.GuildId != 0 ? $"{Context.Guild.Name} ({Context.Guild.Id})" : "擁有者", false)
                            .AddField("執行者", $"{Context.User.GlobalName} ({Context.User} / {Context.User.Id})", false)
                            .AddField("是否使用 OAuth 忽略人數要求", usedOAuthBypass ? "是" : "否", false)
                            .AddField("頻道狀態", "普通", true)
                            .AddField("頻道錄影", "關閉", true).Build(),
                        components: new ComponentBuilder()
                            .WithButton("切換頻道狀態", $"spider_twitch:warning:{userData.Id}", ButtonStyle.Danger)
                            .WithButton("切換頻道錄影", $"spider_twitch:record:{userData.Id}", ButtonStyle.Success).Build());
                }
                catch (Exception ex) { Log.Error(ex.ToString()); }
            }
        }

        [CommandSummary("移除 Twitch 頻道檢測爬蟲\n" +
            "爬蟲必須由本伺服器新增才可移除")]
        [CommandExample("998rrr", "https://twitch.tv/998rrr")]
        [SlashCommand("remove", "移除 Twitch 頻道爬蟲")]
        public async Task RemoveChannelSpider([Summary("channel", "頻道網址"), Autocomplete(typeof(GuildTwitchSpiderAutocompleteHandler))] string twitchId)
        {
            await DeferAsync(true).ConfigureAwait(false);

            DataBase.Table.TwitchSpider twitchSpider = null;
            using (var db = _dbService.GetDbContext())
            {
                if (!db.TwitchSpider.Any((x) => x.UserId == twitchId))
                {
                    await SendLocalizedErrorAsync("Spider.NotConfigured", true, true, twitchId).ConfigureAwait(false);
                    return;
                }

                if (Context.Interaction.User.Id != Bot.ApplicatonOwner.Id && !db.TwitchSpider.Any((x) => x.UserId == twitchId && x.GuildId == Context.Guild.Id))
                {
                    await SendLocalizedErrorAsync("Spider.NotOwnedByGuild", true).ConfigureAwait(false);
                    return;
                }

                twitchSpider = db.TwitchSpider.First((x) => x.UserId == twitchId);
                db.TwitchSpider.Remove(twitchSpider);
                db.SaveChanges();
            }

            await PublishReconcileRequestedAsync(twitchId, "spider_removed");

            await SendLocalizedConfirmAsync("Spider.Removed", true, false, twitchSpider?.UserName).ConfigureAwait(false);

            try
            {
                await (await Bot.ApplicatonOwner.CreateDMChannelAsync()).SendMessageAsync(embed: new EmbedBuilder()
                    .WithErrorColor()
                    .WithTitle("已移除 Twitch 頻道爬蟲")
                    .AddField("頻道", Format.Url(twitchSpider?.UserName, $"https://twitch.tv/{twitchSpider.UserLogin}"), false)
                    .AddField("伺服器", $"{Context.Guild.Name} ({Context.Guild.Id})", false)
                    .AddField("執行者", $"{Context.User.Username} ({Context.User.Id})", false).Build());
            }
            catch (Exception ex) { Log.Error(ex.ToString()); }
        }

        [SlashCommand("list", "顯示已加入爬蟲檢測的頻道")]
        public async Task ListChannelSpider([Summary("page", "頁數")] int page = 0)
        {
            if (page < 0) page = 0;
            string locale = await GetLocaleAsync(false);

            using (var db = _dbService.GetDbContext())
            {
                try
                {
                    // 跨 shard：以合併快照（B1）解析持有伺服器名稱，別 shard 持有的伺服器不會被誤標為已退出
                    var guildMap = await _clusterQuery.GetGuildNameMapAsync();
                    var list = db.TwitchSpider.Where((x) => !x.IsWarningUser).Select((x) =>
                        BotLocalizer.Format("Spider.ListEntry", locale,
                            Format.Url(x.UserName, $"https://twitch.tv/{x.UserLogin}"),
                            x.GuildId == 0 ? BotLocalizer.Get("Common.BotOwner", locale) :
                            (guildMap.ContainsKey(x.GuildId) ? guildMap[x.GuildId] : BotLocalizer.Get("Common.LeftGuild", locale))));
                    int warningChannelNum = db.TwitchSpider.Count((x) => x.IsWarningUser);

                    await Context.SendPaginatedConfirmAsync(BotLocalizer, locale, page, page =>
                    {
                        return new EmbedBuilder()
                            .WithOkColor()
                            .WithTitle(BotLocalizer.Get("TwitchSpider.ListTitle", locale))
                            .WithDescription(string.Join('\n', list.Skip(page * 20).Take(20)))
                            .WithFooter(BotLocalizer.Format("Spider.ListFooter", locale,
                                Math.Min(list.Count(), (page + 1) * 20), list.Count(), warningChannelNum));
                    }, list.Count(), 10, false).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), $"Twitch-Spider-List Error");
                    await SendLocalizedErrorAsync("Errors.OperationFailed", false, true);
                }
            }
        }

        [SlashCommand("list-not-trusted", "顯示已加入但為警告狀態的爬蟲檢測頻道 (本清單可能內含中之人或前世的頻道)")]
        public async Task ListNotTrustedChannelSpider([Summary("page", "頁數")] int page = 0)
        {
            if (page < 0) page = 0;
            string locale = await GetLocaleAsync(false);

            using (var db = _dbService.GetDbContext())
            {
                // 跨 shard：以合併快照（B1）解析持有伺服器名稱，別 shard 持有的伺服器不會被誤標為已退出
                var guildMap = await _clusterQuery.GetGuildNameMapAsync();
                var list = db.TwitchSpider.Where((x) => x.IsWarningUser).Select((x) =>
                    BotLocalizer.Format("Spider.ListEntry", locale,
                        Format.Url(x.UserName, $"https://twitch.tv/{x.UserLogin}"),
                        x.GuildId == 0 ? BotLocalizer.Get("Common.BotOwner", locale) :
                        (guildMap.ContainsKey(x.GuildId) ? guildMap[x.GuildId] : BotLocalizer.Get("Common.LeftGuild", locale))));

                await Context.SendPaginatedConfirmAsync(BotLocalizer, locale, page, page =>
                {
                    return new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle(BotLocalizer.Get("Spider.WarningListTitle", locale))
                        .WithDescription(string.Join('\n', list.Skip(page * 20).Take(20)))
                        .WithFooter(BotLocalizer.Format("Common.ChannelCountFooter", locale,
                            Math.Min(list.Count(), (page + 1) * 20), list.Count()));
                }, list.Count(), 10, false, true).ConfigureAwait(false);
            }
        }

        internal static Task PublishReconcileRequestedAsync(string twitchUserId, string reason)
        {
            return Bot.RedisSub.PublishAsync(
                new RedisChannel(RedisChannels.Twitch.ReconcileRequested, RedisChannel.PatternMode.Literal),
                JsonConvert.SerializeObject(new { TwitchUserId = twitchUserId, Reason = reason }));
        }
    }
}
