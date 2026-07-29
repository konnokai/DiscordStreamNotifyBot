using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.HttpClients;
using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Localization;

#if !DEBUG
using Polly;
#endif

using Bot = DiscordStreamNotifyBot.Shared.BotState;

namespace DiscordStreamNotifyBot.SharedService.Twitcasting
{
    /// <summary>
    /// TwitCasting 指令支援 + 通知發送（Notifier 專用）：消費匯流排 <see cref="Shared.Messages.TwitcastingNotification"/>
    /// 後重建 embed 並只發送給本 shard 持有的伺服器。偵測（Timer / WebHook 維護 / Redis 訂閱）由 Scraper 負責。
    /// </summary>
    public class TwitcastingService : IInteractionService
    {
        public bool IsEnable { get; private set; } = true;

        private readonly DiscordSocketClient _client;
        private readonly TwitcastingClient _twitcastingClient;
        private readonly EmojiService _emojiService;
        private readonly MainDbService _dbService;
        private readonly BotConfig _botConfig;
        private readonly NoticeCache<DataBase.Table.NoticeTwitcastingStreamChannel> _noticeCache;
        private readonly BotLocalizer _localizer;
        private readonly GuildLocaleService _guildLocaleService;
        private readonly NotifierMetrics _metrics;

        public TwitcastingService(DiscordSocketClient client, TwitcastingClient twitcastingClient,
            BotConfig botConfig, EmojiService emojiService, MainDbService dbService,
            BotLocalizer localizer, GuildLocaleService guildLocaleService, NotifierMetrics metrics)
        {
            _metrics = metrics;
            if (string.IsNullOrEmpty(botConfig.TwitCastingClientId) || string.IsNullOrEmpty(botConfig.TwitCastingClientSecret))
            {
                Log.Warn($"{nameof(botConfig.TwitCastingClientId)} 或 {nameof(botConfig.TwitCastingClientSecret)} 遺失，無法運行 TwitCasting 類功能");
                IsEnable = false;
                return;
            }

            _client = client;
            _twitcastingClient = twitcastingClient;
            _emojiService = emojiService;
            _botConfig = botConfig;
            _dbService = dbService;
            _localizer = localizer;
            _guildLocaleService = guildLocaleService;
            _noticeCache = new NoticeCache<DataBase.Table.NoticeTwitcastingStreamChannel>(dbService, db => db.NoticeTwitcastingStreamChannels.AsNoTracking().ToList());
        }

#nullable enable

        public async Task<HttpClients.Twitcasting.Model.Broadcaster?> GetChannelNameAndTitleAsync(string channelUrl)
        {
            string channelName = channelUrl.Split('?')[0].Replace("https://twitcasting.tv/", "").Split('/')[0];
            if (string.IsNullOrEmpty(channelName))
                return null;

            var data = await _twitcastingClient.GetUserInfoAsync(channelName).ConfigureAwait(false);

            return data?.User;
        }

        public async Task<string?> GetChannelTitleAsync(string channelName)
        {
            try
            {
                HtmlAgilityPack.HtmlWeb htmlWeb = new HtmlAgilityPack.HtmlWeb();
                var htmlDocument = await htmlWeb.LoadFromWebAsync($"https://twitcasting.tv/{channelName}");
                var htmlNodes = htmlDocument.DocumentNode.Descendants();
                var htmlNode = htmlNodes.FirstOrDefault((x) => x.Name == "span" && x.HasClass("tw-user-nav-name") || x.HasClass("tw-user-nav2-name"));

                if (htmlNode != null)
                {
                    return htmlNode.InnerText.Trim();
                }

                return null;
            }
            catch (Exception ex)
            {
                Log.Error($"TwitCastingService-GetChannelNameAsync: {ex}");
                return null;
            }
        }

        public void InvalidateNoticeCache() => _noticeCache?.Invalidate();

#nullable disable

        /// <summary>
        /// 通知匯流排消費端入口：還原 TwitcastingStream 後實際發送。
        /// </summary>
        public Task DispatchFromBusAsync(Shared.Messages.TwitcastingNotification dto)
            => SendStreamMessageAsync(new TwitcastingStream
            {
                ChannelId = dto.ChannelId,
                ChannelTitle = dto.ChannelTitle,
                StreamId = dto.StreamId,
                StreamTitle = dto.StreamTitle,
                StreamSubTitle = dto.StreamSubTitle,
                Category = dto.Category,
                ThumbnailUrl = dto.ThumbnailUrl,
                StreamStartAt = dto.StreamStartAt,
            }, dto.IsPrivate, dto.IsRecord);

        private async Task SendStreamMessageAsync(TwitcastingStream twitcastingStream, bool isPrivate, bool isRecord)
        {
#if DEBUG
            Log.New($"TwitCasting 開台通知: {twitcastingStream.ChannelTitle} - {twitcastingStream.StreamTitle} (isPrivate: {isPrivate})");
#else
            using (var db = _dbService.GetDbContext())
            {
                // 通知設定改讀記憶體快取（§12.3）
                var noticeGuildList = _noticeCache.Get().Where((x) => x.ScreenId == twitcastingStream.ChannelId).ToList();
                Log.New($"發送 TwitCasting 開台通知 ({noticeGuildList.Count}): {twitcastingStream.ChannelTitle} - {twitcastingStream.StreamTitle} (私人直播: {isPrivate})");

                var variants = new Dictionary<string, Lazy<TwitcastingNotificationVariant>>(StringComparer.Ordinal);
                var guildsById = noticeGuildList
                    .Select(item => item.GuildId)
                    .Distinct()
                    .Select(guildId => _client.GetGuild(guildId))
                    .Where(guild => guild != null)
                    .GroupBy(guild => guild.Id)
                    .ToDictionary(group => group.Key, group => group.First());
                Dictionary<ulong, string> localesByGuildId = await _guildLocaleService.GetManyAsync(guildsById.Values);

                foreach (var item in noticeGuildList)
                {
                    NotificationDeliveryResult? deliveryResult = null;
                    Stopwatch deliveryStopwatch = null;
                    bool primaryMessageSent = false;
                    try
                    {
                        if (!guildsById.TryGetValue(item.GuildId, out SocketGuild guild))
                        {
                            // 多 Shard 環境：非本 Shard 持有的伺服器，或尚未 Ready，皆靜默略過，避免互刪設定
                            if (!Bot.ShouldDeleteMissingGuild(item.GuildId))
                                continue;

                            Log.Warn($"TwitCasting 通知 ({item.DiscordChannelId}) | 找不到伺服器 {item.GuildId}");
                            deliveryResult = NotificationDeliveryResult.MissingGuild;
                            db.NoticeTwitcastingStreamChannels.RemoveRange(db.NoticeTwitcastingStreamChannels.Where((x) => x.GuildId == item.GuildId));
                            db.SaveChanges();
                            _noticeCache.Invalidate();
                            continue;
                        }

                        string locale = localesByGuildId[guild.Id];
                        if (!variants.TryGetValue(locale, out var variantValue))
                        {
                            variantValue = new Lazy<TwitcastingNotificationVariant>(() =>
                            {
                                Embed embed = TwitcastingEmbedBuilderFactory.CreateStreamStarted(
                                    twitcastingStream, isPrivate, isRecord, _localizer, locale).Build();
                                MessageComponent component = new ComponentBuilder()
                                    .WithButton(_localizer.Get("Notifications.Button.SupportEcpay", locale),
                                        style: ButtonStyle.Link, emote: _emojiService.ECPayEmote,
                                        url: Utility.ECPayUrl, row: 1)
                                    .WithButton(_localizer.Get("Notifications.Button.SupportPaypal", locale),
                                        style: ButtonStyle.Link, emote: _emojiService.PayPalEmote,
                                        url: Utility.PaypalUrl, row: 1)
                                    .Build();
                                return new TwitcastingNotificationVariant(embed, component);
                            }, LazyThreadSafetyMode.ExecutionAndPublication);
                            variants.Add(locale, variantValue);
                        }
                        TwitcastingNotificationVariant variant = variantValue.Value;

                        var channel = guild.GetTextChannel(item.DiscordChannelId);
                        if (channel == null)
                        {
                            deliveryResult = NotificationDeliveryResult.MissingChannel;
                            continue;
                        }

                        deliveryStopwatch = Stopwatch.StartNew();
                        await Policy.Handle<TimeoutException>()
                            .Or<Discord.Net.HttpException>((httpEx) => ((int)httpEx.HttpCode).ToString().StartsWith("50"))
                            .WaitAndRetryAsync(3, (retryAttempt) =>
                            {
                                _metrics.RecordNotificationDeliveryRetry(NotificationMetricEvent.TwitcastingStart);
                                var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                                Log.Warn($"{item.GuildId} / {item.DiscordChannelId} 發送失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                                return timeSpan;
                            })
                            .ExecuteAsync(async () =>
                            {
                                var message = await channel.SendMessageAsync(text: item.StartStreamMessage,
                                    embed: variant.Embed, components: variant.Component,
                                    options: new RequestOptions() { RetryMode = RetryMode.AlwaysRetry });
                                primaryMessageSent = true;

                                try
                                {
                                    if (channel is INewsChannel && Utility.OfficialGuildList.Contains(guild.Id))
                                        await message.CrosspostAsync();
                                }
                                catch (Discord.Net.HttpException httpEx) when (httpEx.DiscordCode == DiscordErrorCode.MessageAlreadyCrossposted)
                                {
                                    // ignore
                                }
                            });
                        deliveryResult = NotificationDeliveryResult.Sent;
                    }
                    catch (Discord.Net.HttpException httpEx)
                    {
                        if (Bot.TryShutdownOnDiscordAuthorizationFailure(httpEx, $"TwitCasting 通知 ({item.DiscordChannelId})"))
                        {
                            deliveryResult = primaryMessageSent
                                ? NotificationDeliveryResult.Sent
                                : NotificationDeliveryResult.AuthorizationFailure;
                            throw;
                        }

                        if (httpEx.DiscordCode.HasValue && (httpEx.DiscordCode.Value == DiscordErrorCode.InsufficientPermissions || httpEx.DiscordCode.Value == DiscordErrorCode.MissingPermissions))
                        {
                            deliveryResult = primaryMessageSent
                                ? NotificationDeliveryResult.Sent
                                : NotificationDeliveryResult.MissingPermission;
                            Log.Warn($"TwitCasting 通知 - 遺失權限 {item.GuildId} / {item.DiscordChannelId}");
                            db.NoticeTwitcastingStreamChannels.RemoveRange(db.NoticeTwitcastingStreamChannels.Where((x) => x.DiscordChannelId == item.DiscordChannelId));
                            db.SaveChanges();
                            _noticeCache.Invalidate();
                        }
                        else if (((int)httpEx.HttpCode).ToString().StartsWith("50"))
                        {
                            deliveryResult = primaryMessageSent
                                ? NotificationDeliveryResult.Sent
                                : NotificationDeliveryResult.Discord5xx;
                            Log.Warn($"TwitCasting 通知 - Discord 50X 錯誤: {httpEx.HttpCode}");
                        }
                        else
                        {
                            deliveryResult = primaryMessageSent
                                ? NotificationDeliveryResult.Sent
                                : NotificationDeliveryResult.UnknownError;
                            Log.Error(httpEx, $"TwitCasting 通知 - Discord 未知錯誤 {item.GuildId} / {item.DiscordChannelId}");
                        }
                    }
                    catch (TimeoutException)
                    {
                        deliveryResult = primaryMessageSent
                            ? NotificationDeliveryResult.Sent
                            : NotificationDeliveryResult.Timeout;
                        Log.Warn($"TwitCasting 通知 - Timeout {item.GuildId} / {item.DiscordChannelId}");
                    }
                    catch (Exception ex)
                    {
                        deliveryResult = primaryMessageSent
                            ? NotificationDeliveryResult.Sent
                            : NotificationDeliveryResult.UnknownError;
                        Log.Error(ex.Demystify(), $"TwitCasting 通知 - 未知錯誤 {item.GuildId} / {item.DiscordChannelId}");
                    }
                    finally
                    {
                        if (deliveryStopwatch != null)
                        {
                            deliveryStopwatch.Stop();
                            _metrics.ObserveNotificationDeliveryDuration(NotificationMetricEvent.TwitcastingStart, deliveryStopwatch.Elapsed);
                        }

                        if (deliveryResult.HasValue)
                            _metrics.RecordNotificationDelivery(NotificationMetricEvent.TwitcastingStart, deliveryResult.Value);
                    }
                }
            }
#endif
        }

        private sealed record TwitcastingNotificationVariant(Embed Embed, MessageComponent Component);
    }
}
