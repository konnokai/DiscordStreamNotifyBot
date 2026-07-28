using DiscordStreamNotifyBot.Scraper.Detection.Youtube;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class YoutubeApiVideoPolicyTests
    {
        private static readonly DateTime PublishedAt = new(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void UploadIsClassifiedAsNewVideo()
        {
            var decision = Classify(hasLiveStreamingDetails: false);

            Assert.Equal(YoutubeApiVideoAction.NewVideo, decision.Action);
            Assert.Equal(PublishedAt, decision.EventTime);
        }

        [Theory]
        [InlineData(true, true, 1)]
        [InlineData(true, false, 2)]
        [InlineData(false, true, 2)]
        public void OnlyFifteenSecondUploadWithDisabledCommentsIsIgnored(
            bool isFifteenSecondUpload,
            bool commentsDisabled,
            int expected)
        {
            var decision = YoutubeApiVideoPolicy.Classify(new YoutubeApiVideoFacts(
                false,
                PublishedAt,
                null,
                null,
                false,
                isFifteenSecondUpload,
                commentsDisabled));

            Assert.Equal((YoutubeApiVideoAction)expected, decision.Action);
        }

        [Fact]
        public void ActualStartTakesPrecedenceOverScheduleAndActiveChat()
        {
            var actualStart = PublishedAt.AddHours(1);
            var decision = Classify(
                actualStartTime: actualStart,
                scheduledStartTime: PublishedAt.AddHours(2),
                hasActiveLiveChat: true);

            Assert.Equal(YoutubeApiVideoAction.Started, decision.Action);
            Assert.Equal(actualStart, decision.EventTime);
        }

        [Fact]
        public void ScheduledVideoUsesScheduledStart()
        {
            var scheduledStart = PublishedAt.AddDays(1);
            var decision = Classify(scheduledStartTime: scheduledStart);

            Assert.Equal(YoutubeApiVideoAction.Scheduled, decision.Action);
            Assert.Equal(scheduledStart, decision.EventTime);
        }

        [Fact]
        public void ActiveChatWithoutStartTimesIsKeptAsActiveChatOnly()
        {
            var decision = Classify(hasActiveLiveChat: true);

            Assert.Equal(YoutubeApiVideoAction.ActiveChatOnly, decision.Action);
            Assert.Equal(PublishedAt, decision.EventTime);
        }

        [Fact]
        public void LiveDetailsWithoutAnyRecognizedStateAreIgnored()
        {
            Assert.Equal(YoutubeApiVideoAction.Ignore, Classify().Action);
        }

        private static YoutubeApiVideoDecision Classify(
            bool hasLiveStreamingDetails = true,
            DateTime? actualStartTime = null,
            DateTime? scheduledStartTime = null,
            bool hasActiveLiveChat = false)
            => YoutubeApiVideoPolicy.Classify(new YoutubeApiVideoFacts(
                hasLiveStreamingDetails,
                PublishedAt,
                actualStartTime,
                scheduledStartTime,
                hasActiveLiveChat,
                false,
                false));
    }
}
