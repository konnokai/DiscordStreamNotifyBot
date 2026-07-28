using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Shared.Messages;

namespace DiscordStreamNotifyBot.Scraper.Detection.Twitch
{
    internal enum TwitchReconcileAction
    {
        RejectClientIdMismatch,
        EnsurePermanentSubscriptions,
        EnsureFallbackSubscriptions,
        KeepPollingWithoutSubscriptions,
        DeferApiFailure,
        DeferLive,
        ScheduleOfflineConfirmation,
        DeleteSubscriptions,
        DeleteSubscriptionsThenEvaluateGuild
    }

    internal sealed record TwitchReconcileFacts(
        bool HasSpider,
        bool IsWarningSpider,
        bool HasAuthorization,
        bool HasValidAuthorization,
        bool HasClientIdMismatch,
        bool LiveStateKnown,
        bool IsLive,
        bool HasLocalStreamState,
        bool OfflineConfirmationCompleted,
        bool AuthorizationRevokedDuringCurrentStream);

    internal static class TwitchReconcilePolicy
    {
        public static TwitchReconcileAction Decide(TwitchReconcileFacts facts)
        {
            if (facts.HasClientIdMismatch)
                return TwitchReconcileAction.RejectClientIdMismatch;
            if (facts.HasValidAuthorization && facts.HasSpider)
                return TwitchReconcileAction.EnsurePermanentSubscriptions;

            if (!facts.LiveStateKnown)
            {
                return !facts.HasAuthorization && facts.IsWarningSpider
                    ? TwitchReconcileAction.KeepPollingWithoutSubscriptions
                    : TwitchReconcileAction.DeferApiFailure;
            }

            if (facts.IsLive)
            {
                if (!facts.HasAuthorization && facts.HasSpider && !facts.IsWarningSpider)
                    return TwitchReconcileAction.EnsureFallbackSubscriptions;
                if (!facts.HasAuthorization && facts.IsWarningSpider)
                    return TwitchReconcileAction.KeepPollingWithoutSubscriptions;
                if (facts.AuthorizationRevokedDuringCurrentStream)
                    return TwitchReconcileAction.DeferLive;
                if (facts.IsWarningSpider)
                    return TwitchReconcileAction.KeepPollingWithoutSubscriptions;
                if (facts.HasSpider)
                    return TwitchReconcileAction.EnsureFallbackSubscriptions;
                return TwitchReconcileAction.DeferLive;
            }

            if (!facts.HasAuthorization && facts.IsWarningSpider)
                return TwitchReconcileAction.KeepPollingWithoutSubscriptions;
            if (facts.HasLocalStreamState && !facts.OfflineConfirmationCompleted)
                return TwitchReconcileAction.ScheduleOfflineConfirmation;
            return facts.HasAuthorization && facts.HasSpider
                ? TwitchReconcileAction.DeleteSubscriptionsThenEvaluateGuild
                : TwitchReconcileAction.DeleteSubscriptions;
        }

        public static bool WasAuthorizationRevokedDuringStream(DateTime? revokedAt, DateTime streamStartedAt)
        {
            if (revokedAt == null)
                return false;

            DateTime startedAtUtc = AsUtc(streamStartedAt);
            DateTime revokedAtUtc = AsUtc(revokedAt.Value);
            return startedAtUtc <= revokedAtUtc;
        }

        private static DateTime AsUtc(DateTime value) => value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    internal sealed record TwitchMissingGuildObservation(
        int ShardId,
        DateTime SnapshotUpdatedAtUtc,
        DateTime FirstObservedAtUtc);

    internal enum TwitchMissingObservationAction
    {
        Preserve,
        Remove,
        Set
    }

    internal sealed record TwitchGuildEligibilityFacts(
        bool IsExempt,
        bool IsTotalShardCountAvailable,
        bool IsNotifierAvailable,
        bool IsSnapshotAvailable,
        int OwnerShard,
        bool IsGuildPresent,
        int MemberCount,
        DateTime SnapshotUpdatedAtUtc,
        DateTime NowUtc,
        TwitchMissingGuildObservation PreviousMissingObservation);

    internal sealed record TwitchGuildEligibilityDecision(
        TwitchGuildEligibilityStatus Status,
        TwitchMissingObservationAction ObservationAction,
        TwitchMissingGuildObservation Observation = null);

    internal static class TwitchGuildEligibilityPolicy
    {
        internal static readonly TimeSpan MissingConfirmationDelay = TimeSpan.FromMinutes(15);

        public static TwitchGuildEligibilityDecision Decide(TwitchGuildEligibilityFacts facts)
        {
            if (facts.IsExempt)
                return new(TwitchGuildEligibilityStatus.Eligible, TwitchMissingObservationAction.Remove);
            if (!facts.IsTotalShardCountAvailable)
                return new(TwitchGuildEligibilityStatus.SnapshotUnavailable, TwitchMissingObservationAction.Preserve);
            if (!facts.IsNotifierAvailable)
                return new(TwitchGuildEligibilityStatus.NotifierUnavailable, TwitchMissingObservationAction.Preserve);
            if (!facts.IsSnapshotAvailable)
                return new(TwitchGuildEligibilityStatus.SnapshotUnavailable, TwitchMissingObservationAction.Preserve);
            if (facts.IsGuildPresent)
            {
                return new(
                    facts.MemberCount >= 200
                        ? TwitchGuildEligibilityStatus.Eligible
                        : TwitchGuildEligibilityStatus.Ineligible,
                    TwitchMissingObservationAction.Remove);
            }

            var previous = facts.PreviousMissingObservation;
            if (previous == null || previous.ShardId != facts.OwnerShard)
            {
                var observation = new TwitchMissingGuildObservation(
                    facts.OwnerShard, facts.SnapshotUpdatedAtUtc, facts.NowUtc);
                return new(TwitchGuildEligibilityStatus.PendingSnapshot,
                    TwitchMissingObservationAction.Set, observation);
            }

            if (facts.SnapshotUpdatedAtUtc <= previous.SnapshotUpdatedAtUtc ||
                facts.NowUtc - previous.FirstObservedAtUtc < MissingConfirmationDelay)
            {
                return new(TwitchGuildEligibilityStatus.PendingSnapshot,
                    TwitchMissingObservationAction.Preserve);
            }

            return new(TwitchGuildEligibilityStatus.MissingConfirmed,
                TwitchMissingObservationAction.Preserve);
        }
    }

    internal enum TwitchSpiderRemovalAction
    {
        DeferApiFailure,
        DeferLive,
        AlreadyRemoved,
        StateChanged,
        EvaluateEligibility,
        DeferNotifier,
        DeferSnapshot,
        Remove
    }

    internal sealed record TwitchSpiderRemovalFacts(
        bool StreamLookupSucceeded,
        bool IsLive,
        bool SpiderExists,
        bool GuildBindingMatches,
        bool HasValidAuthorization,
        bool HasClientIdMismatch,
        TwitchSpiderRemovalMetricReason Reason,
        TwitchGuildEligibilityStatus? LatestEligibility);

    internal static class TwitchSpiderRemovalPolicy
    {
        public static TwitchSpiderRemovalAction Decide(TwitchSpiderRemovalFacts facts)
        {
            if (!facts.StreamLookupSucceeded)
                return TwitchSpiderRemovalAction.DeferApiFailure;
            if (facts.IsLive)
                return TwitchSpiderRemovalAction.DeferLive;
            if (!facts.SpiderExists)
                return TwitchSpiderRemovalAction.AlreadyRemoved;
            if (!facts.GuildBindingMatches || facts.HasValidAuthorization || facts.HasClientIdMismatch)
                return TwitchSpiderRemovalAction.StateChanged;
            if (facts.LatestEligibility == null)
                return TwitchSpiderRemovalAction.EvaluateEligibility;

            bool removalAllowed = facts.Reason switch
            {
                TwitchSpiderRemovalMetricReason.GuildIneligible =>
                    facts.LatestEligibility == TwitchGuildEligibilityStatus.Ineligible,
                TwitchSpiderRemovalMetricReason.GuildMissing =>
                    facts.LatestEligibility == TwitchGuildEligibilityStatus.MissingConfirmed,
                _ => false
            };
            if (removalAllowed)
                return TwitchSpiderRemovalAction.Remove;
            if (facts.LatestEligibility == TwitchGuildEligibilityStatus.NotifierUnavailable)
                return TwitchSpiderRemovalAction.DeferNotifier;
            return TwitchSpiderRemovalAction.DeferSnapshot;
        }
    }

    internal enum TwitchStreamStartAction
    {
        IgnoreInvalid,
        IgnoreMissingSpider,
        RefreshStateOnly,
        PublishStart
    }

    internal sealed record TwitchStreamStartFacts(
        string StreamId,
        string UserId,
        bool HasSpider,
        bool ProcessDuplicate,
        bool RedisDuplicate,
        bool DatabaseDuplicate);

    internal static class TwitchStreamStartPolicy
    {
        public static TwitchStreamStartAction Decide(TwitchStreamStartFacts facts)
        {
            if (string.IsNullOrWhiteSpace(facts.StreamId) || string.IsNullOrWhiteSpace(facts.UserId))
                return TwitchStreamStartAction.IgnoreInvalid;
            if (!facts.HasSpider)
                return TwitchStreamStartAction.IgnoreMissingSpider;
            return facts.ProcessDuplicate || facts.RedisDuplicate || facts.DatabaseDuplicate
                ? TwitchStreamStartAction.RefreshStateOnly
                : TwitchStreamStartAction.PublishStart;
        }
    }

    internal sealed record TwitchStreamDataFacts(
        string StreamId,
        string StreamTitle,
        DateTime StreamStartAt,
        string UserId,
        string UserLogin,
        string UserName,
        string GameName,
        string ThumbnailUrl);

    internal static class TwitchStreamNotificationFactory
    {
        public static TwitchStream CreateState(TwitchStreamDataFacts facts) => new()
        {
            StreamId = facts.StreamId,
            StreamTitle = facts.StreamTitle,
            GameName = facts.GameName,
            ThumbnailUrl = (facts.ThumbnailUrl ?? string.Empty).Replace("{width}", "854").Replace("{height}", "480"),
            UserId = facts.UserId,
            UserLogin = facts.UserLogin,
            UserName = facts.UserName,
            StreamStartAt = facts.StreamStartAt
        };

        public static TwitchNotification CreateStart(TwitchStream stream, bool isRecord) => new()
        {
            NoticeType = TwitchNoticeType.StartStream,
            UserId = stream.UserId,
            StreamId = stream.StreamId,
            UserLogin = stream.UserLogin,
            UserName = stream.UserName,
            StreamTitle = stream.StreamTitle,
            GameName = stream.GameName,
            ThumbnailUrl = stream.ThumbnailUrl,
            StreamStartAt = stream.StreamStartAt,
            IsRecord = isRecord
        };
    }

    internal enum TwitchOfflineAction
    {
        Defer,
        ResumeStream,
        ClearState,
        PublishEnd,
        Ignore
    }

    internal sealed record TwitchOfflineFacts(
        bool StreamLookupSucceeded,
        bool HasResumedStream,
        bool CleanupStillDeferredForLive,
        bool PublishEndRequested,
        bool HasStreamState,
        bool HasSpider);

    internal static class TwitchOfflinePolicy
    {
        public static TwitchOfflineAction Decide(TwitchOfflineFacts facts)
        {
            if (!facts.StreamLookupSucceeded || facts.CleanupStillDeferredForLive)
                return TwitchOfflineAction.Defer;
            if (facts.HasResumedStream)
                return TwitchOfflineAction.ResumeStream;
            if (!facts.PublishEndRequested)
                return TwitchOfflineAction.ClearState;
            return facts.HasStreamState || facts.HasSpider
                ? TwitchOfflineAction.PublishEnd
                : TwitchOfflineAction.Ignore;
        }
    }

    internal enum TwitchOfflineScheduleAction
    {
        Schedule,
        KeepExisting,
        ReplaceExisting
    }

    internal static class TwitchOfflineSchedulePolicy
    {
        public static TwitchOfflineScheduleAction Decide(bool hasExisting, bool replaceExisting)
        {
            if (replaceExisting)
                return TwitchOfflineScheduleAction.ReplaceExisting;
            return hasExisting
                ? TwitchOfflineScheduleAction.KeepExisting
                : TwitchOfflineScheduleAction.Schedule;
        }
    }

    internal sealed record TwitchChannelStateFacts(
        string Title,
        string Category,
        string UserLogin,
        string UserName,
        DateTime StreamStartedAtUtc);

    internal sealed record TwitchChannelEventFacts(
        string Title,
        string Category,
        string UserLogin,
        string UserName,
        DateTime ObservedAtUtc);

    internal sealed record TwitchChannelUpdateChange(
        long ElapsedSeconds,
        string OldTitle,
        string NewTitle,
        string OldCategory,
        string NewCategory)
    {
        public bool HasChanges => NewTitle != null || NewCategory != null;

        public TwitchChannelUpdateInfo ToDto() => new()
        {
            ElapsedSeconds = ElapsedSeconds,
            OldTitle = OldTitle,
            NewTitle = NewTitle,
            OldCategory = OldCategory,
            NewCategory = NewCategory
        };

        public static TwitchChannelUpdateChange FromDto(TwitchChannelUpdateInfo update) => new(
            update.ElapsedSeconds, update.OldTitle, update.NewTitle, update.OldCategory, update.NewCategory);
    }

    internal enum TwitchChannelUpdateAction
    {
        Ignore,
        RefreshState,
        Queue
    }

    internal sealed record TwitchChannelUpdateDecision(
        TwitchChannelUpdateAction Action,
        TwitchChannelUpdateChange Change,
        TwitchChannelStateFacts NextState);

    internal sealed record TwitchChannelUpdateBatch(
        IReadOnlyList<TwitchChannelUpdateInfo> Updates,
        string LegacyDescription);

    internal static class TwitchChannelUpdatePolicy
    {
        public static TwitchChannelUpdateDecision Decide(TwitchChannelStateFacts current,
            TwitchChannelEventFacts incoming)
        {
            bool titleChanged = current.Title != incoming.Title;
            bool categoryChanged = current.Category != incoming.Category;
            var next = new TwitchChannelStateFacts(incoming.Title, incoming.Category,
                incoming.UserLogin, incoming.UserName, current.StreamStartedAtUtc);
            if (!titleChanged && !categoryChanged)
            {
                bool identityChanged = current.UserLogin != incoming.UserLogin || current.UserName != incoming.UserName;
                return new(identityChanged ? TwitchChannelUpdateAction.RefreshState : TwitchChannelUpdateAction.Ignore,
                    null, next);
            }

            var change = new TwitchChannelUpdateChange(
                Math.Max(0, (long)(incoming.ObservedAtUtc - current.StreamStartedAtUtc).TotalSeconds),
                titleChanged ? current.Title : null,
                titleChanged ? incoming.Title : null,
                categoryChanged ? current.Category : null,
                categoryChanged ? incoming.Category : null);
            return new(TwitchChannelUpdateAction.Queue, change, next);
        }

        public static IReadOnlyList<TwitchChannelUpdateChange> Aggregate(
            IEnumerable<TwitchChannelUpdateChange> changes) =>
            changes.Where(x => x?.HasChanges == true).ToArray();

        public static TwitchChannelUpdateBatch CreateBatch(IEnumerable<TwitchChannelUpdateChange> changes)
        {
            var aggregated = Aggregate(changes);
            return new TwitchChannelUpdateBatch(
                aggregated.Select(x => x.ToDto()).ToArray(),
                string.Join("\n\n", aggregated.Select(FormatLegacy)));
        }

        public static string FormatLegacy(TwitchChannelUpdateChange update)
        {
            string message = $"`{TimeSpan.FromSeconds(update.ElapsedSeconds):hh':'mm':'ss}`";
            if (update.NewTitle != null)
                message += $"\n標題變更 `{update.OldTitle}` => `{update.NewTitle}`";
            if (update.NewCategory != null)
            {
                message += $"\n分類變更 `{(string.IsNullOrEmpty(update.OldCategory) ? "無" : update.OldCategory)}`" +
                    $" => `{(string.IsNullOrEmpty(update.NewCategory) ? "無" : update.NewCategory)}`";
            }
            return message;
        }
    }
}
