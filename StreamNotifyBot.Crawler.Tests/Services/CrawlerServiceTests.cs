using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;
using StreamNotifyBot.Crawler;
using StreamNotifyBot.Crawler.Configuration;
using DiscordStreamNotifyBot.DataBase;

namespace StreamNotifyBot.Crawler.Tests.Services;

/// <summary>
/// 測試 CrawlerService 核心功能
/// </summary>
public class CrawlerServiceTests : IDisposable
{
    private readonly IHost _host;
    private readonly CrawlerService _crawlerService;

    public CrawlerServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .Build();

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config => config.AddConfiguration(configuration))
            .ConfigureServices((context, services) =>
            {
                services.Configure<CrawlerConfig>(context.Configuration);
                
                // 使用 InMemory 資料庫進行測試
                services.AddDbContext<MainDbContext>(options =>
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
                
                services.AddLogging(builder => 
                    builder.AddConsole().SetMinimumLevel(LogLevel.Information));
                
                services.AddHostedService<CrawlerService>();
                services.AddHttpClient();
                
                // 註冊測試用的平台監控器
                services.AddTransient<StreamNotifyBot.Crawler.Services.IPlatformMonitor, 
                    StreamNotifyBot.Crawler.PlatformMonitors.YoutubeMonitor>();
            })
            .Build();

        _crawlerService = _host.Services.GetServices<IHostedService>()
            .OfType<CrawlerService>()
            .First();
    }

    [Fact]
    public async Task CrawlerService_ShouldStart_WithoutException()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act & Assert
        var startTask = _crawlerService.StartAsync(cancellationTokenSource.Token);
        await startTask;
        startTask.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task CrawlerService_ShouldStop_GracefullyAfterStart()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        
        // Act - 啟動服務
        await _crawlerService.StartAsync(cancellationTokenSource.Token);
        
        // 等待短時間確保服務完全啟動
        await Task.Delay(1000, cancellationTokenSource.Token);
        
        // Act - 停止服務
        var stopTask = _crawlerService.StopAsync(cancellationTokenSource.Token);
        
        // Assert
        await stopTask;
        stopTask.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task CrawlerService_ShouldInitializeDatabase_Successfully()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var dbContext = _host.Services.GetRequiredService<MainDbContext>();

        // Act
        await _crawlerService.StartAsync(cancellationTokenSource.Token);

        // Assert - 檢查資料庫是否可以正常存取
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationTokenSource.Token);
        canConnect.Should().BeTrue("Database should be accessible after service initialization");
        
        // Cleanup
        await _crawlerService.StopAsync(cancellationTokenSource.Token);
    }

    [Fact]
    public async Task CrawlerService_ShouldHandleMultipleStartStop_Gracefully()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        // Act & Assert - 多次啟動停止循環
        for (int i = 0; i < 3; i++)
        {
            await _crawlerService.StartAsync(cancellationTokenSource.Token);
            await Task.Delay(500, cancellationTokenSource.Token);
            await _crawlerService.StopAsync(cancellationTokenSource.Token);
            await Task.Delay(200, cancellationTokenSource.Token);
        }

        // 服務應該能夠處理多次啟動/停止而不拋出異常
    }

    public void Dispose()
    {
        _host?.Dispose();
    }
}
