using System.Text.RegularExpressions;

namespace DiscordStreamNotifyBot.SharedService.Chzzk
{
    /// <summary>
    /// CHZZK 頻道來源解析：只接受 <c>channelId</c>（32 位十六進位）或 CHZZK 官方網域的頻道／直播 URL。
    /// 不抓取使用者提供的任意網址（SSRF 防線）。
    /// </summary>
    public static class ChzzkUrlParser
    {
        private static readonly Regex ChannelIdPattern = new(
            "^[0-9a-f]{32}$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public static bool TryParseChannelId(string source, out string channelId)
        {
            channelId = null;
            if (string.IsNullOrWhiteSpace(source))
                return false;

            string value = source.Trim();
            if (ChannelIdPattern.IsMatch(value))
            {
                channelId = value.ToLowerInvariant();
                return true;
            }

            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri))
                return false;
            if (!string.Equals(uri.Host, "chzzk.naver.com", StringComparison.OrdinalIgnoreCase))
                return false;

            string[] segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            int index = segments.Length switch
            {
                1 => 0,
                2 when string.Equals(segments[0], "live", StringComparison.OrdinalIgnoreCase) => 1,
                _ => -1
            };
            if (index < 0 || !ChannelIdPattern.IsMatch(segments[index]))
                return false;

            channelId = segments[index].ToLowerInvariant();
            return true;
        }
    }

    /// <summary>CHZZK 官方連結產生器（實作驗證時確認可正常導向）。</summary>
    public static class ChzzkUrls
    {
        public static string Channel(string channelId) => $"https://chzzk.naver.com/{channelId}";

        public static string Live(string channelId) => $"https://chzzk.naver.com/live/{channelId}";
    }
}
