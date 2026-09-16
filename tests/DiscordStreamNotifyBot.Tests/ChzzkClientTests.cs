using DiscordStreamNotifyBot.HttpClients.Chzzk;
using DiscordStreamNotifyBot.SharedService.Chzzk;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Scraper.Detection.Chzzk;
using DiscordStreamNotifyBot.Shared.Messages;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class ChzzkClientTests
    {
        private const string ChannelId = "f39c3d74e33a81ab3080356b91bb8de5";

        private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
        {
            public Uri LastRequestUri { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequestUri = request.RequestUri;
                return Task.FromResult(respond(request));
            }
        }

        private static HttpResponseMessage Json(string file, HttpStatusCode statusCode = HttpStatusCode.OK)
            => new(statusCode)
            {
                Content = new StringContent(
                    File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Chzzk", file)),
                    Encoding.UTF8, "application/json")
            };

        private static ChzzkClient CreateClient(StubHandler handler)
            => new(new HttpClient(handler));

        [Fact]
        public async Task OpenFixtureIsParsedAndKeyUsesNormalizedOpenDate()
        {
            var handler = new StubHandler(_ => Json("live-status.open.json"));
            var result = await CreateClient(handler).GetLiveStatusAsync(ChannelId);

            Assert.True(result.IsSuccess);
            Assert.Equal(ChzzkLiveStatusValues.Open, ChzzkClient.TryGetKnownStatus(result));
            Assert.Equal("2026-09-15 13:41:52", result.Status.OpenDate);
            Assert.Null(result.Status.CloseDate);
            Assert.Equal("Just Chatting", result.Status.LiveCategoryValue);

            Assert.True(ChzzkStreamIdentity.TryCreate(result.Status.ChannelId, result.Status.OpenDate, out string key));
            Assert.Equal($"{ChannelId}:20260915_134152", key);
        }

        [Fact]
        public async Task CloseFixtureIgnoresNestedPollingStatus()
        {
            // CLOSE 樣本的 livePollingStatusJson 仍為 STARTED；生命週期只認頂層 status。
            var handler = new StubHandler(_ => Json("live-status.close.json"));
            var result = await CreateClient(handler).GetLiveStatusAsync(ChannelId);

            Assert.True(result.IsSuccess);
            Assert.Equal(ChzzkLiveStatusValues.Close, ChzzkClient.TryGetKnownStatus(result));
            Assert.Equal("2026-09-15 15:20:00", result.Status.CloseDate);
        }

        [Fact]
        public async Task ChannelFixtureIsParsed()
        {
            var handler = new StubHandler(_ => Json("channel.json"));
            var result = await CreateClient(handler).GetChannelAsync(ChannelId);

            Assert.True(result.IsSuccess);
            Assert.Equal("테스트 채널", result.Channel.ChannelName);
            Assert.Equal("https://example.invalid/avatar.png", result.Channel.ChannelImageUrl);
            Assert.Contains($"/service/v1/channels/{ChannelId}", handler.LastRequestUri.AbsolutePath);
        }

        [Fact]
        public async Task ReportedCloseReachesDelayedConfirmationWithPersistedNormalizedKey()
        {
            const string channelId = "64d76089fba26b180d9c9e48a32600d9";
            const string key = channelId + ":20260915_175844";
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {"code":200,"content":{
                      "channelId":"64d76089fba26b180d9c9e48a32600d9",
                      "status":"CLOSE","openDate":"2026-09-15 17:58:44","closeDate":"2026-09-16 03:04:30",
                      "livePollingStatusJson":"{\"status\":\"STARTED\",\"isPublishing\":true,\"playableStatus\":\"PLAYABLE\"}"
                    }}
                    """, Encoding.UTF8, "application/json")
            });
            var client = CreateClient(handler);
            var now = new DateTime(2026, 9, 15, 18, 5, 0, DateTimeKind.Utc);
            var spider = new ChzzkSpider { ChannelId = channelId, CurrentStreamKey = key, InitializedAt = now.AddDays(-1) };
            var current = new ChzzkStream
            {
                StreamKey = key, ChannelId = channelId, OpenDateRaw = "2026-09-15 17:58:44",
                Status = ChzzkStreamStatus.Open,
                LastObservedAt = new DateTime(2026, 9, 15, 18, 4, 28, DateTimeKind.Utc)
            };
            var result = await client.GetLiveStatusAsync(channelId);

            Assert.True(result.IsSuccess);
            Assert.Equal(ChzzkPollAction.StartPendingClose,
                ChzzkDetectionService.DecideObservation(spider, current, result.Status, now, out var observedKey));
            Assert.Equal(key, observedKey);

            current.Status = ChzzkStreamStatus.PendingClose;
            current.LastObservedAt = now;
            Assert.Equal(ChzzkPollAction.Ignore,
                ChzzkDetectionService.DecideObservation(spider, current, result.Status,
                    now + ChzzkPollPolicy.CloseConfirmationDelay - TimeSpan.FromTicks(1), out _));

            // 重新讀取 API 並還原持久化場次，確認重啟不會重設等待起點。
            current = JsonConvert.DeserializeObject<ChzzkStream>(JsonConvert.SerializeObject(current));
            result = await client.GetLiveStatusAsync(channelId);
            Assert.True(result.IsSuccess);
            Assert.Equal(ChzzkPollAction.ConfirmClose,
                ChzzkDetectionService.DecideObservation(spider, current, result.Status,
                    now + ChzzkPollPolicy.CloseConfirmationDelay, out _));
            Assert.Equal(now, current.LastObservedAt);

            ChzzkNotification notification = null;
            await ChzzkDetectionService.ConfirmCloseAsync(spider, current, result.Status.CloseDate,
                now + ChzzkPollPolicy.CloseConfirmationDelay, dto =>
                {
                    Assert.Equal(ChzzkStreamStatus.PendingClose, current.Status);
                    notification = dto;
                    return Task.CompletedTask;
                });
            var payload = JsonConvert.SerializeObject(notification);
            Assert.Equal(ChzzkNoticeType.EndStream, notification.NoticeType);
            Assert.Equal(ChzzkStreamStatus.Closed, current.Status);
            Assert.Equal("2026-09-16 03:04:30", current.CloseDateRaw);
            Assert.Equal(key, notification.StreamKey);
            Assert.Equal(new DateTime(2026, 9, 15, 8, 58, 44, DateTimeKind.Utc), notification.StreamStartAt);
            Assert.Equal(new DateTime(2026, 9, 15, 18, 4, 30, DateTimeKind.Utc), notification.StreamEndAt);
            Assert.Equal($"notified:0:cz:{key}:1", NotificationDedupPolicy.TryGetKey(0, NotifyType.Chzzk, payload));
            Assert.Equal(ChzzkPollAction.Ignore,
                ChzzkDetectionService.DecideObservation(spider, current, result.Status,
                    now + ChzzkPollPolicy.CloseConfirmationDelay, out _));
        }

        [Theory]
        [InlineData("\"WAIT\"")]
        [InlineData("null")]
        public async Task UnknownStatusIsNotTreatedAsKnown(string statusJson)
        {
            string body = "{\"code\":200,\"content\":{\"channelId\":\"" + ChannelId +
                "\",\"status\":" + statusJson + ",\"openDate\":\"2026-09-15 13:41:52\"}}";
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
            var result = await CreateClient(handler).GetLiveStatusAsync(ChannelId);

            Assert.Null(ChzzkClient.TryGetKnownStatus(result));
        }

        [Fact]
        public async Task ChannelIdMismatchIsRejected()
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"code":200,"content":{"channelId":"other-channel","status":"OPEN","openDate":"2026-09-15 13:41:52"}}""",
                    Encoding.UTF8, "application/json")
            });
            var result = await CreateClient(handler).GetLiveStatusAsync(ChannelId);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task BusinessErrorCodeIsNotSuccess()
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"code":404,"message":"not found","content":null}""", Encoding.UTF8, "application/json")
            });
            var result = await CreateClient(handler).GetLiveStatusAsync(ChannelId);

            Assert.False(result.IsSuccess);
            Assert.False(result.IsNotFound);
        }

        [Fact]
        public async Task NotFoundIsReportedWithoutDeletionSignal()
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
            var result = await CreateClient(handler).GetChannelAsync(ChannelId);

            Assert.False(result.IsSuccess);
            Assert.True(result.IsNotFound);
        }

        [Fact]
        public async Task RateLimitExposesRetryAfter()
        {
            var handler = new StubHandler(_ =>
            {
                var response = new HttpResponseMessage((HttpStatusCode)429);
                response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
                return response;
            });
            var result = await CreateClient(handler).GetLiveStatusAsync(ChannelId);

            Assert.False(result.IsSuccess);
            Assert.Equal(TimeSpan.FromSeconds(30), result.RetryAfter);
        }

        [Fact]
        public async Task NetworkFailureIsUnknown()
        {
            var handler = new StubHandler(_ => throw new HttpRequestException("boom"));
            var result = await CreateClient(handler).GetLiveStatusAsync(ChannelId);

            Assert.False(result.IsSuccess);
            Assert.Null(result.RetryAfter);
        }
    }
}
