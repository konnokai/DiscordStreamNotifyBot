using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Interaction;
using DiscordStreamNotifyBot.Shared.Messages;
using HtmlAgilityPack;
using Polly;
using Google.Apis.YouTube.v3;
using TableVideo = DiscordStreamNotifyBot.DataBase.Table.Video;
using YTApiVideo = Google.Apis.YouTube.v3.Data.Video;

using Bot = DiscordStreamNotifyBot.Shared.BotState;

namespace DiscordStreamNotifyBot.SharedService.Youtube
{
    /// <summary>
    /// YouTube 指令支援 + 通知發送（Notifier 專用）：指令所需的 YouTube API 一律委派 Shared
    /// <see cref="Shared.YoutubeApiService"/>；消費匯流排 <see cref="YoutubeNotification"/> / <see cref="BannerChangeNotification"/>
    /// 後重建 embed，只發送給本 shard 持有的伺服器（含建立活動、更換伺服器橫幅）。
    /// 偵測（排程爬取 / Redis 訂閱 / PubSub 維護 / reminder 排程）由 Scraper 負責。
    /// </summary>
    public partial class YoutubeStreamService : IInteractionService
    {
        public enum NoticeType
        {
            [ChoiceDisplay("新待機室")]
            NewStream,
            [ChoiceDisplay("新上傳影片")]
            NewVideo,
            [ChoiceDisplay("開始直播\\首播")]
            Start,
            [ChoiceDisplay("結束直播\\首播")]
            End,
            [ChoiceDisplay("變更直播時間")]
            ChangeTime,
            [ChoiceDisplay("已刪除或私人化直播")]
            Delete
        }

        public enum NowStreamingHost
        {
            [ChoiceDisplay("Holo")]
            Holo,
            //[ChoiceDisplay("彩虹社")]
            //Niji
        }

        public bool IsRecord { get; set; } = true;

        /// <summary>YouTube API 用戶端，委派至 Shared 的 <see cref="Shared.YoutubeApiService"/>（單一來源）。</summary>
        public YouTubeService YouTubeService => _apiService.YouTubeService;

        private static readonly HttpClient SharedHttpClient = new HttpClient();

        private readonly DiscordSocketClient _client;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MessageComponent _messageComponent;
        private readonly MainDbService _dbService;
        private readonly BotConfig _botConfig;
        private readonly Shared.YoutubeApiService _apiService;

        public YoutubeStreamService(DiscordSocketClient client, IHttpClientFactory httpClientFactory, BotConfig botConfig, EmojiService emojiService, MainDbService dbService, Shared.YoutubeApiService apiService)
        {
            _client = client;
            _httpClientFactory = httpClientFactory;
            _dbService = dbService;
            _botConfig = botConfig;
            _apiService = apiService;

            _messageComponent = new ComponentBuilder()
                        .WithButton("好手氣，隨機帶你到一個影片或直播", style: ButtonStyle.Link, emote: emojiService.YouTubeEmote, url: "https://api.konnokai.me/randomvideo")
                        .WithButton("贊助小幫手 (綠界) #ad", style: ButtonStyle.Link, emote: emojiService.ECPayEmote, url: Utility.ECPayUrl, row: 1)
                        .WithButton("贊助小幫手 (Paypal) #ad", style: ButtonStyle.Link, emote: emojiService.PayPalEmote, url: Utility.PaypalUrl, row: 1).Build();
        }

        #region 指令支援（委派 Shared YoutubeApiService）
        public Task<string> GetChannelIdAsync(string channelUrl) => _apiService.GetChannelIdAsync(channelUrl);

        public string GetVideoId(string videoUrl) => _apiService.GetVideoId(videoUrl);

        public Task<string> GetChannelTitle(string channelId) => _apiService.GetChannelTitle(channelId);

        public Task<List<string>> GetChannelTitle(IEnumerable<string> channelId, bool formatUrl) => _apiService.GetChannelTitle(channelId, formatUrl);

        public Task<YTApiVideo> GetVideoAsync(string videoId) => _apiService.GetVideoAsync(videoId);

        public Task<bool> PostSubscribeRequestAsync(string channelId, bool subscribe = true) => _apiService.PostSubscribeRequestAsync(channelId, subscribe);
        #endregion

        public async Task<Embed> GetNowStreamingChannel(NowStreamingHost host)
        {
            try
            {
                List<string> idList = new List<string>();
                switch (host)
                {
                    case NowStreamingHost.Holo:
                        {
                            HtmlWeb htmlWeb = new HtmlWeb();
                            HtmlDocument htmlDocument = htmlWeb.Load("https://schedule.hololive.tv/lives/all");
                            idList.AddRange(htmlDocument.DocumentNode.Descendants()
                                .Where((x) => x.Name == "a" &&
                                    x.Attributes["href"].Value.StartsWith("https://www.youtube.com/watch") &&
                                    x.Attributes["style"].Value.Contains("border: 3px"))
                                .Select((x) => x.Attributes["href"].Value.Split("?v=")[1]));
                        }
                        break;
                }

                var video = YouTubeService.Videos.List("snippet");
                video.Id = string.Join(",", idList);
                var videoResult = await video.ExecuteAsync().ConfigureAwait(false);

                EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor()
                    .WithTitle("正在直播的清單")
                    .WithThumbnailUrl("https://schedule.hololive.tv/dist/images/logo.png")
                    .WithCurrentTimestamp()
                    .WithDescription(string.Join("\n", videoResult.Items.Select((x) => $"{x.Snippet.ChannelTitle} - {Format.Url(x.Snippet.Title, $"https://www.youtube.com/watch?v={x.Id}")}")));

                return embedBuilder.Build();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"GetNowStreamingChannel: {host}");
                return null;
            }
        }

        #region 通知匯流排消費端（重建 embed → 發送）
        /// <summary>通知匯流排消費端入口：將 scraper 發來的 DTO 還原為 embed 後送出。</summary>
        public async Task DispatchFromBusAsync(YoutubeNotification dto)
        {
            var streamVideo = new TableVideo
            {
                VideoId = dto.VideoId,
                ChannelId = dto.ChannelId,
                ChannelTitle = dto.ChannelTitle,
                VideoTitle = dto.VideoTitle,
                ScheduledStartTime = dto.ScheduledStartTime,
                ChannelType = dto.ChannelType,
            };

            var embed = BuildEmbedForBus(dto, streamVideo);
            await SendStreamMessageAsync(streamVideo, embed, MapNoticeType(dto.NoticeType)).ConfigureAwait(false);
        }

        /// <summary>通知匯流排消費端入口：伺服器橫幅變更事件。</summary>
        public Task DispatchBannerFromBusAsync(BannerChangeNotification dto)
            => ChangeGuildBannerAsync(dto.ChannelId, dto.VideoId);

        private static Embed BuildEmbedForBus(YoutubeNotification dto, TableVideo video)
        {
            switch (dto.NoticeType)
            {
                case YoutubeNoticeType.NewStream:
                    return EmbedBuilderFactory.CreateNewStream(video, dto.ScheduledStartTime).Build();
                case YoutubeNoticeType.NewVideo:
                    return EmbedBuilderFactory.CreateNewVideo(video).Build();
                case YoutubeNoticeType.Start:
                    return EmbedBuilderFactory.CreateStreamStarted(video).Build();
                case YoutubeNoticeType.End:
                    return EmbedBuilderFactory.CreateStreamEnded(
                        video,
                        dto.ActualStartTime ?? dto.ScheduledStartTime,
                        dto.ActualEndTime ?? DateTime.Now).Build();
                case YoutubeNoticeType.ChangeTime:
                    return EmbedBuilderFactory.CreateStreamTimeChanged(video, dto.ScheduledStartTime).Build();
                case YoutubeNoticeType.Delete:
                    return EmbedBuilderFactory.CreateStreamDeleted(video).Build();
                default:
                    return EmbedBuilderFactory.CreateStreamStarted(video).Build();
            }
        }

        private static NoticeType MapNoticeType(YoutubeNoticeType busNoticeType)
            => busNoticeType switch
            {
                YoutubeNoticeType.NewStream => NoticeType.NewStream,
                YoutubeNoticeType.NewVideo => NoticeType.NewVideo,
                YoutubeNoticeType.Start => NoticeType.Start,
                YoutubeNoticeType.End => NoticeType.End,
                YoutubeNoticeType.ChangeTime => NoticeType.ChangeTime,
                YoutubeNoticeType.Delete => NoticeType.Delete,
                _ => NoticeType.Start,
            };

        private async Task SendStreamMessageAsync(TableVideo streamVideo, Embed embed, NoticeType noticeType)
        {
            if (!Bot.IsConnect)
                return;

            string type;
            switch (streamVideo.ChannelType)
            {
                case TableVideo.YTChannelType.Holo:
                    type = "holo";
                    break;
                case TableVideo.YTChannelType.Nijisanji:
                    type = "2434";
                    break;
                default:
                    type = "other";
                    break;
            }

            List<NoticeYoutubeStreamChannel> noticeYoutubeStreamChannels = new List<NoticeYoutubeStreamChannel>();
            using (var db = _dbService.GetDbContext())
            {
                try
                {
                    // 有設定該頻道的通知就不用過濾，他們肯定是要這頻道的通知
                    noticeYoutubeStreamChannels.AddRange(db.NoticeYoutubeStreamChannel.AsNoTracking().Where((x) => x.YouTubeChannelId == streamVideo.ChannelId));
                }
                catch (Exception ex)
                {
                    // 原則上不會有錯，我也不知道加這幹嘛
                    Log.Error(ex.Demystify(), $"SendStreamMessageAsyncChannel: {streamVideo.VideoId}");
                }

                //類型檢查，其他類型的頻道要特別檢查，確保必須是認可的頻道才可被添加到其他類型通知
                try
                {
                    if (type != "other" || //如果不是其他類的頻道，直接添加到對應的類型通知即可
                        !db.YoutubeChannelSpider.AsNoTracking().Any((x) => x.ChannelId == streamVideo.ChannelId) || //若該頻道非在爬蟲清單內，那也沒有認不認可的問題
                        db.YoutubeChannelSpider.AsNoTracking().First((x) => x.ChannelId == streamVideo.ChannelId).IsTrustedChannel) //最後該爬蟲必須是已認可的頻道，才可添加至其他類型的通知
                    {
                        noticeYoutubeStreamChannels.AddRange(db.NoticeYoutubeStreamChannel.AsNoTracking().Where((x) => x.YouTubeChannelId == type));
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), $"SendStreamMessageAsyncOtherChannel: {streamVideo.VideoId}");
                }

                Log.New($"發送 YouTube 通知 ({noticeYoutubeStreamChannels.Count} / {noticeType}): {streamVideo.ChannelTitle} - {streamVideo.VideoTitle}");

#if DEBUG || DEBUG_DONTREGISTERCOMMAND
                return;
#endif

                Image? coverImage = null;
                if (noticeType == NoticeType.NewStream && noticeYoutubeStreamChannels.Any((x) => x.IsCreateEventForNewStream))
                {
                    Log.Info($"YouTube 通知 ({streamVideo.VideoId}) | 嘗試下載封面: {embed.Image.Value.Url}");
                    try
                    {
                        var stream = await Policy.Handle<TimeoutException>()
                            .Or<Discord.Net.HttpException>((httpEx) => ((int)httpEx.HttpCode).ToString().StartsWith("50"))
                            .WaitAndRetryAsync(3, (retryAttempt) =>
                            {
                                var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                                Log.Warn($"YouTube 通知 ({streamVideo.VideoId}) | 封面下載失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                                return timeSpan;
                            })
                            .ExecuteAsync(async () =>
                            {
                                // Use shared HttpClient
                                return await SharedHttpClient.GetStreamAsync(embed.Image.Value.Url);
                            });

                        coverImage = new Image(stream);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Demystify(), $"YouTube 通知 ({streamVideo.VideoId}) | 封面下載失敗，可能是找不到圖檔");
                    }
                }

                foreach (var item in noticeYoutubeStreamChannels)
                {
                    try
                    {
                        var guild = _client.GetGuild(item.GuildId);
                        if (guild == null)
                        {
                            // 多 Shard 環境：非本 Shard 持有的伺服器，或尚未 Ready，皆靜默略過，避免互刪設定
                            if (!Bot.ShouldDeleteMissingGuild(item.GuildId))
                                continue;

                            Log.Warn($"YouTube 通知 ({streamVideo.VideoId}) | 找不到伺服器 {item.GuildId}");
                            db.NoticeYoutubeStreamChannel.RemoveRange(db.NoticeYoutubeStreamChannel.Where((x) => x.GuildId == item.GuildId));
                            db.SaveChanges();
                            continue;
                        }

                        // 只有新影片會發到影片通知頻道，首播類的影片歸類在直播類型
                        // 原則上 DiscordNoticeVideoChannelId 預設會跟 DiscordNoticeStreamChannelId 一樣，不該為空
                        var channel = guild.GetTextChannel(noticeType == NoticeType.NewVideo ? item.DiscordNoticeVideoChannelId : item.DiscordNoticeStreamChannelId);
                        if (channel == null) continue;

                        // 如果是新直播的話就建立活動，或更改活動開始時間
                        try
                        {
                            if (item.IsCreateEventForNewStream)
                            {
                                if (!guild.GetUser(_client.CurrentUser.Id).GuildPermissions.ManageEvents)
                                {
                                    Log.Warn($"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} 無權限可建立活動，關閉此功能");
                                    item.IsCreateEventForNewStream = false;
                                    db.NoticeYoutubeStreamChannel.Update(item);
                                    db.SaveChanges();

                                    try
                                    {
                                        await Policy.Handle<TimeoutException>()
                                            .Or<Discord.Net.HttpException>((httpEx) => ((int)httpEx.HttpCode).ToString().StartsWith("50"))
                                            .WaitAndRetryAsync(3, (retryAttempt) =>
                                            {
                                                var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                                                Log.Warn($"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} / {channel.Id} 無權限提示發送失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                                                return timeSpan;
                                            })
                                            .ExecuteAsync(async () =>
                                            {
                                                await channel.SendMessageAsync(embed: new EmbedBuilder().WithErrorColor().WithDescription("我在伺服器沒有 `管理活動` 的權限\n" +
                                                    "請給予權限後再次執行 `/youtube toggle-create-event` 來開啟自動建立活動的功能").Build());
                                            });
                                    }
                                    catch (Exception) { }
                                }
                                else
                                {
                                    if (noticeType == NoticeType.NewStream)
                                    {
                                        Log.Info($"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} 嘗試建立活動");
                                        DateTime startTime = streamVideo.ScheduledStartTime;

                                        // 若預定開台時間在現在之後，就從現在時間往後推一分鐘
                                        // The start time for an event cannot be in the past (Parameter 'startTime')
                                        if (startTime <= DateTime.Now)
                                        {
                                            startTime = DateTime.Now.AddMinutes(1);
                                        }

                                        startTime = startTime.ToUniversalTime();

                                        await Policy.Handle<TimeoutException>()
                                            .Or<Discord.Net.HttpException>((httpEx) => ((int)httpEx.HttpCode).ToString().StartsWith("50"))
                                            .WaitAndRetryAsync(3, (retryAttempt) =>
                                            {
                                                var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                                                Log.Warn($"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} 建立活動失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                                                return timeSpan;
                                            })
                                            .ExecuteAsync(async () =>
                                            {
                                                await guild.CreateEventAsync(streamVideo.VideoTitle,
                                                    startTime,
                                                    GuildScheduledEventType.External,
                                                    description: Format.Url(streamVideo.ChannelTitle, $"https://youtube.com/channel/{streamVideo.ChannelId}"),
                                                    endTime: startTime.AddHours(1),
                                                    location: $"https://youtube.com/watch?v={streamVideo.VideoId}",
                                                    coverImage: coverImage);
                                            });
                                    }
                                    else if (noticeType == NoticeType.ChangeTime)
                                    {
                                        Log.Info($"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} 嘗試更改活動開始時間");
                                        await Policy.Handle<TimeoutException>()
                                            .Or<Discord.Net.HttpException>((httpEx) => ((int)httpEx.HttpCode).ToString().StartsWith("50"))
                                            .WaitAndRetryAsync(3, (retryAttempt) =>
                                            {
                                                var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                                                Log.Warn($"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} 更改活動時間失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                                                return timeSpan;
                                            })
                                            .ExecuteAsync(async () =>
                                            {
                                                var @event = (await guild.GetEventsAsync()).FirstOrDefault((x) => x.Creator.Id == _client.CurrentUser.Id && x.Location.EndsWith(streamVideo.VideoId));

                                                if (@event == null)
                                                {
                                                    Log.Warn($"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} 更改活動時間失敗，找不到對應的活動");
                                                }
                                                else
                                                {
                                                    await @event.ModifyAsync((act) =>
                                                    {
                                                        act.Name = streamVideo.VideoTitle;
                                                        act.StartTime = (DateTimeOffset)streamVideo.ScheduledStartTime.ToUniversalTime();
                                                        act.EndTime = (DateTimeOffset)streamVideo.ScheduledStartTime.ToUniversalTime().AddHours(1);
                                                    });
                                                }
                                            });
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.Demystify(), $"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} 建立活動失敗");
                        }

                        string sendMessage = "";
                        switch (noticeType)
                        {
                            case NoticeType.NewStream:
                                sendMessage = item.NewStreamMessage;
                                break;
                            case NoticeType.NewVideo:
                                sendMessage = item.NewVideoMessage;
                                break;
                            case NoticeType.Start:
                                sendMessage = item.StratMessage;
                                break;
                            case NoticeType.End:
                                sendMessage = item.EndMessage;
                                break;
                            case NoticeType.ChangeTime:
                                sendMessage = item.ChangeTimeMessage;
                                break;
                            case NoticeType.Delete:
                                sendMessage = item.DeleteMessage;
                                break;
                        }

                        if (sendMessage == "-") continue;

                        await Policy.Handle<TimeoutException>()
                            .Or<Discord.Net.HttpException>((httpEx) => ((int)httpEx.HttpCode).ToString().StartsWith("50"))
                            .WaitAndRetryAsync(3, (retryAttempt) =>
                            {
                                var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                                Log.Warn($"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} / {channel.Id} 發送失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                                return timeSpan;
                            })
                            .ExecuteAsync(async () =>
                            {
                                var message = await channel.SendMessageAsync(text: sendMessage, embed: embed, components: noticeType == NoticeType.Start ? _messageComponent : null, options: new RequestOptions() { RetryMode = RetryMode.AlwaysRetry });

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
                            Log.Warn($"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} / {item.DiscordNoticeVideoChannelId} 遺失權限");
                            db.NoticeYoutubeStreamChannel.RemoveRange(db.NoticeYoutubeStreamChannel.Where((x) => x.DiscordNoticeVideoChannelId == item.DiscordNoticeVideoChannelId));
                            db.SaveChanges();
                        }
                        else if (((int)httpEx.HttpCode).ToString().StartsWith("50"))
                        {
                            Log.Warn($"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} / {item.DiscordNoticeVideoChannelId} Discord 50X 錯誤: {httpEx.HttpCode}");
                        }
                        else
                        {
                            Log.Error(httpEx, $"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} / {item.DiscordNoticeVideoChannelId} Discord 未知錯誤");
                        }
                    }
                    catch (TimeoutException)
                    {
                        Log.Warn($"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} / {item.DiscordNoticeVideoChannelId} Timeout");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Demystify(), $"YouTube 通知 ({streamVideo.VideoId}) | {item.GuildId} / {item.DiscordNoticeVideoChannelId} 未知錯誤");
                    }
                }
            }
        }
        #endregion

        #region 伺服器橫幅變更（消費端套用，需 GetGuild）
        private async Task ChangeGuildBannerAsync(string channelId, string videoId)
        {
#if DEBUG || DEBUG_DONTREGISTERCOMMAND
            return;
#endif
            List<DataBase.Table.BannerChange> list;

            using (var db = _dbService.GetDbContext())
            {
                list = db.BannerChange.AsNoTracking()
                    .Where(x => x.ChannelId == channelId)
                    .ToList();
            }

            if (list.Count == 0) return;

            foreach (var item in list)
            {
                try
                {
                    var guild = _client.GetGuild(item.GuildId);
                    if (guild == null)
                    {
                        // 多 Shard 環境：非本 Shard 持有的伺服器，或尚未 Ready，皆靜默略過，避免互刪設定
                        if (!Bot.ShouldDeleteMissingGuild(item.GuildId))
                            continue;

                        Log.Warn($"Guild not found: {item.GuildId} / {channelId} / {videoId}");
                        using (var db = _dbService.GetDbContext())
                        {
                            db.BannerChange.Remove(item);
                            await db.SaveChangesAsync();
                        }
                        continue;
                    }

                    if (guild.PremiumTier < PremiumTier.Tier2) continue;

                    if (videoId != item.LastChangeStreamId)
                    {
                        MemoryStream memStream;
                        try
                        {
                            memStream = new MemoryStream(await _httpClientFactory.CreateClient("").GetByteArrayAsync($"https://i.ytimg.com/vi/{videoId}/maxresdefault.jpg"));
                            if (memStream.Length < 2048) memStream = null;
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"DownloadGuildBanner - {item.GuildId}\n" +
                                $"{channelId} / {videoId}\n" +
                                $"{ex.Message}\n" +
                                $"{ex.StackTrace}");
                            continue;
                        }

                        try
                        {
                            if (memStream != null)
                            {
                                Image image = new Image(memStream);
                                await guild.ModifyAsync((func) => func.Banner = image);
                            }

                            item.LastChangeStreamId = videoId;

                            using (var db = _dbService.GetDbContext())
                            {
                                db.BannerChange.Update(item);
                                await db.SaveChangesAsync();
                            }

                            Log.Info("ChangeGuildBanner" + (memStream == null ? "(Without Change)" : "") + $": {item.GuildId} / {videoId}");
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.Demystify(), $"ChangeGuildBanner - {item.GuildId}: {channelId} / {videoId}");
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), $"ChangeGuildBanner - {item.GuildId}");
                    continue;
                }
            }
        }
        #endregion
    }
}
