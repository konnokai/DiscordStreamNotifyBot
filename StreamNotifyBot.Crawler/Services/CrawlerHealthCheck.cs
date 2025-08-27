using DiscordStreamNotifyBot.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using StreamNotifyBot.Crawler.Models;

namespace StreamNotifyBot.Crawler.Services;

/// <summary>
/// Crawler 健康檢查介面
/// </summary>
public interface ICrawlerHealthCheck
{
    /// <summary>
    /// 執行健康檢查
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>健康檢查結果</returns>
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得服務健康狀態
    /// </summary>
    /// <returns>服務健康狀態</returns>
    Task<ServiceHealth> GetServiceHealthAsync();
}

/// <summary>
/// Crawler 健康檢查實作
/// </summary>
public class CrawlerHealthCheck : IHealthCheck, ICrawlerHealthCheck
{
    private readonly MainDbContext _dbContext;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CrawlerHealthCheck> _logger;
    private readonly IServiceProvider _serviceProvider;

    public CrawlerHealthCheck(
        MainDbContext dbContext,
        IConnectionMultiplexer redis,
        ILogger<CrawlerHealthCheck> logger,
        IServiceProvider serviceProvider)
    {
        _dbContext = dbContext;
        _redis = redis;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return await CheckHealthAsync(cancellationToken);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var healthData = new Dictionary<string, object>();
        var healthyChecks = new List<string>();
        var unhealthyChecks = new List<string>();

        try
        {
            // 檢查資料庫連接
            var dbHealthy = await CheckDatabaseHealthAsync(cancellationToken);
            healthData.Add("database", dbHealthy ? "healthy" : "unhealthy");

            if (dbHealthy)
                healthyChecks.Add("database");
            else
                unhealthyChecks.Add("database");

            // 檢查 Redis 連接
            var redisHealthy = await CheckRedisHealthAsync(cancellationToken);
            healthData.Add("redis", redisHealthy ? "healthy" : "unhealthy");

            if (redisHealthy)
                healthyChecks.Add("redis");
            else
                unhealthyChecks.Add("redis");

            // 檢查平台監控器狀態
            var platformStatus = await GetPlatformMonitorStatusAsync(cancellationToken);
            healthData.Add("platforms", platformStatus);

            // 檢查記憶體使用量
            var memoryInfo = await GetMemoryInfoAsync();
            healthData.Add("memory", memoryInfo);

            // 檢查服務運行時間
            var uptimeInfo = await GetUptimeInfoAsync();
            healthData.Add("uptime", uptimeInfo);

            var isHealthy = dbHealthy && redisHealthy;

            healthData.Add("healthy_checks", healthyChecks);
            healthData.Add("unhealthy_checks", unhealthyChecks);

            return isHealthy
                ? HealthCheckResult.Healthy("Crawler service is healthy", healthData)
                : HealthCheckResult.Unhealthy("Crawler service has issues", data: healthData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            healthData.Add("error", ex.Message);
            return HealthCheckResult.Unhealthy("Health check exception", ex, healthData);
        }
    }

    public async Task<ServiceHealth> GetServiceHealthAsync()
    {
        try
        {
            var healthCheck = await CheckHealthAsync();

            return new ServiceHealth
            {
                ServiceName = "StreamNotifyBot.Crawler",
                Status = MapHealthStatus(healthCheck.Status),
                Description = healthCheck.Description ?? "No description available",
                Data = healthCheck.Data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>(),
                CheckTime = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service health");

            return new ServiceHealth
            {
                ServiceName = "StreamNotifyBot.Crawler",
                Status = Models.HealthStatus.Unhealthy,
                Description = $"Health check error: {ex.Message}",
                Data = new Dictionary<string, object> { { "error", ex.Message } },
                CheckTime = DateTime.UtcNow
            };
        }
    }

    private async Task<bool> CheckDatabaseHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            // 測試資料庫連接
            await _dbContext.Database.CanConnectAsync(cancellationToken);

            // 執行簡單查詢測試
            var canQuery = await _dbContext.GuildConfig
                .Take(1)
                .AnyAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return false;
        }
    }

    private async Task<bool> CheckRedisHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            var database = _redis.GetDatabase();
            var pingResult = await database.PingAsync();

            // 檢查 Ping 延遲是否合理 (< 5 秒)
            var isHealthy = pingResult.TotalMilliseconds < 5000;

            if (!isHealthy)
            {
                _logger.LogWarning("Redis ping latency is high: {Latency}ms", pingResult.TotalMilliseconds);
            }

            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return false;
        }
    }

    private async Task<Dictionary<string, object>> GetPlatformMonitorStatusAsync(CancellationToken cancellationToken)
    {
        var platformStatus = new Dictionary<string, object>();

        try
        {
            // 這裡會在後續 Story 中實作平台監控器的實際狀態檢查
            platformStatus.Add("youtube", "not_implemented");
            platformStatus.Add("twitch", "not_implemented");
            platformStatus.Add("twitter", "not_implemented");
            platformStatus.Add("twitcasting", "not_implemented");
            platformStatus.Add("total_monitors", 0);
            platformStatus.Add("active_monitors", 0);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting platform monitor status");
            platformStatus.Add("error", ex.Message);
        }

        return platformStatus;
    }

    private async Task<Dictionary<string, object>> GetMemoryInfoAsync()
    {
        try
        {
            var memoryInfo = new Dictionary<string, object>();

            var totalMemory = GC.GetTotalMemory(false);
            memoryInfo.Add("total_memory_bytes", totalMemory);
            memoryInfo.Add("total_memory_mb", totalMemory / 1024 / 1024);

            // 觸發垃圾回收並獲取清理後的記憶體使用量
            var gcMemory = GC.GetTotalMemory(true);
            memoryInfo.Add("gc_memory_bytes", gcMemory);
            memoryInfo.Add("gc_memory_mb", gcMemory / 1024 / 1024);

            memoryInfo.Add("gen0_collections", GC.CollectionCount(0));
            memoryInfo.Add("gen1_collections", GC.CollectionCount(1));
            memoryInfo.Add("gen2_collections", GC.CollectionCount(2));

            return await Task.FromResult(memoryInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting memory info");
            return new Dictionary<string, object> { { "error", ex.Message } };
        }
    }

    private async Task<Dictionary<string, object>> GetUptimeInfoAsync()
    {
        try
        {
            var uptime = Environment.TickCount64;
            var uptimeSpan = TimeSpan.FromMilliseconds(uptime);

            var uptimeInfo = new Dictionary<string, object>
            {
                { "uptime_ms", uptime },
                { "uptime_seconds", uptimeSpan.TotalSeconds },
                { "uptime_minutes", uptimeSpan.TotalMinutes },
                { "uptime_hours", uptimeSpan.TotalHours },
                { "uptime_string", uptimeSpan.ToString(@"dd\.hh\:mm\:ss") },
                { "started_at", DateTime.UtcNow - uptimeSpan }
            };

            return await Task.FromResult(uptimeInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting uptime info");
            return new Dictionary<string, object> { { "error", ex.Message } };
        }
    }

    private static Models.HealthStatus MapHealthStatus(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus healthStatus)
    {
        return healthStatus switch
        {
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy => Models.HealthStatus.Healthy,
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded => Models.HealthStatus.Degraded,
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy => Models.HealthStatus.Unhealthy,
            _ => Models.HealthStatus.Unhealthy
        };
    }
}
