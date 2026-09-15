using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Scraper.Detection.Chzzk;
using DiscordStreamNotifyBot.Shared.Messages;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class ChzzkPollPolicyTests
    {
        private const string Key1 = "channel-1:20260915_134152";
        private const string Key2 = "channel-1:20260915_180000";
        private static readonly DateTime Now = new(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);

        private static ChzzkPollFacts OpenFacts(
            string currentStreamKey = null,
            ChzzkStreamStatus? currentStatus = null,
            bool initialized = true,
            bool hasValidStreamKey = true,
            string streamKey = Key1)
            => new(
                IsOpen: true,
                HasValidStreamKey: hasValidStreamKey,
                StreamKey: streamKey,
                IsInitialized: initialized,
                CurrentStreamKey: currentStreamKey,
                CurrentStatus: currentStatus,
                PendingCloseSinceUtc: null,
                NowUtc: Now);

        private static ChzzkPollFacts CloseFacts(
            string currentStreamKey = null,
            ChzzkStreamStatus? currentStatus = null,
            bool initialized = true,
            string streamKey = Key1,
            DateTime? pendingSince = null)
            => new(
                IsOpen: false,
                HasValidStreamKey: true,
                StreamKey: streamKey,
                IsInitialized: initialized,
                CurrentStreamKey: currentStreamKey,
                CurrentStatus: currentStatus,
                PendingCloseSinceUtc: pendingSince,
                NowUtc: Now);

        [Fact]
        public void FirstOpenTracksNewStream()
        {
            Assert.Equal(ChzzkPollAction.TrackNewStream,
                ChzzkPollPolicy.Decide(OpenFacts(initialized: false)));
        }

        [Fact]
        public void FirstCloseOnlyCreatesOfflineBaseline()
        {
            Assert.Equal(ChzzkPollAction.BaselineOffline,
                ChzzkPollPolicy.Decide(CloseFacts(initialized: false)));
        }

        [Fact]
        public void CloseWithoutKnownStreamIsIgnored()
        {
            // 已建立離線基線後，未觀察過 OPEN 的場次不得補發關台。
            Assert.Equal(ChzzkPollAction.Ignore,
                ChzzkPollPolicy.Decide(CloseFacts(currentStreamKey: null)));
        }

        [Fact]
        public void SameKeyOpenRefreshesWithoutRepublish()
        {
            Assert.Equal(ChzzkPollAction.RefreshObserved,
                ChzzkPollPolicy.Decide(OpenFacts(Key1, ChzzkStreamStatus.Open)));
        }

        [Fact]
        public void SameKeyOpenAfterClosedDoesNotRepublish()
        {
            Assert.Equal(ChzzkPollAction.RefreshObserved,
                ChzzkPollPolicy.Decide(OpenFacts(Key1, ChzzkStreamStatus.Closed)));
        }

        [Fact]
        public void SameKeyOpenDuringPendingCloseCancelsConfirmation()
        {
            Assert.Equal(ChzzkPollAction.CancelPendingClose,
                ChzzkPollPolicy.Decide(OpenFacts(Key1, ChzzkStreamStatus.PendingClose)));
        }

        [Fact]
        public void NewOpenDateIsANewStreamAndSupersedesTheOldOne()
        {
            Assert.Equal(ChzzkPollAction.SupersedeAndTrack,
                ChzzkPollPolicy.Decide(OpenFacts(Key1, ChzzkStreamStatus.Open, streamKey: Key2)));
            Assert.Equal(ChzzkPollAction.SupersedeAndTrack,
                ChzzkPollPolicy.Decide(OpenFacts(Key1, ChzzkStreamStatus.PendingClose, streamKey: Key2)));
        }

        [Fact]
        public void MissingOpenDateOnOpenIsUnknown()
        {
            Assert.Equal(ChzzkPollAction.Unknown,
                ChzzkPollPolicy.Decide(OpenFacts(hasValidStreamKey: false, streamKey: null)));
        }

        [Fact]
        public void SameKeyCloseEntersPendingConfirmation()
        {
            Assert.Equal(ChzzkPollAction.StartPendingClose,
                ChzzkPollPolicy.Decide(CloseFacts(Key1, ChzzkStreamStatus.Open)));
        }

        [Fact]
        public void RepeatedCloseBeforeDelayDoesNotConfirmOrReset()
        {
            var decision = ChzzkPollPolicy.Decide(CloseFacts(Key1, ChzzkStreamStatus.PendingClose,
                pendingSince: Now - TimeSpan.FromMinutes(1)));
            Assert.Equal(ChzzkPollAction.Ignore, decision);
        }

        [Fact]
        public void PendingCloseConfirmsAfterThreeMinutes()
        {
            Assert.Equal(ChzzkPollAction.ConfirmClose,
                ChzzkPollPolicy.Decide(CloseFacts(Key1, ChzzkStreamStatus.PendingClose,
                    pendingSince: Now - ChzzkPollPolicy.CloseConfirmationDelay)));
        }

        [Fact]
        public void PendingCloseWithoutStartTimeStaysUnknown()
        {
            Assert.Equal(ChzzkPollAction.Ignore,
                ChzzkPollPolicy.Decide(CloseFacts(Key1, ChzzkStreamStatus.PendingClose, pendingSince: null)));
        }

        [Fact]
        public void ForeignCloseDoesNotCloseCurrentStream()
        {
            Assert.Equal(ChzzkPollAction.Ignore,
                ChzzkPollPolicy.Decide(CloseFacts(Key1, ChzzkStreamStatus.Open, streamKey: Key2)));
        }

        [Fact]
        public void RepeatedCloseOnClosedStreamIsIgnored()
        {
            Assert.Equal(ChzzkPollAction.Ignore,
                ChzzkPollPolicy.Decide(CloseFacts(Key1, ChzzkStreamStatus.Closed)));
            Assert.Equal(ChzzkPollAction.Ignore,
                ChzzkPollPolicy.Decide(CloseFacts(Key1, ChzzkStreamStatus.Superseded)));
        }

        [Fact]
        public void NotificationFactoryConvertsTimestampsAndKeepsRawStrings()
        {
            var spider = new ChzzkSpider { ChannelId = "channel-1", ChannelName = "테스트 채널", ChannelImageUrl = "img" };
            var stream = new ChzzkStream
            {
                StreamKey = Key1,
                ChannelId = "channel-1",
                OpenDateRaw = "2026-09-15 12:41:10",
                CloseDateRaw = "2026-09-15 15:20:00",
                StreamTitle = "title",
                CategoryName = "cat"
            };

            var dto = ChzzkDetectionService.CreateNotification(spider, stream, ChzzkNoticeType.EndStream);

            Assert.Equal("2026-09-15 12:41:10", dto.OpenDate);
            Assert.Equal("2026-09-15 15:20:00", dto.CloseDate);
            Assert.Equal(new DateTime(2026, 9, 15, 3, 41, 10, DateTimeKind.Utc), dto.StreamStartAt);
            Assert.Equal(new DateTime(2026, 9, 15, 6, 20, 0, DateTimeKind.Utc), dto.StreamEndAt);
            Assert.Equal(Key1, dto.StreamKey);
            Assert.Equal("테스트 채널", dto.ChannelName);
        }

        [Fact]
        public void NotificationFactoryLeavesMissingCloseTimeNull()
        {
            var dto = ChzzkDetectionService.CreateNotification(
                new ChzzkSpider { ChannelId = "channel-1", ChannelName = "name" },
                new ChzzkStream
                {
                    StreamKey = Key1,
                    ChannelId = "channel-1",
                    OpenDateRaw = "2026-09-15 12:41:10",
                    CloseDateRaw = null
                },
                ChzzkNoticeType.StartStream);

            Assert.NotNull(dto.StreamStartAt);
            Assert.Null(dto.StreamEndAt);
        }
    }
}
