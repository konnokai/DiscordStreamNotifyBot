using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.Shared.Messages;
using Google;
using Polly;
using TableVideo = DiscordStreamNotifyBot.DataBase.Table.Video;
using YTApiVideo = Google.Apis.YouTube.v3.Data.Video;

using Bot = DiscordStreamNotifyBot.Shared.BotState;

namespace DiscordStreamNotifyBot.Scraper.Detection.Youtube
{
    public partial class YoutubeDetectionService
    {
        private const int MaxReminderDays = 14;
        private const int ReminderAdvanceMinutes = 1;
        private const int StartTimeGraceMinutes = 2;
        private const int MinTimerDelayMs = 1000;

        private void StartReminder(TableVideo streamVideo, TableVideo.YTChannelType channelType)
        {
            if (streamVideo.ScheduledStartTime > DateTime.Now.AddDays(MaxReminderDays)) return;

            try
            {
                TimeSpan ts = streamVideo.ScheduledStartTime.AddMinutes(-ReminderAdvanceMinutes).Subtract(DateTime.Now);

                if (ts <= TimeSpan.Zero)
                {
                    Task.Run(() => SafeReminderTimerActionAsync(streamVideo));
                }
                else
                {
                    var remT = new Timer(TimerCallbackWrapper, streamVideo, Math.Max(MinTimerDelayMs, (long)ts.TotalMilliseconds), Timeout.Infinite);

                    if (!Reminders.TryAdd(streamVideo.VideoId, new ReminderItem() { StreamVideo = streamVideo, Timer = remT, ChannelType = channelType }))
                    {
                        remT.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"StartReminder: {streamVideo.VideoTitle} - {streamVideo.ScheduledStartTime}");
                throw;
            }
        }

        private void TimerCallbackWrapper(object state)
        {
            _ = SafeReminderTimerActionAsync(state);
        }

        private async Task SafeReminderTimerActionAsync(object rObj)
        {
            try
            {
                await ReminderTimerActionAsync(rObj);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"SafeReminderTimerActionAsync: {((TableVideo)rObj).VideoId}");
            }
        }

        private async Task ReminderTimerActionAsync(object rObj)
        {
            var streamVideo = (TableVideo)rObj;
            var db = _dbService.GetDbContext();

            try
            {
                var videoResult = await TryGetVideoResult(streamVideo);
                if (videoResult == null) return;

                if (!TryGetStartTime(videoResult, out DateTime startTime))
                {
                    Log.Error($"無法解析影片開始時間: {streamVideo.VideoId}");
                    return;
                }

                if (startTime.AddMinutes(-StartTimeGraceMinutes) < DateTime.Now)
                {
                    await HandleStreamStartAsync(streamVideo, videoResult, db);
                }
                else
                {
                    await HandleStreamTimeChangedAsync(streamVideo, videoResult, db, startTime);
                }
            }
            catch (Exception ex) { Log.Error(ex.Demystify(), $"ReminderAction: {streamVideo.VideoId}"); }
        }

        private async Task<YTApiVideo> TryGetVideoResult(TableVideo streamVideo)
        {
            try
            {
                var videoResult = await GetVideoAsync(streamVideo.VideoId);
                if (videoResult == null)
                {
                    Log.Info($"{streamVideo.VideoId} 待機所被刪了");
                    await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.Delete).ConfigureAwait(false);
                    return null;
                }
                return videoResult;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"ReminderTimerAction-CheckVideoExist");
                return null;
            }
        }

        private bool TryGetStartTime(YTApiVideo videoResult, out DateTime startTime)
        {
            startTime = default;
            if (!string.IsNullOrEmpty(videoResult.LiveStreamingDetails?.ScheduledStartTimeRaw))
                return DateTime.TryParse(videoResult.LiveStreamingDetails.ScheduledStartTimeRaw, out startTime);
            if (!string.IsNullOrEmpty(videoResult.LiveStreamingDetails?.ActualStartTimeRaw))
                return DateTime.TryParse(videoResult.LiveStreamingDetails.ActualStartTimeRaw, out startTime);
            return false;
        }

        private async Task HandleStreamStartAsync(TableVideo streamVideo, YTApiVideo videoResult, MainDbContext db)
        {
            bool isRecord = false;
            streamVideo.VideoTitle = videoResult.Snippet.Title;
            var video = GetDbVideoByType(db, streamVideo);
            try
            {
                if (video != null)
                {
                    video.VideoTitle = streamVideo.VideoTitle;
                    db.UpdateAndSave(video);
                }
                else if (addNewStreamVideo.ContainsKey(streamVideo.VideoId))
                {
                    addNewStreamVideo[streamVideo.VideoId] = streamVideo;
                }
                else
                {
                    Log.Error($"({streamVideo.ChannelType}) 直播標題變更保存失敗，找不到資料: {streamVideo.VideoId}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"({streamVideo.ChannelType}) 直播標題變更保存失敗: {streamVideo.VideoId}");
            }

#if RELEASE
            try
            {
                if (CanRecord(streamVideo))
                {
                    if (Bot.Redis != null)
                    {
                        if (await Bot.RedisSub.PublishAsync(new RedisChannel("youtube.record", RedisChannel.PatternMode.Literal), streamVideo.VideoId) != 0)
                        {
                            Log.Info($"已發送 YouTube 錄影請求: {streamVideo.VideoId}");
                            isRecord = true;
                        }
                        else
                        {
                            Log.Warn($"Redis Sub 頻道不存在，請開啟錄影工具: {streamVideo.VideoId}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"ReminderTimerAction-Record: {streamVideo.VideoId}\n{ex}");
            }
#endif

            await ChangeGuildBannerAsync(streamVideo.ChannelId, streamVideo.VideoId);

            if (!isRecord)
            {
                await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.Start).ConfigureAwait(false);
            }

            if (Reminders.TryRemove(streamVideo.VideoId, out var t))
                t.Timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private async Task HandleStreamTimeChangedAsync(TableVideo streamVideo, YTApiVideo videoResult, MainDbContext db, DateTime startTime)
        {
            Log.Info($"時間已更改 {streamVideo.ChannelTitle} - {streamVideo.VideoTitle}");

            streamVideo.ScheduledStartTime = startTime;
            var video = GetDbVideoByType(db, streamVideo);
            try
            {
                if (video != null)
                {
                    video.ScheduledStartTime = streamVideo.ScheduledStartTime;
                    db.UpdateAndSave(video);
                }
                else if (addNewStreamVideo.ContainsKey(streamVideo.VideoId))
                {
                    addNewStreamVideo[streamVideo.VideoId] = streamVideo;
                }
                else
                {
                    Log.Error($"({streamVideo.ChannelType}) 直播時間變更保存失敗，找不到資料: {streamVideo.VideoId}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"({streamVideo.ChannelType}) 直播時間變更保存失敗: {streamVideo.VideoId}");
            }

            await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.ChangeTime).ConfigureAwait(false);

            if (Reminders.TryRemove(streamVideo.VideoId, out var t))
                t.Timer.Change(Timeout.Infinite, Timeout.Infinite);

            StartReminder(streamVideo, streamVideo.ChannelType);
        }

        private TableVideo GetDbVideoByType(MainDbContext db, TableVideo streamVideo)
        {
            return streamVideo.ChannelType switch
            {
                TableVideo.YTChannelType.Holo => db.HoloVideos.FirstOrDefault((x) => x.VideoId == streamVideo.VideoId),
                TableVideo.YTChannelType.Nijisanji => db.NijisanjiVideos.FirstOrDefault((x) => x.VideoId == streamVideo.VideoId),
                TableVideo.YTChannelType.Other => db.OtherVideos.FirstOrDefault((x) => x.VideoId == streamVideo.VideoId),
                _ => null
            };
        }

        public async Task<YTApiVideo> GetVideoDurationAsync(string videoId)
        {
            var pBreaker = Policy<YTApiVideo>
                .Handle<Exception>()
                .WaitAndRetryAsync(3, (retryAttempt) =>
                {
                    var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                    Log.Warn($"YouTube GetVideoDurationAsync ({videoId}) 失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                    return timeSpan;
                });

            return await pBreaker.ExecuteAsync(async () =>
            {
                var video = YouTubeService.Videos.List("contentDetails");
                video.Id = videoId;
                var videoResult = await video.ExecuteAsync().ConfigureAwait(false);
                if (videoResult.Items.Count == 0) return null;
                return videoResult.Items[0];
            });
        }

        public async Task<bool> GetCommentThreadsIsDisabledAsync(string videoId)
        {
            var pBreaker = Policy<bool>
                .Handle<Exception>()
                .WaitAndRetryAsync(3, (retryAttempt) =>
                {
                    var timeSpan = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                    Log.Warn($"YouTube GetCommentThreadsIsDisabledAsync ({videoId}) 失敗，將於 {timeSpan.TotalSeconds} 秒後重試 (第 {retryAttempt} 次重試)");
                    return timeSpan;
                });

            return await pBreaker.ExecuteAsync(async () =>
            {
                var listComment = YouTubeService.CommentThreads.List("id");
                listComment.VideoId = videoId;

                try
                {
                    await listComment.ExecuteAsync().ConfigureAwait(false);
                    return false;
                }
                catch (GoogleApiException apiEx) when ((apiEx.HttpStatusCode == System.Net.HttpStatusCode.Forbidden) || (apiEx.HttpStatusCode == System.Net.HttpStatusCode.BadRequest))
                {
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"GetCommentThreadsIsDisabledAsync: {videoId} 未知的錯誤");
                    return true;
                }
            });
        }
    }
}
