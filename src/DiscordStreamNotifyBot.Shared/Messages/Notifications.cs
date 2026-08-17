namespace DiscordStreamNotifyBot.Shared.Messages
{
    /// <summary>
    /// Redis Streams 通知匯流排（<c>bot:notify</c>）的訊息 <c>type</c> 欄位值（計畫 §4.1）。
    /// <para>
    /// 註：會限身分組的<b>逐使用者驗證</b>（member role 檢查）<b>不走匯流排</b> —— 經 shard 守衛後天然按 shard 分區
    /// （各 shard 只檢查自己持有伺服器的成員，OAuth quota 自動分攤），role 操作為 REST 不綁 gateway。
    /// 但「會限影片探索」由 Scraper 偵測，並透過 <see cref="YoutubeMemberVideoLogNotification"/>
    /// 將結果寫入對應的紀錄頻道（<see cref="YoutubeMemberVideoLog"/>）。
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

        /// <summary>最多觀看 Clip 清單的舊版繁中字串，供舊版 payload 或舊版 Notifier 回退時使用。</summary>
        public string ClipsValue { get; set; }

        /// <summary>語言中立的直播資料更新清單（ChangeStreamData 用，去抖動後合併）。</summary>
        public List<TwitchChannelUpdateInfo> Updates { get; set; }

        /// <summary>直播資料更新的舊版繁中字串，供舊版 payload 或舊版 Notifier 回退時使用。</summary>
        public string Description { get; set; }
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
    /// 跨層：Scraper 探索會限影片後，通知 Notifier 將結果寫入對應紀錄頻道的事件。
    /// Notifier 消費後依 shard 守衛發送，對應原 <c>YoutubeMemberService.SendMsgToLogChannelAsync</c> 的參數。
    /// </summary>
    public class YoutubeMemberVideoLogNotification
    {
        /// <summary>會限頻道 Id（= SendMsgToLogChannelAsync 的 checkChannelId，用來反查各 guild 的 log channel）。</summary>
        public string CheckChannelId { get; set; }

        /// <summary>要傳送給伺服器紀錄頻道或伺服器擁有者的訊息。</summary>
        public string Message { get; set; }

        /// <summary>可由 Notifier 依 guild locale 排版的穩定訊息代碼；舊 payload 可為空。</summary>
        public string MessageCode { get; set; }

        /// <summary>訊息代碼的語言中立參數；不包含 locale。</summary>
        public string[] MessageArguments { get; set; }

        /// <summary>送出後是否移除該會限頻道設定（沿用 SendMsgToLogChannelAsync 語意，各 shard 依守衛刪自己的）。</summary>
        public bool IsNeedRemove { get; set; } = true;

        /// <summary>是否同時私訊 guild owner（沿用 SendMsgToLogChannelAsync 語意）。</summary>
        public bool IsNeedSendToOwner { get; set; } = true;

        /// <summary>非空時，Notifier shard 0 額外私訊 Bot 擁有者（ApplicatonOwner）此診斷訊息。</summary>
        public string BotOwnerMessage { get; set; }
    }
}
