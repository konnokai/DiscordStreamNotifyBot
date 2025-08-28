namespace StreamNotifyBot.Crawler.Models;

/// <summary>
/// YouTube 影片資訊
/// </summary>
public class YouTubeVideoInfo
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
    /// 頻道標題
    /// </summary>
    public string ChannelTitle { get; set; } = "";

    /// <summary>
    /// 影片描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 縮圖 URL
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// 實際開始時間
    /// </summary>
    public DateTime? ActualStartTime { get; set; }

    /// <summary>
    /// 實際結束時間
    /// </summary>
    public DateTime? ActualEndTime { get; set; }

    /// <summary>
    /// 排程開始時間
    /// </summary>
    public DateTime? ScheduledStartTime { get; set; }

    /// <summary>
    /// 觀看人數
    /// </summary>
    public long? ViewerCount { get; set; }

    /// <summary>
    /// 是否為直播內容
    /// </summary>
    public bool IsLiveContent { get; set; }

    /// <summary>
    /// 直播廣播內容狀態 (live, upcoming, none)
    /// </summary>
    public string? LiveBroadcastContent { get; set; }

    /// <summary>
    /// 影片隱私狀態 (public, unlisted, private)
    /// </summary>
    public string? PrivacyStatus { get; set; }

    /// <summary>
    /// 影片類別
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// 發布時間
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// 額外的元數據
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// 從 Google YouTube API 影片資料轉換
    /// </summary>
    public static YouTubeVideoInfo FromGoogleVideo(Google.Apis.YouTube.v3.Data.Video video)
    {
        var info = new YouTubeVideoInfo
        {
            VideoId = video.Id ?? "",
            Title = video.Snippet?.Title ?? "",
            ChannelId = video.Snippet?.ChannelId ?? "",
            ChannelTitle = video.Snippet?.ChannelTitle ?? "",
            Description = video.Snippet?.Description,
            ThumbnailUrl = video.Snippet?.Thumbnails?.High?.Url ?? video.Snippet?.Thumbnails?.Default__?.Url,
            PublishedAt = video.Snippet?.PublishedAtDateTimeOffset?.DateTime,
            Category = video.Snippet?.CategoryId,
            PrivacyStatus = video.Status?.PrivacyStatus,
            IsLiveContent = video.Snippet?.LiveBroadcastContent != "none"
        };

        // 設定直播相關資訊
        if (video.Snippet != null)
        {
            info.LiveBroadcastContent = video.Snippet.LiveBroadcastContent;
        }

        if (video.LiveStreamingDetails != null)
        {
            info.ActualStartTime = video.LiveStreamingDetails.ActualStartTimeDateTimeOffset?.DateTime;
            info.ActualEndTime = video.LiveStreamingDetails.ActualEndTimeDateTimeOffset?.DateTime;
            info.ScheduledStartTime = video.LiveStreamingDetails.ScheduledStartTimeDateTimeOffset?.DateTime;
            
            // 觀看人數
            if (video.LiveStreamingDetails.ConcurrentViewers.HasValue)
            {
                info.ViewerCount = (long)video.LiveStreamingDetails.ConcurrentViewers.Value;
            }
        }

        return info;
    }
}

/// <summary>
/// 平台監控器狀態資訊
/// </summary>
public class PlatformMonitorStatus
{
    /// <summary>
    /// 平台名稱 (YouTube, Twitch, Twitter, TwitCasting)
    /// </summary>
    public string PlatformName { get; set; } = "";

    /// <summary>
    /// 監控器是否健康
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// 監控的直播數量
    /// </summary>
    public int MonitoredStreamsCount { get; set; }

    /// <summary>
    /// 最後更新時間
    /// </summary>
    public DateTime LastUpdateTime { get; set; }

    /// <summary>
    /// 錯誤訊息（如果有的話）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 額外的元數據資訊
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// API 使用量資訊
    /// </summary>
    public ApiUsageInfo ApiUsage { get; set; } = new();
}

/// <summary>
/// API 使用量資訊
/// </summary>
public class ApiUsageInfo
{
    /// <summary>
    /// 目前使用的配額數量
    /// </summary>
    public int UsedQuota { get; set; }

    /// <summary>
    /// 總配額限制
    /// </summary>
    public int QuotaLimit { get; set; }

    /// <summary>
    /// 配額使用率（百分比）
    /// </summary>
    public double UsagePercentage => QuotaLimit > 0 ? (double)UsedQuota / QuotaLimit * 100 : 0;

    /// <summary>
    /// 配額重置時間
    /// </summary>
    public DateTime QuotaResetTime { get; set; }

    /// <summary>
    /// 剩餘請求數
    /// </summary>
    public int RemainingRequests { get; set; }
}

/// <summary>
/// 直播狀態變化事件參數
/// </summary>
public class StreamStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// 直播資料
    /// </summary>
    public StreamData Stream { get; set; } = new();

    /// <summary>
    /// 是否為上線狀態
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// 事件發生時間
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 平台名稱
    /// </summary>
    public string Platform { get; set; } = "";

    /// <summary>
    /// 變化類型
    /// </summary>
    public StreamChangeType ChangeType { get; set; }
}

/// <summary>
/// 直播資料
/// </summary>
public class StreamData
{
    /// <summary>
    /// 平台特定的直播識別符
    /// </summary>
    public string StreamKey { get; set; } = "";

    /// <summary>
    /// 平台名稱
    /// </summary>
    public string Platform { get; set; } = "";

    /// <summary>
    /// 頻道/使用者 ID
    /// </summary>
    public string DiscordChannelId { get; set; } = "";

    /// <summary>
    /// 頻道/使用者名稱
    /// </summary>
    public string ChannelName { get; set; } = "";

    /// <summary>
    /// 直播標題
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// 直播 URL
    /// </summary>
    public string StreamUrl { get; set; } = "";

    /// <summary>
    /// 縮圖 URL
    /// </summary>
    public string ThumbnailUrl { get; set; } = "";

    /// <summary>
    /// 直播開始時間
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 觀看人數
    /// </summary>
    public int? ViewerCount { get; set; }

    /// <summary>
    /// 額外的元數據
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 直播狀態變化類型
/// </summary>
public enum StreamChangeType
{
    /// <summary>
    /// 直播開始
    /// </summary>
    StreamOnline,

    /// <summary>
    /// 直播結束
    /// </summary>
    StreamOffline,

    /// <summary>
    /// 直播資訊更新
    /// </summary>
    StreamUpdated,

    /// <summary>
    /// 頻道資訊更新
    /// </summary>
    ChannelUpdated
}

/// <summary>
/// 直播追蹤事件資料
/// </summary>
public class StreamFollowEvent
{
    /// <summary>
    /// 平台名稱
    /// </summary>
    public string Platform { get; set; } = "";

    /// <summary>
    /// 平台特定的識別符
    /// </summary>
    public string StreamKey { get; set; } = "";

    /// <summary>
    /// Discord Guild ID
    /// </summary>
    public ulong GuildId { get; set; }

    /// <summary>
    /// Discord Channel ID
    /// </summary>
    public ulong DiscordChannelId { get; set; }

    /// <summary>
    /// 發起追蹤的使用者 ID
    /// </summary>
    public ulong UserId { get; set; }

    /// <summary>
    /// 事件時間戳記
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 額外的元數據
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 取消追蹤事件資料
/// </summary>
public class StreamUnfollowEvent : StreamFollowEvent
{
    // 繼承所有欄位，只是語意不同
}

/// <summary>
/// Redis 事件訊息基底類別
/// </summary>
public abstract class RedisEventMessage
{
    /// <summary>
    /// 事件 ID
    /// </summary>
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 事件類型
    /// </summary>
    public string EventType { get; set; } = "";

    /// <summary>
    /// 事件時間戳記
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 事件版本
    /// </summary>
    public string Version { get; set; } = "1.0";
}

/// <summary>
/// 直播狀態廣播訊息
/// </summary>
public class StreamStatusBroadcastMessage : RedisEventMessage
{
    /// <summary>
    /// 狀態變化的直播清單
    /// </summary>
    public List<StreamStatusChangedEventArgs> Streams { get; set; } = new();

    /// <summary>
    /// 批量處理標記
    /// </summary>
    public bool IsBatch { get; set; }
}

/// <summary>
/// 服務健康狀態
/// </summary>
public class ServiceHealth
{
    /// <summary>
    /// 服務名稱
    /// </summary>
    public string ServiceName { get; set; } = "";

    /// <summary>
    /// 健康狀態
    /// </summary>
    public HealthStatus Status { get; set; }

    /// <summary>
    /// 狀態描述
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// 詳細的健康檢查資料
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// 檢查時間
    /// </summary>
    public DateTime CheckTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 健康狀態枚舉
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// 健康
    /// </summary>
    Healthy,

    /// <summary>
    /// 降級服務
    /// </summary>
    Degraded,

    /// <summary>
    /// 不健康
    /// </summary>
    Unhealthy
}
