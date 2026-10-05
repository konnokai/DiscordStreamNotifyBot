using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.HttpClients.Chzzk;
using DiscordStreamNotifyBot.HttpClients.Chzzk.Model;
using DiscordStreamNotifyBot.Scraper.Detection.Chzzk;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.Chzzk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class ChzzkRecordTests
    {
        private const string ChannelId = "4de764d9dad3b25602284be6db3ac647";
        private const string StreamKey = ChannelId + ":20260918_120000";

        private static ChzzkSpider CreateSpider(bool isRecord)
            => new() { ChannelId = ChannelId, ChannelName = "테스트 채널", IsRecord = isRecord };

        private static ChzzkStream CreateStream()
            => new()
            {
                StreamKey = StreamKey,
                ChannelId = ChannelId,
                OpenDateRaw = "2026-09-18 12:00:00",
                Status = ChzzkStreamStatus.Open
            };

        [Fact]
        public void IsRecordDefaultsToFalseSoExistingCrawlersDoNotStartRecording()
        {
            Assert.False(new ChzzkSpider().IsRecord);
        }

        [Fact]
        public void RecordRequestJsonContractUsesCamelCaseFields()
        {
            string json = JsonConvert.SerializeObject(new ChzzkRecordRequest
            {
                ChannelId = ChannelId,
                StreamKey = StreamKey
            });

            var token = JObject.Parse(json);
            Assert.Equal(["channelId", "streamKey"], token.Properties().Select(x => x.Name).ToArray());
            Assert.Equal(ChannelId, token.Value<string>("channelId"));
            Assert.Equal(StreamKey, token.Value<string>("streamKey"));

            var roundTrip = JsonConvert.DeserializeObject<ChzzkRecordRequest>(json);
            Assert.Equal(ChannelId, roundTrip.ChannelId);
            Assert.Equal(StreamKey, roundTrip.StreamKey);
        }

        [Fact]
        public async Task NewStreamDelegatesRecordingOnlyWhenAutoRecordEnabled()
        {
            string publishedChannelId = null;
            string publishedStreamKey = null;
            int notifications = 0;

            bool delegated = await ChzzkDetectionService.DelegateRecordThenPublishAsync(
                CreateSpider(isRecord: true),
                CreateStream(),
                (channelId, streamKey) =>
                {
                    publishedChannelId = channelId;
                    publishedStreamKey = streamKey;
                    return Task.FromResult(1L);
                },
                _ => { notifications++; return Task.CompletedTask; });

            Assert.True(delegated);
            Assert.Equal(ChannelId, publishedChannelId);
            Assert.Equal(StreamKey, publishedStreamKey);
            Assert.Equal(1, notifications);

            int disabledPublishes = 0;
            bool disabledDelegated = await ChzzkDetectionService.DelegateRecordThenPublishAsync(
                CreateSpider(isRecord: false),
                CreateStream(),
                (_, _) => { disabledPublishes++; return Task.FromResult(1L); },
                _ => Task.CompletedTask);

            Assert.False(disabledDelegated);
            Assert.Equal(0, disabledPublishes);
        }

        [Fact]
        public async Task RecordPublishFailureDoesNotBlockStartNotification()
        {
            int notifications = 0;

            bool delegated = await ChzzkDetectionService.DelegateRecordThenPublishAsync(
                CreateSpider(isRecord: true),
                CreateStream(),
                (_, _) => Task.FromException<long>(new IOException("Redis unavailable")),
                _ => { notifications++; return Task.CompletedTask; });

            Assert.False(delegated);
            Assert.Equal(1, notifications);
        }

        [Fact]
        public async Task NoSubscriberDoesNotClaimRecordingDelegated()
        {
            int notifications = 0;

            bool delegated = await ChzzkDetectionService.DelegateRecordThenPublishAsync(
                CreateSpider(isRecord: true),
                CreateStream(),
                (_, _) => Task.FromResult(0L),
                _ => { notifications++; return Task.CompletedTask; });

            Assert.False(delegated);
            Assert.Equal(1, notifications);
        }

        [Fact]
        public async Task ImmediateRecordPublishesValidatedStreamKey()
        {
            string publishedChannelId = null;
            string publishedStreamKey = null;

            var result = await ChzzkRecordService.PublishRecordRequestAsync(
                ChannelId,
                CreateStatusResult(true, ChzzkLiveStatusValues.Open, "2026-09-18 12:00:00"),
                (channelId, streamKey) =>
                {
                    publishedChannelId = channelId;
                    publishedStreamKey = streamKey;
                    return Task.FromResult(2L);
                });

            Assert.Equal("applied", result.State);
            Assert.Equal("record.requested", result.Code);
            Assert.Equal(ChannelId, publishedChannelId);
            Assert.Equal(StreamKey, publishedStreamKey);
        }

        [Fact]
        public async Task ImmediateRecordRejectsNonOpenStatusWithoutPublishing()
        {
            int publishCount = 0;

            var result = await ChzzkRecordService.PublishRecordRequestAsync(
                ChannelId,
                CreateStatusResult(true, ChzzkLiveStatusValues.Close, "2026-09-18 12:00:00"),
                (_, _) => { publishCount++; return Task.FromResult(1L); });

            Assert.Equal("rejected", result.State);
            Assert.Equal("record.not-live", result.Code);
            Assert.Equal(0, publishCount);
        }

        [Fact]
        public async Task ImmediateRecordRejectsUnknownStatusAndApiFailure()
        {
            int publishCount = 0;

            var failed = await ChzzkRecordService.PublishRecordRequestAsync(
                ChannelId,
                new ChzzkLiveStatusResult(false, false, null, null),
                (_, _) => { publishCount++; return Task.FromResult(1L); });
            Assert.Equal("record.status-unavailable", failed.Code);

            var unknown = await ChzzkRecordService.PublishRecordRequestAsync(
                ChannelId,
                CreateStatusResult(true, "WAIT", "2026-09-18 12:00:00"),
                (_, _) => { publishCount++; return Task.FromResult(1L); });
            Assert.Equal("record.not-live", unknown.Code);
            Assert.Equal(0, publishCount);
        }

        [Fact]
        public async Task ImmediateRecordRejectsInvalidOpenDateWithoutPublishing()
        {
            int publishCount = 0;

            var result = await ChzzkRecordService.PublishRecordRequestAsync(
                ChannelId,
                CreateStatusResult(true, ChzzkLiveStatusValues.Open, "invalid"),
                (_, _) => { publishCount++; return Task.FromResult(1L); });

            Assert.Equal("record.stream-key-unavailable", result.Code);
            Assert.Equal(0, publishCount);
        }

        [Fact]
        public async Task ImmediateRecordReportsPublishFailureAndMissingSubscriber()
        {
            var failed = await ChzzkRecordService.PublishRecordRequestAsync(
                ChannelId,
                CreateStatusResult(true, ChzzkLiveStatusValues.Open, "2026-09-18 12:00:00"),
                (_, _) => Task.FromException<long>(new IOException("Redis unavailable")));
            Assert.Equal("record.publish-failed", failed.Code);

            var offline = await ChzzkRecordService.PublishRecordRequestAsync(
                ChannelId,
                CreateStatusResult(true, ChzzkLiveStatusValues.Open, "2026-09-18 12:00:00"),
                (_, _) => Task.FromResult(0L));
            Assert.Equal("record.record-tool-offline", offline.Code);
        }

        private static ChzzkLiveStatusResult CreateStatusResult(bool isSuccess, string status, string openDate)
            => new(isSuccess, false, null,
                new ChzzkLiveStatus { ChannelId = ChannelId, Status = status, OpenDate = openDate });
    }
}
