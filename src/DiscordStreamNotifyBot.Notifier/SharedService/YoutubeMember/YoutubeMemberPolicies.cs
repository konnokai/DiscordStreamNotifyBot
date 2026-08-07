using DiscordStreamNotifyBot.DataBase.Table;

namespace DiscordStreamNotifyBot.SharedService.YoutubeMember
{
    internal readonly record struct YoutubeMemberCheckStateSnapshot(
        int Id,
        bool IsChecked,
        bool PendingRoleRemoval);

    /// <summary>provider call 前的設定快照；套用結果前必須確認探測影片仍是同一筆設定。</summary>
    internal readonly record struct YoutubeMemberProbeConfigurationSnapshot(
        int Id,
        ulong GuildId,
        string ChannelId,
        string VideoId,
        bool IsManualVideoId);

    internal readonly record struct YoutubeMemberSelectionTransition(
        string ChannelId,
        bool AddQueuedCheck,
        bool MarkRoleRemovalPending,
        bool RequeueExistingCheck);

    internal enum YoutubeMemberSingleConfigurationQueueAction
    {
        Add,
        PreserveVerified,
        RequeuePendingRoleRemoval,
        PreserveQueued
    }

    public enum YoutubeMemberRoleApplyResult
    {
        Applied,
        UnknownMember,
        Failed
    }

    /// <summary>會限驗證狀態轉換的純規則，避免互動與背景工作各自判斷造成狀態漂移。</summary>
    internal static class YoutubeMemberPolicies
    {
        public static bool TryParseSelectionRoute(string customId, out ulong guildId, out ulong userId)
        {
            guildId = 0;
            userId = 0;
            string[] parts = customId?.Split(':') ?? [];
            return parts.Length == 3 &&
                parts[0] == "youtube-member-check" &&
                ulong.TryParse(parts[1], out guildId) && guildId != 0 &&
                ulong.TryParse(parts[2], out userId) && userId != 0;
        }

        public static bool IsValidSelection(IReadOnlyCollection<string> selectedChannelIds)
            => selectedChannelIds is { Count: >= 1 and <= 25 } &&
                selectedChannelIds.All(channelId => !string.IsNullOrWhiteSpace(channelId)) &&
                selectedChannelIds.Distinct(StringComparer.Ordinal).Count() == selectedChannelIds.Count;

        public static bool IsActiveConfiguration(GuildYoutubeMemberConfig config)
            => config != null && !config.DeletionPending &&
                !string.IsNullOrWhiteSpace(config.MemberCheckChannelId);

        /// <summary>
        /// previous role 是尚未完成 migration 的唯一 repair checkpoint；存在時只可重送目前 role，
        /// 否則第二次更新會遺失中間 role，讓已驗證成員永久保有不再受管理的角色。
        /// </summary>
        public static string ValidateRoleUpdateState(GuildYoutubeMemberConfig config, ulong requestedRoleId)
        {
            if (config == null)
                return null;
            if (config.DeletionPending)
                return "MemberSetting.Errors.DeletionPending";
            if (config.PreviousMemberCheckGrantRoleId.HasValue &&
                config.MemberCheckGrantRoleId != requestedRoleId)
            {
                return "MemberSetting.Errors.SharedRoleRepairPending";
            }
            return null;
        }

        public static void QueueConfigurationDeletion(
            GuildYoutubeMemberConfig config,
            IEnumerable<YoutubeMemberCheck> checks)
        {
            config.DeletionPending = true;
            foreach (YoutubeMemberCheck check in checks)
                QueueRoleRemoval(check);
        }

        /// <summary>保存 migration checkpoint 的純轉換；呼叫端必須先驗證 state 並立即 SaveChanges。</summary>
        public static void BeginRoleMigration(GuildYoutubeMemberConfig config, ulong requestedRoleId)
        {
            if (config.MemberCheckGrantRoleId != requestedRoleId)
            {
                config.PreviousMemberCheckGrantRoleId = config.MemberCheckGrantRoleId;
                config.MemberCheckGrantRoleId = requestedRoleId;
            }
        }

        /// <summary>Discord/log 權限是可恢復的營運錯誤，不能轉成設定刪除。</summary>
        public static bool ShouldPreserveConfigurationForOperationalFailure()
            => true;

        public static IReadOnlyList<YoutubeMemberSelectionTransition> BuildSelectionTransition(
            IEnumerable<YoutubeMemberCheck> existingChecks,
            IReadOnlyCollection<string> selectedChannelIds)
        {
            var existing = existingChecks.ToDictionary(x => x.CheckYTChannelId, StringComparer.Ordinal);
            var selected = selectedChannelIds.ToHashSet(StringComparer.Ordinal);
            var transitions = new List<YoutubeMemberSelectionTransition>();

            foreach (string channelId in selected)
            {
                YoutubeMemberCheck existingCheck = existing.GetValueOrDefault(channelId);
                transitions.Add(new YoutubeMemberSelectionTransition(
                    channelId,
                    AddQueuedCheck: existingCheck == null,
                    MarkRoleRemovalPending: false,
                    // 已驗證的選項是保留 entitlement，不可因再次送出選單而重新排隊。
                    // 使用者重新選回尚未完成角色移除的列時，才取消 removal 並重新驗證。
                    RequeueExistingCheck: existingCheck?.PendingRoleRemoval == true));
            }

            foreach (YoutubeMemberCheck check in existing.Values)
            {
                if (!selected.Contains(check.CheckYTChannelId))
                    transitions.Add(new YoutubeMemberSelectionTransition(
                        check.CheckYTChannelId,
                        AddQueuedCheck: false,
                        MarkRoleRemovalPending: true,
                        RequeueExistingCheck: false));
            }

            return transitions;
        }

        public static YoutubeMemberCheckStateSnapshot CaptureState(YoutubeMemberCheck check)
            => new(check.Id, check.IsChecked, check.PendingRoleRemoval);

        /// <summary>單一設定與多選選單的重送語意必須一致，已驗證 entitlement 不可被重新排隊。</summary>
        public static YoutubeMemberSingleConfigurationQueueAction DecideSingleConfigurationQueue(
            YoutubeMemberCheck check)
        {
            if (check == null)
                return YoutubeMemberSingleConfigurationQueueAction.Add;
            if (check.PendingRoleRemoval)
                return YoutubeMemberSingleConfigurationQueueAction.RequeuePendingRoleRemoval;
            return check.IsChecked
                ? YoutubeMemberSingleConfigurationQueueAction.PreserveVerified
                : YoutubeMemberSingleConfigurationQueueAction.PreserveQueued;
        }

        public static YoutubeMemberProbeConfigurationSnapshot CaptureProbeConfiguration(
            GuildYoutubeMemberConfig configuration)
            => new(configuration.Id, configuration.GuildId, configuration.MemberCheckChannelId,
                configuration.MemberCheckVideoId, configuration.IsManualVideoId);

        public static bool IsProviderResultApplicable(
            YoutubeMemberCheckStateSnapshot snapshot,
            YoutubeMemberCheck currentCheck)
            => currentCheck != null &&
                currentCheck.Id == snapshot.Id &&
                currentCheck.IsChecked == snapshot.IsChecked &&
                currentCheck.PendingRoleRemoval == snapshot.PendingRoleRemoval;

        /// <summary>
        /// 所有會觸發 DB/Discord 狀態轉換的 provider 結果都必須仍對應同一筆 OAuth 密文、check 與探測設定。
        /// 呼叫端在 MySQL row lock transaction 內使用此規則，避免跨程序寫入穿透 in-process coordinator。
        /// </summary>
        public static bool CanApplyProviderResult(
            YoutubeMemberProbeResultKind resultKind,
            string expectedEncryptedToken,
            string currentEncryptedToken,
            YoutubeMemberCheckStateSnapshot checkSnapshot,
            YoutubeMemberCheck currentCheck,
            YoutubeMemberProbeConfigurationSnapshot configurationSnapshot,
            GuildYoutubeMemberConfig currentConfiguration)
            => (resultKind is YoutubeMemberProbeResultKind.Member or
                YoutubeMemberProbeResultKind.NotMember or
                YoutubeMemberProbeResultKind.ProbeVideoInvalid or
                YoutubeMemberProbeResultKind.AuthorizationInvalid) &&
                IsCurrentTokenPayload(expectedEncryptedToken, currentEncryptedToken) &&
                IsProviderResultApplicable(checkSnapshot, currentCheck) &&
                IsProbeConfigurationCurrent(configurationSnapshot, currentConfiguration);

        public static bool IsProbeConfigurationCurrent(
            YoutubeMemberProbeConfigurationSnapshot snapshot,
            GuildYoutubeMemberConfig currentConfiguration)
            => currentConfiguration != null &&
                !currentConfiguration.DeletionPending &&
                currentConfiguration.Id == snapshot.Id &&
                currentConfiguration.GuildId == snapshot.GuildId &&
                currentConfiguration.MemberCheckChannelId == snapshot.ChannelId &&
                currentConfiguration.MemberCheckVideoId == snapshot.VideoId &&
                currentConfiguration.IsManualVideoId == snapshot.IsManualVideoId &&
                currentConfiguration.MemberCheckVideoId != "-";

        /// <summary>provider 回應只可作用於呼叫前讀到的同一份 MySQL 密文 token。</summary>
        public static bool IsCurrentTokenPayload(string expectedEncryptedToken, string currentEncryptedToken)
            => !string.IsNullOrEmpty(expectedEncryptedToken) &&
                string.Equals(expectedEncryptedToken, currentEncryptedToken, StringComparison.Ordinal);

        public static void QueueRoleRemoval(YoutubeMemberCheck check)
        {
            check.IsChecked = false;
            check.PendingRoleRemoval = true;
        }

        public static void QueueVerification(YoutubeMemberCheck check)
        {
            check.IsChecked = false;
            check.PendingRoleRemoval = false;
        }

        /// <summary>只有每筆 check 的 durable 移除 intent 都已落庫時，才允許移除本機 OAuth token。</summary>
        public static bool CanDeleteLocalTokenAfterCleanupIntent(IEnumerable<YoutubeMemberCheck> checks)
            => checks.All(check => check.PendingRoleRemoval);

        public static void MarkVerified(YoutubeMemberCheck check)
        {
            check.IsChecked = true;
            check.PendingRoleRemoval = false;
        }

        public static bool IsActive(YoutubeMemberCheck check)
            => check.IsChecked && !check.PendingRoleRemoval;

        /// <summary>角色遷移除了有效授權，也必須清理 pending check 尚未移除的舊角色。</summary>
        public static bool RequiresRoleMigration(YoutubeMemberCheck check)
            => IsActive(check) || check.PendingRoleRemoval;

        public static (ulong UserId, string VideoId) BuildProbeCacheKey(ulong userId, string memberCheckVideoId)
            => (userId, memberCheckVideoId);

        /// <summary>離開 guild 的使用者無法再持有舊角色，migration 可安全完成；一般驗證授予仍必須失敗。</summary>
        public static bool IsRoleMigrationSynchronized(YoutubeMemberRoleApplyResult result)
            => result is YoutubeMemberRoleApplyResult.Applied or YoutubeMemberRoleApplyResult.UnknownMember;
    }
}
