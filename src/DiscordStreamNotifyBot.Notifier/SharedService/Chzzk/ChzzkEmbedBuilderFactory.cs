using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.Shared.Messages;

namespace DiscordStreamNotifyBot.SharedService.Chzzk
{
    /// <summary>
    /// CHZZK 通知 embed 重建（消費匯流排後執行）。
    /// <para>
    /// 時間欄位使用 DTO 已轉換的 UTC 時間（偵測端以 KST UTC+9 轉換一次），以 Discord timestamp 顯示；
    /// 缺少或無法解析的欄位省略，不捏造時間。CHZZK 無直播縮圖，使用頻道頭像。
    /// </para>
    /// </summary>
    public static class ChzzkEmbedBuilderFactory
    {
        public static EmbedBuilder CreateStreamStarted(ChzzkNotification dto, BotLocalizer localizer, string locale)
        {
            var embedBuilder = new EmbedBuilder()
                .WithOkColor()
                .WithTitle(GetTitle(dto, localizer, locale))
                .WithUrl(ChzzkUrls.Live(dto.ChannelId))
                .WithDescription(Format.Url(GetChannelName(dto), ChzzkUrls.Channel(dto.ChannelId)))
                .AddField(localizer.Get("Notifications.Field.Status", locale),
                    localizer.Get("Chzzk.StreamStatus.Live", locale));

            if (!string.IsNullOrEmpty(dto.Category))
                embedBuilder.AddField(localizer.Get("Notifications.Field.Category", locale), dto.Category, true);
            if (dto.StreamStartAt is { } openAtUtc)
                embedBuilder.AddField(localizer.Get("Notifications.Field.StartedAt", locale),
                    openAtUtc.ConvertDateTimeToDiscordMarkdown());

            WithChannelImage(embedBuilder, dto);
            return embedBuilder;
        }

        public static EmbedBuilder CreateStreamEnded(ChzzkNotification dto, BotLocalizer localizer, string locale)
        {
            var embedBuilder = new EmbedBuilder()
                .WithErrorColor()
                .WithTitle(GetTitle(dto, localizer, locale))
                .WithUrl(ChzzkUrls.Channel(dto.ChannelId))
                .WithDescription(Format.Url(GetChannelName(dto), ChzzkUrls.Channel(dto.ChannelId)))
                .AddField(localizer.Get("Notifications.Field.Status", locale),
                    localizer.Get("Chzzk.StreamStatus.Offline", locale));

            if (dto.StreamStartAt is { } openAtUtc)
            {
                embedBuilder.AddField(localizer.Get("Notifications.Field.StartedAt", locale),
                    openAtUtc.ConvertDateTimeToDiscordMarkdown());
                if (dto.StreamEndAt is { } closeAtUtc)
                {
                    embedBuilder.AddField(localizer.Get("Notifications.Field.EndedAt", locale),
                        closeAtUtc.ConvertDateTimeToDiscordMarkdown());
                    embedBuilder.AddField(localizer.Get("Notifications.Field.Duration", locale),
                        FormatDuration(closeAtUtc - openAtUtc, localizer, locale));
                }
            }

            WithChannelImage(embedBuilder, dto);
            return embedBuilder;
        }

        private static string GetTitle(ChzzkNotification dto, BotLocalizer localizer, string locale)
            => string.IsNullOrEmpty(dto.StreamTitle)
                ? localizer.Get("Chzzk.Notification.UnknownTitle", locale)
                : dto.StreamTitle;

        private static string GetChannelName(ChzzkNotification dto)
            => string.IsNullOrEmpty(dto.ChannelName) ? dto.ChannelId : dto.ChannelName;

        private static void WithChannelImage(EmbedBuilder embedBuilder, ChzzkNotification dto)
        {
            if (!string.IsNullOrEmpty(dto.ChannelImageUrl))
                embedBuilder.WithThumbnailUrl(dto.ChannelImageUrl);
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
