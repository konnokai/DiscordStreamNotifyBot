using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.SharedService.Member;

namespace DiscordStreamNotifyBot.SharedService.YoutubeMember
{
    public sealed class YoutubeMemberRoleConfigurationResult
    {
        public bool IsSuccess { get; init; }
        public string Error { get; init; }
        public GuildYoutubeMemberConfig Config { get; init; }
    }

    /// <summary>
    /// YouTube 會限驗證所有 Discord 身分組操作的唯一入口。
    /// 設定變更和刪除先保存 checkpoint，Discord 暫時失敗時能由相同設定或週期清理安全續跑。
    /// </summary>
    public sealed class YoutubeMemberRoleService
    {
        private readonly MainDbService _dbService;
        private readonly DiscordSocketClient _client;
        private readonly MemberOperationCoordinator _operationCoordinator;
        private readonly MemberRoleOwnershipService _roleOwnershipService;

        public YoutubeMemberRoleService(
            MainDbService dbService,
            DiscordSocketClient client,
            MemberOperationCoordinator operationCoordinator,
            MemberRoleOwnershipService roleOwnershipService)
        {
            _dbService = dbService;
            _client = client;
            _operationCoordinator = operationCoordinator;
            _roleOwnershipService = roleOwnershipService;
        }

        /// <summary>建立設定或以 durable previous-role checkpoint 遷移既有設定。</summary>
        public async Task<YoutubeMemberRoleConfigurationResult> ConfigureRoleAsync(
            SocketGuild guild,
            string channelId,
            IRole requestedRole,
            CancellationToken cancellationToken)
        {
            await using var guildLock = await _operationCoordinator.LockGuildAsync(guild.Id, cancellationToken);
            using var db = _dbService.GetDbContext();
            var configs = await db.GuildYoutubeMemberConfig
                .Where(x => x.GuildId == guild.Id)
                .ToListAsync(cancellationToken);
            GuildYoutubeMemberConfig config = configs.SingleOrDefault(x => x.MemberCheckChannelId == channelId);
            string stateError = YoutubeMemberPolicies.ValidateRoleUpdateState(config, requestedRole.Id);
            if (stateError != null)
                return new YoutubeMemberRoleConfigurationResult { Error = stateError };

            string validationError = ValidateRole(guild, requestedRole);
            if (validationError != null)
                return new YoutubeMemberRoleConfigurationResult { Error = validationError };
            if ((config == null || config.MemberCheckGrantRoleId != requestedRole.Id) &&
                await _roleOwnershipService.IsRoleReferencedByTwitchConfigurationAsync(
                    guild.Id, requestedRole.Id, cancellationToken))
            {
                return new YoutubeMemberRoleConfigurationResult
                {
                    Error = "MemberSetting.Errors.CrossPlatformRoleCollision"
                };
            }

            bool isNew = config == null;
            if (isNew)
            {
                config = new GuildYoutubeMemberConfig
                {
                    GuildId = guild.Id,
                    MemberCheckChannelId = channelId,
                    MemberCheckGrantRoleId = requestedRole.Id
                };
                // 沿用相同 YouTube 頻道已探索出的 probe 資料，避免新 guild 再次等待 Scraper。
                var knownChannel = await db.GuildYoutubeMemberConfig.AsNoTracking()
                    .Where(x => x.MemberCheckChannelId == channelId &&
                        !string.IsNullOrEmpty(x.MemberCheckChannelTitle) && x.MemberCheckVideoId != "-")
                    .Select(x => new { x.MemberCheckChannelTitle, x.MemberCheckVideoId })
                    .FirstOrDefaultAsync(cancellationToken);
                if (knownChannel != null)
                {
                    config.MemberCheckChannelTitle = knownChannel.MemberCheckChannelTitle;
                    config.MemberCheckVideoId = knownChannel.MemberCheckVideoId;
                }
                db.GuildYoutubeMemberConfig.Add(config);
                await db.SaveChangesAsync(cancellationToken);
                return new YoutubeMemberRoleConfigurationResult { IsSuccess = true, Config = config };
            }

            ulong previousCurrentRoleId = config.MemberCheckGrantRoleId;
            if (previousCurrentRoleId != requestedRole.Id)
            {
                // 必須在任何 Discord mutation 前完成這次寫入；中斷後只能重試目前 target，不能累積第三個 role。
                YoutubeMemberPolicies.BeginRoleMigration(config, requestedRole.Id);
                await db.SaveChangesAsync(cancellationToken);
            }

            if (!config.PreviousMemberCheckGrantRoleId.HasValue)
                return new YoutubeMemberRoleConfigurationResult { IsSuccess = true, Config = config };

            var migrationChecks = (await db.YoutubeMemberCheck.AsNoTracking()
                .Where(x => x.GuildId == guild.Id && x.CheckYTChannelId == channelId)
                .ToArrayAsync(cancellationToken))
                .Where(YoutubeMemberPolicies.RequiresRoleMigration)
                .ToArray();
            MemberRoleOwnershipSnapshot ownership = await _roleOwnershipService.LoadSnapshotAsync(guild.Id, cancellationToken);
            bool synchronized = true;
            foreach (YoutubeMemberCheck check in migrationChecks)
            {
                if (YoutubeMemberPolicies.IsActive(check))
                {
                    YoutubeMemberRoleApplyResult grantResult = await GrantForMigrationAsync(
                        config, check.UserId, cancellationToken);
                    synchronized &= YoutubeMemberPolicies.IsRoleMigrationSynchronized(grantResult);
                }
                synchronized &= await RemoveRoleIdsAsync(
                    config,
                    check.UserId,
                    [config.PreviousMemberCheckGrantRoleId.Value],
                    ownership,
                    cancellationToken);
            }

            if (synchronized)
            {
                config.PreviousMemberCheckGrantRoleId = null;
                await db.SaveChangesAsync(cancellationToken);
            }
            return new YoutubeMemberRoleConfigurationResult
            {
                IsSuccess = synchronized,
                Error = synchronized ? null : "MemberSetting.Errors.RepairPending",
                Config = config
            };
        }

        /// <summary>冪等授予目前 role；deletion-pending、managed、hierarchy 不合法時一律保留 retry state。</summary>
        public async Task<bool> GrantAsync(
            GuildYoutubeMemberConfig config,
            ulong userId,
            CancellationToken cancellationToken)
            => await GrantCoreAsync(config, userId, cancellationToken) == YoutubeMemberRoleApplyResult.Applied;

        /// <summary>migration 將已離開 guild 的使用者視為同步完成，避免舊 checkpoint 永久卡住。</summary>
        private Task<YoutubeMemberRoleApplyResult> GrantForMigrationAsync(
            GuildYoutubeMemberConfig config,
            ulong userId,
            CancellationToken cancellationToken)
            => GrantCoreAsync(config, userId, cancellationToken);

        private async Task<YoutubeMemberRoleApplyResult> GrantCoreAsync(
            GuildYoutubeMemberConfig config,
            ulong userId,
            CancellationToken cancellationToken)
        {
            if (config == null || config.DeletionPending)
                return YoutubeMemberRoleApplyResult.Failed;
            SocketGuild guild = _client.GetGuild(config.GuildId);
            if (guild == null || !CanManageRole(guild, config.MemberCheckGrantRoleId))
                return YoutubeMemberRoleApplyResult.Failed;
            try
            {
                await _client.Rest.AddRoleAsync(guild.Id, userId, config.MemberCheckGrantRoleId,
                    new RequestOptions { CancelToken = cancellationToken });
                return YoutubeMemberRoleApplyResult.Applied;
            }
            catch (Discord.Net.HttpException ex) when (ex.DiscordCode is DiscordErrorCode.UnknownAccount or
                DiscordErrorCode.UnknownMember or DiscordErrorCode.UnknownUser)
            {
                return YoutubeMemberRoleApplyResult.UnknownMember;
            }
            catch (Exception ex)
            {
                Log.Warn($"授予 YouTube 會限身分組失敗: {config.GuildId} / {userId} / {ex.GetType().Name}");
                return YoutubeMemberRoleApplyResult.Failed;
            }
        }

        /// <summary>移除設定 current/previous role，legacy collision 仍由跨平台 entitlement 保護。</summary>
        public async Task<bool> RemoveAsync(
            GuildYoutubeMemberConfig config,
            ulong userId,
            CancellationToken cancellationToken)
        {
            if (config == null)
                return true;
            MemberRoleOwnershipSnapshot ownership = await _roleOwnershipService.LoadSnapshotAsync(
                config.GuildId, cancellationToken);
            return await RemoveRoleIdsAsync(
                config,
                userId,
                [config.MemberCheckGrantRoleId, config.PreviousMemberCheckGrantRoleId ?? 0],
                ownership,
                cancellationToken);
        }

        /// <summary>設定刪除的可恢復 terminal transition；即使沒有 check 也會移除 config。</summary>
        public async Task<bool> DeleteConfigurationAsync(
            GuildYoutubeMemberConfig requestedConfig,
            CancellationToken cancellationToken)
        {
            await using var guildLock = await _operationCoordinator.LockGuildAsync(
                requestedConfig.GuildId, cancellationToken);
            using var db = _dbService.GetDbContext();
            var config = await db.GuildYoutubeMemberConfig.SingleOrDefaultAsync(x =>
                x.GuildId == requestedConfig.GuildId &&
                x.MemberCheckChannelId == requestedConfig.MemberCheckChannelId, cancellationToken);
            if (config == null)
                return true;

            var checks = await db.YoutubeMemberCheck.Where(x =>
                x.GuildId == config.GuildId && x.CheckYTChannelId == config.MemberCheckChannelId)
                .ToListAsync(cancellationToken);
            YoutubeMemberPolicies.QueueConfigurationDeletion(config, checks);
            await db.SaveChangesAsync(cancellationToken);

            MemberRoleOwnershipSnapshot ownership = await _roleOwnershipService.LoadSnapshotAsync(
                config.GuildId, cancellationToken);
            bool allRemoved = true;
            foreach (YoutubeMemberCheck check in checks)
            {
                bool removed = await RemoveRoleIdsAsync(
                    config,
                    check.UserId,
                    [config.MemberCheckGrantRoleId, config.PreviousMemberCheckGrantRoleId ?? 0],
                    ownership,
                    cancellationToken);
                if (removed)
                    db.YoutubeMemberCheck.Remove(check);
                else
                    allRemoved = false;
            }
            if (!allRemoved)
            {
                await db.SaveChangesAsync(cancellationToken);
                return false;
            }

            db.GuildYoutubeMemberConfig.Remove(config);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }

        /// <summary>不依賴 YouTube API 的 pending cleanup；供既有週期在 provider 停用時安全呼叫。</summary>
        public async Task RetryPendingCleanupAsync(CancellationToken cancellationToken)
        {
            using (var db = _dbService.GetDbContext())
            {
                var deletingConfigs = await db.GuildYoutubeMemberConfig.AsNoTracking()
                    .Where(x => x.DeletionPending)
                    .ToArrayAsync(cancellationToken);
                foreach (GuildYoutubeMemberConfig config in deletingConfigs.Where(x => Bot.IsServerOnThisShard(x.GuildId)))
                    await DeleteConfigurationAsync(config, cancellationToken);
            }

            using var readDb = _dbService.GetDbContext();
            var pendingChecks = await readDb.YoutubeMemberCheck.AsNoTracking()
                .Where(x => x.PendingRoleRemoval)
                .ToArrayAsync(cancellationToken);
            foreach (YoutubeMemberCheck pendingCheck in pendingChecks.Where(x => Bot.IsServerOnThisShard(x.GuildId)))
            {
                await using var userLock = await _operationCoordinator.LockUserAsync(pendingCheck.UserId, cancellationToken);
                await using var guildLock = await _operationCoordinator.LockGuildAsync(pendingCheck.GuildId, cancellationToken);
                await RetryPendingCheckCoreAsync(pendingCheck.Id, cancellationToken);
            }
        }

        /// <summary>Redis revoke hint 專用：只重試已 durable pending 的資料，絕不改動 active check 或 OAuth token。</summary>
        public async Task RetryPendingCleanupForUserAsync(ulong userId, CancellationToken cancellationToken)
        {
            using var readDb = _dbService.GetDbContext();
            var pendingChecks = await readDb.YoutubeMemberCheck.AsNoTracking()
                .Where(x => x.UserId == userId && x.PendingRoleRemoval)
                .ToArrayAsync(cancellationToken);
            foreach (YoutubeMemberCheck pendingCheck in pendingChecks.Where(x => Bot.IsServerOnThisShard(x.GuildId)))
            {
                await using var userLock = await _operationCoordinator.LockUserAsync(userId, cancellationToken);
                await using var guildLock = await _operationCoordinator.LockGuildAsync(pendingCheck.GuildId, cancellationToken);
                await RetryPendingCheckCoreAsync(pendingCheck.Id, cancellationToken);
            }
        }

        /// <summary>孤兒對帳沿用呼叫端已載入的 snapshot，不能為每位 member 再查資料庫。</summary>
        public async Task<bool> RemoveOrphanAsync(
            SocketGuild guild,
            ulong userId,
            ulong roleId,
            MemberRoleOwnershipSnapshot ownership,
            CancellationToken cancellationToken)
        {
            if (guild.GetRole(roleId) == null || ownership.HasOtherActiveEntitlement(userId, roleId))
                return true;
            if (!CanManageRole(guild, roleId))
                return false;
            try
            {
                await _client.Rest.RemoveRoleAsync(guild.Id, userId, roleId,
                    new RequestOptions { CancelToken = cancellationToken });
                return true;
            }
            catch (Discord.Net.HttpException ex) when (ex.DiscordCode is DiscordErrorCode.UnknownAccount or
                DiscordErrorCode.UnknownMember or DiscordErrorCode.UnknownUser)
            {
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn($"移除 YouTube 孤兒會限身分組失敗: {guild.Id} / {userId} / {ex.GetType().Name}");
                return false;
            }
        }

        /// <summary>供 guild lock 內的孤兒對帳一次載入跨平台 entitlement 快照。</summary>
        public Task<MemberRoleOwnershipSnapshot> LoadOwnershipSnapshotAsync(
            ulong guildId,
            CancellationToken cancellationToken)
            => _roleOwnershipService.LoadSnapshotAsync(guildId, cancellationToken);

        private async Task RetryPendingCheckCoreAsync(int checkId, CancellationToken cancellationToken)
        {
            using var db = _dbService.GetDbContext();
            var check = await db.YoutubeMemberCheck.SingleOrDefaultAsync(x => x.Id == checkId, cancellationToken);
            if (check == null || !check.PendingRoleRemoval)
                return;
            var config = await db.GuildYoutubeMemberConfig.AsNoTracking().SingleOrDefaultAsync(x =>
                x.GuildId == check.GuildId && x.MemberCheckChannelId == check.CheckYTChannelId, cancellationToken);
            if (config?.DeletionPending == true)
                return;
            if (config == null)
            {
                // role id 已只存在於遺失設定中，直接刪列會讓人工修復失去唯一證據。
                Log.Warn($"YouTube pending cleanup 找不到設定，保留待清理列: {check.GuildId} / {check.Id}");
                return;
            }
            if (await RemoveAsync(config, check.UserId, cancellationToken))
            {
                db.YoutubeMemberCheck.Remove(check);
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task<bool> RemoveRoleIdsAsync(
            GuildYoutubeMemberConfig config,
            ulong userId,
            IEnumerable<ulong> roleIds,
            MemberRoleOwnershipSnapshot ownership,
            CancellationToken cancellationToken)
        {
            SocketGuild guild = _client.GetGuild(config.GuildId);
            if (guild == null)
                return Bot.ShouldDeleteMissingGuild(config.GuildId);
            foreach (ulong roleId in roleIds.Where(x => x != 0).Distinct())
            {
                if (ownership.HasOtherActiveEntitlement(
                        userId,
                        roleId,
                        MemberEntitlementProvider.Youtube,
                        config.MemberCheckChannelId))
                    continue;
                if (guild.GetRole(roleId) == null)
                    continue;
                if (!CanManageRole(guild, roleId))
                    return false;
                try
                {
                    await _client.Rest.RemoveRoleAsync(guild.Id, userId, roleId,
                        new RequestOptions { CancelToken = cancellationToken });
                }
                catch (Discord.Net.HttpException ex) when (ex.DiscordCode is DiscordErrorCode.UnknownAccount or
                    DiscordErrorCode.UnknownMember or DiscordErrorCode.UnknownUser)
                {
                }
                catch (Exception ex)
                {
                    Log.Warn($"移除 YouTube 會限身分組失敗: {guild.Id} / {userId} / {ex.GetType().Name}");
                    return false;
                }
            }
            return true;
        }

        private string ValidateRole(SocketGuild guild, IRole role)
        {
            SocketGuildUser bot = guild.GetUser(_client.CurrentUser.Id);
            if (bot?.GuildPermissions.ManageRoles != true)
                return "MemberSetting.Errors.ManageRolesRequired";
            if (role.Id == guild.EveryoneRole.Id)
                return "MemberSetting.Errors.EveryoneRole";
            if (role.IsManaged)
                return "MemberSetting.Errors.ManagedRole";
            if (role.Position >= bot.Roles.Max(x => x.Position))
                return "MemberSetting.Errors.RoleTooHigh";
            return null;
        }

        private bool CanManageRole(SocketGuild guild, ulong roleId)
        {
            SocketGuildUser bot = guild.GetUser(_client.CurrentUser.Id);
            SocketRole role = guild.GetRole(roleId);
            return bot?.GuildPermissions.ManageRoles == true &&
                role != null &&
                role.Id != guild.EveryoneRole.Id &&
                !role.IsManaged &&
                role.Position < bot.Roles.Max(x => x.Position);
        }
    }
}
