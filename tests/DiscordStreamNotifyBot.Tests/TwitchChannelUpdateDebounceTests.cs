using DiscordStreamNotifyBot.Scraper.Detection.Twitch.Debounce;
using DiscordStreamNotifyBot.Shared.Messages;
using Microsoft.Extensions.Time.Testing;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class TwitchChannelUpdateDebounceTests
    {
        [Fact]
        public async Task SingleUpdatePublishesAfterQuietWindow()
        {
            var fixture = new DebounceFixture();
            using var debounce = fixture.Create();

            debounce.AddUpdate(Update("first"));
            fixture.Time.Advance(TimeSpan.FromMinutes(1) - TimeSpan.FromTicks(1));
            Assert.Empty(fixture.Batches);

            fixture.Time.Advance(TimeSpan.FromTicks(1));
            await debounce.WaitForIdleAsync();

            Assert.Single(fixture.Batches);
            Assert.Equal("first", fixture.Batches[0].Single().NewTitle);
        }

        [Fact]
        public async Task LaterUpdateRestartsQuietWindowAndPreservesOrder()
        {
            var fixture = new DebounceFixture();
            using var debounce = fixture.Create();

            debounce.AddUpdate(Update("first"));
            fixture.Time.Advance(TimeSpan.FromSeconds(59));
            debounce.AddUpdate(Update("second"));
            fixture.Time.Advance(TimeSpan.FromSeconds(1));
            Assert.Empty(fixture.Batches);

            fixture.Time.Advance(TimeSpan.FromSeconds(59));
            await debounce.WaitForIdleAsync();

            Assert.Equal(new[] { "first", "second" }, fixture.Batches.Single().Select(update => update.NewTitle));
        }

        [Fact]
        public async Task ContinuousUpdatesPublishAtThreeMinuteTimeout()
        {
            var fixture = new DebounceFixture();
            using var debounce = fixture.Create();

            debounce.AddUpdate(Update("0"));
            for (int index = 1; index <= 3; index++)
            {
                fixture.Time.Advance(TimeSpan.FromSeconds(50));
                debounce.AddUpdate(Update(index.ToString()));
            }

            fixture.Time.Advance(TimeSpan.FromSeconds(29));
            Assert.Empty(fixture.Batches);
            fixture.Time.Advance(TimeSpan.FromSeconds(1));
            await debounce.WaitForIdleAsync();

            Assert.Equal(new[] { "0", "1", "2", "3" }, fixture.Batches.Single().Select(update => update.NewTitle));
        }

        [Fact]
        public async Task CancellationSuppressesPendingBatchAndDoesNotLeakIntoNextBatch()
        {
            var fixture = new DebounceFixture();
            using var cancellation = new CancellationTokenSource();
            using var debounce = fixture.Create(cancellation.Token);

            debounce.AddUpdate(Update("canceled"));
            cancellation.Cancel();
            fixture.Time.Advance(TimeSpan.FromMinutes(3));
            await debounce.WaitForIdleAsync();

            Assert.Empty(fixture.Batches);
        }

        [Fact]
        public async Task CancelPendingAllowsFreshBatch()
        {
            var fixture = new DebounceFixture();
            using var debounce = fixture.Create();

            debounce.AddUpdate(Update("canceled"));
            debounce.CancelPending();
            debounce.AddUpdate(Update("fresh"));
            fixture.Time.Advance(TimeSpan.FromMinutes(1));
            await debounce.WaitForIdleAsync();

            Assert.Equal("fresh", fixture.Batches.Single().Single().NewTitle);
        }

        [Fact]
        public async Task DisposeSuppressesPendingBatchAndIsIdempotent()
        {
            var fixture = new DebounceFixture();
            var debounce = fixture.Create();

            debounce.AddUpdate(Update("pending"));
            debounce.Dispose();
            debounce.Dispose();
            fixture.Time.Advance(TimeSpan.FromMinutes(3));
            await debounce.WaitForIdleAsync();

            Assert.Empty(fixture.Batches);
            Assert.Throws<ObjectDisposedException>(() => debounce.AddUpdate(Update("late")));
        }

        [Fact]
        public async Task PublishReceivesCapturedIdentity()
        {
            var fixture = new DebounceFixture();
            using var debounce = fixture.Create();

            debounce.AddUpdate(Update("first"));
            fixture.Time.Advance(TimeSpan.FromMinutes(1));
            await debounce.WaitForIdleAsync();

            Assert.Equal(("user-id", "User Name", "user_login"), fixture.Identity);
        }

        [Fact]
        public async Task UpdateArrivingDuringPublishIsKeptForNextBatch()
        {
            var timeProvider = new FakeTimeProvider();
            var firstPublishEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseFirstPublish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var firstPublishCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondPublishCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var batches = new List<IReadOnlyCollection<TwitchChannelUpdateInfo>>();
            int publishes = 0;
            using var debounce = new DebounceChannelUpdateMessage(
                "User Name",
                "user_login",
                "user-id",
                timeProvider,
                async (_, _, _, updates) =>
                {
                    batches.Add(updates);
                    if (Interlocked.Increment(ref publishes) == 1)
                    {
                        firstPublishEntered.TrySetResult();
                        await releaseFirstPublish.Task;
                        firstPublishCompleted.TrySetResult();
                    }
                    else
                    {
                        secondPublishCompleted.TrySetResult();
                    }
                });

            debounce.AddUpdate(Update("first"));
            timeProvider.Advance(TimeSpan.FromMinutes(1));
            await firstPublishEntered.Task;

            debounce.AddUpdate(Update("second"));
            timeProvider.Advance(TimeSpan.FromMinutes(1));
            await secondPublishCompleted.Task;
            releaseFirstPublish.TrySetResult();
            await firstPublishCompleted.Task;
            await debounce.WaitForIdleAsync();

            Assert.Equal(2, batches.Count);
            Assert.Equal("first", batches[0].Single().NewTitle);
            Assert.Equal("second", batches[1].Single().NewTitle);
        }

        private static TwitchChannelUpdateInfo Update(string title)
            => new() { OldTitle = $"old-{title}", NewTitle = title };

        private sealed class DebounceFixture
        {
            public FakeTimeProvider Time { get; } = new();
            public List<IReadOnlyCollection<TwitchChannelUpdateInfo>> Batches { get; } = new();
            public (string UserId, string UserName, string UserLogin) Identity { get; private set; }

            public DebounceChannelUpdateMessage Create(CancellationToken cancellationToken = default)
            {
                return new DebounceChannelUpdateMessage(
                    "User Name",
                    "user_login",
                    "user-id",
                    Time,
                    (userId, userName, userLogin, updates) =>
                    {
                        Identity = (userId, userName, userLogin);
                        Batches.Add(updates);
                        return Task.CompletedTask;
                    },
                    cancellationToken);
            }
        }
    }
}
