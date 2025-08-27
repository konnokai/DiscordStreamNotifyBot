using System.ComponentModel.DataAnnotations;

namespace StreamNotifyBot.Crawler.Configuration;

public class CrawlerConfig
{
    public DatabaseConfig Database { get; set; } = new();
    public RedisConfig Redis { get; set; } = new();
    public PlatformConfig Platforms { get; set; } = new();
    public MonitoringConfig Monitoring { get; set; } = new();
    public HealthCheckConfig HealthCheck { get; set; } = new();

    public void Validate()
    {
        Database.Validate();
        Redis.Validate();
        Platforms.Validate();
        Monitoring.Validate();
        HealthCheck.Validate();
    }
}

public class DatabaseConfig
{
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    public int CommandTimeoutSeconds { get; set; } = 60;
    public int MaxRetryCount { get; set; } = 3;

    public void Validate()
    {
        if (ConnectionTimeoutSeconds <= 0)
            throw new InvalidOperationException("Database connection timeout must be positive");

        if (CommandTimeoutSeconds <= 0)
            throw new InvalidOperationException("Database command timeout must be positive");
    }
}

public class RedisConfig
{
    public int Database { get; set; } = 0;
    public int ConnectionTimeoutMs { get; set; } = 5000;
    public int SyncTimeoutMs { get; set; } = 1000;
    public int MaxRetryCount { get; set; } = 3;
    public bool AbortOnConnectFail { get; set; } = false;

    public void Validate()
    {
        if (ConnectionTimeoutMs <= 0)
            throw new InvalidOperationException("Redis connection timeout must be positive");
    }
}

public class PlatformConfig
{
    public YouTubeConfig YouTube { get; set; } = new();
    public TwitchConfig Twitch { get; set; } = new();
    public TwitterConfig Twitter { get; set; } = new();
    public TwitCastingConfig TwitCasting { get; set; } = new();

    public void Validate()
    {
        YouTube.Validate();
        Twitch.Validate();
        Twitter.Validate();
        TwitCasting.Validate();
    }
}

public class YouTubeConfig
{
    public List<string> ApiKeys { get; set; } = new();
    public int QuotaLimit { get; set; } = 10000;
    public int CheckIntervalSeconds { get; set; } = 30;
    public int MaxConcurrentRequests { get; set; } = 10;
    public string WebhookSecret { get; set; } = "";
    public string WebhookCallbackUrl { get; set; } = "";

    public void Validate()
    {
        if (ApiKeys.Count == 0)
            throw new InvalidOperationException("At least one YouTube API key is required");

        if (CheckIntervalSeconds <= 0)
            throw new InvalidOperationException("YouTube check interval must be positive");
    }
}

public class TwitchConfig
{
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public int CheckIntervalSeconds { get; set; } = 30;
    public int MaxConcurrentRequests { get; set; } = 10;
    public string WebhookSecret { get; set; } = "";
    public string WebhookCallbackUrl { get; set; } = "";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ClientId))
            throw new InvalidOperationException("Twitch Client ID is required");

        if (string.IsNullOrWhiteSpace(ClientSecret))
            throw new InvalidOperationException("Twitch Client Secret is required");

        if (CheckIntervalSeconds <= 0)
            throw new InvalidOperationException("Twitch check interval must be positive");
    }
}

public class TwitterConfig
{
    public string BearerToken { get; set; } = "";
    public string ConsumerKey { get; set; } = "";
    public string ConsumerSecret { get; set; } = "";
    public int CheckIntervalSeconds { get; set; } = 60;
    public int MaxConcurrentRequests { get; set; } = 5;
    public int RateLimitPerWindowRequests { get; set; } = 75;
    public int RateLimitWindowMinutes { get; set; } = 15;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BearerToken) &&
            (string.IsNullOrWhiteSpace(ConsumerKey) || string.IsNullOrWhiteSpace(ConsumerSecret)))
            throw new InvalidOperationException("Twitter authentication credentials are required");

        if (CheckIntervalSeconds <= 0)
            throw new InvalidOperationException("Twitter check interval must be positive");
    }
}

public class TwitCastingConfig
{
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public int CheckIntervalSeconds { get; set; } = 30;
    public int MaxConcurrentRequests { get; set; } = 10;
    public int RateLimitPerHourRequests { get; set; } = 1000;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ClientId))
            throw new InvalidOperationException("TwitCasting Client ID is required");

        if (CheckIntervalSeconds <= 0)
            throw new InvalidOperationException("TwitCasting check interval must be positive");
    }
}

public class MonitoringConfig
{
    public int CheckIntervalSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
    public int BatchSize { get; set; } = 100;
    public bool EnableDetailedLogging { get; set; } = false;

    public void Validate()
    {
        if (CheckIntervalSeconds <= 0)
            throw new InvalidOperationException("Monitoring check interval must be positive");

        if (MaxRetryAttempts < 0)
            throw new InvalidOperationException("Max retry attempts must be non-negative");

        if (BatchSize <= 0)
            throw new InvalidOperationException("Batch size must be positive");
    }
}

public class HealthCheckConfig
{
    public int Port { get; set; } = 6111;
    public string Endpoint { get; set; } = "/health";
    public int TimeoutSeconds { get; set; } = 30;
    public bool EnableDetailedResponse { get; set; } = true;

    public void Validate()
    {
        if (Port <= 0 || Port > 65535)
            throw new InvalidOperationException("Health check port must be between 1 and 65535");

        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new InvalidOperationException("Health check endpoint is required");

        if (TimeoutSeconds <= 0)
            throw new InvalidOperationException("Health check timeout must be positive");
    }
}
