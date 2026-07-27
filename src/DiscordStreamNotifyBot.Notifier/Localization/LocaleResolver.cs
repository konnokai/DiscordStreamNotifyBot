namespace DiscordStreamNotifyBot.Localization
{
    public sealed class LocaleResolver
    {
        public string ResolvePublic(string configuredGuildLocale, string discordGuildLocale)
            => FirstSupported(configuredGuildLocale, discordGuildLocale);

        public string ResolvePrivate(string userLocale, string configuredGuildLocale, string discordGuildLocale)
            => FirstSupported(userLocale, configuredGuildLocale, discordGuildLocale);

        public string ResolveDelayedDirectMessage(string savedUserLocale, string configuredGuildLocale)
            => FirstSupported(savedUserLocale, configuredGuildLocale);

        public string ResolveInitial(string discordGuildLocale, string userLocale)
            => FirstSupported(discordGuildLocale, userLocale);

        private static string FirstSupported(params string[] locales)
        {
            foreach (string locale in locales)
            {
                string normalized = SupportedLocale.Normalize(locale);
                if (normalized != null)
                    return normalized;
            }

            return SupportedLocale.Default;
        }
    }
}
