using DiscordStreamNotifyBot.DataBase;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using HtmlAgilityPack;
using Polly;
using System.Net;
using System.Text.RegularExpressions;
using YTApiVideo = Google.Apis.YouTube.v3.Data.Video;

namespace DiscordStreamNotifyBot.Shared
{
    /// <summary>
    /// 無狀態 YouTube API 封裝（計畫 §2.1）：頻道 / 影片查詢、PubSubHubbub 訂閱請求。
    /// 不依賴 Discord 或 <c>Bot</c> 靜態狀態，偵測層（Scraper）與指令層（Notifier）皆可直接使用。
    /// </summary>
    public class YoutubeApiService
    {
        public YouTubeService YouTubeService { get; }

        private readonly MainDbService _dbService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiServerUrl;

        public YoutubeApiService(BotConfig botConfig, MainDbService dbService, IHttpClientFactory httpClientFactory)
        {
            _dbService = dbService;
            _httpClientFactory = httpClientFactory;
            _apiServerUrl = botConfig.ApiServerDomain;

            YouTubeService = new YouTubeService(new BaseClientService.Initializer
            {
                ApplicationName = "DiscordStreamBot",
                ApiKey = botConfig.GoogleApiKey,
            });
        }

        public async Task<string> GetChannelIdAsync(string channelUrl)
        {
            if (string.IsNullOrEmpty(channelUrl))
                throw new ArgumentNullException(channelUrl);

            channelUrl = channelUrl.Trim();

            switch (channelUrl.ToLower())
            {
                case "all":
                case "holo":
                case "2434":
                case "other":
                    return channelUrl.ToLower();
            }

            if (channelUrl.StartsWith("UC") && channelUrl.Length == 24)
                return channelUrl;

            string channelId;

            channelUrl = channelUrl.Replace("https://m.youtube.com", "https://www.youtube.com");
            channelUrl = channelUrl.Split('?')[0]; // 移除網址上的參數

            Regex regexNewFormat = new Regex(@"(http[s]{0,1}://){0,1}(www\.){0,1}(?'Host'[^/]+)/@(?'CustomId'[^/]+)");
            Regex regexOldFormat = new Regex(@"(http[s]{0,1}://){0,1}(www\.){0,1}(?'Host'[^/]+)/(?'Type'[^/]+)/(?'ChannelName'[\w%\-]+)");
            Match matchNewFormat = regexNewFormat.Match(channelUrl);
            Match matchOldFormat = regexOldFormat.Match(channelUrl);

            if (matchNewFormat.Success)
            {
                string channelName = matchNewFormat.Groups["CustomId"].Value.ToLower();

                using (var db = _dbService.GetDbContext())
                {
                    try
                    {
                        channelId = db.YoutubeChannelNameToId.SingleOrDefault((x) => x.ChannelName == channelName)?.ChannelId;

                        if (string.IsNullOrEmpty(channelId))
                        {
                            try
                            {
                                channelId = await GetChannelIdByUrlAsync($"https://www.youtube.com/@{channelName}");
                                db.YoutubeChannelNameToId.Add(new DataBase.Table.YoutubeChannelNameToId() { ChannelName = channelName, ChannelId = channelId });
                                await db.SaveChangesAsync();
                            }
                            catch (UriFormatException ex)
                            {
                                Log.Error(ex.Demystify(), $"GetChannelIdAsync-GetChannelIdByUrlAsync-UriFormatException: {channelUrl}");
                                throw;
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.Demystify(), $"GetChannelIdAsync-GetChannelIdByUrlAsync-Exception: {channelUrl}");
                                throw;
                            }
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        Log.Error(ex.Demystify(), $"GetChannelIdAsync-GetChannelIdByUrlAsync-InvalidOperationException: {channelUrl}");
                        throw new FormatException("網址格式化錯誤，請向 Bot 擁有者回報此問題");
                    }
                }
            }
            else if (matchOldFormat.Success)
            {
                string host = matchOldFormat.Groups["Host"].Value.ToLower();
                if (host != "youtube.com")
                    throw new FormatException("錯誤，請確認是否輸入 YouTube 頻道網址");

                string type = matchOldFormat.Groups["Type"].Value.ToLower();
                if (type == "channel")
                {
                    channelId = matchOldFormat.Groups["ChannelName"].Value;
                    if (!channelId.StartsWith("UC")) throw new FormatException("錯誤，頻道 Id 格式不正確");
                    if (channelId.Length != 24) throw new FormatException("錯誤，頻道 Id 字元數不正確");
                }
                else if (type == "c" || type == "user")
                {
                    string channelName = WebUtility.UrlDecode(matchOldFormat.Groups["ChannelName"].Value);

                    using (var db = _dbService.GetDbContext())
                    {
                        channelId = db.YoutubeChannelNameToId.SingleOrDefault((x) => x.ChannelName == channelName)?.ChannelId;

                        if (string.IsNullOrEmpty(channelId))
                        {
                            try
                            {
                                channelId = await GetChannelIdByUrlAsync($"https://www.youtube.com/{type}/{channelName}");
                                db.YoutubeChannelNameToId.Add(new DataBase.Table.YoutubeChannelNameToId() { ChannelName = channelName, ChannelId = channelId });
                                await db.SaveChangesAsync();
                            }
                            catch (UriFormatException ex)
                            {
                                Log.Error(ex.Demystify(), $"GetChannelIdAsync-GetChannelIdByUrlAsync-UriFormatException: {channelUrl}");
                                throw;
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.Demystify(), $"GetChannelIdAsync-GetChannelIdByUrlAsync-Exception: {channelUrl}");
                                throw;
                            }
                        }
                    }
                }
                else throw new FormatException("錯誤，網址格式不正確");
            }
            else
            {
                Log.Error($"GetChannelIdAsync-NoMatch: {channelUrl}");
                throw new FormatException("錯誤，找不到對應的網址處理方式，請向 Bot 擁有者聯絡\n" +
                    "若你是透過自動提示來輸入頻道名稱，請勿切換 Discord 頻道，這會導致自動代入的名稱錯誤");
            }

            return channelId;
        }

        private async Task<string> GetChannelIdByUrlAsync(string channelUrl)
        {
            try
            {
                string channelId = "";

                HtmlWeb htmlWeb = new HtmlWeb();
                var htmlDocument = await htmlWeb.LoadFromWebAsync(channelUrl);
                var node = htmlDocument.DocumentNode.Descendants().FirstOrDefault((x) => x.Name == "meta" && x.Attributes.Any((x2) => x2.Name == "itemprop" && x2.Value == "channelId" || x2.Value == "identifier"));

                if (node == null)
                    throw new UriFormatException("錯誤，找不到節點\n" +
                        "請確認是否輸入正確的 YouTube 頻道網址，或確認該頻道是否存在\n" +
                        "部分頻道會有跳轉後遇到 404 錯誤的問題，你可以嘗試直接開啟連結確認是否會出現 404 錯誤\n" +
                        "有需要可直接向 Bot 擁有者詢問\n" +
                        "(你可以使用 `/utility send-message-to-bot-owner` 指令來聯絡 Bot 擁有者)");

                channelId = node.Attributes.FirstOrDefault((x) => x.Name == "content").Value;
                if (string.IsNullOrEmpty(channelId))
                    throw new UriFormatException("錯誤，找不到頻道 Id\n" +
                        "正常來說不該遇到這問題才對，請直接向 Bot 擁有者詢問\n" +
                        "(你可以使用 `/utility send-message-to-bot-owner` 指令來聯絡 Bot 擁有者)");

                return channelId;
            }
            catch { throw; }
        }

        public string GetVideoId(string videoUrl)
        {
            if (string.IsNullOrEmpty(videoUrl))
                throw new ArgumentNullException(videoUrl);

            videoUrl = videoUrl.Trim();

            if (videoUrl.Contains("www.youtube.com/watch")) //https://www.youtube.com/watch?v=7DqDRE_SW34
                videoUrl = videoUrl.Substring(videoUrl.IndexOf("?v=") + 3, 11);
            else if (videoUrl.Contains("https://youtu.be")) //https://youtu.be/Z-UJbyLqioM
                videoUrl = videoUrl.Substring(17, 11);
            else if (videoUrl.Contains("https://www.youtube.com/live/")) //https://www.youtube.com/live/MdmQgxffY6k?feature=share
                videoUrl = videoUrl.Substring(29, 11);

            if (videoUrl.Length == 11)
                return videoUrl;

            Regex regex = new Regex(@"(?:https?:)?(?:\/\/)?(?:[0-9A-Z-]+\.)?(?:youtu\.be\/|youtube(?:-nocookie)?\.com\S*?[^\w\s-])(?'VideoId'[\w-]{11})(?=[^\w-]|$)(?![?=&+%\w.-]*(?:['""][^<>]*>|<\/a>))[?=&+%\w.-]*"); //https://regex101.com/r/OY96XI/1
            Match match = regex.Match(videoUrl);
            if (!match.Success)
                throw new UriFormatException("錯誤，請確認是否輸入 YouTube 影片網址");

            return match.Groups["VideoId"].Value;
        }

        public async Task<string> GetChannelTitle(string channelId)
        {
            try
            {
                if (channelId.Length != 24)
                    return "";

                var channel = YouTubeService.Channels.List("snippet");
                channel.Id = channelId;
                var response = await channel.ExecuteAsync().ConfigureAwait(false);
                return response.Items[0].Snippet.Title;
            }
            catch (NullReferenceException)
            {
                Log.Warn($"YouTube GetChannelTitle 可能已被刪除的頻道: {channelId}");
                return "";
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"YouTube GetChannelTitle 未知的錯誤: {channelId}");
                return "";
            }
        }

        public async Task<List<string>> GetChannelTitle(IEnumerable<string> channelId, bool formatUrl)
        {
            try
            {
                var channel = YouTubeService.Channels.List("snippet");
                channel.Id = string.Join(",", channelId);
                var response = await channel.ExecuteAsync().ConfigureAwait(false);
                if (formatUrl) return response.Items.Select((x) => Format.Url(x.Snippet.Title, $"https://www.youtube.com/channel/{x.Id}")).ToList();
                else return response.Items.Select((x) => x.Snippet.Title).ToList();
            }
            catch (NullReferenceException)
            {
                Log.Warn($"YouTube GetChannelTitle 可能已被刪除的頻道: {channelId}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"YouTube GetChannelTitles 未知的錯誤: {string.Join(", ", channelId)}");
                return null;
            }
        }

        public async Task<YTApiVideo> GetVideoAsync(string videoId)
        {
            var pBreaker = Policy<YTApiVideo>
                .Handle<Exception>()
                .WaitAndRetryAsync(3, (retryAttempt) =>
                {
                    var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                    Log.Warn($"YouTube GetVideoAsync ({videoId}) 失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                    return timeSpan;
                });

            return await pBreaker.ExecuteAsync(async () =>
            {
                var video = YouTubeService.Videos.List("snippet,liveStreamingDetails");
                video.Id = videoId;
                var videoResult = await video.ExecuteAsync().ConfigureAwait(false);
                if (videoResult.Items.Count == 0) return null;
                return videoResult.Items[0];
            });
        }

        public async Task<IEnumerable<YTApiVideo>> GetVideosAsync(IEnumerable<string> videoIds)
        {
            var pBreaker = Policy<IEnumerable<YTApiVideo>>
                .Handle<Exception>()
                .WaitAndRetryAsync(3, (retryAttempt) =>
                {
                    var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                    Log.Warn($"YouTube GetVideosAsync ({videoIds.Count()}) 失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                    return timeSpan;
                });

            return await pBreaker.ExecuteAsync(async () =>
            {
                var video = YouTubeService.Videos.List("snippet,liveStreamingDetails");
                video.Id = string.Join(',', videoIds);
                var videoResult = await video.ExecuteAsync().ConfigureAwait(false);
                if (videoResult.Items.Count == 0) return null;
                return videoResult.Items;
            });
        }

        //https://github.com/JulianusIV/PubSubHubBubReciever/blob/master/DefaultPlugins/YouTubeConsumer/YouTubeConsumerPlugin.cs
        public async Task<bool> PostSubscribeRequestAsync(string channelId, bool subscribe = true)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage();

                request.RequestUri = new("https://pubsubhubbub.appspot.com/subscribe");
                request.Method = HttpMethod.Post;
                string guid = Guid.NewGuid().ToString();

                var formList = new Dictionary<string, string>()
                {
                    { "hub.mode", subscribe ? "subscribe" : "unsubscribe" },
                    { "hub.topic", $"https://www.youtube.com/xml/feeds/videos.xml?channel_id={channelId}" },
                    { "hub.callback", $"https://{_apiServerUrl}/NotificationCallback" },
                    { "hub.verify", "async" },
                    { "hub.secret", guid },
                    { "hub.verify_token", guid },
                    { "hub.lease_seconds", "864000"}
                };

                request.Content = new FormUrlEncodedContent(formList);
                var response = await httpClient.SendAsync(request);
                var result = response.StatusCode == HttpStatusCode.Accepted;
                if (!result)
                {
                    Log.Error($"{channelId} PubSub 註冊失敗");
                    Log.Error(response.StatusCode + " - " + await response.Content.ReadAsStringAsync());
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"{channelId} PubSub 註冊失敗");
                return false;
            }
        }
    }
}
