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
    }

    /// <summary>Stop 與事件註冊共用 gate，避免 drain 看見空集合後才新增工作。</summary>
    internal sealed class YoutubeMemberLifecycleTaskRegistry
    {
        private readonly object _gate = new();
        private readonly HashSet<Task> _tasks = new();
        private bool _stopping;

        public bool TryRegister(Task completion)
        {
            lock (_gate)
            {
                if (_stopping)
                    return false;

                _tasks.Add(completion);
                return true;
            }
        }

        public Task[] StopAndSnapshot()
        {
            lock (_gate)
            {
                _stopping = true;
                return _tasks.ToArray();
            }
        }

        public void Complete(Task completion)
        {
            lock (_gate)
                _tasks.Remove(completion);
        }
    }
}
