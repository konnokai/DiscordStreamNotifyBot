public sealed class RedisConnection
{
    private static readonly object SyncRoot = new();
    private static Lazy<RedisConnection> lazy = CreateLazy();
    private static string _settingOption;

    private static Lazy<RedisConnection> CreateLazy() => new(() =>
    {
        if (String.IsNullOrEmpty(_settingOption)) throw new InvalidOperationException("Please call Init() first.");
        return new RedisConnection();
    });

    public readonly ConnectionMultiplexer ConnectionMultiplexer;

    public static RedisConnection Instance
    {
        get
        {
            return lazy.Value;
        }
    }

    private RedisConnection()
    {
        ConnectionMultiplexer = ConnectionMultiplexer.Connect(_settingOption);
    }

    public static void Init(string settingOption)
    {
        _settingOption = settingOption;
    }

    internal static void ResetForRetry(string settingOption)
    {
        Lazy<RedisConnection> previous;
        lock (SyncRoot)
        {
            _settingOption = settingOption;
            previous = lazy;
            lazy = CreateLazy();
        }

        if (previous.IsValueCreated)
            previous.Value.ConnectionMultiplexer.Dispose();
    }
}

