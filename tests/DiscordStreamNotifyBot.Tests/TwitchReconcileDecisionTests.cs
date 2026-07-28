using DiscordStreamNotifyBot.Scraper;
using DiscordStreamNotifyBot.Scraper.Detection.Twitch;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class TwitchReconcileDecisionTests
    {
        [Fact]
        public void ValidAuthorizationWithSpiderEnsuresPermanentSubscriptions()
        {
            var action = TwitchReconcilePolicy.Decide(ReconcileFacts(
                hasAuthorization: true, hasValidAuthorization: true));

            Assert.Equal(TwitchReconcileAction.EnsurePermanentSubscriptions, action);
        }

        [Fact]
        public void ValidAuthorizationWithoutSpiderDeletesOrphanSubscriptions()
        {
            var action = TwitchReconcilePolicy.Decide(ReconcileFacts(
                hasSpider: false,
                hasAuthorization: true,
                hasValidAuthorization: true,
                liveStateKnown: true));

            Assert.Equal(TwitchReconcileAction.DeleteSubscriptions, action);
        }

        [Fact]
        public void ClientIdMismatchBlocksAllAutomaticMutation()
        {
            var action = TwitchReconcilePolicy.Decide(ReconcileFacts(
                hasAuthorization: true,
                hasClientIdMismatch: true,
                isLive: true));

            Assert.Equal(TwitchReconcileAction.RejectClientIdMismatch, action);
        }

        [Fact]
        public void UnknownLiveStateDefersDestructiveCleanup()
        {
            var action = TwitchReconcilePolicy.Decide(ReconcileFacts(
                hasAuthorization: true,
                liveStateKnown: false));

            Assert.Equal(TwitchReconcileAction.DeferApiFailure, action);
        }

        [Fact]
        public void UnauthorisedLiveSpiderUsesFallbackSubscriptions()
        {
            var action = TwitchReconcilePolicy.Decide(ReconcileFacts(isLive: true));

            Assert.Equal(TwitchReconcileAction.EnsureFallbackSubscriptions, action);
        }

        [Fact]
        public void WarningSpiderWithoutAuthorizationKeepsPollingWithoutSubscriptions()
        {
            var action = TwitchReconcilePolicy.Decide(ReconcileFacts(
                isWarningSpider: true,
                liveStateKnown: false));

            Assert.Equal(TwitchReconcileAction.KeepPollingWithoutSubscriptions, action);
        }

        [Fact]
        public void RevocationDuringCurrentStreamDefersCleanupUntilOffline()
        {
            var action = TwitchReconcilePolicy.Decide(ReconcileFacts(
                hasAuthorization: true,
                isLive: true,
                authorizationRevokedDuringCurrentStream: true));

            Assert.Equal(TwitchReconcileAction.DeferLive, action);
            Assert.True(TwitchReconcilePolicy.WasAuthorizationRevokedDuringStream(
                new DateTime(2026, 7, 27, 12, 5, 0),
                new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc)));
        }

        [Fact]
        public void LocalStreamStateRequiresOfflineConfirmationBeforeDeletion()
        {
            var pending = TwitchReconcilePolicy.Decide(ReconcileFacts(
                hasAuthorization: true,
                hasLocalStreamState: true));
            var confirmed = TwitchReconcilePolicy.Decide(ReconcileFacts(
                hasAuthorization: true,
                hasLocalStreamState: true,
                offlineConfirmationCompleted: true));

            Assert.Equal(TwitchReconcileAction.ScheduleOfflineConfirmation, pending);
            Assert.Equal(TwitchReconcileAction.DeleteSubscriptionsThenEvaluateGuild, confirmed);
        }

        [Fact]
        public void MissingGuildRequiresNewSnapshotAndConfirmationDelay()
        {
            DateTime firstObserved = new(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);
            DateTime firstSnapshot = firstObserved.AddMinutes(-1);
            var first = TwitchGuildEligibilityPolicy.Decide(GuildFacts(
                snapshotUpdatedAtUtc: firstSnapshot,
                nowUtc: firstObserved));

            Assert.Equal(TwitchGuildEligibilityStatus.PendingSnapshot, first.Status);
            Assert.Equal(TwitchMissingObservationAction.Set, first.ObservationAction);

            var sameGeneration = TwitchGuildEligibilityPolicy.Decide(GuildFacts(
                snapshotUpdatedAtUtc: firstSnapshot,
                nowUtc: firstObserved.AddMinutes(20),
                previous: first.Observation));
            var tooSoon = TwitchGuildEligibilityPolicy.Decide(GuildFacts(
                snapshotUpdatedAtUtc: firstSnapshot.AddMinutes(1),
                nowUtc: firstObserved.AddMinutes(14),
                previous: first.Observation));
            var confirmed = TwitchGuildEligibilityPolicy.Decide(GuildFacts(
                snapshotUpdatedAtUtc: firstSnapshot.AddMinutes(1),
                nowUtc: firstObserved.AddMinutes(15),
                previous: first.Observation));

            Assert.Equal(TwitchGuildEligibilityStatus.PendingSnapshot, sameGeneration.Status);
            Assert.Equal(TwitchGuildEligibilityStatus.PendingSnapshot, tooSoon.Status);
            Assert.Equal(TwitchGuildEligibilityStatus.MissingConfirmed, confirmed.Status);
        }

        [Theory]
        [InlineData(199, "Ineligible")]
        [InlineData(200, "Eligible")]
        public void PresentGuildUsesMemberThresholdAndClearsMissingObservation(
            int memberCount, string expected)
        {
            DateTime observedAt = new(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);
            var decision = TwitchGuildEligibilityPolicy.Decide(GuildFacts(
                isGuildPresent: true,
                memberCount: memberCount,
                previous: new TwitchMissingGuildObservation(1, observedAt, observedAt)));

            Assert.Equal(Enum.Parse<TwitchGuildEligibilityStatus>(expected), decision.Status);
            Assert.Equal(TwitchMissingObservationAction.Remove, decision.ObservationAction);
        }

        [Fact]
        public void UnavailableNotifierPreservesMissingObservation()
        {
            var decision = TwitchGuildEligibilityPolicy.Decide(GuildFacts(isNotifierAvailable: false));

            Assert.Equal(TwitchGuildEligibilityStatus.NotifierUnavailable, decision.Status);
            Assert.Equal(TwitchMissingObservationAction.Preserve, decision.ObservationAction);
        }

        [Fact]
        public void ExemptionAndUnavailableInfrastructureHaveExplicitConservativeDecisions()
        {
            var exempt = TwitchGuildEligibilityPolicy.Decide(GuildFacts() with { IsExempt = true });
            var noShardCount = TwitchGuildEligibilityPolicy.Decide(GuildFacts() with
            {
                IsTotalShardCountAvailable = false
            });
            var noSnapshot = TwitchGuildEligibilityPolicy.Decide(GuildFacts() with
            {
                IsSnapshotAvailable = false
            });

            Assert.Equal(TwitchGuildEligibilityStatus.Eligible, exempt.Status);
            Assert.Equal(TwitchMissingObservationAction.Remove, exempt.ObservationAction);
            Assert.Equal(TwitchGuildEligibilityStatus.SnapshotUnavailable, noShardCount.Status);
            Assert.Equal(TwitchGuildEligibilityStatus.SnapshotUnavailable, noSnapshot.Status);
            Assert.Equal(TwitchMissingObservationAction.Preserve, noSnapshot.ObservationAction);
        }

        [Theory]
        [InlineData(false, false, true, true, "DeferApiFailure")]
        [InlineData(true, true, true, true, "DeferLive")]
        [InlineData(true, false, false, true, "AlreadyRemoved")]
        [InlineData(true, false, true, false, "StateChanged")]
        public void FinalRemovalPreflightUsesLatestSafetyFacts(
            bool streamLookupSucceeded,
            bool isLive,
            bool spiderExists,
            bool guildMatches,
            string expected)
        {
            var action = TwitchSpiderRemovalPolicy.Decide(RemovalFacts(
                streamLookupSucceeded,
                isLive,
                spiderExists,
                guildMatches));

            Assert.Equal(Enum.Parse<TwitchSpiderRemovalAction>(expected), action);
        }

        [Theory]
        [InlineData(TwitchSpiderRemovalMetricReason.GuildIneligible, "Ineligible")]
        [InlineData(TwitchSpiderRemovalMetricReason.GuildMissing, "MissingConfirmed")]
        public void FinalRemovalRequiresMatchingLatestEligibility(
            TwitchSpiderRemovalMetricReason reason,
            string eligibility)
        {
            var action = TwitchSpiderRemovalPolicy.Decide(RemovalFacts(
                reason: reason,
                eligibility: Enum.Parse<TwitchGuildEligibilityStatus>(eligibility)));

            Assert.Equal(TwitchSpiderRemovalAction.Remove, action);
        }

        [Fact]
        public void ReauthorizationAtFinalDefenseCancelsRemoval()
        {
            var action = TwitchSpiderRemovalPolicy.Decide(RemovalFacts(hasValidAuthorization: true));

            Assert.Equal(TwitchSpiderRemovalAction.StateChanged, action);
        }

        [Fact]
        public void FinalDefenseDefersWhenLatestNotifierOrSnapshotIsNotSafe()
        {
            var notifier = TwitchSpiderRemovalPolicy.Decide(RemovalFacts(
                eligibility: TwitchGuildEligibilityStatus.NotifierUnavailable));
            var snapshot = TwitchSpiderRemovalPolicy.Decide(RemovalFacts(
                eligibility: TwitchGuildEligibilityStatus.Eligible));
            var clientMismatch = TwitchSpiderRemovalPolicy.Decide(RemovalFacts(
                hasClientIdMismatch: true));

            Assert.Equal(TwitchSpiderRemovalAction.DeferNotifier, notifier);
            Assert.Equal(TwitchSpiderRemovalAction.DeferSnapshot, snapshot);
            Assert.Equal(TwitchSpiderRemovalAction.StateChanged, clientMismatch);
        }

        private static TwitchReconcileFacts ReconcileFacts(
            bool hasSpider = true,
            bool isWarningSpider = false,
            bool hasAuthorization = false,
            bool hasValidAuthorization = false,
            bool hasClientIdMismatch = false,
            bool liveStateKnown = true,
            bool isLive = false,
            bool hasLocalStreamState = false,
            bool offlineConfirmationCompleted = false,
            bool authorizationRevokedDuringCurrentStream = false) => new(
                hasSpider,
                isWarningSpider,
                hasAuthorization,
                hasValidAuthorization,
                hasClientIdMismatch,
                liveStateKnown,
                isLive,
                hasLocalStreamState,
                offlineConfirmationCompleted,
                authorizationRevokedDuringCurrentStream);

        private static TwitchGuildEligibilityFacts GuildFacts(
            bool isNotifierAvailable = true,
            bool isGuildPresent = false,
            int memberCount = 0,
            DateTime? snapshotUpdatedAtUtc = null,
            DateTime? nowUtc = null,
            TwitchMissingGuildObservation previous = null) => new(
                IsExempt: false,
                IsTotalShardCountAvailable: true,
                isNotifierAvailable,
                IsSnapshotAvailable: true,
                OwnerShard: 1,
                isGuildPresent,
                memberCount,
                snapshotUpdatedAtUtc ?? new DateTime(2026, 7, 27, 11, 59, 0, DateTimeKind.Utc),
                nowUtc ?? new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc),
                previous);

        private static TwitchSpiderRemovalFacts RemovalFacts(
            bool streamLookupSucceeded = true,
            bool isLive = false,
            bool spiderExists = true,
            bool guildMatches = true,
            bool hasValidAuthorization = false,
            bool hasClientIdMismatch = false,
            TwitchSpiderRemovalMetricReason reason = TwitchSpiderRemovalMetricReason.GuildIneligible,
            TwitchGuildEligibilityStatus? eligibility = null) => new(
                streamLookupSucceeded,
                isLive,
                spiderExists,
                guildMatches,
                hasValidAuthorization,
                hasClientIdMismatch,
                reason,
                eligibility);
    }
}
