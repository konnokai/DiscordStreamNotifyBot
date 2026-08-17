using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.SharedService.Member;

namespace DiscordStreamNotifyBot.SharedService.TwitchSubscription
{
    public sealed class TwitchRoleConfigurationResult
    {
        public bool IsSuccess { get; init; }
        public bool IsNew { get; init; }
        public string Error { get; init; }
        public GuildTwitchSubscriptionConfig Config { get; init; }
    }

    public sealed class TwitchSubscriptionRoleService
    {
        private readonly MainDbService _dbService;
        private readonly DiscordSocketClient _client;
        private readonly NotifierMetrics _metrics;
        private readonly MemberOperationCoordinator _operationCoordinator;
        private readonly MemberRoleOwnershipService _roleOwnershipService;

        public TwitchSubscriptionRoleService(
            MainDbService dbService,
            DiscordSocketClient client,
            NotifierMetrics metrics,
            MemberOperationCoordinator operationCoordinator,
            MemberRoleOwnershipService roleOwnershipService)
        {
            _dbService = dbService;
            _client = client;
            _metrics = metrics;
            _operationCoordinator = operationCoordinator;
            _roleOwnershipService = roleOwnershipService;
        }

        /// <summary>建立或修復 Twitch 驗證設定與 Tier 角色，先保存可重試 checkpoint，再同步既有成員。</summary>
        public async Task<TwitchRoleConfigurationResult> CreateOrRepairConfigurationAsync(
            SocketGuild guild,
            string broadcasterId,
            string broadcasterLogin,
            string broadcasterDisplayName,
            IRole subscriberRole,
            CancellationToken cancellationToken)
        {
            await using var guildLock = await _operationCoordinator.LockGuildAsync(guild.Id, cancellationToken);
            using var db = _dbService.GetDbContext();
            var existingConfigs = await db.GuildTwitchSubscriptionConfig
                .Where(x => x.GuildId == guild.Id)
                .ToListAsync(cancellationToken);
            var config = existingConfigs.SingleOrDefault(x => x.BroadcasterId == broadcasterId);
            bool isNew = config == null;
            if (!TwitchSubscriptionConfigurationPolicy.CanSaveConfiguration(
                existingConfigs.Count(x => !x.DeletionPending), !isNew))
                return new TwitchRoleConfigurationResult { Error = "TwitchMemberSetting.Errors.TooManyChannels" };

            if (!isNew)
            {
                string stateError = TwitchSubscriptionConfigurationPolicy.ValidateUpdateState(config, subscriberRole.Id);
                if (stateError != null)
                    return new TwitchRoleConfigurationResult { Error = stateError };
            }

            string validationError = ValidateSubscriberRole(guild, subscriberRole);
            if (validationError != null)
                return new TwitchRoleConfigurationResult { Error = validationError };
            if ((isNew || config.SubscriberRoleId != subscriberRole.Id) &&
                await _roleOwnershipService.IsRoleReferencedByYoutubeConfigurationAsync(
                    guild.Id, subscriberRole.Id, cancellationToken))
            {
                return new TwitchRoleConfigurationResult { Error = "TwitchMemberSetting.Errors.CrossPlatformRoleCollision" };
            }

            validationError = TwitchSubscriptionConfigurationPolicy.ValidateCommonRole(
                subscriberRole.Id,
                existingConfigs);
            if (validationError != null)
                return new TwitchRoleConfigurationResult { Error = validationError };

            config ??= new GuildTwitchSubscriptionConfig
            {
                GuildId = guild.Id,
                BroadcasterId = broadcasterId,
                DateAdded = DateTime.UtcNow
            };
            ulong oldSubscriberRoleId = config.SubscriberRoleId;
            ulong[] oldTierRoleIds = [config.Tier1RoleId, config.Tier2RoleId, config.Tier3RoleId];

            config.BroadcasterLogin = broadcasterLogin;
            config.BroadcasterDisplayName = broadcasterDisplayName;
            config.SubscriberRoleId = subscriberRole.Id;
            // 舊共用角色 ID 是跨失敗重試的 repair checkpoint；未完成前禁止再切到第三個角色。
            if (oldSubscriberRoleId != 0 && oldSubscriberRoleId != config.SubscriberRoleId)
                config.PreviousSubscriberRoleId ??= oldSubscriberRoleId;

            var createdRoles = new List<IRole>();
            bool configurationPersisted = false;
            string policyError = null;
            try
            {
                config.Tier1RoleId = await EnsureTierRoleExistsAsync(guild, config.Tier1RoleId,
                    TwitchSubscriptionRolePolicy.GetTierRoleName(subscriberRole.Name, "1000"), createdRoles, cancellationToken);
                config.Tier2RoleId = await EnsureTierRoleExistsAsync(guild, config.Tier2RoleId,
                    TwitchSubscriptionRolePolicy.GetTierRoleName(subscriberRole.Name, "2000"), createdRoles, cancellationToken);
                config.Tier3RoleId = await EnsureTierRoleExistsAsync(guild, config.Tier3RoleId,
                    TwitchSubscriptionRolePolicy.GetTierRoleName(subscriberRole.Name, "3000"), createdRoles, cancellationToken);

                validationError = TwitchSubscriptionConfigurationPolicy.ValidateResultingRoleSet(
                    config.Id,
                    config.SubscriberRoleId,
                    [config.Tier1RoleId, config.Tier2RoleId, config.Tier3RoleId],
                    existingConfigs);
                if (validationError != null)
                {
                    policyError = validationError;
                    throw new InvalidOperationException("Twitch 訂閱驗證身分組設定違反重疊規則。");
                }

                if (!CanManageRole(guild, subscriberRole.Id) ||
                    new[] { config.Tier1RoleId, config.Tier2RoleId, config.Tier3RoleId }.Any(x =>
                        createdRoles.All(role => role.Id != x) && !CanManageRole(guild, x)))
                {
                    throw new InvalidOperationException("Bot 無法管理 Twitch 訂閱驗證所需的身分組。");
                }

                if (isNew)
                    db.GuildTwitchSubscriptionConfig.Add(config);
                await db.SaveChangesAsync(cancellationToken);
                configurationPersisted = true;

                await EnsureTierRoleNameAsync(guild, config.Tier1RoleId,
                    TwitchSubscriptionRolePolicy.GetTierRoleName(subscriberRole.Name, "1000"), cancellationToken);
                await EnsureTierRoleNameAsync(guild, config.Tier2RoleId,
                    TwitchSubscriptionRolePolicy.GetTierRoleName(subscriberRole.Name, "2000"), cancellationToken);
                await EnsureTierRoleNameAsync(guild, config.Tier3RoleId,
                    TwitchSubscriptionRolePolicy.GetTierRoleName(subscriberRole.Name, "3000"), cancellationToken);
                await PositionTierRolesAsync(guild, subscriberRole, config, cancellationToken);

                bool rolesChanged = oldSubscriberRoleId != config.SubscriberRoleId ||
                    !oldTierRoleIds.SequenceEqual([config.Tier1RoleId, config.Tier2RoleId, config.Tier3RoleId]);
                if (!isNew && (rolesChanged || config.PreviousSubscriberRoleId.HasValue))
                {
                    // 設定已先落盤，後續失敗可由相同設定重跑；所有已驗證成員換角完成後才能清除 checkpoint。
                    var checks = await db.TwitchSubscriptionCheck.AsNoTracking()
                        .Where(x => x.GuildId == guild.Id && x.BroadcasterId == broadcasterId && x.IsChecked)
                        .ToListAsync(cancellationToken);
                    MemberRoleOwnershipSnapshot ownership = await _roleOwnershipService.LoadSnapshotAsync(
                        guild.Id, cancellationToken);
                    foreach (var check in checks)
                    {
                        if (!await SynchronizeSubscribedRolesAsync(
                            config, check.DiscordUserId, check.Tier, ownership, cancellationToken))
                            throw new InvalidOperationException("更新已驗證成員的 Twitch 訂閱身分組失敗。");
                    }
                    config.PreviousSubscriberRoleId = null;
                    await db.SaveChangesAsync(cancellationToken);
                }
                return new TwitchRoleConfigurationResult { IsSuccess = true, IsNew = isNew, Config = config };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (TwitchSubscriptionConfigurationPolicy.ShouldCompensateCreatedRoles(configurationPersisted))
                {
                    foreach (IRole role in createdRoles)
                    {
                        try { await role.DeleteAsync(new RequestOptions { CancelToken = cancellationToken }); }
                        catch (Exception cleanupException)
                        {
                            Log.Warn($"補償刪除 Twitch Tier 身分組失敗: {role.Id} / {cleanupException.GetType().Name}");
                        }
                    }
                }
                Log.Error(ex.Demystify(), "建立或修復 Twitch 訂閱身分組失敗");
                return new TwitchRoleConfigurationResult
                {
                    Error = configurationPersisted
                        ? "TwitchMemberSetting.Errors.RepairPending"
                        : policyError ?? "TwitchMemberSetting.Errors.SaveFailed"
                };
            }
        }

        /// <summary>修復缺少的層級身分組後同步成員角色，並拒絕 deletion-pending 設定重新授權。</summary>
        public async Task<bool> SynchronizeSubscribedRolesAsync(
            GuildTwitchSubscriptionConfig config,
            ulong discordUserId,
            string tier,
            CancellationToken cancellationToken)
        {
            MemberRoleOwnershipSnapshot ownership = await _roleOwnershipService.LoadSnapshotAsync(
                config.GuildId, cancellationToken);
            return await SynchronizeSubscribedRolesAsync(
                config, discordUserId, tier, ownership, cancellationToken);
        }

        internal async Task<bool> SynchronizeSubscribedRolesAsync(
            GuildTwitchSubscriptionConfig config,
            ulong discordUserId,
            string tier,
            MemberRoleOwnershipSnapshot ownership,
            CancellationToken cancellationToken)
        {
            if (config.DeletionPending)
                return false;
            SocketGuild guild = _client.GetGuild(config.GuildId);
            if (guild == null || !CanManageRoles(guild))
                return false;
            SocketRole subscriberRole = guild.GetRole(config.SubscriberRoleId);
            if (subscriberRole == null || !CanManageRole(guild, subscriberRole.Id))
                return false;
            if (tier is not ("1000" or "2000" or "3000"))
                return false;
            ulong tierRoleId = TwitchSubscriptionRolePolicy.GetTierRoleId(config, tier);
            if (tierRoleId != 0 && guild.GetRole(tierRoleId) != null && !CanManageRole(guild, tierRoleId))
                return false;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await RepairMissingTierRolesAsync(guild, config, subscriberRole, cancellationToken);
                tierRoleId = TwitchSubscriptionRolePolicy.GetTierRoleId(config, tier);

                var options = new RequestOptions { CancelToken = cancellationToken };
                IGuildUser member = await ((IGuild)guild).GetUserAsync(
                    discordUserId, CacheMode.AllowDownload, options);
                if (member == null)
                    return false;
                HashSet<ulong> currentRoleIds = member.RoleIds.ToHashSet();
                var diff = TwitchSubscriptionRolePolicy.GetSynchronizationDiff(config, tier, currentRoleIds);
                foreach (ulong roleId in diff.AddRoleIds)
                    await _client.Rest.AddRoleAsync(guild.Id, discordUserId, roleId, options);
                foreach (ulong roleId in diff.RemoveRoleIds)
                {
                    if (guild.GetRole(roleId) == null)
                        continue;
                    if (ownership.HasOtherActiveEntitlement(
                        discordUserId,
                        roleId,
                        MemberEntitlementProvider.Twitch,
                        config.BroadcasterId))
                        continue;
                    if (!CanManageRole(guild, roleId))
                        return false;
                    await _client.Rest.RemoveRoleAsync(guild.Id, discordUserId, roleId, options);
                }
                if (config.PreviousSubscriberRoleId is ulong previousRoleId &&
                    previousRoleId != config.SubscriberRoleId &&
                    currentRoleIds.Contains(previousRoleId) &&
                    !await RemoveObsoleteSharedRoleAsync(
                        guild,
                        discordUserId,
                        previousRoleId,
                        config.BroadcasterId,
                        ownership,
                        cancellationToken))
                {
                    return false;
                }
                _metrics.RecordTwitchSubscriptionRoleOperation(TwitchSubscriptionRoleOperation.Synchronize, TwitchSubscriptionRoleResult.Success);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                RecordRoleFailure(TwitchSubscriptionRoleOperation.Synchronize, ex);
                Log.Warn($"同步 Twitch 訂閱身分組失敗: {guild.Id} / {discordUserId} / {ex.GetType().Name}");
                return false;
            }
        }

        private async Task RepairMissingTierRolesAsync(
            SocketGuild guild,
            GuildTwitchSubscriptionConfig config,
            IRole subscriberRole,
            CancellationToken cancellationToken)
        {
            if (!TwitchSubscriptionRolePolicy.HasMissingTierRole(config, id => guild.GetRole(id) != null))
                return;

            ulong[] previousRoleIds = [config.Tier1RoleId, config.Tier2RoleId, config.Tier3RoleId];
            var createdRoles = new List<IRole>();
            bool persisted = false;
            try
            {
                config.Tier1RoleId = await EnsureTierRoleExistsAsync(guild, config.Tier1RoleId,
                    TwitchSubscriptionRolePolicy.GetTierRoleName(subscriberRole.Name, "1000"), createdRoles, cancellationToken);
                config.Tier2RoleId = await EnsureTierRoleExistsAsync(guild, config.Tier2RoleId,
                    TwitchSubscriptionRolePolicy.GetTierRoleName(subscriberRole.Name, "2000"), createdRoles, cancellationToken);
                config.Tier3RoleId = await EnsureTierRoleExistsAsync(guild, config.Tier3RoleId,
                    TwitchSubscriptionRolePolicy.GetTierRoleName(subscriberRole.Name, "3000"), createdRoles, cancellationToken);

                using var db = _dbService.GetDbContext();
                var persistedConfig = await db.GuildTwitchSubscriptionConfig.SingleOrDefaultAsync(
                    x => x.GuildId == config.GuildId && x.BroadcasterId == config.BroadcasterId,
                    cancellationToken) ?? throw new InvalidOperationException("找不到 Twitch 訂閱驗證設定。");
                persistedConfig.Tier1RoleId = config.Tier1RoleId;
                persistedConfig.Tier2RoleId = config.Tier2RoleId;
                persistedConfig.Tier3RoleId = config.Tier3RoleId;
                await db.SaveChangesAsync(cancellationToken);
                persisted = true;

                await PositionTierRolesAsync(guild, subscriberRole, config, cancellationToken);
                Log.Info($"已重建 Twitch 訂閱層級身分組: {guild.Id} / {config.BroadcasterId}");
            }
            catch
            {
                if (!persisted)
                {
                    config.Tier1RoleId = previousRoleIds[0];
                    config.Tier2RoleId = previousRoleIds[1];
                    config.Tier3RoleId = previousRoleIds[2];
                    foreach (IRole role in createdRoles)
                    {
                        try { await role.DeleteAsync(new RequestOptions { CancelToken = cancellationToken }); }
                        catch (Exception cleanupException)
                        {
                            Log.Warn($"補償刪除 Twitch 訂閱層級身分組失敗: {role.Id} / {cleanupException.GetType().Name}");
                        }
                    }
                }
                throw;
            }
        }

        /// <summary>移除指定設定授予的 Tier 角色；共用角色仍有其他有效 entitlement 時予以保留。</summary>
        public async Task<bool> RemoveSubscriptionRolesAsync(
            GuildTwitchSubscriptionConfig config,
            ulong discordUserId,
            CancellationToken cancellationToken)
        {
            MemberRoleOwnershipSnapshot ownership = await _roleOwnershipService.LoadSnapshotAsync(
                config.GuildId, cancellationToken);
            return await RemoveSubscriptionRolesAsync(config, discordUserId, ownership, cancellationToken);
        }

        internal async Task<bool> RemoveSubscriptionRolesAsync(
            GuildTwitchSubscriptionConfig config,
            ulong discordUserId,
            MemberRoleOwnershipSnapshot ownership,
            CancellationToken cancellationToken)
        {
            SocketGuild guild = _client.GetGuild(config.GuildId);
            if (guild == null)
                return Bot.ShouldDeleteMissingGuild(config.GuildId);
            if (!CanManageRoles(guild))
                return false;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var options = new RequestOptions { CancelToken = cancellationToken };
                foreach (ulong roleId in new[] { config.Tier1RoleId, config.Tier2RoleId, config.Tier3RoleId }.Where(x => x != 0).Distinct())
                {
                    if (guild.GetRole(roleId) == null)
                        continue;
                    if (ownership.HasOtherActiveEntitlement(
                        discordUserId,
                        roleId,
                        MemberEntitlementProvider.Twitch,
                        config.BroadcasterId))
                        continue;
                    if (!CanManageRole(guild, roleId))
                        return false;
                    await _client.Rest.RemoveRoleAsync(guild.Id, discordUserId, roleId, options);
                }

                if (!ownership.HasOtherActiveEntitlement(
                        discordUserId,
                        config.SubscriberRoleId,
                        MemberEntitlementProvider.Twitch,
                        config.BroadcasterId) &&
                    guild.GetRole(config.SubscriberRoleId) != null)
                {
                    if (!CanManageRole(guild, config.SubscriberRoleId))
                        return false;
                    await _client.Rest.RemoveRoleAsync(guild.Id, discordUserId, config.SubscriberRoleId, options);
                }

                _metrics.RecordTwitchSubscriptionRoleOperation(TwitchSubscriptionRoleOperation.Remove, TwitchSubscriptionRoleResult.Success);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Discord.Net.HttpException ex) when (ex.DiscordCode is DiscordErrorCode.UnknownMember or DiscordErrorCode.UnknownUser or DiscordErrorCode.UnknownAccount)
            {
                _metrics.RecordTwitchSubscriptionRoleOperation(TwitchSubscriptionRoleOperation.Remove, TwitchSubscriptionRoleResult.UserMissing);
                return true;
            }
            catch (Exception ex)
            {
                RecordRoleFailure(TwitchSubscriptionRoleOperation.Remove, ex);
                Log.Warn($"移除 Twitch 訂閱身分組失敗: {guild.Id} / {discordUserId} / {ex.GetType().Name}");
                return false;
            }
        }

        /// <summary>供 guild lock 內的批次同步與孤兒對帳共用同一份 ownership snapshot。</summary>
        internal Task<MemberRoleOwnershipSnapshot> LoadOwnershipSnapshotAsync(
            ulong guildId,
            CancellationToken cancellationToken)
            => _roleOwnershipService.LoadSnapshotAsync(guildId, cancellationToken);

        /// <summary>持久化刪除意圖後清理成員與系統 Tier 角色；任何 Discord 失敗皆保留設定供排程重試。</summary>
        public async Task<bool> DeleteConfigurationAsync(
            GuildTwitchSubscriptionConfig config,
            CancellationToken cancellationToken)
        {
            await using var guildLock = await _operationCoordinator.LockGuildAsync(config.GuildId, cancellationToken);
            using var db = _dbService.GetDbContext();
            config = await db.GuildTwitchSubscriptionConfig.SingleOrDefaultAsync(
                x => x.GuildId == config.GuildId && x.BroadcasterId == config.BroadcasterId,
                cancellationToken);
            if (config == null)
                return true;

            var checks = await db.TwitchSubscriptionCheck
                .Where(x => x.GuildId == config.GuildId && x.BroadcasterId == config.BroadcasterId)
                .ToListAsync(cancellationToken);
            // 先持久化刪除意圖再碰 Discord；失敗時驗證流程會停止授權，排程可由此 checkpoint 接續。
            foreach (var check in checks)
            {
                check.IsChecked = false;
                check.PendingRoleRemoval = true;
            }
            config.DeletionPending = true;
            await db.SaveChangesAsync(cancellationToken);

            MemberRoleOwnershipSnapshot ownership = await _roleOwnershipService.LoadSnapshotAsync(
                config.GuildId, cancellationToken);
            bool allRemoved = true;
            foreach (var check in checks)
            {
                bool removed = await RemoveSubscriptionRolesAsync(
                    config, check.DiscordUserId, ownership, cancellationToken);
                SocketGuild currentGuild = _client.GetGuild(config.GuildId);
                if (removed && currentGuild != null && config.PreviousSubscriberRoleId is ulong previousRoleId)
                {
                    removed = await RemoveObsoleteSharedRoleAsync(
                        currentGuild,
                        check.DiscordUserId,
                        previousRoleId,
                        config.BroadcasterId,
                        ownership,
                        cancellationToken);
                }
                allRemoved &= removed;
            }
            if (!allRemoved)
                return false;

            SocketGuild guild = _client.GetGuild(config.GuildId);
            if (guild != null)
            {
                try
                {
                    var protectedConfigs = await db.GuildTwitchSubscriptionConfig.AsNoTracking()
                        .Where(x => x.GuildId == config.GuildId && x.Id != config.Id)
                        .ToArrayAsync(cancellationToken);
                    HashSet<ulong> protectedRoles = protectedConfigs
                        .SelectMany(x => new[]
                        {
                            x.SubscriberRoleId,
                            x.PreviousSubscriberRoleId ?? 0,
                            x.Tier1RoleId,
                            x.Tier2RoleId,
                            x.Tier3RoleId
                        })
                        .ToHashSet();
                    foreach (ulong roleId in new[] { config.Tier1RoleId, config.Tier2RoleId, config.Tier3RoleId }.Where(x => x != 0).Distinct())
                    {
                        if (protectedRoles.Contains(roleId) || !ownership.CanDeleteTwitchTierRole(roleId))
                            continue;
                        IRole role = guild.GetRole(roleId);
                        if (role != null)
                        {
                            if (!CanManageRole(guild, roleId))
                                return false;
                            await role.DeleteAsync(new RequestOptions { CancelToken = cancellationToken });
                        }
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    RecordRoleFailure(TwitchSubscriptionRoleOperation.Remove, ex);
                    Log.Warn($"刪除 Twitch Tier 身分組失敗: {config.GuildId} / {ex.GetType().Name}");
                    return false;
                }
            }

            db.TwitchSubscriptionCheck.RemoveRange(checks);
            var trackedConfig = await db.GuildTwitchSubscriptionConfig
                .SingleOrDefaultAsync(x => x.Id == config.Id, cancellationToken);
            if (trackedConfig != null)
                db.GuildTwitchSubscriptionConfig.Remove(trackedConfig);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> RemoveOrphanRoleAsync(
            SocketGuild guild,
            ulong discordUserId,
            ulong roleId,
            MemberRoleOwnershipSnapshot ownership,
            CancellationToken cancellationToken)
        {
            if (guild.GetRole(roleId) == null)
                return true;
            if (ownership.HasOtherActiveEntitlement(discordUserId, roleId))
                return true;
            if (!CanManageRole(guild, roleId))
                return false;
            try
            {
                await _client.Rest.RemoveRoleAsync(
                    guild.Id,
                    discordUserId,
                    roleId,
                    new RequestOptions { CancelToken = cancellationToken });
                _metrics.RecordTwitchSubscriptionRoleOperation(
                    TwitchSubscriptionRoleOperation.Remove,
                    TwitchSubscriptionRoleResult.Success);
                return true;
            }
            catch (Discord.Net.HttpException ex) when (ex.DiscordCode is DiscordErrorCode.UnknownMember or DiscordErrorCode.UnknownUser or DiscordErrorCode.UnknownAccount)
            {
                _metrics.RecordTwitchSubscriptionRoleOperation(
                    TwitchSubscriptionRoleOperation.Remove,
                    TwitchSubscriptionRoleResult.UserMissing);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                RecordRoleFailure(TwitchSubscriptionRoleOperation.Remove, ex);
                return false;
            }
        }

        private async Task<ulong> EnsureTierRoleExistsAsync(
            SocketGuild guild,
            ulong roleId,
            string expectedName,
            ICollection<IRole> createdRoles,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IRole role = roleId == 0 ? null : guild.GetRole(roleId);
            if (role == null)
            {
                role = await guild.CreateRoleAsync(
                    expectedName,
                    GuildPermissions.None,
                    color: null,
                    isHoisted: false,
                    isMentionable: false,
                    options: new RequestOptions { CancelToken = cancellationToken });
                createdRoles.Add(role);
            }
            return role.Id;
        }

        private async Task EnsureTierRoleNameAsync(
            SocketGuild guild,
            ulong roleId,
            string expectedName,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IRole role = guild.GetRole(roleId)
                ?? throw new InvalidOperationException("找不到已保存的 Twitch Tier 身分組。");
            if (role.Name != expectedName)
            {
                if (!CanManageRole(guild, role.Id))
                    throw new InvalidOperationException("Bot 無法管理既有的 Twitch Tier 身分組。");
                await role.ModifyAsync(properties => properties.Name = expectedName,
                    new RequestOptions { CancelToken = cancellationToken });
            }
        }

        private static async Task PositionTierRolesAsync(
            SocketGuild guild,
            IRole subscriberRole,
            GuildTwitchSubscriptionConfig config,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int basePosition = Math.Max(1, subscriberRole.Position - 1);
            await guild.ReorderRolesAsync(
            [
                new ReorderRoleProperties(config.Tier1RoleId, basePosition),
                new ReorderRoleProperties(config.Tier2RoleId, Math.Max(1, basePosition - 1)),
                new ReorderRoleProperties(config.Tier3RoleId, Math.Max(1, basePosition - 2))
            ], new RequestOptions { CancelToken = cancellationToken });
        }

        /// <summary>在不存在其他 entitlement 時移除 repair checkpoint 指向的舊共用角色。</summary>
        private async Task<bool> RemoveObsoleteSharedRoleAsync(
            SocketGuild guild,
            ulong discordUserId,
            ulong oldSubscriberRoleId,
            string currentBroadcasterId,
            MemberRoleOwnershipSnapshot ownership,
            CancellationToken cancellationToken)
        {
            bool stillEntitled = ownership.HasOtherActiveEntitlement(
                discordUserId,
                oldSubscriberRoleId,
                MemberEntitlementProvider.Twitch,
                currentBroadcasterId);
            if (stillEntitled || guild.GetRole(oldSubscriberRoleId) == null)
                return true;
            if (!CanManageRole(guild, oldSubscriberRoleId))
                return false;

            try
            {
                await _client.Rest.RemoveRoleAsync(
                    guild.Id,
                    discordUserId,
                    oldSubscriberRoleId,
                    new RequestOptions { CancelToken = cancellationToken });
                return true;
            }
            catch (Discord.Net.HttpException ex) when (ex.DiscordCode is DiscordErrorCode.UnknownMember or DiscordErrorCode.UnknownUser or DiscordErrorCode.UnknownAccount)
            {
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                RecordRoleFailure(TwitchSubscriptionRoleOperation.Remove, ex);
                return false;
            }
        }

        private string ValidateSubscriberRole(SocketGuild guild, IRole role)
        {
            SocketGuildUser bot = guild.GetUser(_client.CurrentUser.Id);
            if (bot?.GuildPermissions.ManageRoles != true)
                return "TwitchMemberSetting.Errors.MissingManageRoles";
            if (role.Id == guild.EveryoneRole.Id)
                return "TwitchMemberSetting.Errors.EveryoneRole";
            if (role.IsManaged)
                return "TwitchMemberSetting.Errors.ManagedRole";
            int botHighestPosition = bot.Roles.Max(x => x.Position);
            if (role.Position >= botHighestPosition)
                return "TwitchMemberSetting.Errors.RoleTooHigh";
            return null;
        }

        private bool CanManageRoles(SocketGuild guild)
            => guild.GetUser(_client.CurrentUser.Id)?.GuildPermissions.ManageRoles == true;

        private bool CanManageRole(SocketGuild guild, ulong roleId)
        {
            SocketGuildUser bot = guild.GetUser(_client.CurrentUser.Id);
            SocketRole role = guild.GetRole(roleId);
            return bot?.GuildPermissions.ManageRoles == true &&
                role != null &&
                !role.IsManaged &&
                role.Id != guild.EveryoneRole.Id &&
                role.Position < bot.Roles.Max(x => x.Position);
        }

        private void RecordRoleFailure(TwitchSubscriptionRoleOperation operation, Exception exception)
        {
            TwitchSubscriptionRoleResult result = exception is Discord.Net.HttpException httpException &&
                httpException.DiscordCode is DiscordErrorCode.MissingPermissions or DiscordErrorCode.InsufficientPermissions
                    ? TwitchSubscriptionRoleResult.MissingPermission
                    : exception is Discord.Net.HttpException
                        ? TwitchSubscriptionRoleResult.DiscordError
                        : TwitchSubscriptionRoleResult.UnknownError;
            _metrics.RecordTwitchSubscriptionRoleOperation(operation, result);
        }
    }
}
