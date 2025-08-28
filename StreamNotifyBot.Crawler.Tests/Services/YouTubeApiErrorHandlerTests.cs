using Microsoft.Extensions.Logging;
using Moq;
using StreamNotifyBot.Crawler.Services;
using Xunit;
using Google;
using System.Net;

namespace StreamNotifyBot.Crawler.Tests.Services;

/// <summary>
/// YouTube API 錯誤處理器單元測試
/// </summary>
public class YouTubeApiErrorHandlerTests
{
    private readonly Mock<ILogger<YouTubeApiErrorHandler>> _mockLogger;
    private readonly YouTubeApiErrorHandler _errorHandler;

    public YouTubeApiErrorHandlerTests()
    {
        _mockLogger = new Mock<ILogger<YouTubeApiErrorHandler>>();
        _errorHandler = new YouTubeApiErrorHandler(_mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_SuccessfulCall_ReturnsResult()
    {
        // Arrange
        const string expectedResult = "success";
        var apiCall = new Func<Task<string>>(() => Task.FromResult(expectedResult));

        // Act
        var result = await _errorHandler.ExecuteWithRetryAsync(apiCall, "Test_SuccessfulCall");

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_TransientError_RetriesAndSucceeds()
    {
        // Arrange
        var callCount = 0;
        const string expectedResult = "success";

        var apiCall = new Func<Task<string>>(() =>
        {
            callCount++;
            if (callCount == 1)
            {
                throw new GoogleApiException("YouTube Data API", "Temporary error")
                {
                    HttpStatusCode = HttpStatusCode.InternalServerError
                };
            }
            return Task.FromResult(expectedResult);
        });

        // Act
        var result = await _errorHandler.ExecuteWithRetryAsync(apiCall, "Test_TransientError");

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(2, callCount); // 第一次失敗，第二次成功
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_TooManyRequestsError_RetriesWithBackoff()
    {
        // Arrange
        var callCount = 0;
        const string expectedResult = "success";

        var apiCall = new Func<Task<string>>(() =>
        {
            callCount++;
            if (callCount <= 2)
            {
                var ex = new GoogleApiException("YouTube Data API", "Quota exceeded")
                {
                    HttpStatusCode = HttpStatusCode.TooManyRequests
                };
                ex.Data["Retry-After"] = "30";
                throw ex;
            }
            return Task.FromResult(expectedResult);
        });

        // Act
        var result = await _errorHandler.ExecuteWithRetryAsync(apiCall, "Test_TooManyRequests");

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(3, callCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_PermanentError_DoesNotRetry()
    {
        // Arrange
        var callCount = 0;

        var apiCall = new Func<Task<string>>(() =>
        {
            callCount++;
            throw new GoogleApiException("YouTube Data API", "Access forbidden")
            {
                HttpStatusCode = HttpStatusCode.Forbidden
            };
        });

        // Act & Assert
        await Assert.ThrowsAsync<GoogleApiException>(() => _errorHandler.ExecuteWithRetryAsync(apiCall, "Test_PermanentError"));
        Assert.Equal(1, callCount); // 不應該重試
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_MaxRetriesExceeded_ThrowsException()
    {
        // Arrange
        var callCount = 0;

        var apiCall = new Func<Task<string>>(() =>
        {
            callCount++;
            throw new GoogleApiException("YouTube Data API", "Temporary error")
            {
                HttpStatusCode = HttpStatusCode.InternalServerError
            };
        });

        // Act & Assert
        await Assert.ThrowsAsync<GoogleApiException>(() => _errorHandler.ExecuteWithRetryAsync(apiCall, "Test_MaxRetries"));
        Assert.Equal(3, callCount); // 預設最大重試次數為 3
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_NotFoundError_DoesNotRetry()
    {
        // Arrange
        var callCount = 0;

        var apiCall = new Func<Task<string>>(() =>
        {
            callCount++;
            throw new GoogleApiException("YouTube Data API", "Video not found")
            {
                HttpStatusCode = HttpStatusCode.NotFound
            };
        });

        // Act & Assert
        await Assert.ThrowsAsync<GoogleApiException>(() => _errorHandler.ExecuteWithRetryAsync(apiCall, "Test_NotFound"));
        Assert.Equal(1, callCount); // 不應該重試
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_GenericException_DoesNotRetry()
    {
        // Arrange
        var callCount = 0;

        var apiCall = new Func<Task<string>>(() =>
        {
            callCount++;
            throw new InvalidOperationException("Generic error");
        });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _errorHandler.ExecuteWithRetryAsync(apiCall, "Test_GenericException"));
        Assert.Equal(1, callCount); // 根據實作，非預期錯誤不重試
    }

    [Fact]
    public void GetErrorStatistics_AfterErrors_ReturnsCorrectStats()
    {
        // Arrange
        var apiCall1 = new Func<Task<string>>(() =>
            throw new GoogleApiException("YouTube Data API", "Quota exceeded")
            {
                HttpStatusCode = HttpStatusCode.TooManyRequests
            });

        var apiCall2 = new Func<Task<string>>(() =>
            throw new GoogleApiException("YouTube Data API", "Not found")
            {
                HttpStatusCode = HttpStatusCode.NotFound
            });

        // Act
        try { var _ = _errorHandler.ExecuteWithRetryAsync(apiCall1, "Operation1").Result; } catch { }
        try { var _ = _errorHandler.ExecuteWithRetryAsync(apiCall2, "Operation2").Result; } catch { }

        var stats = _errorHandler.GetErrorStatistics();

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.ContainsKey("total_operations_with_errors"));
        Assert.True(stats.ContainsKey("error_counts_by_operation"));
        Assert.True((int)stats["total_operations_with_errors"] >= 2);
    }

    [Fact]
    public void ResetStatistics_AfterReset_ClearsAllStats()
    {
        // Arrange
        var apiCall = new Func<Task<string>>(() =>
            throw new GoogleApiException("YouTube Data API", "Error")
            {
                HttpStatusCode = HttpStatusCode.InternalServerError
            });

        try { var _ = _errorHandler.ExecuteWithRetryAsync(apiCall, "TestOperation").Result; } catch { }

        // Act
        _errorHandler.ResetStatistics();
        var stats = _errorHandler.GetErrorStatistics();

        // Assert
        Assert.Equal(0, stats["total_operations_with_errors"]);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new YouTubeApiErrorHandler(null!));
    }
}
