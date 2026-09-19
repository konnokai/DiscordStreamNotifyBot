using DiscordStreamNotifyBot.Scraper.Detection.Youtube;
using DiscordStreamNotifyBot.SharedService.Youtube;
using System.Net;
using System.Text;

namespace DiscordStreamNotifyBot.Tests
{
    /// <summary>
    /// Atom fallback 輪詢測試（計畫 §9）：304、validator 更新時機、跨頻道去重、429 退避。
    /// HTTP 以 stub handler 取代，validator store 以記憶體實作取代。
    /// </summary>
    public sealed class YoutubeAtomFallbackTests
    {
        private const string ChannelA = "UCabcdefghijklmnopqrstuv";
        private const string ChannelB = "UCzyxwvutsrqponmlkjihgfe";

        [Fact]
        public async Task NoCandidateChannelsMakesNoRequest()
        {
            var handler = new StubHandler(_ => throw new InvalidOperationException("沒有候選頻道時不該發出任何請求"));
            var validators = new InMemoryValidatorStore();
            var processed = new List<string>();

            var runner = CreateRunner(handler, validators, [], processed);
            await runner.RunAsync(CancellationToken.None);

            Assert.Empty(handler.Requests);
            Assert.Empty(processed);
        }

        [Fact]
        public async Task NotModifiedResponseSkipsProcessingAndKeepsValidators()
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotModified));
            var validators = new InMemoryValidatorStore();
            validators.Seed(ChannelA, "\"etag-a\"", "Wed, 17 Sep 2026 00:00:00 GMT");
            var processed = new List<string>();

            var runner = CreateRunner(handler, validators, [ChannelA], processed);
            await runner.RunAsync(CancellationToken.None);

            Assert.Empty(processed);
            Assert.Equal(("\"etag-a\"", "Wed, 17 Sep 2026 00:00:00 GMT"), validators.Get(ChannelA));
            Assert.Equal("\"etag-a\"", handler.Requests[0].Headers["If-None-Match"].Single());
            Assert.Equal("Wed, 17 Sep 2026 00:00:00 GMT", handler.Requests[0].Headers["If-Modified-Since"].Single());
        }

        [Fact]
        public async Task ModifiedFeedsDeduplicateVideoIdsAcrossChannels()
        {
            var handler = new StubHandler(request => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Feed(request.RequestUri!.Query.Contains(ChannelA, StringComparison.Ordinal) ? ChannelA : ChannelB), Encoding.UTF8, "application/atom+xml"),
                Headers = { ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"new\"") }
            });
            var validators = new InMemoryValidatorStore();
            var processed = new List<string>();

            var runner = CreateRunner(handler, validators, [ChannelA, ChannelB], processed);
            await runner.RunAsync(CancellationToken.None);

            // ChannelB 的 feed 也回傳 ChannelA 的影片，跨頻道只處理一次。
            Assert.Equal(["dQw4w9WgXcQ"], processed);
            Assert.Equal("\"new\"", validators.Get(ChannelA).ETag);
            Assert.Equal("\"new\"", validators.Get(ChannelB).ETag);
        }

        [Fact]
        public async Task FailedFeedProcessingKeepsOldValidator()
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Feed(ChannelA), Encoding.UTF8, "application/atom+xml"),
                Headers = { ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"new\"") }
            });
            var validators = new InMemoryValidatorStore();
            validators.Seed(ChannelA, "\"old\"", null);

            var runner = new YoutubeAtomFallback(
                new StubHttpClientFactory(handler),
                validators,
                _ => Task.FromResult<IReadOnlyList<string>>([ChannelA]),
                (ids, _) => Task.FromResult<IReadOnlyCollection<string>>(["dQw4w9WgXcQ"]));

            await runner.RunAsync(CancellationToken.None);

            Assert.Equal("\"old\"", validators.Get(ChannelA).ETag);
        }

        [Fact]
        public async Task ChannelMismatchIsDiscarded()
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Feed(ChannelB), Encoding.UTF8, "application/atom+xml"),
                Headers = { ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"new\"") }
            });
            var validators = new InMemoryValidatorStore();
            var processed = new List<string>();

            var runner = CreateRunner(handler, validators, [ChannelA], processed);
            await runner.RunAsync(CancellationToken.None);

            Assert.Empty(processed);
            Assert.Null(validators.Get(ChannelA).ETag);
        }

        [Fact]
        public async Task RejectsFeedWhoseRootDeclaresAnotherChannel()
        {
            // self link 與 entry 都是 ChannelA，只有 feed 根節點宣告 ChannelB。
            string feed = Feed(ChannelA).Replace(
                "<feed xmlns:yt=\"http://www.youtube.com/xml/schemas/2015\" xmlns=\"http://www.w3.org/2005/Atom\">",
                "<feed xmlns:yt=\"http://www.youtube.com/xml/schemas/2015\" xmlns=\"http://www.w3.org/2005/Atom\">"
                + $"<yt:channelId>{ChannelB}</yt:channelId>");
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(feed, Encoding.UTF8, "application/atom+xml"),
                Headers = { ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"new\"") }
            });
            var validators = new InMemoryValidatorStore();
            var processed = new List<string>();

            var runner = CreateRunner(handler, validators, [ChannelA], processed);
            await runner.RunAsync(CancellationToken.None);

            Assert.Empty(processed);
            Assert.Null(validators.Get(ChannelA).ETag);
        }

        [Fact]
        public async Task SingleChannelServerErrorDoesNotStopOtherChannels()
        {
            var handler = new StubHandler(request =>
                request.RequestUri!.Query.Contains(ChannelA, StringComparison.Ordinal)
                    ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    : new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(Feed(ChannelB), Encoding.UTF8, "application/atom+xml")
                    });
            var validators = new InMemoryValidatorStore();
            var processed = new List<string>();

            var runner = CreateRunner(handler, validators, [ChannelA, ChannelB], processed);
            await runner.RunAsync(CancellationToken.None);

            // ChannelA 的 503 只記錄診斷，ChannelB 仍要被處理。
            Assert.Equal(2, handler.Requests.Count);
            Assert.Equal(["dQw4w9WgXcQ"], processed);
            Assert.Null(validators.Get(ChannelA).ETag);
        }

        [Fact]
        public async Task SkipsEntriesOwnedByAnotherChannelButKeepsItsOwn()
        {
            // 實測形狀：頻道 feed 會夾帶其他頻道的合作／翻唱影片（15 筆中有 2 筆屬於別的頻道），
            // 這些 entry 要逐筆略過，不能因此丟棄整份 feed。
            string feed = $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <feed xmlns:yt="http://www.youtube.com/xml/schemas/2015" xmlns="http://www.w3.org/2005/Atom">
                  <link rel="self" href="https://www.youtube.com/feeds/videos.xml?channel_id={ChannelA}"/>
                  <entry>
                    <yt:videoId>dQw4w9WgXcQ</yt:videoId>
                    <yt:channelId>{ChannelA}</yt:channelId>
                    <published>2026-09-18T12:00:00+00:00</published>
                    <updated>2026-09-19T03:00:00+00:00</updated>
                  </entry>
                  <entry>
                    <yt:videoId>QlaGDL69HjY</yt:videoId>
                    <yt:channelId>{ChannelB}</yt:channelId>
                    <published>2026-09-18T12:00:00+00:00</published>
                    <updated>2026-09-19T03:00:00+00:00</updated>
                  </entry>
                </feed>
                """;
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(feed, Encoding.UTF8, "application/atom+xml"),
                Headers = { ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"new\"") }
            });
            var validators = new InMemoryValidatorStore();
            var processed = new List<string>();

            var runner = CreateRunner(handler, validators, [ChannelA], processed);
            await runner.RunAsync(CancellationToken.None);

            Assert.Equal(["dQw4w9WgXcQ"], processed);
            Assert.Equal("\"new\"", validators.Get(ChannelA).ETag);
        }

        [Fact]
        public async Task RateLimitStopsRoundAndHonoursRetryAfter()
        {
            var handler = new StubHandler(_ =>
            {
                var response = new HttpResponseMessage((HttpStatusCode)429);
                response.Headers.Add("Retry-After", "3600");
                return response;
            });
            var validators = new InMemoryValidatorStore();
            var processed = new List<string>();

            var runner = CreateRunner(handler, validators, [ChannelA, ChannelB], processed);
            await runner.RunAsync(CancellationToken.None);

            // 第一個頻道就 429：不再打第二個頻道，也不處理任何影片。
            Assert.Single(handler.Requests);
            Assert.Empty(processed);

            // 退避期間內的下一輪不得再發出任何請求。
            await runner.RunAsync(CancellationToken.None);
            Assert.Single(handler.Requests);
        }

        [Fact]
        public async Task HttpFailureDoesNotRemoveCrawlerOrValidators()
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
            var validators = new InMemoryValidatorStore();
            validators.Seed(ChannelA, "\"old\"", "Wed, 17 Sep 2026 00:00:00 GMT");
            var processed = new List<string>();

            var runner = CreateRunner(handler, validators, [ChannelA], processed);
            await runner.RunAsync(CancellationToken.None);

            Assert.Empty(processed);
            Assert.Equal("\"old\"", validators.Get(ChannelA).ETag);
        }

        private static YoutubeAtomFallback CreateRunner(
            StubHandler handler, InMemoryValidatorStore validators, IReadOnlyList<string> channelIds, List<string> processed)
            => new(
                new StubHttpClientFactory(handler),
                validators,
                _ => Task.FromResult(channelIds),
                (ids, _) =>
                {
                    processed.AddRange(ids);
                    return Task.FromResult<IReadOnlyCollection<string>>([]);
                });

        internal static string Feed(string channelId)
            => $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <feed xmlns:yt="http://www.youtube.com/xml/schemas/2015" xmlns="http://www.w3.org/2005/Atom">
                  <link rel="self" href="https://www.youtube.com/feeds/videos.xml?channel_id={channelId}"/>
                  <entry>
                    <yt:videoId>dQw4w9WgXcQ</yt:videoId>
                    <yt:channelId>{channelId}</yt:channelId>
                    <published>2026-09-18T12:00:00+00:00</published>
                    <updated>2026-09-19T03:00:00+00:00</updated>
                  </entry>
                </feed>
                """;

        internal sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
        {
            public List<RecordedRequest> Requests { get; } = [];

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // request 由呼叫端 using 釋放，所以這裡先快照需要的欄位。
                Requests.Add(new RecordedRequest(
                    request.RequestUri,
                    request.Headers.ToDictionary(x => x.Key, x => x.Value.ToArray())));
                return Task.FromResult(responder(request));
            }
        }

        internal sealed record RecordedRequest(Uri Uri, IReadOnlyDictionary<string, string[]> Headers);

        internal sealed class StubHttpClientFactory(StubHandler handler) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
        }

        internal sealed class InMemoryValidatorStore : IYoutubeAtomValidatorStore
        {
            private readonly Dictionary<string, (string ETag, string LastModified)> _values = new(StringComparer.Ordinal);

            public void Seed(string channelId, string etag, string lastModified) => _values[channelId] = (etag, lastModified);

            public (string ETag, string LastModified) Get(string channelId)
                => _values.TryGetValue(channelId, out var value) ? value : (null, null);

            public Task<(string ETag, string LastModified)> GetAsync(string channelId, CancellationToken cancellationToken = default)
                => Task.FromResult(Get(channelId));

            public Task SetAsync(string channelId, string etag, string lastModified, CancellationToken cancellationToken = default)
            {
                var current = Get(channelId);
                _values[channelId] = (etag ?? current.ETag, lastModified ?? current.LastModified);
                return Task.CompletedTask;
            }

            public Task RemoveAsync(string channelId, CancellationToken cancellationToken = default)
            {
                _values.Remove(channelId);
                return Task.CompletedTask;
            }
        }
    }
}
