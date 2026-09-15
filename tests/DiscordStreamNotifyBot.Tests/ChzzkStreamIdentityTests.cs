using DiscordStreamNotifyBot.SharedService.Chzzk;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class ChzzkStreamIdentityTests
    {
        private const string ChannelId = "f39c3d74e33a81ab3080356b91bb8de5";

        [Fact]
        public void StreamKeyNormalizesOpenDate()
        {
            Assert.True(ChzzkStreamIdentity.TryCreate(ChannelId, "2026-09-15 13:41:52", out string key));
            Assert.Equal($"{ChannelId}:20260915_134152", key);
        }

        [Fact]
        public void StreamKeyNormalizationMatchesRequestedReplace()
        {
            // 使用者指定：移除冒號與減號、空格改底線（2026-09-15 12:41:10 → 20260915_124110）。
            const string raw = "2026-09-15 12:41:10";
            string replaced = raw.Replace(":", string.Empty).Replace("-", string.Empty).Replace(" ", "_");

            Assert.True(ChzzkStreamIdentity.TryCreate(ChannelId, raw, out string key));
            Assert.Equal($"{ChannelId}:{replaced}", key);
            Assert.Equal($"{ChannelId}:20260915_124110", key);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("2026-09-15T13:41:52")]
        [InlineData("2026-09-15 13:41")]
        [InlineData("not a date")]
        public void InvalidOpenDateCannotCreateStreamKey(string openDate)
        {
            Assert.False(ChzzkStreamIdentity.TryCreate(ChannelId, openDate, out string key));
            Assert.Null(key);
        }

        [Fact]
        public void MissingChannelIdCannotCreateStreamKey()
        {
            Assert.False(ChzzkStreamIdentity.TryCreate(null, "2026-09-15 13:41:52", out _));
        }

        [Fact]
        public void KstTimeIsConvertedToUtc()
        {
            // 使用者已確認為 UTC+9；未確認前不得宣稱時區，確認後以本轉換顯示 Discord timestamp。
            Assert.True(ChzzkTime.TryParseKstToUtc("2026-09-15 13:41:52", out DateTime utc));
            Assert.Equal(new DateTime(2026, 9, 15, 4, 41, 52, DateTimeKind.Utc), utc);
        }

        [Fact]
        public void InvalidKstTimeIsRejected()
        {
            Assert.False(ChzzkTime.TryParseKstToUtc("2026-09-15 25:00:00", out _));
            Assert.False(ChzzkTime.TryParseKstToUtc(null, out _));
        }

        [Theory]
        [InlineData(ChannelId, ChannelId)]
        [InlineData("https://chzzk.naver.com/f39c3d74e33a81ab3080356b91bb8de5", ChannelId)]
        [InlineData("https://chzzk.naver.com/live/f39c3d74e33a81ab3080356b91bb8de5?foo=bar", ChannelId)]
        [InlineData("https://chzzk.naver.com/live/F39C3D74E33A81AB3080356B91BB8DE5", ChannelId)]
        public void SupportedSourcesAreParsed(string source, string expected)
        {
            Assert.True(ChzzkUrlParser.TryParseChannelId(source, out string channelId));
            Assert.Equal(expected, channelId);
        }

        [Theory]
        [InlineData("https://example.com/f39c3d74e33a81ab3080356b91bb8de5")]
        [InlineData("https://chzzk.naver.com/")]
        [InlineData("https://chzzk.naver.com/live/short")]
        [InlineData("not-a-channel")]
        [InlineData("")]
        public void UnsupportedSourcesAreRejected(string source)
        {
            Assert.False(ChzzkUrlParser.TryParseChannelId(source, out string channelId));
            Assert.Null(channelId);
        }
    }
}
