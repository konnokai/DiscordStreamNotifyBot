using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Shared;

namespace DiscordStreamNotifyBot.Interaction
{
    /// <summary>
    /// 偵測 / 通知共用的擴充方法與 DB 查詢輔助方法（自原 Notifier 的 Interaction.Extensions 拆出至 Shared）。
    /// <para>命名空間維持 <c>DiscordStreamNotifyBot.Interaction</c>，使既有 <c>using</c> 不需變動即可解析擴充方法；
    /// 靜態輔助方法改以 <c>SharedExtensions.</c> 呼叫。</para>
    /// </summary>
    public static class SharedExtensions
    {
        public static EmbedBuilder WithOkColor(this EmbedBuilder eb) =>
           eb.WithColor(00, 229, 132);
        public static EmbedBuilder WithErrorColor(this EmbedBuilder eb) =>
           eb.WithColor(40, 40, 40);
        public static EmbedBuilder WithRecordColor(this EmbedBuilder eb) =>
           eb.WithColor(255, 0, 0);

        public static string ConvertDateTimeToDiscordMarkdown(this DateTime dateTime)
        {
            long UTCTime = ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
            return $"<t:{UTCTime}:F> (<t:{UTCTime}:R>)";
        }

        public static DataBase.Table.Video.YTChannelType GetProductionType(this DataBase.Table.Video streamVideo)
        {
            using (var db = BotState.DbService.GetDbContext())
            {
                DataBase.Table.Video.YTChannelType type;
                var channel = db.YoutubeChannelOwnedType.AsNoTracking().FirstOrDefault((x) => x.ChannelId == streamVideo.ChannelId);

                if (channel != null)
                    type = channel.ChannelType;
                else
                    type = streamVideo.ChannelType;

                return type;
            }
        }

        public static string GetProductionName(this DataBase.Table.Video.YTChannelType channelType) =>
                channelType == DataBase.Table.Video.YTChannelType.Holo ? "Hololive" : channelType == DataBase.Table.Video.YTChannelType.Nijisanji ? "彩虹社" : "其他";

        public static bool HasStreamVideoByVideoId(string videoId)
        {
            videoId = videoId.Trim();

            using var db = BotState.DbService.GetDbContext();
            if (db.HoloVideos.AsNoTracking().Any((x) => x.VideoId == videoId)) return true;
            if (db.NijisanjiVideos.AsNoTracking().Any((x) => x.VideoId == videoId)) return true;
            if (db.OtherVideos.AsNoTracking().Any((x) => x.VideoId == videoId)) return true;
            if (db.NonApprovedVideos.AsNoTracking().Any((x) => x.VideoId == videoId)) return true;

            return false;
        }

        public static DataBase.Table.Video GetStreamVideoByVideoId(string videoId)
        {
            videoId = videoId.Trim();

            using var db = BotState.DbService.GetDbContext();
            return (DataBase.Table.Video)db.HoloVideos.AsNoTracking().FirstOrDefault((x) => x.VideoId == videoId)
                ?? (DataBase.Table.Video)db.NijisanjiVideos.AsNoTracking().FirstOrDefault((x) => x.VideoId == videoId)
                ?? (DataBase.Table.Video)db.OtherVideos.AsNoTracking().FirstOrDefault((x) => x.VideoId == videoId)
                ?? db.NonApprovedVideos.AsNoTracking().FirstOrDefault((x) => x.VideoId == videoId);
        }

        // 依預定開播時間排序時，用於聊天的待機室可能影響結果；暫時保留此方法。
        public static DataBase.Table.Video GetLastStreamVideoByChannelId(string channelId)
        {
            channelId = channelId.Trim();

            using var db = BotState.DbService.GetDbContext();
            return (DataBase.Table.Video)db.HoloVideos.AsNoTracking().OrderByDescending((x) => x.ScheduledStartTime).FirstOrDefault((x) => x.ChannelId == channelId)
                ?? (DataBase.Table.Video)db.NijisanjiVideos.AsNoTracking().OrderByDescending((x) => x.ScheduledStartTime).FirstOrDefault((x) => x.ChannelId == channelId)
                ?? (DataBase.Table.Video)db.OtherVideos.AsNoTracking().OrderByDescending((x) => x.ScheduledStartTime).FirstOrDefault((x) => x.ChannelId == channelId)
                ?? db.NonApprovedVideos.AsNoTracking().OrderByDescending((x) => x.ScheduledStartTime).FirstOrDefault((x) => x.ChannelId == channelId);
        }

        public static bool IsChannelInDb(string channelId)
        {
            channelId = channelId.Trim();

            using var db = BotState.DbService.GetDbContext();
            if (db.HoloVideos.AsNoTracking().Any((x) => x.ChannelId == channelId)) return true;
            if (db.NijisanjiVideos.AsNoTracking().Any((x) => x.ChannelId == channelId)) return true;
            if (db.OtherVideos.AsNoTracking().Any((x) => x.ChannelId == channelId)) return true;
            if (db.NonApprovedVideos.AsNoTracking().Any((x) => x.ChannelId == channelId)) return true;

            return false;
        }

        public static string GetNonApprovedChannelTitleByChannelId(this MainDbContext db, string channelId)
        {
            channelId = channelId.Trim();

            YoutubeChannelSpider youtubeChannelSpider;
            if ((youtubeChannelSpider = db.YoutubeChannelSpider.FirstOrDefault((x) => x.ChannelId == channelId)) != null)
                return youtubeChannelSpider.ChannelTitle;

            if (db.NonApprovedVideos.AsNoTracking().Any((x) => x.ChannelId == channelId))
                return db.NonApprovedVideos.OrderByDescending((x) => x.ScheduledStartTime).First((x) => x.ChannelId == channelId).ChannelTitle;

            return channelId;
        }
    }
}
