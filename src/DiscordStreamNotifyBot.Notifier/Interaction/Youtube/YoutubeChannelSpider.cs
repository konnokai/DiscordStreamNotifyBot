using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Interaction.Attribute;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService.Cluster;

namespace DiscordStreamNotifyBot.Interaction.Youtube
{
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    [Group("youtube-spider", "YouTube 爬蟲設定")]
    public class YoutubeChannelSpider : TopLevelModule<SharedService.Youtube.YoutubeStreamService>
    {
        private readonly MainDbService _dbService;
        private readonly ClusterQueryService _clusterQuery;
        public class GuildYoutubeChannelSpiderAutocompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                return await Task.Run(() =>
                {
                    using var db = Bot.DbService.GetDbContext();
                    IQueryable<DataBase.Table.YoutubeChannelSpider> channelList;

                    if (autocompleteInteraction.User.Id == Bot.ApplicatonOwner.Id)
                    {
                        channelList = db.YoutubeChannelSpider;
                    }
                    else
                    {
                        if (!db.YoutubeChannelSpider.Any((x) => x.GuildId == autocompleteInteraction.GuildId))
                            return AutocompletionResult.FromSuccess();

                        channelList = db.YoutubeChannelSpider.Where((x) => x.GuildId == autocompleteInteraction.GuildId);
                    }

                    try
                    {
                        string value = autocompleteInteraction.Data.Current.Value?.ToString();
                        var candidates = channelList.Select(item =>
                            new AutocompleteCandidate(item.ChannelTitle, item.ChannelId));
                        var results = AutocompleteSearch.Filter(candidates, value)
                            .Select(item => new AutocompleteResult(item.Name, item.Value));
                        return AutocompletionResult.FromSuccess(results);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"GuildYoutubeChannelSpiderAutocompleteHandler - {ex}");
                        return AutocompletionResult.FromSuccess();
                    }
                });
            }
        }

        public YoutubeChannelSpider(MainDbService dbService, ClusterQueryService clusterQuery)
        {
            _dbService = dbService;
            _clusterQuery = clusterQuery;
        }

        [CommandSummary("新增非兩大箱的頻道檢測爬蟲\n" +
           "如有任何需要請向 Bot 擁有者詢問")]
        [CommandExample("https://www.youtube.com/channel/UUMOs5FNYPHeZz5f7N1BDExxfg",
            "https://www.youtube.com/@998rrr")]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [SlashCommand("add", "新增非兩大箱的頻道檢測爬蟲")]
        public async Task AddChannelSpider([Summary("channel", "頻道網址")] string channelUrl)
        {
            await DeferAsync(true).ConfigureAwait(false);
            bool addForBotOwner = Context.User.Id == Bot.ApplicatonOwner.Id &&
                !await PromptUserConfirmAsync("Spider.UseForCurrentGuildPrompt");
            var result = await _service.AddCrawlerAsync(
                Context.Guild, Context.User.Id, channelUrl, GracefulShutdown.Token, addForBotOwner);
            await SendCrawlerResultAsync(result, channelUrl, "youtube");
        }

        [CommandSummary("移除非兩大箱的頻道檢測爬蟲\n" +
            "爬蟲必須由本伺服器新增才可移除")]
        [CommandExample("https://www.youtube.com/channel/UUMOs5FNYPHeZz5f7N1BDExxfg",
            "https://www.youtube.com/@998rrr")]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [SlashCommand("remove", "移除非兩大箱的頻道檢測爬蟲")]
        public async Task RemoveChannelSpider([Summary("channel", "頻道網址"), Autocomplete(typeof(GuildYoutubeChannelSpiderAutocompleteHandler))] string channelUrl)
        {
            await DeferAsync(true).ConfigureAwait(false);
            try
            {
                string channelId = await _service.GetChannelIdAsync(channelUrl).ConfigureAwait(false);
                var result = await _service.RemoveCrawlerAsync(
                    Context.Guild.Id, channelId, GracefulShutdown.Token, Context.User.Id == Bot.ApplicatonOwner.Id);
                await SendCrawlerResultAsync(result, channelId, "youtube");
            }
            catch (FormatException)
            {
                await SendLocalizedErrorAsync("Errors.InvalidYoutubeChannel", true).ConfigureAwait(false);
                return;
            }
            catch (ArgumentNullException)
            {
                await SendLocalizedErrorAsync("Errors.UrlRequired", true).ConfigureAwait(false);
                return;
            }

        }

        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [SlashCommand("list", "顯示已加入的爬蟲頻道")]
        public async Task ListChannelSpider([Summary("page", "頁數")] int page = 0)
        {
            if (page < 0) page = 0;
            string locale = await GetLocaleAsync(false);

            using (var db = _dbService.GetDbContext())
            {
                // 跨 shard：以合併快照（B1）解析持有伺服器名稱，別 shard 持有的伺服器不會被誤標為已退出
                var guildMap = await _clusterQuery.GetGuildNameMapAsync();
                var list = db.YoutubeChannelSpider.Where((x) => x.IsTrustedChannel).Select((x) =>
                    BotLocalizer.Format("Spider.ListEntry", locale,
                        Format.Url(x.ChannelTitle, $"https://www.youtube.com/channel/{x.ChannelId}"),
                        x.GuildId == 0 ? BotLocalizer.Get("Common.BotOwner", locale) :
                        (guildMap.ContainsKey(x.GuildId) ? guildMap[x.GuildId] : BotLocalizer.Get("Common.LeftGuild", locale))));
                int warningChannelNum = db.YoutubeChannelSpider.Count((x) => !x.IsTrustedChannel);

                await Context.SendPaginatedConfirmAsync(BotLocalizer, locale, page, page =>
                {
                    return new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle(BotLocalizer.Get("YoutubeSpider.ListTitle", locale))
                        .WithDescription(string.Join('\n', list.Skip(page * 20).Take(20)))
                        .WithFooter(BotLocalizer.Format("Spider.ListFooter", locale,
                            Math.Min(list.Count(), (page + 1) * 20), list.Count(), warningChannelNum));
                }, list.Count(), 10, false).ConfigureAwait(false);
            }
        }

        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [SlashCommand("list-not-trusted", "顯示已加入但非認可的爬蟲檢測頻道 (本清單可能內含中之人或前世的頻道)")]
        public async Task ListNotTrustedChannelSpider([Summary("page", "頁數")] int page = 0)
        {
            if (page < 0) page = 0;
            string locale = await GetLocaleAsync(false);

            using (var db = _dbService.GetDbContext())
            {
                // 跨 shard：以合併快照（B1）解析持有伺服器名稱，別 shard 持有的伺服器不會被誤標為已退出
                var guildMap = await _clusterQuery.GetGuildNameMapAsync();
                var list = db.YoutubeChannelSpider.Where((x) => !x.IsTrustedChannel).Select((x) =>
                    BotLocalizer.Format("Spider.ListEntry", locale,
                        Format.Url(x.ChannelTitle, $"https://www.youtube.com/channel/{x.ChannelId}"),
                        x.GuildId == 0 ? BotLocalizer.Get("Common.BotOwner", locale) :
                        (guildMap.ContainsKey(x.GuildId) ? guildMap[x.GuildId] : BotLocalizer.Get("Common.LeftGuild", locale))));

                await Context.SendPaginatedConfirmAsync(BotLocalizer, locale, page, page =>
                {
                    return new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle(BotLocalizer.Get("YoutubeSpider.UntrustedListTitle", locale))
                        .WithDescription(string.Join('\n', list.Skip(page * 20).Take(20)))
                        .WithFooter(BotLocalizer.Format("Common.ChannelCountFooter", locale,
                            Math.Min(list.Count(), (page + 1) * 20), list.Count()));
                }, list.Count(), 10, false, true).ConfigureAwait(false);
            }
        }
    }
}
