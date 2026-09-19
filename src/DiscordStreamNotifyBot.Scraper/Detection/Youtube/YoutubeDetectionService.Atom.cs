using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService.Youtube;
using Bot = DiscordStreamNotifyBot.Shared.BotState;
using YTApiVideo = Google.Apis.YouTube.v3.Data.Video;

namespace DiscordStreamNotifyBot.Scraper.Detection.Youtube
{
    /// <summary>
    /// YouTube 偵測服務的 Atom fallback 部分（計畫 §9）：條件式抓取 canonical Atom feed，把未知 video ID
    /// 交給既有 claim／<c>videos.list</c>／<c>AddOtherDataAsync</c> 流程，不建立第二套分類或通知邏輯。
    /// </summary>
    public partial class YoutubeDetectionService
    {
        /// <summary>單次 <c>videos.list</c> 批次大小；單輪可處理多個批次。</summary>
        private const int AtomVideoBatchSize = 50;

        private readonly YoutubeWebSubService _webSubService;
        private readonly YoutubeAtomFallback _atomFallback;

        /// <summary>Atom fallback 一輪的實際內容抓取；只在 Scraper leader 內由 <see cref="PeriodicRunner"/> 呼叫且不重入。</summary>
        private Task AtomFallbackAsync() => _atomFallback.RunAsync(GracefulShutdown.Token);

        /// <summary>
        /// Atom fallback 的候選頻道：WebSub 訂閱沒有在續訂週期內被 challenge 確認過的頻道
        /// （`LastSubscribeTime` 過舊或從未成功，後者為 <see cref="DateTime.MinValue"/>），同一 channel 只抓一次。
        /// 已確認的頻道不輪詢；否則會對全體頻道做無條件下載，而 feed 端點不支援條件式 GET。
        /// </summary>
        private async Task<IReadOnlyList<string>> ListAtomChannelIdsAsync(CancellationToken cancellationToken)
        {
            DateTime renewBefore = DateTime.Now.Subtract(YoutubeWebSubContract.RenewAfter);
            using var db = _dbService.GetDbContext();
            return await db.YoutubeChannelSpider
                .AsNoTracking()
                .Where((x) => x.LastSubscribeTime < renewBefore)
                .Select(static x => x.ChannelId)
                .Distinct()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// 先以既有 claim 過濾，再每 50 筆呼叫 <c>videos.list</c>，逐筆交給 <see cref="AddOtherDataAsync"/>。
        /// 回傳未能完成處理的 video ID，讓呼叫端保留舊 validator 並在下輪重試。
        /// <para>
        /// 「其他排程正在處理」不算完成：結果尚未確定時必須保留舊 validator，否則該影片可能永遠被跳過。
        /// </para>
        /// </summary>
        private async Task<IReadOnlyCollection<string>> ProcessAtomVideoIdsAsync(
            IReadOnlyList<string> videoIds, CancellationToken cancellationToken)
        {
            using var claims = _newStreamClaims.CreateBatch();

            var claimed = new List<string>(videoIds.Count);
            var failed = new List<string>();

            foreach (string videoId in videoIds)
            {
                switch (ClaimUnknownVideo(videoId, claims))
                {
                    case UnknownVideoClaim.Claimed:
                        claimed.Add(videoId);
                        break;
                    case UnknownVideoClaim.Busy:
                        failed.Add(videoId);
                        break;
                }
            }

            for (int offset = 0; offset < claimed.Count; offset += AtomVideoBatchSize)
            {
                if (cancellationToken.IsCancellationRequested || Bot.IsDisconnect)
                {
                    failed.AddRange(claimed.Skip(offset));
                    break;
                }

                List<string> batch = claimed.GetRange(offset, Math.Min(AtomVideoBatchSize, claimed.Count - offset));
                IEnumerable<YTApiVideo> videos;
                try
                {
                    videos = await GetVideosAsync(batch, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    failed.AddRange(claimed.Skip(offset));
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), $"Atom fallback videos.list 失敗（{batch.Count} 筆）");
                    failed.AddRange(batch);
                    continue;
                }

                var processed = new HashSet<string>(StringComparer.Ordinal);
                foreach (YTApiVideo video in videos ?? [])
                {
                    if (cancellationToken.IsCancellationRequested || Bot.IsDisconnect)
                        break;

                    try
                    {
                        // 與 WebSub 共用同一個處置入口（已認可頻道走分類、其餘走非認可流程）；
                        // 回傳 false 不得算完成，否則 validator 會被提前更新、通知也不會再重試。
                        bool completed = await ProcessDiscoveredVideoAsync(
                            video.Id,
                            video.Snippet.ChannelId,
                            video.Snippet.Title,
                            ParseApiTime(video.Snippet.PublishedAtRaw) ?? DateTime.Now,
                            video).ConfigureAwait(false);

                        if (!completed)
                        {
                            Log.Warn($"Atom fallback 影片未完成處理，保留舊 validator：{video.Id}");
                            continue;
                        }

                        claims.Complete(video.Id);
                        processed.Add(video.Id);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Demystify(), $"Atom fallback 處理影片失敗：{video.Id}");
                    }
                }

                // API 沒回傳的 ID 不建立假資料；claim 由 batch 釋放，下一輪可重試。
                foreach (string videoId in batch)
                {
                    if (!processed.Contains(videoId))
                        failed.Add(videoId);
                }
            }

            return failed;
        }
    }
}
