using StreamNotifyBot.Crawler.Models;

namespace StreamNotifyBot.Crawler.Services;

/// <summary>
/// 平台監控器介面
/// 定義所有平台監控器必須實作的基本功能
/// </summary>
public interface IPlatformMonitor
{
    /// <summary>
    /// 平台名稱
    /// </summary>
    string PlatformName { get; }

    /// <summary>
    /// 啟動監控器
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>啟動任務</returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 停止監控器
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>停止任務</returns>
    Task StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 取得監控器狀態
    /// </summary>
    /// <returns>平台監控器狀態</returns>
    Task<PlatformMonitorStatus> GetStatusAsync();

    /// <summary>
    /// 直播狀態變化事件
    /// </summary>
    event EventHandler<StreamStatusChangedEventArgs>? StreamStatusChanged;

    /// <summary>
    /// 新增要監控的直播
    /// </summary>
    /// <param name="streamKey">直播識別符</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>新增成功與否</returns>
    Task<bool> AddStreamAsync(string streamKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除要監控的直播
    /// </summary>
    /// <param name="streamKey">直播識別符</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>移除成功與否</returns>
    Task<bool> RemoveStreamAsync(string streamKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得目前監控的直播清單
    /// </summary>
    /// <returns>直播識別符清單</returns>
    Task<IReadOnlyList<string>> GetMonitoredStreamsAsync();

    /// <summary>
    /// 強制檢查所有監控的直播狀態
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>檢查任務</returns>
    Task ForceCheckAllAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// YouTube 監控器介面
/// </summary>
public interface IYoutubeMonitor : IPlatformMonitor
{
    /// <summary>
    /// 設定 API 金鑰
    /// </summary>
    /// <param name="apiKeys">API 金鑰清單</param>
    void SetApiKeys(IEnumerable<string> apiKeys);

    /// <summary>
    /// 取得 API 配額使用情況
    /// </summary>
    /// <returns>API 使用量資訊</returns>
    Task<ApiUsageInfo> GetApiUsageAsync();

    /// <summary>
    /// 重新整理 Webhook 訂閱
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>重新整理任務</returns>
    Task RefreshWebhookSubscriptionsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Twitch 監控器介面
/// </summary>
public interface ITwitchMonitor : IPlatformMonitor
{
    /// <summary>
    /// 設定 Twitch API 認證資訊
    /// </summary>
    /// <param name="clientId">客戶端 ID</param>
    /// <param name="clientSecret">客戶端密碼</param>
    Task SetCredentialsAsync(string clientId, string clientSecret);

    /// <summary>
    /// 重新整理 EventSub 訂閱
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>重新整理任務</returns>
    Task RefreshEventSubSubscriptionsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Twitter 監控器介面
/// </summary>
public interface ITwitterMonitor : IPlatformMonitor
{
    /// <summary>
    /// 設定 Twitter API 認證資訊
    /// </summary>
    /// <param name="bearerToken">Bearer Token</param>
    /// <param name="consumerKey">Consumer Key</param>
    /// <param name="consumerSecret">Consumer Secret</param>
    Task SetCredentialsAsync(string bearerToken, string consumerKey, string consumerSecret);

    /// <summary>
    /// 檢查 API 限制狀態
    /// </summary>
    /// <returns>API 使用量資訊</returns>
    Task<ApiUsageInfo> GetRateLimitStatusAsync();
}

/// <summary>
/// TwitCasting 監控器介面
/// </summary>
public interface ITwitCastingMonitor : IPlatformMonitor
{
    /// <summary>
    /// 設定 TwitCasting API 認證資訊
    /// </summary>
    /// <param name="clientId">客戶端 ID</param>
    /// <param name="clientSecret">客戶端密碼</param>
    Task SetCredentialsAsync(string clientId, string clientSecret);
}

/// <summary>
/// 追蹤統計資訊
/// </summary>
public class TrackingStatistics
{
    /// <summary>
    /// 總追蹤數
    /// </summary>
    public int TotalTrackingCount { get; set; }

    /// <summary>
    /// 各平台追蹤數
    /// </summary>
    public Dictionary<string, int> PlatformTrackingCounts { get; set; } = new();

    /// <summary>
    /// 活躍的 Guild 數量
    /// </summary>
    public int ActiveGuildCount { get; set; }

    /// <summary>
    /// 最後更新時間
    /// </summary>
    public DateTime LastUpdateTime { get; set; } = DateTime.UtcNow;
}
