namespace DiscordStreamNotifyBot.Shared.Messages
{
    /// <summary>
    /// Redis Streams 通知匯流排（<c>bot:notify</c>）的訊息 <c>type</c> 欄位值（計畫 §4.1）。
    /// </summary>
    public static class NotifyType
    {
        public const string Youtube = "youtube";
        public const string Twitch = "twitch";
        public const string Banner = "banner";
    }

    /// <summary>
    /// YouTube 通知事件的「通知類型」線路列舉。與 UI 用的 <c>YoutubeStreamService.NoticeType</c>
    /// （帶 Discord <c>[ChoiceDisplay]</c>）分離，僅作跨層傳遞契約，成員順序須對應。
    /// </summary>
    public enum YoutubeNoticeType
    {
        NewStream,
        NewVideo,
        Start,
        End,
        ChangeTime,
        Delete
    }

    /// <summary>
    /// 跨層 YouTube 通知事件（scraper 偵測 → notifier 重建 embed 發送，計畫 §4.1）。
    /// 以結構化資料傳遞，不序列化 Embed 物件。
    /// </summary>
    public class YoutubeNotification
    {
        public YoutubeNoticeType NoticeType { get; set; }
        public string VideoId { get; set; }
        public string ChannelId { get; set; }
        public string ChannelTitle { get; set; }
        public string VideoTitle { get; set; }
        public DateTime ScheduledStartTime { get; set; }
        public DateTime? PreviousScheduledStartTime { get; set; }
        public DateTime? ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }
        public bool IsMemberOnly { get; set; }
        public bool IsUnarchived { get; set; }
        public DataBase.Table.Video.YTChannelType ChannelType { get; set; }
    }

    /// <summary>
    /// Twitch 通知事件的「通知類型」線路列舉。與 UI 用的 <c>TwitchService.NoticeType</c>
    /// （帶 Discord <c>[ChoiceDisplay]</c>）分離，僅作跨層傳遞契約，成員順序須對應。
    /// </summary>
    public enum TwitchNoticeType
    {
        StartStream,
        EndStream,
        ChangeStreamData
    }

    /// <summary>
    /// 跨層 Twitch 通知事件。Profile/Offline 圖片不入 DTO，由消費端自 DB（TwitchSpider）查詢。
    /// </summary>
    public class TwitchNotification
    {
        public TwitchNoticeType NoticeType { get; set; }
        public string UserId { get; set; }

        /// <summary>
        /// Twitch 直播場次 id（全域唯一）。StartStream/EndStream 帶入，供消費端以「場次」為單位去重
        /// （避免同一實況主 5 分鐘內的新場次被舊場次的去重鍵誤擋）。ChangeStreamData 不帶＝該類型不去重。
        /// </summary>
        public string StreamId { get; set; }

        public string UserLogin { get; set; }
        public string UserName { get; set; }

        /// <summary>直播標題（StartStream 必有；EndStream 可為 null＝Redis/VOD 皆無資料）。</summary>
        public string StreamTitle { get; set; }

        /// <summary>分類（StartStream 用）。</summary>
        public string GameName { get; set; }

        /// <summary>預覽圖（StartStream 用）。</summary>
        public string ThumbnailUrl { get; set; }

        /// <summary>開台時間 UTC（StartStream 必有；EndStream 可為 null＝無法計算直播時長）。</summary>
        public DateTime? StreamStartAt { get; set; }

        /// <summary>關台時間（EndStream 用，已扣除去抖動時間）。</summary>
        public DateTime? StreamEndAt { get; set; }

        /// <summary>是否已發送錄影請求（StartStream 用；錄影副作用在偵測端完成）。</summary>
        public bool IsRecord { get; set; }

        /// <summary>語言中立的最多觀看 Clip 清單（EndStream 用）。</summary>
        public List<TwitchClipInfo> Clips { get; set; }

        /// <summary>語言中立的直播資料更新清單（ChangeStreamData 用，去抖動後合併）。</summary>
        public List<TwitchChannelUpdateInfo> Updates { get; set; }
    }

    public class TwitchClipInfo
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string CreatorName { get; set; }
        public int ViewCount { get; set; }
    }

    public class TwitchChannelUpdateInfo
    {
        public long ElapsedSeconds { get; set; }
        public string OldTitle { get; set; }
        public string NewTitle { get; set; }
        public string OldCategory { get; set; }
        public string NewCategory { get; set; }
    }

    /// <summary>跨層伺服器橫幅變更事件（開台時換 banner，需 notifier 端 GetGuild）。</summary>
    public class BannerChangeNotification
    {
        public string ChannelId { get; set; }
        public string VideoId { get; set; }
    }
}
