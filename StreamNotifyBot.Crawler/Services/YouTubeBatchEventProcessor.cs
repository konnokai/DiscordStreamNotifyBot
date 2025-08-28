using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StreamNotifyBot.Crawler.Configuration;
using System.Text.Json;
using System.Collections.Concurrent;

namespace StreamNotifyBot.Crawler.Services;

/// <summary>
/// YouTube 批量事件處理服務
/// 用於合併和批量發送事件，減少 Redis PubSub 負載
/// </summary>
public class YouTubeBatchEventProcessor : IDisposable
{
    private readonly ILogger<YouTubeBatchEventProcessor> _logger;
    private readonly YouTubeConfig _config;
    private readonly IConnectionMultiplexer _redis;
    private readonly ISubscriber _subscriber;
    
    // 事件緩衝區
    private readonly ConcurrentQueue<StreamEvent> _eventQueue = new();
    private readonly Timer _processingTimer;
    private readonly SemaphoreSlim _processingLock = new(1, 1);
    private bool _disposed = false;

    public YouTubeBatchEventProcessor(
        ILogger<YouTubeBatchEventProcessor> logger,
        IOptions<CrawlerConfig> config,
        IConnectionMultiplexer redis)
    {
        _logger = logger;
        _config = config.Value.Platforms.YouTube;
        _redis = redis;
        _subscriber = redis.GetSubscriber();
        
        // 設定定時處理器
        var interval = TimeSpan.FromSeconds(_config.BatchEventBufferSeconds);
        _processingTimer = new Timer(ProcessQueuedEvents, null, interval, interval);
    }

    /// <summary>
    /// 加入直播狀態變化事件到批量處理佇列
    /// </summary>
    /// <param name="videoId">影片 ID</param>
    /// <param name="channelId">頻道 ID</param>
    /// <param name="isOnline">是否上線</param>
    /// <param name="title">影片標題</param>
    public void EnqueueStreamStatusChange(string videoId, string channelId, bool isOnline, string title = "")
    {
        if (_disposed)
        {
            _logger.LogWarning("Attempted to enqueue event after disposal");
            return;
        }

        var streamEvent = new StreamEvent
        {
            VideoId = videoId,
            ChannelId = channelId,
            IsOnline = isOnline,
            Title = title,
            EventType = isOnline ? StreamEventType.StreamOnline : StreamEventType.StreamOffline,
            Timestamp = DateTime.UtcNow
        };

        _eventQueue.Enqueue(streamEvent);
        
        _logger.LogDebug("Enqueued stream status change: {VideoId} - {Status}", videoId, isOnline ? "Online" : "Offline");
    }

    /// <summary>
    /// 加入錄影請求事件到佇列
    /// </summary>
    /// <param name="videoId">影片 ID</param>
    public void EnqueueRecordingRequest(string videoId)
    {
        if (_disposed)
        {
            _logger.LogWarning("Attempted to enqueue recording request after disposal");
            return;
        }

        var recordingEvent = new StreamEvent
        {
            VideoId = videoId,
            EventType = StreamEventType.RecordingRequest,
            Timestamp = DateTime.UtcNow
        };

        _eventQueue.Enqueue(recordingEvent);
        
        _logger.LogDebug("Enqueued recording request: {VideoId}", videoId);
    }

    /// <summary>
    /// 處理佇列中的事件（定時執行）
    /// </summary>
    private async void ProcessQueuedEvents(object? state)
    {
        if (_disposed || !await _processingLock.WaitAsync(100))
        {
            return;
        }

        try
        {
            if (_eventQueue.IsEmpty)
            {
                return;
            }

            var eventsToProcess = new List<StreamEvent>();
            
            // 從佇列中取出待處理事件
            while (eventsToProcess.Count < _config.BatchEventBufferSeconds * 10 && _eventQueue.TryDequeue(out var streamEvent))
            {
                eventsToProcess.Add(streamEvent);
            }

            if (eventsToProcess.Count == 0)
            {
                return;
            }

            _logger.LogDebug("Processing {Count} queued events", eventsToProcess.Count);

            // 按事件類型分組處理
            await ProcessEventsByTypeAsync(eventsToProcess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing queued events");
        }
        finally
        {
            _processingLock.Release();
        }
    }

    /// <summary>
    /// 按事件類型分組處理事件
    /// </summary>
    private async Task ProcessEventsByTypeAsync(List<StreamEvent> events)
    {
        var eventGroups = events.GroupBy(e => e.EventType).ToList();

        foreach (var group in eventGroups)
        {
            try
            {
                switch (group.Key)
                {
                    case StreamEventType.StreamOnline:
                        await ProcessOnlineEventsAsync(group.ToList());
                        break;

                    case StreamEventType.StreamOffline:
                        await ProcessOfflineEventsAsync(group.ToList());
                        break;

                    case StreamEventType.RecordingRequest:
                        await ProcessRecordingRequestsAsync(group.ToList());
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing {EventType} events", group.Key);
            }
        }
    }

    /// <summary>
    /// 處理直播上線事件
    /// </summary>
    private async Task ProcessOnlineEventsAsync(List<StreamEvent> onlineEvents)
    {
        if (onlineEvents.Count == 0) return;

        // 去重：同一個 VideoId 只保留最新的事件
        var uniqueEvents = onlineEvents
            .GroupBy(e => e.VideoId)
            .Select(g => g.OrderByDescending(e => e.Timestamp).First())
            .ToList();

        _logger.LogInformation("Broadcasting {Count} stream online events", uniqueEvents.Count);

        // 建立批量事件資料
        var batchData = uniqueEvents.Select(e => new
        {
            VideoId = e.VideoId,
            ChannelId = e.ChannelId,
            Title = e.Title,
            Platform = "youtube"
        }).ToArray();

        var batchJson = JsonSerializer.Serialize(batchData);

        // 發送給 Discord Shard
        await _subscriber.PublishAsync(RedisChannel.Literal("streams.online"), batchJson);

        // 發送錄影請求（逐個發送）
        foreach (var streamEvent in uniqueEvents)
        {
            await _subscriber.PublishAsync(RedisChannel.Literal("youtube.record"), streamEvent.VideoId);
        }

        _logger.LogDebug("Sent {Count} online events and recording requests", uniqueEvents.Count);
    }

    /// <summary>
    /// 處理直播下線事件
    /// </summary>
    private async Task ProcessOfflineEventsAsync(List<StreamEvent> offlineEvents)
    {
        if (offlineEvents.Count == 0) return;

        // 去重：同一個 VideoId 只保留最新的事件
        var uniqueEvents = offlineEvents
            .GroupBy(e => e.VideoId)
            .Select(g => g.OrderByDescending(e => e.Timestamp).First())
            .ToList();

        _logger.LogInformation("Broadcasting {Count} stream offline events", uniqueEvents.Count);

        // 建立批量事件資料
        var batchData = uniqueEvents.Select(e => new
        {
            VideoId = e.VideoId,
            ChannelId = e.ChannelId,
            Title = e.Title,
            Platform = "youtube"
        }).ToArray();

        var batchJson = JsonSerializer.Serialize(batchData);

        // 發送給 Discord Shard
        await _subscriber.PublishAsync(RedisChannel.Literal("streams.offline"), batchJson);

        _logger.LogDebug("Sent {Count} offline events", uniqueEvents.Count);
    }

    /// <summary>
    /// 處理錄影請求事件
    /// </summary>
    private async Task ProcessRecordingRequestsAsync(List<StreamEvent> recordingEvents)
    {
        if (recordingEvents.Count == 0) return;

        // 去重錄影請求
        var uniqueVideoIds = recordingEvents.Select(e => e.VideoId).Distinct().ToList();

        _logger.LogInformation("Sending {Count} recording requests", uniqueVideoIds.Count);

        foreach (var videoId in uniqueVideoIds)
        {
            await _subscriber.PublishAsync(RedisChannel.Literal("youtube.record"), videoId);
        }

        _logger.LogDebug("Sent recording requests for {Count} videos", uniqueVideoIds.Count);
    }

    /// <summary>
    /// 強制處理佇列中的所有事件（用於停機時）
    /// </summary>
    public async Task FlushQueueAsync()
    {
        if (_disposed) return;

        await _processingLock.WaitAsync();
        try
        {
            _logger.LogInformation("Flushing event queue...");
            ProcessQueuedEvents(null);
            await Task.Delay(1000); // 等待處理完成
        }
        finally
        {
            _processingLock.Release();
        }
    }

    /// <summary>
    /// 檢查錄影工具可用性
    /// </summary>
    public async Task<bool> CheckRecordingToolAvailabilityAsync()
    {
        try
        {
            var subscriberCount = await _subscriber.PublishAsync(RedisChannel.Literal("youtube.test"), "ping");
            var isAvailable = subscriberCount > 0;
            
            if (isAvailable)
            {
                _logger.LogInformation("Recording tool is available (subscribers: {Count})", subscriberCount);
            }
            else
            {
                _logger.LogWarning("No recording tool detected, please ensure recording service is running");
            }
            
            return isAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking recording tool availability");
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        
        try
        {
            _processingTimer?.Dispose();
            
            // 處理剩餘事件
            Task.Run(async () =>
            {
                try
                {
                    await FlushQueueAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during disposal flush");
                }
            }).Wait(TimeSpan.FromSeconds(5));
            
            _processingLock?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disposal");
        }
    }
}

/// <summary>
/// 串流事件類型
/// </summary>
public enum StreamEventType
{
    StreamOnline,
    StreamOffline,
    RecordingRequest
}

/// <summary>
/// 串流事件資料
/// </summary>
public class StreamEvent
{
    public string VideoId { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public StreamEventType EventType { get; set; }
    public DateTime Timestamp { get; set; }
}
