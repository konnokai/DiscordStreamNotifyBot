using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Interaction.Attribute;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.Twitch;

namespace DiscordStreamNotifyBot.Interaction.Twitch
{
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    [DefaultMemberPermissions(GuildPermission.ManageMessages)]
    [Group("twitch", "Twitch 通知設定")]
    public class Twitch : TopLevelModule<TwitchService>
    {
        private readonly DiscordSocketClient _client;
        private readonly MainDbService _dbService;

        public class GuildNoticeTwitchChannelIdAutocompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                return await Task.Run(async () =>
                {
                    using var db = Bot.DbService.GetDbContext();
                    if (!await db.NoticeTwitchStreamChannels.AsNoTracking().AnyAsync((x) => x.GuildId == context.Guild.Id))
                        return AutocompletionResult.FromSuccess();

                    var candidates = db.NoticeTwitchStreamChannels
                        .AsNoTracking()
                        .Where((x) => x.GuildId == context.Guild.Id)
                        .Select((x) => new AutocompleteCandidate(
                            db.GetTwitchUserNameByUserId(x.NoticeTwitchUserId), x.NoticeTwitchUserId));

                    try
                    {
                        string value = autocompleteInteraction.Data.Current.Value?.ToString();
                        var results = AutocompleteSearch.Filter(candidates, value)
                            .Select(item => new AutocompleteResult(item.Name, item.Value));
                        return AutocompletionResult.FromSuccess(results);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"GuildNoticeTwitchChannelIdAutocompleteHandler - {ex}");
                        return AutocompletionResult.FromSuccess();
                    }
                });
            }
        }

        public Twitch(DiscordSocketClient client, MainDbService dbService)
        {
            _client = client;
            _dbService = dbService;
        }

        [CommandExample("998rrr", "https://twitch.tv/998rrr")]
        [DefaultMemberPermissions(GuildPermission.ManageMessages)]
        [SlashCommand("add", "新增 Twitch 直播通知的頻道")]
        public async Task AddChannel([Summary("streamer", "頻道網址")] string twitchUrl,
            [Summary("notification-channel", "發送通知的頻道"), ChannelTypes(ChannelType.Text, ChannelType.News)] IChannel channel)
        {
            if (!_service.IsEnable)
            {
                await SendLocalizedErrorAsync("Errors.FeatureDisabled").ConfigureAwait(false);
                return;
            }

            await DeferAsync(true).ConfigureAwait(false);

            try
            {

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

                var userData = await _service.GetUserAsync(twitchUserLogin: _service.GetUserLoginByUrl(twitchUrl));
                if (userData == null)
                {
                    await SendLocalizedErrorAsync("Twitch.Errors.UserNotFound", true);
                    return;
                }

                using (var db = _dbService.GetDbContext())
                {
                    await CheckIsFirstSetNoticeAndSendWarningMessageAsync(db);

                    var noticeTwitchStreamChannel = db.NoticeTwitchStreamChannels.FirstOrDefault((x) => x.GuildId == Context.Guild.Id && x.NoticeTwitchUserId == userData.Id);
                    if (noticeTwitchStreamChannel != null)
                    {
                        if (!await PromptUserConfirmAsync("Notifications.OverwritePrompt", userData.DisplayName).ConfigureAwait(false))
                            return;
                    }

                    var messages = noticeTwitchStreamChannel == null
                        ? new AdminSettingsTwitchMessages()
                        : new AdminSettingsTwitchMessages
                        {
                            Start = noticeTwitchStreamChannel.StartStreamMessage,
                            End = noticeTwitchStreamChannel.EndStreamMessage,
                            Change = noticeTwitchStreamChannel.ChangeStreamDataMessage
                        };
                    var result = await _service.UpsertNotificationAsync(
                        Context.Guild,
                        userData.Id,
                        textChannel.Id,
                        messages,
                        GracefulShutdown.Token);
                    if (result.State != "applied")
                    {
                        await SendLocalizedErrorAsync("Errors.OperationFailed", true);
                        return;
                    }

                    if (noticeTwitchStreamChannel != null)
                    {
                        await SendLocalizedConfirmAsync("Notifications.ChannelChanged", true, true,
                            userData.DisplayName, textChannel).ConfigureAwait(false);
                        return;
                    }

                    string addString = "";
                    if (!db.TwitchSpider.Any((x) => x.UserId == userData.Id))
                    {
                        string spiderPath = CommandDisplayResolver.GetCommandPath(locale, "twitch-spider", "add");
                        addString = BotLocalizer.Format("Notifications.SpiderWarning", locale, spiderPath);
                    }
                    await SendLocalizedConfirmAsync("Twitch.Notifications.Added", true, true,
                        userData.DisplayName, addString).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"Twitch Add Error: {twitchUrl}");
                await SendLocalizedErrorAsync("Errors.OperationFailed", true);
            }
        }

        [CommandExample("998rrr", "https://twitch.tv/998rrr")]
        [DefaultMemberPermissions(GuildPermission.ManageMessages)]
        [SlashCommand("remove", "移除 Twitch 直播通知的頻道")]
        public async Task RemoveChannel([Summary("channel", "頻道名稱"), Autocomplete(typeof(GuildNoticeTwitchChannelIdAutocompleteHandler))] string twitchId)
        {
            using (var db = _dbService.GetDbContext())
            {
                var noticeTwitchStreamChannel = db.NoticeTwitchStreamChannels.FirstOrDefault((x) => x.GuildId == Context.Guild.Id && x.NoticeTwitchUserId == twitchId);

                if (noticeTwitchStreamChannel == null)
                {
                    await SendLocalizedErrorAsync("Notifications.NotConfigured", false, true, twitchId).ConfigureAwait(false);
                }
                else
                {
                    string userName = db.GetTwitchUserNameByUserId(twitchId);
                    var result = await _service.RemoveNotificationAsync(
                        Context.Guild.Id,
                        twitchId,
                        GracefulShutdown.Token);
                    if (result.State != "applied")
                    {
                        await SendLocalizedErrorAsync("Errors.OperationFailed", false, true);
                        return;
                    }

                    await SendLocalizedConfirmAsync("Notifications.Removed", false, true,
                        userName).ConfigureAwait(false);
                }
            }
        }

        [DefaultMemberPermissions(GuildPermission.ManageMessages)]
        [SlashCommand("list", "顯示已加入通知清單的 Twitch 直播頻道")]
        public async Task ListChannel([Summary("page", "頁數")] int page = 0)
        {
            string locale = await GetLocaleAsync(false);
            using (var db = _dbService.GetDbContext())
            {
                var list = Queryable.Where(db.NoticeTwitchStreamChannels, (x) => x.GuildId == Context.Guild.Id)
                    .Select((x) => $"`{db.GetTwitchUserNameByUserId(x.NoticeTwitchUserId)}` => <#{x.DiscordChannelId}>").ToList();
                if (!list.Any()) { await SendLocalizedErrorAsync("Twitch.Notifications.Empty").ConfigureAwait(false); return; }

                await Context.SendPaginatedConfirmAsync(BotLocalizer, locale, page, page =>
                {
                    return new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle(BotLocalizer.Get("Twitch.Notifications.ListTitle", locale))
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
            "請先新增直播通知，再設定通知訊息（`/help get-command-help twitch add`）\n\n" +
            "（若通知訊息要提及特定身分組，Bot 必須具備提及所有身分組權限）")]
        [CommandExample("998rrr 開台啦", "https://twitch.tv/998rrr 開始直播 開台啦")]
        [DefaultMemberPermissions(GuildPermission.ManageMessages)]
        [SlashCommand("set-message", "設定通知訊息")]
        public async Task SetMessage([Summary("channel", "頻道名稱"), Autocomplete(typeof(GuildNoticeTwitchChannelIdAutocompleteHandler))] string twitchId,
            [Summary("notification-type", "通知類型")] TwitchService.NoticeType noticeType,
            [Summary("message", "通知訊息")] string message = "")
        {
            await DeferAsync(true).ConfigureAwait(false);

            if (!int.TryParse(twitchId, out _))
            {
                await SendLocalizedErrorAsync("Twitch.Errors.InvalidAutocompleteValue", true).ConfigureAwait(false);
                return;
            }

            using (var db = _dbService.GetDbContext())
            {
                string locale = await GetLocaleAsync(true);
                var noticeTwitchStreamChannel = db.NoticeTwitchStreamChannels.FirstOrDefault((x) => x.GuildId == Context.Guild.Id && x.NoticeTwitchUserId == twitchId);
                if (noticeTwitchStreamChannel == null)
                {
                    string addPath = CommandDisplayResolver.GetCommandPath(locale, "twitch", "add");
                    await SendLocalizedErrorAsync("Twitch.Notifications.ConfigureFirst", true, true,
                        twitchId, addPath).ConfigureAwait(false);
                    return;
                }
                else
                {
                    string noticeTypeString = "", result = "";

                    message = message.Trim();
                    switch (noticeType)
                    {
                        case TwitchService.NoticeType.StartStream:
                            noticeTwitchStreamChannel.StartStreamMessage = message;
                            noticeTypeString = BotLocalizer.Get("Twitch.NoticeType.Start", locale);
                            break;
                        case TwitchService.NoticeType.EndStream:
                            noticeTwitchStreamChannel.EndStreamMessage = message;
                            noticeTypeString = BotLocalizer.Get("Twitch.NoticeType.End", locale);
                            break;
                        case TwitchService.NoticeType.ChangeStreamData:
                            noticeTwitchStreamChannel.ChangeStreamDataMessage = message;
                            noticeTypeString = BotLocalizer.Get("Twitch.NoticeType.Change", locale);
                            break;
                    }

                    db.NoticeTwitchStreamChannels.Update(noticeTwitchStreamChannel);
                    db.SaveChanges();
                    _service.InvalidateNoticeCache();

                    if (message == "-")
                    {
                        result = BotLocalizer.Format("Notifications.TypeDisabled", locale,
                            db.GetTwitchUserNameByUserId(twitchId), noticeTypeString);
                    }
                    else if (message != "")
                    {
                        result = BotLocalizer.Format("Notifications.MessageSet", locale,
                            db.GetTwitchUserNameByUserId(twitchId), noticeTypeString, message);
                    }
                    else
                    {
                        result = BotLocalizer.Format("Notifications.MessageCleared", locale,
                            db.GetTwitchUserNameByUserId(twitchId), noticeTypeString);
                    }

                    await Context.Interaction.SendConfirmAsync(result, true, true).ConfigureAwait(false);
                }
            }
        }

        string GetCurrectMessage(string message, string locale)
            => message == "-" ? BotLocalizer.Get("Notifications.TypeDisabledValue", locale) : message;

        [DefaultMemberPermissions(GuildPermission.ManageMessages)]
        [SlashCommand("list-message", "列出已設定的 Twitch 直播通知訊息")]
        public async Task ListMessage([Summary("page", "頁數")] int page = 0)
        {
            try
            {
                string locale = await GetLocaleAsync(false);
                using (var db = _dbService.GetDbContext())
                {
                    if (db.NoticeTwitchStreamChannels.Any((x) => x.GuildId == Context.Guild.Id))
                    {
                        var noticeTwitchStreamChannels = db.NoticeTwitchStreamChannels.Where((x) => x.GuildId == Context.Guild.Id);
                        Dictionary<string, string> dic = new Dictionary<string, string>();

                        foreach (var item in noticeTwitchStreamChannels)
                        {
                            dic.Add(db.GetTwitchUserNameByUserId(item.NoticeTwitchUserId),
                                BotLocalizer.Format("Twitch.Messages.ListValue", locale,
                                    GetCurrectMessage(item.StartStreamMessage, locale),
                                    GetCurrectMessage(item.EndStreamMessage, locale),
                                    GetCurrectMessage(item.ChangeStreamDataMessage, locale)));
                        }

                        try
                        {
                            await Context.SendPaginatedConfirmAsync(BotLocalizer, locale, page, (page) =>
                            {
                                EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor()
                                    .WithTitle(BotLocalizer.Get("Twitch.Messages.ListTitle", locale))
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
                            Log.Error(ex.Message + "\n" + ex.StackTrace);
                        }
                    }
                    else
                    {
                        string addPath = CommandDisplayResolver.GetCommandPath(locale, "twitch", "add");
                        await SendLocalizedErrorAsync("Twitch.Notifications.ConfigureAnyFirst", false, true, addPath).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "Twitch ListMessage");
                await SendLocalizedErrorAsync("Errors.Unknown");
            }
        }
    }
}
