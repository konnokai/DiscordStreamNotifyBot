using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Interaction;
using Microsoft.EntityFrameworkCore;

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
        public async Task VideoLookupSearchesAllFourTables()
        {
            var suffix = Guid.NewGuid().ToString("N");
            var expected = new Video[]
            {
                CreateVideo<HoloVideos>($"holo-{suffix}", Video.YTChannelType.Holo),
                CreateVideo<NijisanjiVideos>($"niji-{suffix}", Video.YTChannelType.Nijisanji),
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
        public async Task VideoLookupUsesHoloNijisanjiOtherNonApprovedPrecedence()
        {
            var videoId = $"precedence-{Guid.NewGuid():N}";
            var channelId = $"channel-{Guid.NewGuid():N}";
            var now = DateTime.UtcNow;

            await using (var db = _fixture.DbService.GetDbContext())
            {
                db.HoloVideos.Add(CreateVideo<HoloVideos>(videoId, Video.YTChannelType.Holo, channelId, now));
                db.NijisanjiVideos.Add(CreateVideo<NijisanjiVideos>(videoId, Video.YTChannelType.Nijisanji, channelId, now.AddMinutes(1)));
                db.OtherVideos.Add(CreateVideo<OtherVideos>(videoId, Video.YTChannelType.Other, channelId, now.AddMinutes(2)));
                db.NonApprovedVideos.Add(CreateVideo<NonApprovedVideos>(videoId, Video.YTChannelType.NonApproved, channelId, now.AddMinutes(3)));
                await db.SaveChangesAsync();
            }

            Assert.IsType<HoloVideos>(SharedExtensions.GetStreamVideoByVideoId(videoId));
            Assert.IsType<HoloVideos>(SharedExtensions.GetLastStreamVideoByChannelId(channelId));
        }

        [MySqlComponentFact]
        public async Task DeleteThenStaleUpdateRaisesConcurrencyException()
        {
            var userId = (ulong)Random.Shared.NextInt64(1, long.MaxValue);
            await using (var seedDb = _fixture.DbService.GetDbContext())
            {
                seedDb.YoutubeMemberAccessToken.Add(new YoutubeMemberAccessToken
                {
                    DiscordUserId = userId,
                    EncryptedAccessToken = "before-delete"
                });
                await seedDb.SaveChangesAsync();
            }

            await using var deleteDb = _fixture.DbService.GetDbContext();
            await using var staleDb = _fixture.DbService.GetDbContext();
            var deletedEntity = await deleteDb.YoutubeMemberAccessToken.SingleAsync(x => x.DiscordUserId == userId);
            var staleEntity = await staleDb.YoutubeMemberAccessToken.SingleAsync(x => x.DiscordUserId == userId);

            deleteDb.YoutubeMemberAccessToken.Remove(deletedEntity);
            await deleteDb.SaveChangesAsync();

            staleEntity.EncryptedAccessToken = "stale-update";
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleDb.SaveChangesAsync());
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
