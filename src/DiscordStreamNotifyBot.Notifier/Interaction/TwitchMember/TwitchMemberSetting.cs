using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService.Twitch;
using DiscordStreamNotifyBot.SharedService.TwitchSubscription;

namespace DiscordStreamNotifyBot.Interaction.TwitchMember
{
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    [Group("twitch-member-set", "Twitch 訂閱驗證設定")]
    public sealed class TwitchMemberSetting : TopLevelModule
    {
        private readonly MainDbService _dbService;
        private readonly TwitchApiService _twitchApiService;
        private readonly TwitchSubscriptionRoleService _roleService;

        public TwitchMemberSetting(
            MainDbService dbService,
            TwitchApiService twitchApiService,
            TwitchSubscriptionRoleService roleService)
        {
            _dbService = dbService;
            _twitchApiService = twitchApiService;
            _roleService = roleService;
        }

        public sealed class ConfiguredBroadcasterAutocompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
                IInteractionContext context,
                IAutocompleteInteraction autocompleteInteraction,
                IParameterInfo parameter,
                IServiceProvider services)
            {
                using var db = Bot.DbService.GetDbContext();
                string value = autocompleteInteraction.Data.Current.Value?.ToString();
                var candidates = await db.GuildTwitchSubscriptionConfig.AsNoTracking()
                    .ActiveConfigurations()
                    .Where(x => x.GuildId == context.Guild.Id)
                    .Select(x => new AutocompleteCandidate(x.BroadcasterDisplayName, x.BroadcasterId))
                    .ToListAsync();
                return AutocompletionResult.FromSuccess(AutocompleteSearch.Filter(candidates, value)
                    .Select(x => new AutocompleteResult(x.Name, x.Value)));
            }
        }

        [SlashCommand("add-subscription-check", "新增或更新 Twitch 訂閱驗證頻道")]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        public async Task AddSubscriptionCheckAsync(
            [Summary("channel-url", "Twitch 頻道網址")] string channel,
            [Summary("role", "驗證成功後給予的身分組 (額外給予各 Tier 等級身分組)")] IRole role)
        {
            if (!_twitchApiService.IsEnable)
            {
                await SendLocalizedErrorAsync("Errors.FeatureDisabled");
                return;
            }

            await DeferAsync(true);
            string login = _twitchApiService.GetUserLoginByUrl(channel);
            var user = await _twitchApiService.GetUserAsync(twitchUserLogin: login);
            if (user == null)
            {
                await SendLocalizedErrorAsync("TwitchMemberSetting.Errors.ChannelNotFound", true);
                return;
            }
            if (!IsEligibleBroadcaster(user.BroadcasterType))
            {
                await SendLocalizedErrorAsync("TwitchMemberSetting.Errors.IneligibleBroadcaster", true);
                return;
            }

            TwitchRoleConfigurationResult result = await _roleService.CreateOrRepairConfigurationAsync(
                (SocketGuild)Context.Guild,
                user.Id,
                user.Login,
                user.DisplayName,
                role,
                GracefulShutdown.Token);
            if (!result.IsSuccess)
            {
                await SendLocalizedErrorAsync(result.Error, true);
                return;
            }

            await SendLocalizedConfirmAsync(
                "TwitchMemberSetting.Configured", true, true, user.DisplayName, role.Name);
        }

        [SlashCommand("remove-subscription-check", "移除 Twitch 訂閱驗證頻道")]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        public async Task RemoveSubscriptionCheckAsync(
            [Summary("channel", "已設定的 Twitch 頻道"), Autocomplete(typeof(ConfiguredBroadcasterAutocompleteHandler))] string broadcasterId)
        {
            await DeferAsync(true);
            using var db = _dbService.GetDbContext();
            var config = await db.GuildTwitchSubscriptionConfig.AsNoTracking().SingleOrDefaultAsync(
                x => x.GuildId == Context.Guild.Id && x.BroadcasterId == broadcasterId && !x.DeletionPending);
            if (config == null)
            {
                await SendLocalizedErrorAsync("TwitchMemberSetting.Errors.NotConfigured", true);
                return;
            }
            if (!await PromptUserConfirmAsync("TwitchMemberSetting.RemovePrompt", config.BroadcasterDisplayName))
                return;

            bool removed = await _roleService.DeleteConfigurationAsync(config, GracefulShutdown.Token);
            if (removed)
                await SendLocalizedConfirmAsync(
                    "TwitchMemberSetting.Removed", true, true, config.BroadcasterDisplayName);
            else
                await SendLocalizedErrorAsync("TwitchMemberSetting.Errors.RemovePending", true);
        }

        [SlashCommand("list-checked-member", "列出已驗證的 Twitch 訂閱者")]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        public async Task ListCheckedMemberAsync([Summary("page", "頁數")] int page = 1)
        {
            using var db = _dbService.GetDbContext();
            var checks = from check in db.TwitchSubscriptionCheck.AsNoTracking()
                          join config in db.GuildTwitchSubscriptionConfig.AsNoTracking()
                              on new { check.GuildId, check.BroadcasterId } equals new { config.GuildId, config.BroadcasterId }
                          where check.GuildId == Context.Guild.Id && check.IsChecked && !config.DeletionPending
                         orderby check.DiscordUserId, config.BroadcasterDisplayName
                         select new { check.DiscordUserId, check.Tier, config.BroadcasterDisplayName };
            int count = await checks.CountAsync();
            if (count == 0)
            {
                await SendLocalizedErrorAsync("TwitchMemberSetting.Errors.NoCheckedMembers");
                return;
            }
            page = Math.Max(0, page - 1);
            string locale = await GetLocaleAsync(true);
            await Context.SendPaginatedConfirmAsync(page, currentPage => new EmbedBuilder()
                .WithOkColor()
                .WithTitle(BotLocalizer.Get("TwitchMemberSetting.CheckedMembersTitle", locale))
                .WithDescription(string.Join('\n', checks.Skip(currentPage * 20).Take(20)
                    .AsEnumerable()
                    .Select(x => $"<@{x.DiscordUserId}>: `{x.BroadcasterDisplayName}` / {TwitchMember.FormatTier(x.Tier)}"))),
                count, 20, true, true);
        }

        internal static bool IsEligibleBroadcaster(string broadcasterType)
            => string.Equals(broadcasterType, "affiliate", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(broadcasterType, "partner", StringComparison.OrdinalIgnoreCase);
    }
}
