using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StreamNotifyBot.Crawler.Configuration;
using System.Net;

namespace StreamNotifyBot.Crawler.Services;

/// <summary>
/// 統一錯誤處理服務
/// 提供平台特定的錯誤分類、重試機制和異常恢復
/// </summary>
public class ErrorHandlingService
{
    private readonly ILogger<ErrorHandlingService> _logger;
    private readonly MonitoringConfig _config;
    private readonly Dictionary<string, RetryPolicy> _retryPolicies;
    private readonly Dictionary<string, ErrorStatistics> _errorStats;

    public ErrorHandlingService(
        ILogger<ErrorHandlingService> logger,
        IOptions<CrawlerConfig> config)
    {
        _logger = logger;
        _config = config.Value.Monitoring;
        _retryPolicies = new Dictionary<string, RetryPolicy>();
        _errorStats = new Dictionary<string, ErrorStatistics>();

        InitializeRetryPolicies();
    }

    /// <summary>
    /// 執行具有重試機制的操作
    /// </summary>
    public async Task<T> ExecuteWithRetryAsync<T>(
        string platform,
        string operation,
        Func<Task<T>> func,
        CancellationToken cancellationToken = default)
    {
        var policy = GetRetryPolicy(platform);
        Exception? lastException = null;

        for (int attempt = 1; attempt <= policy.MaxRetryAttempts; attempt++)
        {
            try
            {
                _logger.LogDebug("Executing {Platform} {Operation}, attempt {Attempt}/{MaxAttempts}", 
                    platform, operation, attempt, policy.MaxRetryAttempts);

                var result = await func();
                
                if (attempt > 1)
                {
                    _logger.LogInformation("{Platform} {Operation} succeeded on attempt {Attempt}", 
                        platform, operation, attempt);
                }

                RecordSuccess(platform, operation);
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                RecordError(platform, operation, ex);

                var isRetryable = IsRetryableError(platform, ex);
                var isLastAttempt = attempt == policy.MaxRetryAttempts;

                _logger.LogWarning(ex, 
                    "{Platform} {Operation} failed on attempt {Attempt}/{MaxAttempts}. Retryable: {IsRetryable}", 
                    platform, operation, attempt, policy.MaxRetryAttempts, isRetryable);

                if (!isRetryable || isLastAttempt)
                {
                    break;
                }

                // 計算延遲時間
                var delay = policy.CalculateDelay(attempt);
                _logger.LogDebug("Waiting {Delay}ms before retry attempt {NextAttempt}", 
                    delay.TotalMilliseconds, attempt + 1);

                await Task.Delay(delay, cancellationToken);
            }
        }

        RecordFinalFailure(platform, operation, lastException!);
        _logger.LogError(lastException, 
            "{Platform} {Operation} failed after {MaxAttempts} attempts", 
            platform, operation, policy.MaxRetryAttempts);

        throw lastException!;
    }

    /// <summary>
    /// 執行不需要傳回值的操作（具有重試機制）
    /// </summary>
    public async Task ExecuteWithRetryAsync(
        string platform,
        string operation,
        Func<Task> func,
        CancellationToken cancellationToken = default)
    {
        await ExecuteWithRetryAsync(platform, operation, async () =>
        {
            await func();
            return true; // 包裝成有傳回值的方法
        }, cancellationToken);
    }

    /// <summary>
    /// 取得平台的錯誤統計資訊
    /// </summary>
    public ErrorStatistics GetErrorStatistics(string platform)
    {
        return _errorStats.GetValueOrDefault(platform, new ErrorStatistics(platform));
    }

    /// <summary>
    /// 取得所有平台的錯誤統計資訊
    /// </summary>
    public Dictionary<string, ErrorStatistics> GetAllErrorStatistics()
    {
        return new Dictionary<string, ErrorStatistics>(_errorStats);
    }

    /// <summary>
    /// 重設指定平台的錯誤統計
    /// </summary>
    public void ResetErrorStatistics(string platform)
    {
        if (_errorStats.ContainsKey(platform))
        {
            _errorStats[platform] = new ErrorStatistics(platform);
            _logger.LogInformation("Reset error statistics for platform: {Platform}", platform);
        }
    }

    /// <summary>
    /// 檢查錯誤是否可重試
    /// </summary>
    public bool IsRetryableError(string platform, Exception exception)
    {
        return platform.ToLower() switch
        {
            "youtube" => IsRetryableYouTubeError(exception),
            "twitch" => IsRetryableTwitchError(exception),
            "twitter" => IsRetryableTwitterError(exception),
            "twitcasting" => IsRetryableTwitCastingError(exception),
            _ => IsRetryableGeneralError(exception)
        };
    }

    /// <summary>
    /// 取得建議的錯誤處理策略
    /// </summary>
    public ErrorHandlingStrategy GetErrorHandlingStrategy(string platform, Exception exception)
    {
        var strategy = new ErrorHandlingStrategy
        {
            Platform = platform,
            ExceptionType = exception.GetType().Name,
            ShouldRetry = IsRetryableError(platform, exception),
            RecommendedAction = GetRecommendedAction(platform, exception),
            Severity = GetErrorSeverity(platform, exception),
            RequiresUserIntervention = RequiresUserIntervention(platform, exception)
        };

        return strategy;
    }

    #region Private Methods

    /// <summary>
    /// 初始化各平台的重試策略
    /// </summary>
    private void InitializeRetryPolicies()
    {
        // YouTube 重試策略
        _retryPolicies["youtube"] = new RetryPolicy
        {
            MaxRetryAttempts = _config.MaxRetryAttempts,
            BaseDelayMs = _config.RetryDelaySeconds * 1000,
            RetryType = RetryType.ExponentialBackoff,
            MaxDelayMs = 300000 // 最長 5 分鐘
        };

        // Twitch 重試策略
        _retryPolicies["twitch"] = new RetryPolicy
        {
            MaxRetryAttempts = _config.MaxRetryAttempts,
            BaseDelayMs = _config.RetryDelaySeconds * 1000,
            RetryType = RetryType.Linear,
            MaxDelayMs = 60000 // 最長 1 分鐘
        };

        // Twitter 重試策略 (更寬鬆，因為 API 不穩定)
        _retryPolicies["twitter"] = new RetryPolicy
        {
            MaxRetryAttempts = Math.Max(_config.MaxRetryAttempts, 5),
            BaseDelayMs = _config.RetryDelaySeconds * 1000 * 2, // 延遲更長
            RetryType = RetryType.ExponentialBackoff,
            MaxDelayMs = 600000 // 最長 10 分鐘
        };

        // TwitCasting 重試策略
        _retryPolicies["twitcasting"] = new RetryPolicy
        {
            MaxRetryAttempts = _config.MaxRetryAttempts,
            BaseDelayMs = _config.RetryDelaySeconds * 1000 * 3, // TwitCasting 需要更長延遲
            RetryType = RetryType.Linear,
            MaxDelayMs = 120000 // 最長 2 分鐘
        };

        // 通用重試策略
        _retryPolicies["general"] = new RetryPolicy
        {
            MaxRetryAttempts = _config.MaxRetryAttempts,
            BaseDelayMs = _config.RetryDelaySeconds * 1000,
            RetryType = RetryType.Linear,
            MaxDelayMs = 30000 // 最長 30 秒
        };
    }

    /// <summary>
    /// 取得指定平台的重試策略
    /// </summary>
    private RetryPolicy GetRetryPolicy(string platform)
    {
        var key = platform.ToLower();
        return _retryPolicies.GetValueOrDefault(key, _retryPolicies["general"]);
    }

    /// <summary>
    /// 記錄成功執行
    /// </summary>
    private void RecordSuccess(string platform, string operation)
    {
        if (!_errorStats.ContainsKey(platform))
            _errorStats[platform] = new ErrorStatistics(platform);

        _errorStats[platform].RecordSuccess(operation);
    }

    /// <summary>
    /// 記錄錯誤
    /// </summary>
    private void RecordError(string platform, string operation, Exception exception)
    {
        if (!_errorStats.ContainsKey(platform))
            _errorStats[platform] = new ErrorStatistics(platform);

        _errorStats[platform].RecordError(operation, exception);
    }

    /// <summary>
    /// 記錄最終失敗
    /// </summary>
    private void RecordFinalFailure(string platform, string operation, Exception exception)
    {
        if (!_errorStats.ContainsKey(platform))
            _errorStats[platform] = new ErrorStatistics(platform);

        _errorStats[platform].RecordFinalFailure(operation, exception);
    }

    #region Platform-Specific Error Handling

    /// <summary>
    /// YouTube 錯誤可重試判斷
    /// </summary>
    private bool IsRetryableYouTubeError(Exception exception)
    {
        return exception switch
        {
            HttpRequestException httpEx => CheckHttpStatusForRetry(httpEx),
            TaskCanceledException => true,
            TimeoutException => true,
            _ => false
        };
    }

    /// <summary>
    /// Twitch 錯誤可重試判斷
    /// </summary>
    private bool IsRetryableTwitchError(Exception exception)
    {
        return exception switch
        {
            HttpRequestException httpEx => CheckHttpStatusForRetry(httpEx, new[] 
            { 
                HttpStatusCode.TooManyRequests,
                HttpStatusCode.InternalServerError,
                HttpStatusCode.BadGateway,
                HttpStatusCode.ServiceUnavailable 
            }),
            TaskCanceledException => true,
            TimeoutException => true,
            _ => false
        };
    }

    /// <summary>
    /// Twitter 錯誤可重試判斷
    /// </summary>
    private bool IsRetryableTwitterError(Exception exception)
    {
        // Twitter API 比較不穩定，大部分錯誤都嘗試重試
        return exception switch
        {
            HttpRequestException => true,
            TaskCanceledException => true,
            TimeoutException => true,
            UnauthorizedAccessException => true, // Cookie 過期，可以嘗試重新認證
            _ => false
        };
    }

    /// <summary>
    /// TwitCasting 錯誤可重試判斷
    /// </summary>
    private bool IsRetryableTwitCastingError(Exception exception)
    {
        return exception switch
        {
            HttpRequestException httpEx => CheckHttpStatusForRetry(httpEx, new[] 
            { 
                HttpStatusCode.TooManyRequests,
                HttpStatusCode.InternalServerError,
                HttpStatusCode.BadGateway,
                HttpStatusCode.ServiceUnavailable,
                HttpStatusCode.GatewayTimeout
            }),
            TaskCanceledException => true,
            TimeoutException => true,
            _ => false
        };
    }

    /// <summary>
    /// 通用錯誤可重試判斷
    /// </summary>
    private bool IsRetryableGeneralError(Exception exception)
    {
        return exception switch
        {
            HttpRequestException httpEx => CheckHttpStatusForRetry(httpEx),
            TaskCanceledException => true,
            TimeoutException => true,
            _ => false
        };
    }

    /// <summary>
    /// 檢查 HTTP 狀態碼是否可重試
    /// </summary>
    private bool CheckHttpStatusForRetry(HttpRequestException httpException, HttpStatusCode[]? allowedCodes = null)
    {
        // 嘗試從 Exception.Data 中取得狀態碼
        if (httpException.Data.Contains("StatusCode") && httpException.Data["StatusCode"] is HttpStatusCode statusCode)
        {
            if (allowedCodes != null)
            {
                return allowedCodes.Contains(statusCode);
            }

            // 預設的可重試狀態碼
            return statusCode switch
            {
                HttpStatusCode.RequestTimeout => true,
                HttpStatusCode.TooManyRequests => true,
                HttpStatusCode.InternalServerError => true,
                HttpStatusCode.BadGateway => true,
                HttpStatusCode.ServiceUnavailable => true,
                HttpStatusCode.GatewayTimeout => true,
                _ => false
            };
        }

        // 如果無法取得狀態碼，預設為可重試（除非明確不可重試的情況）
        return true;
    }

    /// <summary>
    /// 取得建議的錯誤處理動作
    /// </summary>
    private string GetRecommendedAction(string platform, Exception exception)
    {
        return platform.ToLower() switch
        {
            "youtube" => GetYouTubeRecommendedAction(exception),
            "twitch" => GetTwitchRecommendedAction(exception),
            "twitter" => GetTwitterRecommendedAction(exception),
            "twitcasting" => GetTwitCastingRecommendedAction(exception),
            _ => "檢查網路連接和服務狀態"
        };
    }

    private string GetYouTubeRecommendedAction(Exception exception)
    {
        return exception switch
        {
            HttpRequestException httpEx when httpEx.Data.Contains("StatusCode") && 
                (HttpStatusCode)httpEx.Data["StatusCode"]! == HttpStatusCode.Forbidden => "檢查 YouTube API 金鑰和配額",
            HttpRequestException => "檢查 YouTube API 金鑰和網路連接",
            TaskCanceledException => "增加 YouTube API 超時時間設定",
            TimeoutException => "檢查網路連接速度",
            _ => "檢查 YouTube API 服務狀態"
        };
    }

    private string GetTwitchRecommendedAction(Exception exception)
    {
        return exception switch
        {
            HttpRequestException httpEx when httpEx.Data.Contains("StatusCode") &&
                (HttpStatusCode)httpEx.Data["StatusCode"]! == HttpStatusCode.Unauthorized => "檢查 Twitch API 認證資訊",
            HttpRequestException httpEx when httpEx.Data.Contains("StatusCode") &&
                (HttpStatusCode)httpEx.Data["StatusCode"]! == HttpStatusCode.TooManyRequests => "降低 Twitch API 調用頻率",
            TaskCanceledException => "增加 Twitch API 超時時間",
            _ => "檢查 Twitch API 服務狀態"
        };
    }

    private string GetTwitterRecommendedAction(Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException => "更新 Twitter Cookie 認證資訊",
            HttpRequestException httpEx when httpEx.Data.Contains("StatusCode") &&
                (HttpStatusCode)httpEx.Data["StatusCode"]! == HttpStatusCode.Unauthorized => "更新 Twitter Cookie 認證資訊",
            HttpRequestException => "檢查 Twitter GraphQL API 可用性",
            TaskCanceledException => "增加 Twitter API 超時時間",
            _ => "檢查 Twitter 服務可用性和認證狀態"
        };
    }

    private string GetTwitCastingRecommendedAction(Exception exception)
    {
        return exception switch
        {
            HttpRequestException httpEx when httpEx.Data.Contains("StatusCode") &&
                (HttpStatusCode)httpEx.Data["StatusCode"]! == HttpStatusCode.TooManyRequests => "延長 TwitCasting API 調用間隔",
            HttpRequestException httpEx when httpEx.Data.Contains("StatusCode") &&
                (HttpStatusCode)httpEx.Data["StatusCode"]! == HttpStatusCode.Unauthorized => "檢查 TwitCasting API 認證資訊",
            TaskCanceledException => "增加 TwitCasting API 超時時間",
            _ => "檢查 TwitCasting API 服務狀態"
        };
    }

    /// <summary>
    /// 取得錯誤嚴重程度
    /// </summary>
    private ErrorSeverity GetErrorSeverity(string platform, Exception exception)
    {
        if (exception is UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            return ErrorSeverity.High; // 配置或認證問題
        }

        if (exception is HttpRequestException httpEx && httpEx.Data.Contains("StatusCode"))
        {
            var statusCode = (HttpStatusCode)httpEx.Data["StatusCode"]!;
            return statusCode switch
            {
                HttpStatusCode.Unauthorized => ErrorSeverity.High,
                HttpStatusCode.Forbidden => ErrorSeverity.High,
                HttpStatusCode.TooManyRequests => ErrorSeverity.Medium,
                HttpStatusCode.InternalServerError => ErrorSeverity.Medium,
                HttpStatusCode.BadGateway => ErrorSeverity.Medium,
                HttpStatusCode.ServiceUnavailable => ErrorSeverity.Medium,
                _ => ErrorSeverity.Low
            };
        }

        return ErrorSeverity.Low; // 網路或暫時性問題
    }

    /// <summary>
    /// 判斷是否需要使用者介入
    /// </summary>
    private bool RequiresUserIntervention(string platform, Exception exception)
    {
        if (exception is ArgumentException or InvalidOperationException)
            return true; // 配置問題

        if (exception is UnauthorizedAccessException)
            return true; // 認證問題

        if (exception is HttpRequestException httpEx && httpEx.Data.Contains("StatusCode"))
        {
            var statusCode = (HttpStatusCode)httpEx.Data["StatusCode"]!;
            return statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
        }

        return false;
    }

    #endregion

    #endregion
}

/// <summary>
/// 錯誤統計資訊
/// </summary>
public class ErrorStatistics
{
    public string Platform { get; }
    public DateTime CreatedAt { get; }
    public DateTime LastUpdatedAt { get; private set; }

    public int TotalOperations { get; private set; }
    public int SuccessfulOperations { get; private set; }
    public int FailedOperations { get; private set; }
    public int FinalFailures { get; private set; }

    public double SuccessRate => TotalOperations == 0 ? 0.0 : (double)SuccessfulOperations / TotalOperations * 100;
    public double FailureRate => TotalOperations == 0 ? 0.0 : (double)FailedOperations / TotalOperations * 100;

    public Dictionary<string, int> OperationCounts { get; }
    public Dictionary<string, int> ErrorTypeCounts { get; }
    public List<ErrorRecord> RecentErrors { get; }

    public ErrorStatistics(string platform)
    {
        Platform = platform;
        CreatedAt = DateTime.UtcNow;
        LastUpdatedAt = DateTime.UtcNow;
        OperationCounts = new Dictionary<string, int>();
        ErrorTypeCounts = new Dictionary<string, int>();
        RecentErrors = new List<ErrorRecord>();
    }

    public void RecordSuccess(string operation)
    {
        TotalOperations++;
        SuccessfulOperations++;
        LastUpdatedAt = DateTime.UtcNow;

        if (OperationCounts.ContainsKey(operation))
            OperationCounts[operation]++;
        else
            OperationCounts[operation] = 1;
    }

    public void RecordError(string operation, Exception exception)
    {
        TotalOperations++;
        FailedOperations++;
        LastUpdatedAt = DateTime.UtcNow;

        var errorType = exception.GetType().Name;
        if (ErrorTypeCounts.ContainsKey(errorType))
            ErrorTypeCounts[errorType]++;
        else
            ErrorTypeCounts[errorType] = 1;

        // 保留最近 100 個錯誤記錄
        RecentErrors.Add(new ErrorRecord
        {
            Operation = operation,
            ErrorType = errorType,
            ErrorMessage = exception.Message,
            Timestamp = DateTime.UtcNow
        });

        if (RecentErrors.Count > 100)
        {
            RecentErrors.RemoveAt(0);
        }
    }

    public void RecordFinalFailure(string operation, Exception exception)
    {
        FinalFailures++;
        LastUpdatedAt = DateTime.UtcNow;

        // 也記錄為錯誤（但不增加 TotalOperations，因為已經在 RecordError 中增加過）
        var errorType = exception.GetType().Name;
        RecentErrors.Add(new ErrorRecord
        {
            Operation = operation,
            ErrorType = errorType,
            ErrorMessage = $"[FINAL FAILURE] {exception.Message}",
            Timestamp = DateTime.UtcNow,
            IsFinalFailure = true
        });

        if (RecentErrors.Count > 100)
        {
            RecentErrors.RemoveAt(0);
        }
    }
}

/// <summary>
/// 錯誤記錄
/// </summary>
public class ErrorRecord
{
    public string Operation { get; set; } = "";
    public string ErrorType { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public bool IsFinalFailure { get; set; } = false;
}

/// <summary>
/// 錯誤處理策略
/// </summary>
public class ErrorHandlingStrategy
{
    public string Platform { get; set; } = "";
    public string ExceptionType { get; set; } = "";
    public bool ShouldRetry { get; set; }
    public string RecommendedAction { get; set; } = "";
    public ErrorSeverity Severity { get; set; }
    public bool RequiresUserIntervention { get; set; }
}

/// <summary>
/// 錯誤嚴重程度
/// </summary>
public enum ErrorSeverity
{
    Low,    // 暫時性問題，通常可以自動恢復
    Medium, // 服務或網路問題，可能需要等待或重試
    High    // 配置或認證問題，需要人工介入
}

/// <summary>
/// 重試策略配置
/// </summary>
public class RetryPolicy
{
    public int MaxRetryAttempts { get; set; }
    public int BaseDelayMs { get; set; }
    public RetryType RetryType { get; set; }
    public int MaxDelayMs { get; set; }

    /// <summary>
    /// 計算指定嘗試次數的延遲時間
    /// </summary>
    public TimeSpan CalculateDelay(int attemptNumber)
    {
        int delayMs = RetryType switch
        {
            RetryType.Linear => BaseDelayMs * attemptNumber,
            RetryType.ExponentialBackoff => (int)(BaseDelayMs * Math.Pow(2, attemptNumber - 1)),
            _ => BaseDelayMs
        };

        // 限制最大延遲時間
        delayMs = Math.Min(delayMs, MaxDelayMs);
        return TimeSpan.FromMilliseconds(delayMs);
    }
}

/// <summary>
/// 重試類型
/// </summary>
public enum RetryType
{
    Linear,             // 線性延遲：delay * attemptNumber
    ExponentialBackoff  // 指數退避：delay * 2^attemptNumber
}
