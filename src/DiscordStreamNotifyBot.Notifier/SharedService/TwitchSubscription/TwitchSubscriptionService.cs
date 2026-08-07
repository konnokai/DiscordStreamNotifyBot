using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService.Member;
using DiscordStreamNotifyBot.SharedService.Twitch;
using System.Collections.Concurrent;

namespace DiscordStreamNotifyBot.SharedService.TwitchSubscription
{
    public enum TwitchSubscriptionCancellationStatus
    {
        NotFound,
        Completed,
        RetryPending
    }

    public sealed class TwitchSubscriptionService
    {
        private readonly MainDbService _dbService;
        private readonly DiscordSocketClient _client;
        private readonly BotConfig _botConfig;
        private readonly TwitchApiService _twitchApiService;
        private readonly TwitchAuthorizationTokenService _tokenService;
        private readonly TwitchSubscriptionApiClient _apiClient;
        private readonly TwitchSubscriptionRoleService _roleService;
        private readonly MemberOperationCoordinator _operationCoordinator;
        private readonly NotifierMetrics _metrics;
        private readonly BotLocalizer _localizer;
        private readonly GuildLocaleService _guildLocaleService;
        private readonly CancellationTokenSource _lifecycleCancellation;
        private readonly ConcurrentDictionary<string, DateTimeOffset> _rateLimitUntilByTwitchUserId = new(StringComparer.Ordinal);
        private Task _reverificationTask;
        private Task _orphanReconciliationTask;
        private int _started;
        private int _stopped;

        public TwitchSubscriptionService(
            MainDbService dbService,
            DiscordSocketClient client,
            BotConfig botConfig,
            TwitchApiService twitchApiService,
            TwitchAuthorizationTokenService tokenService,
            TwitchSubscriptionApiClient apiClient,
            TwitchSubscriptionRoleService roleService,
            MemberOperationCoordinator operationCoordinator,
            NotifierMetrics metrics,
            BotLocalizer localizer,
            GuildLocaleService guildLocaleService)
        {
            _dbService = dbService;
            _client = client;
            _botConfig = botConfig;
            _twitchApiService = twitchApiService;
            _tokenService = tokenService;
            _apiClient = apiClient;
            _roleService = roleService;
            _operationCoordinator = operationCoordinator;
            _metrics = metrics;
            _localizer = localizer;
            _guildLocaleService = guildLocaleService;
            _lifecycleCancellation = CancellationTokenSource.CreateLinkedTokenSource(GracefulShutdown.Token);
        }

        /// <summary>訂閱授權狀態事件，並啟動每小時複驗及選用的孤兒角色對帳。</summary>
        public void Start()
        {
            if (Interlocked.Exchange(ref _started, 1) != 0)
                return;

            CancellationToken cancellationToken = _lifecycleCancellation.Token;
            Bot.RedisSub.Subscribe(
                new RedisChannel(RedisChannels.Twitch.AuthorizationChanged, RedisChannel.PatternMode.Literal),
                (channel, value) => _ = HandleAuthorizationChangedAsync(value, cancellationToken));

            if (_botConfig.EnableGuildMembersIntent)
            {
                _client.UserJoined += RestoreOnUserJoinedAsync;
                _orphanReconciliationTask = PeriodicRunner.RunAsync(
                    "Twitch-subscription-orphan-role",
                    TimeSpan.FromMinutes(5),
                    TimeSpan.FromDays(1),
                    () => ReconcileOrphanRolesAsync(cancellationToken),
                    cancellationToken);
            }

            _reverificationTask = PeriodicRunner.RunAsync(
                "Twitch-subscription-reverification",
                TimeSpan.FromMinutes(1),
                TimeSpan.FromHours(1),
                () => RunReverificationCycleAsync(cancellationToken),
                cancellationToken);
        }

        /// <summary>取消背景排程、解除 Discord/Redis 事件，並等待 token rotation 安全 drain。</summary>
        public async Task StopAsync()
        {
            if (Volatile.Read(ref _started) == 0 || Interlocked.Exchange(ref _stopped, 1) != 0)
                return;

            if (_botConfig.EnableGuildMembersIntent)
                _client.UserJoined -= RestoreOnUserJoinedAsync;
            await _lifecycleCancellation.CancelAsync();
            try
            {
                Bot.RedisSub.Unsubscribe(
                    new RedisChannel(RedisChannels.Twitch.AuthorizationChanged, RedisChannel.PatternMode.Literal));
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException or InvalidOperationException)
            {
                Log.Warn($"關閉 Twitch 授權事件訂閱時 Redis 暫時失敗: {ex.GetType().Name}");
            }

            Task[] tasks = new[] { _reverificationTask, _orphanReconciliationTask }
                .Where(x => x != null)
                .ToArray();
            if (tasks.Length > 0)
            {
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (OperationCanceledException) when (_lifecycleCancellation.IsCancellationRequested)
                {
                }
            }
            _lifecycleCancellation.Dispose();
        }

        /// <summary>建立待驗證紀錄、查詢 Twitch，並在重新確認設定仍有效後套用 Discord 角色結果。</summary>
        public async Task<TwitchSubscriptionResult> VerifyAsync(
            ulong guildId,
            ulong discordUserId,
            string broadcasterId,
            string locale,
            CancellationToken cancellationToken)
        {
            await using var userLock = await _operationCoordinator.LockUserAsync(discordUserId, cancellationToken);
            await using (await _operationCoordinator.LockGuildAsync(guildId, cancellationToken))
            {
                using var db = _dbService.GetDbContext();
                var configExists = await db.GuildTwitchSubscriptionConfig.AsNoTracking().AnyAsync(
                    x => x.GuildId == guildId && x.BroadcasterId == broadcasterId && !x.DeletionPending,
                    cancellationToken);
                if (!configExists)
                    return Result(TwitchSubscriptionStatus.BroadcasterUnavailable, broadcasterId);

                var check = await db.TwitchSubscriptionCheck.SingleOrDefaultAsync(x =>
                    x.GuildId == guildId &&
                    x.DiscordUserId == discordUserId &&
                    x.BroadcasterId == broadcasterId, cancellationToken);
                if (check == null)
                {
                    db.TwitchSubscriptionCheck.Add(new TwitchSubscriptionCheck
                    {
                        GuildId = guildId,
                        DiscordUserId = discordUserId,
                        BroadcasterId = broadcasterId,
                        Locale = SupportedLocale.Normalize(locale),
                        IsChecked = false,
                        PendingRoleRemoval = false,
                        LastCheckTime = DateTime.UtcNow,
                        DateAdded = DateTime.UtcNow
                    });
                }
                else
                {
                    check.Locale = SupportedLocale.Normalize(locale);
                    check.PendingRoleRemoval = false;
                }
                await db.SaveChangesAsync(cancellationToken);
            }

            TwitchSubscriptionResult lookup = await LookupAsync(discordUserId, broadcasterId, cancellationToken);
            if (lookup.Status is TwitchSubscriptionStatus.AuthorizationInvalid or TwitchSubscriptionStatus.AuthorizationMissing)
            {
                if (await IsAuthorizationCleanupStillRequiredAsync(
                    discordUserId,
                    lookup.Status,
                    cancellationToken))
                {
                    await CleanupAuthorizationCoreAsync(discordUserId, cancellationToken);
                }
                return lookup;
            }

            await using var guildLock = await _operationCoordinator.LockGuildAsync(guildId, cancellationToken);
            return await ApplyResultCoreAsync(guildId, discordUserId, broadcasterId, lookup, cancellationToken);
        }

        /// <summary>將使用者所有 Twitch 驗證標記待清理，移除角色並保留失敗項目供排程重試。</summary>
        public async Task<TwitchSubscriptionCancellationStatus> CancelAsync(
            ulong guildId,
            ulong discordUserId,
            CancellationToken cancellationToken)
        {
            await using var userLock = await _operationCoordinator.LockUserAsync(discordUserId, cancellationToken);
            await using var guildLock = await _operationCoordinator.LockGuildAsync(guildId, cancellationToken);
            return await CancelCoreAsync(guildId, discordUserId, cancellationToken);
        }

        private async Task<TwitchSubscriptionCancellationStatus> CancelCoreAsync(
            ulong guildId,
            ulong discordUserId,
            CancellationToken cancellationToken)
        {
            using var db = _dbService.GetDbContext();
            var checks = await db.TwitchSubscriptionCheck
                .Where(x => x.GuildId == guildId && x.DiscordUserId == discordUserId)
                .ToListAsync(cancellationToken);
            if (checks.Count == 0)
                return TwitchSubscriptionCancellationStatus.NotFound;
            foreach (var check in checks)
            {
                check.IsChecked = false;
                check.PendingRoleRemoval = true;
            }
            await db.SaveChangesAsync(cancellationToken);

            var broadcasterIds = checks.Select(x => x.BroadcasterId).Distinct().ToArray();
            var configs = await db.GuildTwitchSubscriptionConfig.AsNoTracking()
                .Where(x => x.GuildId == guildId && broadcasterIds.Contains(x.BroadcasterId))
                .ToDictionaryAsync(x => x.BroadcasterId, cancellationToken);
            MemberRoleOwnershipSnapshot ownership = await _roleService.LoadOwnershipSnapshotAsync(
                guildId, cancellationToken);
            bool cleanupComplete = true;
            foreach (var check in checks.ToArray())
            {
                if (configs.TryGetValue(check.BroadcasterId, out var pendingConfig) && pendingConfig.DeletionPending)
                {
                    cleanupComplete = false;
                    continue;
                }
                if (!configs.TryGetValue(check.BroadcasterId, out var config) ||
                    await _roleService.RemoveSubscriptionRolesAsync(
                        config, discordUserId, ownership, cancellationToken))
                {
                    db.TwitchSubscriptionCheck.Remove(check);
                }
                else
                {
                    cleanupComplete = false;
                }
            }
            await db.SaveChangesAsync(cancellationToken);
            return cleanupComplete
                ? TwitchSubscriptionCancellationStatus.Completed
                : TwitchSubscriptionCancellationStatus.RetryPending;
        }

        /// <summary>依 shard 處理設定刪除、待移除角色及到期驗證，並按使用者與 broadcaster 共用 Twitch 查詢結果。</summary>
        public async Task RunReverificationCycleAsync(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            bool succeeded = false;
            try
            {
                using var db = _dbService.GetDbContext();
                // DeletionPending 是獨立的耐久工作，不依賴 subscription check 是否仍存在。
                // 即使成員紀錄已清空，排程仍須重試 Tier 角色與設定本身的刪除。
                var pendingDeletions = await db.GuildTwitchSubscriptionConfig.AsNoTracking()
                    .DeletionPendingConfigurations()
                    .ToListAsync(cancellationToken);
                foreach (var config in pendingDeletions.Where(x => Bot.IsServerOnThisShard(x.GuildId)))
                    await _roleService.DeleteConfigurationAsync(config, cancellationToken);

                DateTime cutoff = DateTime.UtcNow.AddHours(-1);
                var dueChecks = await db.TwitchSubscriptionCheck.AsNoTracking()
                    .Where(x => x.LastCheckTime <= cutoff &&
                        db.GuildTwitchSubscriptionConfig.Any(config =>
                            config.GuildId == x.GuildId &&
                            config.BroadcasterId == x.BroadcasterId &&
                            !config.DeletionPending))
                    .ToListAsync(cancellationToken);
                dueChecks = dueChecks.Where(x => Bot.IsServerOnThisShard(x.GuildId)).ToList();

                foreach (var pendingCheck in dueChecks.Where(x => x.PendingRoleRemoval).ToArray())
                {
                    await using var userLock = await _operationCoordinator.LockUserAsync(
                        pendingCheck.DiscordUserId, cancellationToken);
                    await using var guildLock = await _operationCoordinator.LockGuildAsync(
                        pendingCheck.GuildId, cancellationToken);
                    await RetryPendingRoleRemovalAsync(pendingCheck, cancellationToken);
                }
                dueChecks = dueChecks.Where(x => !x.PendingRoleRemoval).ToList();

                // Twitch 停用時仍完成耐久刪除與待移除角色，但不發出訂閱查詢或 token refresh。
                if (!_twitchApiService.IsEnable)
                {
                    succeeded = true;
                    return;
                }

                foreach (var group in dueChecks.GroupBy(x => new { x.DiscordUserId, x.BroadcasterId }))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await using var userLock = await _operationCoordinator.LockUserAsync(
                        group.Key.DiscordUserId, cancellationToken);
                    TwitchSubscriptionResult result = await LookupAsync(
                        group.Key.DiscordUserId,
                        group.Key.BroadcasterId,
                        cancellationToken);
                    if (result.Status is TwitchSubscriptionStatus.AuthorizationInvalid or TwitchSubscriptionStatus.AuthorizationMissing)
                    {
                        if (await IsAuthorizationCleanupStillRequiredAsync(
                            group.Key.DiscordUserId,
                            result.Status,
                            cancellationToken))
                        {
                            await CleanupAuthorizationCoreAsync(group.Key.DiscordUserId, cancellationToken);
                        }
                        continue;
                    }

                    foreach (var check in group)
                    {
                        await using var guildLock = await _operationCoordinator.LockGuildAsync(
                            check.GuildId, cancellationToken);
                        await ApplyResultCoreAsync(
                            check.GuildId,
                            check.DiscordUserId,
                            check.BroadcasterId,
                            result,
                            cancellationToken);
                    }
                }
                succeeded = true;
            }
            finally
            {
                stopwatch.Stop();
                _metrics.RecordTwitchSubscriptionCycle(succeeded);
                _metrics.ObserveTwitchSubscriptionCycleDuration(stopwatch.Elapsed);
            }
        }

        /// <summary>清理目前 shard 持有 guild 中指定使用者的 Twitch 驗證與角色，失敗時保留耐久重試狀態。</summary>
        private async Task CleanupAuthorizationCoreAsync(
            ulong discordUserId,
            CancellationToken cancellationToken)
        {
            using var readDb = _dbService.GetDbContext();
            ulong[] guildIds = await readDb.TwitchSubscriptionCheck.AsNoTracking()
                .Where(x => x.DiscordUserId == discordUserId)
                .Select(x => x.GuildId)
                .Distinct()
                .ToArrayAsync(cancellationToken);
            foreach (ulong guildId in guildIds.Where(Bot.IsServerOnThisShard))
            {
                await using var guildLock = await _operationCoordinator.LockGuildAsync(guildId, cancellationToken);
                await CleanupAuthorizationGuildCoreAsync(discordUserId, guildId, cancellationToken);
            }
        }

        private async Task<bool> IsAuthorizationCleanupStillRequiredAsync(
            ulong discordUserId,
            TwitchSubscriptionStatus status,
            CancellationToken cancellationToken)
        {
            using var db = _dbService.GetDbContext();
            var authorization = await db.TwitchBroadcasterAuthorization.AsNoTracking()
                .Where(x => x.DiscordUserId == discordUserId)
                .Select(x => new { x.RevokedAt })
                .SingleOrDefaultAsync(cancellationToken);
            return status == TwitchSubscriptionStatus.AuthorizationMissing
                ? authorization == null
                : authorization?.RevokedAt != null;
        }

        private async Task CleanupAuthorizationGuildCoreAsync(
            ulong discordUserId,
            ulong guildId,
            CancellationToken cancellationToken)
        {
            using var db = _dbService.GetDbContext();
            var checks = await db.TwitchSubscriptionCheck
                .Where(x => x.DiscordUserId == discordUserId && x.GuildId == guildId)
                .ToListAsync(cancellationToken);
            var previouslyChecked = checks.Where(x => x.IsChecked).Select(x => x.Id).ToHashSet();
            var staleChecks = checks.Where(x =>
                _client.GetGuild(x.GuildId) == null && Bot.ShouldDeleteMissingGuild(x.GuildId)).ToArray();
            if (staleChecks.Length > 0)
                db.TwitchSubscriptionCheck.RemoveRange(staleChecks);
            checks = checks.Except(staleChecks).Where(x => _client.GetGuild(x.GuildId) != null).ToList();
            foreach (var check in checks)
            {
                check.IsChecked = false;
                check.PendingRoleRemoval = true;
            }
            await db.SaveChangesAsync(cancellationToken);

            var broadcasterIds = checks.Select(x => x.BroadcasterId).Distinct().ToArray();
            var configs = await db.GuildTwitchSubscriptionConfig.AsNoTracking()
                .Where(x => x.GuildId == guildId && broadcasterIds.Contains(x.BroadcasterId))
                .ToListAsync(cancellationToken);
            MemberRoleOwnershipSnapshot ownership = await _roleService.LoadOwnershipSnapshotAsync(
                guildId, cancellationToken);
            foreach (var check in checks.ToArray())
            {
                var config = configs.FirstOrDefault(x => x.GuildId == check.GuildId && x.BroadcasterId == check.BroadcasterId);
                if (config?.DeletionPending == true)
                    continue;
                if (config == null || await _roleService.RemoveSubscriptionRolesAsync(
                    config, discordUserId, ownership, cancellationToken))
                {
                    db.TwitchSubscriptionCheck.Remove(check);
                    if (config != null && previouslyChecked.Contains(check.Id))
                        await LogStatusAsync(check.GuildId, "TwitchMember.StatusLog.AuthorizationInvalid",
                            cancellationToken, discordUserId, config.BroadcasterDisplayName);
                }
                else if (previouslyChecked.Contains(check.Id))
                {
                    await LogStatusAsync(check.GuildId, "TwitchMember.StatusLog.RemovalFailed",
                        cancellationToken, discordUserId, config.BroadcasterDisplayName);
                }
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        /// <summary>取得可用授權並查詢訂閱；遇到 401 時只 refresh 與重試一次，其他錯誤保留既有角色。</summary>
        private async Task<TwitchSubscriptionResult> LookupAsync(
            ulong discordUserId,
            string broadcasterId,
            CancellationToken cancellationToken)
        {
            if (!_twitchApiService.IsEnable)
            {
                _metrics.RecordTwitchSubscriptionVerification(TwitchSubscriptionStatus.TemporaryFailure, null);
                return Result(TwitchSubscriptionStatus.TemporaryFailure, broadcasterId);
            }

            TwitchAuthorizationAccessResult authorization = await _tokenService.GetAsync(discordUserId, cancellationToken);
            if (authorization.Status != TwitchSubscriptionStatus.Subscribed)
            {
                _metrics.RecordTwitchSubscriptionVerification(authorization.Status, null);
                return Result(authorization.Status, broadcasterId, authorization.TwitchUserId);
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (_rateLimitUntilByTwitchUserId.TryGetValue(authorization.TwitchUserId, out DateTimeOffset retryAfter))
            {
                if (TwitchRateLimitPolicy.IsBlocked(now, retryAfter))
                    return Result(TwitchSubscriptionStatus.TemporaryFailure, broadcasterId, authorization.TwitchUserId);
                _rateLimitUntilByTwitchUserId.TryRemove(authorization.TwitchUserId, out _);
            }

            TwitchSubscriptionResult result = await _apiClient.CheckUserSubscriptionAsync(
                authorization.AccessToken,
                authorization.TwitchUserId,
                broadcasterId,
                cancellationToken);
            TrackRateLimit(authorization.TwitchUserId, result.RetryAfter);
            if (result.Status == TwitchSubscriptionStatus.AuthorizationInvalid)
            {
                authorization = await _tokenService.RefreshAfterUnauthorizedAsync(
                    authorization.TwitchUserId,
                    cancellationToken);
                if (authorization.Status == TwitchSubscriptionStatus.Subscribed)
                {
                    result = await _apiClient.CheckUserSubscriptionAsync(
                        authorization.AccessToken,
                        authorization.TwitchUserId,
                        broadcasterId,
                        cancellationToken);
                    TrackRateLimit(authorization.TwitchUserId, result.RetryAfter);
                    if (result.Status == TwitchSubscriptionStatus.AuthorizationInvalid)
                    {
                        TwitchSubscriptionStatus finalStatus = await _tokenService.InvalidateIfCurrentAccessTokenAsync(
                            authorization.TwitchUserId,
                            authorization.AccessToken,
                            cancellationToken);
                        result = Result(finalStatus, broadcasterId, authorization.TwitchUserId);
                    }
                }
                else
                {
                    result = Result(authorization.Status, broadcasterId, authorization.TwitchUserId);
                }
            }

            _metrics.RecordTwitchSubscriptionVerification(result.Status, result.Tier);
            return result;
        }

        private void TrackRateLimit(string twitchUserId, DateTimeOffset? retryAfter)
        {
            if (string.IsNullOrWhiteSpace(twitchUserId) || !retryAfter.HasValue)
                return;
            _rateLimitUntilByTwitchUserId.AddOrUpdate(
                twitchUserId,
                retryAfter.Value,
                (_, current) => current > retryAfter.Value ? current : retryAfter.Value);
        }

        /// <summary>重新讀取 check/config 後套用訂閱結果，確保刪除中的設定不會重新授權。</summary>
        private async Task<TwitchSubscriptionResult> ApplyResultCoreAsync(
            ulong guildId,
            ulong discordUserId,
            string broadcasterId,
            TwitchSubscriptionResult result,
            CancellationToken cancellationToken)
        {
            using var db = _dbService.GetDbContext();
            var check = await db.TwitchSubscriptionCheck.SingleOrDefaultAsync(x =>
                x.GuildId == guildId &&
                x.DiscordUserId == discordUserId &&
                x.BroadcasterId == broadcasterId, cancellationToken);
            if (check == null)
                return Result(TwitchSubscriptionStatus.BroadcasterUnavailable, broadcasterId, result.TwitchUserId);
            var config = await db.GuildTwitchSubscriptionConfig.AsNoTracking().SingleOrDefaultAsync(x =>
                x.GuildId == guildId && x.BroadcasterId == broadcasterId,
                cancellationToken);
            if (config == null)
            {
                db.TwitchSubscriptionCheck.Remove(check);
                await db.SaveChangesAsync(cancellationToken);
                return Result(TwitchSubscriptionStatus.BroadcasterUnavailable, broadcasterId, result.TwitchUserId);
            }
            if (config.DeletionPending)
                return Result(TwitchSubscriptionStatus.TemporaryFailure, broadcasterId, result.TwitchUserId);
            if (check.PendingRoleRemoval && result.Status == TwitchSubscriptionStatus.Subscribed)
                return Result(TwitchSubscriptionStatus.TemporaryFailure, broadcasterId, result.TwitchUserId);

            check.LastCheckTime = DateTime.UtcNow;
            bool wasChecked = check.IsChecked;
            string previousTier = check.Tier;
            if (result.Status == TwitchSubscriptionStatus.Subscribed)
            {
                bool synchronized = await _roleService.SynchronizeSubscribedRolesAsync(
                    config, discordUserId, result.Tier, cancellationToken);
                if (synchronized)
                {
                    check.Tier = result.Tier;
                    check.IsGift = result.IsGift;
                    check.IsChecked = true;
                    check.PendingRoleRemoval = false;
                }
                await db.SaveChangesAsync(cancellationToken);
                if (synchronized && (!wasChecked || previousTier != result.Tier))
                    await LogStatusAsync(guildId, "TwitchMember.StatusLog.Verified", cancellationToken,
                        discordUserId, config.BroadcasterDisplayName,
                        Interaction.TwitchMember.TwitchMember.FormatTier(result.Tier));
                else if (!synchronized)
                    await LogStatusAsync(guildId, "TwitchMember.StatusLog.RoleFailed", cancellationToken,
                        discordUserId, config.BroadcasterDisplayName);
                return synchronized
                    ? result
                    : Result(TwitchSubscriptionStatus.TemporaryFailure, broadcasterId, result.TwitchUserId);
            }

            if (result.Status == TwitchSubscriptionStatus.NotSubscribed)
            {
                check.IsChecked = false;
                check.PendingRoleRemoval = true;
                check.Tier = null;
                check.IsGift = false;
                await db.SaveChangesAsync(cancellationToken);
                bool removed = await _roleService.RemoveSubscriptionRolesAsync(
                    config, discordUserId, cancellationToken);
                if (removed)
                {
                    db.TwitchSubscriptionCheck.Remove(check);
                    await db.SaveChangesAsync(cancellationToken);
                    if (wasChecked)
                        await LogStatusAsync(guildId, "TwitchMember.StatusLog.Removed", cancellationToken,
                            discordUserId, config.BroadcasterDisplayName);
                }
                else if (wasChecked)
                {
                    await LogStatusAsync(guildId, "TwitchMember.StatusLog.RemovalFailed", cancellationToken,
                        discordUserId, config.BroadcasterDisplayName);
                }
                return removed
                    ? result
                    : Result(TwitchSubscriptionStatus.TemporaryFailure, broadcasterId, result.TwitchUserId);
            }

            await db.SaveChangesAsync(cancellationToken);
            return result;
        }

        private async Task RetryPendingRoleRemovalAsync(
            TwitchSubscriptionCheck pendingCheck,
            CancellationToken cancellationToken)
        {
            using var db = _dbService.GetDbContext();
            var check = await db.TwitchSubscriptionCheck.SingleOrDefaultAsync(x => x.Id == pendingCheck.Id, cancellationToken);
            if (check == null || !check.PendingRoleRemoval)
                return;
            var config = await db.GuildTwitchSubscriptionConfig.AsNoTracking().SingleOrDefaultAsync(x =>
                x.GuildId == check.GuildId && x.BroadcasterId == check.BroadcasterId,
                cancellationToken);
            if (config?.DeletionPending == true)
                return;
            if (config == null || await _roleService.RemoveSubscriptionRolesAsync(config, check.DiscordUserId, cancellationToken))
            {
                db.TwitchSubscriptionCheck.Remove(check);
                await db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                check.LastCheckTime = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task LogStatusAsync(
            ulong guildId,
            string resourceKey,
            CancellationToken cancellationToken,
            params object[] arguments)
        {
            SocketGuild guild = _client.GetGuild(guildId);
            if (guild == null)
                return;
            using var db = _dbService.GetDbContext();
            ulong? channelId = await db.GuildConfig.AsNoTracking()
                .Where(x => x.GuildId == guildId && x.LogMemberStatusChannelId != 0)
                .Select(x => (ulong?)x.LogMemberStatusChannelId)
                .SingleOrDefaultAsync(cancellationToken);
            SocketTextChannel channel = channelId.HasValue ? guild.GetTextChannel(channelId.Value) : null;
            if (channel == null)
                return;

            try
            {
                string locale = await _guildLocaleService.GetAsync(guildId, guild);
                await channel.SendMessageAsync(
                    embed: new EmbedBuilder()
                        .WithOkColor()
                        .WithDescription(_localizer.Format(resourceKey, locale, arguments))
                        .Build(),
                    options: new RequestOptions { CancelToken = cancellationToken });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Warn($"寫入 Twitch 訂閱驗證紀錄失敗: {guildId} / {ex.GetType().Name}");
            }
        }

        /// <summary>處理可能延遲或重複的授權事件，並以 MySQL 現況確認仍 revoked 後才清理角色。</summary>
        private async Task HandleAuthorizationChangedAsync(RedisValue value, CancellationToken cancellationToken)
        {
            try
            {
                var payload = JsonConvert.DeserializeObject<TwitchAuthorizationChangedPayload>(value!);
                string status = payload?.Status?.Trim().ToLowerInvariant();
                if (payload == null || string.IsNullOrWhiteSpace(payload.TwitchUserId))
                    return;

                using var db = _dbService.GetDbContext();
                ulong? discordUserId = await db.TwitchBroadcasterAuthorization.AsNoTracking()
                    .Where(x => x.TwitchUserId == payload.TwitchUserId)
                    .Select(x => (ulong?)x.DiscordUserId)
                    .SingleOrDefaultAsync(cancellationToken);
                if (!discordUserId.HasValue)
                    return;

                await using var userLock = await _operationCoordinator.LockUserAsync(
                    discordUserId.Value,
                    cancellationToken);
                // Pub/Sub 可能延遲或重複；取得 user lock 後須重讀 MySQL，不能只信 payload。
                // 這可避免舊 invalid 事件清掉使用者重新連結後的新授權。
                using var currentDb = _dbService.GetDbContext();
                var current = await currentDb.TwitchBroadcasterAuthorization.AsNoTracking()
                    .Where(x => x.TwitchUserId == payload.TwitchUserId && x.DiscordUserId == discordUserId.Value)
                    .Select(x => new { x.RevokedAt })
                    .SingleOrDefaultAsync(cancellationToken);
                if (TwitchAuthorizationEventPolicy.ShouldCleanup(
                    status,
                    current != null,
                    current?.RevokedAt != null))
                {
                    await CleanupAuthorizationCoreAsync(discordUserId.Value, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "處理 Twitch 訂閱授權失效事件失敗");
            }
        }

        /// <summary>使用者重新加入 guild 時，依最後有效紀錄恢復共用與 Tier 角色。</summary>
        private async Task RestoreOnUserJoinedAsync(SocketGuildUser user)
        {
            try
            {
                await using var userLock = await _operationCoordinator.LockUserAsync(
                    user.Id,
                    _lifecycleCancellation.Token);
                await using var guildLock = await _operationCoordinator.LockGuildAsync(
                    user.Guild.Id,
                    _lifecycleCancellation.Token);
                using var db = _dbService.GetDbContext();
                var checks = await db.TwitchSubscriptionCheck.AsNoTracking()
                    .Where(x => x.GuildId == user.Guild.Id && x.DiscordUserId == user.Id && x.IsChecked)
                    .ToListAsync(_lifecycleCancellation.Token);
                string[] broadcasterIds = checks.Select(x => x.BroadcasterId).ToArray();
                var configs = await db.GuildTwitchSubscriptionConfig.AsNoTracking()
                    .ActiveConfigurations()
                    .Where(x => x.GuildId == user.Guild.Id && broadcasterIds.Contains(x.BroadcasterId))
                    .ToListAsync(_lifecycleCancellation.Token);
                MemberRoleOwnershipSnapshot ownership = await _roleService.LoadOwnershipSnapshotAsync(
                    user.Guild.Id, _lifecycleCancellation.Token);
                foreach (var check in checks)
                {
                    var config = configs.FirstOrDefault(x => x.BroadcasterId == check.BroadcasterId);
                    if (config != null)
                        await _roleService.SynchronizeSubscribedRolesAsync(
                            config, user.Id, check.Tier, ownership, _lifecycleCancellation.Token);
                }
            }
            catch (OperationCanceledException) when (_lifecycleCancellation.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "Twitch 訂閱者重新加入身分組回補失敗");
            }
        }

        /// <summary>以 guild entitlement snapshot 對帳 Twitch 角色，移除沒有有效驗證紀錄的孤兒授權。</summary>
        private async Task ReconcileOrphanRolesAsync(CancellationToken cancellationToken)
        {
            using var db = _dbService.GetDbContext();
            ulong[] guildIds = await db.GuildTwitchSubscriptionConfig.AsNoTracking()
                .ActiveConfigurations()
                .Select(x => x.GuildId)
                .Distinct()
                .ToArrayAsync(cancellationToken);
            foreach (ulong guildId in guildIds.Where(Bot.IsServerOnThisShard))
            {
                SocketGuild guild = _client.GetGuild(guildId);
                if (guild == null)
                    continue;
                try
                {
                    await guild.DownloadUsersAsync();
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Log.Warn($"下載 Twitch 訂閱角色成員清單失敗: {guild.Id} / {ex.GetType().Name}");
                    continue;
                }

                // 成員下載不持有 guild lock；下載後才鎖定並一次載入 entitlement snapshot。
                // snapshot 同時避免與角色授予交錯，以及逐成員查詢造成 N+1。
                await using var guildLock = await _operationCoordinator.LockGuildAsync(guildId, cancellationToken);
                using var guildDb = _dbService.GetDbContext();
                var configs = await guildDb.GuildTwitchSubscriptionConfig.AsNoTracking()
                    .ActiveConfigurations()
                    .Where(x => x.GuildId == guildId)
                    .ToListAsync(cancellationToken);
                var checks = await guildDb.TwitchSubscriptionCheck.AsNoTracking()
                    .Where(x => x.GuildId == guildId && x.IsChecked && !x.PendingRoleRemoval)
                    .ToListAsync(cancellationToken);
                MemberRoleOwnershipSnapshot ownership = await _roleService.LoadOwnershipSnapshotAsync(
                    guildId, cancellationToken);
                var checksByBroadcaster = checks
                    .GroupBy(x => x.BroadcasterId)
                    .ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.Ordinal);

                foreach (var config in configs)
                {
                    checksByBroadcaster.TryGetValue(config.BroadcasterId, out TwitchSubscriptionCheck[] broadcasterChecks);
                    broadcasterChecks ??= [];
                    foreach ((string tier, ulong roleId) in new[]
                    {
                        ("1000", config.Tier1RoleId),
                        ("2000", config.Tier2RoleId),
                        ("3000", config.Tier3RoleId)
                    })
                    {
                        SocketRole role = guild.GetRole(roleId);
                        if (role == null)
                            continue;
                        HashSet<ulong> validUsers = broadcasterChecks
                            .Where(x => x.Tier == tier)
                            .Select(x => x.DiscordUserId)
                            .ToHashSet();
                        foreach (SocketGuildUser member in role.Members.Where(x => !validUsers.Contains(x.Id)).ToArray())
                            await _roleService.RemoveOrphanRoleAsync(
                                guild, member.Id, role.Id, ownership, cancellationToken);
                    }
                }

                foreach (var sharedRoleGroup in configs.GroupBy(x => x.SubscriberRoleId))
                {
                    SocketRole sharedRole = guild.GetRole(sharedRoleGroup.Key);
                    if (sharedRole == null)
                        continue;
                    HashSet<string> broadcasterIds = sharedRoleGroup
                        .Select(x => x.BroadcasterId)
                        .ToHashSet(StringComparer.Ordinal);
                    HashSet<ulong> validUsers = checks
                        .Where(x => broadcasterIds.Contains(x.BroadcasterId))
                        .Select(x => x.DiscordUserId)
                        .ToHashSet();
                    foreach (SocketGuildUser member in sharedRole.Members.Where(x => !validUsers.Contains(x.Id)).ToArray())
                        await _roleService.RemoveOrphanRoleAsync(
                            guild, member.Id, sharedRole.Id, ownership, cancellationToken);
                }
            }
        }

        private static TwitchSubscriptionResult Result(
            TwitchSubscriptionStatus status,
            string broadcasterId,
            string twitchUserId = null)
            => new() { Status = status, BroadcasterId = broadcasterId, TwitchUserId = twitchUserId };

        private sealed class TwitchAuthorizationChangedPayload
        {
            public string TwitchUserId { get; set; }
            public string Status { get; set; }
        }
    }
}
