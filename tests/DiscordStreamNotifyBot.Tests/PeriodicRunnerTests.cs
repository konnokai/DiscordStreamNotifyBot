using DiscordStreamNotifyBot.Shared;
using Microsoft.Extensions.Time.Testing;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class PeriodicRunnerTests
    {
        [Fact]
        public async Task FirstActionRunsAfterDueTimeBeforePeriodicTick()
        {
            var timeProvider = new FakeTimeProvider();
            using var cancellation = new CancellationTokenSource();
            int calls = 0;
            Task runner = PeriodicRunner.RunCoreAsync(
                "test",
                TimeSpan.FromSeconds(10),
                TimeSpan.FromMinutes(1),
                () =>
                {
                    calls++;
                    cancellation.Cancel();
                    return Task.CompletedTask;
                },
                timeProvider,
                cancellation.Token);

            timeProvider.Advance(TimeSpan.FromSeconds(9));
            Assert.Equal(0, calls);

            timeProvider.Advance(TimeSpan.FromSeconds(1));
            await runner;

            Assert.Equal(1, calls);
        }

        [Fact]
        public async Task CancellationDuringDueTimeSkipsFirstAction()
        {
            var timeProvider = new FakeTimeProvider();
            using var cancellation = new CancellationTokenSource();
            int calls = 0;
            Task runner = PeriodicRunner.RunCoreAsync(
                "test",
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(1),
                () =>
                {
                    calls++;
                    return Task.CompletedTask;
                },
                timeProvider,
                cancellation.Token);

            cancellation.Cancel();
            await runner;

            Assert.Equal(0, calls);
        }

        [Fact]
        public async Task CancellationWhileWaitingForTickStopsRunner()
        {
            var timeProvider = new FakeTimeProvider();
            using var cancellation = new CancellationTokenSource();
            int calls = 0;
            Task runner = PeriodicRunner.RunCoreAsync(
                "test",
                TimeSpan.Zero,
                TimeSpan.FromMinutes(1),
                () =>
                {
                    calls++;
                    return Task.CompletedTask;
                },
                timeProvider,
                cancellation.Token);

            Assert.Equal(1, calls);
            cancellation.Cancel();
            await runner;

            Assert.Equal(1, calls);
        }

        [Fact]
        public async Task ActionExceptionDoesNotStopNextExecution()
        {
            var timeProvider = new FakeTimeProvider();
            using var cancellation = new CancellationTokenSource();
            int calls = 0;
            Task runner = PeriodicRunner.RunCoreAsync(
                "test",
                TimeSpan.Zero,
                TimeSpan.FromMinutes(1),
                () =>
                {
                    calls++;
                    if (calls == 1)
                        return Task.FromException(new InvalidOperationException("expected"));

                    cancellation.Cancel();
                    return Task.CompletedTask;
                },
                timeProvider,
                cancellation.Token);

            Assert.Equal(1, calls);
            timeProvider.Advance(TimeSpan.FromMinutes(1));
            await runner;

            Assert.Equal(2, calls);
        }

        [Fact]
        public async Task UnrelatedOperationCanceledExceptionDoesNotStopNextExecution()
        {
            var timeProvider = new FakeTimeProvider();
            using var cancellation = new CancellationTokenSource();
            int calls = 0;
            Task runner = PeriodicRunner.RunCoreAsync(
                "test",
                TimeSpan.Zero,
                TimeSpan.FromMinutes(1),
                () =>
                {
                    calls++;
                    if (calls == 1)
                        return Task.FromException(new TaskCanceledException("external timeout"));

                    cancellation.Cancel();
                    return Task.CompletedTask;
                },
                timeProvider,
                cancellation.Token);

            timeProvider.Advance(TimeSpan.FromMinutes(1));
            await runner;

            Assert.Equal(2, calls);
        }

        [Fact]
        public async Task RunningActionIsNeverReentered()
        {
            var timeProvider = new FakeTimeProvider();
            using var cancellation = new CancellationTokenSource();
            var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            int calls = 0;
            int active = 0;
            int maxActive = 0;

            Task runner = PeriodicRunner.RunCoreAsync(
                "test",
                TimeSpan.Zero,
                TimeSpan.FromSeconds(10),
                async () =>
                {
                    int currentActive = Interlocked.Increment(ref active);
                    UpdateMaximum(ref maxActive, currentActive);
                    int call = Interlocked.Increment(ref calls);
                    try
                    {
                        if (call == 1)
                            await releaseFirst.Task;
                        else
                        {
                            secondStarted.TrySetResult();
                            cancellation.Cancel();
                        }
                    }
                    finally
                    {
                        Interlocked.Decrement(ref active);
                    }
                },
                timeProvider,
                cancellation.Token);

            Assert.Equal(1, calls);
            timeProvider.Advance(TimeSpan.FromSeconds(30));
            Assert.Equal(1, calls);
            Assert.Equal(1, maxActive);

            releaseFirst.TrySetResult();
            await secondStarted.Task;
            await runner;

            Assert.Equal(2, calls);
            Assert.Equal(1, maxActive);
        }

        private static void UpdateMaximum(ref int maximum, int candidate)
        {
            int current;
            do
            {
                current = maximum;
                if (candidate <= current)
                    return;
            }
            while (Interlocked.CompareExchange(ref maximum, candidate, current) != current);
        }
    }
}
