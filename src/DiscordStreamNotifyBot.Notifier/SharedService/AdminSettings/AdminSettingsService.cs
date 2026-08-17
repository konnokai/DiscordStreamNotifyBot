using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Interaction.Utility.Service;
using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.Twitcasting;
using DiscordStreamNotifyBot.SharedService.Twitch;
using DiscordStreamNotifyBot.SharedService.TwitchSubscription;
using DiscordStreamNotifyBot.SharedService.Youtube;
using DiscordStreamNotifyBot.SharedService.YoutubeMember;
using Newtonsoft.Json.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

#nullable enable

namespace DiscordStreamNotifyBot.SharedService.AdminSettings
{
    /// <summary>
    /// 接收 Backend 的網頁管理設定 request，由持有 guild 的 Notifier shard 建立快照或執行既有業務服務。
    /// </summary>
    public sealed class AdminSettingsService
    {
        internal enum RequestRoute
        {
            Ignore,
            UnsupportedVersion,
            UnsupportedAction,
            Snapshot,
            Command
        }

        private static readonly string[] Capabilities =
        [
            "guild.common",
            "youtube-notification",
            "twitch-notification",
            "twitcasting-notification",
            "youtube-crawler",
            "twitch-crawler",
            "twitcasting-crawler",
            "youtube-verification",
            "twitch-verification"
        ];

        private readonly DiscordSocketClient _client;
        private readonly MainDbService _dbService;
        private readonly GuildLocaleService _guildLocaleService;
        private readonly UtilityService _utilityService;
        private readonly YoutubeStreamService _youtubeService;
        private readonly TwitchService _twitchService;
        private readonly TwitcastingService _twitcastingService;
        private readonly YoutubeMemberService _youtubeMemberService;
        private readonly TwitchSubscriptionService _twitchSubscriptionService;
        private int _started;

        public AdminSettingsService(
            DiscordSocketClient client,
            MainDbService dbService,
            GuildLocaleService guildLocaleService,
            UtilityService utilityService,
            YoutubeStreamService youtubeService,
            TwitchService twitchService,
            TwitcastingService twitcastingService,
            YoutubeMemberService youtubeMemberService,
            TwitchSubscriptionService twitchSubscriptionService)
        {
            _client = client;
            _dbService = dbService;
            _guildLocaleService = guildLocaleService;
            _utilityService = utilityService;
            _youtubeService = youtubeService;
            _twitchService = twitchService;
            _twitcastingService = twitcastingService;
            _youtubeMemberService = youtubeMemberService;
            _twitchSubscriptionService = twitchSubscriptionService;
        }

        public void Start()
        {
            if (Interlocked.Exchange(ref _started, 1) != 0)
                return;

            Bot.RedisSub.Subscribe(
                new RedisChannel(RedisChannels.AdminSettings.SnapshotRequest, RedisChannel.PatternMode.Literal),
                (channel, value) => _ = HandleAsync(value.ToString(), true, GracefulShutdown.Token));
            Bot.RedisSub.Subscribe(
                new RedisChannel(RedisChannels.AdminSettings.CommandRequest, RedisChannel.PatternMode.Literal),
                (channel, value) => _ = HandleAsync(value.ToString(), false, GracefulShutdown.Token));
            Log.Info($"Shard {Bot.ShardId} 已啟動網頁管理設定 request/reply");
        }

        internal static RequestRoute Classify(
            AdminSettingsRequestEnvelope? request,
            Func<ulong, bool> guildExists,
            Func<ulong, bool> ownsGuild,
            out ulong guildId,
            out ulong actorUserId)
        {
            guildId = 0;
            actorUserId = 0;
            if (request == null ||
                !Guid.TryParseExact(request.CorrelationId, "N", out _) ||
                !ulong.TryParse(request.GuildId, NumberStyles.None, CultureInfo.InvariantCulture, out guildId) ||
                !ulong.TryParse(request.ActorUserId, NumberStyles.None, CultureInfo.InvariantCulture, out actorUserId) ||
                !guildExists(guildId) ||
                !ownsGuild(guildId))
                return RequestRoute.Ignore;

            if (request.ContractVersion != AdminSettingsContract.Version)
                return RequestRoute.UnsupportedVersion;
            if (!IsSupportedAction(request.Action))
                return RequestRoute.UnsupportedAction;
            return request.Action == AdminSettingsContract.SnapshotAction
                ? RequestRoute.Snapshot
                : RequestRoute.Command;
        }

        internal static bool IsSupportedAction(string? action)
            => action is AdminSettingsContract.SnapshotAction
                or AdminSettingsContract.SetLocaleAction
                or AdminSettingsContract.SetGlobalNoticeChannelAction
                or AdminSettingsContract.SetVerificationLogChannelAction
                or AdminSettingsContract.YoutubeUpsertAction
                or AdminSettingsContract.YoutubeRemoveAction
                or AdminSettingsContract.TwitchUpsertAction
                or AdminSettingsContract.TwitchRemoveAction
                or AdminSettingsContract.TwitcastingUpsertAction
                or AdminSettingsContract.TwitcastingRemoveAction
                or AdminSettingsContract.YoutubeCrawlerAddAction
                or AdminSettingsContract.YoutubeCrawlerRemoveAction
                or AdminSettingsContract.TwitchCrawlerAddAction
                or AdminSettingsContract.TwitchCrawlerRemoveAction
                or AdminSettingsContract.TwitcastingCrawlerAddAction
                or AdminSettingsContract.TwitcastingCrawlerRemoveAction
                or AdminSettingsContract.YoutubeVerificationUpsertAction
                or AdminSettingsContract.YoutubeVerificationRemoveAction
                or AdminSettingsContract.YoutubeVerificationSetProbeVideoAction
                or AdminSettingsContract.YoutubeVerificationAutomaticProbeAction
                or AdminSettingsContract.TwitchVerificationUpsertAction
                or AdminSettingsContract.TwitchVerificationRemoveAction;

        internal static bool TryParseChannelId(string? value, bool allowUnset, out ulong channelId)
        {
            channelId = 0;
            if (allowUnset && string.IsNullOrEmpty(value))
                return true;
            return ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out channelId) &&
                (allowUnset || channelId != 0);
        }

        internal static bool TryReadRequest(string json, [NotNullWhen(true)] out AdminSettingsRequestEnvelope? request)
        {
            request = null;
            try
            {
                var token = JObject.Parse(json);
                if (token["contractVersion"]?.Type != JTokenType.Integer ||
                    token["correlationId"]?.Type != JTokenType.String ||
                    token["guildId"]?.Type != JTokenType.String ||
                    token["actorUserId"]?.Type != JTokenType.String ||
                    token["action"]?.Type != JTokenType.String ||
                    token["payload"]?.Type is not (JTokenType.Object or JTokenType.Null))
                    return false;

                request = token.ToObject<AdminSettingsRequestEnvelope>();
                return request != null;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        internal static bool ValidYoutubeUpsertPayload(JObject payload)
            => HasString(payload, "source") && HasString(payload, "streamChannelId") &&
                HasString(payload, "videoChannelId") && payload["createEvent"]?.Type == JTokenType.Boolean &&
                payload["messages"] is JObject messages &&
                HasString(messages, "newStream", true) && HasString(messages, "newVideo", true) &&
                HasString(messages, "start", true) && HasString(messages, "end", true) &&
                HasString(messages, "changeTime", true) && HasString(messages, "delete", true);

        internal static bool ValidTwitchUpsertPayload(JObject payload)
            => HasString(payload, "source") && HasString(payload, "channelId") &&
                payload["messages"] is JObject messages &&
                HasString(messages, "start", true) && HasString(messages, "end", true) &&
                HasString(messages, "change", true);

        internal static bool ValidCrawlerOrVerificationPayload(string action, JObject payload)
            => action switch
            {
                AdminSettingsContract.YoutubeCrawlerAddAction or
                AdminSettingsContract.TwitchCrawlerAddAction or
                AdminSettingsContract.TwitcastingCrawlerAddAction => HasString(payload, "source"),
                AdminSettingsContract.YoutubeCrawlerRemoveAction or
                AdminSettingsContract.TwitchCrawlerRemoveAction or
                AdminSettingsContract.TwitcastingCrawlerRemoveAction or
                AdminSettingsContract.YoutubeVerificationRemoveAction or
                AdminSettingsContract.YoutubeVerificationAutomaticProbeAction or
                AdminSettingsContract.TwitchVerificationRemoveAction => HasString(payload, "sourceId"),
                AdminSettingsContract.YoutubeVerificationUpsertAction or
                AdminSettingsContract.TwitchVerificationUpsertAction => HasString(payload, "source") &&
                    HasString(payload, "roleId"),
                AdminSettingsContract.YoutubeVerificationSetProbeVideoAction => HasString(payload, "sourceId") &&
                    HasString(payload, "video"),
                _ => false
            };

        private async Task HandleAsync(string json, bool snapshotRequest, CancellationToken cancellationToken)
        {
            if (!TryReadRequest(json, out AdminSettingsRequestEnvelope? request))
            {
                Log.Warn("忽略無法解析的網頁管理設定 request");
                return;
            }

            RequestRoute route = Classify(
                request,
                guildId => _client.GetGuild(guildId) != null,
                Bot.IsServerOnThisShard,
                out ulong guildId,
                out ulong actorUserId);
            if (route == RequestRoute.Ignore || request == null)
                return;

            var guild = _client.GetGuild(guildId);
            try
            {
                if (route == RequestRoute.UnsupportedVersion)
                {
                    await PublishResultAsync(request, AdminSettingsMutationResult.Rejected("settings.unsupported-version"),
                        guildId, actorUserId, cancellationToken);
                    return;
                }

                if (route == RequestRoute.UnsupportedAction || snapshotRequest != (route == RequestRoute.Snapshot))
                {
                    await PublishResultAsync(request, AdminSettingsMutationResult.Rejected("settings.unsupported-action"),
                        guildId, actorUserId, cancellationToken);
                    return;
                }

                if (route == RequestRoute.Snapshot)
                {
                    var snapshot = await BuildSnapshotAsync(guild, cancellationToken);
                    await PublishResponseAsync(request, snapshot, guildId, actorUserId,
                        "applied", "settings.snapshot", cancellationToken);
                    return;
                }

                AdminSettingsMutationResult result = await DispatchCommandAsync(
                    request.Action,
                    request.Payload,
                    guild,
                    actorUserId,
                    cancellationToken);
                await PublishResultAsync(request, result, guildId, actorUserId, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // 關閉期間不再延長 request 生命週期。
                LogResult(request, guildId, actorUserId, "rejected", "settings.cancelled");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(),
                    $"網頁管理設定執行失敗 | CorrelationId: {request.CorrelationId} | Guild: {guildId} | Actor: {actorUserId} | Action: {request.Action}");
                var result = AdminSettingsMutationResult.Rejected("settings.operation-failed");
                await PublishResultAsync(request, result, guildId, actorUserId, cancellationToken);
            }
        }

        private async Task<AdminSettingsMutationResult> DispatchCommandAsync(
            string action,
            JObject? payload,
            SocketGuild guild,
            ulong actorUserId,
            CancellationToken cancellationToken)
        {
            if (payload == null)
                return InvalidPayload();

            switch (action)
            {
                case AdminSettingsContract.SetLocaleAction:
                    if (!HasString(payload, "locale") ||
                        !TryPayload(payload, out AdminSetLocalePayload? localePayload) ||
                        string.IsNullOrWhiteSpace(localePayload.Locale))
                        return InvalidPayload();
                    return await _utilityService.SetLocaleAsync(guild.Id, localePayload.Locale, cancellationToken);

                case AdminSettingsContract.SetGlobalNoticeChannelAction:
                    if (!HasString(payload, "channelId", true) ||
                        !TryPayload(payload, out AdminSetChannelPayload? globalPayload) ||
                        !TryParseChannelId(globalPayload.ChannelId, true, out ulong globalChannelId))
                        return InvalidPayload();
                    return await _utilityService.SetGlobalNoticeChannelAsync(guild, globalChannelId, cancellationToken);

                case AdminSettingsContract.SetVerificationLogChannelAction:
                    if (!HasString(payload, "channelId", true) ||
                        !TryPayload(payload, out AdminSetChannelPayload? verificationPayload) ||
                        !TryParseChannelId(verificationPayload.ChannelId, true, out ulong verificationChannelId))
                        return InvalidPayload();
                    return await _utilityService.SetVerificationLogChannelAsync(guild, verificationChannelId, cancellationToken);

                case AdminSettingsContract.YoutubeUpsertAction:
                    if (!ValidYoutubeUpsertPayload(payload) ||
                        !TryPayload(payload, out AdminYoutubeUpsertPayload? youtubePayload) ||
                        string.IsNullOrWhiteSpace(youtubePayload.Source) ||
                        !TryParseChannelId(youtubePayload.StreamChannelId, false, out ulong streamChannelId) ||
                        !TryParseChannelId(youtubePayload.VideoChannelId, false, out ulong videoChannelId) ||
                        !youtubePayload.CreateEvent.HasValue || !Valid(youtubePayload.Messages))
                        return InvalidPayload();
                    return await _youtubeService.UpsertNotificationAsync(
                        guild,
                        youtubePayload.Source,
                        streamChannelId,
                        videoChannelId,
                        youtubePayload.CreateEvent.Value,
                        new AdminSettingsYoutubeMessages
                        {
                            NewStream = youtubePayload.Messages!.NewStream!,
                            NewVideo = youtubePayload.Messages.NewVideo!,
                            Start = youtubePayload.Messages.Start!,
                            End = youtubePayload.Messages.End!,
                            ChangeTime = youtubePayload.Messages.ChangeTime!,
                            Delete = youtubePayload.Messages.Delete!
                        },
                        cancellationToken);

                case AdminSettingsContract.YoutubeRemoveAction:
                    if (!HasString(payload, "source") ||
                        !TryPayload(payload, out AdminRemoveNotificationPayload? youtubeRemove) ||
                        string.IsNullOrWhiteSpace(youtubeRemove.Source))
                        return InvalidPayload();
                    return await _youtubeService.RemoveNotificationAsync(guild.Id, youtubeRemove.Source, cancellationToken);

                case AdminSettingsContract.TwitchUpsertAction:
                    if (!ValidTwitchUpsertPayload(payload) ||
                        !TryPayload(payload, out AdminTwitchUpsertPayload? twitchPayload) ||
                        string.IsNullOrWhiteSpace(twitchPayload.Source) ||
                        !TryParseChannelId(twitchPayload.ChannelId, false, out ulong twitchChannelId) ||
                        !Valid(twitchPayload.Messages))
                        return InvalidPayload();
                    return await _twitchService.UpsertNotificationAsync(
                        guild,
                        twitchPayload.Source,
                        twitchChannelId,
                        new AdminSettingsTwitchMessages
                        {
                            Start = twitchPayload.Messages!.Start!,
                            End = twitchPayload.Messages.End!,
                            Change = twitchPayload.Messages.Change!
                        },
                        cancellationToken);

                case AdminSettingsContract.TwitchRemoveAction:
                    if (!HasString(payload, "source") ||
                        !TryPayload(payload, out AdminRemoveNotificationPayload? twitchRemove) ||
                        string.IsNullOrWhiteSpace(twitchRemove.Source))
                        return InvalidPayload();
                    return await _twitchService.RemoveNotificationAsync(guild.Id, twitchRemove.Source, cancellationToken);

                case AdminSettingsContract.TwitcastingUpsertAction:
                    if (!HasString(payload, "source") || !HasString(payload, "channelId") ||
                        !HasString(payload, "startMessage", true) ||
                        !TryPayload(payload, out AdminTwitcastingUpsertPayload? twitcastingPayload) ||
                        string.IsNullOrWhiteSpace(twitcastingPayload.Source) ||
                        twitcastingPayload.StartMessage == null ||
                        !TryParseChannelId(twitcastingPayload.ChannelId, false, out ulong twitcastingChannelId))
                        return InvalidPayload();
                    return await _twitcastingService.UpsertNotificationAsync(
                        guild,
                        twitcastingPayload.Source,
                        twitcastingChannelId,
                        twitcastingPayload.StartMessage,
                        cancellationToken);

                case AdminSettingsContract.TwitcastingRemoveAction:
                    if (!HasString(payload, "source") ||
                        !TryPayload(payload, out AdminRemoveNotificationPayload? twitcastingRemove) ||
                        string.IsNullOrWhiteSpace(twitcastingRemove.Source))
                        return InvalidPayload();
                    return await _twitcastingService.RemoveNotificationAsync(guild.Id, twitcastingRemove.Source, cancellationToken);

                case AdminSettingsContract.YoutubeCrawlerAddAction:
                case AdminSettingsContract.TwitchCrawlerAddAction:
                case AdminSettingsContract.TwitcastingCrawlerAddAction:
                    if (!HasString(payload, "source") || !TryPayload(payload, out AdminSourcePayload? addCrawler) ||
                        string.IsNullOrWhiteSpace(addCrawler.Source))
                        return InvalidPayload();
                    return action switch
                    {
                        AdminSettingsContract.YoutubeCrawlerAddAction => await _youtubeService.AddCrawlerAsync(
                            guild, actorUserId, addCrawler.Source, cancellationToken),
                        AdminSettingsContract.TwitchCrawlerAddAction => await _twitchService.AddCrawlerAsync(
                            guild, actorUserId, addCrawler.Source, cancellationToken),
                        _ => await _twitcastingService.AddCrawlerAsync(
                            guild, actorUserId, addCrawler.Source, cancellationToken)
                    };

                case AdminSettingsContract.YoutubeCrawlerRemoveAction:
                case AdminSettingsContract.TwitchCrawlerRemoveAction:
                case AdminSettingsContract.TwitcastingCrawlerRemoveAction:
                    if (!HasString(payload, "sourceId") || !TryPayload(payload, out AdminSourceIdPayload? removeCrawler) ||
                        string.IsNullOrWhiteSpace(removeCrawler.SourceId))
                        return InvalidPayload();
                    return action switch
                    {
                        AdminSettingsContract.YoutubeCrawlerRemoveAction => await _youtubeService.RemoveCrawlerAsync(
                            guild.Id, removeCrawler.SourceId, cancellationToken),
                        AdminSettingsContract.TwitchCrawlerRemoveAction => await _twitchService.RemoveCrawlerAsync(
                            guild.Id, removeCrawler.SourceId, cancellationToken),
                        _ => await _twitcastingService.RemoveCrawlerAsync(
                            guild.Id, removeCrawler.SourceId, cancellationToken)
                    };

                case AdminSettingsContract.YoutubeVerificationUpsertAction:
                case AdminSettingsContract.TwitchVerificationUpsertAction:
                    if (!HasString(payload, "source") || !HasString(payload, "roleId") ||
                        !TryPayload(payload, out AdminVerificationUpsertPayload? verification) ||
                        string.IsNullOrWhiteSpace(verification.Source) ||
                        !TryParseChannelId(verification.RoleId, false, out ulong roleId))
                        return InvalidPayload();
                    return action == AdminSettingsContract.YoutubeVerificationUpsertAction
                        ? await _youtubeMemberService.ConfigureAsync(guild, actorUserId, verification.Source, roleId, cancellationToken)
                        : await _twitchSubscriptionService.ConfigureAsync(guild, verification.Source, roleId, cancellationToken);

                case AdminSettingsContract.YoutubeVerificationRemoveAction:
                case AdminSettingsContract.TwitchVerificationRemoveAction:
                case AdminSettingsContract.YoutubeVerificationAutomaticProbeAction:
                    if (!HasString(payload, "sourceId") || !TryPayload(payload, out AdminSourceIdPayload? verificationSource) ||
                        string.IsNullOrWhiteSpace(verificationSource.SourceId))
                        return InvalidPayload();
                    if (action == AdminSettingsContract.YoutubeVerificationRemoveAction)
                        return await _youtubeMemberService.RemoveConfigurationAsync(guild.Id, verificationSource.SourceId, cancellationToken);
                    if (action == AdminSettingsContract.TwitchVerificationRemoveAction)
                        return await _twitchSubscriptionService.RemoveConfigurationAsync(guild.Id, verificationSource.SourceId, cancellationToken);
                    return await _youtubeMemberService.UseAutomaticProbeAsync(guild.Id, verificationSource.SourceId, cancellationToken);

                case AdminSettingsContract.YoutubeVerificationSetProbeVideoAction:
                    if (!HasString(payload, "sourceId") || !HasString(payload, "video") ||
                        !TryPayload(payload, out AdminProbeVideoPayload? probe) ||
                        string.IsNullOrWhiteSpace(probe.SourceId) || string.IsNullOrWhiteSpace(probe.Video))
                        return InvalidPayload();
                    return await _youtubeMemberService.SetProbeVideoAsync(
                        guild.Id, probe.SourceId, probe.Video, cancellationToken);

                default:
                    return AdminSettingsMutationResult.Rejected("settings.unsupported-action");
            }
        }

        private async Task<AdminSettingsSnapshot> BuildSnapshotAsync(
            SocketGuild guild,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            using var db = _dbService.GetDbContext();
            var config = await db.GuildConfig.AsNoTracking()
                .FirstOrDefaultAsync(x => x.GuildId == guild.Id, cancellationToken);
            var youtube = await db.NoticeYoutubeStreamChannel.AsNoTracking()
                .Where(x => x.GuildId == guild.Id)
                .ToListAsync(cancellationToken);
            var twitch = await db.NoticeTwitchStreamChannels.AsNoTracking()
                .Where(x => x.GuildId == guild.Id)
                .ToListAsync(cancellationToken);
            var twitcasting = await db.NoticeTwitcastingStreamChannels.AsNoTracking()
                .Where(x => x.GuildId == guild.Id)
                .ToListAsync(cancellationToken);
            var youtubeSpiders = await db.YoutubeChannelSpider.AsNoTracking()
                .Select(x => x.ChannelId)
                .ToListAsync(cancellationToken);
            var twitchSpiders = await db.TwitchSpider.AsNoTracking()
                .ToDictionaryAsync(x => x.UserId, x => x.UserName, cancellationToken);
            var twitcastingSpiders = (await db.TwitcastingSpider.AsNoTracking().ToListAsync(cancellationToken))
                .GroupBy(x => x.ScreenId)
                .ToDictionary(x => x.Key, x => x.First().ChannelTitle);
            var ownedYoutubeSpiders = await db.YoutubeChannelSpider.AsNoTracking()
                .Where(x => x.GuildId == guild.Id).ToListAsync(cancellationToken);
            var ownedTwitchSpiders = await db.TwitchSpider.AsNoTracking()
                .Where(x => x.GuildId == guild.Id).ToListAsync(cancellationToken);
            var ownedTwitcastingSpiders = await db.TwitcastingSpider.AsNoTracking()
                .Where(x => x.GuildId == guild.Id).ToListAsync(cancellationToken);
            var youtubeVerification = await db.GuildYoutubeMemberConfig.AsNoTracking()
                .Where(x => x.GuildId == guild.Id).ToListAsync(cancellationToken);
            var twitchVerification = await db.GuildTwitchSubscriptionConfig.AsNoTracking()
                .Where(x => x.GuildId == guild.Id).ToListAsync(cancellationToken);
            var youtubeVerifiedCounts = await db.YoutubeMemberCheck.AsNoTracking()
                .Where(x => x.GuildId == guild.Id && x.IsChecked)
                .GroupBy(x => x.CheckYTChannelId)
                .ToDictionaryAsync(x => x.Key, x => x.Count(), cancellationToken);
            var youtubePendingCounts = await db.YoutubeMemberCheck.AsNoTracking()
                .Where(x => x.GuildId == guild.Id && x.PendingRoleRemoval)
                .GroupBy(x => x.CheckYTChannelId)
                .ToDictionaryAsync(x => x.Key, x => x.Count(), cancellationToken);
            var twitchVerifiedCounts = await db.TwitchSubscriptionCheck.AsNoTracking()
                .Where(x => x.GuildId == guild.Id && x.IsChecked)
                .GroupBy(x => x.BroadcasterId)
                .ToDictionaryAsync(x => x.Key, x => x.Count(), cancellationToken);
            var twitchPendingCounts = await db.TwitchSubscriptionCheck.AsNoTracking()
                .Where(x => x.GuildId == guild.Id && x.PendingRoleRemoval)
                .GroupBy(x => x.BroadcasterId)
                .ToDictionaryAsync(x => x.Key, x => x.Count(), cancellationToken);
            int youtubeCrawlerLimit = await YoutubeStreamService.GetYoutubeCrawlerLimitAsync(db, guild.Id, cancellationToken);
            int twitchCrawlerLimit = await TwitchService.GetTwitchCrawlerLimitAsync(db, guild.Id, cancellationToken);
            int twitcastingCrawlerLimit = await TwitcastingService.GetTwitcastingCrawlerLimitAsync(db, guild.Id, cancellationToken);

            foreach (string sourceId in twitch.Select(x => x.NoticeTwitchUserId).Distinct())
            {
                if (twitchSpiders.GetValueOrDefault(sourceId, sourceId) != sourceId || !_twitchService.IsEnable)
                    continue;

                try
                {
                    var user = await _twitchService.GetUserAsync(twitchUserId: sourceId).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(user?.DisplayName))
                        twitchSpiders[sourceId] = user.DisplayName;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Log.Warn($"網頁管理設定無法取得 Twitch 頻道名稱: {sourceId} / {ex.GetType().Name}");
                }
            }

            foreach (string sourceId in twitcasting.Select(x => x.ScreenId).Distinct())
            {
                if (twitcastingSpiders.GetValueOrDefault(sourceId, sourceId) != sourceId || !_twitcastingService.IsEnable)
                    continue;

                string? sourceName = await _twitcastingService.GetChannelTitleAsync(sourceId).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(sourceName))
                    twitcastingSpiders[sourceId] = sourceName;
            }
            var youtubeSourceNames = new Dictionary<string, string>();
            foreach (string sourceId in youtube.Select(x => x.YouTubeChannelId).Distinct())
            {
                string sourceName = GetYoutubeSourceName(db, sourceId);
                if (sourceName == sourceId)
                {
                    string apiSourceName = await _youtubeService.GetChannelTitle(sourceId).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(apiSourceName))
                        sourceName = apiSourceName;
                }
                youtubeSourceNames[sourceId] = sourceName;
            }

            var botUser = guild.GetUser(_client.CurrentUser.Id);
            var channels = guild.Channels
                .Where(channel => channel.ChannelType is ChannelType.Text or ChannelType.News)
                .OrderBy(channel => channel.Position)
                .ThenBy(channel => channel.Name, StringComparer.OrdinalIgnoreCase)
                .Select(channel =>
                {
                    ChannelPermissions permissions = botUser?.GetPermissions(channel) ?? default;
                    return new AdminSettingsChannel
                    {
                        Id = Id(channel.Id),
                        Name = channel.Name,
                        Type = channel.ChannelType == ChannelType.News ? "news" : "text",
                        CanView = permissions.ViewChannel,
                        CanSendMessages = permissions.SendMessages,
                        CanEmbedLinks = permissions.EmbedLinks,
                        CanManageEvents = botUser?.GuildPermissions.ManageEvents ?? false
                    };
                })
                .ToList();
            int botTopRolePosition = botUser?.Roles.Max(x => x.Position) ?? -1;
            bool canManageRoles = botUser?.GuildPermissions.ManageRoles == true;
            var roles = guild.Roles.OrderByDescending(role => role.Position).Select(role => new AdminSettingsRole
            {
                Id = Id(role.Id),
                Name = role.Name,
                Position = role.Position,
                Managed = role.IsManaged,
                Everyone = role.Id == guild.EveryoneRole.Id,
                BotCanManage = CanManageRole(canManageRoles, role.Id == guild.EveryoneRole.Id,
                    role.IsManaged, role.Position, botTopRolePosition)
            }).ToList();

            var snapshot = new AdminSettingsSnapshot
            {
                Capabilities = [.. Capabilities],
                Guild = new AdminSettingsGuild { Id = Id(guild.Id), Name = guild.Name, MemberCount = guild.MemberCount },
                Health = new AdminSettingsHealth
                {
                    BotConnected = Bot.IsConnect && _client.ConnectionState == ConnectionState.Connected
                },
                Resources = new AdminSettingsResources { Channels = channels, Roles = roles },
                Common = new AdminSettingsCommon
                {
                    Locale = await _guildLocaleService.GetAsync(guild.Id, guild),
                    GlobalNoticeChannelId = OptionalId(config?.NoticeChannelId ?? 0),
                    VerificationLogChannelId = OptionalId(config?.VerificationLogChannelId ?? 0)
                },
                Notifications = new AdminSettingsNotifications
                {
                    Youtube = youtube.Select(item => new AdminSettingsYoutubeNotification
                    {
                        SourceId = item.YouTubeChannelId,
                        SourceName = youtubeSourceNames[item.YouTubeChannelId],
                        StreamChannelId = OptionalId(item.DiscordNoticeStreamChannelId),
                        VideoChannelId = OptionalId(item.DiscordNoticeVideoChannelId),
                        CreateEvent = item.IsCreateEventForNewStream,
                        Messages = new AdminSettingsYoutubeMessages
                        {
                            NewStream = item.NewStreamMessage ?? "",
                            NewVideo = item.NewVideoMessage ?? "",
                            Start = item.StratMessage ?? "",
                            End = item.EndMessage ?? "",
                            ChangeTime = item.ChangeTimeMessage ?? "",
                            Delete = item.DeleteMessage ?? ""
                        },
                        DetectionEnabled = item.YouTubeChannelId is "holo" or "2434" or "other" ||
                            youtubeSpiders.Contains(item.YouTubeChannelId) ||
                            SharedExtensions.IsChannelInDb(item.YouTubeChannelId)
                    }).ToList(),
                    Twitch = twitch.Select(item => new AdminSettingsTwitchNotification
                    {
                        SourceId = item.NoticeTwitchUserId,
                        SourceName = twitchSpiders.GetValueOrDefault(item.NoticeTwitchUserId, item.NoticeTwitchUserId),
                        ChannelId = OptionalId(item.DiscordChannelId),
                        Messages = new AdminSettingsTwitchMessages
                        {
                            Start = item.StartStreamMessage ?? "",
                            End = item.EndStreamMessage ?? "",
                            Change = item.ChangeStreamDataMessage ?? ""
                        },
                        DetectionEnabled = twitchSpiders.ContainsKey(item.NoticeTwitchUserId)
                    }).ToList(),
                    Twitcasting = twitcasting.Select(item => new AdminSettingsTwitcastingNotification
                    {
                        SourceId = item.ScreenId,
                        SourceName = twitcastingSpiders.GetValueOrDefault(item.ScreenId, item.ScreenId),
                        ChannelId = OptionalId(item.DiscordChannelId),
                        StartMessage = item.StartStreamMessage ?? "",
                        DetectionEnabled = twitcastingSpiders.ContainsKey(item.ScreenId)
                    }).ToList()
                },
                Crawlers = new AdminSettingsCrawlers
                {
                    Youtube = CrawlerPlatform(true, youtubeCrawlerLimit,
                        ownedYoutubeSpiders.Select(x => (x.ChannelId, x.ChannelTitle))),
                    Twitch = CrawlerPlatform(_twitchService.IsEnable, twitchCrawlerLimit,
                        ownedTwitchSpiders.Select(x => (x.UserId, x.UserName))),
                    Twitcasting = CrawlerPlatform(_twitcastingService.IsEnable, twitcastingCrawlerLimit,
                        ownedTwitcastingSpiders.Select(x => (x.ScreenId, x.ChannelTitle)))
                },
                Verification = new AdminSettingsVerification
                {
                    Youtube = youtubeVerification.Select(x => new AdminSettingsYoutubeVerification
                    {
                        SourceId = x.MemberCheckChannelId,
                        SourceName = string.IsNullOrWhiteSpace(x.MemberCheckChannelTitle)
                            ? x.MemberCheckChannelId : x.MemberCheckChannelTitle,
                        RoleId = Id(x.MemberCheckGrantRoleId),
                        PreviousRoleId = NullableId(x.PreviousMemberCheckGrantRoleId),
                        DeletionPending = x.DeletionPending,
                        ProbeMode = x.IsManualVideoId ? "manual" : "automatic",
                        ProbeVideoId = x.MemberCheckVideoId,
                        VerifiedMemberCount = youtubeVerifiedCounts.GetValueOrDefault(x.MemberCheckChannelId),
                        PendingRoleRemovalCount = youtubePendingCounts.GetValueOrDefault(x.MemberCheckChannelId)
                    }).ToList(),
                    Twitch = twitchVerification.Select(x => new AdminSettingsTwitchVerification
                    {
                        SourceId = x.BroadcasterId,
                        SourceLogin = x.BroadcasterLogin,
                        SourceName = x.BroadcasterDisplayName,
                        SubscriberRoleId = Id(x.SubscriberRoleId),
                        PreviousSubscriberRoleId = NullableId(x.PreviousSubscriberRoleId),
                        TierRoleIds = new Dictionary<string, string>
                        {
                            ["1000"] = Id(x.Tier1RoleId),
                            ["2000"] = Id(x.Tier2RoleId),
                            ["3000"] = Id(x.Tier3RoleId)
                        },
                        DeletionPending = x.DeletionPending,
                        VerifiedMemberCount = twitchVerifiedCounts.GetValueOrDefault(x.BroadcasterId),
                        PendingRoleRemovalCount = twitchPendingCounts.GetValueOrDefault(x.BroadcasterId)
                    }).ToList()
                }
            };
            stopwatch.Stop();
            Log.Info("網頁管理設定快照處理完成 | Guild: {GuildId} | ElapsedMs: {ElapsedMs}",
                guild.Id, stopwatch.ElapsedMilliseconds);
            return snapshot;
        }

        private static AdminSettingsCrawlerPlatform CrawlerPlatform(
            bool enabled,
            int limit,
            IEnumerable<(string Id, string Name)> items)
        {
            var result = items.Select(x => new AdminSettingsCrawlerItem
            {
                SourceId = x.Id,
                SourceName = x.Name
            }).ToList();
            return new AdminSettingsCrawlerPlatform
            {
                Enabled = enabled,
                Count = result.Count,
                Limit = limit,
                Items = result
            };
        }

        internal static bool CanManageRole(
            bool hasManageRoles,
            bool everyone,
            bool managed,
            int rolePosition,
            int botTopRolePosition)
            => hasManageRoles && !everyone && !managed && rolePosition < botTopRolePosition;

        private static string GetYoutubeSourceName(MainDbContext db, string sourceId)
            => sourceId switch
            {
                "holo" => "Hololive",
                "2434" => "Nijisanji",
                "other" => "Other",
                _ => db.GetYoutubeChannelTitleByChannelId(sourceId)
            };

        private Task PublishResultAsync(
            AdminSettingsRequestEnvelope request,
            AdminSettingsMutationResult result,
            ulong guildId,
            ulong actorUserId,
            CancellationToken cancellationToken)
            => PublishResponseAsync(request, new AdminSettingsCommandReply
            {
                CorrelationId = request.CorrelationId,
                ShardId = Bot.ShardId,
                State = result.State,
                Code = result.Code,
                Arguments = result.Arguments
            }, guildId, actorUserId, result.State, result.Code, cancellationToken);

        private static async Task PublishResponseAsync(
            AdminSettingsRequestEnvelope request,
            object response,
            ulong guildId,
            ulong actorUserId,
            string state,
            string code,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await PublishAsync(request.CorrelationId, response);
                LogResult(request, guildId, actorUserId, state, code);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(),
                    $"網頁管理設定回覆發送失敗 | CorrelationId: {request.CorrelationId} | Guild: {guildId} | Actor: {actorUserId} | Action: {request.Action} | State: {state} | Code: {code}");
            }
        }

        private static Task PublishAsync(string correlationId, object value)
            => Bot.RedisSub.PublishAsync(
                new RedisChannel(RedisChannels.AdminSettings.Reply(correlationId), RedisChannel.PatternMode.Literal),
                JsonConvert.SerializeObject(value));

        private static bool TryPayload<T>(JObject payload, [NotNullWhen(true)] out T? value) where T : class
        {
            try
            {
                value = payload.ToObject<T>();
                return value != null;
            }
            catch (JsonException)
            {
                value = null;
                return false;
            }
        }

        internal static bool Valid(AdminYoutubeMessagesPayload? messages)
            => messages?.NewStream != null && messages.NewVideo != null && messages.Start != null &&
                messages.End != null && messages.ChangeTime != null && messages.Delete != null;

        internal static bool Valid(AdminTwitchMessagesPayload? messages)
            => messages?.Start != null && messages.End != null && messages.Change != null;

        private static bool HasString(JObject value, string property, bool allowEmpty = false)
            => value[property]?.Type == JTokenType.String &&
                (allowEmpty || !string.IsNullOrWhiteSpace(value.Value<string>(property)));

        private static AdminSettingsMutationResult InvalidPayload()
            => AdminSettingsMutationResult.Rejected("settings.invalid-payload");

        private static string Id(ulong value) => value.ToString(CultureInfo.InvariantCulture);

        private static string OptionalId(ulong value) => value == 0 ? "" : Id(value);

        private static string? NullableId(ulong? value) => value.HasValue ? Id(value.Value) : null;

        private static void LogResult(
            AdminSettingsRequestEnvelope request,
            ulong guildId,
            ulong actorUserId,
            string state,
            string code)
            => Log.Info(
                $"網頁管理設定 | CorrelationId: {request.CorrelationId} | Guild: {guildId} | Actor: {actorUserId} | Action: {request.Action} | State: {state} | Code: {code}");
    }
}
