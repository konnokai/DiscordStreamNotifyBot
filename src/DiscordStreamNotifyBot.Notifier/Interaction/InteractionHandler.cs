using Discord.Interactions;
using DiscordStreamNotifyBot.Localization;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;

namespace DiscordStreamNotifyBot.Interaction
{
    class InteractionHandler : IInteractionService
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactions;
        private readonly IServiceProvider _services;
        private readonly BotLocalizer _botLocalizer;
        private readonly CommandDisplayResolver _commandDisplayResolver;
        private readonly GuildLocaleService _guildLocaleService;
        private readonly LocaleResolver _localeResolver;
        private const string CommandResourceName = "DiscordStreamNotifyBot.Localization.Resources.InteractionCommands";
        private const string LocalizationPolicyMarker =
            "localization-policy|canonical-english-names|name-localizations:none|descriptions:zh-TW,en-US,ja";
        private static readonly Regex CanonicalCommandNameRegex = new(
            @"^[a-z0-9_-]{1,32}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private readonly ResourceManager _commandResourceManager = new(CommandResourceName, typeof(InteractionHandler).Assembly);

        /// <summary>
        /// 會被全球註冊的 Slash 指令規格雜湊，包含 localization policy、canonical metadata、choices 與三語 description 資源。
        /// </summary>
        public string CommandSignature => BuildCommandSignature(false);

        /// <summary>
        /// Debug 測試 guild 的實際註冊規格雜湊，另納入 <c>DontAutoRegister</c> 模組的 canonical metadata。
        /// </summary>
        public string DebugCommandSignature => BuildCommandSignature(true);

        /// <summary>供測試與 review 使用的可讀 Slash command registration contract。</summary>
        internal string ReadableCommandContract => BuildReadableCommandContract();

        private string BuildReadableCommandContract()
        {
            var sb = new StringBuilder();
            sb.AppendLine("slash-contract v1");
            AppendReadableCommandSection(sb, "global", command => !command.Module.DontAutoRegister);
            AppendReadableCommandSection(sb, "guild-only", command => command.Module.DontAutoRegister);
            return sb.ToString().ReplaceLineEndings("\n").TrimEnd();
        }

        private void AppendReadableCommandSection(
            StringBuilder sb,
            string sectionName,
            Func<SlashCommandInfo, bool> includeCommand)
        {
            sb.Append('[').Append(sectionName).AppendLine("]");
            var commands = _interactions.SlashCommands
                .Where(includeCommand)
                .OrderBy(command => string.Join(".", GetCommandPath(command)), StringComparer.Ordinal)
                .ToList();

            foreach (var module in commands
                .Where(command => command.Module.IsSlashGroup)
                .Select(command => command.Module)
                .Distinct()
                .OrderBy(module => string.Join(".", GetModulePath(module)), StringComparer.Ordinal))
            {
                sb.Append("group /")
                    .Append(string.Join(" ", GetModulePath(module)))
                    .AppendLine();
            }

            foreach (SlashCommandInfo command in commands)
            {
                sb.Append("command /")
                    .Append(string.Join(" ", GetCommandPath(command)))
                    .Append(" permissions=").Append(command.DefaultMemberPermissions?.ToString() ?? "-")
                    .Append(" dm=").Append(command.IsEnabledInDm.ToString().ToLowerInvariant())
                    .Append(" nsfw=").Append(command.IsNsfw.ToString().ToLowerInvariant())
                    .Append(" contexts=").Append(FormatContractValues(command.ContextTypes))
                    .Append(" integrations=").Append(FormatContractValues(command.IntegrationTypes))
                    .AppendLine();

                for (int index = 0; index < command.Parameters.Count; index++)
                {
                    SlashCommandParameterInfo parameter = command.Parameters[index];
                    sb.Append("  option ").Append(index).Append(' ')
                        .Append(parameter.Name)
                        .Append(" type=").Append(parameter.DiscordOptionType?.ToString() ?? parameter.ParameterType.Name)
                        .Append(" required=").Append(parameter.IsRequired.ToString().ToLowerInvariant())
                        .Append(" autocomplete=").Append(parameter.IsAutocomplete.ToString().ToLowerInvariant())
                        .Append(" channels=").Append(FormatContractValues(parameter.ChannelTypes))
                        .Append(" min=").Append(FormatContractNumber(parameter.MinValue, -9007199254740991D))
                        .Append(" max=").Append(FormatContractNumber(parameter.MaxValue, 9007199254740991D))
                        .Append(" minLength=").Append(FormatContractValue(parameter.MinLength))
                        .Append(" maxLength=").Append(FormatContractValue(parameter.MaxLength))
                        .AppendLine();

                    int choiceIndex = 0;
                    foreach (var choice in GetChoices(parameter))
                    {
                        sb.Append("    choice ").Append(choiceIndex++)
                            .Append(" name=").Append(JsonConvert.SerializeObject(choice.DisplayName))
                            .Append(" value=").Append(JsonConvert.SerializeObject(choice.Value))
                            .AppendLine();
                    }
                }
            }
        }

        private static string FormatContractValues<T>(IEnumerable<T> values)
        {
            if (values == null)
                return "-";

            string result = string.Join(",", values.Select(value => Convert.ToString(value, CultureInfo.InvariantCulture)));
            return result.Length == 0 ? "-" : result;
        }

        private static string FormatContractValue(object value)
            => value == null ? "-" : Convert.ToString(value, CultureInfo.InvariantCulture);

        private static string FormatContractNumber(double? value, double defaultValue)
            => !value.HasValue || value.Value == defaultValue
                ? "-"
                : value.Value.ToString(CultureInfo.InvariantCulture);

        private string BuildCommandSignature(bool includeDontAutoRegister)
        {
            var sb = new StringBuilder();
            sb.Append(LocalizationPolicyMarker).Append('\n');

            var modules = _interactions.SlashCommands
                .Where(cmd => (includeDontAutoRegister || !cmd.Module.DontAutoRegister) && cmd.Module.IsSlashGroup)
                .Select(cmd => cmd.Module)
                .Distinct()
                .OrderBy(module => string.Join(".", GetModulePath(module)), StringComparer.Ordinal)
                .ToList();

            foreach (var module in modules)
            {
                sb.Append("group|")
                    .Append(string.Join(".", GetModulePath(module))).Append('|')
                    .Append(module.Description).Append('\n');
            }

            var commands = _interactions.SlashCommands
                .Where(cmd => includeDontAutoRegister || !cmd.Module.DontAutoRegister)
                .Select(cmd =>
                {
                    string commandPath = string.Join(".", GetCommandPath(cmd));
                    var parameters = cmd.Parameters
                        .Select((p, index) =>
                        {
                            var choices = GetChoices(p)
                                .OrderBy(choice => choice.DisplayName, StringComparer.Ordinal)
                                .ThenBy(choice => choice.Value, StringComparer.Ordinal)
                                .Select(choice => $"{choice.DisplayName}={choice.Value}");
                            return $"{index}:{p.Name}:{p.ParameterType.FullName}:{(p.IsRequired ? "R" : "O")}:{p.Description}:[{string.Join(",", choices)}]";
                        })
                        .ToList();
                    return (Name: commandPath, Description: cmd.Description, Parameters: parameters);
                })
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .ToList();

            foreach (var cmd in commands)
            {
                sb.Append(cmd.Name).Append('|').Append(cmd.Description).Append('|');
                foreach (var p in cmd.Parameters)
                    sb.Append(p).Append(';');
                sb.Append('\n');
            }

            foreach (string locale in SupportedLocale.Values)
            {
                sb.Append("locale|").Append(locale).Append('\n');
                foreach (var resource in LoadCommandResourceValues(locale).OrderBy(x => x.Key, StringComparer.Ordinal))
                    sb.Append(resource.Key).Append('=').Append(resource.Value).Append('\n');
            }

            using var sha = System.Security.Cryptography.SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
        }

        public InteractionHandler(
            IServiceProvider services,
            InteractionService interactions,
            DiscordSocketClient client,
            BotLocalizer botLocalizer,
            CommandDisplayResolver commandDisplayResolver,
            GuildLocaleService guildLocaleService,
            LocaleResolver localeResolver)
        {
            _client = client;
            _interactions = interactions;
            _services = services;
            _botLocalizer = botLocalizer;
            _commandDisplayResolver = commandDisplayResolver;
            _guildLocaleService = guildLocaleService;
            _localeResolver = localeResolver;
        }

        public async Task InitializeAsync()
        {
            await _interactions.AddModulesAsync(
                assembly: Assembly.GetEntryAssembly(),
                services: _services);

            ValidateCommandLocalizationResources();

            _client.InteractionCreated += (slash) => { var _ = Task.Run(() => HandleInteraction(slash)); return Task.CompletedTask; };
            _interactions.SlashCommandExecuted += SlashCommandExecuted;
        }

        internal void ValidateCommandLocalizationResources()
        {
            var commands = _interactions.SlashCommands
                .Where(command => !command.Module.DontAutoRegister)
                .OrderBy(command => string.Join(".", GetCommandPath(command)), StringComparer.Ordinal)
                .ToList();
            var expectedKeys = new HashSet<string>(StringComparer.Ordinal);
            var scopedNames = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            foreach (var module in commands
                .Where(command => command.Module.IsSlashGroup)
                .Select(command => command.Module)
                .Distinct()
                .OrderBy(module => string.Join(".", GetModulePath(module)), StringComparer.Ordinal))
            {
                IList<string> modulePath = GetModulePath(module);
                ValidateCanonicalName(module.SlashGroupName, "group", string.Join(".", modulePath.Take(modulePath.Count - 1)), scopedNames);
                ValidateDescriptionOnlyTarget(modulePath, LocalizationTarget.Group, expectedKeys);
            }

            foreach (SlashCommandInfo command in commands)
            {
                IList<string> commandPath = GetCommandPath(command);
                string parentPath = string.Join(".", commandPath.Take(commandPath.Count - 1));
                ValidateCanonicalName(command.Name, "command", parentPath, scopedNames);
                ValidateDescriptionOnlyTarget(commandPath, LocalizationTarget.Command, expectedKeys);

                foreach (SlashCommandParameterInfo parameter in command.FlattenedParameters)
                {
                    IList<string> parameterPath = commandPath.Concat(new[] { parameter.Name }).ToList();
                    ValidateCanonicalName(parameter.Name, "parameter", string.Join(".", commandPath), scopedNames);
                    ValidateDescriptionOnlyTarget(parameterPath, LocalizationTarget.Parameter, expectedKeys);

                    var choiceNames = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var choice in GetChoices(parameter).OrderBy(choice => choice.DisplayName, StringComparer.Ordinal))
                    {
                        IList<string> choicePath = parameterPath.Concat(new[] { choice.DisplayName }).ToList();
                        var names = _interactions.LocalizationManager.GetAllNames(choicePath, LocalizationTarget.Choice);
                        if (names.Count != 0)
                            throw new InvalidOperationException($"Slash 選項不得提供名稱本地化：{string.Join('.', choicePath)}");

                        ValidateCanonicalChoiceName(choice.DisplayName, string.Join(".", parameterPath));
                        if (!choiceNames.Add(choice.DisplayName))
                            throw new InvalidOperationException($"Slash 選項固定名稱重複：{choice.DisplayName}（{string.Join('.', parameterPath)}）");
                    }
                }
            }

            foreach (string locale in SupportedLocale.Values)
            {
                var actualKeys = LoadCommandResourceValues(locale).Keys.ToHashSet(StringComparer.Ordinal);
                if (!expectedKeys.SetEquals(actualKeys))
                {
                    string missing = string.Join(", ", expectedKeys.Except(actualKeys, StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal));
                    string extra = string.Join(", ", actualKeys.Except(expectedKeys, StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal));
                    throw new InvalidOperationException($"InteractionCommands.{locale} 資源索引鍵不符合指令中繼資料。缺少：[{missing}]；多出：[{extra}]");
                }
            }
        }

        private void ValidateDescriptionOnlyTarget(
            IList<string> path,
            LocalizationTarget target,
            ISet<string> expectedKeys)
        {
            string descriptionKey = GetResourceKey(path, "description");
            expectedKeys.Add(descriptionKey);

            var names = _interactions.LocalizationManager.GetAllNames(path, target);
            if (names.Count != 0)
                throw new InvalidOperationException($"Slash {target} 不得提供名稱本地化：{string.Join('.', path)}");

            var descriptions = _interactions.LocalizationManager.GetAllDescriptions(path, target);
            foreach (string locale in SupportedLocale.Values)
            {
                if (!descriptions.TryGetValue(locale, out string localizedDescription))
                    throw new InvalidOperationException($"缺少 Slash 說明本地化資源：{descriptionKey}（{locale}）");

                if (localizedDescription.Length is < 1 or > 100)
                    throw new InvalidOperationException($"Slash 說明長度必須為 1 至 100 個字元：{descriptionKey}（{locale}）");
            }
        }

        private static void ValidateCanonicalName(
            string name,
            string target,
            string scope,
            IDictionary<string, HashSet<string>> scopedNames)
        {
            if (!CanonicalCommandNameRegex.IsMatch(name))
                throw new InvalidOperationException($"Slash {target} 固定名稱格式無效：{name}");

            string scopeKey = target == "parameter" ? $"parameter|{scope}" : $"command|{scope}";
            if (!scopedNames.TryGetValue(scopeKey, out HashSet<string> names))
            {
                names = new HashSet<string>(StringComparer.Ordinal);
                scopedNames[scopeKey] = names;
            }

            if (!names.Add(name))
                throw new InvalidOperationException($"Slash {target} 固定名稱重複：{name}（{scope}）");
        }

        private Dictionary<string, string> LoadCommandResourceValues(string locale)
        {
            CultureInfo culture = locale == SupportedLocale.TraditionalChinese
                ? CultureInfo.InvariantCulture
                : CultureInfo.GetCultureInfo(locale);
            ResourceSet resourceSet = _commandResourceManager.GetResourceSet(culture, true, false)
                ?? throw new MissingManifestResourceException($"找不到 InteractionCommands 資源：{locale}");

            return resourceSet.Cast<DictionaryEntry>()
                .ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value, StringComparer.Ordinal);
        }

        private static IList<string> GetModulePath(ModuleInfo module)
        {
            var path = new List<string>();
            for (ModuleInfo current = module; current != null; current = current.Parent)
            {
                if (current.IsSlashGroup)
                    path.Insert(0, current.SlashGroupName);
            }
            return path;
        }

        private static IList<string> GetCommandPath(SlashCommandInfo command)
        {
            if (command.IgnoreGroupNames)
                return new List<string> { command.Name };

            IList<string> path = GetModulePath(command.Module);
            path.Add(command.Name);
            return path;
        }

        private static IEnumerable<(string DisplayName, string Value)> GetChoices(SlashCommandParameterInfo parameter)
        {
            foreach (ParameterChoice choice in parameter.Choices)
                yield return (choice.Name, Convert.ToString(choice.Value, CultureInfo.InvariantCulture));

            Type enumType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
            if (!enumType.IsEnum)
                yield break;

            foreach (FieldInfo member in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (member.IsDefined(typeof(HideAttribute), true))
                    continue;

                string displayName = member.GetCustomAttribute<ChoiceDisplayAttribute>()?.Name ?? member.Name;
                yield return (displayName, Convert.ToString(member.GetRawConstantValue(), CultureInfo.InvariantCulture));
            }
        }

        private static string GetResourceKey(IEnumerable<string> path, string identifier)
            => string.Join(".", path) + "." + identifier;

        private static void ValidateCanonicalChoiceName(string name, string parameterPath)
        {
            if (name.Length is < 1 or > 100 || name.Any(character => character is < ' ' or > '~'))
                throw new InvalidOperationException($"Slash 選項固定名稱必須為 1 至 100 個可列印 ASCII 字元：{name}（{parameterPath}）");
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            try
            {
                var ctx = new SocketInteractionContext(_client, arg);
                await _interactions.ExecuteCommandAsync(ctx, _services);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Demystify(), $"處理互動時發生未攔截例外：{arg.Type} / {arg.User?.Id}");
                try
                {
                    string locale = await ResolveResponseLocaleAsync(arg, true);
                    await arg.SendErrorAsync(_botLocalizer, locale, "Errors.Unknown", arg.HasResponded, true);
                }
                catch (Exception responseException)
                {
                    Log.Error(responseException.Demystify(), "回覆 Interaction 未知錯誤時失敗");
                }
            }
        }

        private async Task SlashCommandExecuted(SlashCommandInfo arg1, IInteractionContext arg2, IResult arg3)
        {
            string slashCommand = $"/{arg1}";
            var commandData = arg2.Interaction.Data as SocketSlashCommandData;
            if (commandData?.Options.Count > 0) slashCommand += GetOptionsValue(commandData.Options.First());

            string location = arg2.Guild == null
                ? $"私人訊息/{arg2.Channel?.Name ?? "未知頻道"}"
                : $"{arg2.Guild.Name}/{arg2.Channel?.Name ?? "未知頻道"}";

            if (arg3.IsSuccess)
            {
                Log.Info($"[{location}] {arg2.User.Username} 執行 `{slashCommand}`");
            }
            else
            {
                Log.Error($"[{location}] {arg2.User.Username} 執行 `{slashCommand}` 發生錯誤\r\n{arg3.ErrorReason}");
                string locale = await ResolveResponseLocaleAsync(arg2.Interaction, true);
                string contactPath = arg3.Error == InteractionCommandError.UnmetPrecondition
                    ? _commandDisplayResolver.GetCommandPath(locale, "utility", "send-message-to-bot-owner")
                    : null;
                InteractionErrorDescriptor error = InteractionErrorPolicy.Resolve(
                    arg3.Error, arg3.ErrorReason, contactPath);
                await arg2.Interaction.SendErrorAsync(
                    _botLocalizer, locale, error.ResourceKey, false, true, error.Arguments);
            }
        }

        private async Task<string> ResolveResponseLocaleAsync(IDiscordInteraction interaction, bool isPrivate)
        {
            string guildLocale = null;
            if (interaction.GuildId is ulong guildId)
                guildLocale = await _guildLocaleService.GetAsync(guildId, _client.GetGuild(guildId));

            return isPrivate
                ? _localeResolver.ResolvePrivate(interaction.UserLocale, guildLocale, interaction.GuildLocale)
                : _localeResolver.ResolvePublic(guildLocale, interaction.GuildLocale);
        }

        private string GetOptionsValue(SocketSlashCommandDataOption socketSlashCommandDataOption)
        {
            try
            {
                if (socketSlashCommandDataOption.Type != ApplicationCommandOptionType.SubCommand && socketSlashCommandDataOption.Type != ApplicationCommandOptionType.SubCommandGroup && !socketSlashCommandDataOption.Options.Any())
                    return $" {socketSlashCommandDataOption.Value}";

                if (socketSlashCommandDataOption.Type == ApplicationCommandOptionType.SubCommand || socketSlashCommandDataOption.Type == ApplicationCommandOptionType.SubCommandGroup) GetOptionsValue(socketSlashCommandDataOption.Options.First());
                return " " + string.Join(' ', socketSlashCommandDataOption.Options.Select(option => option.Value));
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
