namespace DiscordStreamNotifyBot.Scraper.Detection.Twitcasting
{
    internal sealed record TwitcastingLiveStartEvent(
        string UserId,
        string ScreenId,
        string ChannelTitle,
        int StreamId,
        string StreamTitle,
        string StreamSubTitle,
        string CategoryId,
        string ThumbnailUrl,
        long CreatedAtUnixSeconds,
        bool IsProtected);

    internal static class TwitcastingWebhookParser
    {
        internal static bool TryParseLiveStart(string json, out TwitcastingLiveStartEvent value)
        {
            value = null;

            if (string.IsNullOrWhiteSpace(json))
                return false;

            TwitCastingWebHookJson payload;
            try
            {
                payload = JsonConvert.DeserializeObject<TwitCastingWebHookJson>(json);
            }
            catch (JsonException)
            {
                return false;
            }

            if (payload?.Movie == null || payload.Broadcaster == null ||
                !string.Equals(payload.Event, "livestart", StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(payload.Broadcaster.Id) ||
                string.IsNullOrWhiteSpace(payload.Broadcaster.ScreenId) ||
                !string.Equals(payload.Movie.UserId, payload.Broadcaster.Id, StringComparison.Ordinal) ||
                !int.TryParse(payload.Movie.Id, out int streamId) || streamId <= 0)
            {
                return false;
            }

            value = new TwitcastingLiveStartEvent(
                payload.Broadcaster.Id,
                payload.Broadcaster.ScreenId,
                payload.Broadcaster.Name ?? string.Empty,
                streamId,
                payload.Movie.Title,
                payload.Movie.Subtitle,
                payload.Movie.Category,
                payload.Movie.LargeThumbnail,
                payload.Movie.Created,
                payload.Movie.IsProtected);
            return true;
        }
    }
}
