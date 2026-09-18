using System.ComponentModel.DataAnnotations;

namespace DiscordStreamNotifyBot.DataBase.Table
{
    /// <summary>
    /// CHZZK 頻道追蹤來源（每個頻道一筆，多個 guild 訂閱同一頻道不重複建立）。
    /// <para>
    /// <see cref="GuildId"/> 為登記來源的 guild，Bot Owner 歸屬時為 0；
    /// 通知訂閱關係另存於 <see cref="NoticeChzzkStreamChannel"/>。
    /// </para>
    /// </summary>
    public class ChzzkSpider
    {
        [Key]
        public string ChannelId { get; set; }
        public string ChannelName { get; set; }
        public string ChannelImageUrl { get; set; } = "";
        public ulong GuildId { get; set; }
        public DateTime? DateAdded { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 是否對之後偵測到的新場次委派自動錄影。預設關閉，既有資料 migration 後不會自動開始錄影；
        /// 開啟前已發布開台通知的場次不會補錄。
        /// </summary>
        public bool IsRecord { get; set; } = false;

        /// <summary>首次有效基線建立時間（UTC）；null 表示尚未初始化，不得據以發布關台。</summary>
        public DateTime? InitializedAt { get; set; }

        /// <summary>目前或最近確認的場次鍵（<c>channelId:20260915_124110</c>）；尚無場次時為 null。</summary>
        public string CurrentStreamKey { get; set; }
    }
}
