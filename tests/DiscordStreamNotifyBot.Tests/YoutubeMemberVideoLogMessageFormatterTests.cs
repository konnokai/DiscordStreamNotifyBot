using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.Shared.Messages;
using DiscordStreamNotifyBot.SharedService.YoutubeMember;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class YoutubeMemberVideoLogMessageFormatterTests
    {
        private readonly BotLocalizer _localizer = new();
        private readonly CommandDisplayResolver _commandDisplayResolver = new();

        [Theory]
        [InlineData("zh-TW", "目前找不到 UC123 可用於驗證且已開放留言的會員限定影片。請等頻道主發布符合條件的新影片後，再進行會員驗證。你可以使用 /youtube get-member-only-playlist 查看播放清單。")]
        [InlineData("en-US", "UC123 currently has no members-only video with comments enabled that can be used for verification. Wait for the channel owner to publish an eligible video, then try membership verification again. Use /youtube get-member-only-playlist to inspect the playlist.")]
        [InlineData("ja", "UC123 には現在、コメントが有効で認証に使用できるメンバー限定動画がありません。条件を満たす新しい動画が公開されてから、メンバーシップ認証を再度お試しください。再生リストは /youtube get-member-only-playlist で確認できます。")]
        public void NoVideosUsesLocalizedMessageAndCanonicalCommandPath(string locale, string expected)
        {
            var notification = Create("NoVideos", "UC123");

            Assert.Equal(expected, Format(notification, locale));
        }

        [Theory]
        [InlineData("zh-TW", "已為會員頻道 `UC123` 選用新的驗證影片：`video-id`。")]
        [InlineData("en-US", "A new verification video was selected for membership channel `UC123`: `video-id`.")]
        [InlineData("ja", "メンバーシップチャンネル `UC123` の新しい認証用動画として `video-id` を選択しました。")]
        public void NewProbeVideoUsesLocalizedMessage(string locale, string expected)
        {
            var notification = Create("NewProbeVideo", "UC123", "video-id");

            Assert.Equal(expected, Format(notification, locale));
        }

        [Theory]
        [InlineData("zh-TW", "舊名稱", "會員頻道名稱已變更：`舊名稱` → `新名稱`。")]
        [InlineData("en-US", "Old title", "The membership channel name changed: `Old title` → `新名稱`.")]
        [InlineData("ja", "旧名", "メンバーシップチャンネル名が変更されました：`旧名` → `新名稱`。")]
        [InlineData("zh-TW", "", "會員頻道名稱已變更：`無` → `新名稱`。")]
        [InlineData("en-US", null, "The membership channel name changed: `None` → `新名稱`.")]
        [InlineData("ja", "", "メンバーシップチャンネル名が変更されました：`なし` → `新名稱`。")]
        public void ChannelTitleChangedUsesLocalizedMessageAndNoneFallback(
            string locale, string oldTitle, string expected)
        {
            var notification = Create("ChannelTitleChanged", oldTitle, "新名稱");

            Assert.Equal(expected, Format(notification, locale));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("Unknown", null)]
        [InlineData("novideos", null)]
        [InlineData("NoVideos", null)]
        [InlineData("NewProbeVideo", "only-one")]
        [InlineData("ChannelTitleChanged", "only-one")]
        public void MissingUnknownOrMalformedCodeUsesLegacyMessage(string messageCode, string argument)
        {
            var notification = new YoutubeMemberVideoLogNotification
            {
                Message = "legacy message",
                MessageCode = messageCode,
                MessageArguments = argument == null ? null : new[] { argument },
            };

            Assert.Equal("legacy message", Format(notification, "en-US"));
        }

        [Fact]
        public void UnsupportedLocaleFallsBackToTraditionalChinese()
        {
            var notification = Create("NewProbeVideo", "UC123", "video-id");

            Assert.Equal("已為會員頻道 `UC123` 選用新的驗證影片：`video-id`。", Format(notification, "fr"));
        }

        private string Format(YoutubeMemberVideoLogNotification notification, string locale)
            => YoutubeMemberVideoLogMessageFormatter.Format(
                notification, locale, _localizer, _commandDisplayResolver);

        private static YoutubeMemberVideoLogNotification Create(string code, params string[] arguments)
            => new()
            {
                Message = "legacy message",
                MessageCode = code,
                MessageArguments = arguments,
            };
    }
}
