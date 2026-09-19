using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using DiscordStreamNotifyBot.Shared;
using StackExchange.Redis;

namespace DiscordStreamNotifyBot.SharedService.Youtube
{
    /// <summary>pending action 與其原始 JSON；原始字串用於 compare-and-set，避免覆蓋 Backend 剛寫入的狀態。</summary>
    public sealed class YoutubeWebSubPendingSnapshot
    {
        public YoutubeWebSubPendingAction Action { get; init; }
        public string RawJson { get; init; }
    }

    /// <summary>
    /// Redis DB 1 的 WebSub 共享狀態存取：pending action 與每頻道固定 HMAC secret。
    /// <para>
    /// 只做讀寫與 TTL 維護，不含 HTTP、決策或 Atom 邏輯；Backend 有另一份等價實作（計畫 §7.1）。
    /// pending 的更新一律以原始 JSON 做 compare-and-set，任何一側都不會用舊快照覆蓋新狀態。
    /// </para>
    /// </summary>
    public sealed class YoutubeWebSubState
    {
        private readonly IDatabase _database;

        public YoutubeWebSubState(IDatabase database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public int DatabaseNumber => _database.Database;

        /// <summary>
        /// 讀取 pending action 與原始 JSON；不存在時回傳 null。格式無法解析時 <see cref="YoutubeWebSubPendingSnapshot.Action"/>
        /// 為 null 但保留原始 JSON：決策上視為沒有 pending，CAS 仍以它為條件才可能取代損壞的資料。
        /// </summary>
        public async Task<YoutubeWebSubPendingSnapshot> GetPendingSnapshotAsync(string channelId)
        {
            RedisValue value = await _database.StringGetAsync(RedisChannels.YoutubeWebSub.PendingKey(channelId));
            if (!value.HasValue)
                return null;

            string raw = value.ToString();
            if (!YoutubeWebSubPendingAction.TryParse(raw, out var action, out var error))
            {
                Log.Warn($"YouTube WebSub pending 無法解析，將以新要求取代：{channelId}（{error}）");
                return new YoutubeWebSubPendingSnapshot { Action = null, RawJson = raw };
            }

            return new YoutubeWebSubPendingSnapshot { Action = action, RawJson = raw };
        }

        public async Task<YoutubeWebSubPendingAction> GetPendingAsync(string channelId)
            => (await GetPendingSnapshotAsync(channelId))?.Action;

        public Task<TimeSpan?> GetPendingTtlAsync(string channelId)
            => _database.KeyTimeToLiveAsync(RedisChannels.YoutubeWebSub.PendingKey(channelId));

        /// <summary>
        /// 只有 Redis 內的 pending 仍等於 <paramref name="expected"/> 時才寫入新值。
        /// CAS 失敗代表另一側（Backend 標記或本程序另一輪）已更新，呼叫端必須重讀後重新決策。
        /// </summary>
        public async Task<bool> TryWritePendingAsync(YoutubeWebSubPendingSnapshot expected, YoutubeWebSubPendingAction action, TimeSpan ttl)
        {
            RedisKey key = RedisChannels.YoutubeWebSub.PendingKey(action.ChannelId);
            var transaction = _database.CreateTransaction();
            if (expected == null)
                transaction.AddCondition(Condition.KeyNotExists(key));
            else
                transaction.AddCondition(Condition.StringEqual(key, expected.RawJson));

            _ = transaction.StringSetAsync(key, JsonConvert.SerializeObject(action), ttl);
            return await transaction.ExecuteAsync();
        }

        public async Task<string> GetSecretAsync(string channelId)
        {
            RedisValue value = await _database.StringGetAsync(RedisChannels.YoutubeWebSub.HmacSecretKey(channelId));
            return value.HasValue ? value.ToString() : null;
        }

        public Task<TimeSpan?> GetSecretTtlAsync(string channelId)
            => _database.KeyTimeToLiveAsync(RedisChannels.YoutubeWebSub.HmacSecretKey(channelId));

        /// <summary>
        /// 取得每頻道固定 HMAC secret；不存在時以 <c>SET NX</c> 建立後重讀實際值，避免併發輪替。
        /// 續訂一律重用同一份 secret，不因每次 request 旋轉；既有 secret 的 TTL 不在此處延長，
        /// 以免送出失敗（Hub 5xx 或 challenge 未完成）時讓續訂篩選誤判為已補送（計畫 §8.3）。
        /// </summary>
        public async Task<string> GetOrCreateSecretAsync(string channelId, TimeSpan newSecretTtl)
        {
            string existing = await GetSecretAsync(channelId);
            if (existing != null)
                return existing;

            string created = YoutubeWebSubContract.CreateSecret();
            if (await _database.StringSetAsync(RedisChannels.YoutubeWebSub.HmacSecretKey(channelId), created, newSecretTtl, When.NotExists))
                return created;

            string actual = await GetSecretAsync(channelId);
            if (actual == null)
                throw new InvalidOperationException($"無法建立或讀取 {channelId} 的 HMAC secret");

            return actual;
        }

        /// <summary>
        /// 取得同一頻道的跨程序送出鎖；Scraper 續訂與 Notifier 取消訂閱可能同時發生，
        /// 程序內 semaphore 擋不住。鎖會在 <paramref name="ttl"/> 後自動失效，崩潰不會永久卡住頻道。
        /// </summary>
        public async Task<string> TryAcquireRequestLockAsync(string channelId, TimeSpan ttl)
        {
            string owner = $"bot:{Environment.ProcessId}:{Guid.NewGuid():N}";
            return await _database.StringSetAsync(RedisChannels.YoutubeWebSub.InFlightKey(channelId), owner, ttl, When.NotExists)
                ? owner
                : null;
        }

        /// <summary>只刪除自己持有的鎖，避免延遲的舊要求刪掉接手者的鎖。</summary>
        public async Task ReleaseRequestLockAsync(string channelId, string owner)
        {
            RedisKey key = RedisChannels.YoutubeWebSub.InFlightKey(channelId);
            var transaction = _database.CreateTransaction();
            transaction.AddCondition(Condition.StringEqual(key, owner));
            _ = transaction.KeyDeleteAsync(key);
            await transaction.ExecuteAsync();
        }
    }

    /// <summary>
    /// YouTube WebSub 訂閱／取消訂閱要求的唯一入口：pending action 決策、每頻道固定 callback URL 與
    /// Hub 表單送出（計畫 §6.1、§8.1～§8.3）。
    /// <para>
    /// 僅表示 Hub 是否受理（任一 2xx 即 Accepted）；<c>LastSubscribeTime</c> 與 secret 實際 lease
    /// 一律等 Backend 收到 challenge 後才更新，因此本服務不碰 MySQL。
    /// </para>
    /// <para>
    /// <c>ponytail:</c> 防重分三層：程序內相同要求共用同一個 Task、程序內不同要求以 per-channel
    /// semaphore 排序、跨程序（Scraper 續訂 vs Notifier 取消訂閱）以 Redis 短 TTL 鎖互斥。
    /// 再往上就不需要分散式協調，pending 本身以 CAS 保證不會被舊快照覆蓋。
    /// </para>
    /// </summary>
    public sealed class YoutubeWebSubService
    {
        /// <summary>pending 的 compare-and-set 嘗試次數：對手只有 Backend 的 confirmed／denied 標記，重讀一次足夠。</summary>
        private const int PendingCasAttempts = 2;

        /// <summary>跨程序送出鎖的 TTL；大於 HttpClient 預設 100 秒 timeout，程序崩潰時最多讓該頻道延後兩分鐘。</summary>
        private static readonly TimeSpan RequestLockTtl = TimeSpan.FromMinutes(2);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiServerDomain;
        private readonly YoutubeWebSubState _state;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _channelGates = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, Task<YoutubeWebSubRequestResult>> _inFlight = new(StringComparer.Ordinal);
        private long _notBeforeUtcTicks;

        public YoutubeWebSubService(BotConfig botConfig, IHttpClientFactory httpClientFactory, YoutubeWebSubState state)
        {
            _apiServerDomain = botConfig.ApiServerDomain;
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>以 Bot 慣例建立服務：WebSub 共享狀態固定使用 <see cref="RedisChannels.YoutubeWebSub.DatabaseNumber"/>。</summary>
        public static YoutubeWebSubService Create(BotConfig botConfig, IHttpClientFactory httpClientFactory, IConnectionMultiplexer connection)
        {
            ArgumentNullException.ThrowIfNull(connection);
            return new YoutubeWebSubService(
                botConfig,
                httpClientFactory,
                new YoutubeWebSubState(connection.GetDatabase(RedisChannels.YoutubeWebSub.DatabaseNumber)));
        }

        /// <summary>
        /// 對 Google Hub 送出單一頻道的 subscribe／unsubscribe。
        /// <para>
        /// 同一頻道、同一種要求已有 in-flight 時直接等待該次結果，不重複送 Hub（定期續訂與 NeedRegister
        /// 同時發生時只送一個 request）；不同 mode 或強制重訂則以 per-channel semaphore 依序執行，不重疊。
        /// </para>
        /// </summary>
        /// <param name="force">owner 強制重新訂閱；可清除既有 denied 狀態。</param>
        /// <param name="renewDue">已確認的 pending 是否已到續訂時間（定期續訂為 true，NeedRegister 補送為 false）。</param>
        public async Task<YoutubeWebSubRequestResult> RequestAsync(
            string channelId,
            bool subscribe = true,
            bool force = false,
            bool renewDue = true,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
            cancellationToken.ThrowIfCancellationRequested();

            // renewDue 只影響 pending 決策、不影響送出的內容，因此不列入 in-flight 的識別。
            string requestKey = $"{channelId}|{subscribe}|{force}";
            while (true)
            {
                if (_inFlight.TryGetValue(requestKey, out Task<YoutubeWebSubRequestResult> running))
                    return await running.ConfigureAwait(false);

                var completion = new TaskCompletionSource<YoutubeWebSubRequestResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                if (!_inFlight.TryAdd(requestKey, completion.Task))
                    continue;

                try
                {
                    var result = await RequestGuardedAsync(channelId, subscribe, force, renewDue, cancellationToken).ConfigureAwait(false);
                    completion.SetResult(result);
                    return result;
                }
                catch
                {
                    // 等待同一次要求的呼叫端取得可處理的結果，避免未觀察的 Task 例外。
                    completion.SetResult(YoutubeWebSubRequestResult.Transient(null, null, "WebSub 要求失敗"));
                    throw;
                }
                finally
                {
                    _inFlight.TryRemove(requestKey, out _);
                }
            }
        }

        private async Task<YoutubeWebSubRequestResult> RequestGuardedAsync(
            string channelId, bool subscribe, bool force, bool renewDue, CancellationToken cancellationToken)
        {
            SemaphoreSlim gate = _channelGates.GetOrAdd(channelId, static _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                string lockOwner = await _state.TryAcquireRequestLockAsync(channelId, RequestLockTtl).ConfigureAwait(false);
                if (lockOwner == null)
                    return YoutubeWebSubRequestResult.Transient(null, RequestLockTtl, "同一頻道已有 in-flight 的 WebSub 要求");

                try
                {
                    return await RequestCoreAsync(channelId, subscribe, force, renewDue, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    await _state.ReleaseRequestLockAsync(channelId, lockOwner).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is RedisException or TimeoutException or InvalidOperationException)
            {
                Log.Error(ex.Demystify(), $"YouTube WebSub 要求失敗（狀態存取）：{channelId}");
                return YoutubeWebSubRequestResult.Transient(null, null, "WebSub 共享狀態存取失敗");
            }
            finally
            {
                gate.Release();
            }
        }

        private async Task<YoutubeWebSubRequestResult> RequestCoreAsync(
            string channelId, bool subscribe, bool force, bool renewDue, CancellationToken cancellationToken)
        {
            long notBefore = Volatile.Read(ref _notBeforeUtcTicks);
            if (DateTimeOffset.UtcNow.UtcTicks < notBefore)
            {
                var wait = TimeSpan.FromTicks(notBefore - DateTimeOffset.UtcNow.UtcTicks);
                return YoutubeWebSubRequestResult.Transient(null, wait, "Hub 要求的 Retry-After 尚未到期，本輪不重送");
            }

            string mode = subscribe ? YoutubeWebSubContract.ModeSubscribe : YoutubeWebSubContract.ModeUnsubscribe;

            // pending 由 Bot 與 Backend 共用：讀→決策→CAS 寫入；CAS 失敗代表另一側剛更新，
            // 重讀一次再決策（例如 Backend 已標記 confirmed／denied）。第二次仍失敗則留給下一輪。
            for (int attempt = 0; attempt < PendingCasAttempts; attempt++)
            {
                YoutubeWebSubPendingSnapshot existing = await _state.GetPendingSnapshotAsync(channelId).ConfigureAwait(false);
                string topic = YoutubeWebSubContract.CanonicalTopic(channelId);

                // 已被 Hub 拒絕且未強制重訂：連 state 都不動，不建立 secret、不延長任何 TTL。
                if (subscribe && existing?.Action?.DeniedAtUtc != null && !force)
                    return YoutubeWebSubRequestResult.Suppressed();

                string secret = subscribe
                    ? await _state.GetOrCreateSecretAsync(channelId, YoutubeWebSubContract.PendingTtl).ConfigureAwait(false)
                    : await _state.GetSecretAsync(channelId).ConfigureAwait(false);

                // unsubscribe 成功後 secret 已被刪除，該次重試只能靠 pending 內的 token；此時才需要隨機 token。
                string callbackToken = secret != null
                    ? YoutubeWebSubContract.DeriveCallbackToken(secret)
                    : existing?.Action?.CallbackToken ?? YoutubeWebSubContract.CreateSecret();

                var decision = YoutubeWebSubPendingPolicy.Decide(
                    existing?.Action, mode, topic, callbackToken, renewDue: subscribe && renewDue, force: force);
                if (decision == YoutubeWebSubPendingDecision.Blocked)
                    return YoutubeWebSubRequestResult.Suppressed();

                TimeSpan pendingTtl = YoutubeWebSubContract.PendingTtl;
                YoutubeWebSubPendingAction action = existing?.Action;
                if (decision == YoutubeWebSubPendingDecision.Replace)
                {
                    action = YoutubeWebSubPendingAction.Create(channelId, mode, callbackToken, DateTime.UtcNow);
                }
                else
                {
                    TimeSpan? remaining = await _state.GetPendingTtlAsync(channelId).ConfigureAwait(false);
                    if (remaining > pendingTtl)
                        pendingTtl = remaining.Value;
                }

                if (!await _state.TryWritePendingAsync(existing, action, pendingTtl).ConfigureAwait(false))
                {
                    Log.Warn($"YouTube WebSub pending 同時被更新，重讀後重試：{channelId}");
                    continue;
                }

                string callbackUrl = YoutubeWebSubContract.CallbackUrl(_apiServerDomain, channelId, callbackToken);
                IReadOnlyList<KeyValuePair<string, string>> form =
                    YoutubeWebSubContract.BuildRequestForm(mode, topic, callbackUrl, secret);

                return await SendAsync(channelId, mode, form, secret, callbackToken, cancellationToken).ConfigureAwait(false);
            }

            return YoutubeWebSubRequestResult.Transient(null, null, "WebSub pending 併發更新，本輪不重送");
        }

        /// <summary>
        /// 續訂篩選（計畫 §8.3）：lease 到期、secret 缺失／TTL 已低於緩衝，或先前的送出尚未被 challenge 確認
        /// （Hub 5xx、challenge 未到達）都要重試。只看 secret TTL 會讓失敗的提早續訂被略過數天。
        /// </summary>
        public async Task<bool> IsRenewalDueAsync(string channelId, DateTime lastSubscribeTime, DateTime now, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
            cancellationToken.ThrowIfCancellationRequested();

            if (lastSubscribeTime < now.Subtract(YoutubeWebSubContract.RenewAfter))
                return true;

            TimeSpan? ttl = await _state.GetSecretTtlAsync(channelId).ConfigureAwait(false);
            if (ttl == null || ttl <= YoutubeWebSubContract.SecretRenewThreshold)
                return true;

            YoutubeWebSubPendingAction pending = await _state.GetPendingAsync(channelId).ConfigureAwait(false);
            return pending?.Mode == YoutubeWebSubContract.ModeSubscribe
                && pending.ConfirmedAtUtc == null
                && pending.DeniedAtUtc == null;
        }

        private async Task<YoutubeWebSubRequestResult> SendAsync(
            string channelId, string mode, IReadOnlyList<KeyValuePair<string, string>> form,
            string secret, string callbackToken, CancellationToken cancellationToken)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, YoutubeWebSubContract.HubSubscribeEndpoint)
                {
                    Content = new FormUrlEncodedContent(form)
                };
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

                YoutubeWebSubRequestOutcome outcome = YoutubeWebSubContract.ClassifyStatus(response.StatusCode);
                if (outcome == YoutubeWebSubRequestOutcome.Accepted)
                    return YoutubeWebSubRequestResult.Accepted(response.StatusCode);

                TimeSpan? retryAfter = ParseRetryAfter(response.Headers.RetryAfter);
                if (retryAfter.HasValue)
                {
                    // 計畫 §8.2：只要 Hub 給了有效 Retry-After 就必須遵守，永久失敗也一樣。
                    long notBefore = DateTimeOffset.UtcNow.Add(retryAfter.Value).UtcTicks;
                    Interlocked.Exchange(ref _notBeforeUtcTicks, notBefore);
                    Log.Warn($"YouTube WebSub {mode} 收到 Retry-After：{channelId} / HTTP {(int)response.StatusCode} / {retryAfter.Value.TotalSeconds:0} 秒");
                }

                string summary = "";
                try
                {
                    summary = YoutubeWebSubContract.Summarize(
                        await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false),
                        sensitiveValues: [secret, callbackToken]);
                }
                catch (Exception ex) { summary = $"讀取回應內容失敗：{ex.GetType().Name}"; }

                if (outcome == YoutubeWebSubRequestOutcome.PermanentFailure)
                {
                    Log.Error($"YouTube WebSub {mode} 被 Hub 拒絕：{channelId} / HTTP {(int)response.StatusCode} / {summary}");
                    return YoutubeWebSubRequestResult.Permanent(response.StatusCode, summary);
                }

                if (retryAfter == null)
                    Log.Warn($"YouTube WebSub {mode} 暫時失敗：{channelId} / HTTP {(int)response.StatusCode} / {summary}");

                return YoutubeWebSubRequestResult.Transient(response.StatusCode, retryAfter, summary);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"YouTube WebSub {mode} 送出失敗：{channelId}");
                return YoutubeWebSubRequestResult.Transient(null, null, ex.GetType().Name);
            }
        }

        private static TimeSpan? ParseRetryAfter(RetryConditionHeaderValue retryAfter)
        {
            if (retryAfter == null)
                return null;

            if (retryAfter.Delta.HasValue)
                return retryAfter.Delta.Value > TimeSpan.Zero ? retryAfter.Delta.Value : null;

            if (retryAfter.Date.HasValue)
            {
                TimeSpan delta = retryAfter.Date.Value - DateTimeOffset.UtcNow;
                return delta > TimeSpan.Zero ? delta : null;
            }

            return null;
        }
    }
}
