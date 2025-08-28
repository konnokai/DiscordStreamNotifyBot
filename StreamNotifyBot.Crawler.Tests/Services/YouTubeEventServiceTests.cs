using Microsoft.Extensions.Logging;
using Moq;
using StreamNotifyBot.Crawler.Configuration;
using StreamNotifyBot.Crawler.Models;
using StreamNotifyBot.Crawler.Services;
using StackExchange.Redis;
using Xunit;

namespace StreamNotifyBot.Crawler.Tests.Services;

/// <summary>
/// YouTube 事件服務單元測試
/// </summary>
public class YouTubeEventServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<ISubscriber> _mockSubscriber;
    private readonly Mock<ILogger<YouTubeEventService>> _mockLogger;
    private readonly RedisConfig _redisConfig;
    private readonly YouTubeEventService _eventService;

    public YouTubeEventServiceTests()
    {
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockSubscriber = new Mock<ISubscriber>();
        _mockLogger = new Mock<ILogger<YouTubeEventService>>();

        _redisConfig = new RedisConfig
        {
            KeyPrefix = "test-crawler"
        };

        _mockRedis.Setup(x => x.GetSubscriber(It.IsAny<object>()))
               .Returns(_mockSubscriber.Object);

        _eventService = new YouTubeEventService(
            _mockRedis.Object,
            _mockLogger.Object,
            _redisConfig);
    }

    [Fact]
    public async Task BroadcastStreamStartAsync_ValidVideoInfo_PublishesCorrectEvent()
    {
        // Arrange
        var videoInfo = new YouTubeVideoInfo
        {
            VideoId = "test-video-id",
            ChannelId = "test-channel-id",
            Title = "Test Video",
            ChannelTitle = "Test Channel",
            IsLiveContent = true
        };

        _mockSubscriber.Setup(x => x.PublishAsync(
                It.IsAny<RedisChannel>(), 
                It.IsAny<RedisValue>(), 
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);

        // Act
        await _eventService.BroadcastStreamStartAsync(videoInfo);

        // Assert
        _mockSubscriber.Verify(x => x.PublishAsync(
            It.Is<RedisChannel>(ch => ch.ToString().Contains("stream.start")),
            It.Is<RedisValue>(msg => msg.ToString().Contains("test-video-id")),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task BroadcastStreamStartAsync_NullVideoInfo_LogsWarningAndReturns()
    {
        // Act
        await _eventService.BroadcastStreamStartAsync(null!);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("null video info")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockSubscriber.Verify(x => x.PublishAsync(
            It.IsAny<RedisChannel>(),
            It.IsAny<RedisValue>(),
            It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task BroadcastStreamEndAsync_ValidVideoInfo_PublishesCorrectEvent()
    {
        // Arrange
        var videoInfo = new YouTubeVideoInfo
        {
            VideoId = "test-video-id",
            ChannelId = "test-channel-id",
            Title = "Test Video",
            ChannelTitle = "Test Channel",
            ActualStartTime = DateTime.UtcNow.AddHours(-2),
            ActualEndTime = DateTime.UtcNow,
            ViewerCount = 1000
        };

        _mockSubscriber.Setup(x => x.PublishAsync(
                It.IsAny<RedisChannel>(), 
                It.IsAny<RedisValue>(), 
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);

        // Act
        await _eventService.BroadcastStreamEndAsync(videoInfo);

        // Assert
        _mockSubscriber.Verify(x => x.PublishAsync(
            It.Is<RedisChannel>(ch => ch.ToString().Contains("stream.end")),
            It.Is<RedisValue>(msg => msg.ToString().Contains("test-video-id")),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task BroadcastChannelUpdateAsync_ValidData_PublishesCorrectEvent()
    {
        // Arrange
        const string channelId = "test-channel-id";
        const string channelTitle = "Test Channel";
        var additionalData = new Dictionary<string, object>
        {
            ["UpdateType"] = "TitleChange",
            ["PreviousTitle"] = "Old Title"
        };

        _mockSubscriber.Setup(x => x.PublishAsync(
                It.IsAny<RedisChannel>(), 
                It.IsAny<RedisValue>(), 
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);

        // Act
        await _eventService.BroadcastChannelUpdateAsync(channelId, channelTitle, additionalData);

        // Assert
        _mockSubscriber.Verify(x => x.PublishAsync(
            It.Is<RedisChannel>(ch => ch.ToString().Contains("channel.update")),
            It.Is<RedisValue>(msg => msg.ToString().Contains(channelId) && msg.ToString().Contains(channelTitle)),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task BroadcastChannelUpdateAsync_EmptyChannelId_LogsWarningAndReturns()
    {
        // Act
        await _eventService.BroadcastChannelUpdateAsync("", "Test Channel");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("empty channel ID")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastErrorEventAsync_ValidError_PublishesCorrectEvent()
    {
        // Arrange
        const string errorType = "ApiError";
        const string message = "Test error message";
        var context = new Dictionary<string, object>
        {
            ["VideoId"] = "test-video-id",
            ["ChannelId"] = "test-channel-id"
        };

        _mockSubscriber.Setup(x => x.PublishAsync(
                It.IsAny<RedisChannel>(), 
                It.IsAny<RedisValue>(), 
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);

        // Act
        await _eventService.BroadcastErrorEventAsync(errorType, message, context);

        // Assert
        _mockSubscriber.Verify(x => x.PublishAsync(
            It.Is<RedisChannel>(ch => ch.ToString().Contains("error")),
            It.Is<RedisValue>(msg => msg.ToString().Contains(errorType) && msg.ToString().Contains(message)),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task BroadcastMonitoringStatsAsync_ValidStats_PublishesCorrectEvent()
    {
        // Arrange
        const int channelsChecked = 100;
        const int videosFound = 25;
        const int errorsEncountered = 5;
        var duration = TimeSpan.FromMinutes(10);

        _mockSubscriber.Setup(x => x.PublishAsync(
                It.IsAny<RedisChannel>(), 
                It.IsAny<RedisValue>(), 
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);

        // Act
        await _eventService.BroadcastMonitoringStatsAsync(channelsChecked, videosFound, errorsEncountered, duration);

        // Assert
        _mockSubscriber.Verify(x => x.PublishAsync(
            It.Is<RedisChannel>(ch => ch.ToString().Contains("monitoring.stats")),
            It.Is<RedisValue>(msg => msg.ToString().Contains(channelsChecked.ToString())),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task BroadcastBatchEventsAsync_MultipleEvents_PublishesAllEvents()
    {
        // Arrange
        var events = new List<(string EventType, object EventData)>
        {
            ("test.event1", new { Data = "Event 1" }),
            ("test.event2", new { Data = "Event 2" }),
            ("test.event3", new { Data = "Event 3" })
        };

        _mockSubscriber.Setup(x => x.PublishAsync(
                It.IsAny<RedisChannel>(), 
                It.IsAny<RedisValue>(), 
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);

        // Act
        await _eventService.BroadcastBatchEventsAsync(events);

        // Assert
        _mockSubscriber.Verify(x => x.PublishAsync(
            It.IsAny<RedisChannel>(),
            It.IsAny<RedisValue>(),
            It.IsAny<CommandFlags>()), Times.Exactly(3));
    }

    [Fact]
    public async Task BroadcastBatchEventsAsync_EmptyEvents_DoesNotPublish()
    {
        // Arrange
        var events = new List<(string EventType, object EventData)>();

        // Act
        await _eventService.BroadcastBatchEventsAsync(events);

        // Assert
        _mockSubscriber.Verify(x => x.PublishAsync(
            It.IsAny<RedisChannel>(),
            It.IsAny<RedisValue>(),
            It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public void IsConnected_RedisConnected_ReturnsTrue()
    {
        // Arrange
        _mockRedis.Setup(x => x.IsConnected).Returns(true);

        // Act
        var isConnected = _eventService.IsConnected;

        // Assert
        Assert.True(isConnected);
    }

    [Fact]
    public void IsConnected_RedisDisconnected_ReturnsFalse()
    {
        // Arrange
        _mockRedis.Setup(x => x.IsConnected).Returns(false);

        // Act
        var isConnected = _eventService.IsConnected;

        // Assert
        Assert.False(isConnected);
    }

    [Fact]
    public void Constructor_NullRedis_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new YouTubeEventService(null!, _mockLogger.Object, _redisConfig));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new YouTubeEventService(_mockRedis.Object, null!, _redisConfig));
    }

    [Fact]
    public void Constructor_NullRedisConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new YouTubeEventService(_mockRedis.Object, _mockLogger.Object, null!));
    }
}
