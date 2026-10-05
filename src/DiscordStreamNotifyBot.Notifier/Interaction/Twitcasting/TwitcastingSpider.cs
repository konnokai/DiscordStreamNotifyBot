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
            public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                using var db = Bot.DbService.GetDbContext();
                IQueryable<DataBase.Table.TwitcastingSpider> channelList = autocompleteInteraction.User.Id == Bot.ApplicatonOwner.Id
                    ? db.TwitcastingSpider
                    : db.TwitcastingSpider.AsNoTracking().Where((x) => x.GuildId == autocompleteInteraction.GuildId);

                var candidates = channelList.Select(item =>
                    new AutocompleteCandidate(item.ChannelTitle, item.ScreenId));
                return Task.FromResult(AutocompleteResponse.FromCandidates(autocompleteInteraction, candidates,
                    ex => Log.Error($"GuildTwitCastingSpiderAutocompleteHandler - {ex}")));
            }
        }

        public TwitcastingSpider(MainDbService dbService, ClusterQueryService clusterQuery)
        {
            _dbService = dbService;
            _clusterQuery = clusterQuery;
        }

        [RequireGuildMemberCount(500)]
        [CommandExample("nana_kaguraaa", "https://twitcasting.tv/nana_kaguraaa")]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
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

        [CommandExample("nana_kaguraaa", "https://twitcasting.tv/nana_kaguraaa")]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
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

        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [SlashCommand("list", "顯示已加入爬蟲檢測的頻道")]
        public async Task ListChannelSpider([Summary("page", "頁數")] int page = 0)
        {
            if (page < 0) page = 0;
            string locale = await GetLocaleAsync(false);

            using (var db = _dbService.GetDbContext())
            {
                var spiders = db.TwitcastingSpider.AsNoTracking().Where((x) => !x.IsWarningUser).AsEnumerable()
                    .Select((x) => (x.ChannelTitle, $"https://twitcasting.tv/{x.ScreenId}", x.GuildId));
                await SendSpiderListAsync(locale, page, "TwitcastingSpider.ListTitle", _clusterQuery, spiders,
                    db.TwitcastingSpider.AsNoTracking().Count((x) => x.IsWarningUser)).ConfigureAwait(false);
            }
        }

        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [SlashCommand("list-not-trusted", "顯示已加入但為警告狀態的爬蟲檢測頻道（此清單可能包含中之人或前世的頻道）")]
        public async Task ListNotTrustedChannelSpider([Summary("page", "頁數")] int page = 0)
        {
            if (page < 0) page = 0;
            string locale = await GetLocaleAsync(false);

            using (var db = _dbService.GetDbContext())
            {
                var spiders = db.TwitcastingSpider.AsNoTracking().Where((x) => x.IsWarningUser).AsEnumerable()
                    .Select((x) => (x.ChannelTitle, $"https://twitcasting.tv/{x.ScreenId}", x.GuildId));
                await SendSpiderListAsync(locale, page, "Spider.WarningListTitle", _clusterQuery, spiders,
                    ephemeral: true).ConfigureAwait(false);
            }
        }
    }
}
