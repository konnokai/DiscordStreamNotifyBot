namespace DiscordStreamNotifyBot.DataBase.Table
{
    /// <summary>CHZZK 場次本地生命週期。</summary>
    public enum ChzzkStreamStatus
    {
        /// <summary>已觀察到 OPEN。</summary>
        Open,

        /// <summary>已收到 CLOSE，等待延遲後重新確認。</summary>
        PendingClose,

        /// <summary>已確認關台。</summary>
        Closed,

        /// <summary>已觀察到新場次，但未確認舊場關台時間；不補造 closeDate。</summary>
        Superseded
    }

    /// <summary>
    /// CHZZK 場次快照（每個 streamKey 一筆）。重啟恢復、關台確認與舊場辨識皆以本表為準。
    /// </summary>
    public class ChzzkStream : DbEntity
    {
        /// <summary><c>channelId + ":" + 正規化 openDate</c>（如 <c>20260915_124110</c>），唯一。</summary>
        public string StreamKey { get; set; }

        public string ChannelId { get; set; }

        /// <summary>API 原始開台時間字串（場次識別來源，不重新格式化）。</summary>
        public string OpenDateRaw { get; set; }

        /// <summary>API 回報的關台時間原始字串；未確認時為 null。</summary>
        public string CloseDateRaw { get; set; }

        public string StreamTitle { get; set; }
        public string CategoryName { get; set; }
        public ChzzkStreamStatus Status { get; set; }

        /// <summary>
        /// 最近一次有效觀察時間（UTC）；<see cref="ChzzkStreamStatus.PendingClose"/> 期間凍結在進入待確認的
        /// 時間點，作為關台確認的等待起點，重複 CLOSE 不重設。
        /// </summary>
        public DateTime LastObservedAt { get; set; }
    }
}
