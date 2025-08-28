using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StreamNotifyBot.Crawler.Configuration;
using StreamNotifyBot.Crawler.Models;
using System.Text.Json;

namespace StreamNotifyBot.Crawler.Services;

public class YouTubePubSubEventProcessor
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<YouTubePubSubEventProcessor> _logger;
    private readonly CrawlerConfig _config;
    private readonly YouTubeApiService _youtubeApiService;
    private readonly ISubscriber _subscriber;
    private CancellationTokenSource? _cancellationTokenSource;

    public YouTubePubSubEventProcessor(
        IConnectionMultiplexer redis,
        ILogger<YouTubePubSubEventProcessor> logger,
        IOptions<CrawlerConfig> config,
        YouTubeApiService youtubeApiService)
    {
        _redis = redis;
        _logger = logger;
        _config = config.Value;
        _youtubeApiService = youtubeApiService;
        _subscriber = _redis.GetSubscriber();
    }

    /// <summary>
    /// 開始監聽外部 Backend 發送的 YouTube PubSub 通知
    /// </summary>
    public async Task StartListeningAsync(CancellationToken cancellationToken = default)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        try
        {
            _logger.LogInformation("Starting YouTube PubSub event processor...");

            // 監聽外部 Backend 發送的 YouTube 通知事件
            await _subscriber.SubscribeAsync(RedisChannel.Literal("youtube.pubsub.notification"), async (channel, message) =>
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                    return;

                try
                {
                    await ProcessPubSubNotificationAsync(message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing YouTube PubSub notification: {Message}", message.ToString());
                }
            });

            _logger.LogInformation("YouTube PubSub event processor started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start YouTube PubSub event processor");
            throw;
        }
    }

    /// <summary>
    /// 停止監聽 PubSub 事件
    /// </summary>
    public async Task StopListeningAsync()
    {
        try
        {
            _logger.LogInformation("Stopping YouTube PubSub event processor...");
            
            _cancellationTokenSource?.Cancel();
            
            // 取消訂閱 Redis 頻道
            await _subscriber.UnsubscribeAsync(RedisChannel.Literal("youtube.pubsub.notification"));
            
            _logger.LogInformation("YouTube PubSub event processor stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping YouTube PubSub event processor");
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    /// <summary>
    /// 處理來自外部 Backend 的 PubSubHubbub 通知
    /// </summary>
    /// <param name="message">Redis 消息內容</param>
    private async Task ProcessPubSubNotificationAsync(RedisValue message)
    {
        try
        {
            if (string.IsNullOrEmpty(message))
            {
                _logger.LogWarning("Received empty YouTube PubSub notification");
                return;
            }

            // 解析來自外部 Backend 的通知資料
            var notification = JsonSerializer.Deserialize<YoutubePubSubNotification>(message!);
            if (notification == null)
            {
                _logger.LogWarning("Failed to deserialize YouTube PubSub notification: {Message}", message.ToString());
                return;
            }

            _logger.LogDebug("Processing YouTube PubSub notification: VideoId={VideoId}, ChannelId={ChannelId}, Type={NotificationType}",
                notification.VideoId, notification.ChannelId, notification.NotificationType);

            // 根據通知類型進行處理
            await ProcessVideoNotificationAsync(notification);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for YouTube PubSub notification: {Message}", message.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing YouTube PubSub notification");
        }
    }

    /// <summary>
    /// 處理影片相關的通知事件
    /// </summary>
    /// <param name="notification">PubSub 通知資料</param>
    private async Task ProcessVideoNotificationAsync(YoutubePubSubNotification notification)
    {
        try
        {
            switch (notification.NotificationType?.ToLowerInvariant())
            {
                case "published":
                case "updated":
                    await HandleVideoPublishedOrUpdatedAsync(notification);
                    break;

                case "deleted":
                    await HandleVideoDeletedAsync(notification);
                    break;

                default:
                    _logger.LogDebug("Unknown notification type: {NotificationType} for video {VideoId}",
                        notification.NotificationType, notification.VideoId);
                    // 預設情況下當作影片更新處理
                    await HandleVideoPublishedOrUpdatedAsync(notification);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling video notification for {VideoId}", notification.VideoId);
        }
    }

    /// <summary>
    /// 處理影片發布或更新事件
    /// </summary>
    private async Task HandleVideoPublishedOrUpdatedAsync(YoutubePubSubNotification notification)
    {
        try
        {
            if (string.IsNullOrEmpty(notification.VideoId))
            {
                _logger.LogWarning("VideoId is empty in PubSub notification");
                return;
            }

            _logger.LogDebug("Processing video published/updated: {VideoId}", notification.VideoId);

            // 使用 YouTube API 獲取最新的影片資訊
            var videoData = await _youtubeApiService.GetVideoAsync(notification.VideoId);
            
            if (videoData == null)
            {
                _logger.LogWarning("Failed to retrieve video data for {VideoId}", notification.VideoId);
                return;
            }

            // 檢查是否為直播相關的影片
            await ProcessLiveStreamStatusChangeAsync(videoData, notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling video published/updated for {VideoId}", notification.VideoId);
        }
    }

    /// <summary>
    /// 處理影片刪除事件
    /// </summary>
    private async Task HandleVideoDeletedAsync(YoutubePubSubNotification notification)
    {
        try
        {
            _logger.LogInformation("Video deleted: {VideoId}", notification.VideoId);
            
            // TODO: 實作影片刪除處理邏輯
            // - 移除資料庫中的影片記錄
            // - 發送刪除事件通知
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling video deleted for {VideoId}", notification.VideoId);
        }
    }

    /// <summary>
    /// 處理直播狀態變化
    /// </summary>
    private async Task ProcessLiveStreamStatusChangeAsync(dynamic videoData, YoutubePubSubNotification notification)
    {
        try
        {
            // TODO: 在後續 Task 中實作完整的直播狀態檢測邏輯
            // 這裡先記錄收到的通知，完整實作將在 Task 5 中完成

            _logger.LogDebug("Live stream status change detected for video {VideoId}, will be fully processed in monitoring system",
                notification.VideoId);

            // 暫時只記錄接收到的事件
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing live stream status change for {VideoId}", notification.VideoId);
        }
    }

    /// <summary>
    /// 檢查處理器是否正在運行
    /// </summary>
    public bool IsRunning => _cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested;

    /// <summary>
    /// 獲取處理器狀態資訊
    /// </summary>
    public async Task<Dictionary<string, object>> GetStatusAsync()
    {
        try
        {
            var subscriberCount = await _redis.GetSubscriber().PublishAsync(RedisChannel.Literal("youtube.pubsub.status"), "ping");
            
            return new Dictionary<string, object>
            {
                ["is_running"] = IsRunning,
                ["redis_connected"] = _redis.IsConnected,
                ["subscriber_count"] = subscriberCount,
                ["last_activity"] = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting YouTube PubSub processor status");
            return new Dictionary<string, object>
            {
                ["is_running"] = IsRunning,
                ["redis_connected"] = false,
                ["error"] = ex.Message
            };
        }
    }
}
