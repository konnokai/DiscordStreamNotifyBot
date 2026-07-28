using StackExchange.Redis;

namespace DiscordStreamNotifyBot.Tests.Component.Redis
{
    public sealed class RedisComponentFactAttribute : FactAttribute
    {
        public RedisComponentFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(RedisComponentFixture.OptionEnvironmentVariable)))
                Skip = $"未設定 {RedisComponentFixture.OptionEnvironmentVariable}，略過 Redis component tests。";
        }
    }

    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class RedisComponentCollection : ICollectionFixture<RedisComponentFixture>
    {
        public const string Name = "Redis component tests";
    }

    public sealed class RedisComponentFixture : IAsyncLifetime
    {
        public const string OptionEnvironmentVariable = "REDIS_COMPONENT_OPTION";
        private readonly string _ownerValue = $"component-owner-{Guid.NewGuid():N}";
        private readonly RedisKey _ownerKey = "discord-stream-bot:component-test:owner";

        public ConnectionMultiplexer Connection { get; private set; }

        public string Option { get; private set; }

        public int DatabaseNumber { get; private set; }

        public IDatabase Database => Connection.GetDatabase(DatabaseNumber);

        public async Task InitializeAsync()
        {
            Option = Environment.GetEnvironmentVariable(OptionEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(Option))
                throw new InvalidOperationException($"{OptionEnvironmentVariable} 未設定。");

            var options = ParseOptions();
            if (!options.DefaultDatabase.HasValue || options.DefaultDatabase.Value < 2)
            {
                throw new InvalidOperationException(
                    $"{OptionEnvironmentVariable} 必須明確設定 defaultDatabase=2 以上，避免碰觸 production DB 0 與 token DB 1。");
            }

            DatabaseNumber = options.DefaultDatabase.Value;
            Connection = await ConnectionMultiplexer.ConnectAsync(options);
            await Database.PingAsync();

            var endpoints = Connection.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = Connection.GetServer(endpoint);
                if (!server.IsConnected || server.IsReplica)
                    continue;

                var existingKeys = server.Keys(DatabaseNumber, pageSize: 1).Take(1).ToArray();
                if (existingKeys.Length != 0)
                {
                    throw new InvalidOperationException(
                        $"Redis DB {DatabaseNumber} 不是空的；發現鍵 '{existingKeys[0]}'，拒絕執行 component tests。");
                }
            }

            if (!await Database.StringSetAsync(_ownerKey, _ownerValue, TimeSpan.FromMinutes(30), When.NotExists))
                throw new InvalidOperationException($"Redis DB {DatabaseNumber} 已被另一個 component test 執行占用。");
        }

        public async Task DisposeAsync()
        {
            if (Connection != null)
                await DeleteStringIfOwnedAsync(Database, _ownerKey, _ownerValue);
            Connection?.Dispose();
        }

        public async Task<ConnectionMultiplexer> OpenConnectionAsync()
        {
            return await ConnectionMultiplexer.ConnectAsync(ParseOptions());
        }

        private ConfigurationOptions ParseOptions()
        {
            var options = ConfigurationOptions.Parse(Option);
            options.AbortOnConnectFail = true;
            return options;
        }

        public static async Task AssertKeysAbsentAsync(IDatabase db, params RedisKey[] keys)
        {
            foreach (var key in keys)
            {
                Assert.False(await db.KeyExistsAsync(key),
                    $"Redis component tests 需要專用資料庫；鍵 '{key}' 已存在，測試拒絕覆寫或刪除既有資料。");
            }
        }

        public static async Task DeleteStringIfOwnedAsync(
            IDatabase db,
            RedisKey key,
            params RedisValue[] expectedValues)
        {
            foreach (var expectedValue in expectedValues)
            {
                var transaction = db.CreateTransaction();
                transaction.AddCondition(Condition.StringEqual(key, expectedValue));
                var deleteTask = transaction.KeyDeleteAsync(key);
                if (await transaction.ExecuteAsync())
                {
                    await deleteTask;
                    return;
                }
            }
        }
    }
}
