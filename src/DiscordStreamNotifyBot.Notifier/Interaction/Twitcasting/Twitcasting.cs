using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Interaction.Attribute;
using DiscordStreamNotifyBot.Shared;

namespace DiscordStreamNotifyBot.Interaction.TwitCasting
{
    [RequireContext(ContextType.Guild)]
    [Group("twitcasting", "TwitCasting 通知")]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    [DefaultMemberPermissions(GuildPermission.ManageMessages)]
    public class Twitcasting : TopLevelModule<SharedService.Twitcasting.TwitcastingService>
    {
        private readonly DiscordSocketClient _client;
        private readonly MainDbService _dbService;

        public class GuildNoticeTwitCastingChannelIdAutocompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                return await Task.Run(async () =>
                {
                    using var db = Bot.DbService.GetDbContext();
                    if (!await db.NoticeTwitcastingStreamChannels.AsNoTracking().AnyAsync((x) => x.GuildId == context.Guild.Id))
                        return AutocompletionResult.FromSuccess();

                    var candidates = db.NoticeTwitcastingStreamChannels
                        .AsNoTracking()
                        .Where((x) => x.GuildId == context.Guild.Id)
                        .Select((x) => new AutocompleteCandidate(
                            db.GetTwitCastingChannelTitleByScreenId(x.ScreenId), x.ScreenId));

                    try
                    {
                        string value = autocompleteInteraction.Data.Current.Value?.ToString();
                        var results = AutocompleteSearch.Filter(candidates, value)
                            .Select(item => new AutocompleteResult(item.Name, item.Value));
                        return AutocompletionResult.FromSuccess(results);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"GuildNoticeTwitCastingChannelIdAutocompleteHandler - {ex}");
                        return AutocompletionResult.FromSuccess();
                    }
                });
            }
        }

        public Twitcasting(DiscordSocketClient client, MainDbService dbService)
        {
            _client = client;
            _dbService = dbService;
        }

        [CommandExample("nana_kaguraaa", "https://twitcasting.tv/nana_kaguraaa")]
        [SlashCommand("add", "新增 TwitCasting 直播通知的頻道")]
        public async Task AddChannel([Summary("streamer", "頻道網址")] string channelUrl,
            [Summary("notification-channel", "發送通知的頻道"), ChannelTypes(ChannelType.Text, ChannelType.News)] IChannel channel)
        {
            if (!_service.IsEnable)
            {
                await SendLocalizedErrorAsync("Errors.FeatureDisabled").ConfigureAwait(false);
                return;
            }

            await DeferAsync(true).ConfigureAwait(false);

            var textChannel = channel as IGuildChannel;
            string locale = await GetLocaleAsync(true);

            var permissions = Context.Guild.GetUser(_client.CurrentUser.Id).GetPermissions(textChannel);
            if (!permissions.ViewChannel || !permissions.SendMessages)
            {
                await SendLocalizedErrorAsync("Permissions.MissingChannelPermissions", true, true,
                    $"`{textChannel}`", BotLocalizer.Format("Permissions.List", locale,
                        BotLocalizer.Get("Permissions.Name.ViewChannel", locale),
                        BotLocalizer.Get("Permissions.Name.SendMessages", locale)));
                return;
            }

            if (!permissions.EmbedLinks)
            {
                await SendLocalizedErrorAsync("Permissions.MissingChannelPermissions", true, true,
                    $"`{textChannel}`", BotLocalizer.Get("Permissions.Name.EmbedLinks", locale));
                return;
            }

            var channelData = await _service.GetChannelNameAndTitleAsync(channelUrl);
            if (channelData == null)
            {
                await SendLocalizedErrorAsync("Twitcasting.Errors.UserNotFound", true);
                return;
            }

            using (var db = _dbService.GetDbContext())
            {
                await CheckIsFirstSetNoticeAndSendWarningMessageAsync(db);

                var noticeTwitCastingStreamChannel = db.NoticeTwitcastingStreamChannels.FirstOrDefault((x) => x.GuildId == Context.Guild.Id && x.ScreenId == channelData.ScreenId);
                if (noticeTwitCastingStreamChannel != null)
                {
                    if (!await PromptUserConfirmAsync("Notifications.OverwritePrompt", channelData.Name).ConfigureAwait(false))
                        return;
                }

                var result = await _service.UpsertNotificationAsync(
                    Context.Guild,
                    channelData.ScreenId,
                    textChannel.Id,
                    noticeTwitCastingStreamChannel?.StartStreamMessage ?? "",
                    GracefulShutdown.Token);
                if (result.State != "applied")
                {
                    await SendLocalizedErrorAsync("Errors.OperationFailed", true);
                    return;
                }

                if (noticeTwitCastingStreamChannel != null)
                {
                    await SendLocalizedConfirmAsync("Notifications.ChannelChanged", true, true,
                        channelData.Name, textChannel).ConfigureAwait(false);
                    return;
                }

                string addString = "";
                if (!db.TwitcastingSpider.Any((x) => x.ScreenId == channelData.ScreenId))
                {
                    string spiderPath = CommandDisplayResolver.GetCommandPath(locale, "twitcasting-spider", "add");
                    addString = BotLocalizer.Format("Notifications.SpiderWarning", locale, spiderPath);
                }
                await SendLocalizedConfirmAsync("Twitcasting.Notifications.Added", true, true,
                    channelData.Name, addString).ConfigureAwait(false);
            }
        }

        [CommandExample("nana_kaguraaa", "https://twitcasting.tv/nana_kaguraaa")]
        [SlashCommand("remove", "移除 TwitCasting 直播通知的頻道")]
        public async Task RemoveChannel([Summary("channel", "頻道網址"), Autocomplete(typeof(GuildNoticeTwitCastingChannelIdAutocompleteHandler))] string channelUrl)
        {
            await DeferAsync(true).ConfigureAwait(false);

            var channelData = await _service.GetChannelNameAndTitleAsync(channelUrl);
            if (channelData == null)
            {
                await SendLocalizedErrorAsync("Twitcasting.Errors.UserNotFound", true).ConfigureAwait(false);
                return;
            }

            using (var db = _dbService.GetDbContext())
            {
                if (!db.NoticeTwitcastingStreamChannels.Any((x) => x.GuildId == Context.Guild.Id))
                {
                    await SendLocalizedErrorAsync("Twitcasting.Notifications.NoneConfigured", true).ConfigureAwait(false);
                    return;
                }

                if (!db.NoticeTwitcastingStreamChannels.Any((x) => x.GuildId == Context.Guild.Id && x.ScreenId == channelData.ScreenId))
                {
                    await SendLocalizedErrorAsync("Notifications.NotConfigured", true, true, channelData.Name).ConfigureAwait(false);
                    return;
                }
                else
                {
                    var result = await _service.RemoveNotificationAsync(
                        Context.Guild.Id,
                        channelData.ScreenId,
                        GracefulShutdown.Token);
                    if (result.State != "applied")
                    {
                        await SendLocalizedErrorAsync("Errors.OperationFailed", true, true);
                        return;
                    }
                    await SendLocalizedConfirmAsync("Notifications.Removed", true, true, channelData.Name).ConfigureAwait(false);
                }
            }
        }

        [SlashCommand("list", "顯示已加入通知清單的 TwitCasting 直播頻道")]
        public async Task ListChannel([Summary("page", "頁數")] int page = 0)
        {
            string locale = await GetLocaleAsync(false);
            using (var db = _dbService.GetDbContext())
            {
                var list = Queryable.Where(db.NoticeTwitcastingStreamChannels, (x) => x.GuildId == Context.Guild.Id)
                    .Select((x) => $"`{db.GetTwitCastingChannelTitleByScreenId(x.ScreenId)}` => <#{x.DiscordChannelId}>").ToList();

                if (list.Count == 0) { await SendLocalizedErrorAsync("Twitcasting.Notifications.Empty").ConfigureAwait(false); return; }

                await Context.SendPaginatedConfirmAsync(BotLocalizer, locale, page, page =>
                {
                    return new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle(BotLocalizer.Get("Twitcasting.Notifications.ListTitle", locale))
                        .WithDescription(string.Join('\n', list.Skip(page * 20).Take(20)))
                        .WithFooter(BotLocalizer.Format("Common.ChannelCountFooter", locale,
                            Math.Min(list.Count, (page + 1) * 20), list.Count));
                }, list.Count, 20, false);
            }
        }

        [RequireBotPermission(GuildPermission.MentionEveryone)]
        [CommandSummary("設定通知訊息\n" +
            "未輸入通知訊息時，會清除自訂通知訊息\n" +
            "請先新增直播通知，再設定通知訊息（`/help get-command-help twitcasting add`）\n\n" +
            "（若通知訊息要提及特定身分組，Bot 必須具備提及所有身分組權限）")]
        [CommandExample("nana_kaguraaa 開台啦", "https://twitcasting.tv/nana_kaguraaa 開台啦")]
        [SlashCommand("set-message", "設定通知訊息")]
        public async Task SetMessage([Summary("channel", "頻道網址"), Autocomplete(typeof(GuildNoticeTwitCastingChannelIdAutocompleteHandler))] string channelUrl, [Summary("message", "通知訊息")] string message = "")
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
                string locale = await GetLocaleAsync(true);
                if (db.NoticeTwitcastingStreamChannels.Any((x) => x.GuildId == Context.Guild.Id && x.ScreenId == channelData.ScreenId))
                {
                    var noticeStreamChannel = db.NoticeTwitcastingStreamChannels.First((x) => x.GuildId == Context.Guild.Id && x.ScreenId == channelData.ScreenId);

                    noticeStreamChannel.StartStreamMessage = message.Trim();
                    db.NoticeTwitcastingStreamChannels.Update(noticeStreamChannel);
                    db.SaveChanges();
                    _service.InvalidateNoticeCache();

                    if (message != "")
                        await SendLocalizedConfirmAsync("Notifications.MessageSetSimple", true, true, channelData.Name, message).ConfigureAwait(false);
                    else
                        await SendLocalizedConfirmAsync("Notifications.MessageClearedSimple", true, true, channelData.Name).ConfigureAwait(false);
                }
                else
                {
                    string addPath = CommandDisplayResolver.GetCommandPath(locale, "twitcasting", "add");
                    await SendLocalizedErrorAsync("Twitcasting.Notifications.ConfigureFirst", true, true,
                        channelData.Name, addPath).ConfigureAwait(false);
                }
            }
        }

        [SlashCommand("list-message", "列出已設定的 TwitCasting 直播通知訊息")]
        public async Task ListMessage([Summary("page", "頁數")] int page = 0)
        {
            string locale = await GetLocaleAsync(false);
            using (var db = _dbService.GetDbContext())
            {
                if (db.NoticeTwitcastingStreamChannels.Any((x) => x.GuildId == Context.Guild.Id))
                {
                    var noticeTwitterSpaces = db.NoticeTwitcastingStreamChannels.Where((x) => x.GuildId == Context.Guild.Id);
                    Dictionary<string, string> dic = new Dictionary<string, string>();

                    foreach (var item in noticeTwitterSpaces)
                    {
                        string message = string.IsNullOrWhiteSpace(item.StartStreamMessage)
                            ? BotLocalizer.Get("Common.None", locale)
                            : item.StartStreamMessage;
                        dic.Add(db.GetTwitCastingChannelTitleByScreenId(item.ScreenId), message);
                    }

                    try
                    {
                        await Context.SendPaginatedConfirmAsync(BotLocalizer, locale, page, (page) =>
                        {
                            EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor()
                                .WithTitle(BotLocalizer.Get("Twitcasting.Messages.ListTitle", locale))
                                .WithDescription(BotLocalizer.Get("Notifications.MessageListDescription", locale));

                            foreach (var item in dic.Skip(page * 10).Take(10))
                            {
                                embedBuilder.AddField(item.Key, item.Value);
                            }

                            return embedBuilder;
                        }, dic.Count, 10).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Demystify(), $"TwitCasting-ListMessage: {Context.Guild.Id}");
                    }
                }
                else
                {
                    string addPath = CommandDisplayResolver.GetCommandPath(locale, "twitcasting", "add");
                    await SendLocalizedErrorAsync("Twitcasting.Notifications.ConfigureAnyFirst", false, true, addPath).ConfigureAwait(false);
                }
            }
        }
    }
}
