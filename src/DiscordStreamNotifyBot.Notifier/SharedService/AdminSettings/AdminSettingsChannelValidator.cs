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
                return Reject("settings.channel-not-found", channelId);

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

        private static AdminSettingsMutationResult Reject(string code, ulong channelId)
            => AdminSettingsMutationResult.Rejected(code, new JObject
            {
                ["channelId"] = channelId.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });
    }
}
