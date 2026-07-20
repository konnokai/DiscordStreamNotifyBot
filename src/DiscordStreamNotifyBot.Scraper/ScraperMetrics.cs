using Prometheus;

namespace DiscordStreamNotifyBot.Scraper
{
    public enum TwitchSpiderMetricMode
    {
        OAuth,
        Fallback,
        Warning,
        Unmonitored
    }

    public enum TwitchEventSubMetricType
    {
        StreamOnline,
        ChannelUpdate,
        StreamOffline
    }

    public enum TwitchEventSubMetricStatus
    {
        Enabled,
        WebhookCallbackVerificationPending,
        WebhookCallbackVerificationFailed,
        NotificationFailuresExceeded,
        AuthorizationRevoked,
        ModeratorRemoved,
        UserRemoved,
        VersionRemoved,
        BetaMaintenance,
        WebsocketDisconnected,
        WebsocketFailedPingPong,
        WebsocketReceivedInboundTraffic,
        Unknown
    }

    public enum ScraperMetricResult
    {
        Success,
        Failure
    }

    public enum TwitchAuthorizationChangeMetricResult
    {
        Authorized,
        Reauthorized,
        Revoked,
        Invalid,
        Failure
    }

    public enum TwitchSpiderRemovalMetricReason
    {
        AuthorizationRevoked,
        AuthorizationInvalid,
        GuildIneligible,
        GuildMissing
    }

    public enum TwitchEventSubCleanupDeferredMetricReason
    {
        StreamLive,
        TwitchApiFailure,
        GuildSnapshotUnavailable,
        NotifierUnavailable
    }

    /// <summary>
    /// Scraper 的 Twitch Prometheus 指標封裝。所有 label 都由固定 enum 轉換，禁止帶入使用者、
    /// guild、broadcaster 或 EventSub subscription 識別碼。
    /// </summary>
    public sealed class ScraperMetrics
    {
        private const string Prefix = "discord_stream_notify_";

        private readonly Gauge _spiders = Metrics.CreateGauge(
            Prefix + "twitch_spiders", "各偵測模式目前的 Twitch spider 數。",
            new GaugeConfiguration { LabelNames = ["mode"] });
        private readonly Gauge _eventSubSubscriptions = Metrics.CreateGauge(
            Prefix + "twitch_eventsub_subscriptions", "依事件種類、偵測模式與狀態統計的 EventSub subscription 數。",
            new GaugeConfiguration { LabelNames = ["type", "mode", "status"] });
        private readonly Gauge _eventSubTotalCost = Metrics.CreateGauge(
            Prefix + "twitch_eventsub_total_cost", "Twitch EventSub 目前總成本。");
        private readonly Gauge _eventSubMaxTotalCost = Metrics.CreateGauge(
            Prefix + "twitch_eventsub_max_total_cost", "Twitch EventSub 目前允許的最大總成本。");
        private readonly Counter _reconcile = Metrics.CreateCounter(
            Prefix + "twitch_reconcile_total", "Twitch EventSub reconcile 執行次數。",
            new CounterConfiguration { LabelNames = ["result"] });
        private readonly Gauge _reconcileLastSuccessUnixTime = Metrics.CreateGauge(
            Prefix + "twitch_reconcile_last_success_unixtime", "最近一次成功完成 Twitch EventSub reconcile 的 Unix timestamp。");
        private readonly Counter _pollCycles = Metrics.CreateCounter(
            Prefix + "twitch_poll_cycles_total", "Twitch polling 迴圈執行次數。",
            new CounterConfiguration { LabelNames = ["result"] });
        private readonly Counter _authorizationChanges = Metrics.CreateCounter(
            Prefix + "twitch_authorization_changes_total", "Scraper 處理 Twitch 授權狀態變更的次數。",
            new CounterConfiguration { LabelNames = ["result"] });
        private readonly Counter _spiderRemovals = Metrics.CreateCounter(
            Prefix + "twitch_spider_removals_total", "授權失效後自動移除 Twitch spider 的次數。",
            new CounterConfiguration { LabelNames = ["reason"] });
        private readonly Gauge _spiderCleanupPending = Metrics.CreateGauge(
            Prefix + "twitch_spider_cleanup_pending", "等待授權或 guild 資格確認的 Twitch spider cleanup 數。");
        private readonly Gauge _eventSubCleanupDeferred = Metrics.CreateGauge(
            Prefix + "twitch_eventsub_cleanup_deferred", "目前延後執行的 Twitch EventSub cleanup 數。",
            new GaugeConfiguration { LabelNames = ["reason"] });
        private readonly Counter _oauthBypassAdditions = Metrics.CreateCounter(
            Prefix + "twitch_oauth_bypass_additions_total", "使用 Twitch OAuth 豁免 200 人限制新增 spider 的次數。");

        public void SetSpiderCount(TwitchSpiderMetricMode mode, int count)
        {
            _spiders.WithLabels(ToLabel(mode)).Set(NonNegative(count));
        }

        public void SetEventSubSubscriptionCount(TwitchEventSubMetricType type, TwitchSpiderMetricMode mode,
            TwitchEventSubMetricStatus status, int count)
        {
            _eventSubSubscriptions.WithLabels(ToLabel(type), ToLabel(mode), ToLabel(status)).Set(NonNegative(count));
        }

        public void UpdateEventSubCosts(double totalCost, double maxTotalCost)
        {
            _eventSubTotalCost.Set(NonNegative(totalCost));
            _eventSubMaxTotalCost.Set(NonNegative(maxTotalCost));
        }

        public void RecordReconcile(ScraperMetricResult result)
        {
            _reconcile.WithLabels(ToLabel(result)).Inc();
            if (result == ScraperMetricResult.Success)
                _reconcileLastSuccessUnixTime.Set(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        public void RecordPollCycle(ScraperMetricResult result)
        {
            _pollCycles.WithLabels(ToLabel(result)).Inc();
        }

        public void RecordAuthorizationChange(TwitchAuthorizationChangeMetricResult result)
        {
            _authorizationChanges.WithLabels(ToLabel(result)).Inc();
        }

        public void RecordSpiderRemoval(TwitchSpiderRemovalMetricReason reason)
        {
            _spiderRemovals.WithLabels(ToLabel(reason)).Inc();
        }

        public void SetSpiderCleanupPendingCount(int count)
        {
            _spiderCleanupPending.Set(NonNegative(count));
        }

        public void SetEventSubCleanupDeferredCount(TwitchEventSubCleanupDeferredMetricReason reason, int count)
        {
            _eventSubCleanupDeferred.WithLabels(ToLabel(reason)).Set(NonNegative(count));
        }

        public void RecordOAuthBypassAddition()
        {
            _oauthBypassAdditions.Inc();
        }

        private static double NonNegative(double value) => Math.Max(0, value);

        private static string ToLabel(TwitchSpiderMetricMode mode) => mode switch
        {
            TwitchSpiderMetricMode.OAuth => "oauth",
            TwitchSpiderMetricMode.Fallback => "fallback",
            TwitchSpiderMetricMode.Warning => "warning",
            TwitchSpiderMetricMode.Unmonitored => "unmonitored",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

        private static string ToLabel(TwitchEventSubMetricType type) => type switch
        {
            TwitchEventSubMetricType.StreamOnline => "stream_online",
            TwitchEventSubMetricType.ChannelUpdate => "channel_update",
            TwitchEventSubMetricType.StreamOffline => "stream_offline",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        private static string ToLabel(TwitchEventSubMetricStatus status) => status switch
        {
            TwitchEventSubMetricStatus.Enabled => "enabled",
            TwitchEventSubMetricStatus.WebhookCallbackVerificationPending => "webhook_callback_verification_pending",
            TwitchEventSubMetricStatus.WebhookCallbackVerificationFailed => "webhook_callback_verification_failed",
            TwitchEventSubMetricStatus.NotificationFailuresExceeded => "notification_failures_exceeded",
            TwitchEventSubMetricStatus.AuthorizationRevoked => "authorization_revoked",
            TwitchEventSubMetricStatus.ModeratorRemoved => "moderator_removed",
            TwitchEventSubMetricStatus.UserRemoved => "user_removed",
            TwitchEventSubMetricStatus.VersionRemoved => "version_removed",
            TwitchEventSubMetricStatus.BetaMaintenance => "beta_maintenance",
            TwitchEventSubMetricStatus.WebsocketDisconnected => "websocket_disconnected",
            TwitchEventSubMetricStatus.WebsocketFailedPingPong => "websocket_failed_ping_pong",
            TwitchEventSubMetricStatus.WebsocketReceivedInboundTraffic => "websocket_received_inbound_traffic",
            TwitchEventSubMetricStatus.Unknown => "unknown",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

        private static string ToLabel(ScraperMetricResult result) => result switch
        {
            ScraperMetricResult.Success => "success",
            ScraperMetricResult.Failure => "failure",
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, null)
        };

        private static string ToLabel(TwitchAuthorizationChangeMetricResult result) => result switch
        {
            TwitchAuthorizationChangeMetricResult.Authorized => "authorized",
            TwitchAuthorizationChangeMetricResult.Reauthorized => "reauthorized",
            TwitchAuthorizationChangeMetricResult.Revoked => "revoked",
            TwitchAuthorizationChangeMetricResult.Invalid => "invalid",
            TwitchAuthorizationChangeMetricResult.Failure => "failure",
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, null)
        };

        private static string ToLabel(TwitchSpiderRemovalMetricReason reason) => reason switch
        {
            TwitchSpiderRemovalMetricReason.AuthorizationRevoked => "authorization_revoked",
            TwitchSpiderRemovalMetricReason.AuthorizationInvalid => "authorization_invalid",
            TwitchSpiderRemovalMetricReason.GuildIneligible => "guild_ineligible",
            TwitchSpiderRemovalMetricReason.GuildMissing => "guild_missing",
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null)
        };

        private static string ToLabel(TwitchEventSubCleanupDeferredMetricReason reason) => reason switch
        {
            TwitchEventSubCleanupDeferredMetricReason.StreamLive => "stream_live",
            TwitchEventSubCleanupDeferredMetricReason.TwitchApiFailure => "twitch_api_failure",
            TwitchEventSubCleanupDeferredMetricReason.GuildSnapshotUnavailable => "guild_snapshot_unavailable",
            TwitchEventSubCleanupDeferredMetricReason.NotifierUnavailable => "notifier_unavailable",
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null)
        };
    }
}
