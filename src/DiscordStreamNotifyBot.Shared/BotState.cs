using DiscordStreamNotifyBot.DataBase;

namespace DiscordStreamNotifyBot.Shared
{
    /// <summary>
    /// 跨層共用的全域執行期狀態。原本散落在 <c>Bot</c> 的靜態成員集中於此，讓移入 Shared 的工具
    /// （Utility 等）與後續拆出的偵測服務、Notifier/Scraper 皆能存取同一份狀態，不再相互參考專案。
    /// <para>階段 1：<c>Bot</c> 的對應成員委派至此（單一真實來源）。階段 2/3 各角色直接寫入此處。</para>
    /// </summary>
    public static class BotState
    {
        // 基礎相依
        public static MainDbService DbService { get; set; }
        public static ConnectionMultiplexer Redis { get; set; }
        public static ISubscriber RedisSub { get; set; }
        public static IDatabase RedisDb { get; set; }

        /// <summary>Bot 應用程式擁有者（Notifier 登入後設定；Scraper 無頭模式為 null）。</summary>
        public static IUser ApplicatonOwner { get; set; }

        // 連線 / 程序狀態
        public static bool IsConnect { get; set; } = false;
        public static bool IsDisconnect { get; set; } = false;

        /// <summary>本程序是否為偵測宿主（Scraper 設 true；Notifier 恆 false）。</summary>
        public static bool IsDetectionHost { get; set; } = false;

        // 爬蟲執行中旗標（偵測再入防護）
        public static bool IsHoloChannelSpider { get; set; } = false;
        public static bool IsNijisanjiChannelSpider { get; set; } = false;
        public static bool IsOtherChannelSpider { get; set; } = false;

        // Shard
        public static int ShardId { get; set; }
        public static int TotalShardCount { get; set; }

        /// <summary>依 Discord 官方公式 <c>(guildId &gt;&gt; 22) % totalShards</c> 判斷該伺服器是否歸屬於本 Shard。</summary>
        public static bool IsServerOnThisShard(ulong guildId)
        {
            if (TotalShardCount <= 1)
                return true;

            return (int)((guildId >> 22) % (ulong)TotalShardCount) == ShardId;
        }

        /// <summary>
        /// 在 <c>GetGuild(guildId) == null</c> 時判斷是否「真的」該刪除該伺服器設定：
        /// 僅當「已 Ready」且「歸屬本 Shard」才回傳 true，避免多 Shard 互刪設定或啟動未完成時誤刪。
        /// </summary>
        public static bool ShouldDeleteMissingGuild(ulong guildId)
            => IsConnect && IsServerOnThisShard(guildId);
    }
}
