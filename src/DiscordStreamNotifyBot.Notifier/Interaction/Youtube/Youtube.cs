using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Interaction.Attribute;
using DiscordStreamNotifyBot.SharedService.Youtube;
using Video = Google.Apis.YouTube.v3.Data.Video;

namespace DiscordStreamNotifyBot.Interaction.Youtube
{
    [Group("youtube", "YouTube 通知設定")]
    public class Youtube : TopLevelModule<YoutubeStreamService>
    {
        private readonly DiscordSocketClient _client;
        private readonly MainDbService _dbService;

        public class GuildNoticeYoutubeChannelIdAutocompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                return await Task.Run(async () =>
                {
                    using var db = Bot.DbService.GetDbContext();
                    if (!await db.NoticeYoutubeStreamChannel.AsNoTracking().AnyAsync((x) => x.GuildId == context.Guild.Id))
                        return AutocompletionResult.FromSuccess();

                    var candidates = db.NoticeYoutubeStreamChannel
                        .AsNoTracking()
                        .Where((x) => x.GuildId == context.Guild.Id)
                        .Select((x) => new AutocompleteCandidate(
                            db.GetYoutubeChannelTitleByChannelId(x.YouTubeChannelId), x.YouTubeChannelId));

                    try
                    {
                        string value = autocompleteInteraction.Data.Current.Value?.ToString();
                        var results = AutocompleteSearch.Filter(candidates, value)
                            .Select(item => new AutocompleteResult(item.Name, item.Value));
                        return AutocompletionResult.FromSuccess(results);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Demystify(), $"GuildNoticeYoutubeChannelIdAutocompleteHandler");
                        return AutocompletionResult.FromSuccess();
                    }
                });
            }
        }

        public Youtube(DiscordSocketClient client, MainDbService dbService)
        {
            _client = client;
            _dbService = dbService;
        }

        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [DefaultMemberPermissions(GuildPermission.ManageMessages)]
        [SlashCommand("list-record-channel", "顯示直播記錄頻道")]
        public async Task ListRecordChannel([Summary("page", "頁數")] int page = 0)
        {
            string locale = await GetLocaleAsync(false);
            using (var db = _dbService.GetDbContext())
            {
                if (db.RecordYoutubeChannel.Any())
                {
                    var list = new List<string>();

                    foreach (var item in db.RecordYoutubeChannel.ToList().Chunk(50))
                    {
                        list.AddRange(await _service.GetChannelTitle(item.Select((x) => x.YoutubeChannelId), true));
                    }

                    list.Sort();
                    await Context.SendPaginatedConfirmAsync(BotLocalizer, locale, page, page =>
                    {
                        return new EmbedBuilder()
                            .WithOkColor()
                            .WithTitle(BotLocalizer.Get("Youtube.RecordList.Title", locale))
                            .WithDescription(string.Join('\n', list.Skip(page * 20).Take(20)))
                            .WithFooter(BotLocalizer.Format("Common.ChannelCountFooter", locale,
                                Math.Min(list.Count, (page + 1) * 20), list.Count));
                    }, list.Count, 20, false);
                }
                else await SendLocalizedErrorAsync("Youtube.RecordList.Empty").ConfigureAwait(false);
            }
        }

        [SlashCommand("now-streaming", "取得現在直播的成員")]
        public async Task NowStreaming(YoutubeStreamService.NowStreamingHost host)
        {
            string locale = await GetLocaleAsync(false);
            var embed = await _service.GetNowStreamingChannel(host, locale).ConfigureAwait(false);

            if (embed == null)
            {
                await SendLocalizedErrorAsync("Youtube.NowStreaming.Failed").ConfigureAwait(false);
                return;
            }

            await Context.Interaction.RespondAsync(embed: embed).ConfigureAwait(false);
        }

        [SlashCommand("coming-soon-stream", "顯示接下來直播的清單")]
        public async Task ComingSoonStream([Summary("page", "頁數")] int page = 0)
        {
            try
            {
                string locale = await GetLocaleAsync(false);
                List<Video> result = new List<Video>();

                // 接下來開台的清單改由 DB 查詢（偵測排程的 Reminders 在 Scraper 端，不跨程序共享）
                List<string> videoIds = new List<string>();
                using (var reminderDb = _dbService.GetDbContext())
                {
                    videoIds.AddRange(reminderDb.HoloVideos.AsNoTracking().Where((x) => x.ScheduledStartTime > DateTime.Now && !x.IsPrivate).Select((x) => x.VideoId));
                    videoIds.AddRange(reminderDb.NijisanjiVideos.AsNoTracking().Where((x) => x.ScheduledStartTime > DateTime.Now && !x.IsPrivate).Select((x) => x.VideoId));
                    videoIds.AddRange(reminderDb.OtherVideos.AsNoTracking().Where((x) => x.ScheduledStartTime > DateTime.Now && !x.IsPrivate).Select((x) => x.VideoId));
                }

                for (int i = 0; i < videoIds.Count; i += 50)
                {
                    var yt = _service.YouTubeService.Videos.List("snippet,liveStreamingDetails");
                    yt.Id = string.Join(',', videoIds.Skip(i).Take(50));
                    result.AddRange((await yt.ExecuteAsync().ConfigureAwait(false)).Items);
                }

                using (var db = _dbService.GetDbContext())
                {
                    result = result.OrderBy((x) => x.LiveStreamingDetails.ScheduledStartTimeDateTimeOffset).ToList();
                    await Context.SendPaginatedConfirmAsync(BotLocalizer, locale, page, (act) =>
                    {
                        return new EmbedBuilder().WithOkColor()
                        .WithTitle(BotLocalizer.Get("Youtube.Upcoming.Title", locale))
                        .WithDescription(string.Join("\n\n",
                           result.Skip(act * 7).Take(7)
                           .Select((x) => BotLocalizer.Format("Youtube.Upcoming.Entry", locale,
                               Format.Url(x.Snippet.Title, $"https://www.youtube.com/watch?v={x.Id}"),
                               Format.Url(x.Snippet.ChannelTitle, $"https://www.youtube.com/channel/{x.Snippet.ChannelId}"),
                               TimestampTag.FromDateTimeOffset(x.LiveStreamingDetails.ScheduledStartTimeDateTimeOffset.Value, TimestampTagStyles.LongDateTime),
                               BotLocalizer.Get(db.RecordYoutubeChannel.Any(x2 => x2.YoutubeChannelId.Trim() == x.Snippet.ChannelId)
                                   ? "Common.Yes" : "Common.No", locale)))));
                    }, result.Count, 7).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "r\n" + ex.StackTrace);
                await SendLocalizedErrorAsync("Errors.Unknown", true);
            }
        }

        [SlashCommand("get-member-only-playlist", "將頻道網址轉換成會員限定清單網址")]
        public async Task GetMemberOnlyPlayListAsync([Summary("channel", "頻道網址")] string channelUrl)
        {
            await DeferAsync(true);

            try
            {
                string channelId = "";
                try
                {
                    channelId = await _service.GetChannelIdAsync(channelUrl).ConfigureAwait(false);
                    await SendLocalizedConfirmAsync("Youtube.MemberPlaylist", true, true,
                        $"https://www.youtube.com/playlist?list={channelId.Replace("UC", "UUMO")}");
                }
                catch (FormatException)
                {
                    await SendLocalizedErrorAsync("Errors.InvalidYoutubeChannel", true);
                    return;
                }
                catch (ArgumentNullException)
                {
                    await SendLocalizedErrorAsync("Errors.UrlRequired", true);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"GetMemberOnlyPlayListAsync: {channelUrl}");
                await SendLocalizedErrorAsync("Errors.Unknown", true);
            }
        }

        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(GuildPermission.ManageEvents)]
        [RequireUserPermission(GuildPermission.ManageEvents)]
        [DefaultMemberPermissions(GuildPermission.ManageEvents)]
        [SlashCommand("toggle-create-event", "當收到新直播通知時同時在 Discord 上建立該直播的新活動")]
        public async Task ToggleCreateEvent([Summary("channel", "頻道名稱"), Autocomplete(typeof(GuildNoticeYoutubeChannelIdAutocompleteHandler))] string channelName)
        {
            await DeferAsync(true);

            try
            {
                string channelId = "";
                try
                {
                    channelId = await _service.GetChannelIdAsync(channelName).ConfigureAwait(false);
                }
                catch (FormatException)
                {
                    await SendLocalizedErrorAsync("Errors.InvalidYoutubeChannel", true);
                    return;
                }
                catch (ArgumentNullException)
                {
                    await SendLocalizedErrorAsync("Errors.UrlRequired", true);
                    return;
                }

                using var db = _dbService.GetDbContext();
                var noticeYoutubeStreamChannel = db.NoticeYoutubeStreamChannel.First((x) => x.GuildId == Context.Guild.Id && x.YouTubeChannelId == channelId);

                //var channel = Context.Guild.GetTextChannel(noticeYoutubeStreamChannel.DiscordNoticeStreamChannelId);
                //if (channel == null)
                //{
                //    await Context.Interaction.SendErrorAsync($"無法獲取 `{noticeYoutubeStreamChannel.YouTubeChannelId}` 所設定的通知頻道，請重新加入通知後重試", true);
                //    db.NoticeYoutubeStreamChannel.Remove(noticeYoutubeStreamChannel);
                //    db.SaveChanges();
                //    return;
                //}

                // 不知道為啥 CreateEvents 權限歸類在頻道內，但明明這權限要從伺服器身分組那邊設定
                // 故需要直接建立活動來驗證權限是否正常
                //var permission = Context.Guild.GetUser(Context.Client.CurrentUser.Id).GetPermissions(channel);
                //if (!permission.CreateEvents)
                //{
                //    await Context.Interaction.SendErrorAsync($"我在伺服器沒有 `建立 & 管理活動 ` 的權限，請給予權限後再次執行本指令", true);
                //    return;
                //}

                // 經測試，只要有管理活動的權限就可以建立，不用另外去伺服器用戶組那邊開建立活動權限
                //try
                //{
                //    var testEvent = await Context.Guild.CreateEventAsync("測試用活動",
                //        DateTimeOffset.Now.AddHours(1),
                //        GuildScheduledEventType.External,
                //        endTime: DateTimeOffset.Now.AddHours(2),
                //        location: "https://www.youtube.com/watch?v=dQw4w9WgXcQ");
                //    await testEvent.DeleteAsync();
                //}
                //catch (Discord.Net.HttpException httpEx) when (httpEx.DiscordCode == DiscordErrorCode.MissingPermissions)
                //{
                //    await Context.Interaction.SendErrorAsync($"我在伺服器沒有 `管理活動 ` 的權限，請給予權限後再次執行本指令", true);
                //    return;
                //}

                //if (!noticeYoutubeStreamChannel.IsCreateEventForNewStream && noticeYoutubeStreamChannel.NewStreamMessage == "-")
                //{
                //    if (await PromptUserConfirmAsync("開啟此功能需要同時開啟新待機所通知，是否開啟?"))
                //    {
                //        noticeYoutubeStreamChannel.NewStreamMessage = "";
                //    }
                //    else
                //    {
                //        return;
                //    }
                //}

                noticeYoutubeStreamChannel.IsCreateEventForNewStream = !noticeYoutubeStreamChannel.IsCreateEventForNewStream;
                db.NoticeYoutubeStreamChannel.Update(noticeYoutubeStreamChannel);

                var channelTitle = db.GetYoutubeChannelTitleByChannelId(channelId);
                if (noticeYoutubeStreamChannel.IsCreateEventForNewStream)
                {
                    await SendLocalizedConfirmAsync("Youtube.Events.Enabled", true, true, channelTitle);

                    if (noticeYoutubeStreamChannel.NewStreamMessage != "-")
                    {
                        try
                        {
                            if (await PromptUserConfirmAsync("Youtube.Events.DisableNewStreamPrompt"))
                            {
                                noticeYoutubeStreamChannel.NewStreamMessage = "-";
                                await SendLocalizedConfirmAsync("Youtube.Notifications.TypeDisabled", true, true,
                                    channelTitle, BotLocalizer.Get("Youtube.NoticeType.NewStream", await GetLocaleAsync(true)));
                            }
                        }
                        catch { }
                    }
                }
                else
                {
                    await SendLocalizedConfirmAsync("Youtube.Events.Disabled", true, true, channelTitle);
                }

                db.SaveChanges();
                _service.InvalidateNoticeCache();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "CreateEvent");
                await SendLocalizedErrorAsync("Errors.Unknown", true);
            }
        }

        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(GuildPermission.ManageGuild)]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        [DefaultMemberPermissions(GuildPermission.ManageGuild)]
        [CommandSummary("設定伺服器橫幅使用指定頻道的最新影片(直播)縮圖\n" +
            "若未輸入頻道網址則關閉本設定\n\n" +
            "Bot 需要有管理伺服器權限\n" +
            "且伺服器需有 Boost Lv2 才可使用本設定\n" +
            "(此功能依賴直播通知，請確保設定的頻道在兩大箱或是爬蟲清單內)")]
        [CommandExample("https://www.youtube.com/@998rrr")]
        [SlashCommand("set-banner-change", "設定伺服器橫幅使用指定頻道的最新影片(直播)縮圖")]
        public async Task SetBannerChange([Summary("channel-url", "頻道網址")] string channelUrl = "")
        {
            using (var db = _dbService.GetDbContext())
            {
                if (channelUrl == "")
                {
                    if (db.BannerChange.Any((x) => x.GuildId == Context.Guild.Id))
                    {
                        var guild = db.BannerChange.First((x) => x.GuildId == Context.Guild.Id);
                        db.BannerChange.Remove(guild);
                        db.SaveChanges();
                        await SendLocalizedConfirmAsync("Youtube.Banner.Removed").ConfigureAwait(false);
                    }
                    else
                    {
                        await SendLocalizedErrorAsync("Youtube.Banner.NotConfigured").ConfigureAwait(false);
                    }
                }
                else
                {
                    if (Context.Guild.PremiumTier < PremiumTier.Tier2)
                    {
                        await SendLocalizedErrorAsync("Youtube.Banner.RequiresTier2").ConfigureAwait(false);
                        return;
                    }

                    await DeferAsync().ConfigureAwait(false);

                    string channelId = "";
                    try
                    {
                        channelId = await _service.GetChannelIdAsync(channelUrl).ConfigureAwait(false);
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

                    string channelTitle = await _service.GetChannelTitle(channelId);
                    if (channelTitle == "")
                    {
                        await SendLocalizedErrorAsync("Errors.ChannelNotFound", true, true, channelId).ConfigureAwait(false);
                        return;
                    }

                    if (db.BannerChange.Any((x) => x.GuildId == Context.Guild.Id))
                    {
                        var guild = db.BannerChange.First((x) => x.GuildId == Context.Guild.Id);
                        guild.ChannelId = channelId;
                        db.BannerChange.Update(guild);
                    }
                    else
                    {
                        db.BannerChange.Add(new BannerChange() { GuildId = Context.Guild.Id, ChannelId = channelId });
                    }

                    await SendLocalizedConfirmAsync("Youtube.Banner.Changed", true, false, channelTitle).ConfigureAwait(false);
                    db.SaveChanges();
                }
            }
        }

        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [DefaultMemberPermissions(GuildPermission.ManageMessages)]
        [CommandSummary("新增直播開台通知的頻道\n" +
            "輸入 `holo` 通知全部 `Holo成員` 的直播\n" +
            "輸入 `2434` 通知全部 `彩虹社成員` 的直播\n" +
            "(僅JP、EN 跟 VR 的成員歸類在此選項內，如需其他成員建議先用 `/youtube-spider add` 設定)\n" +
            "輸入 `other` 通知部分 `非兩大箱` 的直播\n" +
            "(可以使用 `/youtube-spider list` 查詢有哪些頻道)")]
        [CommandExample("https://www.youtube.com/@998rrr", "other", "2434")]
        [SlashCommand("add", "新增YouTube直播開台通知的頻道")]
        public async Task AddChannel([Summary("channel-or-group", "頻道網址")] string channelUrl,
            [Summary("channel", "發送通知的頻道"), ChannelTypes(ChannelType.Text, ChannelType.News)] IChannel channel)
        {
            try
            {
                await DeferAsync(true).ConfigureAwait(false);

                var textChannel = channel as IGuildChannel;
                string locale = await GetLocaleAsync(true);
                var permissions = Context.Guild.GetUser(_client.CurrentUser.Id).GetPermissions(textChannel);
                if (!permissions.ViewChannel || !permissions.SendMessages)
                {
                    await SendLocalizedErrorAsync("Permissions.MissingChannelPermissions", true, true,
                        $"`{textChannel}`", BotLocalizer.Format("Permissions.List", locale,
                            BotLocalizer.Get("Permissions.Name.ViewChannel", locale),
                            BotLocalizer.Get("Permissions.Name.SendMessages", locale))).ConfigureAwait(false);
                    return;
                }

                if (!permissions.EmbedLinks)
                {
                    await SendLocalizedErrorAsync("Permissions.MissingChannelPermissions", true, true,
                        $"`{textChannel}`", BotLocalizer.Get("Permissions.Name.EmbedLinks", locale)).ConfigureAwait(false);
                    return;
                }

                string channelId = "";
                try
                {
                    channelId = await _service.GetChannelIdAsync(channelUrl);
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

                using (var db = _dbService.GetDbContext())
                {
                    await CheckIsFirstSetNoticeAndSendWarningMessageAsync(db);

                    var noticeYoutubeStreamChannel = db.NoticeYoutubeStreamChannel.FirstOrDefault((x) => x.GuildId == Context.Guild.Id && x.YouTubeChannelId == channelId);
                    if (noticeYoutubeStreamChannel != null)
                    {
                        if (await PromptUserConfirmAsync("Notifications.OverwritePrompt", channelId).ConfigureAwait(false))
                        {
                            noticeYoutubeStreamChannel.DiscordNoticeStreamChannelId = textChannel.Id;
                            db.NoticeYoutubeStreamChannel.Update(noticeYoutubeStreamChannel);
                            db.SaveChanges();
                            _service.InvalidateNoticeCache();
                            await SendLocalizedConfirmAsync("Youtube.Notifications.StreamChannelChanged", true, true,
                                channelId, textChannel).ConfigureAwait(false);
                        }
                        return;
                    }

                    if (channelId == "holo" || channelId == "2434" || channelId == "other")
                    {
                        db.NoticeYoutubeStreamChannel.Add(new NoticeYoutubeStreamChannel()
                        {
                            GuildId = Context.Guild.Id,
                            DiscordNoticeStreamChannelId = textChannel.Id,
                            DiscordNoticeVideoChannelId = textChannel.Id,
                            YouTubeChannelId = channelId
                        });
                        await SendLocalizedConfirmAsync("Youtube.Notifications.Added", true, true, channelId).ConfigureAwait(false);
                    }
                    else
                    {
                        string channelTitle = await _service.GetChannelTitle(channelId);
                        if (channelTitle == "")
                        {
                            await SendLocalizedErrorAsync("Errors.ChannelNotFound", true, true, channelId).ConfigureAwait(false);
                            return;
                        }

                        string videoNoticePath = CommandDisplayResolver.GetCommandPath(locale, "youtube", "set-video-notice-channel");
                        string addString = BotLocalizer.Format("Youtube.Notifications.VideoChannelHint", locale, videoNoticePath);
                        if (!db.YoutubeChannelSpider.Any((x) => x.ChannelId == channelId) && !SharedExtensions.IsChannelInDb(channelId))
                        {
                            string spiderPath = CommandDisplayResolver.GetCommandPath(locale, "youtube-spider", "add");
                            addString += BotLocalizer.Format("Notifications.SpiderWarning", locale, spiderPath);
                        }

                        db.NoticeYoutubeStreamChannel.Add(new NoticeYoutubeStreamChannel()
                        {
                            GuildId = Context.Guild.Id,
                            DiscordNoticeStreamChannelId = textChannel.Id,
                            DiscordNoticeVideoChannelId = textChannel.Id,
                            YouTubeChannelId = channelId
                        });
                        await SendLocalizedConfirmAsync("Youtube.Notifications.AddedWithHint", true, true,
                            channelTitle, addString).ConfigureAwait(false);
                    }

                    db.SaveChanges();
                    _service.InvalidateNoticeCache();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"YouTube Add: {Context.Guild.Id} - {channelUrl}");
                await SendLocalizedErrorAsync("Errors.Unknown", true);
            }
        }

        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [DefaultMemberPermissions(GuildPermission.ManageMessages)]
        [CommandSummary("移除通知頻道\n" +
            "輸入 `holo` 移除全部 `Holo成員` 的直播通知\n" +
            "輸入 `2434` 移除全部 `彩虹社成員` 的直播通知\n" +
            "輸入 `other` 移除部分 `非兩大箱` 的直播通知\n" +
            "輸入 `all` 移除全部的直播通知")]
        [CommandExample("https://www.youtube.com/@998rrr", "all", "2434")]
        [SlashCommand("remove", "移除 YouTube 直播開台通知的頻道")]
        public async Task RemoveChannel([Summary("channel", "頻道名稱"), Autocomplete(typeof(GuildNoticeYoutubeChannelIdAutocompleteHandler))] string channelName)
        {
            await DeferAsync(true).ConfigureAwait(false);

            string channelId = "";
            try
            {
                channelId = await _service.GetChannelIdAsync(channelName).ConfigureAwait(false);
            }
            catch (FormatException)
            {
                await SendLocalizedErrorAsync("Errors.InvalidYoutubeChannel", true);
                return;
            }
            catch (ArgumentNullException)
            {
                await SendLocalizedErrorAsync("Errors.UrlRequired", true);
                return;
            }

            using (var db = _dbService.GetDbContext())
            {
                if (!db.NoticeYoutubeStreamChannel.Any((x) => x.GuildId == Context.Guild.Id))
                {
                    await SendLocalizedErrorAsync("Youtube.Notifications.NoneConfigured", true).ConfigureAwait(false);
                    return;
                }

                if (channelId == "all")
                {
                    if (await PromptUserConfirmAsync("Youtube.Notifications.RemoveAllPrompt").ConfigureAwait(false))
                    {
                        db.NoticeYoutubeStreamChannel.RemoveRange(Queryable.Where(db.NoticeYoutubeStreamChannel, (x) => x.GuildId == Context.Guild.Id));
                        await SendLocalizedConfirmAsync("Notifications.AllRemoved", true, true).ConfigureAwait(false);
                        db.SaveChanges();
                        _service.InvalidateNoticeCache();
                        return;
                    }
                    else return;
                }

                if (!db.NoticeYoutubeStreamChannel.Any((x) => x.GuildId == Context.Guild.Id && x.YouTubeChannelId == channelId))
                {
                    await SendLocalizedErrorAsync("Notifications.NotConfigured", true, true, channelId).ConfigureAwait(false);
                }
                else
                {
                    db.NoticeYoutubeStreamChannel.Remove(db.NoticeYoutubeStreamChannel.First((x) => x.GuildId == Context.Guild.Id && x.YouTubeChannelId == channelId));
                    await SendLocalizedConfirmAsync("Notifications.Removed", true, true, channelId).ConfigureAwait(false);

                    db.SaveChanges();
                    _service.InvalidateNoticeCache();
                }
            }
        }

        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [DefaultMemberPermissions(GuildPermission.ManageMessages)]
        [SlashCommand("set-video-notice-channel", "設定 YouTube 影片上傳通知頻道")]
        public async Task SetVideoNoticeChannel([Summary("youtube-channel", "頻道網址"), Autocomplete(typeof(GuildNoticeYoutubeChannelIdAutocompleteHandler))] string channelName,
             [Summary("notification-channel", "發送通知的頻道"), ChannelTypes(ChannelType.Text, ChannelType.News)] IChannel channel)
        {
            await DeferAsync(true).ConfigureAwait(false);

            var textChannel = channel as IGuildChannel;
            string locale = await GetLocaleAsync(true);
            var permissions = Context.Guild.GetUser(_client.CurrentUser.Id).GetPermissions(textChannel);
            if (!permissions.ViewChannel || !permissions.SendMessages)
            {
                await SendLocalizedErrorAsync("Permissions.MissingChannelPermissions", true, true,
                    $"`{textChannel}`", BotLocalizer.Format("Permissions.List", locale,
                        BotLocalizer.Get("Permissions.Name.ViewChannel", locale),
                        BotLocalizer.Get("Permissions.Name.SendMessages", locale))).ConfigureAwait(false);
                return;
            }

            if (!permissions.EmbedLinks)
            {
                await SendLocalizedErrorAsync("Permissions.MissingChannelPermissions", true, true,
                    $"`{textChannel}`", BotLocalizer.Get("Permissions.Name.EmbedLinks", locale)).ConfigureAwait(false);
                return;
            }

            string channelId = "";
            try
            {
                channelId = await _service.GetChannelIdAsync(channelName);
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

            using (var db = _dbService.GetDbContext())
            {
                var noticeYoutubeStreamChannel = db.NoticeYoutubeStreamChannel.FirstOrDefault((x) => x.GuildId == Context.Guild.Id && x.YouTubeChannelId == channelId);
                if (noticeYoutubeStreamChannel != null)
                {
                    noticeYoutubeStreamChannel.DiscordNoticeVideoChannelId = textChannel.Id;
                    db.NoticeYoutubeStreamChannel.Update(noticeYoutubeStreamChannel);
                    db.SaveChanges();
                    _service.InvalidateNoticeCache();
                    await SendLocalizedConfirmAsync("Youtube.Notifications.VideoChannelChanged", true, true,
                        channelId, textChannel).ConfigureAwait(false);
                }
                else
                {
                    string addPath = CommandDisplayResolver.GetCommandPath(locale, "youtube", "add");
                    await SendLocalizedErrorAsync("Youtube.Notifications.ConfigureFirst", true, true,
                        channelId, addPath).ConfigureAwait(false);
                }
            }
        }

        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [DefaultMemberPermissions(GuildPermission.ManageMessages)]
        [SlashCommand("list", "顯示現在已加入通知清單的 YouTube 頻道")]
        public async Task ListChannel([Summary("page", "頁數")] int page = 0)
        {
            await DeferAsync();

            try
            {
                string locale = await GetLocaleAsync(false);
                using (var db = _dbService.GetDbContext())
                {
                    if (!db.NoticeYoutubeStreamChannel.Any((x) => x.GuildId == Context.Guild.Id))
                    {
                        await SendLocalizedErrorAsync("Youtube.Notifications.Empty", true).ConfigureAwait(false);
                        return;
                    }

                    var ytChannelList = db.NoticeYoutubeStreamChannel
                        .Where((x) => x.GuildId == Context.Guild.Id && x.YouTubeChannelId.StartsWith("UC"))
                        .Select((x) => BotLocalizer.Format("Youtube.Notifications.ListEntry", locale,
                            db.GetYoutubeChannelTitleByChannelId(x.YouTubeChannelId),
                            $"<#{x.DiscordNoticeStreamChannelId}>", $"<#{x.DiscordNoticeVideoChannelId}>"))
                        .ToList();

                    var notYTChannelNoticeList = db.NoticeYoutubeStreamChannel
                        .Where((x) => x.GuildId == Context.Guild.Id && !x.YouTubeChannelId.StartsWith("UC"))
                        .Select((x) => BotLocalizer.Format("Youtube.Notifications.ListEntry", locale,
                            db.GetYoutubeChannelTitleByChannelId(x.YouTubeChannelId),
                            $"<#{x.DiscordNoticeStreamChannelId}>", $"<#{x.DiscordNoticeVideoChannelId}>"))
                        .ToList();

                    ytChannelList.AddRange(notYTChannelNoticeList);

                    await Context.SendPaginatedConfirmAsync(BotLocalizer, locale, page, page =>
                    {
                        return new EmbedBuilder()
                            .WithOkColor()
                            .WithTitle(BotLocalizer.Get("Youtube.Notifications.ListTitle", locale))
                            .WithDescription(string.Join('\n', ytChannelList.Skip(page * 20).Take(20)))
                            .WithFooter(BotLocalizer.Format("Common.ChannelCountFooter", locale,
                                Math.Min(ytChannelList.Count, (page + 1) * 20), ytChannelList.Count));
                    }, ytChannelList.Count, 20, isFollowup: true);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"YouTube ListChannel Error: {Context.Guild.Id}");
                await SendLocalizedErrorAsync("Errors.Unknown");
            }
        }

        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages | GuildPermission.MentionEveryone)]
        [DefaultMemberPermissions(GuildPermission.ManageMessages | GuildPermission.MentionEveryone)]
        [RequireBotPermission(GuildPermission.MentionEveryone)]
        [CommandSummary("設定通知訊息\n" +
            "不輸入通知訊息的話則會關閉該類型的通知\n" +
            "若輸入 `-` 則可以關閉該通知類型\n" +
            "需先新增直播通知後才可設定通知訊息 (`/help get-command-help youtube add`)\n\n" +
            "(考慮到有伺服器需 Ping 特定用戶組的情況，故 Bot 需提及所有身分組權限)")]
        [CommandExample("998rrr 開始直播\\首播 @通知用的用戶組 玖玖巴開台啦",
            "holo 新待機室 @某人 新待機所建立",
            "UCUKD-uaobj9jiqB-VXt71mA 新上傳影片 -",
            "UUMOs5FNYPHeZz5f7N1BDExxfg 結束直播\\首播")]
        [SlashCommand("set-message", "設定 YouTube 通知訊息")]
        public async Task SetMessage([Summary("channel", "頻道網址"), Autocomplete(typeof(GuildNoticeYoutubeChannelIdAutocompleteHandler))] string channelUrl,
            [Summary("notification-type", "通知類型")] YoutubeStreamService.NoticeType noticeType,
            [Summary("message", "通知訊息")] string message = "")
        {
            await DeferAsync(true).ConfigureAwait(false);

            string channelId = "";
            try
            {
                channelId = await _service.GetChannelIdAsync(channelUrl).ConfigureAwait(false);
            }
            catch (FormatException)
            {
                await SendLocalizedErrorAsync("Errors.InvalidYoutubeChannel", true);
                return;
            }
            catch (ArgumentNullException)
            {
                await SendLocalizedErrorAsync("Errors.UrlRequired", true);
                return;
            }

            using (var db = _dbService.GetDbContext())
            {
                var channelTitle = db.GetYoutubeChannelTitleByChannelId(channelId);
                string locale = await GetLocaleAsync(true);
                var noticeStreamChannel = db.NoticeYoutubeStreamChannel.FirstOrDefault((x) => x.GuildId == Context.Guild.Id && x.YouTubeChannelId == channelId);
                if (noticeStreamChannel == null)
                {
                    string addPath = CommandDisplayResolver.GetCommandPath(locale, "youtube", "add");
                    await SendLocalizedErrorAsync("Youtube.Notifications.ConfigureFirst", true, true,
                        channelTitle, addPath).ConfigureAwait(false);

                    return;
                }

                string noticeTypeString = "", result = "";
                message = message.Trim();

                switch (noticeType)
                {
                    case YoutubeStreamService.NoticeType.NewStream:
                        noticeStreamChannel.NewStreamMessage = message;
                        noticeTypeString = BotLocalizer.Get("Youtube.NoticeType.NewStream", locale);
                        break;
                    case YoutubeStreamService.NoticeType.NewVideo:
                        noticeStreamChannel.NewVideoMessage = message;
                        noticeTypeString = BotLocalizer.Get("Youtube.NoticeType.NewVideo", locale);
                        break;
                    case YoutubeStreamService.NoticeType.Start:
                        noticeStreamChannel.StratMessage = message;
                        noticeTypeString = BotLocalizer.Get("Youtube.NoticeType.Start", locale);
                        break;
                    case YoutubeStreamService.NoticeType.End:
                        noticeStreamChannel.EndMessage = message;
                        noticeTypeString = BotLocalizer.Get("Youtube.NoticeType.End", locale);
                        break;
                    case YoutubeStreamService.NoticeType.ChangeTime:
                        noticeStreamChannel.ChangeTimeMessage = message;
                        noticeTypeString = BotLocalizer.Get("Youtube.NoticeType.ChangeTime", locale);
                        break;
                    case YoutubeStreamService.NoticeType.Delete:
                        noticeStreamChannel.DeleteMessage = message;
                        noticeTypeString = BotLocalizer.Get("Youtube.NoticeType.Delete", locale);
                        break;
                }

                if (noticeType == YoutubeStreamService.NoticeType.NewStream && message == "-" && !noticeStreamChannel.IsCreateEventForNewStream)
                {
                    if (await PromptUserConfirmAsync("Youtube.Events.EnableReplacementPrompt"))
                    {
                        result = BotLocalizer.Format("Youtube.Events.Enabled", locale, channelTitle) + "\n";
                        noticeStreamChannel.IsCreateEventForNewStream = true;
                    }
                    else
                    {
                        return;
                    }
                }

                db.NoticeYoutubeStreamChannel.Update(noticeStreamChannel);
                db.SaveChanges();
                _service.InvalidateNoticeCache();

                if (message == "-")
                {
                    result += BotLocalizer.Format("Youtube.Notifications.TypeDisabled", locale, channelTitle, noticeTypeString);
                }
                else if (message != "")
                {
                    result += BotLocalizer.Format("Notifications.MessageSet", locale, channelTitle, noticeTypeString, message);

                    if (noticeType == YoutubeStreamService.NoticeType.End && !db.RecordYoutubeChannel.AsNoTracking().Any((x) => x.YoutubeChannelId == channelId))
                    {
                        result += BotLocalizer.Get("Youtube.Notifications.NoEndWarning", locale);
                    }
                    else if (!db.YoutubeChannelSpider.FirstOrDefault((x) => x.IsTrustedChannel)?.IsTrustedChannel ?? false &&
                        (channelId != "holo" && channelId != "2434" && channelId != "other"))
                    {
                        result += BotLocalizer.Get("Youtube.Notifications.VideoOnlyWarning", locale);
                    }
                }
                else
                {
                    result = BotLocalizer.Format("Notifications.MessageCleared", locale, channelTitle, noticeTypeString);
                }

                await Context.Interaction.SendConfirmAsync(result, true, true).ConfigureAwait(false);
            }
        }

        string GetCurrectMessage(string message, string locale)
            => message == "-" ? BotLocalizer.Get("Notifications.TypeDisabledValue", locale) : message;

        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [DefaultMemberPermissions(GuildPermission.ManageMessages)]
        [SlashCommand("list-message", "列出已設定的通知訊息")]
        public async Task ListMessage([Summary("page", "頁數")] int page = 0)
        {
            try
            {
                string locale = await GetLocaleAsync(false);
                using var db = _dbService.GetDbContext();
                if (await db.NoticeYoutubeStreamChannel.AnyAsync((x) => x.GuildId == Context.Guild.Id))
                {
                    var noticeStreamChannels = db.NoticeYoutubeStreamChannel.AsNoTracking().Where((x) => x.GuildId == Context.Guild.Id).ToList();
                    Dictionary<string, string> dic = new Dictionary<string, string>();

                    foreach (var item in noticeStreamChannels)
                    {
                        var channelTitle = item.YouTubeChannelId;
                        if (channelTitle.StartsWith("UC"))
                        {
                            var ytChannelTitle = db.GetYoutubeChannelTitleByChannelId(channelTitle);
                            channelTitle = (ytChannelTitle.StartsWith("UC") ? BotLocalizer.Get("Youtube.ChannelNameMissing", locale) : ytChannelTitle) + $" ({item.YouTubeChannelId})";
                        }

                        dic.Add(channelTitle,
                            BotLocalizer.Format("Youtube.Messages.ListValue", locale,
                                BotLocalizer.Get(item.IsCreateEventForNewStream ? "Common.Yes" : "Common.No", locale),
                                GetCurrectMessage(item.NewStreamMessage, locale),
                                GetCurrectMessage(item.NewVideoMessage, locale),
                                GetCurrectMessage(item.StratMessage, locale),
                                GetCurrectMessage(item.EndMessage, locale),
                                GetCurrectMessage(item.ChangeTimeMessage, locale),
                                GetCurrectMessage(item.DeleteMessage, locale)));
                    }

                    await Context.SendPaginatedConfirmAsync(BotLocalizer, locale, page, (page) =>
                    {
                        EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor()
                            .WithTitle(BotLocalizer.Get("Youtube.Messages.ListTitle", locale))
                            .WithDescription(BotLocalizer.Get("Notifications.MessageListDescription", locale));

                        foreach (var item in dic.Skip(page * 4).Take(4))
                        {
                            embedBuilder.AddField(item.Key, item.Value);
                        }

                        return embedBuilder;
                    }, dic.Count, 4);
                }
                else
                {
                    string addPath = CommandDisplayResolver.GetCommandPath(locale, "youtube", "add");
                    await SendLocalizedErrorAsync("Youtube.Notifications.ConfigureAnyFirst", false, true, addPath).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "YouTube ListMessage");
                await SendLocalizedErrorAsync("Errors.Unknown");
            }
        }
    }
}
