using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.Shared.Messages;

namespace DiscordStreamNotifyBot.SharedService.Twitch
{
    public static class TwitchEmbedBuilderFactory
    {
        public static EmbedBuilder CreateStreamStarted(TwitchStream twitchStream, string profileImageUrl,
            bool isRecord, long thumbnailCacheBuster, BotLocalizer localizer, string locale)
        {
            var embedBuilder = new EmbedBuilder()
                .WithTitle(twitchStream.StreamTitle)
                .WithDescription(Format.Url(twitchStream.UserName, $"https://twitch.tv/{twitchStream.UserLogin}"))
                .WithUrl($"https://twitch.tv/{twitchStream.UserLogin}")
                .WithThumbnailUrl(profileImageUrl)
                .WithImageUrl($"{twitchStream.ThumbnailUrl}?t={thumbnailCacheBuster}")
                .AddField(localizer.Get("Notifications.Field.Status", locale), localizer.Get("Twitch.StreamStatus.Live", locale));

            if (!string.IsNullOrEmpty(twitchStream.GameName))
                embedBuilder.AddField(localizer.Get("Notifications.Field.Category", locale), twitchStream.GameName, true);

            embedBuilder.AddField(localizer.Get("Notifications.Field.StartedAt", locale),
                twitchStream.StreamStartAt.ConvertDateTimeToDiscordMarkdown());
            return isRecord ? embedBuilder.WithRecordColor() : embedBuilder.WithOkColor();
        }

        public static EmbedBuilder CreateStreamEnded(string userName, string userLogin,
            string streamTitle, DateTime? streamStartAtUtc, DateTime endAt,
            IReadOnlyCollection<TwitchClipInfo> clips, string clipsFallback,
            string profileImageUrl, string offlineImageUrl, BotLocalizer localizer, string locale)
        {
            var embedBuilder = new EmbedBuilder()
                .WithErrorColor()
                .WithTitle(localizer.Get("Twitch.Notification.UnknownTitle", locale))
                .WithUrl($"https://twitch.tv/{userLogin}")
                .WithDescription(Format.Url(userName, $"https://twitch.tv/{userLogin}"))
                .AddField(localizer.Get("Notifications.Field.Status", locale), localizer.Get("Twitch.StreamStatus.Offline", locale));

            if (streamStartAtUtc.HasValue)
            {
                var streamTime = endAt.ToUniversalTime().Subtract(streamStartAtUtc.Value);
                if (!string.IsNullOrEmpty(streamTitle))
                    embedBuilder.WithTitle(streamTitle);
                embedBuilder.AddField(localizer.Get("Notifications.Field.Duration", locale),
                    FormatDuration(streamTime, localizer, locale));
            }

            embedBuilder.AddField(localizer.Get("Notifications.Field.EndedAt", locale), endAt.ConvertDateTimeToDiscordMarkdown());

            string clipsValue = FormatClips(clips, localizer, locale);
            if (string.IsNullOrEmpty(clipsValue))
                clipsValue = clipsFallback;
            if (!string.IsNullOrEmpty(clipsValue))
                embedBuilder.AddField(localizer.Get("Twitch.Notification.Clips", locale), clipsValue);

            if (!string.IsNullOrEmpty(offlineImageUrl))
                embedBuilder.WithImageUrl(offlineImageUrl);
            if (!string.IsNullOrEmpty(profileImageUrl))
                embedBuilder.WithThumbnailUrl(profileImageUrl);
            return embedBuilder;
        }

        public static EmbedBuilder CreateChannelUpdate(string userName, string userLogin,
            IReadOnlyCollection<TwitchChannelUpdateInfo> updates, string descriptionFallback,
            string profileImageUrl, BotLocalizer localizer, string locale)
        {
            string description = FormatUpdates(updates, localizer, locale);
            if (string.IsNullOrEmpty(description))
                description = descriptionFallback;

            var embedBuilder = new EmbedBuilder()
                .WithOkColor()
                .WithTitle(localizer.Format("Twitch.Notification.UpdateTitle", locale, userName))
                .WithUrl($"https://twitch.tv/{userLogin}")
                .WithDescription(description);
            if (!string.IsNullOrEmpty(profileImageUrl))
                embedBuilder.WithThumbnailUrl(profileImageUrl);
            return embedBuilder;
        }

        private static string FormatClips(IReadOnlyCollection<TwitchClipInfo> clips, BotLocalizer localizer, string locale)
        {
            if (clips == null || clips.Count == 0)
                return null;
            return string.Join('\n', clips.Select((clip, index) => localizer.Format(
                "Twitch.Notification.ClipEntry", locale, index + 1, clip.Title, clip.Url, clip.CreatorName,
                clip.ViewCount.ToString("N0", SupportedLocale.GetCulture(locale)))));
        }

        private static string FormatUpdates(IReadOnlyCollection<TwitchChannelUpdateInfo> updates,
            BotLocalizer localizer, string locale)
        {
            if (updates == null || updates.Count == 0)
                return null;

            return string.Join("\n\n", updates.Select(update =>
            {
                var lines = new List<string>
                {
                    $"`{FormatDuration(TimeSpan.FromSeconds(update.ElapsedSeconds), localizer, locale)}`"
                };
                if (update.NewTitle != null)
                    lines.Add(localizer.Format("Twitch.Notification.TitleChanged", locale, update.OldTitle, update.NewTitle));
                if (update.NewCategory != null)
                {
                    string oldCategory = string.IsNullOrEmpty(update.OldCategory) ? localizer.Get("Common.None", locale) : update.OldCategory;
                    string newCategory = string.IsNullOrEmpty(update.NewCategory) ? localizer.Get("Common.None", locale) : update.NewCategory;
                    lines.Add(localizer.Format("Twitch.Notification.CategoryChanged", locale, oldCategory, newCategory));
                }
                return string.Join('\n', lines);
            }));
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
