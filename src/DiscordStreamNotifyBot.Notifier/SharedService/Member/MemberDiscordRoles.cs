namespace DiscordStreamNotifyBot.SharedService.Member
{
    internal enum MemberRoleRemovalResult
    {
        /// <summary>角色已不存在或仍有其他有效 entitlement，未呼叫 Discord。</summary>
        Skipped,
        Removed,
        /// <summary>成員已離開 guild，視同完成。</summary>
        UserMissing,
        /// <summary>Bot 無法管理角色或 Discord 呼叫失敗；呼叫端必須保留重試狀態。</summary>
        Failed
    }

    /// <summary>YouTube 與 Twitch 共用的 Discord 身分組權限判斷與單一角色移除。</summary>
    internal static class MemberDiscordRoles
    {
        public static bool CanManageRole(SocketGuild guild, ulong roleId)
        {
            SocketGuildUser bot = guild.CurrentUser;
            SocketRole role = guild.GetRole(roleId);
            return bot?.GuildPermissions.ManageRoles == true &&
                role != null &&
                !role.IsManaged &&
                role.Id != guild.EveryoneRole.Id &&
                role.Position < bot.Roles.Max(x => x.Position);
        }

        /// <summary>驗證管理員指定的驗證身分組；錯誤 key 沿用各平台既有的 resource 前綴。</summary>
        public static string ValidateConfiguredRole(
            SocketGuild guild,
            IRole role,
            string errorKeyPrefix,
            string missingManageRolesKey)
        {
            SocketGuildUser bot = guild.CurrentUser;
            if (bot?.GuildPermissions.ManageRoles != true)
                return missingManageRolesKey;
            if (role.Id == guild.EveryoneRole.Id)
                return errorKeyPrefix + "EveryoneRole";
            if (role.IsManaged)
                return errorKeyPrefix + "ManagedRole";
            if (role.Position >= bot.Roles.Max(x => x.Position))
                return errorKeyPrefix + "RoleTooHigh";
            return null;
        }

        /// <summary>
        /// 移除單一角色；仍被其他 entitlement 引用的角色一律保留。
        /// 其他例外交由 onFailure 記錄；propagateCancellation 為 true 時，關機取消直接往外拋。
        /// </summary>
        public static async Task<MemberRoleRemovalResult> RemoveRoleAsync(
            DiscordSocketClient client,
            SocketGuild guild,
            ulong userId,
            ulong roleId,
            MemberRoleOwnershipSnapshot ownership,
            MemberEntitlementProvider? excludedProvider,
            string excludedConfigurationKey,
            Action<Exception> onFailure,
            bool propagateCancellation,
            CancellationToken cancellationToken)
        {
            if (guild.GetRole(roleId) == null ||
                ownership.HasOtherActiveEntitlement(userId, roleId, excludedProvider, excludedConfigurationKey))
                return MemberRoleRemovalResult.Skipped;
            if (!CanManageRole(guild, roleId))
                return MemberRoleRemovalResult.Failed;
            try
            {
                await client.Rest.RemoveRoleAsync(guild.Id, userId, roleId,
                    new RequestOptions { CancelToken = cancellationToken });
                return MemberRoleRemovalResult.Removed;
            }
            catch (Discord.Net.HttpException ex) when (ex.DiscordCode is DiscordErrorCode.UnknownMember or
                DiscordErrorCode.UnknownUser or DiscordErrorCode.UnknownAccount)
            {
                return MemberRoleRemovalResult.UserMissing;
            }
            catch (OperationCanceledException) when (propagateCancellation && cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                onFailure(ex);
                return MemberRoleRemovalResult.Failed;
            }
        }
    }
}
