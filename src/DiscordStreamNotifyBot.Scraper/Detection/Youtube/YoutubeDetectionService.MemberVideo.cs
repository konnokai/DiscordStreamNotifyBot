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
            List<(ulong GuildId, string ChannelId, YoutubeMemberChannelDecision Decision)> needCheckList;
            using (var db = _dbService.GetDbContext())
            {
                var configs = db.GuildYoutubeMemberConfig
                    .AsNoTracking()
                    .Where((x) => !string.IsNullOrEmpty(x.MemberCheckChannelId) && x.MemberCheckChannelId.Length == 24)
                    .ToList();

                needCheckList = configs
                    .GroupBy((x) => x.MemberCheckChannelId)
                    .Select((group) =>
                    {
                        var decision = YoutubeMemberVideoPolicy.PlanChannel(new YoutubeMemberChannelFacts(
                            group.Any((x) => !x.IsManualVideoId &&
                                (string.IsNullOrEmpty(x.MemberCheckVideoId) || x.MemberCheckVideoId == "-")),
                            group.Any((x) => string.IsNullOrEmpty(x.MemberCheckChannelTitle))));
                        return (group.First().GuildId, ChannelId: group.Key, Decision: decision);
                    })
                    .Where((x) => x.Decision.DiscoverVideo || x.Decision.RefreshChannelTitle)
                    .ToList();
            }

            foreach (var item in needCheckList)
            {
                using var db = _dbService.GetDbContext();

                if (item.Decision.DiscoverVideo)
                {
                    try
                    {
                        var request = YouTubeService.PlaylistItems.List("snippet");
                        request.PlaylistId = item.ChannelId.Replace("UC", "UUMO");
                        var result = await request.ExecuteAsync().ConfigureAwait(false);
                        var videoList = result.Items.ToList();
                        bool selected = false;
                        bool aborted = false;

                        while (videoList.Count > 0)
                        {
                            int candidateIndex = Random.Shared.Next(videoList.Count);
                            var videoSnippet = videoList[candidateIndex];
                            videoList.RemoveAt(candidateIndex);
                            var videoId = videoSnippet.Snippet.ResourceId.VideoId;
                            var commentRequest = YouTubeService.CommentThreads.List("snippet");
                            commentRequest.VideoId = videoId;

                            YoutubeMemberCandidateAction action;
                            try
                            {
                                _ = await commentRequest.ExecuteAsync().ConfigureAwait(false);
                                action = YoutubeMemberVideoPolicy.ClassifyCandidate(
                                    new YoutubeMemberCandidateFacts(true, null, null));
                            }
                            catch (Exception ex)
                            {
                                int? statusCode = ex is GoogleApiException apiException
                                    ? (int)apiException.HttpStatusCode
                                    : null;
                                action = YoutubeMemberVideoPolicy.ClassifyCandidate(
                                    new YoutubeMemberCandidateFacts(false, statusCode, ex.Message));
                            }

                            if (action is YoutubeMemberCandidateAction.IgnorePublicVideo or
                                YoutubeMemberCandidateAction.IgnoreCommentsDisabled or
                                YoutubeMemberCandidateAction.IgnoreUnavailable)
                                continue;

                            if (action == YoutubeMemberCandidateAction.SelectMemberOnlyVideo)
                            {
                                Log.Info($"新會限影片（{item.ChannelId}）：{videoId}");
                                await PublishMemberVideoLogAsync(item.ChannelId,
                                    $"新會限檢測影片（{item.ChannelId}）：{videoId}",
                                    isNeedRemove: false, isNeedSendToOwner: false,
                                    messageCode: "NewProbeVideo", messageArguments: [item.ChannelId, videoId]);

                                foreach (var config in await db.GuildYoutubeMemberConfig
                                    .Where((x) => x.MemberCheckChannelId == item.ChannelId && !x.IsManualVideoId)
                                    .ToListAsync())
                                {
                                    config.MemberCheckVideoId = videoId;
                                    db.GuildYoutubeMemberConfig.Update(config);
                                }

                                selected = true;
                                break;
                            }

                            Log.Error($"{item.ChannelId} 新會限影片檢查錯誤");
                            foreach (var config in await db.GuildYoutubeMemberConfig
                                .Where((x) => x.MemberCheckChannelId == item.ChannelId && !x.IsManualVideoId)
                                .ToListAsync())
                            {
                                config.MemberCheckVideoId = "";
                                db.GuildYoutubeMemberConfig.Update(config);
                            }

                            aborted = true;
                            break;
                        }

                        if (!selected && !aborted)
                            await PublishNoMemberVideosAsync(item.ChannelId, notifyBotOwner: true);
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

                if (item.Decision.RefreshChannelTitle)
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
                            $"會限頻道名稱已變更：`" +
                            (string.IsNullOrEmpty(previousTitle) ? "無" : previousTitle) + $"` -> `{channel.Snippet.Title}`",
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

        private static Task PublishNoMemberVideosAsync(string channelId, bool notifyBotOwner)
            => PublishMemberVideoLogAsync(channelId,
                $"{channelId} 無會限影片，請等待該頻道主有新的會限影片且可留言時再使用會限驗證功能\n" +
                $"你可以使用 `/youtube get-member-only-playlist` 來確認該頻道是否有可驗證的影片",
                messageCode: "NoVideos",
                messageArguments: [channelId],
                botOwnerMessage: notifyBotOwner ? $"{channelId} 無任何可檢測的會限影片！" : null);

        private static Task PublishMemberVideoLogAsync(string checkChannelId, string message,
            bool isNeedRemove = true, bool isNeedSendToOwner = true, string botOwnerMessage = null,
            string messageCode = null, string[] messageArguments = null)
            => NotificationBus.PublishAsync(Bot.RedisDb, NotifyType.YoutubeMemberVideoLog, new YoutubeMemberVideoLogNotification
            {
                CheckChannelId = checkChannelId,
                Message = message,
                MessageCode = messageCode,
                MessageArguments = messageArguments,
                IsNeedRemove = isNeedRemove,
                IsNeedSendToOwner = isNeedSendToOwner,
                BotOwnerMessage = botOwnerMessage,
            });
    }
}
