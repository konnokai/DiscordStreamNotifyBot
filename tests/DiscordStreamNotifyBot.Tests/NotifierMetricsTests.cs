using DiscordStreamNotifyBot.Shared.Messages;
using Prometheus;
using System.Text;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class NotifierMetricsTests
    {
        [Fact]
        public async Task RecordsOnlyFixedYoutubeMemberAndNotificationLabels()
        {
            var registry = Metrics.NewCustomRegistry();
            var metrics = new NotifierMetrics(Metrics.WithCustomRegistry(registry));

            metrics.Start();
            metrics.RecordYoutubeMemberCheckCycle(YoutubeMemberCheckType.New, YoutubeMemberCheckCycleResult.Success);
            metrics.ObserveYoutubeMemberCheckDuration(YoutubeMemberCheckType.New, TimeSpan.FromSeconds(2));
            metrics.RecordYoutubeMemberVerification(YoutubeMemberCheckType.Old, YoutubeMemberVerificationResult.QuotaExceeded);
            metrics.RecordYoutubeMemberRoleOperation(YoutubeMemberRoleOperation.Remove, YoutubeMemberRoleResult.MissingPermission);
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
