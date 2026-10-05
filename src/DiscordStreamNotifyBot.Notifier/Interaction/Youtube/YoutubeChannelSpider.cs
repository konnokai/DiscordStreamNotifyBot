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
            public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                using var db = Bot.DbService.GetDbContext();
                IQueryable<DataBase.Table.YoutubeChannelSpider> channelList = autocompleteInteraction.User.Id == Bot.ApplicatonOwner.Id
                    ? db.YoutubeChannelSpider
                    : db.YoutubeChannelSpider.Where((x) => x.GuildId == autocompleteInteraction.GuildId);

                var candidates = channelList.Select(item =>
                    new AutocompleteCandidate(item.ChannelTitle, item.ChannelId));
                return Task.FromResult(AutocompleteResponse.FromCandidates(autocompleteInteraction, candidates,
                    ex => Log.Error($"GuildYoutubeChannelSpiderAutocompleteHandler - {ex}")));
            }
        }

        public YoutubeChannelSpider(MainDbService dbService, ClusterQueryService clusterQuery)
        {
            _dbService = dbService;
            _clusterQuery = clusterQuery;
        }

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
                var spiders = db.YoutubeChannelSpider.AsNoTracking().Where((x) => x.IsTrustedChannel).AsEnumerable()
                    .Select((x) => (x.ChannelTitle, $"https://www.youtube.com/channel/{x.ChannelId}", x.GuildId));
                await SendSpiderListAsync(locale, page, "YoutubeSpider.ListTitle", _clusterQuery, spiders,
                    db.YoutubeChannelSpider.Count((x) => !x.IsTrustedChannel)).ConfigureAwait(false);
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
                var spiders = db.YoutubeChannelSpider.AsNoTracking().Where((x) => !x.IsTrustedChannel).AsEnumerable()
                    .Select((x) => (x.ChannelTitle, $"https://www.youtube.com/channel/{x.ChannelId}", x.GuildId));
                await SendSpiderListAsync(locale, page, "YoutubeSpider.UntrustedListTitle", _clusterQuery, spiders,
                    ephemeral: true).ConfigureAwait(false);
            }
        }
    }
}
