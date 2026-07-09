namespace DiscordStreamNotifyBot.DataBase.Table
{
    public class GuildYoutubeMemberConfig : DbEntity
    {
        public ulong GuildId { get; set; }
        public string MemberCheckChannelId { get; set; } = "";
        public string MemberCheckChannelTitle { get; set; } = "";
        public string MemberCheckVideoId { get; set; } = "-";
        public ulong MemberCheckGrantRoleId { get; set; } = 0;

        /// <summary>
        /// 由管理員以 <c>/member-set set-check-video</c> 手動指定 <see cref="MemberCheckVideoId"/>（避免自動探索挑到高階會限影片）。
        /// 為 true 時，Scraper 的會限影片自動探索與各失敗重置點都不會覆寫此 videoId。
        /// </summary>
        public bool IsManualVideoId { get; set; } = false;
    }
}
