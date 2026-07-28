namespace DiscordStreamNotifyBot.Tests
{
    public sealed class InteractionCommandLocalizationTests
    {
        [Fact]
        public async Task AllRegisteredCommandsHaveDescriptionsInEverySupportedLocale()
        {
            using InteractionMetadataFixture fixture = await InteractionMetadataFixture.CreateAsync();
            fixture.Handler.ValidateCommandLocalizationResources();
        }
    }
}
