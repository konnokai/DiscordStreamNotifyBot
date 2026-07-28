namespace DiscordStreamNotifyBot.Scraper.Detection.Youtube
{
    internal static class YoutubeReminderPolicy
    {
        private static readonly TimeSpan MaxReminderAdvance = TimeSpan.FromDays(14);
        private static readonly TimeSpan ReminderAdvance = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan StartTimeGrace = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan MinTimerDelay = TimeSpan.FromSeconds(1);

        internal static YoutubeReminderStartDecision PlanStart(DateTime scheduledStart, DateTime now)
        {
            if (scheduledStart > now + MaxReminderAdvance)
                return new YoutubeReminderStartDecision(YoutubeReminderStartAction.Ignore, TimeSpan.Zero);

            TimeSpan delay = scheduledStart - ReminderAdvance - now;
            if (delay <= TimeSpan.Zero)
                return new YoutubeReminderStartDecision(YoutubeReminderStartAction.RunImmediately, TimeSpan.Zero);

            if (delay < MinTimerDelay)
                delay = MinTimerDelay;
            return new YoutubeReminderStartDecision(YoutubeReminderStartAction.ScheduleTimer, delay);
        }

        internal static YoutubeReminderApiAction DecideApiRecheck(DateTime apiStart, DateTime now)
            => apiStart - StartTimeGrace < now
                ? YoutubeReminderApiAction.TreatAsStarted
                : YoutubeReminderApiAction.TreatAsTimeChanged;

        internal static YoutubeReminderBatchChangeAction PlanBatchChange(
            DateTime previousStart,
            DateTime newStart,
            DateTime now)
        {
            if (previousStart == newStart)
                return YoutubeReminderBatchChangeAction.Unchanged;
            if (newStart <= now || newStart >= now + MaxReminderAdvance)
                return YoutubeReminderBatchChangeAction.RemoveWithoutReplacement;

            return PlanStart(newStart, now).Action == YoutubeReminderStartAction.RunImmediately
                ? YoutubeReminderBatchChangeAction.PublishAndRunImmediately
                : YoutubeReminderBatchChangeAction.PublishAndReplaceTimer;
        }

        internal static YoutubeReminderReconciliationAction ReconcileBatch(
            YoutubeReminderBatchFacts facts)
        {
            if (!facts.ApiVideoFound)
                return YoutubeReminderReconciliationAction.PublishDeleteAndRemove;
            if (!facts.HasLiveStreamingDetails || !facts.HasScheduledStartTime)
                return YoutubeReminderReconciliationAction.PublishStartAndRemove;
            if (!facts.ScheduledStartTime.HasValue)
                return YoutubeReminderReconciliationAction.KeepExisting;

            return PlanBatchChange(facts.PreviousStart, facts.ScheduledStartTime.Value, facts.Now) switch
            {
                YoutubeReminderBatchChangeAction.Unchanged => YoutubeReminderReconciliationAction.KeepExisting,
                YoutubeReminderBatchChangeAction.RemoveWithoutReplacement => YoutubeReminderReconciliationAction.RemoveWithoutReplacement,
                YoutubeReminderBatchChangeAction.PublishAndRunImmediately => YoutubeReminderReconciliationAction.PublishChangeAndRunImmediately,
                YoutubeReminderBatchChangeAction.PublishAndReplaceTimer => YoutubeReminderReconciliationAction.PublishChangeAndReplaceTimer,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
    }

    internal enum YoutubeReminderStartAction
    {
        Ignore,
        RunImmediately,
        ScheduleTimer,
    }

    internal readonly record struct YoutubeReminderStartDecision(
        YoutubeReminderStartAction Action,
        TimeSpan Delay);

    internal enum YoutubeReminderApiAction
    {
        TreatAsStarted,
        TreatAsTimeChanged,
    }

    internal enum YoutubeReminderBatchChangeAction
    {
        Unchanged,
        RemoveWithoutReplacement,
        PublishAndRunImmediately,
        PublishAndReplaceTimer,
    }

    internal readonly record struct YoutubeReminderBatchFacts(
        bool ApiVideoFound,
        bool HasLiveStreamingDetails,
        bool HasScheduledStartTime,
        DateTime? ScheduledStartTime,
        DateTime PreviousStart,
        DateTime Now);

    internal enum YoutubeReminderReconciliationAction
    {
        KeepExisting,
        PublishDeleteAndRemove,
        PublishStartAndRemove,
        RemoveWithoutReplacement,
        PublishChangeAndRunImmediately,
        PublishChangeAndReplaceTimer,
    }
}
