using DiscordStreamNotifyBot.Auth;
using DiscordStreamNotifyBot.SharedService.Twitch;
using System.Net;
using System.Net.Http.Headers;

namespace DiscordStreamNotifyBot.SharedService.TwitchSubscription
{
    public enum TwitchProviderResultStatus
    {
        Success,
        Invalid,
        Failure,
        TemporaryFailure
    }

    public sealed class TwitchProviderResult<T>
    {
        public TwitchProviderResultStatus Status { get; private init; }
        public T Value { get; private init; }

        public static TwitchProviderResult<T> Success(T value) => new() { Status = TwitchProviderResultStatus.Success, Value = value };
        public static TwitchProviderResult<T> Invalid() => new() { Status = TwitchProviderResultStatus.Invalid };
        public static TwitchProviderResult<T> Failure() => new() { Status = TwitchProviderResultStatus.Failure };
        public static TwitchProviderResult<T> TemporaryFailure() => new() { Status = TwitchProviderResultStatus.TemporaryFailure };
    }

    public sealed class TwitchSubscriptionApiClient
    {
        internal const string HttpClientName = "twitch-subscription";
        private const string HelixSubscriptionUrl = "https://api.twitch.tv/helix/subscriptions/user";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly BotConfig _botConfig;
        private readonly NotifierMetrics _metrics;

        public TwitchSubscriptionApiClient(
            IHttpClientFactory httpClientFactory,
            BotConfig botConfig,
            NotifierMetrics metrics)
        {
            _httpClientFactory = httpClientFactory;
            _botConfig = botConfig;
            _metrics = metrics;
        }

        /// <summary>使用指定使用者 token 查詢其對 broadcaster 的訂閱狀態，並將 HTTP 回應分類為不會誤撤角色的領域結果。</summary>
        public async Task<TwitchSubscriptionResult> CheckUserSubscriptionAsync(
            string accessToken,
            string twitchUserId,
            string broadcasterId,
            CancellationToken cancellationToken)
        {
            try
            {
                string url = $"{HelixSubscriptionUrl}?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&user_id={Uri.EscapeDataString(twitchUserId)}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("Client-Id", _botConfig.TwitchClientId);
                using var response = await _httpClientFactory.CreateClient(HttpClientName).SendAsync(request, cancellationToken);

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return Result(TwitchSubscriptionStatus.NotSubscribed, twitchUserId, broadcasterId);
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return Result(TwitchSubscriptionStatus.AuthorizationInvalid, twitchUserId, broadcasterId);
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _metrics.RecordTwitchSubscriptionProviderError(TwitchSubscriptionProviderError.RateLimited);
                    DateTimeOffset? retryAfter = TryGetRateLimitReset(response);
                    return new TwitchSubscriptionResult
                    {
                        Status = TwitchSubscriptionStatus.TemporaryFailure,
                        TwitchUserId = twitchUserId,
                        BroadcasterId = broadcasterId,
                        RetryAfter = retryAfter
                    };
                }
                if ((int)response.StatusCode >= 500)
                {
                    _metrics.RecordTwitchSubscriptionProviderError(TwitchSubscriptionProviderError.Provider5xx);
                    return Result(TwitchSubscriptionStatus.TemporaryFailure, twitchUserId, broadcasterId);
                }
                if (!response.IsSuccessStatusCode)
                {
                    _metrics.RecordTwitchSubscriptionProviderError(TwitchSubscriptionProviderError.Provider4xx);
                    return Result(TwitchSubscriptionStatus.BroadcasterUnavailable, twitchUserId, broadcasterId);
                }

                string body = await response.Content.ReadAsStringAsync(cancellationToken);
                var payload = JsonConvert.DeserializeObject<TwitchSubscriptionResponse>(body);
                TwitchSubscriptionData subscription = payload?.Data?.FirstOrDefault();
                if (subscription == null || subscription.BroadcasterId != broadcasterId)
                {
                    _metrics.RecordTwitchSubscriptionProviderError(TwitchSubscriptionProviderError.InvalidResponse);
                    return Result(TwitchSubscriptionStatus.TemporaryFailure, twitchUserId, broadcasterId);
                }
                if (subscription.Tier is not ("1000" or "2000" or "3000"))
                {
                    _metrics.RecordTwitchSubscriptionProviderError(TwitchSubscriptionProviderError.InvalidResponse);
                    return Result(TwitchSubscriptionStatus.TemporaryFailure, twitchUserId, broadcasterId);
                }

                return new TwitchSubscriptionResult
                {
                    Status = TwitchSubscriptionStatus.Subscribed,
                    Tier = subscription.Tier,
                    IsGift = subscription.IsGift,
                    TwitchUserId = twitchUserId,
                    BroadcasterId = broadcasterId
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _metrics.RecordTwitchSubscriptionProviderError(
                    ex is JsonException ? TwitchSubscriptionProviderError.InvalidResponse : TwitchSubscriptionProviderError.NetworkFailure);
                Log.Warn($"Twitch 訂閱查詢暫時失敗: {ex.GetType().Name}");
                return Result(TwitchSubscriptionStatus.TemporaryFailure, twitchUserId, broadcasterId);
            }
        }

        /// <summary>向 Twitch 驗證 access token 的應用程式、使用者、scope 與有效期限。</summary>
        public async Task<TwitchProviderResult<TwitchValidateTokenData>> ValidateTokenAsync(
            string accessToken,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return TwitchProviderResult<TwitchValidateTokenData>.Failure();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://id.twitch.tv/oauth2/validate");
                request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", accessToken);
                using var response = await _httpClientFactory.CreateClient(HttpClientName).SendAsync(request, cancellationToken);
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return TwitchProviderResult<TwitchValidateTokenData>.Invalid();
                if (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500)
                    return TwitchProviderResult<TwitchValidateTokenData>.TemporaryFailure();
                if (!response.IsSuccessStatusCode)
                    return TwitchProviderResult<TwitchValidateTokenData>.Failure();

                var data = JsonConvert.DeserializeObject<TwitchValidateTokenData>(
                    await response.Content.ReadAsStringAsync(cancellationToken));
                return data == null
                    ? TwitchProviderResult<TwitchValidateTokenData>.Failure()
                    : TwitchProviderResult<TwitchValidateTokenData>.Success(data);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                return TwitchProviderResult<TwitchValidateTokenData>.TemporaryFailure();
            }
        }

        /// <summary>使用 refresh token 取得新的憑證，並保留 provider 的永久失效與暫時失敗差異。</summary>
        public async Task<TwitchProviderResult<TwitchAccessTokenData>> RefreshTokenAsync(
            string refreshToken,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return TwitchProviderResult<TwitchAccessTokenData>.Invalid();

            try
            {
                using var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = _botConfig.TwitchClientId,
                    ["client_secret"] = _botConfig.TwitchClientSecret,
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = refreshToken
                });
                using var response = await _httpClientFactory.CreateClient(HttpClientName).PostAsync(
                    "https://id.twitch.tv/oauth2/token", content, cancellationToken);
                string body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var token = JsonConvert.DeserializeObject<TwitchAccessTokenData>(body);
                    return string.IsNullOrWhiteSpace(token?.AccessToken)
                        ? TwitchProviderResult<TwitchAccessTokenData>.Failure()
                        : TwitchProviderResult<TwitchAccessTokenData>.Success(token);
                }
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return TwitchProviderResult<TwitchAccessTokenData>.Invalid();
                if (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500)
                    return TwitchProviderResult<TwitchAccessTokenData>.TemporaryFailure();

                TwitchTokenErrorData error = null;
                try { error = JsonConvert.DeserializeObject<TwitchTokenErrorData>(body); } catch (JsonException) { }
                if (string.Equals(error?.Error, "invalid_grant", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(error?.Message, "Invalid refresh token", StringComparison.OrdinalIgnoreCase))
                    return TwitchProviderResult<TwitchAccessTokenData>.Invalid();
                return TwitchProviderResult<TwitchAccessTokenData>.Failure();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                return TwitchProviderResult<TwitchAccessTokenData>.TemporaryFailure();
            }
        }

        private static TwitchSubscriptionResult Result(
            TwitchSubscriptionStatus status,
            string twitchUserId,
            string broadcasterId)
            => new() { Status = status, TwitchUserId = twitchUserId, BroadcasterId = broadcasterId };

        private static DateTimeOffset? TryGetRateLimitReset(HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues("Ratelimit-Reset", out var values) ||
                !long.TryParse(values.FirstOrDefault(), out long seconds))
                return null;
            try { return DateTimeOffset.FromUnixTimeSeconds(seconds); }
            catch (ArgumentOutOfRangeException) { return null; }
        }

        private sealed class TwitchSubscriptionResponse
        {
            [JsonProperty("data")]
            public TwitchSubscriptionData[] Data { get; set; }
        }

        private sealed class TwitchSubscriptionData
        {
            [JsonProperty("broadcaster_id")]
            public string BroadcasterId { get; set; }

            [JsonProperty("is_gift")]
            public bool IsGift { get; set; }

            [JsonProperty("tier")]
            public string Tier { get; set; }
        }
    }
}
