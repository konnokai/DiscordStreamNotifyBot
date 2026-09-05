using DiscordStreamNotifyBot.Scraper.Detection.Twitch;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class TwitchChannelUpdateDecisionTests
    {
        private static readonly DateTime StartedAt =
            new(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void EqualTitleAndCategoryAreIgnoredButIdentityIsRefreshed()
        {
            var decision = TwitchChannelUpdatePolicy.Decide(
                State("title", "game"),
                Event("title", "game", StartedAt.AddMinutes(1)));

            Assert.Equal(TwitchChannelUpdateAction.RefreshState, decision.Action);
            Assert.Null(decision.Change);
            Assert.Equal("new_login", decision.NextState.UserLogin);
            Assert.Equal("New Name", decision.NextState.UserName);
        }

        [Fact]
        public void CompletelyEqualUpdateIsIgnored()
        {
            var current = State("title", "game");
            var decision = TwitchChannelUpdatePolicy.Decide(
                current,
                new TwitchChannelEventFacts(
                    "title", "game", current.UserLogin, current.UserName, StartedAt.AddMinutes(1)));

            Assert.Equal(TwitchChannelUpdateAction.Ignore, decision.Action);
            Assert.Null(decision.Change);
        }

        [Fact]
        public void DiffIncludesOnlyChangedFieldsAndClampsNegativeElapsedTime()
        {
            var title = TwitchChannelUpdatePolicy.Decide(
                State("old title", "game"),
                Event("new title", "game", StartedAt.AddSeconds(-1)));
            var category = TwitchChannelUpdatePolicy.Decide(
                State("title", "old game"),
                Event("title", "new game", StartedAt.AddSeconds(90)));

            Assert.Equal(TwitchChannelUpdateAction.Queue, title.Action);
            Assert.Equal(0, title.Change.ElapsedSeconds);
            Assert.Equal("old title", title.Change.OldTitle);
            Assert.Equal("new title", title.Change.NewTitle);
            Assert.Null(title.Change.NewCategory);

            Assert.Equal(90, category.Change.ElapsedSeconds);
            Assert.Null(category.Change.NewTitle);
            Assert.Equal("old game", category.Change.OldCategory);
            Assert.Equal("new game", category.Change.NewCategory);
        }

        [Fact]
        public void BatchFactoryPreservesOrderAndFiltersNoOps()
        {
            TwitchChannelUpdateChange[] changes =
            [
                new(65, "old", "new", null, null),
                new(66, null, null, null, null),
                new(67, null, null, "", "game")
            ];

            var batch = TwitchChannelUpdatePolicy.CreateBatch(changes.Select(x => x.ToDto()));

            Assert.Equal(2, batch.Count);
            Assert.Equal("new", batch[0].NewTitle);
            Assert.Equal("game", batch[1].NewCategory);
        }

        private static TwitchChannelStateFacts State(string title, string category) => new(
            title,
            category,
            "old_login",
            "Old Name",
            StartedAt);

        private static TwitchChannelEventFacts Event(string title, string category, DateTime observedAt) => new(
            title,
            category,
            "new_login",
            "New Name",
            observedAt);
    }
}
