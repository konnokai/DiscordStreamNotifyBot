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

                // dueTime 由 PlanStart 限制在 0 ~ 14 天，不會超出 Timer 範圍
                remT.Change(dueTime, Timeout.InfiniteTimeSpan);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"StartReminder: {streamVideo.VideoTitle} - {streamVideo.ScheduledStartTime}");
                throw;
            }
        }

        /// <summary>Timer 的 state 一律是 <see cref="ReminderItem"/>；<see cref="ReminderTimerActionAsync"/> 內部已攔截例外。</summary>
        private void TimerCallbackWrapper(object state)
        {
            var owner = (ReminderItem)state;
            _ = ReminderTimerActionAsync(owner.StreamVideo, owner);
        }

        /// <summary>
        /// 執行到點提醒；回傳 false 代表通知發布失敗或處理中發生例外，呼叫端可據此保留補償狀態。
        /// </summary>
        private async Task<bool> ReminderTimerActionAsync(TableVideo streamVideo, ReminderItem owner = null)
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
                            return await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.Delete).ConfigureAwait(false);

                        return true;
                    }

                    ScheduleReminderRetry(streamVideo, owner);
                    return true;
                }

                if (!TryGetStartTime(videoResult, out DateTime startTime))
                {
                    Log.Error($"無法解析影片開始時間：{streamVideo.VideoId}");
                    ScheduleReminderRetry(streamVideo, owner);
                    return true;
                }

                if (!TryClaimReminderAction(streamVideo, owner))
                    return true;

                if (YoutubeReminderPolicy.DecideApiRecheck(startTime, DateTime.Now) ==
                    YoutubeReminderApiAction.TreatAsStarted)
                {
                    return await HandleStreamStartAsync(streamVideo, videoResult, db);
                }

                return await HandleStreamTimeChangedAsync(streamVideo, videoResult, db, startTime);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"ReminderAction: {streamVideo.VideoId}");
                return false;
            }
        }

        private async Task<(YTApiVideo Video, bool IsDeleted)> TryGetVideoResult(TableVideo streamVideo)
        {
            try
            {
                var videoResult = await GetVideoAsync(streamVideo.VideoId);
                if (videoResult == null)
                {
                    Log.Info($"{streamVideo.VideoId} 待機室已刪除");
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

        private async Task<bool> HandleStreamStartAsync(
            TableVideo streamVideo,
            YTApiVideo videoResult,
            MainDbContext db)
        {
            bool isRecord = false;
            streamVideo.VideoTitle = videoResult.Snippet.Title;
            var video = GetDbVideoByType(db, streamVideo);
            try
            {
                SaveStreamVideoChange(db, video, streamVideo, (x) => x.VideoTitle = streamVideo.VideoTitle, "直播標題");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"({streamVideo.ChannelType}) 直播標題變更儲存失敗：{streamVideo.VideoId}");
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
                            Log.Info($"已發送 YouTube 錄影請求：{streamVideo.VideoId}");
                            isRecord = true;
                        }
                        else
                        {
                            Log.Warn($"Redis Sub 頻道不存在，請開啟錄影工具：{streamVideo.VideoId}");
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

            if (isRecord)
                return true;

            return await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.Start).ConfigureAwait(false);
        }

        private async Task<bool> HandleStreamTimeChangedAsync(
            TableVideo streamVideo,
            YTApiVideo videoResult,
            MainDbContext db,
            DateTime startTime)
        {
            var previousScheduledStartTime = streamVideo.ScheduledStartTime;
            Log.Info($"直播時間已變更 {streamVideo.ChannelTitle} - {streamVideo.VideoTitle}：{previousScheduledStartTime:O} -> {startTime:O}");

            streamVideo.ScheduledStartTime = startTime;
            var video = GetDbVideoByType(db, streamVideo);
            try
            {
                SaveStreamVideoChange(db, video, streamVideo, (x) => x.ScheduledStartTime = streamVideo.ScheduledStartTime, "直播時間");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"({streamVideo.ChannelType}) 直播時間變更儲存失敗：{streamVideo.VideoId}");
            }

            bool published = await PublishYoutubeNotificationAsync(streamVideo, YoutubeNoticeType.ChangeTime,
                previousScheduledStartTime: previousScheduledStartTime).ConfigureAwait(false);

            StartReminder(streamVideo, streamVideo.ChannelType);
            return published;
        }

        /// <summary>
        /// 沒有 owner（排程直接呼叫）時，只有尚無提醒才可執行；有 owner 時必須能原子取走自己那筆提醒，
        /// 過期的 callback 不能搶走較新的替換提醒。
        /// </summary>
        private bool TryClaimReminderAction(TableVideo streamVideo, ReminderItem owner)
            => owner == null
                ? !Reminders.ContainsKey(streamVideo.VideoId)
                : RemoveReminder(streamVideo.VideoId, streamVideo, owner);

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

        /// <summary>
        /// 把影片變更寫回資料庫；還沒寫入資料庫的影片改替換 <see cref="addNewStreamVideo"/> 裡的那筆，兩邊都找不到才記錄錯誤。
        /// </summary>
        /// <param name="persistedVideo">呼叫端以 <see cref="GetDbVideoByType"/> 取得的資料庫資料，可為 null。</param>
        /// <param name="applyChange">要套用到資料庫資料上的變更。</param>
        /// <param name="changeName">log 用的變更名稱，例如「直播標題」。</param>
        private static void SaveStreamVideoChange(MainDbContext db, TableVideo persistedVideo, TableVideo streamVideo,
            Action<TableVideo> applyChange, string changeName)
        {
            if (persistedVideo != null)
            {
                applyChange(persistedVideo);
                db.UpdateAndSave(persistedVideo);
            }
            else if (addNewStreamVideo.ContainsKey(streamVideo.VideoId))
            {
                addNewStreamVideo[streamVideo.VideoId] = streamVideo;
            }
            else
            {
                Log.Error($"({streamVideo.ChannelType}) {changeName}變更儲存失敗，找不到資料：{streamVideo.VideoId}");
            }
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
                    Log.Warn($"YouTube GetVideoDurationAsync ({videoId}) 失敗，將於 {timeSpan.TotalSeconds} 秒後重試（第 {retryAttempt} 次重試）");
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
            // API 例外一律在這裡轉成結果，不需要重試
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
                Log.Error(ex, $"GetCommentThreadsIsDisabledAsync: {videoId} 未知錯誤");
                return true;
            }
        }
    }
}
