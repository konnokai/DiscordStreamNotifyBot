﻿using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Interaction;

namespace DiscordStreamNotifyBot.SharedService.YoutubeMember
{
    public partial class YoutubeMemberService
    {
        //https://github.com/member-gentei/member-gentei/blob/90f62385f554eb4c02ed8732e15061b9dd1dd6d0/gentei/apis/youtube.go#L100
        private async void CheckMemberShipOnlyVideoId(object stats)
        {
            List<GuildYoutubeMemberConfig> needCheckList = new();
            using (var db = _dbService.GetDbContext())
            {
                needCheckList = db.GuildYoutubeMemberConfig
                    .AsNoTracking()
                    .Where((x) => !string.IsNullOrEmpty(x.MemberCheckChannelId) && x.MemberCheckChannelId.Length == 24 && (x.MemberCheckVideoId == "-" || string.IsNullOrEmpty(x.MemberCheckChannelTitle)))
                    .Distinct((x) => x.MemberCheckChannelId)
                    .ToList();
            }

            foreach (var item in needCheckList)
            {
                using var db = _dbService.GetDbContext();

                try
                {
                    var s = _streamService.YouTubeService.PlaylistItems.List("snippet");
                    s.PlaylistId = item.MemberCheckChannelId.Replace("UC", "UUMO");
                    var result = await s.ExecuteAsync().ConfigureAwait(false);
                    var videoList = result.Items.ToList();

                    bool isCheck = false;
                    do
                    {
                        if (videoList.Count == 0)
                        {
                            await Bot.ApplicatonOwner.SendMessageAsync($"{item.MemberCheckChannelId} 無任何可檢測的會限影片!");
                            await SendMsgToLogChannelAsync(item.MemberCheckChannelId, $"{item.MemberCheckChannelId} 無會限影片，請等待該頻道主有新的會限影片且可留言時再使用會限驗證功能\n" +
                                $"你可以使用 `/youtube get-member-only-playlist` 來確認該頻道是否有可驗證的影片");
                            db.GuildYoutubeMemberConfig.Remove(item);
                            break;
                        }

                        var videoSnippet = videoList[new Random().Next(0, videoList.Count)];
                        var videoId = videoSnippet.Snippet.ResourceId.VideoId;
                        var ct = _streamService.YouTubeService.CommentThreads.List("snippet");
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
                                await SendMsgToLogChannelAsync(item.MemberCheckChannelId, $"新會限檢測影片 - ({item.MemberCheckChannelId}): {videoId}", false, false);

                                foreach (var item2 in db.GuildYoutubeMemberConfig.Where((x) => x.MemberCheckChannelId == item.MemberCheckChannelId))
                                {
                                    item2.MemberCheckVideoId = videoId;
                                    db.GuildYoutubeMemberConfig.Update(item2);
                                }

                                isCheck = true;
                            }
                            else
                            {
                                Log.Error(ex.Demystify(), $"{item.MemberCheckChannelId} 新會限影片檢查錯誤");

                                foreach (var item2 in db.GuildYoutubeMemberConfig.Where((x) => x.MemberCheckChannelId == item.MemberCheckChannelId))
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
                        await SendMsgToLogChannelAsync(item.MemberCheckChannelId, $"{item.MemberCheckChannelId} 無會限影片，請等待該頻道主有新的會限影片且可留言時再使用會限驗證功能\n" +
                            $"你可以使用 `/youtube get-member-only-playlist` 來確認該頻道是否有可驗證的影片");
                        db.GuildYoutubeMemberConfig.Remove(item);
                        continue;
                    }
                    else Log.Warn($"CheckMemberShipOnlyVideoId: {item.GuildId} / {item.MemberCheckChannelId}\n{ex}");
                }

                try
                {
                    var c = _streamService.YouTubeService.Channels.List("snippet");
                    c.Id = item.MemberCheckChannelId;
                    var channelResult = await c.ExecuteAsync();
                    var channel = channelResult.Items.First();

                    Log.Info($"會限頻道名稱已變更 - ({item.MemberCheckChannelId}): `" + (string.IsNullOrEmpty(item.MemberCheckChannelTitle) ? "無" : item.MemberCheckChannelTitle) + $"` -> `{channel.Snippet.Title}`");
                    await SendMsgToLogChannelAsync(item.MemberCheckChannelId, $"會限頻道名稱已變更: `" + (string.IsNullOrEmpty(item.MemberCheckChannelTitle) ? "無" : item.MemberCheckChannelTitle) + $"` -> `{channel.Snippet.Title}`", false, false);

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

            //Log.Info("檢查新會限影片完成");
        }
    }
}