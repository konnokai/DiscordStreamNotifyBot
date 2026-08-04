using DiscordStreamNotifyBot.SharedService.Twitch;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class TwitchApiServiceDisabledTests
    {
        [Fact]
        public async Task NetworkMethodsReturnFailureWithoutAccessingUninitializedApi()
        {
            var service = new TwitchApiService(new BotConfig());

            Assert.False(service.IsEnable);
            Assert.Null(await service.GetUserAsync(twitchUserLogin: "channel"));
            Assert.Empty(await service.GetUsersAsync("channel"));
            Assert.Null(await service.GetLatestVODAsync("123"));
            Assert.Empty(await service.GetClipsAsync("123", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow));

            TwitchStreamsResult streams = await service.GetNowStreamsResultAsync("123");
            Assert.False(streams.IsSuccess);
            Assert.Empty(streams.Streams);

            TwitchEventSubSubscriptionsResult subscriptions =
                await service.GetEventSubSubscriptionsResultAsync("123");
            Assert.False(subscriptions.IsSuccess);
            Assert.Empty(subscriptions.Subscriptions);
            Assert.Null(await service.GetEventSubSubscriptionsAsync("123"));

            TwitchEventSubEnsureResult ensure = await service.EnsureEventSubSubscriptionsAsync(
                "123", TwitchEventSubEnsureMode.Fallback);
            Assert.False(ensure.IsSuccess);
            Assert.Equal(TwitchEventSubEnsureMode.Fallback, ensure.Mode);
            Assert.False(await service.CreateEventSubSubscriptionAsync("123"));

            TwitchEventSubDeleteResult deletion = await service.DeleteEventSubSubscriptionResultAsync("123");
            Assert.Equal(TwitchEventSubDeleteStatus.ApiFailure, deletion.Status);
            Assert.False(await service.DeleteEventSubSubscriptionAsync("123"));
        }
    }
}
