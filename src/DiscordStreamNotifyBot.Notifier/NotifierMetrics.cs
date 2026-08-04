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

    internal enum TwitchSubscriptionRoleOperation
    {
        Synchronize,
        Remove
    }

    internal enum TwitchSubscriptionRoleResult
    {
        Success,
        MissingPermission,
        UserMissing,
        DiscordError,
        UnknownError
    }

    internal enum TwitchTokenOperation
    {
        Decrypt,
        Validate,
        Refresh,
        RefreshLock
    }

    internal enum TwitchTokenOperationResult
    {
        Success,
        Invalid,
        Contended,
        TemporaryFailure
    }

    internal enum TwitchSubscriptionProviderError
    {
        RateLimited,
        Provider4xx,
        Provider5xx,
        NetworkFailure,
        InvalidResponse
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
        private readonly Counter _twitchSubscriptionVerifications;
        private readonly Counter _twitchSubscriptionRoleOperations;
        private readonly Counter _twitchTokenOperations;
        private readonly Gauge _twitchRefreshPendingPersistences;
        private readonly Gauge _twitchRefreshShutdownDraining;
        private readonly Histogram _twitchRefreshShutdownDrainDuration;
        private readonly Counter _twitchSubscriptionCycles;
        private readonly Histogram _twitchSubscriptionCycleDuration;
        private readonly Counter _twitchSubscriptionProviderErrors;
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
            _twitchSubscriptionVerifications = metricFactory.CreateCounter(
                Prefix + "twitch_subscription_verifications_total", "Twitch 訂閱驗證結果。",
                new CounterConfiguration { LabelNames = ["result", "tier"] });
            _twitchSubscriptionRoleOperations = metricFactory.CreateCounter(
                Prefix + "twitch_subscription_role_operations_total", "Twitch 訂閱驗證身分組操作結果。",
                new CounterConfiguration { LabelNames = ["operation", "result"] });
            _twitchTokenOperations = metricFactory.CreateCounter(
                Prefix + "twitch_subscription_token_operations_total", "Twitch 訂閱驗證 token 操作結果。",
                new CounterConfiguration { LabelNames = ["operation", "result"] });
            _twitchRefreshPendingPersistences = metricFactory.CreateGauge(
                Prefix + "twitch_refresh_pending_persistences", "已由 Twitch 接受 rotation、仍等待保存至 MySQL 的 token 數量。");
            _twitchRefreshShutdownDraining = metricFactory.CreateGauge(
                Prefix + "twitch_refresh_shutdown_draining", "Notifier 是否正在等待已接受的 Twitch token rotation 保存完成。");
            _twitchRefreshShutdownDrainDuration = metricFactory.CreateHistogram(
                Prefix + "twitch_refresh_shutdown_drain_duration_seconds", "Notifier 關閉時等待 Twitch token rotation 保存的耗時秒數。",
                new HistogramConfiguration { Buckets = [0.1, 1, 5, 15, 30, 60, 120, 300, 600] });
            _twitchSubscriptionCycles = metricFactory.CreateCounter(
                Prefix + "twitch_subscription_cycles_total", "Twitch 訂閱重新驗證週期執行次數。",
                new CounterConfiguration { LabelNames = ["result"] });
            _twitchSubscriptionCycleDuration = metricFactory.CreateHistogram(
                Prefix + "twitch_subscription_cycle_duration_seconds", "Twitch 訂閱重新驗證週期耗時秒數。",
                new HistogramConfiguration { Buckets = [1, 5, 15, 30, 60, 120, 300, 600] });
            _twitchSubscriptionProviderErrors = metricFactory.CreateCounter(
                Prefix + "twitch_subscription_provider_errors_total", "Twitch 訂閱查詢 provider 錯誤。",
                new CounterConfiguration { LabelNames = ["reason"] });
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

        internal void RecordTwitchSubscriptionVerification(SharedService.Twitch.TwitchSubscriptionStatus result, string tier)
        {
            _twitchSubscriptionVerifications.WithLabels(ToLabel(result), ToTwitchTierLabel(tier)).Inc();
        }

        internal void RecordTwitchSubscriptionRoleOperation(TwitchSubscriptionRoleOperation operation, TwitchSubscriptionRoleResult result)
        {
            _twitchSubscriptionRoleOperations.WithLabels(ToLabel(operation), ToLabel(result)).Inc();
        }

        internal void RecordTwitchTokenOperation(TwitchTokenOperation operation, TwitchTokenOperationResult result)
        {
            _twitchTokenOperations.WithLabels(ToLabel(operation), ToLabel(result)).Inc();
        }

        internal void SetTwitchRefreshPendingPersistenceCount(int count)
        {
            _twitchRefreshPendingPersistences.Set(Math.Max(0, count));
        }

        internal void SetTwitchRefreshShutdownDraining(bool draining)
        {
            _twitchRefreshShutdownDraining.Set(draining ? 1 : 0);
        }

        internal void ObserveTwitchRefreshShutdownDrainDuration(TimeSpan duration)
        {
            _twitchRefreshShutdownDrainDuration.Observe(Math.Max(0, duration.TotalSeconds));
        }

        internal void RecordTwitchSubscriptionCycle(bool succeeded)
        {
            _twitchSubscriptionCycles.WithLabels(succeeded ? "success" : "failure").Inc();
        }

        internal void ObserveTwitchSubscriptionCycleDuration(TimeSpan duration)
        {
            _twitchSubscriptionCycleDuration.Observe(Math.Max(0, duration.TotalSeconds));
        }

        internal void RecordTwitchSubscriptionProviderError(TwitchSubscriptionProviderError error)
        {
            _twitchSubscriptionProviderErrors.WithLabels(ToLabel(error)).Inc();
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

        internal static string ToLabel(SharedService.Twitch.TwitchSubscriptionStatus value) => value switch
        {
            SharedService.Twitch.TwitchSubscriptionStatus.Subscribed => "subscribed",
            SharedService.Twitch.TwitchSubscriptionStatus.NotSubscribed => "not_subscribed",
            SharedService.Twitch.TwitchSubscriptionStatus.AuthorizationMissing => "authorization_missing",
            SharedService.Twitch.TwitchSubscriptionStatus.AuthorizationInvalid => "authorization_invalid",
            SharedService.Twitch.TwitchSubscriptionStatus.BroadcasterUnavailable => "broadcaster_unavailable",
            SharedService.Twitch.TwitchSubscriptionStatus.TemporaryFailure => "temporary_failure",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        internal static string ToTwitchTierLabel(string tier) => tier switch
        {
            "1000" => "1000",
            "2000" => "2000",
            "3000" => "3000",
            _ => "unknown"
        };

        private static string ToLabel(TwitchSubscriptionRoleOperation value) => value switch
        {
            TwitchSubscriptionRoleOperation.Synchronize => "synchronize",
            TwitchSubscriptionRoleOperation.Remove => "remove",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        private static string ToLabel(TwitchSubscriptionRoleResult value) => value switch
        {
            TwitchSubscriptionRoleResult.Success => "success",
            TwitchSubscriptionRoleResult.MissingPermission => "missing_permission",
            TwitchSubscriptionRoleResult.UserMissing => "user_missing",
            TwitchSubscriptionRoleResult.DiscordError => "discord_error",
            TwitchSubscriptionRoleResult.UnknownError => "unknown_error",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        private static string ToLabel(TwitchTokenOperation value) => value switch
        {
            TwitchTokenOperation.Decrypt => "decrypt",
            TwitchTokenOperation.Validate => "validate",
            TwitchTokenOperation.Refresh => "refresh",
            TwitchTokenOperation.RefreshLock => "refresh_lock",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        private static string ToLabel(TwitchTokenOperationResult value) => value switch
        {
            TwitchTokenOperationResult.Success => "success",
            TwitchTokenOperationResult.Invalid => "invalid",
            TwitchTokenOperationResult.Contended => "contended",
            TwitchTokenOperationResult.TemporaryFailure => "temporary_failure",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

        private static string ToLabel(TwitchSubscriptionProviderError value) => value switch
        {
            TwitchSubscriptionProviderError.RateLimited => "rate_limited",
            TwitchSubscriptionProviderError.Provider4xx => "provider_4xx",
            TwitchSubscriptionProviderError.Provider5xx => "provider_5xx",
            TwitchSubscriptionProviderError.NetworkFailure => "network_failure",
            TwitchSubscriptionProviderError.InvalidResponse => "invalid_response",
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
