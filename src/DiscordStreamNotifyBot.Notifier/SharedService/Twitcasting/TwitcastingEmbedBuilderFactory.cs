using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Interaction;

namespace DiscordStreamNotifyBot.SharedService.Twitcasting
{
    /// <summary>
    /// TwitCasting 通知 embed 工廠（階段 3 前置：集中化 embed 建構，供 cross-process 消費端重建）。
    /// </summary>
    public static class TwitcastingEmbedBuilderFactory
    {
        /// <summary>TwitCasting 開台通知。</summary>
        public static EmbedBuilder CreateStreamStarted(TwitcastingStream twitcastingStream, bool isPrivate, bool isRecord)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder()
                .WithTitle(twitcastingStream.StreamTitle)
                .WithDescription(Format.Url($"{twitcastingStream.ChannelTitle}", $"https://twitcasting.tv/{twitcastingStream.ChannelId}"))
                .WithUrl($"https://twitcasting.tv/{twitcastingStream.ChannelId}/movie/{twitcastingStream.StreamId}")
                .WithImageUrl(twitcastingStream.ThumbnailUrl)
                .AddField("需要密碼的私人直播", isPrivate ? "是" : "否", true);

            if (!string.IsNullOrEmpty(twitcastingStream.StreamSubTitle)) embedBuilder.AddField("副標題", twitcastingStream.StreamSubTitle, true);
            if (!string.IsNullOrEmpty(twitcastingStream.Category)) embedBuilder.AddField("分類", twitcastingStream.Category, true);

            embedBuilder.AddField("開始時間", twitcastingStream.StreamStartAt.ConvertDateTimeToDiscordMarkdown());

            if (isPrivate) embedBuilder.WithErrorColor();
            if (isRecord) embedBuilder.WithRecordColor();
            else embedBuilder.WithOkColor();

            return embedBuilder;
        }
    }
}
