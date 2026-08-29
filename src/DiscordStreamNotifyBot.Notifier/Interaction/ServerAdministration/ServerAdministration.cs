using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Interaction.Utility.Service;
using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.Shared;

namespace DiscordStreamNotifyBot.Interaction.ServerAdministration
{
    /// <summary>提供伺服器管理員使用的共用設定與聯絡指令。</summary>
    [RequireContext(ContextType.Guild)]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    [RequireUserPermission(GuildPermission.Administrator)]
    [Group("server-admin", "伺服器管理")]
    public sealed class ServerAdministration : TopLevelModule<UtilityService>
    {
        private readonly DiscordSocketClient _client;
        private readonly MainDbService _dbService;
        private readonly BotLocalizer _botLocalizer;

        public ServerAdministration(
            DiscordSocketClient client,
            MainDbService dbService,
            BotLocalizer botLocalizer)
        {
            _client = client;
            _dbService = dbService;
            _botLocalizer = botLocalizer;
        }

        [RequireContext(ContextType.Guild)]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [SlashCommand("send-message-to-bot-owner", "聯繫 Bot 擁有者")]
        public async Task SendMessageToBotOwner()
        {
            string locale = await GetLocaleAsync(true);
            var modalBuilder = new ModalBuilder().WithTitle(BotLocalizer.Get("Utility.Contact.Title", locale))
                .WithCustomId("send-message-to-bot-owner")
                .AddTextInput(BotLocalizer.Get("Utility.Contact.MessageLabel", locale), "message", TextInputStyle.Paragraph,
                    BotLocalizer.Get("Utility.Contact.MessagePlaceholder", locale), 10, null, true)
                .AddFileUpload(BotLocalizer.Get("Utility.Contact.AttachmentsLabel", locale), "file", maxValues: 4,
                    isRequired: false, description: BotLocalizer.Get("Utility.Contact.AttachmentsDescription", locale))
                .AddTextInput(BotLocalizer.Get("Utility.Contact.MethodLabel", locale), "contact-method", TextInputStyle.Short,
                    BotLocalizer.Get("Utility.Contact.MethodPlaceholder", locale), 3, null, true);

            await RespondWithModalAsync(modalBuilder.Build());
        }

        [RequireContext(ContextType.Guild)]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [SlashCommand("set-language", "設定伺服器公開內容與背景通知使用的語言")]
        public async Task SetLanguageAsync(
            [Summary("language", "語言")]
            [Choice("Traditional Chinese", SupportedLocale.TraditionalChinese)]
            [Choice("English", SupportedLocale.English)]
            [Choice("Japanese", SupportedLocale.Japanese)] string locale)
        {
            var result = await _service.SetLocaleAsync(Context.Guild.Id, locale, GracefulShutdown.Token);
            if (result.State != "applied")
            {
                await SendLocalizedErrorAsync("Errors.OperationFailed", false, true);
                return;
            }
            string selectedLocale = result.Arguments.Value<string>("locale");
            string responseLocale = await GetLocaleAsync(true);
            string displayLanguage = _botLocalizer.GetLocaleDisplayName(selectedLocale, responseLocale);
            await Context.Interaction.SendConfirmAsync(_botLocalizer, responseLocale, "Utility.LanguageChanged",
                false, true, displayLanguage);
        }

        [RequireContext(ContextType.Guild)]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [SlashCommand("set-verification-log-channel", "設定會員與訂閱驗證紀錄頻道")]
        public async Task SetVerificationLogChannelAsync(
            [Summary("log-channel", "紀錄頻道")] ITextChannel textChannel)
        {
            await DeferAsync(true);

            using var db = _dbService.GetDbContext();
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

            var result = await _service.SetVerificationLogChannelAsync(
                Context.Guild,
                textChannel.Id,
                GracefulShutdown.Token);
            if (result.State != "applied")
            {
                await SendLocalizedErrorAsync("Errors.OperationFailed", true, true);
                return;
            }

            await SendLocalizedConfirmAsync("MemberSetting.LogChannelChanged", true, false, textChannel);
        }

        [RequireContext(ContextType.Guild)]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [SlashCommand("set-global-notice-channel", "設定要接收 Bot 擁有者發送的訊息頻道")]
        public async Task SetGlobalNoticeChannel(
            [Summary("channel", "接收通知的頻道"), ChannelTypes(ChannelType.Text, ChannelType.News)] IChannel channel)
        {
            try
            {
                string locale = await GetLocaleAsync(true);
                var textChannel = channel as IGuildChannel;
                var permissions = Context.Guild.GetUser(_client.CurrentUser.Id).GetPermissions(textChannel);
                if (!permissions.ViewChannel || !permissions.SendMessages)
                {
                    await SendLocalizedErrorAsync("Permissions.MissingChannelPermissions", false, true,
                        $"`{textChannel}`", BotLocalizer.Format("Permissions.List", locale,
                            BotLocalizer.Get("Permissions.Name.ViewChannel", locale),
                            BotLocalizer.Get("Permissions.Name.SendMessages", locale)));
                    return;
                }

                if (!permissions.EmbedLinks)
                {
                    await SendLocalizedErrorAsync("Permissions.MissingChannelPermissions", false, true,
                        $"`{textChannel}`", BotLocalizer.Get("Permissions.Name.EmbedLinks", locale));
                    return;
                }

                var result = await _service.SetGlobalNoticeChannelAsync(
                    Context.Guild,
                    channel.Id,
                    GracefulShutdown.Token);
                if (result.State != "applied")
                {
                    await SendLocalizedErrorAsync("Utility.GlobalNoticeChannelFailed");
                    return;
                }

                await SendLocalizedConfirmAsync("Utility.GlobalNoticeChannelChanged", false, true, channel);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "Set Notice Channel Error");
                await SendLocalizedErrorAsync("Utility.GlobalNoticeChannelFailed");
            }
        }
    }
}
