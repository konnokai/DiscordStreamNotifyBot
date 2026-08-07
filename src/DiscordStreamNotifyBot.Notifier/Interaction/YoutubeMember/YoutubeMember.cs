using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService.Member;
using DiscordStreamNotifyBot.SharedService.YoutubeMember;

namespace DiscordStreamNotifyBot.Interaction.YoutubeMember
{
    [Group("youtube-member", "YouTube 會限驗證相關指令")]
    public class YoutubeMember : TopLevelModule<YoutubeMemberService>
    {
        private readonly MainDbService _dbService;
        private readonly MemberOperationCoordinator _operationCoordinator;
        private readonly YoutubeMemberRoleService _roleService;

        public YoutubeMember(
            MainDbService dbService,
            MemberOperationCoordinator operationCoordinator,
            YoutubeMemberRoleService roleService)
        {
            _dbService = dbService;
            _operationCoordinator = operationCoordinator;
            _roleService = roleService;
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
                List<DataBase.Table.GuildYoutubeMemberConfig> guildYoutubeMemberConfigs;
                using (var db = _dbService.GetDbContext())
                {
                    guildYoutubeMemberConfigs = await db.GuildYoutubeMemberConfig.AsNoTracking()
                        .Where(x => x.GuildId == Context.Guild.Id && !x.DeletionPending)
                        .ToListAsync(GracefulShutdown.Token);
                }
                if (guildYoutubeMemberConfigs.Count == 0)
                {
                    await SendLocalizedErrorAsync("Member.Errors.NotConfigured", true);
                    return;
                }

                if (guildYoutubeMemberConfigs.Any(x => string.IsNullOrEmpty(x.MemberCheckChannelTitle) || x.MemberCheckVideoId == "-"))
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

                if (guildYoutubeMemberConfigs.Count > 25)
                {
                    await SendLocalizedErrorAsync("MemberSetting.Errors.SelectLimit", true, true, 25);
                    return;
                }

                if (guildYoutubeMemberConfigs.Count == 1)
                {
                    if (!await QueueSingleConfigurationCheckAsync(guildYoutubeMemberConfigs[0].MemberCheckChannelId))
                    {
                        await SendLocalizedErrorAsync("Components.Invalid", true, true);
                        return;
                    }
                    await SendLocalizedConfirmAsync("Member.CheckQueuedWithDmNotice", true, true, 5);
                }
                else
                {
                    // Todo: 超過 25 個選項時需提供換頁的選項
                    SelectMenuBuilder selectMenuBuilder = new SelectMenuBuilder()
                       .WithPlaceholder(BotLocalizer.Get("Member.Select.ChannelPlaceholder", await GetLocaleAsync(true)))
                       .WithMinValues(1)
                       .WithMaxValues(guildYoutubeMemberConfigs.Count)
                        .WithCustomId($"youtube-member-check:{Context.Guild.Id}:{Context.User.Id}");

                    foreach (var item in guildYoutubeMemberConfigs)
                        selectMenuBuilder.AddOption(item.MemberCheckChannelTitle, item.MemberCheckChannelId);

                    string locale = await GetLocaleAsync(true);
                    await Context.Interaction.FollowupAsync(BotLocalizer.Get("Member.Select.Description", locale), components: new ComponentBuilder()
                   .WithSelectMenu(selectMenuBuilder)
                   .Build(), ephemeral: true);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "Member Check Error");
                await SendLocalizedErrorAsync("Errors.Unknown", true);
            }
        }

        /// <summary>單一設定也要使用與選單相同的 user→guild 鎖與 fresh read，跨 instance 唯一鍵競爭時重讀後安全 requeue。</summary>
        private async Task<bool> QueueSingleConfigurationCheckAsync(string channelId)
        {
            string locale = SupportedLocale.Normalize(Context.Interaction.UserLocale);
            await using var userLock = await _operationCoordinator.LockUserAsync(Context.User.Id, GracefulShutdown.Token);
            await using var guildLock = await _operationCoordinator.LockGuildAsync(Context.Guild.Id, GracefulShutdown.Token);
            try
            {
                using var db = _dbService.GetDbContext();
                var config = await db.GuildYoutubeMemberConfig.AsNoTracking().SingleOrDefaultAsync(x =>
                    x.GuildId == Context.Guild.Id && x.MemberCheckChannelId == channelId && !x.DeletionPending,
                    GracefulShutdown.Token);
                if (!YoutubeMemberPolicies.IsActiveConfiguration(config))
                    return false;

                var check = await db.YoutubeMemberCheck.SingleOrDefaultAsync(x =>
                    x.UserId == Context.User.Id && x.GuildId == Context.Guild.Id && x.CheckYTChannelId == channelId,
                    GracefulShutdown.Token);
                if (check == null)
                {
                    db.YoutubeMemberCheck.Add(new DataBase.Table.YoutubeMemberCheck
                    {
                        UserId = Context.User.Id,
                        GuildId = Context.Guild.Id,
                        CheckYTChannelId = channelId,
                        Locale = locale
                    });
                }
                else
                    ApplySingleConfigurationQueue(check, locale);
                await db.SaveChangesAsync(GracefulShutdown.Token);
                return true;
            }
            catch (DbUpdateException)
            {
                // 其他 Notifier instance 剛插入相同自然鍵時，重新讀取並將它轉回待驗證而非回傳 500。
                using var retryDb = _dbService.GetDbContext();
                var existing = await retryDb.YoutubeMemberCheck.SingleOrDefaultAsync(x =>
                    x.UserId == Context.User.Id && x.GuildId == Context.Guild.Id && x.CheckYTChannelId == channelId,
                    GracefulShutdown.Token);
                if (existing == null)
                    return false;
                ApplySingleConfigurationQueue(existing, locale);
                await retryDb.SaveChangesAsync(GracefulShutdown.Token);
                return true;
            }
        }

        private static void ApplySingleConfigurationQueue(DataBase.Table.YoutubeMemberCheck check, string locale)
        {
            // 必須與 YoutubeMemberComponent 的 selection diff 保持一致；重試同樣不可降級 verified row。
            if (YoutubeMemberPolicies.DecideSingleConfigurationQueue(check) ==
                YoutubeMemberSingleConfigurationQueueAction.RequeuePendingRoleRemoval)
            {
                YoutubeMemberPolicies.QueueVerification(check);
            }
            check.Locale = locale;
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
                    await using var userLock = await _operationCoordinator.LockUserAsync(
                        Context.User.Id, GracefulShutdown.Token);
                    await using var guildLock = await _operationCoordinator.LockGuildAsync(
                        Context.Guild.Id, GracefulShutdown.Token);
                    var youtubeMemberChecks = db.YoutubeMemberCheck.Where((x) => x.UserId == Context.User.Id && x.GuildId == Context.Guild.Id).ToList();
                    if (!youtubeMemberChecks.Any())
                    {
                        await SendLocalizedErrorAsync("Member.Errors.NoActiveCheck", true);
                        return;
                    }

                    foreach (var item in youtubeMemberChecks)
                        YoutubeMemberPolicies.QueueRoleRemoval(item);
                    db.SaveChanges();

                    var guildYoutubeMemberConfigs = db.GuildYoutubeMemberConfig.AsNoTracking()
                        .Where((x) => x.GuildId == Context.Guild.Id).ToDictionary(x => x.MemberCheckChannelId);
                    bool cleanupComplete = true;
                    foreach (var item in youtubeMemberChecks)
                    {
                        if (!guildYoutubeMemberConfigs.TryGetValue(item.CheckYTChannelId, out var config))
                        {
                            cleanupComplete = false;
                            continue;
                        }

                        if (await _roleService.RemoveAsync(config, Context.User.Id, GracefulShutdown.Token))
                            db.YoutubeMemberCheck.Remove(item);
                        else
                            cleanupComplete = false;
                    }
                    db.SaveChanges();

                    if (cleanupComplete)
                        await SendLocalizedConfirmAsync("Member.CheckCancelled", true, true);
                    else
                        await SendLocalizedErrorAsync("Member.Errors.RoleCleanupPending", true, true);
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

                    try
                    {
                        await _service.RevokeUserGoogleCertAsync(Context.User.Id.ToString());
                        // 本 shard 已保存 cleanup intent 並刪除本機 token，其他 shard 僅用此 hint 補做 Discord cleanup。
                        await Bot.RedisSub.PublishAsync(new RedisChannel("member.revokeToken", RedisChannel.PatternMode.Literal), Context.User.Id);
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
                var guildYoutubeMemberConfigs = db.GuildYoutubeMemberConfig
                    .Where((x) => x.GuildId == Context.Guild.Id && !x.DeletionPending);
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
