using DiscordStreamNotifyBot.Shared;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class YoutubeVideoIdParserTests
    {
        private const string VideoId = "AbCdEfGhI_1";

        [Theory]
        [InlineData(VideoId)]
        [InlineData("  AbCdEfGhI_1  ")]
        [InlineData("https://www.youtube.com/watch?v=AbCdEfGhI_1")]
        [InlineData("https://www.youtube.com/watch?feature=share&v=AbCdEfGhI_1&t=30")]
        [InlineData("http://m.youtube.com/watch?v=AbCdEfGhI_1")]
        [InlineData("youtube.com/watch?list=playlist&v=AbCdEfGhI_1")]
        [InlineData("//www.youtube.com/watch?v=AbCdEfGhI_1")]
        [InlineData("https://youtu.be/AbCdEfGhI_1?si=share")]
        [InlineData("https://www.youtube.com/live/AbCdEfGhI_1?feature=share")]
        [InlineData("https://www.youtube.com/shorts/AbCdEfGhI_1")]
        [InlineData("https://www.youtube.com/embed/AbCdEfGhI_1")]
        [InlineData("https://www.youtube-nocookie.com/embed/AbCdEfGhI_1")]
        [InlineData("https://youtube.com/v/AbCdEfGhI_1")]
        public void TryParseAcceptsSupportedInput(string input)
        {
            bool parsed = YoutubeVideoIdParser.TryParse(input, out string videoId);

            Assert.True(parsed);
            Assert.Equal(VideoId, videoId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("AbCdEfGhI1")]
        [InlineData("AbCdEfGhI_12")]
        [InlineData("AbCdEfGhI+1")]
        [InlineData("https://example.com/watch?v=AbCdEfGhI_1")]
        [InlineData("https://notyoutube.com/watch?v=AbCdEfGhI_1")]
        [InlineData("https://www.youtube.com/watch?feature=share")]
        [InlineData("https://www.youtube.com/watch?v=AbCdEfGhI_12")]
        [InlineData("https://www.youtube.com/channel/AbCdEfGhI_1")]
        [InlineData("https://www.youtube.com/AbCdEfGhI_1")]
        [InlineData("https://youtu.be/too-short")]
        [InlineData("ftp://www.youtube.com/watch?v=AbCdEfGhI_1")]
        [InlineData("video https://youtu.be/AbCdEfGhI_1")]
        public void TryParseRejectsUnsupportedInput(string input)
        {
            bool parsed = YoutubeVideoIdParser.TryParse(input, out string videoId);

            Assert.False(parsed);
            Assert.Null(videoId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ParseRejectsMissingInput(string input)
        {
            Assert.Throws<ArgumentNullException>(() => YoutubeVideoIdParser.Parse(input));
        }

        [Theory]
        [InlineData("   ")]
        [InlineData("https://www.youtube.com/watch?feature=share")]
        [InlineData("not-a-video!")]
        public void ParseRejectsInvalidInput(string input)
        {
            Assert.Throws<UriFormatException>(() => YoutubeVideoIdParser.Parse(input));
        }
    }
}
