using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Interaction.Attribute;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService.Youtube;
using DiscordStreamNotifyBot.SharedService.YoutubeMember;
using DiscordStreamNotifyBot.SharedService.Member;
using Google;

namespace DiscordStreamNotifyBot.Interaction.YoutubeMember
{
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    [Group("youtube-member-set", "YouTube 會限驗證設定")]
    public class YoutubeMemberSetting : TopLevelModule<YoutubeMemberService>
    {
        private readonly DiscordSocketClient _client;
        private readonly YoutubeStreamService _ytservice;
        private readonly MainDbService _dbService;
        private readonly YoutubeMemberRoleService _roleService;
        private readonly MemberOperationCoordinator _operationCoordinator;

        public YoutubeMemberSetting(
            DiscordSocketClient client,
            YoutubeStreamService youtubeStreamService,
            MainDbService dbService,
            YoutubeMemberRoleService roleService,
            MemberOperationCoordinator operationCoordinator)
        {
            _client = client;
            _ytservice = youtubeStreamService;
            _dbService = dbService;
            _roleService = roleService;
            _operationCoordinator = operationCoordinator;
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

                    var configuredChannels = await db.GuildYoutubeMemberConfig
                        .AsNoTracking()
                        .Where((x) => x.GuildId == context.Guild.Id)
                        .Select(x => new { x.MemberCheckChannelTitle, x.MemberCheckChannelId })
                        .ToListAsync();
                    var duplicateTitles = configuredChannels
                        .Where(x => !string.IsNullOrWhiteSpace(x.MemberCheckChannelTitle))
                        .GroupBy(x => x.MemberCheckChannelTitle, StringComparer.OrdinalIgnoreCase)
                        .Where(group => group.Count() > 1)
                        .Select(group => group.Key)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var candidates = configuredChannels.Select(x => new AutocompleteCandidate(
                        x.MemberCheckChannelTitle,
                        string.IsNullOrWhiteSpace(x.MemberCheckChannelTitle) || duplicateTitles.Contains(x.MemberCheckChannelTitle)
                            ? x.MemberCheckChannelId
                            : x.MemberCheckChannelTitle,
                        x.MemberCheckChannelId));

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

            if (role.IsManaged)
            {
                await SendLocalizedErrorAsync("MemberSetting.Errors.ManagedRole");
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

                    if (guildConfig.VerificationLogChannelId == 0)
                    {
                        string logLocale = await GetLocaleAsync(true);
                        string setLogPath = CommandDisplayResolver.GetCommandPath(logLocale, "utility", "set-verification-log-channel");
                        await SendLocalizedErrorAsync("MemberSetting.Errors.LogChannelRequired", true, true, setLogPath);
                        return;
                    }
                    else if (Context.Guild.GetTextChannel(guildConfig.VerificationLogChannelId) == null)
                    {
                        string logLocale = await GetLocaleAsync(true);
                        string setLogPath = CommandDisplayResolver.GetCommandPath(logLocale, "utility", "set-verification-log-channel");
                        await SendLocalizedErrorAsync("MemberSetting.Errors.LogChannelDeleted", true, true, setLogPath);

                        guildConfig.VerificationLogChannelId = 0;
                        db.GuildConfig.Update(guildConfig);
                        db.SaveChanges();
                        return;
                    }

                    var channelId = await _ytservice.GetChannelIdAsync(url);
                    var guildYoutubeMemberConfig = db.GuildYoutubeMemberConfig.AsNoTracking().FirstOrDefault(
                        (x) => x.GuildId == Context.Guild.Id && x.MemberCheckChannelId == channelId);
                    bool isNewConfiguration = guildYoutubeMemberConfig == null;
                    YoutubeMemberRoleConfigurationResult result = await _roleService.ConfigureRoleAsync(
                        Context.Guild, channelId, role, GracefulShutdown.Token);
                    if (!result.IsSuccess)
                    {
                        await SendLocalizedErrorAsync(result.Error ?? "MemberSetting.Errors.SaveFailed", true);
                        return;
                    }
                    guildYoutubeMemberConfig = result.Config;
                    bool channelDataExist = !string.IsNullOrEmpty(guildYoutubeMemberConfig.MemberCheckChannelTitle) &&
                        guildYoutubeMemberConfig.MemberCheckVideoId != "-";
                    if (isNewConfiguration)
                    {
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
                    db.SaveChanges();

                    string locale = await GetLocaleAsync(true);
                    await SendLocalizedConfirmAsync("MemberSetting.ChannelConfigured", true, true,
                        GetChannelDisplayName(guildYoutubeMemberConfig), role.Name, BotLocalizer.Get(channelDataExist
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
                    var channelId = await ResolveConfiguredChannelIdAsync(url);
                    var guildYoutubeMemberConfig = db.GuildYoutubeMemberConfig.AsNoTracking().FirstOrDefault((x) => x.GuildId == Context.Guild.Id && x.MemberCheckChannelId == channelId);

                    if (guildYoutubeMemberConfig == null)
                    {
                        await SendLocalizedErrorAsync("MemberSetting.Errors.ChannelNotConfigured", true);
                    }
                    else
                    {
                        bool deleted = await _roleService.DeleteConfigurationAsync(guildYoutubeMemberConfig, GracefulShutdown.Token);
                        if (deleted)
                            await SendLocalizedConfirmAsync("MemberSetting.ChannelRemoved", true, false,
                                GetChannelDisplayName(guildYoutubeMemberConfig));
                        else
                        {
                            await SendLocalizedErrorAsync("MemberSetting.Errors.RemovePending", true);
                            return;
                        }

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

            try
            {
                var channelId = await ResolveConfiguredChannelIdAsync(url);
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
                catch (GoogleApiException ex) when (YoutubeMemberApiClient.IsDocumentedMembershipForbidden(ex))
                {
                }
                catch (GoogleApiException ex) when (YoutubeMemberApiClient.HasReason(ex, "commentsDisabled"))
                {
                    await SendLocalizedErrorAsync("MemberSetting.Errors.CommentsDisabled", true);
                    return;
                }
                catch (GoogleApiException)
                {
                    await SendLocalizedErrorAsync("MemberSetting.Errors.InvalidVideoId", true);
                    return;
                }

                // 設定 mutation 與背景 provider result 使用同一把 guild lock；拿鎖後必須重讀 config。
                await using var guildLock = await _operationCoordinator.LockGuildAsync(Context.Guild.Id, GracefulShutdown.Token);
                using var db = _dbService.GetDbContext();
                var config = await db.GuildYoutubeMemberConfig.SingleOrDefaultAsync(
                    x => x.GuildId == Context.Guild.Id && x.MemberCheckChannelId == channelId,
                    GracefulShutdown.Token);
                if (config == null || config.DeletionPending)
                {
                    string locale = await GetLocaleAsync(true);
                    string addPath = CommandDisplayResolver.GetCommandPath(locale, "youtube-member-set", "add-member-check");
                    await SendLocalizedErrorAsync("MemberSetting.Errors.ConfigureFirst", true, true, addPath);
                    return;
                }

                config.MemberCheckVideoId = videoId;
                config.IsManualVideoId = true;
                db.GuildYoutubeMemberConfig.Update(config);
                await db.SaveChangesAsync(GracefulShutdown.Token);

                await SendLocalizedConfirmAsync("MemberSetting.CheckVideoChanged", true, false,
                    GetChannelDisplayName(config), videoId);
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

            try
            {
                var channelId = await ResolveConfiguredChannelIdAsync(url);
                await using var guildLock = await _operationCoordinator.LockGuildAsync(Context.Guild.Id, GracefulShutdown.Token);
                using var db = _dbService.GetDbContext();
                var config = await db.GuildYoutubeMemberConfig.SingleOrDefaultAsync(
                    x => x.GuildId == Context.Guild.Id && x.MemberCheckChannelId == channelId,
                    GracefulShutdown.Token);
                if (config == null)
                {
                    await SendLocalizedErrorAsync("MemberSetting.Errors.ChannelNotConfigured", true);
                    return;
                }

                config.IsManualVideoId = false;
                config.MemberCheckVideoId = "-";
                db.GuildYoutubeMemberConfig.Update(config);
                await db.SaveChangesAsync(GracefulShutdown.Token);

                await SendLocalizedConfirmAsync("MemberSetting.CheckVideoCleared", true, false,
                    GetChannelDisplayName(config), 5);
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

        private async Task<string> ResolveConfiguredChannelIdAsync(string channel)
        {
            using var db = _dbService.GetDbContext();
            List<string> channelIds = await db.GuildYoutubeMemberConfig.AsNoTracking()
                .Where(x => x.GuildId == Context.Guild.Id &&
                    (x.MemberCheckChannelTitle == channel || x.MemberCheckChannelId == channel))
                .Select(x => x.MemberCheckChannelId)
                .Distinct()
                .Take(2)
                .ToListAsync(GracefulShutdown.Token);
            if (channelIds.Count == 1)
                return channelIds[0];
            if (channelIds.Count > 1)
                throw new FormatException("有多個相同名稱的 YouTube 頻道，請從自動完成選單選擇頻道");
            return await _ytservice.GetChannelIdAsync(channel);
        }

        private static string GetChannelDisplayName(DataBase.Table.GuildYoutubeMemberConfig config)
            => string.IsNullOrWhiteSpace(config.MemberCheckChannelTitle)
                ? config.MemberCheckChannelId
                : config.MemberCheckChannelTitle;

        [SlashCommand("list-checked-member", "顯示現在已成功驗證的成員清單")]
        public async Task ListCheckedMemberAsync([Summary("page", "頁數")] int page = 1)
        {
            string locale = await GetLocaleAsync(true);
            using (var db = _dbService.GetDbContext())
            {
                var youtubeMemberChecks = from check in db.YoutubeMemberCheck
                                          join config in db.GuildYoutubeMemberConfig
                                              on new { check.GuildId, ChannelId = check.CheckYTChannelId }
                                              equals new { config.GuildId, ChannelId = config.MemberCheckChannelId }
                                          where check.GuildId == Context.Guild.Id && check.IsChecked &&
                                              !check.PendingRoleRemoval && !config.DeletionPending
                                          select new
                                          {
                                              check.UserId,
                                              config.MemberCheckChannelId,
                                              config.MemberCheckChannelTitle
                                          };
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
                                .AsEnumerable()
                                .Select(x => $"<@{x.UserId}>: " +
                                    (string.IsNullOrWhiteSpace(x.MemberCheckChannelTitle)
                                        ? x.MemberCheckChannelId
                                        : x.MemberCheckChannelTitle))));
                }, youtubeMemberChecks.Count(), 20, true, true);
            }
        }
    }
}
