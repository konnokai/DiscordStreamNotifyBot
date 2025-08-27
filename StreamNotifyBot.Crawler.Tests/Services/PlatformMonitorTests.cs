using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
using FluentAssertions;
using StreamNotifyBot.Crawler.Services;
using StreamNotifyBot.Crawler.Models;

namespace StreamNotifyBot.Crawler.Tests.Services;

/// <summary>
/// 測試平台監控器介面和基礎功能
/// </summary>
public class PlatformMonitorTests
{
    [Fact]
    public void PlatformMonitorStatus_ShouldInitialize_WithDefaultValues()
    {
        // Act
        var status = new PlatformMonitorStatus();

        // Assert
        status.PlatformName.Should().Be("");
        status.IsHealthy.Should().BeFalse();
        status.MonitoredStreamsCount.Should().Be(0);
        status.ErrorMessage.Should().BeNull();
        status.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void PlatformMonitorStatus_ShouldSetProperties_Correctly()
    {
        // Arrange & Act
        var status = new PlatformMonitorStatus
        {
            PlatformName = "YouTube",
            IsHealthy = true,
            MonitoredStreamsCount = 10,
            LastUpdateTime = DateTime.UtcNow,
            ErrorMessage = null,
            Metadata = new Dictionary<string, object> { { "test_key", "test_value" } }
        };

        // Assert
        status.PlatformName.Should().Be("YouTube");
        status.IsHealthy.Should().BeTrue();
        status.MonitoredStreamsCount.Should().Be(10);
        status.ErrorMessage.Should().BeNull();
        status.Metadata.Should().ContainKey("test_key").WhoseValue.Should().Be("test_value");
    }

    [Fact]
    public void StreamStatusChangedEventArgs_ShouldInitialize_WithCurrentTimestamp()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);
        
        // Act
        var eventArgs = new StreamStatusChangedEventArgs();
        
        var afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Assert
        eventArgs.Timestamp.Should().BeAfter(beforeCreation);
        eventArgs.Timestamp.Should().BeBefore(afterCreation);
        eventArgs.IsOnline.Should().BeFalse(); // 預設值
    }

    [Fact]
    public void StreamData_ShouldBeAssignable_InEventArgs()
    {
        // Arrange
        var streamData = new StreamData
        {
            StreamKey = "test_stream_id",
            Title = "Test Stream",
            Platform = "YouTube"
        };

        // Act
        var eventArgs = new StreamStatusChangedEventArgs
        {
            Stream = streamData,
            IsOnline = true
        };

        // Assert
        eventArgs.Stream.Should().Be(streamData);
        eventArgs.Stream.StreamKey.Should().Be("test_stream_id");
        eventArgs.IsOnline.Should().BeTrue();
    }
}
