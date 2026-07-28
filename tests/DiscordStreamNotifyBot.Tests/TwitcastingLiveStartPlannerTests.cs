using DiscordStreamNotifyBot.HttpClients.Twitcasting.Model;
using DiscordStreamNotifyBot.Scraper.Detection.Twitcasting;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class TwitcastingLiveStartPlannerTests
    {
        private const string ValidPayload = """
            {
              "event": "livestart",
              "signature": "signature",
              "movie": {
                "id": "12345",
                "user_id": "182224938",
                "title": "直播標題",
                "subtitle": "副標題",
                "category": "music",
                "large_thumbnail": "https://example.com/large.jpg",
                "created": 1720000000,
                "is_protected": false
              },
              "broadcaster": {
                "id": "182224938",
                "screen_id": "twitcasting_jp",
                "name": "TwitCasting"
              },
              "unknown_official_field": true
            }
            """;

        [Fact]
        public void ValidLiveStartPayloadParsesImmutableFacts()
        {
            Assert.True(TwitcastingWebhookParser.TryParseLiveStart(ValidPayload, out var result));

            Assert.Equal("182224938", result.UserId);
            Assert.Equal("twitcasting_jp", result.ScreenId);
            Assert.Equal("TwitCasting", result.ChannelTitle);
            Assert.Equal(12345, result.StreamId);
            Assert.Equal("直播標題", result.StreamTitle);
            Assert.Equal("副標題", result.StreamSubTitle);
            Assert.Equal("music", result.CategoryId);
            Assert.Equal("https://example.com/large.jpg", result.ThumbnailUrl);
            Assert.Equal(1720000000, result.CreatedAtUnixSeconds);
            Assert.False(result.IsProtected);
        }

        [Theory]
        [InlineData("")]
        [InlineData("not-json")]
        [InlineData("{}")]
        [InlineData("{\"event\":\"liveend\",\"movie\":{},\"broadcaster\":{}}")]
        public void InvalidPayloadReturnsFalseWithoutThrowing(string json)
        {
            Assert.False(TwitcastingWebhookParser.TryParseLiveStart(json, out var result));
            Assert.Null(result);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("0")]
        [InlineData("2147483648")]
        public void InvalidMovieIdReturnsFalse(string movieId)
        {
            string json = ValidPayload.Replace("\"12345\"", $"\"{movieId}\"");

            Assert.False(TwitcastingWebhookParser.TryParseLiveStart(json, out _));
        }

        [Fact]
        public void MovieUserIdMustMatchBroadcasterId()
        {
            string json = ValidPayload.Replace("\"user_id\": \"182224938\"", "\"user_id\": \"other\"");

            Assert.False(TwitcastingWebhookParser.TryParseLiveStart(json, out _));
        }

        [Fact]
        public void StreamMappingUsesScreenIdAndUtcTimestamp()
        {
            var plan = TwitcastingLiveStartPlanner.Plan(new TwitcastingLiveStartFacts(
                CreateEvent(),
                StreamAlreadyExists: false,
                IsRecordingEnabled: true,
                ResolvedCategoryName: "音樂"));

            Assert.Equal(TwitcastingLiveStartAction.PersistRequestRecordingAndNotify, plan.Action);
            Assert.Equal("twitcasting_jp", plan.Stream.ChannelId);
            Assert.Equal(new DateTime(2024, 7, 3, 9, 46, 40, DateTimeKind.Utc), plan.Stream.StreamStartAt);
            Assert.Equal("音樂", plan.Stream.Category);

            var entity = TwitcastingLiveStartPlanner.ToEntity(plan.Stream);
            Assert.Equal(plan.Stream.ChannelId, entity.ChannelId);
            Assert.Equal(plan.Stream.ChannelTitle, entity.ChannelTitle);
            Assert.Equal(plan.Stream.StreamId, entity.StreamId);
            Assert.Equal(plan.Stream.StreamTitle, entity.StreamTitle);
            Assert.Equal(plan.Stream.StreamSubTitle, entity.StreamSubTitle);
            Assert.Equal(plan.Stream.Category, entity.Category);
            Assert.Equal(plan.Stream.ThumbnailUrl, entity.ThumbnailUrl);
            Assert.Equal(plan.Stream.StreamStartAt, entity.StreamStartAt);
        }

        [Theory]
        [InlineData(true, true, 1)]
        [InlineData(true, false, 1)]
        [InlineData(false, false, 1)]
        [InlineData(false, true, 2)]
        public void RecordingDecisionRequiresPublicStreamAndEnabledSpider(
            bool isProtected,
            bool isRecordingEnabled,
            int expected)
        {
            var plan = TwitcastingLiveStartPlanner.Plan(new TwitcastingLiveStartFacts(
                CreateEvent(isProtected),
                StreamAlreadyExists: false,
                isRecordingEnabled,
                "音樂"));

            Assert.Equal((TwitcastingLiveStartAction)expected, plan.Action);
        }

        [Fact]
        public void DuplicateStreamHasNoSideEffectPlan()
        {
            var plan = TwitcastingLiveStartPlanner.Plan(new TwitcastingLiveStartFacts(
                CreateEvent(),
                StreamAlreadyExists: true,
                IsRecordingEnabled: true,
                ResolvedCategoryName: "音樂"));

            Assert.Equal(TwitcastingLiveStartAction.IgnoreDuplicate, plan.Action);
            Assert.Null(plan.Stream);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void NotificationUsesActualRecordingDelegationResult(bool recordingDelegated, bool expected)
        {
            var plan = TwitcastingLiveStartPlanner.Plan(new TwitcastingLiveStartFacts(
                CreateEvent(), false, true, "音樂"));

            var notification = TwitcastingLiveStartPlanner.CreateNotification(plan, recordingDelegated);

            Assert.Equal("twitcasting_jp", notification.ChannelId);
            Assert.Equal(12345, notification.StreamId);
            Assert.Equal(expected, notification.IsRecord);
            Assert.False(notification.IsPrivate);
        }

        [Fact]
        public void SkippedRecordingCannotProduceRecordedNotification()
        {
            var plan = TwitcastingLiveStartPlanner.Plan(new TwitcastingLiveStartFacts(
                CreateEvent(isProtected: true), false, true, "音樂"));

            var notification = TwitcastingLiveStartPlanner.CreateNotification(plan, recordingDelegated: true);

            Assert.False(notification.IsRecord);
            Assert.True(notification.IsPrivate);
        }

        [Fact]
        public void NullTitleUsesFallbackAndOptionalStringsUseEmptyValues()
        {
            var startEvent = CreateEvent() with
            {
                StreamTitle = null,
                StreamSubTitle = null,
                ThumbnailUrl = null,
            };

            var plan = TwitcastingLiveStartPlanner.Plan(new TwitcastingLiveStartFacts(
                startEvent, false, false, null));

            Assert.Equal("無標題", plan.Stream.StreamTitle);
            Assert.Equal(string.Empty, plan.Stream.StreamSubTitle);
            Assert.Equal(string.Empty, plan.Stream.ThumbnailUrl);
            Assert.Equal(string.Empty, plan.Stream.Category);
        }

        [Fact]
        public void CategoryResolutionUsesSubcategoryNameAndFallsBackToId()
        {
            var categories = new[]
            {
                new Category
                {
                    SubCategories =
                    [
                        new SubCategory { Id = "music", Name = "音樂" },
                    ],
                },
            };

            Assert.Equal("音樂", TwitcastingLiveStartPlanner.ResolveCategoryName("music", categories));
            Assert.Equal("unknown", TwitcastingLiveStartPlanner.ResolveCategoryName("unknown", categories));
            Assert.Equal(string.Empty, TwitcastingLiveStartPlanner.ResolveCategoryName(null, categories));
        }

        private static TwitcastingLiveStartEvent CreateEvent(bool isProtected = false)
        {
            return new TwitcastingLiveStartEvent(
                "182224938",
                "twitcasting_jp",
                "TwitCasting",
                12345,
                "直播標題",
                "副標題",
                "music",
                "https://example.com/large.jpg",
                1720000000,
                isProtected);
        }
    }
}
