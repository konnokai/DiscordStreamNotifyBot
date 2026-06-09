using DiscordStreamNotifyBot.Interaction;
using TableVideo = DiscordStreamNotifyBot.DataBase.Table.Video;
using YTApiVideo = Google.Apis.YouTube.v3.Data.Video;

namespace DiscordStreamNotifyBot.SharedService.Youtube
{
    public static class EmbedBuilderFactory
    {
        public static EmbedBuilder CreateStreamDeleted(TableVideo video)
        {
            return new EmbedBuilder()
                .WithErrorColor()
                .WithTitle(video.VideoTitle)
                .WithDescription(Format.Url(video.ChannelTitle, $"https://www.youtube.com/channel/{video.ChannelId}"))
                .WithUrl($"https://www.youtube.com/watch?v={video.VideoId}")
                .AddField("直播狀態", "已刪除直播")
                .AddField("排定開台時間", video.ScheduledStartTime.ConvertDateTimeToDiscordMarkdown());
        }

        public static EmbedBuilder CreateStreamStarted(TableVideo video)
        {
            return new EmbedBuilder()
                .WithTitle(video.VideoTitle)
                .WithOkColor()
                .WithDescription(Format.Url(video.ChannelTitle, $"https://www.youtube.com/channel/{video.ChannelId}"))
                .WithImageUrl($"https://i.ytimg.com/vi/{video.VideoId}/maxresdefault.jpg")
                .WithUrl($"https://www.youtube.com/watch?v={video.VideoId}")
                .AddField("直播狀態", "開台中")
                .AddField("排定開台時間", video.ScheduledStartTime.ConvertDateTimeToDiscordMarkdown());
        }

        public static EmbedBuilder CreateStreamTimeChanged(TableVideo video, DateTime newStartTime)
        {
            return new EmbedBuilder()
                .WithErrorColor()
                .WithTitle(video.VideoTitle)
                .WithDescription(Format.Url(video.ChannelTitle, $"https://www.youtube.com/channel/{video.ChannelId}"))
                .WithImageUrl($"https://i.ytimg.com/vi/{video.VideoId}/maxresdefault.jpg")
                .WithUrl($"https://www.youtube.com/watch?v={video.VideoId}")
                .AddField("直播狀態", "尚未開台(已更改時間)")
                .AddField("排定開台時間", video.ScheduledStartTime.ConvertDateTimeToDiscordMarkdown())
                .AddField("更改開台時間", newStartTime.ConvertDateTimeToDiscordMarkdown());
        }

        // === 錄影程序 IPC 通知用（由 Redis 訂閱觸發；item 為 YouTube API 物件、video 為 DB 物件）===

        /// <summary>錄影開台通知（資料來自 YouTube API）。會員限定為綠色、否則紅色。</summary>
        public static EmbedBuilder CreateRecordStreamStarted(YTApiVideo item, DateTime startTime, bool isMemberOnly)
        {
            var embedBuilder = new EmbedBuilder()
                .WithTitle(item.Snippet.Title)
                .WithDescription(Format.Url(item.Snippet.ChannelTitle, $"https://www.youtube.com/channel/{item.Snippet.ChannelId}"))
                .WithImageUrl($"https://i.ytimg.com/vi/{item.Id}/maxresdefault.jpg")
                .WithUrl($"https://www.youtube.com/watch?v={item.Id}")
                .AddField("直播狀態", "開台中")
                .AddField("開台時間", startTime.ConvertDateTimeToDiscordMarkdown());

            if (isMemberOnly) embedBuilder.WithOkColor();
            else embedBuilder.WithRecordColor();

            return embedBuilder;
        }

        /// <summary>錄影關台通知（資料來自 YouTube API）。</summary>
        public static EmbedBuilder CreateRecordStreamEnded(YTApiVideo item, DateTime startTime, DateTime endTime)
        {
            return new EmbedBuilder()
                .WithErrorColor()
                .WithTitle(item.Snippet.Title)
                .WithDescription(Format.Url(item.Snippet.ChannelTitle, $"https://www.youtube.com/channel/{item.Snippet.ChannelId}"))
                .WithImageUrl($"https://i.ytimg.com/vi/{item.Id}/maxresdefault.jpg")
                .WithUrl($"https://www.youtube.com/watch?v={item.Id}")
                .AddField("直播狀態", "已關台")
                .AddField("直播時長", $"{endTime.Subtract(startTime):hh'時'mm'分'ss'秒'}")
                .AddField("關台時間", endTime.ConvertDateTimeToDiscordMarkdown());
        }

        /// <summary>關台並變更為會限影片通知（資料來自 DB）。</summary>
        public static EmbedBuilder CreateStreamEndedAsMemberOnly(TableVideo video, DateTime startTime, DateTime endTime)
        {
            return new EmbedBuilder()
                .WithErrorColor()
                .WithTitle(video.VideoTitle)
                .WithDescription(Format.Url(video.ChannelTitle, $"https://www.youtube.com/channel/{video.ChannelId}"))
                .WithImageUrl($"https://i.ytimg.com/vi/{video.VideoId}/maxresdefault.jpg")
                .WithUrl($"https://www.youtube.com/watch?v={video.VideoId}")
                .AddField("直播狀態", "已關台並變更為會限影片")
                .AddField("直播時間", $"{endTime.Subtract(startTime):hh'時'mm'分'ss'秒'}")
                .AddField("關台時間", endTime.ConvertDateTimeToDiscordMarkdown());
        }

        /// <summary>關台並變更為私人存檔（unarchived）通知（資料來自 DB）。</summary>
        public static EmbedBuilder CreateStreamUnarchived(TableVideo video)
        {
            return new EmbedBuilder()
                .WithTitle(video.VideoTitle)
                .WithOkColor()
                .WithDescription(Format.Url(video.ChannelTitle, $"https://www.youtube.com/channel/{video.ChannelId}"))
                .WithImageUrl($"https://i.ytimg.com/vi/{video.VideoId}/maxresdefault.jpg")
                .WithUrl($"https://www.youtube.com/watch?v={video.VideoId}")
                .AddField("直播狀態", "已關台並變更為私人存檔")
                .AddField("排定開台時間", video.ScheduledStartTime.ConvertDateTimeToDiscordMarkdown());
        }

        /// <summary>新上傳影片通知（PubSub，資料來自 DB）。</summary>
        public static EmbedBuilder CreateNewVideo(TableVideo video)
        {
            return new EmbedBuilder()
                .WithOkColor()
                .WithTitle(video.VideoTitle)
                .WithDescription(Format.Url(video.ChannelTitle, $"https://www.youtube.com/channel/{video.ChannelId}"))
                .WithImageUrl($"https://i.ytimg.com/vi/{video.VideoId}/maxresdefault.jpg")
                .WithUrl($"https://www.youtube.com/watch?v={video.VideoId}")
                .AddField("上傳時間", video.ScheduledStartTime.ConvertDateTimeToDiscordMarkdown());
        }

        /// <summary>PubSub 影片刪除通知（資料來自 DB）。</summary>
        public static EmbedBuilder CreatePubSubVideoDeleted(TableVideo video)
        {
            return new EmbedBuilder()
                .WithOkColor()
                .WithTitle(video.VideoTitle)
                .WithDescription(Format.Url(video.ChannelTitle, $"https://www.youtube.com/channel/{video.ChannelId}"))
                .WithImageUrl($"https://i.ytimg.com/vi/{video.VideoId}/maxresdefault.jpg")
                .WithUrl($"https://www.youtube.com/watch?v={video.VideoId}")
                .AddField("狀態", "已刪除")
                .AddField("排定開台/上傳時間", video.ScheduledStartTime.ConvertDateTimeToDiscordMarkdown());
        }
    }
}
