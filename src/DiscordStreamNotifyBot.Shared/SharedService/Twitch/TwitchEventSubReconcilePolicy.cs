namespace DiscordStreamNotifyBot.SharedService.Twitch
{
    internal sealed record TwitchEventSubFact(
        string Id,
        string Type,
        string Version,
        string BroadcasterUserId,
        string Status,
        string TransportMethod,
        string Callback,
        int Cost,
        int ConditionCount);

    internal sealed record TwitchEventSubCreateSpec(string Type, string Version);

    internal sealed record TwitchEventSubReconcilePlan(
        IReadOnlyList<TwitchEventSubCreateSpec> Create,
        IReadOnlyList<string> Delete);

    internal sealed record TwitchEventSubFinalDecision(
        bool AllDesiredEnabled,
        bool IsPermanentCostValid)
    {
        public bool IsSuccess => AllDesiredEnabled && IsPermanentCostValid;
    }

    /// <summary>只判斷 EventSub 差集與最終狀態；API 呼叫由 TwitchApiService 執行。</summary>
    internal static class TwitchEventSubReconcilePolicy
    {
        private static readonly TwitchEventSubCreateSpec[] PermanentSpecs =
        [
            new("stream.online", "1"),
            new("channel.update", "2"),
            new("stream.offline", "1")
        ];

        private static readonly TwitchEventSubCreateSpec[] FallbackSpecs =
        [
            new("channel.update", "2"),
            new("stream.offline", "1")
        ];

        private static readonly HashSet<string> ManagedTypes =
            ["stream.online", "channel.update", "stream.offline"];

        public static TwitchEventSubReconcilePlan Plan(TwitchEventSubEnsureMode mode,
            string broadcasterUserId, string callbackUrl, IReadOnlyCollection<TwitchEventSubFact> subscriptions)
        {
            var desired = GetDesired(mode);
            var relevant = subscriptions.Where(x => ManagedTypes.Contains(x.Type) &&
                x.BroadcasterUserId == broadcasterUserId).ToArray();
            var create = new List<TwitchEventSubCreateSpec>();
            var delete = new List<string>();

            foreach (var spec in desired)
            {
                var candidates = relevant.Where(x => x.Type == spec.Type).ToArray();
                var valid = candidates
                    .Where(x => IsExact(x, spec, broadcasterUserId, callbackUrl))
                    .OrderBy(x => x.Status == "enabled" ? 0 : 1)
                    .ToArray();
                if (valid.Length == 0)
                    create.Add(spec);

                delete.AddRange(candidates.Where(x => valid.Length == 0 || x.Id != valid[0].Id)
                    .Select(x => x.Id));
            }

            var desiredTypes = desired.Select(x => x.Type).ToHashSet(StringComparer.Ordinal);
            delete.AddRange(relevant.Where(x => !desiredTypes.Contains(x.Type)).Select(x => x.Id));

            return new TwitchEventSubReconcilePlan(
                create.ToArray(),
                delete.Where(x => !string.IsNullOrEmpty(x)).Distinct(StringComparer.Ordinal).ToArray());
        }

        public static TwitchEventSubFinalDecision EvaluateFinal(TwitchEventSubEnsureMode mode,
            string broadcasterUserId, string callbackUrl, IReadOnlyCollection<TwitchEventSubFact> subscriptions)
        {
            var desired = GetDesired(mode);
            bool allDesiredEnabled = desired.All(spec => subscriptions.Any(x =>
                IsExact(x, spec, broadcasterUserId, callbackUrl)));
            bool permanentCostValid = mode != TwitchEventSubEnsureMode.PermanentOAuth ||
                desired.All(spec => subscriptions.Any(x =>
                    IsExpectedConfiguration(x, spec, broadcasterUserId, callbackUrl) && x.Cost == 0));
            return new TwitchEventSubFinalDecision(allDesiredEnabled, permanentCostValid);
        }

        private static IReadOnlyList<TwitchEventSubCreateSpec> GetDesired(TwitchEventSubEnsureMode mode) =>
            mode == TwitchEventSubEnsureMode.PermanentOAuth ? PermanentSpecs : FallbackSpecs;

        private static bool IsExact(TwitchEventSubFact subscription, TwitchEventSubCreateSpec spec,
            string broadcasterUserId, string callbackUrl) =>
            subscription.Status is "enabled" or "webhook_callback_verification_pending" &&
            IsExpectedConfiguration(subscription, spec, broadcasterUserId, callbackUrl);

        private static bool IsExpectedConfiguration(TwitchEventSubFact subscription, TwitchEventSubCreateSpec spec,
            string broadcasterUserId, string callbackUrl) =>
            subscription.Type == spec.Type && subscription.Version == spec.Version &&
            subscription.BroadcasterUserId == broadcasterUserId && subscription.ConditionCount == 1 &&
            subscription.TransportMethod == "webhook" &&
            string.Equals(subscription.Callback?.TrimEnd('/'), callbackUrl?.TrimEnd('/'), StringComparison.Ordinal);
    }
}
