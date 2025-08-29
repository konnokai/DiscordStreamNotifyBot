namespace DiscordStreamNotifyBot.DataBase
{
    public class MainDbService
    {
        private readonly DbContextOptions<MainDbContext> _options;
        private readonly string _connectionString;

        public MainDbService(string connectionString)
        {
            _connectionString = connectionString;

            var optionsBuilder = new DbContextOptionsBuilder<MainDbContext>();
            optionsBuilder.UseMySql(_connectionString, ServerVersion.AutoDetect(_connectionString));
            optionsBuilder.UseSnakeCaseNamingConvention();            
            _options = optionsBuilder.Options;
        }

        public MainDbContext GetDbContext()
        {
            return new MainDbContext(_options);
        }
    }
}
