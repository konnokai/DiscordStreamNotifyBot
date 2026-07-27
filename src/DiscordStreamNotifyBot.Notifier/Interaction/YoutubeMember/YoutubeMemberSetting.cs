using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Interaction.Attribute;
using DiscordStreamNotifyBot.SharedService.Youtube;
using DiscordStreamNotifyBot.SharedService.YoutubeMember;

namespace DiscordStreamNotifyBot.Interaction.YoutubeMember
{
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    [Group("member-set", "YouTube 會限驗證設定")]
    public class YoutubeMemberSetting : TopLevelModule<YoutubeMemberService>
    {
        private readonly DiscordSocketClient _client;
        private readonly YoutubeStreamService _ytservice;
        private readonly MainDbService _dbService;

        public YoutubeMemberSetting(DiscordSocketClient client, YoutubeStreamService youtubeStreamService, MainDbService dbService)
        {
            _client = client;
            _ytservice = youtubeStreamService;
            _dbService = dbService;
        }

        public class GuildYoutubeMemberCheckChannelIdAutocompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                return await Task.Run(async () =>
                {
                    using var db = Bot.DbService.GetDbContext();
                    if (!await db.GuildYoutubeMemberConfig.AsNoTracking().AnyAsync((x) => x.GuildId == context.Guild.Id))
                        return AutocompletionResult.FromSuccess();

                    var candidates = db.GuildYoutubeMemberConfig
                        .AsNoTracking()
                        .Where((x) => x.GuildId == context.Guild.Id)
                        .Select((x) => new AutocompleteCandidate(x.MemberCheckChannelTitle, x.MemberCheckChannelId));

                    try
                    {
                        string value = autocompleteInteraction.Data.Current.Value?.ToString();
                        var results = AutocompleteSearch.Filter(candidates, value)
                            .Select(item => new AutocompleteResult(item.Name, item.Value));
                        return AutocompletionResult.FromSuccess(results);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"GuildYoutubeMemberCheckChannelIdAutocompleteHandler - {ex}");
                        return AutocompletionResult.FromSuccess();
                    }
                });
            }
        }

        [SlashCommand("set-notice-member-status-channel", "設定會限驗證狀態紀錄頻道")]
        public async Task SetNoticeMemberStatusChannel([Summary("log-channel", "紀錄頻道")] ITextChannel textChannel)
        {
            await DeferAsync(true);

            if (!_service.IsEnable)
            {
                await SendLocalizedErrorAsync("Member.Errors.Disabled", true, true, Bot.ApplicatonOwner);
                return;
            }

            using (var db = _dbService.GetDbContext())
            {
                var permissions = Context.Guild.GetUser(_client.CurrentUser.Id).GetPermissions(textChannel);
                string locale = await GetLocaleAsync(true);
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

                await CheckIsFirstSetNoticeAndSendWarningMessageAsync(db);

                var guildConfig = db.GuildConfig.FirstOrDefault((x) => x.GuildId == Context.Guild.Id);
                if (guildConfig == null)
                {
                    guildConfig = new DataBase.Table.GuildConfig() { GuildId = Context.Guild.Id };
                    db.GuildConfig.Add(guildConfig);
                }

                guildConfig.LogMemberStatusChannelId = textChannel.Id;
                db.GuildConfig.Update(guildConfig);
                db.SaveChanges();

                await SendLocalizedConfirmAsync("MemberSetting.LogChannelChanged", true, false, textChannel);
            }
        }

        [RequireGuildMemberCount(250)]
        [CommandSummary("新增會限驗證頻道，目前可上限為 5 個頻道\n" +
           "如新增同個頻道則可變更要授予的用戶組\n" +
           "伺服器需大於 250 人才可使用\n" +
           "如有任何需要請向擁有者詢問")]
        [CommandExample("https://www.youtube.com/@998rrr @玖桃")]
        [SlashCommand("add-member-check", "新增會限驗證頻道")]
        public async Task AddMemberCheckAsync([Summary("channel-url", "頻道連結")] string url, [Summary("role", "用戶組Id")] IRole role)
        {
            if (!_service.IsEnable)
            {
                await SendLocalizedErrorAsync("Member.Errors.Disabled", false, true, Bot.ApplicatonOwner);
                return;
            }

            var currentBotUser = Context.Guild.GetUser(_client.CurrentUser.Id);
            if (!currentBotUser.GuildPermissions.ManageRoles)
            {
                await SendLocalizedErrorAsync("MemberSetting.Errors.ManageRolesRequired");
                return;
            }

            if (role == Context.Guild.EveryoneRole)
            {
                await SendLocalizedErrorAsync("MemberSetting.Errors.EveryoneRole");
                return;
            }

            using (var db = _dbService.GetDbContext())
            {
                try
                {
                    await DeferAsync(true);

                    if (currentBotUser.Roles.Max(x => x.Position) < role.Position)
                    {
                        await SendLocalizedErrorAsync("MemberSetting.Errors.RoleTooHigh", true, true, role.Name);
                        return;
                    }

                    var guildConfig = db.GuildConfig.FirstOrDefault((x) => x.GuildId == Context.Guild.Id);
                    if (guildConfig == null)
                    {
                        guildConfig = new DataBase.Table.GuildConfig() { GuildId = Context.Guild.Id };
                        db.GuildConfig.Add(guildConfig);
                    }

                    int maxCount = 5;
                    if (guildConfig != null && guildConfig.MaxYouTubeMemberCheckCount > 0)
                        maxCount = (int)guildConfig.MaxYouTubeMemberCheckCount;

                    if (!DiscordStreamNotifyBot.Utility.OfficialGuildContains(Context.Guild.Id) && db.GuildYoutubeMemberConfig.Count((x) => x.GuildId == Context.Guild.Id) >= maxCount)
                    {
                        await SendLocalizedErrorAsync("MemberSetting.Errors.ChannelLimit", true, true, maxCount);
                        return;
                    }

                    // 因 Discord 的 SelectMenu 最多只能有 25 個選項，故暫時先做限制避免遇到選單跑不出來的問題
                    if (db.GuildYoutubeMemberConfig.Count((x) => x.GuildId == Context.Guild.Id) > 25)
                    {
                        await SendLocalizedErrorAsync("MemberSetting.Errors.SelectLimit", true, true, 25);
                        return;
                    }

                    if (guildConfig.LogMemberStatusChannelId == 0)
                    {
                        string logLocale = await GetLocaleAsync(true);
                        string setLogPath = CommandDisplayResolver.GetCommandPath(logLocale, "member-set", "set-notice-member-status-channel");
                        await SendLocalizedErrorAsync("MemberSetting.Errors.LogChannelRequired", true, true, setLogPath);
                        return;
                    }
                    else if (Context.Guild.GetTextChannel(guildConfig.LogMemberStatusChannelId) == null)
                    {
                        string logLocale = await GetLocaleAsync(true);
                        string setLogPath = CommandDisplayResolver.GetCommandPath(logLocale, "member-set", "set-notice-member-status-channel");
                        await SendLocalizedErrorAsync("MemberSetting.Errors.LogChannelDeleted", true, true, setLogPath);

                        guildConfig.LogMemberStatusChannelId = 0;
                        db.GuildConfig.Update(guildConfig);
                        db.SaveChanges();
                        return;
                    }

                    var channelId = await _ytservice.GetChannelIdAsync(url);
                    bool channelDataExist = false;
                    var guildYoutubeMemberConfig = db.GuildYoutubeMemberConfig.FirstOrDefault((x) => x.GuildId == Context.Guild.Id && x.MemberCheckChannelId == channelId);
                    if (guildYoutubeMemberConfig == null)
                    {
                        guildYoutubeMemberConfig = new DataBase.Table.GuildYoutubeMemberConfig()
                        {
                            GuildId = Context.Guild.Id,
                            MemberCheckChannelId = channelId,
                            MemberCheckGrantRoleId = role.Id
                        };

                        var youtubeChannel = db.GuildYoutubeMemberConfig.FirstOrDefault((x) => x.MemberCheckChannelId == channelId && !string.IsNullOrEmpty(x.MemberCheckChannelTitle) && x.MemberCheckVideoId != "-");
                        if (youtubeChannel != null)
                        {
                            guildYoutubeMemberConfig.MemberCheckChannelTitle = youtubeChannel.MemberCheckChannelTitle;
                            guildYoutubeMemberConfig.MemberCheckVideoId = youtubeChannel.MemberCheckVideoId;
                            channelDataExist = true;
                        }

                        db.GuildYoutubeMemberConfig.Add(guildYoutubeMemberConfig);

                        try
                        {
                            await (await Bot.ApplicatonOwner.CreateDMChannelAsync()).SendMessageAsync(embed: new EmbedBuilder()
                                .WithOkColor()
                                .WithTitle("已新增會限驗證頻道")
                                .AddField("頻道", Format.Url(channelId, $"https://www.youtube.com/channel/{channelId}"), false)
                                .AddField("伺服器", $"{Context.Guild.Name} ({Context.Guild.Id})", false)
                                .AddField("執行者", $"{Context.User.Username} ({Context.User.Id})", false).Build());
                        }
                        catch (Exception ex) { Log.Error(ex.ToString()); }
                    }
                    else
                    {
                        channelDataExist = true;
                        guildYoutubeMemberConfig.MemberCheckGrantRoleId = role.Id;
                        db.GuildYoutubeMemberConfig.Update(guildYoutubeMemberConfig);
                    }
                    db.SaveChanges();

                    string locale = await GetLocaleAsync(true);
                    await SendLocalizedConfirmAsync("MemberSetting.ChannelConfigured", true, true,
                        channelId, role.Name, BotLocalizer.Get(channelDataExist
                            ? "MemberSetting.ReadyNow" : "MemberSetting.ReadyLater", locale));
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), "新增會限驗證頻道時失敗");
                    await SendLocalizedErrorAsync("Errors.InvalidYoutubeChannel", true);
                }
            }
        }

        [CommandSummary("移除會限驗證頻道")]
        [CommandExample("https://www.youtube.com/@998rrr")]
        [SlashCommand("remove-member-check", "移除會限驗證頻道")]
        public async Task RemoveMemberCheckAsync([Summary("channel-url", "頻道連結"), Autocomplete(typeof(GuildYoutubeMemberCheckChannelIdAutocompleteHandler))] string url)
        {
            await DeferAsync(true);

            using (var db = _dbService.GetDbContext())
            {
                try
                {
                    var channelId = await _ytservice.GetChannelIdAsync(url);
                    var guildYoutubeMemberConfig = db.GuildYoutubeMemberConfig.FirstOrDefault((x) => x.GuildId == Context.Guild.Id && x.MemberCheckChannelId == channelId);

                    if (guildYoutubeMemberConfig == null)
                    {
                        await SendLocalizedErrorAsync("MemberSetting.Errors.ChannelNotConfigured", true);
                    }
                    else
                    {
                        db.GuildYoutubeMemberConfig.Remove(guildYoutubeMemberConfig);
                        await SendLocalizedConfirmAsync("MemberSetting.ChannelRemoved", true, false, channelId);

                        try
                        {
                            await (await Bot.ApplicatonOwner.CreateDMChannelAsync()).SendMessageAsync(embed: new EmbedBuilder()
                                .WithOkColor()
                                .WithTitle("已移除會限驗證頻道")
                                .AddField("頻道", Format.Url(channelId, $"https://www.youtube.com/channel/{channelId}"), false)
                                .AddField("伺服器", $"{Context.Guild.Name} ({Context.Guild.Id})", false)
                                .AddField("執行者", $"{Context.User.Username} ({Context.User.Id})", false).Build());
                        }
                        catch (Exception ex) { Log.Error(ex.ToString()); }
                    }

                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    await SendLocalizedErrorAsync("Errors.SaveFailed", true);
                    Log.Error(ex.ToString());
                }
            }
        }

        [CommandSummary("手動指定會限驗證用的偵測影片\n" +
            "用於頻道有多階會員時，指定「最低階」的會限影片，避免低階但合法的會員被誤判失敗\n" +
            "指定後自動探索不會再覆寫此影片；該影片失效時會發送通知到通知頻道提醒需重設")]
        [CommandExample("頻道名稱 https://youtu.be/xxxxxxxxxxx")]
        [SlashCommand("set-check-video", "手動指定會限驗證探測影片")]
        public async Task SetCheckVideoAsync(
            [Summary("channel", "頻道名稱"), Autocomplete(typeof(GuildYoutubeMemberCheckChannelIdAutocompleteHandler))] string url,
            [Summary("video", "會限影片連結或ID")] string videoUrlOrId)
        {
            await DeferAsync(true);

            string videoId = ExtractVideoId(videoUrlOrId);
            if (string.IsNullOrEmpty(videoId))
            {
                await SendLocalizedErrorAsync("MemberSetting.Errors.InvalidVideoId", true);
                return;
            }

            using var db = _dbService.GetDbContext();
            try
            {
                var channelId = await _ytservice.GetChannelIdAsync(url);
                var config = db.GuildYoutubeMemberConfig.FirstOrDefault((x) => x.GuildId == Context.Guild.Id && x.MemberCheckChannelId == channelId);
                if (config == null)
                {
                    string locale = await GetLocaleAsync(true);
                    string addPath = CommandDisplayResolver.GetCommandPath(locale, "member-set", "add-member-check");
                    await SendLocalizedErrorAsync("MemberSetting.Errors.ConfigureFirst", true, true, addPath);
                    return;
                }

                // 驗證：用 bot 金鑰探測留言。member-only → 403/forbidden；公開影片 → 200（不可當探針，否則所有人都會通過驗證）
                try
                {
                    var ct = _ytservice.YouTubeService.CommentThreads.List("id");
                    ct.VideoId = videoId;
                    await ct.ExecuteAsync();

                    // 可讀留言 ＝ 非會限影片
                    await SendLocalizedErrorAsync("MemberSetting.Errors.VideoNotMembersOnly", true);
                    return;
                }
                catch (Exception ex)
                {
                    if (ex.Message.ToLower().Contains("disabled comments"))
                    {
                        await SendLocalizedErrorAsync("MemberSetting.Errors.CommentsDisabled", true);
                        return;
                    }
                    // 403 / forbidden / not properly authorized ＝ 會限影片，符合預期，往下設定
                }

                config.MemberCheckVideoId = videoId;
                config.IsManualVideoId = true;
                db.GuildYoutubeMemberConfig.Update(config);
                db.SaveChanges();

                await SendLocalizedConfirmAsync("MemberSetting.CheckVideoChanged", true, false, channelId, videoId);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "手動指定會限驗證影片時失敗");
                await SendLocalizedErrorAsync("Errors.InvalidYoutubeInput", true);
            }
        }

        [CommandSummary("改回自動挑選會限驗證偵測影片（取消手動指定）")]
        [CommandExample("https://www.youtube.com/@998rrr")]
        [SlashCommand("clear-check-video", "改回自動挑選會限驗證偵測影片")]
        public async Task ClearCheckVideoAsync(
            [Summary("channel-url", "頻道連結"), Autocomplete(typeof(GuildYoutubeMemberCheckChannelIdAutocompleteHandler))] string url)
        {
            await DeferAsync(true);

            using var db = _dbService.GetDbContext();
            try
            {
                var channelId = await _ytservice.GetChannelIdAsync(url);
                var config = db.GuildYoutubeMemberConfig.FirstOrDefault((x) => x.GuildId == Context.Guild.Id && x.MemberCheckChannelId == channelId);
                if (config == null)
                {
                    await SendLocalizedErrorAsync("MemberSetting.Errors.ChannelNotConfigured", true);
                    return;
                }

                config.IsManualVideoId = false;
                config.MemberCheckVideoId = "-";
                db.GuildYoutubeMemberConfig.Update(config);
                db.SaveChanges();

                await SendLocalizedConfirmAsync("MemberSetting.CheckVideoCleared", true, false, channelId, 5);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "恢復自動挑選會限驗證影片時失敗");
                await SendLocalizedErrorAsync("Errors.InvalidYoutubeInput", true);
            }
        }

        private string ExtractVideoId(string input)
        {
            try
            {
                return _ytservice.GetVideoId(input);
            }
            catch (ArgumentNullException)
            {
                return null;
            }
            catch (UriFormatException)
            {
                return null;
            }
        }

        [SlashCommand("list-checked-member", "顯示現在已成功驗證的成員清單")]
        public async Task ListCheckedMemberAsync([Summary("page", "頁數")] int page = 1)
        {
            string locale = await GetLocaleAsync(true);
            using (var db = _dbService.GetDbContext())
            {
                var youtubeMemberChecks = db.YoutubeMemberCheck.Where((x) => x.GuildId == Context.Guild.Id && x.IsChecked);
                if (!youtubeMemberChecks.Any())
                {
                    await SendLocalizedErrorAsync("MemberSetting.Errors.NoVerifiedMembers");
                    return;
                }
                page -= 1;
                page = Math.Max(0, page);

                await Context.SendPaginatedConfirmAsync(BotLocalizer, locale, page, (page) =>
                {
                    return new EmbedBuilder().WithOkColor()
                    .WithTitle(BotLocalizer.Get("MemberSetting.VerifiedListTitle", locale))
                    .WithDescription(string.Join('\n',
                        youtubeMemberChecks.Skip(page * 20).Take(20)
                            .Select((x) => $"<@{x.UserId}>: {x.CheckYTChannelId}")));
                }, youtubeMemberChecks.Count(), 20, true, true);
            }
        }
    }
}
