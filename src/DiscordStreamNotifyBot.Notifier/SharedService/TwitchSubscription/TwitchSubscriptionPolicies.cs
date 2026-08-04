using DiscordStreamNotifyBot.DataBase.Table;

namespace DiscordStreamNotifyBot.SharedService.TwitchSubscription
{
    internal enum TwitchAuthorizationLocalState
    {
        Active,
        Missing,
        PersistedInvalid,
        TemporaryFailure
    }

    internal static class TwitchAuthorizationLocalStatePolicy
    {
        public static TwitchAuthorizationLocalState ClassifyEntity(
            bool exists,
            bool isPersistedRevoked,
            bool clientIdMatches,
            bool hasCiphertext,
            bool hasRequiredScope)
        {
            if (!exists)
                return TwitchAuthorizationLocalState.Missing;
            if (isPersistedRevoked)
                return TwitchAuthorizationLocalState.PersistedInvalid;
            return clientIdMatches && hasCiphertext && hasRequiredScope
                ? TwitchAuthorizationLocalState.Active
                : TwitchAuthorizationLocalState.TemporaryFailure;
        }

        public static TwitchAuthorizationLocalState ClassifyToken(
            bool hasAccessToken,
            bool hasRefreshToken,
            bool hasTokenType,
            bool twitchUserIdMatches,
            bool scopeMatches)
            => hasAccessToken && hasRefreshToken && hasTokenType && twitchUserIdMatches && scopeMatches
                ? TwitchAuthorizationLocalState.Active
                : TwitchAuthorizationLocalState.TemporaryFailure;
    }

    internal static class TwitchAuthorizationEventPolicy
    {
        public static bool ShouldCleanup(string status, bool rowExists, bool isPersistedRevoked)
        {
            string normalized = status?.Trim().ToLowerInvariant();
            return rowExists && isPersistedRevoked && normalized is "invalid" or "revoked" or "unlinked";
        }
    }

    internal enum TwitchRefreshPersistenceDecision
    {
        WriteReplacement,
        AlreadyPersisted,
        Stale
    }

    internal static class TwitchRefreshPersistencePolicy
    {
        public static TwitchRefreshPersistenceDecision Decide(
            string currentCiphertext,
            string expectedCiphertext,
            string replacementCiphertext,
            bool isPersistedRevoked)
        {
            if (isPersistedRevoked)
                return TwitchRefreshPersistenceDecision.Stale;
            if (string.Equals(currentCiphertext, replacementCiphertext, StringComparison.Ordinal))
                return TwitchRefreshPersistenceDecision.AlreadyPersisted;
            return string.Equals(currentCiphertext, expectedCiphertext, StringComparison.Ordinal)
                ? TwitchRefreshPersistenceDecision.WriteReplacement
                : TwitchRefreshPersistenceDecision.Stale;
        }
    }

    internal static class TwitchSubscriptionConfigurationPolicy
    {
        public const int MaximumConfigurationsPerGuild = 25;

        public static bool CanSaveConfiguration(int configurationCount, bool alreadyExists)
            => alreadyExists || configurationCount < MaximumConfigurationsPerGuild;

        public static string ValidateCommonRole(
            ulong commonRoleId,
            IReadOnlyCollection<GuildTwitchSubscriptionConfig> existingConfigs)
        {
            if (commonRoleId == 0)
                return "TwitchMemberSetting.Errors.InvalidCommonRole";
            if (existingConfigs.Any(x => TierRoleIds(x).Contains(commonRoleId)))
                return "TwitchMemberSetting.Errors.CommonRoleOverlapsTier";
            return null;
        }

        public static string ValidateResultingRoleSet(
            int currentConfigId,
            ulong commonRoleId,
            IReadOnlyCollection<ulong> tierRoleIds,
            IReadOnlyCollection<GuildTwitchSubscriptionConfig> existingConfigs)
        {
            ulong[] tiers = tierRoleIds.Where(x => x != 0).ToArray();
            if (tiers.Length != 3 || tiers.Distinct().Count() != 3 || tiers.Contains(commonRoleId))
                return "TwitchMemberSetting.Errors.RolesMustBeDistinct";

            GuildTwitchSubscriptionConfig[] otherConfigs = existingConfigs
                .Where(x => x.Id != currentConfigId)
                .ToArray();
            var protectedRoleIds = otherConfigs
                .SelectMany(x => TierRoleIds(x).Append(x.SubscriberRoleId))
                .Where(x => x != 0)
                .ToHashSet();
            if (tiers.Any(protectedRoleIds.Contains))
                return "TwitchMemberSetting.Errors.TierRoleOverlap";
            if (otherConfigs.SelectMany(TierRoleIds).Contains(commonRoleId))
                return "TwitchMemberSetting.Errors.CommonRoleOverlapsTier";
            return null;
        }

        public static bool ShouldCompensateCreatedRoles(bool configurationPersisted)
            => !configurationPersisted;

        public static bool CanApplyDiscordMutations(bool configurationPersisted)
            => configurationPersisted;

        public static string ValidateUpdateState(
            GuildTwitchSubscriptionConfig config,
            ulong requestedSubscriberRoleId)
        {
            if (config.DeletionPending)
                return "TwitchMemberSetting.Errors.DeletionPending";
            if (config.PreviousSubscriberRoleId.HasValue &&
                config.SubscriberRoleId != requestedSubscriberRoleId)
            {
                return "TwitchMemberSetting.Errors.SharedRoleRepairPending";
            }
            return null;
        }

        private static IEnumerable<ulong> TierRoleIds(GuildTwitchSubscriptionConfig config)
            => [config.Tier1RoleId, config.Tier2RoleId, config.Tier3RoleId];
    }

    internal static class TwitchSubscriptionConfigurationQueries
    {
        public static IQueryable<GuildTwitchSubscriptionConfig> ActiveConfigurations(
            this IQueryable<GuildTwitchSubscriptionConfig> source)
            => source.Where(x => !x.DeletionPending);

        public static IQueryable<GuildTwitchSubscriptionConfig> DeletionPendingConfigurations(
            this IQueryable<GuildTwitchSubscriptionConfig> source)
            => source.Where(x => x.DeletionPending);
    }

    internal sealed class TwitchRefreshRotationLifecycle
    {
        private readonly object _gate = new();
        private readonly Dictionary<long, Task> _acceptedPersistenceTasks = [];
        private readonly Action<int> _pendingCountChanged;
        private TaskCompletionSource _activeOperationsDrained = CompletedSource();
        private Task _stopTask;
        private long _nextPersistenceId;
        private int _activeOperations;
        private bool _stopping;

        public TwitchRefreshRotationLifecycle(Action<int> pendingCountChanged = null)
        {
            _pendingCountChanged = pendingCountChanged;
        }

        public int ActiveOperationCount
        {
            get
            {
                lock (_gate)
                    return _activeOperations;
            }
        }

        public int PendingPersistenceCount
        {
            get
            {
                lock (_gate)
                    return _acceptedPersistenceTasks.Count;
            }
        }

        /// <summary>在尚未進入關機時登記一個 refresh operation，使 drain 能等待其完成 persistence 交接。</summary>
        public bool TryBeginRefresh(out Lease lease)
        {
            lock (_gate)
            {
                if (_stopping)
                {
                    lease = null;
                    return false;
                }

                if (_activeOperations++ == 0)
                    _activeOperationsDrained = NewSource();
                lease = new Lease(this);
                return true;
            }
        }

        /// <summary>追蹤 Twitch 已接受之 rotation 的保存工作，直到成功或確認 stale 才允許關機完成。</summary>
        public void TrackAcceptedPersistence(Task task)
        {
            ArgumentNullException.ThrowIfNull(task);
            long id;
            int count;
            lock (_gate)
            {
                id = ++_nextPersistenceId;
                _acceptedPersistenceTasks.Add(id, task);
                count = _acceptedPersistenceTasks.Count;
            }
            NotifyPendingCountChanged(count);
            _ = RemoveWhenCompletedAsync(id, task);
        }

        /// <summary>原子停止接納新 refresh，等待既有 operation 交接後 drain 全部保存工作。</summary>
        public Task StopAcceptingAndDrainAsync()
        {
            lock (_gate)
            {
                // 關機先拒絕新 refresh，再等執行中的 refresh 登記其 persistence task。
                // drain 必須持續到 task 集合為空，否則可能只留下已失效的舊 refresh token。
                _stopping = true;
                return _stopTask ??= DrainAsync(_activeOperationsDrained.Task);
            }
        }

        private async Task DrainAsync(Task activeOperationsDrained)
        {
            await Task.Yield();
            await activeOperationsDrained;
            while (true)
            {
                Task[] pending;
                lock (_gate)
                    pending = _acceptedPersistenceTasks.Values.ToArray();
                if (pending.Length == 0)
                    return;
                await Task.WhenAll(pending);
                await Task.Yield();
            }
        }

        private async Task RemoveWhenCompletedAsync(long id, Task task)
        {
            try
            {
                await task;
            }
            catch
            {
                // 原始 task 由關閉 drain 觀察；此 observer 只負責移除追蹤狀態。
            }
            finally
            {
                int count;
                lock (_gate)
                {
                    _acceptedPersistenceTasks.Remove(id);
                    count = _acceptedPersistenceTasks.Count;
                }
                NotifyPendingCountChanged(count);
            }
        }

        private void NotifyPendingCountChanged(int count)
        {
            try
            {
                _pendingCountChanged?.Invoke(count);
            }
            catch
            {
                // 指標更新不得中斷已接受的 refresh rotation 保存。
            }
        }

        private void CompleteRefresh()
        {
            TaskCompletionSource drained = null;
            lock (_gate)
            {
                if (--_activeOperations == 0)
                    drained = _activeOperationsDrained;
            }
            drained?.TrySetResult();
        }

        private static TaskCompletionSource CompletedSource()
        {
            var source = NewSource();
            source.SetResult();
            return source;
        }

        private static TaskCompletionSource NewSource()
            => new(TaskCreationOptions.RunContinuationsAsynchronously);

        public sealed class Lease : IDisposable
        {
            private TwitchRefreshRotationLifecycle _owner;

            internal Lease(TwitchRefreshRotationLifecycle owner)
            {
                _owner = owner;
            }

            public void Dispose()
                => Interlocked.Exchange(ref _owner, null)?.CompleteRefresh();
        }
    }

    internal static class TwitchRateLimitPolicy
    {
        public static bool IsBlocked(DateTimeOffset now, DateTimeOffset? retryAfter)
            => retryAfter.HasValue && retryAfter.Value > now;
    }
}
