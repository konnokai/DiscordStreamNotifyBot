namespace StreamNotifyBot.Crawler.Services;

/// <summary>
/// 直播追蹤管理服務介面
/// </summary>
public interface IStreamTracker
{
    /// <summary>
    /// 新增追蹤
    /// </summary>
    /// <param name="platform">平台名稱</param>
    /// <param name="streamKey">直播識別符</param>
    /// <param name="guildId">Guild ID</param>
    /// <param name="channelId">Channel ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>是否成功</returns>
    Task<bool> AddTrackingAsync(string platform, string streamKey, ulong guildId, ulong channelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除追蹤
    /// </summary>
    /// <param name="platform">平台名稱</param>
    /// <param name="streamKey">直播識別符</param>
    /// <param name="guildId">Guild ID</param>
    /// <param name="channelId">Channel ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>是否成功</returns>
    Task<bool> RemoveTrackingAsync(string platform, string streamKey, ulong guildId, ulong channelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得追蹤此直播的 Guild 清單
    /// </summary>
    /// <param name="platform">平台名稱</param>
    /// <param name="streamKey">直播識別符</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Guild ID 清單</returns>
    Task<IReadOnlyList<ulong>> GetTrackingGuildsAsync(string platform, string streamKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得追蹤統計資訊
    /// </summary>
    /// <returns>追蹤統計資訊</returns>
    Task<TrackingStatistics> GetTrackingStatisticsAsync();

    /// <summary>
    /// 從資料庫載入追蹤資料
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns></returns>
    Task LoadTrackingDataAsync(CancellationToken cancellationToken = default);
}