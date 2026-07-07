namespace DiscordStreamNotifyBot.Shared
{
    /// <summary>
    /// 集中管理 Redis Pub/Sub 頻道與控制平面鍵字串。
    /// <para>
    /// 錄影 IPC 頻道（<see cref="Youtube"/> / <see cref="Twitch"/> / <see cref="Twitcasting"/> / <see cref="Member"/>）
    /// 為與外部錄影工具 <c>YoutubeStreamRecord</c> 的既有契約，<b>不可單方面更改字串</b>。
    /// </para>
    /// <para>
    /// 控制平面鍵（<see cref="Cluster"/>）為水平擴展新增（心跳 / leader 鎖 / shard 租約），詳見計畫 §4.2。
    /// </para>
    /// </summary>
    public static class RedisChannels
    {
        /// <summary>YouTube 錄影 IPC 頻道（與錄影工具共用契約）。</summary>
        public static class Youtube
        {
            public const string StartStream = "youtube.startstream";
            public const string EndStream = "youtube.endstream";
            public const string AddStream = "youtube.addstream";
            public const string DeleteStream = "youtube.deletestream";
            public const string Unarchived = "youtube.unarchived";
            public const string MemberOnly = "youtube.memberonly";
            public const string Record = "youtube.record";
            public const string Error429 = "youtube.429error";
            public const string Test = "youtube.test";

            public const string NewStream = "youtube.newstream";
            public const string ChangeStreamTime = "youtube.changestreamtime";
            public const string OtherStart = "youtube.otherStart";

            // PubSubHubbub（由後端轉發的 webhook）
            public const string PubSubCreateOrUpdate = "youtube.pubsub.CreateOrUpdate";
            public const string PubSubDeleted = "youtube.pubsub.Deleted";
            public const string PubSubNeedRegister = "youtube.pubsub.NeedRegister";

            /// <summary>彩虹社成員頻道（<c>{affiliation}</c> 為所屬團體）。</summary>
            public const string NijisanjiLiverTemplate = "youtube.nijisanji.liver.{affiliation}";
        }

        /// <summary>Twitch IPC 頻道與設定鍵（與錄影工具 / 後端共用契約）。</summary>
        public static class Twitch
        {
            public const string Record = "twitch.record";
            public const string ChannelUpdate = "twitch:channel_update";
            public const string StreamOffline = "twitch:stream_offline";
            public const string WebhookSecret = "twitch:webhook_secret";
        }

        /// <summary>TwitCasting IPC 頻道（與後端共用契約）。</summary>
        public static class Twitcasting
        {
            public const string PubSubStartLive = "twitcasting.pubsub.startlive";
        }

        /// <summary>YouTube 會限 OAuth Token IPC 頻道（與後端共用契約）。</summary>
        public static class Member
        {
            public const string RevokeToken = "member.revokeToken";
            public const string SyncRedisToken = "member.syncRedisToken";
        }

        /// <summary>跨 shard 共享狀態鍵（計畫階段 5）。</summary>
        public static class SharedState
        {
            /// <summary>官方伺服器白名單（Redis SET，成員為 guildId）。取代原 OfficialList.json 檔案同步。</summary>
            public const string OfficialGuildList = "DiscordStreamBot:OfficialGuildList";

            /// <summary>各 shard 伺服器數（HASH，field = shardId）。狀態列彙總顯示用。</summary>
            public const string GuildCountHash = "cluster:stats:guild_count";

            /// <summary>各 shard 服務成員數（HASH，field = shardId）。狀態列彙總顯示用。</summary>
            public const string MemberCountHash = "cluster:stats:member_count";

            /// <summary>各 shard 持有的伺服器快照（HASH，field = shardId，value = JSON guild 清單）。跨 shard 讀取彙總用。</summary>
            public const string GuildSnapshotHash = "cluster:stats:guild_snapshot";
        }

        /// <summary>Notifier 控制平面頻道（指令觸發，廣播至所有 shard）。</summary>
        public static class Notifier
        {
            /// <summary>關閉所有 Notifier shard（die 指令廣播，各 shard 收到後設 <c>Bot.IsDisconnect = true</c>）。</summary>
            public const string Shutdown = "notifier.control.shutdown";

            /// <summary>離開指定伺服器（payload = guildId；僅持有該伺服器的 shard 會實際離開）。</summary>
            public const string LeaveGuild = "notifier.control.leaveGuild";

            /// <summary>離開未設定通知的伺服器（payload = correlationId；各 shard 離開自己的並回報數量）。</summary>
            public const string LeaveNoNotifyGuild = "notifier.control.leaveNoNotify";

            /// <summary>全球訊息發送（payload = JSON SendAllPayload；各 shard 對自己持有的伺服器發送）。</summary>
            public const string SendMessageToAll = "notifier.control.sendMessageToAll";
        }

        /// <summary>叢集控制平面鍵（水平擴展新增，詳見計畫 §4.2）。</summary>
        public static class Cluster
        {
            /// <summary>scraper leader 鎖（SET NX EX）。</summary>
            public const string ScraperLeader = "cluster:scraper:leader";

            /// <summary>TOTAL_SHARDS 公告（叢集真實來源）。</summary>
            public const string TotalShards = "cluster:total_shards";

            /// <summary>各程序心跳鍵：<c>cluster:heartbeat:{role}:{id}</c>。</summary>
            public static string Heartbeat(string role, string id) => $"cluster:heartbeat:{role}:{id}";

            /// <summary>notifier shard 租約鍵：<c>cluster:shard:lease:{shardId}</c>。</summary>
            public static string ShardLease(int shardId) => $"cluster:shard:lease:{shardId}";

            /// <summary>跨 shard 查詢請求頻道（scatter-gather；payload 內含 correlationId）。</summary>
            public const string QueryRequest = "cluster:query:request";

            /// <summary>跨 shard 查詢回應頻道：<c>cluster:query:reply:{correlationId}</c>（請求端依 correlationId 訂閱收集）。</summary>
            public static string QueryReply(string correlationId) => $"cluster:query:reply:{correlationId}";
        }
    }
}
