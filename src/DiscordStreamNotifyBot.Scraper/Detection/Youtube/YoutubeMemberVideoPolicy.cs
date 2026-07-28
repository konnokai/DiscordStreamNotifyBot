namespace DiscordStreamNotifyBot.Scraper.Detection.Youtube
{
    internal static class YoutubeMemberVideoPolicy
    {
        internal static YoutubeMemberChannelDecision PlanChannel(YoutubeMemberChannelFacts facts)
            => new(facts.HasAutomaticConfigWithoutVideo, facts.HasMissingChannelTitle);

        internal static YoutubeMemberCandidateAction ClassifyCandidate(YoutubeMemberCandidateFacts facts)
        {
            if (facts.RequestSucceeded)
                return YoutubeMemberCandidateAction.IgnorePublicVideo;

            string message = facts.ErrorMessage?.ToLowerInvariant() ?? string.Empty;
            if (message.Contains("disabled comments"))
                return YoutubeMemberCandidateAction.IgnoreCommentsDisabled;
            if (facts.HttpStatusCode == 404 || message.Contains("notfound") || message.Contains("not found"))
                return YoutubeMemberCandidateAction.IgnoreUnavailable;
            if (facts.HttpStatusCode == 403 || message.Contains("403") || message.Contains("forbidden") ||
                message.Contains("unauthorized") || message.Contains("the request might not be properly authorized"))
                return YoutubeMemberCandidateAction.SelectMemberOnlyVideo;

            return YoutubeMemberCandidateAction.AbortDiscovery;
        }
    }

    internal readonly record struct YoutubeMemberChannelFacts(
        bool HasAutomaticConfigWithoutVideo,
        bool HasMissingChannelTitle);

    internal readonly record struct YoutubeMemberChannelDecision(
        bool DiscoverVideo,
        bool RefreshChannelTitle);

    internal readonly record struct YoutubeMemberCandidateFacts(
        bool RequestSucceeded,
        int? HttpStatusCode,
        string ErrorMessage);

    internal enum YoutubeMemberCandidateAction
    {
        IgnorePublicVideo,
        IgnoreCommentsDisabled,
        IgnoreUnavailable,
        SelectMemberOnlyVideo,
        AbortDiscovery,
    }
}
