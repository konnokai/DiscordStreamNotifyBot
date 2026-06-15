using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.HttpClients;
using DiscordStreamNotifyBot.HttpClients.Twitcasting.Model;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using System.Runtime.InteropServices;

using Bot = DiscordStreamNotifyBot.Shared.BotState;

namespace DiscordStreamNotifyBot.Scraper.Detection.Twitcasting
{
    /// <summary>
    /// TwitCasting 偵測服務（Scraper 專用）：分類 / WebHook 輪詢 Timer、開台 Redis 訂閱、錄影，
    /// 偵測到開台時 publish <see cref="TwitcastingNotification"/> 至通知匯流排（不碰 Discord gateway）。
    /// 指令支援與通知發送由 Notifier 端的 TwitcastingService 負責。
    /// </summary>
    public class TwitcastingDetectionService
    {
        public bool IsEnable { get; private set; } = true;

        private readonly TwitcastingClient _twitcastingClient;
        private readonly MainDbService _dbService;
        private readonly BotConfig _botConfig;

        private List<Category> categories;
        private string twitcastingRecordPath = "";

        public TwitcastingDetectionService(TwitcastingClient twitcastingClient, BotConfig botConfig, MainDbService dbService)
        {
            if (string.IsNullOrEmpty(botConfig.TwitCastingClientId) || string.IsNullOrEmpty(botConfig.TwitCastingClientSecret))
            {
                Log.Warn($"{nameof(botConfig.TwitCastingClientId)} 或 {nameof(botConfig.TwitCastingClientSecret)} 遺失，無法運行 TwitCasting 偵測");
                IsEnable = false;
                return;
            }

            _twitcastingClient = twitcastingClient;
            _botConfig = botConfig;
            _dbService = dbService;

            twitcastingRecordPath = botConfig.TwitCastingRecordPath;
            if (string.IsNullOrEmpty(twitcastingRecordPath)) twitcastingRecordPath = Utility.GetDataFilePath("");
            if (!twitcastingRecordPath.EndsWith(Utility.GetPlatformSlash())) twitcastingRecordPath += Utility.GetPlatformSlash();

            // 偵測排程（計畫 §12.1）：PeriodicTimer 背景輪詢，await 友善、無重入、吃 CancellationToken
            var token = GracefulShutdown.Token;
            PeriodicRunner.RunAsync("TwitCasting-categories", TimeSpan.FromSeconds(3), TimeSpan.FromMinutes(30), async () =>
            {
                categories = await _twitcastingClient.GetCategoriesAsync();
            }, token);

            PeriodicRunner.RunAsync("TwitCasting-webhook", TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(15), TimerHandel, token);

            Bot.RedisSub.Subscribe(new RedisChannel("twitcasting.pubsub.startlive", RedisChannel.PatternMode.Literal), async (channel, message) =>
            {
                var webHookJson = JsonConvert.DeserializeObject<TwitCastingWebHookJson>(message);
                if (webHookJson == null)
                {
                    Log.Error("TwitCasting WebHook JSON 反序列化失敗");
                    return;
                }

                using var db = _dbService.GetDbContext();
                if (await db.TwitcastingStreams.AsNoTracking().AnyAsync((x) => x.StreamId == int.Parse(webHookJson.Movie.Id)))
                {
                    Log.Warn($"TwitCasting 重複開台通知: {webHookJson.Movie.Id} - {webHookJson.Movie.Title}");
                    return;
                }

                bool isRecord = db.TwitcastingSpider.SingleOrDefault((x) => x.ScreenId == webHookJson.Broadcaster.Id)?.IsRecord ?? false;
                var twitcastingStream = new TwitcastingStream()
                {
                    ChannelId = webHookJson.Broadcaster.Id,
                    ChannelTitle = webHookJson.Broadcaster.Name,
                    StreamId = int.Parse(webHookJson.Movie.Id),
                    StreamTitle = webHookJson.Movie.Title ?? "無標題",
                    StreamSubTitle = webHookJson.Movie.Subtitle,
                    Category = GetCategorieNameById(webHookJson.Movie.Category),
                    ThumbnailUrl = webHookJson.Movie.LargeThumbnail,
                    StreamStartAt = UnixTimeStampToDateTime(webHookJson.Movie.Created)
                };

                await db.TwitcastingStreams.AddAsync(twitcastingStream);
                await db.SaveChangesAsync();

                await PublishStartLiveAsync(twitcastingStream, webHookJson.Movie.IsProtected,
                    !webHookJson.Movie.IsProtected && isRecord && RecordTwitCasting(twitcastingStream));
            });
        }

        private async Task TimerHandel()
        {
#if DEBUG
            return;
#endif

            // PeriodicTimer 保證單一迴圈不重疊，無需 isRuning 重入旗標（§12.1）
            using var db = _dbService.GetDbContext();
            var spiderList = db.TwitcastingSpider.AsNoTracking().ToList();

            try
            {
                // 取得所有已註冊的 webhook
                var registeredWebhooks = await _twitcastingClient.GetAllRegistedWebHookAsync();
                if (registeredWebhooks == null)
                {
                    Log.Error("TwitCastingService-Timer: 無法獲取已註冊的 Webhook 列表，請檢查 TwitCasting API 設定是否正確。");
                    return;
                }
                var registeredChannelIds = registeredWebhooks.Select(x => x.UserId).ToHashSet();

                // 需要註冊 webhook 的頻道
                var spiderChannelIds = spiderList.Where((x) => !string.IsNullOrEmpty(x.ChannelId)).Select(x => x.ChannelId).ToHashSet();

                // 註冊缺少的 webhook
                foreach (var channelId in spiderChannelIds.Except(registeredChannelIds))
                {
                    await _twitcastingClient.RegisterWebHookAsync(channelId);
                    Log.Info($"註冊 TwitCasting Webhook: {channelId}");
                }

                // 移除多餘的 webhook
                foreach (var channelId in registeredChannelIds.Except(spiderChannelIds))
                {
                    await _twitcastingClient.RemoveWebHookAsync(channelId);
                    Log.Info($"移除 TwitCasting Webhook: {channelId}");
                }
            }
            catch (Exception ex) { Log.Error(ex.Demystify(), "TwitCastingService-Timer"); }

            await db.SaveChangesAsync();
        }

        /// <summary>偵測到開台：publish DTO 至通知匯流排（取代直接送 Discord）。</summary>
        private async Task PublishStartLiveAsync(TwitcastingStream twitcastingStream, bool isPrivate, bool isRecord)
        {
#if DEBUG
            Log.New($"TwitCasting 開台通知: {twitcastingStream.ChannelTitle} - {twitcastingStream.StreamTitle} (isPrivate: {isPrivate})");
#else
            try
            {
                await NotificationBusPublisher.PublishJsonAsync(_botConfig.RabbitMQ,
                    NotifyRoutingKeys.Twitcasting,
                    new TwitcastingNotification
                    {
                        ChannelId = twitcastingStream.ChannelId,
                        ChannelTitle = twitcastingStream.ChannelTitle,
                        StreamId = twitcastingStream.StreamId,
                        StreamTitle = twitcastingStream.StreamTitle,
                        StreamSubTitle = twitcastingStream.StreamSubTitle,
                        Category = twitcastingStream.Category,
                        ThumbnailUrl = twitcastingStream.ThumbnailUrl,
                        StreamStartAt = twitcastingStream.StreamStartAt,
                        IsPrivate = isPrivate,
                        IsRecord = isRecord,
                    }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"PublishTwitcastingStartLive: {twitcastingStream.ChannelId} / {twitcastingStream.StreamId}");
            }
#endif
        }

        private bool RecordTwitCasting(TwitcastingStream twitcastingStream)
        {
            Log.Info($"{twitcastingStream.ChannelTitle} ({twitcastingStream.StreamId}): {twitcastingStream.StreamTitle}");

            try
            {
                if (!Directory.Exists(twitcastingRecordPath))
                    Directory.CreateDirectory(twitcastingRecordPath);
            }
            catch (Exception ex)
            {
                Log.Error($"TwitCasting 保存路徑不存在且不可建立: {twitcastingRecordPath}");
                Log.Error($"更改保存路徑至Data資料夾: {Utility.GetDataFilePath("")}");
                Log.Error(ex.ToString());

                twitcastingRecordPath = Utility.GetDataFilePath("");
            }

            // 自幹 Tc 錄影能錄但時間會出問題，還是用 StreamLink 方案好了
            string procArgs = $"streamlink https://twitcasting.tv/{twitcastingStream.ChannelId} best --output \"{twitcastingRecordPath}[{twitcastingStream.ChannelId}]{twitcastingStream.StreamStartAt:yyyyMMdd} - {twitcastingStream.StreamId}.ts\"";
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) Process.Start("tmux", $"new-window -d -n \"TwitCasting {twitcastingStream.ChannelId}\" {procArgs}");
                else Process.Start(new ProcessStartInfo()
                {
                    FileName = "streamlink",
                    Arguments = procArgs.Replace("streamlink", ""),
                    CreateNoWindow = false,
                    UseShellExecute = true
                });

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "RecordTwitCasting 失敗，請確認是否已安裝 StreamLink");
                return false;
            }
        }

        // https://stackoverflow.com/questions/249760/how-can-i-convert-a-unix-timestamp-to-datetime-and-vice-versa
        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }

        private string GetCategorieNameById(string categorieId)
        {
            string result = categorieId;

            if (categories != null && categories.Any())
            {
                foreach (var item in categories)
                {
                    var subCategory = item.SubCategories.FirstOrDefault((x) => x.Id == categorieId);
                    if (subCategory != null)
                        result = subCategory.Name;
                }
            }

            return result;
        }
    }
}
