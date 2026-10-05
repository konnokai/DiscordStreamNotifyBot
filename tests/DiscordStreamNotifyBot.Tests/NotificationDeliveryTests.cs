using Discord;
using Discord.Net;
using DiscordStreamNotifyBot.SharedService;
using Prometheus;
using System.Net;
using System.Text;

namespace DiscordStreamNotifyBot.Tests
{
    /// <summary>
    /// 逐目標發送共用收尾的錯誤分類：哪些結果寫入完成 checkpoint、哪些保留事件重試、哪些清除通知設定。
    /// </summary>
    public sealed class NotificationDeliveryTests
    {
        private const string Target = "1:2";

        [Fact]
        public async Task SentTargetIsRecordedAndCompleted()
        {
            var harness = new Harness();

            await harness.RunAsync(attempt =>
            {
                attempt.StartTiming();
                attempt.Result = NotificationDeliveryResult.Sent;
                return Task.CompletedTask;
            });

            Assert.True(harness.IsCompleted);
            harness.Progress.ThrowIfFailed();
            string exposition = await harness.ExportAsync();
            Assert.Contains("notification_deliveries_total{platform=\"twitch\",event=\"start\",result=\"sent\"} 1", exposition);
            Assert.Contains("notification_delivery_duration_seconds_count{platform=\"twitch\",event=\"start\"} 1", exposition);
        }

        [Fact]
        public async Task CompletedTargetIsSkipped()
        {
            var harness = new Harness(completedTarget: true);
            bool invoked = false;

            await harness.RunAsync(_ =>
            {
                invoked = true;
                return Task.CompletedTask;
            });

            Assert.False(invoked);
        }

        [Fact]
        public async Task TargetWithoutResultIsNeitherRecordedNorCompleted()
        {
            var harness = new Harness();

            await harness.RunAsync(_ => Task.CompletedTask);

            Assert.False(harness.IsCompleted);
            Assert.DoesNotContain("notification_deliveries_total{", await harness.ExportAsync());
        }

        [Fact]
        public async Task PermanentFailureRemovesNoticeAndCompletes()
        {
            var harness = new Harness();

            await harness.RunAsync(_ => throw new HttpException(HttpStatusCode.Forbidden, null, DiscordErrorCode.MissingPermissions, "測試用", null));

            Assert.Equal(1, harness.PermanentFailures);
            Assert.True(harness.IsCompleted);
            harness.Progress.ThrowIfFailed();
            Assert.Contains("result=\"missing_permission\"} 1", await harness.ExportAsync());
        }

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError, (int)NotificationFailureLog.Discord5xx, "discord_5xx")]
        [InlineData(HttpStatusCode.BadRequest, (int)NotificationFailureLog.DiscordUnknownError, "unknown_error")]
        public async Task DiscordFailureKeepsEventForRetry(HttpStatusCode statusCode, int expectedLog, string expectedResult)
        {
            var harness = new Harness();

            await harness.RunAsync(_ => throw new HttpException(statusCode, null, null, "測試用", null));

            Assert.Equal([(NotificationFailureLog)expectedLog], harness.FailureLogs);
            Assert.False(harness.IsCompleted);
            Assert.Throws<AggregateException>(harness.Progress.ThrowIfFailed);
            Assert.Contains($"result=\"{expectedResult}\"}} 1", await harness.ExportAsync());
        }

        [Fact]
        public async Task TimeoutKeepsEventForRetry()
        {
            var harness = new Harness();

            await harness.RunAsync(_ => throw new TimeoutException());

            Assert.Equal([NotificationFailureLog.Timeout], harness.FailureLogs);
            Assert.False(harness.IsCompleted);
            Assert.Throws<AggregateException>(harness.Progress.ThrowIfFailed);
            Assert.Contains("result=\"timeout\"} 1", await harness.ExportAsync());
        }

        [Fact]
        public async Task FailureAfterPrimaryMessageIsStillRecordedAsSent()
        {
            var harness = new Harness();

            await harness.RunAsync(attempt =>
            {
                attempt.PrimaryMessageSent = true;
                throw new InvalidOperationException("crosspost 失敗");
            });

            Assert.Equal([NotificationFailureLog.UnknownError], harness.FailureLogs);
            Assert.False(harness.IsCompleted);
            Assert.Contains("result=\"sent\"} 1", await harness.ExportAsync());
        }

        [Fact]
        public async Task RetryRequestedByDeliveryIsNotCompleted()
        {
            var harness = new Harness();

            await harness.RunAsync(attempt =>
            {
                attempt.RetryRequired = true;
                attempt.Result = NotificationDeliveryResult.Disabled;
                return Task.CompletedTask;
            });

            Assert.False(harness.IsCompleted);
            Assert.Contains("result=\"disabled\"} 1", await harness.ExportAsync());
        }

        private sealed class Harness
        {
            private readonly CollectorRegistry _registry = Metrics.NewCustomRegistry();
            private readonly Dictionary<string, string> _persisted = [];
            private readonly NotificationDelivery _delivery;

            public Harness(bool completedTarget = false)
            {
                var completed = new Dictionary<string, string>();
                if (completedTarget)
                    completed.Add($"done:{Target}", "1");

                Progress = new NotificationDeliveryProgress(completed, (key, value) =>
                {
                    _persisted[key] = value;
                    return Task.CompletedTask;
                });
                _delivery = new NotificationDelivery(new NotifierMetrics(Metrics.WithCustomRegistry(_registry)),
                    NotificationMetricEvent.TwitchStart, Progress);
            }

            public NotificationDeliveryProgress Progress { get; }
            public List<NotificationFailureLog> FailureLogs { get; } = [];
            public int PermanentFailures { get; private set; }
            public bool IsCompleted => _persisted.ContainsKey($"done:{Target}");

            public Task RunAsync(Func<NotificationDeliveryAttempt, Task> deliverAsync)
                => _delivery.RunAsync(Target, "測試通知", _ => PermanentFailures++,
                    (failure, _) =>
                    {
                        FailureLogs.Add(failure);
                        return "測試通知失敗";
                    },
                    deliverAsync);

            public async Task<string> ExportAsync()
            {
                await using var stream = new MemoryStream();
                await _registry.CollectAndExportAsTextAsync(stream, CancellationToken.None);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }
}
