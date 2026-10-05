using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Shared.Messages;
using Newtonsoft.Json.Linq;

#nullable enable

namespace DiscordStreamNotifyBot.SharedService.AdminSettings
{
    internal static class AdminSettingsChannelValidator
    {
        public static AdminSettingsMutationResult? Validate(
            DiscordSocketClient client,
            SocketGuild guild,
            ulong channelId,
            bool requireManageEvents = false)
        {
            var channel = guild.GetChannel(channelId);
            if (channel?.ChannelType is not (ChannelType.Text or ChannelType.News))
                return AdminSettingsMutationResult.Rejected("settings.channel-not-found", new JObject
                {
                    ["channelId"] = channelId.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });

            var botUser = guild.GetUser(client.CurrentUser.Id);
            if (botUser == null)
                return AdminSettingsMutationResult.Rejected("settings.bot-unavailable");

            var permissions = botUser.GetPermissions(channel);
            var missing = new JArray();
            if (!permissions.ViewChannel)
                missing.Add("viewChannel");
            if (!permissions.SendMessages)
                missing.Add("sendMessages");
            if (!permissions.EmbedLinks)
                missing.Add("embedLinks");
            if (requireManageEvents && !botUser.GuildPermissions.ManageEvents)
                missing.Add("manageEvents");

            return missing.Count == 0
                ? null
                : AdminSettingsMutationResult.Rejected("settings.channel-missing-permissions", new JObject
                {
                    ["channelId"] = channelId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["permissions"] = missing
                });
        }

        /// <summary>會員/訂閱驗證設定前必須已設定且仍存在的驗證紀錄頻道。</summary>
        public static AdminSettingsMutationResult? ValidateVerificationLogChannel(SocketGuild guild, GuildConfig? guildConfig)
        {
            if (guildConfig?.VerificationLogChannelId is not > 0)
                return AdminSettingsMutationResult.Rejected("verification.log-channel-required");
            if (guild.GetTextChannel(guildConfig.VerificationLogChannelId) == null)
                return AdminSettingsMutationResult.Rejected("verification.log-channel-missing");
            return null;
        }
    }
}
