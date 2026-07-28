namespace DiscordStreamNotifyBot.Scraper.Detection.Youtube
{
    internal static class YoutubeApiVideoPolicy
    {
        internal static YoutubeApiVideoDecision Classify(YoutubeApiVideoFacts facts)
        {
            if (!facts.HasLiveStreamingDetails)
            {
                return facts.IsFifteenSecondUpload && facts.CommentsDisabled
                    ? new YoutubeApiVideoDecision(YoutubeApiVideoAction.IgnoreFakePost, null)
                    : new YoutubeApiVideoDecision(YoutubeApiVideoAction.NewVideo, facts.PublishedAt);
            }

            if (facts.ActualStartTime.HasValue)
                return new YoutubeApiVideoDecision(YoutubeApiVideoAction.Started, facts.ActualStartTime);
            if (facts.ScheduledStartTime.HasValue)
                return new YoutubeApiVideoDecision(YoutubeApiVideoAction.Scheduled, facts.ScheduledStartTime);
            if (facts.HasActiveLiveChat)
                return new YoutubeApiVideoDecision(YoutubeApiVideoAction.ActiveChatOnly, facts.PublishedAt);

            return new YoutubeApiVideoDecision(YoutubeApiVideoAction.Ignore, null);
        }
    }

    internal readonly record struct YoutubeApiVideoFacts(
        bool HasLiveStreamingDetails,
        DateTime PublishedAt,
        DateTime? ActualStartTime,
        DateTime? ScheduledStartTime,
        bool HasActiveLiveChat,
        bool IsFifteenSecondUpload,
        bool CommentsDisabled);

    internal readonly record struct YoutubeApiVideoDecision(
        YoutubeApiVideoAction Action,
        DateTime? EventTime);

    internal enum YoutubeApiVideoAction
    {
        Ignore,
        IgnoreFakePost,
        NewVideo,
        Started,
        Scheduled,
        ActiveChatOnly,
    }
}
