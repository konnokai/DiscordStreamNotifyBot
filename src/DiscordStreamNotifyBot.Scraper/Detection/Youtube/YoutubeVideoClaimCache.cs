using System.Collections.Concurrent;

namespace DiscordStreamNotifyBot.Scraper.Detection.Youtube
{
    /// <summary>
    /// YouTube 排程影片 ID 的程序內 claim 快取。同一 ID 在 absolute TTL 內只允許一個來源繼續執行後續檢查。
    /// </summary>
    internal sealed class YoutubeVideoClaimCache
    {
        private readonly ConcurrentDictionary<string, DateTimeOffset> _claims = new(StringComparer.Ordinal);
        private readonly TimeProvider _timeProvider;
        private readonly TimeSpan _ttl;

        internal YoutubeVideoClaimCache(TimeProvider timeProvider, TimeSpan ttl)
        {
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            if (ttl <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(ttl));
            _ttl = ttl;
        }

        internal int Count => _claims.Count;

        /// <summary>
        /// 嘗試取得影片 ID 的 claim。未過期時不延長期限；不存在或已到期時只有一個併發呼叫會成功。
        /// </summary>
        internal bool TryClaim(string videoId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(videoId);

            DateTimeOffset now = _timeProvider.GetUtcNow();
            DateTimeOffset expiresAt = now.Add(_ttl);
            while (true)
            {
                if (!_claims.TryGetValue(videoId, out DateTimeOffset currentExpiry))
                {
                    if (_claims.TryAdd(videoId, expiresAt))
                        return true;
                    continue;
                }

                if (currentExpiry > now)
                    return false;

                if (_claims.TryUpdate(videoId, expiresAt, currentExpiry))
                    return true;
            }
        }

        /// <summary>後續兜底檢查拋出例外時釋放 claim，讓下一輪可以重試。</summary>
        internal void Release(string videoId)
            => _claims.TryRemove(videoId, out _);

        /// <summary>移除已到期且未被其他執行緒更新的 claim。</summary>
        internal int RemoveExpired()
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            int removed = 0;
            foreach (KeyValuePair<string, DateTimeOffset> claim in _claims)
            {
                if (claim.Value <= now && _claims.TryRemove(claim))
                    removed++;
            }
            return removed;
        }
    }
}
