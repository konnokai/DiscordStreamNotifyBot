using System.Text.Json;
using Microsoft.Extensions.Logging;
using StreamNotifyBot.Crawler.Configuration;
using StreamNotifyBot.Crawler.Models;
using StackExchange.Redis;

namespace StreamNotifyBot.Crawler.Services;

/// <summary>
/// YouTube 事件廣播服務，負責將監控事件透過 Redis PubSub 廣播到外部系統
/// </summary>
public class YouTubeEventService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<YouTubeEventService> _logger;
    private readonly RedisConfig _redisConfig;
    
    // 事件類型常數
    private const string STREAM_START_EVENT = "youtube.stream.start";
    private const string STREAM_END_EVENT = "youtube.stream.end";
    private const string CHANNEL_UPDATE_EVENT = "youtube.channel.update";
    private const string VIDEO_UPDATE_EVENT = "youtube.video.update";
    private const string ERROR_EVENT = "youtube.error";
    
    public YouTubeEventService(
        IConnectionMultiplexer redis,
        ILogger<YouTubeEventService> logger,
        RedisConfig redisConfig)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _redisConfig = redisConfig ?? throw new ArgumentNullException(nameof(redisConfig));
    }

    /// <summary>
    /// 廣播直播開始事件
    /// </summary>
    public async Task BroadcastStreamStartAsync(YouTubeVideoInfo videoInfo)
    {
        if (videoInfo == null)
        {
            _logger.LogWarning("Cannot broadcast stream start event with null video info");
            return;
        }

        try
        {
            var eventData = new
            {
                EventType = STREAM_START_EVENT,
                Timestamp = DateTime.UtcNow,
                VideoId = videoInfo.VideoId,
                ChannelId = videoInfo.ChannelId,
                Title = videoInfo.Title,
                ChannelTitle = videoInfo.ChannelTitle,
                Description = videoInfo.Description,
                ThumbnailUrl = videoInfo.ThumbnailUrl,
                ActualStartTime = videoInfo.ActualStartTime,
                ScheduledStartTime = videoInfo.ScheduledStartTime,
                ViewerCount = videoInfo.ViewerCount,
                IsLiveContent = videoInfo.IsLiveContent
            };

            await PublishEventAsync(STREAM_START_EVENT, eventData);
            _logger.LogInformation("Broadcasted stream start event for video {VideoId} on channel {ChannelId}", 
                videoInfo.VideoId, videoInfo.ChannelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast stream start event for video {VideoId}", videoInfo.VideoId);
        }
    }

    /// <summary>
    /// 廣播直播結束事件
    /// </summary>
    public async Task BroadcastStreamEndAsync(YouTubeVideoInfo videoInfo)
    {
        if (videoInfo == null)
        {
            _logger.LogWarning("Cannot broadcast stream end event with null video info");
            return;
        }

        try
        {
            var eventData = new
            {
                EventType = STREAM_END_EVENT,
                Timestamp = DateTime.UtcNow,
                VideoId = videoInfo.VideoId,
                ChannelId = videoInfo.ChannelId,
                Title = videoInfo.Title,
                ChannelTitle = videoInfo.ChannelTitle,
                ActualEndTime = videoInfo.ActualEndTime,
                Duration = CalculateStreamDuration(videoInfo),
                FinalViewerCount = videoInfo.ViewerCount
            };

            await PublishEventAsync(STREAM_END_EVENT, eventData);
            _logger.LogInformation("Broadcasted stream end event for video {VideoId} on channel {ChannelId}", 
                videoInfo.VideoId, videoInfo.ChannelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast stream end event for video {VideoId}", videoInfo.VideoId);
        }
    }

    /// <summary>
    /// 廣播頻道更新事件
    /// </summary>
    public async Task BroadcastChannelUpdateAsync(string channelId, string channelTitle, Dictionary<string, object>? additionalData = null)
    {
        if (string.IsNullOrEmpty(channelId))
        {
            _logger.LogWarning("Cannot broadcast channel update event with empty channel ID");
            return;
        }

        try
        {
            var eventData = new Dictionary<string, object>
            {
                ["EventType"] = CHANNEL_UPDATE_EVENT,
                ["Timestamp"] = DateTime.UtcNow,
                ["ChannelId"] = channelId,
                ["ChannelTitle"] = channelTitle ?? "Unknown"
            };

            if (additionalData != null)
            {
                foreach (var kvp in additionalData)
                {
                    eventData[kvp.Key] = kvp.Value;
                }
            }

            await PublishEventAsync(CHANNEL_UPDATE_EVENT, eventData);
            _logger.LogInformation("Broadcasted channel update event for channel {ChannelId}", channelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast channel update event for channel {ChannelId}", channelId);
        }
    }

    /// <summary>
    /// 廣播影片更新事件
    /// </summary>
    public async Task BroadcastVideoUpdateAsync(YouTubeVideoInfo videoInfo, string updateType)
    {
        if (videoInfo == null)
        {
            _logger.LogWarning("Cannot broadcast video update event with null video info");
            return;
        }

        try
        {
            var eventData = new
            {
                EventType = VIDEO_UPDATE_EVENT,
                Timestamp = DateTime.UtcNow,
                VideoId = videoInfo.VideoId,
                ChannelId = videoInfo.ChannelId,
                UpdateType = updateType,
                Title = videoInfo.Title,
                ChannelTitle = videoInfo.ChannelTitle,
                ViewerCount = videoInfo.ViewerCount,
                IsLiveContent = videoInfo.IsLiveContent,
                LiveBroadcastContent = videoInfo.LiveBroadcastContent
            };

            await PublishEventAsync(VIDEO_UPDATE_EVENT, eventData);
            _logger.LogDebug("Broadcasted video update event ({UpdateType}) for video {VideoId}", updateType, videoInfo.VideoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast video update event for video {VideoId}", videoInfo.VideoId);
        }
    }

    /// <summary>
    /// 廣播錯誤事件
    /// </summary>
    public async Task BroadcastErrorEventAsync(string errorType, string message, Dictionary<string, object>? context = null)
    {
        try
        {
            var eventData = new Dictionary<string, object>
            {
                ["EventType"] = ERROR_EVENT,
                ["Timestamp"] = DateTime.UtcNow,
                ["ErrorType"] = errorType,
                ["Message"] = message,
                ["Severity"] = "Error"
            };

            if (context != null)
            {
                eventData["Context"] = context;
            }

            await PublishEventAsync(ERROR_EVENT, eventData);
            _logger.LogWarning("Broadcasted error event: {ErrorType} - {Message}", errorType, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast error event: {ErrorType} - {Message}", errorType, message);
        }
    }

    /// <summary>
    /// 廣播批次監控統計事件
    /// </summary>
    public async Task BroadcastMonitoringStatsAsync(int channelsChecked, int videosFound, int errorsEncountered, TimeSpan duration)
    {
        try
        {
            var eventData = new
            {
                EventType = "youtube.monitoring.stats",
                Timestamp = DateTime.UtcNow,
                ChannelsChecked = channelsChecked,
                VideosFound = videosFound,
                ErrorsEncountered = errorsEncountered,
                Duration = duration.TotalMilliseconds,
                DurationFormatted = duration.ToString(@"hh\:mm\:ss\.fff")
            };

            await PublishEventAsync("youtube.monitoring.stats", eventData);
            _logger.LogInformation("Broadcasted monitoring stats: {ChannelsChecked} channels, {VideosFound} videos, {ErrorsEncountered} errors in {Duration:F2}ms", 
                channelsChecked, videosFound, errorsEncountered, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast monitoring stats");
        }
    }

    /// <summary>
    /// 核心 Redis 發布方法
    /// </summary>
    private async Task PublishEventAsync(string eventType, object eventData)
    {
        try
        {
            var subscriber = _redis.GetSubscriber();
            var json = JsonSerializer.Serialize(eventData, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var channel = RedisChannel.Literal($"{_redisConfig.KeyPrefix}:events:{eventType}");
            await subscriber.PublishAsync(channel, json);
            
            _logger.LogDebug("Published event to Redis channel {Channel}: {EventType}", channel, eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType} to Redis", eventType);
            throw;
        }
    }

    /// <summary>
    /// 計算直播時長
    /// </summary>
    private TimeSpan? CalculateStreamDuration(YouTubeVideoInfo videoInfo)
    {
        if (videoInfo.ActualStartTime.HasValue && videoInfo.ActualEndTime.HasValue)
        {
            return videoInfo.ActualEndTime.Value - videoInfo.ActualStartTime.Value;
        }
        
        return null;
    }

    /// <summary>
    /// 批次廣播多個事件（用於批次處理場景）
    /// </summary>
    public async Task BroadcastBatchEventsAsync(IEnumerable<(string EventType, object EventData)> events)
    {
        if (events == null || !events.Any())
        {
            return;
        }

        var tasks = events.Select(evt => PublishEventAsync(evt.EventType, evt.EventData));
        
        try
        {
            await Task.WhenAll(tasks);
            _logger.LogInformation("Successfully broadcasted {Count} batch events", events.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast some batch events");
        }
    }

    /// <summary>
    /// 檢查 Redis 連線狀態
    /// </summary>
    public bool IsConnected => _redis.IsConnected;

    /// <summary>
    /// 取得已發送事件統計
    /// </summary>
    public async Task<Dictionary<string, long>> GetEventStatsAsync()
    {
        try
        {
            var database = _redis.GetDatabase();
            var stats = new Dictionary<string, long>();
            
            var eventTypes = new[] 
            { 
                STREAM_START_EVENT, 
                STREAM_END_EVENT, 
                CHANNEL_UPDATE_EVENT, 
                VIDEO_UPDATE_EVENT, 
                ERROR_EVENT 
            };

            foreach (var eventType in eventTypes)
            {
                var key = $"{_redisConfig.KeyPrefix}:stats:events:{eventType}";
                var count = await database.StringGetAsync(key);
                stats[eventType] = count.HasValue ? (long)count : 0L;
            }

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get event statistics");
            return new Dictionary<string, long>();
        }
    }
}
