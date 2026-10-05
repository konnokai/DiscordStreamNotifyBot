using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.HttpClients.Twitcasting.Model;
using DiscordStreamNotifyBot.Shared.Messages;

namespace DiscordStreamNotifyBot.Scraper.Detection.Twitcasting
{
    internal static class TwitcastingLiveStartPlanner
    {
        /// <summary>由開台事件建立通知；<see cref="TwitcastingNotification.IsRecord"/> 由呼叫端依實際錄影委派結果設定。</summary>
        internal static TwitcastingNotification CreateNotification(TwitcastingLiveStartEvent startEvent, string categoryName)
            => new()
            {
                ChannelId = startEvent.ScreenId,
                ChannelTitle = startEvent.ChannelTitle ?? string.Empty,
                StreamId = startEvent.StreamId,
                StreamTitle = startEvent.StreamTitle ?? "無標題",
                StreamSubTitle = startEvent.StreamSubTitle ?? string.Empty,
                Category = categoryName ?? string.Empty,
                ThumbnailUrl = startEvent.ThumbnailUrl ?? string.Empty,
                StreamStartAt = DateTimeOffset.FromUnixTimeSeconds(startEvent.CreatedAtUnixSeconds).UtcDateTime,
                IsPrivate = startEvent.IsProtected,
            };

        internal static TwitcastingStream ToEntity(TwitcastingNotification notification)
            => new()
            {
                ChannelId = notification.ChannelId,
                ChannelTitle = notification.ChannelTitle,
                StreamId = notification.StreamId,
                StreamTitle = notification.StreamTitle,
                StreamSubTitle = notification.StreamSubTitle,
                Category = notification.Category,
                ThumbnailUrl = notification.ThumbnailUrl,
                StreamStartAt = notification.StreamStartAt,
            };

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
