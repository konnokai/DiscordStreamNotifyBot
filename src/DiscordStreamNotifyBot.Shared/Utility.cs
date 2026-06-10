using DiscordStreamNotifyBot.DataBase;
using System.Runtime.InteropServices;

namespace DiscordStreamNotifyBot
{
    public static class Utility
    {
        public const string ECPayUrl = "https://p.ecpay.com.tw/B8CCC";
        public const string PaypalUrl = "https://paypal.me/jun112561";

        //static Regex videoIdRegex = new Regex(@"youtube_(?'ChannelId'[\w\-]{24})_(?'Date'[\d]{8})_(?'Time'[\d]{6})_(?'VideoId'[\w\-]{11}).mp4.part");
        public static string RedisKey { get; set; } = "";
        public static HashSet<ulong> OfficialGuildList { get; set; } = new HashSet<ulong>();

        public static List<string> GetNowRecordStreamList()
        {
            try
            {
                // 改用 Shared 的 Redis 連線單例，不再依賴 monolith 的 Bot 靜態狀態
                return RedisConnection.Instance.ConnectionMultiplexer.GetDatabase()
                    .SetMembers("youtube.nowRecord").Select((x) => x.ToString()).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return new List<string>();
            }
        }

        public static int GetDbStreamCount(MainDbService dbService)
        {
            try
            {
                int total = 0;

                using var db = dbService.GetDbContext();
                total += db.HoloVideos.AsNoTracking().Count();
                total += db.NijisanjiVideos.AsNoTracking().Count();
                total += db.OtherVideos.AsNoTracking().Count();

                return total;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "Utility-GetDbStreamCount");
                return 0;
            }
        }

        public static bool OfficialGuildContains(ulong guildId) =>
            OfficialGuildList.Contains(guildId);

        /// <summary>自 Redis 載入官方伺服器白名單（階段 5：跨 shard 共享，取代 OfficialList.json 檔案同步）。</summary>
        public static async Task LoadOfficialGuildListFromRedisAsync()
        {
            var db = RedisConnection.Instance.ConnectionMultiplexer.GetDatabase();
            var members = await db.SetMembersAsync(Shared.RedisChannels.SharedState.OfficialGuildList);
            OfficialGuildList = members.Select((x) => ulong.Parse(x.ToString())).ToHashSet();
        }

        /// <summary>將目前的官方伺服器白名單全量寫回 Redis（DEL + SADD，transaction）。</summary>
        public static async Task<bool> SaveOfficialGuildListToRedisAsync()
        {
            try
            {
                var db = RedisConnection.Instance.ConnectionMultiplexer.GetDatabase();
                var tran = db.CreateTransaction();
                _ = tran.KeyDeleteAsync(Shared.RedisChannels.SharedState.OfficialGuildList);
                if (OfficialGuildList.Count > 0)
                    _ = tran.SetAddAsync(Shared.RedisChannels.SharedState.OfficialGuildList,
                        OfficialGuildList.Select((x) => (RedisValue)x).ToArray());
                return await tran.ExecuteAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "SaveOfficialGuildListToRedis Error");
                return false;
            }
        }

        public static string GetDataFilePath(string fileName)
            => $"{AppDomain.CurrentDomain.BaseDirectory}Data{GetPlatformSlash()}{fileName}";

        public static string GetPlatformSlash()
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "\\" : "/";
    }
}
