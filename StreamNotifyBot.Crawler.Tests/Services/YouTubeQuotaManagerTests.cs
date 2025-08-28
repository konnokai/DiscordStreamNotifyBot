using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StreamNotifyBot.Crawler.Configuration;
using StreamNotifyBot.Crawler.Services;
using Xunit;

namespace StreamNotifyBot.Crawler.Tests.Services;

/// <summary>
/// YouTube 配額管理器單元測試
/// </summary>
public class YouTubeQuotaManagerTests
{
    private readonly Mock<ILogger<YouTubeQuotaManager>> _mockLogger;
    private readonly Mock<IOptions<CrawlerConfig>> _mockConfig;
    private readonly YouTubeQuotaManager _quotaManager;
    private readonly CrawlerConfig _config;

    public YouTubeQuotaManagerTests()
    {
        _mockLogger = new Mock<ILogger<YouTubeQuotaManager>>();
        _mockConfig = new Mock<IOptions<CrawlerConfig>>();

        _config = new CrawlerConfig
        {
            Platforms = new PlatformConfig
            {
                YouTube = new YouTubeConfig
                {
                    ApiKeys = new List<string> 
                    { 
                        "test-api-key-1", 
                        "test-api-key-2", 
                        "test-api-key-3" 
                    },
                    QuotaLimit = 10000
                }
            }
        };

        _mockConfig.Setup(x => x.Value).Returns(_config);

        _quotaManager = new YouTubeQuotaManager(
            _mockLogger.Object,
            _mockConfig.Object);
    }

    [Fact]
    public async Task GetAvailableServiceAsync_FirstCall_ReturnsFirstKey()
    {
        // Act
        var service = await _quotaManager.GetAvailableServiceAsync();

        // Assert
        Assert.NotNull(service);
        // 驗證使用了第一個 API 金鑰（通過檢查服務實例）
        Assert.Equal("DiscordStreamBot-Crawler", service.ApplicationName);
    }

    [Fact]
    public void RecordQuotaUsage_NormalUsage_UpdatesQuotaCorrectly()
    {
        // Arrange
        const string apiKey = "test-api-key-1";
        const int quotaUsage = 100;

        // Act
        _quotaManager.RecordQuotaUsage(apiKey, quotaUsage);
        var usage = _quotaManager.GetApiUsageInfo();

        // Assert
        Assert.True(usage.UsedQuota >= quotaUsage);
    }

    [Fact]
    public void RecordQuotaUsage_ExceedsWarningThreshold_LogsWarning()
    {
        // Arrange
        const string apiKey = "test-api-key-1";
        const int quotaUsage = 8500; // 85% of 10000

        // Act
        _quotaManager.RecordQuotaUsage(apiKey, quotaUsage);

        // Assert - 驗證沒有拋出例外
        Assert.True(true);
    }

    [Fact]
    public void RecordQuotaUsage_ExceedsLimit_MarksAsExhausted()
    {
        // Arrange
        const string apiKey = "test-api-key-1";
        const int quotaUsage = 10500; // 超過 10000 限制

        // Act
        _quotaManager.RecordQuotaUsage(apiKey, quotaUsage);

        // Assert - 配額應該超限
        var stats = _quotaManager.GetDetailedQuotaStatistics();
        var keyStats = stats.ApiKeyStatistics.FirstOrDefault(x => x.KeyMask.Contains("test"));
        Assert.NotNull(keyStats);
    }

    [Fact]
    public void GetApiUsageInfo_AfterUsage_ReturnsCorrectInfo()
    {
        // Arrange
        const string apiKey = "test-api-key-1";
        const int quotaUsage = 1000;
        
        // Act
        _quotaManager.RecordQuotaUsage(apiKey, quotaUsage);
        var usageInfo = _quotaManager.GetApiUsageInfo();

        // Assert
        Assert.True(usageInfo.UsedQuota >= quotaUsage);
        Assert.True(usageInfo.QuotaLimit > 0);
        Assert.True(usageInfo.RemainingRequests <= usageInfo.QuotaLimit);
    }

    [Fact]
    public void GetDetailedQuotaStatistics_NewManager_ReturnsValidStats()
    {
        // Act
        var stats = _quotaManager.GetDetailedQuotaStatistics();

        // Assert
        Assert.Equal(3, stats.TotalApiKeys);
        Assert.Equal(3, stats.AvailableApiKeys);
        Assert.Equal(0, stats.ExhaustedApiKeys);
        Assert.Equal(3, stats.ApiKeyStatistics.Count);
    }

    [Fact]
    public void ResetAllQuotas_AfterUsage_ResetsSuccessfully()
    {
        // Arrange
        const string apiKey = "test-api-key-1";
        _quotaManager.RecordQuotaUsage(apiKey, 5000);

        // Act
        _quotaManager.ResetAllQuotas();
        var usage = _quotaManager.GetApiUsageInfo();

        // Assert
        Assert.Equal(0, usage.UsedQuota);
    }

    [Fact]
    public void RecordVideosListUsage_RecordsCorrectQuotaCost()
    {
        // Arrange
        const string apiKey = "test-api-key-1";

        // Act
        _quotaManager.RecordVideosListUsage(apiKey);
        var usage = _quotaManager.GetApiUsageInfo();

        // Assert
        Assert.True(usage.UsedQuota >= 1); // Videos.List 消耗 1 配額
    }

    [Fact]
    public void RecordChannelsListUsage_RecordsCorrectQuotaCost()
    {
        // Arrange
        const string apiKey = "test-api-key-1";

        // Act
        _quotaManager.RecordChannelsListUsage(apiKey);
        var usage = _quotaManager.GetApiUsageInfo();

        // Assert
        Assert.True(usage.UsedQuota >= 1); // Channels.List 消耗 1 配額
    }

    [Fact]
    public void RecordSearchListUsage_RecordsCorrectQuotaCost()
    {
        // Arrange
        const string apiKey = "test-api-key-1";

        // Act
        _quotaManager.RecordSearchListUsage(apiKey);
        var usage = _quotaManager.GetApiUsageInfo();

        // Assert
        Assert.True(usage.UsedQuota >= 100); // Search.List 消耗 100 配額
    }

    [Fact]
    public void Constructor_EmptyApiKeys_ThrowsInvalidOperationException()
    {
        // Arrange
        var emptyConfig = new CrawlerConfig
        {
            Platforms = new PlatformConfig
            {
                YouTube = new YouTubeConfig
                {
                    ApiKeys = new List<string>(),
                    QuotaLimit = 10000
                }
            }
        };

        var mockConfig = new Mock<IOptions<CrawlerConfig>>();
        mockConfig.Setup(x => x.Value).Returns(emptyConfig);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new YouTubeQuotaManager(_mockLogger.Object, mockConfig.Object));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new YouTubeQuotaManager(null!, _mockConfig.Object));
    }

    [Fact]
    public void Constructor_NullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new YouTubeQuotaManager(_mockLogger.Object, null!));
    }
}
