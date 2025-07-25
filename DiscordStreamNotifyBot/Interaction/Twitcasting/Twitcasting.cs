﻿using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Interaction.Attribute;

namespace DiscordStreamNotifyBot.Interaction.TwitCasting
{
    [RequireContext(ContextType.Guild)]
    [Group("twitcasting", "TwitCasting 通知")]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    [DefaultMemberPermissions(GuildPermission.ManageMessages)]
    public class Twitcasting : TopLevelModule<SharedService.Twitcasting.TwitcastingService>
    {
        private readonly DiscordSocketClient _client;
        private readonly MainDbService _dbService;

        public class GuildNoticeTwitCastingChannelIdAutocompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                return await Task.Run(async () =>
                {
                    using var db = Bot.DbService.GetDbContext();
                    if (!await db.NoticeTwitcastingStreamChannels.AsNoTracking().AnyAsync((x) => x.GuildId == context.Guild.Id))
                        return AutocompletionResult.FromSuccess();

                    var channelIdList = db.NoticeTwitcastingStreamChannels
                        .AsNoTracking()
                        .Where((x) => x.GuildId == context.Guild.Id)
                        .Select((x) => new KeyValuePair<string, string>(db.GetTwitCastingChannelTitleByScreenId(x.ScreenId), x.ScreenId));

                    var channelIdList2 = new Dictionary<string, string>();
                    try
                    {
                        string value = autocompleteInteraction.Data.Current.Value.ToString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            foreach (var item in channelIdList)
                            {
                                if (item.Key.Contains(value, StringComparison.CurrentCultureIgnoreCase) || item.Value.Contains(value, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    channelIdList2.Add(item.Key, item.Value);
                                }
                            }
                        }
                        else
                        {
                            foreach (var item in channelIdList)
                            {
                                channelIdList2.Add(item.Key, item.Value);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"GuildNoticeTwitCastingChannelIdAutocompleteHandler - {ex}");
                    }

                    List<AutocompleteResult> results = new();
                    foreach (var item in channelIdList2)
                    {
                        results.Add(new AutocompleteResult(item.Key, item.Value));
                    }

                    return AutocompletionResult.FromSuccess(results.Take(25));
                });
            }
        }

        public Twitcasting(DiscordSocketClient client, MainDbService dbService)
        {
            _client = client;
            _dbService = dbService;
        }

        [CommandExample("nana_kaguraaa", "https://twitcasting.tv/nana_kaguraaa")]
        [SlashCommand("add", "新增 TwitCasting 直播通知的頻道")]
        public async Task AddChannel([Summary("頻道網址")] string channelUrl,
            [Summary("發送通知的頻道"), ChannelTypes(ChannelType.Text, ChannelType.News)] IChannel channel)
        {
            if (!_service.IsEnable)
            {
                await Context.Interaction.SendErrorAsync("此 Bot 的 TwitCasting 功能已關閉，請向 Bot 擁有者確認").ConfigureAwait(false);
                return;
            }

            await DeferAsync(true).ConfigureAwait(false);

            var textChannel = channel as IGuildChannel;

            var permissions = Context.Guild.GetUser(_client.CurrentUser.Id).GetPermissions(textChannel);
            if (!permissions.ViewChannel || !permissions.SendMessages)
            {
                await Context.Interaction.SendErrorAsync($"我在 `{textChannel}` 沒有 `讀取&編輯頻道` 的權限，請給予權限後再次執行本指令", true);
                return;
            }

            if (!permissions.EmbedLinks)
            {
                await Context.Interaction.SendErrorAsync($"我在 `{textChannel}` 沒有 `嵌入連結` 的權限，請給予權限後再次執行本指令", true);
                return;
            }

            var channelData = await _service.GetChannelNameAndTitleAsync(channelUrl);
            if (channelData == null)
            {
                await Context.Interaction.SendErrorAsync("TwitCasting 上找不到該使用者的名稱\n" +
                    "這不是 Twitch 直播通知指令!!!!!!!!!!!!!!!!!!\n" +
                    "請確認網址是否正確，若正確請向 Bot 擁有者回報", true);
                return;
            }

            using (var db = _dbService.GetDbContext())
            {
                var noticeTwitCastingStreamChannel = db.NoticeTwitcastingStreamChannels.FirstOrDefault((x) => x.GuildId == Context.Guild.Id && x.ScreenId == channelData.ScreenId);
                if (noticeTwitCastingStreamChannel != null)
                {
                    if (await PromptUserConfirmAsync($"`{channelData.Name}` 已在直播通知清單內，是否覆蓋設定?").ConfigureAwait(false))
                    {
                        noticeTwitCastingStreamChannel.DiscordChannelId = textChannel.Id;
                        db.NoticeTwitcastingStreamChannels.Update(noticeTwitCastingStreamChannel);
                        await Context.Interaction.SendConfirmAsync($"已將 `{channelData.Name}` 的通知頻道變更至: {textChannel}", true, true).ConfigureAwait(false);
                    }
                    else return;
                }
                else
                {
                    string addString = "";
                    if (!db.TwitcastingSpider.Any((x) => x.ScreenId == channelData.ScreenId))
                        addString += $"\n\n(注意: 該頻道未加入爬蟲清單\n如長時間無通知請使用 `/help get-command-help twitcasting-spider add` 查看說明並加入爬蟲)";
                    db.NoticeTwitcastingStreamChannels.Add(new NoticeTwitcastingStreamChannel() { GuildId = Context.Guild.Id, DiscordChannelId = textChannel.Id, ScreenId = channelData.ScreenId });
                    await Context.Interaction.SendConfirmAsync($"已將 `{channelData.Name}` 加入到 TwitCasting 通知頻道清單內{addString}", true, true).ConfigureAwait(false);
                }

                db.SaveChanges();
            }
        }

        [CommandExample("nana_kaguraaa", "https://twitcasting.tv/nana_kaguraaa")]
        [SlashCommand("remove", "移除 TwitCasting 直播通知的頻道")]
        public async Task RemoveChannel([Summary("頻道網址"), Autocomplete(typeof(GuildNoticeTwitCastingChannelIdAutocompleteHandler))] string channelUrl)
        {
            await DeferAsync(true).ConfigureAwait(false);

            var channelData = await _service.GetChannelNameAndTitleAsync(channelUrl);
            if (channelData == null)
            {
                await Context.Interaction.SendErrorAsync("錯誤，TwitCasting 找不到該使用者的名稱\n" +
                    "請確認網址是否正確，若正確請向 Bot 擁有者回報\n" +
                    $"(注意: 設定時請勿切換 Discord 頻道，這會導致自動輸入的頻道名稱跑掉)", true).ConfigureAwait(false);
                return;
            }

            using (var db = _dbService.GetDbContext())
            {
                if (!db.NoticeTwitcastingStreamChannels.Any((x) => x.GuildId == Context.Guild.Id))
                {
                    await Context.Interaction.SendErrorAsync("並未設定直播通知...", true).ConfigureAwait(false);
                    return;
                }

                if (!db.NoticeTwitcastingStreamChannels.Any((x) => x.GuildId == Context.Guild.Id && x.ScreenId == channelData.ScreenId))
                {
                    await Context.Interaction.SendErrorAsync($"並未設定 `{channelData.Name}` 的直播通知...", true).ConfigureAwait(false);
                    return;
                }
                else
                {
                    db.NoticeTwitcastingStreamChannels.Remove(db.NoticeTwitcastingStreamChannels.First((x) => x.GuildId == Context.Guild.Id && x.ScreenId == channelData.ScreenId));
                    db.SaveChanges();
                    await Context.Interaction.SendConfirmAsync($"已移除 `{channelData.Name}`", true, true).ConfigureAwait(false);
                }
            }
        }

        [SlashCommand("list", "顯示現在已加入通知清單的 TwitCasting 直播頻道")]
        public async Task ListChannel([Summary("頁數")] int page = 0)
        {
            using (var db = _dbService.GetDbContext())
            {
                var list = Queryable.Where(db.NoticeTwitcastingStreamChannels, (x) => x.GuildId == Context.Guild.Id)
                    .Select((x) => $"`{db.GetTwitCastingChannelTitleByScreenId(x.ScreenId)}` => <#{x.DiscordChannelId}>").ToList();

                if (list.Count == 0) { await Context.Interaction.SendErrorAsync("TwitCasting 直播通知清單為空").ConfigureAwait(false); return; }

                await Context.SendPaginatedConfirmAsync(page, page =>
                {
                    return new EmbedBuilder()
                        .WithOkColor()
                        .WithTitle("TwitCasting 直播通知清單")
                        .WithDescription(string.Join('\n', list.Skip(page * 20).Take(20)))
                        .WithFooter($"{Math.Min(list.Count, (page + 1) * 20)} / {list.Count} 個頻道");
                }, list.Count, 20, false);
            }
        }

        [RequireBotPermission(GuildPermission.MentionEveryone)]
        [CommandSummary("設定通知訊息\n" +
            "不輸入通知訊息的話則會關閉通知訊息\n" +
            "需先新增直播通知後才可設定通知訊息(`/help get-command-help twitcasting add`)\n\n" +
            "(考慮到有伺服器需 Ping 特定用戶組的情況，故 Bot 需提及所有身分組權限)")]
        [CommandExample("nana_kaguraaa 開台啦", "https://twitcasting.tv/nana_kaguraaa 開台啦")]
        [SlashCommand("set-message", "設定通知訊息")]
        public async Task SetMessage([Summary("頻道網址"), Autocomplete(typeof(GuildNoticeTwitCastingChannelIdAutocompleteHandler))] string channelUrl, [Summary("通知訊息")] string message = "")
        {
            await DeferAsync(true).ConfigureAwait(false);

            var channelData = await _service.GetChannelNameAndTitleAsync(channelUrl);
            if (channelData == null)
            {
                await Context.Interaction.SendErrorAsync("錯誤，TwitCasting 找不到該使用者的名稱\n" +
                    "請確認網址是否正確，若正確請向 Bot 擁有者回報", true);
                return;
            }

            using (var db = _dbService.GetDbContext())
            {
                if (db.NoticeTwitcastingStreamChannels.Any((x) => x.GuildId == Context.Guild.Id && x.ScreenId == channelData.ScreenId))
                {
                    var noticeStreamChannel = db.NoticeTwitcastingStreamChannels.First((x) => x.GuildId == Context.Guild.Id && x.ScreenId == channelData.ScreenId);

                    noticeStreamChannel.StartStreamMessage = message.Trim();
                    db.NoticeTwitcastingStreamChannels.Update(noticeStreamChannel);
                    db.SaveChanges();

                    if (message != "") await Context.Interaction.SendConfirmAsync($"已設定 `{channelData.Name}` 的 TwitCasting 直播通知訊息為:\n{message}", true, true).ConfigureAwait(false);
                    else await Context.Interaction.SendConfirmAsync($"已取消 `{channelData.Name}` 的 TwitCasting 直播通知訊息", true, true).ConfigureAwait(false);
                }
                else
                {
                    await Context.Interaction.SendErrorAsync($"並未設定 `{channelData.Name}` 的 TwitCasting 直播通知\n" +
                        $"請先使用 `/twitcasting add {channelData.ScreenId}` 新增通知後再設定通知訊息", true).ConfigureAwait(false);
                }
            }
        }

        [SlashCommand("list-message", "列出已設定的 TwitCasting 直播通知訊息")]
        public async Task ListMessage([Summary("頁數")] int page = 0)
        {
            using (var db = _dbService.GetDbContext())
            {
                if (db.NoticeTwitcastingStreamChannels.Any((x) => x.GuildId == Context.Guild.Id))
                {
                    var noticeTwitterSpaces = db.NoticeTwitcastingStreamChannels.Where((x) => x.GuildId == Context.Guild.Id);
                    Dictionary<string, string> dic = new Dictionary<string, string>();

                    foreach (var item in noticeTwitterSpaces)
                    {
                        string message = string.IsNullOrWhiteSpace(item.StartStreamMessage) ? "無" : item.StartStreamMessage;
                        dic.Add(db.GetTwitCastingChannelTitleByScreenId(item.ScreenId), message);
                    }

                    try
                    {
                        await Context.SendPaginatedConfirmAsync(page, (page) =>
                        {
                            EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor().WithTitle("TwitCasting 直播通知訊息清單")
                                .WithDescription("如果沒訊息的話就代表沒設定\n不用擔心會 Tag 到用戶組，Embed 不會有 Ping 的反應");

                            foreach (var item in dic.Skip(page * 10).Take(10))
                            {
                                embedBuilder.AddField(item.Key, item.Value);
                            }

                            return embedBuilder;
                        }, dic.Count, 10).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Demystify(), $"TwitCasting-ListMessage: {Context.Guild.Id}");
                    }
                }
                else
                {
                    await Context.Interaction.SendErrorAsync($"並未設定 TwitCasting 直播通知\n" +
                        $"請先使用 `/help get-command-help twitcasting add` 查看說明並新增 TwitCasting 直播通知").ConfigureAwait(false);
                }
            }
        }
    }
}