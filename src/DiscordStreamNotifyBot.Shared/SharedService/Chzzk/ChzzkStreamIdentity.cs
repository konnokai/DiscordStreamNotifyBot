using System.Globalization;

namespace DiscordStreamNotifyBot.SharedService.Chzzk
{
    /// <summary>
    /// CHZZK 時間字串處理。網站時間字串沒有 offset；已確認（使用者指示＋實測）為韓國標準時間
    /// <c>UTC+9</c>，轉換為 UTC 後才可供 Discord timestamp 顯示。
    /// </summary>
    public static class ChzzkTime
    {
        /// <summary>已觀察到的原始格式；格式不符即視為無效，不得產生開台事件。</summary>
        internal const string RawDateTimeFormat = "yyyy-MM-dd HH:mm:ss";

        public static readonly TimeSpan KoreaStandardTimeOffset = TimeSpan.FromHours(9);

        /// <summary>解析 API 原始時間字串（KST 牆上時間）；格式無效時回傳 <see langword="false"/>。</summary>
        internal static bool TryParseKst(string raw, out DateTime kstLocal)
        {
            kstLocal = default;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            return DateTime.TryParseExact(raw.Trim(), RawDateTimeFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out kstLocal);
        }

        /// <summary>把 API 原始時間字串（KST）轉成 UTC；格式無效時回傳 <see langword="false"/>。</summary>
        public static bool TryParseKstToUtc(string raw, out DateTime utc)
        {
            utc = default;
            if (!TryParseKst(raw, out DateTime kstLocal))
                return false;

            utc = new DateTimeOffset(DateTime.SpecifyKind(kstLocal, DateTimeKind.Unspecified), KoreaStandardTimeOffset)
                .UtcDateTime;
            return true;
        }
    }

    /// <summary>
    /// CHZZK 場次識別（計畫 §3；格式由使用者於 2026-09-15 指定）：
    /// <c>streamKey = channelId + ":" + 正規化 openDate</c>。
    /// <para>
    /// 正規化把原始 <c>2026-09-15 12:41:10</c> 轉為 <c>20260915_124110</c>
    /// （移除冒號與減號、空格改底線）。因為原始格式已由嚴格驗證固定，直接以解析後的 KST 牆上時間重新格式化，
    /// 結果與逐字元取代一致且不會因空白差異產生不同鍵。DB 仍保留 API 原始字串（<c>OpenDateRaw</c>）供顯示與診斷。
    /// </para>
    /// </summary>
    public static class ChzzkStreamIdentity
    {
        /// <summary>場次鍵的 openDate 正規化格式。<c>20260915_124110</c>。</summary>
        internal const string NormalizedOpenDateFormat = "yyyyMMdd_HHmmss";

        public static bool TryCreate(string channelId, string openDateRaw, out string streamKey)
        {
            streamKey = null;
            if (string.IsNullOrWhiteSpace(channelId) || !ChzzkTime.TryParseKst(openDateRaw, out DateTime kstLocal))
                return false;

            streamKey = channelId + ":" + kstLocal.ToString(NormalizedOpenDateFormat, CultureInfo.InvariantCulture);
            return true;
        }
    }
}
