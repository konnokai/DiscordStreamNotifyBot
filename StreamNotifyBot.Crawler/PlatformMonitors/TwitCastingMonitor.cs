using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StreamNotifyBot.Crawler.Configuration;
using StreamNotifyBot.Crawler.Models;
using StreamNotifyBot.Crawler.Services;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;
using System.Text;

namespace StreamNotifyBot.Crawler.PlatformMonitors;

/// <summary>
/// TwitCasting 監控器實作
/// 負責監控 TwitCasting 直播狀態變化和 Webhook 管理
/// </summary>
public class TwitCastingMonitor : ITwitCastingMonitor
{
    private readonly ILogger<TwitCastingMonitor> _logger;
    private readonly TwitCastingConfig _config;
    private readonly IDatabase _redis;
    private readonly ISubscriber _redisSub;
    private readonly ConcurrentDictionary<string, StreamData> _monitoredStreams = new();
    private readonly HashSet<int> _processedStreamIds = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    private HttpClient? _httpClient;
    private bool _isRunning = false;
    private Task? _webhookManagementTask;
    private Task? _categoriesRefreshTask;
    private Dictionary<string, string> _categories = new();
    private DateTime _lastApiCall = DateTime.MinValue;
    private readonly SemaphoreSlim _rateLimitSemaphore;

    // TwitCasting API 端點
    private const string TwitCastingApiBaseUrl = "https://apiv2.twitcasting.tv";

    public TwitCastingMonitor(
        ILogger<TwitCastingMonitor> logger,
        IOptions<CrawlerConfig> config,
        IConnectionMultiplexer redis)
    {
        _logger = logger;
        _config = config.Value.Platforms.TwitCasting;
        _redis = redis.GetDatabase();
        _redisSub = redis.GetSubscriber();
        _rateLimitSemaphore = new SemaphoreSlim(1, 1);
    }

    public string PlatformName => "TwitCasting";

    public event EventHandler<StreamStatusChangedEventArgs>? StreamStatusChanged;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_isRunning)
        {
            _logger.LogWarning("TwitCasting monitor is already running");
            return;
        }

        if (!_config.Enabled)
        {
            _logger.LogInformation("TwitCasting monitor is disabled in configuration");
            return;
        }

        _logger.LogInformation("Starting TwitCasting monitor...");

        try
        {
            // 初始化 HTTP 客戶端
            await InitializeHttpClientAsync();

            // 訂閱 Redis Webhook 事件
            await SubscribeToWebhookEventsAsync();

            // 啟動 Webhook 管理任務
            _webhookManagementTask = StartWebhookManagementAsync(_cancellationTokenSource.Token);

            // 啟動分類刷新任務
            _categoriesRefreshTask = StartCategoriesRefreshAsync(_cancellationTokenSource.Token);

            _isRunning = true;
            _logger.LogInformation("TwitCasting monitor started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start TwitCasting monitor");
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

        _logger.LogInformation("Stopping TwitCasting monitor...");

        try
        {
            // 取消所有任務
            _cancellationTokenSource.Cancel();

            // 等待任務完成
            var tasks = new List<Task?> { _webhookManagementTask, _categoriesRefreshTask }
                .Where(t => t != null).Cast<Task>().ToArray();

            if (tasks.Length > 0)
            {
                await Task.WhenAll(tasks);
            }

            // 取消訂閱 Redis 事件
            await UnsubscribeFromWebhookEventsAsync();

            // 清理資源
            _httpClient?.Dispose();
            _rateLimitSemaphore.Dispose();
            _monitoredStreams.Clear();
            _processedStreamIds.Clear();
            _isRunning = false;
            
            _logger.LogInformation("TwitCasting monitor stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while stopping TwitCasting monitor");
        }
    }

    public async Task<PlatformMonitorStatus> GetStatusAsync()
    {
        var currentHour = DateTime.UtcNow.Hour;
        var usedQuota = Math.Max(0, (_processedStreamIds.Count / (currentHour + 1))); // 估算每小時使用量

        var apiUsage = new ApiUsageInfo
        {
            UsedQuota = usedQuota,
            QuotaLimit = _config.RateLimitPerHourRequests,
            QuotaResetTime = DateTime.UtcNow.AddHours(1).Date.AddHours(currentHour + 1),
            RemainingRequests = Math.Max(0, _config.RateLimitPerHourRequests - usedQuota)
        };

        return await Task.FromResult(new PlatformMonitorStatus
        {
            PlatformName = PlatformName,
            IsHealthy = _isRunning && _httpClient != null,
            MonitoredStreamsCount = _monitoredStreams.Count,
            LastUpdateTime = DateTime.UtcNow,
            ErrorMessage = _isRunning ? null : "Monitor is not running",
            ApiUsage = apiUsage,
            Metadata = new Dictionary<string, object>
            {
                ["http_client_initialized"] = _httpClient != null,
                ["categories_loaded"] = _categories.Count > 0,
                ["processed_stream_ids_count"] = _processedStreamIds.Count,
                ["webhook_management_running"] = _webhookManagementTask?.Status == TaskStatus.Running,
                ["categories_refresh_running"] = _categoriesRefreshTask?.Status == TaskStatus.Running
            }
        });
    }

    public async Task<bool> AddStreamAsync(string streamKey, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Adding TwitCasting channel to monitor: {StreamKey}", streamKey);

            // TODO: 從資料庫查詢 TwitCasting 頻道資訊
            var streamData = new StreamData
            {
                StreamKey = streamKey,
                Platform = PlatformName,
                StreamUrl = $"https://twitcasting.tv/{streamKey}"
            };

            _monitoredStreams.TryAdd(streamKey, streamData);

            // 註冊 Webhook
            if (_httpClient != null)
            {
                await RegisterWebhookAsync(streamKey);
            }

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add TwitCasting channel: {StreamKey}", streamKey);
            return false;
        }
    }

    public async Task<bool> RemoveStreamAsync(string streamKey, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Removing TwitCasting channel from monitor: {StreamKey}", streamKey);

            _monitoredStreams.TryRemove(streamKey, out _);

            // 移除 Webhook
            if (_httpClient != null)
            {
                await RemoveWebhookAsync(streamKey);
            }

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove TwitCasting channel: {StreamKey}", streamKey);
            return false;
        }
    }

    public async Task<IReadOnlyList<string>> GetMonitoredStreamsAsync()
    {
        return await Task.FromResult(_monitoredStreams.Keys.ToList().AsReadOnly());
    }

    public async Task ForceCheckAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Force checking all monitored TwitCasting channels");

        try
        {
            // TwitCasting 主要依賴 Webhook，強制檢查主要是驗證 Webhook 狀態
            await RefreshWebhookRegistrationsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to force check all TwitCasting channels");
        }
    }

    public async Task SetCredentialsAsync(string clientId, string clientSecret)
    {
        _logger.LogDebug("Setting TwitCasting API credentials");

        try
        {
            // 重新建立 HTTP 客戶端
            _httpClient?.Dispose();
            await InitializeHttpClientWithCredentialsAsync(clientId, clientSecret);
            
            _logger.LogInformation("TwitCasting API credentials set successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set TwitCasting API credentials");
            _httpClient = null;
            throw;
        }
    }

    #region Private Methods

    /// <summary>
    /// 初始化 HTTP 客戶端
    /// </summary>
    private async Task InitializeHttpClientAsync()
    {
        if (string.IsNullOrEmpty(_config.ClientId) || string.IsNullOrEmpty(_config.ClientSecret))
        {
            throw new InvalidOperationException("TwitCasting ClientId and ClientSecret are required");
        }

        await InitializeHttpClientWithCredentialsAsync(_config.ClientId, _config.ClientSecret);
    }

    /// <summary>
    /// 使用指定認證初始化 HTTP 客戶端
    /// </summary>
    private async Task InitializeHttpClientWithCredentialsAsync(string clientId, string clientSecret)
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(TwitCastingApiBaseUrl);

        // 設定基本認證
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "DiscordStreamNotifyBot-Crawler/1.0");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // 測試 API 連接
        try
        {
            await GetCategoriesAsync();
            _logger.LogInformation("TwitCasting API connection verified");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify TwitCasting API connection");
            throw;
        }
    }

    /// <summary>
    /// 訂閱 Webhook 事件
    /// </summary>
    private async Task SubscribeToWebhookEventsAsync()
    {
        await _redisSub.SubscribeAsync(
            RedisChannel.Literal("twitcasting.pubsub.startlive"),
            OnWebhookEventReceived);

        _logger.LogDebug("Subscribed to TwitCasting webhook events");
    }

    /// <summary>
    /// 取消訂閱 Webhook 事件
    /// </summary>
    private async Task UnsubscribeFromWebhookEventsAsync()
    {
        await _redisSub.UnsubscribeAsync(RedisChannel.Literal("twitcasting.pubsub.startlive"));
        _logger.LogDebug("Unsubscribed from TwitCasting webhook events");
    }

    /// <summary>
    /// 處理 Webhook 事件
    /// </summary>
    private void OnWebhookEventReceived(RedisChannel channel, RedisValue message)
    {
        try
        {
            var webhookData = JsonSerializer.Deserialize<TwitCastingWebhookData>(message!);
            if (webhookData?.Movie == null) return;

            var streamId = int.Parse(webhookData.Movie.Id);

            // 檢查是否已經處理過此直播
            if (_processedStreamIds.Contains(streamId))
            {
                _logger.LogWarning("TwitCasting duplicate stream notification: {StreamId} - {Title}", 
                    streamId, webhookData.Movie.Title);
                return;
            }

            _processedStreamIds.Add(streamId);

            _logger.LogInformation("TwitCasting stream started: {ChannelId} - {Title}", 
                webhookData.Broadcaster.Id, webhookData.Movie.Title);

            // 建立直播資料
            var streamData = CreateStreamDataFromWebhook(webhookData);

            // 觸發直播開始事件
            StreamStatusChanged?.Invoke(this, new StreamStatusChangedEventArgs
            {
                Stream = streamData,
                IsOnline = true,
                Platform = PlatformName,
                ChangeType = StreamChangeType.StreamOnline,
                Timestamp = UnixTimeStampToDateTime(webhookData.Movie.Created)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing TwitCasting webhook event");
        }
    }

    /// <summary>
    /// 啟動 Webhook 管理任務
    /// </summary>
    private async Task StartWebhookManagementAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting TwitCasting webhook management");

        // 初始延遲
        await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RefreshWebhookRegistrationsAsync();
                await Task.Delay(TimeSpan.FromMinutes(15), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("TwitCasting webhook management cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TwitCasting webhook management");
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }
    }

    /// <summary>
    /// 啟動分類刷新任務
    /// </summary>
    private async Task StartCategoriesRefreshAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting TwitCasting categories refresh");

        // 初始載入
        await RefreshCategoriesAsync();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(30), cancellationToken);
                await RefreshCategoriesAsync();
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("TwitCasting categories refresh cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TwitCasting categories refresh");
                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
            }
        }
    }

    /// <summary>
    /// 刷新 Webhook 註冊
    /// </summary>
    private async Task RefreshWebhookRegistrationsAsync()
    {
        if (_httpClient == null) return;

        try
        {
            await ApplyRateLimitAsync();

            // 取得已註冊的 Webhook
            var registeredWebhooks = await GetRegisteredWebhooksAsync();
            var registeredChannelIds = registeredWebhooks.Select(w => w.UserId).ToHashSet();

            // 需要監控的頻道
            var monitoredChannelIds = _monitoredStreams.Keys.ToHashSet();

            // 註冊缺少的 Webhook
            foreach (var channelId in monitoredChannelIds.Except(registeredChannelIds))
            {
                await RegisterWebhookAsync(channelId);
                _logger.LogInformation("Registered TwitCasting webhook: {ChannelId}", channelId);
            }

            // 移除多餘的 Webhook
            foreach (var channelId in registeredChannelIds.Except(monitoredChannelIds))
            {
                await RemoveWebhookAsync(channelId);
                _logger.LogInformation("Removed TwitCasting webhook: {ChannelId}", channelId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh TwitCasting webhook registrations");
        }
    }

    /// <summary>
    /// 刷新分類資料
    /// </summary>
    private async Task RefreshCategoriesAsync()
    {
        try
        {
            var categories = await GetCategoriesAsync();
            _categories = categories.ToDictionary(c => c.Id, c => c.Name);
            _logger.LogDebug("Refreshed TwitCasting categories: {Count} categories", _categories.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh TwitCasting categories");
        }
    }

    /// <summary>
    /// 取得分類清單
    /// </summary>
    private async Task<List<TwitCastingCategory>> GetCategoriesAsync()
    {
        if (_httpClient == null) return new List<TwitCastingCategory>();

        await ApplyRateLimitAsync();

        var response = await _httpClient.GetAsync("/categories");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TwitCastingCategoriesResponse>(json);

        return result?.Categories ?? new List<TwitCastingCategory>();
    }

    /// <summary>
    /// 取得已註冊的 Webhook 清單
    /// </summary>
    private async Task<List<TwitCastingWebhook>> GetRegisteredWebhooksAsync()
    {
        if (_httpClient == null) return new List<TwitCastingWebhook>();

        await ApplyRateLimitAsync();

        var response = await _httpClient.GetAsync("/webhooks");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TwitCastingWebhooksResponse>(json);

        return result?.Webhooks ?? new List<TwitCastingWebhook>();
    }

    /// <summary>
    /// 註冊 Webhook
    /// </summary>
    private async Task RegisterWebhookAsync(string channelId)
    {
        if (_httpClient == null) return;

        await ApplyRateLimitAsync();

        var payload = new
        {
            user_id = channelId,
            events = new[] { "livestart" }
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/webhooks", content);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogDebug("Successfully registered TwitCasting webhook for channel: {ChannelId}", channelId);
        }
        else
        {
            _logger.LogWarning("Failed to register TwitCasting webhook for channel: {ChannelId}, Status: {Status}", 
                channelId, response.StatusCode);
        }
    }

    /// <summary>
    /// 移除 Webhook
    /// </summary>
    private async Task RemoveWebhookAsync(string channelId)
    {
        if (_httpClient == null) return;

        await ApplyRateLimitAsync();

        var response = await _httpClient.DeleteAsync($"/webhooks/{channelId}");

        if (response.IsSuccessStatusCode)
        {
            _logger.LogDebug("Successfully removed TwitCasting webhook for channel: {ChannelId}", channelId);
        }
        else
        {
            _logger.LogWarning("Failed to remove TwitCasting webhook for channel: {ChannelId}, Status: {Status}", 
                channelId, response.StatusCode);
        }
    }

    /// <summary>
    /// 應用 Rate Limit
    /// </summary>
    private async Task ApplyRateLimitAsync()
    {
        await _rateLimitSemaphore.WaitAsync();
        try
        {
            var elapsed = DateTime.UtcNow - _lastApiCall;
            var minInterval = TimeSpan.FromMilliseconds(_config.RateLimitDelayMs);

            if (elapsed < minInterval)
            {
                var delay = minInterval - elapsed;
                await Task.Delay(delay);
            }

            _lastApiCall = DateTime.UtcNow;
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    /// <summary>
    /// 從 Webhook 資料建立 StreamData
    /// </summary>
    private StreamData CreateStreamDataFromWebhook(TwitCastingWebhookData webhookData)
    {
        var categoryName = _categories.GetValueOrDefault(webhookData.Movie.Category, "未知分類");

        return new StreamData
        {
            StreamKey = webhookData.Broadcaster.Id,
            Platform = PlatformName,
            ChannelName = webhookData.Broadcaster.Name,
            Title = webhookData.Movie.Title ?? "無標題",
            StreamUrl = $"https://twitcasting.tv/{webhookData.Broadcaster.Id}/movie/{webhookData.Movie.Id}",
            ThumbnailUrl = webhookData.Movie.LargeThumbnail,
            StartTime = UnixTimeStampToDateTime(webhookData.Movie.Created),
            Metadata = new Dictionary<string, object>
            {
                ["movieId"] = webhookData.Movie.Id,
                ["channelId"] = webhookData.Broadcaster.Id,
                ["subtitle"] = webhookData.Movie.Subtitle ?? "",
                ["category"] = categoryName,
                ["isProtected"] = webhookData.Movie.IsProtected,
                ["isPrivate"] = webhookData.Movie.IsProtected
            }
        };
    }

    /// <summary>
    /// Unix 時間戳轉換為 DateTime
    /// </summary>
    private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp).DateTime;
    }

    #endregion
}

#region Data Models

/// <summary>
/// TwitCasting Webhook 資料模型
/// </summary>
public class TwitCastingWebhookData
{
    [JsonPropertyName("broadcaster")]
    public TwitCastingBroadcaster Broadcaster { get; set; } = new();

    [JsonPropertyName("movie")]
    public TwitCastingMovie Movie { get; set; } = new();
}

/// <summary>
/// TwitCasting 廣播者資料模型
/// </summary>
public class TwitCastingBroadcaster
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}

/// <summary>
/// TwitCasting 影片資料模型
/// </summary>
public class TwitCastingMovie
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("subtitle")]
    public string? Subtitle { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("large_thumbnail")]
    public string LargeThumbnail { get; set; } = "";

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("is_protected")]
    public bool IsProtected { get; set; }
}

/// <summary>
/// TwitCasting 分類資料模型
/// </summary>
public class TwitCastingCategory
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}

/// <summary>
/// TwitCasting 分類回應模型
/// </summary>
public class TwitCastingCategoriesResponse
{
    [JsonPropertyName("categories")]
    public List<TwitCastingCategory> Categories { get; set; } = new();
}

/// <summary>
/// TwitCasting Webhook 資料模型
/// </summary>
public class TwitCastingWebhook
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = "";

    [JsonPropertyName("events")]
    public List<string> Events { get; set; } = new();
}

/// <summary>
/// TwitCasting Webhook 回應模型
/// </summary>
public class TwitCastingWebhooksResponse
{
    [JsonPropertyName("webhooks")]
    public List<TwitCastingWebhook> Webhooks { get; set; } = new();
}

#endregion
