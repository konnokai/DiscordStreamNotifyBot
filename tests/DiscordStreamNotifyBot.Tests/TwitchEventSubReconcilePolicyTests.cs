using DiscordStreamNotifyBot.SharedService.Twitch;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class TwitchEventSubReconcilePolicyTests
    {
        private const string Callback = "https://api.example.test/TwitchWebHooks";

        [Fact]
        public void PermanentOAuthPlansAllThreeSubscriptionsWhenMissing()
        {
            var plan = TwitchEventSubReconcilePolicy.Plan(
                TwitchEventSubEnsureMode.PermanentOAuth, "user", Callback, []);

            Assert.Equal(
                ["stream.online:1", "channel.update:2", "stream.offline:1"],
                plan.Create.Select(x => $"{x.Type}:{x.Version}"));
            Assert.Empty(plan.Delete);
        }

        [Fact]
        public void FallbackKeepsUpdateAndOfflineAndDeletesPermanentOnline()
        {
            TwitchEventSubFact[] current =
            [
                Subscription("online", "stream.online", "1"),
                Subscription("update", "channel.update", "2"),
                Subscription("offline", "stream.offline", "1")
            ];

            var plan = TwitchEventSubReconcilePolicy.Plan(
                TwitchEventSubEnsureMode.Fallback, "user", Callback, current);

            Assert.Empty(plan.Create);
            Assert.Equal(["online"], plan.Delete);
        }

        [Fact]
        public void PlanReplacesMalformedAndDuplicateManagedSubscriptionsOnly()
        {
            TwitchEventSubFact[] current =
            [
                Subscription("update-good", "channel.update", "2"),
                Subscription("update-duplicate", "channel.update", "2"),
                Subscription("online-invalid", "stream.online", "1", status: "authorization_revoked"),
                Subscription("offline-wrong-callback", "stream.offline", "1", callback: "https://wrong.test/callback"),
                Subscription("other-user", "stream.online", "1", broadcasterUserId: "other"),
                Subscription("unmanaged", "channel.follow", "2")
            ];

            var plan = TwitchEventSubReconcilePolicy.Plan(
                TwitchEventSubEnsureMode.PermanentOAuth, "user", Callback, current);

            Assert.Equal(["stream.online:1", "stream.offline:1"],
                plan.Create.Select(x => $"{x.Type}:{x.Version}"));
            Assert.Equal(
                ["offline-wrong-callback", "online-invalid", "update-duplicate"],
                plan.Delete.OrderBy(x => x));
        }

        [Fact]
        public void DuplicateSelectionPrefersEnabledOverPendingVerification()
        {
            TwitchEventSubFact[] current =
            [
                Subscription("update-pending", "channel.update", "2", status: "webhook_callback_verification_pending"),
                Subscription("update-enabled", "channel.update", "2"),
                Subscription("offline", "stream.offline", "1")
            ];

            var plan = TwitchEventSubReconcilePolicy.Plan(
                TwitchEventSubEnsureMode.Fallback, "user", Callback, current);

            Assert.Empty(plan.Create);
            Assert.Equal(["update-pending"], plan.Delete);
        }

        [Fact]
        public void PermanentFinalStateAcceptsPendingVerificationButRequiresZeroCost()
        {
            TwitchEventSubFact[] current =
            [
                Subscription("online", "stream.online", "1", status: "webhook_callback_verification_pending"),
                Subscription("update", "channel.update", "2"),
                Subscription("offline", "stream.offline", "1")
            ];

            var valid = TwitchEventSubReconcilePolicy.EvaluateFinal(
                TwitchEventSubEnsureMode.PermanentOAuth, "user", Callback, current);
            var costly = TwitchEventSubReconcilePolicy.EvaluateFinal(
                TwitchEventSubEnsureMode.PermanentOAuth, "user", Callback,
                current.Select(x => x.Type == "channel.update" ? x with { Cost = 1 } : x).ToArray());

            Assert.True(valid.IsSuccess);
            Assert.True(costly.AllDesiredEnabled);
            Assert.False(costly.IsPermanentCostValid);
            Assert.False(costly.IsSuccess);
        }

        [Fact]
        public void FinalStateRejectsWrongVersionEvenWhenConfigurationOtherwiseMatches()
        {
            TwitchEventSubFact[] current =
            [
                Subscription("online", "stream.online", "1"),
                Subscription("update", "channel.update", "1"),
                Subscription("offline", "stream.offline", "1")
            ];

            var decision = TwitchEventSubReconcilePolicy.EvaluateFinal(
                TwitchEventSubEnsureMode.PermanentOAuth, "user", Callback, current);

            Assert.False(decision.AllDesiredEnabled);
            Assert.False(decision.IsPermanentCostValid);
            Assert.False(decision.IsSuccess);
        }

        private static TwitchEventSubFact Subscription(
            string id,
            string type,
            string version,
            string status = "enabled",
            string callback = Callback,
            string broadcasterUserId = "user",
            int cost = 0) => new(
                id,
                type,
                version,
                broadcasterUserId,
                status,
                "webhook",
                callback,
                cost,
                ConditionCount: 1);
    }
}
