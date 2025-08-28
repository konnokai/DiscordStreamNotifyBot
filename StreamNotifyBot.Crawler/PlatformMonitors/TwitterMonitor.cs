using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StreamNotifyBot.Crawler.Configuration;
using StreamNotifyBot.Crawler.Models;
using StreamNotifyBot.Crawler.Services;
using System.Collections.Concurrent;
using System.Net;

namespace StreamNotifyBot.Crawler.PlatformMonitors;

/// <summary>
/// Twitter Spaces 監控器實作
/// 負責監控 Twitter Spaces 直播狀態變化
/// </summary>
public class TwitterMonitor : ITwitterMonitor
{
    private readonly ILogger<TwitterMonitor> _logger;
    private readonly TwitterConfig _config;
    private readonly ConcurrentDictionary<string, StreamData> _monitoredStreams = new();
    private readonly HashSet<string> _processedSpaceIds = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    private HttpClient? _httpClient;
    private CookieContainer? _cookieContainer;
    private bool _isRunning = false;
    private Task? _monitoringTask;
    private DateTime _lastSuccessfulCheck = DateTime.MinValue;
    private int _consecutiveFailures = 0;

    // Twitter GraphQL API 端點和配置
    private const string TwitterApiBaseUrl = "https://twitter.com";
    private const string SpacesGraphQLUrl = "https://twitter.com/i/api/graphql";
    private string? _queryId;
    private Dictionary<string, string> _featureSwitches = new();

    public TwitterMonitor(
        ILogger<TwitterMonitor> logger,
        IOptions<CrawlerConfig> config)
    {
        _logger = logger;
        _config = config.Value.Platforms.Twitter;
    }

    public string PlatformName => "Twitter";

    public event EventHandler<StreamStatusChangedEventArgs>? StreamStatusChanged;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_isRunning)
        {
            _logger.LogWarning("Twitter monitor is already running");
            return;
        }

        if (!_config.Enabled)
        {
            _logger.LogInformation("Twitter monitor is disabled in configuration");
            return;
        }

        _logger.LogInformation("Starting Twitter monitor...");

        try
        {
            // 初始化 HTTP 客戶端和 Cookie 認證
            await InitializeHttpClientAsync();

            // 獲取 GraphQL 查詢 ID 和功能開關
            await InitializeGraphQLConfigAsync();

            // 啟動監控任務
            _monitoringTask = StartMonitoringLoopAsync(_cancellationTokenSource.Token);

            _isRunning = true;
            _logger.LogInformation("Twitter monitor started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Twitter monitor");
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

        _logger.LogInformation("Stopping Twitter monitor...");

        try
        {
            // 取消監控任務
            _cancellationTokenSource.Cancel();

            // 等待監控任務完成
            if (_monitoringTask != null)
            {
                await _monitoringTask;
            }

            // 清理資源
            _httpClient?.Dispose();
            _monitoredStreams.Clear();
            _processedSpaceIds.Clear();
            _isRunning = false;
            
            _logger.LogInformation("Twitter monitor stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while stopping Twitter monitor");
        }
    }

    public async Task<PlatformMonitorStatus> GetStatusAsync()
    {
        var apiUsage = new ApiUsageInfo
        {
            // Twitter API 限制估算
            UsedQuota = _consecutiveFailures, // 使用失敗次數作為指標
            QuotaLimit = _config.RateLimitPerWindowRequests,
            QuotaResetTime = DateTime.UtcNow.AddMinutes(_config.RateLimitWindowMinutes),
            RemainingRequests = Math.Max(0, _config.RateLimitPerWindowRequests - _consecutiveFailures)
        };

        return await Task.FromResult(new PlatformMonitorStatus
        {
            PlatformName = PlatformName,
            IsHealthy = _isRunning && _httpClient != null && _consecutiveFailures < _config.MaxConcurrentRequests,
            MonitoredStreamsCount = _monitoredStreams.Count,
            LastUpdateTime = _lastSuccessfulCheck > DateTime.MinValue ? _lastSuccessfulCheck : DateTime.UtcNow,
            ErrorMessage = GetStatusErrorMessage(),
            ApiUsage = apiUsage,
            Metadata = new Dictionary<string, object>
            {
                ["http_client_initialized"] = _httpClient != null,
                ["query_id_available"] = !string.IsNullOrEmpty(_queryId),
                ["consecutive_failures"] = _consecutiveFailures,
                ["last_successful_check"] = _lastSuccessfulCheck,
                ["authentication_valid"] = await ValidateAuthenticationAsync()
            }
        });
    }

    public async Task<bool> AddStreamAsync(string streamKey, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Adding Twitter user to monitor: {StreamKey}", streamKey);

            // TODO: 從資料庫查詢 Twitter 使用者資訊
            var streamData = new StreamData
            {
                StreamKey = streamKey,
                Platform = PlatformName,
                StreamUrl = $"https://twitter.com/{streamKey}/spaces"
            };

            _monitoredStreams.TryAdd(streamKey, streamData);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add Twitter user: {StreamKey}", streamKey);
            return false;
        }
    }

    public async Task<bool> RemoveStreamAsync(string streamKey, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Removing Twitter user from monitor: {StreamKey}", streamKey);
            _monitoredStreams.TryRemove(streamKey, out _);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove Twitter user: {StreamKey}", streamKey);
            return false;
        }
    }

    public async Task<IReadOnlyList<string>> GetMonitoredStreamsAsync()
    {
        return await Task.FromResult(_monitoredStreams.Keys.ToList().AsReadOnly());
    }

    public async Task ForceCheckAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Force checking all monitored Twitter users");

        try
        {
            if (_httpClient == null || !_monitoredStreams.Any())
                return;

            var userScreenNames = _monitoredStreams.Keys.ToArray();
            await CheckSpacesStatusAsync(userScreenNames);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to force check all Twitter users");
        }
    }

    public async Task SetCredentialsAsync(string bearerToken, string consumerKey, string consumerSecret)
    {
        _logger.LogDebug("Setting Twitter API credentials");

        try
        {
            // 更新配置中的認證資訊
            // 注意：Twitter Spaces 使用 Cookie 認證，不是標準的 API 認證
            _logger.LogInformation("Twitter credentials updated (Note: Twitter Spaces uses Cookie authentication)");
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set Twitter API credentials");
            throw;
        }
    }

    public async Task<ApiUsageInfo> GetRateLimitStatusAsync()
    {
        // Twitter Spaces 使用非官方 API，沒有標準的 Rate Limit 資訊
        return await Task.FromResult(new ApiUsageInfo
        {
            UsedQuota = _consecutiveFailures,
            QuotaLimit = _config.RateLimitPerWindowRequests,
            QuotaResetTime = DateTime.UtcNow.AddMinutes(_config.RateLimitWindowMinutes),
            RemainingRequests = Math.Max(0, _config.RateLimitPerWindowRequests - _consecutiveFailures)
        });
    }

    #region Private Methods

    /// <summary>
    /// 初始化 HTTP 客戶端和 Cookie 認證
    /// </summary>
    private async Task InitializeHttpClientAsync()
    {
        if (string.IsNullOrEmpty(_config.ConsumerKey) && string.IsNullOrEmpty(_config.BearerToken))
        {
            throw new InvalidOperationException("Twitter authentication credentials are required");
        }

        // 建立 Cookie Container
        _cookieContainer = new CookieContainer();
        
        // TODO: 從配置或 Redis 加載 Twitter Cookie
        // 這裡應該加載實際的 Twitter 認證 Cookie
        AddTwitterAuthenticationCookies();

        // 建立 HTTP 客戶端
        var handler = new HttpClientHandler()
        {
            CookieContainer = _cookieContainer,
            UseCookies = true
        };

        _httpClient = new HttpClient(handler);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        _httpClient.DefaultRequestHeaders.Add("Referer", "https://twitter.com/");
        _httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

        await Task.CompletedTask;
    }

    /// <summary>
    /// 添加 Twitter 認證 Cookie
    /// </summary>
    private void AddTwitterAuthenticationCookies()
    {
        // TODO: 實作從配置載入 Twitter Cookie 邏輯
        // 這裡需要根據實際配置添加認證 Cookie
        
        if (!string.IsNullOrEmpty(_config.ConsumerKey))
        {
            _cookieContainer?.Add(new Cookie("auth_token", _config.ConsumerKey, "/", ".twitter.com"));
        }

        if (!string.IsNullOrEmpty(_config.ConsumerSecret))
        {
            _cookieContainer?.Add(new Cookie("ct0", _config.ConsumerSecret, "/", ".twitter.com"));
        }

        _logger.LogDebug("Added Twitter authentication cookies");
    }

    /// <summary>
    /// 初始化 GraphQL 配置
    /// </summary>
    private async Task InitializeGraphQLConfigAsync()
    {
        try
        {
            // TODO: 實作獲取 Twitter GraphQL Query ID 和 Feature Switches
            // 這需要解析 Twitter 的前端程式碼來獲取最新的 GraphQL 配置
            
            _queryId = "DefaultQueryId"; // 暫時使用預設值
            _featureSwitches = new Dictionary<string, string>
            {
                ["spaces_enabled"] = "true",
                ["graphql_timeline_v2_enabled"] = "true"
            };

            _logger.LogDebug("Initialized Twitter GraphQL configuration");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Twitter GraphQL configuration");
            throw;
        }
    }

    /// <summary>
    /// 驗證認證狀態
    /// </summary>
    private async Task<bool> ValidateAuthenticationAsync()
    {
        try
        {
            if (_httpClient == null) return false;

            // 嘗試訪問 Twitter API 來驗證認證
            var response = await _httpClient.GetAsync("https://twitter.com/i/api/1.1/account/verify_credentials.json");
            
            return response.IsSuccessStatusCode || response.StatusCode != HttpStatusCode.Unauthorized;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Authentication validation failed");
            return false;
        }
    }

    /// <summary>
    /// 啟動監控循環
    /// </summary>
    private async Task StartMonitoringLoopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Twitter monitoring loop");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_httpClient != null && _monitoredStreams.Any())
                {
                    var userScreenNames = _monitoredStreams.Keys.ToArray();
                    await CheckSpacesStatusAsync(userScreenNames);
                    
                    _lastSuccessfulCheck = DateTime.UtcNow;
                    _consecutiveFailures = 0;
                }

                await Task.Delay(TimeSpan.FromSeconds(_config.CheckIntervalSeconds), cancellationToken);
            }
            catch (HttpRequestException httpEx) when (httpEx.Message.Contains("429") || httpEx.Message.Contains("Too Many Requests"))
            {
                _consecutiveFailures++;
                _logger.LogWarning("Twitter API rate limit exceeded, extending delay");
                await Task.Delay(TimeSpan.FromMinutes(_config.RateLimitWindowMinutes), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Twitter monitoring loop cancelled");
                break;
            }
            catch (Exception ex)
            {
                _consecutiveFailures++;
                _logger.LogError(ex, "Error in Twitter monitoring loop (consecutive failures: {Count})", _consecutiveFailures);
                
                // 指數退避
                var delay = Math.Min(300, Math.Pow(2, Math.Min(_consecutiveFailures, 8)));
                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
            }
        }
    }

    /// <summary>
    /// 檢查 Spaces 狀態
    /// </summary>
    private async Task CheckSpacesStatusAsync(string[] userScreenNames)
    {
        try
        {
            if (_httpClient == null || string.IsNullOrEmpty(_queryId)) return;

            // 批量處理用戶，避免一次請求太多
            for (int i = 0; i < userScreenNames.Length; i += 100)
            {
                var batch = userScreenNames.Skip(i).Take(100).ToArray();
                await CheckSpacesBatchAsync(batch);
                
                // 避免 Rate Limiting
                if (i + 100 < userScreenNames.Length)
                {
                    await Task.Delay(1000);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check Twitter Spaces status");
        }
    }

    /// <summary>
    /// 批量檢查 Spaces
    /// </summary>
    private async Task CheckSpacesBatchAsync(string[] userScreenNames)
    {
        try
        {
            // TODO: 實作實際的 Twitter GraphQL API 調用
            // 這需要調用 Twitter 的非官方 GraphQL API 來獲取 Spaces 資料
            
            _logger.LogDebug("Checking Twitter Spaces for {Count} users", userScreenNames.Length);
            
            // 暫時的模擬邏輯
            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check Twitter Spaces batch");
        }
    }

    /// <summary>
    /// 從 Twitter Spaces 資料建立 StreamData
    /// </summary>
    private StreamData CreateStreamDataFromSpace(TwitterSpaceData spaceData)
    {
        return new StreamData
        {
            StreamKey = spaceData.UserScreenName,
            Platform = PlatformName,
            ChannelName = spaceData.UserName,
            Title = spaceData.SpaceTitle,
            StreamUrl = $"https://twitter.com/i/spaces/{spaceData.SpaceId}/peek",
            ThumbnailUrl = spaceData.UserProfileImageUrl,
            StartTime = spaceData.StartAt,
            Metadata = new Dictionary<string, object>
            {
                ["spaceId"] = spaceData.SpaceId,
                ["userId"] = spaceData.UserId,
                ["userScreenName"] = spaceData.UserScreenName,
                ["masterPlaylistUrl"] = spaceData.MasterPlaylistUrl ?? ""
            }
        };
    }

    /// <summary>
    /// 取得狀態錯誤訊息
    /// </summary>
    private string? GetStatusErrorMessage()
    {
        if (!_isRunning)
            return "Monitor is not running";
        
        if (_httpClient == null)
            return "HTTP client not initialized";
        
        if (string.IsNullOrEmpty(_queryId))
            return "GraphQL query ID not available";
        
        if (_consecutiveFailures >= 5)
            return $"Too many consecutive failures ({_consecutiveFailures})";
        
        return null;
    }

    #endregion
}

/// <summary>
/// Twitter Spaces 資料模型
/// </summary>
public class TwitterSpaceData
{
    public string SpaceId { get; set; } = "";
    public string SpaceTitle { get; set; } = "";
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string UserScreenName { get; set; } = "";
    public string UserProfileImageUrl { get; set; } = "";
    public DateTime? StartAt { get; set; }
    public string? MasterPlaylistUrl { get; set; }
}
