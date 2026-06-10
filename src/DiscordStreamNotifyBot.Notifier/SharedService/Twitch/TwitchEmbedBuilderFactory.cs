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

        /// <summary>
        /// Twitch 關台通知。VOD/Clip 資料由偵測端先行取得（<paramref name="clipsValue"/> 為已組好的 Markdown）；
        /// <paramref name="streamStartAtUtc"/> 為 null 時代表 Redis/VOD 皆無資料，略過標題與直播時長欄位。
        /// </summary>
        public static EmbedBuilder CreateStreamEnded(
            string userName, string userLogin,
            string streamTitle, DateTime? streamStartAtUtc, DateTime endAt,
            string clipsValue, string profileImageUrl, string offlineImageUrl)
        {
            var embedBuilder = new EmbedBuilder()
                .WithErrorColor()
                .WithTitle("(找不到標題)")
                .WithUrl($"https://twitch.tv/{userLogin}")
                .WithDescription(Format.Url($"{userName}", $"https://twitch.tv/{userLogin}"))
                .AddField("直播狀態", "已關台");

            if (streamStartAtUtc.HasValue)
            {
                // StreamStartAt 是 UTC+0 時間，因此 endAt 也需要先轉換成 UTC+0 之後再做計算
                var streamTime = endAt.ToUniversalTime().Subtract(streamStartAtUtc.Value);

                embedBuilder
                    .WithTitle(streamTitle)
                    .AddField("直播時長", streamTime.TotalDays >= 1 ? $"{streamTime:d' 天 'h' 時 'm' 分 's' 秒'}" : $"{streamTime:h' 時 'm' 分 's' 秒'}");
            }

            embedBuilder.AddField("關台時間", endAt.ConvertDateTimeToDiscordMarkdown());

            // 最後才新增 Clip 資訊
            if (!string.IsNullOrEmpty(clipsValue))
            {
                embedBuilder.AddField("最多觀看的 Clip", clipsValue);
            }

            if (!string.IsNullOrEmpty(offlineImageUrl))
                embedBuilder.WithImageUrl(offlineImageUrl);
            if (!string.IsNullOrEmpty(profileImageUrl))
                embedBuilder.WithThumbnailUrl(profileImageUrl);

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
