using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.AdminSettings;
using DiscordStreamNotifyBot.SharedService.Member;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;

namespace DiscordStreamNotifyBot.Interaction.Utility.Service
{
    public class UtilityService : IInteractionService
    {
        private readonly BotLocalizer _localizer;
        private readonly IServiceProvider _services;
        private readonly DiscordSocketClient _client;
        private readonly MainDbService _dbService;
        private readonly GuildLocaleService _guildLocaleService;
        private readonly MemberOperationCoordinator _operationCoordinator;

        public UtilityService(DiscordSocketClient client, BotLocalizer localizer, IServiceProvider services,
            MainDbService dbService, GuildLocaleService guildLocaleService,
            MemberOperationCoordinator operationCoordinator)
        {
            _client = client;
            _localizer = localizer;
            _services = services;
            _dbService = dbService;
            _guildLocaleService = guildLocaleService;
            _operationCoordinator = operationCoordinator;
            client.ModalSubmitted += async modal =>
            {
                string modalRoute = modal.Data.CustomId.Split(':')[0];
                switch (modalRoute)
                {
                    case "send-message-to-bot-owner":
                        {
                            await modal.DeferAsync(true);
                            string locale = await modal.ResolveLocaleAsync(_services, true);

                            List<SocketMessageComponentData> components = modal.Data.Components.ToList();
                            string message = components.First(x => x.CustomId == "message").Value;
                            string contactMethod = components.First(x => x.CustomId == "contact-method").Value;

                            var embedBuilder = new EmbedBuilder()
                                .WithOkColor()
                                .WithTitle("新的使用者訊息")
                                .WithAuthor(modal.User)
                                .AddField("訊息", message)
                                .AddField("聯絡方式", contactMethod)
                                .AddField("伺服器 ID", modal.GuildId ?? 0);

                            var componentBuilder = new ComponentBuilder()
                                .WithButton("傳送回覆", $"send-reply-to-user:{modal.User.Id}:{locale}", ButtonStyle.Success);

                            await Bot.ApplicatonOwner.SendMessageAsync(embed: embedBuilder.Build(), components: componentBuilder.Build());

                            if (modal.Data.Attachments != null && modal.Data.Attachments.Count > 0)
                            {
                                foreach (var attachment in modal.Data.Attachments)
                                {
                                    await Bot.ApplicatonOwner.SendMessageAsync($"附加檔案：{attachment.Url}");
                                }
                            }

                            embedBuilder
                                .WithTitle("")
                                .WithDescription(_localizer.Get("Utility.Contact.Received", locale))
                                .AddField(_localizer.Get("Utility.Contact.AttachmentCount", locale), modal.Data.Attachments?.Count ?? 0);

                            await modal.FollowupAsync(embed: embedBuilder.Build(), ephemeral: true);
                        }
                        break;
                    case "send-reply-to-user":
                        {
                            await modal.DeferAsync(true);
                            string ownerLocale = await modal.ResolveLocaleAsync(_services, true);

                            List<SocketMessageComponentData> components = modal.Data.Components.ToList();
                            string[] routeData = modal.Data.CustomId.Split(':');
                            ulong userId = routeData.Length > 1
                                ? ulong.Parse(routeData[1])
                                : ulong.Parse(components.First(x => x.CustomId == "userId").Value);
                            string userLocale = routeData.Length > 2
                                ? SupportedLocale.NormalizeOrDefault(routeData[2])
                                : SupportedLocale.TraditionalChinese;
                            string message = components.First(x => x.CustomId == "message").Value;

                            try
                            {
                                var user = await client.Rest.GetUserAsync(userId);
                                await user.SendMessageAsync(embed: new EmbedBuilder()
                                        .WithOkColor()
                                        .WithTitle(_localizer.Get("Utility.Contact.OwnerReplyTitle", userLocale))
                                        .WithDescription(message)
                                        .Build());

                                if (modal.Data.Attachments != null && modal.Data.Attachments.Count > 0)
                                {
                                    foreach (var attachment in modal.Data.Attachments)
                                    {
                                        await user.SendMessageAsync(_localizer.Format("Utility.Contact.ReplyAttachment", userLocale, attachment.Url));
                                    }
                                }

                                await modal.SendConfirmAsync(_localizer, ownerLocale, "Utility.Contact.ReplySent", true, true,
                                    message, modal.Data.Attachments?.Count ?? 0);
                            }
                            catch (Discord.Net.HttpException httpEx) when (httpEx.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
                            {
                                await modal.SendErrorAsync(_localizer, ownerLocale, "Utility.Contact.UserDmClosed", true, true);
                                return;
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.Demystify(), "Bot 擁有者回覆使用者時失敗");
                                await modal.SendErrorAsync(_localizer, SupportedLocale.TraditionalChinese,
                                    "Utility.Contact.OwnerReplyFailed", true, true);
                                return;
                            }
                        }
                        break;
                    default:
                        break;
                }
            };

            client.ButtonExecuted += async button =>
            {
                if (button.HasResponded)
                    return;

                if (!button.Data.CustomId.StartsWith("send-reply-to-user"))
                    return;

                string ownerLocale = await button.ResolveLocaleAsync(_services, true);
                string userId = button.Data.CustomId.Split(':')[1];
                var modalBuilder = new ModalBuilder().WithTitle(_localizer.Get("Utility.Contact.ReplyModalTitle", ownerLocale))
                   .WithCustomId(button.Data.CustomId)
                   .AddTextInput(_localizer.Get("Utility.Contact.ReplyUserIdLabel", ownerLocale), "userId", TextInputStyle.Short, "", null, null, true, userId)
                   .AddTextInput(_localizer.Get("Utility.Contact.MessageLabel", ownerLocale), "message", TextInputStyle.Paragraph,
                       _localizer.Get("Utility.Contact.ReplyMessagePlaceholder", ownerLocale), null, null, true)
                   .AddFileUpload(_localizer.Get("Utility.Contact.ReplyAttachmentsLabel", ownerLocale), "file", maxValues: 4, isRequired: false);

                await button.RespondWithModalAsync(modalBuilder.Build());
            };
        }

        public async Task<AdminSettingsMutationResult> SetLocaleAsync(
            ulong guildId,
            string locale,
            CancellationToken cancellationToken)
        {
            try
            {
                await using var guildLock = await _operationCoordinator.LockGuildAsync(guildId, cancellationToken);
                string normalized = await _guildLocaleService.SetAsync(guildId, locale, cancellationToken);
                return AdminSettingsMutationResult.Applied(arguments: new JObject { ["locale"] = normalized });
            }
            catch (ArgumentException)
            {
                return AdminSettingsMutationResult.Rejected("settings.invalid-locale");
            }
        }

        public Task<AdminSettingsMutationResult> SetGlobalNoticeChannelAsync(
            SocketGuild guild,
            ulong channelId,
            CancellationToken cancellationToken)
            => SetChannelAsync(guild, channelId, false, cancellationToken);

        public Task<AdminSettingsMutationResult> SetVerificationLogChannelAsync(
            SocketGuild guild,
            ulong channelId,
            CancellationToken cancellationToken)
            => SetChannelAsync(guild, channelId, true, cancellationToken);

        private async Task<AdminSettingsMutationResult> SetChannelAsync(
            SocketGuild guild,
            ulong channelId,
            bool verificationLog,
            CancellationToken cancellationToken)
        {
            if (channelId != 0)
            {
                var rejected = AdminSettingsChannelValidator.Validate(_client, guild, channelId);
                if (rejected != null)
                    return rejected;
            }

            await using var guildLock = await _operationCoordinator.LockGuildAsync(guild.Id, cancellationToken);
            using var db = _dbService.GetDbContext();
            var config = await db.GuildConfig.FirstOrDefaultAsync(x => x.GuildId == guild.Id, cancellationToken);
            if (config == null)
            {
                config = new GuildConfig { GuildId = guild.Id };
                db.GuildConfig.Add(config);
            }

            if (verificationLog)
                config.VerificationLogChannelId = channelId;
            else
                config.NoticeChannelId = channelId;

            await db.SaveChangesAsync(cancellationToken);
            return AdminSettingsMutationResult.Applied(arguments: new JObject
            {
                ["channelId"] = channelId == 0
                    ? ""
                    : channelId.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });
        }

        /// <summary>伺服器專屬頭像與橫幅只存在 Discord 端，這裡不寫資料庫。</summary>
        public async Task<AdminSettingsMutationResult> SetBotProfileAsync(
            SocketGuild guild,
            ulong actorUserId,
            AdminSettingsProfileImage avatar,
            AdminSettingsProfileImage banner,
            CancellationToken cancellationToken)
        {
            bool avatarValid = TryDecodeProfileImage(avatar, out MemoryStream avatarStream, out string avatarSummary);
            bool bannerValid = TryDecodeProfileImage(banner, out MemoryStream bannerStream, out string bannerSummary);
            using (avatarStream)
            using (bannerStream)
            {
                if (!avatarValid || !bannerValid)
                    return AdminSettingsMutationResult.Rejected("settings.invalid-image");

                try
                {
                    await guild.ModifyCurrentUserAsync(props =>
                    {
                        if (avatar.Specified)
                            props.Avatar = avatarStream == null ? (Image?)null : new Image(avatarStream);
                        if (banner.Specified)
                            props.Banner = bannerStream == null ? (Image?)null : new Image(bannerStream);
                    }, new RequestOptions
                    {
                        CancelToken = cancellationToken,
                        // 被限流時不要自動重送：網頁已回逾時，圖片卻會在背景被改掉。
                        RetryMode = RetryMode.RetryTimeouts | RetryMode.Retry502
                    });
                }
                catch (Discord.Net.RateLimitedException)
                {
                    return AdminSettingsMutationResult.Rejected("settings.rate-limited");
                }
                catch (Discord.Net.HttpException httpEx) when (httpEx.HttpCode == System.Net.HttpStatusCode.BadRequest)
                {
                    return AdminSettingsMutationResult.Rejected("settings.invalid-image");
                }

                // 有管理權限的人都能改這裡，留下 guild、actor、圖片雜湊與大小方便事後追查。
                Log.Info($"機器人外觀已更新 | Guild: {guild.Id} | Actor: {actorUserId} | Avatar: {avatarSummary} | Banner: {bannerSummary}");
                return AdminSettingsMutationResult.Applied();
            }
        }

        private const int MaxProfileImageBytes = 512 * 1024;

        private static bool TryDecodeProfileImage(
            AdminSettingsProfileImage image,
            out MemoryStream stream,
            out string summary)
        {
            stream = null;
            if (!image.Specified)
            {
                summary = "未變更";
                return true;
            }
            if (image.Base64 == null)
            {
                summary = "恢復全域";
                return true;
            }

            var buffer = new byte[MaxProfileImageBytes];
            // Discord.Net 送圖時把 MIME 寫死成 image/jpeg，所以只收 JPEG。
            if (!Convert.TryFromBase64String(image.Base64, buffer, out int length) || length < 3 ||
                buffer[0] != 0xFF || buffer[1] != 0xD8 || buffer[2] != 0xFF)
            {
                summary = "格式不符";
                return false;
            }

            summary = $"{Convert.ToHexString(SHA256.HashData(buffer.AsSpan(0, length)))[..16]} ({length} bytes)";
            stream = new MemoryStream(buffer, 0, length, writable: false);
            return true;
        }
    }
}
