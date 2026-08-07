using DiscordStreamNotifyBot.DataBase;

namespace DiscordStreamNotifyBot.SharedService.Member
{
    public enum MemberEntitlementProvider
    {
        Youtube,
        Twitch
    }

    public readonly record struct MemberRoleEntitlement(
        MemberEntitlementProvider Provider,
        string ConfigurationKey,
        ulong UserId,
        ulong RoleId);

    /// <summary>跨平台 role ownership 的純判斷，讓 Discord role 物件不必出現在測試中。</summary>
    internal static class MemberRoleOwnershipPolicy
    {
        public static bool HasRoleReference(ulong roleId, IEnumerable<ulong> references)
            => roleId != 0 && references.Contains(roleId);

        public static bool HasOtherActiveEntitlement(
            IEnumerable<MemberRoleEntitlement> entitlements,
            ulong discordUserId,
            ulong roleId,
            MemberEntitlementProvider? excludedProvider = null,
            string excludedConfigurationKey = null)
            => roleId != 0 && entitlements.Any(x =>
                x.UserId == discordUserId &&
                x.RoleId == roleId &&
                (x.Provider != excludedProvider || x.ConfigurationKey != excludedConfigurationKey));

        public static bool CanDeleteTwitchTierRole(ulong roleId, IEnumerable<ulong> youtubeRoleReferences)
            => !HasRoleReference(roleId, youtubeRoleReferences);
    }

    /// <summary>
    /// 同一 guild 在同一把 guild lock 內使用的 entitlement 快照。
    /// 對帳工作必須重用此快照，避免每位 Discord 成員各自查一次資料庫而造成 N+1 與跨平台誤刪。
    /// </summary>
    public sealed class MemberRoleOwnershipSnapshot
    {
        internal MemberRoleOwnershipSnapshot(
            IReadOnlyCollection<MemberRoleEntitlement> entitlements,
            IReadOnlyCollection<ulong> youtubeRoleReferences,
            IReadOnlyCollection<ulong> twitchRoleReferences)
        {
            Entitlements = entitlements;
            YoutubeRoleReferences = youtubeRoleReferences;
            TwitchRoleReferences = twitchRoleReferences;
        }

        public IReadOnlyCollection<MemberRoleEntitlement> Entitlements { get; }
        public IReadOnlyCollection<ulong> YoutubeRoleReferences { get; }
        public IReadOnlyCollection<ulong> TwitchRoleReferences { get; }

        public bool HasOtherActiveEntitlement(
            ulong discordUserId,
            ulong roleId,
            MemberEntitlementProvider? excludedProvider = null,
            string excludedConfigurationKey = null)
            => MemberRoleOwnershipPolicy.HasOtherActiveEntitlement(
                Entitlements,
                discordUserId,
                roleId,
                excludedProvider,
                excludedConfigurationKey);

        public bool CanDeleteTwitchTierRole(ulong roleId)
            => MemberRoleOwnershipPolicy.CanDeleteTwitchTierRole(roleId, YoutubeRoleReferences);
    }

    /// <summary>集中查詢兩平台同 guild 的 role reference 與已驗證 entitlement，避免 legacy collision 互相移除角色。</summary>
    public sealed class MemberRoleOwnershipService
    {
        private readonly MainDbService _dbService;

        public MemberRoleOwnershipService(MainDbService dbService)
        {
            _dbService = dbService;
        }

        public async Task<bool> IsRoleReferencedByYoutubeConfigurationAsync(
            ulong guildId,
            ulong roleId,
            CancellationToken cancellationToken)
        {
            ulong[] references = await LoadYoutubeConfigurationRoleReferencesAsync(guildId, cancellationToken);
            return MemberRoleOwnershipPolicy.HasRoleReference(roleId, references);
        }

        public async Task<bool> IsRoleReferencedByTwitchConfigurationAsync(
            ulong guildId,
            ulong roleId,
            CancellationToken cancellationToken)
        {
            ulong[] references = await LoadTwitchConfigurationRoleReferencesAsync(guildId, cancellationToken);
            return MemberRoleOwnershipPolicy.HasRoleReference(roleId, references);
        }

        public async Task<MemberRoleOwnershipSnapshot> LoadSnapshotAsync(
            ulong guildId,
            CancellationToken cancellationToken)
        {
            // 三個查詢刻意各自使用短生命週期 context；呼叫端必須先取得 guild lock，
            // 因而 snapshot 與隨後的 Discord mutation 不會和本 Notifier 的設定操作交錯。
            Task<ulong[]> youtubeReferences = LoadYoutubeConfigurationRoleReferencesAsync(guildId, cancellationToken);
            Task<ulong[]> twitchReferences = LoadTwitchConfigurationRoleReferencesAsync(guildId, cancellationToken);
            Task<MemberRoleEntitlement[]> entitlements = LoadActiveEntitlementsAsync(guildId, cancellationToken);
            await Task.WhenAll(youtubeReferences, twitchReferences, entitlements);
            return new MemberRoleOwnershipSnapshot(
                entitlements.Result,
                youtubeReferences.Result,
                twitchReferences.Result);
        }

        private async Task<ulong[]> LoadYoutubeConfigurationRoleReferencesAsync(
            ulong guildId,
            CancellationToken cancellationToken)
        {
            using var db = _dbService.GetDbContext();
            var configs = await db.GuildYoutubeMemberConfig.AsNoTracking()
                .Where(x => x.GuildId == guildId)
                .Select(x => new { x.MemberCheckGrantRoleId, x.PreviousMemberCheckGrantRoleId })
                .ToArrayAsync(cancellationToken);
            return configs
                .SelectMany(x => new[] { x.MemberCheckGrantRoleId, x.PreviousMemberCheckGrantRoleId ?? 0 })
                .Where(x => x != 0)
                .Distinct()
                .ToArray();
        }

        private async Task<ulong[]> LoadTwitchConfigurationRoleReferencesAsync(
            ulong guildId,
            CancellationToken cancellationToken)
        {
            using var db = _dbService.GetDbContext();
            var configs = await db.GuildTwitchSubscriptionConfig.AsNoTracking()
                .Where(x => x.GuildId == guildId)
                .Select(x => new
                {
                    x.SubscriberRoleId,
                    x.PreviousSubscriberRoleId,
                    x.Tier1RoleId,
                    x.Tier2RoleId,
                    x.Tier3RoleId
                })
                .ToArrayAsync(cancellationToken);
            return configs
                .SelectMany(x => new[]
                {
                    x.SubscriberRoleId,
                    x.PreviousSubscriberRoleId ?? 0,
                    x.Tier1RoleId,
                    x.Tier2RoleId,
                    x.Tier3RoleId
                })
                .Where(x => x != 0)
                .Distinct()
                .ToArray();
        }

        private async Task<MemberRoleEntitlement[]> LoadActiveEntitlementsAsync(
            ulong guildId,
            CancellationToken cancellationToken)
        {
            using var db = _dbService.GetDbContext();
            var youtube = await (from check in db.YoutubeMemberCheck.AsNoTracking()
                                 join config in db.GuildYoutubeMemberConfig.AsNoTracking()
                                     on new { check.GuildId, ChannelId = check.CheckYTChannelId }
                                     equals new { config.GuildId, ChannelId = config.MemberCheckChannelId }
                                 where check.GuildId == guildId && check.IsChecked &&
                                     !check.PendingRoleRemoval && !config.DeletionPending
                                 select new
                                 {
                                     check.UserId,
                                     config.MemberCheckChannelId,
                                     config.MemberCheckGrantRoleId,
                                     config.PreviousMemberCheckGrantRoleId
                                 }).ToArrayAsync(cancellationToken);
            var twitch = await (from check in db.TwitchSubscriptionCheck.AsNoTracking()
                                join config in db.GuildTwitchSubscriptionConfig.AsNoTracking()
                                    on new { check.GuildId, check.BroadcasterId }
                                    equals new { config.GuildId, config.BroadcasterId }
                                where check.GuildId == guildId && check.IsChecked &&
                                    !check.PendingRoleRemoval && !config.DeletionPending
                                select new
                                {
                                    check.DiscordUserId,
                                    check.BroadcasterId,
                                    check.Tier,
                                    config.SubscriberRoleId,
                                    config.PreviousSubscriberRoleId,
                                    config.Tier1RoleId,
                                    config.Tier2RoleId,
                                    config.Tier3RoleId
                                }).ToArrayAsync(cancellationToken);

            var result = new List<MemberRoleEntitlement>();
            foreach (var item in youtube)
            {
                AddEntitlement(result, MemberEntitlementProvider.Youtube, item.MemberCheckChannelId,
                    item.UserId, item.MemberCheckGrantRoleId);
                AddEntitlement(result, MemberEntitlementProvider.Youtube, item.MemberCheckChannelId,
                    item.UserId, item.PreviousMemberCheckGrantRoleId ?? 0);
            }
            foreach (var item in twitch)
            {
                AddEntitlement(result, MemberEntitlementProvider.Twitch, item.BroadcasterId,
                    item.DiscordUserId, item.SubscriberRoleId);
                AddEntitlement(result, MemberEntitlementProvider.Twitch, item.BroadcasterId,
                    item.DiscordUserId, item.PreviousSubscriberRoleId ?? 0);
                AddEntitlement(result, MemberEntitlementProvider.Twitch, item.BroadcasterId,
                    item.DiscordUserId, GetTierRoleId(item.Tier, item.Tier1RoleId, item.Tier2RoleId, item.Tier3RoleId));
            }
            return result.ToArray();
        }

        private static void AddEntitlement(
            ICollection<MemberRoleEntitlement> entitlements,
            MemberEntitlementProvider provider,
            string configurationKey,
            ulong discordUserId,
            ulong roleId)
        {
            if (roleId != 0)
                entitlements.Add(new MemberRoleEntitlement(provider, configurationKey, discordUserId, roleId));
        }

        private static ulong GetTierRoleId(string tier, ulong tier1RoleId, ulong tier2RoleId, ulong tier3RoleId)
            => tier switch
            {
                "1000" => tier1RoleId,
                "2000" => tier2RoleId,
                "3000" => tier3RoleId,
                _ => 0
            };
    }
}
