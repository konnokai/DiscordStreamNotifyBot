using DiscordStreamNotifyBot.SharedService.YoutubeMember;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class YoutubeMemberSelectMenuTests
    {
        [Theory]
        [InlineData("member:check:123:456", true)]
        [InlineData("twitch-member-check:123:456", false)]
        [InlineData("spider_youtube:trusted:channel", false)]
        [InlineData(null, false)]
        public void OnlyYoutubeMemberSelectMenusAreHandled(string customId, bool expected)
        {
            Assert.Equal(expected, YoutubeMemberService.IsYoutubeMemberSelectMenu(customId));
        }
    }
}
