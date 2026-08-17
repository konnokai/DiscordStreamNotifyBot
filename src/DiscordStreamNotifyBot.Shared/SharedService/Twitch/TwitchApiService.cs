using DiscordStreamNotifyBot.Shared;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Helix.Models.EventSub;
using Bot = DiscordStreamNotifyBot.Shared.BotState;
using Clip = TwitchLib.Api.Helix.Models.Clips.GetClips.Clip;
using Stream = TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream;
using User = TwitchLib.Api.Helix.Models.Users.GetUsers.User;
using Video = TwitchLib.Api.Helix.Models.Videos.GetVideos.Video;

namespace DiscordStreamNotifyBot.SharedService.Twitch
{
    /// <summary>
    /// Twitch 無狀態 API 存取（Shared 單一來源）：封裝 <see cref="TwitchAPI"/> 與 Helix 呼叫、EventSub CRUD、
    /// WebHook secret 維護。偵測（Scraper）與指令／發送（Notifier）皆透過本服務呼叫 Twitch，避免重複實作。
    /// </summary>
    public class TwitchApiService
    {
        public bool IsEnable { get; private set; } = true;
        public Lazy<TwitchAPI> TwitchApi { get; }
        public string ApiServerUrl { get; }
        public string EventSubCallbackUrl { get; }
        public string WebHookSecret { get; private set; }

        private static readonly HttpClient OAuthHttpClient = new();
        private static readonly Uri TwitchTokenEndpoint = new("https://id.twitch.tv/oauth2/token");
        private readonly string _twitchClientId;
        private readonly string _twitchClientSecret;
        private readonly SemaphoreSlim _appTokenLock = new(1, 1);
        private string _appAccessToken;
        private DateTime _appAccessTokenExpiresAtUtc;
        private readonly Regex _userLoginRegex = new(@"twitch.tv/(?<name>[\w\d\-_]+)/?",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public TwitchApiService(BotConfig botConfig)
        {
            if (string.IsNullOrEmpty(botConfig.TwitchClientId) || string.IsNullOrEmpty(botConfig.TwitchClientSecret))
            {
                Log.Warn($"{nameof(botConfig.TwitchClientId)} 或 {nameof(botConfig.TwitchClientSecret)} 遺失，無法使用 Twitch 相關功能");
                IsEnable = false;
                return;
            }

            _twitchClientId = botConfig.TwitchClientId;
            _twitchClientSecret = botConfig.TwitchClientSecret;

            try
            {
                WebHookSecret = Bot.RedisDb.StringGet(RedisChannels.Twitch.WebhookSecret);
                if (string.IsNullOrEmpty(WebHookSecret))
                {
                    Log.Warn("缺少 TwitchWebHookSecret，嘗試重新建立…");

                    var candidate = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
                    Bot.RedisDb.StringSet(RedisChannels.Twitch.WebhookSecret, candidate, when: When.NotExists);
                    WebHookSecret = Bot.RedisDb.StringGet(RedisChannels.Twitch.WebhookSecret);
                    if (string.IsNullOrEmpty(WebHookSecret))
                        throw new InvalidOperationException("建立 TwitchWebHookSecret 後無法自 Redis 讀回");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "取得 TwitchWebHookSecret 失敗，無法使用 Twitch 相關功能");
                IsEnable = false;
                return;
            }

            ApiServerUrl = botConfig.ApiServerDomain;
            try
            {
                EventSubCallbackUrl = NormalizeEventSubCallbackUrl(ApiServerUrl);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "Twitch EventSub callback URL 設定無效，無法使用 Twitch 相關功能");
                IsEnable = false;
                return;
            }

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

        private static string NormalizeEventSubCallbackUrl(string apiServerUrl)
        {
            if (string.IsNullOrWhiteSpace(apiServerUrl))
                throw new ArgumentException("API server URL 不可為空", nameof(apiServerUrl));

            string value = apiServerUrl.Trim();
            bool suppliedAbsoluteUri = Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) && !string.IsNullOrEmpty(uri.Host);
            if (!suppliedAbsoluteUri)
                Uri.TryCreate($"https://{value.TrimStart('/')}", UriKind.Absolute, out uri);

            if (uri == null || string.IsNullOrEmpty(uri.Host))
                throw new ArgumentException("API server URL 格式錯誤", nameof(apiServerUrl));
            if (uri.Scheme == Uri.UriSchemeHttp && !uri.IsLoopback)
                throw new ArgumentException("非本機 Twitch callback 必須使用 HTTPS", nameof(apiServerUrl));

            var builder = new UriBuilder(uri)
            {
                Port = uri.IsDefaultPort ? -1 : uri.Port,
                Path = "/TwitchWebHooks",
                Query = string.Empty,
                Fragment = string.Empty
            };
            return builder.Uri.AbsoluteUri.TrimEnd('/');
        }

        private async Task<string> GetAppAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_appAccessToken) && DateTime.UtcNow < _appAccessTokenExpiresAtUtc)
                return _appAccessToken;

            await _appTokenLock.WaitAsync();
            try
            {
                if (!string.IsNullOrEmpty(_appAccessToken) && DateTime.UtcNow < _appAccessTokenExpiresAtUtc)
                    return _appAccessToken;

                using var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = _twitchClientId,
                    ["client_secret"] = _twitchClientSecret,
                    ["grant_type"] = "client_credentials"
                });
                using var response = await OAuthHttpClient.PostAsync(TwitchTokenEndpoint, content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<TwitchAppAccessTokenResponse>(json);
                if (string.IsNullOrEmpty(tokenResponse?.AccessToken))
                    throw new InvalidOperationException("Twitch App Access Token 回應缺少 access_token");

                _appAccessToken = tokenResponse.AccessToken;
                _appAccessTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(Math.Max(0, tokenResponse.ExpiresIn - 60));
                return _appAccessToken;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "取得 Twitch App Access Token 失敗");
                throw;
            }
            finally
            {
                _appTokenLock.Release();
            }
        }

        private sealed class TwitchAppAccessTokenResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }
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
            // 定義規則運算式以比對天、時、分、秒
            Regex regex = new Regex(@"(\d+)d|(\d+)h|(\d+)m|(\d+)s");
            MatchCollection matches = regex.Matches(input);
            // 逐一處理比對結果，並將值指派給對應變數
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
            var result = await EnsureEventSubSubscriptionsAsync(broadcasterUserId, TwitchEventSubEnsureMode.Fallback);
            return result.IsSuccess;
        }

        public async Task<TwitchEventSubEnsureResult> EnsureEventSubSubscriptionsAsync(
            string broadcasterUserId, TwitchEventSubEnsureMode mode)
        {
            if (string.IsNullOrWhiteSpace(broadcasterUserId))
            {
                Log.Error("建立 Twitch EventSub 時 broadcaster user ID 不可為空");
                return new TwitchEventSubEnsureResult { Mode = mode };
            }
            if (!IsEnable)
                return new TwitchEventSubEnsureResult { Mode = mode };

            var current = await GetEventSubSubscriptionsResultAsync(broadcasterUserId);
            if (!current.IsSuccess)
                return CreateEnsureFailure(mode, current);

            var plan = TwitchEventSubReconcilePolicy.Plan(mode, broadcasterUserId, EventSubCallbackUrl,
                current.Subscriptions.Select(ToEventSubFact).ToArray());

            string appAccessToken;
            int deletedCount = 0;
            try
            {
                appAccessToken = await GetAppAccessTokenAsync();
                foreach (string subscriptionId in plan.Delete)
                {
                    bool deleted = await TwitchApi.Value.Helix.EventSub.DeleteEventSubSubscriptionAsync(
                        subscriptionId, clientId: _twitchClientId, accessToken: appAccessToken);
                    if (!deleted)
                        throw new InvalidOperationException($"Twitch EventSub 刪除 API 回傳失敗，subscription ID：{subscriptionId}");
                    deletedCount++;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"清理 broadcaster {broadcasterUserId} 的 Twitch EventSub 失敗");
                return new TwitchEventSubEnsureResult
                {
                    Mode = mode,
                    DeletedCount = deletedCount,
                    Subscriptions = current
                };
            }

            int createdCount = 0;
            try
            {
                foreach (var spec in plan.Create)
                {
                    await TwitchApi.Value.Helix.EventSub.CreateEventSubSubscriptionAsync(
                        spec.Type, spec.Version, new() { ["broadcaster_user_id"] = broadcasterUserId },
                        EventSubTransportMethod.Webhook, webhookCallback: EventSubCallbackUrl,
                        webhookSecret: WebHookSecret, clientId: _twitchClientId, accessToken: appAccessToken);
                    createdCount++;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"建立 broadcaster {broadcasterUserId} 的 Twitch EventSub 失敗");
                return new TwitchEventSubEnsureResult
                {
                    Mode = mode,
                    CreatedCount = createdCount,
                    DeletedCount = deletedCount,
                    Subscriptions = current
                };
            }

            var final = await GetEventSubSubscriptionsResultAsync(broadcasterUserId);
            var finalDecision = TwitchEventSubReconcilePolicy.EvaluateFinal(mode, broadcasterUserId,
                EventSubCallbackUrl, final.Subscriptions.Select(ToEventSubFact).ToArray());
            if (final.IsSuccess && !finalDecision.IsPermanentCostValid)
                Log.Warn($"broadcaster {broadcasterUserId} 的永久 Twitch EventSub 成本不是預期的 0");

            return new TwitchEventSubEnsureResult
            {
                IsSuccess = final.IsSuccess && finalDecision.IsSuccess,
                Mode = mode,
                CreatedCount = createdCount,
                DeletedCount = deletedCount,
                IsPermanentCostValid = finalDecision.IsPermanentCostValid,
                Subscriptions = final
            };
        }

        private TwitchEventSubEnsureResult CreateEnsureFailure(
            TwitchEventSubEnsureMode mode, TwitchEventSubSubscriptionsResult subscriptions)
            => new()
            {
                Mode = mode,
                Subscriptions = subscriptions
            };

        private static TwitchEventSubFact ToEventSubFact(EventSubSubscription subscription)
        {
            string broadcasterUserId = null;
            subscription.Condition?.TryGetValue("broadcaster_user_id", out broadcasterUserId);
            return new TwitchEventSubFact(
                subscription.Id,
                subscription.Type,
                subscription.Version,
                broadcasterUserId,
                subscription.Status,
                subscription.Transport?.Method,
                subscription.Transport?.Callback,
                subscription.Cost,
                subscription.Condition?.Count ?? 0);
        }

        #region TwitchAPI
        public async Task<User> GetUserAsync(string twitchUserId = "", string twitchUserLogin = "")
        {
            List<string> userId = null, userLogin = null;
            if (!string.IsNullOrEmpty(twitchUserId))
                userId = new List<string> { twitchUserId };
            else if (!string.IsNullOrEmpty(twitchUserLogin))
                userLogin = new List<string> { twitchUserLogin };
            else throw new ArgumentException("兩個參數不可同時為空");
            if (!IsEnable)
                return null;

            try
            {
                var users = await TwitchApi.Value.Helix.Users.GetUsersAsync(userId, userLogin);
                return users.Users.FirstOrDefault();
            }
            catch (BadRequestException)
            {
                Log.Error($"無法取得 Twitch 資料，可能找不到指定的使用者：({twitchUserId}) {twitchUserLogin}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"無法取得 Twitch 資料：({twitchUserId}) {twitchUserLogin}");
                return null;
            }
        }

        public async Task<IReadOnlyList<User>> GetUsersAsync(params string[] twitchUserLogins)
        {
            if (!IsEnable)
                return Array.Empty<User>();

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
                Log.Error($"無法取得 Twitch 資料，可能找不到指定的使用者：{twitchUserLogins.First()}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"無法取得 Twitch 資料：{twitchUserLogins.First()}");
                return null;
            }
        }

        public async Task<Video> GetLatestVODAsync(string twitchUserId)
        {
            if (!IsEnable)
                return null;

            try
            {
                var videosResponse = await TwitchApi.Value.Helix.Videos.GetVideosAsync(userId: twitchUserId, first: 1, type: VideoType.Archive);
                return videosResponse.Videos.FirstOrDefault();
            }
            catch (BadRequestException)
            {
                Log.Error($"無法取得 Twitch 資料，可能找不到指定的使用者：{twitchUserId}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"無法取得 Twitch 資料：{twitchUserId}");
                return null;
            }
        }

        public async Task<IReadOnlyList<Clip>> GetClipsAsync(string twitchUserId, DateTime startedAt, DateTime endedAt)
        {
            if (!IsEnable)
                return Array.Empty<Clip>();

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
                Log.Error($"無法取得 Twitch 資料，可能找不到指定的使用者：{twitchUserId}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"無法取得 Twitch 資料：{twitchUserId}");
                return null;
            }
        }

        public async Task<TwitchStreamsResult> GetNowStreamsResultAsync(params string[] twitchUserIds)
        {
            if (!IsEnable)
                return new TwitchStreamsResult();

            try
            {
                List<Stream> result = new();
                string appAccessToken = await GetAppAccessTokenAsync();
                foreach (var item in twitchUserIds.Chunk(100))
                {
                    var streams = await TwitchApi.Value.Helix.Streams.GetStreamsAsync(
                        first: 100, userIds: [.. item], accessToken: appAccessToken);
                    if (streams.Streams.Length != 0)
                    {
                        result.AddRange(streams.Streams);
                    }
                }

                return new TwitchStreamsResult { IsSuccess = true, Streams = result };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"無法取得 Twitch 資料，請確認 {nameof(BotConfig.TwitchClientId)} 或 {nameof(BotConfig.TwitchClientSecret)} 是否正常");
                return new TwitchStreamsResult();
            }
        }

        public async Task<IReadOnlyList<Stream>> GetNowStreamsAsync(params string[] twitchUserIds)
            => (await GetNowStreamsResultAsync(twitchUserIds)).Streams;

        public async Task<TwitchEventSubSubscriptionsResult> GetEventSubSubscriptionsResultAsync(string userId = null)
        {
            if (!IsEnable)
                return new TwitchEventSubSubscriptionsResult();

            try
            {
                string appAccessToken = await GetAppAccessTokenAsync();
                string cursor = null;
                var seenCursors = new HashSet<string>(StringComparer.Ordinal);
                var subscriptions = new List<EventSubSubscription>();
                int total = 0, totalCost = 0, maxTotalCost = 0;

                do
                {
                    var page = await TwitchApi.Value.Helix.EventSub.GetEventSubSubscriptionsAsync(
                        userId: userId, after: cursor, clientId: _twitchClientId, accessToken: appAccessToken);
                    subscriptions.AddRange(page.Subscriptions ?? Array.Empty<EventSubSubscription>());
                    total = page.Total;
                    totalCost = page.TotalCost;
                    maxTotalCost = page.MaxTotalCost;
                    cursor = page.Pagination?.Cursor;

                    if (!string.IsNullOrEmpty(cursor) && !seenCursors.Add(cursor))
                        throw new InvalidOperationException("Twitch EventSub 分頁回傳重複的 cursor");
                }
                while (!string.IsNullOrEmpty(cursor));

                return new TwitchEventSubSubscriptionsResult
                {
                    IsSuccess = true,
                    Subscriptions = subscriptions,
                    Total = total,
                    TotalCost = totalCost,
                    MaxTotalCost = maxTotalCost
                };
            }
            catch (BadRequestException ex)
            {
                Log.Error(ex.Demystify(), $"取得 broadcaster {userId} 的 Twitch EventSub 分頁遭 Twitch API 拒絕");
                return new TwitchEventSubSubscriptionsResult();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"取得 broadcaster {userId} 的 Twitch EventSub 分頁失敗");
                return new TwitchEventSubSubscriptionsResult();
            }
        }

        public async Task<IReadOnlyList<EventSubSubscription>> GetEventSubSubscriptionsAsync(string userId = null)
        {
            var result = await GetEventSubSubscriptionsResultAsync(userId);
            return result.IsSuccess && result.Subscriptions.Count != 0 ? result.Subscriptions : null;
        }

        public async Task<TwitchEventSubDeleteResult> DeleteEventSubSubscriptionResultAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                Log.Error("刪除 Twitch EventSub 時 broadcaster user ID 不可為空");
                return new TwitchEventSubDeleteResult { Status = TwitchEventSubDeleteStatus.ApiFailure };
            }
            if (!IsEnable)
                return new TwitchEventSubDeleteResult { Status = TwitchEventSubDeleteStatus.ApiFailure };

            var streams = await GetNowStreamsResultAsync(userId);
            if (!streams.IsSuccess)
                return new TwitchEventSubDeleteResult { Status = TwitchEventSubDeleteStatus.ApiFailure };
            if (streams.Streams.Any(x => x.UserId == userId))
                return new TwitchEventSubDeleteResult { Status = TwitchEventSubDeleteStatus.DeferredLive };

            var subscriptions = await GetEventSubSubscriptionsResultAsync(userId);
            if (!subscriptions.IsSuccess)
                return new TwitchEventSubDeleteResult { Status = TwitchEventSubDeleteStatus.ApiFailure };

            string[] subscriptionIds = subscriptions.Subscriptions.Select(x => x.Id)
                .Where(x => !string.IsNullOrEmpty(x)).Distinct(StringComparer.Ordinal).ToArray();
            if (subscriptionIds.Length == 0)
                return new TwitchEventSubDeleteResult { Status = TwitchEventSubDeleteStatus.NoSubscriptions };

            var deletedIds = new List<string>();
            try
            {
                string appAccessToken = await GetAppAccessTokenAsync();
                foreach (string subscriptionId in subscriptionIds)
                {
                    bool deleted = await TwitchApi.Value.Helix.EventSub.DeleteEventSubSubscriptionAsync(
                        subscriptionId, clientId: _twitchClientId, accessToken: appAccessToken);
                    if (!deleted)
                        throw new InvalidOperationException($"Twitch EventSub 刪除 API 回傳失敗，subscription ID：{subscriptionId}");
                    deletedIds.Add(subscriptionId);
                }

                Log.Info($"已刪除 broadcaster {userId} 的 {deletedIds.Count} 筆 Twitch EventSub");
                return new TwitchEventSubDeleteResult
                {
                    Status = TwitchEventSubDeleteStatus.Deleted,
                    DeletedSubscriptionIds = deletedIds
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"刪除 broadcaster {userId} 的 Twitch EventSub 失敗");
                return new TwitchEventSubDeleteResult
                {
                    Status = TwitchEventSubDeleteStatus.ApiFailure,
                    DeletedSubscriptionIds = deletedIds
                };
            }
        }

        public async Task<bool> DeleteEventSubSubscriptionAsync(string userId)
        {
            var result = await DeleteEventSubSubscriptionResultAsync(userId);
            return result.Status is TwitchEventSubDeleteStatus.Deleted or TwitchEventSubDeleteStatus.NoSubscriptions;
        }
        #endregion
    }
}
