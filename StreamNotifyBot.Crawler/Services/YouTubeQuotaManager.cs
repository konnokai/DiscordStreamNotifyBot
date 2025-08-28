using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StreamNotifyBot.Crawler.Configuration;
using StreamNotifyBot.Crawler.Models;
using System.Collections.Concurrent;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace StreamNotifyBot.Crawler.Services;

/// <summary>
/// YouTube API 配額管理器
/// 負責管理多個 API 金鑰的配額使用和輪替
/// </summary>
public class YouTubeQuotaManager
{
    private readonly ILogger<YouTubeQuotaManager> _logger;
    private readonly YouTubeConfig _config;
    
    // API 金鑰配額追蹤
    private readonly ConcurrentDictionary<string, ApiKeyInfo> _apiKeyInfos = new();
    private readonly object _keyRotationLock = new object();

    // 配額消耗常數 (基於 YouTube API 文件)
    private const int COST_VIDEOS_LIST = 1;
    private const int COST_CHANNELS_LIST = 1;
    private const int COST_SEARCH_LIST = 100;
    private const int COST_ACTIVITIES_LIST = 1;

    public YouTubeQuotaManager(ILogger<YouTubeQuotaManager> logger, IOptions<CrawlerConfig> config)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(config);

        _logger = logger;
        _config = config.Value.Platforms.YouTube;
        
        InitializeApiKeys();
    }

    /// <summary>
    /// 取得可用的 YouTube 服務實例
    /// 自動選擇配額最少的 API 金鑰
    /// </summary>
    public async Task<YouTubeService> GetAvailableServiceAsync()
    {
        var apiKeyInfo = await GetAvailableApiKeyAsync();
        
        if (apiKeyInfo == null)
        {
            throw new InvalidOperationException("No available YouTube API keys with remaining quota");
        }

        var service = new YouTubeService(new BaseClientService.Initializer
        {
            ApplicationName = "DiscordStreamBot-Crawler",
            ApiKey = apiKeyInfo.ApiKey,
        });

        _logger.LogDebug("Using API key {KeyIndex} with {RemainingQuota} quota remaining",
            apiKeyInfo.KeyIndex, _config.QuotaLimit - apiKeyInfo.UsedDailyQuota);

        return service;
    }

    /// <summary>
    /// 記錄 API 配額使用
    /// </summary>
    public void RecordQuotaUsage(string apiKey, int cost)
    {
        if (_apiKeyInfos.TryGetValue(apiKey, out var keyInfo))
        {
            lock (keyInfo.Lock)
            {
                keyInfo.UsedDailyQuota += cost;
                keyInfo.LastUsedTime = DateTime.UtcNow;
                
                // 檢查配額警告閾值
                var usagePercentage = (double)keyInfo.UsedDailyQuota / _config.QuotaLimit * 100;
                
                if (usagePercentage >= 80 && !keyInfo.WarningLogged)
                {
                    _logger.LogWarning("YouTube API 金鑰 {KeyMask} 配額使用已達 {Percentage:F1}% ({Used}/{Limit})",
                        MaskApiKey(apiKey), usagePercentage, keyInfo.UsedDailyQuota, _config.QuotaLimit);
                    keyInfo.WarningLogged = true;
                }

                if (keyInfo.UsedDailyQuota >= _config.QuotaLimit)
                {
                    _logger.LogError("YouTube API 金鑰 {KeyMask} 配額已耗盡 ({Used}/{Limit})",
                        MaskApiKey(apiKey), keyInfo.UsedDailyQuota, _config.QuotaLimit);
                    keyInfo.IsExhausted = true;
                }
            }
        }
    }

    /// <summary>
    /// 記錄 Videos.List API 調用
    /// </summary>
    public void RecordVideosListUsage(string apiKey)
    {
        RecordQuotaUsage(apiKey, COST_VIDEOS_LIST);
    }

    /// <summary>
    /// 記錄 Channels.List API 調用
    /// </summary>
    public void RecordChannelsListUsage(string apiKey)
    {
        RecordQuotaUsage(apiKey, COST_CHANNELS_LIST);
    }

    /// <summary>
    /// 記錄 Search.List API 調用
    /// </summary>
    public void RecordSearchListUsage(string apiKey)
    {
        RecordQuotaUsage(apiKey, COST_SEARCH_LIST);
    }

    /// <summary>
    /// 取得 API 配額使用情況
    /// </summary>
    public ApiUsageInfo GetApiUsageInfo()
    {
        var totalUsed = _apiKeyInfos.Values.Sum(k => k.UsedDailyQuota);
        var totalLimit = _apiKeyInfos.Count * _config.QuotaLimit;
        var availableKeys = _apiKeyInfos.Values.Count(k => !k.IsExhausted);

        return new ApiUsageInfo
        {
            UsedQuota = totalUsed,
            QuotaLimit = totalLimit,
            RemainingRequests = totalLimit - totalUsed,
            QuotaResetTime = DateTime.UtcNow.Date.AddDays(1) // 配額每日午夜 UTC 重置
        };
    }

    /// <summary>
    /// 取得詳細的配額統計資訊
    /// </summary>
    public YouTubeQuotaStatistics GetDetailedQuotaStatistics()
    {
        var keyStats = _apiKeyInfos.Values.Select(k => new ApiKeyStatistics
        {
            KeyIndex = k.KeyIndex,
            KeyMask = MaskApiKey(k.ApiKey),
            UsedQuota = k.UsedDailyQuota,
            QuotaLimit = _config.QuotaLimit,
            UsagePercentage = (double)k.UsedDailyQuota / _config.QuotaLimit * 100,
            IsExhausted = k.IsExhausted,
            LastUsedTime = k.LastUsedTime,
            QuotaResetTime = DateTime.UtcNow.Date.AddDays(1)
        }).ToList();

        return new YouTubeQuotaStatistics
        {
            TotalApiKeys = _apiKeyInfos.Count,
            AvailableApiKeys = _apiKeyInfos.Values.Count(k => !k.IsExhausted),
            ExhaustedApiKeys = _apiKeyInfos.Values.Count(k => k.IsExhausted),
            TotalQuotaUsed = _apiKeyInfos.Values.Sum(k => k.UsedDailyQuota),
            TotalQuotaLimit = _apiKeyInfos.Count * _config.QuotaLimit,
            ApiKeyStatistics = keyStats,
            LastResetTime = DateTime.UtcNow.Date,
            NextResetTime = DateTime.UtcNow.Date.AddDays(1)
        };
    }

    /// <summary>
    /// 強制重置所有 API 金鑰配額
    /// 通常用於測試或每日配額重置
    /// </summary>
    public void ResetAllQuotas()
    {
        _logger.LogInformation("Resetting all YouTube API quotas");

        foreach (var keyInfo in _apiKeyInfos.Values)
        {
            lock (keyInfo.Lock)
            {
                keyInfo.UsedDailyQuota = 0;
                keyInfo.IsExhausted = false;
                keyInfo.WarningLogged = false;
                keyInfo.LastResetTime = DateTime.UtcNow;
            }
        }

        _logger.LogInformation("Reset {Count} API key quotas", _apiKeyInfos.Count);
    }

    /// <summary>
    /// 檢查是否需要重置配額（每日重置）
    /// </summary>
    public void CheckAndResetDailyQuotas()
    {
        var now = DateTime.UtcNow;
        var shouldReset = false;

        foreach (var keyInfo in _apiKeyInfos.Values)
        {
            if (keyInfo.LastResetTime.Date < now.Date)
            {
                shouldReset = true;
                break;
            }
        }

        if (shouldReset)
        {
            _logger.LogInformation("Daily quota reset triggered");
            ResetAllQuotas();
        }
    }

    #region Private Methods

    private void InitializeApiKeys()
    {
        if (_config.ApiKeys.Count == 0)
        {
            throw new InvalidOperationException("No YouTube API keys configured");
        }

        for (int i = 0; i < _config.ApiKeys.Count; i++)
        {
            var apiKey = _config.ApiKeys[i];
            var keyInfo = new ApiKeyInfo
            {
                ApiKey = apiKey,
                KeyIndex = i,
                UsedDailyQuota = 0,
                IsExhausted = false,
                WarningLogged = false,
                LastUsedTime = DateTime.MinValue,
                LastResetTime = DateTime.UtcNow.Date,
                Lock = new object()
            };

            _apiKeyInfos.TryAdd(apiKey, keyInfo);
        }

        _logger.LogInformation("Initialized {Count} YouTube API keys", _config.ApiKeys.Count);
    }

    private Task<ApiKeyInfo?> GetAvailableApiKeyAsync()
    {
        // 檢查是否需要重置配額
        CheckAndResetDailyQuotas();

        // 尋找可用的 API 金鑰（配額未耗盡且使用量最少的）
        var availableKeys = _apiKeyInfos.Values
            .Where(k => !k.IsExhausted)
            .OrderBy(k => k.UsedDailyQuota)
            .ThenBy(k => k.LastUsedTime)
            .ToList();

        if (availableKeys.Count == 0)
        {
            _logger.LogError("All YouTube API keys have exhausted their quota");
            return Task.FromResult<ApiKeyInfo?>(null);
        }

        var selectedKey = availableKeys.First();

        // 如果所有金鑰都接近限制，輪替到使用最少的
        if (selectedKey.UsedDailyQuota >= _config.QuotaLimit * 0.9)
        {
            _logger.LogWarning("All available API keys are near quota limit, selecting least used key");
        }

        return Task.FromResult<ApiKeyInfo?>(selectedKey);
    }

    private string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey.Length < 8)
            return "****";

        return apiKey[..4] + "..." + apiKey[^4..];
    }

    #endregion

    #region Nested Classes

    private class ApiKeyInfo
    {
        public string ApiKey { get; set; } = "";
        public int KeyIndex { get; set; }
        public int UsedDailyQuota { get; set; }
        public bool IsExhausted { get; set; }
        public bool WarningLogged { get; set; }
        public DateTime LastUsedTime { get; set; }
        public DateTime LastResetTime { get; set; }
        public object Lock { get; set; } = new object();
    }

    #endregion
}

/// <summary>
/// YouTube 配額統計資訊
/// </summary>
public class YouTubeQuotaStatistics
{
    public int TotalApiKeys { get; set; }
    public int AvailableApiKeys { get; set; }
    public int ExhaustedApiKeys { get; set; }
    public int TotalQuotaUsed { get; set; }
    public int TotalQuotaLimit { get; set; }
    public double OverallUsagePercentage => TotalQuotaLimit > 0 ? (double)TotalQuotaUsed / TotalQuotaLimit * 100 : 0;
    public List<ApiKeyStatistics> ApiKeyStatistics { get; set; } = new();
    public DateTime LastResetTime { get; set; }
    public DateTime NextResetTime { get; set; }
}

/// <summary>
/// 單一 API 金鑰統計資訊
/// </summary>
public class ApiKeyStatistics
{
    public int KeyIndex { get; set; }
    public string KeyMask { get; set; } = "";
    public int UsedQuota { get; set; }
    public int QuotaLimit { get; set; }
    public double UsagePercentage { get; set; }
    public bool IsExhausted { get; set; }
    public DateTime LastUsedTime { get; set; }
    public DateTime QuotaResetTime { get; set; }
}
