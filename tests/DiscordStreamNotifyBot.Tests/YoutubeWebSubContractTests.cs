using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService.Youtube;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;

namespace DiscordStreamNotifyBot.Tests
{
    /// <summary>
    /// WebSub 契約測試：canonical topic／callback／pending action／結果分類。Backend 有另一份等價測試，
    /// 兩邊以相同測試向量鎖住衍生結果與 JSON 欄位（計畫 §12.1）。
    /// </summary>
    public sealed class YoutubeWebSubContractTests
    {
        private const string ChannelId = "UCabcdefghijklmnopqrstuv";
        private const string OtherChannelId = "UCzyxwvutsrqponmlkjihgfe";

        [Fact]
        public void CanonicalTopicUsesOfficialFeedUrlWithoutLegacyXmlPath()
        {
            string topic = YoutubeWebSubContract.CanonicalTopic(ChannelId);

            Assert.Equal("https://www.youtube.com/feeds/videos.xml?channel_id=" + ChannelId, topic);
            Assert.DoesNotContain("/xml/feeds/", topic, StringComparison.Ordinal);
        }

        [Fact]
        public void CanonicalTopicRejectsInvalidChannelId()
        {
            Assert.Throws<ArgumentException>(() => YoutubeWebSubContract.CanonicalTopic("UC123"));
            Assert.Throws<ArgumentException>(() => YoutubeWebSubContract.CanonicalTopic("XXabcdefghijklmnopqrstuv"));
            Assert.Throws<ArgumentException>(() => YoutubeWebSubContract.CanonicalTopic("UCabcdefghijklmnopqrstu/"));
        }

        [Fact]
        public void CallbackUrlCarriesChannelAndTokenWithoutSecret()
        {
            string secret = "unit-test-secret";
            string token = YoutubeWebSubContract.DeriveCallbackToken(secret);
            string url = YoutubeWebSubContract.CallbackUrl("api.example.com", ChannelId, token);

            var uri = new Uri(url);
            Assert.Equal("https", uri.Scheme);
            Assert.Equal("api.example.com", uri.Host);
            Assert.Equal("/NotificationCallback", uri.AbsolutePath);
            Assert.Contains($"channelId={ChannelId}", url, StringComparison.Ordinal);
            Assert.Contains($"token={token}", url, StringComparison.Ordinal);
            Assert.DoesNotContain(secret, url, StringComparison.Ordinal);
        }

        [Fact]
        public void CallbackUrlToleratesTrailingSlashInApiServerDomain()
        {
            Assert.Equal(
                YoutubeWebSubContract.CallbackUrl("api.example.com", ChannelId, "token"),
                YoutubeWebSubContract.CallbackUrl("api.example.com/", ChannelId, "token"));
        }

        [Fact]
        public void CallbackTokenMatchesSharedVectorAcrossRepositories()
        {
            // 與 Backend 的 DiscordStreamBotBackend.Tests.YoutubeWebSubContractTests 相同的固定向量。
            Assert.Equal("EjDM6cKhgaOC03v0EVmyG7elV6uPVNqTM29h4wyfMT0", YoutubeWebSubContract.DeriveCallbackToken("test-secret"));
        }

        [Fact]
        public void CallbackTokenRejectsEmptySecret()
            => Assert.Throws<ArgumentException>(() => YoutubeWebSubContract.DeriveCallbackToken(""));

        [Fact]
        public void SubscribeFormOmitsLegacyVerifyFieldsAndUsesLeaseSeconds()
        {
            Assert.Equal("https://pubsubhubbub.appspot.com/subscribe", YoutubeWebSubContract.HubSubscribeEndpoint);

            var fields = YoutubeWebSubContract.BuildRequestForm(
                YoutubeWebSubContract.ModeSubscribe,
                YoutubeWebSubContract.CanonicalTopic(ChannelId),
                "https://api.example.com/NotificationCallback?channelId=x&token=y",
                "secret-value");

            Assert.Equal(
                ["hub.mode", "hub.topic", "hub.callback", "hub.lease_seconds", "hub.secret"],
                fields.Select(x => x.Key));
            Assert.Equal("subscribe", fields[0].Value);
            Assert.Equal("864000", fields[3].Value);
            Assert.Equal("secret-value", fields[4].Value);
            Assert.DoesNotContain(fields, x => x.Key.Contains("verify", StringComparison.Ordinal));
            Assert.DoesNotContain(fields, x => x.Value.Contains("/xml/feeds/", StringComparison.Ordinal));
        }

        [Fact]
        public void UnsubscribeFormOmitsSecret()
        {
            var fields = YoutubeWebSubContract.BuildRequestForm(
                YoutubeWebSubContract.ModeUnsubscribe,
                YoutubeWebSubContract.CanonicalTopic(ChannelId),
                "https://api.example.com/NotificationCallback?channelId=x&token=y",
                null);

            Assert.Equal(["hub.mode", "hub.topic", "hub.callback", "hub.lease_seconds"], fields.Select(x => x.Key));
            Assert.Equal("unsubscribe", fields[0].Value);
        }

        [Fact]
        public void CreatedSecretIsShortEnoughForHubSecretLimit()
        {
            string secret = YoutubeWebSubContract.CreateSecret();

            Assert.True(secret.Length > 0);
            Assert.True(System.Text.Encoding.UTF8.GetByteCount(secret) < 200);
            Assert.All(secret, c => Assert.True(char.IsAsciiLetterOrDigit(c) || c is '-' or '_'));
            Assert.NotEqual(secret, YoutubeWebSubContract.CreateSecret());
        }

        [Theory]
        [InlineData(HttpStatusCode.OK, YoutubeWebSubRequestOutcome.Accepted)]
        [InlineData(HttpStatusCode.Accepted, YoutubeWebSubRequestOutcome.Accepted)]
        [InlineData(HttpStatusCode.NoContent, YoutubeWebSubRequestOutcome.Accepted)]
        [InlineData((HttpStatusCode)429, YoutubeWebSubRequestOutcome.TransientFailure)]
        [InlineData(HttpStatusCode.RequestTimeout, YoutubeWebSubRequestOutcome.TransientFailure)]
        [InlineData(HttpStatusCode.InternalServerError, YoutubeWebSubRequestOutcome.TransientFailure)]
        [InlineData(HttpStatusCode.ServiceUnavailable, YoutubeWebSubRequestOutcome.TransientFailure)]
        [InlineData(HttpStatusCode.BadRequest, YoutubeWebSubRequestOutcome.PermanentFailure)]
        [InlineData(HttpStatusCode.Forbidden, YoutubeWebSubRequestOutcome.PermanentFailure)]
        [InlineData(HttpStatusCode.NotFound, YoutubeWebSubRequestOutcome.PermanentFailure)]
        public void RequestResultsClassifyByStatusCode(HttpStatusCode statusCode, YoutubeWebSubRequestOutcome expected)
            => Assert.Equal(expected, YoutubeWebSubContract.ClassifyStatus(statusCode));

        [Fact]
        public void PendingActionJsonUsesAgreedFieldNames()
        {
            var action = YoutubeWebSubPendingAction.Create(
                ChannelId, YoutubeWebSubContract.ModeSubscribe, "token-value", new DateTime(2026, 9, 19, 0, 0, 0, DateTimeKind.Utc));
            string raw = JsonConvert.SerializeObject(action);
            var json = Newtonsoft.Json.Linq.JObject.Parse(raw);

            Assert.Equal(
                ["callbackToken", "channelId", "confirmedAtUtc", "deniedAtUtc", "mode", "requestedAtUtc", "topic", "version"],
                json.Properties().Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal));
            Assert.Equal(1, json.Value<int>("version"));
            Assert.Equal("subscribe", json.Value<string>("mode"));
            Assert.Equal(YoutubeWebSubContract.CanonicalTopic(ChannelId), json.Value<string>("topic"));
            Assert.Equal(JTokenType.Null, json["confirmedAtUtc"].Type);
            Assert.Equal(JTokenType.Null, json["deniedAtUtc"].Type);
            // JObject.Parse 會把 ISO 日期轉成 DateTime 後以目前文化格式化，因此直接比對原始字串。
            Assert.Contains("\"requestedAtUtc\":\"2026-09-19T00:00:00Z\"", raw, StringComparison.Ordinal);
        }

        [Fact]
        public void PendingActionRoundTripsThroughJson()
        {
            var requested = new DateTime(2026, 9, 19, 1, 2, 3, DateTimeKind.Utc);
            var action = YoutubeWebSubPendingAction.Create(ChannelId, YoutubeWebSubContract.ModeUnsubscribe, "token", requested);

            Assert.True(YoutubeWebSubPendingAction.TryParse(JsonConvert.SerializeObject(action), out var parsed, out var error));
            Assert.Null(error);
            Assert.Equal(ChannelId, parsed.ChannelId);
            Assert.Equal(YoutubeWebSubContract.ModeUnsubscribe, parsed.Mode);
            Assert.Equal(requested, parsed.RequestedAtUtc);
            Assert.Equal(DateTimeKind.Utc, parsed.RequestedAtUtc.Kind);
            Assert.Null(parsed.ConfirmedAtUtc);
        }

        [Theory]
        [InlineData("")]
        [InlineData("not json")]
        [InlineData("{}")]
        [InlineData("{\"version\":2,\"channelId\":\"UCabcdefghijklmnopqrstuv\",\"mode\":\"subscribe\",\"topic\":\"https://www.youtube.com/feeds/videos.xml?channel_id=UCabcdefghijklmnopqrstuv\",\"callbackToken\":\"t\",\"requestedAtUtc\":\"2026-09-19T00:00:00Z\"}")]
        [InlineData("{\"version\":1,\"channelId\":\"UCabcdefghijklmnopqrstuv\",\"mode\":\"other\",\"topic\":\"https://www.youtube.com/feeds/videos.xml?channel_id=UCabcdefghijklmnopqrstuv\",\"callbackToken\":\"t\",\"requestedAtUtc\":\"2026-09-19T00:00:00Z\"}")]
        [InlineData("{\"version\":1,\"channelId\":\"UCabcdefghijklmnopqrstuv\",\"mode\":\"subscribe\",\"topic\":\"https://www.youtube.com/feeds/videos.xml?channel_id=UCzyxwvutsrqponmlkjihgfe\",\"callbackToken\":\"t\",\"requestedAtUtc\":\"2026-09-19T00:00:00Z\"}")]
        [InlineData("{\"version\":1,\"channelId\":\"UCabcdefghijklmnopqrstuv\",\"mode\":\"subscribe\",\"topic\":\"https://www.youtube.com/feeds/videos.xml?channel_id=UCabcdefghijklmnopqrstuv\",\"callbackToken\":\"\",\"requestedAtUtc\":\"2026-09-19T00:00:00Z\"}")]
        [InlineData("{\"version\":1,\"channelId\":\"UCabcdefghijklmnopqrstuv\",\"mode\":\"subscribe\",\"topic\":\"https://www.youtube.com/xml/feeds/videos.xml?channel_id=UCabcdefghijklmnopqrstuv\",\"callbackToken\":\"t\",\"requestedAtUtc\":\"2026-09-19T00:00:00Z\"}")]
        public void PendingActionRejectsUnusablePayload(string json)
            => Assert.False(YoutubeWebSubPendingAction.TryParse(json, out _, out var error), $"不應接受：{json} / {error}");

        [Fact]
        public void PendingPolicyReusesUnconfirmedSameModeRequest()
        {
            var existing = YoutubeWebSubPendingAction.Create(ChannelId, YoutubeWebSubContract.ModeSubscribe, "t", DateTime.UtcNow);

            Assert.Equal(
                YoutubeWebSubPendingDecision.Reuse,
                YoutubeWebSubPendingPolicy.Decide(existing, YoutubeWebSubContract.ModeSubscribe, existing.Topic, "t", renewDue: false));
        }

        [Fact]
        public void PendingPolicyReplacesWhenModeTopicOrSecretChanges()
        {
            var existing = YoutubeWebSubPendingAction.Create(ChannelId, YoutubeWebSubContract.ModeSubscribe, "t", DateTime.UtcNow);

            Assert.Equal(YoutubeWebSubPendingDecision.Replace,
                YoutubeWebSubPendingPolicy.Decide(existing, YoutubeWebSubContract.ModeUnsubscribe, existing.Topic, "t", renewDue: false));
            Assert.Equal(YoutubeWebSubPendingDecision.Replace,
                YoutubeWebSubPendingPolicy.Decide(existing, YoutubeWebSubContract.ModeSubscribe, "https://other", "t", renewDue: false));
            Assert.Equal(YoutubeWebSubPendingDecision.Replace,
                YoutubeWebSubPendingPolicy.Decide(existing, YoutubeWebSubContract.ModeSubscribe, existing.Topic, "new-token", renewDue: false));
        }

        [Fact]
        public void PendingPolicyReplacesConfirmedRequestOnlyWhenRenewalIsDue()
        {
            var existing = YoutubeWebSubPendingAction.Create(ChannelId, YoutubeWebSubContract.ModeSubscribe, "t", DateTime.UtcNow);
            existing.ConfirmedAtUtc = DateTime.UtcNow;

            Assert.Equal(YoutubeWebSubPendingDecision.Reuse,
                YoutubeWebSubPendingPolicy.Decide(existing, YoutubeWebSubContract.ModeSubscribe, existing.Topic, "t", renewDue: false));
            Assert.Equal(YoutubeWebSubPendingDecision.Replace,
                YoutubeWebSubPendingPolicy.Decide(existing, YoutubeWebSubContract.ModeSubscribe, existing.Topic, "t", renewDue: true));
        }

        [Fact]
        public void PendingPolicyBlocksDeniedUntilForced()
        {
            var existing = YoutubeWebSubPendingAction.Create(ChannelId, YoutubeWebSubContract.ModeSubscribe, "t", DateTime.UtcNow);
            existing.DeniedAtUtc = DateTime.UtcNow;

            Assert.Equal(YoutubeWebSubPendingDecision.Blocked,
                YoutubeWebSubPendingPolicy.Decide(existing, YoutubeWebSubContract.ModeSubscribe, existing.Topic, "t", renewDue: true));
            Assert.Equal(YoutubeWebSubPendingDecision.Replace,
                YoutubeWebSubPendingPolicy.Decide(existing, YoutubeWebSubContract.ModeSubscribe, existing.Topic, "t", renewDue: true, force: true));
        }

        [Fact]
        public void RedisKeysMatchSharedContract()
        {
            Assert.Equal(1, RedisChannels.YoutubeWebSub.DatabaseNumber);
            Assert.Equal($"youtube:websub:pending:{ChannelId}", RedisChannels.YoutubeWebSub.PendingKey(ChannelId));
            Assert.Equal($"youtube.pubsub.HMACSecret:{ChannelId}", RedisChannels.YoutubeWebSub.HmacSecretKey(ChannelId));
            Assert.Equal($"youtube:websub:inflight:{ChannelId}", RedisChannels.YoutubeWebSub.InFlightKey(ChannelId));
            Assert.Equal($"youtube:atom:etag:{ChannelId}", RedisChannels.YoutubeWebSub.AtomEtagKey(ChannelId));
            Assert.Equal($"youtube:atom:last-modified:{ChannelId}", RedisChannels.YoutubeWebSub.AtomLastModifiedKey(ChannelId));
            Assert.NotEqual(OtherChannelId, RedisChannels.YoutubeWebSub.PendingKey(ChannelId));
        }

        [Fact]
        public void DiagnosticSummaryRedactsSecretsAndTokens()
        {
            const string secret = "s3cr3t-value";
            string token = YoutubeWebSubContract.DeriveCallbackToken(secret);
            string body = $"hub.secret={secret}&hub.callback=https://api.example.com/NotificationCallback?channelId={ChannelId}&token={token}";

            string summary = YoutubeWebSubContract.Summarize(body, sensitiveValues: [secret, token]);

            Assert.DoesNotContain(secret, summary, StringComparison.Ordinal);
            Assert.DoesNotContain(token, summary, StringComparison.Ordinal);
            Assert.Contains("[redacted]", summary, StringComparison.Ordinal);
        }

        [Fact]
        public void DiagnosticSummaryAlsoRedactsUrlEncodedSecrets()
        {
            const string secret = "v/+/value=";
            string body = $"hub.secret={Uri.EscapeDataString(secret)}";

            string summary = YoutubeWebSubContract.Summarize(body, sensitiveValues: [secret]);

            Assert.DoesNotContain(Uri.EscapeDataString(secret), summary, StringComparison.Ordinal);
            Assert.Contains("[redacted]", summary, StringComparison.Ordinal);
        }
    }
}
