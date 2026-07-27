using Discord.Interactions;

namespace DiscordStreamNotifyBot.Interaction.Attribute
{
    public class RequireGuildOwnerAttribute : PreconditionAttribute
    {
        public RequireGuildOwnerAttribute()
        {
        }

        public override string ErrorMessage { get; } = InteractionErrorCodes.GuildOwnerOnly;

        public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            if (context.Interaction.User.Id == Bot.ApplicatonOwner.Id) return Task.FromResult(PreconditionResult.FromSuccess());

            if (context.Guild == null)
                return Task.FromResult(PreconditionResult.FromError(InteractionErrorCodes.GuildOnly));

            if (context.Interaction.User.Id == context.Guild.OwnerId) return Task.FromResult(PreconditionResult.FromSuccess());
            return Task.FromResult(PreconditionResult.FromError(InteractionErrorCodes.GuildOwnerOnly));
        }
    }
}