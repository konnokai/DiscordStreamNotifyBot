using Microsoft.Extensions.Logging;
using StreamNotifyBot.Crawler.Models;
using StreamNotifyBot.Crawler.Services;

namespace StreamNotifyBot.Crawler.PlatformMonitors;

/// <summary>
/// YouTube 監控器暫時實作
/// 實際的監控邏輯將在 Story 2.2 中實作
/// </summary>
public class YoutubeMonitor : IYoutubeMonitor
{
    private readonly ILogger<YoutubeMonitor> _logger;
    private bool _isRunning = false;

    public YoutubeMonitor(ILogger<YoutubeMonitor> logger)
    {
        _logger = logger;
    }

    public string PlatformName => "YouTube";

    public event EventHandler<StreamStatusChangedEventArgs>? StreamStatusChanged;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting YouTube monitor (placeholder)");
        _isRunning = true;
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping YouTube monitor (placeholder)");
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

    public void SetApiKeys(IEnumerable<string> apiKeys)
    {
        _logger.LogDebug("SetApiKeys placeholder: {Count} keys", apiKeys.Count());
    }

    public async Task<ApiUsageInfo> GetApiUsageAsync()
    {
        return await Task.FromResult(new ApiUsageInfo());
    }

    public async Task RefreshWebhookSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("RefreshWebhookSubscriptions placeholder");
        await Task.CompletedTask;
    }
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
