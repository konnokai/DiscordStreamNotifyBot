using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService.TwitchSubscription;

namespace DiscordStreamNotifyBot.Interaction.TwitchSubscription
{
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator)]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    [Group("twitch-subscription-set", "Twitch 訂閱驗證設定")]
    public sealed class TwitchSubscriptionSetting : TopLevelModule
    {
        private readonly MainDbService _dbService;
        private readonly TwitchSubscriptionService _subscriptionService;

        public TwitchSubscriptionSetting(
            MainDbService dbService,
            TwitchSubscriptionService subscriptionService)
        {
            _dbService = dbService;
            _subscriptionService = subscriptionService;
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
                    .Select(x => new AutocompleteCandidate(
                        x.BroadcasterDisplayName,
                        x.BroadcasterDisplayName,
                        x.BroadcasterLogin,
                        x.BroadcasterId))
                    .ToListAsync();
                return AutocompletionResult.FromSuccess(AutocompleteSearch.Filter(candidates, value)
                    .Select(x => new AutocompleteResult(x.Name, x.Value)));
            }
        }

        [SlashCommand("add-subscription-check", "新增或更新 Twitch 訂閱驗證頻道")]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        public async Task AddSubscriptionCheckAsync(
            [Summary("channel-url", "Twitch 頻道網址")] string channel,
            [Summary("role", "驗證成功後給予的身分組（額外給予各層級身分組）")] IRole role)
        {
            await DeferAsync(true);
            var result = await _subscriptionService.ConfigureAsync(
                Context.Guild, Context.User.Id, channel, role.Id, GracefulShutdown.Token);
            await SendVerificationResultAsync(result, channel, true, role.Name);
        }

        [SlashCommand("remove-subscription-check", "移除 Twitch 訂閱驗證頻道")]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        public async Task RemoveSubscriptionCheckAsync(
            [Summary("channel", "已設定的 Twitch 頻道"), Autocomplete(typeof(ConfiguredBroadcasterAutocompleteHandler))] string channel)
        {
            await DeferAsync(true);
            using var db = _dbService.GetDbContext();
            var config = await db.GuildTwitchSubscriptionConfig.AsNoTracking().SingleOrDefaultAsync(
                x => x.GuildId == Context.Guild.Id && !x.DeletionPending &&
                    (x.BroadcasterDisplayName == channel || x.BroadcasterLogin == channel || x.BroadcasterId == channel));
            if (config == null)
            {
                await SendLocalizedErrorAsync("TwitchMemberSetting.Errors.NotConfigured", true);
                return;
            }
            if (!await PromptUserConfirmAsync("TwitchMemberSetting.RemovePrompt", config.BroadcasterDisplayName))
                return;
            var result = await _subscriptionService.RemoveConfigurationAsync(
                Context.Guild.Id, config.BroadcasterId, GracefulShutdown.Token);
            await SendVerificationResultAsync(result, config.BroadcasterDisplayName, true);
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
                    .Select(x => $"<@{x.DiscordUserId}>: `{x.BroadcasterDisplayName}` / {TwitchSubscription.FormatTier(x.Tier, locale)}"))),
                count, 20, true, true);
        }

        internal static bool IsEligibleBroadcaster(string broadcasterType)
            => string.Equals(broadcasterType, "affiliate", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(broadcasterType, "partner", StringComparison.OrdinalIgnoreCase);
    }
}
