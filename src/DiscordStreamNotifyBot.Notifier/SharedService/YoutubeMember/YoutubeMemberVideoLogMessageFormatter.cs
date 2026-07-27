using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.Shared.Messages;

namespace DiscordStreamNotifyBot.SharedService.YoutubeMember
{
    internal static class YoutubeMemberVideoLogMessageFormatter
    {
        internal static string Format(
            YoutubeMemberVideoLogNotification notification,
            string locale,
            BotLocalizer localizer,
            CommandDisplayResolver commandDisplayResolver)
        {
            ArgumentNullException.ThrowIfNull(notification);
            ArgumentNullException.ThrowIfNull(localizer);
            ArgumentNullException.ThrowIfNull(commandDisplayResolver);

            string[] arguments = notification.MessageArguments ?? Array.Empty<string>();
            switch (notification.MessageCode)
            {
                case "NoVideos" when arguments.Length >= 1:
                    string playlistPath = commandDisplayResolver.GetCommandPath(locale,
                        "youtube", "get-member-only-playlist");
                    return localizer.Format("Member.VideoLog.NoVideos", locale, arguments[0], playlistPath);
                case "NewProbeVideo" when arguments.Length >= 2:
                    return localizer.Format("Member.VideoLog.NewProbeVideo", locale, arguments[0], arguments[1]);
                case "ChannelTitleChanged" when arguments.Length >= 2:
                    string oldTitle = string.IsNullOrEmpty(arguments[0])
                        ? localizer.Get("Common.None", locale)
                        : arguments[0];
                    return localizer.Format("Member.VideoLog.ChannelTitleChanged", locale, oldTitle, arguments[1]);
                default:
                    return notification.Message;
            }
        }
    }
}
