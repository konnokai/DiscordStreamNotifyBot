using DiscordStreamNotifyBot.Command;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Shared;
using System.Collections.Concurrent;

namespace DiscordStreamNotifyBot.SharedService.Cluster
{
    /// <summary>
    /// 跨 shard 指令支援服務（指令系統共用，反射自動註冊為 Singleton）。
    /// <para>
    /// 因為每個 Notifier shard 是獨立程序、只持有自己負責的伺服器，且 Discord 把所有 DM 路由到 shard 0，
    /// 故 owner/admin 指令若直接讀 <c>_client.Guilds</c> 只看得到單一 shard。本服務提供兩種 Redis 機制：
    /// </para>
    /// <list type="bullet">
    /// <item><b>B1 共享快照</b>：各 shard 把自己持有的伺服器寫入 Redis HASH，請求端一次讀回合併（純讀取、無等待）。</item>
    /// <item><b>B2 request/reply</b>：少數需即時打到持有/在線 shard 的查詢（UserInfo / GuildInfo / GetInviteUrl），
    /// 以 correlationId 散播請求、收集回應，逾時即用部分結果。</item>
    /// </list>
    /// </summary>
    public class ClusterQueryService : ICommandService
    {
        /// <summary>跨 shard 查詢類型。</summary>
        public enum ClusterQueryType { UserInfo, GuildInfo, GetInviteUrl }

        /// <summary>單一伺服器的快照（B1，寫入 <see cref="RedisChannels.SharedState.GuildSnapshotHash"/>）。</summary>
        public class GuildSnapshot
        {
            public ulong Id { get; set; }
            public string Name { get; set; }
            public ulong OwnerId { get; set; }
            public int MemberCount { get; set; }
        }

        private class QueryRequest
        {
            public string CorrelationId { get; set; }
            public ClusterQueryType QueryType { get; set; }
            public string Arg { get; set; }
        }

        /// <summary><see cref="ClusterQueryType.UserInfo"/> 回應：本 shard 與該使用者的共同伺服器。</summary>
        public class UserInfoResponse
        {
            public int ShardId { get; set; }
            public List<string> GuildNames { get; set; } = new();
        }

        /// <summary><see cref="ClusterQueryType.GuildInfo"/> 回應：持有 shard 的即時伺服器資訊。</summary>
        public class GuildInfoResponse
        {
            public int ShardId { get; set; }
            public string Name { get; set; }
            public ulong OwnerId { get; set; }
            public int MemberCount { get; set; }
            /// <summary>頻道 Id → 名稱（供請求端把 DB 內的頻道 Id 解析成名稱）。</summary>
            public Dictionary<ulong, string> Channels { get; set; } = new();
        }

        /// <summary><see cref="ClusterQueryType.GetInviteUrl"/> 回應：持有 shard 建立的邀請或文字頻道清單。</summary>
        public class InviteResponse
        {
            public int ShardId { get; set; }
            /// <summary>有指定頻道時的邀請連結；<c>__NOPERM__</c> 代表缺少權限。</summary>
            public string InviteUrl { get; set; }
            /// <summary>未指定頻道時回傳的文字頻道清單（供使用者挑選）。</summary>
            public List<ChannelInfo> TextChannels { get; set; }
        }

        public class ChannelInfo
        {
            public ulong Id { get; set; }
            public string Name { get; set; }
        }

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2.5);

        private readonly DiscordSocketClient _client;
        private readonly MainDbService _dbService;

        public ClusterQueryService(DiscordSocketClient client, MainDbService dbService)
        {
            _client = client;
            _dbService = dbService;

            // 每個 shard 都訂閱查詢請求，計算本 shard 結果後回 publish 至對應的 reply 頻道
            Bot.RedisSub.Subscribe(new RedisChannel(RedisChannels.Cluster.QueryRequest, RedisChannel.PatternMode.Literal), (channel, value) =>
            {
                _ = HandleRequestAsync(value);
            });
        }

        #region B1：共享快照（純讀取）

        /// <summary>將本 shard 持有的伺服器快照寫入 Redis HASH（由 Bot 在 Ready / Joined / Left / 週期 timer 呼叫）。</summary>
        public static async Task WriteGuildSnapshotAsync(DiscordSocketClient client)
        {
            try
            {
                var list = client.Guilds
                    .Select((g) => new GuildSnapshot { Id = g.Id, Name = g.Name, OwnerId = g.OwnerId, MemberCount = g.MemberCount })
                    .ToList();

                await Bot.RedisDb.HashSetAsync(RedisChannels.SharedState.GuildSnapshotHash, Bot.ShardId, JsonConvert.SerializeObject(list));
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "寫入 guild 快照失敗");
            }
        }

        /// <summary>
        /// 取得全叢集伺服器清單：本 shard 用即時 <c>_client.Guilds</c>（最準），其餘 shard 讀 Redis 快照；
        /// 僅採 <c>shardId &lt; TotalShardCount</c> 的欄位，避免縮容殘留干擾。
        /// </summary>
        public async Task<List<GuildSnapshot>> ReadMergedGuildsAsync()
        {
            var result = _client.Guilds
                .Select((g) => new GuildSnapshot { Id = g.Id, Name = g.Name, OwnerId = g.OwnerId, MemberCount = g.MemberCount })
                .ToList();

            if (Bot.TotalShardCount <= 1)
                return result;

            try
            {
                int total = GetExpectedShardCount();
                foreach (var entry in await Bot.RedisDb.HashGetAllAsync(RedisChannels.SharedState.GuildSnapshotHash))
                {
                    if (!int.TryParse(entry.Name, out int sid) || sid == Bot.ShardId || sid >= total)
                        continue; // 本 shard 用即時資料；跳過縮容殘留

                    var list = JsonConvert.DeserializeObject<List<GuildSnapshot>>(entry.Value);
                    if (list != null)
                        result.AddRange(list);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "讀取 guild 快照失敗");
            }

            return result;
        }

        /// <summary>從合併後的全叢集清單篩出「未設定任何通知/會限、且非官方白名單」的伺服器（依人數遞減）。</summary>
        public List<GuildSnapshot> FilterNoNotifyGuilds(List<GuildSnapshot> guilds)
        {
            var configured = new HashSet<ulong>();
            using (var db = _dbService.GetDbContext())
            {
                foreach (var id in db.NoticeYoutubeStreamChannel.AsNoTracking().Select((x) => x.GuildId).Distinct())
                    configured.Add(id);
                foreach (var id in db.NoticeTwitchStreamChannels.AsNoTracking().Select((x) => x.GuildId).Distinct())
                    configured.Add(id);
                foreach (var id in db.NoticeTwitcastingStreamChannels.AsNoTracking().Select((x) => x.GuildId).Distinct())
                    configured.Add(id);
                foreach (var id in db.GuildYoutubeMemberConfig.AsNoTracking().Select((x) => x.GuildId).Distinct())
                    configured.Add(id);
            }

            foreach (var id in Utility.OfficialGuildList)
                configured.Add(id);

            return guilds
                .Where((g) => !configured.Contains(g.Id))
                .OrderByDescending((g) => g.MemberCount)
                .ToList();
        }

        #endregion

        #region B2：request / reply（即時動作）

        /// <summary>
        /// 向所有 shard 散播查詢，收集回應（逾時即回傳部分結果）。
        /// <para>單 shard 直接本機計算、跳過 round-trip。</para>
        /// </summary>
        public async Task<(List<T> Responses, int Responded, int Expected)> RequestAsync<T>(ClusterQueryType type, string arg, TimeSpan? timeout = null) where T : class
        {
            int expected = GetExpectedShardCount();

            // 單 shard 最佳化：本機計算即可
            if (expected <= 1)
            {
                var local = new List<T>();
                string json = await BuildResponseJsonAsync(type, arg);
                if (json != null)
                {
                    var obj = JsonConvert.DeserializeObject<T>(json);
                    if (obj != null)
                        local.Add(obj);
                }
                return (local, local.Count, 1);
            }

            string correlationId = Guid.NewGuid().ToString("N");
            var (rawReplies, responded, exp) = await CollectAsync(correlationId, expected, () =>
                Bot.RedisSub.PublishAsync(
                    new RedisChannel(RedisChannels.Cluster.QueryRequest, RedisChannel.PatternMode.Literal),
                    JsonConvert.SerializeObject(new QueryRequest { CorrelationId = correlationId, QueryType = type, Arg = arg })),
                timeout);

            var responses = rawReplies
                .Select((x) => JsonConvert.DeserializeObject<T>(x))
                .Where((x) => x != null)
                .ToList();

            return (responses, responded, exp);
        }

        /// <summary>
        /// 通用的「散播 + 依 correlationId 收集回應」機制：先訂閱 reply 頻道，再執行 <paramref name="publishRequest"/>，
        /// 等到所有 shard 回應或逾時。回應端須在 payload 內帶 <c>ShardId</c> 供去重與計數。
        /// </summary>
        public async Task<(List<string> Replies, int Responded, int Expected)> CollectAsync(
            string correlationId, int? expected, Func<Task> publishRequest, TimeSpan? timeout = null)
        {
            int expectedShards = expected ?? GetExpectedShardCount();
            var replyChannel = new RedisChannel(RedisChannels.Cluster.QueryReply(correlationId), RedisChannel.PatternMode.Literal);
            var replies = new ConcurrentBag<string>();
            var respondedShards = new ConcurrentDictionary<int, byte>();

            await Bot.RedisSub.SubscribeAsync(replyChannel, (_, value) =>
            {
                try
                {
                    int sid = ExtractShardId(value);
                    if (respondedShards.TryAdd(sid, 0))
                        replies.Add(value);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), "ClusterQuery 回應解析失敗");
                }
            });

            try
            {
                await publishRequest();

                var deadline = timeout ?? DefaultTimeout;
                var sw = Stopwatch.StartNew();
                while (sw.Elapsed < deadline && respondedShards.Count < expectedShards)
                    await Task.Delay(50);
            }
            finally
            {
                await Bot.RedisSub.UnsubscribeAsync(replyChannel);
            }

            return (replies.ToList(), respondedShards.Count, expectedShards);
        }

        private async Task HandleRequestAsync(string raw)
        {
            try
            {
                var req = JsonConvert.DeserializeObject<QueryRequest>(raw);
                if (req == null)
                    return;

                string replyJson = await BuildResponseJsonAsync(req.QueryType, req.Arg);
                if (replyJson == null)
                    return; // 本 shard 無相關資料（如非持有者），不回應以免雜訊

                await Bot.RedisSub.PublishAsync(
                    new RedisChannel(RedisChannels.Cluster.QueryReply(req.CorrelationId), RedisChannel.PatternMode.Literal),
                    replyJson);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "ClusterQuery 處理請求失敗");
            }
        }

        /// <summary>計算本 shard 對某查詢的回應 JSON；回傳 null 表示本 shard 不回應。</summary>
        private async Task<string> BuildResponseJsonAsync(ClusterQueryType type, string arg)
        {
            switch (type)
            {
                case ClusterQueryType.UserInfo:
                    {
                        if (!ulong.TryParse(arg, out ulong uid))
                            return null;

                        var resp = new UserInfoResponse { ShardId = Bot.ShardId };
                        foreach (var g in _client.Guilds)
                        {
                            if (g.GetUser(uid) != null)
                                resp.GuildNames.Add($"{g.Name} ({g.Id})");
                        }

                        // 即使為空也回應，讓請求端能正確統計「N/M shard 已回應」
                        return JsonConvert.SerializeObject(resp);
                    }

                case ClusterQueryType.GuildInfo:
                    {
                        if (!ulong.TryParse(arg, out ulong gid))
                            return null;

                        var g = _client.GetGuild(gid);
                        if (g == null)
                            return null; // 非持有 shard 不回應

                        var resp = new GuildInfoResponse
                        {
                            ShardId = Bot.ShardId,
                            Name = g.Name,
                            OwnerId = g.OwnerId,
                            MemberCount = g.MemberCount
                        };
                        foreach (var ch in g.Channels)
                            resp.Channels[ch.Id] = ch.Name;

                        return JsonConvert.SerializeObject(resp);
                    }

                case ClusterQueryType.GetInviteUrl:
                    {
                        // arg = "gid" 或 "gid:cid"
                        var parts = arg.Split(':');
                        if (!ulong.TryParse(parts[0], out ulong gid))
                            return null;

                        var g = _client.GetGuild(gid);
                        if (g == null)
                            return null; // 非持有 shard 不回應

                        ulong cid = parts.Length > 1 && ulong.TryParse(parts[1], out ulong c) ? c : 0;
                        var resp = new InviteResponse { ShardId = Bot.ShardId };

                        if (cid == 0)
                        {
                            // 忽略 ticket- & closed- 開頭的頻道（與原 GetInviteURL 行為一致）
                            resp.TextChannels = g.TextChannels
                                .Where((x) => !x.Name.StartsWith("ticket-") && !x.Name.StartsWith("closed-"))
                                .Select((x) => new ChannelInfo { Id = x.Id, Name = x.Name })
                                .ToList();
                        }
                        else
                        {
                            try
                            {
                                var tc = g.GetTextChannel(cid);
                                if (tc != null)
                                {
                                    var invite = await tc.CreateInviteAsync(300, 1, false);
                                    resp.InviteUrl = invite.Url;
                                }
                            }
                            catch (Discord.Net.HttpException httpEx) when (
                                httpEx.DiscordCode == DiscordErrorCode.InsufficientPermissions ||
                                httpEx.DiscordCode == DiscordErrorCode.MissingPermissions)
                            {
                                resp.InviteUrl = "__NOPERM__";
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.Demystify(), $"建立邀請失敗: {gid}/{cid}");
                            }
                        }

                        return JsonConvert.SerializeObject(resp);
                    }
            }

            return null;
        }

        #endregion

        /// <summary>叢集 shard 總數：以 Coordinator 公告的 <see cref="RedisChannels.Cluster.TotalShards"/> 為準，fallback 本機。</summary>
        private static int GetExpectedShardCount()
        {
            try
            {
                var val = Bot.RedisDb.StringGet(RedisChannels.Cluster.TotalShards);
                if (val.HasValue && int.TryParse(val, out int n) && n > 0)
                    return n;
            }
            catch { }

            return Math.Max(1, Bot.TotalShardCount);
        }

        /// <summary>從回應 JSON 取出 <c>ShardId</c>（供去重/計數）。</summary>
        private static int ExtractShardId(string json)
        {
            try
            {
                return Newtonsoft.Json.Linq.JObject.Parse(json).Value<int>("ShardId");
            }
            catch
            {
                return -1;
            }
        }
    }
}
