using DiscordStreamNotifyBot.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using StreamNotifyBot.Crawler.Configuration;
using StreamNotifyBot.Crawler.PlatformMonitors;
using StreamNotifyBot.Crawler.Services;

namespace StreamNotifyBot.Crawler;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // 配置設定
                services.Configure<CrawlerConfig>(context.Configuration.GetSection("CrawlerConfig"));

                // 註冊核心服務
                services.AddHostedService<CrawlerService>();

                // 註冊資料庫服務
                var connectionString = context.Configuration.GetConnectionString("Database");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Database connection string is not configured");
                }

                services.AddTransient<MainDbContext>(provider =>
                {
                    return new MainDbContext(connectionString);
                });

                // 註冊 Redis 服務
                var redisConnection = context.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
                services.AddSingleton<IConnectionMultiplexer>(provider =>
                {
                    var configuration = ConfigurationOptions.Parse(redisConnection);
                    return ConnectionMultiplexer.Connect(configuration);
                });

                // 註冊 HTTP 客戶端
                services.AddHttpClient();

                // 註冊平台監控器介面和實作
                services.AddTransient<IYoutubeMonitor, YoutubeMonitor>();
                services.AddTransient<ITwitchMonitor, TwitchMonitor>();
                services.AddTransient<ITwitterMonitor, TwitterMonitor>();
                services.AddTransient<ITwitCastingMonitor, TwitCastingMonitor>();

                // 註冊共用服務
                services.AddSingleton<IStreamTracker, StreamTrackerService>();
                services.AddSingleton<ICrawlerHealthCheck, CrawlerHealthCheck>();

                // 註冊健康檢查
                services.AddHealthChecks()
                    .AddCheck<CrawlerHealthCheck>("crawler");
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();

        try
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Starting StreamNotifyBot.Crawler service...");

            await host.RunAsync();
        }
        catch (Exception ex)
        {
            //var logger = host.Services.GetService<ILogger<Program>>();
            //logger?.LogCritical(ex, "Application terminated unexpectedly");
            throw;
        }
        //finally
        //{
        //    var logger = host.Services.GetService<ILogger<Program>>();
        //    logger?.LogInformation("StreamNotifyBot.Crawler service stopped");
        //}
    }
}
