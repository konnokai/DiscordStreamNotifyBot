using System.Net;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService.Youtube;
using Bot = DiscordStreamNotifyBot.Shared.BotState;

namespace DiscordStreamNotifyBot.Scraper.Detection.Youtube
{
    /// <summary>
    /// Atom feed 補償輪詢（計畫 §6.4、§9）：對全部 <c>YoutubeChannelSpider</c> 低頻抓取 canonical Atom feed，
    /// 只把未知 video ID 交給既有 claim／<c>videos.list</c>／<c>AddOtherDataAsync</c> 流程。
    /// <para>
    /// 不是第二套偵測邏輯：HTTP、validator 與批次處理都以委派注入，本身只負責條件式 GET、逾時／429 退避與
    /// 「整個頻道處理成功後才更新 ETag／Last-Modified」。任何錯誤都不刪 crawler、不清除影片狀態。
    /// </para>
    /// </summary>
    internal sealed class YoutubeAtomFallback
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IYoutubeAtomValidatorStore _validators;
        private readonly Func<CancellationToken, Task<IReadOnlyList<string>>> _listChannelIds;
        private readonly Func<IReadOnlyList<string>, CancellationToken, Task<IReadOnlyCollection<string>>> _processUnknownVideos;
        private long _notBeforeUtcTicks;

        internal YoutubeAtomFallback(
            IHttpClientFactory httpClientFactory,
            IYoutubeAtomValidatorStore validators,
            Func<CancellationToken, Task<IReadOnlyList<string>>> listChannelIds,
            Func<IReadOnlyList<string>, CancellationToken, Task<IReadOnlyCollection<string>>> processUnknownVideos)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _validators = validators ?? throw new ArgumentNullException(nameof(validators));
            _listChannelIds = listChannelIds ?? throw new ArgumentNullException(nameof(listChannelIds));
            _processUnknownVideos = processUnknownVideos ?? throw new ArgumentNullException(nameof(processUnknownVideos));
        }

        /// <summary>執行一輪補償；同一時間只允許一輪（由 <see cref="PeriodicRunner"/> 保證不重入）。</summary>
        internal async Task RunAsync(CancellationToken cancellationToken)
        {
            if (DateTimeOffset.UtcNow.UtcTicks < Volatile.Read(ref _notBeforeUtcTicks))
            {
                Log.Warn("Atom fallback 仍在 YouTube 要求的退避期間，略過本輪");
                return;
            }

            IReadOnlyList<string> channelIds = await _listChannelIds(cancellationToken).ConfigureAwait(false);
            if (channelIds.Count == 0)
                return;

            using HttpClient httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");

            var modified = new List<ChannelFeed>();
            var candidates = new List<string>();
            var seenVideoIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (string channelId in channelIds)
            {
                if (cancellationToken.IsCancellationRequested || Bot.IsDisconnect)
                    break;

                ChannelFetch fetch;
                try
                {
                    fetch = await FetchAsync(httpClient, channelId, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // 單一頻道的連線／內容失敗不得中止其他頻道；本輪只記錄，下一輪（5 分鐘後）自然重試。
                    Log.Error(ex.Demystify(), $"Atom fallback 取得 feed 失敗：{channelId}");
                    continue;
                }

                if (fetch.Kind == ChannelFetchKind.NotModified)
                    continue;

                if (fetch.Kind == ChannelFetchKind.Failed)
                {
                    // 只有 Hub 明確的限流（429／Retry-After）才視為全域性限制並停止本輪。
                    if (fetch.StopRound)
                        break;

                    continue;
                }

                if (fetch.SkippedEntryCount > 0)
                    Log.Warn($"Atom fallback 略過 {fetch.SkippedEntryCount} 筆無效 entry：{channelId}");

                modified.Add(new ChannelFeed(channelId, fetch.ETag, fetch.LastModified, fetch.VideoIds));
                foreach (string videoId in fetch.VideoIds)
                {
                    if (seenVideoIds.Add(videoId))
                        candidates.Add(videoId);
                }
            }

            if (modified.Count == 0)
                return;

            IReadOnlyCollection<string> failed;
            try
            {
                failed = candidates.Count == 0
                    ? []
                    : await _processUnknownVideos(candidates, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "Atom fallback 處理未知影片失敗，保留既有 validator");
                return;
            }

            var failedSet = failed as IReadOnlySet<string> ?? failed.ToHashSet(StringComparer.Ordinal);
            int updated = 0;
            foreach (ChannelFeed feed in modified)
            {
                if (feed.VideoIds.Any(failedSet.Contains))
                    continue;

                try
                {
                    await _validators.SetAsync(feed.ChannelId, feed.ETag, feed.LastModified, cancellationToken).ConfigureAwait(false);
                    updated++;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), $"Atom fallback 更新 validator 失敗：{feed.ChannelId}");
                }
            }

            Log.Info($"Atom fallback 完成：{modified.Count} 個 feed 有更新、{candidates.Count} 個未知 video ID、{updated} 個 validator 已更新");
        }

        private async Task<ChannelFetch> FetchAsync(HttpClient httpClient, string channelId, CancellationToken cancellationToken)
        {
            (string etag, string lastModified) = await _validators.GetAsync(channelId, cancellationToken).ConfigureAwait(false);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                YoutubeWebSubContract.TopicPrefix + Uri.EscapeDataString(channelId));
            if (!string.IsNullOrEmpty(etag))
                request.Headers.TryAddWithoutValidation("If-None-Match", etag);
            if (!string.IsNullOrEmpty(lastModified))
                request.Headers.TryAddWithoutValidation("If-Modified-Since", lastModified);

            // 預設 completion mode：feed 本來就要完整讀成 bytes，讓 HttpClient.Timeout 涵蓋 body 讀取，
            // 避免 headers 已到、body 卡住時佔住不重入的 runner。
            using HttpResponseMessage response = await httpClient
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotModified)
                return ChannelFetch.NotModified();

            if (!response.IsSuccessStatusCode)
            {
                int statusCode = (int)response.StatusCode;
                TimeSpan? retryAfter = ParseRetryAfter(response.Headers.RetryAfter);
                bool stopRound = statusCode == 429 || retryAfter.HasValue;

                if (retryAfter.HasValue)
                {
                    Interlocked.Exchange(ref _notBeforeUtcTicks, DateTimeOffset.UtcNow.Add(retryAfter.Value).UtcTicks);
                    Log.Warn($"Atom fallback 收到 Retry-After：{channelId} / HTTP {statusCode} / {retryAfter.Value.TotalSeconds:0} 秒");
                }
                else
                {
                    Log.Warn($"Atom fallback feed HTTP {statusCode}：{channelId}");
                }

                return ChannelFetch.Failed(stopRound: stopRound);
            }

            byte[] content = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            YoutubeAtomParseResult parsed = YoutubeAtomFeedParser.Parse(content);
            if (!parsed.Success)
            {
                Log.Warn($"Atom fallback feed 解析失敗：{channelId} / {parsed.Error}");
                return ChannelFetch.Failed();
            }

            if (!IsFeedForChannel(parsed, channelId))
            {
                Log.Warn($"Atom fallback feed channel 不符，整份略過：{channelId} / {parsed.FeedChannelId ?? "(無 self link)"}");
                return ChannelFetch.Failed();
            }

            IReadOnlyList<string> videoIds = parsed.Entries.Select(static x => x.VideoId).ToArray();
            string newEtag = response.Headers.ETag?.Tag;
            string newLastModified = response.Content.Headers.LastModified?.ToString("R");

            return ChannelFetch.Modified(videoIds, newEtag, newLastModified, parsed.SkippedEntryCount);
        }

        /// <summary>feed self link 與**去重前**每個 entry 的 channel ID 都必須與請求的頻道相同。</summary>
        private static bool IsFeedForChannel(YoutubeAtomParseResult parsed, string channelId)
        {
            if (!string.IsNullOrEmpty(parsed.FeedChannelId) && parsed.FeedChannelId != channelId)
                return false;

            if (string.IsNullOrEmpty(parsed.FeedChannelId) && parsed.ChannelIds.Count == 0)
                return false;

            foreach (string declaredChannelId in parsed.ChannelIds)
            {
                if (declaredChannelId != channelId)
                    return false;
            }

            return true;
        }

        private static TimeSpan? ParseRetryAfter(System.Net.Http.Headers.RetryConditionHeaderValue retryAfter)
        {
            if (retryAfter == null)
                return null;

            if (retryAfter.Delta.HasValue)
                return retryAfter.Delta.Value > TimeSpan.Zero ? retryAfter.Delta.Value : null;

            if (retryAfter.Date.HasValue)
            {
                TimeSpan delta = retryAfter.Date.Value - DateTimeOffset.UtcNow;
                return delta > TimeSpan.Zero ? delta : null;
            }

            return null;
        }

        private readonly record struct ChannelFeed(
            string ChannelId,
            string ETag,
            string LastModified,
            IReadOnlyList<string> VideoIds);

        private enum ChannelFetchKind { Modified, NotModified, Failed }

        private readonly record struct ChannelFetch(
            ChannelFetchKind Kind,
            IReadOnlyList<string> VideoIds,
            string ETag,
            string LastModified,
            int SkippedEntryCount,
            bool StopRound)
        {
            internal static ChannelFetch Modified(
                IReadOnlyList<string> videoIds, string etag, string lastModified, int skippedEntryCount)
                => new(ChannelFetchKind.Modified, videoIds, etag, lastModified, skippedEntryCount, false);

            internal static ChannelFetch NotModified()
                => new(ChannelFetchKind.NotModified, [], null, null, 0, false);

            internal static ChannelFetch Failed(bool stopRound = false)
                => new(ChannelFetchKind.Failed, [], null, null, 0, stopRound);
        }
    }
}
