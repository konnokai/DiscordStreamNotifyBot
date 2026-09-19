using Discord;
using Discord.Net;
using System.Net;

namespace DiscordStreamNotifyBot.Tests
{
    /// <summary>
    /// 永久失敗分類測試：權限與目標已不存在不該留在匯流排重試，暫時性錯誤仍要重試。
    /// </summary>
    public sealed class NotificationTargetFailureTests
    {
        [Theory]
        [InlineData(DiscordErrorCode.MissingPermissions)]
        [InlineData(DiscordErrorCode.InsufficientPermissions)]
        [InlineData(DiscordErrorCode.UnknownChannel)]
        [InlineData(DiscordErrorCode.UnknownGuild)]
        public void PermissionAndMissingTargetCodesArePermanent(DiscordErrorCode code)
            => Assert.True(NotificationTargetFailure.IsPermanent(CreateHttpException(code)));

        [Fact]
        public void OtherDiscordCodesAreNotPermanent()
            => Assert.False(NotificationTargetFailure.IsPermanent(CreateHttpException(DiscordErrorCode.MessageAlreadyCrossposted)));

        [Fact]
        public void TransientFailuresAreNotPermanent()
        {
            Assert.False(NotificationTargetFailure.IsPermanent(new HttpException(HttpStatusCode.InternalServerError, null, null, "Discord 5xx", null)));
            Assert.False(NotificationTargetFailure.IsPermanent(new TimeoutException()));
            Assert.False(NotificationTargetFailure.IsPermanent(new AggregateException(new TimeoutException())));
        }

        [Fact]
        public void OnlyUnknownGuildUsesGuildScopeCleanup()
        {
            Assert.True(NotificationTargetFailure.IsUnknownGuild(CreateHttpException(DiscordErrorCode.UnknownGuild)));
            Assert.False(NotificationTargetFailure.IsUnknownGuild(CreateHttpException(DiscordErrorCode.MissingPermissions)));
        }

        private static HttpException CreateHttpException(DiscordErrorCode code)
            => new(HttpStatusCode.Forbidden, null, code, "測試用", null);
    }
}
