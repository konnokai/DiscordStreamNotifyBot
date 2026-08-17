using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Shared;

namespace DiscordStreamNotifyBot.SharedService.YoutubeMember
{
    public partial class YoutubeMemberService
    {
        public async Task CheckMemberShip(bool isOldCheck)
        {
            YoutubeMemberCheckType checkType = isOldCheck ? YoutubeMemberCheckType.Old : YoutubeMemberCheckType.New;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await CheckMemberShipCore(isOldCheck, GracefulShutdown.Token);
                _metrics.RecordYoutubeMemberCheckCycle(checkType, YoutubeMemberCheckCycleResult.Success);
            }
            catch
            {
                _metrics.RecordYoutubeMemberCheckCycle(checkType, YoutubeMemberCheckCycleResult.Failure);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _metrics.ObserveYoutubeMemberCheckDuration(checkType, stopwatch.Elapsed);
            }
        }

        /// <summary>先處理 durable cleanup；provider 停用、配額或本機憑證故障都不可撤銷既有 entitlement。</summary>
        private async Task CheckMemberShipCore(bool isOldCheck, CancellationToken cancellationToken)
        {
            YoutubeMemberCheckType checkType = isOldCheck ? YoutubeMemberCheckType.Old : YoutubeMemberCheckType.New;
            await _roleService.RetryPendingCleanupAsync(cancellationToken);
            if (!YoutubeMemberLifecyclePolicy.ShouldRunProviderCheck(IsEnable))
                return;

            List<GuildYoutubeMemberConfig> configurations;
            using (var db = _dbService.GetDbContext())
            {
                configurations = await db.GuildYoutubeMemberConfig.AsNoTracking()
                    .Where(config => !config.DeletionPending &&
                        !string.IsNullOrEmpty(config.MemberCheckChannelId) &&
                        !string.IsNullOrEmpty(config.MemberCheckChannelTitle) &&
                        config.MemberCheckVideoId != "-")
                    .ToListAsync(cancellationToken);
            }

            int total = 0;
            int members = 0;
            var authorizations = new Dictionary<ulong, YoutubeMemberAuthorizationResult>();
            var authorizationValidation = new Dictionary<ulong, YoutubeMemberProbeResult>();
            var probeResults = new Dictionary<(ulong UserId, string VideoId),
                (YoutubeMemberProbeResult Result, string EncryptedTokenPayload)>();
            var cleanedInvalidUsers = new HashSet<ulong>();
            const int splitDay = 3;
            foreach (GuildYoutubeMemberConfig configuration in configurations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!Bot.IsServerOnThisShard(configuration.GuildId) ||
                    (isOldCheck && (int)(configuration.GuildId % splitDay) != DateTime.Now.DayOfYear % splitDay))
                    continue;

                List<YoutubeMemberCheck> checks;
                GuildConfig guildConfig;
                using (var db = _dbService.GetDbContext())
                {
                    checks = await db.YoutubeMemberCheck.AsNoTracking()
                    .Where(check => check.GuildId == configuration.GuildId &&
                        check.CheckYTChannelId == configuration.MemberCheckChannelId &&
                        !check.PendingRoleRemoval &&
                        (isOldCheck ? check.IsChecked : !check.IsChecked))
                    .ToListAsync(cancellationToken);
                    guildConfig = await db.GuildConfig.AsNoTracking().FirstOrDefaultAsync(
                        x => x.GuildId == configuration.GuildId, cancellationToken);
                }
                if (checks.Count == 0)
                    continue;

                SocketGuild guild = _client.GetGuild(configuration.GuildId);
                if (guild == null)
                {
                    if (Bot.ShouldDeleteMissingGuild(configuration.GuildId))
                    {
                        Log.Warn($"{configuration.GuildId} Guild 不存在，保留設定等待既有刪除流程處理");
                    }
                    continue;
                }
                SocketTextChannel logChannel = guildConfig == null ? null : guild.GetTextChannel(guildConfig.VerificationLogChannelId);
                SocketRole role = guild.GetRole(configuration.MemberCheckGrantRoleId);
                if (!CanProcessConfiguration(guild, configuration, role, logChannel))
                    continue;

                string guildLocale = await _guildLocaleService.GetAsync(guild.Id, guild);
                YoutubeMemberProbeConfigurationSnapshot configurationSnapshot =
                    YoutubeMemberPolicies.CaptureProbeConfiguration(configuration);
                foreach (YoutubeMemberCheck check in checks)
                {
                    total++;
                    YoutubeMemberCheckStateSnapshot snapshot = YoutubeMemberPolicies.CaptureState(check);
                    string userLocale = _localeResolver.ResolveDelayedDirectMessage(check.Locale, guildLocale);
                    var key = YoutubeMemberPolicies.BuildProbeCacheKey(check.UserId, configuration.MemberCheckVideoId);
                    if (!probeResults.TryGetValue(key, out var probeExecution))
                    {
                        if (!authorizations.TryGetValue(check.UserId, out YoutubeMemberAuthorizationResult authorization))
                        {
                            authorization = await _authorizationService.GetCredentialAsync(
                                check.UserId.ToString(), cancellationToken);
                            authorizations[check.UserId] = authorization;
                        }
                        if (authorization.Status == YoutubeMemberAuthorizationStatus.Ready)
                        {
                            if (!authorizationValidation.TryGetValue(check.UserId, out YoutubeMemberProbeResult validation))
                            {
                                validation = await _apiClient.ValidateAuthorizationAsync(
                                    authorization.Credential, cancellationToken);
                                authorizationValidation[check.UserId] = validation;
                            }
                            YoutubeMemberProbeResult probeResult = validation.Kind == YoutubeMemberProbeResultKind.Member
                                ? await _apiClient.ProbeAsync(authorization.Credential, configuration.MemberCheckVideoId,
                                    authorizationValidated: true, cancellationToken: cancellationToken)
                                : validation;
                            probeExecution = new(probeResult, authorization.EncryptedTokenPayload);
                        }
                        else
                        {
                            probeExecution = new(authorization.Status switch
                            {
                                YoutubeMemberAuthorizationStatus.AuthorizationInvalid => new(YoutubeMemberProbeResultKind.AuthorizationInvalid),
                                YoutubeMemberAuthorizationStatus.TemporaryFailure => new(YoutubeMemberProbeResultKind.TemporaryFailure),
                                _ => new(YoutubeMemberProbeResultKind.LocalContractFailure)
                            }, authorization.EncryptedTokenPayload);
                        }
                        probeResults[key] = probeExecution;
                    }
                    YoutubeMemberProbeResult result = probeExecution.Result;

                    switch (result.Kind)
                    {
                        case YoutubeMemberProbeResultKind.Member:
                            _metrics.RecordYoutubeMemberVerification(checkType, YoutubeMemberVerificationResult.Member);
                            if (await ApplyMemberAsync(configurationSnapshot, check, snapshot,
                                    probeExecution.EncryptedTokenPayload, cancellationToken))
                            {
                                members++;
                                if (!isOldCheck)
                                    await SendVerifiedMessagesAsync(logChannel, guild, configuration, check.UserId, userLocale, guildLocale);
                            }
                            break;
                        case YoutubeMemberProbeResultKind.NotMember:
                            _metrics.RecordYoutubeMemberVerification(checkType, YoutubeMemberVerificationResult.NotMember);
                            YoutubeMemberNotMemberApplyResult notMember = await ApplyNotMemberAsync(
                                configurationSnapshot, check, snapshot, probeExecution.EncryptedTokenPayload,
                                cancellationToken);
                            if (notMember.Applied)
                                await NotifyNotMemberAsync(logChannel, guild, configuration, check.UserId, isOldCheck,
                                    notMember.WasChecked, userLocale, guildLocale);
                            break;
                        case YoutubeMemberProbeResultKind.AuthorizationInvalid:
                            _metrics.RecordYoutubeMemberVerification(checkType, YoutubeMemberVerificationResult.CredentialExpired);
                            // 僅明確 invalidation 進入 cleanup，且先 durable 標記所有 check 才刪本機 token。
                            if (!cleanedInvalidUsers.Contains(check.UserId) && await RemoveMemberCheckFromDbAsync(
                                    check.UserId, probeExecution.EncryptedTokenPayload, configurationSnapshot,
                                    snapshot, check.Id, cancellationToken))
                            {
                                cleanedInvalidUsers.Add(check.UserId);
                                await NotifyCredentialInvalidAsync(logChannel, configuration, check.UserId, userLocale, guildLocale);
                            }
                            break;
                        case YoutubeMemberProbeResultKind.ProbeVideoInvalid:
                            _metrics.RecordYoutubeMemberVerification(checkType, YoutubeMemberVerificationResult.VideoNotFound);
                            await MarkProbeVideoInvalidAsync(configurationSnapshot, check, snapshot, logChannel, guildLocale,
                                probeExecution.EncryptedTokenPayload, cancellationToken);
                            break;
                        case YoutubeMemberProbeResultKind.QuotaExceeded:
                            _metrics.RecordYoutubeMemberVerification(checkType, YoutubeMemberVerificationResult.QuotaExceeded);
                            Log.Warn($"YouTube 會員驗證配額不足: {configuration.GuildId} / {configuration.MemberCheckChannelId}");
                            break;
                        case YoutubeMemberProbeResultKind.RateLimited:
                        case YoutubeMemberProbeResultKind.TemporaryFailure:
                            _metrics.RecordYoutubeMemberVerification(checkType, YoutubeMemberVerificationResult.TemporaryFailure);
                            Log.Warn($"YouTube 會員驗證暫時失敗，保留既有 entitlement: {configuration.GuildId} / {check.UserId}");
                            break;
                        case YoutubeMemberProbeResultKind.LocalContractFailure:
                            _metrics.RecordYoutubeMemberVerification(checkType, YoutubeMemberVerificationResult.UnknownError);
                            Log.Warn($"YouTube 會員驗證本機契約失敗，保留既有 entitlement: {configuration.GuildId} / {check.UserId}");
                            break;
                    }
                }
            }

            if (total > 0)
                Log.Info((isOldCheck ? "舊" : "新") + $"會限檢查完畢，總驗證: {total} 位，成功驗證: {members} 位");
        }

        private bool CanProcessConfiguration(SocketGuild guild, GuildYoutubeMemberConfig configuration,
            SocketRole role, SocketTextChannel logChannel)
        {
            if (logChannel == null || role == null || role == guild.EveryoneRole || role.IsManaged)
            {
                Log.Warn($"YouTube 會員設定暫時無法處理，保留設定: {configuration.GuildId} / {configuration.MemberCheckChannelId}");
                return false;
            }
            var permission = guild.CurrentUser.GetPermissions(logChannel);
            if (!permission.ViewChannel || !permission.SendMessages || !permission.EmbedLinks ||
                !guild.CurrentUser.GuildPermissions.ManageRoles)
            {
                Log.Warn($"YouTube 會員設定缺少 Discord 權限，保留設定: {configuration.GuildId}");
                return false;
            }
            return true;
        }

        private async Task<bool> ApplyMemberAsync(YoutubeMemberProbeConfigurationSnapshot configurationSnapshot,
            YoutubeMemberCheck check, YoutubeMemberCheckStateSnapshot snapshot, string expectedEncryptedToken,
            CancellationToken cancellationToken)
        {
            await using var userLock = await _operationCoordinator.LockUserAsync(check.UserId, cancellationToken);
            await using var guildLock = await _operationCoordinator.LockGuildAsync(configurationSnapshot.GuildId, cancellationToken);
            using var db = _dbService.GetDbContext();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            YoutubeMemberAccessToken token = await LockTokenAsync(db, check.UserId, cancellationToken);
            YoutubeMemberCheck current = await LockCheckAsync(db, check.Id, cancellationToken);
            GuildYoutubeMemberConfig currentConfiguration = await LockConfigurationAsync(db, configurationSnapshot.Id,
                cancellationToken);
            if (!YoutubeMemberPolicies.CanApplyProviderResult(YoutubeMemberProbeResultKind.Member,
                    expectedEncryptedToken, token?.EncryptedAccessToken, snapshot, current, configurationSnapshot,
                    currentConfiguration))
                return false;
            if (
                !await _roleService.GrantAsync(currentConfiguration, check.UserId, cancellationToken))
            {
                _metrics.RecordYoutubeMemberRoleOperation(YoutubeMemberRoleOperation.Add, YoutubeMemberRoleResult.DiscordError);
                return false;
            }
            YoutubeMemberPolicies.MarkVerified(current);
            current.LastCheckTime = DateTime.Now;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _metrics.RecordYoutubeMemberRoleOperation(YoutubeMemberRoleOperation.Add, YoutubeMemberRoleResult.Success);
            return true;
        }

        private readonly record struct YoutubeMemberNotMemberApplyResult(bool Applied, bool WasChecked);

        private async Task<YoutubeMemberNotMemberApplyResult> ApplyNotMemberAsync(YoutubeMemberProbeConfigurationSnapshot configurationSnapshot,
            YoutubeMemberCheck check, YoutubeMemberCheckStateSnapshot snapshot, string expectedEncryptedToken,
            CancellationToken cancellationToken)
        {
            await using var userLock = await _operationCoordinator.LockUserAsync(check.UserId, cancellationToken);
            await using var guildLock = await _operationCoordinator.LockGuildAsync(configurationSnapshot.GuildId, cancellationToken);
            using var db = _dbService.GetDbContext();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            YoutubeMemberAccessToken token = await LockTokenAsync(db, check.UserId, cancellationToken);
            YoutubeMemberCheck current = await LockCheckAsync(db, check.Id, cancellationToken);
            GuildYoutubeMemberConfig currentConfiguration = await LockConfigurationAsync(db, configurationSnapshot.Id,
                cancellationToken);
            if (!YoutubeMemberPolicies.CanApplyProviderResult(YoutubeMemberProbeResultKind.NotMember,
                    expectedEncryptedToken, token?.EncryptedAccessToken, snapshot, current, configurationSnapshot,
                    currentConfiguration))
                return default;
            bool wasChecked = current.IsChecked;
            YoutubeMemberPolicies.QueueRoleRemoval(current);
            await db.SaveChangesAsync(cancellationToken);
            if (!await _roleService.RemoveAsync(currentConfiguration, check.UserId, cancellationToken))
            {
                _metrics.RecordYoutubeMemberRoleOperation(YoutubeMemberRoleOperation.Remove, YoutubeMemberRoleResult.DiscordError);
                return default;
            }
            db.YoutubeMemberCheck.Remove(current);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _metrics.RecordYoutubeMemberRoleOperation(YoutubeMemberRoleOperation.Remove, YoutubeMemberRoleResult.Success);
            return new(true, wasChecked);
        }

        private async Task NotifyNotMemberAsync(SocketTextChannel logChannel, SocketGuild guild,
            GuildYoutubeMemberConfig configuration, ulong userId, bool isOldCheck, bool wasChecked,
            string userLocale, string guildLocale)
        {
            string state = isOldCheck && wasChecked ? "Member.Status.MembershipExpired" : "Member.Status.NotMember";
            await logChannel.SendErrorMessageAsync(_client, userId, configuration.MemberCheckChannelTitle,
                _localizer.Get(state, guildLocale), _localizer, guildLocale);
            string checkPath = _commandDisplayResolver.GetCommandPath(userLocale, "youtube-member", "check");
            string cancelPath = _commandDisplayResolver.GetCommandPath(userLocale, "youtube-member", "cancel-member-check");
            string showPath = _commandDisplayResolver.GetCommandPath(userLocale, "youtube-member", "show-my-youtube-account");
            string message = isOldCheck && wasChecked
                ? _localizer.Format("Member.Background.MembershipExpired", userLocale, guild.Name,
                    configuration.MemberCheckChannelTitle, cancelPath, checkPath)
                : _localizer.Format("Member.Background.NotMember", userLocale, guild.Name,
                    configuration.MemberCheckChannelTitle, showPath, cancelPath, Bot.ApplicatonOwner);
            await userId.SendErrorMessageAsync(_client, message, logChannel, _localizer, guildLocale);
        }

        private async Task MarkProbeVideoInvalidAsync(YoutubeMemberProbeConfigurationSnapshot configurationSnapshot,
            YoutubeMemberCheck check, YoutubeMemberCheckStateSnapshot snapshot, SocketTextChannel logChannel,
            string guildLocale, string expectedEncryptedToken, CancellationToken cancellationToken)
        {
            await using var userLock = await _operationCoordinator.LockUserAsync(check.UserId, cancellationToken);
            await using var guildLock = await _operationCoordinator.LockGuildAsync(configurationSnapshot.GuildId, cancellationToken);
            using var db = _dbService.GetDbContext();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            YoutubeMemberAccessToken token = await LockTokenAsync(db, check.UserId, cancellationToken);
            YoutubeMemberCheck currentCheck = await LockCheckAsync(db, check.Id, cancellationToken);
            GuildYoutubeMemberConfig current = await LockConfigurationAsync(db, configurationSnapshot.Id,
                cancellationToken);
            if (!YoutubeMemberPolicies.CanApplyProviderResult(YoutubeMemberProbeResultKind.ProbeVideoInvalid,
                    expectedEncryptedToken, token?.EncryptedAccessToken, snapshot, currentCheck, configurationSnapshot,
                    current))
                return;
            string videoId = current.MemberCheckVideoId;
            current.MemberCheckVideoId = "-";
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            Log.Warn($"YouTube 會員探測影片失效，等待 Scraper 重新探索: {configurationSnapshot.GuildId} / {videoId}");
            if (current.IsManualVideoId)
            {
                string path = _commandDisplayResolver.GetCommandPath(guildLocale, "youtube-member-set", "set-check-video");
                await logChannel.SendMessageAsync(_localizer.Format("Member.Status.ManualVideoDeleted", guildLocale, videoId, path));
            }
        }

        private async Task NotifyCredentialInvalidAsync(SocketTextChannel logChannel, GuildYoutubeMemberConfig configuration,
            ulong userId, string userLocale, string guildLocale)
        {
            string website = Format.Url(_localizer.Get("Common.Website", userLocale), "https://stream-bot.konnokai.me/");
            string security = Format.Url(_localizer.Get("Common.GoogleSecurity", userLocale),
                "https://myaccount.google.com/permissions?continue=https%3A%2F%2Fmyaccount.google.com%2Fsecurity");
            string checkPath = _commandDisplayResolver.GetCommandPath(userLocale, "youtube-member", "check");
            await logChannel.SendErrorMessageAsync(_client, userId, configuration.MemberCheckChannelTitle,
                _localizer.Get("Member.Status.CredentialExpired", guildLocale), _localizer, guildLocale);
            await userId.SendErrorMessageAsync(_client, _localizer.Format("Member.Background.CredentialExpired",
                userLocale, security, website, checkPath), logChannel, _localizer, guildLocale);
        }

        private async Task SendVerifiedMessagesAsync(SocketTextChannel logChannel, SocketGuild guild,
            GuildYoutubeMemberConfig configuration, ulong userId, string userLocale, string guildLocale)
        {
            await logChannel.SendConfirmMessageAsync(_client, userId, new EmbedBuilder()
                .AddField(_localizer.Get("Member.Status.Channel", guildLocale), configuration.MemberCheckChannelTitle)
                .AddField(_localizer.Get("Member.Status.State", guildLocale), _localizer.Get("Member.Status.Verified", guildLocale)));
            await userId.SendConfirmMessageAsync(_client, _localizer.Format("Member.Background.Verified", userLocale,
                guild.Name, configuration.MemberCheckChannelTitle), logChannel, _localizer, guildLocale);
        }
    }
}
