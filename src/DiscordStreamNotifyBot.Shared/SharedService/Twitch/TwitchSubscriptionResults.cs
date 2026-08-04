namespace DiscordStreamNotifyBot.SharedService.Twitch
{
    public enum TwitchSubscriptionStatus
    {
        Subscribed,
        NotSubscribed,
        AuthorizationMissing,
        AuthorizationInvalid,
        BroadcasterUnavailable,
        TemporaryFailure
    }

    public sealed class TwitchSubscriptionResult
    {
        public TwitchSubscriptionStatus Status { get; init; }
        public string Tier { get; init; }
        public bool IsGift { get; init; }
        public string TwitchUserId { get; init; }
        public string BroadcasterId { get; init; }
        public DateTimeOffset? RetryAfter { get; init; }
    }
}
