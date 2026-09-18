namespace DiscordStreamNotifyBot.Shared.Messages
{
    /// <summary>
    /// <c>chzzk.record</c> 錄影請求 payload（Bot 與 StreamRecordTools 共用契約）。
    /// JSON 欄位固定為 <c>channelId</c> 與 <c>streamKey</c>，兩端須同輪修改。
    /// </summary>
    public class ChzzkRecordRequest
    {
        [JsonProperty("channelId")]
        public string ChannelId { get; set; }

        /// <summary>場次鍵：<c>channelId:yyyyMMdd_HHmmss</c>（KST 開台時間正規化）；僅供檔名與日誌追蹤。</summary>
        [JsonProperty("streamKey")]
        public string StreamKey { get; set; }
    }

    /// <summary>
    /// CHZZK 錄影請求的發布入口：集中 channel 名稱與序列化，讓 Scraper 自動委派與 Notifier 立即錄影
    /// 不會各自漂移。回傳值為 Redis Pub/Sub 訂閱者數量，不是 worker 確認。
    /// </summary>
    public static class ChzzkRecordBus
    {
        public static Task<long> PublishAsync(ISubscriber subscriber, string channelId, string streamKey)
            => subscriber.PublishAsync(
                new RedisChannel(RedisChannels.Chzzk.Record, RedisChannel.PatternMode.Literal),
                JsonConvert.SerializeObject(new ChzzkRecordRequest { ChannelId = channelId, StreamKey = streamKey }));
    }
}
