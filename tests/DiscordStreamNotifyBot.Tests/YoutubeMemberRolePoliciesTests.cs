using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.SharedService.Member;
using DiscordStreamNotifyBot.SharedService.YoutubeMember;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class YoutubeMemberRolePoliciesTests
    {
        [Fact]
        public void PreviousRoleRepairAllowsOnlyCurrentTargetAndRejectsThirdRole()
        {
            var config = new GuildYoutubeMemberConfig
            {
                MemberCheckGrantRoleId = 200,
                PreviousMemberCheckGrantRoleId = 100
            };

            Assert.Null(YoutubeMemberPolicies.ValidateRoleUpdateState(config, 200));
            Assert.Equal("MemberSetting.Errors.SharedRoleRepairPending",
                YoutubeMemberPolicies.ValidateRoleUpdateState(config, 300));
        }

        [Fact]
        public void InterruptedRoleMigrationPersistsOldAndNewRoleBeforeDiscordRepair()
        {
            var config = new GuildYoutubeMemberConfig { MemberCheckGrantRoleId = 100 };

            YoutubeMemberPolicies.BeginRoleMigration(config, 200);

            Assert.Equal((ulong)200, config.MemberCheckGrantRoleId);
            Assert.Equal((ulong)100, config.PreviousMemberCheckGrantRoleId);
            Assert.Equal("MemberSetting.Errors.SharedRoleRepairPending",
                YoutubeMemberPolicies.ValidateRoleUpdateState(config, 300));
        }

        [Fact]
        public void DeletionIntentSurvivesRoleFailureAndZeroCheckRetryCanFinish()
        {
            var config = new GuildYoutubeMemberConfig { MemberCheckGrantRoleId = 100 };
            var check = new YoutubeMemberCheck { IsChecked = true };

            YoutubeMemberPolicies.QueueConfigurationDeletion(config, [check]);

            Assert.True(config.DeletionPending);
            Assert.False(check.IsChecked);
            Assert.True(check.PendingRoleRemoval);

            var zeroCheckConfig = new GuildYoutubeMemberConfig { MemberCheckGrantRoleId = 200 };
            YoutubeMemberPolicies.QueueConfigurationDeletion(zeroCheckConfig, []);
            Assert.True(zeroCheckConfig.DeletionPending);
        }

        [Fact]
        public void PendingChecksAndDeletingConfigurationsAreExcludedFromActiveEntitlements()
        {
            var active = new YoutubeMemberCheck { IsChecked = true };
            var pending = new YoutubeMemberCheck { IsChecked = true, PendingRoleRemoval = true };
            var config = new GuildYoutubeMemberConfig { MemberCheckChannelId = "UC1" };

            Assert.True(YoutubeMemberPolicies.IsActive(active));
            Assert.False(YoutubeMemberPolicies.IsActive(pending));
            Assert.True(YoutubeMemberPolicies.IsActiveConfiguration(config));
            config.DeletionPending = true;
            Assert.False(YoutubeMemberPolicies.IsActiveConfiguration(config));
        }

        [Fact]
        public void PendingChecksStillParticipateInPreviousRoleMigration()
        {
            var active = new YoutubeMemberCheck { IsChecked = true };
            var pending = new YoutubeMemberCheck { PendingRoleRemoval = true };
            var inactive = new YoutubeMemberCheck();

            Assert.True(YoutubeMemberPolicies.RequiresRoleMigration(active));
            Assert.True(YoutubeMemberPolicies.RequiresRoleMigration(pending));
            Assert.False(YoutubeMemberPolicies.RequiresRoleMigration(inactive));
            Assert.False(YoutubeMemberPolicies.IsActive(pending));
        }

        [Fact]
        public void OperationalLogAndManagedRoleFailuresPreserveConfigurationForRetry()
        {
            Assert.True(YoutubeMemberPolicies.ShouldPreserveConfigurationForOperationalFailure());
        }

        [Theory]
        [InlineData(YoutubeMemberRoleApplyResult.Applied, true)]
        [InlineData(YoutubeMemberRoleApplyResult.UnknownMember, true)]
        [InlineData(YoutubeMemberRoleApplyResult.Failed, false)]
        public void RoleMigrationTreatsDepartedDiscordUserAsSynchronizedOnlyForCheckpointRepair(
            YoutubeMemberRoleApplyResult result,
            bool expected)
        {
            Assert.Equal(expected, YoutubeMemberPolicies.IsRoleMigrationSynchronized(result));
        }

        [Fact]
        public void LegacyCrossPlatformCollisionRetainsRoleNeededByOtherProvider()
        {
            var snapshot = new MemberRoleOwnershipSnapshot(
                [
                    new MemberRoleEntitlement(MemberEntitlementProvider.Youtube, "UC1", 42, 500),
                    new MemberRoleEntitlement(MemberEntitlementProvider.Twitch, "streamer", 42, 500)
                ],
                [],
                []);

            Assert.True(snapshot.HasOtherActiveEntitlement(
                42, 500, MemberEntitlementProvider.Youtube, "UC1"));
        }

        [Fact]
        public void LocalTokenCannotBeDeletedWhileAnyCheckIsStillActive()
        {
            Assert.False(YoutubeMemberPolicies.CanDeleteLocalTokenAfterCleanupIntent(
                [new YoutubeMemberCheck { PendingRoleRemoval = true }, new YoutubeMemberCheck()]));
            Assert.True(YoutubeMemberPolicies.CanDeleteLocalTokenAfterCleanupIntent(
                [new YoutubeMemberCheck { PendingRoleRemoval = true }]));
        }

        [Fact]
        public void MissingConfigurationMustRetainPendingCleanupEvidence()
        {
            var pending = new YoutubeMemberCheck { Id = 10, PendingRoleRemoval = true };

            Assert.True(pending.PendingRoleRemoval);
            Assert.False(YoutubeMemberPolicies.IsActive(pending));
        }
    }
}
