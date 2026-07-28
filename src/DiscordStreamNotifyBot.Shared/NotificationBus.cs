namespace DiscordStreamNotifyBot.Shared
{
    /// <summary>
    /// 內部通知匯流排（Scraper → Notifier）的 Redis Streams 封裝（計畫 §4）。
    /// <para>
    /// 單一 stream <c>bot:notify</c>；每個 shard 一個 consumer group <c>shard-{id}</c>（各 group 都收到全部訊息＝廣播 + 各自過濾）；
    /// 每個 group 固定一個 consumer <c>notifier-{id}</c>（重啟後同名接手自己的 PEL）。at-least-once，發送成功後 XACK。
    /// </para>
    /// <para>
    /// StackExchange.Redis 內建（<c>StreamAdd</c> / <c>StreamReadGroup</c> / <c>StreamAcknowledge</c> / <c>StreamAutoClaim</c>），零新套件。
    /// StackExchange.Redis 不支援 blocking read → 消費端用短輪詢（§4.3），不要嘗試 BLOCK。
    /// </para>
    /// </summary>
    public static class NotificationBus
    {
        /// <summary>匯流排 stream 鍵。</summary>
        public const string StreamKey = "bot:notify";

        /// <summary>訊息欄位名：訊息類型（<see cref="Messages.NotifyType"/>）。</summary>
        public const string FieldType = "type";

        /// <summary>訊息欄位名：DTO 的 JSON。</summary>
        public const string FieldPayload = "payload";

        /// <summary>XADD 修剪上限（近似），防無人消費時無限堆積（§4.1，正常量遠低於此）。</summary>
        public const int MaxApproxLength = 10000;

        /// <summary>shard 的 consumer group 名稱。</summary>
        public static string GroupName(int shardId) => $"shard-{shardId}";

        /// <summary>shard 的 consumer 名稱（重啟後同名接手 pending）。</summary>
        public static string ConsumerName(int shardId) => $"notifier-{shardId}";

        /// <summary>
        /// 發佈一則通知（Scraper 偵測端）。payload 以 Newtonsoft 序列化為 JSON。
        /// XADD 帶近似 MAXLEN 修剪。回傳訊息 Id。
        /// </summary>
        public static Task<RedisValue> PublishAsync(IDatabase db, string type, object payload)
        {
            var fields = new NameValueEntry[]
            {
                new(FieldType, type),
                new(FieldPayload, JsonConvert.SerializeObject(payload)),
            };

            return db.StreamAddAsync(StreamKey, fields, messageId: null,
                maxLength: MaxApproxLength, useApproximateMaxLength: true);
        }

        /// <summary>以 Redis Lua 原子完成去重檢查、XADD 與 marker 寫入，供不可重播的來源安全重試。</summary>
        public static async Task<RedisValue> PublishOnceAsync(
            IDatabase db,
            RedisKey dedupKey,
            TimeSpan dedupExpiry,
            string type,
            object payload)
        {
            const string script = """
                local existing = redis.call('GET', KEYS[2])
                if existing then
                    return existing
                end
                local id = redis.call('XADD', KEYS[1], 'MAXLEN', '~', ARGV[1], '*', ARGV[2], ARGV[3], ARGV[4], ARGV[5])
                redis.call('SET', KEYS[2], id, 'EX', ARGV[6])
                return id
                """;
            var result = await db.ScriptEvaluateAsync(
                script,
                [StreamKey, dedupKey],
                [
                    MaxApproxLength,
                    FieldType,
                    type,
                    FieldPayload,
                    JsonConvert.SerializeObject(payload),
                    Math.Max(1, (long)Math.Ceiling(dedupExpiry.TotalSeconds)),
                ]).ConfigureAwait(false);
            return (string)result;
        }

        /// <summary>
        /// 建立本 shard 的 consumer group（§4.4）：從 <c>0</c> 建群（首次部署不漏既有訊息），
        /// 並以 <c>MKSTREAM</c> 建 stream；已存在（BUSYGROUP）視為成功。歷史重播由消費端去重鍵吸收。
        /// </summary>
        public static async Task EnsureConsumerGroupAsync(IDatabase db, int shardId)
        {
            try
            {
                await db.StreamCreateConsumerGroupAsync(StreamKey, GroupName(shardId),
                    position: StreamPosition.Beginning, createStream: true);
            }
            catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
            {
                // group 已存在，視為成功
            }
        }

        /// <summary>讀取本 group 尚未投遞的新訊息（position <c>&gt;</c>）。空陣列代表暫無新訊息（呼叫端短輪詢後重讀）。</summary>
        public static Task<StreamEntry[]> ReadNewAsync(IDatabase db, int shardId, int count)
            => db.StreamReadGroupAsync(StreamKey, GroupName(shardId), ConsumerName(shardId),
                position: StreamPosition.NewMessages, count: count);

        /// <summary>認可（XACK）已處理完成的訊息。</summary>
        public static Task<long> AckAsync(IDatabase db, int shardId, params RedisValue[] messageIds)
            => db.StreamAcknowledgeAsync(StreamKey, GroupName(shardId), messageIds);

        /// <summary>
        /// 認領本 consumer PEL 中閒置逾 <paramref name="minIdle"/> 的訊息重新處理（崩潰恢復，§4.3）。
        /// 回傳被認領的訊息，供呼叫端重跑處理迴圈。
        /// </summary>
        public static async Task<StreamEntry[]> AutoClaimAsync(IDatabase db, int shardId, TimeSpan minIdle, int count)
        {
            var result = await AutoClaimPageAsync(db, shardId, minIdle, "0-0", count);
            return result.ClaimedEntries;
        }

        /// <summary>
        /// 從指定 cursor 認領一頁 pending 訊息；呼叫端必須保存 <see cref="StreamAutoClaimResult.NextStartId" />，
        /// 避免持續失敗的前段訊息讓後段 PEL 永遠無法被掃到。
        /// </summary>
        public static Task<StreamAutoClaimResult> AutoClaimPageAsync(
            IDatabase db,
            int shardId,
            TimeSpan minIdle,
            RedisValue startId,
            int count)
            => db.StreamAutoClaimAsync(
                StreamKey,
                GroupName(shardId),
                ConsumerName(shardId),
                (long)minIdle.TotalMilliseconds,
                startId,
                count);

        /// <summary>
        /// 取得 stream 各 consumer group 的狀態（pending 數 / consumer 數）供 Coordinator 監控（§4.4 / §9.3）。
        /// stream 尚未建立時回傳空陣列。
        /// </summary>
        public static async Task<StreamGroupInfo[]> GetGroupsAsync(IDatabase db)
        {
            try
            {
                return await db.StreamGroupInfoAsync(StreamKey);
            }
            catch (RedisServerException ex) when (ex.Message.Contains("no such key"))
            {
                return Array.Empty<StreamGroupInfo>();
            }
        }

        /// <summary>從 stream 訊息取出 <c>type</c> 與 <c>payload</c>；欄位缺失時回傳 false。</summary>
        public static bool TryGetPayload(StreamEntry entry, out string type, out string payload)
        {
            type = entry[FieldType];
            payload = entry[FieldPayload];
            return !string.IsNullOrEmpty(type) && !string.IsNullOrEmpty(payload);
        }
    }
}
