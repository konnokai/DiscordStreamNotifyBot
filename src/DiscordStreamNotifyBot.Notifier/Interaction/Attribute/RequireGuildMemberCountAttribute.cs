using Discord.Interactions;

namespace DiscordStreamNotifyBot.Interaction.Attribute
{
    public class RequireGuildMemberCountAttribute : PreconditionAttribute
    {
        public RequireGuildMemberCountAttribute(uint gCount)
        {
            GuildMemberCount = gCount;
        }

        public uint? GuildMemberCount { get; }
        public override string ErrorMessage { get; } = InteractionErrorCodes.GuildUnavailable;

        public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            if (context.Interaction.User.Id == Bot.ApplicatonOwner.Id) return Task.FromResult(PreconditionResult.FromSuccess());

            if (context.Guild == null)
                return Task.FromResult(PreconditionResult.FromError(InteractionErrorCodes.GuildOnly));

            if (DiscordStreamNotifyBot.Utility.OfficialGuildList.Contains(context.Guild.Id)) return Task.FromResult(PreconditionResult.FromSuccess());

            var memberCount = ((SocketGuild)context.Guild).MemberCount;
            if (memberCount >= GuildMemberCount) return Task.FromResult(PreconditionResult.FromSuccess());
            return Task.FromResult(PreconditionResult.FromError(
                InteractionErrorCodes.GuildMemberCount(GuildMemberCount.Value, memberCount)));
        }
    }
}