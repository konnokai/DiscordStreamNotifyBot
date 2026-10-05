using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using Google;

using Bot = DiscordStreamNotifyBot.Shared.BotState;

namespace DiscordStreamNotifyBot.Scraper.Detection.Youtube
{
    public partial class YoutubeDetectionService
    {
        // 會限影片探索在 Scraper 單例執行，逐使用者驗證仍由各 Notifier shard 負責。
        internal async Task CheckMemberShipOnlyVideoIdAsync()
        {
            List<(ulong GuildId, string ChannelId, bool DiscoverVideo, bool RefreshChannelTitle)> needCheckList;
            using (var db = _dbService.GetDbContext())
            {
                var configs = db.GuildYoutubeMemberConfig
                    .AsNoTracking()
                    .Where((x) => !string.IsNullOrEmpty(x.MemberCheckChannelId) && x.MemberCheckChannelId.Length == 24)
                    .ToList();

                needCheckList = configs
                    .GroupBy((x) => x.MemberCheckChannelId)
                    .Select((group) => (
                        group.First().GuildId,
                        ChannelId: group.Key,
                        // 手動指定影片的設定不參與自動探索
                        DiscoverVideo: group.Any((x) => !x.IsManualVideoId &&
                            (string.IsNullOrEmpty(x.MemberCheckVideoId) || x.MemberCheckVideoId == "-")),
                        RefreshChannelTitle: group.Any((x) => string.IsNullOrEmpty(x.MemberCheckChannelTitle))))
                    .Where((x) => x.DiscoverVideo || x.RefreshChannelTitle)
                    .ToList();
            }

            foreach (var item in needCheckList)
            {
                using var db = _dbService.GetDbContext();

                if (item.DiscoverVideo)
                {
                    try
                    {
                        var request = YouTubeService.PlaylistItems.List("snippet");
                        request.PlaylistId = item.ChannelId.Replace("UC", "UUMO");
                        var result = await request.ExecuteAsync().ConfigureAwait(false);
                        var videoList = result.Items.ToList();
                        // 選到會限影片時寫入影片 ID，中止探索時清空；兩者都沒發生代表沒有可檢測的影片
                        bool stopped = false;
                        string newVideoId = null;

                        while (videoList.Count > 0)
                        {
                            int candidateIndex = Random.Shared.Next(videoList.Count);
                            var videoSnippet = videoList[candidateIndex];
                            videoList.RemoveAt(candidateIndex);
                            var videoId = videoSnippet.Snippet.ResourceId.VideoId;
                            var commentRequest = YouTubeService.CommentThreads.List("snippet");
                            commentRequest.VideoId = videoId;

                            MemberCandidateAction action;
                            try
                            {
                                // 能讀到留言代表是公開影片，換下一部
                                _ = await commentRequest.ExecuteAsync().ConfigureAwait(false);
                                action = MemberCandidateAction.Skip;
                            }
                            catch (Exception ex)
                            {
                                int? statusCode = ex is GoogleApiException apiException
                                    ? (int)apiException.HttpStatusCode
                                    : null;
                                action = ClassifyMemberCandidateError(statusCode, ex.Message);
                            }

                            if (action == MemberCandidateAction.Skip)
                                continue;

                            if (action == MemberCandidateAction.Select)
                            {
                                Log.Info($"新會限影片（{item.ChannelId}）：{videoId}");
                                await PublishMemberVideoLogAsync(item.ChannelId,
                                    isNeedRemove: false, isNeedSendToOwner: false,
                                    messageCode: "NewProbeVideo", messageArguments: [item.ChannelId, videoId]);
                                newVideoId = videoId;
                            }
                            else
                            {
                                Log.Error($"{item.ChannelId} 新會限影片檢查錯誤");
                                newVideoId = "";
                            }

                            stopped = true;
                            break;
                        }

                        if (!stopped)
                        {
                            await PublishNoMemberVideosAsync(item.ChannelId, notifyBotOwner: true);
                        }
                        else
                        {
                            foreach (var config in await db.GuildYoutubeMemberConfig
                                .Where((x) => x.MemberCheckChannelId == item.ChannelId && !x.IsManualVideoId)
                                .ToListAsync())
                            {
                                config.MemberCheckVideoId = newVideoId;
                                db.GuildYoutubeMemberConfig.Update(config);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("playlistid", StringComparison.OrdinalIgnoreCase))
                        {
                            Log.Warn($"CheckMemberShipOnlyVideoId: {item.GuildId} / {item.ChannelId} 無會限影片可供檢測");
                            await PublishNoMemberVideosAsync(item.ChannelId, notifyBotOwner: false);
                        }
                        else
                        {
                            Log.Warn($"CheckMemberShipOnlyVideoId: {item.GuildId} / {item.ChannelId}\n{ex}");
                        }
                    }
                }

                if (item.RefreshChannelTitle)
                {
                    try
                    {
                        var request = YouTubeService.Channels.List("snippet");
                        request.Id = item.ChannelId;
                        var channelResult = await request.ExecuteAsync();
                        var channel = channelResult.Items.First();
                        var previousTitle = await db.GuildYoutubeMemberConfig.AsNoTracking()
                            .Where((x) => x.MemberCheckChannelId == item.ChannelId)
                            .Select((x) => x.MemberCheckChannelTitle)
                            .FirstOrDefaultAsync();

                        Log.Info($"會限頻道名稱已變更（{item.ChannelId}）：`" +
                            (string.IsNullOrEmpty(previousTitle) ? "無" : previousTitle) + $"` -> `{channel.Snippet.Title}`");
                        await PublishMemberVideoLogAsync(item.ChannelId,
                            isNeedRemove: false, isNeedSendToOwner: false,
                            messageCode: "ChannelTitleChanged",
                            messageArguments: [previousTitle ?? string.Empty, channel.Snippet.Title]);

                        foreach (var config in await db.GuildYoutubeMemberConfig
                            .Where((x) => x.MemberCheckChannelId == item.ChannelId)
                            .ToListAsync())
                        {
                            config.MemberCheckChannelTitle = channel.Snippet.Title;
                            db.GuildYoutubeMemberConfig.Update(config);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warn($"CheckMemberShipOnlyChannelName: {item.GuildId} / {item.ChannelId}\n{ex}");
                    }
                }

                await db.SaveChangesAsync();
            }
        }

        internal enum MemberCandidateAction { Skip, Select, Abort }

        /// <summary>
        /// 依留言串查詢的錯誤判斷候選影片：留言關閉或影片不存在就換下一部，沒有權限代表是會限影片，其他錯誤中止探索。
        /// 留言關閉要先判斷，避免同時帶 403 時被當成會限影片。
        /// </summary>
        internal static MemberCandidateAction ClassifyMemberCandidateError(int? httpStatusCode, string errorMessage)
        {
            string message = errorMessage?.ToLowerInvariant() ?? string.Empty;
            if (message.Contains("disabled comments") ||
                httpStatusCode == 404 || message.Contains("notfound") || message.Contains("not found"))
                return MemberCandidateAction.Skip;
            if (httpStatusCode == 403 || message.Contains("403") || message.Contains("forbidden") ||
                message.Contains("unauthorized") || message.Contains("the request might not be properly authorized"))
                return MemberCandidateAction.Select;

            return MemberCandidateAction.Abort;
        }

        private static Task PublishNoMemberVideosAsync(string channelId, bool notifyBotOwner)
            => PublishMemberVideoLogAsync(channelId,
                messageCode: "NoVideos",
                messageArguments: [channelId],
                botOwnerMessage: notifyBotOwner ? $"{channelId} 無任何可檢測的會限影片！" : null);

        private static Task PublishMemberVideoLogAsync(string checkChannelId, string messageCode, string[] messageArguments,
            bool isNeedRemove = true, bool isNeedSendToOwner = true, string botOwnerMessage = null)
            => NotificationBus.PublishAsync(Bot.RedisDb, NotifyType.YoutubeMemberVideoLog, new YoutubeMemberVideoLogNotification
            {
                CheckChannelId = checkChannelId,
                MessageCode = messageCode,
                MessageArguments = messageArguments,
                IsNeedRemove = isNeedRemove,
                IsNeedSendToOwner = isNeedSendToOwner,
                BotOwnerMessage = botOwnerMessage,
            });
    }
}
