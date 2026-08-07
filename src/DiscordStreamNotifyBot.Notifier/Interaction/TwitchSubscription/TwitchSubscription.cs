using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService.Twitch;
using DiscordStreamNotifyBot.SharedService.TwitchSubscription;

namespace DiscordStreamNotifyBot.Interaction.TwitchSubscription
{
    [Group("twitch-subscription", "Twitch 訂閱驗證相關指令")]
    public sealed class TwitchSubscription : TopLevelModule
    {
        private readonly MainDbService _dbService;
        private readonly TwitchApiService _twitchApiService;
        private readonly TwitchSubscriptionService _subscriptionService;

        public TwitchSubscription(
            MainDbService dbService,
            TwitchApiService twitchApiService,
            TwitchSubscriptionService subscriptionService)
        {
            _dbService = dbService;
            _twitchApiService = twitchApiService;
            _subscriptionService = subscriptionService;
        }

        [RequireContext(ContextType.Guild)]
        [SlashCommand("check", "立即確認 Twitch 訂閱資格")]
        public async Task CheckAsync()
        {
            if (!_twitchApiService.IsEnable)
            {
                await SendLocalizedErrorAsync("Errors.FeatureDisabled");
                return;
            }

            await DeferAsync(true);
            using var db = _dbService.GetDbContext();
            var configs = await db.GuildTwitchSubscriptionConfig.AsNoTracking()
                .ActiveConfigurations()
                .Where(x => x.GuildId == Context.Guild.Id)
                .OrderBy(x => x.BroadcasterDisplayName)
                .Take(26)
                .ToListAsync();
            if (configs.Count == 0)
            {
                await SendLocalizedErrorAsync("TwitchMember.Errors.NotConfigured", true);
                return;
            }
            if (configs.Count > 25)
            {
                await SendLocalizedErrorAsync("TwitchMember.Errors.TooManyChannels", true);
                return;
            }

            if (configs.Count == 1)
            {
                string locale = await GetLocaleAsync(true);
                TwitchSubscriptionResult result = await _subscriptionService.VerifyAsync(
                    Context.Guild.Id,
                    Context.User.Id,
                    configs[0].BroadcasterId,
                    locale,
                    GracefulShutdown.Token);
                await SendResultAsync(result, configs[0], locale, true);
                return;
            }

            string responseLocale = await GetLocaleAsync(true);
            var menu = new SelectMenuBuilder()
                .WithPlaceholder(BotLocalizer.Get("TwitchMember.Select.Placeholder", responseLocale))
                .WithMinValues(1)
                .WithMaxValues(configs.Count)
                .WithCustomId($"twitch-subscription-check:{Context.Guild.Id}:{Context.User.Id}");
            foreach (var config in configs)
                menu.AddOption(config.BroadcasterDisplayName, config.BroadcasterId);

            await Context.Interaction.FollowupAsync(
                BotLocalizer.Get("TwitchMember.Select.Description", responseLocale),
                components: new ComponentBuilder().WithSelectMenu(menu).Build(),
                ephemeral: true);
        }

        [RequireContext(ContextType.Guild)]
        [SlashCommand("cancel-subscription-check", "取消此伺服器的 Twitch 訂閱驗證")]
        public async Task CancelAsync()
        {
            await DeferAsync(true);
            TwitchSubscriptionCancellationStatus status = await _subscriptionService.CancelAsync(
                Context.Guild.Id,
                Context.User.Id,
                GracefulShutdown.Token);
            if (status == TwitchSubscriptionCancellationStatus.Completed)
                await SendLocalizedConfirmAsync("TwitchMember.Cancelled", true, true);
            else if (status == TwitchSubscriptionCancellationStatus.NotFound)
                await SendLocalizedErrorAsync("TwitchMember.Errors.NoActiveCheck", true);
            else
                await SendLocalizedErrorAsync("TwitchMember.Errors.TemporaryFailure", true);
        }

        [SlashCommand("show-my-twitch-account", "顯示目前連結的 Twitch 帳號")]
        public async Task ShowMyTwitchAccountAsync()
        {
            await DeferAsync(true);
            using var db = _dbService.GetDbContext();
            var authorization = await db.TwitchBroadcasterAuthorization.AsNoTracking()
                .SingleOrDefaultAsync(x => x.DiscordUserId == Context.User.Id);
            if (authorization == null || authorization.RevokedAt != null || string.IsNullOrWhiteSpace(authorization.EncryptedAccessToken))
            {
                string locale = await GetLocaleAsync(true);
                await SendLocalizedErrorAsync("TwitchMember.Errors.AuthorizationMissing", true, true,
                    Format.Url(BotLocalizer.Get("Common.Website", locale), "https://stream-bot.konnokai.me/"));
                return;
            }

            await SendLocalizedConfirmAsync(
                "TwitchMember.LinkedAccount",
                true,
                true,
                Format.Url(authorization.DisplayName, $"https://twitch.tv/{authorization.UserLogin}"));
        }

        [RequireContext(ContextType.Guild)]
        [SlashCommand("list-can-check-channel", "列出此伺服器可驗證的 Twitch 頻道")]
        public async Task ListCanCheckChannelAsync()
        {
            using var db = _dbService.GetDbContext();
            var configs = await db.GuildTwitchSubscriptionConfig.AsNoTracking()
                .ActiveConfigurations()
                .Where(x => x.GuildId == Context.Guild.Id)
                .OrderBy(x => x.BroadcasterDisplayName)
                .ToListAsync();
            if (configs.Count == 0)
            {
                await SendLocalizedErrorAsync("TwitchMember.Errors.NotConfigured");
                return;
            }
            string channels = string.Join('\n', configs.Select(x =>
                $"{Format.Url(x.BroadcasterDisplayName, $"https://twitch.tv/{x.BroadcasterLogin}")}: <@&{x.SubscriberRoleId}>"));
            await SendLocalizedConfirmAsync("TwitchMember.ChannelList", false, true, channels);
        }

        private async Task SendResultAsync(
            TwitchSubscriptionResult result,
            DataBase.Table.GuildTwitchSubscriptionConfig config,
            string locale,
            bool isFollowup)
        {
            switch (result.Status)
            {
                case TwitchSubscriptionStatus.Subscribed:
                    await Context.Interaction.SendConfirmAsync(BotLocalizer, locale, "TwitchMember.Verified",
                        isFollowup, true, config.BroadcasterDisplayName, FormatTier(result.Tier), result.IsGift
                            ? BotLocalizer.Get("TwitchMember.Gift", locale)
                            : "");
                    break;
                case TwitchSubscriptionStatus.NotSubscribed:
                    await Context.Interaction.SendErrorAsync(BotLocalizer, locale, "TwitchMember.NotSubscribed",
                        isFollowup, true, config.BroadcasterDisplayName);
                    break;
                case TwitchSubscriptionStatus.AuthorizationMissing:
                    await Context.Interaction.SendErrorAsync(BotLocalizer, locale, "TwitchMember.Errors.AuthorizationMissing",
                        isFollowup, true, Format.Url(BotLocalizer.Get("Common.Website", locale), "https://stream-bot.konnokai.me/"));
                    break;
                case TwitchSubscriptionStatus.AuthorizationInvalid:
                    await Context.Interaction.SendErrorAsync(BotLocalizer, locale, "TwitchMember.Errors.AuthorizationInvalid",
                        isFollowup, true, Format.Url(BotLocalizer.Get("Common.Website", locale), "https://stream-bot.konnokai.me/"));
                    break;
                case TwitchSubscriptionStatus.BroadcasterUnavailable:
                    await Context.Interaction.SendErrorAsync(BotLocalizer, locale, "TwitchMember.Errors.BroadcasterUnavailable",
                        isFollowup, true);
                    break;
                default:
                    await Context.Interaction.SendErrorAsync(BotLocalizer, locale, "TwitchMember.Errors.TemporaryFailure",
                        isFollowup, true);
                    break;
            }
        }

        internal static string FormatTier(string tier) => tier switch
        {
            "1000" => "Tier 1",
            "2000" => "Tier 2",
            "3000" => "Tier 3",
            _ => "Unknown"
        };
    }

    public sealed class TwitchSubscriptionComponent : TopLevelModule
    {
        private readonly MainDbService _dbService;
        private readonly TwitchApiService _twitchApiService;
        private readonly TwitchSubscriptionService _subscriptionService;

        public TwitchSubscriptionComponent(
            MainDbService dbService,
            TwitchApiService twitchApiService,
            TwitchSubscriptionService subscriptionService)
        {
            _dbService = dbService;
            _twitchApiService = twitchApiService;
            _subscriptionService = subscriptionService;
        }

        [ComponentInteraction("twitch-subscription-check:*:*", true)]
        public async Task HandleSelectionAsync(string guildValue, string userValue, string[] broadcasterIds)
        {
            var component = (SocketMessageComponent)Context.Interaction;
            string locale = await GetLocaleAsync(true);
            if (!ulong.TryParse(guildValue, out ulong guildId) ||
                !ulong.TryParse(userValue, out ulong userId) ||
                Context.User.Id != userId ||
                Context.Guild?.Id != guildId)
            {
                await component.SendErrorAsync(BotLocalizer, locale, "Components.NotAllowed", false, true);
                return;
            }

            if (!_twitchApiService.IsEnable)
            {
                await component.SendErrorAsync(BotLocalizer, locale, "Errors.FeatureDisabled", false, true);
                return;
            }

            await component.DeferAsync(true);
            using var db = _dbService.GetDbContext();
            var configs = await db.GuildTwitchSubscriptionConfig.AsNoTracking()
                .ActiveConfigurations()
                .Where(x => x.GuildId == guildId && broadcasterIds.Contains(x.BroadcasterId))
                .ToDictionaryAsync(x => x.BroadcasterId);
            var messages = new List<string>();
            foreach (string broadcasterId in broadcasterIds.Take(25))
            {
                if (!configs.TryGetValue(broadcasterId, out var config))
                    continue;
                TwitchSubscriptionResult result = await _subscriptionService.VerifyAsync(
                    guildId, userId, broadcasterId, locale, GracefulShutdown.Token);
                messages.Add(BotLocalizer.Format(
                    result.Status == TwitchSubscriptionStatus.Subscribed
                        ? "TwitchMember.Selection.Verified"
                        : "TwitchMember.Selection.Failed",
                    locale,
                    config.BroadcasterDisplayName,
                    result.Status == TwitchSubscriptionStatus.Subscribed
                        ? TwitchSubscription.FormatTier(result.Tier)
                        : BotLocalizer.Get($"TwitchMember.Status.{result.Status}", locale)));
            }

            await component.FollowupAsync(
                embed: new EmbedBuilder().WithOkColor().WithDescription(string.Join('\n', messages)).Build(),
                ephemeral: true);
        }
    }
}
