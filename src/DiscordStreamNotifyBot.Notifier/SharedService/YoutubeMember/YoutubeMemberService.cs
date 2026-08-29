using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.Google;
using DiscordStreamNotifyBot.SharedService.Youtube;
using Newtonsoft.Json.Linq;
using Polly;

namespace DiscordStreamNotifyBot.SharedService.YoutubeMember
{
    public partial class YoutubeMemberService : IInteractionService
    {
        public bool IsEnable { get; private set; } = true;

        private readonly CancellationTokenSource _lifecycleCancellation;
        private readonly YoutubeMemberLifecycleTaskRegistry _eventTasks = new();
        private Task _oldCheckTask;
        private Task _newCheckTask;
        private Task _orphanCheckTask;
        private int _started;
        private int _stopped;
        private readonly YoutubeStreamService _streamService;
        private readonly DiscordSocketClient _client;
        private readonly BotConfig _botConfig;
        private readonly MainDbService _dbService;
        private readonly BotLocalizer _localizer;
        private readonly CommandDisplayResolver _commandDisplayResolver;
        private readonly GuildLocaleService _guildLocaleService;
        private readonly LocaleResolver _localeResolver;
        private readonly NotifierMetrics _metrics;
        private readonly YoutubeMemberRoleService _roleService;
        private readonly SharedService.Member.MemberOperationCoordinator _operationCoordinator;
        private readonly YoutubeMemberApiClient _apiClient;
        private readonly YoutubeMemberAuthorizationService _authorizationService;
        private readonly GoogleOAuthOperationLock _googleOperationLock;

        public YoutubeMemberService(YoutubeStreamService streamService, DiscordSocketClient discordSocketClient,
            BotConfig botConfig, MainDbService dbService, BotLocalizer localizer,
            CommandDisplayResolver commandDisplayResolver, GuildLocaleService guildLocaleService,
            LocaleResolver localeResolver, NotifierMetrics metrics,
            YoutubeMemberRoleService roleService,
            SharedService.Member.MemberOperationCoordinator operationCoordinator,
            YoutubeMemberApiClient apiClient,
            YoutubeMemberAuthorizationService authorizationService,
            GoogleOAuthOperationLock googleOperationLock)
        {
            _streamService = streamService;
            _client = discordSocketClient;
            _botConfig = botConfig;
            _dbService = dbService;
            _localizer = localizer;
            _commandDisplayResolver = commandDisplayResolver;
            _guildLocaleService = guildLocaleService;
            _localeResolver = localeResolver;
            _metrics = metrics;
            _roleService = roleService;
            _operationCoordinator = operationCoordinator;
            _apiClient = apiClient;
            _authorizationService = authorizationService;
            _googleOperationLock = googleOperationLock;
            _lifecycleCancellation = CancellationTokenSource.CreateLinkedTokenSource(GracefulShutdown.Token);

            if (!_authorizationService.IsConfigured)
            {
                Log.Warn($"{nameof(BotConfig.GoogleClientId)} 或 {nameof(BotConfig.GoogleClientSecret)} 空白，無法使用會限驗證系統");
                IsEnable = false;
            }

        }

        /// <summary>在 Discord/Redis 已就緒後才訂閱事件並啟動可 await 的會限背景工作。</summary>
        public void Start()
        {
            if (Interlocked.Exchange(ref _started, 1) != 0)
                return;

            CancellationToken cancellationToken = _lifecycleCancellation.Token;
            Bot.RedisSub.Subscribe(new RedisChannel("member.revokeToken", RedisChannel.PatternMode.Literal),
                (_, value) => TrackEventTask(() => HandleRevokeTokenAsync(value, cancellationToken)));
            _newCheckTask = PeriodicRunner.RunAsync("Youtube-member-new-check", TimeSpan.FromSeconds(15),
                TimeSpan.FromMinutes(5), () => CheckMemberShipCore(false, cancellationToken), cancellationToken);
            _oldCheckTask = PeriodicRunner.RunAsync("Youtube-member-old-check",
                YoutubeMemberLifecyclePolicy.NextOldCheckDelay(DateTime.Now), TimeSpan.FromDays(1),
                () => CheckMemberShipCore(true, cancellationToken), cancellationToken);

            if (YoutubeMemberLifecyclePolicy.ShouldManageGuildMemberSubscription(_botConfig.EnableGuildMembersIntent))
            {
                _client.UserJoined += OnUserJoinedRestoreMemberRoleAsync;
                _orphanCheckTask = PeriodicRunner.RunAsync("Youtube-member-orphan-role", TimeSpan.FromMinutes(5),
                    TimeSpan.FromDays(1), () => ReconcileMemberRolesAsync(cancellationToken), cancellationToken);
            }
        }

        /// <summary>先解除事件來源，再取消並等待所有週期與既有事件工作結束。</summary>
        public async Task StopAsync()
        {
            if (Volatile.Read(ref _started) == 0 || Interlocked.Exchange(ref _stopped, 1) != 0)
                return;

            if (YoutubeMemberLifecyclePolicy.ShouldManageGuildMemberSubscription(_botConfig.EnableGuildMembersIntent))
                _client.UserJoined -= OnUserJoinedRestoreMemberRoleAsync;
            try
            {
                Bot.RedisSub.Unsubscribe(new RedisChannel("member.revokeToken", RedisChannel.PatternMode.Literal));
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException or InvalidOperationException)
            {
                Log.Warn($"關閉 YouTube 解除授權事件訂閱時 Redis 暫時失敗: {ex.GetType().Name}");
            }

            Task[] eventTasks = _eventTasks.StopAndSnapshot();
            await _lifecycleCancellation.CancelAsync();
            try
            {
                Task[] tasks = new[] { _newCheckTask, _oldCheckTask, _orphanCheckTask }
                    .Where(task => task != null).Concat(eventTasks).ToArray();
                await YoutubeMemberLifecyclePolicy.DrainAsync(tasks);
            }
            catch (OperationCanceledException) when (_lifecycleCancellation.IsCancellationRequested)
            {
            }
            _lifecycleCancellation.Dispose();
        }

        private Task TrackEventTask(Func<Task> action)
        {
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!_eventTasks.TryRegister(completion.Task, out long taskId))
                return Task.CompletedTask;

            _ = RunTrackedEventAsync(taskId, action, completion);
            return completion.Task;
        }

        private async Task RunTrackedEventAsync(
            long taskId,
            Func<Task> action,
            TaskCompletionSource completion)
        {
            try { await action(); }
            catch (OperationCanceledException) when (_lifecycleCancellation.IsCancellationRequested) { }
            catch (Exception ex) { Log.Error(ex.Demystify(), "處理 YouTube 會員生命週期事件失敗"); }
            finally
            {
                _eventTasks.Complete(taskId);
                completion.TrySetResult();
            }
        }

        private async Task HandleRevokeTokenAsync(RedisValue value, CancellationToken cancellationToken)
        {
            // Redis 僅作 wake-up hint；週期清理的 MySQL pending state 才是 durable truth。
            if (Bot.ShardId != 0 || !ulong.TryParse(value.ToString(), out ulong userId))
                return;
            Log.Info($"收到 Redis 的 Revoke 請求：{userId}");
            // Backend 已完成 revoke；延遲或重複 hint 絕不可重新把 active check 轉成 pending，
            // 更不能碰可能已重新綁定的 token。它只喚醒既有 durable cleanup。
            await _roleService.RetryPendingCleanupForUserAsync(userId, cancellationToken);
        }

        public async Task<bool> IsExistUserTokenAsync(string discordUserId)
        {
            return await _authorizationService.IsExistUserTokenAsync(discordUserId);
        }

        public async Task RevokeUserGoogleCertAsync(string discordUserId = "")
        {
            try
            {
                if (string.IsNullOrEmpty(discordUserId))
                    throw new NullReferenceException("userId");

                ulong userId = ulong.Parse(discordUserId);
                GoogleOAuthOperationLockAcquireResult lockResult = await _googleOperationLock.TryAcquireAsync(
                    userId, CancellationToken.None);
                if (lockResult.Status != GoogleOAuthOperationLockAcquireStatus.Acquired)
                    throw new InvalidOperationException($"無法取得 Google OAuth 跨程序 lease: {lockResult.Status}");
                await using var operationLease = lockResult.Lease;

                YoutubeMemberTokenSnapshot? snapshot = await _authorizationService.GetTokenSnapshotAsync(
                    discordUserId, CancellationToken.None);
                if (snapshot == null || !await PrepareMemberCheckCleanupAsync(
                        userId, snapshot.Value.EncryptedTokenPayload, CancellationToken.None,
                        null, null))
                {
                    throw new InvalidOperationException("Google 憑證已被新的綁定取代，取消本次解除授權。");
                }

                // provider 結果不明時 RevokeAsync 會丟出並保留本機 token；pending intent 留下供安全重試。
                if (await operationLease.EnsureOwnedAsync(CancellationToken.None) !=
                    GoogleOAuthOperationLockOwnershipStatus.Owned)
                {
                    throw new InvalidOperationException("Google OAuth 跨程序 lease 已失效，取消 provider revoke。");
                }
                await _authorizationService.RevokeAsync(snapshot.Value, CancellationToken.None);
                if (await operationLease.EnsureOwnedAsync(CancellationToken.None) !=
                    GoogleOAuthOperationLockOwnershipStatus.Owned)
                {
                    throw new InvalidOperationException("Google OAuth 跨程序 lease 已失效，保留 durable unlink intent。");
                }
                if (!await CompleteMemberCheckCleanupAsync(
                        userId, snapshot.Value.EncryptedTokenPayload, CancellationToken.None))
                {
                    throw new InvalidOperationException("Google 憑證已被新的綁定取代，保留新的憑證。");
                }
                Log.Info($"{discordUserId} 已解除 Google 憑證");
            }
            catch (Exception ex)
            {
                // provider 例外可能攜帶 OAuth response body，僅記錄型別避免意外輸出憑證資料。
                Log.Error(YoutubeMemberSafeLogging.DescribeFailure("RevokeToken", ex));
                throw;
            }
        }

        /// <summary>
        /// 僅在 provider 已明確回傳 authorization invalid 時呼叫。expectedEncryptedToken 是 provider
        /// 呼叫前讀到的原始密文，避免延遲結果刪掉其後重新綁定的憑證或 entitlement。
        /// </summary>
        private async Task<bool> RemoveMemberCheckFromDbAsync(ulong userId, string expectedEncryptedToken,
            YoutubeMemberProbeConfigurationSnapshot configurationSnapshot,
            YoutubeMemberCheckStateSnapshot checkSnapshot,
            int checkId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                GoogleOAuthOperationLockAcquireResult lockResult = await _googleOperationLock.TryAcquireAsync(
                    userId, cancellationToken);
                if (lockResult.Status != GoogleOAuthOperationLockAcquireStatus.Acquired)
                    return false;
                await using var operationLease = lockResult.Lease;
                if (await operationLease.EnsureOwnedAsync(cancellationToken) !=
                    GoogleOAuthOperationLockOwnershipStatus.Owned)
                {
                    return false;
                }
                if (!await PrepareMemberCheckCleanupAsync(userId, expectedEncryptedToken, cancellationToken,
                        configurationSnapshot, (checkId, checkSnapshot)) ||
                    await operationLease.EnsureOwnedAsync(cancellationToken) !=
                        GoogleOAuthOperationLockOwnershipStatus.Owned ||
                    !await CompleteMemberCheckCleanupAsync(userId, expectedEncryptedToken, cancellationToken))
                    return false;
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "AfterRevokeUserCertAsync");
                throw;
            }
        }

        /// <summary>在任何 provider revoke 或本機 token 刪除之前，先保存每筆角色移除 intent。</summary>
        private async Task<bool> PrepareMemberCheckCleanupAsync(ulong userId, string expectedEncryptedToken,
            CancellationToken cancellationToken,
            YoutubeMemberProbeConfigurationSnapshot? providerConfigurationSnapshot,
            (int CheckId, YoutubeMemberCheckStateSnapshot Snapshot)? providerCheckSnapshot)
        {
            await using var userLock = await _operationCoordinator.LockUserAsync(userId, cancellationToken);
            ulong[] expectedGuildIds = await GetUserCheckGuildIdsAsync(userId, cancellationToken);
            await using var guildLocks = await _operationCoordinator.LockGuildsAsync(expectedGuildIds, cancellationToken);
            Log.Info($"標記此使用者的會限驗證待清理: {userId}");
            using var intentDb = _dbService.GetDbContext();
            await using var transaction = await intentDb.Database.BeginTransactionAsync(cancellationToken);
            YoutubeMemberAccessToken token = await LockTokenAsync(intentDb, userId, cancellationToken);
            if (!YoutubeMemberPolicies.IsCurrentTokenPayload(expectedEncryptedToken, token?.EncryptedAccessToken))
            {
                Log.Warn($"YouTube OAuth token 已更新，停止過期 cleanup: {userId}");
                return false;
            }
            List<YoutubeMemberCheck> checks = await LockUserChecksAsync(intentDb, userId, cancellationToken);
            ulong[] actualGuildIds = checks.Select(x => x.GuildId).Distinct().Order().ToArray();
            if (!actualGuildIds.SequenceEqual(expectedGuildIds))
            {
                Log.Warn($"YouTube cleanup 期間的 guild 清單已變更，保留供下次重試: {userId}");
                return false;
            }
            if (providerConfigurationSnapshot.HasValue || providerCheckSnapshot.HasValue)
            {
                if (!providerConfigurationSnapshot.HasValue || !providerCheckSnapshot.HasValue)
                    return false;
                YoutubeMemberCheck providerCheck = checks.SingleOrDefault(x => x.Id == providerCheckSnapshot.Value.CheckId);
                GuildYoutubeMemberConfig providerConfiguration = await LockConfigurationAsync(intentDb,
                    providerConfigurationSnapshot.Value.Id, cancellationToken);
                if (!YoutubeMemberPolicies.CanApplyProviderResult(YoutubeMemberProbeResultKind.AuthorizationInvalid,
                        expectedEncryptedToken, token.EncryptedAccessToken, providerCheckSnapshot.Value.Snapshot,
                        providerCheck, providerConfigurationSnapshot.Value, providerConfiguration))
                {
                    Log.Warn($"YouTube OAuth token、check 或探測設定已更新，忽略過期 authorization invalid 結果: {userId}");
                    return false;
                }
            }
            GoogleOAuthUnlinkIntent intent = await intentDb.GoogleOAuthUnlinkIntent
                .SingleOrDefaultAsync(x => x.DiscordUserId == userId, cancellationToken);
            if (intent == null)
            {
                intentDb.GoogleOAuthUnlinkIntent.Add(new GoogleOAuthUnlinkIntent
                {
                    DiscordUserId = userId,
                    ExpectedEncryptedToken = expectedEncryptedToken,
                    DateAdded = DateTime.UtcNow
                });
            }
            else
            {
                intent.ExpectedEncryptedToken = expectedEncryptedToken;
                intent.DateAdded = DateTime.UtcNow;
            }
            await LockGuildConfigurationsAsync(intentDb, actualGuildIds, cancellationToken);
            foreach (YoutubeMemberCheck check in checks)
                YoutubeMemberPolicies.QueueRoleRemoval(check);
            await intentDb.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        /// <summary>只刪除仍等於 provider call 前 payload 的 token，然後嘗試既有 pending role cleanup。</summary>
        private async Task<bool> CompleteMemberCheckCleanupAsync(ulong userId, string expectedEncryptedToken,
            CancellationToken cancellationToken)
        {
            await using var userLock = await _operationCoordinator.LockUserAsync(userId, cancellationToken);
            ulong[] expectedGuildIds = await GetUserCheckGuildIdsAsync(userId, cancellationToken);
            await using var guildLocks = await _operationCoordinator.LockGuildsAsync(expectedGuildIds, cancellationToken);
            using (var tokenDb = _dbService.GetDbContext())
            {
                await using var transaction = await tokenDb.Database.BeginTransactionAsync(cancellationToken);
                YoutubeMemberAccessToken token = await LockTokenAsync(tokenDb, userId, cancellationToken);
                if (token != null && !YoutubeMemberPolicies.IsCurrentTokenPayload(expectedEncryptedToken, token.EncryptedAccessToken))
                {
                    Log.Warn($"YouTube OAuth token 已更新，保留新的憑證: {userId}");
                    return false;
                }
                List<YoutubeMemberCheck> currentChecks = await LockUserChecksAsync(tokenDb, userId, cancellationToken);
                ulong[] actualGuildIds = currentChecks.Select(x => x.GuildId).Distinct().Order().ToArray();
                if (!actualGuildIds.SequenceEqual(expectedGuildIds))
                {
                    Log.Warn($"YouTube cleanup 完成前的 guild 清單已變更，保留 token: {userId}");
                    return false;
                }
                await LockGuildConfigurationsAsync(tokenDb, actualGuildIds, cancellationToken);
                if (token != null && !YoutubeMemberPolicies.CanDeleteLocalTokenAfterCleanupIntent(currentChecks))
                {
                    Log.Warn($"YouTube OAuth 清理 intent 尚未完整，保留 token: {userId}");
                    return false;
                }
                if (token != null && await tokenDb.Database.ExecuteSqlInterpolatedAsync($"""
                    DELETE FROM `youtube_member_access_token`
                    WHERE `discord_user_id` = {userId}
                      AND BINARY `encrypted_access_token` = BINARY {expectedEncryptedToken}
                    """) != 1)
                {
                    Log.Warn($"YouTube OAuth token 已更新，保留新的憑證: {userId}");
                    return false;
                }
                await tokenDb.GoogleOAuthUnlinkIntent
                    .Where(x => x.DiscordUserId == userId)
                    .ExecuteDeleteAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }

            foreach (ulong guildId in expectedGuildIds)
            {
                await CleanupPendingUserChecksInGuildAsync(userId, guildId, cancellationToken);
            }
            return true;
        }

        private async Task CleanupPendingUserChecksInGuildAsync(ulong userId, ulong guildId, CancellationToken cancellationToken)
        {
            using var db = _dbService.GetDbContext();
            var checks = await db.YoutubeMemberCheck.Where(x => x.UserId == userId && x.GuildId == guildId &&
                    x.PendingRoleRemoval)
                .ToListAsync(cancellationToken);
            if (checks.Count == 0)
                return;
            var configs = await db.GuildYoutubeMemberConfig.AsNoTracking().Where(x => x.GuildId == guildId)
                .ToDictionaryAsync(x => x.MemberCheckChannelId, cancellationToken);
            foreach (YoutubeMemberCheck check in checks)
            {
                if (!configs.TryGetValue(check.CheckYTChannelId, out GuildYoutubeMemberConfig config))
                {
                    // 沒有設定時無法知道要移除哪個 role；保留 evidence 等管理員修復或設定刪除流程處理。
                    Log.Warn($"YouTube pending cleanup 找不到設定，保留待清理列: {guildId} / {check.Id}");
                    continue;
                }
                if (await _roleService.RemoveAsync(config, userId, cancellationToken))
                    db.YoutubeMemberCheck.Remove(check);
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task<string> GetYoutubeDataAsync(string discordUserId)
        {
            try
            {
                if (string.IsNullOrEmpty(discordUserId))
                    throw new NullReferenceException("userId");

                return await _authorizationService.GetLinkedChannelAsync(discordUserId, CancellationToken.None);

            }
            catch { throw; }
        }

        public async Task<AdminSettingsMutationResult> ConfigureAsync(
            SocketGuild guild,
            ulong actorUserId,
            string source,
            ulong roleId,
            CancellationToken cancellationToken)
        {
            if (!IsEnable)
                return AdminSettingsMutationResult.Rejected("verification.platform-disabled");
            SocketRole role = guild.GetRole(roleId);
            if (role == null)
                return AdminSettingsMutationResult.Rejected("verification.role-invalid");
            if (actorUserId != Bot.ApplicatonOwner.Id && guild.MemberCount < 250 && !Utility.OfficialGuildContains(guild.Id))
                return AdminSettingsMutationResult.Rejected("verification.guild-member-requirement", new JObject
                {
                    ["requiredMemberCount"] = 250,
                    ["memberCount"] = guild.MemberCount
                });

            try
            {
                using var db = _dbService.GetDbContext();
                var guildConfig = await db.GuildConfig.SingleOrDefaultAsync(
                    x => x.GuildId == guild.Id, cancellationToken);
                if (guildConfig?.VerificationLogChannelId is not > 0)
                    return AdminSettingsMutationResult.Rejected("verification.log-channel-required");
                if (guild.GetTextChannel(guildConfig.VerificationLogChannelId) == null)
                    return AdminSettingsMutationResult.Rejected("verification.log-channel-missing");
                int limit = guildConfig.MaxYouTubeMemberCheckCount > 0
                    ? (int)guildConfig.MaxYouTubeMemberCheckCount
                    : 5;
                string sourceId = await _streamService.GetChannelIdAsync(source);
                bool exists = await db.GuildYoutubeMemberConfig.AsNoTracking().AnyAsync(
                    x => x.GuildId == guild.Id && x.MemberCheckChannelId == sourceId, cancellationToken);
                if (!exists && !Utility.OfficialGuildContains(guild.Id) &&
                    await db.GuildYoutubeMemberConfig.AsNoTracking().CountAsync(x => x.GuildId == guild.Id, cancellationToken) >= limit)
                    return AdminSettingsMutationResult.Rejected("verification.limit-reached", new JObject { ["limit"] = limit });

                YoutubeMemberRoleConfigurationResult result = await _roleService.ConfigureRoleAsync(
                    guild, sourceId, role, cancellationToken);
                if (!exists && result.IsSuccess)
                {
                    try
                    {
                        SocketGuildUser actor = guild.GetUser(actorUserId);
                        string actorText = actor == null
                            ? actorUserId.ToString()
                            : $"{actor.GlobalName ?? actor.Username} ({actor} / {actorUserId})";
                        await Bot.ApplicatonOwner.SendMessageAsync(embed: new EmbedBuilder()
                            .WithOkColor()
                            .WithTitle("已新增會限驗證頻道")
                            .AddField("頻道", Format.Url(sourceId, $"https://www.youtube.com/channel/{sourceId}"), false)
                            .AddField("伺服器", $"{guild.Name} ({guild.Id})", false)
                            .AddField("執行者", actorText, false)
                            .Build());
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Demystify(), "發送 YouTube 會限驗證新增通知給 Bot 擁有者時失敗");
                    }
                }
                return MapRoleResult(result.Error, sourceId);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex) when (ex is FormatException or ArgumentException or UriFormatException)
            {
                return AdminSettingsMutationResult.Rejected("verification.source-not-found");
            }
        }

        public async Task<AdminSettingsMutationResult> RemoveConfigurationAsync(
            ulong guildId,
            string sourceId,
            CancellationToken cancellationToken)
        {
            using var db = _dbService.GetDbContext();
            var config = await db.GuildYoutubeMemberConfig.AsNoTracking().SingleOrDefaultAsync(
                x => x.GuildId == guildId && x.MemberCheckChannelId == sourceId, cancellationToken);
            if (config == null)
                return AdminSettingsMutationResult.Rejected("verification.not-configured");
            bool marked = await _roleService.MarkConfigurationDeletionPendingAsync(config, cancellationToken);
            return marked
                ? AdminSettingsMutationResult.Pending("verification.cleanup-pending")
                : AdminSettingsMutationResult.Rejected("verification.not-configured");
        }

        public async Task<AdminSettingsMutationResult> SetProbeVideoAsync(
            ulong guildId,
            string sourceId,
            string video,
            CancellationToken cancellationToken)
        {
            string videoId;
            try { videoId = _streamService.GetVideoId(video); }
            catch (Exception ex) when (ex is ArgumentException or UriFormatException)
            {
                return AdminSettingsMutationResult.Rejected("verification.probe-video-invalid");
            }
            if (string.IsNullOrWhiteSpace(videoId))
                return AdminSettingsMutationResult.Rejected("verification.probe-video-invalid");

            try
            {
                var request = _streamService.YouTubeService.CommentThreads.List("id");
                request.VideoId = videoId;
                await request.ExecuteAsync(cancellationToken);
                return AdminSettingsMutationResult.Rejected("verification.probe-video-invalid");
            }
            catch (global::Google.GoogleApiException ex) when (YoutubeMemberApiClient.IsDocumentedMembershipForbidden(ex)) { }
            catch (global::Google.GoogleApiException)
            {
                return AdminSettingsMutationResult.Rejected("verification.probe-video-invalid");
            }

            await using var guildLock = await _operationCoordinator.LockGuildAsync(guildId, cancellationToken);
            using var db = _dbService.GetDbContext();
            var config = await db.GuildYoutubeMemberConfig.SingleOrDefaultAsync(
                x => x.GuildId == guildId && x.MemberCheckChannelId == sourceId, cancellationToken);
            if (config == null)
                return AdminSettingsMutationResult.Rejected("verification.not-configured");
            if (config.DeletionPending)
                return AdminSettingsMutationResult.Rejected("verification.deletion-pending");
            config.MemberCheckVideoId = videoId;
            config.IsManualVideoId = true;
            await db.SaveChangesAsync(cancellationToken);
            return AdminSettingsMutationResult.Applied("verification.probe-video-set", new JObject
            {
                ["videoId"] = videoId
            });
        }

        public async Task<AdminSettingsMutationResult> UseAutomaticProbeAsync(
            ulong guildId,
            string sourceId,
            CancellationToken cancellationToken)
        {
            await using var guildLock = await _operationCoordinator.LockGuildAsync(guildId, cancellationToken);
            using var db = _dbService.GetDbContext();
            var config = await db.GuildYoutubeMemberConfig.SingleOrDefaultAsync(
                x => x.GuildId == guildId && x.MemberCheckChannelId == sourceId, cancellationToken);
            if (config == null)
                return AdminSettingsMutationResult.Rejected("verification.not-configured");
            if (config.DeletionPending)
                return AdminSettingsMutationResult.Rejected("verification.deletion-pending");
            config.MemberCheckVideoId = "-";
            config.IsManualVideoId = false;
            await db.SaveChangesAsync(cancellationToken);
            return AdminSettingsMutationResult.Applied("verification.probe-automatic");
        }

        private static AdminSettingsMutationResult MapRoleResult(string error, string sourceId)
            => error switch
            {
                null => AdminSettingsMutationResult.Applied("verification.configured", new JObject { ["sourceId"] = sourceId }),
                "MemberSetting.Errors.ManageRolesRequired" => AdminSettingsMutationResult.Rejected("verification.manage-roles-required"),
                "MemberSetting.Errors.RoleTooHigh" => AdminSettingsMutationResult.Rejected("verification.role-too-high"),
                "MemberSetting.Errors.CrossPlatformRoleCollision" => AdminSettingsMutationResult.Rejected("verification.role-collision"),
                "MemberSetting.Errors.RepairPending" => AdminSettingsMutationResult.Pending("verification.cleanup-pending"),
                "MemberSetting.Errors.ConfigurationDeletionPending" => AdminSettingsMutationResult.Rejected("verification.deletion-pending"),
                _ => AdminSettingsMutationResult.Rejected("verification.role-invalid")
            };

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
        private Task OnUserJoinedRestoreMemberRoleAsync(SocketGuildUser user)
            => TrackEventTask(() => RestoreMemberRoleOnUserJoinedCoreAsync(user));

        private async Task RestoreMemberRoleOnUserJoinedCoreAsync(SocketGuildUser user)
        {
            try
            {
                await using var userLock = await _operationCoordinator.LockUserAsync(user.Id, _lifecycleCancellation.Token);
                await using var guildLock = await _operationCoordinator.LockGuildAsync(user.Guild.Id, _lifecycleCancellation.Token);
                using var db = _dbService.GetDbContext();
                var checks = await db.YoutubeMemberCheck.AsNoTracking()
                    .Where((x) => x.GuildId == user.Guild.Id && x.UserId == user.Id && x.IsChecked && !x.PendingRoleRemoval)
                    .ToListAsync();
                if (checks.Count == 0)
                    return;

                var configs = await db.GuildYoutubeMemberConfig.AsNoTracking()
                    .Where((x) => x.GuildId == user.Guild.Id && !x.DeletionPending).ToListAsync();

                foreach (var chk in checks)
                {
                    var cfg = configs.FirstOrDefault((c) => c.MemberCheckChannelId == chk.CheckYTChannelId);
                    if (cfg == null || cfg.MemberCheckGrantRoleId == 0)
                        continue;

                    await _roleService.GrantAsync(cfg, user.Id, _lifecycleCancellation.Token);
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
        private async Task ReconcileMemberRolesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var db = _dbService.GetDbContext();
                ulong[] candidateGuildIds = await db.GuildYoutubeMemberConfig.AsNoTracking()
                    .Where(x => x.MemberCheckGrantRoleId != 0 && !x.DeletionPending)
                    .Select(x => x.GuildId).Distinct().ToArrayAsync(cancellationToken);

                foreach (ulong guildId in candidateGuildIds)
                {
                    var guild = _client.GetGuild(guildId);
                    if (guild == null)
                        continue; // 非本 shard 持有，交由持有該 guild 的 shard 處理。

                    try { await guild.DownloadUsersAsync(); } catch { } // 需 GuildMembers intent 才有完整名單

                    await using var guildLock = await _operationCoordinator.LockGuildAsync(guild.Id, cancellationToken);
                    // config 可能在 DownloadUsersAsync 期間由管理員更新或刪除；鎖後重讀才是對帳真相。
                    using var currentDb = _dbService.GetDbContext();
                    var configs = await currentDb.GuildYoutubeMemberConfig.AsNoTracking()
                        .Where(x => x.GuildId == guild.Id && x.MemberCheckGrantRoleId != 0 && !x.DeletionPending)
                        .ToArrayAsync(cancellationToken);
                    DiscordStreamNotifyBot.SharedService.Member.MemberRoleOwnershipSnapshot ownership = await _roleService.LoadOwnershipSnapshotAsync(
                        guild.Id, cancellationToken);
                    foreach (var roleGroup in configs.GroupBy(x => x.MemberCheckGrantRoleId))
                    {
                        SocketRole role = guild.GetRole(roleGroup.Key);
                        if (role == null)
                            continue;
                        foreach (SocketGuildUser user in role.Members.ToList())
                        {
                            if (ownership.HasOtherActiveEntitlement(user.Id, role.Id))
                                continue;
                            if (await _roleService.RemoveOrphanAsync(
                                    guild, user.Id, role.Id, ownership, cancellationToken))
                                Log.Info($"孤兒會限身分組回收: {guild.Id} / {user.Id}");
                        }
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

                        Log.Warn($"SendMsgToLogChannelAsync：{item.GuildId} 不存在。");
                        continue;
                    }

                    string guildLocale = await _guildLocaleService.GetAsync(guild.Id, guild);
                    string message = YoutubeMemberVideoLogMessageFormatter.Format(
                        dto, guildLocale, _localizer, _commandDisplayResolver);
                    string setLogChannelPath = _commandDisplayResolver.GetCommandPath(guildLocale,
                        "server-admin", "set-verification-log-channel");

                    var guildConfig = await db.GuildConfig.FirstOrDefaultAsync((x) => x.GuildId == item.GuildId);
                    if (guildConfig == null)
                    {
                        Log.Warn($"SendMsgToLogChannelAsync: {item.GuildId} 無 GuildConfig");
                        await db.GuildConfig.AddAsync(new GuildConfig { GuildId = guild.Id });

                        message += "\n" + _localizer.Format("Member.VideoLog.LogChannelMissing", guildLocale,
                            guild.Name, setLogChannelPath);
                        try { await guild.Owner.SendMessageAsync(embed: new EmbedBuilder().WithErrorColor().WithDescription(message).Build()); }
                        catch { }

                        continue;
                    }

                    var logChannel = guild.GetTextChannel(guildConfig.VerificationLogChannelId);
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
                            Log.Warn($"{item.GuildId} / {guildConfig.VerificationLogChannelId} 無權限可紀錄");
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
                        if (!await _roleService.MarkConfigurationDeletionPendingAsync(item, GracefulShutdown.Token))
                            Log.Warn($"YouTube 會限設定刪除標記失敗: {item.GuildId} / {item.MemberCheckChannelId}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"SendMsgToLogChannelAsync: {ex}");
                }
            }

            await db.SaveChangesAsync();
        }

    }

    static class Ext
    {
        // RestUser 無法序列化，暫不使用快取。
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
                    .Or<KeyNotFoundException>()
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
                    .Or<KeyNotFoundException>()
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
                    .Or<KeyNotFoundException>()
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
                        ?? $"無法傳送訊息至：<@{userId}>\n請提醒該使用者開啟 `允許來自伺服器成員的私人訊息`";
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
                        ?? $"無法傳送訊息至：<@{userId}>\n請提醒該使用者開啟 `允許來自伺服器成員的私人訊息`";
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
