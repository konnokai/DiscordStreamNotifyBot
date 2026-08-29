using Discord.Interactions;
using DiscordStreamNotifyBot.Interaction;

namespace DiscordStreamNotifyBot.Tests
{
    public sealed class InteractionErrorPolicyTests
    {
        [Theory]
        [InlineData(InteractionCommandError.UnknownCommand, null, "Errors.UnknownCommand")]
        [InlineData(InteractionCommandError.BadArgs, null, "Errors.InvalidArguments")]
        [InlineData(InteractionCommandError.Exception, "Discord API error 50001", "Permissions.BotMissingRequired")]
        [InlineData(InteractionCommandError.Exception, "Discord API error 50013", "Errors.Unknown")]
        [InlineData(null, null, "Errors.Unknown")]
        public void CommandErrorMapsToExpectedResource(
            InteractionCommandError? error,
            string errorReason,
            string expectedResourceKey)
        {
            InteractionErrorDescriptor descriptor = InteractionErrorPolicy.Resolve(error, errorReason, null);

            Assert.Equal(expectedResourceKey, descriptor.ResourceKey);
            Assert.Empty(descriptor.Arguments);
        }

        [Theory]
        [InlineData(InteractionErrorCodes.GuildOnly, "Preconditions.GuildOnly")]
        [InlineData(InteractionErrorCodes.GuildUnavailable, "Preconditions.GuildUnavailable")]
        [InlineData(InteractionErrorCodes.GuildOwnerOnly, "Preconditions.GuildOwnerOnly")]
        public void KnownPreconditionMapsToExpectedResource(string errorCode, string expectedResourceKey)
        {
            InteractionErrorDescriptor descriptor = InteractionErrorPolicy.Resolve(
                InteractionCommandError.UnmetPrecondition, errorCode, "/server-admin send-message-to-bot-owner");

            Assert.Equal(expectedResourceKey, descriptor.ResourceKey);
            Assert.Empty(descriptor.Arguments);
        }

        [Fact]
        public void GuildMemberCountPreconditionMapsValuesAndContactPath()
        {
            const string contactPath = "/server-admin send-message-to-bot-owner";
            string errorCode = InteractionErrorCodes.GuildMemberCount(100, 42);

            InteractionErrorDescriptor descriptor = InteractionErrorPolicy.Resolve(
                InteractionCommandError.UnmetPrecondition, errorCode, contactPath);

            Assert.Equal("Preconditions.GuildMemberCount", descriptor.ResourceKey);
            Assert.Collection(descriptor.Arguments,
                argument => Assert.Equal((uint)100, argument),
                argument => Assert.Equal(42, argument),
                argument => Assert.Equal(contactPath, argument));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("other-precondition")]
        [InlineData(InteractionErrorCodes.GuildMemberCountPrefix)]
        [InlineData(InteractionErrorCodes.GuildMemberCountPrefix + "abc:42")]
        [InlineData(InteractionErrorCodes.GuildMemberCountPrefix + "100:abc")]
        [InlineData(InteractionErrorCodes.GuildMemberCountPrefix + "100:42:1")]
        public void UnknownOrMalformedPreconditionUsesFallback(string errorCode)
        {
            InteractionErrorDescriptor descriptor = InteractionErrorPolicy.Resolve(
                InteractionCommandError.UnmetPrecondition, errorCode, "/server-admin send-message-to-bot-owner");

            Assert.Equal("Preconditions.Unmet", descriptor.ResourceKey);
            Assert.Empty(descriptor.Arguments);
        }
    }
}
