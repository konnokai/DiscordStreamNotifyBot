using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Interaction.Attribute;
using DiscordStreamNotifyBot.Shared;
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
        [CommandSummary("新增 TwitCasting 頻道監測爬蟲\n" +
           "伺服器人數至少 500 人才可使用\n" +
           "未來會根據情況增減可新增的頻道數量\n" +
           "如有需求，請聯絡擁有者")]
        [CommandExample("nana_kaguraaa", "https://twitcasting.tv/nana_kaguraaa")]
        [SlashCommand("add", "新增 TwitCasting 頻道監測爬蟲")]
        public async Task AddChannelSpider([Summary("channel", "頻道網址")] string channelUrl)
        {
            await DeferAsync(true).ConfigureAwait(false);
            bool addForBotOwner = Context.User.Id == Bot.ApplicatonOwner.Id &&
                !await PromptUserConfirmAsync("Spider.UseForCurrentGuildPrompt");
            var result = await _service.AddCrawlerAsync(
                Context.Guild, Context.User.Id, channelUrl, GracefulShutdown.Token, addForBotOwner);
            await SendCrawlerResultAsync(result, channelUrl, "twitcasting");
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
            var result = await _service.RemoveCrawlerAsync(
                Context.Guild.Id, channelData.ScreenId, GracefulShutdown.Token,
                Context.User.Id == Bot.ApplicatonOwner.Id);
            await SendCrawlerResultAsync(result, channelData.Name, "twitcasting");
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

        [SlashCommand("list-not-trusted", "顯示已加入但為警告狀態的爬蟲檢測頻道（此清單可能包含中之人或前世的頻道）")]
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
