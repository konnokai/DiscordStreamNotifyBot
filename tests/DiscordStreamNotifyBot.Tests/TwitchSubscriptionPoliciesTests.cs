using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.SharedService.TwitchSubscription;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class TwitchSubscriptionPoliciesTests
    {
        [Fact]
        public void LocalAuthorizationStateSeparatesRevocationFromTemporaryLocalFailures()
        {
            Assert.Equal(
                TwitchAuthorizationLocalState.Missing,
                TwitchAuthorizationLocalStatePolicy.ClassifyEntity(false, false, false, false, false));
            Assert.Equal(
                TwitchAuthorizationLocalState.PersistedInvalid,
                TwitchAuthorizationLocalStatePolicy.ClassifyEntity(true, true, true, true, true));
            Assert.Equal(
                TwitchAuthorizationLocalState.Active,
                TwitchAuthorizationLocalStatePolicy.ClassifyEntity(true, false, true, true, true));

            Assert.Equal(
                TwitchAuthorizationLocalState.TemporaryFailure,
                TwitchAuthorizationLocalStatePolicy.ClassifyEntity(true, false, false, true, true));
            Assert.Equal(
                TwitchAuthorizationLocalState.TemporaryFailure,
                TwitchAuthorizationLocalStatePolicy.ClassifyEntity(true, false, true, false, true));
            Assert.Equal(
                TwitchAuthorizationLocalState.TemporaryFailure,
                TwitchAuthorizationLocalStatePolicy.ClassifyEntity(true, false, true, true, false));
            Assert.Equal(
                TwitchAuthorizationLocalState.TemporaryFailure,
                TwitchAuthorizationLocalStatePolicy.ClassifyToken(false, true, true, true, true));
            Assert.Equal(
                TwitchAuthorizationLocalState.TemporaryFailure,
                TwitchAuthorizationLocalStatePolicy.ClassifyToken(true, false, true, true, true));
            Assert.Equal(
                TwitchAuthorizationLocalState.TemporaryFailure,
                TwitchAuthorizationLocalStatePolicy.ClassifyToken(true, true, false, true, true));
            Assert.Equal(
                TwitchAuthorizationLocalState.TemporaryFailure,
                TwitchAuthorizationLocalStatePolicy.ClassifyToken(true, true, true, false, true));
            Assert.Equal(
                TwitchAuthorizationLocalState.TemporaryFailure,
                TwitchAuthorizationLocalStatePolicy.ClassifyToken(true, true, true, true, false));
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData(" REVOKED ")]
        [InlineData("Unlinked")]
        public void AuthorizationEventRequiresCurrentPersistedRevocation(string status)
        {
            Assert.True(TwitchAuthorizationEventPolicy.ShouldCleanup(status, true, true));
            Assert.False(TwitchAuthorizationEventPolicy.ShouldCleanup(status, true, false));
            Assert.False(TwitchAuthorizationEventPolicy.ShouldCleanup(status, false, true));
            Assert.False(TwitchAuthorizationEventPolicy.ShouldCleanup("linked", true, true));
        }

        [Fact]
        public void RefreshPersistenceUsesExpectedCiphertextCompareAndSwap()
        {
            Assert.Equal(
                TwitchRefreshPersistenceDecision.WriteReplacement,
                TwitchRefreshPersistencePolicy.Decide("old", "old", "new", false));
            Assert.Equal(
                TwitchRefreshPersistenceDecision.AlreadyPersisted,
                TwitchRefreshPersistencePolicy.Decide("new", "old", "new", false));
            Assert.Equal(
                TwitchRefreshPersistenceDecision.Stale,
                TwitchRefreshPersistencePolicy.Decide("other", "old", "new", false));
            Assert.Equal(
                TwitchRefreshPersistenceDecision.Stale,
                TwitchRefreshPersistencePolicy.Decide("old", "old", "new", true));
        }

        [Fact]
        public void ConfigurationPolicyProtectsTierRolesAndMaximumCount()
        {
            var existing = new[]
            {
                Configuration(1, 100, 101, 102, 103),
                Configuration(2, 200, 201, 202, 203)
            };

            Assert.Equal(
                "TwitchMemberSetting.Errors.CommonRoleOverlapsTier",
                TwitchSubscriptionConfigurationPolicy.ValidateCommonRole(102, existing));
            Assert.Equal(
                "TwitchMemberSetting.Errors.RolesMustBeDistinct",
                TwitchSubscriptionConfigurationPolicy.ValidateResultingRoleSet(1, 100, [101, 101, 103], existing));
            Assert.Equal(
                "TwitchMemberSetting.Errors.TierRoleOverlap",
                TwitchSubscriptionConfigurationPolicy.ValidateResultingRoleSet(1, 100, [101, 102, 200], existing));
            Assert.Null(TwitchSubscriptionConfigurationPolicy.ValidateResultingRoleSet(1, 100, [101, 102, 103], existing));
            Assert.True(TwitchSubscriptionConfigurationPolicy.CanSaveConfiguration(25, alreadyExists: true));
            Assert.False(TwitchSubscriptionConfigurationPolicy.CanSaveConfiguration(25, alreadyExists: false));
            Assert.True(TwitchSubscriptionConfigurationPolicy.CanSaveConfiguration(24, alreadyExists: false));
        }

        [Fact]
        public void ConfigurationQueriesSeparateActiveAndDeletionPendingRows()
        {
            var configs = new[]
            {
                new GuildTwitchSubscriptionConfig { Id = 1 },
                new GuildTwitchSubscriptionConfig { Id = 2, DeletionPending = true }
            }.AsQueryable();

            Assert.Equal([1], configs.ActiveConfigurations().Select(x => x.Id).ToArray());
            Assert.Equal([2], configs.DeletionPendingConfigurations().Select(x => x.Id).ToArray());
        }

        [Fact]
        public void ConfigurationUpdateRejectsDeletionAndIntermediateSharedRoleChanges()
        {
            var deleting = new GuildTwitchSubscriptionConfig
            {
                SubscriberRoleId = 100,
                DeletionPending = true
            };
            var repairing = new GuildTwitchSubscriptionConfig
            {
                SubscriberRoleId = 200,
                PreviousSubscriberRoleId = 100
            };

            Assert.Equal(
                "TwitchMemberSetting.Errors.DeletionPending",
                TwitchSubscriptionConfigurationPolicy.ValidateUpdateState(deleting, 100));
            Assert.Equal(
                "TwitchMemberSetting.Errors.SharedRoleRepairPending",
                TwitchSubscriptionConfigurationPolicy.ValidateUpdateState(repairing, 300));
            Assert.Null(TwitchSubscriptionConfigurationPolicy.ValidateUpdateState(repairing, 200));
            Assert.Equal((ulong)100, repairing.PreviousSubscriberRoleId);
        }

        [Fact]
        public void RateLimitBlocksOnlyUntilProviderReset()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            Assert.True(TwitchRateLimitPolicy.IsBlocked(now, now.AddSeconds(1)));
            Assert.False(TwitchRateLimitPolicy.IsBlocked(now, now));
            Assert.False(TwitchRateLimitPolicy.IsBlocked(now, null));
        }

        private static GuildTwitchSubscriptionConfig Configuration(
            int id,
            ulong commonRoleId,
            ulong tier1RoleId,
            ulong tier2RoleId,
            ulong tier3RoleId)
            => new()
            {
                Id = id,
                SubscriberRoleId = commonRoleId,
                Tier1RoleId = tier1RoleId,
                Tier2RoleId = tier2RoleId,
                Tier3RoleId = tier3RoleId
            };
    }


    public sealed class TwitchRefreshRotationLifecycleTests
    {
        [Fact]
        public async Task StopRejectsNewRefreshesAndWaitsForInFlightOperation()
        {
            var lifecycle = new TwitchRefreshRotationLifecycle();
            Assert.True(lifecycle.TryBeginRefresh(out var refresh));

            Task stop = lifecycle.StopAcceptingAndDrainAsync();

            Assert.False(lifecycle.TryBeginRefresh(out _));
            Assert.False(stop.IsCompleted);
            refresh.Dispose();
            await stop.WaitAsync(TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task AcceptedPersistenceDrainsAfterRefreshOperationCompletes()
        {
            var pendingCounts = new List<int>();
            var lifecycle = new TwitchRefreshRotationLifecycle(pendingCounts.Add);
            Assert.True(lifecycle.TryBeginRefresh(out var refresh));
            var persisted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            lifecycle.TrackAcceptedPersistence(persisted.Task);

            Task stop = lifecycle.StopAcceptingAndDrainAsync();
            refresh.Dispose();

            Assert.False(stop.IsCompleted);
            Assert.Equal(1, lifecycle.PendingPersistenceCount);
            persisted.SetResult();
            await stop.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.Equal(0, lifecycle.PendingPersistenceCount);
            Assert.Equal([1, 0], pendingCounts);
        }

        [Fact]
        public async Task CompletedAndStalePersistenceTasksDoNotRemainTracked()
        {
            var lifecycle = new TwitchRefreshRotationLifecycle();
            Assert.True(lifecycle.TryBeginRefresh(out var refresh));
            lifecycle.TrackAcceptedPersistence(Task.CompletedTask);
            refresh.Dispose();

            await lifecycle.StopAcceptingAndDrainAsync().WaitAsync(TimeSpan.FromSeconds(1));

            Assert.Equal(0, lifecycle.ActiveOperationCount);
            Assert.Equal(0, lifecycle.PendingPersistenceCount);
        }

        [Fact]
        public async Task MetricsCallbackFailureCannotInterruptAcceptedPersistenceDrain()
        {
            var lifecycle = new TwitchRefreshRotationLifecycle(_ => throw new InvalidOperationException("metric failure"));
            Assert.True(lifecycle.TryBeginRefresh(out var refresh));
            lifecycle.TrackAcceptedPersistence(Task.CompletedTask);
            refresh.Dispose();

            await lifecycle.StopAcceptingAndDrainAsync().WaitAsync(TimeSpan.FromSeconds(1));

            Assert.Equal(0, lifecycle.PendingPersistenceCount);
        }
    }

    public sealed class TwitchSubscriptionOperationCoordinatorTests
    {
        [Fact]
        public async Task SameUserOperationsAreSerialized()
        {
            var coordinator = new TwitchSubscriptionOperationCoordinator();
            await using var first = await coordinator.LockUserAsync(42, CancellationToken.None);
            Task<TwitchSubscriptionOperationCoordinator.Lease> second =
                coordinator.LockUserAsync(42, CancellationToken.None);

            await Task.Delay(25);
            Assert.False(second.IsCompleted);

            await first.DisposeAsync();
            await using var secondLease = await second;
        }

        [Fact]
        public async Task SameGuildOperationsAreSerialized()
        {
            var coordinator = new TwitchSubscriptionOperationCoordinator();
            await using var first = await coordinator.LockGuildAsync(84, CancellationToken.None);
            Task<TwitchSubscriptionOperationCoordinator.Lease> second =
                coordinator.LockGuildAsync(84, CancellationToken.None);

            await Task.Delay(25);
            Assert.False(second.IsCompleted);

            await first.DisposeAsync();
            await using var secondLease = await second;
        }
    }
}
