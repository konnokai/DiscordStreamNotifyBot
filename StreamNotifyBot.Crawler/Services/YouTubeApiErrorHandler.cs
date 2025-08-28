using Microsoft.Extensions.Logging;
using Google;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace StreamNotifyBot.Crawler.Services;

/// <summary>
/// YouTube API 錯誤處理和重試機制服務
/// </summary>
public class YouTubeApiErrorHandler
{
    private readonly ILogger<YouTubeApiErrorHandler> _logger;

    // 配置常數
    private const int DEFAULT_MAX_RETRY_ATTEMPTS = 3;
    private const int BASE_DELAY_SECONDS = 2;
    private const int MAX_DELAY_SECONDS = 60;

    // 錯誤統計
    private readonly Dictionary<string, int> _errorCounts = new();
    private readonly Dictionary<string, DateTime> _lastErrorTimes = new();
    private readonly object _statsLock = new();

    public YouTubeApiErrorHandler(ILogger<YouTubeApiErrorHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    /// <summary>
    /// 執行 YouTube API 呼叫並處理錯誤和重試
    /// </summary>
    /// <typeparam name="T">回傳類型</typeparam>
    /// <param name="apiCall">API 呼叫函式</param>
    /// <param name="operationName">操作名稱，用於記錄</param>
    /// <param name="maxRetries">最大重試次數</param>
    /// <returns>API 呼叫結果</returns>
    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> apiCall,
        string operationName,
        int maxRetries = DEFAULT_MAX_RETRY_ATTEMPTS)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < maxRetries)
        {
            try
            {
                _logger.LogDebug("Executing YouTube API call: {Operation}, Attempt: {Attempt}/{MaxRetries}",
                    operationName, attempt + 1, maxRetries);

                var result = await apiCall();

                // 成功時記錄（如果之前有失敗）
                if (attempt > 0)
                {
                    _logger.LogInformation("YouTube API call succeeded after {Attempts} attempts: {Operation}",
                        attempt + 1, operationName);
                }

                return result;
            }
            catch (GoogleApiException ex)
            {
                lastException = ex;
                var shouldRetry = await HandleGoogleApiExceptionAsync(ex, operationName, attempt, maxRetries);

                if (!shouldRetry)
                {
                    RecordError(operationName, ex);
                    throw;
                }
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                await HandleNetworkExceptionAsync(ex, operationName, attempt);

                if (attempt == maxRetries - 1)
                {
                    RecordError(operationName, ex);
                    throw;
                }
            }
            catch (TaskCanceledException ex)
            {
                lastException = ex;
                await HandleTimeoutExceptionAsync(ex, operationName, attempt);

                if (attempt == maxRetries - 1)
                {
                    RecordError(operationName, ex);
                    throw;
                }
            }
            catch (Exception ex)
            {
                // 非預期錯誤，記錄但不重試
                RecordError(operationName, ex);
                _logger.LogError(ex, "Unexpected error in YouTube API call: {Operation}", operationName);
                throw;
            }

            attempt++;

            if (attempt < maxRetries)
            {
                var delay = CalculateDelay(attempt);
                _logger.LogDebug("Waiting {Delay}ms before retry attempt {Attempt} for operation: {Operation}",
                    delay, attempt + 1, operationName);
                await Task.Delay(delay);
            }
        }

        // 所有重試都失敗
        if (lastException != null)
        {
            RecordError(operationName, lastException);
        }
        _logger.LogError("YouTube API call failed after {Attempts} attempts: {Operation}",
            maxRetries, operationName);
        throw lastException ?? new InvalidOperationException("Max retry attempts exceeded");
    }

    /// <summary>
    /// 處理 Google API 異常
    /// </summary>
    private async Task<bool> HandleGoogleApiExceptionAsync(
        GoogleApiException ex,
        string operationName,
        int attempt,
        int maxRetries)
    {
        switch (ex.HttpStatusCode)
        {
            case HttpStatusCode.TooManyRequests: // 429
                _logger.LogWarning("YouTube API quota/rate limit exceeded for operation {Operation}, attempt {Attempt}/{MaxRetries}. Error: {Error}",
                    operationName, attempt + 1, maxRetries, ex.Message);

                // 從回應標頭中提取重試延遲時間
                var retryAfter = ExtractRetryAfterSeconds(ex);
                if (retryAfter.HasValue && retryAfter.Value > 0)
                {
                    var delayMs = Math.Min(retryAfter.Value * 1000, MAX_DELAY_SECONDS * 1000);
                    _logger.LogInformation("Rate limit hit, waiting {Delay}ms as suggested by API", delayMs);
                    await Task.Delay(delayMs);
                }

                return attempt < maxRetries - 1; // 允許重試

            case HttpStatusCode.Forbidden: // 403
                _logger.LogError("YouTube API access forbidden for operation {Operation}: {Error}",
                    operationName, ex.Message);

                // 403 可能是配額用盡或 API 金鑰無效，不重試
                return false;

            case HttpStatusCode.NotFound: // 404
                _logger.LogWarning("YouTube API resource not found for operation {Operation}: {Error}",
                    operationName, ex.Message);

                // 資源不存在，不重試
                return false;

            case HttpStatusCode.BadRequest: // 400
                _logger.LogError("YouTube API bad request for operation {Operation}: {Error}",
                    operationName, ex.Message);

                // 請求格式錯誤，不重試
                return false;

            case HttpStatusCode.InternalServerError: // 500
            case HttpStatusCode.BadGateway: // 502
            case HttpStatusCode.ServiceUnavailable: // 503
            case HttpStatusCode.GatewayTimeout: // 504
                _logger.LogWarning("YouTube API server error for operation {Operation}, attempt {Attempt}/{MaxRetries}. Status: {Status}, Error: {Error}",
                    operationName, attempt + 1, maxRetries, ex.HttpStatusCode, ex.Message);

                return attempt < maxRetries - 1; // 伺服器錯誤，允許重試

            default:
                _logger.LogError("YouTube API unexpected HTTP status for operation {Operation}: {Status} - {Error}",
                    operationName, ex.HttpStatusCode, ex.Message);

                return false; // 未知 HTTP 狀態，不重試
        }
    }

    /// <summary>
    /// 處理網路連線異常
    /// </summary>
    private async Task HandleNetworkExceptionAsync(HttpRequestException ex, string operationName, int attempt)
    {
        _logger.LogWarning("Network error for YouTube API operation {Operation}, attempt {Attempt}: {Error}",
            operationName, attempt + 1, ex.Message);

        // 網路錯誤通常是暫時性的，可以重試
        await Task.CompletedTask;
    }

    /// <summary>
    /// 處理超時異常
    /// </summary>
    private async Task HandleTimeoutExceptionAsync(TaskCanceledException ex, string operationName, int attempt)
    {
        _logger.LogWarning("Timeout error for YouTube API operation {Operation}, attempt {Attempt}: {Error}",
            operationName, attempt + 1, ex.Message);

        // 超時錯誤可以重試
        await Task.CompletedTask;
    }

    /// <summary>
    /// 計算指數退避延遲時間
    /// </summary>
    private int CalculateDelay(int attempt)
    {
        // 指數退避: 2^attempt * BASE_DELAY_SECONDS，但不超過 MAX_DELAY_SECONDS
        var delay = Math.Pow(2, attempt) * BASE_DELAY_SECONDS;
        var delaySeconds = Math.Min(delay, MAX_DELAY_SECONDS);
        return (int)(delaySeconds * 1000); // 轉換為毫秒
    }

    /// <summary>
    /// 從 HTTP 回應標頭中提取 Retry-After 時間
    /// </summary>
    private int? ExtractRetryAfterSeconds(GoogleApiException ex)
    {
        try
        {
            // Google API 異常可能包含 Retry-After 標頭資訊
            var errorResponse = ex.Error;
            if (errorResponse?.Errors != null)
            {
                foreach (var error in errorResponse.Errors)
                {
                    // 檢查錯誤詳情中是否包含重試相關資訊
                    if (error.Reason != null && error.Reason.Contains("quota", StringComparison.OrdinalIgnoreCase))
                    {
                        // 配額相關錯誤，建議等待較長時間
                        return 60; // 60 秒
                    }
                    if (error.Reason != null && error.Reason.Contains("rate", StringComparison.OrdinalIgnoreCase))
                    {
                        // 速率限制錯誤，建議等待較短時間
                        return 30; // 30 秒
                    }
                }
            }
        }
        catch (Exception parseEx)
        {
            _logger.LogDebug("Failed to parse retry-after from API exception: {Error}", parseEx.Message);
        }

        return null;
    }

    /// <summary>
    /// 記錄錯誤統計
    /// </summary>
    private void RecordError(string operationName, Exception ex)
    {
        lock (_statsLock)
        {
            _errorCounts.TryGetValue(operationName, out var count);
            _errorCounts[operationName] = count + 1;
            _lastErrorTimes[operationName] = DateTime.UtcNow;
        }

        _logger.LogError(ex, "YouTube API error recorded for operation {Operation}. Total errors for this operation: {Count}",
            operationName, _errorCounts[operationName]);
    }

    /// <summary>
    /// 取得錯誤統計資訊
    /// </summary>
    public Dictionary<string, object> GetErrorStatistics()
    {
        lock (_statsLock)
        {
            return new Dictionary<string, object>
            {
                ["total_operations_with_errors"] = _errorCounts.Count,
                ["error_counts_by_operation"] = new Dictionary<string, int>(_errorCounts),
                ["last_error_times"] = new Dictionary<string, DateTime>(_lastErrorTimes)
            };
        }
    }

    /// <summary>
    /// 重設錯誤統計
    /// </summary>
    public void ResetStatistics()
    {
        lock (_statsLock)
        {
            _errorCounts.Clear();
            _lastErrorTimes.Clear();
        }

        _logger.LogInformation("YouTube API error statistics have been reset");
    }
}

/// <summary>
/// API 錯誤處理相關的擴展方法
/// </summary>
public static class YouTubeApiErrorHandlerExtensions
{
    /// <summary>
    /// 判斷異常是否為可重試的錯誤
    /// </summary>
    public static bool IsRetryableError(this Exception ex)
    {
        return ex switch
        {
            GoogleApiException gae => IsRetryableGoogleApiException(gae),
            HttpRequestException => true,
            TaskCanceledException => true,
            SocketException => true,
            _ => false
        };
    }

    private static bool IsRetryableGoogleApiException(GoogleApiException ex)
    {
        return ex.HttpStatusCode switch
        {
            HttpStatusCode.TooManyRequests => true,
            HttpStatusCode.InternalServerError => true,
            HttpStatusCode.BadGateway => true,
            HttpStatusCode.ServiceUnavailable => true,
            HttpStatusCode.GatewayTimeout => true,
            _ => false
        };
    }
}
