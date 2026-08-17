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
            await DeferAsync(true).ConfigureAwait(false);
            bool addForBotOwner = Context.User.Id == Bot.ApplicatonOwner.Id &&
                !await PromptUserConfirmAsync("Spider.UseForCurrentGuildPrompt");
            var result = await _service.AddCrawlerAsync(
                Context.Guild, Context.User.Id, twitchUrl, GracefulShutdown.Token, addForBotOwner);
            await SendCrawlerResultAsync(result, twitchUrl, "twitch");
        }

        [CommandSummary("移除 Twitch 頻道檢測爬蟲\n" +
            "爬蟲必須由本伺服器新增才可移除")]
        [CommandExample("998rrr", "https://twitch.tv/998rrr")]
        [SlashCommand("remove", "移除 Twitch 頻道爬蟲")]
        public async Task RemoveChannelSpider([Summary("channel", "頻道網址"), Autocomplete(typeof(GuildTwitchSpiderAutocompleteHandler))] string twitchId)
        {
            await DeferAsync(true).ConfigureAwait(false);
            var result = await _service.RemoveCrawlerAsync(
                Context.Guild.Id, twitchId, GracefulShutdown.Token, Context.User.Id == Bot.ApplicatonOwner.Id);
            await SendCrawlerResultAsync(result, twitchId, "twitch");
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
