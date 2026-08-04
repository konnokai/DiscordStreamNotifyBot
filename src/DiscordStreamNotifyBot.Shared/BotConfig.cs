using DiscordStreamNotifyBot.Shared;

public class BotConfig
{
    public string MySqlConnectionString { get; set; } = "Server=localhost;Port=3306;User Id=stream_bot;Password=Ch@nge_Me;Database=discord_stream_bot";
    public string RedisOption { get; set; } = "127.0.0.1,syncTimeout=3000";
    public string ProviderTokenEncryptionKey { get; set; } = "";

    public string ApiServerDomain { get; set; } = "";
    public string UptimeKumaPushUrl { get; set; } = "";
    public string LokiUrl { get; set; } = "";

    public string DiscordToken { get; set; } = "";
    public ulong[] TestSlashCommandGuildIds { get; set; } = [];
    public string WebHookUrl { get; set; } = "";

    public string GoogleApiKey { get; set; } = "";
    public string GoogleClientId { get; set; } = "";
    public string GoogleClientSecret { get; set; } = "";

    public string TwitCastingClientId { get; set; } = "";
    public string TwitCastingClientSecret { get; set; } = "";

    // https://streamlink.github.io/cli/plugins/twitch.html#authentication
    // 先放著，未來可能會用到
    public string TwitchCookieAuthToken { get; set; } = "";
    public string TwitchClientId { get; set; } = "";
    public string TwitchClientSecret { get; set; } = "";

    public ulong YouTubeEmoteId { get; set; } = 1265158558299848827;
    public ulong PayPalEmoteId { get; set; } = 1265158658015236107;
    public ulong ECPayEmoteId { get; set; } = 1379272194210795622;

    /// <summary>
    /// 是否啟用 GuildMembers 特權 intent（會員重加入即時回補會限身分組 + 孤兒身分組回收對帳）。
    /// <para>預設 false：未在 Discord 開發者後台開啟 Server Members Intent 前務必保持關閉，否則 bot login 會因
    /// disallowed intent（4014）連線失敗。開啟特權並送審通過後才設 true（或環境變數 ENABLE_GUILD_MEMBERS_INTENT）。</para>
    /// </summary>
    public bool EnableGuildMembersIntent { get; set; } = false;

    #region 水平擴展（三層拆分）設定 (計畫 §3)
    /// <summary>
    /// 叢集 shard 總數，供 Coordinator 公告 TOTAL_SHARDS 並比對存活 notifier 數（可由環境變數 TOTAL_SHARDS 覆寫）。
    /// <para>Notifier 自身的 shard 身分與總數由啟動參數 <c>[ShardId TotalShards]</c> 決定，不讀本欄位。</para>
    /// </summary>
    public int TotalShards { get; set; } = 1;

    /// <summary>各程序寫入心跳鍵的間隔秒數。</summary>
    public int HeartbeatIntervalSeconds { get; set; } = 10;

    /// <summary>心跳鍵的 TTL 秒數（應明顯大於間隔，避免誤判離線）。</summary>
    public int HeartbeatTtlSeconds { get; set; } = 30;
    // 通知匯流排 = Redis Streams（就在既有 RedisOption 指向的 Redis 上，無額外連線設定，計畫 §4）
    #endregion

    /// <summary>
    /// 載入 bot_config.json、套用環境變數覆寫並驗證必填欄位。
    /// </summary>
    /// <param name="role">
    /// 程序角色；決定哪些欄位為必填（計畫 §5.3）。
    /// <c>null</c>（預設，單體 monolith 用）等同 notifier，維持原有「全部必填」行為。
    /// coordinator 僅需 Redis；scraper 需 Google/ApiServerDomain 但不需 Discord。
    /// </param>
    public void InitBotConfig(BotRole? role = null)
    {
        try { File.WriteAllText("bot_config_example.json", JsonConvert.SerializeObject(new BotConfig(), Formatting.Indented)); } catch { }
        if (!File.Exists("bot_config.json"))
        {
            Log.Error($"bot_config.json 遺失，請依照 {Path.GetFullPath("bot_config_example.json")} 內的格式填入正確的數值");
            if (!Console.IsInputRedirected)
                Console.ReadKey();
            Environment.Exit(3);
        }

        var config = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText("bot_config.json"));

        // 先以環境變數覆寫（正式環境 / Docker Compose 用 .env 注入），再進行必填驗證 (計畫 §3)
        config.ApplyEnvironmentOverrides();

        try
        {
            // 依角色決定必填欄位：notifier(或 monolith) 需 Discord/WebHook；scraper/notifier 需 Google/ApiServerDomain；coordinator 皆不需
            bool needsDiscord = role is null or BotRole.Notifier;
            bool needsYoutube = role is null or BotRole.Notifier or BotRole.Scraper;

            if (needsDiscord)
            {
                RequireField(config.DiscordToken, nameof(DiscordToken));
                RequireField(config.WebHookUrl, nameof(WebHookUrl));
            }

            if (needsYoutube)
            {
                RequireField(config.GoogleApiKey, nameof(GoogleApiKey));
                RequireField(config.ApiServerDomain, nameof(ApiServerDomain));
            }

            MySqlConnectionString = config.MySqlConnectionString;
            RedisOption = config.RedisOption;
            ProviderTokenEncryptionKey = config.ProviderTokenEncryptionKey;
            ApiServerDomain = config.ApiServerDomain;
            DiscordToken = config.DiscordToken;
            WebHookUrl = config.WebHookUrl;
            GoogleApiKey = config.GoogleApiKey;
            TestSlashCommandGuildIds = config.TestSlashCommandGuildIds ?? [];
            TwitCastingClientId = config.TwitCastingClientId;
            TwitCastingClientSecret = config.TwitCastingClientSecret;
            TwitchCookieAuthToken = config.TwitchCookieAuthToken;
            TwitchClientId = config.TwitchClientId;
            TwitchClientSecret = config.TwitchClientSecret;
            GoogleClientId = config.GoogleClientId;
            GoogleClientSecret = config.GoogleClientSecret;
            UptimeKumaPushUrl = config.UptimeKumaPushUrl;
            LokiUrl = config.LokiUrl;
            YouTubeEmoteId = config.YouTubeEmoteId;
            PayPalEmoteId = config.PayPalEmoteId;
            ECPayEmoteId = config.ECPayEmoteId;
            EnableGuildMembersIntent = config.EnableGuildMembersIntent;
            TotalShards = config.TotalShards;
            HeartbeatIntervalSeconds = config.HeartbeatIntervalSeconds;
            HeartbeatTtlSeconds = config.HeartbeatTtlSeconds;

            if (needsDiscord)
                ValidateProviderTokenEncryptionKey(ProviderTokenEncryptionKey);
        }
        catch (Exception ex)
        {
            Log.Error($"設定檔讀取失敗: {ex}");
            throw;
        }
    }

    /// <summary>
    /// 以環境變數覆寫設定（env 優先）。對應計畫 §3 的覆寫表；正式環境 / Docker Compose 透過 .env 注入，
    /// 敏感值不入 image。未設定的環境變數則維持 bot_config.json 的值。
    /// </summary>
    public void ApplyEnvironmentOverrides()
    {
        SetIfPresent("MYSQL_CONNECTION_STRING", v => MySqlConnectionString = v);
        SetIfPresent("REDIS_OPTION", v => RedisOption = v);
        SetIfPresent("PROVIDER_TOKEN_ENCRYPTION_KEY", v => ProviderTokenEncryptionKey = v);
        SetIfPresent("DISCORD_TOKEN", v => DiscordToken = v);
        SetIfPresent("GOOGLE_API_KEY", v => GoogleApiKey = v);
        SetIfPresent("LOKI_URL", v => LokiUrl = v);
        SetIfPresentInt("TOTAL_SHARDS", v => TotalShards = v);
        SetIfPresentBool("ENABLE_GUILD_MEMBERS_INTENT", v => EnableGuildMembersIntent = v);
    }

    private static void SetIfPresent(string envName, Action<string> setter)
    {
        var value = Environment.GetEnvironmentVariable(envName);
        if (!string.IsNullOrWhiteSpace(value))
            setter(value);
    }

    private static void SetIfPresentInt(string envName, Action<int> setter)
    {
        var value = Environment.GetEnvironmentVariable(envName);
        if (!string.IsNullOrWhiteSpace(value) && int.TryParse(value, out var parsed))
            setter(parsed);
    }

    private static void SetIfPresentBool(string envName, Action<bool> setter)
    {
        var value = Environment.GetEnvironmentVariable(envName);
        if (!string.IsNullOrWhiteSpace(value) && bool.TryParse(value, out var parsed))
            setter(parsed);
    }

    private static void RequireField(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Log.Error($"{fieldName} 遺失，請輸入至 bot_config.json（或對應環境變數）後重開 Bot");
            if (!Console.IsInputRedirected)
                Console.ReadKey();
            Environment.Exit(3);
        }
    }

    internal static void ValidateProviderTokenEncryptionKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{nameof(ProviderTokenEncryptionKey)} 不得為空");
        if (value.Length < 64)
            throw new InvalidOperationException($"{nameof(ProviderTokenEncryptionKey)} 長度不得少於 64 字元");
    }

}
