using System.Net;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService.Youtube;
using Bot = DiscordStreamNotifyBot.Shared.BotState;

namespace DiscordStreamNotifyBot.Scraper.Detection.Youtube
{
    /// <summary>
    /// Atom fallback 單輪的影片處理結果：失敗的 video ID（呼叫端保留舊 validator、下輪重試）
    /// 與實際查詢規模（供 log 顯示真正花掉 API 查詢的數量，而不是 feed 列出的全部 ID）。
    /// </summary>
    internal readonly record struct YoutubeAtomProcessResult(
        IReadOnlyCollection<string> FailedVideoIds,
        int QueryCandidateCount,
        int BatchCount);

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
        private readonly Func<IReadOnlyList<string>, CancellationToken, Task<YoutubeAtomProcessResult>> _processUnknownVideos;
        private long _notBeforeUtcTicks;

        internal YoutubeAtomFallback(
            IHttpClientFactory httpClientFactory,
            IYoutubeAtomValidatorStore validators,
            Func<CancellationToken, Task<IReadOnlyList<string>>> listChannelIds,
            Func<IReadOnlyList<string>, CancellationToken, Task<YoutubeAtomProcessResult>> processUnknownVideos)
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
            int notModified = 0, fetchFailed = 0, foreignEntries = 0;

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
                    // 單一頻道的連線／內容失敗不得中止其他頻道；本輪只記錄，下一輪自然重試。
                    Log.Error(ex.Demystify(), $"Atom fallback 取得 feed 失敗：{channelId}");
                    fetchFailed++;
                    continue;
                }

                if (fetch.Kind == ChannelFetchKind.NotModified)
                {
                    notModified++;
                    continue;
                }

                if (fetch.Kind == ChannelFetchKind.Failed)
                {
                    // 只有 Hub 明確的限流（429／Retry-After）才視為全域性限制並停止本輪。
                    if (fetch.StopRound)
                        break;

                    fetchFailed++;
                    continue;
                }

                if (fetch.SkippedEntryCount > 0)
                    Log.Warn($"Atom fallback 略過 {fetch.SkippedEntryCount} 筆無效 entry：{channelId}");

                modified.Add(new ChannelFeed(channelId, fetch.ETag, fetch.LastModified, fetch.VideoIds));
                foreignEntries += fetch.ForeignEntryCount;
                foreach (string videoId in fetch.VideoIds)
                {
                    if (seenVideoIds.Add(videoId))
                        candidates.Add(videoId);
                }
            }

            if (modified.Count == 0)
            {
                Log.Info($"Atom fallback 完成：候選 {channelIds.Count} 個頻道，無可用 feed（200 0／304 {notModified}／失敗 {fetchFailed}）");
                return;
            }

            YoutubeAtomProcessResult processResult;
            try
            {
                processResult = candidates.Count == 0
                    ? new YoutubeAtomProcessResult([], 0, 0)
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

            var failedSet = processResult.FailedVideoIds as IReadOnlySet<string>
                ?? processResult.FailedVideoIds.ToHashSet(StringComparer.Ordinal);
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

            Log.Info($"Atom fallback 完成：候選 {channelIds.Count} 個頻道（200 {modified.Count}／304 {notModified}／失敗 {fetchFailed}）、"
                + $"feed entry {candidates.Count} 筆（略過其他頻道 {foreignEntries} 筆）、"
                + $"claim 後查詢 {processResult.QueryCandidateCount} 筆／{processResult.BatchCount} 批次、"
                + $"validator 更新 {updated}、保留 {modified.Count - updated}");
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

            // YouTube 的頻道 feed 會夾帶其他頻道的合作／翻唱影片（實測：15 筆中有 2 筆屬於另一個頻道），
            // 逐筆過濾即可；整份丟棄會讓該頻道的 validator 永遠無法前進。
            YoutubeAtomEntry[] ownEntries = parsed.Entries.Where((x) => x.ChannelId == channelId).ToArray();
            if (ownEntries.Length != parsed.Entries.Count)
            {
                IEnumerable<string> foreignChannels = parsed.Entries
                    .Where((x) => x.ChannelId != channelId)
                    .Select(static x => x.ChannelId)
                    .Distinct();
                Log.Warn($"Atom fallback 略過 {parsed.Entries.Count - ownEntries.Length} 筆其他頻道的 entry：{channelId} / {string.Join(", ", foreignChannels)}");
            }

            IReadOnlyList<string> videoIds = ownEntries.Select(static x => x.VideoId).ToArray();
            string newEtag = response.Headers.ETag?.Tag;
            string newLastModified = response.Content.Headers.LastModified?.ToString("R");

            return ChannelFetch.Modified(
                videoIds, newEtag, newLastModified, parsed.SkippedEntryCount, parsed.Entries.Count - ownEntries.Length);
        }

        /// <summary>
        /// feed 本身（根節點 <c>yt:channelId</c> 或 self link）必須是請求的頻道；
        /// entry 的 channel 不在此判斷，由呼叫端逐筆過濾。
        /// </summary>
        private static bool IsFeedForChannel(YoutubeAtomParseResult parsed, string channelId)
            => parsed.FeedChannelId == channelId;

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
            int ForeignEntryCount,
            bool StopRound)
        {
            internal static ChannelFetch Modified(
                IReadOnlyList<string> videoIds, string etag, string lastModified, int skippedEntryCount, int foreignEntryCount)
                => new(ChannelFetchKind.Modified, videoIds, etag, lastModified, skippedEntryCount, foreignEntryCount, false);

            internal static ChannelFetch NotModified()
                => new(ChannelFetchKind.NotModified, [], null, null, 0, 0, false);

            internal static ChannelFetch Failed(bool stopRound = false)
                => new(ChannelFetchKind.Failed, [], null, null, 0, 0, stopRound);
        }
    }
}
