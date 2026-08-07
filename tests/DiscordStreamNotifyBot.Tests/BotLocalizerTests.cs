using DiscordStreamNotifyBot.Localization;
using System.Resources;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class BotLocalizerTests
    {
        private static readonly BotLocalizer Localizer = new();

        [Fact]
        public void ConstructorValidatesCurrentResources()
        {
            Exception exception = Record.Exception(() => new BotLocalizer());

            Assert.Null(exception);
        }

        [Theory]
        [InlineData("zh-TW", "/youtube-member check")]
        [InlineData("en-US", "/youtube-member check")]
        [InlineData("ja", "/youtube-member check")]
        public void LegacyYoutubeMemberSelectionExpiryIsLocalized(string locale, string commandPath)
        {
            Assert.Contains(commandPath, Localizer.Get("Member.Select.Expired", locale));
        }

        [Theory]
        [InlineData("zh-TW", "是")]
        [InlineData("en-US", "Yes")]
        [InlineData("ja", "はい")]
        public void GetReturnsValueFromRequestedLocale(string locale, string expected)
        {
            Assert.Equal(expected, Localizer.Get("Common.Yes", locale));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("fr-FR")]
        [InlineData("unknown")]
        public void GetFallsBackToTraditionalChinese(string locale)
        {
            Assert.Equal("是", Localizer.Get("Common.Yes", locale));
        }

        [Theory]
        [InlineData("zh-TW", "是")]
        [InlineData("en-US", "Yes")]
        [InlineData("ja", "はい")]
        public void TryGetReturnsValueFromRequestedLocale(string locale, string expected)
        {
            bool found = Localizer.TryGet("Common.Yes", locale, out string value);

            Assert.True(found);
            Assert.Equal(expected, value);
        }

        [Fact]
        public void TryGetFallsBackToTraditionalChineseForNullLocale()
        {
            bool found = Localizer.TryGet("Common.Yes", null, out string value);

            Assert.True(found);
            Assert.Equal("是", value);
        }

        [Fact]
        public void TryGetReturnsFalseForMissingKey()
        {
            bool found = Localizer.TryGet("Missing.Key", "en-US", out string value);

            Assert.False(found);
            Assert.Null(value);
        }

        [Theory]
        [InlineData("zh-TW", "頻道", "找不到指定的頻道。")]
        [InlineData("en-US", "channel", "The specified channel could not be found.")]
        [InlineData("ja", "チャンネル", "指定されたチャンネルが見つかりません。")]
        public void FormatUsesLocalizedTemplate(string locale, string argument, string expected)
        {
            Assert.Equal(expected, Localizer.Format("Errors.NotFound", locale, argument));
        }

        [Fact]
        public void FormatTreatsNullArgumentArrayAsEmpty()
        {
            Assert.Equal("Yes", Localizer.Format("Common.Yes", "en-US", (object[])null));
        }

        [Fact]
        public void FormatFallsBackForNullLocale()
        {
            Assert.Equal("找不到指定的頻道。", Localizer.Format("Errors.NotFound", null, "頻道"));
        }

        [Theory]
        [InlineData("zh-TW", "zh-TW", "繁體中文")]
        [InlineData("en-US", "zh-TW", "English")]
        [InlineData("ja", "zh-TW", "日本語")]
        [InlineData("zh-TW", "en-US", "Traditional Chinese")]
        [InlineData("en-US", "en-US", "English")]
        [InlineData("ja", "en-US", "Japanese")]
        [InlineData("zh-TW", "ja", "繁体字中国語")]
        [InlineData("en-US", "ja", "英語")]
        [InlineData("ja", "ja", "日本語")]
        public void GetLocaleDisplayNameUsesRequestedDisplayLocale(string locale, string displayLocale, string expected)
        {
            Assert.Equal(expected, Localizer.GetLocaleDisplayName(locale, displayLocale));
        }

        [Theory]
        [InlineData(null, null, "繁體中文")]
        [InlineData("fr-FR", "fr-FR", "繁體中文")]
        public void GetLocaleDisplayNameFallsBackToTraditionalChinese(string locale, string displayLocale, string expected)
        {
            Assert.Equal(expected, Localizer.GetLocaleDisplayName(locale, displayLocale));
        }

        [Fact]
        public void GetThrowsForNullKey()
        {
            Assert.Throws<ArgumentNullException>(() => Localizer.Get(null, "en-US"));
        }

        [Fact]
        public void TryGetThrowsForNullKey()
        {
            Assert.Throws<ArgumentNullException>(() => Localizer.TryGet(null, "en-US", out _));
        }

        [Fact]
        public void FormatThrowsForNullKey()
        {
            Assert.Throws<ArgumentNullException>(() => Localizer.Format(null, "en-US"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void GetThrowsForBlankKey(string key)
        {
            Assert.Throws<ArgumentException>(() => Localizer.Get(key, "en-US"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void TryGetThrowsForBlankKey(string key)
        {
            Assert.Throws<ArgumentException>(() => Localizer.TryGet(key, "en-US", out _));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void FormatThrowsForBlankKey(string key)
        {
            Assert.Throws<ArgumentException>(() => Localizer.Format(key, "en-US"));
        }

        [Fact]
        public void GetThrowsForMissingKey()
        {
            Assert.Throws<MissingManifestResourceException>(() => Localizer.Get("Missing.Key", "en-US"));
        }

        [Fact]
        public void FormatThrowsForMissingKey()
        {
            Assert.Throws<MissingManifestResourceException>(() => Localizer.Format("Missing.Key", "en-US"));
        }
    }
}
