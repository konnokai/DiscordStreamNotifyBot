using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.SharedService.YoutubeMember;

namespace DiscordStreamNotifyBot.Interaction.YoutubeMember
{
    [Group("member", "YouTube 會限驗證相關指令")]
    public class YoutubeMember : TopLevelModule<YoutubeMemberService>
    {
        private readonly MainDbService _dbService;
        public YoutubeMember(MainDbService dbService)
        {
            _dbService = dbService;
        }

        [RequireContext(ContextType.Guild)]
        [SlashCommand("check", "確認是否已到網站登入綁定")]
        public async Task CheckAsync()
        {
            await DeferAsync(true);

            if (!_service.IsEnable)
            {
                await SendLocalizedErrorAsync("Member.Errors.Disabled", true, true, Bot.ApplicatonOwner);
                return;
            }

            try
            {
                using (var db = _dbService.GetDbContext())
                {
                    var guildYoutubeMemberConfigs = db.GuildYoutubeMemberConfig.AsNoTracking().Where((x) => x.GuildId == Context.Guild.Id);
                    if (!guildYoutubeMemberConfigs.Any())
                    {
                        await SendLocalizedErrorAsync("Member.Errors.NotConfigured", true);
                        return;
                    }

                    if (guildYoutubeMemberConfigs.Any((x) => string.IsNullOrEmpty(x.MemberCheckChannelTitle) || x.MemberCheckVideoId == "-"))
                    {
                        await SendLocalizedErrorAsync("Member.Errors.Initializing", true);
                        return;
                    }

                    if (!await _service.IsExistUserTokenAsync(Context.User.Id.ToString()))
                    {
                        string locale = await GetLocaleAsync(true);
                        await SendLocalizedErrorAsync("Member.Errors.LoginRequired", true, true,
                            Format.Url(BotLocalizer.Get("Common.Website", locale), "https://stream-bot.konnokai.me/"));
                        return;
                    }

                    if (guildYoutubeMemberConfigs.Count() == 1)
                    {
                        string locale = SupportedLocale.Normalize(Context.Interaction.UserLocale);
                        var memberCheck = db.YoutubeMemberCheck.FirstOrDefault((x) =>
                            x.UserId == Context.User.Id &&
                            x.GuildId == Context.Guild.Id &&
                            x.CheckYTChannelId == guildYoutubeMemberConfigs.First().MemberCheckChannelId);
                        if (memberCheck == null)
                        {
                            db.YoutubeMemberCheck.Add(new DataBase.Table.YoutubeMemberCheck()
                            {
                                UserId = Context.User.Id,
                                GuildId = Context.Guild.Id,
                                CheckYTChannelId = guildYoutubeMemberConfigs.First().MemberCheckChannelId,
                                Locale = locale
                            });
                        }
                        else
                        {
                            memberCheck.Locale = locale;
                        }
                        db.SaveChanges();
                        await SendLocalizedConfirmAsync("Member.CheckQueuedWithDmNotice", true, true, 5);
                    }
                    else
                    {
                        // Todo: 超過 25 個選項時需提供換頁的選項
                        SelectMenuBuilder selectMenuBuilder = new SelectMenuBuilder()
                           .WithPlaceholder(BotLocalizer.Get("Member.Select.ChannelPlaceholder", await GetLocaleAsync(true)))
                           .WithMinValues(1)
                           .WithMaxValues(guildYoutubeMemberConfigs.Count())
                           .WithCustomId($"member:check:{Context.Guild.Id}:{Context.User.Id}");

                        foreach (var item in guildYoutubeMemberConfigs)
                            selectMenuBuilder.AddOption(item.MemberCheckChannelTitle, item.MemberCheckChannelId);

                        string locale = await GetLocaleAsync(true);
                        await Context.Interaction.FollowupAsync(BotLocalizer.Get("Member.Select.Description", locale), components: new ComponentBuilder()
                       .WithSelectMenu(selectMenuBuilder)
                       .Build(), ephemeral: true);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "Member Check Error");
                await SendLocalizedErrorAsync("Errors.Unknown", true);
            }
        }

        [RequireContext(ContextType.Guild)]
        [SlashCommand("cancel-member-check", "取消本伺服器的會限驗證，會一併移除會限驗證用戶組")]
        public async Task CancelMemberCheckAsync()
        {
            await DeferAsync(true);

            using (var db = _dbService.GetDbContext())
            {
                try
                {
                    var youtubeMemberChecks = db.YoutubeMemberCheck.Where((x) => x.UserId == Context.User.Id && x.GuildId == Context.Guild.Id);
                    if (!youtubeMemberChecks.Any())
                    {
                        await SendLocalizedErrorAsync("Member.Errors.NoActiveCheck", true);
                        return;
                    }

                    var guildYoutubeMemberConfigs = db.GuildYoutubeMemberConfig.Where((x) => x.GuildId == Context.Guild.Id);
                    foreach (var item in guildYoutubeMemberConfigs)
                    {
                        try
                        {
                            await Context.Client.Rest.RemoveRoleAsync(Context.Guild.Id, Context.User.Id, item.MemberCheckGrantRoleId);
                        }
                        catch { }
                    }

                    db.YoutubeMemberCheck.RemoveRange(youtubeMemberChecks);
                    db.SaveChanges();

                    await SendLocalizedConfirmAsync("Member.CheckCancelled", true, true);
                }
                catch (Exception ex)
                {
                    await SendLocalizedErrorAsync("Member.Errors.SaveFailed", true, true, Bot.ApplicatonOwner);
                    Log.Error(ex.ToString());
                }
            }
        }

        [SlashCommand("unlink", "解除 Discord 與 Google 綁定並移除授權")]
        public async Task UnlinkAsync()
        {
            await DeferAsync(true);

            if (!_service.IsEnable)
            {
                await SendLocalizedErrorAsync("Member.Errors.Disabled", true, true, Bot.ApplicatonOwner);
                return;
            }

            using (var db = _dbService.GetDbContext())
            {
                if (await _service.IsExistUserTokenAsync(Context.User.Id.ToString()))
                {
                    if (!await PromptUserConfirmAsync("Member.UnlinkPrompt"))
                        return;

                    await Bot.RedisSub.PublishAsync(new RedisChannel("member.revokeToken", RedisChannel.PatternMode.Literal), Context.User.Id);

                    try
                    {
                        await _service.RevokeUserGoogleCertAsync(Context.User.Id.ToString());
                        await SendLocalizedConfirmAsync("Member.Unlinked", true, true);
                    }
                    catch (NullReferenceException nullEx)
                    {
                        string locale = await GetLocaleAsync(true);
                        await SendLocalizedErrorAsync("Member.Errors.GoogleRevokeFailed", true, true,
                            Format.Url(BotLocalizer.Get("Common.GoogleSecurity", locale), "https://myaccount.google.com/permissions"));
                        Log.Warn($"RevokeTokenNull: {nullEx.Message} ({Context.User.Id})");
                    }
                    catch (Exception)
                    {
                        await SendLocalizedErrorAsync("Member.Errors.UnlinkFailed", true, true, Bot.ApplicatonOwner);
                    }
                }
                else
                {
                    await SendLocalizedErrorAsync("Member.Errors.NothingToUnlink", true, true);
                }
            }
        }

        [RequireContext(ContextType.Guild)]
        [SlashCommand("list-can-check-channel", "顯示現在可供驗證的會限頻道清單")]
        public async Task ListCheckChannel()
        {
            using (var db = _dbService.GetDbContext())
            {
                var guildYoutubeMemberConfigs = db.GuildYoutubeMemberConfig.Where((x) => x.GuildId == Context.Guild.Id);
                if (!guildYoutubeMemberConfigs.Any())
                {
                    await SendLocalizedErrorAsync("Member.Errors.ChannelListEmpty");
                    return;
                }

                if (guildYoutubeMemberConfigs.Any((x) => string.IsNullOrEmpty(x.MemberCheckChannelTitle) || x.MemberCheckVideoId == "-"))
                {
                    await SendLocalizedErrorAsync("Member.Errors.Initializing");
                    return;
                }

                await SendLocalizedConfirmAsync("Member.ChannelList", false, true,
                    string.Join('\n', guildYoutubeMemberConfigs.Select((x) =>
                        $"{Format.Url(x.MemberCheckChannelTitle, $"https://www.youtube.com/channel/{x.MemberCheckChannelId}")}: <@&{x.MemberCheckGrantRoleId}>")));
            }
        }

        [SlashCommand("show-my-youtube-account", "顯示現在綁定的 Youtube 帳號")]
        public async Task ShowYoutubeAccountAsync()
        {
            await DeferAsync(true);

            if (!_service.IsEnable)
            {
                await SendLocalizedErrorAsync("Member.Errors.Disabled", true, true, Bot.ApplicatonOwner);
                return;
            }

            try
            {
                var channelUrl = await _service.GetYoutubeDataAsync(Context.User.Id.ToString());
                await SendLocalizedConfirmAsync("Member.LinkedChannel", true, true, channelUrl);
            }
            catch (NullReferenceException nullEx)
            {
                switch (nullEx.Message)
                {
                    case "userId":
                        await SendLocalizedErrorAsync("Errors.Unknown", true);
                        break;
                    case "token":
                    case "userCert":
                    case "channel":
                        await SendLocalizedErrorAsync("Member.Errors.LinkedChannelUnavailable", true);
                        break;
                    default:
                        await SendLocalizedErrorAsync("Member.Errors.LinkedChannelUnavailableWithOwner", true, true, Bot.ApplicatonOwner);
                        Log.Error(nullEx.ToString());
                        break;
                }
            }
            catch (Exception ex)
            {
                await SendLocalizedErrorAsync("Member.Errors.LinkedChannelUnavailableWithOwner", true, true, Bot.ApplicatonOwner);
                Log.Error(ex.ToString());
            }
        }
    }
}
