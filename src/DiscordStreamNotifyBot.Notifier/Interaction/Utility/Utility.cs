using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Interaction.Utility.Service;
using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService.Cluster;
using System.Globalization;

namespace DiscordStreamNotifyBot.Interaction.Utility
{
    [Group("utility", "工具")]
    public class Utility : TopLevelModule<UtilityService>
    {
        private readonly DiscordSocketClient _client;
        private readonly HttpClients.DiscordWebhookClient _discordWebhookClient;
        private readonly MainDbService _dbService;
        private readonly ClusterQueryService _clusterQuery;
        private readonly GuildLocaleService _guildLocaleService;
        private readonly BotLocalizer _botLocalizer;

        public Utility(
            DiscordSocketClient client,
            HttpClients.DiscordWebhookClient discordWebhookClient,
            MainDbService dbService,
            ClusterQueryService clusterQuery,
            GuildLocaleService guildLocaleService,
            BotLocalizer botLocalizer)
        {
            _client = client;
            _discordWebhookClient = discordWebhookClient;
            _dbService = dbService;
            _clusterQuery = clusterQuery;
            _guildLocaleService = guildLocaleService;
            _botLocalizer = botLocalizer;
        }

        [SlashCommand("ping", "延遲檢測")]
        public async Task PingAsync()
        {
            await SendLocalizedConfirmAsync("Utility.Ping", false, false, _client.Latency);
        }

        [SlashCommand("invite", "取得邀請連結")]
        public async Task InviteAsync()
        {
#if RELEASE
            if (Context.User.Id != Bot.ApplicatonOwner.Id)
            {
                _discordWebhookClient.SendMessageToDiscord($"[{Context.Guild.Name}-{Context.Channel.Name}] {Context.User.Username}:({Context.User.Id}) 使用了邀請指令");
            }
#endif     
            await SendLocalizedConfirmAsync("Utility.Invite", false, true,
                $"https://discordapp.com/api/oauth2/authorize?client_id={_client.CurrentUser.Id}&permissions=11006299201&scope=bot+applications.commands");
        }

        [SlashCommand("status", "顯示機器人目前的狀態")]
        public async Task StatusAsync()
        {
            string locale = await GetLocaleAsync(false);
            EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor();
            embedBuilder.WithTitle(BotLocalizer.Get("Utility.Status.Title", locale));

#if DEBUG || DEBUG_DONTREGISTERCOMMAND
            embedBuilder.Title += BotLocalizer.Get("Utility.Status.TestBuild", locale);
#endif

            embedBuilder.WithDescription(BotLocalizer.Format("Utility.Status.Build", locale, Program.Version));
            embedBuilder.AddField(BotLocalizer.Get("Utility.Status.Author", locale), "孤之界 (konnokai)", true);
            embedBuilder.AddField(BotLocalizer.Get("Utility.Status.Owner", locale), $"{Bot.ApplicatonOwner}", true);
            // 跨 shard：以合併快照（B1）彙總全叢集的伺服器數與成員數，而非只算本 shard
            var mergedGuilds = await _clusterQuery.ReadMergedGuildsAsync();
            embedBuilder.AddField(BotLocalizer.Get("Utility.Status.State", locale),
                BotLocalizer.Format("Utility.Status.StateValue", locale, mergedGuilds.Count, mergedGuilds.Sum(x => x.MemberCount)), false);
            embedBuilder.AddField(BotLocalizer.Get("Utility.Status.StreamCount", locale), DiscordStreamNotifyBot.Utility.GetDbStreamCount(), true);
            embedBuilder.AddField(BotLocalizer.Get("Utility.Status.Uptime", locale),
                BotLocalizer.Format("Utility.Status.UptimeValue", locale, Bot.StopWatch.Elapsed.Days,
                    Bot.StopWatch.Elapsed.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)), false);

            await RespondAsync(embed: embedBuilder.Build());
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
            string selectedLocale = await _guildLocaleService.SetAsync(Context.Guild.Id, locale);
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

            var guildConfig = db.GuildConfig.FirstOrDefault(x => x.GuildId == Context.Guild.Id);
            if (guildConfig == null)
            {
                guildConfig = new DataBase.Table.GuildConfig { GuildId = Context.Guild.Id };
                db.GuildConfig.Add(guildConfig);
            }

            guildConfig.LogMemberStatusChannelId = textChannel.Id;
            await db.SaveChangesAsync(GracefulShutdown.Token);

            await SendLocalizedConfirmAsync("MemberSetting.LogChannelChanged", true, false, textChannel);
        }

        [RequireContext(ContextType.Guild)]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [SlashCommand("set-global-notice-channel", "設定要接收 Bot 擁有者發送的訊息頻道")]
        public async Task SetGlobalNoticeChannel([Summary("channel", "接收通知的頻道"), ChannelTypes(ChannelType.Text, ChannelType.News)] IChannel channel)
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

                using var db = _dbService.GetDbContext();
                var guildConfig = db.GuildConfig.FirstOrDefault((x) => x.GuildId == Context.Guild.Id);
                if (guildConfig == null)
                {
                    guildConfig = new DataBase.Table.GuildConfig { GuildId = Context.Guild.Id };
                    db.GuildConfig.Add(guildConfig);
                }
                guildConfig.NoticeChannelId = channel.Id;
                db.SaveChanges();

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
