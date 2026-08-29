using Discord.Commands;

namespace DiscordStreamNotifyBot.Command.Attribute
{
    public class RequireGuildMemberCountAttribute : PreconditionAttribute
    {
        public RequireGuildMemberCountAttribute(uint gCount)
        {
            GuildMemberCount = gCount;
        }

        public uint? GuildMemberCount { get; }
        public override string ErrorMessage { get; set; } = "此伺服器不可使用本指令";

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.Message.Author.Id == Bot.ApplicatonOwner.Id) return Task.FromResult(PreconditionResult.FromSuccess());

            if (Utility.OfficialGuildList.Contains(context.Guild.Id)) return Task.FromResult(PreconditionResult.FromSuccess());

            var memberCount = ((SocketGuild)context.Guild).MemberCount;
            if (memberCount >= GuildMemberCount) return Task.FromResult(PreconditionResult.FromSuccess());
            else return Task.FromResult(PreconditionResult.FromError($"此伺服器不可使用本指令\n" +
                $"指令要求的伺服器人數：`{GuildMemberCount}` 人\n" +
                $"Bot 目前取得的伺服器人數：`{memberCount}` 人\n" +
                $"由於快取的關係，可能會遇到伺服器人數錯誤的問題\n" +
                $"如有需求，請使用 `/server-admin send-message-to-bot-owner` 聯絡 Bot 擁有者。"));
        }
    }
}
