namespace DiscordStreamNotifyBot.Shared.Messages
{
    /// <summary>單一 Discord 伺服器的跨服務快照。</summary>
    public class GuildSnapshot
    {
        public ulong Id { get; set; }
        public string Name { get; set; }
        public ulong OwnerId { get; set; }
        public int MemberCount { get; set; }
    }

    /// <summary>單一 shard 寫入 Redis 的伺服器快照封套。</summary>
    public class GuildSnapshotEnvelope
    {
        public int ShardId { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public bool IsConnected { get; set; }
        public List<GuildSnapshot> Guilds { get; set; } = new();
    }
}
