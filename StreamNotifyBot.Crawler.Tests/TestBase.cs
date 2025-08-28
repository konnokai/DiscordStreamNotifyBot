using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using DiscordStreamNotifyBot.DataBase;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Moq;
using StreamNotifyBot.Crawler.Configuration;
using StreamNotifyBot.Crawler.Services;

namespace StreamNotifyBot.Crawler.Tests
{
    public abstract class TestBase : IDisposable
    {
        protected IServiceProvider ServiceProvider { get; private set; } = default!;
        protected IConfiguration Configuration { get; private set; } = default!;
        private ServiceCollection _services = default!;
        private bool _disposed = false;

        protected TestBase()
        {
            SetupConfiguration();
            SetupServices();
        }

        private void SetupConfiguration()
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Test.json", optional: true)
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:ConnectionString"] = "Data Source=:memory:",
                    ["Database:ConnectionTimeoutSeconds"] = "30",
                    ["Database:CommandTimeoutSeconds"] = "60",
                    ["Redis:ConnectionString"] = "localhost:6379",
                    ["Redis:Database"] = "0",
                    ["HealthCheck:Port"] = "6111",
                    ["HealthCheck:Endpoint"] = "/health",
                    ["HealthCheck:TimeoutSeconds"] = "30",
                    ["Monitoring:CheckIntervalSeconds"] = "30",
                    ["Monitoring:MaxRetryAttempts"] = "3",
                    ["Monitoring:BatchSize"] = "100",
                    ["Platforms:YouTube:ApiKeys:0"] = "test_youtube_key",
                    ["Platforms:YouTube:CheckIntervalSeconds"] = "30",
                    ["Platforms:Twitch:ClientId"] = "test_twitch_client_id",
                    ["Platforms:Twitch:ClientSecret"] = "test_twitch_secret",
                    ["Platforms:Twitch:CheckIntervalSeconds"] = "30"
                });

            Configuration = configBuilder.Build();
        }

        private void SetupServices()
        {
            _services = new ServiceCollection();

            // Add Configuration
            _services.AddSingleton(Configuration);

            // Add Logging
            _services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Add CrawlerConfig
            _services.Configure<CrawlerConfig>(Configuration);
            _services.AddSingleton(provider =>
            {
                var config = new CrawlerConfig();
                Configuration.Bind(config);
                return config;
            });

            // Add DbContext with connection string parameter using factory
            _services.AddDbContext<MainDbContext>(options =>
            {
                options.UseInMemoryDatabase("StreamBotTestDb");
                options.UseSnakeCaseNamingConvention();
            });

            // Mock Redis Connection
            var mockConnectionMultiplexer = new Mock<IConnectionMultiplexer>();
            var mockDatabase = new Mock<IDatabase>();
            mockConnectionMultiplexer.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(mockDatabase.Object);
            _services.AddSingleton(mockConnectionMultiplexer.Object);

            // Add Health Checks
            _services.AddHealthChecks();

            // Add ICrawlerHealthCheck
            _services.AddSingleton<ICrawlerHealthCheck>(provider =>
            {
                var mockHealthCheck = new Mock<ICrawlerHealthCheck>();
                mockHealthCheck.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new HealthCheckResult(HealthStatus.Healthy, "Test healthy"));
                return mockHealthCheck.Object;
            });

            // Add CrawlerService
            _services.AddSingleton<CrawlerService>();

            // Add Hosted Service
            _services.AddHostedService<CrawlerService>();

            ServiceProvider = _services.BuildServiceProvider();
        }

        protected T GetService<T>() where T : notnull
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        protected T? GetOptionalService<T>() where T : class
        {
            return ServiceProvider.GetService<T>();
        }

        protected IHost CreateHost()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddConfiguration(Configuration);
                })
                .ConfigureServices((context, services) =>
                {
                    // Copy all registered services to the host
                    foreach (var service in _services)
                    {
                        services.Add(service);
                    }
                });

            return hostBuilder.Build();
        }

        public virtual void Dispose()
        {
            if (!_disposed)
            {
                if (ServiceProvider is IDisposable disposableProvider)
                {
                    disposableProvider.Dispose();
                }
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
