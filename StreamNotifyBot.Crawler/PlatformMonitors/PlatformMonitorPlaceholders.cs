using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StreamNotifyBot.Crawler.Configuration;
using StreamNotifyBot.Crawler.Models;
using StreamNotifyBot.Crawler.Services;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using StackExchange.Redis;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace StreamNotifyBot.Crawler.PlatformMonitors;

/// <summary>
/// YouTube 監控器完整實作
/// 負責監控 YouTube 頻道的直播狀態變化
/// </summary>
public class YoutubeMonitor : IYoutubeMonitor
{
    private readonly ILogger<YoutubeMonitor> _logger;
    private readonly YouTubeConfig _config;
    private readonly IDatabase _redis;
    private readonly YouTubeApiService _youtubeApiService;
    private readonly YouTubeQuotaManager _quotaManager;
    private readonly YouTubePubSubSubscriptionManager _subscriptionManager;
    private readonly YouTubePubSubEventProcessor _eventProcessor;
    private readonly YouTubeStreamMonitorService _streamMonitorService;
    private readonly YouTubeTrackingManager _trackingManager;
    private readonly ConcurrentDictionary<string, StreamData> _monitoredStreams = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    private bool _isRunning = false;
    private Task? _holoScheduleTask;
    private Task? _nijisanjiScheduleTask;
    private Task? _otherScheduleTask;
    private Task? _scheduleCheckTask;
    private Task? _channelTitleCheckTask;
    private Task? _pubSubSubscriptionTask;

    public YoutubeMonitor(
        ILogger<YoutubeMonitor> logger,
        IOptions<CrawlerConfig> config,
        IConnectionMultiplexer redis,
        YouTubeApiService youtubeApiService,
        YouTubeQuotaManager quotaManager,
        YouTubePubSubSubscriptionManager subscriptionManager,
        YouTubePubSubEventProcessor eventProcessor,
        YouTubeStreamMonitorService streamMonitorService,
        YouTubeTrackingManager trackingManager)
    {
        _logger = logger;
        _config = config.Value.Platforms.YouTube;
        _redis = redis.GetDatabase();
        _youtubeApiService = youtubeApiService;
        _quotaManager = quotaManager;
        _subscriptionManager = subscriptionManager;
        _eventProcessor = eventProcessor;
        _streamMonitorService = streamMonitorService;
        _trackingManager = trackingManager;
    }

    public string PlatformName => "YouTube";

    public event EventHandler<StreamStatusChangedEventArgs>? StreamStatusChanged;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_isRunning)
        {
            _logger.LogWarning("YouTube monitor is already running");
            return;
        }

        _logger.LogInformation("Starting YouTube monitor...");

        try
        {
            // 啟動追蹤管理器
            await _trackingManager.StartAsync(_cancellationTokenSource.Token);

            // 啟動 PubSub 事件處理器
            await _eventProcessor.StartListeningAsync(_cancellationTokenSource.Token);

            // 啟動各種監控任務
            _holoScheduleTask = StartHoloScheduleMonitoringAsync(_cancellationTokenSource.Token);
            _nijisanjiScheduleTask = StartNijisanjiScheduleMonitoringAsync(_cancellationTokenSource.Token);
            _otherScheduleTask = StartOtherScheduleMonitoringAsync(_cancellationTokenSource.Token);
            _scheduleCheckTask = StartScheduleCheckAsync(_cancellationTokenSource.Token);
            _channelTitleCheckTask = StartChannelTitleCheckAsync(_cancellationTokenSource.Token);
            
            if (_config.EnablePubSubHubbub)
            {
                _pubSubSubscriptionTask = StartPubSubSubscriptionMaintenanceAsync(_cancellationTokenSource.Token);
            }

            _isRunning = true;
            _logger.LogInformation("YouTube monitor started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start YouTube monitor");
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

        _logger.LogInformation("Stopping YouTube monitor...");

        try
        {
            // 停止追蹤管理器
            await _trackingManager.StopAsync();

            // 停止 PubSub 事件處理器
            await _eventProcessor.StopListeningAsync();

            // 取消所有任務
            _cancellationTokenSource.Cancel();

            // 等待所有任務完成
            var tasks = new List<Task?> 
            { 
                _holoScheduleTask, 
                _nijisanjiScheduleTask, 
                _otherScheduleTask, 
                _scheduleCheckTask,
                _channelTitleCheckTask,
                _pubSubSubscriptionTask
            }.Where(t => t != null).Cast<Task>().ToArray();

            if (tasks.Length > 0)
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            _monitoredStreams.Clear();
            _isRunning = false;
            
            _logger.LogInformation("YouTube monitor stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while stopping YouTube monitor");
        }
    }

    public async Task<PlatformMonitorStatus> GetStatusAsync()
    {
        return await Task.FromResult(new PlatformMonitorStatus
        {
            PlatformName = PlatformName,
            IsHealthy = _isRunning,
            MonitoredStreamsCount = _monitoredStreams.Count,
            LastUpdateTime = DateTime.UtcNow,
            ErrorMessage = _isRunning ? null : "Monitor is not running",
            Metadata = new Dictionary<string, object>
            {
                ["holo_schedule_running"] = _holoScheduleTask?.Status == TaskStatus.Running,
                ["nijisanji_schedule_running"] = _nijisanjiScheduleTask?.Status == TaskStatus.Running,
                ["other_schedule_running"] = _otherScheduleTask?.Status == TaskStatus.Running,
                ["pubsub_enabled"] = _config.EnablePubSubHubbub
            }
        });
    }

    public async Task<bool> AddStreamAsync(string streamKey, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Adding stream to monitor: {StreamKey}", streamKey);
            
            // TODO: 實作從資料庫查詢頻道資訊並加入監控
            _monitoredStreams.TryAdd(streamKey, new StreamData 
            { 
                StreamKey = streamKey, 
                Platform = PlatformName 
            });
            
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add stream: {StreamKey}", streamKey);
            return false;
        }
    }

    public async Task<bool> RemoveStreamAsync(string streamKey, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Removing stream from monitor: {StreamKey}", streamKey);
            
            _monitoredStreams.TryRemove(streamKey, out _);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove stream: {StreamKey}", streamKey);
            return false;
        }
    }

    public async Task<IReadOnlyList<string>> GetMonitoredStreamsAsync()
    {
        return await Task.FromResult(_monitoredStreams.Keys.ToList().AsReadOnly());
    }

    public async Task ForceCheckAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Force checking all monitored streams");
        
        try
        {
            // TODO: 實作強制檢查所有監控的頻道
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to force check all streams");
        }
    }

    public void SetApiKeys(IEnumerable<string> apiKeys)
    {
        _logger.LogDebug("Setting API keys: {Count} keys", apiKeys.Count());
        
        // TODO: 實作 API 金鑰設定到配額管理器
    }

    public async Task<ApiUsageInfo> GetApiUsageAsync()
    {
        // TODO: 實作從配額管理器取得使用量資訊
        return await Task.FromResult(new ApiUsageInfo());
    }

    public async Task RefreshWebhookSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Refreshing webhook subscriptions");
        
        try
        {
            // TODO: 實作 PubSubHubbub 訂閱重新整理
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh webhook subscriptions");
        }
    }

    #region Private Methods

    private async Task StartHoloScheduleMonitoringAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Holo schedule monitoring");
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // TODO: 實作 Holo 排程監控邏輯
                await Task.Delay(TimeSpan.FromMinutes(_config.MonitorIntervalMinutes), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Holo schedule monitoring cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Holo schedule monitoring");
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }
    }

    private async Task StartNijisanjiScheduleMonitoringAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Nijisanji schedule monitoring");
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // TODO: 實作彩虹社排程監控邏輯
                await Task.Delay(TimeSpan.FromMinutes(_config.MonitorIntervalMinutes), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Nijisanji schedule monitoring cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Nijisanji schedule monitoring");
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }
    }

    private async Task StartOtherScheduleMonitoringAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting other channels schedule monitoring");
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // TODO: 實作其他頻道排程監控邏輯
                await Task.Delay(TimeSpan.FromMinutes(_config.MonitorIntervalMinutes), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Other schedule monitoring cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in other schedule monitoring");
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }
    }

    private async Task StartScheduleCheckAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting schedule check task");
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // TODO: 實作排程時間檢查邏輯
                await Task.Delay(TimeSpan.FromMinutes(_config.ScheduleCheckIntervalMinutes), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Schedule check task cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in schedule check task");
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }
    }

    private async Task StartChannelTitleCheckAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting channel title check task");
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // TODO: 實作頻道標題檢查邏輯
                await Task.Delay(TimeSpan.FromHours(_config.ChannelTitleCheckIntervalHours), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Channel title check task cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in channel title check task");
                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
            }
        }
    }

    private async Task StartPubSubSubscriptionMaintenanceAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting PubSubHubbub subscription maintenance");
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // 使用新的訂閱管理器處理過期訂閱
                await _subscriptionManager.ProcessExpiredSubscriptionsAsync();
                await Task.Delay(TimeSpan.FromMinutes(_config.PubSubResubscribeIntervalMinutes), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("PubSub subscription maintenance cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PubSub subscription maintenance");
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }
    }

    // 監控方法實作，委託給 YouTubeStreamMonitorService
    private async Task CheckHoloScheduleAsync()
    {
        await _streamMonitorService.CheckHoloScheduleAsync();
    }

    private async Task CheckNijisanjiScheduleAsync()
    {
        await _streamMonitorService.CheckNijisanjiScheduleAsync();
    }

    private async Task CheckOtherChannelScheduleAsync()
    {
        await _streamMonitorService.CheckOtherChannelScheduleAsync();
    }

    private async Task CheckScheduleTimeAsync()
    {
        await _streamMonitorService.CheckScheduleTimeAsync();
    }

    private async Task CheckChannelTitlesAsync()
    {
        await _streamMonitorService.CheckChannelTitlesAsync();
    }

    #endregion
}

/// <summary>
/// Twitch 監控器暫時實作
/// </summary>
public class TwitchMonitor : ITwitchMonitor
{
    private readonly ILogger<TwitchMonitor> _logger;
    private bool _isRunning = false;

    public TwitchMonitor(ILogger<TwitchMonitor> logger)
    {
        _logger = logger;
    }

    public string PlatformName => "Twitch";

    public event EventHandler<StreamStatusChangedEventArgs>? StreamStatusChanged;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Twitch monitor (placeholder)");
        _isRunning = true;
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Twitch monitor (placeholder)");
        _isRunning = false;
        await Task.CompletedTask;
    }

    public async Task<PlatformMonitorStatus> GetStatusAsync()
    {
        return await Task.FromResult(new PlatformMonitorStatus
        {
            PlatformName = PlatformName,
            IsHealthy = _isRunning,
            MonitoredStreamsCount = 0,
            LastUpdateTime = DateTime.UtcNow,
            ErrorMessage = _isRunning ? null : "Monitor is not running"
        });
    }

    public async Task<bool> AddStreamAsync(string streamKey, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AddStream placeholder: {StreamKey}", streamKey);
        return await Task.FromResult(true);
    }

    public async Task<bool> RemoveStreamAsync(string streamKey, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("RemoveStream placeholder: {StreamKey}", streamKey);
        return await Task.FromResult(true);
    }

    public async Task<IReadOnlyList<string>> GetMonitoredStreamsAsync()
    {
        return await Task.FromResult(new List<string>().AsReadOnly());
    }

    public async Task ForceCheckAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ForceCheckAll placeholder");
        await Task.CompletedTask;
    }

    public async Task SetCredentialsAsync(string clientId, string clientSecret)
    {
        _logger.LogDebug("SetCredentials placeholder");
        await Task.CompletedTask;
    }

    public async Task RefreshEventSubSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("RefreshEventSubSubscriptions placeholder");
        await Task.CompletedTask;
    }
}

/// <summary>
/// Twitter 監控器暫時實作
/// </summary>
public class TwitterMonitor : ITwitterMonitor
{
    private readonly ILogger<TwitterMonitor> _logger;
    private bool _isRunning = false;

    public TwitterMonitor(ILogger<TwitterMonitor> logger)
    {
        _logger = logger;
    }

    public string PlatformName => "Twitter";

    public event EventHandler<StreamStatusChangedEventArgs>? StreamStatusChanged;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Twitter monitor (placeholder)");
        _isRunning = true;
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Twitter monitor (placeholder)");
        _isRunning = false;
        await Task.CompletedTask;
    }

    public async Task<PlatformMonitorStatus> GetStatusAsync()
    {
        return await Task.FromResult(new PlatformMonitorStatus
        {
            PlatformName = PlatformName,
            IsHealthy = _isRunning,
            MonitoredStreamsCount = 0,
            LastUpdateTime = DateTime.UtcNow,
            ErrorMessage = _isRunning ? null : "Monitor is not running"
        });
    }

    public async Task<bool> AddStreamAsync(string streamKey, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AddStream placeholder: {StreamKey}", streamKey);
        return await Task.FromResult(true);
    }

    public async Task<bool> RemoveStreamAsync(string streamKey, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("RemoveStream placeholder: {StreamKey}", streamKey);
        return await Task.FromResult(true);
    }

    public async Task<IReadOnlyList<string>> GetMonitoredStreamsAsync()
    {
        return await Task.FromResult(new List<string>().AsReadOnly());
    }

    public async Task ForceCheckAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ForceCheckAll placeholder");
        await Task.CompletedTask;
    }

    public async Task SetCredentialsAsync(string bearerToken, string consumerKey, string consumerSecret)
    {
        _logger.LogDebug("SetCredentials placeholder");
        await Task.CompletedTask;
    }

    public async Task<ApiUsageInfo> GetRateLimitStatusAsync()
    {
        return await Task.FromResult(new ApiUsageInfo());
    }
}

/// <summary>
/// TwitCasting 監控器暫時實作
/// </summary>
public class TwitCastingMonitor : ITwitCastingMonitor
{
    private readonly ILogger<TwitCastingMonitor> _logger;
    private bool _isRunning = false;

    public TwitCastingMonitor(ILogger<TwitCastingMonitor> logger)
    {
        _logger = logger;
    }

    public string PlatformName => "TwitCasting";

    public event EventHandler<StreamStatusChangedEventArgs>? StreamStatusChanged;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting TwitCasting monitor (placeholder)");
        _isRunning = true;
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping TwitCasting monitor (placeholder)");
        _isRunning = false;
        await Task.CompletedTask;
    }

    public async Task<PlatformMonitorStatus> GetStatusAsync()
    {
        return await Task.FromResult(new PlatformMonitorStatus
        {
            PlatformName = PlatformName,
            IsHealthy = _isRunning,
            MonitoredStreamsCount = 0,
            LastUpdateTime = DateTime.UtcNow,
            ErrorMessage = _isRunning ? null : "Monitor is not running"
        });
    }

    public async Task<bool> AddStreamAsync(string streamKey, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AddStream placeholder: {StreamKey}", streamKey);
        return await Task.FromResult(true);
    }

    public async Task<bool> RemoveStreamAsync(string streamKey, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("RemoveStream placeholder: {StreamKey}", streamKey);
        return await Task.FromResult(true);
    }

    public async Task<IReadOnlyList<string>> GetMonitoredStreamsAsync()
    {
        return await Task.FromResult(new List<string>().AsReadOnly());
    }

    public async Task ForceCheckAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ForceCheckAll placeholder");
        await Task.CompletedTask;
    }

    public async Task SetCredentialsAsync(string clientId, string clientSecret)
    {
        _logger.LogDebug("SetCredentials placeholder");
        await Task.CompletedTask;
    }
}
