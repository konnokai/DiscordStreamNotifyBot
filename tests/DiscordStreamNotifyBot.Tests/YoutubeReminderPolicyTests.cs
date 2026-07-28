using DiscordStreamNotifyBot.Scraper.Detection.Youtube;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class YoutubeReminderPolicyTests
    {
        private static readonly DateTime Now = new(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void StartAfterFourteenDaysIsIgnored()
        {
            var decision = YoutubeReminderPolicy.PlanStart(Now.AddDays(14).AddTicks(1), Now);

            Assert.Equal(YoutubeReminderStartAction.Ignore, decision.Action);
            Assert.Equal(TimeSpan.Zero, decision.Delay);
        }

        [Fact]
        public void StartExactlyFourteenDaysIsScheduledOneMinuteEarly()
        {
            var decision = YoutubeReminderPolicy.PlanStart(Now.AddDays(14), Now);

            Assert.Equal(YoutubeReminderStartAction.ScheduleTimer, decision.Action);
            Assert.Equal(TimeSpan.FromDays(14) - TimeSpan.FromMinutes(1), decision.Delay);
        }

        [Theory]
        [InlineData(60)]
        [InlineData(59)]
        [InlineData(0)]
        [InlineData(-60)]
        public void StartAtOrBeforeOneMinuteAheadRunsImmediately(int secondsAhead)
        {
            var decision = YoutubeReminderPolicy.PlanStart(Now.AddSeconds(secondsAhead), Now);

            Assert.Equal(YoutubeReminderStartAction.RunImmediately, decision.Action);
            Assert.Equal(TimeSpan.Zero, decision.Delay);
        }

        [Fact]
        public void PositiveSubSecondTimerDelayIsClampedToOneSecond()
        {
            var decision = YoutubeReminderPolicy.PlanStart(Now.AddMinutes(1).AddMilliseconds(500), Now);

            Assert.Equal(YoutubeReminderStartAction.ScheduleTimer, decision.Action);
            Assert.Equal(TimeSpan.FromSeconds(1), decision.Delay);
        }

        [Fact]
        public void NormalFutureStartUsesOneMinuteAdvance()
        {
            var decision = YoutubeReminderPolicy.PlanStart(Now.AddHours(2), Now);

            Assert.Equal(YoutubeReminderStartAction.ScheduleTimer, decision.Action);
            Assert.Equal(TimeSpan.FromHours(2) - TimeSpan.FromMinutes(1), decision.Delay);
        }

        [Theory]
        [InlineData(119, 0)]
        [InlineData(120, 1)]
        [InlineData(121, 1)]
        [InlineData(-1, 0)]
        public void ApiRecheckUsesStrictTwoMinuteGrace(int secondsAhead, int expected)
        {
            Assert.Equal(
                (YoutubeReminderApiAction)expected,
                YoutubeReminderPolicy.DecideApiRecheck(Now.AddSeconds(secondsAhead), Now));
        }

        [Fact]
        public void UnchangedBatchTimeKeepsExistingTimer()
        {
            Assert.Equal(
                YoutubeReminderBatchChangeAction.Unchanged,
                YoutubeReminderPolicy.PlanBatchChange(Now.AddHours(1), Now.AddHours(1), Now));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void BatchChangeToPastOrNowRemovesWithoutReplacement(int secondsAhead)
        {
            Assert.Equal(
                YoutubeReminderBatchChangeAction.RemoveWithoutReplacement,
                YoutubeReminderPolicy.PlanBatchChange(Now.AddHours(1), Now.AddSeconds(secondsAhead), Now));
        }

        [Fact]
        public void BatchChangeExactlyFourteenDaysRemovesWithoutReplacement()
        {
            Assert.Equal(
                YoutubeReminderBatchChangeAction.RemoveWithoutReplacement,
                YoutubeReminderPolicy.PlanBatchChange(Now.AddHours(1), Now.AddDays(14), Now));
        }

        [Fact]
        public void BatchChangeAfterFourteenDaysRemovesWithoutReplacement()
        {
            Assert.Equal(
                YoutubeReminderBatchChangeAction.RemoveWithoutReplacement,
                YoutubeReminderPolicy.PlanBatchChange(Now.AddHours(1), Now.AddDays(14).AddTicks(1), Now));
        }

        [Fact]
        public void BatchChangeInsideFourteenDaysReplacesTimer()
        {
            Assert.Equal(
                YoutubeReminderBatchChangeAction.PublishAndReplaceTimer,
                YoutubeReminderPolicy.PlanBatchChange(Now.AddHours(1), Now.AddDays(14).AddTicks(-1), Now));
        }

        [Fact]
        public void BatchChangeWithinOneMinutePublishesAndRunsImmediately()
        {
            Assert.Equal(
                YoutubeReminderBatchChangeAction.PublishAndRunImmediately,
                YoutubeReminderPolicy.PlanBatchChange(Now.AddHours(1), Now.AddSeconds(30), Now));
        }
    }
}
