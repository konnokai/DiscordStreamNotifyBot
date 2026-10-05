using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Interaction.Attribute;
using DiscordStreamNotifyBot.Shared;
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
        private readonly YoutubeStreamService _ytservice;
        private readonly MainDbService _dbService;

        public YoutubeMemberSetting(
            YoutubeStreamService youtubeStreamService,
            MainDbService dbService)
        {
            _ytservice = youtubeStreamService;
            _dbService = dbService;
        }

        public class GuildYoutubeMemberCheckChannelIdAutocompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                using var db = Bot.DbService.GetDbContext();
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

                return AutocompleteResponse.FromCandidates(autocompleteInteraction, candidates,
                    ex => Log.Error($"GuildYoutubeMemberCheckChannelIdAutocompleteHandler - {ex}"));
            }
        }

        [RequireGuildMemberCount(250)]
        [CommandExample("https://www.youtube.com/@998rrr @玖桃")]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [SlashCommand("add-member-check", "新增會員驗證頻道")]
        public async Task AddMemberCheckAsync([Summary("channel-url", "頻道連結")] string url, [Summary("role", "身分組 ID")] IRole role)
        {
            await DeferAsync(true);
            var result = await _service.ConfigureAsync(
                Context.Guild, Context.User.Id, url, role.Id, GracefulShutdown.Token);
            await SendVerificationResultAsync(result, url, roleName: role.Name);
        }

        [CommandExample("https://www.youtube.com/@998rrr")]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
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

        [CommandExample("頻道名稱 https://youtu.be/xxxxxxxxxxx")]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
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

        [CommandExample("https://www.youtube.com/@998rrr")]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
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

        [DefaultMemberPermissions(GuildPermission.Administrator)]
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
