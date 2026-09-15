using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Interaction.Attribute;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.Chzzk;

namespace DiscordStreamNotifyBot.Interaction.Chzzk
{
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    [DefaultMemberPermissions(GuildPermission.ManageMessages)]
    [Group("chzzk", "CHZZK 通知設定")]
    public class Chzzk : TopLevelModule<ChzzkService>
    {
        private readonly DiscordSocketClient _client;
        private readonly MainDbService _dbService;

        public class GuildNoticeChzzkChannelIdAutocompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                return await Task.Run(async () =>
                {
                    using var db = Bot.DbService.GetDbContext();
                    var notices = db.NoticeChzzkStreamChannels.AsNoTracking()
                        .Where((x) => x.GuildId == context.Guild.Id).ToList();
                    if (notices.Count == 0)
                        return AutocompletionResult.FromSuccess();

                    var names = db.ChzzkSpider.AsNoTracking().ToDictionary(x => x.ChannelId, x => x.ChannelName);
                    var candidates = notices.Select(x => new AutocompleteCandidate(
                        string.IsNullOrEmpty(names.GetValueOrDefault(x.NoticeChzzkChannelId))
                            ? x.NoticeChzzkChannelId
                            : names[x.NoticeChzzkChannelId],
                        x.NoticeChzzkChannelId));

                    try
                    {
                        string value = autocompleteInteraction.Data.Current.Value?.ToString();
                        var results = AutocompleteSearch.Filter(candidates, value)
                            .Select(item => new AutocompleteResult(item.Name, item.Value));
                        return AutocompletionResult.FromSuccess(results);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"GuildNoticeChzzkChannelIdAutocompleteHandler - {ex}");
                        return AutocompletionResult.FromSuccess();
                    }
                });
            }
        }

        public Chzzk(DiscordSocketClient client, MainDbService dbService)
        {
            _client = client;
            _dbService = dbService;
        }

        [CommandExample("https://chzzk.naver.com/64d76089fba26b180d9c9e48a32600d9",
            "https://chzzk.naver.com/live/64d76089fba26b180d9c9e48a32600d9")]
        [DefaultMemberPermissions(GuildPermission.ManageMessages)]
        [SlashCommand("add", "新增 CHZZK 直播通知的頻道")]
        public async Task AddChannel([Summary("streamer", "頻道網址")] string channelUrl,
            [Summary("notification-channel", "發送通知的頻道"), ChannelTypes(ChannelType.Text, ChannelType.News)] IChannel channel)
        {
            await DeferAsync(true).ConfigureAwait(false);

            try
            {
                if (!ChzzkUrlParser.TryParseChannelId(channelUrl, out string channelId))
                {
                    await SendLocalizedErrorAsync("Chzzk.Errors.ChannelNotFound", true);
                    return;
                }

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

                var channelData = await _service.GetChannelAsync(channelId, GracefulShutdown.Token);
                if (channelData == null)
                {
                    await SendLocalizedErrorAsync("Chzzk.Errors.ChannelNotFound", true);
                    return;
                }

                using (var db = _dbService.GetDbContext())
                {
                    await CheckIsFirstSetNoticeAndSendWarningMessageAsync(db);

                    var notice = db.NoticeChzzkStreamChannels.FirstOrDefault(
                        (x) => x.GuildId == Context.Guild.Id && x.NoticeChzzkChannelId == channelId);
                    if (notice != null)
                    {
                        if (!await PromptUserConfirmAsync("Notifications.OverwritePrompt", channelData.ChannelName).ConfigureAwait(false))
                            return;
                    }

                    var messages = notice == null
                        ? new AdminSettingsChzzkMessages()
                        : new AdminSettingsChzzkMessages
                        {
                            Start = notice.StartStreamMessage,
                            End = notice.EndStreamMessage
                        };
                    var result = await _service.UpsertNotificationAsync(
                        Context.Guild,
                        channelId,
                        textChannel.Id,
                        messages,
                        GracefulShutdown.Token);
                    if (result.State != "applied")
                    {
                        await SendLocalizedErrorAsync("Errors.OperationFailed", true);
                        return;
                    }

                    if (notice != null)
                    {
                        await SendLocalizedConfirmAsync("Notifications.ChannelChanged", true, true,
                            channelData.ChannelName, textChannel).ConfigureAwait(false);
                        return;
                    }

                    string addString = "";
                    if (!db.ChzzkSpider.Any((x) => x.ChannelId == channelId))
                    {
                        string spiderPath = CommandDisplayResolver.GetCommandPath(locale, "chzzk-spider", "add");
                        addString = BotLocalizer.Format("Notifications.SpiderWarning", locale, spiderPath);
                    }
                    await SendLocalizedConfirmAsync("Chzzk.Notifications.Added", true, true,
                        channelData.ChannelName, addString).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"CHZZK Add Error: {channelUrl}");
                await SendLocalizedErrorAsync("Errors.OperationFailed", true);
            }
        }

        [CommandExample("64d76089fba26b180d9c9e48a32600d9")]
        [DefaultMemberPermissions(GuildPermission.ManageMessages)]
        [SlashCommand("remove", "移除 CHZZK 直播通知的頻道")]
        public async Task RemoveChannel([Summary("channel", "頻道名稱"), Autocomplete(typeof(GuildNoticeChzzkChannelIdAutocompleteHandler))] string channelId)
        {
            using (var db = _dbService.GetDbContext())
            {
                var notice = db.NoticeChzzkStreamChannels.AsNoTracking().FirstOrDefault(
                    (x) => x.GuildId == Context.Guild.Id && x.NoticeChzzkChannelId == channelId);

                if (notice == null)
                {
                    await SendLocalizedErrorAsync("Notifications.NotConfigured", false, true, channelId).ConfigureAwait(false);
                    return;
                }

                string channelName = db.ChzzkSpider.AsNoTracking()
                    .FirstOrDefault((x) => x.ChannelId == channelId)?.ChannelName ?? channelId;
                var result = await _service.RemoveNotificationAsync(
                    Context.Guild.Id,
                    channelId,
                    GracefulShutdown.Token);
                if (result.State != "applied")
                {
                    await SendLocalizedErrorAsync("Errors.OperationFailed", false, true);
                    return;
                }

                await SendLocalizedConfirmAsync("Notifications.Removed", false, true, channelName).ConfigureAwait(false);
            }
        }

        [DefaultMemberPermissions(GuildPermission.ManageMessages)]
        [SlashCommand("list", "顯示已加入通知清單的 CHZZK 直播頻道")]
        public async Task ListChannel([Summary("page", "頁數")] int page = 0)
        {
            string locale = await GetLocaleAsync(false);
            using (var db = _dbService.GetDbContext())
            {
                var names = db.ChzzkSpider.AsNoTracking().ToDictionary(x => x.ChannelId, x => x.ChannelName);
                var list = db.NoticeChzzkStreamChannels.AsNoTracking()
                    .Where((x) => x.GuildId == Context.Guild.Id)
                    .ToList()
                    .Select((x) => $"`{names.GetValueOrDefault(x.NoticeChzzkChannelId, x.NoticeChzzkChannelId)}` => <#{x.DiscordChannelId}>")
                    .ToList();
                if (list.Count == 0)
                {
                    await SendLocalizedErrorAsync("Chzzk.Notifications.Empty").ConfigureAwait(false);
                    return;
                }

                await Context.SendPaginatedConfirmAsync(BotLocalizer, locale, page, page =>
                {
                    return new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle(BotLocalizer.Get("Chzzk.Notifications.ListTitle", locale))
                        .WithDescription(string.Join('\n', list.Skip(page * 20).Take(20)))
                        .WithFooter(BotLocalizer.Format("Common.ChannelCountFooter", locale,
                            Math.Min(list.Count, (page + 1) * 20), list.Count));
                }, list.Count, 20, false);
            }
        }

        [RequireBotPermission(GuildPermission.MentionEveryone)]
        [CommandSummary("設定通知訊息\n" +
            "未輸入通知訊息時，會清除自訂通知訊息\n" +
            "輸入 `-` 可關閉該通知類型\n" +
            "請先新增直播通知，再設定通知訊息（`/help get-command-help chzzk add`）\n\n" +
            "（若通知訊息要提及特定身分組，Bot 必須具備提及所有身分組權限）")]
        [CommandExample("64d76089fba26b180d9c9e48a32600d9 開台啦",
            "https://chzzk.naver.com/64d76089fba26b180d9c9e48a32600d9 開台啦")]
        [DefaultMemberPermissions(GuildPermission.ManageMessages)]
        [SlashCommand("set-message", "設定通知訊息")]
        public async Task SetMessage([Summary("channel", "頻道名稱"), Autocomplete(typeof(GuildNoticeChzzkChannelIdAutocompleteHandler))] string channelId,
            [Summary("notification-type", "通知類型")] ChzzkService.NoticeType noticeType,
            [Summary("message", "通知訊息")] string message = "")
        {
            await DeferAsync(true).ConfigureAwait(false);

            using (var db = _dbService.GetDbContext())
            {
                string locale = await GetLocaleAsync(true);
                var notice = db.NoticeChzzkStreamChannels.FirstOrDefault(
                    (x) => x.GuildId == Context.Guild.Id && x.NoticeChzzkChannelId == channelId);
                if (notice == null)
                {
                    string addPath = CommandDisplayResolver.GetCommandPath(locale, "chzzk", "add");
                    await SendLocalizedErrorAsync("Chzzk.Notifications.ConfigureFirst", true, true,
                        channelId, addPath).ConfigureAwait(false);
                    return;
                }

                string noticeTypeString;
                message = message.Trim();
                switch (noticeType)
                {
                    case ChzzkService.NoticeType.StartStream:
                        notice.StartStreamMessage = message;
                        noticeTypeString = BotLocalizer.Get("Chzzk.NoticeType.Start", locale);
                        break;
                    default:
                        notice.EndStreamMessage = message;
                        noticeTypeString = BotLocalizer.Get("Chzzk.NoticeType.End", locale);
                        break;
                }

                db.NoticeChzzkStreamChannels.Update(notice);
                db.SaveChanges();
                _service.InvalidateNoticeCache();

                string channelName = db.ChzzkSpider.AsNoTracking()
                    .FirstOrDefault((x) => x.ChannelId == channelId)?.ChannelName ?? channelId;
                string result;
                if (message == "-")
                {
                    result = BotLocalizer.Format("Notifications.TypeDisabled", locale, channelName, noticeTypeString);
                }
                else if (message != "")
                {
                    result = BotLocalizer.Format("Notifications.MessageSet", locale, channelName, noticeTypeString, message);
                }
                else
                {
                    result = BotLocalizer.Format("Notifications.MessageCleared", locale, channelName, noticeTypeString);
                }

                await Context.Interaction.SendConfirmAsync(result, true, true).ConfigureAwait(false);
            }
        }

        string GetCurrentMessage(string message, string locale)
            => message == "-" ? BotLocalizer.Get("Notifications.TypeDisabledValue", locale) : message;

        [DefaultMemberPermissions(GuildPermission.ManageMessages)]
        [SlashCommand("list-message", "列出已設定的 CHZZK 直播通知訊息")]
        public async Task ListMessage([Summary("page", "頁數")] int page = 0)
        {
            try
            {
                string locale = await GetLocaleAsync(false);
                using (var db = _dbService.GetDbContext())
                {
                    var notices = db.NoticeChzzkStreamChannels.AsNoTracking()
                        .Where((x) => x.GuildId == Context.Guild.Id).ToList();
                    if (notices.Count == 0)
                    {
                        string addPath = CommandDisplayResolver.GetCommandPath(locale, "chzzk", "add");
                        await SendLocalizedErrorAsync("Chzzk.Notifications.ConfigureAnyFirst", false, true, addPath).ConfigureAwait(false);
                        return;
                    }

                    var names = db.ChzzkSpider.AsNoTracking().ToDictionary(x => x.ChannelId, x => x.ChannelName);
                    var dic = notices.ToDictionary(
                        item => names.GetValueOrDefault(item.NoticeChzzkChannelId, item.NoticeChzzkChannelId),
                        item => BotLocalizer.Format("Chzzk.Messages.ListValue", locale,
                            GetCurrentMessage(item.StartStreamMessage, locale),
                            GetCurrentMessage(item.EndStreamMessage, locale)));

                    await Context.SendPaginatedConfirmAsync(BotLocalizer, locale, page, (page) =>
                    {
                        EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor()
                            .WithTitle(BotLocalizer.Get("Chzzk.Messages.ListTitle", locale))
                            .WithDescription(BotLocalizer.Get("Notifications.MessageListDescription", locale));

                        foreach (var item in dic.Skip(page * 10).Take(10))
                        {
                            embedBuilder.AddField(item.Key, item.Value);
                        }

                        return embedBuilder;
                    }, dic.Count, 10).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "CHZZK ListMessage");
                await SendLocalizedErrorAsync("Errors.Unknown");
            }
        }
    }
}
