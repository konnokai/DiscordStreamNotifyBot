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
        public class GuildTwitchSpiderAutocompleteHandler : AutocompleteHandler
        {
            public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                using var db = Bot.DbService.GetDbContext();
                IQueryable<DataBase.Table.TwitchSpider> channelList = autocompleteInteraction.User.Id == Bot.ApplicatonOwner.Id
                    ? db.TwitchSpider
                    : db.TwitchSpider.AsNoTracking().Where((x) => x.GuildId == autocompleteInteraction.GuildId);

                var candidates = channelList.Select(item =>
                    new AutocompleteCandidate(item.UserName, item.UserId, item.UserLogin));
                return Task.FromResult(AutocompleteResponse.FromCandidates(autocompleteInteraction, candidates,
                    ex => Log.Error($"GuildTwitchSpiderAutocompleteHandler - {ex}")));
            }
        }

        public TwitchSpider(MainDbService dbService, ClusterQueryService clusterQuery)
        {
            _dbService = dbService;
            _clusterQuery = clusterQuery;
        }

        [CommandExample("998rrr", "https://twitch.tv/998rrr")]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [SlashCommand("add", "新增 Twitch 頻道爬蟲")]
        public async Task AddChannelSpider([Summary("channel", "頻道網址")] string twitchUrl)
        {
            await DeferAsync(true).ConfigureAwait(false);
            bool addForBotOwner = Context.User.Id == Bot.ApplicatonOwner.Id &&
                !await PromptUserConfirmAsync("Spider.UseForCurrentGuildPrompt");
            var result = await _service.AddCrawlerAsync(
                Context.Guild, Context.User.Id, twitchUrl, GracefulShutdown.Token, addForBotOwner);
            await SendCrawlerResultAsync(result, twitchUrl, "twitch");
        }

        [CommandExample("998rrr", "https://twitch.tv/998rrr")]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [SlashCommand("remove", "移除 Twitch 頻道爬蟲")]
        public async Task RemoveChannelSpider([Summary("channel", "頻道網址"), Autocomplete(typeof(GuildTwitchSpiderAutocompleteHandler))] string twitchId)
        {
            await DeferAsync(true).ConfigureAwait(false);
            var result = await _service.RemoveCrawlerAsync(
                Context.Guild.Id, twitchId, GracefulShutdown.Token, Context.User.Id == Bot.ApplicatonOwner.Id);
            await SendCrawlerResultAsync(result, twitchId, "twitch");
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
                try
                {
                    var spiders = db.TwitchSpider.AsNoTracking().Where((x) => showAll || x.GuildId == guildId).AsEnumerable()
                        .Select((x) => (x.UserName, $"https://twitch.tv/{x.UserLogin}", x.GuildId));
                    await SendSpiderListAsync(locale, page, "TwitchSpider.ListTitle", _clusterQuery, spiders).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), $"Twitch-Spider-List Error");
                    await SendLocalizedErrorAsync("Errors.OperationFailed", false, true);
                }
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
