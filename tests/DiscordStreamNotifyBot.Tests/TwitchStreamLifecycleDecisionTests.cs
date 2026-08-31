using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Scraper.Detection.Twitch;
using DiscordStreamNotifyBot.Shared.Messages;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class TwitchStreamLifecycleDecisionTests
    {
        [Theory]
        [InlineData(true, false, false, "PersistStreamAndRefreshState")]
        [InlineData(false, true, false, "PersistStreamAndRefreshState")]
        [InlineData(false, false, true, "RefreshStateOnly")]
        public void DuplicateSuppressesNotificationAndPersistsOnlyMissingDatabaseRecord(
            bool processDuplicate, bool redisDuplicate, bool databaseDuplicate,
            string expected)
        {
            var action = TwitchStreamStartPolicy.Decide(new TwitchStreamStartFacts(
                "stream", "user", HasSpider: true,
                processDuplicate, redisDuplicate, databaseDuplicate));

            Assert.Equal(Enum.Parse<TwitchStreamStartAction>(expected), action);
        }

        [Fact]
        public void ResumeBeforeOfflineConfirmationPersistsWithoutPublishingAndMarksFollowingSources()
        {
            var handledStreamIds = new ConcurrentDictionary<string, byte>();

            bool processDuplicate = TwitchDetectionService.RecordAndCheckProcessDuplicate(
                handledStreamIds, "new-stream", resumedBeforeOfflineConfirmation: true);
            var action = TwitchStreamStartPolicy.Decide(new TwitchStreamStartFacts(
                "new-stream", "user", HasSpider: true,
                processDuplicate, RedisDuplicate: false, DatabaseDuplicate: false));

            Assert.Equal(TwitchStreamStartAction.PersistStreamAndRefreshState, action);
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
            var action = TwitchStreamStartPolicy.Decide(new TwitchStreamStartFacts(
                "following-stream", "user", HasSpider: true,
                processDuplicate, RedisDuplicate: false, DatabaseDuplicate: false));

            Assert.Equal(TwitchStreamStartAction.PublishStart, action);
        }

        [Fact]
        public void NewValidStreamPublishesStartWhileMissingSpiderIsIgnored()
        {
            var publish = TwitchStreamStartPolicy.Decide(new TwitchStreamStartFacts(
                "stream", "user", HasSpider: true,
                ProcessDuplicate: false, RedisDuplicate: false, DatabaseDuplicate: false));
            var missingSpider = TwitchStreamStartPolicy.Decide(new TwitchStreamStartFacts(
                "stream", "user", HasSpider: false,
                ProcessDuplicate: false, RedisDuplicate: false, DatabaseDuplicate: false));

            Assert.Equal(TwitchStreamStartAction.PublishStart, publish);
            Assert.Equal(TwitchStreamStartAction.IgnoreMissingSpider, missingSpider);
        }

        [Fact]
        public void RepeatedMissingSpiderPollingOnlyRecordsFirstPendingObservation()
        {
            var pendingCleanup = new ConcurrentDictionary<string, byte>();

            Assert.True(TwitchDetectionService.RecordPendingCleanup(pendingCleanup, "user"));
            Assert.False(TwitchDetectionService.RecordPendingCleanup(pendingCleanup, "user"));
        }

        [Theory]
        [InlineData(null, "user")]
        [InlineData("stream", "")]
        public void InvalidStreamIdentityIsIgnored(string streamId, string userId)
        {
            var action = TwitchStreamStartPolicy.Decide(new TwitchStreamStartFacts(
                streamId, userId, HasSpider: true,
                ProcessDuplicate: false, RedisDuplicate: false, DatabaseDuplicate: false));

            Assert.Equal(TwitchStreamStartAction.IgnoreInvalid, action);
        }

        [Fact]
        public void StartFactoryBuildsStateAndWireDtoWithoutChangingContract()
        {
            DateTime startedAt = new(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);
            var state = TwitchStreamNotificationFactory.CreateState(new TwitchStreamDataFacts(
                "stream-id",
                "title",
                startedAt,
                "user-id",
                "user_login",
                "User Name",
                "Game",
                "https://image.test/{width}x{height}.jpg"));

            TwitchNotification dto = TwitchStreamNotificationFactory.CreateStart(state, isRecord: true);

            Assert.Equal("https://image.test/854x480.jpg", state.ThumbnailUrl);
            Assert.Equal(TwitchNoticeType.StartStream, dto.NoticeType);
            Assert.Equal("stream-id", dto.StreamId);
            Assert.Equal("user-id", dto.UserId);
            Assert.Equal("user_login", dto.UserLogin);
            Assert.Equal("User Name", dto.UserName);
            Assert.Equal("title", dto.StreamTitle);
            Assert.Equal("Game", dto.GameName);
            Assert.Equal(startedAt, dto.StreamStartAt);
            Assert.True(dto.IsRecord);
        }

        [Theory]
        [InlineData(false, false, false, true, true, "Defer")]
        [InlineData(true, true, false, true, true, "ResumeStream")]
        [InlineData(true, false, true, true, true, "Defer")]
        [InlineData(true, false, false, false, true, "ClearState")]
        [InlineData(true, false, false, true, true, "PublishEnd")]
        [InlineData(true, false, false, true, false, "Ignore")]
        public void OfflineDecisionSeparatesRetryResumeClearPublishAndIgnore(
            bool lookupSucceeded,
            bool resumed,
            bool cleanupDeferred,
            bool publishRequested,
            bool hasState,
            string expected)
        {
            var action = TwitchOfflinePolicy.Decide(new TwitchOfflineFacts(
                lookupSucceeded,
                resumed,
                cleanupDeferred,
                publishRequested,
                hasState,
                HasSpider: false));

            Assert.Equal(Enum.Parse<TwitchOfflineAction>(expected), action);
        }

        [Fact]
        public void ConfirmedEndedStateIgnoresRepeatedOfflineEvent()
        {
            var action = TwitchOfflinePolicy.Decide(new TwitchOfflineFacts(
                StreamLookupSucceeded: true,
                HasResumedStream: false,
                CleanupStillDeferredForLive: false,
                PublishEndRequested: true,
                HasStreamState: true,
                HasSpider: true,
                AlreadyEnded: true));

            Assert.Equal(TwitchOfflineAction.Ignore, action);
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

        [Fact]
        public void StreamNotificationMarkerExpiresAfterOneDay()
        {
            Assert.Equal(TimeSpan.FromDays(1), TwitchDetectionService.StreamNotificationTtl);
        }

        [Theory]
        [InlineData(false, false, "Schedule")]
        [InlineData(false, true, "ReplaceExisting")]
        [InlineData(true, false, "KeepExisting")]
        [InlineData(true, true, "ReplaceExisting")]
        public void OfflineDebounceScheduleHasExplicitReplacementDecision(
            bool hasExisting, bool replaceExisting, string expected)
        {
            Assert.Equal(Enum.Parse<TwitchOfflineScheduleAction>(expected),
                TwitchOfflineSchedulePolicy.Decide(hasExisting, replaceExisting));
        }
    }
}
