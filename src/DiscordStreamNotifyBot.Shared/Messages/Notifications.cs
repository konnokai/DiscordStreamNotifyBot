namespace DiscordStreamNotifyBot.Shared.Messages
{
    /// <summary>
    /// RabbitMQ 通知匯流排 routing key（綁定 <c>bot.notify</c> topic exchange，計畫 §4.1）。
    /// <para>
    /// 註：會限身分組（member）<b>不走匯流排</b> —— 會限檢查經 shard 守衛後天然按 shard 分區
    /// （各 shard 只檢查自己持有伺服器的成員，OAuth quota 自動分攤），role 操作為 REST 不綁 gateway，
    /// 且其多種檢查結果各對應不同 log/私訊內容，DTO 化高風險零收益。
    /// </para>
    /// </summary>
    public static class NotifyRoutingKeys
    {
        public const string Youtube = "youtube";
        public const string Twitch = "twitch";
        public const string Twitcasting = "twitcasting";
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
        public DateTime? ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }
        public bool IsMemberOnly { get; set; }
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

        /// <summary>最多觀看 Clip 清單（EndStream 用，偵測端已組好的 Markdown；可為空）。</summary>
        public string ClipsValue { get; set; }

        /// <summary>直播資料更新彙整訊息（ChangeStreamData 用，去抖動後合併）。</summary>
        public string Description { get; set; }
    }

    /// <summary>跨層 TwitCasting 開台通知事件（欄位對應 DataBase.Table.TwitcastingStream）。</summary>
    public class TwitcastingNotification
    {
        public string ChannelId { get; set; }
        public string ChannelTitle { get; set; }
        public int StreamId { get; set; }
        public string StreamTitle { get; set; }
        public string StreamSubTitle { get; set; }
        public string Category { get; set; }
        public string ThumbnailUrl { get; set; }
        public DateTime StreamStartAt { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsRecord { get; set; }
    }

    /// <summary>跨層伺服器橫幅變更事件（開台時換 banner，需 notifier 端 GetGuild）。</summary>
    public class BannerChangeNotification
    {
        public string ChannelId { get; set; }
        public string VideoId { get; set; }
    }
}
