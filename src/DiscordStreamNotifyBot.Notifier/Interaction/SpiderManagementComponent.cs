using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService.Chzzk;

namespace DiscordStreamNotifyBot.Interaction
{
    public class SpiderManagementComponent : TopLevelModule
    {
        private readonly MainDbService _dbService;
        private readonly ChzzkRecordService _chzzkRecordService;

        public SpiderManagementComponent(MainDbService dbService, ChzzkRecordService chzzkRecordService)
        {
            _dbService = dbService;
            _chzzkRecordService = chzzkRecordService;
        }

        [ComponentInteraction("spider_youtube:*:*", true)]
        public Task HandleYoutubeAsync(string action, string channelId)
            => RunOwnerButtonAsync("YouTube", async button =>
            {
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
                    // 切換語意（對齊 Twitch／TwitCasting 的切換按鈕），不是加入／移除。
                    youtubeChannelSpider.IsTrustedChannel = !youtubeChannelSpider.IsTrustedChannel;
                    db.YoutubeChannelSpider.Update(youtubeChannelSpider);
                    db.SaveChanges();

                    await button.SendConfirmAsync(BotLocalizer, locale, "YoutubeSpider.TrustedChanged", true, true,
                        youtubeChannelSpider.ChannelTitle,
                        BotLocalizer.Get(youtubeChannelSpider.IsTrustedChannel ? "Common.Enabled" : "Common.Disabled", locale));
                }
                else if (action.Contains("record"))
                {
                    if (db.RecordYoutubeChannel.Any((x) => x.YoutubeChannelId == channelId))
                    {
                        db.RecordYoutubeChannel.Remove(db.RecordYoutubeChannel.First((x) => x.YoutubeChannelId == channelId));
                        db.SaveChanges();
                        await button.SendConfirmAsync(BotLocalizer, locale, "YoutubeSpider.RecordRemoved", true, true);
                    }
                    else
                    {
                        db.RecordYoutubeChannel.Add(new DataBase.Table.RecordYoutubeChannel() { YoutubeChannelId = channelId });
                        db.SaveChanges();
                        await button.SendConfirmAsync(BotLocalizer, locale, "YoutubeSpider.RecordAdded", true, true);
                    }
                }

                var embed = RebuildSpiderEmbed(button, "已新增 YouTube 頻道爬蟲",
                        Format.Url(youtubeChannelSpider.ChannelTitle, $"https://www.youtube.com/channel/{youtubeChannelSpider.ChannelId}"))
                    .AddField("認可頻道", youtubeChannelSpider.IsTrustedChannel ? "是" : "否", true)
                    .AddField("錄影頻道", db.RecordYoutubeChannel.Any((x) => x.YoutubeChannelId == channelId) ? "是" : "否", true).Build();

                await button.ModifyOriginalResponseAsync((func) =>
                {
                    func.Embed = embed;
                });
            });

        [ComponentInteraction("spider_twitch:*:*", true)]
        public Task HandleTwitchAsync(string action, string userId)
            => RunOwnerButtonAsync("Twitch", async button =>
            {
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

                var embed = RebuildSpiderEmbed(button, "已新增 Twitch 頻道爬蟲",
                        Format.Url(twitchSpider.UserName, $"https://twitch.tv/{twitchSpider.UserLogin}"))
                    .AddField("頻道狀態", twitchSpider.IsWarningUser ? "警告" : "普通", true)
                    .AddField("頻道錄影", twitchSpider.IsRecord ? "開啟" : "關閉", true).Build();

                await button.ModifyOriginalResponseAsync((func) =>
                {
                    func.Embed = embed;
                });
            });

        [ComponentInteraction("spider_tc:*:*", true)]
        public Task HandleTwitcastingAsync(string action, string screenId)
            => RunOwnerButtonAsync("TwitCasting", async button =>
            {
                string locale = await GetLocaleAsync(true);

                using var db = _dbService.GetDbContext();
                var twitcastingSpider = await db.TwitcastingSpider.FirstOrDefaultAsync((x) => x.ScreenId == screenId);
                if (twitcastingSpider == null)
                {
                    await button.SendErrorAsync(BotLocalizer, locale, "Components.ChannelRemoved", true, true);
                    return;
                }

                if (action.Contains("warning"))
                {
                    twitcastingSpider.IsWarningUser = !twitcastingSpider.IsWarningUser;
                    db.TwitcastingSpider.Update(twitcastingSpider);
                    await db.SaveChangesAsync();

                    await button.SendConfirmAsync(BotLocalizer, locale, "Spider.StatusChanged", true, true,
                        twitcastingSpider.ChannelTitle,
                        BotLocalizer.Get(twitcastingSpider.IsWarningUser ? "Common.Warning" : "Common.Normal", locale));
                }
                else if (action.Contains("record"))
                {
                    twitcastingSpider.IsRecord = !twitcastingSpider.IsRecord;
                    db.TwitcastingSpider.Update(twitcastingSpider);
                    await db.SaveChangesAsync();

                    await button.SendConfirmAsync(BotLocalizer, locale, "Spider.RecordingChanged", true, true,
                        twitcastingSpider.ChannelTitle,
                        BotLocalizer.Get(twitcastingSpider.IsRecord ? "Common.Enabled" : "Common.Disabled", locale));
                }

                var embed = RebuildSpiderEmbed(button, "已新增 TwitCasting 頻道爬蟲",
                        Format.Url(twitcastingSpider.ChannelTitle, $"https://twitcasting.tv/{twitcastingSpider.ScreenId}"))
                    .AddField("頻道狀態", twitcastingSpider.IsWarningUser ? "警告" : "普通", true)
                    .AddField("頻道錄影", twitcastingSpider.IsRecord ? "開啟" : "關閉", true).Build();

                await button.ModifyOriginalResponseAsync((func) =>
                {
                    func.Embed = embed;
                });
            });

        [ComponentInteraction("spider_chzzk:*:*", true)]
        public Task HandleChzzkAsync(string action, string channelId)
            => RunOwnerButtonAsync("CHZZK", async button =>
            {
                // 切換語意（對齊 Twitch／TwitCasting 的切換按鈕），不是盲目反轉加入／移除；
                // 每次操作都重新檢查擁有者與爬蟲是否存在，舊訊息不得繞過權限或重建已刪除的爬蟲。
                if (!action.Contains("record"))
                {
                    await button.SendErrorAsync("不支援的操作", true);
                    return;
                }

                var result = await _chzzkRecordService.ToggleAutoRecordAsync(channelId, GracefulShutdown.Token);
                string locale = await GetLocaleAsync(true);
                if (result.Code == "record.not-configured")
                {
                    await button.SendErrorAsync(BotLocalizer, locale, "Components.ChannelRemoved", true, true);
                    return;
                }

                bool isRecord = result.Arguments.Value<bool>("enabled");
                string channelName = result.Arguments.Value<string>("sourceName") ?? channelId;
                await button.SendConfirmAsync(BotLocalizer, locale, "Spider.RecordingChanged", true, true,
                    channelName,
                    BotLocalizer.Get(isRecord ? "Common.Enabled" : "Common.Disabled", locale));
                await UpdateChzzkSpiderMessageAsync(button, channelId, channelName, isRecord);
            });

        /// <summary>
        /// 爬蟲管理按鈕的共用流程：限 Bot 擁有者操作、記錄點擊並延遲回應後執行 <paramref name="handler"/>；
        /// 未預期的例外統一記錄並回覆未知錯誤。
        /// </summary>
        private async Task RunOwnerButtonAsync(string platformName, Func<SocketMessageComponent, Task> handler)
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
                await handler(button);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"處理 {platformName} 爬蟲管理按鈕時失敗");
                try
                {
                    string locale = await GetLocaleAsync(true);
                    await Context.Interaction.SendErrorAsync(BotLocalizer, locale, "Errors.Unknown",
                        Context.Interaction.HasResponded, true);
                }
                catch (Exception responseException)
                {
                    Log.Error(responseException.Demystify(), $"回覆 {platformName} 爬蟲管理按鈕未知錯誤時失敗");
                }
            }
        }

        /// <summary>
        /// 沿用原訊息的「伺服器」「執行者」欄位重建爬蟲管理 embed（頻道、伺服器、執行者），其餘欄位由呼叫端附加。
        /// </summary>
        private static EmbedBuilder RebuildSpiderEmbed(SocketMessageComponent button, string title, string channelField)
        {
            var guild = button.Message.Embeds.First().Fields.FirstOrDefault((x) => x.Name == "伺服器").Value;
            var user = button.Message.Embeds.First().Fields.FirstOrDefault((x) => x.Name == "執行者").Value;
            return new EmbedBuilder()
                .WithOkColor()
                .WithTitle(title)
                .AddField("頻道", channelField, false)
                .AddField("伺服器", guild, false)
                .AddField("執行者", user, false);
        }

        private static Task UpdateChzzkSpiderMessageAsync(
            SocketMessageComponent button, string channelId, string channelName, bool isRecord)
        {
            var embed = RebuildSpiderEmbed(button, "已新增 CHZZK 頻道爬蟲",
                    Format.Url(string.IsNullOrEmpty(channelName) ? channelId : channelName, ChzzkUrls.Channel(channelId)))
                .AddField("錄影頻道", isRecord ? "開啟" : "關閉", true)
                .Build();
            var components = new ComponentBuilder()
                .WithButton("切換自動錄影", $"spider_chzzk:record:{channelId}", ButtonStyle.Success)
                .Build();

            return button.ModifyOriginalResponseAsync((func) =>
            {
                func.Embed = embed;
                func.Components = components;
            });
        }
    }
}
