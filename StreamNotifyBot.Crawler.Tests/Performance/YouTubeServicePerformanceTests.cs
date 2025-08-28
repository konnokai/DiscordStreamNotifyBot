using Microsoft.Extensions.Logging;
using Moq;
using StreamNotifyBot.Crawler.Configuration;
using StreamNotifyBot.Crawler.Models;
using StreamNotifyBot.Crawler.Services;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;
using Google.Apis.YouTube.v3.Data;
using System.Collections.Concurrent;

namespace StreamNotifyBot.Crawler.Tests.Performance;

/// <summary>
/// YouTube 服務效能測試
/// 測試大量頻道監控、API 調用效能、記憶體使用等
/// </summary>
public class YouTubeServicePerformanceTests
{
    private readonly ITestOutputHelper _output;

    public YouTubeServicePerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ConcurrentDictionary_LargeScaleOperations_PerformsWithinLimits()
    {
        // Arrange
        const int channelCount = 10000;
        const int operationsPerChannel = 100;

        var trackingCounts = new ConcurrentDictionary<string, int>();
        var channels = Enumerable.Range(1, channelCount).Select(i => $"channel-{i}").ToList();

        // Act & Measure
        var stopwatch = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(true);

        // 大量添加追蹤
        Parallel.ForEach(channels, channel =>
        {
            for (int i = 0; i < operationsPerChannel; i++)
            {
                trackingCounts.AddOrUpdate(channel, 1, (key, value) => value + 1);
            }
        });

        var addTime = stopwatch.Elapsed;
        var addMemory = GC.GetTotalMemory(false) - initialMemory;

        // 測試查詢效能
        stopwatch.Restart();
        var queryResults = new List<bool>();
        Parallel.ForEach(channels, channel =>
        {
            lock (queryResults)
            {
                queryResults.Add(trackingCounts.ContainsKey(channel));
            }
        });

        var queryTime = stopwatch.Elapsed;

        // 測試統計效能
        stopwatch.Restart();
        var totalChannels = trackingCounts.Count;
        var totalTrackingCount = trackingCounts.Values.Sum();
        var statsTime = stopwatch.Elapsed;

        stopwatch.Stop();
        var finalMemory = GC.GetTotalMemory(false);

        // Assert & Report
        _output.WriteLine($"Performance Results for {channelCount} channels with {operationsPerChannel} operations each:");
        _output.WriteLine($"Add Operations: {addTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"Query Operations: {queryTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"Statistics Generation: {statsTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"Memory Usage: {addMemory / 1024 / 1024:F2}MB increase");
        _output.WriteLine($"Final Memory: {finalMemory / 1024 / 1024:F2}MB");
        _output.WriteLine($"Tracked Channels: {totalChannels}");
        _output.WriteLine($"Total Tracking Count: {totalTrackingCount}");

        // Performance assertions
        Assert.True(addTime.TotalMilliseconds < 10000, $"Add operations took too long: {addTime.TotalMilliseconds}ms");
        Assert.True(queryTime.TotalMilliseconds < 2000, $"Query operations took too long: {queryTime.TotalMilliseconds}ms");
        Assert.True(statsTime.TotalMilliseconds < 1000, $"Statistics generation took too long: {statsTime.TotalMilliseconds}ms");
        Assert.True(addMemory < 200 * 1024 * 1024, $"Memory usage too high: {addMemory / 1024 / 1024}MB"); // 200MB limit

        // Correctness assertions
        Assert.Equal(channelCount, totalChannels);
        Assert.Equal(channelCount * operationsPerChannel, totalTrackingCount);
        Assert.All(queryResults, result => Assert.True(result));
    }

    [Fact]
    public void EventService_HighVolumePublishing_MaintainsPerformance()
    {
        // Arrange
        var mockRedis = new Mock<StackExchange.Redis.IConnectionMultiplexer>();
        var mockSubscriber = new Mock<StackExchange.Redis.ISubscriber>();
        var mockLogger = new Mock<ILogger<YouTubeEventService>>();

        mockRedis.Setup(x => x.GetSubscriber(It.IsAny<object>())).Returns(mockSubscriber.Object);
        mockSubscriber.Setup(x => x.PublishAsync(It.IsAny<StackExchange.Redis.RedisChannel>(), 
                It.IsAny<StackExchange.Redis.RedisValue>(), It.IsAny<StackExchange.Redis.CommandFlags>()))
            .ReturnsAsync(1);

        var redisConfig = new RedisConfig { KeyPrefix = "perf-test" };
        var eventService = new YouTubeEventService(mockRedis.Object, mockLogger.Object, redisConfig);

        const int eventCount = 10000;
        var events = new List<(string EventType, object EventData)>();

        for (int i = 0; i < eventCount; i++)
        {
            events.Add(($"test.event.{i % 5}", new { 
                Id = i, 
                Timestamp = DateTime.UtcNow,
                Data = $"Event data {i}" 
            }));
        }

        // Act
        var stopwatch = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(true);

        var publishTask = eventService.BroadcastBatchEventsAsync(events);
        publishTask.Wait();

        stopwatch.Stop();
        var finalMemory = GC.GetTotalMemory(false);

        // Report
        _output.WriteLine($"Event Publishing Performance:");
        _output.WriteLine($"Events: {eventCount}");
        _output.WriteLine($"Time: {stopwatch.Elapsed.TotalMilliseconds:F2}ms");
        _output.WriteLine($"Events/second: {eventCount / stopwatch.Elapsed.TotalSeconds:F2}");
        _output.WriteLine($"Memory Change: {(finalMemory - initialMemory) / 1024:F2}KB");

        // Assert
        Assert.True(stopwatch.Elapsed.TotalSeconds < 30, "Event publishing too slow");
        mockSubscriber.Verify(x => x.PublishAsync(
            It.IsAny<StackExchange.Redis.RedisChannel>(),
            It.IsAny<StackExchange.Redis.RedisValue>(),
            It.IsAny<StackExchange.Redis.CommandFlags>()), Times.Exactly(eventCount));
    }

    [Theory]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(1000)]
    public void ScalabilityTest_VariousChannelCounts_LinearPerformance(int channelCount)
    {
        // Arrange
        var channels = Enumerable.Range(1, channelCount).Select(i => $"channel-{i}").ToArray();
        var trackingCounts = new ConcurrentDictionary<string, int>();

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        Parallel.ForEach(channels, channel =>
        {
            trackingCounts.AddOrUpdate(channel, 1, (key, value) => value + 1);
            Thread.Sleep(1); // 模擬一些處理時間
        });
        
        stopwatch.Stop();

        var timePerChannel = stopwatch.Elapsed.TotalMilliseconds / channelCount;

        // Report
        _output.WriteLine($"Scalability Test - {channelCount} channels:");
        _output.WriteLine($"Total Time: {stopwatch.Elapsed.TotalMilliseconds:F2}ms");
        _output.WriteLine($"Time per Channel: {timePerChannel:F2}ms");

        // Assert - 效能應該大致線性增長
        Assert.True(timePerChannel < 100, $"Performance degraded: {timePerChannel}ms per channel");
        Assert.True(stopwatch.Elapsed.TotalSeconds < 120, "Overall time too long");
        Assert.Equal(channelCount, trackingCounts.Count);
    }
}
