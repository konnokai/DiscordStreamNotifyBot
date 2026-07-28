using DiscordStreamNotifyBot.Localization;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class SupportedLocaleTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData(" \t\r\n ", null)]
        [InlineData("zh", "zh-TW")]
        [InlineData(" ZH ", "zh-TW")]
        [InlineData("zh-CN", "zh-TW")]
        [InlineData("ZH-Hant", "zh-TW")]
        [InlineData("en", "en-US")]
        [InlineData(" EN-us ", "en-US")]
        [InlineData("en-GB", "en-US")]
        [InlineData("ja", "ja")]
        [InlineData(" JA ", "ja")]
        [InlineData("ja-JP", "ja")]
        [InlineData("fr-FR", null)]
        [InlineData("zhTW", null)]
        [InlineData("english", null)]
        public void NormalizeReturnsCanonicalSupportedLocaleOrNull(string locale, string expected)
        {
            Assert.Equal(expected, SupportedLocale.Normalize(locale));
        }

        [Theory]
        [InlineData(null, "zh-TW")]
        [InlineData("", "zh-TW")]
        [InlineData("fr-FR", "zh-TW")]
        [InlineData("zh-HK", "zh-TW")]
        [InlineData("en-AU", "en-US")]
        [InlineData("ja-JP", "ja")]
        public void NormalizeOrDefaultReturnsCanonicalLocaleOrTraditionalChinese(string locale, string expected)
        {
            Assert.Equal(expected, SupportedLocale.NormalizeOrDefault(locale));
        }

        [Theory]
        [InlineData(null, "zh-TW")]
        [InlineData(" ", "zh-TW")]
        [InlineData("unsupported", "zh-TW")]
        [InlineData("zh-CN", "zh-TW")]
        [InlineData("en-GB", "en-US")]
        [InlineData("ja-JP", "ja")]
        public void GetCultureUsesNormalizedLocale(string locale, string expectedCultureName)
        {
            Assert.Equal(expectedCultureName, SupportedLocale.GetCulture(locale).Name);
        }
    }

    public sealed class LocaleResolverTests
    {
        private readonly LocaleResolver _resolver = new();

        [Theory]
        [InlineData("en-US", "ja", "en-US")]
        [InlineData("fr-FR", "ja-JP", "ja")]
        [InlineData(" ", "en-GB", "en-US")]
        [InlineData(null, null, "zh-TW")]
        [InlineData("zh-CN", "en-US", "zh-TW")]
        public void ResolvePublicPrefersConfiguredGuildLocale(string configuredGuildLocale, string discordGuildLocale, string expected)
        {
            Assert.Equal(expected, _resolver.ResolvePublic(configuredGuildLocale, discordGuildLocale));
        }

        [Theory]
        [InlineData("ja", "en-US", "zh-TW", "ja")]
        [InlineData("fr-FR", "en-GB", "ja", "en-US")]
        [InlineData("unsupported", " ", "zh-CN", "zh-TW")]
        [InlineData(null, "fr-FR", "ja-JP", "ja")]
        [InlineData("EN-gb", "ja", "zh-TW", "en-US")]
        [InlineData(null, null, null, "zh-TW")]
        public void ResolvePrivatePrefersUserThenConfiguredThenDiscordLocale(
            string userLocale,
            string configuredGuildLocale,
            string discordGuildLocale,
            string expected)
        {
            Assert.Equal(expected, _resolver.ResolvePrivate(userLocale, configuredGuildLocale, discordGuildLocale));
        }

        [Theory]
        [InlineData("ja", "en-US", "ja")]
        [InlineData("fr-FR", "en-GB", "en-US")]
        [InlineData("zh-CN", "ja", "zh-TW")]
        [InlineData(null, null, "zh-TW")]
        public void ResolveDelayedDirectMessagePrefersSavedUserThenConfiguredLocale(
            string savedUserLocale,
            string configuredGuildLocale,
            string expected)
        {
            Assert.Equal(expected, _resolver.ResolveDelayedDirectMessage(savedUserLocale, configuredGuildLocale));
        }

        [Theory]
        [InlineData("ja-JP", "en-US", "ja")]
        [InlineData("fr-FR", "en-GB", "en-US")]
        [InlineData("zh-HK", "ja", "zh-TW")]
        [InlineData(null, null, "zh-TW")]
        public void ResolveInitialPrefersDiscordGuildThenUserLocale(string discordGuildLocale, string userLocale, string expected)
        {
            Assert.Equal(expected, _resolver.ResolveInitial(discordGuildLocale, userLocale));
        }
    }
}
