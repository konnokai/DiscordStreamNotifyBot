using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.Cluster;

namespace DiscordStreamNotifyBot.Command.Admin
{
    public class AdministrationService : ICommandService
    {
        private string _reloadOfficialGuildListKey = "DiscordStreamBot:Admin:ReloadOfficialGuildList";
        private readonly DiscordSocketClient _client;
        private readonly ClusterQueryService _clusterQuery;

        public AdministrationService(DiscordSocketClient client, ClusterQueryService clusterQuery)
        {
            _client = client;
            _clusterQuery = clusterQuery;

            Bot.RedisSub.Subscribe(new RedisChannel(_reloadOfficialGuildListKey, RedisChannel.PatternMode.Literal), async (_, _) =>
            {
                try
                {
                    // 階段 5：白名單存於 Redis（跨 shard 共享），收到變更通知後重新載入
                    await Utility.LoadOfficialGuildListFromRedisAsync();
                    Log.Info($"官方伺服器白名單已重新載入（{Utility.OfficialGuildList.Count} 筆）");
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), "ReloadOfficialGuildList Error");
                }
            });

            // leave 指令廣播：目標伺服器只在單一 shard，非持有 shard 自動 no-op
            Bot.RedisSub.Subscribe(new RedisChannel(RedisChannels.Notifier.LeaveGuild, RedisChannel.PatternMode.Literal), async (_, value) =>
            {
                if (!ulong.TryParse(value, out ulong gid))
                    return;

                var guild = _client.GetGuild(gid);
                if (guild == null)
                    return; // 非本 shard 持有

                try
                {
                    await guild.LeaveAsync();
                    Log.Info($"已離開伺服器: {guild.Name} ({gid})");
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), $"離開伺服器失敗: {gid}");
                }
            });

            // leaveNoNotify 廣播：各 shard 離開自己持有的「未設定通知」伺服器，並回報數量
            Bot.RedisSub.Subscribe(new RedisChannel(RedisChannels.Notifier.LeaveNoNotifyGuild, RedisChannel.PatternMode.Literal), (_, value) =>
            {
                string correlationId = value;
                int count = LeaveNoNotifyGuildsLocally();
                Bot.RedisSub.Publish(
                    new RedisChannel(RedisChannels.Cluster.QueryReply(correlationId), RedisChannel.PatternMode.Literal),
                    JsonConvert.SerializeObject(new { ShardId = Bot.ShardId, Count = count }));
            });
        }

        /// <summary>廣播關閉訊號至所有 Notifier shard（die 指令用）。</summary>
        internal Task BroadcastShutdownAsync()
            => Bot.RedisSub.PublishAsync(new RedisChannel(RedisChannels.Notifier.Shutdown, RedisChannel.PatternMode.Literal), "");

        /// <summary>廣播離開指定伺服器（只有持有該伺服器的 shard 會實際離開）。</summary>
        internal Task BroadcastLeaveGuildAsync(ulong guildId)
            => Bot.RedisSub.PublishAsync(new RedisChannel(RedisChannels.Notifier.LeaveGuild, RedisChannel.PatternMode.Literal), guildId.ToString());

        /// <summary>
        /// 廣播「離開未設定通知的伺服器」，彙總各 shard 預計離開的數量（實際離開於背景進行）。
        /// 單 shard 時直接本機處理。
        /// </summary>
        internal async Task<(int Total, int Responded, int Expected)> BroadcastLeaveNoNotifyAsync()
        {
            if (Bot.TotalShardCount <= 1)
            {
                int n = LeaveNoNotifyGuildsLocally();
                return (n, 1, 1);
            }

            string correlationId = Guid.NewGuid().ToString("N");
            var (replies, responded, expected) = await _clusterQuery.CollectAsync(correlationId, null, () =>
                Bot.RedisSub.PublishAsync(
                    new RedisChannel(RedisChannels.Notifier.LeaveNoNotifyGuild, RedisChannel.PatternMode.Literal),
                    correlationId));

            int total = 0;
            foreach (var reply in replies)
            {
                try { total += Newtonsoft.Json.Linq.JObject.Parse(reply).Value<int>("Count"); }
                catch (Exception ex) { Log.Error(ex.Demystify(), "解析 LeaveNoNotify 回應失敗"); }
            }

            return (total, responded, expected);
        }

        /// <summary>離開本 shard 持有的「未設定通知」伺服器（回傳預計離開數量；實際離開於背景進行避免阻塞）。</summary>
        private int LeaveNoNotifyGuildsLocally()
        {
            var guilds = GetNoNotifyGuilds();
            int count = guilds.Count;
            if (count == 0)
                return 0;

            _ = Task.Run(async () =>
            {
                foreach (var guild in guilds)
                {
                    try
                    {
                        await guild.LeaveAsync();
                        Log.Info($"已離開未設定通知的伺服器: {guild.Name} ({guild.Id})");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Demystify(), $"離開未設定通知的伺服器失敗: {guild.Id}");
                    }
                }
            });

            return count;
        }

        /// <summary>儲存白名單至 Redis 並廣播變更，讓所有 shard 重新載入（階段 5）。</summary>
        /// <summary>官方伺服器名單變更的結果：<see cref="IsOfficial"/> 為變更後的狀態，<see cref="Broadcasted"/> 為廣播是否成功。</summary>
        internal readonly record struct OfficialGuildChangeResult(bool Success, bool IsOfficial, bool Broadcasted);

        /// <summary>
        /// 以 Redis 為真實來源切換單一伺服器的官方狀態：只對 SET 單一成員做原子的 SADD／SREM，
        /// 不整份覆寫，避免與其他 shard 或連續點擊互相蓋掉；寫入後再廣播讓各 shard（含自己）重新載入。
        /// </summary>
        internal Task<OfficialGuildChangeResult> ToggleOfficialGuildAsync(ulong guildId)
            => ChangeOfficialGuildAsync(guildId, null);

        /// <summary>明確加入或移除單一伺服器（<paramref name="official"/>），與 <see cref="ToggleOfficialGuildAsync"/> 共用 Redis 原子寫入流程。</summary>
        internal Task<OfficialGuildChangeResult> SetOfficialGuildAsync(ulong guildId, bool official)
            => ChangeOfficialGuildAsync(guildId, official);

        private async Task<OfficialGuildChangeResult> ChangeOfficialGuildAsync(ulong guildId, bool? desired)
        {
            bool isOfficial;
            try
            {
                var db = RedisConnection.Instance.ConnectionMultiplexer.GetDatabase();
                bool wasOfficial = await db.SetContainsAsync(RedisChannels.SharedState.OfficialGuildList, guildId);
                isOfficial = desired ?? !wasOfficial;
                if (!isOfficial)
                    await db.SetRemoveAsync(RedisChannels.SharedState.OfficialGuildList, guildId);
                else
                    await db.SetAddAsync(RedisChannels.SharedState.OfficialGuildList, guildId);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"ToggleOfficialGuild 寫入 Redis 失敗: {guildId}");
                return new OfficialGuildChangeResult(false, false, false);
            }

            // 以複本整個替換，避免和其他執行緒讀取同一個 HashSet 時發生併發修改。
            var next = new HashSet<ulong>(Utility.OfficialGuildList);
            if (isOfficial)
                next.Add(guildId);
            else
                next.Remove(guildId);
            Utility.OfficialGuildList = next;

            try
            {
                await Bot.RedisSub.PublishAsync(new RedisChannel(_reloadOfficialGuildListKey, RedisChannel.PatternMode.Literal), "");
                return new OfficialGuildChangeResult(true, isOfficial, true);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"ToggleOfficialGuild 廣播失敗: {guildId}");
                return new OfficialGuildChangeResult(true, isOfficial, false);
            }
        }

        internal IReadOnlyCollection<SocketGuild> GetNoNotifyGuilds()
        {
            var guilds = new List<SocketGuild>(_client.Guilds);
            var snapshots = guilds.Select(guild => new GuildSnapshot
            {
                Id = guild.Id,
                Name = guild.Name,
                OwnerId = guild.OwnerId,
                MemberCount = guild.MemberCount,
            }).ToList();
            var noNotifyGuildIds = _clusterQuery.FilterNoNotifyGuilds(snapshots)
                .Select(guild => guild.Id)
                .ToHashSet();

            return guilds
                .Where(guild => noNotifyGuildIds.Contains(guild.Id))
                .OrderByDescending(guild => guild.MemberCount)
                .ToList();
        }
    }
}
