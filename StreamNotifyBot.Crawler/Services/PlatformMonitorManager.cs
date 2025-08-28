using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StreamNotifyBot.Crawler.Configuration;
using StreamNotifyBot.Crawler.Models;
using System.Collections.Concurrent;

namespace StreamNotifyBot.Crawler.Services;

/// <summary>
/// 平台監控管理器
/// 負責統一管理所有平台監控器的生命週期和狀態
/// </summary>
public class PlatformMonitorManager : IHostedService, IDisposable
{
    private readonly ILogger<PlatformMonitorManager> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly CrawlerConfig _config;
    private readonly ConcurrentDictionary<string, IPlatformMonitor> _monitors = new();
    private readonly ConcurrentDictionary<string, PlatformMonitorStatus> _monitorStatuses = new();
    private readonly StreamEventBroadcaster _eventBroadcaster;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    private bool _isDisposed = false;
    private Timer? _statusCheckTimer;

    public PlatformMonitorManager(
        ILogger<PlatformMonitorManager> logger,
        IServiceProvider serviceProvider,
        IOptions<CrawlerConfig> config,
        StreamEventBroadcaster eventBroadcaster)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _config = config.Value;
        _eventBroadcaster = eventBroadcaster;
    }

    /// <summary>
    /// 取得所有註冊的監控器
    /// </summary>
    public IReadOnlyDictionary<string, IPlatformMonitor> Monitors => _monitors.AsReadOnly();

    /// <summary>
    /// 取得所有監控器的最新狀態
    /// </summary>
    public IReadOnlyDictionary<string, PlatformMonitorStatus> MonitorStatuses => _monitorStatuses.AsReadOnly();

    /// <summary>
    /// 監控器狀態變化事件
    /// </summary>
    public event EventHandler<PlatformMonitorStatus>? MonitorStatusChanged;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Platform Monitor Manager...");

        try
        {
            // 檢查錄影工具可用性
            var recordingToolsAvailable = await _eventBroadcaster.CheckRecordingToolAvailability();
            if (recordingToolsAvailable)
            {
                _logger.LogInformation("錄影工具已檢測到，可以正常錄影");
            }
            else
            {
                _logger.LogWarning("未檢測到錄影工具，請確認錄影工具是否已啟動");
            }

            // 註冊所有平台監控器
            await RegisterMonitorsAsync();

            // 並行啟動所有監控器
            var startTasks = _monitors.Values.Select(m => StartMonitorAsync(m, cancellationToken));
            await Task.WhenAll(startTasks);

            // 啟動狀態監控定時器
            _statusCheckTimer = new Timer(
                CheckMonitorStatusesCallback,
                null,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(_config.Monitoring.MonitorStatusCheckIntervalMinutes));

            _logger.LogInformation("Started {MonitorCount} platform monitors", _monitors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Platform Monitor Manager");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Platform Monitor Manager...");

        try
        {
            // 停止狀態檢查定時器
            _statusCheckTimer?.Dispose();

            // 並行停止所有監控器
            var stopTasks = _monitors.Values.Select(m => StopMonitorAsync(m, cancellationToken));
            await Task.WhenAll(stopTasks);

            // 取消後台任務
            _cancellationTokenSource.Cancel();

            _logger.LogInformation("Platform Monitor Manager stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while stopping Platform Monitor Manager");
        }
    }

    /// <summary>
    /// 取得特定平台的監控器
    /// </summary>
    /// <param name="platformName">平台名稱</param>
    /// <returns>平台監控器，如果不存在則回傳 null</returns>
    public IPlatformMonitor? GetMonitor(string platformName)
    {
        _monitors.TryGetValue(platformName, out var monitor);
        return monitor;
    }

    /// <summary>
    /// 取得特定平台的狀態
    /// </summary>
    /// <param name="platformName">平台名稱</param>
    /// <returns>平台狀態，如果不存在則回傳 null</returns>
    public PlatformMonitorStatus? GetMonitorStatus(string platformName)
    {
        _monitorStatuses.TryGetValue(platformName, out var status);
        return status;
    }

    /// <summary>
    /// 強制檢查所有平台的監控狀態
    /// </summary>
    public async Task ForceCheckAllMonitorsAsync()
    {
        _logger.LogInformation("Force checking all monitored streams across all platforms");

        var checkTasks = _monitors.Values.Select(async monitor =>
        {
            try
            {
                await monitor.ForceCheckAllAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to force check all streams for {Platform}", monitor.PlatformName);
            }
        });

        await Task.WhenAll(checkTasks);
    }

    /// <summary>
    /// 重新啟動特定平台的監控器
    /// </summary>
    /// <param name="platformName">平台名稱</param>
    public async Task<bool> RestartMonitorAsync(string platformName)
    {
        if (!_monitors.TryGetValue(platformName, out var monitor))
        {
            _logger.LogWarning("Monitor not found for platform: {Platform}", platformName);
            return false;
        }

        try
        {
            _logger.LogInformation("Restarting monitor for platform: {Platform}", platformName);
            
            await monitor.StopAsync(_cancellationTokenSource.Token);
            await monitor.StartAsync(_cancellationTokenSource.Token);
            
            _logger.LogInformation("Successfully restarted monitor for platform: {Platform}", platformName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart monitor for platform: {Platform}", platformName);
            return false;
        }
    }

    /// <summary>
    /// 取得整體服務健康狀態
    /// </summary>
    public async Task<ServiceHealth> GetOverallHealthAsync()
    {
        var healthyCount = 0;
        var totalCount = _monitors.Count;
        var unhealthyPlatforms = new List<string>();

        foreach (var (platform, monitor) in _monitors)
        {
            try
            {
                var status = await monitor.GetStatusAsync();
                if (status.IsHealthy)
                {
                    healthyCount++;
                }
                else
                {
                    unhealthyPlatforms.Add(platform);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get status for platform: {Platform}", platform);
                unhealthyPlatforms.Add(platform);
            }
        }

        var healthStatus = totalCount == 0 ? HealthStatus.Unhealthy :
                          healthyCount == totalCount ? HealthStatus.Healthy :
                          healthyCount > 0 ? HealthStatus.Degraded : HealthStatus.Unhealthy;

        return new ServiceHealth
        {
            ServiceName = "Platform Monitor Manager",
            Status = healthStatus,
            Description = $"{healthyCount}/{totalCount} platforms healthy",
            Data = new Dictionary<string, object>
            {
                ["total_platforms"] = totalCount,
                ["healthy_platforms"] = healthyCount,
                ["unhealthy_platforms"] = unhealthyPlatforms
            }
        };
    }

    #region Private Methods

    /// <summary>
    /// 註冊所有平台監控器
    /// </summary>
    private async Task RegisterMonitorsAsync()
    {
        // 註冊 YouTube 監控器
        if (_config.Platforms.YouTube.Enabled)
        {
            var youtubeMonitor = _serviceProvider.GetRequiredService<IYoutubeMonitor>();
            _monitors.TryAdd("YouTube", youtubeMonitor);
            _logger.LogDebug("Registered YouTube monitor");
        }

        // 註冊 Twitch 監控器
        if (_config.Platforms.Twitch.Enabled)
        {
            var twitchMonitor = _serviceProvider.GetRequiredService<ITwitchMonitor>();
            _monitors.TryAdd("Twitch", twitchMonitor);
            _logger.LogDebug("Registered Twitch monitor");
        }

        // 註冊 Twitter 監控器
        if (_config.Platforms.Twitter.Enabled)
        {
            var twitterMonitor = _serviceProvider.GetRequiredService<ITwitterMonitor>();
            _monitors.TryAdd("Twitter", twitterMonitor);
            _logger.LogDebug("Registered Twitter monitor");
        }

        // 註冊 TwitCasting 監控器
        if (_config.Platforms.TwitCasting.Enabled)
        {
            var twitcastingMonitor = _serviceProvider.GetRequiredService<ITwitCastingMonitor>();
            _monitors.TryAdd("TwitCasting", twitcastingMonitor);
            _logger.LogDebug("Registered TwitCasting monitor");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 啟動單一監控器
    /// </summary>
    private async Task StartMonitorAsync(IPlatformMonitor monitor, CancellationToken cancellationToken)
    {
        try
        {
            // 訂閱狀態變化事件
            monitor.StreamStatusChanged += OnStreamStatusChanged;
            
            await monitor.StartAsync(cancellationToken);
            
            _logger.LogInformation("Started {Platform} monitor", monitor.PlatformName);
            
            // 初始化狀態
            var status = await monitor.GetStatusAsync();
            _monitorStatuses.TryAdd(monitor.PlatformName, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start {Platform} monitor", monitor.PlatformName);
            
            // 記錄失敗狀態
            _monitorStatuses.TryAdd(monitor.PlatformName, new PlatformMonitorStatus
            {
                PlatformName = monitor.PlatformName,
                IsHealthy = false,
                ErrorMessage = ex.Message,
                LastUpdateTime = DateTime.UtcNow
            });
            
            // 不拋出例外，允許其他監控器繼續啟動
        }
    }

    /// <summary>
    /// 停止單一監控器
    /// </summary>
    private async Task StopMonitorAsync(IPlatformMonitor monitor, CancellationToken cancellationToken)
    {
        try
        {
            // 取消訂閱事件
            monitor.StreamStatusChanged -= OnStreamStatusChanged;
            
            await monitor.StopAsync(cancellationToken);
            
            _logger.LogDebug("Stopped {Platform} monitor", monitor.PlatformName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop {Platform} monitor", monitor.PlatformName);
        }
    }

    /// <summary>
    /// 處理直播狀態變化事件
    /// </summary>
    private async void OnStreamStatusChanged(object? sender, StreamStatusChangedEventArgs e)
    {
        try
        {
            _logger.LogInformation(
                "Stream status changed: {Platform} - {StreamKey} - {Status}",
                e.Platform, e.Stream.StreamKey, e.IsOnline ? "Online" : "Offline");

            // 廣播事件到 Redis 和 Discord
            await _eventBroadcaster.BroadcastStreamStatusChange(e.Stream, e.IsOnline);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle stream status change event");
        }
    }

    /// <summary>
    /// 定時檢查監控器狀態的回調方法
    /// </summary>
    private void CheckMonitorStatusesCallback(object? state)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await CheckMonitorStatusesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking monitor statuses");
            }
        });
    }

    /// <summary>
    /// 檢查所有監控器狀態
    /// </summary>
    private async Task CheckMonitorStatusesAsync()
    {
        foreach (var (platform, monitor) in _monitors)
        {
            try
            {
                var newStatus = await monitor.GetStatusAsync();
                
                // 更新狀態
                var previousStatus = _monitorStatuses.GetValueOrDefault(platform);
                _monitorStatuses.AddOrUpdate(platform, newStatus, (key, oldValue) => newStatus);

                // 檢查狀態是否發生變化
                if (previousStatus == null || 
                    previousStatus.IsHealthy != newStatus.IsHealthy ||
                    previousStatus.MonitoredStreamsCount != newStatus.MonitoredStreamsCount)
                {
                    MonitorStatusChanged?.Invoke(this, newStatus);
                    
                    _logger.LogDebug(
                        "Monitor status updated: {Platform} - Healthy: {IsHealthy}, Streams: {StreamCount}",
                        platform, newStatus.IsHealthy, newStatus.MonitoredStreamsCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get status for {Platform} monitor", platform);
                
                // 建立錯誤狀態
                var errorStatus = new PlatformMonitorStatus
                {
                    PlatformName = platform,
                    IsHealthy = false,
                    ErrorMessage = ex.Message,
                    LastUpdateTime = DateTime.UtcNow
                };
                
                _monitorStatuses.AddOrUpdate(platform, errorStatus, (key, oldValue) => errorStatus);
                MonitorStatusChanged?.Invoke(this, errorStatus);
            }
        }
    }

    #endregion

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _statusCheckTimer?.Dispose();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _isDisposed = true;
        }
    }
}
