using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Interaction.Attribute;
using DiscordStreamNotifyBot.SharedService.Cluster;

namespace DiscordStreamNotifyBot.Interaction.TwitCasting
{
    [RequireContext(ContextType.Guild)]
    [Group("twitcasting-spider", "TwitCasting 爬蟲設定")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    public class TwitcastingSpider : TopLevelModule<SharedService.Twitcasting.TwitcastingService>
    {
        private readonly MainDbService _dbService;
        private readonly ClusterQueryService _clusterQuery;
        public class GuildTwitCastingSpiderAutocompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                return await Task.Run(async () =>
                {
                    using var db = Bot.DbService.GetDbContext();
                    IQueryable<DataBase.Table.TwitcastingSpider> channelList;

                    if (autocompleteInteraction.User.Id == Bot.ApplicatonOwner.Id)
                    {
                        channelList = db.TwitcastingSpider;
                    }
                    else
                    {
                        if (!(await db.TwitcastingSpider.AsNoTracking().AnyAsync((x) => x.GuildId == autocompleteInteraction.GuildId)))
                            return AutocompletionResult.FromSuccess();

                        channelList = db.TwitcastingSpider.AsNoTracking().Where((x) => x.GuildId == autocompleteInteraction.GuildId);
                    }

                    try
                    {
                        string value = autocompleteInteraction.Data.Current.Value?.ToString();
                        var candidates = channelList.Select(item =>
                            new AutocompleteCandidate(item.ChannelTitle, item.ScreenId));
                        var results = AutocompleteSearch.Filter(candidates, value)
                            .Select(item => new AutocompleteResult(item.Name, item.Value));
                        return AutocompletionResult.FromSuccess(results);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"GuildTwitCastingSpiderAutocompleteHandler - {ex}");
                        return AutocompletionResult.FromSuccess();
                    }
                });
            }
        }

        public TwitcastingSpider(MainDbService dbService, ClusterQueryService clusterQuery)
        {
            _dbService = dbService;
            _clusterQuery = clusterQuery;
        }

        [RequireGuildMemberCount(500)]
        [CommandSummary("新增 TwitCasting 頻道檢測爬蟲\n" +
           "伺服器需大於 500 人才可使用\n" +
           "未來會根據情況增減可新增的頻道數量\n" +
           "如有任何需要請向擁有者詢問")]
        [CommandExample("nana_kaguraaa", "https://twitcasting.tv/nana_kaguraaa")]
        [SlashCommand("add", "新增 TwitCasting 頻道檢測爬蟲")]
        public async Task AddChannelSpider([Summary("channel", "頻道網址")] string channelUrl)
        {
            if (!_service.IsEnable)
            {
                await SendLocalizedErrorAsync("Errors.FeatureDisabled").ConfigureAwait(false);
                return;
            }

            await DeferAsync(true).ConfigureAwait(false);

            var channelData = await _service.GetChannelNameAndTitleAsync(channelUrl);
            if (channelData == null)
            {
                await SendLocalizedErrorAsync("Twitcasting.Errors.UserNotFound", true);
                return;
            }

            using (var db = _dbService.GetDbContext())
            {
                if (await db.TwitcastingSpider.AnyAsync((x) => x.ScreenId == channelData.ScreenId))
                {
                    var item = await db.TwitcastingSpider.FirstOrDefaultAsync((x) => x.ScreenId == channelData.ScreenId);
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
                            isGuildExist = false;

                            try
                            {
                                await (await Bot.ApplicatonOwner.CreateDMChannelAsync())
                                    .SendMessageAsync(embed: new EmbedBuilder()
                                        .WithOkColor()
                                        .WithTitle("已更新 TwitCasting 爬蟲的持有伺服器")
                                        .AddField("頻道", Format.Url(item.ChannelTitle, $"https://twitcasting.tv/{channelData.ScreenId}"), false)
                                        .AddField("原伺服器", item.GuildId, false)
                                        .AddField("新伺服器", $"{Context.Guild.Name} ({Context.Guild.Id})", false).Build());
                            }
                            catch (Exception ex) { Log.Error(ex.ToString()); }

                            item.GuildId = Context.Guild.Id;
                            db.TwitcastingSpider.Update(item);
                            await db.SaveChangesAsync();
                        }
                    }

                    string addPath = CommandDisplayResolver.GetCommandPath(existingResponseLocale, "twitcasting", "add");
                    await SendLocalizedConfirmAsync("Spider.AlreadyExists", true, false,
                        channelData.Name, addPath, channelData.ScreenId,
                        isGuildExist ? BotLocalizer.Format("Spider.OwnerHint", existingResponseLocale, guild) : "").ConfigureAwait(false);
                    return;
                }

                // 取得最大數量設定
                var guildConfig = db.GuildConfig.AsNoTracking().FirstOrDefault((x) => x.GuildId == Context.Guild.Id);
                int maxCount = 2;
                if (guildConfig != null && guildConfig.MaxTwitcastingSpiderCount > 0)
                    maxCount = (int)guildConfig.MaxTwitcastingSpiderCount;

                if (!DiscordStreamNotifyBot.Utility.OfficialGuildContains(Context.Guild.Id) && db.TwitcastingSpider.AsNoTracking().Count((x) => x.GuildId == Context.Guild.Id) >= maxCount)
                {
                    string locale = await GetLocaleAsync(true);
                    string contactPath = CommandDisplayResolver.GetCommandPath(locale, "utility", "send-message-to-bot-owner");
                    await SendLocalizedErrorAsync("Spider.LimitReached", true, true,
                        maxCount, "TwitCasting", contactPath).ConfigureAwait(false);
                    return;
                }

                var spider = new DataBase.Table.TwitcastingSpider()
                {
                    GuildId = Context.Guild.Id,
                    ChannelId = channelData.Id,
                    ScreenId = channelData.ScreenId,
                    ChannelTitle = channelData.Name
                };

                if (Context.User.Id == Bot.ApplicatonOwner.Id && !await PromptUserConfirmAsync("Spider.UseForCurrentGuildPrompt"))
                    spider.GuildId = 0;

                await db.TwitcastingSpider.AddAsync(spider);
                await db.SaveChangesAsync();

                string responseLocale = await GetLocaleAsync(true);
                string notificationPath = CommandDisplayResolver.GetCommandPath(responseLocale, "twitcasting", "add");
                await SendLocalizedConfirmAsync("Spider.Added", true, true,
                    channelData.Name, notificationPath, channelData.ScreenId).ConfigureAwait(false);

                try
                {
                    await (await Bot.ApplicatonOwner.CreateDMChannelAsync()).SendMessageAsync(embed: new EmbedBuilder()
                            .WithOkColor()
                            .WithTitle("已新增 TwitCasting 頻道爬蟲")
                            .AddField("頻道", Format.Url(channelData.Name, $"https://twitcasting.tv/{channelData.ScreenId}"), false)
                            .AddField("伺服器", spider.GuildId != 0 ? $"{Context.Guild.Name} ({Context.Guild.Id})" : "擁有者", false)
                            .AddField("執行者", $"{Context.User.Username} ({Context.User.Id})", false)
                            .AddField("頻道狀態", "普通", true)
                            .AddField("頻道錄影", "關閉", true).Build(),
                        components: new ComponentBuilder()
                            .WithButton("切換頻道狀態", $"spider_tc:warning:{channelData.ScreenId}", ButtonStyle.Danger)
                            .WithButton("切換頻道錄影", $"spider_tc:record:{channelData.ScreenId}", ButtonStyle.Success).Build());
                }
                catch (Exception ex) { Log.Error(ex.ToString()); }
            }
        }

        [CommandSummary("移除 TwitCasting 頻道檢測爬蟲\n" +
            "爬蟲必須由本伺服器新增才可移除")]
        [CommandExample("nana_kaguraaa", "https://twitcasting.tv/nana_kaguraaa")]
        [SlashCommand("remove", "移除 TwitCasting 頻道檢測爬蟲")]
        public async Task RemoveChannelSpider([Summary("channel", "頻道網址"), Autocomplete(typeof(GuildTwitCastingSpiderAutocompleteHandler))] string channelUrl)
        {
            await DeferAsync(true).ConfigureAwait(false);

            var channelData = await _service.GetChannelNameAndTitleAsync(channelUrl);
            if (channelData == null)
            {
                await SendLocalizedErrorAsync("Twitcasting.Errors.UserNotFound", true);
                return;
            }

            using (var db = _dbService.GetDbContext())
            {
                if (!db.TwitcastingSpider.Any((x) => x.ScreenId == channelData.ScreenId))
                {
                    await SendLocalizedErrorAsync("Spider.NotConfigured", true, true, channelData.Name).ConfigureAwait(false);
                    return;
                }

                if (Context.Interaction.User.Id != Bot.ApplicatonOwner.Id && !db.TwitcastingSpider.Any((x) => x.ScreenId == channelData.ScreenId && x.GuildId == Context.Guild.Id))
                {
                    await SendLocalizedErrorAsync("Spider.NotOwnedByGuild", true).ConfigureAwait(false);
                    return;
                }

                db.TwitcastingSpider.Remove(db.TwitcastingSpider.First((x) => x.ScreenId == channelData.ScreenId));
                await db.SaveChangesAsync();
            }
            await SendLocalizedConfirmAsync("Spider.Removed", true, false, channelData.Name).ConfigureAwait(false);

            try
            {
                await (await Bot.ApplicatonOwner.CreateDMChannelAsync()).SendMessageAsync(embed: new EmbedBuilder()
                    .WithErrorColor()
                    .WithTitle("已移除 TwitCasting 頻道爬蟲")
                    .AddField("頻道", Format.Url(channelData.Name, $"https://twitcasting.tv/{channelData.ScreenId}"), false)
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
                // 跨 shard：以合併快照（B1）解析持有伺服器名稱，別 shard 持有的伺服器不會被誤標為已退出
                var guildMap = await _clusterQuery.GetGuildNameMapAsync();
                var list = db.TwitcastingSpider.AsNoTracking().Where((x) => !x.IsWarningUser).Select((x) =>
                    BotLocalizer.Format("Spider.ListEntry", locale,
                        Format.Url(x.ChannelTitle, $"https://twitcasting.tv/{x.ScreenId}"),
                        x.GuildId == 0 ? BotLocalizer.Get("Common.BotOwner", locale) :
                        (guildMap.ContainsKey(x.GuildId) ? guildMap[x.GuildId] : BotLocalizer.Get("Common.LeftGuild", locale))));
                int warningChannelNum = db.TwitcastingSpider.AsNoTracking().Count((x) => x.IsWarningUser);

                await Context.SendPaginatedConfirmAsync(BotLocalizer, locale, page, page =>
                {
                    return new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle(BotLocalizer.Get("TwitcastingSpider.ListTitle", locale))
                        .WithDescription(string.Join('\n', list.Skip(page * 20).Take(20)))
                        .WithFooter(BotLocalizer.Format("Spider.ListFooter", locale,
                            Math.Min(list.Count(), (page + 1) * 20), list.Count(), warningChannelNum));
                }, list.Count(), 10, false).ConfigureAwait(false);
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
                var list = db.TwitcastingSpider.AsNoTracking().Where((x) => x.IsWarningUser).Select((x) =>
                    BotLocalizer.Format("Spider.ListEntry", locale,
                        Format.Url(x.ChannelTitle, $"https://twitcasting.tv/{x.ScreenId}"),
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
    }
}
