using DiscordStreamNotifyBot.HttpClients.Chzzk.Model;
using System.Net;

#nullable enable

namespace DiscordStreamNotifyBot.HttpClients.Chzzk
{
    /// <summary>CHZZK 網站 live-status 的 <c>content.status</c> 已知值。</summary>
    public static class ChzzkLiveStatusValues
    {
        public const string Open = "OPEN";
        public const string Close = "CLOSE";
    }

    public sealed record ChzzkChannelResult(
        bool IsSuccess,
        bool IsNotFound,
        TimeSpan? RetryAfter,
        ChzzkChannel? Channel,
        int? HttpStatus = null);

    public sealed record ChzzkLiveStatusResult(
        bool IsSuccess,
        bool IsNotFound,
        TimeSpan? RetryAfter,
        ChzzkLiveStatus? Status,
        int? HttpStatus = null);

    /// <summary>
    /// CHZZK 網站匿名 endpoint client（計畫 §2、§6）。
    /// <para>
    /// 固定 API origin，只把驗證過的 channelId 放進路徑；不帶任何憑證、不跟隨使用者提供的網址。
    /// 呼叫端必須同時檢查 HTTP、<c>body.code</c> 與 <c>content</c>：本 client 只在三者皆有效時回傳
    /// <c>IsSuccess</c>，其餘一律視為未知，由呼叫端保留原狀態。
    /// </para>
    /// <para>
    /// 429 會帶出有效的 <c>Retry-After</c>（沒有標頭時為 null，由呼叫端自行退避），
    /// 不在此層重試以避免密集重試。
    /// </para>
    /// </summary>
    public sealed class ChzzkClient
    {
        internal const string ApiOrigin = "https://api.chzzk.naver.com";

        private readonly HttpClient _httpClient;

        public ChzzkClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ChzzkChannelResult> GetChannelAsync(string channelId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(channelId))
                return new ChzzkChannelResult(false, false, null, null);

            var (isSuccess, isNotFound, retryAfter, content, httpStatus) = await SendAsync<ChzzkChannel>(
                $"service/v1/channels/{Uri.EscapeDataString(channelId)}", cancellationToken).ConfigureAwait(false);

            // channelId 必須與請求相符，避免汙染快取。
            if (isSuccess && !string.Equals(content!.ChannelId, channelId, StringComparison.Ordinal))
                return new ChzzkChannelResult(false, false, null, null, httpStatus);

            return new ChzzkChannelResult(isSuccess, isNotFound, retryAfter, content, httpStatus);
        }

        public async Task<ChzzkLiveStatusResult> GetLiveStatusAsync(string channelId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(channelId))
                return new ChzzkLiveStatusResult(false, false, null, null);

            var (isSuccess, isNotFound, retryAfter, content, httpStatus) = await SendAsync<ChzzkLiveStatus>(
                $"polling/v3.1/channels/{Uri.EscapeDataString(channelId)}/live-status?includePlayerRecommendContent=false",
                cancellationToken).ConfigureAwait(false);

            if (isSuccess && !string.Equals(content!.ChannelId, channelId, StringComparison.Ordinal))
                return new ChzzkLiveStatusResult(false, false, null, null, httpStatus);

            return new ChzzkLiveStatusResult(isSuccess, isNotFound, retryAfter, content, httpStatus);
        }

        private async Task<(bool IsSuccess, bool IsNotFound, TimeSpan? RetryAfter, T? Content, int? HttpStatus)> SendAsync<T>(
            string path,
            CancellationToken cancellationToken) where T : class
        {
            try
            {
                using var response = await _httpClient
                    .GetAsync(new Uri(ApiOrigin + "/" + path), cancellationToken)
                    .ConfigureAwait(false);

                int statusCode = (int)response.StatusCode;
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    return (false, false, ParseRetryAfter(response), null, statusCode);
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return (false, true, null, null, statusCode);
                if (!response.IsSuccessStatusCode)
                    return (false, false, null, null, statusCode);

                string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var envelope = JsonConvert.DeserializeObject<ChzzkEnvelope<T>>(json);
                if (envelope == null || envelope.Code != 200 || envelope.Content == null)
                    return (false, false, null, null, envelope?.Code);

                return (true, false, null, envelope.Content, statusCode);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                // 網路錯誤與 5xx 一律視為未知：呼叫端不得據以清除或改寫場次狀態。
                return (false, false, null, null, null);
            }
        }

        private static TimeSpan? ParseRetryAfter(HttpResponseMessage response)
        {
            var retryAfter = response.Headers.RetryAfter;
            if (retryAfter?.Delta is { } delta && delta > TimeSpan.Zero)
                return delta;
            if (retryAfter?.Date is { } date && date > DateTimeOffset.UtcNow)
                return date - DateTimeOffset.UtcNow;
            return null;
        }

        /// <summary>解析回應狀態；僅 OPEN/CLOSE 為已知狀態，其餘（含失敗回應）回傳 null。</summary>
        public static string? TryGetKnownStatus(ChzzkLiveStatusResult result)
            => result.IsSuccess ? TryGetKnownStatus(result.Status!.Status) : null;

        /// <summary>解析狀態字串；僅 OPEN/CLOSE 為已知狀態，其餘回傳 null。</summary>
        public static string? TryGetKnownStatus(string status) => status switch
        {
            ChzzkLiveStatusValues.Open => ChzzkLiveStatusValues.Open,
            ChzzkLiveStatusValues.Close => ChzzkLiveStatusValues.Close,
            _ => null
        };
    }
}
