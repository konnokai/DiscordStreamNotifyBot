using DiscordStreamNotifyBot.SharedService.YoutubeMember;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class YoutubeMemberLifecyclePolicyTests
    {
        [Fact]
        public void OldCheckBeforeFourRunsTodayAtFour()
        {
            TimeSpan delay = YoutubeMemberLifecyclePolicy.NextOldCheckDelay(
                new DateTime(2026, 8, 5, 3, 59, 0));

            Assert.Equal(TimeSpan.FromMinutes(1), delay);
        }

        [Fact]
        public void OldCheckAtOrAfterFourRunsAtNextDaysFour()
        {
            TimeSpan atFour = YoutubeMemberLifecyclePolicy.NextOldCheckDelay(
                new DateTime(2026, 8, 5, 4, 0, 0));
            TimeSpan afterFour = YoutubeMemberLifecyclePolicy.NextOldCheckDelay(
                new DateTime(2026, 8, 5, 9, 30, 0));

            Assert.Equal(TimeSpan.FromDays(1), atFour);
            Assert.Equal(TimeSpan.FromHours(18.5), afterFour);
        }

        [Fact]
        public void DisabledApiSkipsOnlyProviderWorkSoCleanupCanRunFirst()
        {
            Assert.False(YoutubeMemberLifecyclePolicy.ShouldRunProviderCheck(false));
            Assert.True(YoutubeMemberLifecyclePolicy.ShouldRunProviderCheck(true));
        }

        [Fact]
        public void GuildMemberSubscriptionUsesTheSameIntentGateForStartAndStop()
        {
            Assert.False(YoutubeMemberLifecyclePolicy.ShouldManageGuildMemberSubscription(false));
            Assert.True(YoutubeMemberLifecyclePolicy.ShouldManageGuildMemberSubscription(true));
        }

        [Fact]
        public async Task StopDrainWaitsForTrackedLifecycleTask()
        {
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Task drain = YoutubeMemberLifecyclePolicy.DrainAsync([release.Task]);

            Assert.False(drain.IsCompleted);
            release.SetResult();
            await drain;
        }

        [Fact]
        public void StopGateRejectsLateRegistrationAndKeepsAlreadyRegisteredCompletionInDrain()
        {
            var registry = new YoutubeMemberLifecycleTaskRegistry();
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            Assert.True(registry.TryRegister(completion.Task, out long taskId));
            Task[] draining = registry.StopAndSnapshot();
            Assert.Contains(completion.Task, draining);
            Assert.False(registry.TryRegister(Task.CompletedTask, out _));

            registry.Complete(taskId);
        }
    }
}
