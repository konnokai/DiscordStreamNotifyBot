using Discord.Interactions;
using System.Reflection;

#if DEBUG || DEBUG_DONTREGISTERCOMMAND
using System.Text.RegularExpressions;
#endif

namespace DiscordStreamNotifyBot.Interaction
{
    class InteractionHandler : IInteractionService
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactions;
        private readonly IServiceProvider _services;

        public int CommandCount => _interactions.ComponentCommands.Count + _interactions.ContextCommands.Count + _interactions.SlashCommands.Count;

        /// <summary>
        /// 會被全球註冊的 Slash 指令「規格」雜湊（指令名稱、描述，以及各參數的位置/名稱/型別/是否必填/描述）。
        /// <para>只要規格有任何變動，雜湊即改變，供啟動時判斷是否需要重新註冊全球指令（取代僅比對指令總數的舊做法）。</para>
        /// </summary>
        public string CommandSignature
        {
            get
            {
                var sb = new System.Text.StringBuilder();

                var commands = _interactions.SlashCommands
                    .Where((cmd) => !cmd.Module.DontAutoRegister) // 只納入會被全球註冊的指令（排除伺服器專屬指令）
                    .Select((cmd) =>
                    {
                        string groupPrefix = string.IsNullOrEmpty(cmd.Module.SlashGroupName) ? "" : cmd.Module.SlashGroupName + " ";
                        // 參數維持宣告順序（即 Discord 上的位置），逐一記錄位置/名稱/型別/是否必填/描述
                        var parameters = cmd.Parameters
                            .Select((p, index) => $"{index}:{p.Name}:{p.ParameterType.Name}:{(p.IsRequired ? "R" : "O")}:{p.Description}")
                            .ToList();
                        return (Name: groupPrefix + cmd.Name, Description: cmd.Description, Parameters: parameters);
                    })
                    .OrderBy((x) => x.Name, StringComparer.Ordinal) // 指令間排序以求穩定；參數內部維持位置順序
                    .ToList();

                foreach (var cmd in commands)
                {
                    sb.Append(cmd.Name).Append('|').Append(cmd.Description).Append('|');
                    foreach (var p in cmd.Parameters)
                        sb.Append(p).Append(';');
                    sb.Append('\n');
                }

                using var sha = System.Security.Cryptography.SHA256.Create();
                return Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(sb.ToString())));
            }
        }

        public InteractionHandler(IServiceProvider services, InteractionService interactions, DiscordSocketClient client)
        {
            _client = client;
            _interactions = interactions;
            _services = services;
        }

        public async Task InitializeAsync()
        {
            await _interactions.AddModulesAsync(
                assembly: Assembly.GetEntryAssembly(),
                services: _services);

            #region 檢查指令是否符合 Discord 的 Regex 規範
#if DEBUG || DEBUG_DONTREGISTERCOMMAND
            bool isError = false;
            Regex regex = new Regex(@"^[\w-]{1,32}$");
            var list = _interactions.Modules.Select(module => module.SlashCommands.Select((x) => new KeyValuePair<string, List<string>>(x.Name, x.Parameters.Select((x2) => x2.Name).ToList())));
            foreach (var item in list)
            {
                foreach (var item2 in item)
                {
                    if (!regex.IsMatch(item2.Key))
                    {
                        isError = true;
                        Log.Error(item2.Key);
                        continue;
                    }

                    foreach (var item3 in item2.Value)
                    {
                        if (!regex.IsMatch(item3))
                        {
                            isError = true;
                            Log.Error($"{item2.Key}: {item3}");
                        }
                    }
                }
            }
            if (isError) Environment.Exit(3);
#endif
            #endregion

            _client.InteractionCreated += (slash) => { var _ = Task.Run(() => HandleInteraction(slash)); return Task.CompletedTask; };
            _interactions.SlashCommandExecuted += SlashCommandExecuted;
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
                Console.WriteLine(ex);

                // If a Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (arg.Type == InteractionType.ApplicationCommand)
                    await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }

        private async Task SlashCommandExecuted(SlashCommandInfo arg1, IInteractionContext arg2, IResult arg3)
        {
            string slashCommand = $"/{arg1}";
            var commandData = arg2.Interaction.Data as SocketSlashCommandData;
            if (commandData.Options.Count > 0) slashCommand += GetOptionsValue(commandData.Options.First());

            if (arg3.IsSuccess)
            {
                Log.Info($"[{arg2.Guild.Name}/{arg2.Channel.Name}] {arg2.User.Username} 執行 `{slashCommand}`");
            }
            else
            {
                Log.Error($"[{arg2.Guild.Name}/{arg2.Channel.Name}] {arg2.User.Username} 執行 `{slashCommand}` 發生錯誤\r\n{arg3.ErrorReason}");
                switch (arg3.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        await arg2.Interaction.SendErrorAsync(arg3.ErrorReason);
                        break;
                    case InteractionCommandError.UnknownCommand:
                        await arg2.Interaction.SendErrorAsync("未知的指令，也許被移除或變更了");
                        break;
                    case InteractionCommandError.BadArgs:
                        await arg2.Interaction.SendErrorAsync("輸入的參數錯誤");
                        break;
                    case InteractionCommandError.Exception when arg3.ErrorReason.Contains("50001"):
                        await arg2.Interaction.SendErrorAsync($"我在 `{arg2.Channel}` 沒有 `讀取 & 編輯頻道 & 嵌入連結` 的權限，請給予權限後再次執行本指令");
                        break;
                    default:
                        await arg2.Interaction.SendErrorAsync("未知的錯誤，請向 Bot 擁有者回報");
                        break;
                }
            }
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
