using DiscordStreamNotifyBot.Auth;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService.Twitch;

namespace DiscordStreamNotifyBot.SharedService.TwitchSubscription
{
    public sealed class TwitchAuthorizationAccessResult
    {
        public TwitchSubscriptionStatus Status { get; init; }
        public string AccessToken { get; init; }
        public string TwitchUserId { get; init; }
        public string UserLogin { get; init; }
        public string DisplayName { get; init; }
    }

    public sealed class TwitchAuthorizationTokenService
    {
        private const string RequiredScope = "user:read:subscriptions";
        private const int ImmediatePersistenceAttempts = 6;
        private static readonly TimeSpan ImmediatePersistenceDelay = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan PendingPersistenceDelay = TimeSpan.FromSeconds(30);

        private readonly MainDbService _dbService;
        private readonly BotConfig _botConfig;
        private readonly TwitchSubscriptionApiClient _apiClient;
        private readonly TwitchOAuthRefreshLock _refreshLock;
        private readonly NotifierMetrics _metrics;
        private readonly TwitchRefreshRotationLifecycle _rotationLifecycle;
        private readonly object _stopGate = new();
        private Task _stopTask;

        public TwitchAuthorizationTokenService(
            MainDbService dbService,
            BotConfig botConfig,
            TwitchSubscriptionApiClient apiClient,
            NotifierMetrics metrics)
        {
            _dbService = dbService;
            _botConfig = botConfig;
            _apiClient = apiClient;
            _refreshLock = TwitchOAuthRefreshLock.Create(Bot.Redis);
            _metrics = metrics;
            _rotationLifecycle = new TwitchRefreshRotationLifecycle(_metrics.SetTwitchRefreshPendingPersistenceCount);
        }

        /// <summary>停止接受新的 refresh，並等待所有已被 Twitch 接受的 rotation 安全保存。</summary>
        public Task StopAsync()
        {
            lock (_stopGate)
                return _stopTask ??= StopCoreAsync();
        }

        /// <summary>讀取並驗證使用者的持久化 Twitch 授權；本地設定或解密異常只回傳暫時失敗，不撤銷授權。</summary>
        public async Task<TwitchAuthorizationAccessResult> GetAsync(
            ulong discordUserId,
            CancellationToken cancellationToken)
        {
            using var db = _dbService.GetDbContext();
            var entity = await db.TwitchBroadcasterAuthorization.AsNoTracking()
                .SingleOrDefaultAsync(x => x.DiscordUserId == discordUserId, cancellationToken);
            TwitchAuthorizationLocalState entityState = ClassifyEntity(entity);
            if (entityState != TwitchAuthorizationLocalState.Active)
                return Status(MapLocalState(entityState), entity?.TwitchUserId);

            TwitchAccessTokenData token;
            try
            {
                token = TokenManager.GetTokenResponseValue<TwitchAccessTokenData>(
                    entity.EncryptedAccessToken,
                    _botConfig.ProviderTokenEncryptionKey);
            }
            catch (Exception ex)
            {
                _metrics.RecordTwitchTokenOperation(TwitchTokenOperation.Decrypt, TwitchTokenOperationResult.Invalid);
                Log.Warn($"Twitch token 解密失敗，保留既有授權資料: {ex.GetType().Name}");
                return Status(TwitchSubscriptionStatus.TemporaryFailure, entity.TwitchUserId);
            }

            TwitchAuthorizationLocalState tokenState = ClassifyToken(entity, token);
            if (tokenState != TwitchAuthorizationLocalState.Active)
                return Status(MapLocalState(tokenState), entity.TwitchUserId);

            _metrics.RecordTwitchTokenOperation(TwitchTokenOperation.Decrypt, TwitchTokenOperationResult.Success);
            return Success(entity, token.AccessToken);
        }

        /// <summary>在 lifecycle 與跨程序 lease 保護下重新驗證或 rotation token，確保同一 refresh token 不被兩端同時消耗。</summary>
        public async Task<TwitchAuthorizationAccessResult> RefreshAfterUnauthorizedAsync(
            string twitchUserId,
            CancellationToken cancellationToken)
        {
            if (!_rotationLifecycle.TryBeginRefresh(out var refreshOperation))
                return Status(TwitchSubscriptionStatus.TemporaryFailure, twitchUserId);
            using (refreshOperation)
            {
                return await RefreshAfterUnauthorizedCoreAsync(twitchUserId, cancellationToken);
            }
        }

        /// <summary>取得 lease 後重讀授權，完成 validate、refresh、CAS 保存與延遲保存交接。</summary>
        private async Task<TwitchAuthorizationAccessResult> RefreshAfterUnauthorizedCoreAsync(
            string twitchUserId,
            CancellationToken cancellationToken)
        {
            TwitchOAuthRefreshLockAcquireResult lockResult = await _refreshLock.TryAcquireAsync(twitchUserId, cancellationToken);
            if (lockResult.Status != TwitchOAuthRefreshLockAcquireStatus.Acquired)
            {
                _metrics.RecordTwitchTokenOperation(
                    TwitchTokenOperation.RefreshLock,
                    lockResult.Status == TwitchOAuthRefreshLockAcquireStatus.Contended
                        ? TwitchTokenOperationResult.Contended
                        : TwitchTokenOperationResult.TemporaryFailure);
                if (lockResult.Exception != null)
                    Log.Warn($"Twitch refresh lock 暫時無法取得: {lockResult.Exception.GetType().Name}");
                return Status(TwitchSubscriptionStatus.TemporaryFailure);
            }
            _metrics.RecordTwitchTokenOperation(TwitchTokenOperation.RefreshLock, TwitchTokenOperationResult.Success);

            bool leaseTransferredToRetry = false;
            try
            {
                // 取得跨程序 lease 後仍須重讀 MySQL；其他 instance 可能已完成 rotation。
                // 後續寫入會以此處讀到的密文作為 CAS 版本，避免舊 token 覆寫新 token。
                TwitchBroadcasterAuthorization entity;
                using (var db = _dbService.GetDbContext())
                {
                    entity = await db.TwitchBroadcasterAuthorization.AsNoTracking()
                        .SingleOrDefaultAsync(x => x.TwitchUserId == twitchUserId, cancellationToken);
                }

                TwitchAuthorizationLocalState entityState = ClassifyEntity(entity);
                if (entityState != TwitchAuthorizationLocalState.Active)
                    return Status(MapLocalState(entityState), entity?.TwitchUserId);

                TwitchAccessTokenData token;
                try
                {
                    token = TokenManager.GetTokenResponseValue<TwitchAccessTokenData>(
                        entity.EncryptedAccessToken,
                        _botConfig.ProviderTokenEncryptionKey);
                }
                catch (Exception ex)
                {
                    Log.Warn($"Twitch token 在 refresh lock 內解密失敗，保留既有授權資料: {ex.GetType().Name}");
                    return Status(TwitchSubscriptionStatus.TemporaryFailure, entity.TwitchUserId);
                }

                if (ClassifyToken(entity, token) != TwitchAuthorizationLocalState.Active)
                    return Status(TwitchSubscriptionStatus.TemporaryFailure, entity.TwitchUserId);

                TwitchProviderResult<TwitchValidateTokenData> validation =
                    await _apiClient.ValidateTokenAsync(token.AccessToken, cancellationToken);
                if (validation.Status == TwitchProviderResultStatus.Success)
                {
                    if (!IsValidIdentity(entity, validation.Value))
                        return Status(TwitchSubscriptionStatus.TemporaryFailure, entity.TwitchUserId);

                    _metrics.RecordTwitchTokenOperation(TwitchTokenOperation.Validate, TwitchTokenOperationResult.Success);
                    return Success(entity, token.AccessToken);
                }
                if (validation.Status is TwitchProviderResultStatus.Failure or TwitchProviderResultStatus.TemporaryFailure)
                {
                    _metrics.RecordTwitchTokenOperation(TwitchTokenOperation.Validate, TwitchTokenOperationResult.TemporaryFailure);
                    return Status(TwitchSubscriptionStatus.TemporaryFailure, entity.TwitchUserId);
                }

                TwitchProviderResult<TwitchAccessTokenData> refresh =
                    await _apiClient.RefreshTokenAsync(token.RefreshToken, cancellationToken);
                if (refresh.Status == TwitchProviderResultStatus.Invalid)
                {
                    _metrics.RecordTwitchTokenOperation(TwitchTokenOperation.Refresh, TwitchTokenOperationResult.Invalid);
                    return await MarkInvalidIfCurrentAsync(
                        entity.TwitchUserId,
                        entity.EncryptedAccessToken,
                        "refresh_invalid",
                        lockResult.Lease,
                        cancellationToken)
                            ? Status(TwitchSubscriptionStatus.AuthorizationInvalid, entity.TwitchUserId)
                            : Status(TwitchSubscriptionStatus.TemporaryFailure, entity.TwitchUserId);
                }
                if (refresh.Status != TwitchProviderResultStatus.Success)
                {
                    _metrics.RecordTwitchTokenOperation(TwitchTokenOperation.Refresh, TwitchTokenOperationResult.TemporaryFailure);
                    return Status(TwitchSubscriptionStatus.TemporaryFailure, entity.TwitchUserId);
                }

                TwitchAccessTokenData refreshedToken = refresh.Value;
                refreshedToken.RefreshToken = string.IsNullOrWhiteSpace(refreshedToken.RefreshToken)
                    ? token.RefreshToken
                    : refreshedToken.RefreshToken;
                refreshedToken.TwitchUserId = entity.TwitchUserId;
                refreshedToken.Scopes ??= token.Scopes;
                refreshedToken.TokenType ??= token.TokenType;
                if (ClassifyToken(entity, refreshedToken) != TwitchAuthorizationLocalState.Active)
                {
                    Log.Warn($"Twitch refresh 回應不符合 token contract，保留既有授權資料: {entity.TwitchUserId}");
                    return Status(TwitchSubscriptionStatus.TemporaryFailure, entity.TwitchUserId);
                }

                DateTime now = DateTime.UtcNow;
                var pending = new PendingRefreshPersistence(
                    entity.TwitchUserId,
                    entity.EncryptedAccessToken,
                    TokenManager.CreateToken(refreshedToken, _botConfig.ProviderTokenEncryptionKey),
                    refreshedToken.ExpiresIn > 0 ? now.AddSeconds(refreshedToken.ExpiresIn) : entity.TokenExpiresAt,
                    now,
                    lockResult.Lease);

                // Twitch 接受 refresh 後舊 token 可能立刻失效，因此 replacement 必須在 lease 內落盤。
                // 立即保存失敗時只能把 lease 一併移交背景重試，不能放行其他 instance 使用舊 token。
                TwitchRefreshPersistenceDecision persistence = await PersistWithRetriesAsync(
                    pending,
                    ImmediatePersistenceAttempts,
                    ImmediatePersistenceDelay,
                    CancellationToken.None);
                if (persistence == TwitchRefreshPersistenceDecision.Stale)
                    return Status(TwitchSubscriptionStatus.TemporaryFailure, entity.TwitchUserId);
                if (persistence != TwitchRefreshPersistenceDecision.AlreadyPersisted)
                {
                    QueuePendingPersistence(pending);
                    leaseTransferredToRetry = true;
                    _metrics.RecordTwitchTokenOperation(TwitchTokenOperation.Refresh, TwitchTokenOperationResult.TemporaryFailure);
                    return Status(TwitchSubscriptionStatus.TemporaryFailure, entity.TwitchUserId);
                }

                TwitchProviderResult<TwitchValidateTokenData> refreshedValidation =
                    await _apiClient.ValidateTokenAsync(refreshedToken.AccessToken, cancellationToken);
                if (refreshedValidation.Status != TwitchProviderResultStatus.Success ||
                    !IsValidIdentity(entity, refreshedValidation.Value))
                {
                    _metrics.RecordTwitchTokenOperation(TwitchTokenOperation.Validate, TwitchTokenOperationResult.TemporaryFailure);
                    return Status(TwitchSubscriptionStatus.TemporaryFailure, entity.TwitchUserId);
                }

                await TryUpdateValidationMetadataAsync(
                    pending,
                    refreshedValidation.Value,
                    CancellationToken.None);
                _metrics.RecordTwitchTokenOperation(TwitchTokenOperation.Refresh, TwitchTokenOperationResult.Success);
                return Success(entity, refreshedToken.AccessToken);
            }
            finally
            {
                if (!leaseTransferredToRetry)
                {
                    var release = await lockResult.Lease.ReleaseAsync(CancellationToken.None);
                    if (release.Status != TwitchOAuthRefreshLockReleaseStatus.Released)
                        Log.Warn($"Twitch refresh lock 釋放結果: {release.Status}");
                }
            }
        }

        /// <summary>僅在被 Twitch 拒絕的 access token 仍是目前版本時標記授權失效，避免舊 401 撤銷新 token。</summary>
        public async Task<TwitchSubscriptionStatus> InvalidateIfCurrentAccessTokenAsync(
            string twitchUserId,
            string rejectedAccessToken,
            CancellationToken cancellationToken)
        {
            TwitchOAuthRefreshLockAcquireResult lockResult = await _refreshLock.TryAcquireAsync(twitchUserId, cancellationToken);
            if (lockResult.Status != TwitchOAuthRefreshLockAcquireStatus.Acquired)
                return TwitchSubscriptionStatus.TemporaryFailure;
            _metrics.RecordTwitchTokenOperation(TwitchTokenOperation.RefreshLock, TwitchTokenOperationResult.Success);

            try
            {
                TwitchBroadcasterAuthorization entity;
                using (var db = _dbService.GetDbContext())
                {
                    entity = await db.TwitchBroadcasterAuthorization.AsNoTracking().SingleOrDefaultAsync(
                        x => x.TwitchUserId == twitchUserId, cancellationToken);
                }
                TwitchAuthorizationLocalState entityState = ClassifyEntity(entity);
                if (entityState != TwitchAuthorizationLocalState.Active)
                    return MapLocalState(entityState);

                TwitchAccessTokenData currentToken;
                try
                {
                    currentToken = TokenManager.GetTokenResponseValue<TwitchAccessTokenData>(
                        entity.EncryptedAccessToken,
                        _botConfig.ProviderTokenEncryptionKey);
                }
                catch (Exception ex)
                {
                    Log.Warn($"Twitch token 最終 401 後解密失敗，保留既有授權資料: {ex.GetType().Name}");
                    return TwitchSubscriptionStatus.TemporaryFailure;
                }

                if (ClassifyToken(entity, currentToken) != TwitchAuthorizationLocalState.Active ||
                    !string.Equals(currentToken.AccessToken, rejectedAccessToken, StringComparison.Ordinal))
                {
                    return TwitchSubscriptionStatus.TemporaryFailure;
                }

                if (!await MarkInvalidIfCurrentAsync(
                    entity.TwitchUserId,
                    entity.EncryptedAccessToken,
                    "subscription_unauthorized_after_refresh",
                    lockResult.Lease,
                    cancellationToken))
                {
                    return TwitchSubscriptionStatus.TemporaryFailure;
                }
                _metrics.RecordTwitchTokenOperation(TwitchTokenOperation.Validate, TwitchTokenOperationResult.Invalid);
                return TwitchSubscriptionStatus.AuthorizationInvalid;
            }
            finally
            {
                var release = await lockResult.Lease.ReleaseAsync(CancellationToken.None);
                if (release.Status != TwitchOAuthRefreshLockReleaseStatus.Released)
                    Log.Warn($"Twitch refresh lock 釋放結果: {release.Status}");
            }
        }

        private TwitchAuthorizationLocalState ClassifyEntity(TwitchBroadcasterAuthorization entity)
            => TwitchAuthorizationLocalStatePolicy.ClassifyEntity(
                entity != null,
                entity?.RevokedAt != null,
                entity?.ClientId == _botConfig.TwitchClientId,
                !string.IsNullOrWhiteSpace(entity?.EncryptedAccessToken),
                HasRequiredScope(entity?.Scopes));

        private static TwitchAuthorizationLocalState ClassifyToken(
            TwitchBroadcasterAuthorization entity,
            TwitchAccessTokenData token)
            => TwitchAuthorizationLocalStatePolicy.ClassifyToken(
                !string.IsNullOrWhiteSpace(token?.AccessToken),
                !string.IsNullOrWhiteSpace(token?.RefreshToken),
                string.Equals(token?.TokenType, "bearer", StringComparison.OrdinalIgnoreCase),
                !string.IsNullOrWhiteSpace(token?.TwitchUserId) && token.TwitchUserId == entity.TwitchUserId,
                token?.Scopes?.Contains(RequiredScope, StringComparer.Ordinal) == true);

        private bool IsValidIdentity(TwitchBroadcasterAuthorization entity, TwitchValidateTokenData validation)
            => validation != null &&
                validation.ClientId == _botConfig.TwitchClientId &&
                validation.UserId == entity.TwitchUserId &&
                !string.IsNullOrWhiteSpace(validation.Login) &&
                validation.ExpiresIn >= 0 &&
                validation.Scopes?.Contains(RequiredScope, StringComparer.Ordinal) == true;

        private static bool HasRequiredScope(string scopes)
        {
            if (string.IsNullOrWhiteSpace(scopes))
                return false;
            try
            {
                string[] values = JsonConvert.DeserializeObject<string[]>(scopes);
                if (values != null)
                    return values.Contains(RequiredScope, StringComparer.Ordinal);
            }
            catch (JsonException)
            {
            }
            return scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Contains(RequiredScope, StringComparer.Ordinal);
        }

        private async Task<TwitchRefreshPersistenceDecision> PersistWithRetriesAsync(
            PendingRefreshPersistence pending,
            int attempts,
            TimeSpan delay,
            CancellationToken cancellationToken)
        {
            for (int attempt = 1; attempt <= attempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    TwitchRefreshPersistenceDecision decision = await PersistRotationOnceAsync(pending, cancellationToken);
                    if (decision != TwitchRefreshPersistenceDecision.WriteReplacement)
                        return decision;
                    return TwitchRefreshPersistenceDecision.AlreadyPersisted;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Log.Warn($"Twitch refresh token rotation 保存第 {attempt} 次失敗，將保留 lease 重試: {ex.GetType().Name}");
                    if (attempt < attempts)
                        await Task.Delay(delay, cancellationToken);
                }
            }
            return TwitchRefreshPersistenceDecision.WriteReplacement;
        }

        /// <summary>確認 lease owner 後，以密文 CAS 保存一次 rotation，並辨識冪等完成或 stale 狀態。</summary>
        private async Task<TwitchRefreshPersistenceDecision> PersistRotationOnceAsync(
            PendingRefreshPersistence pending,
            CancellationToken cancellationToken)
        {
            // 網路或 DB I/O 期間 lease 可能已由新 owner 接手；任何寫入前都要重新確認所有權。
            var ownership = await pending.Lease.EnsureOwnedAsync(cancellationToken);
            if (ownership.Status == TwitchOAuthRefreshLockOwnershipStatus.OwnershipLost)
            {
                Log.Warn("Twitch refresh lock owner 已變更，停止寫入: refresh_token_persistence");
                return TwitchRefreshPersistenceDecision.Stale;
            }
            if (ownership.Status == TwitchOAuthRefreshLockOwnershipStatus.TemporaryFailure)
                throw new InvalidOperationException("暫時無法確認 Twitch refresh lock owner。", ownership.Exception);

            using var db = _dbService.GetDbContext();
            var current = await db.TwitchBroadcasterAuthorization.AsNoTracking()
                .Where(x => x.TwitchUserId == pending.TwitchUserId)
                .Select(x => new { x.EncryptedAccessToken, x.RevokedAt })
                .SingleOrDefaultAsync(cancellationToken);
            if (current == null)
                return TwitchRefreshPersistenceDecision.Stale;

            // 密文同時作為 rotation 版本：expected 可替換，replacement 是冪等成功，其他值皆不可覆寫。
            TwitchRefreshPersistenceDecision decision = TwitchRefreshPersistencePolicy.Decide(
                current.EncryptedAccessToken,
                pending.ExpectedCiphertext,
                pending.ReplacementCiphertext,
                current.RevokedAt != null);
            if (decision != TwitchRefreshPersistenceDecision.WriteReplacement)
                return decision;

            int updated = await db.TwitchBroadcasterAuthorization
                .Where(x => x.TwitchUserId == pending.TwitchUserId &&
                    x.RevokedAt == null &&
                    x.EncryptedAccessToken == pending.ExpectedCiphertext)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.EncryptedAccessToken, pending.ReplacementCiphertext)
                    .SetProperty(x => x.TokenExpiresAt, pending.TokenExpiresAt)
                    .SetProperty(x => x.DateUpdated, pending.DateUpdated), cancellationToken);
            if (updated == 1)
                return TwitchRefreshPersistenceDecision.WriteReplacement;

            using var verifyDb = _dbService.GetDbContext();
            var after = await verifyDb.TwitchBroadcasterAuthorization.AsNoTracking()
                .Where(x => x.TwitchUserId == pending.TwitchUserId)
                .Select(x => new { x.EncryptedAccessToken, x.RevokedAt })
                .SingleOrDefaultAsync(cancellationToken);
            return after == null
                ? TwitchRefreshPersistenceDecision.Stale
                : TwitchRefreshPersistencePolicy.Decide(
                    after.EncryptedAccessToken,
                    pending.ExpectedCiphertext,
                    pending.ReplacementCiphertext,
                    after.RevokedAt != null);
        }

        /// <summary>將立即保存失敗的 rotation 與 lease 登記到背景重試及關機 drain。</summary>
        private void QueuePendingPersistence(PendingRefreshPersistence pending)
        {
            // 先建立暫停中的 task 並登記到關機 drain，再啟動重試，避免交接空窗漏掉已接受的 rotation。
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Task task = RunQueuedPendingPersistenceAsync(pending, start.Task);
            _rotationLifecycle.TrackAcceptedPersistence(task);
            start.SetResult();
        }

        private async Task RunQueuedPendingPersistenceAsync(
            PendingRefreshPersistence pending,
            Task start)
        {
            await start;
            await RetryPendingPersistenceUntilCompletedAsync(pending);
        }

        /// <summary>持續重試已接受的 rotation，直到 replacement 落盤或確認其他狀態已取代它。</summary>
        private async Task RetryPendingPersistenceUntilCompletedAsync(
            PendingRefreshPersistence pending)
        {
            try
            {
                while (true)
                {
                    TwitchRefreshPersistenceDecision decision = await PersistWithRetriesAsync(
                        pending,
                        1,
                        TimeSpan.Zero,
                        CancellationToken.None);
                    if (decision is TwitchRefreshPersistenceDecision.AlreadyPersisted or TwitchRefreshPersistenceDecision.Stale)
                    {
                        if (decision == TwitchRefreshPersistenceDecision.AlreadyPersisted)
                            Log.Info($"Twitch refresh token rotation 延遲保存完成: {pending.TwitchUserId}");
                        else
                            Log.Warn($"Twitch refresh token rotation 延遲保存已過期，停止重試: {pending.TwitchUserId}");
                        return;
                    }
                    await Task.Delay(PendingPersistenceDelay);
                }
            }
            finally
            {
                await pending.Lease.ReleaseAsync(CancellationToken.None);
            }
        }

        /// <summary>等待 refresh operation 完成交接並 drain 所有已接受的 persistence task，同時記錄關機等待指標。</summary>
        private async Task StopCoreAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            Task drainTask = _rotationLifecycle.StopAcceptingAndDrainAsync();
            bool isDraining = _rotationLifecycle.ActiveOperationCount > 0 ||
                _rotationLifecycle.PendingPersistenceCount > 0;
            if (isDraining)
            {
                _metrics.SetTwitchRefreshShutdownDraining(true);
                Log.Warn($"Twitch token 服務正在等待 refresh rotation 保存完成: active={_rotationLifecycle.ActiveOperationCount}, pending={_rotationLifecycle.PendingPersistenceCount}");
            }

            try
            {
                await drainTask;
            }
            finally
            {
                stopwatch.Stop();
                _metrics.SetTwitchRefreshShutdownDraining(false);
                _metrics.ObserveTwitchRefreshShutdownDrainDuration(stopwatch.Elapsed);
            }

            if (isDraining)
                Log.Info($"Twitch refresh rotation 已全部保存，關閉等待 {stopwatch.Elapsed.TotalSeconds:F1} 秒");
        }

        private async Task TryUpdateValidationMetadataAsync(
            PendingRefreshPersistence pending,
            TwitchValidateTokenData validation,
            CancellationToken cancellationToken)
        {
            try
            {
                if (!await EnsureLockOwnedAsync(pending.Lease, "refresh_validation_persistence", cancellationToken))
                    return;

                DateTime validatedAt = DateTime.UtcNow;
                string serializedScopes = JsonConvert.SerializeObject(validation.Scopes ?? Array.Empty<string>());
                using var db = _dbService.GetDbContext();
                await db.TwitchBroadcasterAuthorization
                    .Where(x => x.TwitchUserId == pending.TwitchUserId &&
                        x.RevokedAt == null &&
                        x.EncryptedAccessToken == pending.ReplacementCiphertext)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.UserLogin, validation.Login)
                        .SetProperty(x => x.Scopes, serializedScopes)
                        .SetProperty(x => x.TokenExpiresAt, validatedAt.AddSeconds(validation.ExpiresIn))
                        .SetProperty(x => x.LastValidatedAt, validatedAt)
                        .SetProperty(x => x.DateUpdated, validatedAt), cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Warn($"Twitch refresh token 已保存，但驗證 metadata 更新暫時失敗: {ex.GetType().Name}");
            }
        }

        /// <summary>以目前密文與 lease owner 為條件標記失效，拒絕 stale refresh 對較新授權做破壞性更新。</summary>
        private async Task<bool> MarkInvalidIfCurrentAsync(
            string twitchUserId,
            string expectedCiphertext,
            string reason,
            TwitchOAuthRefreshLockLease lease,
            CancellationToken cancellationToken)
        {
            if (!await EnsureLockOwnedAsync(lease, "authorization_invalidation", cancellationToken))
                return false;

            DateTime now = DateTime.UtcNow;
            using var db = _dbService.GetDbContext();
            int updated = await db.TwitchBroadcasterAuthorization
                .Where(x => x.TwitchUserId == twitchUserId &&
                    x.RevokedAt == null &&
                    x.EncryptedAccessToken == expectedCiphertext)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.EncryptedAccessToken, (string)null)
                    .SetProperty(x => x.TokenExpiresAt, (DateTime?)null)
                    .SetProperty(x => x.RevokedAt, now)
                    .SetProperty(x => x.RevocationReason, reason)
                    .SetProperty(x => x.DateUpdated, now), cancellationToken);
            if (updated != 1)
                return false;

            string payload = JsonConvert.SerializeObject(new { TwitchUserId = twitchUserId, Status = "invalid" });
            try
            {
                await Bot.RedisSub.PublishAsync(
                    new RedisChannel(RedisChannels.Twitch.AuthorizationChanged, RedisChannel.PatternMode.Literal),
                    payload);
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException or InvalidOperationException)
            {
                Log.Warn($"Twitch 授權失效狀態已保存，但 Redis 通知暫時失敗: {ex.GetType().Name}");
            }
            return true;
        }

        private static async Task<bool> EnsureLockOwnedAsync(
            TwitchOAuthRefreshLockLease lease,
            string operation,
            CancellationToken cancellationToken)
        {
            var ownership = await lease.EnsureOwnedAsync(cancellationToken);
            if (ownership.Status == TwitchOAuthRefreshLockOwnershipStatus.Owned)
                return true;

            if (ownership.Status == TwitchOAuthRefreshLockOwnershipStatus.OwnershipLost)
                Log.Warn($"Twitch refresh lock owner 已變更，停止寫入: {operation}");
            else
                Log.Warn($"Twitch refresh lock 無法確認 owner，停止寫入: {operation} / {ownership.Exception?.GetType().Name}");
            return false;
        }

        private static TwitchSubscriptionStatus MapLocalState(TwitchAuthorizationLocalState state)
            => state switch
            {
                TwitchAuthorizationLocalState.Missing => TwitchSubscriptionStatus.AuthorizationMissing,
                TwitchAuthorizationLocalState.PersistedInvalid => TwitchSubscriptionStatus.AuthorizationInvalid,
                TwitchAuthorizationLocalState.Active => TwitchSubscriptionStatus.Subscribed,
                _ => TwitchSubscriptionStatus.TemporaryFailure
            };

        private static TwitchAuthorizationAccessResult Status(TwitchSubscriptionStatus status, string twitchUserId = null)
            => new() { Status = status, TwitchUserId = twitchUserId };

        private static TwitchAuthorizationAccessResult Success(TwitchBroadcasterAuthorization entity, string accessToken)
            => new()
            {
                Status = TwitchSubscriptionStatus.Subscribed,
                AccessToken = accessToken,
                TwitchUserId = entity.TwitchUserId,
                UserLogin = entity.UserLogin,
                DisplayName = entity.DisplayName
            };

        private sealed record PendingRefreshPersistence(
            string TwitchUserId,
            string ExpectedCiphertext,
            string ReplacementCiphertext,
            DateTime? TokenExpiresAt,
            DateTime DateUpdated,
            TwitchOAuthRefreshLockLease Lease);
    }
}
