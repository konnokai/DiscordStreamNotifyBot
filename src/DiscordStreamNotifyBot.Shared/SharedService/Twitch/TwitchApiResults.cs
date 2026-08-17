using TwitchLib.Api.Helix.Models.EventSub;
using Stream = TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream;

namespace DiscordStreamNotifyBot.SharedService.Twitch
{
    /// <summary>EventSub 同步作業應維持的訂閱集合。</summary>
    public enum TwitchEventSubEnsureMode
    {
        /// <summary>已完成 OAuth 授權，永久維持 online、update、offline 三種訂閱。</summary>
        PermanentOAuth,

        /// <summary>未授權的暫時模式，只維持 update、offline 訂閱。</summary>
        Fallback
    }

    /// <summary>安全刪除 broadcaster EventSub 的結果。</summary>
    public enum TwitchEventSubDeleteStatus
    {
        ApiFailure,
        Deleted,
        DeferredLive,
        NoSubscriptions
    }

    /// <summary>可區分 Twitch API 失敗與查無直播的 Get Streams 結果。</summary>
    public sealed class TwitchStreamsResult
    {
        public bool IsSuccess { get; init; }
        public IReadOnlyList<Stream> Streams { get; init; } = Array.Empty<Stream>();
    }

    /// <summary>完整收集所有 EventSub 分頁及成本資訊的結果。</summary>
    public sealed class TwitchEventSubSubscriptionsResult
    {
        public bool IsSuccess { get; init; }
        public IReadOnlyList<EventSubSubscription> Subscriptions { get; init; } = Array.Empty<EventSubSubscription>();
        public int Total { get; init; }
        public int TotalCost { get; init; }
        public int MaxTotalCost { get; init; }
    }

    /// <summary>EventSub 精確同步作業的變更與最終狀態。</summary>
    public sealed class TwitchEventSubEnsureResult
    {
        public bool IsSuccess { get; init; }
        public TwitchEventSubEnsureMode Mode { get; init; }
        public int CreatedCount { get; init; }
        public int DeletedCount { get; init; }
        public bool IsPermanentCostValid { get; init; }
        public TwitchEventSubSubscriptionsResult Subscriptions { get; init; } = new();
    }

    /// <summary>安全刪除 broadcaster 全部 EventSub 的結果。</summary>
    public sealed class TwitchEventSubDeleteResult
    {
        public TwitchEventSubDeleteStatus Status { get; init; }
        public IReadOnlyList<string> DeletedSubscriptionIds { get; init; } = Array.Empty<string>();
    }
}
