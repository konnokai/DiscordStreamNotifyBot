using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StreamNotifyBot.Crawler.Configuration;
using StreamNotifyBot.Crawler.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace StreamNotifyBot.Crawler.Services;

/// <summary>
/// 直播事件廣播服務
/// 負責將直播狀態變化事件廣播到 Redis PubSub，支援錄影工具相容格式
/// </summary>
public class StreamEventBroadcaster
{
    private readonly ILogger<StreamEventBroadcaster> _logger;
    private readonly IDatabase _redis;
    private readonly CrawlerConfig _config;
    private readonly JsonSerializerOptions _jsonOptions;

    public StreamEventBroadcaster(
        ILogger<StreamEventBroadcaster> logger,
        IConnectionMultiplexer redis,
        IOptions<CrawlerConfig> config)
    {
        _logger = logger;
        _config = config.Value;
        _redis = redis.GetDatabase(_config.Redis.Database);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// 廣播直播狀態變化事件
    /// 支援 Discord Shard 批量格式和錄影工具個別格式
    /// </summary>
    /// <param name="stream">直播資料</param>
    /// <param name="isOnline">是否上線</param>
    public async Task BroadcastStreamStatusChange(StreamData stream, bool isOnline)
    {
        try
        {
            // 1. 廣播給 Discord Shard (批量格式)
            await BroadcastToDiscordShard(stream, isOnline);

            // 2. 廣播給錄影工具 (個別格式，保持相容性)
            if (isOnline)
            {
                await BroadcastToRecordingTool(stream);
            }

            _logger.LogDebug(
                "Successfully broadcasted stream status change: {Platform} - {StreamKey} - {Status}",
                stream.Platform, stream.StreamKey, isOnline ? "Online" : "Offline");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to broadcast stream status change: {Platform} - {StreamKey}",
                stream.Platform, stream.StreamKey);
        }
    }

    /// <summary>
    /// 批量廣播多個直播狀態變化
    /// </summary>
    /// <param name="streams">直播狀態變化清單</param>
    public async Task BroadcastBatchStreamStatusChanges(List<StreamStatusChangedEventArgs> streams)
    {
        if (!streams.Any())
            return;

        try
        {
            // Discord Shard 批量事件
            var onlineStreams = streams.Where(s => s.IsOnline).ToList();
            var offlineStreams = streams.Where(s => !s.IsOnline).ToList();

            if (onlineStreams.Any())
            {
                var onlineMessage = new StreamStatusBroadcastMessage
                {
                    EventType = "streams.online",
                    Streams = onlineStreams,
                    IsBatch = true
                };

                await _redis.PublishAsync(RedisChannel.Literal("streams.online"), JsonSerializer.Serialize(onlineMessage, _jsonOptions));
            }

            if (offlineStreams.Any())
            {
                var offlineMessage = new StreamStatusBroadcastMessage
                {
                    EventType = "streams.offline",
                    Streams = offlineStreams,
                    IsBatch = true
                };

                await _redis.PublishAsync(RedisChannel.Literal("streams.offline"), JsonSerializer.Serialize(offlineMessage, _jsonOptions));
            }

            // 錄影工具個別事件 (僅對上線事件)
            var recordingTasks = onlineStreams.Select(async s =>
            {
                try
                {
                    await BroadcastToRecordingTool(s.Stream);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to broadcast to recording tool: {StreamKey}", s.Stream.StreamKey);
                }
            });

            await Task.WhenAll(recordingTasks);

            _logger.LogInformation(
                "Successfully broadcasted batch stream status changes: {OnlineCount} online, {OfflineCount} offline",
                onlineStreams.Count, offlineStreams.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast batch stream status changes");
        }
    }

    /// <summary>
    /// 檢查錄影工具是否可用
    /// 透過發送測試事件並檢查訂閱者數量
    /// </summary>
    /// <returns>錄影工具是否可用</returns>
    public async Task<bool> CheckRecordingToolAvailability()
    {
        try
        {
            // 發送測試事件，檢查是否有訂閱者
            var subscriberCount = await _redis.PublishAsync(RedisChannel.Literal("youtube.test"), "crawler-startup-check");
            
            if (subscriberCount > 0)
            {
                _logger.LogInformation("錄影工具已檢測到 ({SubscriberCount} 訂閱者)，可以正常錄影", subscriberCount);
                return true;
            }
            else
            {
                _logger.LogWarning("未檢測到錄影工具訂閱者，請確認錄影工具是否已啟動");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "檢測錄影工具時發生錯誤");
            return false;
        }
    }

    /// <summary>
    /// 發送自訂事件到指定頻道
    /// </summary>
    /// <param name="channel">Redis 頻道</param>
    /// <param name="message">訊息內容</param>
    public async Task<long> PublishEventAsync(string channel, string message)
    {
        try
        {
            var subscriberCount = await _redis.PublishAsync(RedisChannel.Literal(channel), message);
            _logger.LogDebug("Published event to {Channel}: {SubscriberCount} subscribers", channel, subscriberCount);
            return subscriberCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event to channel: {Channel}", channel);
            return 0;
        }
    }

    #region Private Methods

    /// <summary>
    /// 廣播給 Discord Shard (新的批量格式)
    /// </summary>
    private async Task BroadcastToDiscordShard(StreamData stream, bool isOnline)
    {
        var eventArgs = new StreamStatusChangedEventArgs
        {
            Stream = stream,
            IsOnline = isOnline,
            Platform = stream.Platform,
            ChangeType = isOnline ? StreamChangeType.StreamOnline : StreamChangeType.StreamOffline,
            Timestamp = DateTime.UtcNow
        };

        var message = new StreamStatusBroadcastMessage
        {
            EventType = isOnline ? "streams.online" : "streams.offline",
            Streams = new List<StreamStatusChangedEventArgs> { eventArgs },
            IsBatch = false
        };

        var channel = isOnline ? "streams.online" : "streams.offline";
        await _redis.PublishAsync(RedisChannel.Literal(channel), JsonSerializer.Serialize(message, _jsonOptions));
    }

    /// <summary>
    /// 廣播給錄影工具 (保持原有相容格式)
    /// </summary>
    private async Task BroadcastToRecordingTool(StreamData stream)
    {
        switch (stream.Platform.ToLower())
        {
            case "youtube":
                // 錄影工具期望的格式：videoId (字串)
                if (!string.IsNullOrEmpty(GetVideoIdFromStream(stream)))
                {
                    var videoId = GetVideoIdFromStream(stream);
                    await _redis.PublishAsync(RedisChannel.Literal("youtube.record"), videoId);
                    _logger.LogDebug("Broadcasted YouTube recording event: {VideoId}", videoId);
                }
                break;

            case "twitch":
                // 錄影工具期望的格式：userLogin (使用者名稱)
                if (!string.IsNullOrEmpty(GetUserLoginFromStream(stream)))
                {
                    var userLogin = GetUserLoginFromStream(stream);
                    await _redis.PublishAsync(RedisChannel.Literal("twitch.record"), userLogin);
                    _logger.LogDebug("Broadcasted Twitch recording event: {UserLogin}", userLogin);
                }
                break;

            case "twitter":
            case "twitcasting":
                // 這些平台目前錄影工具不支援，但保留擴展性
                _logger.LogDebug("Platform {Platform} recording not supported by recording tool", stream.Platform);
                break;

            default:
                _logger.LogWarning("Unknown platform for recording tool broadcast: {Platform}", stream.Platform);
                break;
        }
    }

    /// <summary>
    /// 從 StreamData 中提取 YouTube videoId
    /// </summary>
    private string GetVideoIdFromStream(StreamData stream)
    {
        // 優先從 Metadata 中取得
        if (stream.Metadata.TryGetValue("videoId", out var videoIdObj) && videoIdObj is string videoId)
        {
            return videoId;
        }

        // 從 StreamUrl 中解析
        if (!string.IsNullOrEmpty(stream.StreamUrl))
        {
            var uri = new Uri(stream.StreamUrl);
            
            // 處理 youtube.com/watch?v=VIDEO_ID 格式
            if (uri.Host.Contains("youtube.com") && uri.AbsolutePath == "/watch")
            {
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                return query["v"] ?? "";
            }
            
            // 處理 youtu.be/VIDEO_ID 格式
            if (uri.Host == "youtu.be")
            {
                return uri.AbsolutePath.TrimStart('/');
            }
        }

        // 最後嘗試從 StreamKey 取得 (如果 StreamKey 就是 videoId)
        return stream.StreamKey;
    }

    /// <summary>
    /// 從 StreamData 中提取 Twitch userLogin
    /// </summary>
    private string GetUserLoginFromStream(StreamData stream)
    {
        // 優先從 Metadata 中取得
        if (stream.Metadata.TryGetValue("userLogin", out var userLoginObj) && userLoginObj is string userLogin)
        {
            return userLogin;
        }

        if (stream.Metadata.TryGetValue("username", out var usernameObj) && usernameObj is string username)
        {
            return username;
        }

        // 從 StreamUrl 中解析
        if (!string.IsNullOrEmpty(stream.StreamUrl))
        {
            var uri = new Uri(stream.StreamUrl);
            
            // 處理 twitch.tv/USERNAME 格式
            if (uri.Host.Contains("twitch.tv"))
            {
                var pathParts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (pathParts.Length > 0)
                {
                    return pathParts[0];
                }
            }
        }

        // 最後嘗試從 StreamKey 取得 (如果 StreamKey 就是 userLogin)
        return stream.StreamKey;
    }

    #endregion
}
