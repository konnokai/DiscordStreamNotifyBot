using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.Shared.Messages;
using Google;
using Polly;
using System.Collections.Concurrent;
using Bot = DiscordStreamNotifyBot.Shared.BotState;
using TableVideo = DiscordStreamNotifyBot.DataBase.Table.Video;
using YTApiVideo = Google.Apis.YouTube.v3.Data.Video;

namespace DiscordStreamNotifyBot.Scraper.Detection.Youtube
{
    public partial class YoutubeDetectionService
    {
        private static readonly TimeSpan ReminderRetryDelay = TimeSpan.FromMinutes(1);

        private void StartReminder(TableVideo streamVideo, TableVideo.YTChannelType channelType)
        {
            var decision = YoutubeReminderPolicy.PlanStart(streamVideo.ScheduledStartTime, DateTime.Now);
            if (decision.Action == YoutubeReminderStartAction.Ignore)
                return;

            try
            {
                var reminder = new ReminderItem
                {
                    StreamVideo = streamVideo,
                    ChannelType = channelType,
                };
                var dueTime = decision.Action == YoutubeReminderStartAction.RunImmediately
                    ? TimeSpan.Zero
                    : decision.Delay;
                var remT = new Timer(TimerCallbackWrapper, reminder, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                reminder.Timer = remT;

                if (!Reminders.TryAdd(streamVideo.VideoId, reminder))
                {
                    remT.Dispose();
                    return;
                }

                try
                {
                    remT.Change(dueTime, Timeout.InfiniteTimeSpan);
                }
                catch
                {
                    Reminders.TryRemove(new KeyValuePair<string, ReminderItem>(streamVideo.VideoId, reminder));
                    remT.Dispose();
                    throw;
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
            var owner = rObj as ReminderItem;
            var streamVideo = owner?.StreamVideo ?? (TableVideo)rObj;
            try
            {
                await ReminderTimerActionAsync(streamVideo, owner);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"SafeReminderTimerActionAsync: {streamVideo.VideoId}");
            }
        }

        private async Task ReminderTimerActionAsync(TableVideo streamVideo, ReminderItem owner = null)
        {
            using var db = _dbService.GetDbContext();

            try
            {
                var (videoResult, isDeleted) = await TryGetVideoResult(streamVideo);
                if (videoResult == null)
                {
                    if (isDeleted)
                    {
                        if (TryClaimReminderAction(streamVideo, owner))
                            await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.Delete).ConfigureAwait(false);
                    }
                    else
                        ScheduleReminderRetry(streamVideo, owner);
                    return;
                }

                if (!TryGetStartTime(videoResult, out DateTime startTime))
                {
                    Log.Error($"無法解析影片開始時間: {streamVideo.VideoId}");
                    ScheduleReminderRetry(streamVideo, owner);
                    return;
                }

                if (!TryClaimReminderAction(streamVideo, owner))
                    return;

                if (YoutubeReminderPolicy.DecideApiRecheck(startTime, DateTime.Now) ==
                    YoutubeReminderApiAction.TreatAsStarted)
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

        private async Task<(YTApiVideo Video, bool IsDeleted)> TryGetVideoResult(TableVideo streamVideo)
        {
            try
            {
                var videoResult = await GetVideoAsync(streamVideo.VideoId);
                if (videoResult == null)
                {
                    Log.Info($"{streamVideo.VideoId} 待機所被刪了");
                    return (null, true);
                }
                return (videoResult, false);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"ReminderTimerAction-CheckVideoExist");
                return (null, false);
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

        private async Task HandleStreamStartAsync(
            TableVideo streamVideo,
            YTApiVideo videoResult,
            MainDbContext db)
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

            await PublishBannerAsync(streamVideo.ChannelId, streamVideo.VideoId);

            if (!isRecord)
            {
                await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.Start).ConfigureAwait(false);
            }

        }

        private async Task HandleStreamTimeChangedAsync(
            TableVideo streamVideo,
            YTApiVideo videoResult,
            MainDbContext db,
            DateTime startTime)
        {
            var previousScheduledStartTime = streamVideo.ScheduledStartTime;
            Log.Info($"時間已更改 {streamVideo.ChannelTitle} - {streamVideo.VideoTitle}: {previousScheduledStartTime:O} -> {startTime:O}");

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

            await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.ChangeTime,
                previousScheduledStartTime: previousScheduledStartTime).ConfigureAwait(false);

            StartReminder(streamVideo, streamVideo.ChannelType);
        }

        private bool TryClaimReminderAction(TableVideo streamVideo, ReminderItem owner)
        {
            if (!TryClaimReminderAction(
                Reminders,
                streamVideo.VideoId,
                streamVideo,
                owner,
                out var reminder))
                return false;

            if (reminder != null)
            {
                reminder.Timer?.Change(Timeout.Infinite, Timeout.Infinite);
                reminder.Timer?.Dispose();
            }
            return true;
        }

        internal static bool TryClaimReminderAction(
            ConcurrentDictionary<string, ReminderItem> reminders,
            string videoId,
            TableVideo expectedStreamVideo,
            ReminderItem expectedReminder,
            out ReminderItem reminder)
        {
            reminder = null;
            if (expectedReminder == null)
                return !reminders.ContainsKey(videoId);

            return TryTakeReminder(
                reminders,
                videoId,
                expectedStreamVideo,
                expectedReminder,
                out reminder);
        }

        private bool RemoveReminder(
            string videoId,
            TableVideo expectedStreamVideo = null,
            ReminderItem expectedReminder = null)
        {
            if (!TryTakeReminder(Reminders, videoId, expectedStreamVideo, expectedReminder, out var reminder))
                return false;

            reminder.Timer?.Change(Timeout.Infinite, Timeout.Infinite);
            reminder.Timer?.Dispose();
            return true;
        }

        private void ScheduleReminderRetry(TableVideo streamVideo, ReminderItem owner)
        {
            if (Reminders.TryGetValue(streamVideo.VideoId, out var current))
            {
                lock (current)
                {
                    if (!Reminders.TryGetValue(streamVideo.VideoId, out var latest) || !ReferenceEquals(latest, current))
                        return;
                    if (!ReferenceEquals(current.StreamVideo, streamVideo) ||
                        (owner != null && !ReferenceEquals(current, owner)))
                        return;

                    Volatile.Write(ref current.RetryPending, 1);
                    current.Timer.Change(ReminderRetryDelay, Timeout.InfiniteTimeSpan);
                }
                return;
            }

            if (owner != null)
                return;

            var reminder = new ReminderItem
            {
                StreamVideo = streamVideo,
                ChannelType = streamVideo.ChannelType,
                RetryPending = 1,
            };
            var timer = new Timer(TimerCallbackWrapper, reminder, ReminderRetryDelay, Timeout.InfiniteTimeSpan);
            reminder.Timer = timer;
            if (!Reminders.TryAdd(streamVideo.VideoId, reminder))
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                timer.Dispose();
            }
        }

        internal static bool TryTakeReminder(
            ConcurrentDictionary<string, ReminderItem> reminders,
            string videoId,
            TableVideo expectedStreamVideo,
            ReminderItem expectedReminder,
            out ReminderItem reminder)
        {
            reminder = null;
            if (!reminders.TryGetValue(videoId, out var current))
                return false;
            lock (current)
            {
                if (!reminders.TryGetValue(videoId, out var latest) || !ReferenceEquals(latest, current))
                    return false;
                if (expectedStreamVideo != null && !ReferenceEquals(current.StreamVideo, expectedStreamVideo))
                    return false;
                if (expectedReminder != null && !ReferenceEquals(current, expectedReminder))
                    return false;
                if (!reminders.TryRemove(new KeyValuePair<string, ReminderItem>(videoId, current)))
                    return false;
            }

            reminder = current;
            return true;
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
