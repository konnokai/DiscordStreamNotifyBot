namespace DiscordStreamNotifyBot.Shared.Messages
{
    /// <summary>
    /// Redis Streams 通知匯流排（<c>bot:notify</c>）的訊息 <c>type</c> 欄位值（計畫 §4.1）。
    /// <para>
    /// 註：會限身分組的<b>逐使用者驗證</b>（member role 檢查）<b>不走匯流排</b> —— 經 shard 守衛後天然按 shard 分區
    /// （各 shard 只檢查自己持有伺服器的成員，OAuth quota 自動分攤），role 操作為 REST 不綁 gateway。
    /// 但「會限影片探索」（頻道層級、bot 金鑰、無逐使用者 token）改由 Scraper 偵測，其需寫入 guild log channel
    /// 的結果走 <see cref="YoutubeMemberVideoLogNotification"/>（<see cref="YoutubeMemberVideoLog"/>）。
    /// </para>
    /// </summary>
    public static class NotifyType
    {
        public const string Youtube = "youtube";
        public const string Twitch = "twitch";
        public const string Twitcasting = "twitcasting";
        public const string Banner = "banner";
        public const string YoutubeMemberVideoLog = "youtube_member_video_log";
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

    /// <summary>
    /// 跨層：會限影片探索（Scraper）需要寫入某會限頻道 log channel 的事件（notifier 端消費後依 shard 守衛發送）。
    /// 對應原 <c>YoutubeMemberService.SendMsgToLogChannelAsync</c> 的參數。
    /// </summary>
    public class YoutubeMemberVideoLogNotification
    {
        /// <summary>會限頻道 Id（= SendMsgToLogChannelAsync 的 checkChannelId，用來反查各 guild 的 log channel）。</summary>
        public string CheckChannelId { get; set; }

        /// <summary>要送到 guild log channel / guild owner 的訊息。</summary>
        public string Message { get; set; }

        /// <summary>送出後是否移除該會限頻道設定（沿用 SendMsgToLogChannelAsync 語意，各 shard 依守衛刪自己的）。</summary>
        public bool IsNeedRemove { get; set; } = true;

        /// <summary>是否同時私訊 guild owner（沿用 SendMsgToLogChannelAsync 語意）。</summary>
        public bool IsNeedSendToOwner { get; set; } = true;

        /// <summary>非空時，Notifier shard 0 額外私訊 Bot 擁有者（ApplicatonOwner）此診斷訊息。</summary>
        public string BotOwnerMessage { get; set; }
    }
}
