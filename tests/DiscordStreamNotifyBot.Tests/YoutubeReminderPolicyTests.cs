using DiscordStreamNotifyBot.Scraper.Detection.Youtube;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class YoutubeReminderPolicyTests
    {
        private static readonly DateTime Now = new(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void StartAfterFourteenDaysIsIgnored()
        {
            Assert.Null(YoutubeReminderPolicy.GetReminderDelay(Now.AddDays(14).AddTicks(1), Now));
        }

        [Fact]
        public void StartExactlyFourteenDaysIsScheduledOneMinuteEarly()
        {
            Assert.Equal(TimeSpan.FromDays(14) - TimeSpan.FromMinutes(1),
                YoutubeReminderPolicy.GetReminderDelay(Now.AddDays(14), Now));
        }

        [Theory]
        [InlineData(60)]
        [InlineData(59)]
        [InlineData(0)]
        [InlineData(-60)]
        public void StartAtOrBeforeOneMinuteAheadRunsImmediately(int secondsAhead)
        {
            Assert.Equal(TimeSpan.Zero, YoutubeReminderPolicy.GetReminderDelay(Now.AddSeconds(secondsAhead), Now));
        }

        [Fact]
        public void PositiveSubSecondTimerDelayIsClampedToOneSecond()
        {
            Assert.Equal(TimeSpan.FromSeconds(1),
                YoutubeReminderPolicy.GetReminderDelay(Now.AddMinutes(1).AddMilliseconds(500), Now));
        }

        [Fact]
        public void NormalFutureStartUsesOneMinuteAdvance()
        {
            Assert.Equal(TimeSpan.FromHours(2) - TimeSpan.FromMinutes(1),
                YoutubeReminderPolicy.GetReminderDelay(Now.AddHours(2), Now));
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
    }
}
