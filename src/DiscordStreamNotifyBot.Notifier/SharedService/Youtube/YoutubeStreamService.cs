using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.AdminSettings;
using DiscordStreamNotifyBot.SharedService.Cluster;
using DiscordStreamNotifyBot.SharedService.Member;
using Google.Apis.YouTube.v3;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Polly;
using TableVideo = DiscordStreamNotifyBot.DataBase.Table.Video;
using YTApiVideo = Google.Apis.YouTube.v3.Data.Video;

namespace DiscordStreamNotifyBot.SharedService.Youtube
{
    /// <summary>
    /// YouTube 指令支援 + 通知發送（Notifier 專用）：指令所需的 YouTube API 一律委派 Shared
    /// <see cref="Shared.YoutubeApiService"/>；消費匯流排 <see cref="YoutubeNotification"/> / <see cref="BannerChangeNotification"/>
    /// 後重建 embed，只發送給本 shard 持有的伺服器（含建立活動、更換伺服器橫幅）。
    /// 偵測（排程爬取 / Redis 訂閱 / PubSub 維護 / reminder 排程）由 Scraper 負責。
    /// </summary>
    public partial class YoutubeStreamService : IInteractionService
    {
        public enum NoticeType
        {
            [ChoiceDisplay("New waiting room")]
            NewStream,
            [ChoiceDisplay("New upload")]
            NewVideo,
            [ChoiceDisplay("Stream or premiere started")]
            Start,
            [ChoiceDisplay("Stream or premiere ended")]
            End,
            [ChoiceDisplay("Schedule changed")]
            ChangeTime,
            [ChoiceDisplay("Deleted or made private")]
            Delete
        }

        public enum NowStreamingHost
        {
            [ChoiceDisplay("Holo")]
            Holo,
            //[ChoiceDisplay("彩虹社")]
            //Niji
        }

        public bool IsRecord { get; set; } = true;

        /// <summary>YouTube API 用戶端，委派至 Shared 的 <see cref="Shared.YoutubeApiService"/>（單一來源）。</summary>
        public YouTubeService YouTubeService => _apiService.YouTubeService;

        private static readonly HttpClient SharedHttpClient = new HttpClient();

        private readonly DiscordSocketClient _client;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MainDbService _dbService;
        private readonly BotConfig _botConfig;
        private readonly Shared.YoutubeApiService _apiService;
        private readonly NoticeCache<NoticeYoutubeStreamChannel> _noticeCache;
        private readonly BotLocalizer _localizer;
        private readonly GuildLocaleService _guildLocaleService;
        private readonly CommandDisplayResolver _commandDisplayResolver;
        private readonly EmojiService _emojiService;
        private readonly NotifierMetrics _metrics;
        private readonly MemberOperationCoordinator _operationCoordinator;
        private readonly ClusterQueryService _clusterQuery;
        private readonly SharedService.Youtube.YoutubeWebSubService _webSubService;
        private readonly SharedService.Youtube.IYoutubeAtomValidatorStore _atomValidators;

        public YoutubeStreamService(DiscordSocketClient client, IHttpClientFactory httpClientFactory,
            BotConfig botConfig, EmojiService emojiService, MainDbService dbService,
            Shared.YoutubeApiService apiService, BotLocalizer localizer,
            GuildLocaleService guildLocaleService, CommandDisplayResolver commandDisplayResolver,
            NotifierMetrics metrics, MemberOperationCoordinator operationCoordinator,
            ClusterQueryService clusterQuery, SharedService.Youtube.YoutubeWebSubService webSubService,
            SharedService.Youtube.IYoutubeAtomValidatorStore atomValidators)
        {
            _client = client;
            _httpClientFactory = httpClientFactory;
            _dbService = dbService;
            _botConfig = botConfig;
            _apiService = apiService;
            _localizer = localizer;
            _guildLocaleService = guildLocaleService;
            _commandDisplayResolver = commandDisplayResolver;
            _emojiService = emojiService;
            _metrics = metrics;
            _operationCoordinator = operationCoordinator;
            _clusterQuery = clusterQuery;
            _webSubService = webSubService;
            _atomValidators = atomValidators;
            _noticeCache = new NoticeCache<NoticeYoutubeStreamChannel>(dbService, db => db.NoticeYoutubeStreamChannel.AsNoTracking().ToList());
        }

        #region 指令支援（委派 Shared YoutubeApiService）
        public Task<string> GetChannelIdAsync(string channelUrl) => _apiService.GetChannelIdAsync(channelUrl);

        public string GetVideoId(string videoUrl) => _apiService.GetVideoId(videoUrl);

        public Task<string> GetChannelTitle(string channelId, CancellationToken cancellationToken = default)
            => _apiService.GetChannelTitle(channelId, cancellationToken);

        public Task<List<string>> GetChannelTitle(IEnumerable<string> channelId, bool formatUrl) => _apiService.GetChannelTitle(channelId, formatUrl);

        public Task<YTApiVideo> GetVideoAsync(string videoId) => _apiService.GetVideoAsync(videoId);

        /// <summary>
        /// WebSub subscribe／unsubscribe；unsubscribe 代表爬蟲已被移除，順手清掉 Atom validator，
        /// 避免同一頻道之後重新加入時被舊 ETag 誤導成 304。
        /// <para>
        /// 一律不回傳例外：取消訂閱失敗或清理失敗都只記錄，不阻止使用者移除本地 crawler（best-effort 語意）。
        /// </para>
        /// </summary>
        public async Task<SharedService.Youtube.YoutubeWebSubRequestResult> PostSubscribeRequestAsync(string channelId, bool subscribe = true)
        {
            try
            {
                var result = await _webSubService.RequestAsync(channelId, subscribe).ConfigureAwait(false);
                if (!subscribe)
                {
                    try
                    {
                        await _atomValidators.RemoveAsync(channelId).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Log.Warn($"移除 YouTube 爬蟲後清除 Atom validator 失敗: {channelId} / {ex.GetType().Name}");
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"YouTube WebSub 要求失敗: {channelId} / subscribe={subscribe}");
                return SharedService.Youtube.YoutubeWebSubRequestResult.Transient(null, null, ex.GetType().Name);
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
            string sourceId = "";
            try
            {
                sourceId = await GetChannelIdAsync(source);
                using var db = _dbService.GetDbContext();
                bool managed = await db.HoloVideos.AsNoTracking().AnyAsync(x => x.ChannelId == sourceId, cancellationToken) ||
                    await db.NijisanjiVideos.AsNoTracking().AnyAsync(x => x.ChannelId == sourceId, cancellationToken);
                if (managed && !await db.YoutubeChannelOwnedType.AsNoTracking()
                    .AnyAsync(x => x.ChannelId == sourceId, cancellationToken))
                    return AdminSettingsMutationResult.Rejected("crawler.source-ineligible");

                int limit = await GetYoutubeCrawlerLimitAsync(db, guild.Id, cancellationToken);
                var existing = await db.YoutubeChannelSpider.SingleOrDefaultAsync(
                    x => x.ChannelId == sourceId, cancellationToken);
                if (existing != null)
                {
                    if (existing.GuildId == guild.Id || addForBotOwner && existing.GuildId == 0)
                        return AdminSettingsMutationResult.Rejected("crawler.already-exists");
                    if (existing.GuildId == 0 || addForBotOwner)
                        return AdminSettingsMutationResult.Rejected("crawler.source-owned");
                    var guilds = await _clusterQuery.GetGuildNameMapAsync();
                    if (guilds.ContainsKey(existing.GuildId))
                        return AdminSettingsMutationResult.Rejected("crawler.source-owned");
                    if (!Utility.OfficialGuildContains(guild.Id) &&
                        await db.YoutubeChannelSpider.AsNoTracking().CountAsync(x => x.GuildId == guild.Id, cancellationToken) >= limit)
                        return LimitReached(limit);
                    existing.GuildId = guild.Id;
                    await db.SaveChangesAsync(cancellationToken);
                    return Added(sourceId, existing.ChannelTitle);
                }

                if (!Utility.OfficialGuildContains(guild.Id) &&
                    await db.YoutubeChannelSpider.AsNoTracking().CountAsync(x => x.GuildId == guild.Id, cancellationToken) >= limit)
                    return LimitReached(limit);
                string sourceName = await GetChannelTitle(sourceId);
                if (string.IsNullOrWhiteSpace(sourceName))
                    return AdminSettingsMutationResult.Rejected("crawler.source-not-found");
                db.YoutubeChannelSpider.Add(new DataBase.Table.YoutubeChannelSpider
                {
                    GuildId = addForBotOwner ? 0 : guild.Id,
                    ChannelId = sourceId,
                    ChannelTitle = sourceName
                });
                await db.SaveChangesAsync(cancellationToken);
                await CrawlerOwnerNotifier.NotifyAddedAsync(
                    CrawlerPlatform.Youtube, guild, actorUserId, sourceId, sourceName,
                    sourceId, addForBotOwner);
                Log.Info($"已新增 YouTube 頻道爬蟲 | Guild: {guild.Id} | Actor: {actorUserId} | Source: {sourceId}");
                return Added(sourceId, sourceName);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex) when (ex is FormatException or ArgumentException or UriFormatException)
            {
                return AdminSettingsMutationResult.Rejected("crawler.source-not-found");
            }
            catch (DbUpdateException)
            {
                using var db = _dbService.GetDbContext();
                var existing = await db.YoutubeChannelSpider.AsNoTracking().SingleOrDefaultAsync(
                    x => x.ChannelId == sourceId, cancellationToken);
                return existing?.GuildId == guild.Id
                    ? AdminSettingsMutationResult.Rejected("crawler.already-exists")
                    : AdminSettingsMutationResult.Rejected("crawler.source-owned");
            }
        }

        public async Task<AdminSettingsMutationResult> RemoveCrawlerAsync(
            ulong guildId,
            string sourceId,
            CancellationToken cancellationToken,
            bool botOwner = false)
        {
            using var db = _dbService.GetDbContext();
            var crawler = await db.YoutubeChannelSpider.SingleOrDefaultAsync(
                x => x.ChannelId == sourceId, cancellationToken);
            if (crawler == null)
                return AdminSettingsMutationResult.Rejected("crawler.not-configured");
            if (!CrawlerPolicy.CanRemove(crawler.GuildId, guildId, botOwner))
                return AdminSettingsMutationResult.Rejected("crawler.not-owned");
            db.YoutubeChannelSpider.Remove(crawler);
            await db.SaveChangesAsync(cancellationToken);
            try { await PostSubscribeRequestAsync(sourceId, false); }
            catch (Exception ex) { Log.Warn($"移除 YouTube 爬蟲後取消 PubSub 失敗: {sourceId} / {ex.GetType().Name}"); }
            Log.Info($"已移除 YouTube 頻道爬蟲 | Guild: {guildId} | Source: {sourceId}");
            return AdminSettingsMutationResult.Applied("crawler.removed", new JObject { ["sourceId"] = sourceId });
        }

        internal static async Task<int> GetYoutubeCrawlerLimitAsync(
            MainDbContext db,
            ulong guildId,
            CancellationToken cancellationToken)
            => CrawlerPolicy.ResolveLimit(await db.GuildConfig.AsNoTracking()
                .Where(x => x.GuildId == guildId && x.MaxYouTubeSpiderCount > 0)
                .Select(x => (uint?)x.MaxYouTubeSpiderCount)
                .SingleOrDefaultAsync(cancellationToken), 3);

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
            ulong streamChannelId,
            ulong videoChannelId,
            bool createEvent,
            AdminSettingsYoutubeMessages messages,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(source))
                return AdminSettingsMutationResult.Rejected("settings.invalid-source");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                string sourceId = await GetChannelIdAsync(source);
                if (sourceId == "all")
                    return AdminSettingsMutationResult.Rejected("settings.invalid-source");

                string sourceName = sourceId switch
                {
                    "holo" => "Hololive",
                    "2434" => "Nijisanji",
                    "other" => "Other",
                    _ => await GetChannelTitle(sourceId)
                };
                if (string.IsNullOrEmpty(sourceName))
                    return AdminSettingsMutationResult.Rejected("settings.source-not-found");

                var rejected = AdminSettingsChannelValidator.Validate(_client, guild, streamChannelId, createEvent)
                    ?? AdminSettingsChannelValidator.Validate(_client, guild, videoChannelId);
                if (rejected != null)
                    return rejected;

                await using var guildLock = await _operationCoordinator.LockGuildAsync(guild.Id, cancellationToken);
                using var db = _dbService.GetDbContext();
                var notice = await db.NoticeYoutubeStreamChannel.FirstOrDefaultAsync(
                    x => x.GuildId == guild.Id && x.YouTubeChannelId == sourceId,
                    cancellationToken);
                if (notice == null)
                {
                    notice = new NoticeYoutubeStreamChannel { GuildId = guild.Id, YouTubeChannelId = sourceId };
                    db.NoticeYoutubeStreamChannel.Add(notice);
                }

                notice.DiscordNoticeStreamChannelId = streamChannelId;
                notice.DiscordNoticeVideoChannelId = videoChannelId;
                notice.IsCreateEventForNewStream = createEvent;
                notice.NewStreamMessage = messages.NewStream;
                notice.NewVideoMessage = messages.NewVideo;
                notice.StratMessage = messages.Start;
                notice.EndMessage = messages.End;
                notice.ChangeTimeMessage = messages.ChangeTime;
                notice.DeleteMessage = messages.Delete;
                await db.SaveChangesAsync(cancellationToken);
                _noticeCache.Invalidate();

                return AdminSettingsMutationResult.Applied(arguments: new JObject
                {
                    ["sourceId"] = sourceId,
                    ["sourceName"] = sourceName
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException or UriFormatException)
            {
                return AdminSettingsMutationResult.Rejected("settings.invalid-source");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "網頁管理設定 YouTube 通知更新失敗");
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
                string sourceId = await GetChannelIdAsync(source);
                if (sourceId == "all")
                    return AdminSettingsMutationResult.Rejected("settings.invalid-source");

                await using var guildLock = await _operationCoordinator.LockGuildAsync(guildId, cancellationToken);
                using var db = _dbService.GetDbContext();
                await db.NoticeYoutubeStreamChannel
                    .Where(x => x.GuildId == guildId && x.YouTubeChannelId == sourceId)
                    .ExecuteDeleteAsync(cancellationToken);
                _noticeCache.Invalidate();
                return AdminSettingsMutationResult.Applied("settings.removed", new JObject { ["sourceId"] = sourceId });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException or UriFormatException)
            {
                return AdminSettingsMutationResult.Rejected("settings.invalid-source");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "網頁管理設定 YouTube 通知移除失敗");
                return AdminSettingsMutationResult.Rejected("settings.operation-failed");
            }
        }

        public async Task<AdminSettingsMutationResult> RemoveAllNotificationsAsync(
            ulong guildId,
            CancellationToken cancellationToken)
        {
            await using var guildLock = await _operationCoordinator.LockGuildAsync(guildId, cancellationToken);
            using var db = _dbService.GetDbContext();
            await db.NoticeYoutubeStreamChannel.Where(x => x.GuildId == guildId).ExecuteDeleteAsync(cancellationToken);
            _noticeCache.Invalidate();
            return AdminSettingsMutationResult.Applied("settings.removed");
        }
        #endregion

        public async Task<Embed> GetNowStreamingChannel(NowStreamingHost host, string locale)
        {
            try
            {
                List<string> idList = new List<string>();
                switch (host)
                {
                    case NowStreamingHost.Holo:
                        {
                            HtmlWeb htmlWeb = new HtmlWeb();
                            HtmlDocument htmlDocument = htmlWeb.Load("https://schedule.hololive.tv/lives/all");
                            idList.AddRange(htmlDocument.DocumentNode.Descendants()
                                .Where((x) => x.Name == "a" &&
                                    x.Attributes["href"].Value.StartsWith("https://www.youtube.com/watch") &&
                                    x.Attributes["style"].Value.Contains("border: 3px"))
                                .Select((x) => x.Attributes["href"].Value.Split("?v=")[1]));
                        }
                        break;
                }

                var video = YouTubeService.Videos.List("snippet");
                video.Id = string.Join(",", idList);
                var videoResult = await video.ExecuteAsync().ConfigureAwait(false);

                EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor()
                    .WithTitle(_localizer.Get("Youtube.NowStreaming.Title", locale))
                    .WithThumbnailUrl("https://schedule.hololive.tv/dist/images/logo.png")
                    .WithCurrentTimestamp()
                    .WithDescription(string.Join("\n", videoResult.Items.Select((x) => $"{x.Snippet.ChannelTitle} - {Format.Url(x.Snippet.Title, $"https://www.youtube.com/watch?v={x.Id}")}")));

                return embedBuilder.Build();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"GetNowStreamingChannel: {host}");
                return null;
            }
        }

        #region 通知匯流排消費端（重建 embed → 發送）
        /// <summary>通知匯流排消費端入口：將 scraper 發來的 DTO 還原為 embed 後送出。</summary>
        internal async Task DispatchFromBusAsync(YoutubeNotification dto, NotificationDeliveryProgress progress)
        {
            var streamVideo = new TableVideo
            {
                VideoId = dto.VideoId,
                ChannelId = dto.ChannelId,
                ChannelTitle = dto.ChannelTitle,
                VideoTitle = dto.VideoTitle,
                ScheduledStartTime = dto.ScheduledStartTime,
                ChannelType = dto.ChannelType,
            };

            NoticeType? noticeType = MapNoticeType(dto.NoticeType);
            if (!noticeType.HasValue)
            {
                Log.Warn($"忽略未知 YouTube 通知類型: {(int)dto.NoticeType} / {dto.VideoId}");
                return;
            }

            await SendStreamMessageAsync(streamVideo, dto, noticeType.Value, progress).ConfigureAwait(false);
        }

        /// <summary>通知匯流排消費端入口：伺服器橫幅變更事件。</summary>
        public Task DispatchBannerFromBusAsync(BannerChangeNotification dto)
            => ChangeGuildBannerAsync(dto.ChannelId, dto.VideoId);

        private YoutubeNotificationVariant BuildVariantForBus(YoutubeNotification dto, TableVideo video, string locale)
        {
            Embed embed;
            switch (dto.NoticeType)
            {
                case YoutubeNoticeType.NewStream:
                    embed = EmbedBuilderFactory.CreateNewStream(video, dto.ScheduledStartTime, _localizer, locale).Build();
                    break;
                case YoutubeNoticeType.NewVideo:
                    embed = EmbedBuilderFactory.CreateNewVideo(video, _localizer, locale).Build();
                    break;
                case YoutubeNoticeType.Start:
                    embed = EmbedBuilderFactory.CreateStreamStarted(video, _localizer, locale).Build();
                    break;
                case YoutubeNoticeType.End:
                    embed = (dto.IsMemberOnly
                        ? EmbedBuilderFactory.CreateStreamEndedAsMemberOnly(video,
                            dto.ActualStartTime ?? dto.ScheduledStartTime,
                            dto.ActualEndTime ?? DateTime.UtcNow, _localizer, locale)
                        : EmbedBuilderFactory.CreateStreamEnded(video,
                            dto.ActualStartTime ?? dto.ScheduledStartTime,
                            dto.ActualEndTime ?? DateTime.UtcNow, _localizer, locale)).Build();
                    break;
                case YoutubeNoticeType.ChangeTime:
                    embed = EmbedBuilderFactory.CreateStreamTimeChangedReminder(video,
                        dto.PreviousScheduledStartTime ?? dto.ScheduledStartTime, _localizer, locale).Build();
                    break;
                case YoutubeNoticeType.Delete:
                    embed = (dto.IsUnarchived
                        ? EmbedBuilderFactory.CreateStreamUnarchived(video, _localizer, locale)
                        : EmbedBuilderFactory.CreateStreamDeleted(video, _localizer, locale)).Build();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dto.NoticeType));
            }

            MessageComponent component = dto.NoticeType == YoutubeNoticeType.Start
                ? BuildMessageComponent(locale)
                : null;
            string togglePath = _commandDisplayResolver.GetCommandPath(locale, "youtube", "toggle-create-event");
            Embed manageEventsWarning = new EmbedBuilder()
                .WithErrorColor()
                .WithDescription(_localizer.Format("Youtube.Events.ManageEventsMissing", locale, togglePath))
                .Build();
            return new YoutubeNotificationVariant(embed, component, manageEventsWarning);
        }

        private MessageComponent BuildMessageComponent(string locale)
        {
            if (_botConfig.DisableNotificationsAds)
                return null;

            return new ComponentBuilder()
                .WithButton(_localizer.Get("Notifications.Button.RandomVideo", locale), style: ButtonStyle.Link,
                    emote: _emojiService.YouTubeEmote, url: "https://api.konnokai.me/randomvideo")
                .WithButton(_localizer.Get("Notifications.Button.SupportEcpay", locale), style: ButtonStyle.Link,
                    emote: _emojiService.ECPayEmote, url: Utility.ECPayUrl, row: 1)
                .WithButton(_localizer.Get("Notifications.Button.SupportPaypal", locale), style: ButtonStyle.Link,
                    emote: _emojiService.PayPalEmote, url: Utility.PaypalUrl, row: 1)
                .Build();
        }

        private static NoticeType? MapNoticeType(YoutubeNoticeType busNoticeType)
            => busNoticeType switch
            {
                YoutubeNoticeType.NewStream => NoticeType.NewStream,
                YoutubeNoticeType.NewVideo => NoticeType.NewVideo,
                YoutubeNoticeType.Start => NoticeType.Start,
                YoutubeNoticeType.End => NoticeType.End,
                YoutubeNoticeType.ChangeTime => NoticeType.ChangeTime,
                YoutubeNoticeType.Delete => NoticeType.Delete,
                _ => null,
            };

        private async Task SendStreamMessageAsync(TableVideo streamVideo, YoutubeNotification dto, NoticeType noticeType,
            NotificationDeliveryProgress progress)
        {
            if (!Bot.IsConnect)
                throw new InvalidOperationException("Discord 尚未就緒，保留通知等待重試。");

            NotificationMetricEvent metricEvent = NotifierMetrics.ToMetricEvent(dto.NoticeType);

            string type;
            switch (streamVideo.ChannelType)
            {
                case TableVideo.YTChannelType.Holo:
                    type = "holo";
                    break;
                case TableVideo.YTChannelType.Nijisanji:
                    type = "2434";
                    break;
                default:
                    type = "other";
                    break;
            }

            // 通知設定改讀記憶體快取（§12.3），降廣播 fan-out 下的 MySQL 壓力；快取為唯讀快照
            var allNotice = _noticeCache.Get();
            List<NoticeYoutubeStreamChannel> noticeYoutubeStreamChannels = new List<NoticeYoutubeStreamChannel>();
            using (var db = _dbService.GetDbContext())
            {
                // 已指定此頻道的通知設定不需依頻道類型篩選。
                noticeYoutubeStreamChannels.AddRange(allNotice.Where((x) => x.YouTubeChannelId == streamVideo.ChannelId));

                // 類型檢查：其他類型頻道必須未列入爬蟲清單，或已通過認可，才能加入類型通知。
                if (type != "other" || db.YoutubeChannelSpider.AsNoTracking()
                    .Where(x => x.ChannelId == streamVideo.ChannelId)
                    .Select(x => (bool?)x.IsTrustedChannel).FirstOrDefault() != false)
                {
                    noticeYoutubeStreamChannels.AddRange(allNotice.Where((x) => x.YouTubeChannelId == type));
                }

                Log.New($"發送 YouTube 通知 ({noticeYoutubeStreamChannels.Count(x => Bot.IsServerOnThisShard(x.GuildId))} / {noticeType}): {streamVideo.ChannelTitle} - {streamVideo.VideoTitle}");

#if DEBUG || DEBUG_DONTREGISTERCOMMAND
                return;
#endif

                var variants = new Dictionary<string, Lazy<YoutubeNotificationVariant>>(StringComparer.Ordinal);
                var coverBytes = new Lazy<Task<byte[]>>(
                    () => DownloadYoutubeCoverAsync(streamVideo.VideoId),
                    LazyThreadSafetyMode.ExecutionAndPublication);
                var guildsById = noticeYoutubeStreamChannels
                    .Select(item => item.GuildId)
                    .Distinct()
                    .Select(guildId => _client.GetGuild(guildId))
                    .Where(guild => guild != null)
                    .ToDictionary(guild => guild.Id);
                Dictionary<ulong, string> localesByGuildId = await _guildLocaleService.GetManyAsync(guildsById.Values);

                foreach (var item in noticeYoutubeStreamChannels)
                {
                    string target = $"{item.Id}:{(noticeType == NoticeType.NewVideo ? item.DiscordNoticeVideoChannelId : item.DiscordNoticeStreamChannelId)}";
                    if (progress.IsComplete(target))
                        continue;
                    NotificationDeliveryResult? deliveryResult = null;
                    Stopwatch deliveryStopwatch = null;
                    bool primaryMessageSent = false;
                    bool retryRequired = false;
                    try
                    {
                        if (!guildsById.TryGetValue(item.GuildId, out SocketGuild guild))
                        {
                            // 多 Shard 環境：非本 Shard 持有的伺服器，或尚未 Ready，皆靜默略過，避免互刪設定
                            if (!Bot.ShouldDeleteMissingGuild(item.GuildId))
                                continue;

                            Log.Warn($"YouTube 通知 ({streamVideo.VideoId}) | 找不到伺服器 {item.GuildId}");
                            deliveryResult = NotificationDeliveryResult.MissingGuild;
                            db.NoticeYoutubeStreamChannel.RemoveRange(db.NoticeYoutubeStreamChannel.Where((x) => x.GuildId == item.GuildId));
                            db.SaveChanges();
                            _noticeCache.Invalidate();
                            continue;
                        }

                        string locale = localesByGuildId[guild.Id];
                        if (!variants.TryGetValue(locale, out var variantValue))
                        {
                            variantValue = new Lazy<YoutubeNotificationVariant>(
                                () => BuildVariantForBus(dto, streamVideo, locale),
                                LazyThreadSafetyMode.ExecutionAndPublication);
                            variants.Add(locale, variantValue);
                        }
                        YoutubeNotificationVariant variant = variantValue.Value;

                        // 只有新影片會發到影片通知頻道，首播類的影片歸類在直播類型
                        // 原則上 DiscordNoticeVideoChannelId 預設會跟 DiscordNoticeStreamChannelId 一樣，不該為空
                        var channel = guild.GetTextChannel(noticeType == NoticeType.NewVideo ? item.DiscordNoticeVideoChannelId : item.DiscordNoticeStreamChannelId);
                        if (channel == null)
                        {
                            deliveryResult = NotificationDeliveryResult.MissingChannel;
                            continue;
                        }

                        // 如果是新直播的話就建立活動，或更改活動開始時間
                        try
                        {
                            if (item.IsCreateEventForNewStream)
                            {
                                await progress.RunAsync($"event:{target}", async () =>
                                {
                                    if (!guild.GetUser(_client.CurrentUser.Id).GuildPermissions.ManageEvents)
                                    {
                                        Log.Warn($"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} 無權限可建立活動，關閉此功能");
                                        // item 來自唯讀快取，不可 Attach/Update；以 ExecuteUpdate 依 PK 直接更新，避免跨 context 追蹤衝突
                                        db.NoticeYoutubeStreamChannel.Where((x) => x.Id == item.Id)
                                            .ExecuteUpdate((s) => s.SetProperty((p) => p.IsCreateEventForNewStream, false));
                                        _noticeCache.Invalidate();

                                        try
                                        {
                                            await Policy.Handle<TimeoutException>()
                                                .Or<Discord.Net.HttpException>((httpEx) => ((int)httpEx.HttpCode).ToString().StartsWith("50"))
                                                .WaitAndRetryAsync(3, (retryAttempt) =>
                                                {
                                                    var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                                                    Log.Warn($"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} / {channel.Id} 無權限提示發送失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                                                    return timeSpan;
                                                })
                                                .ExecuteAsync(async () =>
                                                {
                                                    await channel.SendMessageAsync(embed: variant.ManageEventsWarning);
                                                });
                                        }
                                        catch (Exception) { }
                                    }
                                    else
                                    {
                                        if (noticeType == NoticeType.NewStream)
                                        {
                                            Log.Info($"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} 嘗試建立活動");
                                            DateTime startTime = streamVideo.ScheduledStartTime;

                                            // 若預定開台時間在現在之後，就從現在時間往後推一分鐘
                                            // The start time for an event cannot be in the past (Parameter 'startTime')
                                            if (startTime <= DateTime.Now)
                                            {
                                                startTime = DateTime.Now.AddMinutes(1);
                                            }

                                            startTime = startTime.ToUniversalTime();

                                            await Policy.Handle<TimeoutException>()
                                                .Or<Discord.Net.HttpException>((httpEx) => ((int)httpEx.HttpCode).ToString().StartsWith("50"))
                                                .WaitAndRetryAsync(3, (retryAttempt) =>
                                                {
                                                    var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                                                    Log.Warn($"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} 建立活動失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                                                    return timeSpan;
                                                })
                                                .ExecuteAsync(async () =>
                                                {
                                                    byte[] bytes = await coverBytes.Value;
                                                    if (bytes == null)
                                                    {
                                                        await guild.CreateEventAsync(streamVideo.VideoTitle,
                                                            startTime, GuildScheduledEventType.External,
                                                            description: Format.Url(streamVideo.ChannelTitle, $"https://youtube.com/channel/{streamVideo.ChannelId}"),
                                                            endTime: startTime.AddHours(1),
                                                            location: $"https://youtube.com/watch?v={streamVideo.VideoId}");
                                                    }
                                                    else
                                                    {
                                                        using var coverStream = new MemoryStream(bytes, writable: false);
                                                        await guild.CreateEventAsync(streamVideo.VideoTitle,
                                                            startTime, GuildScheduledEventType.External,
                                                            description: Format.Url(streamVideo.ChannelTitle, $"https://youtube.com/channel/{streamVideo.ChannelId}"),
                                                            endTime: startTime.AddHours(1),
                                                            location: $"https://youtube.com/watch?v={streamVideo.VideoId}",
                                                            coverImage: new Image(coverStream));
                                                    }
                                                });
                                        }
                                        else if (noticeType == NoticeType.ChangeTime)
                                        {
                                            Log.Info($"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} 嘗試更改活動開始時間");
                                            await Policy.Handle<TimeoutException>()
                                                .Or<Discord.Net.HttpException>((httpEx) => ((int)httpEx.HttpCode).ToString().StartsWith("50"))
                                                .WaitAndRetryAsync(3, (retryAttempt) =>
                                                {
                                                    var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                                                    Log.Warn($"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} 更改活動時間失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                                                    return timeSpan;
                                                })
                                                .ExecuteAsync(async () =>
                                                {
                                                    var @event = (await guild.GetEventsAsync()).FirstOrDefault((x) => x.Creator.Id == _client.CurrentUser.Id && x.Location.EndsWith(streamVideo.VideoId));

                                                    if (@event == null)
                                                    {
                                                        Log.Warn($"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} 更改活動時間失敗，找不到對應的活動");
                                                    }
                                                    else
                                                    {
                                                        await @event.ModifyAsync((act) =>
                                                        {
                                                            act.Name = streamVideo.VideoTitle;
                                                            act.StartTime = (DateTimeOffset)streamVideo.ScheduledStartTime.ToUniversalTime();
                                                            act.EndTime = (DateTimeOffset)streamVideo.ScheduledStartTime.ToUniversalTime().AddHours(1);
                                                        });
                                                    }
                                                });
                                        }
                                    }
                                    return "1";
                                });
                            }
                        }
                        catch (Exception ex) when (NotificationTargetFailure.IsPermanent(ex))
                        {
                            // 活動權限不足或目標已不存在：關閉此設定的建立活動並繼續發送通知，不讓事件無限重試。
                            Log.Error(ex.Demystify(), $"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} 建立活動被拒，關閉此設定的建立活動");
                            db.NoticeYoutubeStreamChannel.Where((x) => x.Id == item.Id)
                                .ExecuteUpdate((s) => s.SetProperty((p) => p.IsCreateEventForNewStream, false));
                            _noticeCache.Invalidate();
                        }
                        catch (Exception ex)
                        {
                            retryRequired = true;
                            progress.Fail(ex);
                            Log.Error(ex.Demystify(), $"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} 建立活動失敗");
                        }

                        string sendMessage = "";
                        switch (noticeType)
                        {
                            case NoticeType.NewStream:
                                sendMessage = item.NewStreamMessage;
                                break;
                            case NoticeType.NewVideo:
                                sendMessage = item.NewVideoMessage;
                                break;
                            case NoticeType.Start:
                                sendMessage = item.StratMessage;
                                break;
                            case NoticeType.End:
                                sendMessage = item.EndMessage;
                                break;
                            case NoticeType.ChangeTime:
                                sendMessage = item.ChangeTimeMessage;
                                break;
                            case NoticeType.Delete:
                                sendMessage = item.DeleteMessage;
                                break;
                        }

                        if (sendMessage == "-")
                        {
                            deliveryResult = NotificationDeliveryResult.Disabled;
                            continue;
                        }

                        deliveryStopwatch = Stopwatch.StartNew();
                        await Policy.Handle<TimeoutException>()
                            .Or<Discord.Net.HttpException>((httpEx) => ((int)httpEx.HttpCode).ToString().StartsWith("50"))
                            .WaitAndRetryAsync(3, (retryAttempt) =>
                            {
                                _metrics.RecordNotificationDeliveryRetry(metricEvent);
                                var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                                Log.Warn($"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} / {channel.Id} 發送失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                                return timeSpan;
                            })
                            .ExecuteAsync(() => progress.SendAsync(target, channel, async () =>
                            {
                                var message = await channel.SendMessageAsync(text: sendMessage, embed: variant.Embed,
                                    components: variant.Component,
                                    options: new RequestOptions() { RetryMode = RetryMode.AlwaysRetry });
                                primaryMessageSent = true;
                                return message;
                            }, channel is INewsChannel && Utility.OfficialGuildList.Contains(guild.Id)));
                        deliveryResult = NotificationDeliveryResult.Sent;
                    }
                    catch (Discord.Net.HttpException httpEx)
                    {
                        if (Bot.TryShutdownOnDiscordAuthorizationFailure(httpEx, $"YouTube 通知 ({streamVideo.VideoId})"))
                        {
                            retryRequired = true;
                            deliveryResult = primaryMessageSent
                                ? NotificationDeliveryResult.Sent
                                : NotificationDeliveryResult.AuthorizationFailure;
                            throw;
                        }

                        if (NotificationTargetFailure.IsPermanent(httpEx))
                        {
                            deliveryResult = primaryMessageSent
                                ? NotificationDeliveryResult.Sent
                                : NotificationDeliveryResult.MissingPermission;

                            // 真正送不出去的是這次實際使用的目的地頻道（新影片用影片頻道，其餘用直播頻道）。
                            ulong failedChannelId = noticeType == NoticeType.NewVideo
                                ? item.DiscordNoticeVideoChannelId
                                : item.DiscordNoticeStreamChannelId;
                            Log.Warn($"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} / {failedChannelId} 永久失敗（權限或目標不存在）：{httpEx.DiscordCode}");

                            // 伺服器已不存在時清除整個伺服器的設定（與上方 MissingGuild 相同的跨 shard 防護）；
                            // 其餘永久失敗清除所有指向這個送不到的目的地頻道的設定。
                            if (NotificationTargetFailure.IsUnknownGuild(httpEx))
                            {
                                if (Bot.ShouldDeleteMissingGuild(item.GuildId))
                                {
                                    db.NoticeYoutubeStreamChannel.RemoveRange(db.NoticeYoutubeStreamChannel.Where((x) => x.GuildId == item.GuildId));
                                    db.SaveChanges();
                                    _noticeCache.Invalidate();
                                }
                            }
                            else
                            {
                                db.NoticeYoutubeStreamChannel.RemoveRange(db.NoticeYoutubeStreamChannel.Where(
                                    (x) => x.DiscordNoticeVideoChannelId == failedChannelId || x.DiscordNoticeStreamChannelId == failedChannelId));
                                db.SaveChanges();
                                _noticeCache.Invalidate();
                            }
                        }
                        else if (((int)httpEx.HttpCode).ToString().StartsWith("50"))
                        {
                            retryRequired = true;
                            progress.Fail(httpEx);
                            deliveryResult = primaryMessageSent
                                ? NotificationDeliveryResult.Sent
                                : NotificationDeliveryResult.Discord5xx;
                            Log.Warn($"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} / {item.DiscordNoticeVideoChannelId} Discord 5xx 錯誤：{httpEx.HttpCode}");
                        }
                        else
                        {
                            retryRequired = true;
                            progress.Fail(httpEx);
                            deliveryResult = primaryMessageSent
                                ? NotificationDeliveryResult.Sent
                                : NotificationDeliveryResult.UnknownError;
                            Log.Error(httpEx, $"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} / {item.DiscordNoticeVideoChannelId} Discord 未知錯誤");
                        }
                    }
                    catch (TimeoutException ex)
                    {
                        retryRequired = true;
                        progress.Fail(ex);
                        deliveryResult = primaryMessageSent
                            ? NotificationDeliveryResult.Sent
                            : NotificationDeliveryResult.Timeout;
                        Log.Warn($"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} / {item.DiscordNoticeVideoChannelId} Discord 逾時");
                    }
                    catch (Exception ex)
                    {
                        retryRequired = true;
                        progress.Fail(ex);
                        deliveryResult = primaryMessageSent
                            ? NotificationDeliveryResult.Sent
                            : NotificationDeliveryResult.UnknownError;
                        Log.Error(ex.Demystify(), $"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} / {item.DiscordNoticeVideoChannelId} 未知錯誤");
                    }
                    finally
                    {
                        if (deliveryStopwatch != null)
                        {
                            deliveryStopwatch.Stop();
                            _metrics.ObserveNotificationDeliveryDuration(metricEvent, deliveryStopwatch.Elapsed);
                        }

                        if (deliveryResult.HasValue)
                        {
                            _metrics.RecordNotificationDelivery(metricEvent, deliveryResult.Value);
                            if (!retryRequired)
                                await progress.CompleteAsync(target);
                        }
                    }
                }
            }
        }

        private static async Task<byte[]> DownloadYoutubeCoverAsync(string videoId)
        {
            string url = $"https://i.ytimg.com/vi/{videoId}/maxresdefault.jpg";
            Log.Info($"YouTube 通知 ({videoId}) | 嘗試下載封面: {url}");
            try
            {
                return await Policy.Handle<TimeoutException>()
                    .Or<Discord.Net.HttpException>((httpEx) => ((int)httpEx.HttpCode).ToString().StartsWith("50"))
                    .WaitAndRetryAsync(3, retryAttempt =>
                    {
                        var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                        Log.Warn($"YouTube 通知 ({videoId}) | 封面下載失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                        return timeSpan;
                    })
                    .ExecuteAsync(() => SharedHttpClient.GetByteArrayAsync(url));
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"YouTube 通知 ({videoId}) | 封面下載失敗，可能是找不到圖檔");
                return null;
            }
        }
        #endregion

        #region 伺服器橫幅變更（消費端套用，需 GetGuild）
        private async Task ChangeGuildBannerAsync(string channelId, string videoId)
        {
#if DEBUG || DEBUG_DONTREGISTERCOMMAND
            return;
#endif
            List<DataBase.Table.BannerChange> list;

            using (var db = _dbService.GetDbContext())
            {
                list = db.BannerChange.AsNoTracking()
                    .Where(x => x.ChannelId == channelId)
                    .ToList();
            }

            if (list.Count == 0) return;

            var bannerBytes = new Lazy<Task<BannerDownloadResult>>(async () =>
            {
                try
                {
                    byte[] bytes = await _httpClientFactory.CreateClient("")
                        .GetByteArrayAsync($"https://i.ytimg.com/vi/{videoId}/maxresdefault.jpg");
                    return new BannerDownloadResult(true, bytes.Length < 2048 ? null : bytes);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), $"DownloadGuildBanner: {channelId} / {videoId}");
                    return new BannerDownloadResult(false, null);
                }
            }, LazyThreadSafetyMode.ExecutionAndPublication);

            foreach (var item in list)
            {
                try
                {
                    var guild = _client.GetGuild(item.GuildId);
                    if (guild == null)
                    {
                        // 多 Shard 環境：非本 Shard 持有的伺服器，或尚未 Ready，皆靜默略過，避免互刪設定
                        if (!Bot.ShouldDeleteMissingGuild(item.GuildId))
                            continue;

                        Log.Warn($"Guild not found: {item.GuildId} / {channelId} / {videoId}");
                        using (var db = _dbService.GetDbContext())
                        {
                            db.BannerChange.Remove(item);
                            await db.SaveChangesAsync();
                        }
                        continue;
                    }

                    if (guild.PremiumTier < PremiumTier.Tier2) continue;

                    if (videoId != item.LastChangeStreamId)
                    {
                        BannerDownloadResult download = await bannerBytes.Value;
                        if (!download.IsSuccess)
                            continue;

                        try
                        {
                            if (download.Bytes != null)
                            {
                                using var memStream = new MemoryStream(download.Bytes, writable: false);
                                await guild.ModifyAsync(func => func.Banner = new Image(memStream));
                            }

                            item.LastChangeStreamId = videoId;

                            using (var db = _dbService.GetDbContext())
                            {
                                db.BannerChange.Update(item);
                                await db.SaveChangesAsync();
                            }

                            Log.Info("ChangeGuildBanner" + (download.Bytes == null ? "(Without Change)" : "") + $": {item.GuildId} / {videoId}");
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.Demystify(), $"ChangeGuildBanner - {item.GuildId}: {channelId} / {videoId}");
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), $"ChangeGuildBanner - {item.GuildId}");
                    continue;
                }
            }
        }
        #endregion

        private sealed record YoutubeNotificationVariant(
            Embed Embed,
            MessageComponent Component,
            Embed ManageEventsWarning);

        private sealed record BannerDownloadResult(bool IsSuccess, byte[] Bytes);
    }
}
