using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Localization;
using Clip = TwitchLib.Api.Helix.Models.Clips.GetClips.Clip;
using User = TwitchLib.Api.Helix.Models.Users.GetUsers.User;
using Video = TwitchLib.Api.Helix.Models.Videos.GetVideos.Video;

#if !DEBUG
using Polly;
#endif

using Bot = DiscordStreamNotifyBot.Shared.BotState;

namespace DiscordStreamNotifyBot.SharedService.Twitch
{
    /// <summary>
    /// Twitch 指令支援 + 通知發送（Notifier 專用）：指令所需的 Twitch API 一律委派 Shared <see cref="TwitchApiService"/>；
    /// 消費匯流排 <see cref="Shared.Messages.TwitchNotification"/> 後重建 embed，只發送給本 shard 持有的伺服器。
    /// 偵測（EventSub 訂閱 / 輪詢 / WebHook 維護）由 Scraper 負責。
    /// </summary>
    public class TwitchService : IInteractionService
    {
        public enum NoticeType
        {
            [ChoiceDisplay("Stream started")]
            StartStream,
            [ChoiceDisplay("Stream ended")]
            EndStream,
            [ChoiceDisplay("Stream details changed")]
            ChangeStreamData
        }

        internal bool IsEnable => _apiService.IsEnable;
        internal Lazy<TwitchLib.Api.TwitchAPI> TwitchApi => _apiService.TwitchApi;

        private readonly DiscordSocketClient _client;
        private readonly TwitchApiService _apiService;
        private readonly EmojiService _emojiService;
        private readonly MainDbService _dbService;
        private readonly BotConfig _botConfig;
        private readonly NoticeCache<DataBase.Table.NoticeTwitchStreamChannel> _noticeCache;
        private readonly BotLocalizer _localizer;
        private readonly GuildLocaleService _guildLocaleService;
        private readonly NotifierMetrics _metrics;

        public TwitchService(DiscordSocketClient client, TwitchApiService apiService, BotConfig botConfig,
            EmojiService emojiService, MainDbService dbService, BotLocalizer localizer,
            GuildLocaleService guildLocaleService, NotifierMetrics metrics)
        {
            _client = client;
            _apiService = apiService;
            _emojiService = emojiService;
            _dbService = dbService;
            _botConfig = botConfig;
            _localizer = localizer;
            _guildLocaleService = guildLocaleService;
            _metrics = metrics;
            _noticeCache = new NoticeCache<DataBase.Table.NoticeTwitchStreamChannel>(dbService, db => db.NoticeTwitchStreamChannels.AsNoTracking().ToList());
        }

        #region 指令支援（委派 Shared TwitchApiService）
        public string GetUserLoginByUrl(string url) => _apiService.GetUserLoginByUrl(url);

        public TimeSpan ParseToTimeSpan(string input) => _apiService.ParseToTimeSpan(input);

        public Task<User> GetUserAsync(string twitchUserId = "", string twitchUserLogin = "")
            => _apiService.GetUserAsync(twitchUserId, twitchUserLogin);

        public Task<IReadOnlyList<User>> GetUsersAsync(params string[] twitchUserLogins)
            => _apiService.GetUsersAsync(twitchUserLogins);

        public Task<Video> GetLatestVODAsync(string twitchUserId) => _apiService.GetLatestVODAsync(twitchUserId);

        public Task<IReadOnlyList<Clip>> GetClipsAsync(string twitchUserId, DateTime startedAt, DateTime endedAt)
            => _apiService.GetClipsAsync(twitchUserId, startedAt, endedAt);

        public Task<IReadOnlyList<TwitchLib.Api.Helix.Models.EventSub.EventSubSubscription>> GetEventSubSubscriptionsAsync(string userId = null)
            => _apiService.GetEventSubSubscriptionsAsync(userId);

        internal Task<bool> CreateEventSubSubscriptionAsync(string broadcasterUserId)
            => _apiService.CreateEventSubSubscriptionAsync(broadcasterUserId);

        public Task<bool> DeleteEventSubSubscriptionAsync(string userId) => _apiService.DeleteEventSubSubscriptionAsync(userId);

        public void InvalidateNoticeCache() => _noticeCache.Invalidate();
        #endregion

        /// <summary>
        /// 通知匯流排消費端入口：依 DTO 類型以工廠重建 embed 後，走 <see cref="SendStreamMessageAsync"/> 發送
        /// （shard 過濾沿用既有守衛）。Profile/Offline 圖片由本端 DB（TwitchSpider）補齊。
        /// </summary>
        public async Task DispatchFromBusAsync(Shared.Messages.TwitchNotification dto)
        {
            DataBase.Table.TwitchSpider twitchSpider;
            using (var db = _dbService.GetDbContext())
                twitchSpider = db.TwitchSpider.AsNoTracking().FirstOrDefault((x) => x.UserId == dto.UserId);

            NoticeType noticeType;
            switch (dto.NoticeType)
            {
                case Shared.Messages.TwitchNoticeType.StartStream:
                    noticeType = NoticeType.StartStream;
                    break;

                case Shared.Messages.TwitchNoticeType.EndStream:
                    noticeType = NoticeType.EndStream;
                    break;

                case Shared.Messages.TwitchNoticeType.ChangeStreamData:
                    noticeType = NoticeType.ChangeStreamData;
                    break;

                default:
                    return;
            }

            long thumbnailCacheBuster = DateTime.UtcNow.ToFileTimeUtc();
            await SendStreamMessageAsync(dto, twitchSpider, noticeType, thumbnailCacheBuster).ConfigureAwait(false);
        }

        private TwitchNotificationVariant BuildVariant(Shared.Messages.TwitchNotification dto,
            DataBase.Table.TwitchSpider twitchSpider, NoticeType noticeType, long thumbnailCacheBuster, string locale)
        {
            Embed embed;
            switch (noticeType)
            {
                case NoticeType.StartStream:
                    var twitchStream = new DataBase.Table.TwitchStream
                    {
                        UserId = dto.UserId,
                        UserLogin = dto.UserLogin,
                        UserName = dto.UserName,
                        StreamTitle = dto.StreamTitle,
                        GameName = dto.GameName,
                        ThumbnailUrl = dto.ThumbnailUrl,
                        StreamStartAt = dto.StreamStartAt ?? DateTime.UtcNow,
                    };
                    embed = TwitchEmbedBuilderFactory.CreateStreamStarted(twitchStream,
                        twitchSpider?.ProfileImageUrl, dto.IsRecord, thumbnailCacheBuster,
                        _localizer, locale).Build();
                    break;
                case NoticeType.EndStream:
                    embed = TwitchEmbedBuilderFactory.CreateStreamEnded(dto.UserName, dto.UserLogin,
                        dto.StreamTitle, dto.StreamStartAt, dto.StreamEndAt ?? DateTime.UtcNow,
                        dto.Clips, dto.ClipsValue, twitchSpider?.ProfileImageUrl,
                        twitchSpider?.OfflineImageUrl, _localizer, locale).Build();
                    break;
                case NoticeType.ChangeStreamData:
                    embed = TwitchEmbedBuilderFactory.CreateChannelUpdate(dto.UserName, dto.UserLogin,
                        dto.Updates, dto.Description, twitchSpider?.ProfileImageUrl, _localizer, locale).Build();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(noticeType));
            }

            MessageComponent component = noticeType == NoticeType.StartStream
                ? new ComponentBuilder()
                    .WithButton(_localizer.Get("Notifications.Button.RandomVideo", locale), style: ButtonStyle.Link,
                        emote: _emojiService.YouTubeEmote, url: "https://api.konnokai.me/randomvideo")
                    .WithButton(_localizer.Get("Notifications.Button.SupportEcpay", locale), style: ButtonStyle.Link,
                        emote: _emojiService.ECPayEmote, url: Utility.ECPayUrl, row: 1)
                    .WithButton(_localizer.Get("Notifications.Button.SupportPaypal", locale), style: ButtonStyle.Link,
                        emote: _emojiService.PayPalEmote, url: Utility.PaypalUrl, row: 1)
                    .Build()
                : null;
            return new TwitchNotificationVariant(embed, component);
        }

        internal async Task SendStreamMessageAsync(Shared.Messages.TwitchNotification dto,
            DataBase.Table.TwitchSpider twitchSpider, NoticeType noticeType, long thumbnailCacheBuster)
        {
            if (!Bot.IsConnect)
                return;

            NotificationMetricEvent metricEvent = NotifierMetrics.ToMetricEvent(dto.NoticeType);

#if DEBUG || DEBUG_DONTREGISTERCOMMAND
            Log.New($"Twitch 通知: {dto.UserId} - {dto.StreamTitle} ({noticeType})");
#else
            using (var db = _dbService.GetDbContext())
            {
                // 通知設定改讀記憶體快取（§12.3）
                var noticeGuildList = _noticeCache.Get().Where((x) => x.NoticeTwitchUserId == dto.UserId).ToList();
                Log.New($"發送 Twitch 通知 ({noticeGuildList.Count} / {noticeType}): ({dto.UserId}) - {dto.StreamTitle}");
                var variants = new Dictionary<string, Lazy<TwitchNotificationVariant>>(StringComparer.Ordinal);
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
                        string sendMessage = "";
                        switch (noticeType)
                        {
                            case NoticeType.StartStream:
                                sendMessage = item.StartStreamMessage;
                                break;
                            case NoticeType.EndStream:
                                sendMessage = item.EndStreamMessage;
                                break;
                            case NoticeType.ChangeStreamData:
                                sendMessage = item.ChangeStreamDataMessage;
                                break;
                        }

                        if (sendMessage == "-")
                        {
                            if (guildsById.ContainsKey(item.GuildId))
                                deliveryResult = NotificationDeliveryResult.Disabled;
                            continue;
                        }

                        if (!guildsById.TryGetValue(item.GuildId, out SocketGuild guild))
                        {
                            // 多 Shard 環境：非本 Shard 持有的伺服器，或尚未 Ready，皆靜默略過，避免互刪設定
                            if (!Bot.ShouldDeleteMissingGuild(item.GuildId))
                                continue;

                            Log.Warn($"Twitch 通知 ({dto.UserId}) | 找不到伺服器 {item.GuildId}");
                            deliveryResult = NotificationDeliveryResult.MissingGuild;
                            db.NoticeTwitchStreamChannels.RemoveRange(db.NoticeTwitchStreamChannels.Where((x) => x.GuildId == item.GuildId));
                            db.SaveChanges();
                            _noticeCache.Invalidate();
                            continue;
                        }

                        string locale = localesByGuildId[guild.Id];
                        if (!variants.TryGetValue(locale, out var variantValue))
                        {
                            variantValue = new Lazy<TwitchNotificationVariant>(
                                () => BuildVariant(dto, twitchSpider, noticeType, thumbnailCacheBuster, locale),
                                LazyThreadSafetyMode.ExecutionAndPublication);
                            variants.Add(locale, variantValue);
                        }
                        TwitchNotificationVariant variant = variantValue.Value;

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
                                _metrics.RecordNotificationDeliveryRetry(metricEvent);
                                var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                                Log.Warn($"Twitch 通知 ({dto.UserId}) | {item.GuildId} / {item.DiscordChannelId} 發送失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                                return timeSpan;
                            })
                            .ExecuteAsync(async () =>
                            {
                                var message = await channel.SendMessageAsync(text: sendMessage, embed: variant.Embed,
                                    components: variant.Component,
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
                        if (Bot.TryShutdownOnDiscordAuthorizationFailure(httpEx, $"Twitch 通知 ({dto.UserId})"))
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
                            Log.Warn($"Twitch 通知 ({dto.UserId}) | 遺失權限 {item.GuildId} / {item.DiscordChannelId}");
                            db.NoticeTwitchStreamChannels.RemoveRange(db.NoticeTwitchStreamChannels.Where((x) => x.DiscordChannelId == item.DiscordChannelId));
                            db.SaveChanges();
                            _noticeCache.Invalidate();
                        }
                        else if (((int)httpEx.HttpCode).ToString().StartsWith("50"))
                        {
                            deliveryResult = primaryMessageSent
                                ? NotificationDeliveryResult.Sent
                                : NotificationDeliveryResult.Discord5xx;
                            Log.Warn($"Twitch 通知 ({dto.UserId}) | Discord 50X 錯誤: {httpEx.HttpCode}");
                        }
                        else
                        {
                            deliveryResult = primaryMessageSent
                                ? NotificationDeliveryResult.Sent
                                : NotificationDeliveryResult.UnknownError;
                            Log.Error(httpEx, $"Twitch 通知 ({dto.UserId}) | Discord 未知錯誤 {item.GuildId} / {item.DiscordChannelId}");
                        }
                    }
                    catch (TimeoutException)
                    {
                        deliveryResult = primaryMessageSent
                            ? NotificationDeliveryResult.Sent
                            : NotificationDeliveryResult.Timeout;
                        Log.Warn($"Twitch 通知 ({dto.UserId}) | Timeout {item.GuildId} / {item.DiscordChannelId}");
                    }
                    catch (Exception ex)
                    {
                        deliveryResult = primaryMessageSent
                            ? NotificationDeliveryResult.Sent
                            : NotificationDeliveryResult.UnknownError;
                        Log.Error(ex.Demystify(), $"Twitch 通知 ({dto.UserId}) | 未知錯誤 {item.GuildId} / {item.DiscordChannelId}");
                    }
                    finally
                    {
                        if (deliveryStopwatch != null)
                        {
                            deliveryStopwatch.Stop();
                            _metrics.ObserveNotificationDeliveryDuration(metricEvent, deliveryStopwatch.Elapsed);
                        }

                        if (deliveryResult.HasValue)
                            _metrics.RecordNotificationDelivery(metricEvent, deliveryResult.Value);
                    }
                }
            }
#endif
        }

        private sealed record TwitchNotificationVariant(Embed Embed, MessageComponent Component);
    }
}
