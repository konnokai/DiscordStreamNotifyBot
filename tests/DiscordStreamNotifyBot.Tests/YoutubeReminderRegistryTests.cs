using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Scraper.Detection.Youtube;
using System.Collections.Concurrent;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class YoutubeReminderRegistryTests
    {
        [Fact]
        public void StaleCallbackCannotTakeNewerReplacement()
        {
            var video = new Video { VideoId = "video-id" };
            using var oldTimer = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);
            using var replacementTimer = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);
            var oldReminder = new ReminderItem { StreamVideo = video, Timer = oldTimer };
            var replacement = new ReminderItem { StreamVideo = video, Timer = replacementTimer };
            var reminders = new ConcurrentDictionary<string, ReminderItem>();
            reminders["video-id"] = replacement;

            bool removed = YoutubeDetectionService.TryTakeReminder(
                reminders,
                "video-id",
                video,
                oldReminder,
                out var taken);

            Assert.False(removed);
            Assert.Null(taken);
            Assert.Same(replacement, reminders["video-id"]);
        }

        [Fact]
        public void OwnerCanAtomicallyTakeCurrentReminder()
        {
            var video = new Video { VideoId = "video-id" };
            using var timer = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);
            var reminder = new ReminderItem { StreamVideo = video, Timer = timer };
            var reminders = new ConcurrentDictionary<string, ReminderItem>();
            reminders["video-id"] = reminder;

            bool removed = YoutubeDetectionService.TryTakeReminder(
                reminders,
                "video-id",
                video,
                reminder,
                out var taken);

            Assert.True(removed);
            Assert.Same(reminder, taken);
            Assert.Empty(reminders);
        }

        [Fact]
        public void StaleCallbackCannotClaimActionBeforeSideEffects()
        {
            var video = new Video { VideoId = "video-id" };
            using var staleTimer = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);
            using var replacementTimer = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);
            var stale = new ReminderItem { StreamVideo = video, Timer = staleTimer };
            var replacement = new ReminderItem { StreamVideo = video, Timer = replacementTimer };
            var reminders = new ConcurrentDictionary<string, ReminderItem>();
            reminders[video.VideoId] = replacement;

            bool claimed = YoutubeDetectionService.TryClaimReminderAction(
                reminders,
                video.VideoId,
                video,
                stale,
                out var taken);

            Assert.False(claimed);
            Assert.Null(taken);
            Assert.Same(replacement, reminders[video.VideoId]);
        }
    }
}
