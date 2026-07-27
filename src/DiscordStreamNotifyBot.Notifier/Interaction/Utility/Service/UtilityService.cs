using DiscordStreamNotifyBot.Localization;

namespace DiscordStreamNotifyBot.Interaction.Utility.Service
{
    public class UtilityService : IInteractionService
    {
        private readonly BotLocalizer _localizer;
        private readonly IServiceProvider _services;

        public UtilityService(DiscordSocketClient client, BotLocalizer localizer, IServiceProvider services)
        {
            _localizer = localizer;
            _services = services;
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
                                .AddField("聯繫方式", contactMethod)
                                .AddField("伺服器 Id", modal.GuildId ?? 0);

                            var componentBuilder = new ComponentBuilder()
                                .WithButton("發送回覆", $"send-reply-to-user:{modal.User.Id}:{locale}", ButtonStyle.Success);

                            await Bot.ApplicatonOwner.SendMessageAsync(embed: embedBuilder.Build(), components: componentBuilder.Build());

                            if (modal.Data.Attachments != null && modal.Data.Attachments.Count > 0)
                            {
                                foreach (var attachment in modal.Data.Attachments)
                                {
                                    await Bot.ApplicatonOwner.SendMessageAsync($"附加檔案: {attachment.Url}");
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
    }
}
