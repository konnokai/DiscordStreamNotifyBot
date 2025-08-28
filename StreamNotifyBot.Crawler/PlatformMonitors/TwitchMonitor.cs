using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StreamNotifyBot.Crawler.Configuration;
using StreamNotifyBot.Crawler.Models;
using StreamNotifyBot.Crawler.Services;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Exceptions;
using Stream = TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream;
using User = TwitchLib.Api.Helix.Models.Users.GetUsers.User;

namespace StreamNotifyBot.Crawler.PlatformMonitors;

/// <summary>
/// Twitch 監控器實作
/// 負責監控 Twitch 直播狀態變化和 EventSub 管理
/// </summary>
public class TwitchMonitor : ITwitchMonitor
{
    private readonly ILogger<TwitchMonitor> _logger;
    private readonly TwitchConfig _config;
    private readonly IDatabase _redis;
    private readonly ISubscriber _redisSub;
    private readonly ConcurrentDictionary<string, StreamData> _monitoredStreams = new();
    private readonly ConcurrentDictionary<string, Timer> _streamOfflineReminders = new();
    private readonly HashSet<string> _processedStreamIds = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    private TwitchAPI? _twitchApi;
    private bool _isRunning = false;
    private Task? _monitoringTask;
    private string? _webhookSecret;

    public TwitchMonitor(
        ILogger<TwitchMonitor> logger,
        IOptions<CrawlerConfig> config,
        IConnectionMultiplexer redis)
    {
        _logger = logger;
        _config = config.Value.Platforms.Twitch;
        _redis = redis.GetDatabase();
        _redisSub = redis.GetSubscriber();
    }

    public string PlatformName => "Twitch";

    public event EventHandler<StreamStatusChangedEventArgs>? StreamStatusChanged;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_isRunning)
        {
            _logger.LogWarning("Twitch monitor is already running");
            return;
        }

        if (!_config.Enabled)
        {
            _logger.LogInformation("Twitch monitor is disabled in configuration");
            return;
        }

        _logger.LogInformation("Starting Twitch monitor...");

        try
        {
            // 初始化 Twitch API
            await InitializeTwitchApiAsync();

            // 訂閱 Redis 事件
            await SubscribeToRedisEventsAsync();

            // 啟動監控任務
            _monitoringTask = StartMonitoringLoopAsync(_cancellationTokenSource.Token);

            _isRunning = true;
            _logger.LogInformation("Twitch monitor started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Twitch monitor");
            await StopAsync(cancellationToken);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_isRunning)
        {
            return;
        }

        _logger.LogInformation("Stopping Twitch monitor...");

        try
        {
            // 取消監控任務
            _cancellationTokenSource.Cancel();

            // 等待監控任務完成
            if (_monitoringTask != null)
            {
                await _monitoringTask;
            }

            // 清理離線提醒 Timer
            foreach (var timer in _streamOfflineReminders.Values)
            {
                timer.Dispose();
            }
            _streamOfflineReminders.Clear();

            // 取消訂閱 Redis 事件
            await UnsubscribeFromRedisEventsAsync();

            _monitoredStreams.Clear();
            _processedStreamIds.Clear();
            _isRunning = false;
            
            _logger.LogInformation("Twitch monitor stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while stopping Twitch monitor");
        }
    }

    public async Task<PlatformMonitorStatus> GetStatusAsync()
    {
        var apiUsage = new ApiUsageInfo
        {
            // TODO: 實作 Twitch API 配額監控
            UsedQuota = 0,
            QuotaLimit = 800, // Twitch API 每小時 800 請求限制
            QuotaResetTime = DateTime.UtcNow.AddHours(1)
        };

        return await Task.FromResult(new PlatformMonitorStatus
        {
            PlatformName = PlatformName,
            IsHealthy = _isRunning && _twitchApi != null,
            MonitoredStreamsCount = _monitoredStreams.Count,
            LastUpdateTime = DateTime.UtcNow,
            ErrorMessage = _isRunning ? null : "Monitor is not running",
            ApiUsage = apiUsage,
            Metadata = new Dictionary<string, object>
            {
                ["api_initialized"] = _twitchApi != null,
                ["webhook_secret_available"] = !string.IsNullOrEmpty(_webhookSecret),
                ["offline_reminders_count"] = _streamOfflineReminders.Count,
                ["processed_stream_ids_count"] = _processedStreamIds.Count
            }
        });
    }

    public async Task<bool> AddStreamAsync(string streamKey, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Adding Twitch stream to monitor: {StreamKey}", streamKey);

            // TODO: 從資料庫查詢 Twitch 使用者資訊
            var streamData = new StreamData
            {
                StreamKey = streamKey,
                Platform = PlatformName,
                StreamUrl = $"https://twitch.tv/{streamKey}"
            };

            _monitoredStreams.TryAdd(streamKey, streamData);

            // 建立 EventSub 訂閱
            if (_twitchApi != null)
            {
                await CreateEventSubSubscriptionAsync(streamKey);
            }

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add Twitch stream: {StreamKey}", streamKey);
            return false;
        }
    }

    public async Task<bool> RemoveStreamAsync(string streamKey, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Removing Twitch stream from monitor: {StreamKey}", streamKey);

            _monitoredStreams.TryRemove(streamKey, out _);

            // 清除離線提醒
            if (_streamOfflineReminders.TryRemove(streamKey, out var timer))
            {
                timer.Dispose();
            }

            // TODO: 移除 EventSub 訂閱

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove Twitch stream: {StreamKey}", streamKey);
            return false;
        }
    }

    public async Task<IReadOnlyList<string>> GetMonitoredStreamsAsync()
    {
        return await Task.FromResult(_monitoredStreams.Keys.ToList().AsReadOnly());
    }

    public async Task ForceCheckAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Force checking all monitored Twitch streams");

        try
        {
            if (_twitchApi == null || !_monitoredStreams.Any())
                return;

            var userLogins = _monitoredStreams.Keys.ToArray();
            await CheckStreamsStatusAsync(userLogins);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to force check all Twitch streams");
        }
    }

    public async Task SetCredentialsAsync(string clientId, string clientSecret)
    {
        _logger.LogDebug("Setting Twitch API credentials");

        try
        {
            _twitchApi = new TwitchAPI()
            {
                Helix =
                {
                    Settings =
                    {
                        ClientId = clientId,
                        Secret = clientSecret
                    }
                }
            };

            // 測試 API 連接
            await _twitchApi.Helix.Users.GetUsersAsync();
            _logger.LogInformation("Twitch API credentials set successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set Twitch API credentials");
            _twitchApi = null;
            throw;
        }
    }

    public async Task RefreshEventSubSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refreshing Twitch EventSub subscriptions");

        try
        {
            if (_twitchApi == null)
                return;

            // 重新建立所有監控中頻道的 EventSub 訂閱
            foreach (var streamKey in _monitoredStreams.Keys)
            {
                await CreateEventSubSubscriptionAsync(streamKey);
            }

            _logger.LogInformation("Twitch EventSub subscriptions refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh Twitch EventSub subscriptions");
        }
    }

    #region Private Methods

    /// <summary>
    /// 初始化 Twitch API
    /// </summary>
    private async Task InitializeTwitchApiAsync()
    {
        if (string.IsNullOrEmpty(_config.ClientId) || string.IsNullOrEmpty(_config.ClientSecret))
        {
            throw new InvalidOperationException("Twitch ClientId and ClientSecret are required");
        }

        // 設定 API 認證
        await SetCredentialsAsync(_config.ClientId, _config.ClientSecret);

        // 設定或獲取 Webhook Secret
        _webhookSecret = await _redis.StringGetAsync("twitch:webhook_secret");
        if (string.IsNullOrEmpty(_webhookSecret))
        {
            _webhookSecret = GenerateWebhookSecret();
            await _redis.StringSetAsync("twitch:webhook_secret", _webhookSecret);
            _logger.LogInformation("Generated new Twitch webhook secret");
        }
    }

    /// <summary>
    /// 訂閱 Redis 事件
    /// </summary>
    private async Task SubscribeToRedisEventsAsync()
    {
        // 訂閱直播離線事件
        await _redisSub.SubscribeAsync(
            RedisChannel.Literal("twitch:stream_offline"),
            OnStreamOfflineReceived);

        // 訂閱頻道更新事件
        await _redisSub.SubscribeAsync(
            RedisChannel.Literal("twitch:channel_update"),
            OnChannelUpdateReceived);

        _logger.LogDebug("Subscribed to Twitch Redis events");
    }

    /// <summary>
    /// 取消訂閱 Redis 事件
    /// </summary>
    private async Task UnsubscribeFromRedisEventsAsync()
    {
        await _redisSub.UnsubscribeAsync(RedisChannel.Literal("twitch:stream_offline"));
        await _redisSub.UnsubscribeAsync(RedisChannel.Literal("twitch:channel_update"));
        _logger.LogDebug("Unsubscribed from Twitch Redis events");
    }

    /// <summary>
    /// 啟動監控循環
    /// </summary>
    private async Task StartMonitoringLoopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Twitch monitoring loop");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_twitchApi != null && _monitoredStreams.Any())
                {
                    var userLogins = _monitoredStreams.Keys.ToArray();
                    await CheckStreamsStatusAsync(userLogins);
                }

                await Task.Delay(TimeSpan.FromSeconds(_config.CheckIntervalSeconds), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Twitch monitoring loop cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Twitch monitoring loop");
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }
    }

    /// <summary>
    /// 檢查直播狀態
    /// </summary>
    private async Task CheckStreamsStatusAsync(string[] userLogins)
    {
        try
        {
            if (_twitchApi == null) return;

            // 取得使用者資訊
            var users = await _twitchApi.Helix.Users.GetUsersAsync(logins: userLogins.ToList());
            var userIds = users.Users.Select(u => u.Id).ToArray();

            // 取得目前直播狀態
            var streams = await _twitchApi.Helix.Streams.GetStreamsAsync(userIds: userIds.ToList());

            foreach (var stream in streams.Streams)
            {
                // 檢查是否為新的直播
                if (_processedStreamIds.Contains(stream.Id))
                    continue;

                _processedStreamIds.Add(stream.Id);

                // 建立直播資料
                var streamData = CreateStreamDataFromTwitchStream(stream);

                // 觸發直播開始事件
                StreamStatusChanged?.Invoke(this, new StreamStatusChangedEventArgs
                {
                    Stream = streamData,
                    IsOnline = true,
                    Platform = PlatformName,
                    ChangeType = StreamChangeType.StreamOnline,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("Detected Twitch stream online: {UserLogin}", stream.UserLogin);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check Twitch streams status");
        }
    }

    /// <summary>
    /// 建立 EventSub 訂閱
    /// </summary>
    private async Task<bool> CreateEventSubSubscriptionAsync(string userLogin)
    {
        try
        {
            if (_twitchApi == null || string.IsNullOrEmpty(_webhookSecret))
                return false;

            // 取得使用者 ID
            var user = await _twitchApi.Helix.Users.GetUsersAsync(logins: new List<string> { userLogin });
            if (!user.Users.Any())
            {
                _logger.LogWarning("Twitch user not found: {UserLogin}", userLogin);
                return false;
            }

            var userId = user.Users[0].Id;
            var callbackUrl = $"https://{_config.WebhookCallbackUrl}/TwitchWebHooks";

            // 建立 channel.update 訂閱
            await _twitchApi.Helix.EventSub.CreateEventSubSubscriptionAsync(
                "channel.update", "2",
                new() { { "broadcaster_user_id", userId } },
                EventSubTransportMethod.Webhook,
                webhookCallback: callbackUrl,
                webhookSecret: _webhookSecret);

            // 建立 stream.offline 訂閱
            await _twitchApi.Helix.EventSub.CreateEventSubSubscriptionAsync(
                "stream.offline", "1",
                new() { { "broadcaster_user_id", userId } },
                EventSubTransportMethod.Webhook,
                webhookCallback: callbackUrl,
                webhookSecret: _webhookSecret);

            _logger.LogDebug("Created Twitch EventSub subscriptions for user: {UserLogin} ({UserId})", userLogin, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create EventSub subscription for user: {UserLogin}", userLogin);
            return false;
        }
    }

    /// <summary>
    /// 處理直播離線事件
    /// </summary>
    private void OnStreamOfflineReceived(RedisChannel channel, RedisValue message)
    {
        try
        {
            var data = JsonSerializer.Deserialize<TwitchStreamOfflineData>(message!);
            if (data == null) return;

            _logger.LogInformation("Twitch stream offline: {UserLogin} ({UserId}), waiting 3 minutes before sending offline notification", 
                data.BroadcasterUserLogin, data.BroadcasterUserId);

            // 移除舊的 Timer
            if (_streamOfflineReminders.TryRemove(data.BroadcasterUserId, out var oldTimer))
            {
                oldTimer.Dispose();
            }

            // 建立 3 分鐘延遲的離線通知
            var timer = new Timer(async _ =>
            {
                await HandleStreamOfflineAsync(data);
            }, null, TimeSpan.FromMinutes(3), Timeout.InfiniteTimeSpan);

            _streamOfflineReminders.TryAdd(data.BroadcasterUserId, timer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Twitch stream offline event");
        }
    }

    /// <summary>
    /// 處理頻道更新事件
    /// </summary>
    private void OnChannelUpdateReceived(RedisChannel channel, RedisValue message)
    {
        try
        {
            var data = JsonSerializer.Deserialize<TwitchChannelUpdateData>(message!);
            if (data == null) return;

            _logger.LogDebug("Twitch channel update: {UserLogin} - {Title}", data.BroadcasterUserLogin, data.Title);

            // TODO: 處理頻道資訊更新邏輯
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Twitch channel update event");
        }
    }

    /// <summary>
    /// 處理直播離線邏輯
    /// </summary>
    private async Task HandleStreamOfflineAsync(TwitchStreamOfflineData data)
    {
        try
        {
            // 清理 EventSub 訂閱
            var subscriptions = await _twitchApi!.Helix.EventSub.GetEventSubSubscriptionsAsync(userId: data.BroadcasterUserId);
            foreach (var subscription in subscriptions.Subscriptions)
            {
                await _twitchApi.Helix.EventSub.DeleteEventSubSubscriptionAsync(subscription.Id);
                _logger.LogDebug("Deleted Twitch EventSub: {SubscriptionId} ({Type})", subscription.Id, subscription.Type);
            }

            // 觸發直播結束事件
            if (_monitoredStreams.TryGetValue(data.BroadcasterUserLogin, out var streamData))
            {
                StreamStatusChanged?.Invoke(this, new StreamStatusChangedEventArgs
                {
                    Stream = streamData,
                    IsOnline = false,
                    Platform = PlatformName,
                    ChangeType = StreamChangeType.StreamOffline,
                    Timestamp = DateTime.UtcNow.AddMinutes(-3) // 扣除延遲時間
                });

                _logger.LogInformation("Sent Twitch stream offline notification: {UserLogin}", data.BroadcasterUserLogin);
            }

            // 清理提醒 Timer
            _streamOfflineReminders.TryRemove(data.BroadcasterUserId, out var timer);
            timer?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Twitch stream offline: {UserId}", data.BroadcasterUserId);
        }
    }

    /// <summary>
    /// 從 Twitch Stream 建立 StreamData
    /// </summary>
    private StreamData CreateStreamDataFromTwitchStream(Stream stream)
    {
        return new StreamData
        {
            StreamKey = stream.UserLogin,
            Platform = PlatformName,
            ChannelName = stream.UserName,
            Title = stream.Title,
            StreamUrl = $"https://twitch.tv/{stream.UserLogin}",
            ThumbnailUrl = stream.ThumbnailUrl?.Replace("{width}", "854").Replace("{height}", "480") ?? string.Empty,
            StartTime = stream.StartedAt,
            ViewerCount = stream.ViewerCount,
            Metadata = new Dictionary<string, object>
            {
                ["streamId"] = stream.Id,
                ["userId"] = stream.UserId,
                ["userLogin"] = stream.UserLogin,
                ["gameName"] = stream.GameName ?? "",
                ["gameId"] = stream.GameId ?? "",
                ["language"] = stream.Language ?? "",
                ["isMature"] = stream.IsMature
            }
        };
    }

    /// <summary>
    /// 產生 Webhook Secret
    /// </summary>
    private string GenerateWebhookSecret()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 64)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    #endregion
}

/// <summary>
/// Twitch 直播離線事件資料
/// </summary>
public class TwitchStreamOfflineData
{
    public string BroadcasterUserId { get; set; } = "";
    public string BroadcasterUserLogin { get; set; } = "";
    public string BroadcasterUserName { get; set; } = "";
}

/// <summary>
/// Twitch 頻道更新事件資料
/// </summary>
public class TwitchChannelUpdateData
{
    public string BroadcasterUserId { get; set; } = "";
    public string BroadcasterUserLogin { get; set; } = "";
    public string BroadcasterUserName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Language { get; set; } = "";
    public string CategoryId { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public bool IsMature { get; set; }
}
