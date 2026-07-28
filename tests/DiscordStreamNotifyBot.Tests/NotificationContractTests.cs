using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Shared.Messages;
using Newtonsoft.Json.Linq;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class NotificationContractTests
    {
        [Fact]
        public void NotifyTypeValuesMatchWireContract()
        {
            Assert.Equal("youtube", NotifyType.Youtube);
            Assert.Equal("twitch", NotifyType.Twitch);
            Assert.Equal("twitcasting", NotifyType.Twitcasting);
            Assert.Equal("banner", NotifyType.Banner);
            Assert.Equal("youtube_member_video_log", NotifyType.YoutubeMemberVideoLog);
        }

        [Fact]
        public void NoticeEnumValuesMatchWireContract()
        {
            Assert.Equal(
                new[] { 0, 1, 2, 3, 4, 5 },
                Enum.GetValues<YoutubeNoticeType>().Select(value => (int)value));
            Assert.Equal(
                new[] { "NewStream", "NewVideo", "Start", "End", "ChangeTime", "Delete" },
                Enum.GetNames<YoutubeNoticeType>());
            Assert.Equal(
                new[] { 0, 1, 2 },
                Enum.GetValues<TwitchNoticeType>().Select(value => (int)value));
            Assert.Equal(
                new[] { "StartStream", "EndStream", "ChangeStreamData" },
                Enum.GetNames<TwitchNoticeType>());
        }

        [Fact]
        public void YoutubeNotificationFieldsAndDefaultsMatchContract()
        {
            var json = JObject.FromObject(new YoutubeNotification());

            AssertFields(json,
                "ActualEndTime", "ActualStartTime", "ChannelId", "ChannelTitle", "ChannelType",
                "IsMemberOnly", "IsUnarchived", "NoticeType", "PreviousScheduledStartTime",
                "ScheduledStartTime", "VideoId", "VideoTitle");
            Assert.Equal(0, json.Value<int>("NoticeType"));
            Assert.Equal(0, json.Value<int>("ChannelType"));
            Assert.Equal(default, json.Value<DateTime>("ScheduledStartTime"));
            Assert.False(json.Value<bool>("IsMemberOnly"));
            Assert.False(json.Value<bool>("IsUnarchived"));
            Assert.Equal(JTokenType.Null, json["PreviousScheduledStartTime"].Type);
            Assert.Equal(JTokenType.Null, json["ActualStartTime"].Type);
            Assert.Equal(JTokenType.Null, json["ActualEndTime"].Type);
        }

        [Fact]
        public void TwitchNotificationFieldsAndDefaultsMatchContract()
        {
            var json = JObject.FromObject(new TwitchNotification());

            AssertFields(json,
                "Clips", "ClipsValue", "Description", "GameName", "IsRecord", "NoticeType",
                "StreamEndAt", "StreamId", "StreamStartAt", "StreamTitle", "ThumbnailUrl", "Updates",
                "UserId", "UserLogin", "UserName");
            Assert.Equal(0, json.Value<int>("NoticeType"));
            Assert.False(json.Value<bool>("IsRecord"));
            Assert.Equal(JTokenType.Null, json["StreamStartAt"].Type);
            Assert.Equal(JTokenType.Null, json["StreamEndAt"].Type);
            Assert.Equal(JTokenType.Null, json["Clips"].Type);
            Assert.Equal(JTokenType.Null, json["Updates"].Type);
        }

        [Fact]
        public void TwitchNestedInfoFieldsMatchContract()
        {
            AssertFields(JObject.FromObject(new TwitchClipInfo()),
                "CreatorName", "Title", "Url", "ViewCount");
            AssertFields(JObject.FromObject(new TwitchChannelUpdateInfo()),
                "ElapsedSeconds", "NewCategory", "NewTitle", "OldCategory", "OldTitle");
        }

        [Fact]
        public void TwitcastingNotificationFieldsAndDefaultsMatchContract()
        {
            var json = JObject.FromObject(new TwitcastingNotification());

            AssertFields(json,
                "Category", "ChannelId", "ChannelTitle", "IsPrivate", "IsRecord", "StreamId",
                "StreamStartAt", "StreamSubTitle", "StreamTitle", "ThumbnailUrl");
            Assert.Equal(0, json.Value<int>("StreamId"));
            Assert.Equal(default, json.Value<DateTime>("StreamStartAt"));
            Assert.False(json.Value<bool>("IsPrivate"));
            Assert.False(json.Value<bool>("IsRecord"));
        }

        [Fact]
        public void BannerChangeNotificationFieldsMatchContract()
        {
            AssertFields(JObject.FromObject(new BannerChangeNotification()), "ChannelId", "VideoId");
        }

        [Fact]
        public void YoutubeMemberVideoLogFieldsAndDefaultsMatchContract()
        {
            var json = JObject.FromObject(new YoutubeMemberVideoLogNotification());

            AssertFields(json,
                "BotOwnerMessage", "CheckChannelId", "IsNeedRemove", "IsNeedSendToOwner", "Message",
                "MessageArguments", "MessageCode");
            Assert.True(json.Value<bool>("IsNeedRemove"));
            Assert.True(json.Value<bool>("IsNeedSendToOwner"));
            Assert.Equal(JTokenType.Null, json["MessageArguments"].Type);
        }

        [Fact]
        public void YoutubeChannelTypeValuesUsedByNotificationMatchContract()
        {
            Assert.Equal(
                new[] { 0, 1, 2, 3 },
                Enum.GetValues<Video.YTChannelType>().Select(value => (int)value));
            Assert.Equal(
                new[] { "Holo", "Nijisanji", "Other", "NonApproved" },
                Enum.GetNames<Video.YTChannelType>());
        }

        private static void AssertFields(JObject json, params string[] expected)
        {
            Assert.Equal(
                expected.OrderBy(value => value, StringComparer.Ordinal),
                json.Properties().Select(property => property.Name).OrderBy(value => value, StringComparer.Ordinal));
        }
    }
}
