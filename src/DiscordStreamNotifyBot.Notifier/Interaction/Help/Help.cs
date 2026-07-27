using Discord.Interactions;
using DiscordStreamNotifyBot.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordStreamNotifyBot.Interaction.Help
{
    [Group("help", "說明")]
    public class Help : TopLevelModule<Service.HelpService>
    {
        private readonly InteractionService _interaction;
        private readonly IServiceProvider _services;

        public Help(InteractionService interaction, IServiceProvider services)
        {
            _interaction = interaction;
            _services = services;
        }

        public class HelpGetModulesAutocompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context,
                IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                try
                {
                    string locale = await autocompleteInteraction.ResolveLocaleAsync(services, true);
                    var displayResolver = services.GetRequiredService<CommandDisplayResolver>();
                    string input = autocompleteInteraction.Data.Current.Value?.ToString();
                    var commands = await GetAvailableCommandsAsync(context, services);
                    var candidates = commands
                        .Select(command => command.Module)
                        .Where(module => CommandDisplayResolver.GetCanonicalModulePath(module).Count > 0)
                        .Select(module => new
                        {
                            Name = displayResolver.GetModuleName(locale, module),
                            Description = displayResolver.GetModuleDescription(locale, module),
                            CanonicalPath = string.Join('.', CommandDisplayResolver.GetCanonicalModulePath(module)),
                        })
                        .Select(module => new AutocompleteCandidate(
                            $"{module.Description} ({module.Name})", module.CanonicalPath,
                            module.Name, module.Description));
                    var results = AutocompleteSearch.Filter(candidates, input)
                        .Select(item => new AutocompleteResult(item.Name, item.Value));
                    return AutocompletionResult.FromSuccess(results);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), "產生 Help 模組 autocomplete 時失敗");
                    return AutocompletionResult.FromSuccess();
                }
            }
        }

        public class HelpGetCommandsAutocompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context,
                IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                try
                {
                    string locale = await autocompleteInteraction.ResolveLocaleAsync(services, true);
                    var displayResolver = services.GetRequiredService<CommandDisplayResolver>();
                    string input = autocompleteInteraction.Data.Current.Value?.ToString();
                    var commands = await GetAvailableCommandsAsync(context, services);
                    var candidates = commands
                        .Select(command => new
                        {
                            Command = command,
                            CanonicalPath = string.Join('.', CommandDisplayResolver.GetCanonicalCommandPath(command)),
                            DisplayPath = displayResolver.GetCommandPath(locale, command),
                            Description = displayResolver.GetCommandDescription(locale, command)
                        })
                        .Select(item => new AutocompleteCandidate(
                            $"{item.DisplayPath} - {item.Description}", item.CanonicalPath,
                            item.DisplayPath, item.Description));
                    var results = AutocompleteSearch.Filter(candidates, input)
                        .Select(item => new AutocompleteResult(item.Name, item.Value));
                    return AutocompletionResult.FromSuccess(results);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Demystify(), "產生 Help 指令 autocomplete 時失敗");
                    return AutocompletionResult.FromSuccess();
                }
            }
        }

        [SlashCommand("get-all-modules", "顯示全部模組")]
        public async Task Modules()
        {
            string locale = await GetLocaleAsync(false);
            var commands = await GetAvailableCommandsAsync(Context, _services);
            var modules = commands
                .Select(command => command.Module)
                .Where(module => CommandDisplayResolver.GetCanonicalModulePath(module).Count > 0)
                .GroupBy(module => string.Join('.', CommandDisplayResolver.GetCanonicalModulePath(module)))
                .Select(group => group.First())
                .OrderBy(module => CommandDisplayResolver.GetModuleName(locale, module), StringComparer.Ordinal)
                .Select(module => "- " + CommandDisplayResolver.GetModuleName(locale, module));
            string commandsPath = CommandDisplayResolver.GetCommandPath(locale, "help", "get-all-commands");

            await RespondAsync(embed: new EmbedBuilder()
                .WithOkColor()
                .WithTitle(BotLocalizer.Get("Help.Modules.Title", locale))
                .WithDescription(string.Join('\n', modules))
                .WithFooter(BotLocalizer.Format("Help.Modules.Footer", locale, commandsPath))
                .Build());
        }

        [SlashCommand("get-all-commands", "顯示模組內包含的指令")]
        public async Task Commands(
            [Summary("module", "模組名稱"), Autocomplete(typeof(HelpGetModulesAutocompleteHandler))] string module)
        {
            string locale = await GetLocaleAsync(false);
            module = module?.Trim();
            if (string.IsNullOrWhiteSpace(module))
            {
                await SendLocalizedErrorAsync("Help.Errors.ModuleRequired");
                return;
            }

            var available = await GetAvailableCommandsAsync(Context, _services);
            var commands = available
                .Where(command => string.Join('.', CommandDisplayResolver.GetCanonicalModulePath(command.Module))
                    .Equals(module, StringComparison.OrdinalIgnoreCase))
                .OrderBy(command => CommandDisplayResolver.GetCommandPath(locale, command), StringComparer.Ordinal)
                .Distinct(new CommandTextEqualityComparer())
                .ToList();
            if (commands.Count == 0)
            {
                await SendLocalizedErrorAsync("Help.Errors.ModuleNotFound", false, true, module);
                return;
            }

            string detailPath = CommandDisplayResolver.GetCommandPath(locale, "help", "get-command-help");
            var embed = new EmbedBuilder()
                .WithOkColor()
                .WithTitle(BotLocalizer.Format("Help.Commands.Title", locale,
                    CommandDisplayResolver.GetModuleName(locale, commands[0].Module)))
                .WithDescription(string.Join('\n', commands.Select(command => $"**`{CommandDisplayResolver.GetCommandPath(locale, command)}`**")))
                .WithFooter(BotLocalizer.Format("Help.Commands.Footer", locale, detailPath));
            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("get-command-help", "顯示指令的詳細說明")]
        public async Task CommandHelp(
            [Summary("module", "模組名稱"), Autocomplete(typeof(HelpGetModulesAutocompleteHandler))] string module = "",
            [Summary("command", "指令名稱"), Autocomplete(typeof(HelpGetCommandsAutocompleteHandler))] string command = "")
        {
            string locale = await GetLocaleAsync(false);
            if (string.IsNullOrWhiteSpace(module) && string.IsNullOrWhiteSpace(command))
            {
                string modulesPath = CommandDisplayResolver.GetCommandPath(locale, "help", "get-all-modules");
                string nowStreamingPath = CommandDisplayResolver.GetCommandPath(locale, "youtube", "now-streaming");
                string recordPath = CommandDisplayResolver.GetCommandPath(locale, "youtube", "list-record-channel");
                string bannerPath = CommandDisplayResolver.GetCommandPath(locale, "youtube", "set-banner-change");
                string bannerHelpPath = CommandDisplayResolver.GetCommandPath(locale, "help", "get-command-help");
                var embed = new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle(BotLocalizer.Format("Help.Overview.Title", locale, Program.Version))
                    .WithDescription(BotLocalizer.Format("Help.Overview.Description", locale,
                        nowStreamingPath, recordPath, bannerPath, bannerHelpPath,
                        Format.Url("ECPay", DiscordStreamNotifyBot.Utility.ECPayUrl),
                        Format.Url("PayPal", DiscordStreamNotifyBot.Utility.PaypalUrl)))
                    .WithFooter(BotLocalizer.Format("Help.Overview.Footer", locale, modulesPath));
                await RespondAsync(embed: embed.Build());
                return;
            }

            var available = await GetAvailableCommandsAsync(Context, _services);
            string canonicalPath = command?.Trim() ?? "";
            if (!canonicalPath.Contains('.') && !string.IsNullOrWhiteSpace(module))
                canonicalPath = module.Trim() + "." + canonicalPath;

            SlashCommandInfo commandInfo = available.FirstOrDefault(item =>
                string.Join('.', CommandDisplayResolver.GetCanonicalCommandPath(item))
                    .Equals(canonicalPath, StringComparison.OrdinalIgnoreCase));
            if (commandInfo == null)
            {
                await SendLocalizedErrorAsync("Help.Errors.CommandNotFound", false, true, command);
                return;
            }

            await RespondAsync(embed: _service.GetCommandHelp(commandInfo, locale).Build());
        }

        private static async Task<IReadOnlyList<SlashCommandInfo>> GetAvailableCommandsAsync(
            IInteractionContext context, IServiceProvider services)
        {
            var interactionService = services.GetRequiredService<InteractionService>();
            var checks = await Task.WhenAll(interactionService.SlashCommands.Select(async command =>
                (Command: command, Result: await command.CheckPreconditionsAsync(context, services).ConfigureAwait(false))));
            return checks.Where(item => item.Result.IsSuccess).Select(item => item.Command).ToList();
        }
    }

    public class CommandTextEqualityComparer : IEqualityComparer<SlashCommandInfo>
    {
        public bool Equals(SlashCommandInfo x, SlashCommandInfo y)
            => string.Join('.', CommandDisplayResolver.GetCanonicalCommandPath(x)) ==
               string.Join('.', CommandDisplayResolver.GetCanonicalCommandPath(y));

        public int GetHashCode(SlashCommandInfo obj)
            => string.Join('.', CommandDisplayResolver.GetCanonicalCommandPath(obj)).GetHashCode(StringComparison.Ordinal);
    }
}
