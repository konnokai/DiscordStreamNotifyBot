using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DiscordStreamNotifyBot.DataBase
{
    public class MainDbService
    {
        // Pooled factory（計畫 §12.8）：scraper/notifier 大量短生命週期 context，
        // 以物件池重用實例降低配置成本。讀取已普遍 AsNoTracking，且皆為 using var 短用後即歸還池。
        private readonly PooledDbContextFactory<MainDbContext> _pool;

        public MainDbService(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MainDbContext>();
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            optionsBuilder.UseSnakeCaseNamingConvention();
            _pool = new PooledDbContextFactory<MainDbContext>(optionsBuilder.Options);
        }

        /// <summary>取得短生命週期 context（來自物件池，Dispose 時歸還）。一律 <c>using var db = GetDbContext()</c>。</summary>
        public MainDbContext GetDbContext()
        {
            return _pool.CreateDbContext();
        }
    }
}
