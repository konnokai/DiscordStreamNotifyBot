namespace DiscordStreamNotifyBot.Tests.Component.MySql
{
    public sealed class MySqlComponentFactAttribute : FactAttribute
    {
        public const string ConnectionStringEnvironmentVariable = "MYSQL_TEST_CONNECTION_STRING";

        public MySqlComponentFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable)))
            {
                Skip = $"{ConnectionStringEnvironmentVariable} 未設定，略過 MySQL/MariaDB component test。";
            }
        }
    }
}
