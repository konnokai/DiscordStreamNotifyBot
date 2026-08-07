using DiscordStreamNotifyBot.Interaction.YoutubeMember;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.SharedService.YoutubeMember;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class YoutubeMemberSelectMenuTests
    {
        [Theory]
        [InlineData("youtube-member-check:123:456", true)]
        [InlineData("member:check:123:456", false)]
        [InlineData("twitch-member-check:123:456", false)]
        [InlineData("spider_youtube:trusted:channel", false)]
        [InlineData("youtube-member-check:123", false)]
        [InlineData("1234", false)]
        [InlineData(null, false)]
        public void OnlyYoutubeMemberSelectMenusAreHandled(string customId, bool expected)
        {
            Assert.Equal(expected, YoutubeMemberComponent.IsYoutubeMemberSelectionCustomId(customId));
        }

        [Fact]
        public void RouteAndSelectionValidationRejectWrongShapeCountAndDuplicateValues()
        {
            Assert.True(YoutubeMemberPolicies.TryParseSelectionRoute("youtube-member-check:123:456", out ulong guildId, out ulong userId));
            Assert.Equal((ulong)123, guildId);
            Assert.Equal((ulong)456, userId);
            Assert.False(YoutubeMemberPolicies.TryParseSelectionRoute("youtube-member-check:123:456:789", out _, out _));
            Assert.False(YoutubeMemberPolicies.TryParseSelectionRoute("twitch-member-check:123:456", out _, out _));
            Assert.False(YoutubeMemberPolicies.IsValidSelection([]));
            Assert.False(YoutubeMemberPolicies.IsValidSelection(Enumerable.Repeat("UC1", 26).ToArray()));
            Assert.False(YoutubeMemberPolicies.IsValidSelection(["UC1", "UC1"]));
            Assert.True(YoutubeMemberPolicies.IsValidSelection(["UC1", "UC2"]));
            Assert.True(YoutubeMemberPolicies.IsActiveConfiguration(
                new GuildYoutubeMemberConfig { MemberCheckChannelId = "UC1" }));
            Assert.False(YoutubeMemberPolicies.IsActiveConfiguration(
                new GuildYoutubeMemberConfig { MemberCheckChannelId = "UC1", DeletionPending = true }));
        }

        [Fact]
        public void SelectionDiffRetainsVerifiedRowsRequeuesPendingRowsAndMarksDeselectedRowsPending()
        {
            var retained = new YoutubeMemberCheck { Id = 1, CheckYTChannelId = "UC1", IsChecked = true };
            var removed = new YoutubeMemberCheck { Id = 2, CheckYTChannelId = "UC2", IsChecked = true };
            var reselectedPending = new YoutubeMemberCheck
            {
                Id = 3, CheckYTChannelId = "UC3", IsChecked = false, PendingRoleRemoval = true
            };

            IReadOnlyList<YoutubeMemberSelectionTransition> transition = YoutubeMemberPolicies.BuildSelectionTransition(
                [retained, removed, reselectedPending], ["UC1", "UC3", "UC4"]);

            Assert.Contains(transition, x => x.ChannelId == "UC1" && !x.AddQueuedCheck &&
                !x.MarkRoleRemovalPending && !x.RequeueExistingCheck);
            Assert.Contains(transition, x => x.ChannelId == "UC3" && !x.AddQueuedCheck &&
                !x.MarkRoleRemovalPending && x.RequeueExistingCheck);
            Assert.Contains(transition, x => x.ChannelId == "UC4" && x.AddQueuedCheck && !x.MarkRoleRemovalPending);
            Assert.Contains(transition, x => x.ChannelId == "UC2" && !x.AddQueuedCheck && x.MarkRoleRemovalPending);

            YoutubeMemberPolicies.QueueRoleRemoval(removed);
            Assert.False(YoutubeMemberPolicies.IsActive(removed));
            Assert.False(removed.IsChecked);
            Assert.True(removed.PendingRoleRemoval);

            YoutubeMemberPolicies.QueueVerification(removed);
            Assert.False(removed.IsChecked);
            Assert.False(removed.PendingRoleRemoval);

            Assert.True(retained.IsChecked);
            Assert.False(retained.PendingRoleRemoval);
        }

        [Fact]
        public void ProviderResultRequiresSameRowAndStateSnapshot()
        {
            var check = new YoutubeMemberCheck { Id = 7, IsChecked = false, PendingRoleRemoval = false };
            YoutubeMemberCheckStateSnapshot snapshot = YoutubeMemberPolicies.CaptureState(check);

            Assert.True(YoutubeMemberPolicies.IsProviderResultApplicable(snapshot, check));
            check.PendingRoleRemoval = true;
            Assert.False(YoutubeMemberPolicies.IsProviderResultApplicable(snapshot, check));
            Assert.False(YoutubeMemberPolicies.IsProviderResultApplicable(snapshot,
                new YoutubeMemberCheck { Id = 8, IsChecked = false, PendingRoleRemoval = false }));

            YoutubeMemberPolicies.MarkVerified(check);
            Assert.True(YoutubeMemberPolicies.IsActive(check));
        }

        [Fact]
        public void ProbeCacheKeyUsesUserAndActualVideoRatherThanChannelRoute()
        {
            Assert.Equal((42UL, "video-a"), YoutubeMemberPolicies.BuildProbeCacheKey(42, "video-a"));
            Assert.NotEqual(
                YoutubeMemberPolicies.BuildProbeCacheKey(42, "video-a"),
                YoutubeMemberPolicies.BuildProbeCacheKey(42, "video-b"));
        }

        [Fact]
        public void ProviderResultRejectsReplacementTokenAndChangedProbeConfiguration()
        {
            var configuration = new GuildYoutubeMemberConfig
            {
                Id = 9,
                GuildId = 10,
                MemberCheckChannelId = "UC1",
                MemberCheckVideoId = "video-a"
            };
            YoutubeMemberProbeConfigurationSnapshot snapshot =
                YoutubeMemberPolicies.CaptureProbeConfiguration(configuration);

            Assert.True(YoutubeMemberPolicies.IsCurrentTokenPayload("ciphertext-a", "ciphertext-a"));
            Assert.False(YoutubeMemberPolicies.IsCurrentTokenPayload("ciphertext-a", "ciphertext-b"));
            Assert.True(YoutubeMemberPolicies.IsProbeConfigurationCurrent(snapshot, configuration));

            configuration.MemberCheckVideoId = "video-b";
            Assert.False(YoutubeMemberPolicies.IsProbeConfigurationCurrent(snapshot, configuration));
        }

        [Fact]
        public void SingleConfigurationQueueMatchesMultiSelectEntitlementSemanticsIncludingRetry()
        {
            Assert.Equal(YoutubeMemberSingleConfigurationQueueAction.Add,
                YoutubeMemberPolicies.DecideSingleConfigurationQueue(null));
            Assert.Equal(YoutubeMemberSingleConfigurationQueueAction.PreserveVerified,
                YoutubeMemberPolicies.DecideSingleConfigurationQueue(new YoutubeMemberCheck { IsChecked = true }));
            Assert.Equal(YoutubeMemberSingleConfigurationQueueAction.RequeuePendingRoleRemoval,
                YoutubeMemberPolicies.DecideSingleConfigurationQueue(new YoutubeMemberCheck
                {
                    IsChecked = false, PendingRoleRemoval = true
                }));
            Assert.Equal(YoutubeMemberSingleConfigurationQueueAction.PreserveQueued,
                YoutubeMemberPolicies.DecideSingleConfigurationQueue(new YoutubeMemberCheck()));
        }

        [Theory]
        [InlineData(YoutubeMemberProbeResultKind.Member)]
        [InlineData(YoutubeMemberProbeResultKind.NotMember)]
        [InlineData(YoutubeMemberProbeResultKind.ProbeVideoInvalid)]
        [InlineData(YoutubeMemberProbeResultKind.AuthorizationInvalid)]
        public void EveryStateChangingProviderResultRejectsReplacementToken(
            YoutubeMemberProbeResultKind resultKind)
        {
            var check = new YoutubeMemberCheck { Id = 11, IsChecked = false };
            var configuration = new GuildYoutubeMemberConfig
            {
                Id = 12,
                GuildId = 13,
                MemberCheckChannelId = "UC1",
                MemberCheckVideoId = "video-a"
            };

            Assert.False(YoutubeMemberPolicies.CanApplyProviderResult(resultKind,
                "provider-call-ciphertext", "replacement-ciphertext",
                YoutubeMemberPolicies.CaptureState(check), check,
                YoutubeMemberPolicies.CaptureProbeConfiguration(configuration), configuration));
        }
    }
}
