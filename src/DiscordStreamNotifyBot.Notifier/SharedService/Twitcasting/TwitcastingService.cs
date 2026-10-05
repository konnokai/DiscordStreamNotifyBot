using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.HttpClients;
using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.AdminSettings;
using DiscordStreamNotifyBot.SharedService.Cluster;
using DiscordStreamNotifyBot.SharedService.Member;
using Newtonsoft.Json.Linq;


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
        private readonly BotConfig _botConfig;
        private readonly EmojiService _emojiService;
        private readonly MainDbService _dbService;
        private readonly NoticeCache<DataBase.Table.NoticeTwitcastingStreamChannel> _noticeCache;
        private readonly BotLocalizer _localizer;
        private readonly GuildLocaleService _guildLocaleService;
        private readonly NotifierMetrics _metrics;
        private readonly MemberOperationCoordinator _operationCoordinator;
        private readonly ClusterQueryService _clusterQuery;

        public TwitcastingService(DiscordSocketClient client, TwitcastingClient twitcastingClient,
            BotConfig botConfig, EmojiService emojiService, MainDbService dbService,
            BotLocalizer localizer, GuildLocaleService guildLocaleService, NotifierMetrics metrics,
            MemberOperationCoordinator operationCoordinator, ClusterQueryService clusterQuery)
        {
            _metrics = metrics;
            _operationCoordinator = operationCoordinator;
            _clusterQuery = clusterQuery;
            _client = client;
            _twitcastingClient = twitcastingClient;
            _botConfig = botConfig;
            _emojiService = emojiService;
            _dbService = dbService;
            _localizer = localizer;
            _guildLocaleService = guildLocaleService;
            _noticeCache = new NoticeCache<DataBase.Table.NoticeTwitcastingStreamChannel>(dbService, db => db.NoticeTwitcastingStreamChannels.AsNoTracking().ToList());

            if (botConfig.DisableTwitcasting)
            {
                Log.Warn("TwitCasting 功能已由 bot_config.json 的 DisableTwitcasting 停用");
                IsEnable = false;
                return;
            }

            if (string.IsNullOrEmpty(botConfig.TwitCastingClientId) || string.IsNullOrEmpty(botConfig.TwitCastingClientSecret))
            {
                Log.Warn($"{nameof(botConfig.TwitCastingClientId)} 或 {nameof(botConfig.TwitCastingClientSecret)} 遺失，無法使用 TwitCasting 相關功能");
                IsEnable = false;
                return;
            }
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

        public async Task<string?> GetChannelTitleAsync(
            string channelName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                HtmlAgilityPack.HtmlWeb htmlWeb = new HtmlAgilityPack.HtmlWeb();
                var htmlDocument = await htmlWeb.LoadFromWebAsync(
                    $"https://twitcasting.tv/{channelName}", cancellationToken);
                var htmlNodes = htmlDocument.DocumentNode.Descendants();
                var htmlNode = htmlNodes.FirstOrDefault((x) => x.Name == "span" && x.HasClass("tw-user-nav-name") || x.HasClass("tw-user-nav2-name"));

                if (htmlNode != null)
                {
                    return htmlNode.InnerText.Trim();
                }

                return null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error($"TwitCastingService-GetChannelNameAsync: {ex}");
                return null;
            }
        }

        public void InvalidateNoticeCache() => _noticeCache.Invalidate();

        public async Task<AdminSettingsMutationResult> AddCrawlerAsync(
            SocketGuild guild,
            ulong actorUserId,
            string source,
            CancellationToken cancellationToken,
            bool addForBotOwner = false)
        {
            if (!IsEnable)
                return AdminSettingsMutationResult.Rejected("crawler.platform-disabled");
            if (!CrawlerPolicy.HasGeneralEligibility(
                actorUserId, Bot.ApplicatonOwner.Id, Utility.OfficialGuildContains(guild.Id), guild.MemberCount, 500))
                return AdminSettingsMutationResult.Rejected("crawler.guild-member-requirement", new JObject
                {
                    ["requiredMemberCount"] = 500,
                    ["memberCount"] = guild.MemberCount
                });
            var broadcaster = await GetChannelNameAndTitleAsync(source.Trim());
            if (broadcaster == null)
                return AdminSettingsMutationResult.Rejected("crawler.source-not-found");

            using var db = _dbService.GetDbContext();
            int limit = await GetTwitcastingCrawlerLimitAsync(db, guild.Id, cancellationToken);
            // Owner 歸屬（GuildId=0）不占操作 guild 名額，也不受該 guild 上限限制（與 CHZZK 一致）。
            bool limitApplies = !addForBotOwner && !Utility.OfficialGuildContains(guild.Id);
            var existing = await db.TwitcastingSpider.SingleOrDefaultAsync(
                x => x.ScreenId == broadcaster.ScreenId, cancellationToken);
            if (existing != null)
            {
                if (existing.GuildId == guild.Id || addForBotOwner && existing.GuildId == 0)
                    return AdminSettingsMutationResult.Rejected("crawler.already-exists");
                if (existing.GuildId == 0 || addForBotOwner)
                    return AdminSettingsMutationResult.Rejected("crawler.source-owned");
                var guilds = await _clusterQuery.GetGuildNameMapAsync();
                if (guilds.ContainsKey(existing.GuildId))
                    return AdminSettingsMutationResult.Rejected("crawler.source-owned");
                if (limitApplies &&
                    await db.TwitcastingSpider.AsNoTracking().CountAsync(x => x.GuildId == guild.Id, cancellationToken) >= limit)
                    return LimitReached(limit);
                existing.GuildId = guild.Id;
                await db.SaveChangesAsync(cancellationToken);
                return Added(existing.ScreenId, existing.ChannelTitle);
            }
            if (limitApplies &&
                await db.TwitcastingSpider.AsNoTracking().CountAsync(x => x.GuildId == guild.Id, cancellationToken) >= limit)
                return LimitReached(limit);

            db.TwitcastingSpider.Add(new DataBase.Table.TwitcastingSpider
            {
                GuildId = addForBotOwner ? 0 : guild.Id,
                ChannelId = broadcaster.Id,
                ScreenId = broadcaster.ScreenId,
                ChannelTitle = broadcaster.Name
            });
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                using var reloadDb = _dbService.GetDbContext();
                var current = await reloadDb.TwitcastingSpider.AsNoTracking().SingleOrDefaultAsync(
                    x => x.ScreenId == broadcaster.ScreenId, cancellationToken);
                return current?.GuildId == guild.Id
                    ? AdminSettingsMutationResult.Rejected("crawler.already-exists")
                    : AdminSettingsMutationResult.Rejected("crawler.source-owned");
            }
            await CrawlerOwnerNotifier.NotifyAddedAsync(
                CrawlerPlatform.Twitcasting, guild, actorUserId, broadcaster.ScreenId,
                broadcaster.Name, broadcaster.ScreenId, addForBotOwner);
            Log.Info($"已新增 TwitCasting 頻道爬蟲 | Guild: {guild.Id} | Actor: {actorUserId} | Source: {broadcaster.ScreenId}");
            return Added(broadcaster.ScreenId, broadcaster.Name);
        }

        public async Task<AdminSettingsMutationResult> RemoveCrawlerAsync(
            ulong guildId,
            string sourceId,
            CancellationToken cancellationToken,
            bool botOwner = false)
        {
            using var db = _dbService.GetDbContext();
            var crawler = await db.TwitcastingSpider.SingleOrDefaultAsync(
                x => x.ScreenId == sourceId, cancellationToken);
            if (crawler == null)
                return AdminSettingsMutationResult.Rejected("crawler.not-configured");
            if (!CrawlerPolicy.CanRemove(crawler.GuildId, guildId, botOwner))
                return AdminSettingsMutationResult.Rejected("crawler.not-owned");
            db.TwitcastingSpider.Remove(crawler);
            await db.SaveChangesAsync(cancellationToken);
            Log.Info($"已移除 TwitCasting 頻道爬蟲 | Guild: {guildId} | Source: {sourceId}");
            return AdminSettingsMutationResult.Applied("crawler.removed", new JObject { ["sourceId"] = sourceId });
        }

        internal static async Task<int> GetTwitcastingCrawlerLimitAsync(
            MainDbContext db,
            ulong guildId,
            CancellationToken cancellationToken)
            => CrawlerPolicy.ResolveLimit(await db.GuildConfig.AsNoTracking()
                .Where(x => x.GuildId == guildId && x.MaxTwitcastingSpiderCount > 0)
                .Select(x => (uint?)x.MaxTwitcastingSpiderCount)
                .SingleOrDefaultAsync(cancellationToken), 2);

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
            string startMessage,
            CancellationToken cancellationToken)
        {
            if (!IsEnable)
                return AdminSettingsMutationResult.Rejected("settings.feature-disabled");
            if (string.IsNullOrWhiteSpace(source))
                return AdminSettingsMutationResult.Rejected("settings.invalid-source");

            cancellationToken.ThrowIfCancellationRequested();
            var broadcaster = await GetChannelNameAndTitleAsync(source.Trim());
            if (broadcaster == null)
                return AdminSettingsMutationResult.Rejected("settings.source-not-found");

            var rejected = AdminSettingsChannelValidator.Validate(_client, guild, channelId);
            if (rejected != null)
                return rejected;

            try
            {
                await using var guildLock = await _operationCoordinator.LockGuildAsync(guild.Id, cancellationToken);
                using var db = _dbService.GetDbContext();
                var notice = await db.NoticeTwitcastingStreamChannels.FirstOrDefaultAsync(
                    x => x.GuildId == guild.Id && x.ScreenId == broadcaster.ScreenId,
                    cancellationToken);
                if (notice == null)
                {
                    notice = new NoticeTwitcastingStreamChannel { GuildId = guild.Id, ScreenId = broadcaster.ScreenId };
                    db.NoticeTwitcastingStreamChannels.Add(notice);
                }

                notice.DiscordChannelId = channelId;
                notice.StartStreamMessage = startMessage;
                await db.SaveChangesAsync(cancellationToken);
                _noticeCache.Invalidate();
                return AdminSettingsMutationResult.Applied(arguments: new JObject
                {
                    ["sourceId"] = broadcaster.ScreenId,
                    ["sourceName"] = broadcaster.Name
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "網頁管理設定 TwitCasting 通知更新失敗");
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
                await using var guildLock = await _operationCoordinator.LockGuildAsync(guildId, cancellationToken);
                using var db = _dbService.GetDbContext();
                string sourceId = source.Trim().Split('?')[0]
                    .Replace("https://twitcasting.tv/", "", StringComparison.OrdinalIgnoreCase)
                    .Split('/')[0];
                if (string.IsNullOrWhiteSpace(sourceId))
                    return AdminSettingsMutationResult.Rejected("settings.invalid-source");

                await db.NoticeTwitcastingStreamChannels
                    .Where(x => x.GuildId == guildId && x.ScreenId == sourceId)
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
                Log.Error(ex.Demystify(), "網頁管理設定 TwitCasting 通知移除失敗");
                return AdminSettingsMutationResult.Rejected("settings.operation-failed");
            }
        }

#nullable disable

        /// <summary>
        /// 通知匯流排消費端入口：還原 TwitcastingStream 後實際發送。
        /// </summary>
        internal Task DispatchFromBusAsync(Shared.Messages.TwitcastingNotification dto, NotificationDeliveryProgress progress)
        {
            // 功能停用時即使匯流排殘留事件也不發送（Scraper 端已不再發布）。
            if (_botConfig.DisableTwitcasting)
                return Task.CompletedTask;

            return SendStreamMessageAsync(new TwitcastingStream
            {
                ChannelId = dto.ChannelId,
                ChannelTitle = dto.ChannelTitle,
                StreamId = dto.StreamId,
                StreamTitle = dto.StreamTitle,
                StreamSubTitle = dto.StreamSubTitle,
                Category = dto.Category,
                ThumbnailUrl = dto.ThumbnailUrl,
                StreamStartAt = dto.StreamStartAt,
            }, dto.IsPrivate, dto.IsRecord, progress);
        }

        private async Task SendStreamMessageAsync(TwitcastingStream twitcastingStream, bool isPrivate, bool isRecord,
            NotificationDeliveryProgress progress)
        {
            if (!Bot.IsConnect)
                throw new InvalidOperationException("Discord 尚未就緒，保留通知等待重試。");
#if DEBUG
            Log.New($"TwitCasting 開台通知: {twitcastingStream.ChannelTitle} - {twitcastingStream.StreamTitle} (isPrivate: {isPrivate})");
#else
            using (var db = _dbService.GetDbContext())
            {
                // 通知設定改讀記憶體快取（§12.3）
                var noticeGuildList = _noticeCache.Get().Where((x) => x.ScreenId == twitcastingStream.ChannelId).ToList();
                Log.New($"發送 TwitCasting 開台通知 ({noticeGuildList.Count(x => Bot.IsServerOnThisShard(x.GuildId))}): {twitcastingStream.ChannelTitle} - {twitcastingStream.StreamTitle} (私人直播: {isPrivate})");

                var guildsById = noticeGuildList
                    .Select(item => item.GuildId)
                    .Distinct()
                    .Select(guildId => _client.GetGuild(guildId))
                    .Where(guild => guild != null)
                    .ToDictionary(guild => guild.Id);
                Dictionary<ulong, string> localesByGuildId = await _guildLocaleService.GetManyAsync(guildsById.Values);

                // TwitCasting 失敗 log 沿用既有的「TwitCasting 通知 - 」開頭；重試 log 與其他平台一致，加上平台與頻道前綴。
                var delivery = new ChannelNotificationDelivery(
                    new NotificationDelivery(_metrics, NotificationMetricEvent.TwitcastingStart, progress),
                    guildsById, localesByGuildId,
                    new NotificationVariantCache<NotificationVariant>(
                        locale => BuildVariant(twitcastingStream, isPrivate, isRecord, locale)),
                    failureLogPrefix: "TwitCasting 通知 - ",
                    retryLogPrefix: $"TwitCasting 通知 ({twitcastingStream.ChannelId}) | ",
                    removeGuildNotices: guildId =>
                    {
                        db.NoticeTwitcastingStreamChannels.RemoveRange(db.NoticeTwitcastingStreamChannels.Where((x) => x.GuildId == guildId));
                        db.SaveChanges();
                        _noticeCache.Invalidate();
                    },
                    removeChannelNotices: channelId =>
                    {
                        db.NoticeTwitcastingStreamChannels.RemoveRange(db.NoticeTwitcastingStreamChannels.Where((x) => x.DiscordChannelId == channelId));
                        db.SaveChanges();
                        _noticeCache.Invalidate();
                    });

                foreach (var item in noticeGuildList)
                {
                    // 與其他平台一致：自訂訊息為「-」時視為關閉通知、不發送
                    await delivery.SendAsync($"{item.Id}:{item.DiscordChannelId}", item.GuildId, item.DiscordChannelId,
                        item.StartStreamMessage, skipDisabledMessage: true, $"TwitCasting 通知 ({item.DiscordChannelId})");
                }
            }
#endif
        }

        private NotificationVariant BuildVariant(TwitcastingStream twitcastingStream, bool isPrivate, bool isRecord,
            string locale)
        {
            Embed embed = TwitcastingEmbedBuilderFactory.CreateStreamStarted(
                twitcastingStream, isPrivate, isRecord, _localizer, locale).Build();
            // TwitCasting 的按鈕只有贊助連結，沒有隨機影片按鈕。
            MessageComponent component = _botConfig.DisableNotificationsAds
                ? null
                : new ComponentBuilder()
                    .WithButton(_localizer.Get("Notifications.Button.SupportEcpay", locale),
                        style: ButtonStyle.Link, emote: _emojiService.ECPayEmote,
                        url: Utility.ECPayUrl, row: 1)
                    .WithButton(_localizer.Get("Notifications.Button.SupportPaypal", locale),
                        style: ButtonStyle.Link, emote: _emojiService.PayPalEmote,
                        url: Utility.PaypalUrl, row: 1)
                    .Build();
            return new NotificationVariant(embed, component);
        }
    }
}
