namespace DiscordStreamNotifyBot.Scraper.Detection.Twitcasting
{
    internal readonly record struct TwitcastingWebhookRegistration(string UserId, string Event);

    internal enum TwitcastingWebhookActionKind
    {
        RegisterLiveStart,
        RemoveLiveStart,
    }

    internal readonly record struct TwitcastingWebhookAction(
        TwitcastingWebhookActionKind Kind,
        string UserId);

    internal static class TwitcastingWebhookRegistrationPlanner
    {
        private const string LiveStartEvent = "livestart";

        internal static IReadOnlyList<TwitcastingWebhookAction> Plan(
            IEnumerable<string> desiredUserIds,
            IEnumerable<TwitcastingWebhookRegistration> registered)
        {
            var desired = NormalizeIds(desiredUserIds);
            var registeredLiveStart = (registered ?? [])
                .Where(item => string.Equals(item.Event, LiveStartEvent, StringComparison.Ordinal))
                .Select(item => item.UserId)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToHashSet(StringComparer.Ordinal);

            return desired.Except(registeredLiveStart, StringComparer.Ordinal)
                .OrderBy(item => item, StringComparer.Ordinal)
                .Select(item => new TwitcastingWebhookAction(TwitcastingWebhookActionKind.RegisterLiveStart, item))
                .Concat(registeredLiveStart.Except(desired, StringComparer.Ordinal)
                    .OrderBy(item => item, StringComparer.Ordinal)
                    .Select(item => new TwitcastingWebhookAction(TwitcastingWebhookActionKind.RemoveLiveStart, item)))
                .ToArray();
        }

        private static HashSet<string> NormalizeIds(IEnumerable<string> ids)
        {
            return (ids ?? [])
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToHashSet(StringComparer.Ordinal);
        }
    }
}
