using Discord;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.Twitcasting;
using DiscordStreamNotifyBot.SharedService.Twitch;
using TableVideo = DiscordStreamNotifyBot.DataBase.Table.Video;
using YoutubeEmbedBuilderFactory = DiscordStreamNotifyBot.SharedService.Youtube.EmbedBuilderFactory;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class NotificationEmbedFactoryTests
    {
        private const string Locale = SupportedLocale.English;
        private static readonly BotLocalizer Localizer = new();
        private static readonly Color OkColor = new(0, 229, 132);
        private static readonly Color ErrorColor = new(40, 40, 40);
        private static readonly Color RecordColor = new(255, 0, 0);

        [Fact]
        public void YoutubeStreamStartedBuildsLiveEmbed()
        {
            TableVideo video = CreateYoutubeVideo();

            Embed embed = YoutubeEmbedBuilderFactory.CreateStreamStarted(video, Localizer, Locale).Build();

            Assert.Equal(video.VideoTitle, embed.Title);
            Assert.Equal($"https://www.youtube.com/watch?v={video.VideoId}", embed.Url);
            Assert.Equal($"[{video.ChannelTitle}](https://www.youtube.com/channel/{video.ChannelId})", embed.Description);
            Assert.Equal($"https://i.ytimg.com/vi/{video.VideoId}/maxresdefault.jpg", embed.Image?.Url);
            AssertColor(OkColor, embed);
            AssertField(embed, "Stream status", "Live");
            AssertField(embed, "Scheduled start", DiscordTimestamp(video.ScheduledStartTime));
        }

        [Theory]
        [InlineData(3723, "1h 2m 3s")]
        [InlineData(-1, "0h 0m 0s")]
        [InlineData(90061, "1d 1h 1m 1s")]
        public void YoutubeStreamEndedFormatsDuration(long elapsedSeconds, string expectedDuration)
        {
            TableVideo video = CreateYoutubeVideo();
            DateTime startAt = UtcDate(2026, 7, 20, 1, 2, 3);
            DateTime endAt = startAt.AddSeconds(elapsedSeconds);

            Embed embed = YoutubeEmbedBuilderFactory
                .CreateStreamEnded(video, startAt, endAt, Localizer, Locale)
                .Build();

            AssertColor(ErrorColor, embed);
            AssertField(embed, "Stream status", "Stream ended");
            AssertField(embed, "Stream duration", expectedDuration);
            AssertField(embed, "Ended at", DiscordTimestamp(endAt));
        }

        [Fact]
        public void YoutubeStreamDeletedBuildsDeletedStatus()
        {
            TableVideo video = CreateYoutubeVideo();

            Embed embed = YoutubeEmbedBuilderFactory.CreateStreamDeleted(video, Localizer, Locale).Build();

            AssertColor(ErrorColor, embed);
            AssertField(embed, "Stream status", "Stream deleted");
            AssertField(embed, "Scheduled start", DiscordTimestamp(video.ScheduledStartTime));
        }

        [Fact]
        public void YoutubeStreamUnarchivedBuildsPrivateArchiveStatus()
        {
            TableVideo video = CreateYoutubeVideo();

            Embed embed = YoutubeEmbedBuilderFactory.CreateStreamUnarchived(video, Localizer, Locale).Build();

            AssertColor(OkColor, embed);
            AssertField(embed, "Stream status", "Ended and changed to a private archive");
            AssertField(embed, "Scheduled start", DiscordTimestamp(video.ScheduledStartTime));
        }

        [Fact]
        public void YoutubeNewVideoBuildsUploadedAtWithoutStreamStatus()
        {
            TableVideo video = CreateYoutubeVideo();

            Embed embed = YoutubeEmbedBuilderFactory.CreateNewVideo(video, Localizer, Locale).Build();

            AssertColor(OkColor, embed);
            AssertField(embed, "Uploaded at", DiscordTimestamp(video.ScheduledStartTime));
            Assert.DoesNotContain(embed.Fields, field => field.Name == "Stream status");
        }

        [Theory]
        [InlineData(true, "Just Chatting", true)]
        [InlineData(false, "Just Chatting", true)]
        [InlineData(false, "", false)]
        public void TwitchStreamStartedUsesRecordingColorAndOptionalCategory(
            bool isRecord, string gameName, bool expectedCategory)
        {
            TwitchStream stream = CreateTwitchStream(gameName);

            Embed embed = TwitchEmbedBuilderFactory
                .CreateStreamStarted(stream, "https://example.com/profile.png", isRecord, 12345, Localizer, Locale)
                .Build();

            Assert.Equal(stream.StreamTitle, embed.Title);
            Assert.Equal("https://twitch.tv/example_login", embed.Url);
            Assert.Equal("[Example User](https://twitch.tv/example_login)", embed.Description);
            Assert.Equal("https://example.com/profile.png", embed.Thumbnail?.Url);
            Assert.Equal("https://example.com/stream.jpg?t=12345", embed.Image?.Url);
            AssertColor(isRecord ? RecordColor : OkColor, embed);
            AssertField(embed, "Stream status", "Live");
            AssertField(embed, "Started at", DiscordTimestamp(stream.StreamStartAt));

            if (expectedCategory)
                AssertField(embed, "Category", gameName);
            else
                Assert.DoesNotContain(embed.Fields, field => field.Name == "Category");
        }

        [Fact]
        public void TwitchStreamEndedUsesUnknownTitleAndClipsFallbackWhenMetadataIsMissing()
        {
            DateTime endAt = UtcDate(2026, 7, 20, 4, 5, 6);

            Embed embed = TwitchEmbedBuilderFactory.CreateStreamEnded(
                "Example User",
                "example_login",
                "",
                null,
                endAt,
                null,
                "legacy clips fallback",
                "https://example.com/profile.png",
                "https://example.com/offline.jpg",
                Localizer,
                Locale).Build();

            Assert.Equal("(stream title unavailable)", embed.Title);
            AssertColor(ErrorColor, embed);
            AssertField(embed, "Stream status", "Offline");
            AssertField(embed, "Ended at", DiscordTimestamp(endAt));
            AssertField(embed, "Most-viewed clips", "legacy clips fallback");
            Assert.DoesNotContain(embed.Fields, field => field.Name == "Stream duration");
            Assert.Equal("https://example.com/profile.png", embed.Thumbnail?.Url);
            Assert.Equal("https://example.com/offline.jpg", embed.Image?.Url);
        }

        [Fact]
        public void TwitchStreamEndedUsesDurationAndStructuredClipsBeforeFallback()
        {
            DateTime startAt = UtcDate(2026, 7, 20, 1, 2, 3);
            DateTime endAt = startAt.AddHours(1).AddMinutes(2).AddSeconds(3);
            var clips = new[]
            {
                new TwitchClipInfo
                {
                    Title = "Best moment",
                    Url = "https://clips.twitch.tv/best-moment",
                    CreatorName = "Clipper",
                    ViewCount = 1234
                }
            };

            Embed embed = TwitchEmbedBuilderFactory.CreateStreamEnded(
                "Example User",
                "example_login",
                "Known title",
                startAt,
                endAt,
                clips,
                "legacy clips fallback",
                null,
                null,
                Localizer,
                Locale).Build();

            Assert.Equal("Known title", embed.Title);
            AssertField(embed, "Stream duration", "1h 2m 3s");
            AssertField(embed, "Most-viewed clips",
                "1. [Best moment](https://clips.twitch.tv/best-moment) by `Clipper` (`1,234` views)");
            Assert.DoesNotContain("legacy clips fallback", FieldValue(embed, "Most-viewed clips"));
        }

        [Fact]
        public void TwitchChannelUpdateClampsNegativeElapsedAndFormatsEmptyCategoryAsNone()
        {
            var updates = new[]
            {
                new TwitchChannelUpdateInfo
                {
                    ElapsedSeconds = -30,
                    OldCategory = "Gaming",
                    NewCategory = ""
                }
            };

            Embed embed = TwitchEmbedBuilderFactory.CreateChannelUpdate(
                "Example User",
                "example_login",
                updates,
                "legacy update fallback",
                null,
                Localizer,
                Locale).Build();

            Assert.Equal("Example User's stream details were updated", embed.Title);
            Assert.Contains("`0h 0m 0s`", embed.Description);
            Assert.Contains("Category changed: `Gaming` → `None`", embed.Description);
            Assert.DoesNotContain("legacy update fallback", embed.Description);
            AssertColor(OkColor, embed);
        }

        [Theory]
        [InlineData(true, true, 40, 40, 40, "Yes", "Recording available")]
        [InlineData(true, false, 40, 40, 40, "Yes", "No recording available")]
        [InlineData(false, true, 255, 0, 0, "No", "Recording available")]
        [InlineData(false, false, 0, 229, 132, "No", "No recording available")]
        public void TwitcastingStreamStartedUsesPrivateThenRecordingColorPrecedence(
            bool isPrivate, bool isRecord, byte red, byte green, byte blue,
            string expectedPrivate, string expectedRecording)
        {
            TwitcastingStream stream = CreateTwitcastingStream("Subtitle", "Music");

            Embed embed = TwitcastingEmbedBuilderFactory
                .CreateStreamStarted(stream, isPrivate, isRecord, Localizer, Locale)
                .Build();

            AssertColor(new Color(red, green, blue), embed);
            AssertField(embed, "Password-protected private stream", expectedPrivate);
            AssertField(embed, "Recording status", expectedRecording);
            AssertField(embed, "Started at", DiscordTimestamp(stream.StreamStartAt));
        }

        [Theory]
        [InlineData("Subtitle", "Music", true)]
        [InlineData("", "", false)]
        public void TwitcastingStreamStartedOnlyIncludesOptionalFieldsWhenProvided(
            string subtitle, string category, bool expectedOptionalFields)
        {
            TwitcastingStream stream = CreateTwitcastingStream(subtitle, category);

            Embed embed = TwitcastingEmbedBuilderFactory
                .CreateStreamStarted(stream, false, false, Localizer, Locale)
                .Build();

            Assert.Equal(stream.StreamTitle, embed.Title);
            Assert.Equal("https://twitcasting.tv/example_channel/movie/24680", embed.Url);
            Assert.Equal("[Example Channel](https://twitcasting.tv/example_channel)", embed.Description);
            Assert.Equal(stream.ThumbnailUrl, embed.Image?.Url);
            Assert.Equal(expectedOptionalFields, embed.Fields.Any(field => field.Name == "Subtitle"));
            Assert.Equal(expectedOptionalFields, embed.Fields.Any(field => field.Name == "Category"));
            if (expectedOptionalFields)
            {
                AssertField(embed, "Subtitle", subtitle);
                AssertField(embed, "Category", category);
            }
        }

        private static TableVideo CreateYoutubeVideo()
        {
            return new TableVideo
            {
                ChannelId = "youtube-channel",
                ChannelTitle = "YouTube Channel",
                VideoId = "video-id",
                VideoTitle = "YouTube stream title",
                ScheduledStartTime = UtcDate(2026, 7, 20, 1, 2, 3)
            };
        }

        private static TwitchStream CreateTwitchStream(string gameName)
        {
            return new TwitchStream
            {
                StreamId = "stream-id",
                StreamTitle = "Twitch stream title",
                StreamStartAt = UtcDate(2026, 7, 20, 1, 2, 3),
                UserId = "user-id",
                UserLogin = "example_login",
                UserName = "Example User",
                GameName = gameName,
                ThumbnailUrl = "https://example.com/stream.jpg"
            };
        }

        private static TwitcastingStream CreateTwitcastingStream(string subtitle, string category)
        {
            return new TwitcastingStream
            {
                ChannelId = "example_channel",
                ChannelTitle = "Example Channel",
                StreamId = 24680,
                StreamTitle = "TwitCasting stream title",
                StreamSubTitle = subtitle,
                Category = category,
                ThumbnailUrl = "https://example.com/twitcasting.jpg",
                StreamStartAt = UtcDate(2026, 7, 20, 1, 2, 3)
            };
        }

        private static DateTime UtcDate(int year, int month, int day, int hour, int minute, int second)
        {
            return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
        }

        private static string DiscordTimestamp(DateTime dateTime)
        {
            long unixTime = new DateTimeOffset(dateTime).ToUnixTimeSeconds();
            return $"<t:{unixTime}:F> (<t:{unixTime}:R>)";
        }

        private static void AssertColor(Color expected, Embed embed)
        {
            Assert.Equal(expected.RawValue, embed.Color?.RawValue);
        }

        private static void AssertField(Embed embed, string name, string value)
        {
            Assert.Equal(value, FieldValue(embed, name));
        }

        private static string FieldValue(Embed embed, string name)
        {
            return Assert.Single(embed.Fields.Where(field => field.Name == name)).Value;
        }
    }
}
