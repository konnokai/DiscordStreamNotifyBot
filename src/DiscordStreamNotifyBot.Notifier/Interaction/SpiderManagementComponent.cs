using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;

namespace DiscordStreamNotifyBot.Interaction
{
    public class SpiderManagementComponent : TopLevelModule
    {
        private readonly MainDbService _dbService;

        public SpiderManagementComponent(MainDbService dbService)
        {
            _dbService = dbService;
        }

        [ComponentInteraction("spider_youtube:*:*", true)]
        public async Task HandleYoutubeAsync(string action, string channelId)
        {
            try
            {
                var button = (SocketMessageComponent)Context.Interaction;
                if (Context.User.Id != Bot.ApplicatonOwner.Id)
                {
                    string ownerLocale = await GetLocaleAsync(true);
                    await button.SendErrorAsync(BotLocalizer, ownerLocale, "Permissions.BotOwnerOnly", false, true);
                    return;
                }

                Log.Info($"\"{button.User}\" Click Button: {button.Data.CustomId}");
                await button.DeferAsync(false);
                string locale = await GetLocaleAsync(true);

                using var db = _dbService.GetDbContext();
                var youtubeChannelSpider = db.YoutubeChannelSpider.FirstOrDefault((x) => x.ChannelId == channelId);
                if (youtubeChannelSpider == null)
                {
                    await button.SendErrorAsync(BotLocalizer, locale, "Components.ChannelRemoved", true, true);
                    return;
                }

                if (action.Contains("trusted"))
                {
                    youtubeChannelSpider.IsTrustedChannel = action == "trusted";
                    db.YoutubeChannelSpider.Update(youtubeChannelSpider);
                    db.SaveChanges();

                    await button.SendConfirmAsync(BotLocalizer, locale, "YoutubeSpider.TrustedChanged", true, true,
                        youtubeChannelSpider.ChannelTitle,
                        BotLocalizer.Get(youtubeChannelSpider.IsTrustedChannel ? "Common.Enabled" : "Common.Disabled", locale));
                }

                var guild = button.Message.Embeds.First().Fields.FirstOrDefault((x) => x.Name == "伺服器").Value;
                var user = button.Message.Embeds.First().Fields.FirstOrDefault((x) => x.Name == "執行者").Value;
                var embed = new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle("已新增 YouTube 頻道爬蟲")
                    .AddField("頻道", Format.Url(youtubeChannelSpider.ChannelTitle, $"https://www.youtube.com/channel/{youtubeChannelSpider.ChannelId}"), false)
                    .AddField("伺服器", guild, false)
                    .AddField("執行者", user, false)
                    .AddField("認可頻道", youtubeChannelSpider.IsTrustedChannel ? "是" : "否", true).Build();

                await button.ModifyOriginalResponseAsync((func) =>
                {
                    func.Embed = embed;
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "處理 YouTube 爬蟲管理按鈕時失敗");
                try
                {
                    string locale = await GetLocaleAsync(true);
                    await Context.Interaction.SendErrorAsync(BotLocalizer, locale, "Errors.Unknown",
                        Context.Interaction.HasResponded, true);
                }
                catch (Exception responseException)
                {
                    Log.Error(responseException.Demystify(), "回覆 YouTube 爬蟲管理按鈕未知錯誤時失敗");
                }
            }
        }

        [ComponentInteraction("spider_twitch:*:*", true)]
        public async Task HandleTwitchAsync(string action, string userId)
        {
            try
            {
                var button = (SocketMessageComponent)Context.Interaction;
                if (Context.User.Id != Bot.ApplicatonOwner.Id)
                {
                    string ownerLocale = await GetLocaleAsync(true);
                    await button.SendErrorAsync(BotLocalizer, ownerLocale, "Permissions.BotOwnerOnly", false, true);
                    return;
                }

                Log.Info($"\"{button.User}\" Click Button: {button.Data.CustomId}");
                await button.DeferAsync(false);
                string locale = await GetLocaleAsync(true);

                using var db = _dbService.GetDbContext();
                var twitchSpider = db.TwitchSpider.FirstOrDefault((x) => x.UserId == userId);
                if (twitchSpider == null)
                {
                    await button.SendErrorAsync(BotLocalizer, locale, "Components.ChannelRemoved", true, true);
                    return;
                }

                if (action.Contains("warning"))
                {
                    twitchSpider.IsWarningUser = !twitchSpider.IsWarningUser;
                    db.TwitchSpider.Update(twitchSpider);
                    db.SaveChanges();
                    await Twitch.TwitchSpider.PublishReconcileRequestedAsync(twitchSpider.UserId, "warning_changed");

                    await button.SendConfirmAsync(BotLocalizer, locale, "Spider.StatusChanged", true, true,
                        twitchSpider.UserName,
                        BotLocalizer.Get(twitchSpider.IsWarningUser ? "Common.Warning" : "Common.Normal", locale));
                }
                else if (action.Contains("record"))
                {
                    twitchSpider.IsRecord = !twitchSpider.IsRecord;
                    db.TwitchSpider.Update(twitchSpider);
                    db.SaveChanges();

                    await button.SendConfirmAsync(BotLocalizer, locale, "Spider.RecordingChanged", true, true,
                        twitchSpider.UserName,
                        BotLocalizer.Get(twitchSpider.IsRecord ? "Common.Enabled" : "Common.Disabled", locale));
                }

                var guild = button.Message.Embeds.First().Fields.FirstOrDefault((x) => x.Name == "伺服器").Value;
                var user = button.Message.Embeds.First().Fields.FirstOrDefault((x) => x.Name == "執行者").Value;
                var embed = new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle("已新增 Twitch 頻道爬蟲")
                    .AddField("頻道", Format.Url(twitchSpider.UserName, $"https://twitch.tv/{twitchSpider.UserLogin}"), false)
                    .AddField("伺服器", guild, false)
                    .AddField("執行者", user, false)
                    .AddField("頻道狀態", twitchSpider.IsWarningUser ? "警告" : "普通", true)
                    .AddField("頻道錄影", twitchSpider.IsRecord ? "開啟" : "關閉", true).Build();

                await button.ModifyOriginalResponseAsync((func) =>
                {
                    func.Embed = embed;
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "處理 Twitch 爬蟲管理按鈕時失敗");
                try
                {
                    string locale = await GetLocaleAsync(true);
                    await Context.Interaction.SendErrorAsync(BotLocalizer, locale, "Errors.Unknown",
                        Context.Interaction.HasResponded, true);
                }
                catch (Exception responseException)
                {
                    Log.Error(responseException.Demystify(), "回覆 Twitch 爬蟲管理按鈕未知錯誤時失敗");
                }
            }
        }
    }
}
