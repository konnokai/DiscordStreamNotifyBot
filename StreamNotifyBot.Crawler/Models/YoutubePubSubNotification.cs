namespace StreamNotifyBot.Crawler.Models;

/// <summary>
/// YouTube PubSubHubbub 通知模型
/// 接收來自外部 Backend 服務的通知資料
/// </summary>
public class YoutubePubSubNotification
{
    /// <summary>
    /// 影片 ID
    /// </summary>
    public string VideoId { get; set; } = "";

    /// <summary>
    /// 頻道 ID
    /// </summary>
    public string ChannelId { get; set; } = "";

    /// <summary>
    /// 影片標題
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// 頻道名稱
    /// </summary>
    public string ChannelTitle { get; set; } = "";

    /// <summary>
    /// 影片發布時間
    /// </summary>
    public DateTime Published { get; set; }

    /// <summary>
    /// 影片更新時間
    /// </summary>
    public DateTime Updated { get; set; }

    /// <summary>
    /// 影片連結
    /// </summary>
    public string VideoUrl { get; set; } = "";

    /// <summary>
    /// 頻道連結
    /// </summary>
    public string ChannelUrl { get; set; } = "";

    /// <summary>
    /// 通知類型 (new, updated, deleted)
    /// </summary>
    public string NotificationType { get; set; } = "new";

    /// <summary>
    /// 原始 XML 資料（用於除錯）
    /// </summary>
    public string? RawXml { get; set; }

    /// <summary>
    /// 接收時間戳
    /// </summary>
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
