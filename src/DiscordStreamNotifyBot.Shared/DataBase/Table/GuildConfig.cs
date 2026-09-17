namespace DiscordStreamNotifyBot.DataBase.Table
{
    public class GuildConfig : DbEntity
    {
        public ulong GuildId { get; set; }
        public string Locale { get; set; }
        public ulong NoticeChannelId { get; set; } = 0;
        public uint MaxYouTubeSpiderCount { get; set; } = 3;
        public uint MaxTwitchSpiderCount { get; set; } = 3;
    }
}
