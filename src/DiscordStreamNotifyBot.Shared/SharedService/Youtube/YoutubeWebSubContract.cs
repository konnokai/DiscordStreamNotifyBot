using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using DiscordStreamNotifyBot.Shared;

namespace DiscordStreamNotifyBot.SharedService.Youtube
{
    /// <summary>YouTube WebSub 訂閱要求的結果分類（計畫 §8.2）。</summary>
    public enum YoutubeWebSubRequestOutcome
    {
        /// <summary>Hub 已受理（任一 2xx），實際成立與否由後續 challenge 決定。</summary>
        Accepted,

        /// <summary>429、5xx、網路錯誤或 timeout；可等待後重試。</summary>
        TransientFailure,

        /// <summary>其他 4xx；重試同一要求不會成功，應略過該頻道。</summary>
        PermanentFailure,

        /// <summary>既有 pending 已被 Hub 拒絕且未強制重訂，未送出要求（計畫 §6.3）。</summary>
        Suppressed,
    }

    /// <summary>單一頻道訂閱要求的結果；保留可判斷的失敗類型與診斷摘要，取代原本的單一 bool。</summary>
    public sealed class YoutubeWebSubRequestResult
    {
        public YoutubeWebSubRequestOutcome Outcome { get; private init; }
        public HttpStatusCode? StatusCode { get; private init; }
        public TimeSpan? RetryAfter { get; private init; }
        public string DiagnosticSummary { get; private init; }

        public static YoutubeWebSubRequestResult Accepted(HttpStatusCode statusCode)
            => new() { Outcome = YoutubeWebSubRequestOutcome.Accepted, StatusCode = statusCode };

        public static YoutubeWebSubRequestResult Transient(HttpStatusCode? statusCode, TimeSpan? retryAfter, string diagnostic)
            => new()
            {
                Outcome = YoutubeWebSubRequestOutcome.TransientFailure,
                StatusCode = statusCode,
                RetryAfter = retryAfter,
                DiagnosticSummary = diagnostic
            };

        public static YoutubeWebSubRequestResult Permanent(HttpStatusCode statusCode, string diagnostic)
            => new()
            {
                Outcome = YoutubeWebSubRequestOutcome.PermanentFailure,
                StatusCode = statusCode,
                DiagnosticSummary = diagnostic
            };

        /// <summary>Hub 已拒絕且未經 owner 強制重訂：不送出要求，也不算失敗。</summary>
        public static YoutubeWebSubRequestResult Suppressed()
            => new() { Outcome = YoutubeWebSubRequestOutcome.Suppressed };
    }

    /// <summary>
    /// YouTube WebSub（PubSubHubbub 0.4）與 Atom fallback 的共用契約：canonical topic、callback URL、
    /// pending action 格式、callback token 衍生與請求結果分類。純函式，不含 HTTP／Redis／DB。
    /// <para>Backend 有另一份等價實作；兩邊以固定的測試向量鎖住衍生結果與 JSON 欄位。</para>
    /// </summary>
    public static class YoutubeWebSubContract
    {
        /// <summary>Hub 表單實際使用的訂閱 endpoint（root URL 是 publisher discovery／publish 用）。</summary>
        public const string HubSubscribeEndpoint = "https://pubsubhubbub.appspot.com/subscribe";

        /// <summary>Backend 的 challenge／通知 callback 路徑。</summary>
        public const string CallbackPath = "NotificationCallback";

        /// <summary>canonical topic 前綴；與官方文件的 <c>https://www.youtube.com/feeds/videos.xml?channel_id=</c> 相同。</summary>
        public const string TopicPrefix = "https://www.youtube.com/feeds/videos.xml?channel_id=";

        /// <summary>目前既有的 10 天 requested lease（864000 秒）。Hub 表單的 <c>hub.lease_numbers</c> 是筆誤，規格欄位固定為 <c>hub.lease_seconds</c>。</summary>
        public const int RequestedLeaseSeconds = 864000;

        public const string ModeSubscribe = "subscribe";
        public const string ModeUnsubscribe = "unsubscribe";

        /// <summary>callback token 衍生的固定用途字串；不隨頻道改變，Backend 才能只憑 HMAC secret 重算。</summary>
        internal const string CallbackTokenPurpose = "discord-stream-bot:youtube-websub-callback:v1";

        /// <summary>HMAC secret 的隨機位元組數；base64url 後為 43 字元，遠低於規格的 200 bytes 上限。</summary>
        internal const int SecretByteLength = 32;

        /// <summary>pending action 的 TTL，等於目前請求的 10 天 lease。</summary>
        public static readonly TimeSpan PendingTtl = TimeSpan.FromDays(10);

        /// <summary>既有政策：訂閱約 7 天後續訂。</summary>
        public static readonly TimeSpan RenewAfter = TimeSpan.FromDays(7);

        /// <summary>secret TTL 低於此值時視為需要提早續訂（對應 10 天 lease − 7 天續訂的緩衝）。</summary>
        public static readonly TimeSpan SecretRenewThreshold = TimeSpan.FromDays(3);

        public static string CanonicalTopic(string channelId)
        {
            if (!IsValidChannelId(channelId))
                throw new ArgumentException($"不是有效的 YouTube channel ID：{channelId}", nameof(channelId));

            return TopicPrefix + Uri.EscapeDataString(channelId);
        }

        /// <summary>每個頻道固定的 callback URL：帶 channelId 與不可猜測的 token，續訂時覆寫同一組 (topic, callback)。</summary>
        public static string CallbackUrl(string apiServerDomain, string channelId, string callbackToken)
        {
            if (string.IsNullOrWhiteSpace(apiServerDomain))
                throw new ArgumentException("ApiServerDomain 不得為空", nameof(apiServerDomain));
            if (!IsValidChannelId(channelId))
                throw new ArgumentException($"不是有效的 YouTube channel ID：{channelId}", nameof(channelId));
            if (string.IsNullOrWhiteSpace(callbackToken))
                throw new ArgumentException("callback token 不得為空", nameof(callbackToken));

            string host = apiServerDomain.Trim().TrimEnd('/');
            return $"https://{host}/{CallbackPath}?channelId={Uri.EscapeDataString(channelId)}&token={Uri.EscapeDataString(callbackToken)}";
        }

        public static bool IsValidChannelId(string channelId)
            => !string.IsNullOrEmpty(channelId)
               && channelId.Length == 24
               && channelId.StartsWith("UC", StringComparison.Ordinal)
               && channelId.All(static c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

        /// <summary>建立新的每頻道 HMAC secret（安全隨機、base64url、無 padding）。</summary>
        public static string CreateSecret()
            => ToBase64Url(RandomNumberGenerator.GetBytes(SecretByteLength));

        /// <summary>
        /// 由 HMAC secret 以 HMAC-SHA256 與固定用途字串衍生 callback token。
        /// 不直接把 secret 放進 URL；Backend 以相同輸入重算並用 constant-time 比較。
        /// </summary>
        public static string DeriveCallbackToken(string hmacSecret)
        {
            if (string.IsNullOrEmpty(hmacSecret))
                throw new ArgumentException("HMAC secret 不得為空", nameof(hmacSecret));

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(hmacSecret));
            return ToBase64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(CallbackTokenPurpose)));
        }

        /// <summary>組出 Hub 訂閱表單；subscribe 才帶 <c>hub.secret</c>，且不送 <c>hub.verify</c>／<c>hub.verify_token</c>。</summary>
        public static IReadOnlyList<KeyValuePair<string, string>> BuildRequestForm(
            string mode, string topic, string callbackUrl, string hmacSecret)
        {
            if (mode is not (ModeSubscribe or ModeUnsubscribe))
                throw new ArgumentException($"未知的 hub.mode：{mode}", nameof(mode));

            var fields = new List<KeyValuePair<string, string>>(5)
            {
                new("hub.mode", mode),
                new("hub.topic", topic),
                new("hub.callback", callbackUrl),
                new("hub.lease_seconds", RequestedLeaseSeconds.ToString(CultureInfo.InvariantCulture)),
            };

            if (mode == ModeSubscribe)
            {
                if (string.IsNullOrEmpty(hmacSecret))
                    throw new ArgumentException("subscribe 必須帶 HMAC secret", nameof(hmacSecret));
                fields.Add(new KeyValuePair<string, string>("hub.secret", hmacSecret));
            }

            return fields;
        }

        /// <summary>依 HTTP 狀態碼分類；429、408、5xx 為暫時性失敗，其餘非 2xx 為永久性失敗。</summary>
        public static YoutubeWebSubRequestOutcome ClassifyStatus(HttpStatusCode statusCode)
        {
            int code = (int)statusCode;
            if (code is >= 200 and < 300)
                return YoutubeWebSubRequestOutcome.Accepted;
            if (code == 408 || code == 429 || code >= 500)
                return YoutubeWebSubRequestOutcome.TransientFailure;

            return YoutubeWebSubRequestOutcome.PermanentFailure;
        }

        /// <summary>
        /// 將 response body 收斂成有長度上限、去除控制字元、並移除指定敏感值的診斷摘要，避免污染 log。
        /// Hub 或代理的錯誤內容可能反射回我們送出的表單，因此 secret／token 必須先移除再截短。
        /// </summary>
        internal static string Summarize(string body, int maxLength = 200, params string[] sensitiveValues)
        {
            if (string.IsNullOrEmpty(body))
                return "";

            foreach (string sensitive in sensitiveValues)
            {
                if (string.IsNullOrEmpty(sensitive))
                    continue;

                body = body.Replace(sensitive, "[redacted]", StringComparison.Ordinal);
                string escaped = Uri.EscapeDataString(sensitive);
                if (escaped != sensitive)
                    body = body.Replace(escaped, "[redacted]", StringComparison.Ordinal);
            }

            var builder = new StringBuilder(Math.Min(body.Length, maxLength));
            foreach (char c in body)
            {
                if (builder.Length >= maxLength)
                    break;
                builder.Append(char.IsControl(c) ? ' ' : c);
            }

            return builder.ToString();
        }

        /// <summary>URL-safe base64（無 padding），可直接放進 query string。</summary>
        internal static string ToBase64Url(byte[] bytes)
            => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>Redis DB 1 的 WebSub pending action（計畫 §7.1）；Bot 與 Backend 必須使用相同 key 與 JSON 欄位。</summary>
    public sealed class YoutubeWebSubPendingAction
    {
        public const int CurrentVersion = 1;

        [JsonProperty("version")]
        public int Version { get; set; } = CurrentVersion;

        [JsonProperty("channelId")]
        public string ChannelId { get; set; }

        /// <summary><see cref="YoutubeWebSubContract.ModeSubscribe"/> 或 <see cref="YoutubeWebSubContract.ModeUnsubscribe"/>。</summary>
        [JsonProperty("mode")]
        public string Mode { get; set; }

        [JsonProperty("topic")]
        public string Topic { get; set; }

        [JsonProperty("callbackToken")]
        public string CallbackToken { get; set; }

        [JsonProperty("requestedAtUtc")]
        public DateTime RequestedAtUtc { get; set; }

        [JsonProperty("confirmedAtUtc")]
        public DateTime? ConfirmedAtUtc { get; set; }

        [JsonProperty("deniedAtUtc")]
        public DateTime? DeniedAtUtc { get; set; }

        public static YoutubeWebSubPendingAction Create(string channelId, string mode, string callbackToken, DateTime requestedAtUtc)
            => new()
            {
                ChannelId = channelId,
                Mode = mode,
                Topic = YoutubeWebSubContract.CanonicalTopic(channelId),
                CallbackToken = callbackToken,
                RequestedAtUtc = requestedAtUtc,
            };

        /// <summary>
        /// 嚴格解析 Redis 內既有資料；格式不符時視為沒有 pending action（而不是沿用半殘狀態）。
        /// </summary>
        public static bool TryParse(string json, out YoutubeWebSubPendingAction action, out string error)
        {
            action = null;
            error = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                error = "payload 為空";
                return false;
            }

            YoutubeWebSubPendingAction parsed;
            try
            {
                parsed = JsonConvert.DeserializeObject<YoutubeWebSubPendingAction>(json);
            }
            catch (JsonException ex)
            {
                error = $"JSON 解析失敗：{ex.Message}";
                return false;
            }

            if (parsed == null)
            {
                error = "JSON 解析結果為 null";
                return false;
            }

            if (parsed.Version != CurrentVersion)
                error = $"version 不支援：{parsed.Version}";
            else if (!YoutubeWebSubContract.IsValidChannelId(parsed.ChannelId))
                error = "channelId 格式不正確";
            else if (parsed.Mode is not (YoutubeWebSubContract.ModeSubscribe or YoutubeWebSubContract.ModeUnsubscribe))
                error = $"mode 不支援：{parsed.Mode}";
            else if (parsed.Topic != YoutubeWebSubContract.CanonicalTopic(parsed.ChannelId))
                error = "topic 與 channelId 不符";
            else if (string.IsNullOrWhiteSpace(parsed.CallbackToken))
                error = "callbackToken 為空";
            else if (parsed.RequestedAtUtc == default)
                error = "requestedAtUtc 為空";

            if (error != null)
                return false;

            action = parsed;
            return true;
        }
    }

    /// <summary>pending action 的處置方式。</summary>
    public enum YoutubeWebSubPendingDecision
    {
        /// <summary>沿用既有 pending（未確認的重試，或第二次訂閱要求）。</summary>
        Reuse,

        /// <summary>建立新的 pending 並覆寫。</summary>
        Replace,

        /// <summary>Hub 已拒絕且未經 owner 強制重訂，不送出要求。</summary>
        Blocked,
    }

    /// <summary>pending action 的重用／替換決策（計畫 §7.1）。</summary>
    public static class YoutubeWebSubPendingPolicy
    {
        /// <param name="existing">Redis 內既有 pending；沒有則為 null。</param>
        /// <param name="mode">本次要求的 hub.mode。</param>
        /// <param name="topic">本次要求的 canonical topic。</param>
        /// <param name="callbackToken">本次要求要放進 callback URL 的 token。</param>
        /// <param name="renewDue">既有 pending 已確認且已到續訂時間。</param>
        /// <param name="force">owner 強制重新訂閱：可清除 denied 狀態。</param>
        public static YoutubeWebSubPendingDecision Decide(
            YoutubeWebSubPendingAction existing,
            string mode,
            string topic,
            string callbackToken,
            bool renewDue,
            bool force = false)
        {
            if (existing == null)
                return YoutubeWebSubPendingDecision.Replace;

            // denied 只有 owner 強制重新訂閱能解除；一般週期不得持續重送相同被拒絕的要求。
            if (existing.DeniedAtUtc != null)
                return force ? YoutubeWebSubPendingDecision.Replace : YoutubeWebSubPendingDecision.Blocked;

            // mode／topic 不同，或 HMAC secret 已遺失／輪替（token 因此改變）都必須換掉 pending。
            if (existing.Mode != mode || existing.Topic != topic || existing.CallbackToken != callbackToken)
                return YoutubeWebSubPendingDecision.Replace;

            if (existing.ConfirmedAtUtc != null && renewDue)
                return YoutubeWebSubPendingDecision.Replace;

            return YoutubeWebSubPendingDecision.Reuse;
        }
    }
}
