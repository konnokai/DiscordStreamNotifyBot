using Discord.Interactions;

namespace DiscordStreamNotifyBot.Interaction
{
    internal sealed record InteractionErrorDescriptor(string ResourceKey, object[] Arguments);

    internal static class InteractionErrorPolicy
    {
        internal static InteractionErrorDescriptor Resolve(
            InteractionCommandError? error,
            string errorReason,
            string contactCommandPath)
        {
            if (error == InteractionCommandError.UnmetPrecondition)
                return ResolvePrecondition(errorReason, contactCommandPath);

            return error switch
            {
                InteractionCommandError.UnknownCommand => Create("Errors.UnknownCommand"),
                InteractionCommandError.BadArgs => Create("Errors.InvalidArguments"),
                InteractionCommandError.Exception when errorReason?.Contains("50001", StringComparison.Ordinal) == true
                    => Create("Permissions.BotMissingRequired"),
                _ => Create("Errors.Unknown"),
            };
        }

        private static InteractionErrorDescriptor ResolvePrecondition(string errorCode, string contactCommandPath)
        {
            switch (errorCode)
            {
                case InteractionErrorCodes.GuildOnly:
                    return Create("Preconditions.GuildOnly");
                case InteractionErrorCodes.GuildUnavailable:
                    return Create("Preconditions.GuildUnavailable");
                case InteractionErrorCodes.GuildOwnerOnly:
                    return Create("Preconditions.GuildOwnerOnly");
            }

            if (errorCode?.StartsWith(InteractionErrorCodes.GuildMemberCountPrefix, StringComparison.Ordinal) == true)
            {
                string[] values = errorCode[InteractionErrorCodes.GuildMemberCountPrefix.Length..].Split(':');
                if (values.Length == 2 && uint.TryParse(values[0], out uint required) && int.TryParse(values[1], out int actual))
                    return Create("Preconditions.GuildMemberCount", required, actual, contactCommandPath);
            }

            return Create("Preconditions.Unmet");
        }

        private static InteractionErrorDescriptor Create(string resourceKey, params object[] arguments)
            => new(resourceKey, arguments);
    }
}
