using System.ComponentModel.DataAnnotations.Schema;

namespace DiscordStreamNotifyBot.DataBase.Table
{
    public class TwitchStream : DbEntity
    {
        public string StreamId { get; set; }
        public string StreamTitle { get; set; }
        public DateTime StreamStartAt { get; set; }
        public string UserId { get; set; }
        public string UserLogin { get; set; }
        public string UserName { get; set; }
        public string GameName { get; set; } = "";
        public string ThumbnailUrl { get; set; } = "";

        /// <summary>Redis 直播狀態的關台確認時間；歷史資料表不儲存此欄位。</summary>
        [NotMapped]
        public DateTime? StreamEndAt { get; set; }
    }
}
