using System.Collections.Concurrent;

namespace DiscordStreamNotifyBot.SharedService.YoutubeMember
{
    internal static class YoutubeMemberLifecyclePolicy
    {
        public static TimeSpan NextOldCheckDelay(DateTime now)
        {
            DateTime next = now.Date.AddHours(4);
            if (next <= now)
                next = next.AddDays(1);
            return next - now;
        }

        public static bool ShouldRunProviderCheck(bool apiEnabled) => apiEnabled;

        // Start/Stop 必須使用同一 intent gate，避免未訂閱卻嘗試解除或反之造成 lifecycle 漂移。
        public static bool ShouldManageGuildMemberSubscription(bool enableGuildMembersIntent)
            => enableGuildMembersIntent;

        public static Task DrainAsync(IEnumerable<Task> tasks)
            => Task.WhenAll(tasks ?? []);
    }

    /// <summary>Stop 與事件註冊共用 gate，避免 drain 看見空集合後才新增工作。</summary>
    internal sealed class YoutubeMemberLifecycleTaskRegistry
    {
        private readonly object _gate = new();
        private readonly ConcurrentDictionary<long, Task> _tasks = new();
        private long _sequence;
        private bool _stopping;

        public bool TryRegister(Task completion, out long taskId)
        {
            lock (_gate)
            {
                if (_stopping)
                {
                    taskId = 0;
                    return false;
                }

                taskId = Interlocked.Increment(ref _sequence);
                if (!_tasks.TryAdd(taskId, completion))
                    throw new InvalidOperationException("無法登記 YouTube 會員生命週期工作。");
                return true;
            }
        }

        public Task[] StopAndSnapshot()
        {
            lock (_gate)
            {
                _stopping = true;
                return _tasks.Values.ToArray();
            }
        }

        public void Complete(long taskId) => _tasks.TryRemove(taskId, out _);
    }
}
