using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService.Cluster;

namespace DiscordStreamNotifyBot.Command.Admin
{
    public class AdministrationService : ICommandService
    {
        private string _reloadOfficialGuildListKey = "DiscordStreamBot:Admin:ReloadOfficialGuildList";
        private readonly DiscordSocketClient _client;
        private readonly MainDbService _dbService;
        private readonly ClusterQueryService _clusterQuery;

        public AdministrationService(DiscordSocketClient client, MainDbService service, ClusterQueryService clusterQuery)
        {
            _client = client;
            _dbService = service;
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

            // die 指令只會被單一 shard 收到，透過 Redis 廣播讓所有 Notifier shard 一起關閉
            Bot.RedisSub.Subscribe(new RedisChannel(RedisChannels.Notifier.Shutdown, RedisChannel.PatternMode.Literal), (_, _) =>
            {
                Log.Info("收到關閉廣播，準備關閉本 shard");
                Bot.IsDisconnect = true;
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

        public async Task ClearUser(ITextChannel textChannel)
        {
            IEnumerable<IMessage> msgs = (await textChannel.GetMessagesAsync(100).FlattenAsync().ConfigureAwait(false))
                  .Where((item) => item.Author.Id == _client.CurrentUser.Id);

            await Task.WhenAll(Task.Delay(1000), textChannel.DeleteMessagesAsync(msgs)).ConfigureAwait(false);
        }

        /// <summary>儲存白名單至 Redis 並廣播變更，讓所有 shard 重新載入（階段 5）。</summary>
        internal async Task<bool> SaveAndBroadcastOfficialGuildListAsync()
        {
            try
            {
                if (!await Utility.SaveOfficialGuildListToRedisAsync())
                    return false;

                await Bot.RedisSub.PublishAsync(new RedisChannel(_reloadOfficialGuildListKey, RedisChannel.PatternMode.Literal), "");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "SaveAndBroadcastOfficialGuildList Error");
                return false;
            }

            return true;
        }

        internal IReadOnlyCollection<SocketGuild> GetNoNotifyGuilds()
        {
            var guilds = new List<SocketGuild>(_client.Guilds);
            using var db = _dbService.GetDbContext();

            db.NoticeYoutubeStreamChannel
                .AsEnumerable()
                .DistinctBy((x) => x.GuildId)
                .Select((x) => x.GuildId)
                .ToList()
                .ForEach((x) =>
                {
                    var guild = guilds.SingleOrDefault((x2) => x2.Id == x);
                    if (guild != null)
                        guilds.Remove(guild);
                });

            db.NoticeTwitchStreamChannels
                .AsEnumerable()
                .DistinctBy((x) => x.GuildId)
                .Select((x) => x.GuildId)
                .ToList()
                .ForEach((x) =>
                {
                    var guild = guilds.SingleOrDefault((x2) => x2.Id == x);
                    if (guild != null)
                        guilds.Remove(guild);
                });

            db.NoticeTwitcastingStreamChannels
                .AsEnumerable()
                .DistinctBy((x) => x.GuildId)
                .Select((x) => x.GuildId)
                .ToList()
                .ForEach((x) =>
                {
                    var guild = guilds.SingleOrDefault((x2) => x2.Id == x);
                    if (guild != null)
                        guilds.Remove(guild);
                });

            db.GuildYoutubeMemberConfig
                .AsEnumerable()
                .DistinctBy((x) => x.GuildId)
                .Select((x) => x.GuildId)
                .ToList()
                .ForEach((x) =>
                {
                    var guild = guilds.SingleOrDefault((x2) => x2.Id == x);
                    if (guild != null)
                        guilds.Remove(guild);
                });

            Utility.OfficialGuildList
                .ToList()
                .ForEach((x) =>
                {
                    var guild = guilds.SingleOrDefault((x2) => x2.Id == x);
                    if (guild != null)
                        guilds.Remove(guild);
                });

            guilds = guilds
                .OrderByDescending((x) => x.MemberCount)
                .ToList();

            return guilds.AsReadOnly();
        }
    }
}
