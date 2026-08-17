using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;

namespace DiscordStreamNotifyBot.Localization
{
    public sealed class BotLocalizer
    {
        private const string BaseResourceName = "DiscordStreamNotifyBot.Localization.Resources.BotMessages";
        private static readonly Regex PlaceholderRegex = new(
            @"(?<!\{)\{(?<index>\d+)(?:[^{}]*)\}(?!\})",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly ResourceManager _resourceManager = new(BaseResourceName, typeof(BotLocalizer).Assembly);

        public BotLocalizer()
        {
            ValidateResources();
        }

        public string Get(string key, string locale)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            string normalizedLocale = SupportedLocale.NormalizeOrDefault(locale);
            string value = _resourceManager.GetString(key, SupportedLocale.GetCulture(normalizedLocale));
            if (value == null)
                throw new MissingManifestResourceException($"找不到執行期本地化資源：{key}（{normalizedLocale}）");

            return value;
        }

        public string Format(string key, string locale, params object[] arguments)
            => string.Format(SupportedLocale.GetCulture(locale), Get(key, locale), arguments ?? Array.Empty<object>());

        public bool TryGet(string key, string locale, out string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            string normalizedLocale = SupportedLocale.NormalizeOrDefault(locale);
            CultureInfo culture = normalizedLocale == SupportedLocale.TraditionalChinese
                ? CultureInfo.InvariantCulture
                : CultureInfo.GetCultureInfo(normalizedLocale);
            ResourceSet resourceSet = _resourceManager.GetResourceSet(culture, true, false);
            value = resourceSet?.GetString(key, false);
            return value != null;
        }

        public string GetLocaleDisplayName(string locale, string displayLocale)
        {
            string key = SupportedLocale.NormalizeOrDefault(locale) switch
            {
                SupportedLocale.English => "Locale.English",
                SupportedLocale.Japanese => "Locale.Japanese",
                _ => "Locale.TraditionalChinese"
            };
            return Get(key, displayLocale);
        }

        private void ValidateResources()
        {
            var resources = SupportedLocale.Values.ToDictionary(
                locale => locale,
                LoadResourceValues,
                StringComparer.Ordinal);

            var referenceKeys = resources[SupportedLocale.TraditionalChinese].Keys.ToHashSet(StringComparer.Ordinal);
            foreach (string locale in SupportedLocale.Values.Skip(1))
            {
                var localeKeys = resources[locale].Keys.ToHashSet(StringComparer.Ordinal);
                if (!referenceKeys.SetEquals(localeKeys))
                {
                    string missing = string.Join(", ", referenceKeys.Except(localeKeys, StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal));
                    string extra = string.Join(", ", localeKeys.Except(referenceKeys, StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal));
                    throw new InvalidOperationException($"BotMessages.{locale} 資源 key 不一致。缺少：[{missing}]；多出：[{extra}]");
                }
            }

            foreach (string key in referenceKeys.OrderBy(x => x, StringComparer.Ordinal))
            {
                var referencePlaceholders = GetPlaceholders(resources[SupportedLocale.TraditionalChinese][key]);
                foreach (string locale in SupportedLocale.Values.Skip(1))
                {
                    var placeholders = GetPlaceholders(resources[locale][key]);
                    if (!referencePlaceholders.SequenceEqual(placeholders, StringComparer.Ordinal))
                        throw new InvalidOperationException($"BotMessages key `{key}` 的格式參數在 {locale} 不一致");
                }
            }
        }

        private Dictionary<string, string> LoadResourceValues(string locale)
        {
            CultureInfo culture = locale == SupportedLocale.TraditionalChinese
                ? CultureInfo.InvariantCulture
                : CultureInfo.GetCultureInfo(locale);
            ResourceSet resourceSet = _resourceManager.GetResourceSet(culture, true, false)
                ?? throw new MissingManifestResourceException($"找不到 BotMessages 資源：{locale}");

            return resourceSet.Cast<DictionaryEntry>()
                .ToDictionary(
                    entry => (string)entry.Key,
                    entry => (string)entry.Value,
                    StringComparer.Ordinal);
        }

        private static string[] GetPlaceholders(string value)
            => PlaceholderRegex.Matches(value)
                .Select(match => match.Groups["index"].Value)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(index => index, StringComparer.Ordinal)
                .ToArray();
    }
}
