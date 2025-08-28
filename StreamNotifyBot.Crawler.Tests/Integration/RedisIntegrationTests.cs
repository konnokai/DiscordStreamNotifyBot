using DiscordStreamNotifyBot.DataBase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using StreamNotifyBot.Crawler.Configuration;
using StreamNotifyBot.Crawler.Models;
using StreamNotifyBot.Crawler.Services;
using System.Text.Json;
using Xunit;

namespace StreamNotifyBot.Crawler.Tests.Integration;

/// <summary>
/// Redis PubSub 整合測試
/// 測試 Redis 事件發布和訂閱功能
/// </summary>
public class RedisIntegrationTests : IDisposable
{
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<ISubscriber> _mockSubscriber;
    private readonly Mock<ILogger<YouTubeEventService>> _mockEventLogger;
    private readonly Mock<ILogger<YouTubeTrackingManager>> _mockTrackingLogger;
    private readonly RedisConfig _redisConfig;
    private readonly YouTubeEventService _eventService;
    private readonly YouTubeTrackingManager _trackingManager;

    public RedisIntegrationTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .Build();

        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockSubscriber = new Mock<ISubscriber>();
        _mockEventLogger = new Mock<ILogger<YouTubeEventService>>();
        _mockTrackingLogger = new Mock<ILogger<YouTubeTrackingManager>>();

        _redisConfig = new RedisConfig
        {
            KeyPrefix = "test-integration"
        };

        _mockRedis.Setup(x => x.GetSubscriber(It.IsAny<object>()))
               .Returns(_mockSubscriber.Object);

        _eventService = new YouTubeEventService(
            _mockRedis.Object,
            _mockEventLogger.Object,
            _redisConfig);

        var dbConnectionString = configuration.GetConnectionString("Database");
        if (string.IsNullOrEmpty(dbConnectionString))
        {
            throw new InvalidOperationException("Database connection string is not configured");
        }
        var mockDbService = new Mock<MainDbService>(dbConnectionString);
        var mockEventService = new Mock<YouTubeEventService>(
            _mockRedis.Object,
            _mockEventLogger.Object,
            _redisConfig);

        _trackingManager = new YouTubeTrackingManager(
            _mockTrackingLogger.Object,
            _mockRedis.Object,
            mockDbService.Object,
            mockEventService.Object,
            _redisConfig);
    }

    [Fact]
    public async Task EventService_PublishStreamStart_TracksCorrectChannel()
    {
        // Arrange
        var videoInfo = new YouTubeVideoInfo
        {
            VideoId = "test-video-id",
            ChannelId = "test-channel-id",
            Title = "Test Stream",
            ChannelTitle = "Test Channel",
            IsLiveContent = true
        };

        var publishedMessage = string.Empty;
        _mockSubscriber.Setup(x => x.PublishAsync(
                It.IsAny<RedisChannel>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisChannel, RedisValue, CommandFlags>((ch, msg, flags) =>
            {
                publishedMessage = msg.ToString();
            })
            .ReturnsAsync(1);

        // Act
        await _eventService.BroadcastStreamStartAsync(videoInfo);

        // Assert
        Assert.NotEmpty(publishedMessage);
        Assert.Contains(videoInfo.VideoId, publishedMessage);
        Assert.Contains(videoInfo.ChannelId, publishedMessage);

        var eventData = JsonSerializer.Deserialize<Dictionary<string, object>>(publishedMessage);
        Assert.NotNull(eventData);
        Assert.True(eventData.ContainsKey("videoId"));
        Assert.True(eventData.ContainsKey("channelId"));
    }

    [Fact]
    public async Task TrackingManager_FollowUnfolowWorkflow_UpdatesTrackingCorrectly()
    {
        // Arrange
        const string streamKey = "test-stream-key";
        var followEvent = new
        {
            StreamKey = streamKey,
            ChannelId = 123,
            Platform = "youtube",
            GuildId = 123,
            UserId = 123
        };

        var unfollowEvent = new
        {
            StreamKey = streamKey,
            ChannelId = 123,
            Platform = "youtube",
            GuildId = 123,
            UserId = 123
        };

        var followMessage = JsonSerializer.Serialize(followEvent);
        var unfollowMessage = JsonSerializer.Serialize(unfollowEvent);

        Action<RedisChannel, RedisValue>? followHandler = null;
        Action<RedisChannel, RedisValue>? unfollowHandler = null;

        _mockSubscriber.Setup(x => x.SubscribeAsync(
                It.Is<RedisChannel>(ch => ch.ToString().Contains("stream.follow")),
                It.IsAny<Action<RedisChannel, RedisValue>>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisChannel, Action<RedisChannel, RedisValue>, CommandFlags>((ch, handler, flags) =>
            {
                followHandler = handler;
            });

        _mockSubscriber.Setup(x => x.SubscribeAsync(
                It.Is<RedisChannel>(ch => ch.ToString().Contains("stream.unfollow")),
                It.IsAny<Action<RedisChannel, RedisValue>>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisChannel, Action<RedisChannel, RedisValue>, CommandFlags>((ch, handler, flags) =>
            {
                unfollowHandler = handler;
            });

        // Act - 啟動追蹤管理器
        await _trackingManager.StartAsync(CancellationToken.None);

        // 驗證初始狀態
        Assert.False(_trackingManager.IsChannelTracked(streamKey));
        Assert.Equal(0, _trackingManager.GetChannelTrackingCount(streamKey));

        // 模擬接收 follow 事件
        Assert.NotNull(followHandler);
        followHandler(new RedisChannel("stream.follow", RedisChannel.PatternMode.Auto), followMessage);

        // 等待異步處理完成
        await Task.Delay(100);

        // 驗證追蹤狀態
        Assert.True(_trackingManager.IsChannelTracked(streamKey));
        Assert.Equal(1, _trackingManager.GetChannelTrackingCount(streamKey));

        // 模擬接收 unfollow 事件
        Assert.NotNull(unfollowHandler);
        unfollowHandler(new RedisChannel("stream.unfollow", RedisChannel.PatternMode.Auto), unfollowMessage);

        // 等待異步處理完成
        await Task.Delay(100);

        // 驗證追蹤狀態已清除
        Assert.False(_trackingManager.IsChannelTracked(streamKey));
        Assert.Equal(0, _trackingManager.GetChannelTrackingCount(streamKey));

        // Assert
        _mockSubscriber.Verify(x => x.SubscribeAsync(
            It.IsAny<RedisChannel>(),
            It.IsAny<Action<RedisChannel, RedisValue>>(),
            It.IsAny<CommandFlags>()), Times.Exactly(2));
    }

    [Fact]
    public async Task EventService_BatchBroadcast_PublishesAllEvents()
    {
        // Arrange
        var events = new List<(string EventType, object EventData)>
        {
            ("stream.start", new { VideoId = "video1", ChannelId = "channel1" }),
            ("stream.start", new { VideoId = "video2", ChannelId = "channel2" }),
            ("channel.update", new { ChannelId = "channel1", Title = "Updated Title" })
        };

        var publishedEvents = new List<(string Channel, string Message)>();
        _mockSubscriber.Setup(x => x.PublishAsync(
                It.IsAny<RedisChannel>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisChannel, RedisValue, CommandFlags>((ch, msg, flags) =>
            {
                publishedEvents.Add((ch.ToString(), msg.ToString()));
            })
            .ReturnsAsync(1);

        // Act
        await _eventService.BroadcastBatchEventsAsync(events);

        // Assert
        Assert.Equal(3, publishedEvents.Count);

        var streamStartEvents = publishedEvents.Where(e => e.Channel.Contains("stream.start")).ToList();
        var channelUpdateEvents = publishedEvents.Where(e => e.Channel.Contains("channel.update")).ToList();

        Assert.Equal(2, streamStartEvents.Count);
        Assert.Single(channelUpdateEvents);

        // 驗證事件內容
        Assert.Contains("video1", streamStartEvents[0].Message);
        Assert.Contains("video2", streamStartEvents[1].Message);
        Assert.Contains("Updated Title", channelUpdateEvents[0].Message);
    }

    [Fact]
    public async Task TrackingManager_MultipleGuildsTracking_HandlesCorrectly()
    {
        // Arrange
        const string streamKey = "shared-channel";
        var followEvents = new[]
        {
            new { StreamKey = streamKey, Platform = "youtube", GuildId = 123, UserId = 123 },
            new { StreamKey = streamKey, Platform = "youtube", GuildId = 456, UserId = 456 },
            new { StreamKey = streamKey, Platform = "youtube", GuildId = 789, UserId = 789 }
        };

        Action<RedisChannel, RedisValue>? followHandler = null;
        _mockSubscriber.Setup(x => x.SubscribeAsync(
                It.Is<RedisChannel>(ch => ch.ToString().Contains("stream.follow")),
                It.IsAny<Action<RedisChannel, RedisValue>>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisChannel, Action<RedisChannel, RedisValue>, CommandFlags>((ch, handler, flags) =>
            {
                followHandler = handler;
            });

        // Act
        await _trackingManager.StartAsync(CancellationToken.None);
        Assert.NotNull(followHandler);

        // 模擬多個 guild 追蹤同一頻道
        foreach (var followEvent in followEvents)
        {
            var message = JsonSerializer.Serialize(followEvent);
            followHandler(new RedisChannel("test:stream.follow", RedisChannel.PatternMode.Auto), message);
            await Task.Delay(50); // 確保順序處理
        }

        // 等待處理完成
        await Task.Delay(200);

        // Assert
        Assert.True(_trackingManager.IsChannelTracked(streamKey));
        Assert.Equal(3, _trackingManager.GetChannelTrackingCount(streamKey));

        var stats = await _trackingManager.GetTrackingStatsAsync();
        Assert.True((int)stats["TotalTrackedChannels"] >= 1);
        Assert.True((int)stats["TotalTrackingCount"] >= 3);
    }

    [Fact]
    public async Task EventService_ConnectionLost_HandlesGracefully()
    {
        // Arrange
        _mockRedis.Setup(x => x.IsConnected).Returns(false);
        _mockSubscriber.Setup(x => x.PublishAsync(
                It.IsAny<RedisChannel>(),
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection lost"));

        var videoInfo = new YouTubeVideoInfo
        {
            VideoId = "test-video-id",
            ChannelId = "test-channel-id",
            Title = "Test Stream",
            ChannelTitle = "Test Channel"
        };

        // Act & Assert - 應該不拋出異常
        await _eventService.BroadcastStreamStartAsync(videoInfo);

        // 驗證連接狀態檢查
        Assert.False(_eventService.IsConnected);

        // 驗證錯誤記錄
        _mockEventLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to broadcast")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrackingManager_InvalidEventData_HandlesGracefully()
    {
        // Arrange
        var invalidMessages = new[]
        {
            "invalid json",
            "{}",
            "{\"StreamKey\": null}",
            "{\"StreamKey\": \"\"}",
            "{\"StreamKey\": \"valid-key\"}",
            "{\"StreamKey\": \"valid-key\", \"Platform\": \"twitch\"}",
            "{\"StreamKey\": \"valid-key\", \"Platform\": \"youtube\"}",
            "{\"StreamKey\": \"valid-key\", \"Platform\": \"youtube\", \"GuildId\": null}",
            "{\"StreamKey\": \"valid-key\", \"Platform\": \"youtube\", \"GuildId\": \"not-a-number\"}",
            "{\"StreamKey\": \"valid-key\", \"Platform\": \"youtube\", \"GuildId\": 123, \"UserId\": null}",
            "{\"StreamKey\": \"valid-key\", \"Platform\": \"youtube\", \"GuildId\": 123, \"UserId\": \"not-a-number\"}",
            "{\"Platform\": \"youtube\", \"GuildId\": 123, \"UserId\": 123}",
            "{\"StreamKey\": \"valid-key\", \"GuildId\": 123, \"UserId\": 123}",
            ""
        };

        Action<RedisChannel, RedisValue>? followHandler = null;
        _mockSubscriber.Setup(x => x.SubscribeAsync(
                It.Is<RedisChannel>(ch => ch.ToString().Contains("stream.follow")),
                It.IsAny<Action<RedisChannel, RedisValue>>(),
                It.IsAny<CommandFlags>()))
            .Callback<RedisChannel, Action<RedisChannel, RedisValue>, CommandFlags>((ch, handler, flags) =>
            {
                followHandler = handler;
            });

        // Act
        await _trackingManager.StartAsync(CancellationToken.None);
        Assert.NotNull(followHandler);

        // Assert - 初始資料數量為 0
        var initialTrackedChannels = await _trackingManager.GetTrackedChannelsAsync();
        var initialTrackedCount = initialTrackedChannels.Count;
        Assert.Equal(0, initialTrackedCount);

        // 發送無效訊息
        foreach (var invalidMessage in invalidMessages)
        {
            followHandler(new RedisChannel("test:stream.follow", RedisChannel.PatternMode.Auto), invalidMessage);
            await Task.Delay(50);
        }

        // 等待處理完成
        await Task.Delay(200);

        // Assert - 會有 1 條紀錄成功執行
        var finalTrackedChannels = await _trackingManager.GetTrackedChannelsAsync();
        Assert.Single(finalTrackedChannels);

        // 驗證錯誤記錄（可能有多次錯誤）
        _mockTrackingLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error processing") || 
                                           v.ToString()!.Contains("Invalid event data") || 
                                           v.ToString()!.Contains("Failed to")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    public void Dispose()
    {
        _trackingManager?.Dispose();
        GC.SuppressFinalize(this);
    }
}
