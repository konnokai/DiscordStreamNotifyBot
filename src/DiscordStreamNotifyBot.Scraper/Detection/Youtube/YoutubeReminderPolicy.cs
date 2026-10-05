namespace DiscordStreamNotifyBot.Scraper.Detection.Youtube
{
    internal static class YoutubeReminderPolicy
    {
        private static readonly TimeSpan MaxReminderAdvance = TimeSpan.FromDays(14);
        private static readonly TimeSpan ReminderAdvance = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan StartTimeGrace = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan MinTimerDelay = TimeSpan.FromSeconds(1);

        /// <summary>提醒 Timer 的延遲；超過 14 天回傳 null 不排程，已到提醒時間回傳 0 立即執行。</summary>
        internal static TimeSpan? GetReminderDelay(DateTime scheduledStart, DateTime now)
        {
            if (scheduledStart > now + MaxReminderAdvance)
                return null;

            TimeSpan delay = scheduledStart - ReminderAdvance - now;
            if (delay <= TimeSpan.Zero)
                return TimeSpan.Zero;

            return delay < MinTimerDelay ? MinTimerDelay : delay;
        }

        internal static YoutubeReminderApiAction DecideApiRecheck(DateTime apiStart, DateTime now)
            => apiStart - StartTimeGrace < now
                ? YoutubeReminderApiAction.TreatAsStarted
                : YoutubeReminderApiAction.TreatAsTimeChanged;

        internal static YoutubeReminderReconciliationAction ReconcileBatch(
            bool apiVideoFound,
            bool hasLiveStreamingDetails,
            bool hasScheduledStartTime,
            DateTime? scheduledStartTime,
            DateTime previousStart,
            DateTime now)
        {
            if (!apiVideoFound)
                return YoutubeReminderReconciliationAction.PublishDeleteAndRemove;
            if (!hasLiveStreamingDetails || !hasScheduledStartTime)
                return YoutubeReminderReconciliationAction.PublishStartAndRemove;
            // 排程時間解析失敗或沒有變更時保留原提醒
            if (scheduledStartTime is not { } newStart || previousStart == newStart)
                return YoutubeReminderReconciliationAction.KeepExisting;
            if (newStart <= now || newStart >= now + MaxReminderAdvance)
                return YoutubeReminderReconciliationAction.RemoveWithoutReplacement;

            return YoutubeReminderReconciliationAction.PublishChange;
        }
    }

    internal enum YoutubeReminderApiAction
    {
        TreatAsStarted,
        TreatAsTimeChanged,
    }

    internal enum YoutubeReminderReconciliationAction
    {
        KeepExisting,
        PublishDeleteAndRemove,
        PublishStartAndRemove,
        RemoveWithoutReplacement,
        /// <summary>發布時間變更通知，並由 StartReminder 依新時間重新排程。</summary>
        PublishChange,
    }
}
