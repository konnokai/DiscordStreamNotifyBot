using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Interaction.Attribute;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService.Chzzk;
using DiscordStreamNotifyBot.SharedService.Cluster;

namespace DiscordStreamNotifyBot.Interaction.Chzzk
{
    [RequireContext(ContextType.Guild)]
    [Group("chzzk-spider", "CHZZK 爬蟲設定")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    public class ChzzkSpider : TopLevelModule<SharedService.Chzzk.ChzzkService>
    {
        private readonly MainDbService _dbService;
        private readonly ClusterQueryService _clusterQuery;

        public class GuildChzzkSpiderAutocompleteHandler : AutocompleteHandler
        {
            public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                using var db = Bot.DbService.GetDbContext();
                IQueryable<DataBase.Table.ChzzkSpider> channelList = autocompleteInteraction.User.Id == Bot.ApplicatonOwner.Id
                    ? db.ChzzkSpider
                    : db.ChzzkSpider.AsNoTracking().Where((x) => x.GuildId == autocompleteInteraction.GuildId);

                var candidates = channelList.Select(item =>
                    new AutocompleteCandidate(item.ChannelName, item.ChannelId));
                return Task.FromResult(AutocompleteResponse.FromCandidates(autocompleteInteraction, candidates,
                    ex => Log.Error($"GuildChzzkSpiderAutocompleteHandler - {ex}")));
            }
        }

        public ChzzkSpider(MainDbService dbService, ClusterQueryService clusterQuery)
        {
            _dbService = dbService;
            _clusterQuery = clusterQuery;
        }

        [CommandExample("https://chzzk.naver.com/64d76089fba26b180d9c9e48a32600d9",
           "https://chzzk.naver.com/live/64d76089fba26b180d9c9e48a32600d9")]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [SlashCommand("add", "新增 CHZZK 頻道爬蟲")]
        public async Task AddChannelSpider([Summary("channel", "頻道網址")] string channelUrl)
        {
            await DeferAsync(true).ConfigureAwait(false);
            bool addForBotOwner = Context.User.Id == Bot.ApplicatonOwner.Id &&
                !await PromptUserConfirmAsync("Spider.UseForCurrentGuildPrompt");
            var result = await _service.AddCrawlerAsync(
                Context.Guild, Context.User.Id, channelUrl, GracefulShutdown.Token, addForBotOwner);
            await SendCrawlerResultAsync(result, channelUrl, "chzzk");
        }

        [CommandExample("64d76089fba26b180d9c9e48a32600d9", "https://chzzk.naver.com/64d76089fba26b180d9c9e48a32600d9")]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [SlashCommand("remove", "移除 CHZZK 頻道檢測爬蟲")]
        public async Task RemoveChannelSpider([Summary("channel", "頻道網址"), Autocomplete(typeof(GuildChzzkSpiderAutocompleteHandler))] string channel)
        {
            await DeferAsync(true).ConfigureAwait(false);
            string sourceId = ChzzkUrlParser.TryParseChannelId(channel, out string channelId)
                ? channelId
                : channel.Trim();
            var result = await _service.RemoveCrawlerAsync(
                Context.Guild.Id, sourceId, GracefulShutdown.Token, Context.User.Id == Bot.ApplicatonOwner.Id);
            await SendCrawlerResultAsync(result, channel, "chzzk");
        }

        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [SlashCommand("list", "顯示已加入爬蟲檢測的頻道")]
        public async Task ListChannelSpider([Summary("page", "頁數")] int page = 0)
        {
            if (page < 0) page = 0;
            string locale = await GetLocaleAsync(false);

            using (var db = _dbService.GetDbContext())
            {
                bool showAll = CanViewAllSpiders;
                ulong guildId = Context.Guild.Id;
                var spiders = db.ChzzkSpider.AsNoTracking().Where((x) => showAll || x.GuildId == guildId).AsEnumerable()
                    .Select((x) => (string.IsNullOrEmpty(x.ChannelName) ? x.ChannelId : x.ChannelName,
                        ChzzkUrls.Channel(x.ChannelId), x.GuildId));
                await SendSpiderListAsync(locale, page, "ChzzkSpider.ListTitle", _clusterQuery, spiders).ConfigureAwait(false);
            }
        }
    }
}
