namespace DiscordStreamNotifyBot.SharedService
{
    /// <summary>通知失敗時要寫 log 的情況；文字由各平台提供，維持既有 log 內容。</summary>
    internal enum NotificationFailureLog
    {
        Discord5xx,
        DiscordUnknownError,
        Timeout,
        UnknownError
    }

    /// <summary>單一接收目標的發送狀態，由發送流程填入，<see cref="NotificationDelivery"/> 依此記錄 metric 與完成 checkpoint。</summary>
    internal sealed class NotificationDeliveryAttempt
    {
        /// <summary>發送結果；維持 null 代表本 shard 不處理此目標，不記 metric 也不寫完成 checkpoint。</summary>
        public NotificationDeliveryResult? Result { get; set; }

        /// <summary>主要通知訊息已送出；之後才發生的失敗仍記為 Sent。</summary>
        public bool PrimaryMessageSent { get; set; }

        /// <summary>保留事件等待重試，不寫入完成 checkpoint。</summary>
        public bool RetryRequired { get; set; }

        internal Stopwatch DurationStopwatch { get; private set; }

        /// <summary>開始計算發送耗時；只有實際呼叫 Discord 發送的目標才記錄耗時。</summary>
        public void StartTiming() => DurationStopwatch = Stopwatch.StartNew();
    }

    /// <summary>依語系建立的通知內容。</summary>
    internal sealed record NotificationVariant(Embed Embed, MessageComponent Component);

    /// <summary>
    /// 同一事件內依語系共用的通知內容。沿用 Lazy 語意：建立失敗時，同語系的後續目標會重拋同一個例外。
    /// </summary>
    internal sealed class NotificationVariantCache<TVariant>(Func<string, TVariant> build)
    {
        private readonly Dictionary<string, Lazy<TVariant>> _variants = new(StringComparer.Ordinal);

        public TVariant Get(string locale)
        {
            if (!_variants.TryGetValue(locale, out var variant))
            {
                variant = new Lazy<TVariant>(() => build(locale), LazyThreadSafetyMode.ExecutionAndPublication);
                _variants.Add(locale, variant);
            }
            return variant.Value;
        }
    }

    /// <summary>
    /// 各平台通知逐目標發送共用的錯誤分類與收尾：授權失效時廣播關閉、永久失敗清除通知設定、暫時性錯誤保留重試，
    /// 最後記錄 metric 並寫入完成 checkpoint。各平台的 log 文字與清除範圍由呼叫端提供。
    /// </summary>
    internal sealed class NotificationDelivery(NotifierMetrics metrics, NotificationMetricEvent metricEvent,
        NotificationDeliveryProgress progress)
    {
        /// <param name="target">完成 checkpoint 的目標鍵，已完成的目標直接略過。</param>
        /// <param name="authorizationSource">Discord 授權失效關閉時記錄的來源。</param>
        /// <param name="onPermanentFailure">權限不足或目標不存在時呼叫，負責寫 log 並清除送不到的通知設定。</param>
        /// <param name="failureLog">其餘失敗的 log 文字；Discord 錯誤會帶入 <see cref="Discord.Net.HttpException"/>，逾時與未知錯誤為 null。</param>
        /// <param name="deliverAsync">實際的發送流程，透過 <see cref="NotificationDeliveryAttempt"/> 回報結果。</param>
        internal async Task RunAsync(string target, string authorizationSource,
            Action<Discord.Net.HttpException> onPermanentFailure,
            Func<NotificationFailureLog, Discord.Net.HttpException, string> failureLog,
            Func<NotificationDeliveryAttempt, Task> deliverAsync)
        {
            if (progress.IsComplete(target))
                return;

            var attempt = new NotificationDeliveryAttempt();
            try
            {
                await deliverAsync(attempt);
            }
            catch (Discord.Net.HttpException httpEx)
            {
                if (Bot.TryShutdownOnDiscordAuthorizationFailure(httpEx, authorizationSource))
                {
                    attempt.RetryRequired = true;
                    attempt.Result = attempt.PrimaryMessageSent
                        ? NotificationDeliveryResult.Sent
                        : NotificationDeliveryResult.AuthorizationFailure;
                    throw;
                }

                if (NotificationTargetFailure.IsPermanent(httpEx))
                {
                    attempt.Result = attempt.PrimaryMessageSent
                        ? NotificationDeliveryResult.Sent
                        : NotificationDeliveryResult.MissingPermission;
                    onPermanentFailure(httpEx);
                }
                else if (((int)httpEx.HttpCode).ToString().StartsWith("50"))
                {
                    attempt.RetryRequired = true;
                    progress.Fail(httpEx);
                    attempt.Result = attempt.PrimaryMessageSent
                        ? NotificationDeliveryResult.Sent
                        : NotificationDeliveryResult.Discord5xx;
                    Log.Warn(failureLog(NotificationFailureLog.Discord5xx, httpEx));
                }
                else
                {
                    attempt.RetryRequired = true;
                    progress.Fail(httpEx);
                    attempt.Result = attempt.PrimaryMessageSent
                        ? NotificationDeliveryResult.Sent
                        : NotificationDeliveryResult.UnknownError;
                    Log.Error(httpEx, failureLog(NotificationFailureLog.DiscordUnknownError, httpEx));
                }
            }
            catch (TimeoutException ex)
            {
                attempt.RetryRequired = true;
                progress.Fail(ex);
                attempt.Result = attempt.PrimaryMessageSent
                    ? NotificationDeliveryResult.Sent
                    : NotificationDeliveryResult.Timeout;
                Log.Warn(failureLog(NotificationFailureLog.Timeout, null));
            }
            catch (Exception ex)
            {
                attempt.RetryRequired = true;
                progress.Fail(ex);
                attempt.Result = attempt.PrimaryMessageSent
                    ? NotificationDeliveryResult.Sent
                    : NotificationDeliveryResult.UnknownError;
                Log.Error(ex.Demystify(), failureLog(NotificationFailureLog.UnknownError, null));
            }
            finally
            {
                if (attempt.DurationStopwatch != null)
                {
                    attempt.DurationStopwatch.Stop();
                    metrics.ObserveNotificationDeliveryDuration(metricEvent, attempt.DurationStopwatch.Elapsed);
                }

                if (attempt.Result.HasValue)
                {
                    metrics.RecordNotificationDelivery(metricEvent, attempt.Result.Value);
                    if (!attempt.RetryRequired)
                        await progress.CompleteAsync(target);
                }
            }
        }

        /// <summary>以共用重試策略發送主要通知訊息，官方伺服器的公告頻道另外發布（crosspost），成功後記為 Sent。</summary>
        /// <param name="retryLogPrefix">重試 log 的前綴，維持各平台既有文字。</param>
        internal async Task SendMessageAsync(NotificationDeliveryAttempt attempt, string target, SocketGuild guild,
            SocketTextChannel channel, string text, Embed embed, MessageComponent components, string retryLogPrefix)
        {
            attempt.StartTiming();
            await DiscordRetryPolicy.Create((retryAttempt, timeSpan) =>
                {
                    metrics.RecordNotificationDeliveryRetry(metricEvent);
                    Log.Warn($"{retryLogPrefix}{guild.Id} / {channel.Id} 發送失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                })
                .ExecuteAsync(() => progress.SendAsync(target, channel, async () =>
                {
                    var message = await channel.SendMessageAsync(text: text, embed: embed,
                        components: components,
                        options: new RequestOptions() { RetryMode = RetryMode.AlwaysRetry });
                    attempt.PrimaryMessageSent = true;
                    return message;
                }, channel is INewsChannel && Utility.OfficialGuildList.Contains(guild.Id)));
            attempt.Result = NotificationDeliveryResult.Sent;
        }
    }

    /// <summary>
    /// Twitch、CHZZK、TwitCasting 共用的單一通知頻道發送流程：找伺服器、依語系取得通知內容、找通知頻道後發送。
    /// </summary>
    /// <param name="failureLogPrefix">失敗 log 的前綴，維持各平台既有文字。</param>
    /// <param name="retryLogPrefix">重試 log 的前綴，維持各平台既有文字。</param>
    /// <param name="removeGuildNotices">伺服器已不存在時，清除該伺服器的所有通知設定。</param>
    /// <param name="removeChannelNotices">權限不足或目標不存在時，清除指向該通知頻道的通知設定。</param>
    internal sealed class ChannelNotificationDelivery(
        NotificationDelivery delivery,
        Dictionary<ulong, SocketGuild> guildsById,
        Dictionary<ulong, string> localesByGuildId,
        NotificationVariantCache<NotificationVariant> variants,
        string failureLogPrefix,
        string retryLogPrefix,
        Action<ulong> removeGuildNotices,
        Action<ulong> removeChannelNotices)
    {
        /// <param name="target">完成 checkpoint 的目標鍵。</param>
        /// <param name="sendMessage">通知設定的自訂訊息。</param>
        /// <param name="skipDisabledMessage">自訂訊息為 "-" 時視為關閉此類通知、不發送。</param>
        /// <param name="source">找不到伺服器 log 與授權失效關閉時記錄的來源。</param>
        internal Task SendAsync(string target, ulong guildId, ulong channelId, string sendMessage,
            bool skipDisabledMessage, string source)
            => delivery.RunAsync(target, source,
                httpEx =>
                {
                    Log.Warn($"{failureLogPrefix}永久失敗（權限或目標不存在）{guildId} / {channelId}：{httpEx.DiscordCode}");
                    removeChannelNotices(channelId);
                },
                (failure, httpEx) => failure switch
                {
                    NotificationFailureLog.Discord5xx => $"{failureLogPrefix}Discord 5xx 錯誤：{httpEx.HttpCode}",
                    NotificationFailureLog.DiscordUnknownError => $"{failureLogPrefix}Discord 未知錯誤 {guildId} / {channelId}",
                    NotificationFailureLog.Timeout => $"{failureLogPrefix}Discord 逾時 {guildId} / {channelId}",
                    _ => $"{failureLogPrefix}未知錯誤 {guildId} / {channelId}",
                },
                async attempt =>
                {
                    if (skipDisabledMessage && sendMessage == "-")
                    {
                        if (guildsById.ContainsKey(guildId))
                            attempt.Result = NotificationDeliveryResult.Disabled;
                        return;
                    }

                    if (!guildsById.TryGetValue(guildId, out SocketGuild guild))
                    {
                        // 多 Shard 環境：非本 Shard 持有的伺服器，或尚未 Ready，皆靜默略過，避免互刪設定
                        if (!Bot.ShouldDeleteMissingGuild(guildId))
                            return;

                        Log.Warn($"{source} | 找不到伺服器 {guildId}");
                        attempt.Result = NotificationDeliveryResult.MissingGuild;
                        removeGuildNotices(guildId);
                        return;
                    }

                    NotificationVariant variant = variants.Get(localesByGuildId[guild.Id]);

                    var channel = guild.GetTextChannel(channelId);
                    if (channel == null)
                    {
                        attempt.Result = NotificationDeliveryResult.MissingChannel;
                        return;
                    }

                    await delivery.SendMessageAsync(attempt, target, guild, channel, sendMessage,
                        variant.Embed, variant.Component, retryLogPrefix);
                });
    }
}
