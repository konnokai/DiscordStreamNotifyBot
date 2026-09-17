using DiscordStreamNotifyBot.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordStreamNotifyBot.Scraper
{
    /// <summary>
    /// 偵測宿主（計畫階段 3 核心）：在 Scraper 程序內實體執行 <c>Detection/</c> 下的偵測服務
    /// （YouTube / Twitch 的輪詢 Timer、錄影 Redis 訂閱、PubSub/EventSub 維護）。
    /// <para>
    /// <b>完全不建立 DiscordSocketClient</b>：偵測路徑一律 publish DTO 至通知匯流排（不觸碰 gateway）。
    /// 發送、建立活動、更換伺服器橫幅、身分組等需 Discord 的動作由 Notifier 消費匯流排後執行。
    /// </para>
    /// <para>
    /// 角色由執行檔決定，只有 Scraper 啟動本宿主。
    /// </para>
    /// </summary>
    public class DetectionHost
    {
        private ServiceProvider _serviceProvider;

        /// <summary>初始化靜態相依並啟動偵測服務（建構子內即啟動 Timer 與 Redis 訂閱）。</summary>
        public void Start(BotConfig config)
        {
            // 設定偵測服務所需的靜態相依（DbService / Redis），不建立 Discord 連線
            BotState.InitDetectionDependencies(config);

            var services = new ServiceCollection()
                .AddHttpClient()
                .AddSingleton(config)
                .AddSingleton(BotState.DbService)
                .AddSingleton<ClusterService>()
                .AddSingleton<Shared.YoutubeApiService>()
                .AddSingleton<Detection.Youtube.YoutubeDetectionService>()
                .AddSingleton<SharedService.Twitch.TwitchApiService>()
                .AddSingleton<Detection.Twitch.TwitchDetectionService>();

            _serviceProvider = services.BuildServiceProvider();

            // 實體化（各服務建構子內啟動偵測 Timer 與 Redis 訂閱）
            _serviceProvider.GetRequiredService<Detection.Youtube.YoutubeDetectionService>();
            _serviceProvider.GetRequiredService<Detection.Twitch.TwitchDetectionService>();

            Log.Info("[Scraper] 偵測服務已啟動（YouTube / Twitch），事件將發布至通知匯流排");
        }

        /// <summary>關閉前儲存偵測狀態（addNewStreamVideo → DB）。</summary>
        public void SaveStateBeforeShutdown()
        {
            try
            {
                Detection.Youtube.YoutubeDetectionService.SaveDateBase();
                Log.Info("[Scraper] 已儲存偵測資料庫狀態");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "[Scraper] 關閉前儲存資料庫失敗");
            }
        }
    }
}
