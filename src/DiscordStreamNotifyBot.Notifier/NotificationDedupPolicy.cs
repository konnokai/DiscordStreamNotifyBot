using DiscordStreamNotifyBot.Shared.Messages;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DiscordStreamNotifyBot
{
    internal static class NotificationDedupPolicy
    {
        /// <summary>
        /// 依 DTO 主鍵與通知類型組去重鍵。解析失敗回傳 null，不做去重。
        /// 鍵必須帶 shardId，避免廣播訊息被其他 shard 的去重鍵誤擋。
        /// </summary>
        internal static string TryGetKey(int shardId, string type, string json)
        {
            try
            {
                var jo = JObject.Parse(json);
                return type switch
                {
                    NotifyType.Youtube => jo.Value<int?>("NoticeType") switch
                    {
                        (int)YoutubeNoticeType.ChangeTime =>
                            $"notified:{shardId}:yt:{jo.Value<string>("VideoId")}:{(int)YoutubeNoticeType.ChangeTime}:{StableHash(json)}",
                        (int)YoutubeNoticeType.Delete =>
                            $"notified:{shardId}:yt:{jo.Value<string>("VideoId")}:{(int)YoutubeNoticeType.Delete}:{jo.Value<bool?>("IsUnarchived")}",
                        var noticeType => $"notified:{shardId}:yt:{jo.Value<string>("VideoId")}:{noticeType}",
                    },
                    // Twitch 以直播場次去重，ChangeStreamData 或缺少 StreamId 時仍只靠 XACK。
                    NotifyType.Twitch => string.IsNullOrEmpty(jo.Value<string>("StreamId"))
                        ? null
                        : $"notified:{shardId}:tw:{jo.Value<string>("StreamId")}:{jo.Value<int?>("NoticeType")}",
                    NotifyType.Twitcasting => $"notified:{shardId}:tc:{jo.Value<string>("ChannelId")}:{jo.Value<int?>("StreamId")}",
                    NotifyType.Banner => $"notified:{shardId}:banner:{jo.Value<string>("ChannelId")}:{jo.Value<string>("VideoId")}",
                    NotifyType.YoutubeMemberVideoLog => $"notified:{shardId}:ytmv:{jo.Value<string>("CheckChannelId")}:{StableHash(jo.Value<string>("Message") ?? "")}",
                    _ => null,
                };
            }
            catch
            {
                return null;
            }
        }

        private static string StableHash(string value)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(hash.AsSpan(0, 12)).ToLowerInvariant();
        }
    }
}
