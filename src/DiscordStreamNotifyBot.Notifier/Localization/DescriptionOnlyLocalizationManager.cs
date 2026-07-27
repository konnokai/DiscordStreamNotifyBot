using Discord.Interactions;
using System.Collections.ObjectModel;

namespace DiscordStreamNotifyBot.Localization
{
    public sealed class DescriptionOnlyLocalizationManager : ILocalizationManager
    {
        private const string BaseResourceName = "DiscordStreamNotifyBot.Localization.Resources.InteractionCommands";
        private static readonly IDictionary<string, string> EmptyNames =
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
        private readonly ResxLocalizationManager _inner = new(
            BaseResourceName,
            typeof(DescriptionOnlyLocalizationManager).Assembly,
            SupportedLocale.Cultures);

        public IDictionary<string, string> GetAllNames(IList<string> key, LocalizationTarget destinationType)
            => EmptyNames;

        public IDictionary<string, string> GetAllDescriptions(IList<string> key, LocalizationTarget destinationType)
            => _inner.GetAllDescriptions(key, destinationType);
    }
}
