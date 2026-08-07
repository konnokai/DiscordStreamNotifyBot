using Discord.Interactions;
using DiscordStreamNotifyBot.DataBase;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Localization;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService.Member;
using DiscordStreamNotifyBot.SharedService.YoutubeMember;

namespace DiscordStreamNotifyBot.Interaction.YoutubeMember
{
    /// <summary>僅處理新版 YouTube 會限選單；Twitch 與其他 component 不會被承認。</summary>
    public sealed class YoutubeMemberComponent : TopLevelModule
    {
        private readonly MainDbService _dbService;
        private readonly MemberOperationCoordinator _operationCoordinator;
        private readonly YoutubeMemberRoleService _roleService;

        public YoutubeMemberComponent(
            MainDbService dbService,
            MemberOperationCoordinator operationCoordinator,
            YoutubeMemberRoleService roleService)
        {
            _dbService = dbService;
            _operationCoordinator = operationCoordinator;
            _roleService = roleService;
        }

        public static bool IsYoutubeMemberSelectionCustomId(string customId)
            => YoutubeMemberPolicies.TryParseSelectionRoute(customId, out _, out _);

        [ComponentInteraction("youtube-member-check:*:*", true)]
        public async Task HandleSelectionAsync(string guildValue, string userValue, string[] selectedChannelIds)
        {
            var component = (SocketMessageComponent)Context.Interaction;
            string locale = await GetLocaleAsync(true);
            if (!YoutubeMemberPolicies.TryParseSelectionRoute(component.Data.CustomId, out ulong guildId, out ulong userId) ||
                guildValue != guildId.ToString() || userValue != userId.ToString() ||
                Context.Guild?.Id != guildId || Context.User.Id != userId)
            {
                await component.SendErrorAsync(BotLocalizer, locale, "Components.NotAllowed", false, true);
                return;
            }

            if (!YoutubeMemberPolicies.IsValidSelection(selectedChannelIds))
            {
                await component.SendErrorAsync(BotLocalizer, locale, "Components.Invalid", false, true);
                return;
            }

            await component.DeferAsync(true);
            await using var userLock = await _operationCoordinator.LockUserAsync(userId, GracefulShutdown.Token);
            await using var guildLock = await _operationCoordinator.LockGuildAsync(guildId, GracefulShutdown.Token);
            using var db = _dbService.GetDbContext();

            string[] selection = selectedChannelIds.ToArray();
            var activeConfigs = await db.GuildYoutubeMemberConfig.AsNoTracking()
                .Where(x => x.GuildId == guildId && !x.DeletionPending && selection.Contains(x.MemberCheckChannelId))
                .ToDictionaryAsync(x => x.MemberCheckChannelId);
            if (activeConfigs.Count != selection.Length ||
                activeConfigs.Values.Any(config => !YoutubeMemberPolicies.IsActiveConfiguration(config)))
            {
                await component.SendErrorAsync(BotLocalizer, locale, "Components.Invalid", true, true);
                return;
            }

            var existingChecks = await db.YoutubeMemberCheck
                .Where(x => x.GuildId == guildId && x.UserId == userId)
                .ToListAsync();
            IReadOnlyList<YoutubeMemberSelectionTransition> transitions =
                YoutubeMemberPolicies.BuildSelectionTransition(existingChecks, selection);
            foreach (YoutubeMemberSelectionTransition transition in transitions.Where(x => x.AddQueuedCheck))
            {
                db.YoutubeMemberCheck.Add(new YoutubeMemberCheck
                {
                    GuildId = guildId,
                    UserId = userId,
                    CheckYTChannelId = transition.ChannelId,
                    Locale = SupportedLocale.Normalize(component.UserLocale),
                    IsChecked = false,
                    PendingRoleRemoval = false
                });
            }

            foreach (YoutubeMemberSelectionTransition transition in transitions.Where(x => x.RequeueExistingCheck))
            {
                YoutubeMemberCheck check = existingChecks.Single(x => x.CheckYTChannelId == transition.ChannelId);
                YoutubeMemberPolicies.QueueVerification(check);
                check.Locale = SupportedLocale.Normalize(component.UserLocale);
            }

            foreach (YoutubeMemberSelectionTransition transition in transitions.Where(x => x.MarkRoleRemovalPending))
            {
                YoutubeMemberCheck check = existingChecks.Single(x => x.CheckYTChannelId == transition.ChannelId);
                YoutubeMemberPolicies.QueueRoleRemoval(check);
            }

            // 先將所有取消項目的 durable intent 寫入，再碰 Discord；失敗會保留待清理列。
            await db.SaveChangesAsync(GracefulShutdown.Token);
            foreach (YoutubeMemberSelectionTransition transition in transitions.Where(x => x.MarkRoleRemovalPending))
            {
                YoutubeMemberCheck check = existingChecks.Single(x => x.CheckYTChannelId == transition.ChannelId);
                GuildYoutubeMemberConfig config = await db.GuildYoutubeMemberConfig.AsNoTracking().SingleOrDefaultAsync(
                    x => x.GuildId == guildId && x.MemberCheckChannelId == transition.ChannelId,
                    GracefulShutdown.Token);
                if (config != null && await _roleService.RemoveAsync(config, userId, GracefulShutdown.Token))
                    db.YoutubeMemberCheck.Remove(check);
            }
            await db.SaveChangesAsync(GracefulShutdown.Token);

            await component.SendConfirmAsync(BotLocalizer, locale, "Member.CheckQueuedWithDmNotice", true, true, 5);
        }

        [ComponentInteraction("member:check:*:*", true)]
        public async Task HandleLegacySelectionAsync(string _, string __)
        {
            string locale = await GetLocaleAsync(true);
            await Context.Interaction.SendErrorAsync(BotLocalizer, locale, "Member.Select.Expired", false, true);
        }
    }
}
