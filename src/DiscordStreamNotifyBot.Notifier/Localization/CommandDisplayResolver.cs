using Discord.Interactions;
using System.Resources;

namespace DiscordStreamNotifyBot.Localization
{
    public sealed class CommandDisplayResolver
    {
        private const string BaseResourceName = "DiscordStreamNotifyBot.Localization.Resources.InteractionCommands";
        private readonly ResourceManager _resourceManager = new(BaseResourceName, typeof(CommandDisplayResolver).Assembly);

        public string GetName(string locale, params string[] canonicalPath)
        {
            ArgumentNullException.ThrowIfNull(canonicalPath);
            if (canonicalPath.Length == 0)
                throw new ArgumentException("指令路徑不可空白", nameof(canonicalPath));

            return canonicalPath[^1];
        }

        public string GetDescription(string locale, params string[] canonicalPath)
            => GetResource(locale, canonicalPath, "description");

        public string GetCommandPath(string locale, params string[] canonicalPath)
        {
            ArgumentNullException.ThrowIfNull(canonicalPath);
            if (canonicalPath.Length == 0)
                throw new ArgumentException("指令路徑不可空白", nameof(canonicalPath));

            return "/" + string.Join(' ', canonicalPath);
        }

        public string GetCommandPath(string locale, SlashCommandInfo command)
            => GetCommandPath(locale, GetCanonicalCommandPath(command).ToArray());

        public string GetModuleName(string locale, ModuleInfo module)
            => module.SlashGroupName;

        public string GetModuleDescription(string locale, ModuleInfo module)
            => GetDescription(locale, GetCanonicalModulePath(module).ToArray());

        public string GetCommandName(string locale, SlashCommandInfo command)
            => command.Name;

        public string GetCommandDescription(string locale, SlashCommandInfo command)
            => GetDescription(locale, GetCanonicalCommandPath(command).ToArray());

        public string GetParameterName(string locale, SlashCommandInfo command, SlashCommandParameterInfo parameter)
            => parameter.Name;

        public string GetParameterDescription(string locale, SlashCommandInfo command, SlashCommandParameterInfo parameter)
            => GetDescription(locale, GetCanonicalCommandPath(command).Append(parameter.Name).ToArray());

        public static IReadOnlyList<string> GetCanonicalModulePath(ModuleInfo module)
        {
            var path = new List<string>();
            for (ModuleInfo current = module; current != null; current = current.Parent)
            {
                if (current.IsSlashGroup)
                    path.Insert(0, current.SlashGroupName);
            }

            return path;
        }

        public static IReadOnlyList<string> GetCanonicalCommandPath(SlashCommandInfo command)
        {
            if (command.IgnoreGroupNames)
                return new[] { command.Name };

            return GetCanonicalModulePath(command.Module).Append(command.Name).ToArray();
        }

        private string GetResource(string locale, IEnumerable<string> canonicalPath, string identifier)
        {
            string key = string.Join('.', canonicalPath) + "." + identifier;
            return _resourceManager.GetString(key, SupportedLocale.GetCulture(locale))
                ?? throw new MissingManifestResourceException($"找不到指令顯示資源：{key}（{locale}）");
        }
    }
}
