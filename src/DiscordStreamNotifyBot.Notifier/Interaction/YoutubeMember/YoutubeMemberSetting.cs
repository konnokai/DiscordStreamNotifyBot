using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Interaction.Attribute;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService.Member;
using DiscordStreamNotifyBot.SharedService.Youtube;
using DiscordStreamNotifyBot.SharedService.YoutubeMember;

namespace DiscordStreamNotifyBot.Interaction.YoutubeMember
{
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    [Group("youtube-member-set", "YouTube 會員驗證設定")]
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
        [CommandSummary("新增會員驗證頻道，目前最多可設定 5 個頻道\n" +
           "新增相同頻道可變更授予的身分組\n" +
           "伺服器人數至少 250 人才可使用\n" +
           "如有需求，請聯絡擁有者")]
        [CommandExample("https://www.youtube.com/@998rrr @玖桃")]
        [SlashCommand("add-member-check", "新增會員驗證頻道")]
        public async Task AddMemberCheckAsync([Summary("channel-url", "頻道連結")] string url, [Summary("role", "身分組 ID")] IRole role)
        {
            await DeferAsync(true);
            var result = await _service.ConfigureAsync(
                Context.Guild, Context.User.Id, url, role.Id, GracefulShutdown.Token);
            await SendVerificationResultAsync(result, url, roleName: role.Name);
        }

        [CommandSummary("移除會員驗證頻道")]
        [CommandExample("https://www.youtube.com/@998rrr")]
        [SlashCommand("remove-member-check", "移除會員驗證頻道")]
        public async Task RemoveMemberCheckAsync([Summary("channel-url", "頻道連結"), Autocomplete(typeof(GuildYoutubeMemberCheckChannelIdAutocompleteHandler))] string url)
        {
            await DeferAsync(true);
            try
            {
                string sourceId = await ResolveConfiguredChannelIdAsync(url);
                var result = await _service.RemoveConfigurationAsync(
                    Context.Guild.Id, sourceId, GracefulShutdown.Token);
                await SendVerificationResultAsync(result, url);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "移除 YouTube 會員驗證設定失敗");
                await SendLocalizedErrorAsync("Errors.SaveFailed", true);
            }
        }

        [CommandSummary("手動指定會員驗證用的偵測影片\n" +
            "用於頻道有多階會員時，指定「最低階」的會員限定影片，避免低階但合法的會員被誤判失敗\n" +
            "指定後自動探索不會再覆寫此影片；該影片失效時會發送通知到通知頻道提醒需重設")]
        [CommandExample("頻道名稱 https://youtu.be/xxxxxxxxxxx")]
        [SlashCommand("set-check-video", "手動指定會員驗證探測影片")]
        public async Task SetCheckVideoAsync(
            [Summary("channel", "頻道名稱"), Autocomplete(typeof(GuildYoutubeMemberCheckChannelIdAutocompleteHandler))] string url,
            [Summary("video", "會員限定影片連結或 ID")] string videoUrlOrId)
        {
            await DeferAsync(true);

            try
            {
                var channelId = await ResolveConfiguredChannelIdAsync(url);
                var result = await _service.SetProbeVideoAsync(
                    Context.Guild.Id, channelId, videoUrlOrId, GracefulShutdown.Token);
                await SendVerificationResultAsync(result, url);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "手動指定會員驗證影片時失敗");
                await SendLocalizedErrorAsync("Errors.InvalidYoutubeInput", true);
            }
        }

        [CommandSummary("改回自動挑選會員驗證偵測影片（取消手動指定）")]
        [CommandExample("https://www.youtube.com/@998rrr")]
        [SlashCommand("clear-check-video", "改回自動挑選會員驗證偵測影片")]
        public async Task ClearCheckVideoAsync(
            [Summary("channel-url", "頻道連結"), Autocomplete(typeof(GuildYoutubeMemberCheckChannelIdAutocompleteHandler))] string url)
        {
            await DeferAsync(true);

            try
            {
                var channelId = await ResolveConfiguredChannelIdAsync(url);
                var result = await _service.UseAutomaticProbeAsync(
                    Context.Guild.Id, channelId, GracefulShutdown.Token);
                await SendVerificationResultAsync(result, url);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "恢復自動挑選會員驗證影片時失敗");
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
                throw new FormatException("找到多個同名 YouTube 頻道，請從自動完成選單選擇頻道");
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
