using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Localization;

namespace DiscordStreamNotifyBot.SharedService.Twitcasting
{
    public static class TwitcastingEmbedBuilderFactory
    {
        public static EmbedBuilder CreateStreamStarted(TwitcastingStream twitcastingStream, bool isPrivate,
            bool isRecord, BotLocalizer localizer, string locale)
        {
            var embedBuilder = new EmbedBuilder()
                .WithTitle(twitcastingStream.StreamTitle)
                .WithDescription(Format.Url(twitcastingStream.ChannelTitle, $"https://twitcasting.tv/{twitcastingStream.ChannelId}"))
                .WithUrl($"https://twitcasting.tv/{twitcastingStream.ChannelId}/movie/{twitcastingStream.StreamId}")
                .WithImageUrl(twitcastingStream.ThumbnailUrl)
                .AddField(localizer.Get("Twitcasting.Notification.Private", locale),
                    localizer.Get(isPrivate ? "Common.Yes" : "Common.No", locale), true);

            if (!string.IsNullOrEmpty(twitcastingStream.StreamSubTitle))
                embedBuilder.AddField(localizer.Get("Notifications.Field.Subtitle", locale), twitcastingStream.StreamSubTitle, true);
            if (!string.IsNullOrEmpty(twitcastingStream.Category))
                embedBuilder.AddField(localizer.Get("Notifications.Field.Category", locale), twitcastingStream.Category, true);

            embedBuilder
                .AddField(localizer.Get("Notifications.Field.StartedAt", locale),
                    twitcastingStream.StreamStartAt.ConvertDateTimeToDiscordMarkdown())
                .AddField(localizer.Get("Twitcasting.Notification.Recording", locale),
                    localizer.Get(isRecord ? "Twitcasting.Recording.Available" : "Twitcasting.Recording.Unavailable", locale), true);

            if (isPrivate)
                embedBuilder.WithErrorColor();
            else if (isRecord)
                embedBuilder.WithRecordColor();
            else
                embedBuilder.WithOkColor();
            return embedBuilder;
        }
    }
}
