using DiscordStreamNotifyBot.Scraper.Detection.Youtube;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class YoutubeReminderReconciliationPolicyTests
    {
        private static readonly DateTime Now = new(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime PreviousStart = Now.AddHours(1);

        [Fact]
        public void MissingApiVideoPublishesDeleteAndRemovesReminder()
        {
            Assert.Equal(
                YoutubeReminderReconciliationAction.PublishDeleteAndRemove,
                Reconcile(apiVideoFound: false));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        public void MissingLiveDetailsOrScheduledTimeTreatsVideoAsStarted(
            bool hasLiveStreamingDetails,
            bool hasScheduledStartTime)
        {
            Assert.Equal(
                YoutubeReminderReconciliationAction.PublishStartAndRemove,
                Reconcile(
                    hasLiveStreamingDetails: hasLiveStreamingDetails,
                    hasScheduledStartTime: hasScheduledStartTime));
        }

        [Fact]
        public void InvalidScheduledTimeKeepsExistingReminder()
        {
            Assert.Equal(
                YoutubeReminderReconciliationAction.KeepExisting,
                YoutubeReminderPolicy.ReconcileBatch(
                    true,
                    true,
                    true,
                    null,
                    PreviousStart,
                    Now));
        }

        [Fact]
        public void UnchangedScheduledTimeKeepsExistingReminder()
        {
            Assert.Equal(
                YoutubeReminderReconciliationAction.KeepExisting,
                Reconcile(scheduledStartTime: PreviousStart));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void PastOrCurrentReplacementIsRemovedWithoutNewTimer(int secondsAhead)
        {
            Assert.Equal(
                YoutubeReminderReconciliationAction.RemoveWithoutReplacement,
                Reconcile(scheduledStartTime: Now.AddSeconds(secondsAhead)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void FourteenDayBoundaryIsRemovedWithoutNewTimer(int ticksAfter)
        {
            Assert.Equal(
                YoutubeReminderReconciliationAction.RemoveWithoutReplacement,
                Reconcile(scheduledStartTime: Now.AddDays(14).AddTicks(ticksAfter)));
        }

        [Theory]
        [InlineData(30)]
        [InlineData(2 * 60 * 60)]
        public void FutureReplacementPublishesChange(int secondsAhead)
        {
            Assert.Equal(
                YoutubeReminderReconciliationAction.PublishChange,
                Reconcile(scheduledStartTime: Now.AddSeconds(secondsAhead)));
        }

        [Fact]
        public void ReplacementJustInsideFourteenDaysPublishesChange()
        {
            Assert.Equal(
                YoutubeReminderReconciliationAction.PublishChange,
                Reconcile(scheduledStartTime: Now.AddDays(14).AddTicks(-1)));
        }

        private static YoutubeReminderReconciliationAction Reconcile(
            bool apiVideoFound = true,
            bool hasLiveStreamingDetails = true,
            bool hasScheduledStartTime = true,
            DateTime? scheduledStartTime = null)
            => YoutubeReminderPolicy.ReconcileBatch(
                apiVideoFound,
                hasLiveStreamingDetails,
                hasScheduledStartTime,
                scheduledStartTime ?? (hasScheduledStartTime ? PreviousStart : null),
                PreviousStart,
                Now);
    }
}
