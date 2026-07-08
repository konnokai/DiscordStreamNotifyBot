using System.Text.RegularExpressions;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Exceptions;
using Clip = TwitchLib.Api.Helix.Models.Clips.GetClips.Clip;
using Stream = TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream;
using User = TwitchLib.Api.Helix.Models.Users.GetUsers.User;
using Video = TwitchLib.Api.Helix.Models.Videos.GetVideos.Video;

using Bot = DiscordStreamNotifyBot.Shared.BotState;

namespace DiscordStreamNotifyBot.SharedService.Twitch
{
    /// <summary>
    /// Twitch 無狀態 API 存取（Shared 單一來源）：封裝 <see cref="TwitchAPI"/> 與 Helix 呼叫、EventSub CRUD、
    /// WebHook secret 維護。偵測（Scraper）與指令/發送（Notifier）皆透過本服務呼叫 Twitch，避免重複實作。
    /// </summary>
    public class TwitchApiService
    {
        public bool IsEnable { get; private set; } = true;
        public Lazy<TwitchAPI> TwitchApi { get; }
        public string ApiServerUrl { get; }
        public string WebHookSecret { get; private set; }

        private readonly Regex _userLoginRegex = new(@"twitch.tv/(?<name>[\w\d\-_]+)/?",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public TwitchApiService(BotConfig botConfig)
        {
            if (string.IsNullOrEmpty(botConfig.TwitchClientId) || string.IsNullOrEmpty(botConfig.TwitchClientSecret))
            {
                Log.Warn($"{nameof(botConfig.TwitchClientId)} 或 {nameof(botConfig.TwitchClientSecret)} 遺失，無法運行 Twitch 類功能");
                IsEnable = false;
                return;
            }

            try
            {
                WebHookSecret = Bot.RedisDb.StringGet("twitch:webhook_secret");
                if (string.IsNullOrEmpty(WebHookSecret))
                {
                    Log.Warn("缺少 TwitchWebHookSecret，嘗試重新建立...");

                    WebHookSecret = BotConfig.GenRandomKey(64);
                    Bot.RedisDb.StringSet("twitch:webhook_secret", WebHookSecret);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "獲取 TwitchWebHookSecret 失敗，無法運行 Twitch 類功能");
                IsEnable = false;
                return;
            }

            ApiServerUrl = botConfig.ApiServerDomain;

            TwitchApi = new(() => new()
            {
                Helix =
                {
                    Settings =
                    {
                        ClientId = botConfig.TwitchClientId,
                        Secret = botConfig.TwitchClientSecret
                    }
                }
            });
        }

        public string GetUserLoginByUrl(string url)
        {
            url = url.Split('?')[0];

            var match = _userLoginRegex.Match(url);
            if (match.Success)
            {
                url = match.Groups["name"].Value;
            }

            return url;
        }

        // Generate by ChatGPT
        public TimeSpan ParseToTimeSpan(string input)
        {
            int days = 0, hours = 0, minutes = 0, seconds = 0;
            // 定義正則表達式去匹配天、時、分、秒
            Regex regex = new Regex(@"(\d+)d|(\d+)h|(\d+)m|(\d+)s");
            MatchCollection matches = regex.Matches(input);
            // 遍歷匹配結果並賦值
            foreach (Match match in matches)
            {
                if (match.Groups[1].Success)
                    days = int.Parse(match.Groups[1].Value);
                if (match.Groups[2].Success)
                    hours = int.Parse(match.Groups[2].Value);
                if (match.Groups[3].Success)
                    minutes = int.Parse(match.Groups[3].Value);
                if (match.Groups[4].Success)
                    seconds = int.Parse(match.Groups[4].Value);
            }
            return new TimeSpan(days, hours, minutes, seconds);
        }

        public async Task<bool> CreateEventSubSubscriptionAsync(string broadcasterUserId)
        {
            try
            {
                var eventSubList = await TwitchApi.Value.Helix.EventSub.GetEventSubSubscriptionsAsync(userId: broadcasterUserId);

                if (!eventSubList.Subscriptions.Any((x) => x.Type == "channel.update"))
                {
                    await TwitchApi.Value.Helix.EventSub.CreateEventSubSubscriptionAsync("channel.update", "2", new() { { "broadcaster_user_id", broadcasterUserId } },
                        EventSubTransportMethod.Webhook, webhookCallback: $"https://{ApiServerUrl}/TwitchWebHooks", webhookSecret: WebHookSecret);
                }

                if (!eventSubList.Subscriptions.Any((x) => x.Type == "stream.offline"))
                {
                    await TwitchApi.Value.Helix.EventSub.CreateEventSubSubscriptionAsync("stream.offline", "1", new() { { "broadcaster_user_id", broadcasterUserId } },
                        EventSubTransportMethod.Webhook, webhookCallback: $"https://{ApiServerUrl}/TwitchWebHooks", webhookSecret: WebHookSecret);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"註冊 {broadcasterUserId} 的 Twitch WebHook 失敗，也許是已經註冊過了?");
            }

            return false;
        }

        #region TwitchAPI
        public async Task<User> GetUserAsync(string twitchUserId = "", string twitchUserLogin = "")
        {
            List<string> userId = null, userLogin = null;
            if (!string.IsNullOrEmpty(twitchUserId))
                userId = new List<string> { twitchUserId };
            else if (!string.IsNullOrEmpty(twitchUserLogin))
                userLogin = new List<string> { twitchUserLogin };
            else throw new ArgumentException("兩者參數不可同時為空");

            try
            {
                var users = await TwitchApi.Value.Helix.Users.GetUsersAsync(userId, userLogin);
                return users.Users.FirstOrDefault();
            }
            catch (BadRequestException)
            {
                Log.Error($"無法取得 Twitch 資料，可能是找不到輸入的使用者資料: ({twitchUserId}) {twitchUserLogin}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"無法取得 Twitch 資料: ({twitchUserId}) {twitchUserLogin}");
                return null;
            }
        }

        public async Task<IReadOnlyList<User>> GetUsersAsync(params string[] twitchUserLogins)
        {
            try
            {
                List<User> result = new();
                foreach (var item in twitchUserLogins.Chunk(100))
                {
                    var users = await TwitchApi.Value.Helix.Users.GetUsersAsync(logins: [.. item]);
                    if (users.Users.Length != 0)
                    {
                        result.AddRange(users.Users);
                    }
                }

                return result;
            }
            catch (BadRequestException)
            {
                Log.Error($"無法取得 Twitch 資料，可能是找不到輸入的使用者資料: {twitchUserLogins.First()}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"無法取得 Twitch 資料: {twitchUserLogins.First()}");
                return null;
            }
        }

        public async Task<Video> GetLatestVODAsync(string twitchUserId)
        {
            try
            {
                var videosResponse = await TwitchApi.Value.Helix.Videos.GetVideosAsync(userId: twitchUserId, first: 1, type: VideoType.Archive);
                return videosResponse.Videos.FirstOrDefault();
            }
            catch (BadRequestException)
            {
                Log.Error($"無法取得 Twitch 資料，可能是找不到輸入的使用者資料: {twitchUserId}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"無法取得 Twitch 資料: {twitchUserId}");
                return null;
            }
        }

        public async Task<IReadOnlyList<Clip>> GetClipsAsync(string twitchUserId, DateTime startedAt, DateTime endedAt)
        {
            try
            {
                var clipsResponse = await TwitchApi.Value.Helix.Clips.GetClipsAsync(broadcasterId: twitchUserId, startedAt: startedAt, endedAt: endedAt, first: 5);
                if (clipsResponse.Clips.Any())
                {
                    return clipsResponse.Clips;
                }
                else
                {
                    return null;
                }
            }
            catch (BadRequestException)
            {
                Log.Error($"無法取得 Twitch 資料，可能是找不到輸入的使用者資料: {twitchUserId}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"無法取得 Twitch 資料: {twitchUserId}");
                return null;
            }
        }

        public async Task<IReadOnlyList<Stream>> GetNowStreamsAsync(params string[] twitchUserIds)
        {
            try
            {
                List<Stream> result = new();
                foreach (var item in twitchUserIds.Chunk(100))
                {
                    var streams = await TwitchApi.Value.Helix.Streams.GetStreamsAsync(first: 100, userIds: [.. twitchUserIds]);
                    if (streams.Streams.Length != 0)
                    {
                        result.AddRange(streams.Streams);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"無法取得 Twitch 資料，請確認 {nameof(BotConfig.TwitchClientId)} 或 {nameof(BotConfig.TwitchClientSecret)} 是否正常");
                return Array.Empty<Stream>();
            }
        }

        public async Task<IReadOnlyList<TwitchLib.Api.Helix.Models.EventSub.EventSubSubscription>> GetEventSubSubscriptionsAsync(string userId = null)
        {
            try
            {
                var eventSubList = await TwitchApi.Value.Helix.EventSub.GetEventSubSubscriptionsAsync(userId: userId);
                if (eventSubList.Subscriptions.Length != 0)
                {
                    return eventSubList.Subscriptions;
                }
                else
                {
                    return null;
                }
            }
            catch (BadRequestException)
            {
                Log.Error($"無法取得 Twitch 資料，可能是找不到輸入的使用者資料: {userId}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"無法取得 Twitch 資料: {userId}");
                return null;
            }
        }

        public async Task<bool> DeleteEventSubSubscriptionAsync(string userId)
        {
            try
            {
                var list = await TwitchApi.Value.Helix.EventSub.GetEventSubSubscriptionsAsync(userId: userId);
                foreach (var item in list.Subscriptions)
                {
                    Log.Info($"Delete EventSub: {item.Id} ({item.Type})");
                    await TwitchApi.Value.Helix.EventSub.DeleteEventSubSubscriptionAsync(item.Id);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"Event Delete Error: {userId}");
                return false;
            }
        }
        #endregion
    }
}
