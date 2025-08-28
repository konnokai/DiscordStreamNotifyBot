using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using StreamNotifyBot.Crawler.Configuration;
using StreamNotifyBot.Crawler.Models;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using System.Text.Json;
using StackExchange.Redis;
using System.Collections.Concurrent;

namespace StreamNotifyBot.Crawler.Services;

/// <summary>
/// YouTube 追蹤管理服務，負責監聽 Discord Shard 的追蹤請求並管理動態監控目標
/// </summary>
public class YouTubeTrackingManager : BackgroundService
{
    private readonly ILogger<YouTubeTrackingManager> _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly ISubscriber _subscriber;
    private readonly MainDbService _dbService;
    private readonly YouTubeEventService _eventService;
    private readonly RedisConfig _redisConfig;
    
    // 全域追蹤計數器 - 追蹤每個頻道被多少個 Guild 關注
    private readonly ConcurrentDictionary<string, int> _globalTrackingCounts = new();
    
    // 目前正在監控的頻道清單
    private readonly ConcurrentDictionary<string, DateTime> _activeMonitoringTargets = new();
    
    private readonly SemaphoreSlim _trackingUpdateSemaphore = new(1, 1);
    private CancellationTokenSource? _listenerCancellationTokenSource;

    public YouTubeTrackingManager(
        ILogger<YouTubeTrackingManager> logger,
        IConnectionMultiplexer redis,
        MainDbService dbService,
        YouTubeEventService eventService,
        RedisConfig redisConfig)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _subscriber = redis.GetSubscriber();
        _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _redisConfig = redisConfig ?? throw new ArgumentNullException(nameof(redisConfig));
    }

    /// <summary>
    /// 啟動追蹤管理服務，開始監聽 Redis PubSub 事件
    /// </summary>
    public new async Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting YouTube tracking manager...");
            
            _listenerCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            // 初始化全域追蹤計數器
            await InitializeTrackingCountsAsync();
            
            // 開始監聽 Discord Shard 事件
            await StartRedisEventListenerAsync(_listenerCancellationTokenSource.Token);
            
            _logger.LogInformation("YouTube tracking manager started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start YouTube tracking manager");
            throw;
        }
        await base.StartAsync(cancellationToken);
    }

    /// <summary>
    /// BackgroundService 的主要執行邏輯
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 在背景服務中等待直到停止
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 定期執行維護任務或清理
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // 正常停止
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in YouTube Tracking Manager background execution");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    /// <summary>
    /// 停止追蹤管理服務
    /// </summary>
    public new async Task StopAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Stopping YouTube tracking manager...");
            
            _listenerCancellationTokenSource?.Cancel();
            
            // 等待所有監聽任務完成
            await Task.Delay(1000);
            
            _logger.LogInformation("YouTube tracking manager stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while stopping YouTube tracking manager");
        }
        finally
        {
            _listenerCancellationTokenSource?.Dispose();
            _listenerCancellationTokenSource = null;
        }
    }

    /// <summary>
    /// 初始化全域追蹤計數器
    /// </summary>
    private async Task InitializeTrackingCountsAsync()
    {
        try
        {
            _logger.LogDebug("Initializing global tracking counts...");
            
            using var db = _dbService.GetDbContext();
            
            // 統計所有平台的追蹤數據
            var trackingCounts = await db.YoutubeChannelSpider
                .Where(x => !string.IsNullOrEmpty(x.ChannelId))
                .GroupBy(x => x.ChannelId)
                .Select(g => new { ChannelId = g.Key, Count = g.Count() })
                .ToListAsync();

            _globalTrackingCounts.Clear();
            _activeMonitoringTargets.Clear();

            foreach (var item in trackingCounts)
            {
                if (!string.IsNullOrEmpty(item.ChannelId))
                {
                    _globalTrackingCounts[item.ChannelId] = item.Count;
                    
                    // 如果有任何 Guild 在追蹤，就加入活躍監控目標
                    if (item.Count > 0)
                    {
                        _activeMonitoringTargets[item.ChannelId] = DateTime.UtcNow;
                    }
                }
            }

            _logger.LogInformation("Initialized tracking counts for {ChannelCount} channels, {ActiveCount} active monitoring targets", 
                _globalTrackingCounts.Count, _activeMonitoringTargets.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize tracking counts");
            throw;
        }
    }

    /// <summary>
    /// 開始監聽 Redis PubSub 事件
    /// </summary>
    private async Task StartRedisEventListenerAsync(CancellationToken cancellationToken)
    {
        try
        {
            // 監聽追蹤事件
            var followChannel = RedisChannel.Literal($"{_redisConfig.KeyPrefix}:events:stream.follow");
            var unfollowChannel = RedisChannel.Literal($"{_redisConfig.KeyPrefix}:events:stream.unfollow");
            
            await _subscriber.SubscribeAsync(followChannel, async (channel, message) =>
            {
                try
                {
                    if (message.HasValue && !string.IsNullOrEmpty(message))
                    {
                        await ProcessFollowEventAsync(message!);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing follow event: {Message}", message);
                }
            });

            await _subscriber.SubscribeAsync(unfollowChannel, async (channel, message) =>
            {
                try
                {
                    if (message.HasValue && !string.IsNullOrEmpty(message))
                    {
                        await ProcessUnfollowEventAsync(message!);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing unfollow event: {Message}", message);
                }
            });

            _logger.LogInformation("Started listening for follow/unfollow events on channels: {FollowChannel}, {UnfollowChannel}", 
                followChannel, unfollowChannel);

            // 移除阻塞迴圈，讓訂閱在背景執行
            // Redis 訂閱是持久性的，不需要保持迴圈
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Redis event listener cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Redis event listener");
            throw; // 重新拋出異常，讓服務啟動失敗
        }
    }

    /// <summary>
    /// 處理追蹤事件
    /// </summary>
    private async Task ProcessFollowEventAsync(string message)
    {
        try
        {
            var followEvent = JsonSerializer.Deserialize<StreamFollowEvent>(message);
            if (followEvent == null)
            {
                _logger.LogWarning("Failed to deserialize follow event: {Message}", message);
                return;
            }

            // 只處理 YouTube 平台的事件
            if (followEvent.Platform?.ToLower() != "youtube")
            {
                return;
            }

            var channelId = followEvent.StreamKey;
            if (string.IsNullOrEmpty(channelId))
            {
                _logger.LogWarning("Follow event missing channel ID: {Message}", message);
                return;
            }

            await UpdateTrackingCountAsync(channelId, 1);
            
            _logger.LogInformation("Added YouTube channel tracking: {ChannelId} for Guild {GuildId}", 
                channelId, followEvent.GuildId);
                
            // 廣播追蹤狀態變化事件
            await _eventService.BroadcastChannelUpdateAsync(
                channelId, 
                "Unknown", 
                new Dictionary<string, object>
                {
                    ["UpdateType"] = "FollowAdded",
                    ["GuildId"] = followEvent.GuildId,
                    ["UserId"] = followEvent.UserId,
                    ["TrackingCount"] = _globalTrackingCounts.GetValueOrDefault(channelId, 0)
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process follow event: {Message}", message);
        }
    }

    /// <summary>
    /// 處理取消追蹤事件
    /// </summary>
    private async Task ProcessUnfollowEventAsync(string message)
    {
        try
        {
            var unfollowEvent = JsonSerializer.Deserialize<StreamUnfollowEvent>(message);
            if (unfollowEvent == null)
            {
                _logger.LogWarning("Failed to deserialize unfollow event: {Message}", message);
                return;
            }

            // 只處理 YouTube 平台的事件
            if (unfollowEvent.Platform?.ToLower() != "youtube")
            {
                return;
            }

            var channelId = unfollowEvent.StreamKey;
            if (string.IsNullOrEmpty(channelId))
            {
                _logger.LogWarning("Unfollow event missing channel ID: {Message}", message);
                return;
            }

            await UpdateTrackingCountAsync(channelId, -1);
            
            _logger.LogInformation("Removed YouTube channel tracking: {ChannelId} for Guild {GuildId}", 
                channelId, unfollowEvent.GuildId);
                
            // 廣播追蹤狀態變化事件
            await _eventService.BroadcastChannelUpdateAsync(
                channelId, 
                "Unknown", 
                new Dictionary<string, object>
                {
                    ["UpdateType"] = "FollowRemoved",
                    ["GuildId"] = unfollowEvent.GuildId,
                    ["UserId"] = unfollowEvent.UserId,
                    ["TrackingCount"] = _globalTrackingCounts.GetValueOrDefault(channelId, 0)
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process unfollow event: {Message}", message);
        }
    }

    /// <summary>
    /// 更新追蹤計數
    /// </summary>
    private async Task UpdateTrackingCountAsync(string channelId, int delta)
    {
        if (!await _trackingUpdateSemaphore.WaitAsync(5000))
        {
            _logger.LogWarning("Timeout waiting for tracking update semaphore for channel {ChannelId}", channelId);
            return;
        }

        try
        {
            var previousCount = _globalTrackingCounts.GetValueOrDefault(channelId, 0);
            var newCount = Math.Max(0, previousCount + delta);
            
            if (newCount > 0)
            {
                _globalTrackingCounts[channelId] = newCount;
                
                // 如果是新的監控目標，加入活躍清單
                if (previousCount == 0)
                {
                    _activeMonitoringTargets[channelId] = DateTime.UtcNow;
                    _logger.LogInformation("Added new monitoring target: {ChannelId}", channelId);
                }
            }
            else
            {
                // 沒有任何 Guild 追蹤此頻道，移除監控
                _globalTrackingCounts.TryRemove(channelId, out _);
                _activeMonitoringTargets.TryRemove(channelId, out _);
                _logger.LogInformation("Removed monitoring target: {ChannelId} (no more trackers)", channelId);
            }

            _logger.LogDebug("Updated tracking count for {ChannelId}: {PreviousCount} → {NewCount}", 
                channelId, previousCount, newCount);
        }
        finally
        {
            _trackingUpdateSemaphore.Release();
        }
    }

    /// <summary>
    /// 取得當前追蹤的頻道清單
    /// </summary>
    public Task<List<string>> GetTrackedChannelsAsync()
    {
        var channels = _activeMonitoringTargets.Keys.ToList();
        return Task.FromResult(channels);
    }

    /// <summary>
    /// 取得當前追蹤的直播數量
    /// </summary>
    public Task<int> GetTrackedStreamCountAsync()
    {
        return Task.FromResult(_activeMonitoringTargets.Count);
    }

    /// <summary>
    /// 檢查指定頻道是否正在被追蹤
    /// </summary>
    public bool IsChannelTracked(string channelId)
    {
        return _activeMonitoringTargets.ContainsKey(channelId);
    }

    /// <summary>
    /// 取得指定頻道的追蹤數量
    /// </summary>
    public int GetChannelTrackingCount(string channelId)
    {
        return _globalTrackingCounts.GetValueOrDefault(channelId, 0);
    }

    /// <summary>
    /// 取得追蹤統計資訊
    /// </summary>
    public Task<Dictionary<string, object>> GetTrackingStatsAsync()
    {
        var stats = new Dictionary<string, object>
        {
            ["TotalTrackedChannels"] = _activeMonitoringTargets.Count,
            ["TotalTrackingCount"] = _globalTrackingCounts.Values.Sum(),
            ["AverageTrackingPerChannel"] = _activeMonitoringTargets.Count > 0 
                ? (double)_globalTrackingCounts.Values.Sum() / _activeMonitoringTargets.Count 
                : 0.0,
            ["LastUpdateTime"] = DateTime.UtcNow,
            ["ActiveTargets"] = _activeMonitoringTargets.Keys.Take(10).ToList() // 前10個作為示例
        };

        return Task.FromResult(stats);
    }

    /// <summary>
    /// 強制重新同步追蹤清單（從資料庫重新載入）
    /// </summary>
    public async Task RefreshTrackingListAsync()
    {
        try
        {
            _logger.LogInformation("Refreshing tracking list from database...");
            await InitializeTrackingCountsAsync();
            _logger.LogInformation("Tracking list refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh tracking list");
        }
    }

    /// <summary>
    /// 清理過期的監控目標（可選，用於維護）
    /// </summary>
    public async Task CleanupExpiredTargetsAsync(TimeSpan maxAge)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.Subtract(maxAge);
            var expiredTargets = _activeMonitoringTargets
                .Where(kvp => kvp.Value < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var channelId in expiredTargets)
            {
                // 重新檢查是否還有追蹤者
                var trackingCount = await GetActualTrackingCountFromDatabaseAsync(channelId);
                
                if (trackingCount == 0)
                {
                    _activeMonitoringTargets.TryRemove(channelId, out _);
                    _globalTrackingCounts.TryRemove(channelId, out _);
                    _logger.LogInformation("Cleaned up expired monitoring target: {ChannelId}", channelId);
                }
                else
                {
                    // 更新時間戳記
                    _activeMonitoringTargets[channelId] = DateTime.UtcNow;
                    _globalTrackingCounts[channelId] = trackingCount;
                }
            }

            if (expiredTargets.Count > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired monitoring targets", expiredTargets.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired targets");
        }
    }

    /// <summary>
    /// 從資料庫獲取實際的追蹤數量
    /// </summary>
    private async Task<int> GetActualTrackingCountFromDatabaseAsync(string channelId)
    {
        try
        {
            using var db = _dbService.GetDbContext();
            return await db.YoutubeChannelSpider
                .Where(x => x.ChannelId == channelId)
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get actual tracking count for channel {ChannelId}", channelId);
            return 0;
        }
    }
}
