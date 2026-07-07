using Microsoft.EntityFrameworkCore.Design;

namespace DiscordStreamNotifyBot.DataBase
{
    /// <summary>
    /// EF Core 設計階段（<c>dotnet ef</c>）用的 DbContext 工廠。
    /// <para>
    /// 讓 <c>dotnet ef migrations add</c> / <c>database update</c> 可直接以本 Shared 專案為目標執行，
    /// 不需指定 startup project。連線字串依序取自：環境變數 <c>MYSQL_CONNECTION_STRING</c>
    /// → 同目錄 <c>bot_config.json</c> 的 <c>MySqlConnectionString</c> → localhost 預設值。
    /// </para>
    /// <para>
    /// 設定內容（UseMySql + snake_case 命名）必須與 <see cref="MainDbService"/> 一致，確保產生的模型相同。
    /// </para>
    /// </summary>
    public class MainDbContextFactory : IDesignTimeDbContextFactory<MainDbContext>
    {
        public MainDbContext CreateDbContext(string[] args)
        {
            string connectionString = ResolveConnectionString();

            // database update（連得到 DB）時用 AutoDetect 取得實際版本；
            // migrations add（離線、無 DB）時退回固定版本，避免設計階段被迫連線。
            ServerVersion serverVersion;
            try
            {
                serverVersion = ServerVersion.AutoDetect(connectionString);
            }
            catch
            {
                serverVersion = new MySqlServerVersion(new Version(8, 0, 32));
            }

            var optionsBuilder = new DbContextOptionsBuilder<MainDbContext>();
            optionsBuilder.UseMySql(connectionString, serverVersion);
            optionsBuilder.UseSnakeCaseNamingConvention();

            return new MainDbContext(optionsBuilder.Options);
        }

        private static string ResolveConnectionString()
        {
            var fromEnv = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING");
            if (!string.IsNullOrWhiteSpace(fromEnv))
                return fromEnv;

            try
            {
                if (File.Exists("bot_config.json"))
                {
                    var config = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText("bot_config.json"));
                    if (config != null && !string.IsNullOrWhiteSpace(config.MySqlConnectionString))
                        return config.MySqlConnectionString;
                }
            }
            catch
            {
                // 設計階段讀檔失敗時退回預設值（migrations add 不需實際連線）
            }

            return "Server=localhost;Port=3306;User Id=stream_bot;Password=Ch@nge_Me;Database=discord_stream_bot";
        }
    }
}
