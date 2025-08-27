using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;
using StreamNotifyBot.Crawler.Configuration;
using DiscordStreamNotifyBot.DataBase;

namespace StreamNotifyBot.Crawler.Tests.Configuration;

/// <summary>
/// 測試配置管理和服務依賴注入
/// </summary>
public class ConfigurationTests : TestBase
{
    [Fact]
    public void CrawlerConfig_ShouldLoadCorrectly_FromConfiguration()
    {
        // Arrange & Act
        var crawlerConfig = GetService<CrawlerConfig>();

        // Assert
        crawlerConfig.Should().NotBeNull();
        crawlerConfig.Database.Should().NotBeNull();
        crawlerConfig.HealthCheck.Should().NotBeNull();
        crawlerConfig.Platforms.Should().NotBeNull();
    }

    [Fact]
    public void DatabaseConnectionString_ShouldBeConfigured()
    {
        // Arrange & Act
        var crawlerConfig = GetService<CrawlerConfig>();
        var dbContext = GetService<MainDbContext>();

        // Assert
        crawlerConfig.Database.ConnectionTimeoutSeconds.Should().BeGreaterThan(0);
        dbContext.Should().NotBeNull();
    }

    [Fact]
    public void RedisConnectionString_ShouldBeConfigured()
    {
        // Arrange & Act
        var crawlerConfig = GetService<CrawlerConfig>();

        // Assert
        crawlerConfig.Redis.Database.Should().BeGreaterOrEqualTo(0);
        crawlerConfig.Redis.ConnectionTimeoutMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ServiceCollection_ShouldRegisterAllServices()
    {
        // Arrange & Act
        var dbContext = GetService<MainDbContext>();
        var config = GetService<IConfiguration>();

        // Assert
        dbContext.Should().NotBeNull();
        config.Should().NotBeNull();
    }

    [Fact]
    public void PlatformConfigs_ShouldHaveDefaultConfiguration()
    {
        // Arrange & Act
        var crawlerConfig = GetService<CrawlerConfig>();

        // Assert
        crawlerConfig.Platforms.Should().NotBeNull();
        crawlerConfig.Platforms.YouTube.Should().NotBeNull();
        crawlerConfig.Platforms.Twitch.Should().NotBeNull();
        
        crawlerConfig.Monitoring.CheckIntervalSeconds.Should().BeGreaterThan(0);
        crawlerConfig.Monitoring.MaxRetryAttempts.Should().BeGreaterThan(0);
    }
}
