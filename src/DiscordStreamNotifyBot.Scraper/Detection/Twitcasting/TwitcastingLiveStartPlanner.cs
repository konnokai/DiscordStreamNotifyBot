using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.HttpClients.Twitcasting.Model;
using DiscordStreamNotifyBot.Shared.Messages;

namespace DiscordStreamNotifyBot.Scraper.Detection.Twitcasting
{
    internal enum TwitcastingLiveStartAction
    {
        IgnoreDuplicate,
        PersistAndNotify,
        PersistRequestRecordingAndNotify,
    }

    internal sealed record TwitcastingLiveStartFacts(
        TwitcastingLiveStartEvent Event,
        bool StreamAlreadyExists,
        bool IsRecordingEnabled,
        string ResolvedCategoryName);

    internal sealed record TwitcastingStreamData(
        string ChannelId,
        string ChannelTitle,
        int StreamId,
        string StreamTitle,
        string StreamSubTitle,
        string Category,
        string ThumbnailUrl,
        DateTime StreamStartAt,
        bool IsPrivate);

    internal sealed record TwitcastingLiveStartPlan(
        TwitcastingLiveStartAction Action,
        TwitcastingStreamData Stream);

    internal static class TwitcastingLiveStartPlanner
    {
        internal static TwitcastingLiveStartPlan Plan(TwitcastingLiveStartFacts facts)
        {
            ArgumentNullException.ThrowIfNull(facts);
            ArgumentNullException.ThrowIfNull(facts.Event);

            if (facts.StreamAlreadyExists)
                return new TwitcastingLiveStartPlan(TwitcastingLiveStartAction.IgnoreDuplicate, null);

            var stream = new TwitcastingStreamData(
                facts.Event.ScreenId,
                facts.Event.ChannelTitle ?? string.Empty,
                facts.Event.StreamId,
                facts.Event.StreamTitle ?? "無標題",
                facts.Event.StreamSubTitle ?? string.Empty,
                facts.ResolvedCategoryName ?? string.Empty,
                facts.Event.ThumbnailUrl ?? string.Empty,
                DateTimeOffset.FromUnixTimeSeconds(facts.Event.CreatedAtUnixSeconds).UtcDateTime,
                facts.Event.IsProtected);

            var action = !facts.Event.IsProtected && facts.IsRecordingEnabled
                ? TwitcastingLiveStartAction.PersistRequestRecordingAndNotify
                : TwitcastingLiveStartAction.PersistAndNotify;
            return new TwitcastingLiveStartPlan(action, stream);
        }

        internal static TwitcastingStream ToEntity(TwitcastingStreamData stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            return new TwitcastingStream
            {
                ChannelId = stream.ChannelId,
                ChannelTitle = stream.ChannelTitle,
                StreamId = stream.StreamId,
                StreamTitle = stream.StreamTitle,
                StreamSubTitle = stream.StreamSubTitle,
                Category = stream.Category,
                ThumbnailUrl = stream.ThumbnailUrl,
                StreamStartAt = stream.StreamStartAt,
            };
        }

        internal static TwitcastingNotification CreateNotification(
            TwitcastingLiveStartPlan plan,
            bool recordingDelegated)
        {
            ArgumentNullException.ThrowIfNull(plan);
            ArgumentNullException.ThrowIfNull(plan.Stream);

            var stream = plan.Stream;
            return new TwitcastingNotification
            {
                ChannelId = stream.ChannelId,
                ChannelTitle = stream.ChannelTitle,
                StreamId = stream.StreamId,
                StreamTitle = stream.StreamTitle,
                StreamSubTitle = stream.StreamSubTitle,
                Category = stream.Category,
                ThumbnailUrl = stream.ThumbnailUrl,
                StreamStartAt = stream.StreamStartAt,
                IsPrivate = stream.IsPrivate,
                IsRecord = plan.Action == TwitcastingLiveStartAction.PersistRequestRecordingAndNotify && recordingDelegated,
            };
        }

        internal static string ResolveCategoryName(string categoryId, IEnumerable<Category> categories)
        {
            if (string.IsNullOrEmpty(categoryId))
                return string.Empty;

            var categoryName = categories?
                .Where(category => category?.SubCategories != null)
                .SelectMany(category => category.SubCategories)
                .FirstOrDefault(category => string.Equals(category.Id, categoryId, StringComparison.Ordinal))
                ?.Name;
            return string.IsNullOrEmpty(categoryName) ? categoryId : categoryName;
        }
    }
}
