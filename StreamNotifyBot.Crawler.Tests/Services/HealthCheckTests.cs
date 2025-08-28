using DiscordStreamNotifyBot.DataBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using StreamNotifyBot.Crawler.Services;
using Xunit;

namespace StreamNotifyBot.Crawler.Tests.Services;

/// <summary>
/// 測試健康檢查功能
/// </summary>
public class HealthCheckTests : IDisposable
{
    private readonly IHost _host;
    private readonly HealthCheckService _healthCheckService;

    public HealthCheckTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .Build();

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config => config.AddConfiguration(configuration))
            .ConfigureServices((context, services) =>
            {
                // 使用 InMemory 資料庫進行測試
                services.AddDbContext<MainDbContext>(options =>
                    options.UseInMemoryDatabase($"HealthTestDb_{Guid.NewGuid()}"));

                // 註冊 Redis 服務
                var redisConnection = context.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
                services.AddSingleton<IConnectionMultiplexer>(provider =>
                {
                    var configuration = ConfigurationOptions.Parse(redisConnection);
                    return ConnectionMultiplexer.Connect(configuration);
                });

                services.AddLogging();
                
                // 註冊健康檢查
                services.AddHealthChecks()
                    .AddCheck<CrawlerHealthCheck>("crawler_health");
                
                // 註冊 CrawlerHealthCheck 依賴的服務
                services.AddScoped<CrawlerHealthCheck>();
            })
            .Build();

        _healthCheckService = _host.Services.GetRequiredService<HealthCheckService>();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthy_WhenDatabaseIsAvailable()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act
        var result = await _healthCheckService.CheckHealthAsync(cancellationTokenSource.Token);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Entries.Should().ContainKey("crawler_health");
    }

    [Fact]
    public async Task CrawlerHealthCheck_ShouldCheckDatabase_Successfully()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var healthCheck = _host.Services.GetRequiredService<CrawlerHealthCheck>();
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context, cancellationTokenSource.Token);

        // Assert
        result.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Degraded);
        result.Data.Should().ContainKey("database");
    }

    [Fact]
    public async Task DatabaseConnection_ShouldBeHealthy_WithInMemoryProvider()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var dbContext = _host.Services.GetRequiredService<MainDbContext>();

        // Act
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationTokenSource.Token);

        // Assert
        canConnect.Should().BeTrue("InMemory database should always be available");
    }

    public void Dispose()
    {
        _host?.Dispose();
    }
}
