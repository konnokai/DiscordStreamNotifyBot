namespace DiscordStreamNotifyBot.Tests
{
    public sealed class ProviderTokenEncryptionKeyTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("short")]
        public void MissingOrShortKeyIsRejected(string key)
        {
            Assert.Throws<InvalidOperationException>(() => BotConfig.ValidateProviderTokenEncryptionKey(key));
        }

        [Fact]
        public void ExistingSixtyFourCharacterKeyIsAccepted()
        {
            BotConfig.ValidateProviderTokenEncryptionKey(new string('k', 64));
        }
    }
}
