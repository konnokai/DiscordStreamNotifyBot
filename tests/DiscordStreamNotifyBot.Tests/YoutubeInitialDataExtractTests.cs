using DiscordStreamNotifyBot.Scraper.Detection.Youtube;

namespace DiscordStreamNotifyBot.Tests
{
    /// <summary>頻道頁 ytInitialData 擷取測試：YouTube 目前會隨機回三種格式，每種都要能取出同一份 JSON。</summary>
    public sealed class YoutubeInitialDataExtractTests
    {
        private const string Json = "{\"contents\":{\"videoId\":\"dQw4w9WgXcQ\"}}";

        [Fact]
        public void ExtractsVarAssignment()
        {
            string html = $"<script nonce=\"abc\">var ytInitialData = {Json};</script><script>var x = 1;</script>";

            Assert.Equal(Json, YoutubeDetectionService.ExtractYtInitialData(html));
        }

        [Fact]
        public void ExtractsJsonScriptElement()
        {
            string html = "<script nonce=\"abc\">var ytDataEl = document.getElementById('yt-initial-data'); "
                + "if (ytDataEl) {try {window['ytInitialData'] = JSON.parse(ytDataEl.textContent);} catch (e) {}}</script>"
                + $"<script id=\"yt-initial-data\" type=\"application/json\" nonce=\"abc\">{Json}</script><script>var x = 1;</script>";

            Assert.Equal(Json, YoutubeDetectionService.ExtractYtInitialData(html));
        }

        [Fact]
        public void ExtractsJsonScriptElementWithDifferentAttributeOrder()
        {
            string html = $"<script type=\"application/json\" id=\"yt-initial-data\">{Json}</script>";

            Assert.Equal(Json, YoutubeDetectionService.ExtractYtInitialData(html));
        }

        [Fact]
        public void ExtractsLegacyWindowAssignment()
        {
            string html = $"<script>window[\"ytInitialData\"] = {Json};\n</script>";

            Assert.Equal(Json, YoutubeDetectionService.ExtractYtInitialData(html));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("<html><body>consent</body></html>")]
        [InlineData("<script id=\"yt-initial-data\" type=\"application/json\"></script>")]
        public void ReturnsNullWhenNotFound(string html)
        {
            Assert.Null(YoutubeDetectionService.ExtractYtInitialData(html));
        }
    }
}
