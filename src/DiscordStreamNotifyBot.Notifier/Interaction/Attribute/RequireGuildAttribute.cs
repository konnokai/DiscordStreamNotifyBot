using Discord.Interactions;

namespace DiscordStreamNotifyBot.Interaction.Attribute
{
    public class RequireGuildAttribute : PreconditionAttribute
    {
        public RequireGuildAttribute(ulong gId)
        {
            GuildId = gId;
        }

        public ulong? GuildId { get; }

        public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            if (context.Guild?.Id == GuildId) return Task.FromResult(PreconditionResult.FromSuccess());
            return Task.FromResult(PreconditionResult.FromError(
                context.Guild == null ? InteractionErrorCodes.GuildOnly : InteractionErrorCodes.GuildUnavailable));
        }
    }
}