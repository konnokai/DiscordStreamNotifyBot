using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Scraper.Detection.Twitch;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class TwitchStreamLifecycleDecisionTests
    {
        [Fact]
        public void ResumeBeforeOfflineConfirmationIsDuplicateAndMarksFollowingSources()
        {
            var handledStreamIds = new ConcurrentDictionary<string, byte>();

            bool processDuplicate = TwitchDetectionService.RecordAndCheckProcessDuplicate(
                handledStreamIds, "new-stream", resumedBeforeOfflineConfirmation: true);

            Assert.True(processDuplicate);
            Assert.True(TwitchDetectionService.RecordAndCheckProcessDuplicate(
                handledStreamIds, "new-stream", resumedBeforeOfflineConfirmation: false));
        }

        [Fact]
        public void ConfirmedOfflineAllowsFollowingStreamToPublish()
        {
            var handledStreamIds = new ConcurrentDictionary<string, byte>();
            TwitchDetectionService.RecordAndCheckProcessDuplicate(
                handledStreamIds, "resumed-stream", resumedBeforeOfflineConfirmation: true);

            handledStreamIds.TryRemove("resumed-stream", out _);
            bool processDuplicate = TwitchDetectionService.RecordAndCheckProcessDuplicate(
                handledStreamIds, "following-stream", resumedBeforeOfflineConfirmation: false);

            Assert.False(processDuplicate);
        }

        [Fact]
        public void StreamEndStateSurvivesRedisSerialization()
        {
            DateTime endAt = new(2026, 8, 31, 12, 0, 0, DateTimeKind.Utc);
            var state = new TwitchStream { StreamId = "stream", StreamEndAt = endAt };

            var restored = JsonConvert.DeserializeObject<TwitchStream>(JsonConvert.SerializeObject(state));

            Assert.NotNull(restored);
            Assert.Equal(endAt, restored.StreamEndAt);
        }
    }
}
