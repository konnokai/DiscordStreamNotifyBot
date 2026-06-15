using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Interaction;
using Clip = TwitchLib.Api.Helix.Models.Clips.GetClips.Clip;
using User = TwitchLib.Api.Helix.Models.Users.GetUsers.User;
using Video = TwitchLib.Api.Helix.Models.Videos.GetVideos.Video;

#if !DEBUG
using Polly;
#endif

using Bot = DiscordStreamNotifyBot.Shared.BotState;

namespace DiscordStreamNotifyBot.SharedService.Twitch
{
    /// <summary>
    /// Twitch 指令支援 + 通知發送（Notifier 專用）：指令所需的 Twitch API 一律委派 Shared <see cref="TwitchApiService"/>；
    /// 消費匯流排 <see cref="Shared.Messages.TwitchNotification"/> 後重建 embed，只發送給本 shard 持有的伺服器。
    /// 偵測（EventSub 訂閱 / 輪詢 / WebHook 維護）由 Scraper 負責。
    /// </summary>
    public class TwitchService : IInteractionService
    {
        public enum NoticeType
        {
            [ChoiceDisplay("開始直播")]
            StartStream,
            [ChoiceDisplay("結束直播")]
            EndStream,
            [ChoiceDisplay("更改直播資料")]
            ChangeStreamData
        }

        internal bool IsEnable => _apiService.IsEnable;
        internal Lazy<TwitchLib.Api.TwitchAPI> TwitchApi => _apiService.TwitchApi;

        private readonly DiscordSocketClient _client;
        private readonly TwitchApiService _apiService;
        private readonly EmojiService _emojiService;
        private readonly MainDbService _dbService;
        private readonly BotConfig _botConfig;
        private readonly MessageComponent _messageComponent;
        private readonly NoticeCache<DataBase.Table.NoticeTwitchStreamChannel> _noticeCache;

        public TwitchService(DiscordSocketClient client, TwitchApiService apiService, BotConfig botConfig, EmojiService emojiService, MainDbService dbService)
        {
            _client = client;
            _apiService = apiService;
            _emojiService = emojiService;
            _dbService = dbService;
            _botConfig = botConfig;
            _noticeCache = new NoticeCache<DataBase.Table.NoticeTwitchStreamChannel>(dbService, db => db.NoticeTwitchStreamChannels.AsNoTracking().ToList());

            _messageComponent = new ComponentBuilder()
                .WithButton("好手氣，隨機帶你到一個影片或直播", style: ButtonStyle.Link, emote: emojiService.YouTubeEmote, url: "https://api.konnokai.me/randomvideo")
                .WithButton("贊助小幫手 (綠界) #ad", style: ButtonStyle.Link, emote: emojiService.ECPayEmote, url: Utility.ECPayUrl, row: 1)
                .WithButton("贊助小幫手 (Paypal) #ad", style: ButtonStyle.Link, emote: emojiService.PayPalEmote, url: Utility.PaypalUrl, row: 1).Build();
        }

        #region 指令支援（委派 Shared TwitchApiService）
        public string GetUserLoginByUrl(string url) => _apiService.GetUserLoginByUrl(url);

        public TimeSpan ParseToTimeSpan(string input) => _apiService.ParseToTimeSpan(input);

        public Task<User> GetUserAsync(string twitchUserId = "", string twitchUserLogin = "")
            => _apiService.GetUserAsync(twitchUserId, twitchUserLogin);

        public Task<IReadOnlyList<User>> GetUsersAsync(params string[] twitchUserLogins)
            => _apiService.GetUsersAsync(twitchUserLogins);

        public Task<Video> GetLatestVODAsync(string twitchUserId) => _apiService.GetLatestVODAsync(twitchUserId);

        public Task<IReadOnlyList<Clip>> GetClipsAsync(string twitchUserId, DateTime startedAt, DateTime endedAt)
            => _apiService.GetClipsAsync(twitchUserId, startedAt, endedAt);

        public Task<IReadOnlyList<TwitchLib.Api.Helix.Models.EventSub.EventSubSubscription>> GetEventSubSubscriptionsAsync(string userId = null)
            => _apiService.GetEventSubSubscriptionsAsync(userId);

        internal Task<bool> CreateEventSubSubscriptionAsync(string broadcasterUserId)
            => _apiService.CreateEventSubSubscriptionAsync(broadcasterUserId);

        public Task<bool> DeleteEventSubSubscriptionAsync(string userId) => _apiService.DeleteEventSubSubscriptionAsync(userId);
        #endregion

        /// <summary>
        /// 通知匯流排消費端入口：依 DTO 類型以工廠重建 embed 後，走 <see cref="SendStreamMessageAsync"/> 發送
        /// （shard 過濾沿用既有守衛）。Profile/Offline 圖片由本端 DB（TwitchSpider）補齊。
        /// </summary>
        public async Task DispatchFromBusAsync(Shared.Messages.TwitchNotification dto)
        {
            DataBase.Table.TwitchSpider twitchSpider;
            using (var db = _dbService.GetDbContext())
                twitchSpider = db.TwitchSpider.AsNoTracking().FirstOrDefault((x) => x.UserId == dto.UserId);

            Embed embed;
            NoticeType noticeType;
            switch (dto.NoticeType)
            {
                case Shared.Messages.TwitchNoticeType.StartStream:
                    var twitchStream = new DataBase.Table.TwitchStream
                    {
                        UserId = dto.UserId,
                        UserLogin = dto.UserLogin,
                        UserName = dto.UserName,
                        StreamTitle = dto.StreamTitle,
                        GameName = dto.GameName,
                        ThumbnailUrl = dto.ThumbnailUrl,
                        StreamStartAt = dto.StreamStartAt ?? DateTime.Now,
                    };
                    embed = TwitchEmbedBuilderFactory.CreateStreamStarted(twitchStream, twitchSpider?.ProfileImageUrl, dto.IsRecord).Build();
                    noticeType = NoticeType.StartStream;
                    break;

                case Shared.Messages.TwitchNoticeType.EndStream:
                    embed = TwitchEmbedBuilderFactory.CreateStreamEnded(
                        dto.UserName, dto.UserLogin,
                        dto.StreamTitle, dto.StreamStartAt, dto.StreamEndAt ?? DateTime.Now,
                        dto.ClipsValue, twitchSpider?.ProfileImageUrl, twitchSpider?.OfflineImageUrl).Build();
                    noticeType = NoticeType.EndStream;
                    break;

                case Shared.Messages.TwitchNoticeType.ChangeStreamData:
                    embed = TwitchEmbedBuilderFactory.CreateChannelUpdate(dto.UserName, dto.UserLogin, dto.Description, twitchSpider?.ProfileImageUrl).Build();
                    noticeType = NoticeType.ChangeStreamData;
                    break;

                default:
                    return;
            }

            await SendStreamMessageAsync(dto.UserId, embed, noticeType).ConfigureAwait(false);
        }

        internal async Task SendStreamMessageAsync(string twitchUserId, Embed embed, NoticeType noticeType)
        {
            if (!Bot.IsConnect)
                return;

#if DEBUG || DEBUG_DONTREGISTERCOMMAND
            Log.New($"Twitch 通知: {twitchUserId} - {embed.Title} ({noticeType})");
#else
            using (var db = _dbService.GetDbContext())
            {
                // 通知設定改讀記憶體快取（§12.3）
                var noticeGuildList = _noticeCache.Get().Where((x) => x.NoticeTwitchUserId == twitchUserId).ToList();
                Log.New($"發送 Twitch 通知 ({noticeGuildList.Count} / {noticeType}): ({twitchUserId}) - {embed.Title}");

                foreach (var item in noticeGuildList)
                {
                    try
                    {
                        string sendMessage = "";
                        switch (noticeType)
                        {
                            case NoticeType.StartStream:
                                sendMessage = item.StartStreamMessage;
                                break;
                            case NoticeType.EndStream:
                                sendMessage = item.EndStreamMessage;
                                break;
                            case NoticeType.ChangeStreamData:
                                sendMessage = item.ChangeStreamDataMessage;
                                break;
                        }

                        if (sendMessage == "-") continue;

                        var guild = _client.GetGuild(item.GuildId);
                        if (guild == null)
                        {
                            // 多 Shard 環境：非本 Shard 持有的伺服器，或尚未 Ready，皆靜默略過，避免互刪設定
                            if (!Bot.ShouldDeleteMissingGuild(item.GuildId))
                                continue;

                            Log.Warn($"Twitch 通知 ({twitchUserId}) | 找不到伺服器 {item.GuildId}");
                            db.NoticeTwitchStreamChannels.RemoveRange(db.NoticeTwitchStreamChannels.Where((x) => x.GuildId == item.GuildId));
                            db.SaveChanges();
                            _noticeCache.Invalidate();
                            continue;
                        }

                        var channel = guild.GetTextChannel(item.DiscordChannelId);
                        if (channel == null) continue;

                        await Policy.Handle<TimeoutException>()
                            .Or<Discord.Net.HttpException>((httpEx) => ((int)httpEx.HttpCode).ToString().StartsWith("50"))
                            .WaitAndRetryAsync(3, (retryAttempt) =>
                            {
                                var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                                Log.Warn($"Twitch 通知 ({twitchUserId}) | {item.GuildId} / {item.DiscordChannelId} 發送失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                                return timeSpan;
                            })
                            .ExecuteAsync(async () =>
                            {
                                var message = await channel.SendMessageAsync(text: sendMessage, embed: embed, components: noticeType == NoticeType.StartStream ? _messageComponent : null, options: new RequestOptions() { RetryMode = RetryMode.AlwaysRetry });

                                try
                                {
                                    if (channel is INewsChannel && Utility.OfficialGuildList.Contains(guild.Id))
                                        await message.CrosspostAsync();
                                }
                                catch (Discord.Net.HttpException httpEx) when (httpEx.DiscordCode == DiscordErrorCode.MessageAlreadyCrossposted)
                                {
                                    // ignore
                                }
                            });
                    }
                    catch (Discord.Net.HttpException httpEx)
                    {
                        if (httpEx.DiscordCode.HasValue && (httpEx.DiscordCode.Value == DiscordErrorCode.InsufficientPermissions || httpEx.DiscordCode.Value == DiscordErrorCode.MissingPermissions))
                        {
                            Log.Warn($"Twitch 通知 ({twitchUserId}) | 遺失權限 {item.GuildId} / {item.DiscordChannelId}");
                            db.NoticeTwitchStreamChannels.RemoveRange(db.NoticeTwitchStreamChannels.Where((x) => x.DiscordChannelId == item.DiscordChannelId));
                            db.SaveChanges();
                            _noticeCache.Invalidate();
                        }
                        else if (((int)httpEx.HttpCode).ToString().StartsWith("50"))
                        {
                            Log.Warn($"Twitch 通知 ({twitchUserId}) | Discord 50X 錯誤: {httpEx.HttpCode}");
                        }
                        else
                        {
                            Log.Error(httpEx, $"Twitch 通知 ({twitchUserId}) | Discord 未知錯誤 {item.GuildId} / {item.DiscordChannelId}");
                        }
                    }
                    catch (TimeoutException)
                    {
                        Log.Warn($"Twitch 通知 ({twitchUserId}) | Timeout {item.GuildId} / {item.DiscordChannelId}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Demystify(), $"Twitch 通知 ({twitchUserId}) | 未知錯誤 {item.GuildId} / {item.DiscordChannelId}");
                    }
                }
            }
#endif
        }
    }
}
