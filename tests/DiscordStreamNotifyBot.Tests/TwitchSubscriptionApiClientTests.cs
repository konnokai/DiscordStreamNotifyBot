using DiscordStreamNotifyBot.SharedService.Twitch;
using DiscordStreamNotifyBot.SharedService.TwitchSubscription;
using Prometheus;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class TwitchSubscriptionApiClientTests
    {
        [Theory]
        [InlineData("1000", false)]
        [InlineData("2000", true)]
        [InlineData("3000", false)]
        public async Task SuccessMapsTierGiftAndRequestContract(string tier, bool isGift)
        {
            HttpRequestMessage captured = null;
            var client = CreateClient(request =>
            {
                captured = CloneRequest(request);
                return Json(HttpStatusCode.OK,
                    $$"""{"data":[{"broadcaster_id":"broadcaster-1","broadcaster_name":"吧噗バブ","broadcaster_login":"babu_desu","is_gift":{{isGift.ToString().ToLowerInvariant()}},"tier":"{{tier}}"}]}""");
            });

            TwitchSubscriptionResult result = await client.CheckUserSubscriptionAsync(
                "access-token", "user-1", "broadcaster-1", CancellationToken.None);

            Assert.Equal(TwitchSubscriptionStatus.Subscribed, result.Status);
            Assert.Equal(tier, result.Tier);
            Assert.Equal(isGift, result.IsGift);
            Assert.Equal("https://api.twitch.tv/helix/subscriptions/user?broadcaster_id=broadcaster-1&user_id=user-1",
                captured.RequestUri.ToString());
            Assert.Equal(new AuthenticationHeaderValue("Bearer", "access-token"), captured.Headers.Authorization);
            Assert.Equal("client-id", Assert.Single(captured.Headers.GetValues("Client-Id")));
        }

        [Theory]
        [InlineData(HttpStatusCode.NotFound, TwitchSubscriptionStatus.NotSubscribed)]
        [InlineData(HttpStatusCode.Unauthorized, TwitchSubscriptionStatus.AuthorizationInvalid)]
        [InlineData(HttpStatusCode.TooManyRequests, TwitchSubscriptionStatus.TemporaryFailure)]
        [InlineData(HttpStatusCode.InternalServerError, TwitchSubscriptionStatus.TemporaryFailure)]
        [InlineData(HttpStatusCode.BadRequest, TwitchSubscriptionStatus.BroadcasterUnavailable)]
        public async Task HttpStatusIsClassifiedWithoutParsingExceptionText(
            HttpStatusCode statusCode,
            TwitchSubscriptionStatus expected)
        {
            var client = CreateClient(_ =>
            {
                var response = Json(statusCode, "{}");
                if (statusCode == HttpStatusCode.TooManyRequests)
                    response.Headers.Add("Ratelimit-Reset", "1785722400");
                return response;
            });

            TwitchSubscriptionResult result = await client.CheckUserSubscriptionAsync(
                "access-token", "user-1", "broadcaster-1", CancellationToken.None);

            Assert.Equal(expected, result.Status);
            if (statusCode == HttpStatusCode.TooManyRequests)
                Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1785722400), result.RetryAfter);
        }

        [Theory]
        [InlineData("4000")]
        [InlineData(null)]
        public async Task InvalidSuccessPayloadIsTemporaryFailure(string tier)
        {
            string payload = tier == null
                ? "{\"data\":[]}"
                : $$"""{"data":[{"broadcaster_id":"broadcaster-1","is_gift":false,"tier":"{{tier}}"}]}""";
            var client = CreateClient(_ => Json(HttpStatusCode.OK, payload));

            TwitchSubscriptionResult result = await client.CheckUserSubscriptionAsync(
                "access-token", "user-1", "broadcaster-1", CancellationToken.None);

            Assert.Equal(TwitchSubscriptionStatus.TemporaryFailure, result.Status);
        }

        [Fact]
        public async Task RefreshUsesFormContractAndMapsRotationPayload()
        {
            string requestBody = null;
            var client = CreateAsyncClient(async (request, cancellationToken) =>
            {
                requestBody = await request.Content.ReadAsStringAsync(cancellationToken);
                return Json(HttpStatusCode.OK,
                    "{\"access_token\":\"new-access\",\"refresh_token\":\"new-refresh\",\"expires_in\":3600,\"scope\":[\"user:read:subscriptions\"],\"token_type\":\"bearer\"}");
            });

            var result = await client.RefreshTokenAsync("old-refresh", CancellationToken.None);

            Assert.Equal(TwitchProviderResultStatus.Success, result.Status);
            Assert.Equal("new-access", result.Value.AccessToken);
            Assert.Contains("client_id=client-id", requestBody);
            Assert.Contains("client_secret=client-secret", requestBody);
            Assert.Contains("grant_type=refresh_token", requestBody);
            Assert.Contains("refresh_token=old-refresh", requestBody);
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, "invalid_grant", TwitchProviderResultStatus.Invalid)]
        [InlineData(HttpStatusCode.BadRequest, "other_error", TwitchProviderResultStatus.Failure)]
        [InlineData(HttpStatusCode.InternalServerError, "server_error", TwitchProviderResultStatus.TemporaryFailure)]
        public async Task RefreshClassifiesStructuredProviderErrors(
            HttpStatusCode status,
            string error,
            TwitchProviderResultStatus expected)
        {
            var client = CreateClient(_ => Json(status, $$"""{"error":"{{error}}","message":"provider message"}"""));

            var result = await client.RefreshTokenAsync("refresh", CancellationToken.None);

            Assert.Equal(expected, result.Status);
        }

        private static TwitchSubscriptionApiClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
            => CreateAsyncClient((request, _) => Task.FromResult(responder(request)));

        private static TwitchSubscriptionApiClient CreateAsyncClient(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        {
            var handler = new StubHandler(responder);
            var factory = new StubHttpClientFactory(new HttpClient(handler));
            var registry = Metrics.NewCustomRegistry();
            return new TwitchSubscriptionApiClient(
                factory,
                new BotConfig { TwitchClientId = "client-id", TwitchClientSecret = "client-secret" },
                new NotifierMetrics(Metrics.WithCustomRegistry(registry)));
        }

        private static HttpResponseMessage Json(HttpStatusCode status, string body)
            => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

        private static HttpRequestMessage CloneRequest(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);
            foreach (var header in request.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            return clone;
        }

        private sealed class StubHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpClient _client;
            public StubHttpClientFactory(HttpClient client) => _client = client;
            public HttpClient CreateClient(string name) => _client;
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;
            public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) => _responder = responder;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => _responder(request, cancellationToken);
        }
    }
}
