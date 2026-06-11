using Discord;
using Discord.WebSocket;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.HttpClients;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace DiscordStreamNotifyBot.Scraper
{
    /// <summary>
    /// 偵測宿主（計畫階段 3 核心）：在 Scraper 程序內以「無頭模式」實體執行 Notifier 組件的偵測服務
    /// （YouTube / Twitch / Twitcasting 的輪詢 Timer、錄影 Redis 訂閱、PubSub/EventSub/WebHook 維護）。
    /// <para>
    /// 無頭模式＝建立一個<b>永不登入</b>的 DiscordSocketClient 作為相依佔位：
    /// 偵測路徑一律 publish DTO 至匯流排（fromBus seam，不觸碰 gateway）、
    /// <c>Bot.ApplicatonOwner</c> 為 null 時相關私訊均有守衛、EmojiService 取不到 emote 時為 null（僅發送端使用）。
    /// </para>
    /// <para>
    /// 角色由執行檔決定：本宿主設定 <c>Bot.IsDetectionHost = true</c> 後偵測才會啟動；
    /// Notifier 程序永不偵測、其通知一律來自匯流排消費。
    /// 會限檢查（YoutubeMemberService）不在此執行：它按 shard 分區，由各 Notifier 自行執行。
    /// </para>
    /// </summary>
    public class DetectionHost
    {
        private ServiceProvider _serviceProvider;

        /// <summary>初始化靜態相依並啟動偵測服務（建構子內即啟動 Timer 與 Redis 訂閱）。</summary>
        public void Start(BotConfig config)
        {
            // 標記本程序為偵測宿主（服務建構子依此啟動偵測 Timer / Redis 訂閱）
            Bot.IsDetectionHost = true;

            // 設定偵測服務所需的 Bot 靜態相依（DbService / Redis），不建立 Discord 連線
            Bot.InitHeadlessHost(config);

            // 永不登入的 client：純作建構子相依佔位，Ready 不會觸發、GetGuild 一律 null
            var headlessClient = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Warning,
                MessageCacheSize = 0,
            });

            var services = new ServiceCollection()
                .AddHttpClient()
                .AddSingleton(headlessClient)
                .AddSingleton(config)
                .AddSingleton(Bot.DbService)
                .AddSingleton<Shared.YoutubeApiService>()
                .AddSingleton<EmojiService>()
                .AddSingleton<SharedService.Youtube.YoutubeStreamService>()
                .AddSingleton<SharedService.Twitch.TwitchService>()
                .AddSingleton<SharedService.Twitcasting.TwitcastingService>();

            // 與 Notifier 端相同的 TwitcastingClient 設定（HandleTransientHttpError 含 5xx 及 408）
            services.AddHttpClient<TwitcastingClient>()
                .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .RetryAsync(3));

            _serviceProvider = services.BuildServiceProvider();

            // 實體化（各服務建構子內啟動偵測 Timer 與 Redis 訂閱）
            _serviceProvider.GetRequiredService<SharedService.Youtube.YoutubeStreamService>();
            _serviceProvider.GetRequiredService<SharedService.Twitch.TwitchService>();
            _serviceProvider.GetRequiredService<SharedService.Twitcasting.TwitcastingService>();

            Log.Info("[Scraper] 偵測服務已啟動（YouTube / Twitch / Twitcasting），事件將發布至通知匯流排");
        }

        /// <summary>關閉前保存偵測狀態（對應 Notifier 端關閉時的 SaveDateBase）。</summary>
        public void SaveStateBeforeShutdown()
        {
            try
            {
                SharedService.Youtube.YoutubeStreamService.SaveDateBase();
                Log.Info("[Scraper] 已保存偵測資料庫狀態");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "[Scraper] 關閉前保存資料庫失敗");
            }
        }
    }
}
