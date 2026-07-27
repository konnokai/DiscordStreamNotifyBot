using Discord.Interactions;
using DiscordStreamNotifyBot.Interaction.Attribute;
using DiscordStreamNotifyBot.Localization;

namespace DiscordStreamNotifyBot.Interaction.Help.Service
{
    public class HelpService : IInteractionService
    {
        private readonly BotLocalizer _localizer;
        private readonly CommandDisplayResolver _displayResolver;

        public HelpService(BotLocalizer localizer, CommandDisplayResolver displayResolver)
        {
            _localizer = localizer;
            _displayResolver = displayResolver;
        }

        public EmbedBuilder GetCommandHelp(SlashCommandInfo command, string locale)
        {
            string commandPath = _displayResolver.GetCommandPath(locale, command);
            string detailKey = "Help.CommandDetail." + string.Join('.', CommandDisplayResolver.GetCanonicalCommandPath(command));
            string description = _localizer.TryGet(detailKey, locale, out string detail)
                ? detail
                : _displayResolver.GetCommandDescription(locale, command);
            var embed = new EmbedBuilder()
                .WithOkColor()
                .WithTitle(commandPath)
                .WithDescription(description);

            if (command.Parameters.Count > 0)
            {
                string parameters = string.Join('\n', command.Parameters.Select(parameter =>
                    $"`{_displayResolver.GetParameterName(locale, command, parameter)}` - {_displayResolver.GetParameterDescription(locale, command, parameter)}"));
                embed.AddField(_localizer.Get("Help.Command.Parameters", locale), parameters);
            }

            string[] userRequirements = GetCommandRequirements(command, locale);
            if (userRequirements.Length > 0)
                embed.AddField(_localizer.Get("Help.Command.UserPermissions", locale), string.Join('\n', userRequirements));

            string[] botRequirements = GetBotCommandRequirements(command, locale);
            if (botRequirements.Length > 0)
                embed.AddField(_localizer.Get("Help.Command.BotPermissions", locale), string.Join('\n', botRequirements));

            string examples = GetCommandExampleString(command, locale);
            if (!string.IsNullOrEmpty(examples))
                embed.AddField(_localizer.Get("Help.Command.Examples", locale), examples);

            embed.WithFooter(_localizer.Format("Help.Command.ModuleFooter", locale,
                _displayResolver.GetModuleName(locale, command.Module)));
            return embed;
        }

        private string[] GetCommandRequirements(SlashCommandInfo command, string locale)
            => command.Preconditions
                .Where(attribute => attribute is RequireOwnerAttribute || attribute is RequireUserPermissionAttribute)
                .SelectMany(attribute => attribute is RequireOwnerAttribute
                    ? new[] { _localizer.Get("Permissions.BotOwnerOnly", locale) }
                    : GetPermissionNames((RequireUserPermissionAttribute)attribute, locale, "Permissions.UserRequirement"))
                .ToArray();

        private string[] GetBotCommandRequirements(SlashCommandInfo command, string locale)
            => command.Preconditions
                .OfType<RequireBotPermissionAttribute>()
                .SelectMany(attribute => GetPermissionNames(attribute, locale, "Permissions.BotRequirement"))
                .ToArray();

        private IEnumerable<string> GetPermissionNames(RequireUserPermissionAttribute attribute, string locale, string templateKey)
        {
            if (attribute.GuildPermission is GuildPermission guildPermissions)
            {
                foreach (GuildPermission permission in Enum.GetValues<GuildPermission>())
                {
                    ulong value = Convert.ToUInt64(permission);
                    if (value != 0 && (value & (value - 1)) == 0 && guildPermissions.HasFlag(permission))
                        yield return _localizer.Format(templateKey, locale, _localizer.Get($"Permissions.Name.{permission}", locale));
                }
            }
            else if (attribute.ChannelPermission is ChannelPermission channelPermission)
            {
                yield return _localizer.Format(templateKey, locale,
                    _localizer.Get($"Permissions.Name.{channelPermission}", locale));
            }
        }

        private IEnumerable<string> GetPermissionNames(RequireBotPermissionAttribute attribute, string locale, string templateKey)
        {
            if (attribute.GuildPermission is GuildPermission guildPermissions)
            {
                foreach (GuildPermission permission in Enum.GetValues<GuildPermission>())
                {
                    ulong value = Convert.ToUInt64(permission);
                    if (value != 0 && (value & (value - 1)) == 0 && guildPermissions.HasFlag(permission))
                        yield return _localizer.Format(templateKey, locale, _localizer.Get($"Permissions.Name.{permission}", locale));
                }
            }
            else if (attribute.ChannelPermission is ChannelPermission channelPermission)
            {
                yield return _localizer.Format(templateKey, locale,
                    _localizer.Get($"Permissions.Name.{channelPermission}", locale));
            }
        }

        private string GetCommandExampleString(SlashCommandInfo command, string locale)
        {
            var attribute = command.Attributes.OfType<CommandExampleAttribute>().FirstOrDefault();
            if (attribute == null)
                return "";

            string commandPath = _displayResolver.GetCommandPath(locale, command);
            return string.Join('\n', attribute.ExpArray.Select(example => $"`{commandPath} {example}`"));
        }
    }
}
