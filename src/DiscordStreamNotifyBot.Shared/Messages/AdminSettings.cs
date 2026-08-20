using Newtonsoft.Json.Linq;

#nullable enable

namespace DiscordStreamNotifyBot.Shared.Messages
{
    public static class AdminSettingsContract
    {
        public const int Version = 1;

        public const string SnapshotAction = "settings.snapshot";
        public const string SetLocaleAction = "guild.set-locale";
        public const string SetGlobalNoticeChannelAction = "guild.set-global-notice-channel";
        public const string SetVerificationLogChannelAction = "guild.set-verification-log-channel";
        public const string YoutubeUpsertAction = "youtube-notification.upsert";
        public const string YoutubeRemoveAction = "youtube-notification.remove";
        public const string TwitchUpsertAction = "twitch-notification.upsert";
        public const string TwitchRemoveAction = "twitch-notification.remove";
        public const string TwitcastingUpsertAction = "twitcasting-notification.upsert";
        public const string TwitcastingRemoveAction = "twitcasting-notification.remove";
        public const string YoutubeCrawlerAddAction = "youtube-crawler.add";
        public const string YoutubeCrawlerRemoveAction = "youtube-crawler.remove";
        public const string TwitchCrawlerAddAction = "twitch-crawler.add";
        public const string TwitchCrawlerRemoveAction = "twitch-crawler.remove";
        public const string TwitcastingCrawlerAddAction = "twitcasting-crawler.add";
        public const string TwitcastingCrawlerRemoveAction = "twitcasting-crawler.remove";
        public const string YoutubeVerificationUpsertAction = "youtube-verification.upsert";
        public const string YoutubeVerificationRemoveAction = "youtube-verification.remove";
        public const string YoutubeVerificationSetProbeVideoAction = "youtube-verification.set-probe-video";
        public const string YoutubeVerificationAutomaticProbeAction = "youtube-verification.use-automatic-probe";
        public const string TwitchVerificationUpsertAction = "twitch-verification.upsert";
        public const string TwitchVerificationRemoveAction = "twitch-verification.remove";
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSettingsRequestEnvelope
    {
        [JsonProperty("contractVersion")]
        public int ContractVersion { get; set; }

        [JsonProperty("correlationId")]
        public string CorrelationId { get; set; } = "";

        [JsonProperty("guildId")]
        public string GuildId { get; set; } = "";

        [JsonProperty("actorUserId")]
        public string ActorUserId { get; set; } = "";

        [JsonProperty("deadlineUnixMs")]
        public long DeadlineUnixMs { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; } = "";

        [JsonProperty("payload")]
        public JObject Payload { get; set; } = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSettingsCommandReply
    {
        [JsonProperty("contractVersion")]
        public int ContractVersion { get; set; } = AdminSettingsContract.Version;

        [JsonProperty("correlationId")]
        public string CorrelationId { get; set; } = "";

        [JsonProperty("shardId")]
        public int ShardId { get; set; }

        [JsonProperty("state")]
        public string State { get; set; } = "";

        [JsonProperty("code")]
        public string Code { get; set; } = "";

        [JsonProperty("arguments")]
        public JObject Arguments { get; set; } = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSettingsMutationResult
    {
        [JsonProperty("state")]
        public string State { get; init; } = "";

        [JsonProperty("code")]
        public string Code { get; init; } = "";

        [JsonProperty("arguments")]
        public JObject Arguments { get; init; } = new();

        public static AdminSettingsMutationResult Applied(string code = "settings.updated", JObject? arguments = null)
            => new() { State = "applied", Code = code, Arguments = arguments ?? new JObject() };

        public static AdminSettingsMutationResult Pending(string code, JObject? arguments = null)
            => new() { State = "pending", Code = code, Arguments = arguments ?? new JObject() };

        public static AdminSettingsMutationResult Rejected(string code, JObject? arguments = null)
            => new() { State = "rejected", Code = code, Arguments = arguments ?? new JObject() };
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSettingsSnapshot
    {
        [JsonProperty("contractVersion")]
        public int ContractVersion { get; set; } = AdminSettingsContract.Version;

        [JsonProperty("capabilities")]
        public List<string> Capabilities { get; set; } = [];

        [JsonProperty("guild")]
        public AdminSettingsGuild Guild { get; set; } = new();

        [JsonProperty("health")]
        public AdminSettingsHealth Health { get; set; } = new();

        [JsonProperty("resources")]
        public AdminSettingsResources Resources { get; set; } = new();

        [JsonProperty("common")]
        public AdminSettingsCommon Common { get; set; } = new();

        [JsonProperty("notifications")]
        public AdminSettingsNotifications Notifications { get; set; } = new();

        [JsonProperty("crawlers")]
        public AdminSettingsCrawlers Crawlers { get; set; } = new();

        [JsonProperty("verification")]
        public AdminSettingsVerification Verification { get; set; } = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSettingsGuild
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("memberCount")]
        public int MemberCount { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSettingsHealth
    {
        [JsonProperty("botConnected")]
        public bool BotConnected { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSettingsResources
    {
        [JsonProperty("channels")]
        public List<AdminSettingsChannel> Channels { get; set; } = [];

        [JsonProperty("roles")]
        public List<AdminSettingsRole> Roles { get; set; } = [];
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSettingsRole
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("managed")]
        public bool Managed { get; set; }

        [JsonProperty("everyone")]
        public bool Everyone { get; set; }

        [JsonProperty("botCanManage")]
        public bool BotCanManage { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSettingsChannel
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("type")]
        public string Type { get; set; } = "";

        [JsonProperty("canView")]
        public bool CanView { get; set; }

        [JsonProperty("canSendMessages")]
        public bool CanSendMessages { get; set; }

        [JsonProperty("canEmbedLinks")]
        public bool CanEmbedLinks { get; set; }

        [JsonProperty("canManageEvents")]
        public bool CanManageEvents { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSettingsCommon
    {
        [JsonProperty("locale")]
        public string Locale { get; set; } = "";

        [JsonProperty("globalNoticeChannelId")]
        public string GlobalNoticeChannelId { get; set; } = "";

        [JsonProperty("verificationLogChannelId")]
        public string VerificationLogChannelId { get; set; } = "";
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSettingsNotifications
    {
        [JsonProperty("youtube")]
        public List<AdminSettingsYoutubeNotification> Youtube { get; set; } = [];

        [JsonProperty("twitch")]
        public List<AdminSettingsTwitchNotification> Twitch { get; set; } = [];

        [JsonProperty("twitcasting")]
        public List<AdminSettingsTwitcastingNotification> Twitcasting { get; set; } = [];
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSettingsYoutubeNotification
    {
        [JsonProperty("sourceId")]
        public string SourceId { get; set; } = "";

        [JsonProperty("sourceName")]
        public string SourceName { get; set; } = "";

        [JsonProperty("streamChannelId")]
        public string StreamChannelId { get; set; } = "";

        [JsonProperty("videoChannelId")]
        public string VideoChannelId { get; set; } = "";

        [JsonProperty("createEvent")]
        public bool CreateEvent { get; set; }

        [JsonProperty("messages")]
        public AdminSettingsYoutubeMessages Messages { get; set; } = new();

        [JsonProperty("detectionEnabled")]
        public bool DetectionEnabled { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSettingsYoutubeMessages
    {
        [JsonProperty("newStream")]
        public string NewStream { get; set; } = "";

        [JsonProperty("newVideo")]
        public string NewVideo { get; set; } = "";

        [JsonProperty("start")]
        public string Start { get; set; } = "";

        [JsonProperty("end")]
        public string End { get; set; } = "";

        [JsonProperty("changeTime")]
        public string ChangeTime { get; set; } = "";

        [JsonProperty("delete")]
        public string Delete { get; set; } = "";
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSettingsTwitchNotification
    {
        [JsonProperty("sourceId")]
        public string SourceId { get; set; } = "";

        [JsonProperty("sourceName")]
        public string SourceName { get; set; } = "";

        [JsonProperty("channelId")]
        public string ChannelId { get; set; } = "";

        [JsonProperty("messages")]
        public AdminSettingsTwitchMessages Messages { get; set; } = new();

        [JsonProperty("detectionEnabled")]
        public bool DetectionEnabled { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSettingsTwitchMessages
    {
        [JsonProperty("start")]
        public string Start { get; set; } = "";

        [JsonProperty("end")]
        public string End { get; set; } = "";

        [JsonProperty("change")]
        public string Change { get; set; } = "";
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSettingsTwitcastingNotification
    {
        [JsonProperty("sourceId")]
        public string SourceId { get; set; } = "";

        [JsonProperty("sourceName")]
        public string SourceName { get; set; } = "";

        [JsonProperty("channelId")]
        public string ChannelId { get; set; } = "";

        [JsonProperty("startMessage")]
        public string StartMessage { get; set; } = "";

        [JsonProperty("detectionEnabled")]
        public bool DetectionEnabled { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSettingsCrawlers
    {
        [JsonProperty("youtube")]
        public AdminSettingsCrawlerPlatform Youtube { get; set; } = new();

        [JsonProperty("twitch")]
        public AdminSettingsCrawlerPlatform Twitch { get; set; } = new();

        [JsonProperty("twitcasting")]
        public AdminSettingsCrawlerPlatform Twitcasting { get; set; } = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSettingsCrawlerPlatform
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; }

        [JsonProperty("items")]
        public List<AdminSettingsCrawlerItem> Items { get; set; } = [];
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSettingsCrawlerItem
    {
        [JsonProperty("sourceId")]
        public string SourceId { get; set; } = "";

        [JsonProperty("sourceName")]
        public string SourceName { get; set; } = "";
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSettingsVerification
    {
        [JsonProperty("youtube")]
        public List<AdminSettingsYoutubeVerification> Youtube { get; set; } = [];

        [JsonProperty("twitch")]
        public List<AdminSettingsTwitchVerification> Twitch { get; set; } = [];
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSettingsYoutubeVerification
    {
        [JsonProperty("sourceId")]
        public string SourceId { get; set; } = "";

        [JsonProperty("sourceName")]
        public string SourceName { get; set; } = "";

        [JsonProperty("roleId")]
        public string RoleId { get; set; } = "";

        [JsonProperty("previousRoleId")]
        public string? PreviousRoleId { get; set; }

        [JsonProperty("deletionPending")]
        public bool DeletionPending { get; set; }

        [JsonProperty("probeMode")]
        public string ProbeMode { get; set; } = "automatic";

        [JsonProperty("probeVideoId")]
        public string ProbeVideoId { get; set; } = "-";

        [JsonProperty("verifiedMemberCount")]
        public int VerifiedMemberCount { get; set; }

        [JsonProperty("pendingRoleRemovalCount")]
        public int PendingRoleRemovalCount { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSettingsTwitchVerification
    {
        [JsonProperty("sourceId")]
        public string SourceId { get; set; } = "";

        [JsonProperty("sourceLogin")]
        public string SourceLogin { get; set; } = "";

        [JsonProperty("sourceName")]
        public string SourceName { get; set; } = "";

        [JsonProperty("subscriberRoleId")]
        public string SubscriberRoleId { get; set; } = "";

        [JsonProperty("previousSubscriberRoleId")]
        public string? PreviousSubscriberRoleId { get; set; }

        [JsonProperty("tierRoleIds")]
        public Dictionary<string, string> TierRoleIds { get; set; } = [];

        [JsonProperty("deletionPending")]
        public bool DeletionPending { get; set; }

        [JsonProperty("verifiedMemberCount")]
        public int VerifiedMemberCount { get; set; }

        [JsonProperty("pendingRoleRemovalCount")]
        public int PendingRoleRemovalCount { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSetLocalePayload
    {
        [JsonProperty("locale")]
        public string? Locale { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSetChannelPayload
    {
        [JsonProperty("channelId")]
        public string? ChannelId { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminYoutubeUpsertPayload
    {
        [JsonProperty("source")]
        public string? Source { get; set; }

        [JsonProperty("streamChannelId")]
        public string? StreamChannelId { get; set; }

        [JsonProperty("videoChannelId")]
        public string? VideoChannelId { get; set; }

        [JsonProperty("createEvent")]
        public bool? CreateEvent { get; set; }

        [JsonProperty("messages")]
        public AdminYoutubeMessagesPayload? Messages { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminYoutubeMessagesPayload
    {
        [JsonProperty("newStream")]
        public string? NewStream { get; set; }

        [JsonProperty("newVideo")]
        public string? NewVideo { get; set; }

        [JsonProperty("start")]
        public string? Start { get; set; }

        [JsonProperty("end")]
        public string? End { get; set; }

        [JsonProperty("changeTime")]
        public string? ChangeTime { get; set; }

        [JsonProperty("delete")]
        public string? Delete { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminTwitchUpsertPayload
    {
        [JsonProperty("source")]
        public string? Source { get; set; }

        [JsonProperty("channelId")]
        public string? ChannelId { get; set; }

        [JsonProperty("messages")]
        public AdminTwitchMessagesPayload? Messages { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminTwitchMessagesPayload
    {
        [JsonProperty("start")]
        public string? Start { get; set; }

        [JsonProperty("end")]
        public string? End { get; set; }

        [JsonProperty("change")]
        public string? Change { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminTwitcastingUpsertPayload
    {
        [JsonProperty("source")]
        public string? Source { get; set; }

        [JsonProperty("channelId")]
        public string? ChannelId { get; set; }

        [JsonProperty("startMessage")]
        public string? StartMessage { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminRemoveNotificationPayload
    {
        [JsonProperty("source")]
        public string? Source { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSourcePayload
    {
        [JsonProperty("source")]
        public string? Source { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminSourceIdPayload
    {
        [JsonProperty("sourceId")]
        public string? SourceId { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminVerificationUpsertPayload
    {
        [JsonProperty("source")]
        public string? Source { get; set; }

        [JsonProperty("roleId")]
        public string? RoleId { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AdminProbeVideoPayload
    {
        [JsonProperty("sourceId")]
        public string? SourceId { get; set; }

        [JsonProperty("video")]
        public string? Video { get; set; }
    }
}
