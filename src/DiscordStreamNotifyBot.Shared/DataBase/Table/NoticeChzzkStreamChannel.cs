namespace DiscordStreamNotifyBot.DataBase.Table
{
    /// <summary>
    /// guild 對 CHZZK 頻道的通知設定（每個 guild 對同一頻道僅一筆，更換目的地時更新原紀錄）。
    /// </summary>
    public class NoticeChzzkStreamChannel : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong DiscordChannelId { get; set; }
        public string NoticeChzzkChannelId { get; set; }
        public string StartStreamMessage { get; set; } = "";
        public string EndStreamMessage { get; set; } = "";
    }
}
