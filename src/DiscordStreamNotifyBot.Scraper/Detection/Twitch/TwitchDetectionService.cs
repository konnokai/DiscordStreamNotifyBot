using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Scraper.Detection.Twitch.Debounce;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.Twitch;
using System.Collections.Concurrent;

using Bot = DiscordStreamNotifyBot.Shared.BotState;

namespace DiscordStreamNotifyBot.Scraper.Detection.Twitch
{
    /// <summary>
    /// Twitch 偵測服務（Scraper 專用）：EventSub Redis 訂閱（stream_offline / channel_update）、
    /// 輪詢 Timer、WebHook 維護、錄影，偵測到事件改 publish <see cref="TwitchNotification"/> 至匯流排。
    /// Twitch API 呼叫一律經 Shared <see cref="TwitchApiService"/>；不碰 Discord gateway。
    /// </summary>
    public class TwitchDetectionService
    {
        private readonly TwitchApiService _apiService;
        private readonly MainDbService _dbService;
        private readonly BotConfig _botConfig;
        private readonly HashSet<string> _hashSet = new();
        private readonly ConcurrentDictionary<string, DebounceChannelUpdateMessage> _debounceChannelUpdateMessage = new();
        private readonly ConcurrentDictionary<string, Timer> _streamOfflineReminders = new();

        public TwitchDetectionService(TwitchApiService apiService, BotConfig botConfig, MainDbService dbService)
        {
            _apiService = apiService;
            _botConfig = botConfig;
            _dbService = dbService;

            if (!_apiService.IsEnable)
            {
                Log.Warn("Twitch API 未啟用，Twitch 偵測不啟動");
                return;
            }

#nullable enable

            Bot.RedisSub.Subscribe(new RedisChannel("twitch:stream_offline", RedisChannel.PatternMode.Literal), (channel, streamData) =>
            {
                var data = JsonConvert.DeserializeObject<TwitchLib.EventSub.Core.SubscriptionTypes.Stream.StreamOffline>(streamData!)!;
                Log.Info($"Twitch 直播離線: {data.BroadcasterUserLogin} ({data.BroadcasterUserId})，等待三分鐘後發送關台通知");

                // 先移除舊的 Timer
                if (_streamOfflineReminders.TryRemove(data.BroadcasterUserId, out var oldTimer))
                {
                    oldTimer.Dispose();
                }

                // 新增三分鐘倒數
                var timer = new Timer(async _ =>
                {
                    // 發送離線通知（原本的通知邏輯

                    // 減掉三分鐘的去抖動時間
                    DateTime endAt = DateTime.Now.AddMinutes(-3);

                    try
                    {
                        await _apiService.DeleteEventSubSubscriptionAsync(data.BroadcasterUserId);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Demystify(), $"Event Delete Error: {data.BroadcasterUserId}");
                    }

                    TwitchStream? twitchStream = null;
                    try
                    {
                        var redisJson = await Bot.RedisDb.StringGetAsync(new RedisKey($"twitch:stream_data:{data.BroadcasterUserId}"));
                        if (redisJson.HasValue)
                        {
                            twitchStream = JsonConvert.DeserializeObject<TwitchStream>(redisJson!);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Demystify(), $"Twitch Get Redis Data Error: {data.BroadcasterUserId}");
                    }

                    string clipsValue = string.Empty;

                    // Todo: 可能需要驗證 VOD 的開始直播時間，很多頻道不會開 VOD 紀錄，有時候會導致小幫手的通知資訊異常
                    var video = await _apiService.GetLatestVODAsync(data.BroadcasterUserId);
                    if (video == null)
                    {
                        Log.Warn($"找不到對應的 Vod 紀錄資料: {data.BroadcasterUserLogin} ({data.BroadcasterUserId})");
                    }
                    else // 僅增加與該場直播有關的 Clip
                    {
                        DateTime createAt = DateTime.Parse(video.CreatedAt);

                        var clips = await _apiService.GetClipsAsync(data.BroadcasterUserId, createAt, createAt + _apiService.ParseToTimeSpan(video.Duration));
                        if (clips != null && clips.Any((x) => x.VideoId == video.Id))
                        {
                            int i = 0;
                            clipsValue = string.Join('\n', clips.Where((x) => x.VideoId == video.Id)
                                .Select((x) => $"{i++}. [{x.Title}]({x.Url}) By `{x.CreatorName}` (`{x.ViewCount}` 次觀看)"));
                        }
                    }

                    // 如果 Redis 沒有資料，則從 Video 資料中取得
                    if (twitchStream == null && video != null)
                    {
                        twitchStream = new()
                        {
                            // 這個會有問題，因為圖奇的 VOD 標題會直接以開播當下的標題為準，中途變更的話也不會改
                            // 所以只有在 Redis 抓不到資料的時候再設定標題上去，否則以 Redis 最新的資料為準
                            StreamTitle = video.Title,
                            StreamStartAt = DateTime.Parse(video.CreatedAt)
                        };
                    }

                    // 通知一律經匯流排：偵測端組好結構化資料後 publish，由消費端重建 embed 發送
                    // （twitchStream 最後還是會有為 null 的可能：Redis 沒資料然後也沒開 VOD 保存）
                    try
                    {
                        await NotificationBusPublisher.PublishJsonAsync(_botConfig.RabbitMQ,
                            NotifyRoutingKeys.Twitch,
                            new TwitchNotification
                            {
                                NoticeType = TwitchNoticeType.EndStream,
                                UserId = data.BroadcasterUserId,
                                UserLogin = data.BroadcasterUserLogin,
                                UserName = data.BroadcasterUserName,
                                StreamTitle = twitchStream?.StreamTitle,
                                StreamStartAt = twitchStream?.StreamStartAt,
                                StreamEndAt = endAt,
                                ClipsValue = clipsValue,
                            });
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Demystify(), $"PublishTwitchEndStream: {data.BroadcasterUserId}");
                    }

                    // 移除 Reminder
                    if (_streamOfflineReminders.TryRemove(data.BroadcasterUserId, out var newTimer))
                    {
                        newTimer.Dispose();
                    }
                }, null, TimeSpan.FromMinutes(3), Timeout.InfiniteTimeSpan);

                _streamOfflineReminders.TryAdd(data.BroadcasterUserId, timer);
            });

            Bot.RedisSub.Subscribe(new RedisChannel("twitch:channel_update", RedisChannel.PatternMode.Literal), async (channel, updateData) =>
            {
                var data = JsonConvert.DeserializeObject<TwitchLib.EventSub.Core.SubscriptionTypes.Channel.ChannelUpdate>(updateData!)!;
                Log.Info($"Twitch 頻道更新: {data.BroadcasterUserName} - {data.Title} ({data.CategoryName})");

                TwitchStream? twitchStream = null;
                try
                {
                    var redisJson = await Bot.RedisDb.StringGetAsync(new RedisKey($"twitch:stream_data:{data.BroadcasterUserId}"));
                    if (redisJson.HasValue)
                        twitchStream = JsonConvert.DeserializeObject<TwitchStream>(redisJson!);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), $"Twitch Get Redis Data Error: {data.BroadcasterUserId}");
                }

                if (twitchStream == null)
                {
                    Log.Warn($"Redis 找不到 Twitch 頻道資料，忽略: {data.BroadcasterUserName}");
                    return;
                }

                bool isChangeTitle = twitchStream.StreamTitle != data.Title;
                bool isChangeCategory = twitchStream.GameName != data.CategoryName;
                if (!isChangeTitle && !isChangeCategory)
                {
                    Log.Warn($"Twitch 頻道更新資料相同，忽略: {data.BroadcasterUserName}");
                    return;
                }

                string message = $"`{DateTime.UtcNow.Subtract(twitchStream.StreamStartAt):hh':'mm':'ss}`";

                if (isChangeTitle)
                {
                    message += $"\n標題變更 `{twitchStream.StreamTitle}` => `{data.Title}`";
                }

                if (isChangeCategory)
                {
                    message += $"\n分類變更 `" +
                    (string.IsNullOrEmpty(twitchStream.GameName) ? "無" : twitchStream.GameName) +
                    "` => `" +
                    (string.IsNullOrEmpty(data.CategoryName) ? "無" : data.CategoryName) +
                    "`";
                }

                _debounceChannelUpdateMessage.AddOrUpdate(data.BroadcasterUserId,
                    (userId) =>
                    {
                        var debounce = new DebounceChannelUpdateMessage(this, data.BroadcasterUserName, data.BroadcasterUserLogin, data.BroadcasterUserId);
                        debounce.AddMessage(message);
                        return debounce;
                    },
                    (userId, debounce) =>
                    {
                        debounce.AddMessage(message);
                        return debounce;
                    });

                try
                {
                    twitchStream = new TwitchStream()
                    {
                        StreamId = twitchStream?.StreamId,
                        StreamTitle = data.Title,
                        GameName = data.CategoryName,
                        ThumbnailUrl = twitchStream?.ThumbnailUrl,
                        UserId = data.BroadcasterUserId,
                        UserLogin = data.BroadcasterUserLogin,
                        UserName = data.BroadcasterUserName,
                        StreamStartAt = twitchStream?.StreamStartAt ?? DateTime.UtcNow
                    };

                    await Bot.RedisDb.StringSetAsync(new($"twitch:stream_data:{data.BroadcasterUserId}"), JsonConvert.SerializeObject(twitchStream));
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), $"Twitch Channel Update Set Redis Data Error: {data.BroadcasterUserId}");
                }
            });

#nullable disable

            // 偵測排程（計畫 §12.1）：PeriodicTimer 背景輪詢，await 友善、無重入、吃 CancellationToken
            var token = GracefulShutdown.Token;
            PeriodicRunner.RunAsync("Twitch-poll", TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), TimerHandel, token);

            // 每日 00:00 定時檢查驗證失敗的 WebHook 並移除
            var now = DateTime.Now;
            var dueTime = now.Date.AddDays(1) - now;
            PeriodicRunner.RunAsync("Twitch-removeFailedWebhook", dueTime, TimeSpan.FromDays(1), async () =>
            {
                Log.Info("開始檢查 Twitch WebHook 驗證失敗的訂閱...");

                try
                {
                    var getEventSubSubscriptionsResponse = await _apiService.TwitchApi.Value.Helix.EventSub.GetEventSubSubscriptionsAsync(status: "webhook_callback_verification_failed");
                    if (getEventSubSubscriptionsResponse == null)
                        return;

                    if (getEventSubSubscriptionsResponse.Subscriptions == null || getEventSubSubscriptionsResponse.Subscriptions.Length == 0)
                        return;

                    foreach (var sub in getEventSubSubscriptionsResponse.Subscriptions)
                    {
                        Log.Info($"刪除驗證失敗的 Twitch WebHook 訂閱: {sub.Id} ({sub.Type})");
                        await _apiService.TwitchApi.Value.Helix.EventSub.DeleteEventSubSubscriptionAsync(sub.Id);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), "檢查 Twitch WebHook 驗證失敗的訂閱時發生錯誤");
                }

                Log.Info("完成檢查 Twitch WebHook 驗證失敗的訂閱");
            }, token);
        }

        private async Task TimerHandel()
        {
            // PeriodicTimer 保證單一迴圈不重疊，無需 isRuning 重入旗標（§12.1）
            try
            {
                using var db = _dbService.GetDbContext();
                var twitchSpiderChunk = db.TwitchSpider.AsEnumerable().DistinctBy((x) => x.UserId).Chunk(100);

                foreach (var twitchSpiders in twitchSpiderChunk)
                {
                    var streams = await _apiService.GetNowStreamsAsync(twitchSpiders.Select((x) => x.UserId).ToArray());
                    if (!streams.Any())
                        continue;

                    using var db2 = _dbService.GetDbContext();
                    foreach (var stream in streams)
                    {
                        if (string.IsNullOrEmpty(stream.Id))
                            continue;

                        if (_hashSet.Contains(stream.Id))
                            continue;

                        _hashSet.Add(stream.Id);

                        if (db2.TwitchStreams.AsNoTracking().Any((x) => x.StreamId == stream.Id))
                            continue;

                        var twitchSpider = twitchSpiders.Single((x) => x.UserId == stream.UserId);
                        var userData = await _apiService.GetUserAsync(twitchUserId: twitchSpider.UserId);
                        twitchSpider.OfflineImageUrl = userData.OfflineImageUrl;
                        twitchSpider.ProfileImageUrl = userData.ProfileImageUrl;
                        twitchSpider.UserName = userData.DisplayName;
                        db2.TwitchSpider.Update(twitchSpider);

                        try
                        {
                            var twitchStream = new TwitchStream()
                            {
                                StreamId = stream.Id,
                                StreamTitle = stream.Title,
                                GameName = stream.GameName,
                                ThumbnailUrl = stream.ThumbnailUrl.Replace("{width}", "854").Replace("{height}", "480"),
                                UserId = stream.UserId,
                                UserLogin = stream.UserLogin,
                                UserName = stream.UserName,
                                StreamStartAt = stream.StartedAt
                            };

                            db2.TwitchStreams.Add(twitchStream);

                            // 如果有設定離線提醒，則移除舊的 Timer 並不發送開始直播通知
                            if (_streamOfflineReminders.TryRemove(stream.UserId, out Timer timer))
                            {
                                Log.Warn($"{stream.UserId} 已經有關台通知，忽略");
                                timer.Dispose();
                            }
                            else
                            {
                                // 錄影副作用維持在偵測端執行，結果以 isRecord 傳遞
                                bool isRecord = twitchSpider.IsRecord && await RecordTwitchAsync(twitchStream);

                                if (!twitchSpider.IsWarningUser)
                                {
                                    try
                                    {
                                        await Bot.RedisDb.StringSetAsync(new($"twitch:stream_data:{stream.UserId}"), JsonConvert.SerializeObject(twitchStream));
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error(ex.Demystify(), $"Twitch Set Redis Data Error: {stream.Id}");
                                    }

                                    if (await _apiService.CreateEventSubSubscriptionAsync(stream.UserId))
                                    {
                                        Log.Info($"已註冊 Twitch WebHook: {twitchSpider.UserId} ({twitchSpider.UserName})");
                                    }
                                }

                                // 通知一律經匯流排：publish DTO，由消費端重建 embed 發送
                                try
                                {
                                    await NotificationBusPublisher.PublishJsonAsync(_botConfig.RabbitMQ,
                                        NotifyRoutingKeys.Twitch,
                                        new TwitchNotification
                                        {
                                            NoticeType = TwitchNoticeType.StartStream,
                                            UserId = twitchStream.UserId,
                                            UserLogin = twitchStream.UserLogin,
                                            UserName = twitchStream.UserName,
                                            StreamTitle = twitchStream.StreamTitle,
                                            GameName = twitchStream.GameName,
                                            ThumbnailUrl = twitchStream.ThumbnailUrl,
                                            StreamStartAt = twitchStream.StreamStartAt,
                                            IsRecord = isRecord,
                                        });
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex.Demystify(), $"PublishTwitchStartStream: {twitchStream.UserId}");
                                }
                            }
                        }
                        catch (Exception ex) { Log.Error(ex.Demystify(), $"TwitchService-GetData: {twitchSpider.UserLogin}"); }
                    }

                    try
                    {
                        await db2.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Demystify(), "TwitchService-Timer: SaveDb Error");
                    }
                }
            }
            catch (Exception ex) { Log.Error(ex.Demystify(), "TwitchService-Timer"); }
        }

        /// <summary>
        /// 直播資料更新通知的發布入口（去抖動彙整後由 <see cref="DebounceChannelUpdateMessage"/> 呼叫）。
        /// publish DTO 至匯流排，由消費端（Notifier）重建 embed 後發送。
        /// </summary>
        internal async Task PublishChannelUpdateAsync(string userId, string userName, string userLogin, string description)
        {
            try
            {
                await NotificationBusPublisher.PublishJsonAsync(_botConfig.RabbitMQ,
                    NotifyRoutingKeys.Twitch,
                    new TwitchNotification
                    {
                        NoticeType = TwitchNoticeType.ChangeStreamData,
                        UserId = userId,
                        UserLogin = userLogin,
                        UserName = userName,
                        Description = description,
                    });
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"PublishTwitchChannelUpdate: {userId}");
            }
        }

        private async Task<bool> RecordTwitchAsync(TwitchStream twitchStream)
        {
            Log.Info($"{twitchStream.UserName} ({twitchStream.StreamId}): {twitchStream.StreamTitle}");

            if (Bot.Redis != null)
            {
                if (await Bot.RedisSub.PublishAsync(new RedisChannel("twitch.record", RedisChannel.PatternMode.Literal), twitchStream.UserLogin) != 0)
                {
                    Log.Info($"已發送 Twitch 錄影請求: {twitchStream.UserLogin}");
                    return true;
                }
                else
                {
                    Log.Warn($"Redis Sub 頻道不存在，請開啟錄影工具: {twitchStream.UserLogin}");
                }
            }

            return false;
        }
    }
}
