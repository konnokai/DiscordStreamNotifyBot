using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.HttpClients.Chzzk;
using DiscordStreamNotifyBot.HttpClients.Chzzk.Model;
using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.AdminSettings;
using DiscordStreamNotifyBot.SharedService.Cluster;
using DiscordStreamNotifyBot.SharedService.Member;
using Newtonsoft.Json.Linq;

namespace DiscordStreamNotifyBot.SharedService.Chzzk
{
    /// <summary>
    /// CHZZK 指令支援 + 通知發送（Notifier 專用）：以網站匿名 endpoint 驗證頻道、維護追蹤來源與通知設定，
    /// 並消費匯流排 <see cref="Shared.Messages.ChzzkNotification"/> 重建 embed，只發送給本 shard 持有的伺服器。
    /// 偵測（輪詢 live-status）由 Scraper 負責。
    /// <para>
    /// 不含 CHZZK OAuth、錄影、警告名單（IsWarningUser）等首版未使用功能。
    /// </para>
    /// </summary>
    public class ChzzkService : IInteractionService
    {
        public enum NoticeType
        {
            [ChoiceDisplay("Stream started")]
            StartStream,
            [ChoiceDisplay("Stream ended")]
            EndStream
        }

        private readonly DiscordSocketClient _client;
        private readonly ChzzkClient _chzzkClient;
        private readonly BotConfig _botConfig;
        private readonly EmojiService _emojiService;
        private readonly MainDbService _dbService;
        private readonly NoticeCache<DataBase.Table.NoticeChzzkStreamChannel> _noticeCache;
        private readonly BotLocalizer _localizer;
        private readonly GuildLocaleService _guildLocaleService;
        private readonly NotifierMetrics _metrics;
        private readonly MemberOperationCoordinator _operationCoordinator;
        private readonly ClusterQueryService _clusterQuery;

        public ChzzkService(DiscordSocketClient client, ChzzkClient chzzkClient, BotConfig botConfig,
            EmojiService emojiService,
            MainDbService dbService, BotLocalizer localizer, GuildLocaleService guildLocaleService,
            NotifierMetrics metrics, MemberOperationCoordinator operationCoordinator, ClusterQueryService clusterQuery)
        {
            _client = client;
            _chzzkClient = chzzkClient;
            _botConfig = botConfig;
            _emojiService = emojiService;
            _dbService = dbService;
            _localizer = localizer;
            _guildLocaleService = guildLocaleService;
            _metrics = metrics;
            _operationCoordinator = operationCoordinator;
            _clusterQuery = clusterQuery;
            _noticeCache = new NoticeCache<DataBase.Table.NoticeChzzkStreamChannel>(dbService,
                db => db.NoticeChzzkStreamChannels.AsNoTracking().ToList());
        }

        public void InvalidateNoticeCache() => _noticeCache.Invalidate();

        /// <summary>取得頻道資料（新增驗證與快照名稱更新用）；失敗回傳 null。</summary>
        public async Task<ChzzkChannel> GetChannelAsync(string channelId, CancellationToken cancellationToken = default)
        {
            var result = await _chzzkClient.GetChannelAsync(channelId, cancellationToken).ConfigureAwait(false);
            return result.IsSuccess ? result.Channel : null;
        }

        #region 爬蟲來源

        public async Task<AdminSettingsMutationResult> AddCrawlerAsync(
            SocketGuild guild,
            ulong actorUserId,
            string source,
            CancellationToken cancellationToken,
            bool addForBotOwner = false)
        {
            if (!ChzzkUrlParser.TryParseChannelId(source, out string channelId))
                return AdminSettingsMutationResult.Rejected("crawler.source-not-found");

            var channelResult = await _chzzkClient.GetChannelAsync(channelId, cancellationToken).ConfigureAwait(false);
            if (!channelResult.IsSuccess)
                return AdminSettingsMutationResult.Rejected(
                    channelResult.IsNotFound ? "crawler.source-not-found" : "settings.operation-failed");
            var channel = channelResult.Channel;

            // CHZZK 無 OAuth 豁免；200 人門檻沿用 Twitch，Bot Owner 與官方伺服器豁免。
            if (!CrawlerPolicy.HasGeneralEligibility(
                actorUserId, Bot.ApplicatonOwner.Id, Utility.OfficialGuildContains(guild.Id), guild.MemberCount, 200))
                return AdminSettingsMutationResult.Rejected("crawler.guild-member-requirement", new JObject
                {
                    ["requiredMemberCount"] = 200,
                    ["memberCount"] = guild.MemberCount
                });

            using var db = _dbService.GetDbContext();
            int limit = await GetChzzkCrawlerLimitAsync(db, guild.Id, cancellationToken);
            // Bot owner 不論以自己或伺服器名義新增，都不受該 guild 爬蟲數量上限限制；官方伺服器亦同。
            bool limitApplies = actorUserId != Bot.ApplicatonOwner.Id && !Utility.OfficialGuildContains(guild.Id);
            bool limitReached = limitApplies && await db.ChzzkSpider.AsNoTracking()
                .CountAsync(x => x.GuildId == guild.Id, cancellationToken) >= limit;

            var existing = await db.ChzzkSpider.SingleOrDefaultAsync(x => x.ChannelId == channelId, cancellationToken);
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
                existing.ChannelName = channel.ChannelName;
                existing.ChannelImageUrl = channel.ChannelImageUrl ?? "";
                await db.SaveChangesAsync(cancellationToken);
                Log.Info($"已接管 CHZZK 頻道爬蟲 | Guild: {guild.Id} | Actor: {actorUserId} | Source: {channelId}");
                return Added(existing.ChannelId, existing.ChannelName);
            }
            if (limitReached)
                return LimitReached(limit);

            db.ChzzkSpider.Add(new ChzzkSpider
            {
                GuildId = addForBotOwner ? 0 : guild.Id,
                ChannelId = channelId,
                ChannelName = channel.ChannelName,
                ChannelImageUrl = channel.ChannelImageUrl ?? ""
            });
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                using var reloadDb = _dbService.GetDbContext();
                var current = await reloadDb.ChzzkSpider.AsNoTracking().SingleOrDefaultAsync(
                    x => x.ChannelId == channelId, cancellationToken);
                return current?.GuildId == guild.Id
                    ? AdminSettingsMutationResult.Rejected("crawler.already-exists")
                    : AdminSettingsMutationResult.Rejected("crawler.source-owned");
            }
            await CrawlerOwnerNotifier.NotifyAddedAsync(
                CrawlerPlatform.Chzzk, guild, actorUserId, channelId, channel.ChannelName, channelId, addForBotOwner);
            Log.Info($"已新增 CHZZK 頻道爬蟲 | Guild: {guild.Id} | Actor: {actorUserId} | Source: {channelId}");
            return Added(channelId, channel.ChannelName);
        }

        public async Task<AdminSettingsMutationResult> RemoveCrawlerAsync(
            ulong guildId,
            string sourceId,
            CancellationToken cancellationToken,
            bool botOwner = false)
        {
            using var db = _dbService.GetDbContext();
            var crawler = await db.ChzzkSpider.SingleOrDefaultAsync(x => x.ChannelId == sourceId, cancellationToken);
            if (crawler == null)
                return AdminSettingsMutationResult.Rejected("crawler.not-configured");
            if (!CrawlerPolicy.CanRemove(crawler.GuildId, guildId, botOwner))
                return AdminSettingsMutationResult.Rejected("crawler.not-owned");

            // 移除爬蟲只停止偵測；其他 guild 的通知設定、場次與待補送事件一律保留。
            db.ChzzkSpider.Remove(crawler);
            await db.SaveChangesAsync(cancellationToken);
            Log.Info($"已移除 CHZZK 頻道爬蟲 | Guild: {guildId} | Source: {sourceId}");
            return AdminSettingsMutationResult.Applied("crawler.removed", new JObject { ["sourceId"] = sourceId });
        }

        internal static async Task<int> GetChzzkCrawlerLimitAsync(
            MainDbContext db,
            ulong guildId,
            CancellationToken cancellationToken)
            => CrawlerPolicy.ResolveLimit(await db.GuildConfig.AsNoTracking()
                .Where(x => x.GuildId == guildId && x.MaxChzzkSpiderCount > 0)
                .Select(x => (uint?)x.MaxChzzkSpiderCount)
                .SingleOrDefaultAsync(cancellationToken), 3);
        #endregion

        #region 通知設定

        public async Task<AdminSettingsMutationResult> UpsertNotificationAsync(
            SocketGuild guild,
            string source,
            ulong discordChannelId,
            AdminSettingsChzzkMessages messages,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(source) || !ChzzkUrlParser.TryParseChannelId(source, out string channelId))
                return AdminSettingsMutationResult.Rejected("settings.invalid-source");

            cancellationToken.ThrowIfCancellationRequested();
            var channelResult = await _chzzkClient.GetChannelAsync(channelId, cancellationToken).ConfigureAwait(false);
            if (!channelResult.IsSuccess)
                return AdminSettingsMutationResult.Rejected(
                    channelResult.IsNotFound ? "settings.source-not-found" : "settings.operation-failed");

            var rejected = AdminSettingsChannelValidator.Validate(_client, guild, discordChannelId);
            if (rejected != null)
                return rejected;

            try
            {
                await using var guildLock = await _operationCoordinator.LockGuildAsync(guild.Id, cancellationToken);
                using var db = _dbService.GetDbContext();
                var notice = await db.NoticeChzzkStreamChannels.FirstOrDefaultAsync(
                    x => x.GuildId == guild.Id && x.NoticeChzzkChannelId == channelId,
                    cancellationToken);
                if (notice == null)
                {
                    notice = new NoticeChzzkStreamChannel { GuildId = guild.Id, NoticeChzzkChannelId = channelId };
                    db.NoticeChzzkStreamChannels.Add(notice);
                }

                notice.DiscordChannelId = discordChannelId;
                notice.StartStreamMessage = messages.Start;
                notice.EndStreamMessage = messages.End;

                // 順便更新來源名稱快取，讓通知與快照使用最新名稱。
                var spider = await db.ChzzkSpider.SingleOrDefaultAsync(x => x.ChannelId == channelId, cancellationToken);
                if (spider != null)
                {
                    spider.ChannelName = channelResult.Channel.ChannelName;
                    spider.ChannelImageUrl = channelResult.Channel.ChannelImageUrl ?? "";
                }

                await db.SaveChangesAsync(cancellationToken);
                _noticeCache.Invalidate();
                return AdminSettingsMutationResult.Applied(arguments: new JObject
                {
                    ["sourceId"] = channelId,
                    ["sourceName"] = channelResult.Channel.ChannelName
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "網頁管理設定 CHZZK 通知更新失敗");
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
                string channelId = source.Trim();
                if (!ChzzkUrlParser.TryParseChannelId(channelId, out _) &&
                    !await db.NoticeChzzkStreamChannels.AsNoTracking()
                        .AnyAsync(x => x.GuildId == guildId && x.NoticeChzzkChannelId == channelId, cancellationToken))
                    return AdminSettingsMutationResult.Rejected("settings.invalid-source");

                await using var guildLock = await _operationCoordinator.LockGuildAsync(guildId, cancellationToken);
                await db.NoticeChzzkStreamChannels
                    .Where(x => x.GuildId == guildId && x.NoticeChzzkChannelId == channelId)
                    .ExecuteDeleteAsync(cancellationToken);
                _noticeCache.Invalidate();
                return AdminSettingsMutationResult.Applied("settings.removed", new JObject { ["sourceId"] = channelId });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "網頁管理設定 CHZZK 通知移除失敗");
                return AdminSettingsMutationResult.Rejected("settings.operation-failed");
            }
        }
        #endregion

        /// <summary>
        /// 通知匯流排消費端入口：依 DTO 類型重建 embed 後發送（shard 過濾沿用既有守衛）。
        /// 頻道頭像由偵測端帶入；來源已被移除時仍以 DTO 內容完成通知。
        /// </summary>
        internal async Task DispatchFromBusAsync(ChzzkNotification dto, NotificationDeliveryProgress progress)
        {
            NoticeType? noticeType = dto.NoticeType switch
            {
                ChzzkNoticeType.StartStream => NoticeType.StartStream,
                ChzzkNoticeType.EndStream => NoticeType.EndStream,
                _ => null
            };
            if (noticeType == null)
                return;

            await SendStreamMessageAsync(dto, noticeType.Value, progress).ConfigureAwait(false);
        }

        private NotificationVariant BuildVariant(ChzzkNotification dto, NoticeType noticeType, string locale)
        {
            Embed embed = noticeType == NoticeType.StartStream
                ? ChzzkEmbedBuilderFactory.CreateStreamStarted(dto, _localizer, locale).Build()
                : ChzzkEmbedBuilderFactory.CreateStreamEnded(dto, _localizer, locale).Build();
            // 沿用既有非平台專屬通知按鈕（僅開台訊息附帶）。
            MessageComponent component = noticeType == NoticeType.StartStream && !_botConfig.DisableNotificationsAds
                ? _emojiService.BuildNotificationAdsComponent(_localizer, locale)
                : null;
            return new NotificationVariant(embed, component);
        }

        internal async Task SendStreamMessageAsync(ChzzkNotification dto,
            NoticeType noticeType, NotificationDeliveryProgress progress)
        {
            if (!Bot.IsConnect)
                throw new InvalidOperationException("Discord 尚未就緒，保留通知等待重試。");

            NotificationMetricEvent metricEvent = NotifierMetrics.ToMetricEvent(dto.NoticeType);

#if DEBUG || DEBUG_DONTREGISTERCOMMAND
            Log.New($"CHZZK 通知: {dto.ChannelId} - {dto.StreamTitle} ({noticeType})");
#else
            using (var db = _dbService.GetDbContext())
            {
                var noticeGuildList = _noticeCache.Get()
                    .Where(x => x.NoticeChzzkChannelId == dto.ChannelId).ToList();
                Log.New($"發送 CHZZK 通知 ({noticeGuildList.Count(x => Bot.IsServerOnThisShard(x.GuildId))} / {noticeType}): ({dto.ChannelId}) - {dto.StreamTitle}");
                var guildsById = noticeGuildList
                    .Select(item => item.GuildId)
                    .Distinct()
                    .Select(guildId => _client.GetGuild(guildId))
                    .Where(guild => guild != null)
                    .ToDictionary(guild => guild.Id);
                Dictionary<ulong, string> localesByGuildId = await _guildLocaleService.GetManyAsync(guildsById.Values);

                string source = $"CHZZK 通知 ({dto.ChannelId})";
                var delivery = new ChannelNotificationDelivery(
                    new NotificationDelivery(_metrics, metricEvent, progress),
                    guildsById, localesByGuildId,
                    new NotificationVariantCache<NotificationVariant>(locale => BuildVariant(dto, noticeType, locale)),
                    failureLogPrefix: $"{source} | ",
                    retryLogPrefix: $"{source} | ",
                    removeGuildNotices: guildId =>
                    {
                        db.NoticeChzzkStreamChannels.RemoveRange(
                            db.NoticeChzzkStreamChannels.Where(x => x.GuildId == guildId));
                        db.SaveChanges();
                        _noticeCache.Invalidate();
                    },
                    removeChannelNotices: channelId =>
                    {
                        db.NoticeChzzkStreamChannels.RemoveRange(
                            db.NoticeChzzkStreamChannels.Where(x => x.DiscordChannelId == channelId));
                        db.SaveChanges();
                        _noticeCache.Invalidate();
                    });

                foreach (var item in noticeGuildList)
                {
                    string sendMessage = noticeType == NoticeType.StartStream
                        ? item.StartStreamMessage
                        : item.EndStreamMessage;

                    await delivery.SendAsync($"{item.Id}:{item.DiscordChannelId}", item.GuildId, item.DiscordChannelId,
                        sendMessage, skipDisabledMessage: true, source);
                }
            }
#endif
        }

        private static AdminSettingsMutationResult Added(string sourceId, string sourceName)
            => AdminSettingsMutationResult.Applied("crawler.added", new JObject
            {
                ["sourceId"] = sourceId,
                ["sourceName"] = sourceName
            });

        private static AdminSettingsMutationResult LimitReached(int limit)
            => AdminSettingsMutationResult.Rejected("crawler.limit-reached", new JObject { ["limit"] = limit });
    }
}
