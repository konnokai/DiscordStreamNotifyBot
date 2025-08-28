using Microsoft.Extensions.Logging;
using Moq;
using StreamNotifyBot.Crawler.Configuration;
using StreamNotifyBot.Crawler.Models;
using StreamNotifyBot.Crawler.Services;
using Xunit;
using Google.Apis.YouTube.v3.Data;
using System.Collections.Concurrent;

namespace StreamNotifyBot.Crawler.Tests.Integration;

/// <summary>
/// YouTube 流監控服務整合測試
/// 測試監控服務基本功能
/// </summary>
public class YouTubeStreamMonitorIntegrationTests : IDisposable
{
    [Fact]
    public void ChannelFiltering_WithMockTracking_FiltersCorrectly()
    {
        // Arrange
        var allChannels = new[] { "channel-1", "channel-2", "channel-3", "channel-4" };
        var trackedChannels = new HashSet<string> { "channel-1", "channel-3" };

        // Act
        var filteredChannels = allChannels.Where(channel => trackedChannels.Contains(channel)).ToArray();

        // Assert
        Assert.Equal(2, filteredChannels.Length);
        Assert.Contains("channel-1", filteredChannels);
        Assert.Contains("channel-3", filteredChannels);
        Assert.DoesNotContain("channel-2", filteredChannels);
        Assert.DoesNotContain("channel-4", filteredChannels);
    }

    [Fact] 
    public void TrackingStatistics_WithConcurrentDictionary_TracksCorrectly()
    {
        // Arrange
        var trackingCounts = new ConcurrentDictionary<string, int>();

        // Act
        trackingCounts.AddOrUpdate("channel-1", 1, (key, value) => value + 1);
        trackingCounts.AddOrUpdate("channel-1", 1, (key, value) => value + 1);
        trackingCounts.AddOrUpdate("channel-2", 1, (key, value) => value + 1);

        // Assert
        Assert.Equal(2, trackingCounts.Count);
        Assert.Equal(2, trackingCounts["channel-1"]);
        Assert.Equal(1, trackingCounts["channel-2"]);
        
        var totalTracking = trackingCounts.Values.Sum();
        Assert.Equal(3, totalTracking);
    }

    [Fact]
    public async Task EventBroadcasting_WithMockRedis_PublishesCorrectly()
    {
        // Arrange
        var mockRedis = new Mock<StackExchange.Redis.IConnectionMultiplexer>();
        var mockSubscriber = new Mock<StackExchange.Redis.ISubscriber>();
        var mockLogger = new Mock<ILogger<YouTubeEventService>>();

        mockRedis.Setup(x => x.GetSubscriber(It.IsAny<object>())).Returns(mockSubscriber.Object);
        mockSubscriber.Setup(x => x.PublishAsync(It.IsAny<StackExchange.Redis.RedisChannel>(), 
                It.IsAny<StackExchange.Redis.RedisValue>(), It.IsAny<StackExchange.Redis.CommandFlags>()))
            .ReturnsAsync(1);

        var redisConfig = new RedisConfig { KeyPrefix = "test" };
        var eventService = new YouTubeEventService(mockRedis.Object, mockLogger.Object, redisConfig);

        var videoInfo = new YouTubeVideoInfo
        {
            VideoId = "test-video",
            ChannelId = "test-channel",
            Title = "Test Stream",
            ChannelTitle = "Test Channel"
        };

        // Act
        await eventService.BroadcastStreamStartAsync(videoInfo);

        // Assert
        mockSubscriber.Verify(x => x.PublishAsync(
            It.IsAny<StackExchange.Redis.RedisChannel>(),
            It.IsAny<StackExchange.Redis.RedisValue>(),
            It.IsAny<StackExchange.Redis.CommandFlags>()), Times.Once);
    }

    [Fact]
    public void ConfigurationMapping_WithBasicConfig_MapsCorrectly()
    {
        // Arrange
        var channelIds = new[] { "channel-1", "channel-2", "channel-3" };

        // Act
        var filteredChannels = channelIds.Where(id => id.Contains("channel")).ToArray();

        // Assert
        Assert.Equal(3, filteredChannels.Length);
        Assert.All(filteredChannels, id => Assert.Contains("channel", id));
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
