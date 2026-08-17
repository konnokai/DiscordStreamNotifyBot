using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.AdminSettings;
using DiscordStreamNotifyBot.SharedService.Cluster;
using DiscordStreamNotifyBot.SharedService.Member;
using Newtonsoft.Json.Linq;
using Clip = TwitchLib.Api.Helix.Models.Clips.GetClips.Clip;
using User = TwitchLib.Api.Helix.Models.Users.GetUsers.User;
using Video = TwitchLib.Api.Helix.Models.Videos.GetVideos.Video;

#if !DEBUG
using Polly;
#endif


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

        private readonly DiscordSocketClient _client;
        private readonly TwitchApiService _apiService;
        private readonly EmojiService _emojiService;
        private readonly MainDbService _dbService;
        private readonly BotConfig _botConfig;
        private readonly NoticeCache<DataBase.Table.NoticeTwitchStreamChannel> _noticeCache;
        private readonly BotLocalizer _localizer;
        private readonly GuildLocaleService _guildLocaleService;
        private readonly NotifierMetrics _metrics;
        private readonly MemberOperationCoordinator _operationCoordinator;
        private readonly ClusterQueryService _clusterQuery;

        public TwitchService(DiscordSocketClient client, TwitchApiService apiService, BotConfig botConfig,
            EmojiService emojiService, MainDbService dbService, BotLocalizer localizer,
            GuildLocaleService guildLocaleService, NotifierMetrics metrics,
            MemberOperationCoordinator operationCoordinator, ClusterQueryService clusterQuery)
        {
            _client = client;
            _apiService = apiService;
            _emojiService = emojiService;
            _dbService = dbService;
            _botConfig = botConfig;
            _localizer = localizer;
            _guildLocaleService = guildLocaleService;
            _metrics = metrics;
            _operationCoordinator = operationCoordinator;
            _clusterQuery = clusterQuery;
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

        public async Task<AdminSettingsMutationResult> AddCrawlerAsync(
            SocketGuild guild,
            ulong actorUserId,
            string source,
            CancellationToken cancellationToken,
            bool addForBotOwner = false)
        {
            if (!_apiService.IsEnable)
                return AdminSettingsMutationResult.Rejected("crawler.platform-disabled");
            var user = ulong.TryParse(source, out _)
                ? await GetUserAsync(twitchUserId: source)
                : await GetUserAsync(twitchUserLogin: GetUserLoginByUrl(source.Trim()));
            if (user == null)
                return AdminSettingsMutationResult.Rejected("crawler.source-not-found");

            using var db = _dbService.GetDbContext();
            bool generallyEligible = CrawlerPolicy.HasGeneralEligibility(
                actorUserId, Bot.ApplicatonOwner.Id, Utility.OfficialGuildContains(guild.Id), guild.MemberCount, 200);
            bool oauthEligible = await db.TwitchBroadcasterAuthorization.AsNoTracking().AnyAsync(x =>
                x.RevokedAt == null && x.ClientId == _botConfig.TwitchClientId &&
                x.DiscordUserId == actorUserId && x.TwitchUserId == user.Id, cancellationToken);
            if (!generallyEligible && !oauthEligible)
                return AdminSettingsMutationResult.Rejected("crawler.oauth-eligibility-required", new JObject
                {
                    ["requiredMemberCount"] = 200,
                    ["memberCount"] = guild.MemberCount
                });

            int limit = await GetTwitchCrawlerLimitAsync(db, guild.Id, cancellationToken);
            bool limitReached = !Utility.OfficialGuildContains(guild.Id) &&
                await db.TwitchSpider.AsNoTracking().CountAsync(x => x.GuildId == guild.Id, cancellationToken) >= limit;
            var existing = await db.TwitchSpider.SingleOrDefaultAsync(x => x.UserId == user.Id, cancellationToken);
            if (existing != null)
            {
                if (existing.GuildId == guild.Id || addForBotOwner && existing.GuildId == 0)
                    return AdminSettingsMutationResult.Rejected("crawler.already-exists");
                if (existing.GuildId == 0 || addForBotOwner)
                    return AdminSettingsMutationResult.Rejected("crawler.source-owned");
                var guilds = await _clusterQuery.GetGuildNameMapAsync();
                if (guilds.ContainsKey(existing.GuildId))
                    return AdminSettingsMutationResult.Rejected("crawler.source-owned");
                if (limitReached)
                    return LimitReached(limit);
                existing.GuildId = guild.Id;
                await db.SaveChangesAsync(cancellationToken);
                await PublishReconcileRequestedAsync(existing.UserId, "spider_owner_changed");
                return Added(existing.UserId, existing.UserName);
            }
            if (limitReached)
                return LimitReached(limit);

            db.TwitchSpider.Add(new DataBase.Table.TwitchSpider
            {
                GuildId = addForBotOwner ? 0 : guild.Id,
                UserId = user.Id,
                UserLogin = user.Login,
                UserName = user.DisplayName,
                ProfileImageUrl = user.ProfileImageUrl,
                OfflineImageUrl = user.OfflineImageUrl
            });
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                using var reloadDb = _dbService.GetDbContext();
                var current = await reloadDb.TwitchSpider.AsNoTracking().SingleOrDefaultAsync(
                    x => x.UserId == user.Id, cancellationToken);
                return current?.GuildId == guild.Id
                    ? AdminSettingsMutationResult.Rejected("crawler.already-exists")
                    : AdminSettingsMutationResult.Rejected("crawler.source-owned");
            }
            await PublishReconcileRequestedAsync(user.Id, generallyEligible ? "spider_added" : "oauth_bypass_addition");
            Log.Info($"已新增 Twitch 頻道爬蟲 | Guild: {guild.Id} | Actor: {actorUserId} | Source: {user.Id}");
            return Added(user.Id, user.DisplayName);
        }

        public async Task<AdminSettingsMutationResult> RemoveCrawlerAsync(
            ulong guildId,
            string sourceId,
            CancellationToken cancellationToken,
            bool botOwner = false)
        {
            using var db = _dbService.GetDbContext();
            var crawler = await db.TwitchSpider.SingleOrDefaultAsync(x => x.UserId == sourceId, cancellationToken);
            if (crawler == null)
                return AdminSettingsMutationResult.Rejected("crawler.not-configured");
            if (!CrawlerPolicy.CanRemove(crawler.GuildId, guildId, botOwner))
                return AdminSettingsMutationResult.Rejected("crawler.not-owned");
            db.TwitchSpider.Remove(crawler);
            await db.SaveChangesAsync(cancellationToken);
            await PublishReconcileRequestedAsync(sourceId, "spider_removed");
            Log.Info($"已移除 Twitch 頻道爬蟲 | Guild: {guildId} | Source: {sourceId}");
            return AdminSettingsMutationResult.Applied("crawler.removed", new JObject { ["sourceId"] = sourceId });
        }

        internal static async Task<int> GetTwitchCrawlerLimitAsync(
            MainDbContext db,
            ulong guildId,
            CancellationToken cancellationToken)
            => CrawlerPolicy.ResolveLimit(await db.GuildConfig.AsNoTracking()
                .Where(x => x.GuildId == guildId && x.MaxTwitchSpiderCount > 0)
                .Select(x => (uint?)x.MaxTwitchSpiderCount)
                .SingleOrDefaultAsync(cancellationToken), 3);

        private static Task PublishReconcileRequestedAsync(string twitchUserId, string reason)
            => Bot.RedisSub.PublishAsync(
                new RedisChannel(RedisChannels.Twitch.ReconcileRequested, RedisChannel.PatternMode.Literal),
                JsonConvert.SerializeObject(new { TwitchUserId = twitchUserId, Reason = reason }));

        private static AdminSettingsMutationResult Added(string sourceId, string sourceName)
            => AdminSettingsMutationResult.Applied("crawler.added", new JObject
            {
                ["sourceId"] = sourceId,
                ["sourceName"] = sourceName
            });

        private static AdminSettingsMutationResult LimitReached(int limit)
            => AdminSettingsMutationResult.Rejected("crawler.limit-reached", new JObject { ["limit"] = limit });

        public async Task<AdminSettingsMutationResult> UpsertNotificationAsync(
            SocketGuild guild,
            string source,
            ulong channelId,
            AdminSettingsTwitchMessages messages,
            CancellationToken cancellationToken)
        {
            if (!_apiService.IsEnable)
                return AdminSettingsMutationResult.Rejected("settings.feature-disabled");
            if (string.IsNullOrWhiteSpace(source))
                return AdminSettingsMutationResult.Rejected("settings.invalid-source");

            cancellationToken.ThrowIfCancellationRequested();
            var user = ulong.TryParse(source, out _)
                ? await GetUserAsync(twitchUserId: source)
                : await GetUserAsync(twitchUserLogin: GetUserLoginByUrl(source.Trim()));
            if (user == null)
                return AdminSettingsMutationResult.Rejected("settings.source-not-found");

            var rejected = AdminSettingsChannelValidator.Validate(_client, guild, channelId);
            if (rejected != null)
                return rejected;

            try
            {
                await using var guildLock = await _operationCoordinator.LockGuildAsync(guild.Id, cancellationToken);
                using var db = _dbService.GetDbContext();
                var notice = await db.NoticeTwitchStreamChannels.FirstOrDefaultAsync(
                    x => x.GuildId == guild.Id && x.NoticeTwitchUserId == user.Id,
                    cancellationToken);
                if (notice == null)
                {
                    notice = new NoticeTwitchStreamChannel { GuildId = guild.Id, NoticeTwitchUserId = user.Id };
                    db.NoticeTwitchStreamChannels.Add(notice);
                }

                notice.DiscordChannelId = channelId;
                notice.StartStreamMessage = messages.Start;
                notice.EndStreamMessage = messages.End;
                notice.ChangeStreamDataMessage = messages.Change;
                await db.SaveChangesAsync(cancellationToken);
                _noticeCache.Invalidate();
                return AdminSettingsMutationResult.Applied(arguments: new JObject
                {
                    ["sourceId"] = user.Id,
                    ["sourceName"] = user.DisplayName
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "網頁管理設定 Twitch 通知更新失敗");
                return AdminSettingsMutationResult.Rejected("settings.operation-failed");
            }
        }

        public async Task<AdminSettingsMutationResult> RemoveNotificationAsync(
            ulong guildId,
            string source,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(source))
                return AdminSettingsMutationResult.Rejected("settings.invalid-source");

            try
            {
                using var db = _dbService.GetDbContext();
                string sourceId = source.Trim();
                bool resolvedId = ulong.TryParse(sourceId, out _) || await db.NoticeTwitchStreamChannels.AsNoTracking()
                    .AnyAsync(x => x.GuildId == guildId && x.NoticeTwitchUserId == sourceId, cancellationToken);
                if (!resolvedId)
                {
                    if (!_apiService.IsEnable)
                        return AdminSettingsMutationResult.Rejected("settings.feature-disabled");
                    var user = ulong.TryParse(sourceId, out _)
                        ? await GetUserAsync(twitchUserId: sourceId)
                        : await GetUserAsync(twitchUserLogin: GetUserLoginByUrl(sourceId));
                    if (user == null)
                        return AdminSettingsMutationResult.Rejected("settings.source-not-found");
                    sourceId = user.Id;
                }

                await using var guildLock = await _operationCoordinator.LockGuildAsync(guildId, cancellationToken);
                await db.NoticeTwitchStreamChannels
                    .Where(x => x.GuildId == guildId && x.NoticeTwitchUserId == sourceId)
                    .ExecuteDeleteAsync(cancellationToken);
                _noticeCache.Invalidate();
                return AdminSettingsMutationResult.Applied("settings.removed", new JObject { ["sourceId"] = sourceId });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "網頁管理設定 Twitch 通知移除失敗");
                return AdminSettingsMutationResult.Rejected("settings.operation-failed");
            }
        }
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
