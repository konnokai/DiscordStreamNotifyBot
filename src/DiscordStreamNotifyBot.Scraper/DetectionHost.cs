using DiscordStreamNotifyBot.HttpClients;
using DiscordStreamNotifyBot.Shared;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace DiscordStreamNotifyBot.Scraper
{
    /// <summary>
    /// 偵測宿主（計畫階段 3 核心）：在 Scraper 程序內實體執行 <c>Detection/</c> 下的偵測服務
    /// （YouTube / Twitch / Twitcasting 的輪詢 Timer、錄影 Redis 訂閱、PubSub/EventSub/WebHook 維護）。
    /// <para>
    /// <b>完全不建立 DiscordSocketClient</b>：偵測路徑一律 publish DTO 至通知匯流排（不觸碰 gateway）。
    /// 發送、建立活動、更換伺服器橫幅、會員身分組等需 Discord 的動作由 Notifier 消費匯流排後執行。
    /// </para>
    /// <para>
    /// 角色由執行檔決定：本宿主設定 <c>BotState.IsDetectionHost = true</c>。
    /// 會限檢查（YoutubeMemberService）不在此執行：它按 shard 分區，由各 Notifier 自行執行。
    /// </para>
    /// </summary>
    public class DetectionHost
    {
        private ServiceProvider _serviceProvider;

        /// <summary>初始化靜態相依並啟動偵測服務（建構子內即啟動 Timer 與 Redis 訂閱）。</summary>
        public void Start(BotConfig config)
        {
            // 標記本程序為偵測宿主
            BotState.IsDetectionHost = true;

            // 設定偵測服務所需的靜態相依（DbService / Redis），不建立 Discord 連線
            BotState.InitDetectionDependencies(config);

            var services = new ServiceCollection()
                .AddHttpClient()
                .AddSingleton(config)
                .AddSingleton(BotState.DbService)
                .AddSingleton<Shared.YoutubeApiService>()
                .AddSingleton<Detection.Youtube.YoutubeDetectionService>()
                .AddSingleton<SharedService.Twitch.TwitchApiService>()
                .AddSingleton<Detection.Twitch.TwitchDetectionService>()
                .AddSingleton<Detection.Twitcasting.TwitcastingDetectionService>();

            // 與 Notifier 端相同的 TwitcastingClient 設定（HandleTransientHttpError 含 5xx 及 408）
            services.AddHttpClient<TwitcastingClient>()
                .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .RetryAsync(3));

            _serviceProvider = services.BuildServiceProvider();

            // 實體化（各服務建構子內啟動偵測 Timer 與 Redis 訂閱）
            _serviceProvider.GetRequiredService<Detection.Youtube.YoutubeDetectionService>();
            _serviceProvider.GetRequiredService<Detection.Twitch.TwitchDetectionService>();
            _serviceProvider.GetRequiredService<Detection.Twitcasting.TwitcastingDetectionService>();

            Log.Info("[Scraper] 偵測服務已啟動（YouTube / Twitch / Twitcasting），事件將發布至通知匯流排");
        }

        /// <summary>關閉前保存偵測狀態（addNewStreamVideo → DB）。</summary>
        public void SaveStateBeforeShutdown()
        {
            try
            {
                Detection.Youtube.YoutubeDetectionService.SaveDateBase();
                Log.Info("[Scraper] 已保存偵測資料庫狀態");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "[Scraper] 關閉前保存資料庫失敗");
            }
        }
    }
}
