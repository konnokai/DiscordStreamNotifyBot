using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Interaction;

namespace DiscordStreamNotifyBot.Tests.Component.MySql
{
    [Collection(MySqlComponentCollection.Name)]
    [Trait("Category", "MySqlComponent")]
    public sealed class VideoQueryAndConcurrencyTests
    {
        private readonly MySqlComponentFixture _fixture;

        public VideoQueryAndConcurrencyTests(MySqlComponentFixture fixture)
        {
            _fixture = fixture;
        }

        [MySqlComponentFact]
        public async Task VideoLookupSearchesBothTables()
        {
            var suffix = Guid.NewGuid().ToString("N");
            var expected = new Video[]
            {
                CreateVideo<OtherVideos>($"other-{suffix}", Video.YTChannelType.Other),
                CreateVideo<NonApprovedVideos>($"non-approved-{suffix}", Video.YTChannelType.NonApproved)
            };

            await using (var db = _fixture.DbService.GetDbContext())
            {
                db.AddRange(expected);
                await db.SaveChangesAsync();
            }

            foreach (var video in expected)
            {
                Assert.True(SharedExtensions.HasStreamVideoByVideoId($" {video.VideoId} "));
                var result = SharedExtensions.GetStreamVideoByVideoId($" {video.VideoId} ");
                Assert.NotNull(result);
                Assert.Equal(video.VideoId, result.VideoId);
                Assert.Equal(video.ChannelType, result.ChannelType);
            }
        }

        [MySqlComponentFact]
        public async Task VideoLookupUsesOtherThenNonApprovedPrecedence()
        {
            var videoId = $"precedence-{Guid.NewGuid():N}";
            var channelId = $"channel-{Guid.NewGuid():N}";
            var now = DateTime.UtcNow;

            await using (var db = _fixture.DbService.GetDbContext())
            {
                db.OtherVideos.Add(CreateVideo<OtherVideos>(videoId, Video.YTChannelType.Other, channelId, now));
                db.NonApprovedVideos.Add(CreateVideo<NonApprovedVideos>(videoId, Video.YTChannelType.NonApproved, channelId, now.AddMinutes(1)));
                await db.SaveChangesAsync();
            }

            Assert.IsType<OtherVideos>(SharedExtensions.GetStreamVideoByVideoId(videoId));
            Assert.IsType<OtherVideos>(SharedExtensions.GetLastStreamVideoByChannelId(channelId));
        }

        private static T CreateVideo<T>(
            string videoId,
            Video.YTChannelType channelType,
            string channelId = null,
            DateTime? scheduledStartTime = null)
            where T : Video, new()
            => new()
            {
                VideoId = videoId,
                ChannelId = channelId ?? $"channel-{videoId}",
                ChannelTitle = $"Channel {channelType}",
                VideoTitle = $"Video {channelType}",
                ScheduledStartTime = scheduledStartTime ?? DateTime.UtcNow,
                ChannelType = channelType
            };
    }
}
