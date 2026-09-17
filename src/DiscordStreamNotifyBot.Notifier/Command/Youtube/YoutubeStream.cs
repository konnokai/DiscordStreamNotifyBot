using Discord.Commands;
using DiscordStreamNotifyBot.Command.Attribute;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Interaction;
using System.Text.RegularExpressions;

namespace DiscordStreamNotifyBot.Command.Youtube
{
    public partial class YoutubeStream : TopLevelModule, ICommandService
    {
        private readonly DiscordSocketClient _client;
        private readonly SharedService.Youtube.YoutubeStreamService _service;
        private readonly MainDbService _dbService;
        private readonly SharedService.Cluster.ClusterQueryService _clusterQuery;

        public YoutubeStream(DiscordSocketClient client, SharedService.Youtube.YoutubeStreamService service, MainDbService dbService, SharedService.Cluster.ClusterQueryService clusterQuery)
        {
            _client = client;
            _service = service;
            _dbService = dbService;
            _clusterQuery = clusterQuery;
        }

        [RequireContext(ContextType.DM)]
        [Command("AddVideoData")]
        [Summary("新增影片資料並發送通知")]
        [Alias("aod")]
        [RequireOwner]
        public async Task AddVideoDataAsync(string videoId)
        {
            await Context.Channel.TriggerTypingAsync();

            if (videoId.Length != 11)
            {
                var match = Regex.Match(videoId, @"(?<=youtu\.be\/|youtube\.com\/(?:watch\?.*v=|live\/))(?'VideoId'[\w-]{11})");

                if (match.Success)
                {
                    videoId = match.Groups["VideoId"].Value;
                }
                else
                {
                    await Context.Channel.SendConfirmAsync("網址格式驗證失敗，請確認網址是否正確").ConfigureAwait(false);
                    return;
                }

                if (videoId.Length != 11)
                {
                    await Context.Channel.SendConfirmAsync("Video ID 格式錯誤，必須為 11 個字元").ConfigureAwait(false);
                    return;
                }
            }

            Google.Apis.YouTube.v3.Data.Video video;
            try
            {
                video = await _service.GetVideoAsync(videoId);
            }
            catch (Exception ex)
            {
                await Context.Channel.SendErrorAsync(ex.ToString());
                return;
            }

            if (video == null)
            {
                await Context.Channel.SendConfirmAsync($"{videoId} 不存在").ConfigureAwait(false);
                return;
            }

            if (!SharedExtensions.HasStreamVideoByVideoId(videoId))
            {
                // 新增影片資料由 Scraper 偵測器負責；發送 addVideo 控制訊息
                await Bot.RedisSub.PublishAsync(new RedisChannel("youtube.control.addVideo", RedisChannel.PatternMode.Literal), videoId);
                await Context.Channel.SendConfirmAsync($"已要求偵測器新增資料：{video.Snippet.ChannelTitle} - {video.Snippet.Title}");
            }
            else
            {
                await Context.Channel.SendErrorAsync($"資料已存在於資料庫內，忽略");
            }
        }

        [RequireContext(ContextType.DM)]
        [Command("ForceReSubscribeSpider")]
        [Summary("強制重新註冊爬蟲（all 或 channelUrl）")]
        [Alias("frss")]
        [CommandExample("all", "998rrr", "UCs5FNYPHeZz5f7N1BDExxfg")]
        [RequireOwner]
        public async Task ForceReSubscribeSpider(string channelUrl)
        {
            await Context.Channel.TriggerTypingAsync();

            string channelId = "";
            try
            {
                channelId = await _service.GetChannelIdAsync(channelUrl).ConfigureAwait(false);
            }
            catch (FormatException fex)
            {
                await Context.Channel.SendErrorAsync(fex.Message);
                return;
            }
            catch (ArgumentNullException)
            {
                await Context.Channel.SendErrorAsync("網址不可空白");
                return;
            }

            using var db = _dbService.GetDbContext();

            if (channelId == "all")
            {
                if (await PromptUserConfirmAsync(new EmbedBuilder().WithOkColor().WithDescription("要重新註冊所有爬蟲嗎？")))
                {
                    foreach (var item in db.YoutubeChannelSpider)
                    {
                        item.LastSubscribeTime = DateTime.MinValue;
                    }
                }
            }
            else
            {
                var youtubeChannelSpider = await db.YoutubeChannelSpider.FirstOrDefaultAsync((x) => x.ChannelId == channelId);
                if (youtubeChannelSpider == null)
                {
                    await Context.Channel.SendErrorAsync($"資料庫中找不到 {channelId} 的爬蟲");
                    return;
                }
                else
                {
                    youtubeChannelSpider.LastSubscribeTime = DateTime.MinValue;
                }
            }

            await db.SaveChangesAsync();

            await Context.Channel.SendConfirmAsync("已更新設定，等待爬蟲重新註冊…");
            // PubSub 重新註冊由 Scraper 偵測器負責；發送控制訊息觸發
            await Bot.RedisSub.PublishAsync(new RedisChannel("youtube.control.subscribePubSub", RedisChannel.PatternMode.Literal), "");
        }

        [RequireContext(ContextType.DM)]
        [Command("GetNotionGuild")]
        [Summary("取得已設定通知的伺服器")]
        [Alias("gng")]
        [CommandExample("998rrr", "UCs5FNYPHeZz5f7N1BDExxfg")]
        [RequireOwner]
        public async Task GetNotionGuild(string channelUrl)
        {
            await Context.Channel.TriggerTypingAsync();

            string channelId = "";
            try
            {
                channelId = await _service.GetChannelIdAsync(channelUrl).ConfigureAwait(false);
            }
            catch (FormatException fex)
            {
                await Context.Channel.SendErrorAsync(fex.Message);
                return;
            }
            catch (ArgumentNullException)
            {
                await Context.Channel.SendErrorAsync("網址不可空白");
                return;
            }

            using var db = _dbService.GetDbContext();

            var youtubeChannelSpider = await db.YoutubeChannelSpider.AsNoTracking().FirstOrDefaultAsync((x) => x.ChannelId == channelId);

            var guildList = new List<string>();
            foreach (var item in db.NoticeYoutubeStreamChannel.AsNoTracking().Where((x) => x.YouTubeChannelId == channelId))
            {
                var guild = _client.GetGuild(item.GuildId);
                if (guild == null)
                {
                    guildList.Add($"{item.GuildId}: (已離開)");
                }
                else
                {
                    guildList.Add($"{item.GuildId}: {guild.Name}");
                }
            }

            await Context.SendPaginatedConfirmAsync(0, (page) =>
            {
                return new EmbedBuilder()
                   .WithOkColor()
                   .WithTitle($"設定 `" + (youtubeChannelSpider != null ? youtubeChannelSpider.ChannelTitle : channelId) + "` 通知的伺服器清單")
                   .WithDescription(string.Join('\n', guildList.Skip(page * 20).Take(20)));
            }, guildList.Count, 20);
        }

        [RequireContext(ContextType.DM)]
        [RequireOwner]
        [Command("GetVideoInfo")]
        [Summary("從資料庫取得已刪檔的直播資訊")]
        [CommandExample("GzWDcutkMQw")]
        [Alias("GVI")]
        public async Task GetVideoInfo(string videoId = "")
        {
            videoId = videoId.Trim();

            if (string.IsNullOrWhiteSpace(videoId))
            {
                await ReplyAsync("Video ID 不可為空").ConfigureAwait(false);
                return;
            }

            videoId = _service.GetVideoId(videoId);

            var video = SharedExtensions.GetStreamVideoByVideoId(videoId);
            if (video == null)
            {
                await ReplyAsync($"找不到影片 {videoId}").ConfigureAwait(false);
                return;
            }

            EmbedBuilder embedBuilder = new EmbedBuilder().WithOkColor()
                .WithTitle(video.VideoTitle)
                .WithUrl($"https://www.youtube.com/watch?v={videoId}")
                .WithDescription(Format.Url(video.ChannelTitle, $"https://www.youtube.com/channel/{video.ChannelId}"))
                .AddField("排定開台時間", video.ScheduledStartTime, true);

            await ReplyAsync(embed: embedBuilder.Build()).ConfigureAwait(false);
        }

        [RequireContext(ContextType.DM)]
        [RequireOwner]
        [Command("FixYoutubeChannelNameToId")]
        [Alias("FixYTCNTI")]
        public async Task FixYoutubeChannelNameToId()
        {
            await Context.Channel.TriggerTypingAsync();

            using (var db = _dbService.GetDbContext())
            {
                try
                {
                    var list = await db.YoutubeChannelNameToId.ToListAsync();

                    foreach (var item in list)
                    {
                        item.ChannelName = item.ChannelName.ToLower();
                    }

                    int result = await db.SaveChangesAsync();

                    await Context.Channel.SendConfirmAsync($"已修正 {result} 個頻道名稱");
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), "FixYoutubeChannelNameToId");
                    await Context.Channel.SendErrorAsync(ex.Message);
                }
            }
        }

        private async Task<string> GetChannelTitle(string channelId)
        {
            try
            {
                var channel = _service.YouTubeService.Channels.List("snippet");
                channel.Id = channelId;
                var response = await channel.ExecuteAsync().ConfigureAwait(false);
                return response.Items[0].Snippet.Title;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "GetChannelTitle");
                return "";
            }
        }

        private async Task<List<string>> GetChannelTitle(IEnumerable<string> channelId)
        {
            try
            {
                var channel = _service.YouTubeService.Channels.List("snippet");
                channel.Id = string.Join(",", channelId);
                var response = await channel.ExecuteAsync().ConfigureAwait(false);
                return response.Items.Select((x) => Format.Url(x.Snippet.Title, $"https://www.youtube.com/channel/{x.Id}")).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), "GetChannelTitle");
                return null;
            }
        }
    }
}
