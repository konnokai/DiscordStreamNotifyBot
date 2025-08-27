using DiscordStreamNotifyBot.DataBase;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StreamNotifyBot.Crawler.Configuration;
using StreamNotifyBot.Crawler.Services;

namespace StreamNotifyBot.Crawler;

public class CrawlerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CrawlerService> _logger;
    private readonly CrawlerConfig _config;
    private readonly MainDbContext _mainDbContext;
    private readonly IConnectionMultiplexer _redis;
    private readonly List<IPlatformMonitor> _platformMonitors = new();
    private readonly CancellationTokenSource _shutdownTokenSource = new();

    public CrawlerService(
        IServiceProvider serviceProvider,
        ILogger<CrawlerService> logger,
        IOptions<CrawlerConfig> config,
        MainDbContext mainDbContext,
        IConnectionMultiplexer redis)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _config = config.Value;
        _mainDbContext = mainDbContext;
        _redis = redis;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Crawler Service...");

        try
        {
            // 初始化資料庫連接
            await InitializeDatabaseAsync(cancellationToken);

            // 初始化 Redis 連接
            await InitializeRedisAsync(cancellationToken);

            // 載入平台監控器
            await LoadPlatformMonitorsAsync(cancellationToken);

            // 啟動所有平台監控器
            await StartPlatformMonitorsAsync(cancellationToken);

            // 監聽 Discord Shard 的追蹤請求
            await StartEventListenersAsync(cancellationToken);

            _logger.LogInformation("Crawler Service started successfully");

            // 呼叫基類的 StartAsync
            await base.StartAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Crawler Service");
            throw;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Crawler Service is running");

        // 啟動定期維護任務
        var maintenanceTask = Task.Run(() => PeriodicMaintenanceAsync(stoppingToken), stoppingToken);

        // 主要服務迴圈
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // 監控服務健康狀態
                await MonitorServiceHealthAsync(stoppingToken);

                // 等待一段時間後再次檢查
                await Task.Delay(TimeSpan.FromSeconds(_config.Monitoring.CheckIntervalSeconds), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Crawler Service execution cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Crawler Service execution error");
        }

        // 等待維護任務完成
        try
        {
            await maintenanceTask;
        }
        catch (OperationCanceledException)
        {
            // 預期的取消操作
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Crawler Service...");

        try
        {
            // 發出關閉信號
            _shutdownTokenSource.Cancel();

            // 優雅關閉所有平台監控器
            await StopPlatformMonitorsAsync(cancellationToken);

            // 關閉 Redis 連接
            await CloseRedisConnectionAsync();

            _logger.LogInformation("Crawler Service stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while stopping Crawler Service");
        }
        finally
        {
            // 呼叫基類的 StopAsync
            await base.StopAsync(cancellationToken);
            _shutdownTokenSource.Dispose();
        }
    }

    private async Task InitializeDatabaseAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        _logger.LogInformation("Initializing database connection...");

        try
        {
            // 測試資料庫連接
            await _mainDbContext.Database.CanConnectAsync(cancellationToken);
            _logger.LogInformation("Database connection initialized successfully");         
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database connection");
            throw;
        }
    }

    private async Task InitializeRedisAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing Redis connection...");

        try
        {
            // 測試 Redis 連接
            var database = _redis.GetDatabase();
            await database.PingAsync();
            _logger.LogInformation("Redis connection initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Redis connection");
            throw;
        }
    }

    private async Task LoadPlatformMonitorsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading platform monitors...");

        try
        {
            // 這裡暫時建立佔位符，在後續 Story 中會實作具體的監控器
            _logger.LogInformation("Platform monitors loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load platform monitors");
            throw;
        }
    }

    private async Task StartPlatformMonitorsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting platform monitors...");

        try
        {
            foreach (var monitor in _platformMonitors)
            {
                await monitor.StartAsync(cancellationToken);
                _logger.LogInformation("Started platform monitor: {PlatformName}", monitor.PlatformName);
            }

            _logger.LogInformation("All platform monitors started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start platform monitors");
            throw;
        }
    }

    private async Task StartEventListenersAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting event listeners...");

        try
        {
            // 這裡會在後續 Story 中實作 Redis PubSub 事件監聽器
            _logger.LogInformation("Event listeners started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start event listeners");
            throw;
        }
    }

    private async Task StopPlatformMonitorsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping platform monitors...");

        var stopTasks = _platformMonitors.Select(monitor =>
            StopMonitorSafelyAsync(monitor, cancellationToken));

        await Task.WhenAll(stopTasks);

        _logger.LogInformation("All platform monitors stopped");
    }

    private async Task StopMonitorSafelyAsync(IPlatformMonitor monitor, CancellationToken cancellationToken)
    {
        try
        {
            await monitor.StopAsync(cancellationToken);
            _logger.LogInformation("Stopped platform monitor: {PlatformName}", monitor.PlatformName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping platform monitor: {PlatformName}", monitor.PlatformName);
        }
    }

    private async Task CloseRedisConnectionAsync()
    {
        try
        {
            if (_redis.IsConnected)
            {
                await _redis.CloseAsync();
                _logger.LogInformation("Redis connection closed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing Redis connection");
        }
    }

    private async Task PeriodicMaintenanceAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting periodic maintenance task");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // 定期維護任務：清理失效追蹤、API 配額管理、錯誤恢復
                await PerformMaintenanceTasksAsync(cancellationToken);

                // 等待下次維護週期
                await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Periodic maintenance task cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in periodic maintenance task");
        }
    }

    private async Task PerformMaintenanceTasksAsync(CancellationToken cancellationToken)
    {
        // 這裡會在後續 Story 中實作具體的維護邏輯
        _logger.LogDebug("Performing maintenance tasks...");
        await Task.CompletedTask;
    }

    private async Task MonitorServiceHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var healthCheck = scope.ServiceProvider.GetRequiredService<ICrawlerHealthCheck>();
            var result = await healthCheck.CheckHealthAsync(cancellationToken);

            if (result.Status != Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy)
            {
                _logger.LogWarning("Service health check failed: {Description}", result.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during service health monitoring");
        }
    }
}
