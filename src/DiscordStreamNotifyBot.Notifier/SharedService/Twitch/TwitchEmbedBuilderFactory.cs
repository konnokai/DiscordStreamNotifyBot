using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Interaction;

namespace DiscordStreamNotifyBot.SharedService.Twitch
{
    /// <summary>
    /// Twitch 通知 embed 工廠（階段 3 前置：集中化 embed 建構，供 cross-process 消費端重建）。
    /// <para>關台通知（含 VOD/Clip 動態欄位）因建構時需即時呼叫 API，未納入工廠、保留於 TwitchService 內。</para>
    /// </summary>
    public static class TwitchEmbedBuilderFactory
    {
        /// <summary>Twitch 開始直播通知。<paramref name="isRecord"/> 由呼叫端先行計算（含錄影副作用）。</summary>
        public static EmbedBuilder CreateStreamStarted(TwitchStream twitchStream, string profileImageUrl, bool isRecord)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder()
                .WithTitle(twitchStream.StreamTitle)
                .WithDescription(Format.Url($"{twitchStream.UserName}", $"https://twitch.tv/{twitchStream.UserLogin}"))
                .WithUrl($"https://twitch.tv/{twitchStream.UserLogin}")
                .WithThumbnailUrl(profileImageUrl)
                .WithImageUrl($"{twitchStream.ThumbnailUrl}?t={DateTime.Now.ToFileTime()}") // 新增參數避免預覽圖被 Discord 快取
                .AddField("直播狀態", "直播中");

            if (!string.IsNullOrEmpty(twitchStream.GameName))
                embedBuilder.AddField("分類", twitchStream.GameName, true);

            embedBuilder.AddField("開始時間", twitchStream.StreamStartAt.ConvertDateTimeToDiscordMarkdown());

            if (isRecord)
                embedBuilder.WithRecordColor();
            else
                embedBuilder.WithOkColor();

            return embedBuilder;
        }

        /// <summary>Twitch 直播資料更新通知（去抖動後彙整）。</summary>
        public static EmbedBuilder CreateChannelUpdate(string userName, string userLogin, string description, string profileImageUrl)
        {
            var embedBuilder = new EmbedBuilder()
                .WithOkColor()
                .WithTitle($"{userName} 直播資料更新")
                .WithUrl($"https://twitch.tv/{userLogin}")
                .WithDescription(description);

            if (!string.IsNullOrEmpty(profileImageUrl))
                embedBuilder.WithThumbnailUrl(profileImageUrl);

            return embedBuilder;
        }
    }
}
