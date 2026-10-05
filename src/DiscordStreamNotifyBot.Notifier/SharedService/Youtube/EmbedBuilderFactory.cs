using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Localization;
using TableVideo = DiscordStreamNotifyBot.DataBase.Table.Video;

namespace DiscordStreamNotifyBot.SharedService.Youtube
{
    public static class EmbedBuilderFactory
    {
        public static EmbedBuilder CreateStreamDeleted(TableVideo video, BotLocalizer localizer, string locale)
            => CreateVideoEmbed(video)
                .WithErrorColor()
                .AddField(localizer.Get("Notifications.Field.Status", locale), localizer.Get("Youtube.NotificationStatus.DeletedStream", locale))
                .AddField(localizer.Get("Notifications.Field.ScheduledStart", locale), video.ScheduledStartTime.ConvertDateTimeToDiscordMarkdown());

        public static EmbedBuilder CreateStreamStarted(TableVideo video, BotLocalizer localizer, string locale)
            => CreateVideoEmbed(video, true)
                .WithOkColor()
                .AddField(localizer.Get("Notifications.Field.Status", locale), localizer.Get("Youtube.StreamStatus.Live", locale))
                .AddField(localizer.Get("Notifications.Field.ScheduledStart", locale), video.ScheduledStartTime.ConvertDateTimeToDiscordMarkdown());

        public static EmbedBuilder CreateStreamEnded(TableVideo video, DateTime startTime, DateTime endTime,
            BotLocalizer localizer, string locale)
            => CreateVideoEmbed(video, true)
                .WithErrorColor()
                .AddField(localizer.Get("Notifications.Field.Status", locale), localizer.Get("Youtube.StreamStatus.Ended", locale))
                .AddField(localizer.Get("Notifications.Field.Duration", locale), FormatDuration(endTime.Subtract(startTime), localizer, locale))
                .AddField(localizer.Get("Notifications.Field.EndedAt", locale), endTime.ConvertDateTimeToDiscordMarkdown());

        public static EmbedBuilder CreateStreamEndedAsMemberOnly(TableVideo video, DateTime startTime, DateTime endTime,
            BotLocalizer localizer, string locale)
            => CreateVideoEmbed(video, true)
                .WithErrorColor()
                .AddField(localizer.Get("Notifications.Field.Status", locale), localizer.Get("Youtube.NotificationStatus.EndedAsMemberOnly", locale))
                .AddField(localizer.Get("Notifications.Field.Duration", locale), FormatDuration(endTime.Subtract(startTime), localizer, locale))
                .AddField(localizer.Get("Notifications.Field.EndedAt", locale), endTime.ConvertDateTimeToDiscordMarkdown());

        public static EmbedBuilder CreateStreamUnarchived(TableVideo video, BotLocalizer localizer, string locale)
            => CreateVideoEmbed(video, true)
                .WithOkColor()
                .AddField(localizer.Get("Notifications.Field.Status", locale), localizer.Get("Youtube.NotificationStatus.Unarchived", locale))
                .AddField(localizer.Get("Notifications.Field.ScheduledStart", locale), video.ScheduledStartTime.ConvertDateTimeToDiscordMarkdown());

        public static EmbedBuilder CreateNewVideo(TableVideo video, BotLocalizer localizer, string locale)
            => CreateVideoEmbed(video, true)
                .WithOkColor()
                .AddField(localizer.Get("Notifications.Field.UploadedAt", locale), video.ScheduledStartTime.ConvertDateTimeToDiscordMarkdown());

        public static EmbedBuilder CreateNewStream(TableVideo video, DateTime scheduledStartTime,
            BotLocalizer localizer, string locale)
            => CreateVideoEmbed(video, true)
                .WithErrorColor()
                .AddField(localizer.Get("Notifications.Field.Status", locale), localizer.Get("Youtube.StreamStatus.Upcoming", locale))
                .AddField(localizer.Get("Notifications.Field.ScheduledStart", locale), scheduledStartTime.ConvertDateTimeToDiscordMarkdown());

        public static EmbedBuilder CreateStreamTimeChangedReminder(TableVideo newVideo, DateTime oldScheduledStartTime,
            BotLocalizer localizer, string locale)
            => CreateVideoEmbed(newVideo, true)
                .WithErrorColor()
                .AddField(localizer.Get("Notifications.Field.Status", locale), localizer.Get("Youtube.NotificationStatus.UpcomingChanged", locale), true)
                .AddField(localizer.Get("Notifications.Field.ScheduledStart", locale), oldScheduledStartTime.ConvertDateTimeToDiscordMarkdown())
                .AddField(localizer.Get("Notifications.Field.ChangedStart", locale), newVideo.ScheduledStartTime.ConvertDateTimeToDiscordMarkdown());

        private static EmbedBuilder CreateVideoEmbed(TableVideo video, bool includeImage = false)
        {
            var embed = new EmbedBuilder()
                .WithTitle(video.VideoTitle)
                .WithDescription(Format.Url(video.ChannelTitle, $"https://www.youtube.com/channel/{video.ChannelId}"))
                .WithUrl($"https://www.youtube.com/watch?v={video.VideoId}");
            if (includeImage)
                embed.WithImageUrl($"https://i.ytimg.com/vi/{video.VideoId}/maxresdefault.jpg");
            return embed;
        }

        private static string FormatDuration(TimeSpan duration, BotLocalizer localizer, string locale)
        {
            if (duration < TimeSpan.Zero)
                duration = TimeSpan.Zero;
            return duration.Days > 0
                ? localizer.Format("Notifications.Duration.Days", locale, duration.Days, duration.Hours, duration.Minutes, duration.Seconds)
                : localizer.Format("Notifications.Duration.Hours", locale, (int)duration.TotalHours, duration.Minutes, duration.Seconds);
        }
    }
}
