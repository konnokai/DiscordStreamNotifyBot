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
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                return await Task.Run(async () =>
                {
                    using var db = Bot.DbService.GetDbContext();
                    IQueryable<DataBase.Table.ChzzkSpider> channelList;

                    if (autocompleteInteraction.User.Id == Bot.ApplicatonOwner.Id)
                    {
                        channelList = db.ChzzkSpider;
                    }
                    else
                    {
                        if (!await db.ChzzkSpider.AsNoTracking().AnyAsync((x) => x.GuildId == autocompleteInteraction.GuildId))
                            return AutocompletionResult.FromSuccess();

                        channelList = db.ChzzkSpider.AsNoTracking().Where((x) => x.GuildId == autocompleteInteraction.GuildId);
                    }

                    try
                    {
                        string value = autocompleteInteraction.Data.Current.Value?.ToString();
                        var candidates = channelList.Select(item =>
                            new AutocompleteCandidate(item.ChannelName, item.ChannelId));
                        var results = AutocompleteSearch.Filter(candidates, value)
                            .Select(item => new AutocompleteResult(item.Name, item.Value));
                        return AutocompletionResult.FromSuccess(results);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"GuildChzzkSpiderAutocompleteHandler - {ex}");
                        return AutocompletionResult.FromSuccess();
                    }
                });
            }
        }

        public ChzzkSpider(MainDbService dbService, ClusterQueryService clusterQuery)
        {
            _dbService = dbService;
            _clusterQuery = clusterQuery;
        }

        [CommandSummary("新增 CHZZK 頻道爬蟲\n" +
           "伺服器人數至少 200 人才可使用\n" +
           "未來會根據情況增減可新增的頻道數量\n" +
           "如有需求，請聯絡擁有者")]
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

        [CommandSummary("移除 CHZZK 頻道檢測爬蟲\n" +
            "爬蟲必須由本伺服器新增才可移除")]
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
                // 跨 shard：以合併快照解析持有伺服器名稱，別 shard 持有的伺服器不會被誤標為已退出
                var guildMap = await _clusterQuery.GetGuildNameMapAsync();
                var list = db.ChzzkSpider.AsNoTracking().AsEnumerable().Select((x) =>
                    BotLocalizer.Format("Spider.ListEntry", locale,
                        Format.Url(string.IsNullOrEmpty(x.ChannelName) ? x.ChannelId : x.ChannelName,
                            ChzzkUrls.Channel(x.ChannelId)),
                        x.GuildId == 0 ? BotLocalizer.Get("Common.BotOwner", locale) :
                        (guildMap.ContainsKey(x.GuildId) ? guildMap[x.GuildId] : BotLocalizer.Get("Common.LeftGuild", locale))))
                    .ToList();

                await Context.SendPaginatedConfirmAsync(BotLocalizer, locale, page, page =>
                {
                    return new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle(BotLocalizer.Get("ChzzkSpider.ListTitle", locale))
                        .WithDescription(string.Join('\n', list.Skip(page * 20).Take(20)))
                        .WithFooter(BotLocalizer.Format("Common.ChannelCountFooter", locale,
                            Math.Min(list.Count, (page + 1) * 20), list.Count));
                }, list.Count, 10, false).ConfigureAwait(false);
            }
        }
    }
}
