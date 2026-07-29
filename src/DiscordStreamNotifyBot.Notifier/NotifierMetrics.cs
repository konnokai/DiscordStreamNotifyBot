using DiscordStreamNotifyBot.Shared.Messages;
using Prometheus;

namespace DiscordStreamNotifyBot
{
    internal enum YoutubeMemberCheckType
    {
        New,
        Old
    }

    internal enum YoutubeMemberCheckCycleResult
    {
        Success,
        Failure
    }

    internal enum YoutubeMemberVerificationResult
    {
        Member,
        NotMember,
        TokenMissing,
        RefreshTokenMissing,
        CredentialExpired,
        CommentsDisabled,
        VideoNotFound,
        QuotaExceeded,
        Provider4xx,
        Provider5xx,
        TemporaryFailure,
        UnknownError
    }

    internal enum YoutubeMemberRoleOperation
    {
        Add,
        Remove
    }

    internal enum YoutubeMemberRoleResult
    {
        Success,
        MissingPermission,
        UserMissing,
        DiscordError,
        UnknownError
    }

    internal enum NotificationBusMetricResult
    {
        InvalidPayload,
        Deduplicated,
        Dispatched,
        DispatchFailed
    }

    internal enum NotificationMetricEvent
    {
        YoutubeNewStream,
        YoutubeNewVideo,
        YoutubeStart,
        YoutubeEnd,
        YoutubeChangeTime,
        YoutubeDelete,
        TwitchStart,
        TwitchEnd,
        TwitchChangeData,
        TwitcastingStart
    }

    internal enum NotificationDeliveryResult
    {
        Sent,
        Disabled,
        MissingGuild,
        MissingChannel,
        MissingPermission,
        Discord5xx,
        Timeout,
        AuthorizationFailure,
        UnknownError
    }

    /// <summary>
    /// Notifier 的 Prometheus 指標封裝。所有 label 都由固定 enum 或已知匯流排 type 轉換，
    /// 禁止帶入 user、guild、channel、video、stream 或例外訊息。
    /// </summary>
    public sealed class NotifierMetrics
    {
        private const string Prefix = "discord_stream_notify_";

        private readonly Gauge _notifierUp;
        private readonly Counter _youtubeMemberCheckCycles;
        private readonly Histogram _youtubeMemberCheckDuration;
        private readonly Gauge _youtubeMemberCheckLastSuccess;
        private readonly Counter _youtubeMemberVerifications;
        private readonly Counter _youtubeMemberRoleOperations;
        private readonly Counter _notificationBusMessages;
        private readonly Counter _notificationDeliveries;
        private readonly Counter _notificationDeliveryRetries;
        private readonly Histogram _notificationDeliveryDuration;

        public NotifierMetrics(IMetricFactory metricFactory = null)
        {
            metricFactory ??= Metrics.DefaultFactory;

            _notifierUp = metricFactory.CreateGauge(
                Prefix + "notifier_up", "Notifier 程序是否已啟動並提供監控服務。");
            _youtubeMemberCheckCycles = metricFactory.CreateCounter(
                Prefix + "youtube_member_check_cycles_total", "YouTube 會限驗證週期執行次數。",
                new CounterConfiguration { LabelNames = ["check_type", "result"] });
            _youtubeMemberCheckDuration = metricFactory.CreateHistogram(
                Prefix + "youtube_member_check_duration_seconds", "YouTube 會限驗證週期耗時秒數。",
                new HistogramConfiguration
                {
                    LabelNames = ["check_type"],
                    Buckets = [1, 5, 15, 30, 60, 120, 300, 600, 1200]
                });
            _youtubeMemberCheckLastSuccess = metricFactory.CreateGauge(
                Prefix + "youtube_member_check_last_success_unixtime", "最近一次成功完成 YouTube 會限驗證週期的 Unix timestamp。",
                new GaugeConfiguration { LabelNames = ["check_type"] });
            _youtubeMemberVerifications = metricFactory.CreateCounter(
                Prefix + "youtube_member_verifications_total", "YouTube 會限逐使用者驗證結果。",
                new CounterConfiguration { LabelNames = ["check_type", "result"] });
            _youtubeMemberRoleOperations = metricFactory.CreateCounter(
                Prefix + "youtube_member_role_operations_total", "YouTube 會限驗證身分組操作結果。",
                new CounterConfiguration { LabelNames = ["operation", "result"] });
            _notificationBusMessages = metricFactory.CreateCounter(
                Prefix + "notification_bus_messages_total", "Notifier 通知匯流排訊息處理結果。",
                new CounterConfiguration { LabelNames = ["type", "result"] });
            _notificationDeliveries = metricFactory.CreateCounter(
                Prefix + "notification_deliveries_total", "三平台通知的最終發送結果。",
                new CounterConfiguration { LabelNames = ["platform", "event", "result"] });
            _notificationDeliveryRetries = metricFactory.CreateCounter(
                Prefix + "notification_delivery_retries_total", "三平台通知因 Discord timeout 或 5xx 進行的重試次數。",
                new CounterConfiguration { LabelNames = ["platform", "event"] });
            _notificationDeliveryDuration = metricFactory.CreateHistogram(
                Prefix + "notification_delivery_duration_seconds", "三平台通知單一目的地的發送耗時秒數。",
                new HistogramConfiguration
                {
                    LabelNames = ["platform", "event"],
                    Buckets = [0.1, 0.25, 0.5, 1, 2, 5, 10, 30]
                });

            _youtubeMemberCheckLastSuccess.WithLabels(ToLabel(YoutubeMemberCheckType.New)).Set(0);
            _youtubeMemberCheckLastSuccess.WithLabels(ToLabel(YoutubeMemberCheckType.Old)).Set(0);
        }

        public void Start() => _notifierUp.Set(1);

        public void Stop() => _notifierUp.Set(0);

        internal void RecordYoutubeMemberCheckCycle(YoutubeMemberCheckType checkType, YoutubeMemberCheckCycleResult result)
        {
            _youtubeMemberCheckCycles.WithLabels(ToLabel(checkType), ToLabel(result)).Inc();
            if (result == YoutubeMemberCheckCycleResult.Success)
                _youtubeMemberCheckLastSuccess.WithLabels(ToLabel(checkType)).Set(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        internal void ObserveYoutubeMemberCheckDuration(YoutubeMemberCheckType checkType, TimeSpan duration)
        {
            _youtubeMemberCheckDuration.WithLabels(ToLabel(checkType)).Observe(Math.Max(0, duration.TotalSeconds));
        }

        internal void RecordYoutubeMemberVerification(YoutubeMemberCheckType checkType, YoutubeMemberVerificationResult result)
        {
            _youtubeMemberVerifications.WithLabels(ToLabel(checkType), ToLabel(result)).Inc();
        }

        internal void RecordYoutubeMemberRoleOperation(YoutubeMemberRoleOperation operation, YoutubeMemberRoleResult result)
        {
            _youtubeMemberRoleOperations.WithLabels(ToLabel(operation), ToLabel(result)).Inc();
        }

        internal void RecordNotificationBusMessage(string type, NotificationBusMetricResult result)
        {
            _notificationBusMessages.WithLabels(ToBusTypeLabel(type), ToLabel(result)).Inc();
        }

        internal void RecordNotificationDelivery(NotificationMetricEvent eventType, NotificationDeliveryResult result)
        {
            (string platform, string eventLabel) = ToLabels(eventType);
            _notificationDeliveries.WithLabels(platform, eventLabel, ToLabel(result)).Inc();
        }

        internal void RecordNotificationDeliveryRetry(NotificationMetricEvent eventType)
        {
            (string platform, string eventLabel) = ToLabels(eventType);
            _notificationDeliveryRetries.WithLabels(platform, eventLabel).Inc();
        }

        internal void ObserveNotificationDeliveryDuration(NotificationMetricEvent eventType, TimeSpan duration)
        {
            (string platform, string eventLabel) = ToLabels(eventType);
            _notificationDeliveryDuration.WithLabels(platform, eventLabel).Observe(Math.Max(0, duration.TotalSeconds));
        }

        internal static NotificationMetricEvent ToMetricEvent(YoutubeNoticeType type) => type switch
        {
            YoutubeNoticeType.NewStream => NotificationMetricEvent.YoutubeNewStream,
            YoutubeNoticeType.NewVideo => NotificationMetricEvent.YoutubeNewVideo,
            YoutubeNoticeType.Start => NotificationMetricEvent.YoutubeStart,
            YoutubeNoticeType.End => NotificationMetricEvent.YoutubeEnd,
            YoutubeNoticeType.ChangeTime => NotificationMetricEvent.YoutubeChangeTime,
            YoutubeNoticeType.Delete => NotificationMetricEvent.YoutubeDelete,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        internal static NotificationMetricEvent ToMetricEvent(TwitchNoticeType type) => type switch
        {
            TwitchNoticeType.StartStream => NotificationMetricEvent.TwitchStart,
            TwitchNoticeType.EndStream => NotificationMetricEvent.TwitchEnd,
            TwitchNoticeType.ChangeStreamData => NotificationMetricEvent.TwitchChangeData,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        internal static string ToLabel(YoutubeMemberCheckType value) => value switch
        {
            YoutubeMemberCheckType.New => "new",
            YoutubeMemberCheckType.Old => "old",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        internal static string ToLabel(YoutubeMemberVerificationResult value) => value switch
        {
            YoutubeMemberVerificationResult.Member => "member",
            YoutubeMemberVerificationResult.NotMember => "not_member",
            YoutubeMemberVerificationResult.TokenMissing => "token_missing",
            YoutubeMemberVerificationResult.RefreshTokenMissing => "refresh_token_missing",
            YoutubeMemberVerificationResult.CredentialExpired => "credential_expired",
            YoutubeMemberVerificationResult.CommentsDisabled => "comments_disabled",
            YoutubeMemberVerificationResult.VideoNotFound => "video_not_found",
            YoutubeMemberVerificationResult.QuotaExceeded => "quota_exceeded",
            YoutubeMemberVerificationResult.Provider4xx => "provider_4xx",
            YoutubeMemberVerificationResult.Provider5xx => "provider_5xx",
            YoutubeMemberVerificationResult.TemporaryFailure => "temporary_failure",
            YoutubeMemberVerificationResult.UnknownError => "unknown_error",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        internal static (string Platform, string Event) ToLabels(NotificationMetricEvent value) => value switch
        {
            NotificationMetricEvent.YoutubeNewStream => ("youtube", "new_stream"),
            NotificationMetricEvent.YoutubeNewVideo => ("youtube", "new_video"),
            NotificationMetricEvent.YoutubeStart => ("youtube", "start"),
            NotificationMetricEvent.YoutubeEnd => ("youtube", "end"),
            NotificationMetricEvent.YoutubeChangeTime => ("youtube", "change_time"),
            NotificationMetricEvent.YoutubeDelete => ("youtube", "delete"),
            NotificationMetricEvent.TwitchStart => ("twitch", "start"),
            NotificationMetricEvent.TwitchEnd => ("twitch", "end"),
            NotificationMetricEvent.TwitchChangeData => ("twitch", "change_data"),
            NotificationMetricEvent.TwitcastingStart => ("twitcasting", "start"),
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        private static string ToLabel(YoutubeMemberCheckCycleResult value) => value switch
        {
            YoutubeMemberCheckCycleResult.Success => "success",
            YoutubeMemberCheckCycleResult.Failure => "failure",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        private static string ToLabel(YoutubeMemberRoleOperation value) => value switch
        {
            YoutubeMemberRoleOperation.Add => "add",
            YoutubeMemberRoleOperation.Remove => "remove",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        private static string ToLabel(YoutubeMemberRoleResult value) => value switch
        {
            YoutubeMemberRoleResult.Success => "success",
            YoutubeMemberRoleResult.MissingPermission => "missing_permission",
            YoutubeMemberRoleResult.UserMissing => "user_missing",
            YoutubeMemberRoleResult.DiscordError => "discord_error",
            YoutubeMemberRoleResult.UnknownError => "unknown_error",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        private static string ToLabel(NotificationBusMetricResult value) => value switch
        {
            NotificationBusMetricResult.InvalidPayload => "invalid_payload",
            NotificationBusMetricResult.Deduplicated => "deduplicated",
            NotificationBusMetricResult.Dispatched => "dispatched",
            NotificationBusMetricResult.DispatchFailed => "dispatch_failed",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        private static string ToLabel(NotificationDeliveryResult value) => value switch
        {
            NotificationDeliveryResult.Sent => "sent",
            NotificationDeliveryResult.Disabled => "disabled",
            NotificationDeliveryResult.MissingGuild => "missing_guild",
            NotificationDeliveryResult.MissingChannel => "missing_channel",
            NotificationDeliveryResult.MissingPermission => "missing_permission",
            NotificationDeliveryResult.Discord5xx => "discord_5xx",
            NotificationDeliveryResult.Timeout => "timeout",
            NotificationDeliveryResult.AuthorizationFailure => "authorization_failure",
            NotificationDeliveryResult.UnknownError => "unknown_error",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        private static string ToBusTypeLabel(string type) => type switch
        {
            NotifyType.Youtube => "youtube",
            NotifyType.Twitch => "twitch",
            NotifyType.Twitcasting => "twitcasting",
            NotifyType.Banner => "banner",
            NotifyType.YoutubeMemberVideoLog => "youtube_member_video_log",
            _ => "unknown"
        };
    }
}
