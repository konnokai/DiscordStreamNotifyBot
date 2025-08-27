using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using DiscordStreamNotifyBot.DataBase;
using StackExchange.Redis;
using Moq;
using StreamNotifyBot.Crawler.Configuration;
using StreamNotifyBot.Crawler.Services;
using Xunit;
using FluentAssertions;

namespace StreamNotifyBot.Crawler.Tests.Services;

/// <summary>
/// 簡化的 CrawlerService 測試，專注於核心功能驗證
/// </summary>
public class SimpleCrawlerServiceTests
{
    [Fact]
    public void CrawlerConfig_ShouldBeCreated_WithDefaultValues()
    {
        // Arrange & Act
        var config = new CrawlerConfig();

        // Assert
        config.Should().NotBeNull();
        config.Database.Should().NotBeNull();
        config.Redis.Should().NotBeNull();
        config.Platforms.Should().NotBeNull();
        config.Monitoring.Should().NotBeNull();
        config.HealthCheck.Should().NotBeNull();
    }

    [Fact]
    public void MainDbContext_ShouldBeCreated_WithConnectionString()
    {
        // Arrange
        var connectionString = "Data Source=:memory:";

        // Act
        using var context = new MainDbContext(connectionString);

        // Assert
        context.Should().NotBeNull();
        context.Database.Should().NotBeNull();
    }

    [Fact]
    public void CrawlerService_ShouldHaveRequiredDependencies()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Add mock dependencies
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockLogger = new Mock<ILogger<CrawlerService>>();
        var mockConnectionMultiplexer = new Mock<IConnectionMultiplexer>();
        var mockDbContext = new Mock<MainDbContext>("test_connection_string");
        
        var crawlerConfig = new CrawlerConfig();
        var mockOptions = new Mock<Microsoft.Extensions.Options.IOptions<CrawlerConfig>>();
        mockOptions.Setup(x => x.Value).Returns(crawlerConfig);

        // Act
        var crawlerService = new CrawlerService(
            mockServiceProvider.Object,
            mockLogger.Object,
            mockOptions.Object,
            mockDbContext.Object,
            mockConnectionMultiplexer.Object
        );

        // Assert
        crawlerService.Should().NotBeNull();
        crawlerService.Should().BeAssignableTo<Microsoft.Extensions.Hosting.BackgroundService>();
    }

    [Fact]
    public void DatabaseConnectionString_ShouldBeConfigurable()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = "Server=localhost;Database=test;",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["CrawlerConfig:Database:ConnectionTimeoutSeconds"] = "30",
                ["CrawlerConfig:Redis:Database"] = "0"
            })
            .Build();

        var crawlerConfig = new CrawlerConfig();
        configuration.GetSection("CrawlerConfig").Bind(crawlerConfig);

        // Act & Assert
        var dbConnectionString = configuration.GetConnectionString("Database");
        var redisConnectionString = configuration.GetConnectionString("Redis");
        
        dbConnectionString.Should().Be("Server=localhost;Database=test;");
        redisConnectionString.Should().Be("localhost:6379");
        crawlerConfig.Database.ConnectionTimeoutSeconds.Should().Be(30);
        crawlerConfig.Redis.Database.Should().Be(0);
    }

    [Fact]
    public void CrawlerConfig_Validation_ShouldWork()
    {
        // Arrange
        var config = new CrawlerConfig();
        
        // Add required platform configurations for validation
        config.Platforms.YouTube.ApiKeys.Add("test_youtube_api_key");
        config.Platforms.Twitch.ClientId = "test_client_id";
        config.Platforms.Twitch.ClientSecret = "test_client_secret";
        config.Platforms.Twitter.BearerToken = "test_bearer_token";
        config.Platforms.TwitCasting.ClientId = "test_twitcasting_client_id";

        // Act & Assert
        var act = () => config.Validate();
        act.Should().NotThrow();
    }
}
