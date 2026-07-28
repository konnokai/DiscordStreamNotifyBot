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
                YoutubeReminderPolicy.ReconcileBatch(new YoutubeReminderBatchFacts(
                    true,
                    true,
                    true,
                    null,
                    PreviousStart,
                    Now)));
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

        [Fact]
        public void FourteenDayBoundaryIsRemovedWithoutNewTimer()
        {
            Assert.Equal(
                YoutubeReminderReconciliationAction.RemoveWithoutReplacement,
                Reconcile(scheduledStartTime: Now.AddDays(14)));
        }

        [Fact]
        public void NearReplacementPublishesChangeAndRunsImmediately()
        {
            Assert.Equal(
                YoutubeReminderReconciliationAction.PublishChangeAndRunImmediately,
                Reconcile(scheduledStartTime: Now.AddSeconds(30)));
        }

        [Fact]
        public void FutureReplacementPublishesChangeAndReplacesTimer()
        {
            Assert.Equal(
                YoutubeReminderReconciliationAction.PublishChangeAndReplaceTimer,
                Reconcile(scheduledStartTime: Now.AddHours(2)));
        }

        private static YoutubeReminderReconciliationAction Reconcile(
            bool apiVideoFound = true,
            bool hasLiveStreamingDetails = true,
            bool hasScheduledStartTime = true,
            DateTime? scheduledStartTime = null)
            => YoutubeReminderPolicy.ReconcileBatch(new YoutubeReminderBatchFacts(
                apiVideoFound,
                hasLiveStreamingDetails,
                hasScheduledStartTime,
                scheduledStartTime ?? (hasScheduledStartTime ? PreviousStart : null),
                PreviousStart,
                Now));
    }
}
