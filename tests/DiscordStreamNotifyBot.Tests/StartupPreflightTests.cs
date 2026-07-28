using DiscordStreamNotifyBot.Shared;
using Microsoft.Extensions.Time.Testing;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class StartupPreflightTests
    {
        private static readonly DateTimeOffset StartTime = new(2026, 7, 27, 0, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task RetrySucceedsImmediatelyWithoutDelay()
        {
            var timeProvider = new FakeTimeProvider(StartTime);
            int attempts = 0;

            await StartupPreflight.RetryWithBackoffAsync(
                "測試服務",
                () =>
                {
                    attempts++;
                    return Task.CompletedTask;
                },
                TimeSpan.FromMinutes(1),
                timeProvider);

            Assert.Equal(1, attempts);
            Assert.Equal(StartTime, timeProvider.GetUtcNow());
        }

        [Fact]
        public async Task RetryUsesExponentialBackoffWithThirtySecondCap()
        {
            var timeProvider = new FakeTimeProvider(StartTime);
            var attemptTimes = new List<DateTimeOffset>();
            var actualDelays = new List<TimeSpan>();

            await StartupPreflight.RetryWithBackoffAsync(
                "測試服務",
                () =>
                {
                    attemptTimes.Add(timeProvider.GetUtcNow());
                    int attempt = attemptTimes.Count;
                    return attempt == 7
                        ? Task.CompletedTask
                        : Task.FromException(new InvalidOperationException($"failure-{attempt}"));
                },
                TimeSpan.FromMinutes(2),
                timeProvider,
                delay =>
                {
                    actualDelays.Add(delay);
                    timeProvider.Advance(delay);
                    return Task.CompletedTask;
                });

            Assert.Equal(
                new[] { 0, 2, 6, 14, 30, 60, 90 },
                attemptTimes.Select(time => (int)(time - StartTime).TotalSeconds));
            Assert.Equal(new[] { 2, 4, 8, 16, 30, 30 }, actualDelays.Select(delay => (int)delay.TotalSeconds));
        }

        [Fact]
        public async Task RetryClampsFinalDelayAndThrowsAtDeadline()
        {
            var timeProvider = new FakeTimeProvider(StartTime);
            var failures = new List<Exception>();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                StartupPreflight.RetryWithBackoffAsync(
                    "Redis",
                    () =>
                    {
                        var failure = new InvalidOperationException($"failure-{failures.Count + 1}");
                        failures.Add(failure);
                        return Task.FromException(failure);
                    },
                    TimeSpan.FromSeconds(7),
                    timeProvider,
                    delay =>
                    {
                        timeProvider.Advance(delay);
                        return Task.CompletedTask;
                    }));

            Assert.Equal(3, failures.Count);
            Assert.Same(failures[^1], exception.InnerException);
            Assert.Contains("7 秒內連上 Redis", exception.Message);
            Assert.Equal(StartTime.AddSeconds(7), timeProvider.GetUtcNow());
        }

        [Fact]
        public async Task ZeroTimeoutDoesNotInvokeProbe()
        {
            var timeProvider = new FakeTimeProvider(StartTime);
            int attempts = 0;

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                StartupPreflight.RetryWithBackoffAsync(
                    "Redis",
                    () =>
                    {
                        attempts++;
                        return Task.CompletedTask;
                    },
                    TimeSpan.Zero,
                    timeProvider));

            Assert.Equal(0, attempts);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public async Task TimeoutAlsoBoundsHungProbe()
        {
            var timeProvider = new FakeTimeProvider(StartTime);
            var neverCompletes = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Task retry = StartupPreflight.RetryWithBackoffAsync(
                "MySQL",
                () => neverCompletes.Task,
                TimeSpan.FromSeconds(10),
                timeProvider);

            timeProvider.Advance(TimeSpan.FromSeconds(10));
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => retry);

            Assert.IsType<TimeoutException>(exception.InnerException);
            Assert.Contains("10 秒內連上 MySQL", exception.Message);
        }

        [Fact]
        public async Task TimeoutAlsoBoundsSynchronouslyBlockedProbe()
        {
            var timeProvider = new FakeTimeProvider(StartTime);
            using var releaseProbe = new ManualResetEventSlim();
            Task retry = StartupPreflight.RetryWithBackoffAsync(
                "Redis",
                () =>
                {
                    releaseProbe.Wait();
                    return Task.CompletedTask;
                },
                TimeSpan.FromSeconds(10),
                timeProvider);

            try
            {
                timeProvider.Advance(TimeSpan.FromSeconds(10));
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => retry);

                Assert.IsType<TimeoutException>(exception.InnerException);
                Assert.Contains("10 秒內連上 Redis", exception.Message);
            }
            finally
            {
                releaseProbe.Set();
            }
        }
    }
}
