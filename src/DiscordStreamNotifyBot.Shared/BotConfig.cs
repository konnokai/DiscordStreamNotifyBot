using DiscordStreamNotifyBot;
using DiscordStreamNotifyBot.Shared;

public class BotConfig
{
    public string MySqlConnectionString { get; set; } = "Server=localhost;Port=3306;User Id=stream_bot;Password=Ch@nge_Me;Database=discord_stream_bot";
    public string RedisOption { get; set; } = "127.0.0.1,syncTimeout=3000";
    public string RedisTokenKey { get; set; } = "";

    public string ApiServerDomain { get; set; } = "";
    public string UptimeKumaPushUrl { get; set; } = "";

    public string DiscordToken { get; set; } = "";
    public ulong TestSlashCommandGuildId { get; set; } = 0;
    public string WebHookUrl { get; set; } = "";

    public string GoogleApiKey { get; set; } = "";
    public string GoogleClientId { get; set; } = "";
    public string GoogleClientSecret { get; set; } = "";

    public string TwitCastingClientId { get; set; } = "";
    public string TwitCastingClientSecret { get; set; } = "";
    public string TwitCastingRecordPath { get; set; } = "";

    // https://streamlink.github.io/cli/plugins/twitch.html#authentication
    // 先放著，未來可能會用到
    public string TwitchCookieAuthToken { get; set; } = "";
    public string TwitchClientId { get; set; } = "";
    public string TwitchClientSecret { get; set; } = "";

    public ulong YouTubeEmoteId { get; set; } = 1265158558299848827;
    public ulong PayPalEmoteId { get; set; } = 1265158658015236107;
    public ulong ECPayEmoteId { get; set; } = 1379272194210795622;

    #region 水平擴展（三層拆分）設定 (計畫 §3)
    /// <summary>程序角色：scraper / notifier / coordinator（可由環境變數 ROLE 覆寫）。</summary>
    public string Role { get; set; } = "notifier";

    /// <summary>叢集 shard 總數（可由環境變數 TOTAL_SHARDS 或 notifier 啟動參數覆寫）。</summary>
    public int TotalShards { get; set; } = 1;

    /// <summary>notifier 專用 shard id（用租約模式時可省略；可由啟動參數覆寫）。</summary>
    public int ShardId { get; set; } = 0;

    /// <summary>各程序寫入心跳鍵的間隔秒數。</summary>
    public int HeartbeatIntervalSeconds { get; set; } = 10;

    /// <summary>心跳鍵的 TTL 秒數（應明顯大於間隔，避免誤判離線）。</summary>
    public int HeartbeatTtlSeconds { get; set; } = 30;

    /// <summary>RabbitMQ 通知匯流排連線設定。</summary>
    public RabbitMqConfig RabbitMQ { get; set; } = new RabbitMqConfig();

    /// <summary>
    /// 是否啟用 RabbitMQ 通知匯流排消費（階段 3 cutover）。預設 false＝維持單體行為（自行偵測並發送）。
    /// 啟用後 notifier 會消費 notify.shard.{id} 並透過 DispatchFromBusAsync 發送。
    /// </summary>
    public bool EnableNotificationBus { get; set; } = false;

    public class RabbitMqConfig
    {
        public string HostName { get; set; } = "rabbitmq";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
    }
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

        // 角色覆寫：若呼叫端明確指定角色，以該角色為準（env/啟動參數已套用至 config.Role）
        if (role.HasValue)
            config.Role = role.Value.ToString().ToLowerInvariant();

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
            RedisTokenKey = config.RedisTokenKey;
            ApiServerDomain = config.ApiServerDomain;
            DiscordToken = config.DiscordToken;
            WebHookUrl = config.WebHookUrl;
            GoogleApiKey = config.GoogleApiKey;
            TestSlashCommandGuildId = config.TestSlashCommandGuildId;
            TwitCastingClientId = config.TwitCastingClientId;
            TwitCastingClientSecret = config.TwitCastingClientSecret;
            TwitCastingRecordPath = config.TwitCastingRecordPath;
            TwitchCookieAuthToken = config.TwitchCookieAuthToken;
            TwitchClientId = config.TwitchClientId;
            TwitchClientSecret = config.TwitchClientSecret;
            GoogleClientId = config.GoogleClientId;
            GoogleClientSecret = config.GoogleClientSecret;
            UptimeKumaPushUrl = config.UptimeKumaPushUrl;
            YouTubeEmoteId = config.YouTubeEmoteId;
            PayPalEmoteId = config.PayPalEmoteId;
            ECPayEmoteId = config.ECPayEmoteId;
            Role = config.Role;
            TotalShards = config.TotalShards;
            ShardId = config.ShardId;
            HeartbeatIntervalSeconds = config.HeartbeatIntervalSeconds;
            HeartbeatTtlSeconds = config.HeartbeatTtlSeconds;
            RabbitMQ = config.RabbitMQ;
            EnableNotificationBus = config.EnableNotificationBus;

            if (string.IsNullOrWhiteSpace(config.RedisTokenKey) || string.IsNullOrWhiteSpace(RedisTokenKey))
            {
                Log.Error($"{nameof(RedisTokenKey)} 遺失，將重新建立隨機亂數");

                RedisTokenKey = GenRandomKey();

                try { File.WriteAllText("bot_config.json", JsonConvert.SerializeObject(this, Formatting.Indented)); }
                catch (Exception ex)
                {
                    Log.Error($"設定檔保存失敗: {ex}");
                    Log.Error($"請手動將此字串填入設定檔中的 \"{nameof(RedisTokenKey)}\" 欄位: {RedisTokenKey}");
                    Environment.Exit(3);
                }
            }

            Utility.RedisKey = RedisTokenKey;
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
        SetIfPresent("DISCORD_TOKEN", v => DiscordToken = v);
        SetIfPresent("GOOGLE_API_KEY", v => GoogleApiKey = v);
        SetIfPresent("ROLE", v => Role = v);
        SetIfPresentInt("TOTAL_SHARDS", v => TotalShards = v);
        SetIfPresentInt("SHARD_ID", v => ShardId = v);

        SetIfPresent("RABBITMQ_HOST", v => RabbitMQ.HostName = v);
        SetIfPresentInt("RABBITMQ_PORT", v => RabbitMQ.Port = v);
        SetIfPresent("RABBITMQ_USER", v => RabbitMQ.UserName = v);
        SetIfPresent("RABBITMQ_PASSWORD", v => RabbitMQ.Password = v);
        SetIfPresent("RABBITMQ_VHOST", v => RabbitMQ.VirtualHost = v);
        SetIfPresent("ENABLE_NOTIFICATION_BUS", v => { if (bool.TryParse(v, out var b)) EnableNotificationBus = b; });
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

    public static string GenRandomKey(int length = 128)
    {
        var characters = "ABCDEF_GHIJKLMNOPQRSTUVWXYZ@abcdefghijklmnopqrstuvwx-yz0123456789";
        var Charsarr = new char[128];
        var random = new Random();

        for (int i = 0; i < Charsarr.Length; i++)
        {
            Charsarr[i] = characters[random.Next(characters.Length)];
        }

        var resultString = new string(Charsarr);
        resultString = resultString[Math.Min(length, resultString.Length)..];
        return resultString;
    }
}
