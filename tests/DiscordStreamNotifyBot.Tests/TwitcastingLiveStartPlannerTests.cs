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
            var notification = TwitcastingLiveStartPlanner.CreateNotification(CreateEvent(), "音樂");

            Assert.Equal("twitcasting_jp", notification.ChannelId);
            Assert.Equal("TwitCasting", notification.ChannelTitle);
            Assert.Equal(12345, notification.StreamId);
            Assert.Equal(new DateTime(2024, 7, 3, 9, 46, 40, DateTimeKind.Utc), notification.StreamStartAt);
            Assert.Equal("音樂", notification.Category);
            Assert.False(notification.IsPrivate);
            Assert.False(notification.IsRecord);

            var entity = TwitcastingLiveStartPlanner.ToEntity(notification);
            Assert.Equal(notification.ChannelId, entity.ChannelId);
            Assert.Equal(notification.ChannelTitle, entity.ChannelTitle);
            Assert.Equal(notification.StreamId, entity.StreamId);
            Assert.Equal(notification.StreamTitle, entity.StreamTitle);
            Assert.Equal(notification.StreamSubTitle, entity.StreamSubTitle);
            Assert.Equal(notification.Category, entity.Category);
            Assert.Equal(notification.ThumbnailUrl, entity.ThumbnailUrl);
            Assert.Equal(notification.StreamStartAt, entity.StreamStartAt);
        }

        [Fact]
        public void ProtectedStreamIsMarkedPrivate()
        {
            Assert.True(TwitcastingLiveStartPlanner.CreateNotification(CreateEvent(isProtected: true), "音樂").IsPrivate);
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

            var notification = TwitcastingLiveStartPlanner.CreateNotification(startEvent, null);

            Assert.Equal("無標題", notification.StreamTitle);
            Assert.Equal(string.Empty, notification.StreamSubTitle);
            Assert.Equal(string.Empty, notification.ThumbnailUrl);
            Assert.Equal(string.Empty, notification.Category);
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
