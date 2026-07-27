using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;

using Bot = DiscordStreamNotifyBot.Shared.BotState;

namespace DiscordStreamNotifyBot.Scraper.Detection.Youtube
{
    public partial class YoutubeDetectionService
    {
        // 會限影片探索（頻道層級、bot 金鑰、無逐使用者 token）：原 Notifier
        // YoutubeMemberService.CheckMemberShipOnlyVideoId 搬來 Scraper（叢集唯一偵測宿主），
        // 避免每個 shard 各跑一份重複燒 YouTube 配額。需寫入 guild log channel 的結果改 publish
        // YoutubeMemberVideoLogNotification 至匯流排，由 Notifier 依 shard 守衛發送。
        // https://github.com/member-gentei/member-gentei/blob/90f62385f554eb4c02ed8732e15061b9dd1dd6d0/gentei/apis/youtube.go#L100
        internal async Task CheckMemberShipOnlyVideoIdAsync()
        {
            List<GuildYoutubeMemberConfig> needCheckList;
            using (var db = _dbService.GetDbContext())
            {
                // 只探索「尚未有有效 videoId 或尚無頻道名稱」且非管理員手動 pin 的頻道（IsManualVideoId 保護）
                needCheckList = db.GuildYoutubeMemberConfig
                    .AsNoTracking()
                    .Where((x) => !string.IsNullOrEmpty(x.MemberCheckChannelId) && x.MemberCheckChannelId.Length == 24
                        && !x.IsManualVideoId
                        && (x.MemberCheckVideoId == "-" || string.IsNullOrEmpty(x.MemberCheckChannelTitle)))
                    .ToList()
                    .DistinctBy((x) => x.MemberCheckChannelId)
                    .ToList();
            }

            foreach (var item in needCheckList)
            {
                using var db = _dbService.GetDbContext();

                try
                {
                    var s = YouTubeService.PlaylistItems.List("snippet");
                    s.PlaylistId = item.MemberCheckChannelId.Replace("UC", "UUMO");
                    var result = await s.ExecuteAsync().ConfigureAwait(false);
                    var videoList = result.Items.ToList();

                    bool isCheck = false;
                    do
                    {
                        if (videoList.Count == 0)
                        {
                            // 原本額外私訊 Bot 擁有者；Scraper 無 gateway，改由 DTO 帶 BotOwnerMessage 讓 Notifier shard 0 補送。
                            // 原本 db.Remove(item)：改由 DTO IsNeedRemove=true 讓 Notifier 依 shard 守衛各刪各的。
                            await PublishMemberVideoLogAsync(item.MemberCheckChannelId,
                                $"{item.MemberCheckChannelId} 無會限影片，請等待該頻道主有新的會限影片且可留言時再使用會限驗證功能\n" +
                                $"你可以使用 `/youtube get-member-only-playlist` 來確認該頻道是否有可驗證的影片",
                                messageCode: "NoVideos",
                                messageArguments: [item.MemberCheckChannelId],
                                botOwnerMessage: $"{item.MemberCheckChannelId} 無任何可檢測的會限影片!");
                            break;
                        }

                        var videoSnippet = videoList[new Random().Next(0, videoList.Count)];
                        var videoId = videoSnippet.Snippet.ResourceId.VideoId;
                        var ct = YouTubeService.CommentThreads.List("snippet");
                        ct.VideoId = videoId;

                        try
                        {
                            _ = await ct.ExecuteAsync().ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.ToLower().Contains("disabled comments"))
                            {
                                videoList.Remove(videoSnippet);
                            }
                            else if (ex.Message.ToLower().Contains("403") || ex.Message.ToLower().Contains("the request might not be properly authorized"))
                            {
                                Log.Info($"新會限影片 - ({item.MemberCheckChannelId}): {videoId}");
                                await PublishMemberVideoLogAsync(item.MemberCheckChannelId,
                                    $"新會限檢測影片 - ({item.MemberCheckChannelId}): {videoId}",
                                    isNeedRemove: false, isNeedSendToOwner: false,
                                    messageCode: "NewProbeVideo", messageArguments: [item.MemberCheckChannelId, videoId]);

                                // 頻道層級批次更新，但跳過管理員手動 pin 的 config（IsManualVideoId 保護）
                                foreach (var item2 in db.GuildYoutubeMemberConfig.Where((x) => x.MemberCheckChannelId == item.MemberCheckChannelId && !x.IsManualVideoId))
                                {
                                    item2.MemberCheckVideoId = videoId;
                                    db.GuildYoutubeMemberConfig.Update(item2);
                                }

                                isCheck = true;
                            }
                            else
                            {
                                Log.Error(ex.Demystify(), $"{item.MemberCheckChannelId} 新會限影片檢查錯誤");

                                foreach (var item2 in db.GuildYoutubeMemberConfig.Where((x) => x.MemberCheckChannelId == item.MemberCheckChannelId && !x.IsManualVideoId))
                                {
                                    item2.MemberCheckVideoId = "";
                                    db.GuildYoutubeMemberConfig.Update(item2);
                                }

                                isCheck = true;
                            }
                        }
                    } while (!isCheck);
                }
                catch (Exception ex)
                {
                    if (ex.Message.ToLower().Contains("playlistid"))
                    {
                        Log.Warn($"CheckMemberShipOnlyVideoId: {item.GuildId} / {item.MemberCheckChannelId} 無會限影片可供檢測");
                        await PublishMemberVideoLogAsync(item.MemberCheckChannelId,
                            $"{item.MemberCheckChannelId} 無會限影片，請等待該頻道主有新的會限影片且可留言時再使用會限驗證功能\n" +
                            $"你可以使用 `/youtube get-member-only-playlist` 來確認該頻道是否有可驗證的影片",
                            messageCode: "NoVideos", messageArguments: [item.MemberCheckChannelId]);
                        continue;
                    }
                    else Log.Warn($"CheckMemberShipOnlyVideoId: {item.GuildId} / {item.MemberCheckChannelId}\n{ex}");
                }

                try
                {
                    var c = YouTubeService.Channels.List("snippet");
                    c.Id = item.MemberCheckChannelId;
                    var channelResult = await c.ExecuteAsync();
                    var channel = channelResult.Items.First();

                    Log.Info($"會限頻道名稱已變更 - ({item.MemberCheckChannelId}): `" + (string.IsNullOrEmpty(item.MemberCheckChannelTitle) ? "無" : item.MemberCheckChannelTitle) + $"` -> `{channel.Snippet.Title}`");
                    await PublishMemberVideoLogAsync(item.MemberCheckChannelId,
                        $"會限頻道名稱已變更: `" + (string.IsNullOrEmpty(item.MemberCheckChannelTitle) ? "無" : item.MemberCheckChannelTitle) + $"` -> `{channel.Snippet.Title}`",
                        isNeedRemove: false, isNeedSendToOwner: false,
                        messageCode: "ChannelTitleChanged",
                        messageArguments: [item.MemberCheckChannelTitle ?? string.Empty, channel.Snippet.Title]);

                    // 頻道名稱與手動 pin 的 videoId 無關，維持全體更新
                    foreach (var item2 in db.GuildYoutubeMemberConfig.Where((x) => x.MemberCheckChannelId == item.MemberCheckChannelId))
                    {
                        item2.MemberCheckChannelTitle = channel.Snippet.Title;
                        db.GuildYoutubeMemberConfig.Update(item2);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"CheckMemberShipOnlyChannelName: {item.GuildId} / {item.MemberCheckChannelId}\n{ex}");
                }

                await db.SaveChangesAsync();
            }
        }

        /// <summary>把「會限影片探索需寫入 log channel」的結果 publish 至匯流排，由 Notifier 依 shard 守衛發送。</summary>
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
