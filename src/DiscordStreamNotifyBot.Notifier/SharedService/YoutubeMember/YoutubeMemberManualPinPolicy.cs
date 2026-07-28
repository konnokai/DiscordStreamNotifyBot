namespace DiscordStreamNotifyBot.SharedService.YoutubeMember
{
    internal static class YoutubeMemberManualPinPolicy
    {
        internal static YoutubeMemberAutomaticMutationAction DecideAutomaticMutation(bool isManualVideoId)
            => isManualVideoId
                ? YoutubeMemberAutomaticMutationAction.PreserveManualPin
                : YoutubeMemberAutomaticMutationAction.Apply;
    }

    internal enum YoutubeMemberAutomaticMutationAction
    {
        Apply,
        PreserveManualPin,
    }
}
