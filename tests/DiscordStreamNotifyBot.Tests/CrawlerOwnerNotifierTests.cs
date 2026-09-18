using Discord;
using DiscordStreamNotifyBot.SharedService.AdminSettings;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class CrawlerOwnerNotifierTests
    {
        [Fact]
        public void ChzzkAddedMessageShowsRecordStateAndToggleButton()
        {
            const string channelId = "4de764d9dad3b25602284be6db3ac647";

            var (embed, components) = CrawlerOwnerNotifier.BuildAddedMessage(
                CrawlerPlatform.Chzzk, channelId, "테스트 채널", channelId, "測試伺服器", "測試執行者");

            Assert.Equal("關閉", embed.Fields.First(x => x.Name == "錄影頻道").Value);

            var buttons = GetButtons(components);
            var button = Assert.Single(buttons);
            Assert.Equal("切換自動錄影", button.Label);
            Assert.Equal($"spider_chzzk:record:{channelId}", button.CustomId);
            Assert.False(button.IsDisabled);
        }

        [Fact]
        public void YoutubeAddedMessageUsesToggleButtonsInsteadOfAddRemove()
        {
            const string channelId = "UC1234567890123456789012";

            var (embed, components) = CrawlerOwnerNotifier.BuildAddedMessage(
                CrawlerPlatform.Youtube, channelId, "測試頻道", channelId, "測試伺服器", "測試執行者");

            Assert.Equal("否", embed.Fields.First(x => x.Name == "認可頻道").Value);
            Assert.Equal("否", embed.Fields.First(x => x.Name == "錄影頻道").Value);

            var buttons = GetButtons(components);
            Assert.Equal(
                ["切換認可頻道", "切換錄影頻道"],
                buttons.Select(x => x.Label).ToArray());
            Assert.Equal(
                [$"spider_youtube:trusted:{channelId}", $"spider_youtube:record:{channelId}"],
                buttons.Select(x => x.CustomId).ToArray());
        }

        private static List<ButtonComponent> GetButtons(MessageComponent components)
            => components.Components
                .SelectMany(row => ((ActionRowComponent)row).Components.OfType<ButtonComponent>())
                .ToList();
    }
}
