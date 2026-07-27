using System.Globalization;

namespace DiscordStreamNotifyBot.Localization
{
    public static class SupportedLocale
    {
        public const string Default = TraditionalChinese;
        public const string TraditionalChinese = "zh-TW";
        public const string English = "en-US";
        public const string Japanese = "ja";

        public static IReadOnlyList<string> Values { get; } =
            new[] { TraditionalChinese, English, Japanese };

        public static CultureInfo[] Cultures => Values
            .Select(CultureInfo.GetCultureInfo)
            .ToArray();

        public static string Normalize(string locale)
        {
            if (string.IsNullOrWhiteSpace(locale))
                return null;

            string value = locale.Trim();
            if (value.Equals("zh", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("zh-", StringComparison.OrdinalIgnoreCase))
                return TraditionalChinese;

            if (value.Equals("en", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("en-", StringComparison.OrdinalIgnoreCase))
                return English;

            if (value.Equals("ja", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("ja-", StringComparison.OrdinalIgnoreCase))
                return Japanese;

            return null;
        }

        public static string NormalizeOrDefault(string locale)
            => Normalize(locale) ?? Default;

        public static CultureInfo GetCulture(string locale)
            => CultureInfo.GetCultureInfo(NormalizeOrDefault(locale));
    }
}
