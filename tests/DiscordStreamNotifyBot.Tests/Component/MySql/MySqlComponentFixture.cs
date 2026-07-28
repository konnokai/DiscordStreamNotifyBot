using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Shared;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace DiscordStreamNotifyBot.Tests.Component.MySql
{
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class MySqlComponentCollection : ICollectionFixture<MySqlComponentFixture>
    {
        public const string Name = "MySQL component tests";
    }

    public sealed class MySqlComponentFixture : IAsyncLifetime
    {
        public const string EncryptionKey = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        private const string DatabaseNamePrefix = "discord_stream_bot_component_";

        private readonly MainDbService _originalDbService = BotState.DbService;
        private readonly string _originalRedisKey = Utility.RedisKey;
        private string _adminConnectionString;
        private string _databaseName;

        public MainDbService DbService { get; private set; }

        public async Task InitializeAsync()
        {
            var connectionString = Environment.GetEnvironmentVariable(
                MySqlComponentFactAttribute.ConnectionStringEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"{MySqlComponentFactAttribute.ConnectionStringEnvironmentVariable} 未設定。");
            }

            var adminBuilder = new MySqlConnectionStringBuilder(connectionString)
            {
                Database = string.Empty,
                Pooling = false
            };
            _adminConnectionString = adminBuilder.ConnectionString;
            _databaseName = DatabaseNamePrefix + Guid.NewGuid().ToString("N");

            await using (var connection = new MySqlConnection(_adminConnectionString))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"CREATE DATABASE `{_databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
                await command.ExecuteNonQueryAsync();
            }

            try
            {
                var databaseBuilder = new MySqlConnectionStringBuilder(connectionString)
                {
                    Database = _databaseName
                };
                DbService = new MainDbService(databaseBuilder.ConnectionString);

                await using var db = DbService.GetDbContext();
                await db.Database.MigrateAsync();

                BotState.DbService = DbService;
                Utility.RedisKey = EncryptionKey;
            }
            catch
            {
                await TryDropDatabaseAsync();
                throw;
            }
        }

        public async Task DisposeAsync()
        {
            BotState.DbService = _originalDbService;
            Utility.RedisKey = _originalRedisKey;
            await DropDatabaseAsync();
        }

        private async Task DropDatabaseAsync()
        {
            if (string.IsNullOrEmpty(_databaseName))
                return;

            if (!_databaseName.StartsWith(DatabaseNamePrefix, StringComparison.Ordinal) ||
                _databaseName.Length != DatabaseNamePrefix.Length + 32)
            {
                throw new InvalidOperationException($"拒絕刪除非 component test 資料庫：{_databaseName}");
            }

            await using var connection = new MySqlConnection(_adminConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"DROP DATABASE IF EXISTS `{_databaseName}`;";
            await command.ExecuteNonQueryAsync();
            _databaseName = null;
        }

        private async Task TryDropDatabaseAsync()
        {
            try
            {
                await DropDatabaseAsync();
            }
            catch
            {
                // 保留原始連線或 migration 例外，避免清理失敗掩蓋真正原因。
            }
        }
    }
}
