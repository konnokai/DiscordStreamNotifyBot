using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Scraper.Detection.Twitch;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class TwitchChannelUpdateDecisionTests
    {
        private static readonly DateTime StartedAt =
            new(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void EqualTitleAndCategoryProduceNoUpdate()
        {
            var update = TwitchDetectionService.CreateChannelUpdate(
                State("title", "game"), "title", "game", StartedAt.AddMinutes(1));

            Assert.Null(update);
        }

        [Fact]
        public void DiffIncludesOnlyChangedFieldsAndClampsNegativeElapsedTime()
        {
            var title = TwitchDetectionService.CreateChannelUpdate(
                State("old title", "game"), "new title", "game", StartedAt.AddSeconds(-1));
            var category = TwitchDetectionService.CreateChannelUpdate(
                State("title", "old game"), "title", "new game", StartedAt.AddSeconds(90));

            Assert.Equal(0, title.ElapsedSeconds);
            Assert.Equal("old title", title.OldTitle);
            Assert.Equal("new title", title.NewTitle);
            Assert.Null(title.OldCategory);
            Assert.Null(title.NewCategory);

            Assert.Equal(90, category.ElapsedSeconds);
            Assert.Null(category.OldTitle);
            Assert.Null(category.NewTitle);
            Assert.Equal("old game", category.OldCategory);
            Assert.Equal("new game", category.NewCategory);
        }

        private static TwitchStream State(string title, string category) => new()
        {
            StreamTitle = title,
            GameName = category,
            UserLogin = "old_login",
            UserName = "Old Name",
            StreamStartAt = StartedAt
        };
    }
}
