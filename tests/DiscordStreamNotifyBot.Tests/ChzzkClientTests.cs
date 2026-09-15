using DiscordStreamNotifyBot.HttpClients.Chzzk;
using DiscordStreamNotifyBot.SharedService.Chzzk;
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
