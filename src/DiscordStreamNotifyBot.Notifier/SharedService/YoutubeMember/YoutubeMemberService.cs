using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.SharedService.Youtube;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Polly;

namespace DiscordStreamNotifyBot.SharedService.YoutubeMember
{
    public partial class YoutubeMemberService : IInteractionService
    {
        public bool IsEnable { get; private set; } = true;

        Timer checkOldMemberStatus, checkNewMemberStatus;
        Timer checkOrphanMemberRole; // 僅 EnableGuildMembersIntent 開啟時建立（孤兒會限身分組對帳）
        private readonly GoogleAuthorizationCodeFlow flow;
        private readonly YoutubeStreamService _streamService;
        private readonly DiscordSocketClient _client;
        private readonly BotConfig _botConfig;
        private readonly MainDbService _dbService;
        private readonly BotLocalizer _localizer;
        private readonly CommandDisplayResolver _commandDisplayResolver;
        private readonly GuildLocaleService _guildLocaleService;
        private readonly LocaleResolver _localeResolver;
        private readonly IServiceProvider _services;
        private readonly NotifierMetrics _metrics;

        public YoutubeMemberService(YoutubeStreamService streamService, DiscordSocketClient discordSocketClient,
            BotConfig botConfig, MainDbService dbService, BotLocalizer localizer,
            CommandDisplayResolver commandDisplayResolver, GuildLocaleService guildLocaleService,
            LocaleResolver localeResolver, IServiceProvider services, NotifierMetrics metrics)
        {
            _streamService = streamService;
            _client = discordSocketClient;
            _botConfig = botConfig;
            _dbService = dbService;
            _localizer = localizer;
            _commandDisplayResolver = commandDisplayResolver;
            _guildLocaleService = guildLocaleService;
            _localeResolver = localeResolver;
            _services = services;
            _metrics = metrics;

            if (string.IsNullOrEmpty(_botConfig.GoogleClientId) || string.IsNullOrEmpty(_botConfig.GoogleClientSecret))
            {
                Log.Warn($"{nameof(BotConfig.GoogleClientId)} 或 {nameof(BotConfig.GoogleClientSecret)} 空白，無法使用會限驗證系統");
                IsEnable = false;
                return;
            }

            flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _botConfig.GoogleClientId,
                    ClientSecret = _botConfig.GoogleClientSecret
                },
                Scopes = ["https://www.googleapis.com/auth/youtube.force-ssl"],
                DataStore = new MySqlDataStore(_dbService)
            });

            Bot.RedisSub.Subscribe(new RedisChannel("member.revokeToken", RedisChannel.PatternMode.Literal), async (channel, value) =>
            {
                // 收斂到 shard 0：RemoveMemberCheckFromDbAsync 做的是全域 DB 刪除 + REST 移除用戶組（REST 跨 shard 有效），
                // 單一 shard 處理即完整；每個 shard 都做只是重複工作與 concurrency 例外。
                // 取捨：shard 0 當下離線時該次 revoke 會漏收，但每日 CheckMemberShip 會偵測 token 消失並移除，屬自癒。
                if (Bot.ShardId != 0)
                    return;

                try
                {
                    ulong userId = 0;
                    if (!ulong.TryParse(value.ToString(), out userId))
                        return;

                    Log.Info($"接收到 Redis 的 Revoke 請求: {userId}");

                    await RemoveMemberCheckFromDbAsync(userId);
                }
                catch (Exception ex)
                {
                    Log.Error($"MemberRevokeTokenFromRedis: {ex}");
                }
            });

            _client.SelectMenuExecuted += async (component) =>
            {
                if (component.HasResponded)
                    return;

                try
                {
                    string locale = await component.ResolveLocaleAsync(_services, true);
                    string[] customId = component.Data.CustomId.Split(new char[] { ':' });
                    if (customId.Length <= 2 || customId[0] != "member")
                    {
                        await component.SendErrorAsync(_localizer, locale, "Components.Invalid", ephemeral: true);
                        return;
                    }

                    using var db = _dbService.GetDbContext();
                    if (customId[1] == "check" && customId.Length == 4)
                    {
                        await component.DeferAsync(true);

                        if (!ulong.TryParse(customId[2], out ulong guildId))
                        {
                            await component.SendErrorAsync(_localizer, locale, "Errors.Unknown", true, true);
                            Log.Error(JsonConvert.SerializeObject(component));
                            return;
                        }

                        if (!ulong.TryParse(customId[3], out ulong userId))
                        {
                            await component.SendErrorAsync(_localizer, locale, "Errors.Unknown", true, true);
                            Log.Error(JsonConvert.SerializeObject(component));
                            return;
                        }

                        if (component.User.Id != userId)
                        {
                            await component.SendErrorAsync(_localizer, locale, "Components.NotAllowed", true, true);
                            return;
                        }

                        var youtubeMembers = db.YoutubeMemberCheck.Where((x) => x.UserId == userId && x.GuildId == guildId).ToList();
                        var guildYoutubeMemberConfigs = youtubeMembers.Count == 0
                            ? []
                            : db.GuildYoutubeMemberConfig.Where((x) => x.GuildId == guildId).ToList();

                        db.YoutubeMemberCheck.RemoveRange(youtubeMembers);
                        db.SaveChanges();

                        if (guildYoutubeMemberConfigs.Any())
                        {
                            foreach (var item in guildYoutubeMemberConfigs)
                            {
                                try { await _client.Rest.RemoveRoleAsync(item.GuildId, userId, item.MemberCheckGrantRoleId); }
                                catch (Exception) { }
                            }
                        }

                        foreach (var item in component.Data.Values)
                        {
                            db.YoutubeMemberCheck.Add(new YoutubeMemberCheck()
                            {
                                UserId = userId,
                                GuildId = guildId,
                                CheckYTChannelId = item,
                                Locale = SupportedLocale.Normalize(component.UserLocale)
                            });
                        }
                        db.SaveChanges();

                        try { await component.Message.DeleteAsync(); }
                        catch
                        {
                            await DisableSelectMenuAsync(component, locale,
                                _localizer.Format("Member.Select.SelectedCount", locale, component.Data.Values.Count));
                        }

                        await component.SendConfirmAsync(_localizer, locale, "Member.CheckQueuedWithDmNotice", true, true, 5);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), "處理會限驗證選單時失敗");
                    string locale = await component.ResolveLocaleAsync(_services, true);
                    await component.SendErrorAsync(_localizer, locale, "Errors.Unknown", true, true);
                    return;
                }
            };

            // 會限影片探索（CheckMemberShipOnlyVideoId）已搬至 Scraper（避免多 shard 重複燒配額），此處不再排程。
            checkOldMemberStatus = new Timer(new TimerCallback(async (obj) => await CheckMemberShip(obj)), true, TimeSpan.FromSeconds(Math.Round(Convert.ToDateTime($"{DateTime.Now.AddDays(1):yyyy/MM/dd 04:00:00}").Subtract(DateTime.Now).TotalSeconds)), TimeSpan.FromDays(1));
            checkNewMemberStatus = new Timer(new TimerCallback(async (obj) => await CheckMemberShip(obj)), false, TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(5));

            // GuildMembers 特權 intent 開啟時才啟用：會員重加入即時回補身分組 + 孤兒身分組每日對帳回收。
            // 旗標關（預設）＝完全不訂閱事件、不建對帳 timer，行為與現況一致，避免未取得特權時影響既有功能。
            if (_botConfig.EnableGuildMembersIntent)
            {
                _client.UserJoined += OnUserJoinedRestoreMemberRoleAsync;
                // 啟動 5 分鐘後（待 client Ready + 成員可下載）跑一次對帳，之後每日一次
                checkOrphanMemberRole = new Timer(new TimerCallback(async (_) => await ReconcileMemberRolesAsync()), null, TimeSpan.FromMinutes(5), TimeSpan.FromDays(1));
            }

            // token 儲存已改走 MySQL（MySqlDataStore 為真實來源），不再需要啟動時 Redis→DB 備份。
            // 一次性 backfill（切換前把 Redis TokenResponse:* 回填至 youtube_member_access_token）見 docs/MEMBER_TOKEN_STORE_MYSQL_PLAN.md 遷移章節。

            // 用 Utility.RedisKey（由 RedisTokenKeyProvisioner 佈建的叢集真實來源），而非 _botConfig.RedisTokenKey
            // （bootstrap 時非 shard 0 的設定檔可能仍為空）。只需公告一次，收斂到 shard 0。
            if (Bot.ShardId == 0)
            {
                Bot.RedisSub.Publish(new RedisChannel("member.syncRedisToken", RedisChannel.PatternMode.Literal), Utility.RedisKey);
                Log.Info("已同步 Redis Token");
            }
            _dbService = dbService;
        }

        public async Task<bool> IsExistUserTokenAsync(string discordUserId)
        {
            return await ((ITokenDataStore)flow.DataStore).IsExistUserTokenAsync<TokenResponse>(discordUserId);
        }

        public async Task RevokeUserGoogleCertAsync(string discordUserId = "")
        {
            try
            {
                if (string.IsNullOrEmpty(discordUserId))
                    throw new NullReferenceException("userId");

                var token = await flow.LoadTokenAsync(discordUserId, CancellationToken.None);
                if (token == null)
                    throw new NullReferenceException("token");

                string revokeToken = token.RefreshToken ?? token.AccessToken;
                await flow.RevokeTokenAsync(discordUserId, revokeToken, CancellationToken.None);

                Log.Info($"{discordUserId} 已解除 Google 憑證");
                await RemoveMemberCheckFromDbAsync(ulong.Parse(discordUserId));
            }
            catch (Exception ex)
            {
                await flow.DeleteTokenAsync(discordUserId, CancellationToken.None);
                Log.Error(ex.Demystify(), "RevokeToken");
                throw;
            }
        }

        public async Task RemoveMemberCheckFromDbAsync(ulong userId)
        {
            try
            {
                using var db = _dbService.GetDbContext();

                var youtubeMembers = db.YoutubeMemberCheck.Where((x) => x.UserId == userId).ToList();
                var youtubeMemberAccessToken = db.YoutubeMemberAccessToken.FirstOrDefault((x) => x.DiscordUserId == userId);

                // OAuth 資料清除與會限驗證資料解耦：只要任一存在就要清（有 token 無 member-check 的使用者 revoke 時也要刪 token）
                if (youtubeMembers.Count == 0 && youtubeMemberAccessToken == null)
                {
                    Log.Warn($"找不到該使用者的會限驗證或 OAuth 資料，忽略: {userId}");
                    return;
                }

                Log.Info($"移除此使用者的會限驗證與 OAuth 資料: {userId}");

                if (youtubeMembers.Count > 0)
                {
                    var guildIds = youtubeMembers.Select((x) => x.GuildId).Distinct().ToList();
                    var guildYoutubeMemberConfigs = db.GuildYoutubeMemberConfig.Where((x) => guildIds.Contains(x.GuildId));
                    foreach (var item in guildYoutubeMemberConfigs)
                    {
                        try { await _client.Rest.RemoveRoleAsync(item.GuildId, userId, item.MemberCheckGrantRoleId); }
                        catch { }
                    }
                    db.YoutubeMemberCheck.RemoveRange(youtubeMembers);
                }

                if (youtubeMemberAccessToken != null)
                    db.YoutubeMemberAccessToken.Remove(youtubeMemberAccessToken);

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "AfterRevokeUserCertAsync");
                throw;
            }
        }

        public async Task<string> GetYoutubeDataAsync(string discordUserId)
        {
            try
            {
                if (string.IsNullOrEmpty(discordUserId))
                    throw new NullReferenceException("userId");

                var token = await flow.LoadTokenAsync(discordUserId, CancellationToken.None);
                if (token == null)
                    throw new NullReferenceException("token");

                var userCert = await GetUserCredentialAsync(discordUserId, token);
                if (userCert == null)
                    throw new NullReferenceException("userCert");

                var service = new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = userCert,
                    ApplicationName = "Discord Youtube Member Check"
                }).Channels.List("id,snippet");
                service.Mine = true;

                try
                {
                    var result = await service.ExecuteAsync();
                    var channel = result.Items.FirstOrDefault();
                    if (channel == null)
                        throw new NullReferenceException("channel");

                    return Format.Url(channel.Snippet.Title, $"https://www.youtube.com/channel/{channel.Id}");
                }
                catch { throw; }

            }
            catch { throw; }
        }

        private async Task DisableSelectMenuAsync(SocketMessageComponent component, string locale, string placeholder = "")
        {
            SelectMenuBuilder selectMenuBuilder = new SelectMenuBuilder()
                .WithPlaceholder(string.IsNullOrEmpty(placeholder) ? _localizer.Get("Member.Select.Selected", locale) : placeholder)
                .WithMinValues(1)
                .WithMaxValues(1)
                .AddOption("1", "2")
                .WithCustomId("1234")
                .WithDisabled(true);

            var newComponent = new ComponentBuilder()
                .WithSelectMenu(selectMenuBuilder)
                .Build();

            try
            {
                await component.UpdateAsync((act) =>
                {
                    act.Components = new Optional<MessageComponent>(newComponent);
                });
            }
            catch
            {
                await component.ModifyOriginalResponseAsync((act) =>
                {
                    act.Components = new Optional<MessageComponent>(newComponent);
                });
            }
        }

        /// <summary>
        /// 消費匯流排的會限影片探索 log 事件（Scraper 探索 → 各 Notifier shard 發送）。
        /// bot owner DM 只在 shard 0 補送一次；log channel / guild owner 由 SendMsgToLogChannelAsync 依 shard 守衛處理。
        /// </summary>
        public async Task DispatchMemberVideoLogFromBusAsync(Shared.Messages.YoutubeMemberVideoLogNotification dto)
        {
            if (!string.IsNullOrEmpty(dto.BotOwnerMessage) && Bot.ShardId == 0 && Bot.ApplicatonOwner != null)
            {
                try { await Bot.ApplicatonOwner.SendMessageAsync(dto.BotOwnerMessage); } catch { }
            }

            await SendMsgToLogChannelAsync(dto);
        }

        /// <summary>
        /// （需 GuildMembers 特權 intent）會員重加入伺服器時，若 DB 仍有其 IsChecked 會限記錄則即時回補身分組。
        /// UserJoined 只在持有該 guild 的 shard 觸發 → 天然 shard-safe。憑既有記錄回補，不當場重打 YouTube API，
        /// 後續舊檢查會再校正（實際已失效者會被移除）。
        /// </summary>
        private async Task OnUserJoinedRestoreMemberRoleAsync(SocketGuildUser user)
        {
            try
            {
                using var db = _dbService.GetDbContext();
                var checks = await db.YoutubeMemberCheck.AsNoTracking()
                    .Where((x) => x.GuildId == user.Guild.Id && x.UserId == user.Id && x.IsChecked)
                    .ToListAsync();
                if (checks.Count == 0)
                    return;

                var configs = await db.GuildYoutubeMemberConfig.AsNoTracking()
                    .Where((x) => x.GuildId == user.Guild.Id).ToListAsync();

                foreach (var chk in checks)
                {
                    var cfg = configs.FirstOrDefault((c) => c.MemberCheckChannelId == chk.CheckYTChannelId);
                    if (cfg == null || cfg.MemberCheckGrantRoleId == 0)
                        continue;

                    try { await _client.Rest.AddRoleAsync(cfg.GuildId, user.Id, cfg.MemberCheckGrantRoleId); } catch { }
                }

                Log.Info($"會員重加入自動回補會限身分組: {user.Guild.Id} / {user.Id}");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "OnUserJoinedRestoreMemberRole");
            }
        }

        /// <summary>
        /// （需 GuildMembers 特權 intent）孤兒會限身分組回收：對各會限頻道的授予身分組成員做對帳，
        /// 移除「持有身分組但 DB 無 IsChecked 記錄」者（曾驗證失敗但身分組沒拿掉、且 DB 已被清）。只查 DB，不打 YouTube API。
        /// GetGuild != null 天然只處理本 shard 的 guild。
        /// </summary>
        private async Task ReconcileMemberRolesAsync()
        {
            try
            {
                using var db = _dbService.GetDbContext();
                var configs = await db.GuildYoutubeMemberConfig.AsNoTracking()
                    .Where((x) => x.MemberCheckGrantRoleId != 0).ToListAsync();

                foreach (var cfg in configs)
                {
                    var guild = _client.GetGuild(cfg.GuildId);
                    if (guild == null)
                        continue; // 非本 shard 持有 → 交給擁有的 shard

                    var role = guild.GetRole(cfg.MemberCheckGrantRoleId);
                    if (role == null)
                        continue;

                    try { await guild.DownloadUsersAsync(); } catch { } // 需 GuildMembers intent 才有完整名單

                    foreach (var user in role.Members.ToList())
                    {
                        bool hasRecord = await db.YoutubeMemberCheck.AsNoTracking().AnyAsync((x) =>
                            x.GuildId == cfg.GuildId && x.UserId == user.Id &&
                            x.CheckYTChannelId == cfg.MemberCheckChannelId && x.IsChecked);
                        if (hasRecord)
                            continue;

                        try
                        {
                            await _client.Rest.RemoveRoleAsync(cfg.GuildId, user.Id, cfg.MemberCheckGrantRoleId);
                            Log.Info($"孤兒會限身分組回收: {cfg.GuildId} / {user.Id}");
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "ReconcileMemberRoles");
            }
        }

        private async Task SendMsgToLogChannelAsync(Shared.Messages.YoutubeMemberVideoLogNotification dto)
        {
            using var db = _dbService.GetDbContext();

            foreach (var item in await db.GuildYoutubeMemberConfig.AsNoTracking()
                .Where(x => x.MemberCheckChannelId == dto.CheckChannelId).ToListAsync())
            {
                try
                {
                    bool isExistLogChannel = true;

                    var guild = _client.GetGuild(item.GuildId);
                    if (guild == null)
                    {
                        // 非本 shard 持有或尚未 Ready，靜默略過，別刪設定（避免多 shard 互刪）。
                        // 本方法會被 bus consumer 在每個 shard 呼叫，各 shard 只清自己持有的 guild。
                        if (!Bot.ShouldDeleteMissingGuild(item.GuildId))
                            continue;

                        Log.Warn($"SendMsgToLogChannelAsync: {item.GuildId} 不存在!");
                        db.GuildYoutubeMemberConfig.Remove(item);
                        continue;
                    }

                    string guildLocale = await _guildLocaleService.GetAsync(guild.Id, guild);
                    string message = YoutubeMemberVideoLogMessageFormatter.Format(
                        dto, guildLocale, _localizer, _commandDisplayResolver);
                    string setLogChannelPath = _commandDisplayResolver.GetCommandPath(guildLocale,
                        "member-set", "set-notice-member-status-channel");

                    var guildConfig = await db.GuildConfig.FirstOrDefaultAsync((x) => x.GuildId == item.GuildId);
                    if (guildConfig == null)
                    {
                        Log.Warn($"SendMsgToLogChannelAsync: {item.GuildId} 無 GuildConfig");
                        await db.GuildConfig.AddAsync(new GuildConfig { GuildId = guild.Id });
                        db.GuildYoutubeMemberConfig.Remove(item);

                        message += "\n" + _localizer.Format("Member.VideoLog.LogChannelMissing", guildLocale,
                            guild.Name, setLogChannelPath);
                        try { await guild.Owner.SendMessageAsync(embed: new EmbedBuilder().WithErrorColor().WithDescription(message).Build()); }
                        catch { }

                        continue;
                    }

                    var logChannel = guild.GetTextChannel(guildConfig.LogMemberStatusChannelId);
                    if (logChannel == null)
                    {
                        isExistLogChannel = false;
                        message += "\n" + _localizer.Format("Member.VideoLog.LogChannelMissing", guildLocale,
                            guild.Name, setLogChannelPath);
                    }
                    else
                    {
                        var permission = guild.GetUser(_client.CurrentUser.Id).GetPermissions(logChannel);
                        if (!permission.ViewChannel || !permission.SendMessages || !permission.EmbedLinks)
                        {
                            Log.Warn($"{item.GuildId} / {guildConfig.LogMemberStatusChannelId} 無權限可紀錄");
                            message += "\n" + _localizer.Format("Member.VideoLog.LogChannelPermissionMissing",
                                guildLocale, guild.Name, logChannel.Name);
                            isExistLogChannel = false;
                        }
                    }

                    var embed = new EmbedBuilder()
                        .WithErrorColor()
                        .WithDescription(message)
                        .Build();

                    if (dto.IsNeedSendToOwner)
                    {
                        try { await guild.Owner.SendMessageAsync(embed: embed); }
                        catch { }
                    }

                    if (isExistLogChannel)
                    {
                        try { await logChannel.SendMessageAsync(embed: embed); }
                        catch { }
                    }

                    if (dto.IsNeedRemove &&
                        YoutubeMemberManualPinPolicy.DecideAutomaticMutation(item.IsManualVideoId) ==
                        YoutubeMemberAutomaticMutationAction.Apply)
                    {
                        db.GuildYoutubeMemberConfig.Remove(item);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"SendMsgToLogChannelAsync: {ex}");
                }
            }

            await db.SaveChangesAsync();
        }

        private async Task<UserCredential> GetUserCredentialAsync(string discordUserId, TokenResponse token)
        {
            if (string.IsNullOrEmpty(token.RefreshToken))
                throw new NullReferenceException("RefreshToken 空白");

            var credential = new UserCredential(flow, discordUserId, token);

            try
            {
                if (token.IsStale)
                {
                    if (!await credential.RefreshTokenAsync(CancellationToken.None))
                    {
                        Log.Warn($"{discordUserId} AccessToken 無法刷新");
                        await flow.DataStore.DeleteAsync<TokenResponse>(discordUserId);
                        credential = null;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("token has been expired or revoked") ||
                    ex.Message.ToLower().Contains("invalid_grant"))
                {
                    Log.Warn($"{discordUserId} AccessToken 已取消授權");
                }
                else
                {
                    Log.Error(ex.Demystify(), $"{discordUserId} AccessToken 發生未知錯誤");
                }

                await flow.DataStore.DeleteAsync<TokenResponse>(discordUserId);
                credential = null;
            }

            return credential;
        }
    }

    static class Ext
    {
        // RestUser無法被序列化，暫時放棄Cache
        //private static async Task<RestUser> GetRestUserFromCatchOrCreate(ulong userId)
        //{
        //    try
        //    {
        //        var userJson = await Bot.RedisDb.StringGetAsync($"discord_stream_bot:restuser:{userId}");
        //        if (userJson.IsNull)
        //        {
        //            var user = await Bot._client.Rest.GetUserAsync(userId);
        //            if (user == null) return null;

        //            await Bot.RedisDb.StringSetAsync($"discord_stream_bot:restuser:{userId}", JsonConvert.SerializeObject(user), TimeSpan.FromHours(1));
        //            return user;
        //        }
        //        else
        //        {
        //            RestUser restUser = JsonConvert.DeserializeObject<RestUser>(userJson.ToString());
        //            return restUser;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error($"Member-GetRestUserFromCatchOrCreate: {userId}");
        //        Log.Error(ex.ToString());
        //        return null;
        //    }
        //}

        public static async Task<IUserMessage> SendConfirmMessageAsync(this ITextChannel tc, DiscordSocketClient client, ulong userId, EmbedBuilder embedBuilder)
        {
            try
            {
                embedBuilder.WithOkColor();

                var user = await client.Rest.GetUserAsync(userId);
                if (user != null)
                {
                    embedBuilder
                        .WithAuthor(user)
                        .WithThumbnailUrl(user.GetAvatarUrl());
                }

                return await Policy.Handle<TimeoutException>()
                    .Or<Discord.Net.HttpException>((httpEx) => ((int)httpEx.HttpCode).ToString().StartsWith("50"))
                    .WaitAndRetryAsync(3, (retryAttempt) =>
                    {
                        var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                        Log.Warn($"YoutubeMemberService-SendConfirmMessageAsync 通知 | {tc.Id} / {userId} 發送失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                        return timeSpan;
                    })
                    .ExecuteAsync(async () =>
                    {
                        return await tc.SendMessageAsync(embed: embedBuilder.Build(), options: new RequestOptions() { RetryMode = RetryMode.AlwaysRetry });
                    });
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"YoutubeMemberService-SendConfirmMessageAsync: {userId} ({tc.Name} / {tc.Id})");
                throw;
            }
        }

        public static async Task<IUserMessage> SendConfirmMessageAsync(this ITextChannel tc, string title, string dec)
        {
            try
            {
                return await Policy.Handle<TimeoutException>()
                    .Or<Discord.Net.HttpException>((httpEx) => ((int)httpEx.HttpCode).ToString().StartsWith("50"))
                    .WaitAndRetryAsync(3, (retryAttempt) =>
                    {
                        var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                        Log.Warn($"YoutubeMemberService-SendConfirmMessageAsync 通知 | {tc.Id} 發送失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                        return timeSpan;
                    })
                    .ExecuteAsync(async () =>
                    {
                        return await tc.SendMessageAsync(embed: new EmbedBuilder().WithOkColor().WithTitle(title).WithDescription(dec).Build(), options: new RequestOptions() { RetryMode = RetryMode.AlwaysRetry });
                    });
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"YoutubeMemberService-SendConfirmMessageAsync: {tc.Name} ({tc.Id})");
                return null;
            }
        }

        public static async Task<IUserMessage> SendErrorMessageAsync(this ITextChannel tc, DiscordSocketClient client,
            ulong userId, string channelTitle, string status, BotLocalizer localizer = null, string locale = null)
        {
            try
            {
                var embedBuilder = new EmbedBuilder()
                    .WithErrorColor()
                    .AddField(localizer?.Get("Member.Status.Channel", locale) ?? "檢查頻道", channelTitle)
                    .AddField(localizer?.Get("Member.Status.State", locale) ?? "狀態", status);

                var user = await client.Rest.GetUserAsync(userId);
                if (user != null)
                {
                    embedBuilder
                        .WithAuthor(user)
                        .WithThumbnailUrl(user.GetAvatarUrl());
                }

                return await Policy.Handle<TimeoutException>()
                    .Or<Discord.Net.HttpException>((httpEx) => ((int)httpEx.HttpCode).ToString().StartsWith("50"))
                    .WaitAndRetryAsync(3, (retryAttempt) =>
                    {
                        var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                        Log.Warn($"YoutubeMemberService-SendErrorMessageAsync 通知 | {tc.Id} / {userId} 發送失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                        return timeSpan;
                    })
                    .ExecuteAsync(async () =>
                    {
                        return await tc.SendMessageAsync(embed: embedBuilder.Build(), options: new RequestOptions() { RetryMode = RetryMode.AlwaysRetry });
                    });
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"YoutubeMemberService-SendErrorMessageAsync: {tc.Name} ({tc.Id})");
                return null;
            }
        }

        public static async Task SendConfirmMessageAsync(this ulong userId, DiscordSocketClient client, string text,
            ITextChannel tc, BotLocalizer localizer = null, string guildLocale = null)
        {
            var user = await client.Rest.GetUserAsync(userId) as IUser;
            if (user == null)
            {
                Log.Warn($"找不到使用者 {userId}");
                return;
            }

            var userChannel = await user.CreateDMChannelAsync();
            if (userChannel == null)
            {
                Log.Warn($"{user.Id} 無法建立使用者私訊");
                return;
            }

            try
            {
                await Policy.Handle<TimeoutException>()
                    .Or<Discord.Net.HttpException>((httpEx) => ((int)httpEx.HttpCode).ToString().StartsWith("50"))
                    .WaitAndRetryAsync(3, (retryAttempt) =>
                    {
                        var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                        Log.Warn($"YoutubeMemberService-SendUserDMConfirmMessageAsync 通知 | {userId} 發送失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                        return timeSpan;
                    })
                    .ExecuteAsync(async () =>
                    {
                        return await userChannel.SendMessageAsync(embed: new EmbedBuilder().WithOkColor().WithDescription(text).Build());
                    });
            }
            catch (Discord.Net.HttpException ex)
            {
                if (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
                {
                    Log.Warn($"無法傳送訊息至: {userChannel.Name} ({userId})");
                    string warning = localizer?.Format("Member.Status.DmUnavailable", guildLocale, userId)
                        ?? $"無法傳送訊息至: <@{userId}>\n請向該用戶提醒開啟 `允許來自伺服器成員的私人訊息`";
                    await tc.SendMessageAsync(warning);
                }
                else
                {
                    Log.Error(ex.Demystify(), $"YoutubeMemberService-SendUserDMConfirmMessageAsync - Discord 錯誤: {userId}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"YoutubeMemberService-SendUserDMConfirmMessageAsync 錯誤: {userId}");
            }
        }

        public static async Task SendErrorMessageAsync(this ulong userId, DiscordSocketClient client, string text,
            ITextChannel tc, BotLocalizer localizer = null, string guildLocale = null)
        {
            var user = await client.Rest.GetUserAsync(userId) as IUser;
            if (user == null)
            {
                Log.Warn($"找不到使用者 {userId}");
                return;
            }

            var userChannel = await user.CreateDMChannelAsync();
            if (userChannel == null)
            {
                Log.Warn($"{user.Id} 無法建立使用者私訊");
                return;
            }

            try
            {
                await Policy.Handle<TimeoutException>()
                    .Or<Discord.Net.HttpException>((httpEx) => ((int)httpEx.HttpCode).ToString().StartsWith("50"))
                    .WaitAndRetryAsync(3, (retryAttempt) =>
                    {
                        var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                        Log.Warn($"YoutubeMemberService-SendUserDMErrorMessageAsync 通知 | {userId} 發送失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                        return timeSpan;
                    })
                    .ExecuteAsync(async () =>
                    {
                        return await userChannel.SendMessageAsync(embed: new EmbedBuilder().WithErrorColor().WithDescription(text).Build());
                    });
            }
            catch (Discord.Net.HttpException ex)
            {
                if (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
                {
                    Log.Warn($"無法傳送訊息至: {userChannel.Name} ({userId})");
                    string warning = localizer?.Format("Member.Status.DmUnavailable", guildLocale, userId)
                        ?? $"無法傳送訊息至: <@{userId}>\n請向該用戶提醒開啟 `允許來自伺服器成員的私人訊息`";
                    await tc.SendMessageAsync(warning);
                }
                else
                {
                    Log.Error(ex.Demystify(), $"YoutubeMemberService-SendUserDMErrorMessageAsync - Discord 錯誤: {userId}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"YoutubeMemberService-SendUserDMErrorMessageAsync 錯誤: {userId}");
            }
        }

        public static async Task SendErrorMessageAsync(this IDMChannel dc, string text)
        {
            if (dc == null) return;

            try
            {
                await Policy.Handle<TimeoutException>()
                    .Or<Discord.Net.HttpException>((httpEx) => ((int)httpEx.HttpCode).ToString().StartsWith("50"))
                    .WaitAndRetryAsync(3, (retryAttempt) =>
                    {
                        var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                        Log.Warn($"YoutubeMemberService-SendUserDMErrorMessageAsync 通知 | {dc.Id} 發送失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                        return timeSpan;
                    })
                    .ExecuteAsync(async () =>
                    {
                        return await dc.SendMessageAsync(embed: new EmbedBuilder().WithErrorColor().WithDescription(text).Build());
                    });
            }
            catch (Discord.Net.HttpException ex)
            {
                if (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
                {
                    Log.Warn($"無法傳送訊息至: {dc.Name}");
                }
                else
                {
                    Log.Error(ex.Demystify(), $"YoutubeMemberService-SendUserDMErrorMessageAsync - Discord 錯誤: {dc.Name}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"YoutubeMemberService-SendUserDMErrorMessageAsync 錯誤: {dc.Name}");
            }
        }
    }
}
