using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.HttpClients.Chzzk;
using DiscordStreamNotifyBot.HttpClients.Chzzk.Model;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.Chzzk;
using System.Collections.Concurrent;
using Bot = DiscordStreamNotifyBot.Shared.BotState;

namespace DiscordStreamNotifyBot.Scraper.Detection.Chzzk
{
    /// <summary>
    /// CHZZK 偵測服務（Scraper 專用）：以網站匿名 live-status endpoint 輪詢已追蹤頻道，
    /// 偵測開台／關台後 publish <see cref="ChzzkNotification"/> 至通知匯流排。
    /// <para>
    /// 不帶任何憑證、不做全站輪詢 fallback；同一頻道即使被多個 guild 訂閱也只查一次（<see cref="ChzzkSpider"/> 每個頻道一筆）。
    /// 狀態轉換以 DB（<see cref="ChzzkSpider"/>＋<see cref="ChzzkStream"/>）保存，重啟後可直接恢復。
    /// </para>
    /// </summary>
    public class ChzzkDetectionService
    {
        /// <summary>輪詢間隔；使用者已決定 30 秒，直接寫死為常數。</summary>
        internal static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

        private readonly ChzzkClient _client;
        private readonly MainDbService _dbService;
        private readonly ScraperMetrics _metrics;
        // 無法取得狀態的頻道只警告一次，避免每輪洗版；恢復正常後移除。
        private readonly ConcurrentDictionary<string, byte> _unavailableChannels = new(StringComparer.Ordinal);
        // 429 的 Retry-After 生效期間跳過輪詢。
        private DateTime _retryAfterUtc = DateTime.MinValue;

        public ChzzkDetectionService(ChzzkClient client, MainDbService dbService, ScraperMetrics metrics)
        {
            _client = client;
            _dbService = dbService;
            _metrics = metrics;

            PeriodicRunner.RunAsync("Chzzk-live-status-poll", TimeSpan.FromSeconds(5), PollInterval,
                PollAllAsync, GracefulShutdown.Token);
        }

        /// <summary>輪詢所有已追蹤頻道；單一頻道失敗不影響其他頻道，也不改動任何狀態。</summary>
        private async Task PollAllAsync()
        {
            if (DateTime.UtcNow < _retryAfterUtc)
                return;

            bool success = true;
            try
            {
                List<ChzzkSpider> spiders;
                using (var db = _dbService.GetDbContext())
                    spiders = await db.ChzzkSpider.AsNoTracking().ToListAsync();
                _metrics.SetChzzkSpiderCount(spiders.Count);

                foreach (var spider in spiders)
                {
                    if (GracefulShutdown.Token.IsCancellationRequested)
                        return;

                    var result = await _client.GetLiveStatusAsync(spider.ChannelId, GracefulShutdown.Token);
                    if (result.RetryAfter is { } retryAfter)
                    {
                        _retryAfterUtc = DateTime.UtcNow + retryAfter;
                        Log.Warn($"CHZZK 已達請求限制，暫停輪詢至 {_retryAfterUtc:O}");
                        return;
                    }

                    if (!result.IsSuccess)
                    {
                        success = false;
                        // 失敗與未知一律保留原狀態（不轉離線、不刪設定）；可辨識的失效只警告一次，避免每輪洗版。
                        if (_unavailableChannels.TryAdd(spider.ChannelId, 0))
                            Log.Warn(result.IsNotFound
                                ? $"CHZZK 頻道不存在或已無法匿名存取：{spider.ChannelId}"
                                : $"CHZZK 無法取得直播狀態（HTTP {result.HttpStatus?.ToString() ?? "未知"}）：{spider.ChannelId}");
                        continue;
                    }

                    _unavailableChannels.TryRemove(spider.ChannelId, out _);
                    try
                    {
                        await ApplyObservationAsync(spider.ChannelId, result.Status!);
                    }
                    catch (Exception ex)
                    {
                        // 單一頻道失敗不影響其他頻道，也不清除其已保存狀態。
                        Log.Error(ex.Demystify(), $"CHZZK 狀態更新失敗：{spider.ChannelId}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                success = false;
                Log.Error(ex.Demystify(), "CHZZK 輪詢週期失敗");
            }
            finally
            {
                _metrics.RecordChzzkPollCycle(success ? ScraperMetricResult.Success : ScraperMetricResult.Failure);
            }
        }

        /// <summary>套用單次有效觀察：依狀態機更新場次，必要時發布開台／關台。</summary>
        private async Task ApplyObservationAsync(string channelId, ChzzkLiveStatus status)
        {
            using var db = _dbService.GetDbContext();
            var spider = await db.ChzzkSpider.SingleOrDefaultAsync(x => x.ChannelId == channelId);
            if (spider == null)
                return;

            ChzzkStream current = null;
            if (!string.IsNullOrEmpty(spider.CurrentStreamKey))
                current = await db.ChzzkStreams
                    .SingleOrDefaultAsync(x => x.StreamKey == spider.CurrentStreamKey);

            DateTime now = DateTime.UtcNow;
            var action = DecideObservation(spider, current, status, now, out string streamKey);

            // 需要目前場次的動作若找不到資料列（異常資料），不捏造場次，下一輪重新觀察。
            if (current == null && action is ChzzkPollAction.RefreshObserved or ChzzkPollAction.CancelPendingClose
                    or ChzzkPollAction.StartPendingClose or ChzzkPollAction.ConfirmClose)
            {
                Log.Warn($"CHZZK 找不到目前場次資料列，本輪不更新狀態：{channelId} / {spider.CurrentStreamKey}");
                return;
            }

            switch (action)
            {
                case ChzzkPollAction.Unknown:
                    Log.Warn($"CHZZK 觀察資料無法識別，本輪不更新狀態：{channelId} / status={status.Status}");
                    return;

                case ChzzkPollAction.Ignore:
                    return;

                case ChzzkPollAction.BaselineOffline:
                    spider.InitializedAt = now;
                    await db.SaveChangesAsync();
                    return;

                case ChzzkPollAction.RefreshObserved:
                    ApplySnapshot(current, status);
                    current.LastObservedAt = now;
                    await db.SaveChangesAsync();
                    return;

                case ChzzkPollAction.CancelPendingClose:
                    current.Status = ChzzkStreamStatus.Open;
                    ApplySnapshot(current, status);
                    current.LastObservedAt = now;
                    await db.SaveChangesAsync();
                    return;

                case ChzzkPollAction.StartPendingClose:
                    // 等待起點記在 LastObservedAt；重複 CLOSE 不再更新，故不會重設等待。
                    current.Status = ChzzkStreamStatus.PendingClose;
                    current.LastObservedAt = now;
                    await db.SaveChangesAsync();
                    Log.Info($"CHZZK 收到關台訊號，等待 {ChzzkPollPolicy.CloseConfirmationDelay.TotalMinutes:0} 分鐘後確認：{current.StreamKey}");
                    return;

                case ChzzkPollAction.ConfirmClose:
                    await ConfirmCloseAsync(spider, current, status.CloseDate, now, PublishAsync);
                    await db.SaveChangesAsync();
                    return;

                case ChzzkPollAction.TrackNewStream:
                case ChzzkPollAction.SupersedeAndTrack:
                    // 同鍵場次列已存在（例如移除爬蟲後重新加入、或先前資料列被接手）：
                    // 接回既有場次且不重發，避免破壞 StreamKey 唯一約束或重複通知。
                    var existingStream = await db.ChzzkStreams
                        .FirstOrDefaultAsync(x => x.StreamKey == streamKey);
                    if (existingStream != null)
                    {
                        existingStream.Status = ChzzkStreamStatus.Open;
                        ApplySnapshot(existingStream, status);
                        existingStream.LastObservedAt = now;
                        spider.CurrentStreamKey = streamKey;
                        spider.InitializedAt ??= now;
                        await db.SaveChangesAsync();
                        return;
                    }

                    if (action == ChzzkPollAction.SupersedeAndTrack && current != null)
                    {
                        current.Status = ChzzkStreamStatus.Superseded;
                        // 未確認舊場關台時間即出現新場次：不補造 closeDate，只記錄取代關係。
                        Log.Info($"CHZZK 場次已被新場次取代：{current.StreamKey} → {streamKey}");
                    }

                    var stream = new ChzzkStream
                    {
                        StreamKey = streamKey,
                        ChannelId = channelId,
                        OpenDateRaw = status.OpenDate,
                        StreamTitle = status.LiveTitle,
                        CategoryName = status.LiveCategoryValue,
                        Status = ChzzkStreamStatus.Open,
                        LastObservedAt = now
                    };
                    db.ChzzkStreams.Add(stream);
                    spider.CurrentStreamKey = streamKey;
                    spider.InitializedAt ??= now;
                    await db.SaveChangesAsync();
                    await DelegateRecordThenPublishAsync(action, spider, stream, PublishRecordAsync, PublishAsync);
                    return;
            }
        }

        /// <summary>將 API 觀察與持久化場次轉為決策；獨立於 I/O，讓測試涵蓋實際輪詢使用的場次鍵轉換。</summary>
        internal static ChzzkPollAction DecideObservation(ChzzkSpider spider, ChzzkStream current,
            ChzzkLiveStatus status, DateTime now, out string streamKey)
        {
            string knownStatus = ChzzkClient.TryGetKnownStatus(status.Status);
            streamKey = null;
            if (knownStatus == null)
                return ChzzkPollAction.Unknown;

            // OPEN 與 CLOSE 都需要相同的場次鍵；只有首次 CLOSE 建立離線基線時不需識別上一場。
            bool hasValidStreamKey = ChzzkStreamIdentity.TryCreate(spider.ChannelId, status.OpenDate, out streamKey);
            if (!hasValidStreamKey && (knownStatus == ChzzkLiveStatusValues.Open || spider.InitializedAt != null))
                return ChzzkPollAction.Unknown;

            return ChzzkPollPolicy.Decide(new ChzzkPollFacts(
                IsOpen: knownStatus == ChzzkLiveStatusValues.Open,
                HasValidStreamKey: hasValidStreamKey,
                StreamKey: streamKey,
                IsInitialized: spider.InitializedAt != null,
                CurrentStreamKey: spider.CurrentStreamKey,
                CurrentStatus: current?.Status,
                PendingCloseSinceUtc: current is { Status: ChzzkStreamStatus.PendingClose } ? current.LastObservedAt : null,
                NowUtc: now));
        }

        private static void ApplySnapshot(ChzzkStream stream, ChzzkLiveStatus status)
        {
            stream.StreamTitle = status.LiveTitle;
            stream.CategoryName = status.LiveCategoryValue;
        }

        /// <summary>
        /// 對齊 Twitch：發布成功才標記關台，失敗保留 PendingClose 供下一輪重新確認並重試。
        /// 發布成功後若 DB 保存失敗仍可能重投，由既有 Notifier 去重；不提供跨系統交易保證。
        /// </summary>
        internal static async Task ConfirmCloseAsync(ChzzkSpider spider, ChzzkStream stream,
            string closeDate, DateTime now, Func<ChzzkNotification, Task> publishAsync)
        {
            stream.CloseDateRaw = closeDate;
            await publishAsync(CreateNotification(spider, stream, ChzzkNoticeType.EndStream));
            stream.Status = ChzzkStreamStatus.Closed;
            stream.LastObservedAt = now;
        }

        /// <summary>
        /// 建立新場次後的兩個獨立副作用：先依 <see cref="ChzzkSpider.IsRecord"/> 委派錄影，再發布開台通知。
        /// 只有新場次（<see cref="ChzzkPollAction.TrackNewStream"/> / <see cref="ChzzkPollAction.SupersedeAndTrack"/>）
        /// 會委派；同場 OPEN、接回既有場次、PendingClose 恢復與關台都不補錄，也不依啟用時間排除。
        /// 錄影發布失敗只記錄、不阻擋開台通知；通知發布失敗也不影響已完成的錄影嘗試，維持既有通知重送政策。
        /// </summary>
        internal static async Task<bool> DelegateRecordThenPublishAsync(
            ChzzkPollAction action,
            ChzzkSpider spider,
            ChzzkStream stream,
            Func<string, string, Task<long>> publishRecordAsync,
            Func<ChzzkNotification, Task> publishNotificationAsync)
        {
            bool recordDelegated = false;
            bool isNewStream = action is ChzzkPollAction.TrackNewStream or ChzzkPollAction.SupersedeAndTrack;
            if (isNewStream && spider.IsRecord)
            {
                try
                {
                    long receiverCount = await publishRecordAsync(stream.ChannelId, stream.StreamKey);
                    recordDelegated = receiverCount > 0;
                    if (recordDelegated)
                        Log.Info($"已發送 CHZZK 錄影請求：{stream.StreamKey}");
                    else
                        Log.Warn($"Redis 訂閱頻道不存在，請啟動錄影工具：{stream.StreamKey}");
                }
                catch (Exception ex)
                {
                    // 不補送：需要時由 Bot 擁有者使用立即錄影指令重試。
                    Log.Error(ex.Demystify(), $"CHZZK 錄影請求發布失敗（不影響開台通知）：{stream.StreamKey}");
                }
            }

            await publishNotificationAsync(CreateNotification(spider, stream, ChzzkNoticeType.StartStream));
            return recordDelegated;
        }

        /// <summary>委派錄影給錄影工具；沒有訂閱者或 Redis 未就緒時回傳 0，由呼叫端記錄但不重試。</summary>
        internal static Task<long> PublishRecordAsync(string channelId, string streamKey)
            => Bot.RedisSub == null
                ? Task.FromResult(0L)
                : ChzzkRecordBus.PublishAsync(Bot.RedisSub, channelId, streamKey);

        /// <summary>
        /// 建立通知 DTO：原始字串供識別與診斷，UTC 時間在偵測端轉換一次，消費端不重複解析。
        /// </summary>
        internal static ChzzkNotification CreateNotification(ChzzkSpider spider, ChzzkStream stream, ChzzkNoticeType noticeType)
        {
            DateTime? startAtUtc = ChzzkTime.TryParseKstToUtc(stream.OpenDateRaw, out DateTime parsedStart)
                ? parsedStart
                : null;
            DateTime? endAtUtc = ChzzkTime.TryParseKstToUtc(stream.CloseDateRaw, out DateTime parsedEnd)
                ? parsedEnd
                : null;

            return new ChzzkNotification
            {
                NoticeType = noticeType,
                ChannelId = stream.ChannelId,
                ChannelName = spider.ChannelName,
                ChannelImageUrl = spider.ChannelImageUrl,
                StreamKey = stream.StreamKey,
                OpenDate = stream.OpenDateRaw,
                CloseDate = stream.CloseDateRaw,
                StreamStartAt = startAtUtc,
                StreamEndAt = endAtUtc,
                StreamTitle = stream.StreamTitle,
                Category = stream.CategoryName
            };
        }

        private async Task PublishAsync(ChzzkNotification notification)
        {
            Log.Info(notification.NoticeType == ChzzkNoticeType.StartStream
                ? $"CHZZK 開台：{notification.ChannelName} ({notification.StreamKey}) - {notification.StreamTitle}"
                : $"CHZZK 關台：{notification.ChannelName} ({notification.StreamKey}) - {notification.StreamTitle}");

            // 讓輪詢端處理失敗，關台不可因吞掉例外而保存為 Closed。
            await NotificationBus.PublishAsync(Bot.RedisDb, NotifyType.Chzzk, notification);
        }
    }
}
