using DiscordStreamNotifyBot.Scraper.Detection.Twitcasting;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class TwitcastingWebhookRegistrationPlannerTests
    {
        [Fact]
        public void MatchingDesiredAndLiveStartRegistrationsProduceNoActions()
        {
            var plan = TwitcastingWebhookRegistrationPlanner.Plan(
                ["user-a"],
                [new TwitcastingWebhookRegistration("user-a", "livestart")]);

            Assert.Empty(plan);
        }

        [Fact]
        public void MissingAndExtraLiveStartProduceStableDifferencePlan()
        {
            var plan = TwitcastingWebhookRegistrationPlanner.Plan(
                ["user-c", "user-a"],
                [
                    new TwitcastingWebhookRegistration("user-b", "livestart"),
                    new TwitcastingWebhookRegistration("user-d", "livestart"),
                ]);

            Assert.Equal(
                [
                    new TwitcastingWebhookAction(TwitcastingWebhookActionKind.RegisterLiveStart, "user-a"),
                    new TwitcastingWebhookAction(TwitcastingWebhookActionKind.RegisterLiveStart, "user-c"),
                    new TwitcastingWebhookAction(TwitcastingWebhookActionKind.RemoveLiveStart, "user-b"),
                    new TwitcastingWebhookAction(TwitcastingWebhookActionKind.RemoveLiveStart, "user-d"),
                ],
                plan);
        }

        [Fact]
        public void LiveEndDoesNotSatisfyOrRemoveLiveStartRegistration()
        {
            var desiredPlan = TwitcastingWebhookRegistrationPlanner.Plan(
                ["user-a"],
                [new TwitcastingWebhookRegistration("user-a", "liveend")]);
            var undesiredPlan = TwitcastingWebhookRegistrationPlanner.Plan(
                [],
                [new TwitcastingWebhookRegistration("user-a", "liveend")]);

            Assert.Equal(
                [new TwitcastingWebhookAction(TwitcastingWebhookActionKind.RegisterLiveStart, "user-a")],
                desiredPlan);
            Assert.Empty(undesiredPlan);
        }

        [Fact]
        public void MixedEventsRemoveOnlyLiveStart()
        {
            var plan = TwitcastingWebhookRegistrationPlanner.Plan(
                [],
                [
                    new TwitcastingWebhookRegistration("user-a", "liveend"),
                    new TwitcastingWebhookRegistration("user-a", "livestart"),
                ]);

            Assert.Equal(
                [new TwitcastingWebhookAction(TwitcastingWebhookActionKind.RemoveLiveStart, "user-a")],
                plan);
        }

        [Fact]
        public void DuplicateAndBlankIdsAreIgnored()
        {
            var plan = TwitcastingWebhookRegistrationPlanner.Plan(
                [null, "", " ", "user-a", "user-a"],
                [
                    new TwitcastingWebhookRegistration(null, "livestart"),
                    new TwitcastingWebhookRegistration("user-b", "livestart"),
                    new TwitcastingWebhookRegistration("user-b", "livestart"),
                ]);

            Assert.Equal(
                [
                    new TwitcastingWebhookAction(TwitcastingWebhookActionKind.RegisterLiveStart, "user-a"),
                    new TwitcastingWebhookAction(TwitcastingWebhookActionKind.RemoveLiveStart, "user-b"),
                ],
                plan);
        }

        [Fact]
        public void EventComparisonRequiresExactWireValue()
        {
            var plan = TwitcastingWebhookRegistrationPlanner.Plan(
                ["user-a"],
                [new TwitcastingWebhookRegistration("user-a", "LiveStart")]);

            Assert.Equal(
                [new TwitcastingWebhookAction(TwitcastingWebhookActionKind.RegisterLiveStart, "user-a")],
                plan);
        }
    }
}
