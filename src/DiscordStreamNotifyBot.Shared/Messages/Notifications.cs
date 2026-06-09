namespace DiscordStreamNotifyBot.Shared.Messages
{
    /// <summary>
    /// RabbitMQ 通知匯流排 routing key（綁定 <c>bot.notify</c> topic exchange，計畫 §4.1）。
    /// </summary>
    public static class NotifyRoutingKeys
    {
        public const string Youtube = "youtube";
        public const string Twitch = "twitch";
        public const string Twitcasting = "twitcasting";
        public const string Banner = "banner";
        public const string Member = "member";
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

    /// <summary>跨層 Twitch 通知事件。</summary>
    public class TwitchNotification
    {
        /// <summary>對應 <c>TwitchService.NoticeType</c>：StartStream / EndStream / ChangeStreamData。</summary>
        public string NoticeType { get; set; }
        public string StreamId { get; set; }
        public string UserId { get; set; }
        public string UserLogin { get; set; }
        public string UserName { get; set; }
        public string Title { get; set; }
        public string GameName { get; set; }
        public string ThumbnailUrl { get; set; }
        public DateTime StreamStartAt { get; set; }
        public DateTime? StreamEndAt { get; set; }
    }

    /// <summary>跨層 TwitCasting 通知事件。</summary>
    public class TwitcastingNotification
    {
        public string MovieId { get; set; }
        public string ChannelId { get; set; }
        public string ChannelTitle { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsRecord { get; set; }
        public DateTime StreamStartAt { get; set; }
    }

    /// <summary>跨層伺服器橫幅變更事件（開台時換 banner，需 notifier 端 GetGuild）。</summary>
    public class BannerChangeNotification
    {
        public string ChannelId { get; set; }
        public string VideoId { get; set; }
    }

    /// <summary>跨層會員身分組事件（授予 / 移除，需 notifier 端 GetGuild）。</summary>
    public class MemberNotification
    {
        /// <summary>動作：Grant / Revoke。</summary>
        public string Action { get; set; }
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public ulong RoleId { get; set; }
        public string MemberCheckChannelId { get; set; }
        public string MemberCheckChannelTitle { get; set; }
    }
}
