using StreamNotifyBot.Crawler.Models;
using Google.Apis.YouTube.v3.Data;

namespace StreamNotifyBot.Crawler.Services;

/// <summary>
/// YouTube API 服務介面
/// </summary>
public interface IYouTubeApiService
{
    /// <summary>
    /// 取得影片詳細資訊
    /// </summary>
    Task<Video?> GetVideoAsync(string videoId);

    /// <summary>
    /// 取得頻道 ID
    /// </summary>
    Task<string?> GetChannelIdAsync(string channelUrl);

    /// <summary>
    /// 取得頻道最新影片
    /// </summary>
    Task<IEnumerable<SearchResult>> GetChannelLatestVideosAsync(string channelId, int maxResults = 10);
}

/// <summary>
/// YouTube 事件服務介面
/// </summary>
public interface IYouTubeEventService
{
    /// <summary>
    /// 廣播直播開始事件
    /// </summary>
    Task BroadcastStreamStartAsync(YouTubeVideoInfo videoInfo);

    /// <summary>
    /// 廣播直播結束事件
    /// </summary>
    Task BroadcastStreamEndAsync(YouTubeVideoInfo videoInfo);

    /// <summary>
    /// 廣播頻道更新事件
    /// </summary>
    Task BroadcastChannelUpdateAsync(string channelId, string channelTitle, Dictionary<string, object>? additionalData = null);

    /// <summary>
    /// 廣播錯誤事件
    /// </summary>
    Task BroadcastErrorEventAsync(string errorType, string message, Dictionary<string, object>? context = null);

    /// <summary>
    /// 廣播監控統計事件
    /// </summary>
    Task BroadcastMonitoringStatsAsync(int channelsChecked, int videosFound, int errorsEncountered, TimeSpan duration);

    /// <summary>
    /// 批量廣播事件
    /// </summary>
    Task BroadcastBatchEventsAsync(IEnumerable<(string EventType, object EventData)> events);

    /// <summary>
    /// Redis 連接狀態
    /// </summary>
    bool IsConnected { get; }
}

/// <summary>
/// YouTube 追蹤管理器介面
/// </summary>
public interface IYouTubeTrackingManager : IDisposable
{
    /// <summary>
    /// 檢查頻道是否被追蹤
    /// </summary>
    bool IsChannelTracked(string channelId);

    /// <summary>
    /// 添加頻道追蹤
    /// </summary>
    void AddChannelTracking(string channelId);

    /// <summary>
    /// 移除頻道追蹤
    /// </summary>
    bool RemoveChannelTracking(string channelId);

    /// <summary>
    /// 取得頻道追蹤數量
    /// </summary>
    int GetTrackedChannelCount(string channelId);

    /// <summary>
    /// 取得所有被追蹤的頻道
    /// </summary>
    List<string> GetAllTrackedChannels();

    /// <summary>
    /// 清除所有追蹤
    /// </summary>
    void ClearAllTracking();

    /// <summary>
    /// 取得追蹤統計資訊
    /// </summary>
    TrackingStatistics GetTrackingStatistics();

    /// <summary>
    /// 啟動服務
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 停止服務
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken);
}
