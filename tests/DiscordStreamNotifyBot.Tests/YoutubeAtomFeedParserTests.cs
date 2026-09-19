using DiscordStreamNotifyBot.Scraper.Detection.Youtube;
using System.Text;

namespace DiscordStreamNotifyBot.Tests
{
    /// <summary>Atom feed 解析器測試（計畫 §12.1）：正常 feed、channel 不符、DTD、壞 entry 與重複 ID。</summary>
    public sealed class YoutubeAtomFeedParserTests
    {
        private const string ChannelId = "UCabcdefghijklmnopqrstuv";

        [Fact]
        public void ParsesCurrentYoutubeFeedNamespace()
        {
            var result = YoutubeAtomFeedParser.Parse(Encoding.UTF8.GetBytes(Feed()));

            Assert.True(result.Success, result.Error);
            Assert.Equal(ChannelId, result.FeedChannelId);
            Assert.Equal(0, result.SkippedEntryCount);
            Assert.Equal(2, result.Entries.Count);

            Assert.Equal("dQw4w9WgXcQ", result.Entries[0].VideoId);
            Assert.Equal(ChannelId, result.Entries[0].ChannelId);
            Assert.Equal(new DateTime(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc), result.Entries[0].Published);
            Assert.Equal(new DateTime(2026, 9, 19, 3, 0, 0, DateTimeKind.Utc), result.Entries[0].Updated);
            Assert.Equal("abcDEFghi12", result.Entries[1].VideoId);
        }

        [Fact]
        public void KeepsValidEntriesWhenOneEntryIsMalformed()
        {
            string feed = Feed().Replace("<yt:channelId>" + ChannelId + "</yt:channelId><!--2-->", "");
            var result = YoutubeAtomFeedParser.Parse(Encoding.UTF8.GetBytes(feed));

            Assert.True(result.Success, result.Error);
            Assert.Equal(1, result.SkippedEntryCount);
            Assert.Single(result.Entries);
            Assert.Equal("dQw4w9WgXcQ", result.Entries[0].VideoId);
        }

        [Fact]
        public void DeduplicatesRepeatedVideoIds()
        {
            string feed = Feed().Replace("abcDEFghi12", "dQw4w9WgXcQ");
            var result = YoutubeAtomFeedParser.Parse(Encoding.UTF8.GetBytes(feed));

            Assert.True(result.Success, result.Error);
            Assert.Single(result.Entries);
            Assert.Equal(1, result.SkippedEntryCount);
        }

        [Fact]
        public void ParsesMinifiedFeedWithoutWhitespaceBetweenElements()
        {
            string feed = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
                + "<feed xmlns:yt=\"http://www.youtube.com/xml/schemas/2015\" xmlns=\"http://www.w3.org/2005/Atom\">"
                + $"<link rel=\"self\" href=\"https://www.youtube.com/feeds/videos.xml?channel_id={ChannelId}\"/>"
                + "<entry><yt:videoId>abcDEFghi12</yt:videoId>"
                + $"<yt:channelId>{ChannelId}</yt:channelId>"
                + "<published>2026-09-17T12:00:00+00:00</published>"
                + "<updated>2026-09-17T13:00:00+00:00</updated></entry></feed>";

            var result = YoutubeAtomFeedParser.Parse(Encoding.UTF8.GetBytes(feed));

            Assert.True(result.Success, result.Error);
            Assert.Equal(ChannelId, result.FeedChannelId);
            Assert.Single(result.Entries);
            Assert.Equal("abcDEFghi12", result.Entries[0].VideoId);
            Assert.Equal(new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc), result.Entries[0].Published);
        }

        [Fact]
        public void RejectsDtdAndExternalEntities()
        {
            string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <!DOCTYPE feed [ <!ENTITY xxe SYSTEM "file:///etc/passwd"> ]>
                <feed xmlns="http://www.w3.org/2005/Atom"><title>&xxe;</title></feed>
                """;

            var result = YoutubeAtomFeedParser.Parse(Encoding.UTF8.GetBytes(xml));

            Assert.False(result.Success);
        }

        [Fact]
        public void RejectsInvalidXmlAndEmptyInput()
        {
            Assert.False(YoutubeAtomFeedParser.Parse(Encoding.UTF8.GetBytes("<feed>")).Success);
            Assert.False(YoutubeAtomFeedParser.Parse([]).Success);
            Assert.False(YoutubeAtomFeedParser.Parse(null).Success);
        }

        [Fact]
        public void FeedLevelChannelIdTakesPrecedenceOverSelfLink()
        {
            // 完整性檢查用：根節點宣告完整 channel ID 時以它為準，可以抓出與 self link 矛盾的 feed。
            string feed = Feed().Replace(
                "<title>YouTube video feed</title>",
                "<title>YouTube video feed</title>"
                + $"<yt:channelId>UCzyxwvutsrqponmlkjihgfe</yt:channelId>");
            var result = YoutubeAtomFeedParser.Parse(Encoding.UTF8.GetBytes(feed));

            Assert.True(result.Success, result.Error);
            Assert.Equal("UCzyxwvutsrqponmlkjihgfe", result.FeedChannelId);
        }

        [Fact]
        public void IgnoresFeedLevelChannelIdWithoutUcPrefix()
        {
            // 實測 YouTube feed 根節點是省略 "UC" 前綴的 22 字元形式：不可當成 channel 不符而丟棄整份 feed。
            string feed = Feed().Replace(
                "<title>YouTube video feed</title>",
                "<title>YouTube video feed</title><yt:channelId>" + ChannelId[2..] + "</yt:channelId>");
            var result = YoutubeAtomFeedParser.Parse(Encoding.UTF8.GetBytes(feed));

            Assert.True(result.Success, result.Error);
            Assert.Equal(ChannelId, result.FeedChannelId);
            Assert.Equal(2, result.Entries.Count);
        }

        /// <summary>
        /// 依 2026-09-19 對真實 feed（UCYxLMfeX1CbMBll9MsGlzmw）實測的形狀：
        /// 根節點 yt:channelId 省略 "UC" 前綴、entry 為完整 ID、self link 為完整 ID。
        /// </summary>
        [Fact]
        public void ParsesRealYoutubeFeedShapeWithUnprefixedRootChannelId()
        {
            string feed = Feed().Replace(
                "<title>YouTube video feed</title>",
                $"<id>yt:channel:{ChannelId[2..]}</id><yt:channelId>{ChannelId[2..]}</yt:channelId><title>YouTube video feed</title>");
            var result = YoutubeAtomFeedParser.Parse(Encoding.UTF8.GetBytes(feed));

            Assert.True(result.Success, result.Error);
            Assert.Equal(ChannelId, result.FeedChannelId);
            Assert.Equal(2, result.Entries.Count);
            Assert.All(result.Entries, entry => Assert.Equal(ChannelId, entry.ChannelId));
        }

        [Fact]
        public void ReportsMissingFeedChannelLink()
        {
            string feed = Feed().Replace("<link rel=\"self\" href=\"https://www.youtube.com/feeds/videos.xml?channel_id=" + ChannelId + "\"/>", "");
            var result = YoutubeAtomFeedParser.Parse(Encoding.UTF8.GetBytes(feed));

            Assert.True(result.Success, result.Error);
            Assert.Null(result.FeedChannelId);
            Assert.Equal(2, result.Entries.Count);
        }

        private static string Feed()
            => $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <feed xmlns:yt="http://www.youtube.com/xml/schemas/2015" xmlns="http://www.w3.org/2005/Atom">
                  <link rel="hub" href="https://pubsubhubbub.appspot.com"/>
                  <link rel="self" href="https://www.youtube.com/feeds/videos.xml?channel_id={ChannelId}"/>
                  <title>YouTube video feed</title>
                  <updated>2026-09-19T03:00:00+00:00</updated>
                  <entry>
                    <id>yt:video:dQw4w9WgXcQ</id>
                    <yt:videoId>dQw4w9WgXcQ</yt:videoId>
                    <yt:channelId>{ChannelId}</yt:channelId>
                    <title>First</title>
                    <link rel="alternate" href="https://www.youtube.com/watch?v=dQw4w9WgXcQ"/>
                    <published>2026-09-18T12:00:00+00:00</published>
                    <updated>2026-09-19T03:00:00+00:00</updated>
                  </entry>
                  <entry>
                    <id>yt:video:abcDEFghi12</id>
                    <yt:videoId>abcDEFghi12</yt:videoId>
                    <yt:channelId>{ChannelId}</yt:channelId><!--2-->
                    <title>Second</title>
                    <published>2026-09-17T12:00:00+00:00</published>
                    <updated>2026-09-17T13:00:00+00:00</updated>
                  </entry>
                </feed>
                """;
    }
}
