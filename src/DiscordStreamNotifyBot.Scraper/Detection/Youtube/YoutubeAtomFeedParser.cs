using DiscordStreamNotifyBot.SharedService.Youtube;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace DiscordStreamNotifyBot.Scraper.Detection.Youtube
{
    /// <summary>Atom feed 內單一 entry 的欄位；不判斷直播／一般影片，分類仍由 <c>videos.list</c> 與既有 policy 負責。</summary>
    internal readonly record struct YoutubeAtomEntry(
        string VideoId,
        string ChannelId,
        DateTime? Published,
        DateTime? Updated);

    /// <summary>
    /// 解析結果；<see cref="FeedChannelId"/> 來自根節點 <c>yt:channelId</c>（完整 ID 才採用）或 feed 的 self link，
    /// 呼叫端據此核對是否為請求的頻道。
    /// </summary>
    internal sealed class YoutubeAtomParseResult
    {
        public bool Success { get; init; }
        public string FeedChannelId { get; init; }
        public IReadOnlyList<YoutubeAtomEntry> Entries { get; init; } = [];
        public int SkippedEntryCount { get; init; }
        public string Error { get; init; }
    }

    /// <summary>
    /// YouTube Atom feed 的純解析器（計畫 §9.3）：輸入 XML bytes，輸出 feed channel ID 與 entry 清單。
    /// <para>
    /// 禁止 DTD 與外部 entity（<see cref="DtdProcessing.Prohibit"/>、<c>XmlResolver = null</c>），且只在
    /// 既有 Atom／YouTube namespace 中取值；單一壞 entry 只會被略過並計數；重複 video ID 只保留一次。
    /// 不含 HTTP、Redis 或 DB。
    /// </para>
    /// </summary>
    internal static class YoutubeAtomFeedParser
    {
        internal const string AtomNamespace = "http://www.w3.org/2005/Atom";
        internal const string YoutubeNamespace = "http://www.youtube.com/xml/schemas/2015";

        private static readonly XNamespace Atom = AtomNamespace;
        private static readonly XNamespace Youtube = YoutubeNamespace;

        internal static YoutubeAtomParseResult Parse(byte[] xml)
        {
            if (xml == null || xml.Length == 0)
                return new YoutubeAtomParseResult { Success = false, Error = "feed 內容為空" };

            try
            {
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    IgnoreComments = true,
                    IgnoreProcessingInstructions = true,
                    CloseInput = false,
                };

                using var memory = new MemoryStream(xml, writable: false);
                using var reader = XmlReader.Create(memory, settings);
                XElement root = XDocument.Load(reader, LoadOptions.None).Root;
                if (root == null || root.Name != Atom + "feed")
                    return new YoutubeAtomParseResult { Success = false, Error = "不是 Atom feed" };

                // 真實 feed 的根節點 yt:channelId 會省略 "UC" 前綴（實測：YxLMfeX1CbMBll9MsGlzmw），
                // entry 與 self link 才是完整 channel ID。因此只在根節點是完整 ID 時採用，否則改用 self link。
                string feedChannelId = (string)root.Element(Youtube + "channelId");
                if (!YoutubeWebSubContract.IsValidChannelId(feedChannelId))
                {
                    feedChannelId = root.Elements(Atom + "link")
                        .Where(static x => string.Equals((string)x.Attribute("rel"), "self", StringComparison.OrdinalIgnoreCase))
                        .Select(static x => ExtractChannelIdFromTopic((string)x.Attribute("href")))
                        .FirstOrDefault(static x => !string.IsNullOrEmpty(x));
                }

                var entries = new List<YoutubeAtomEntry>();
                var seen = new HashSet<string>(StringComparer.Ordinal);
                int skipped = 0;

                foreach (XElement entry in root.Elements(Atom + "entry"))
                {
                    string videoId = (string)entry.Element(Youtube + "videoId");
                    string channelId = (string)entry.Element(Youtube + "channelId");

                    if (string.IsNullOrWhiteSpace(videoId) || string.IsNullOrWhiteSpace(channelId) || !seen.Add(videoId))
                    {
                        skipped++;
                        continue;
                    }

                    entries.Add(new YoutubeAtomEntry(
                        videoId,
                        channelId,
                        ParseTime((string)entry.Element(Atom + "published")),
                        ParseTime((string)entry.Element(Atom + "updated"))));
                }

                return new YoutubeAtomParseResult
                {
                    Success = true,
                    FeedChannelId = feedChannelId,
                    Entries = entries,
                    SkippedEntryCount = skipped,
                };
            }
            catch (XmlException ex)
            {
                return new YoutubeAtomParseResult { Success = false, Error = $"XML 無效：{ex.Message}" };
            }
            catch (Exception ex)
            {
                return new YoutubeAtomParseResult { Success = false, Error = $"解析失敗：{ex.GetType().Name}" };
            }
        }

        /// <summary>由 <c>https://www.youtube.com/feeds/videos.xml?channel_id=...</c> 取出 channel ID。</summary>
        private static string ExtractChannelIdFromTopic(string href)
        {
            if (string.IsNullOrEmpty(href))
                return null;

            const string marker = "channel_id=";
            int index = href.IndexOf(marker, StringComparison.Ordinal);
            if (index < 0)
                return null;

            string value = href[(index + marker.Length)..];
            int end = value.IndexOfAny(['&', '#']);
            if (end >= 0)
                value = value[..end];

            return string.IsNullOrEmpty(value) ? null : value;
        }

        private static DateTime? ParseTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                ? parsed.UtcDateTime
                : null;
        }
    }
}
