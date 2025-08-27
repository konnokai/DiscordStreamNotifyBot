using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
using FluentAssertions;
using StreamNotifyBot.Crawler;
using StreamNotifyBot.Crawler.Configuration;

namespace StreamNotifyBot.Crawler.Tests.Integration;

/// <summary>
/// 整合測試：測試完整的應用程式啟動流程
/// </summary>
public class ApplicationIntegrationTests : IDisposable
{
    private IHost? _host;

    [Fact]
    public async Task Application_ShouldStartAndStop_Successfully()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .Build();

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config => config.AddConfiguration(configuration))
            .ConfigureServices((context, services) =>
            {
                services.Configure<CrawlerConfig>(context.Configuration);
                services.AddLogging(builder => 
                    builder.SetMinimumLevel(LogLevel.Warning)); // 減少測試輸出
                services.AddHostedService<CrawlerService>();
            })
            .Build();

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        // Act & Assert
        var startTask = _host.StartAsync(cancellationTokenSource.Token);
        await startTask;
        startTask.IsCompletedSuccessfully.Should().BeTrue();

        // 等待服務完全啟動
        await Task.Delay(2000, cancellationTokenSource.Token);

        var stopTask = _host.StopAsync(cancellationTokenSource.Token);
        await stopTask;
        stopTask.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public void ServiceProvider_ShouldResolveAllServices_Correctly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .Build();

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config => config.AddConfiguration(configuration))
            .ConfigureServices((context, services) =>
            {
                services.Configure<CrawlerConfig>(context.Configuration);
                services.AddLogging();
                services.AddHostedService<CrawlerService>();
                services.AddHttpClient();
            })
            .Build();

        // Act & Assert
        _host.Services.GetService<IConfiguration>().Should().NotBeNull();
        _host.Services.GetService<ILogger<CrawlerService>>().Should().NotBeNull();
        _host.Services.GetService<IHttpClientFactory>().Should().NotBeNull();
        
        var crawlerConfig = _host.Services.GetService<Microsoft.Extensions.Options.IOptions<CrawlerConfig>>();
        crawlerConfig.Should().NotBeNull();
        crawlerConfig!.Value.Should().NotBeNull();
    }

    [Fact]
    public void Configuration_ShouldBind_CrawlerConfig()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .Build();

        var crawlerConfig = new CrawlerConfig();

        // Act
        configuration.Bind(crawlerConfig);

        // Assert
        crawlerConfig.Monitoring.CheckIntervalSeconds.Should().Be(10);
        crawlerConfig.HealthCheck.Port.Should().Be(6112);
        crawlerConfig.Platforms.YouTube.ApiKeys.Should().Contain("test_youtube_key");
    }

    public void Dispose()
    {
        _host?.Dispose();
    }
}
