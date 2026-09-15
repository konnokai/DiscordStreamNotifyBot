using DiscordStreamNotifyBot.Shared.Messages;
using Newtonsoft.Json;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class NotificationDedupPolicyTests
    {
        [Fact]
        public void YoutubeKeyUsesVideoAndNoticeType()
        {
            var json = JsonConvert.SerializeObject(new YoutubeNotification
            {
                VideoId = "video-1",
                NoticeType = YoutubeNoticeType.Start,
            });

            var key = NotificationDedupPolicy.TryGetKey(2, NotifyType.Youtube, json);

            Assert.Equal("notified:2:yt:video-1:2", key);
        }

        [Theory]
        [InlineData(false, "False")]
        [InlineData(true, "True")]
        public void YoutubeDeleteKeySeparatesUnarchivedState(bool isUnarchived, string expectedValue)
        {
            var json = JsonConvert.SerializeObject(new YoutubeNotification
            {
                VideoId = "video-1",
                NoticeType = YoutubeNoticeType.Delete,
                IsUnarchived = isUnarchived,
            });

            var key = NotificationDedupPolicy.TryGetKey(2, NotifyType.Youtube, json);

            Assert.Equal($"notified:2:yt:video-1:5:{expectedValue}", key);
        }

        [Fact]
        public void YoutubeChangeTimeKeySeparatesDistinctScheduleChanges()
        {
            var first = JsonConvert.SerializeObject(new YoutubeNotification
            {
                VideoId = "video-1",
                NoticeType = YoutubeNoticeType.ChangeTime,
                PreviousScheduledStartTime = new DateTime(2026, 7, 28, 10, 0, 0, DateTimeKind.Utc),
                ScheduledStartTime = new DateTime(2026, 7, 28, 11, 0, 0, DateTimeKind.Utc),
            });
            var second = JsonConvert.SerializeObject(new YoutubeNotification
            {
                VideoId = "video-1",
                NoticeType = YoutubeNoticeType.ChangeTime,
                PreviousScheduledStartTime = new DateTime(2026, 7, 28, 11, 0, 0, DateTimeKind.Utc),
                ScheduledStartTime = new DateTime(2026, 7, 28, 12, 0, 0, DateTimeKind.Utc),
            });

            var firstKey = NotificationDedupPolicy.TryGetKey(2, NotifyType.Youtube, first);
            var duplicateKey = NotificationDedupPolicy.TryGetKey(2, NotifyType.Youtube, first);
            var secondKey = NotificationDedupPolicy.TryGetKey(2, NotifyType.Youtube, second);

            Assert.Equal(firstKey, duplicateKey);
            Assert.NotEqual(firstKey, secondKey);
            Assert.StartsWith("notified:2:yt:video-1:4:", firstKey);
        }

        [Theory]
        [InlineData(TwitchNoticeType.StartStream, 0)]
        [InlineData(TwitchNoticeType.EndStream, 1)]
        public void TwitchKeyUsesStreamAndNoticeType(TwitchNoticeType noticeType, int expectedNoticeType)
        {
            var json = JsonConvert.SerializeObject(new TwitchNotification
            {
                StreamId = "stream-1",
                UserId = "user-1",
                NoticeType = noticeType,
            });

            var key = NotificationDedupPolicy.TryGetKey(3, NotifyType.Twitch, json);

            Assert.Equal($"notified:3:tw:stream-1:{expectedNoticeType}", key);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void TwitchWithoutStreamIdDoesNotDeduplicate(string streamId)
        {
            var json = JsonConvert.SerializeObject(new TwitchNotification
            {
                StreamId = streamId,
                UserId = "user-1",
                NoticeType = TwitchNoticeType.ChangeStreamData,
            });

            Assert.Null(NotificationDedupPolicy.TryGetKey(3, NotifyType.Twitch, json));
        }

        [Fact]
        public void TwitcastingKeyUsesChannelAndStream()
        {
            var json = JsonConvert.SerializeObject(new TwitcastingNotification
            {
                ChannelId = "channel-1",
                StreamId = 42,
            });

            Assert.Equal(
                "notified:4:tc:channel-1:42",
                NotificationDedupPolicy.TryGetKey(4, NotifyType.Twitcasting, json));
        }

        [Fact]
        public void BannerKeyUsesChannelAndVideo()
        {
            var json = JsonConvert.SerializeObject(new BannerChangeNotification
            {
                ChannelId = "channel-1",
                VideoId = "video-1",
            });

            Assert.Equal(
                "notified:5:banner:channel-1:video-1",
                NotificationDedupPolicy.TryGetKey(5, NotifyType.Banner, json));
        }

        [Fact]
        public void MemberVideoLogKeyIncludesCodeArgumentsAndSideEffects()
        {
            var notification = new YoutubeMemberVideoLogNotification
            {
                CheckChannelId = "channel-1",
                MessageCode = "NewProbeVideo",
                MessageArguments = ["channel-1", "video-1"],
            };
            string Key() => NotificationDedupPolicy.TryGetKey(6, NotifyType.YoutubeMemberVideoLog,
                JsonConvert.SerializeObject(notification));

            string first = Key();
            Assert.Equal(first, Key());
            Assert.StartsWith("notified:6:ytmv:channel-1:", first);
            notification.MessageArguments[1] = "video-2";
            string second = Key();
            Assert.NotEqual(first, second);
            notification.IsNeedRemove = !notification.IsNeedRemove;
            Assert.NotEqual(second, Key());
        }

        [Fact]
        public void ChzzkKeyUsesStreamKeyAndNoticeType()
        {
            var start = NotificationDedupPolicy.TryGetKey(2, NotifyType.Chzzk,
                JsonConvert.SerializeObject(new ChzzkNotification
                {
                    StreamKey = "channel-1:2026-09-15 13:41:52",
                }));
            var end = NotificationDedupPolicy.TryGetKey(2, NotifyType.Chzzk,
                JsonConvert.SerializeObject(new ChzzkNotification
                {
                    StreamKey = "channel-1:2026-09-15 13:41:52",
                    NoticeType = ChzzkNoticeType.EndStream,
                }));

            Assert.Equal("notified:2:cz:channel-1:2026-09-15 13:41:52:0", start);
            Assert.Equal("notified:2:cz:channel-1:2026-09-15 13:41:52:1", end);
        }

        [Fact]
        public void ChzzkWithoutStreamKeyDoesNotDeduplicate()
        {
            var json = JsonConvert.SerializeObject(new ChzzkNotification
            {
                NoticeType = ChzzkNoticeType.StartStream,
            });

            Assert.Null(NotificationDedupPolicy.TryGetKey(2, NotifyType.Chzzk, json));
        }

        [Fact]
        public void SameNotificationUsesDifferentKeysForDifferentShards()
        {
            var json = JsonConvert.SerializeObject(new YoutubeNotification
            {
                VideoId = "video-1",
                NoticeType = YoutubeNoticeType.Start,
            });

            var shard0Key = NotificationDedupPolicy.TryGetKey(0, NotifyType.Youtube, json);
            var shard1Key = NotificationDedupPolicy.TryGetKey(1, NotifyType.Youtube, json);

            Assert.NotEqual(shard0Key, shard1Key);
            Assert.StartsWith("notified:0:", shard0Key);
            Assert.StartsWith("notified:1:", shard1Key);
        }

        [Theory]
        [InlineData("unknown", "{}")]
        [InlineData(NotifyType.Youtube, "not-json")]
        [InlineData(NotifyType.Youtube, null)]
        public void UnsupportedOrInvalidPayloadDoesNotDeduplicate(string type, string json)
        {
            Assert.Null(NotificationDedupPolicy.TryGetKey(0, type, json));
        }
    }
}
