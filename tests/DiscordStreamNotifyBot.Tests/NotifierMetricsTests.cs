using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.Twitch;
using Prometheus;
using System.Text;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class NotifierMetricsTests
    {
        [Fact]
        public async Task RecordsOnlyFixedVerificationAndNotificationLabels()
        {
            var registry = Metrics.NewCustomRegistry();
            var metrics = new NotifierMetrics(Metrics.WithCustomRegistry(registry));

            metrics.Start();
            metrics.RecordYoutubeMemberCheckCycle(YoutubeMemberCheckType.New, YoutubeMemberCheckCycleResult.Success);
            metrics.ObserveYoutubeMemberCheckDuration(YoutubeMemberCheckType.New, TimeSpan.FromSeconds(2));
            metrics.RecordYoutubeMemberVerification(YoutubeMemberCheckType.Old, YoutubeMemberVerificationResult.QuotaExceeded);
            metrics.RecordYoutubeMemberRoleOperation(YoutubeMemberRoleOperation.Remove, YoutubeMemberRoleResult.MissingPermission);
            metrics.RecordTwitchSubscriptionVerification(TwitchSubscriptionStatus.Subscribed, "2000");
            metrics.RecordTwitchSubscriptionRoleOperation(TwitchSubscriptionRoleOperation.Synchronize, TwitchSubscriptionRoleResult.Success);
            metrics.RecordTwitchTokenOperation(TwitchTokenOperation.Refresh, TwitchTokenOperationResult.Contended);
            metrics.SetTwitchRefreshPendingPersistenceCount(2);
            metrics.SetTwitchRefreshShutdownDraining(true);
            metrics.ObserveTwitchRefreshShutdownDrainDuration(TimeSpan.FromSeconds(4));
            metrics.RecordTwitchSubscriptionProviderError(TwitchSubscriptionProviderError.RateLimited);
            metrics.RecordTwitchSubscriptionCycle(true);
            metrics.ObserveTwitchSubscriptionCycleDuration(TimeSpan.FromSeconds(3));
            metrics.RecordNotificationBusMessage(NotifyType.Youtube, NotificationBusMetricResult.Deduplicated);
            metrics.RecordNotificationBusMessage("untrusted-external-type", NotificationBusMetricResult.DispatchFailed);
            metrics.RecordNotificationDelivery(NotificationMetricEvent.TwitchStart, NotificationDeliveryResult.Sent);
            metrics.RecordNotificationDeliveryRetry(NotificationMetricEvent.TwitchStart);
            metrics.ObserveNotificationDeliveryDuration(NotificationMetricEvent.TwitchStart, TimeSpan.FromMilliseconds(500));

            string exposition = await ExportAsync(registry);

            Assert.Contains("discord_stream_notify_notifier_up 1", exposition);
            Assert.Contains("discord_stream_notify_youtube_member_check_last_success_unixtime{check_type=\"new\"} ", exposition);
            Assert.Contains("discord_stream_notify_youtube_member_check_last_success_unixtime{check_type=\"old\"} 0", exposition);
            Assert.Contains("discord_stream_notify_youtube_member_check_cycles_total{check_type=\"new\",result=\"success\"} 1", exposition);
            Assert.Contains("discord_stream_notify_youtube_member_verifications_total{check_type=\"old\",result=\"quota_exceeded\"} 1", exposition);
            Assert.Contains("discord_stream_notify_youtube_member_role_operations_total{operation=\"remove\",result=\"missing_permission\"} 1", exposition);
            Assert.Contains("discord_stream_notify_twitch_subscription_verifications_total{result=\"subscribed\",tier=\"2000\"} 1", exposition);
            Assert.Contains("discord_stream_notify_twitch_subscription_role_operations_total{operation=\"synchronize\",result=\"success\"} 1", exposition);
            Assert.Contains("discord_stream_notify_twitch_subscription_token_operations_total{operation=\"refresh\",result=\"contended\"} 1", exposition);
            Assert.Contains("discord_stream_notify_twitch_refresh_pending_persistences 2", exposition);
            Assert.Contains("discord_stream_notify_twitch_refresh_shutdown_draining 1", exposition);
            Assert.Contains("discord_stream_notify_twitch_refresh_shutdown_drain_duration_seconds_count 1", exposition);
            Assert.Contains("discord_stream_notify_twitch_subscription_provider_errors_total{reason=\"rate_limited\"} 1", exposition);
            Assert.Contains("discord_stream_notify_twitch_subscription_cycles_total{result=\"success\"} 1", exposition);
            Assert.Contains("discord_stream_notify_twitch_subscription_cycle_duration_seconds_count 1", exposition);
            Assert.Contains("discord_stream_notify_notification_bus_messages_total{type=\"youtube\",result=\"deduplicated\"} 1", exposition);
            Assert.Contains("discord_stream_notify_notification_bus_messages_total{type=\"unknown\",result=\"dispatch_failed\"} 1", exposition);
            Assert.Contains("discord_stream_notify_notification_deliveries_total{platform=\"twitch\",event=\"start\",result=\"sent\"} 1", exposition);
            Assert.Contains("discord_stream_notify_notification_delivery_retries_total{platform=\"twitch\",event=\"start\"} 1", exposition);
            Assert.Contains("discord_stream_notify_notification_delivery_duration_seconds_count{platform=\"twitch\",event=\"start\"} 1", exposition);
        }

        [Theory]
        [InlineData(YoutubeNoticeType.NewStream, (int)NotificationMetricEvent.YoutubeNewStream)]
        [InlineData(YoutubeNoticeType.NewVideo, (int)NotificationMetricEvent.YoutubeNewVideo)]
        [InlineData(YoutubeNoticeType.Start, (int)NotificationMetricEvent.YoutubeStart)]
        [InlineData(YoutubeNoticeType.End, (int)NotificationMetricEvent.YoutubeEnd)]
        [InlineData(YoutubeNoticeType.ChangeTime, (int)NotificationMetricEvent.YoutubeChangeTime)]
        [InlineData(YoutubeNoticeType.Delete, (int)NotificationMetricEvent.YoutubeDelete)]
        public void YoutubeNoticeTypesMapToBoundedMetricEvents(YoutubeNoticeType noticeType, int expected)
        {
            Assert.Equal((NotificationMetricEvent)expected, NotifierMetrics.ToMetricEvent(noticeType));
        }

        [Theory]
        [InlineData(TwitchNoticeType.StartStream, (int)NotificationMetricEvent.TwitchStart)]
        [InlineData(TwitchNoticeType.EndStream, (int)NotificationMetricEvent.TwitchEnd)]
        [InlineData(TwitchNoticeType.ChangeStreamData, (int)NotificationMetricEvent.TwitchChangeData)]
        public void TwitchNoticeTypesMapToBoundedMetricEvents(TwitchNoticeType noticeType, int expected)
        {
            Assert.Equal((NotificationMetricEvent)expected, NotifierMetrics.ToMetricEvent(noticeType));
        }

        private static async Task<string> ExportAsync(CollectorRegistry registry)
        {
            await using var stream = new MemoryStream();
            await registry.CollectAndExportAsTextAsync(stream, CancellationToken.None);
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
