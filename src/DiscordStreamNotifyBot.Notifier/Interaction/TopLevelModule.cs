using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.Shared.Messages;

namespace DiscordStreamNotifyBot.Interaction
{
    public abstract class TopLevelModule : InteractionModuleBase<SocketInteractionContext>
    {
        public BotLocalizer BotLocalizer { get; set; }
        public CommandDisplayResolver CommandDisplayResolver { get; set; }
        public GuildLocaleService GuildLocaleService { get; set; }
        public LocaleResolver LocaleResolver { get; set; }

        protected async Task<string> GetLocaleAsync(bool isPrivate)
        {
            string guildLocale = null;
            if (Context.Guild != null)
                guildLocale = await GuildLocaleService.GetAsync(Context.Guild.Id, Context.Guild as SocketGuild);

            return isPrivate || Context.Guild == null
                ? LocaleResolver.ResolvePrivate(Context.Interaction.UserLocale, guildLocale, Context.Interaction.GuildLocale)
                : LocaleResolver.ResolvePublic(guildLocale, Context.Interaction.GuildLocale);
        }

        protected async Task<string> LocalizeAsync(string resourceKey, bool isPrivate = true, params object[] arguments)
            => BotLocalizer.Format(resourceKey, await GetLocaleAsync(isPrivate), arguments);

        protected async Task SendLocalizedConfirmAsync(string resourceKey, bool isFollowup = false,
            bool ephemeral = false, params object[] arguments)
        {
            string locale = await GetLocaleAsync(ephemeral);
            await Context.Interaction.SendConfirmAsync(BotLocalizer, locale, resourceKey, isFollowup, ephemeral, arguments);
        }

        protected async Task SendLocalizedErrorAsync(string resourceKey, bool isFollowup = false,
            bool ephemeral = true, params object[] arguments)
        {
            string locale = await GetLocaleAsync(ephemeral);
            await Context.Interaction.SendErrorAsync(BotLocalizer, locale, resourceKey, isFollowup, ephemeral, arguments);
        }

        protected async Task SendCrawlerResultAsync(
            AdminSettingsMutationResult result,
            string source,
            string platform)
        {
            string locale = await GetLocaleAsync(true);
            string addPath = CommandDisplayResolver.GetCommandPath(locale, platform, "add");
            string contactPath = CommandDisplayResolver.GetCommandPath(locale, "server-admin", "send-message-to-bot-owner");
            switch (result.Code)
            {
                case "crawler.added":
                    await SendLocalizedConfirmAsync("Spider.Added", true, true,
                        result.Arguments.Value<string>("sourceName") ?? source,
                        addPath,
                        result.Arguments.Value<string>("sourceId") ?? source);
                    break;
                case "crawler.removed":
                    await SendLocalizedConfirmAsync("Spider.Removed", true, false, source);
                    break;
                case "crawler.already-exists":
                    await SendLocalizedErrorAsync("Spider.AlreadyExists", true, true,
                        source, addPath, source, "");
                    break;
                case "crawler.not-configured":
                    await SendLocalizedErrorAsync("Spider.NotConfigured", true, true, source);
                    break;
                case "crawler.not-owned":
                case "crawler.source-owned":
                    await SendLocalizedErrorAsync("Spider.NotOwnedByGuild", true);
                    break;
                case "crawler.limit-reached":
                    await SendLocalizedErrorAsync("Spider.LimitReachedShort", true, true,
                        result.Arguments.Value<int?>("limit") ?? 0, platform);
                    break;
                case "crawler.platform-disabled":
                    await SendLocalizedErrorAsync("Errors.FeatureDisabled", true);
                    break;
                case "crawler.guild-member-requirement":
                    await SendLocalizedErrorAsync("Preconditions.GuildMemberCount", true, true,
                        result.Arguments.Value<int?>("requiredMemberCount") ?? 0,
                        result.Arguments.Value<int?>("memberCount") ?? Context.Guild.MemberCount,
                        contactPath);
                    break;
                case "crawler.oauth-eligibility-required":
                    await SendLocalizedErrorAsync("TwitchSpider.MemberRequirement", true, true,
                        result.Arguments.Value<int?>("requiredMemberCount") ?? 0,
                        result.Arguments.Value<int?>("memberCount") ?? Context.Guild.MemberCount,
                        contactPath);
                    break;
                case "crawler.source-ineligible":
                    await SendLocalizedErrorAsync("YoutubeSpider.ManagedChannelRejected", true);
                    break;
                default:
                    await SendLocalizedErrorAsync("Errors.SaveFailed", true);
                    break;
            }
        }

        public async Task<bool> PromptUserConfirmAsync(string resourceKey, params object[] arguments)
        {
            string guid = Guid.NewGuid().ToString().Replace("-", "");
            string locale = await GetLocaleAsync(true);

            EmbedBuilder embed = new EmbedBuilder()
                .WithOkColor()
                .WithDescription(BotLocalizer.Format(resourceKey, locale, arguments))
                .WithFooter(BotLocalizer.Get("Confirmation.TimeoutFooter", locale));

            ComponentBuilder component = new ComponentBuilder()
                .WithButton(BotLocalizer.Get("Common.Yes", locale), $"{guid}-yes", ButtonStyle.Success)
                .WithButton(BotLocalizer.Get("Common.No", locale), $"{guid}-no", ButtonStyle.Danger);

            await FollowupAsync(embed: embed.Build(), components: component.Build(), ephemeral: true).ConfigureAwait(false);

            try
            {
                var input = await GetUserClickAsync(Context.User.Id, Context.Channel.Id, guid, locale).ConfigureAwait(false);
                return input;
            }
            finally
            {
            }
        }

        public async Task<bool> GetUserClickAsync(ulong userId, ulong channelId, string guid, string locale)
        {
            var userInputTask = new TaskCompletionSource<bool>();

            try
            {
                Context.Client.ButtonExecuted += ButtonExecuted;

                if ((await Task.WhenAny(userInputTask.Task, Task.Delay(5000)).ConfigureAwait(false)) != userInputTask.Task)
                {
                    return false;
                }

                return await userInputTask.Task.ConfigureAwait(false);
            }
            finally
            {
                Context.Client.ButtonExecuted -= ButtonExecuted;
            }

            Task ButtonExecuted(SocketMessageComponent component)
            {
                var _ = Task.Run(async () =>
                {
                    if (!component.Data.CustomId.StartsWith(guid))
                        return Task.CompletedTask;

                    if (!(component is SocketMessageComponent userMsg) ||
                        userMsg.User.Id != userId ||
                        userMsg.Channel.Id != channelId)
                    {
                        string componentLocale = LocaleResolver.ResolvePrivate(component.UserLocale, null, component.GuildLocale);
                        await component.SendErrorAsync(BotLocalizer, componentLocale, "Components.NotAllowed", true, true).ConfigureAwait(false);
                        return Task.CompletedTask;
                    }

                    userInputTask.TrySetResult(component.Data.CustomId.EndsWith("yes"));

                    await component.UpdateAsync((x) => x.Components = new ComponentBuilder()
                        .WithButton(BotLocalizer.Get("Common.Yes", locale), $"{guid}-yes", ButtonStyle.Success, disabled: true)
                        .WithButton(BotLocalizer.Get("Common.No", locale), $"{guid}-no", ButtonStyle.Danger, disabled: true).Build())
                    .ConfigureAwait(false);
                    return Task.CompletedTask;
                });
                return Task.CompletedTask;
            }
        }

        public async Task CheckIsFirstSetNoticeAndSendWarningMessageAsync(MainDbContext dbContext)
        {
            ulong guildId = Context.Guild.Id;
            bool hasNoYoutubeNotice = !await dbContext.NoticeYoutubeStreamChannel.AsNoTracking().AnyAsync(x => x.GuildId == guildId);
            bool hasNoTwitchNotice = !await dbContext.NoticeTwitchStreamChannels.AsNoTracking().AnyAsync(x => x.GuildId == guildId);
            var initialized = await GuildLocaleService.InitializeAsync(
                dbContext,
                guildId,
                Context.Interaction.GuildLocale,
                Context.Interaction.UserLocale);

            if (hasNoYoutubeNotice && hasNoTwitchNotice)
            {
                string responseLocale = LocaleResolver.ResolvePrivate(
                    Context.Interaction.UserLocale,
                    initialized.Locale,
                    Context.Interaction.GuildLocale);
                string displayLanguage = BotLocalizer.GetLocaleDisplayName(initialized.Locale, responseLocale);
                string setLanguagePath = CommandDisplayResolver.GetCommandPath(responseLocale, "server-admin", "set-language");
                string globalNoticePath = CommandDisplayResolver.GetCommandPath(responseLocale, "server-admin", "set-global-notice-channel");
                string contactPath = CommandDisplayResolver.GetCommandPath(responseLocale, "server-admin", "send-message-to-bot-owner");
                string message = string.Join('\n',
                    BotLocalizer.Format("Onboarding.FirstNotificationSetup", responseLocale, globalNoticePath, contactPath),
                    BotLocalizer.Format("Onboarding.CurrentLanguage", responseLocale, displayLanguage),
                    BotLocalizer.Format("Onboarding.ChangeLanguageHint", responseLocale, setLanguagePath));

                await Context.Interaction.SendConfirmAsync(message, true, true);
            }
        }
    }

    public abstract class TopLevelModule<TService> : TopLevelModule where TService : IInteractionService
    {
        protected TopLevelModule()
        {
        }

        public TService _service { get; set; }
    }
}
