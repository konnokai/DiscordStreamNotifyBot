using Discord.WebSocket;
using DiscordStreamNotifyBot.DataBase.Table;
using DiscordStreamNotifyBot.Shared;
using DiscordStreamNotifyBot.SharedService.Member;
using DiscordStreamNotifyBot.SharedService.YoutubeMember;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using System.Text;

namespace DiscordStreamNotifyBot.Tests.Component.MySql
{
    [Collection(MySqlComponentCollection.Name)]
    [Trait("Category", "MySqlComponent")]
    public sealed class YoutubeMemberCleanupPersistenceTests(MySqlComponentFixture fixture)
    {
        [MySqlComponentFact]
        public async Task FailedRemovalIsCommittedAndDisabledProviderCycleRetriesCleanupWithMetrics()
        {
            using var client = new DiscordSocketClient();
            var registry = Metrics.NewCustomRegistry();
            var service = CreateService(client, registry);
            ulong userId = (ulong)Random.Shared.NextInt64(1, long.MaxValue);
            var config = new GuildYoutubeMemberConfig
            {
                GuildId = userId,
                MemberCheckChannelId = $"channel-{userId}",
                MemberCheckChannelTitle = "Component",
                MemberCheckVideoId = "probe",
                MemberCheckGrantRoleId = 1
            };
            var check = new YoutubeMemberCheck
            {
                GuildId = userId,
                UserId = userId,
                CheckYTChannelId = config.MemberCheckChannelId,
                IsChecked = true
            };
            const string payload = "component-token-snapshot";
            await using (var db = fixture.DbService.GetDbContext())
            {
                db.GuildYoutubeMemberConfig.Add(config);
                db.YoutubeMemberCheck.Add(check);
                db.YoutubeMemberAccessToken.Add(new YoutubeMemberAccessToken
                {
                    DiscordUserId = userId,
                    EncryptedAccessToken = payload
                });
                await db.SaveChangesAsync();
            }

            bool originalConnected = BotState.IsConnect;
            int originalShardCount = BotState.TotalShardCount;
            try
            {
                BotState.IsConnect = false;
                BotState.TotalShardCount = 1;
                var result = await service.ApplyNotMemberAsync(YoutubeMemberPolicies.CaptureProbeConfiguration(config),
                    check, YoutubeMemberPolicies.CaptureState(check), payload, CancellationToken.None);
                Assert.False(result.Applied);
                await using (var readDb = fixture.DbService.GetDbContext())
                {
                    var pending = await readDb.YoutubeMemberCheck.SingleAsync(x => x.Id == check.Id);
                    Assert.True(pending.PendingRoleRemoval);
                    Assert.False(pending.IsChecked);
                }

                // 模擬 Ready 後確認已離開 guild；不登入 Discord，不呼叫 provider 或 REST。
                BotState.IsConnect = true;
                Assert.False(service.IsEnable);
                await service.CheckMemberShip(false, CancellationToken.None);
                await using var verifyDb = fixture.DbService.GetDbContext();
                Assert.False(await verifyDb.YoutubeMemberCheck.AnyAsync(x => x.Id == check.Id));
                Assert.Equal(payload, (await verifyDb.YoutubeMemberAccessToken.SingleAsync(x => x.DiscordUserId == userId))
                    .EncryptedAccessToken);

                using var canceled = new CancellationTokenSource();
                canceled.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CheckMemberShip(true, canceled.Token));
                using var output = new MemoryStream();
                await registry.CollectAndExportAsTextAsync(output);
                string metrics = Encoding.UTF8.GetString(output.ToArray());
                Assert.Contains("youtube_member_check_cycles_total{check_type=\"new\",result=\"success\"} 1", metrics);
                Assert.Contains("youtube_member_check_cycles_total{check_type=\"old\",result=\"failure\"} 1", metrics);
                Assert.Contains("youtube_member_check_duration_seconds_count{check_type=\"new\"} 1", metrics);
                Assert.Contains("youtube_member_check_duration_seconds_count{check_type=\"old\"} 1", metrics);
            }
            finally
            {
                BotState.IsConnect = originalConnected;
                BotState.TotalShardCount = originalShardCount;
                await using var db = fixture.DbService.GetDbContext();
                await db.YoutubeMemberCheck.Where(x => x.Id == check.Id).ExecuteDeleteAsync();
                await db.GuildYoutubeMemberConfig.Where(x => x.Id == config.Id).ExecuteDeleteAsync();
                await db.YoutubeMemberAccessToken.Where(x => x.DiscordUserId == userId).ExecuteDeleteAsync();
            }
        }

        private YoutubeMemberService CreateService(DiscordSocketClient client, CollectorRegistry registry)
        {
            var config = new BotConfig { ProviderTokenEncryptionKey = MySqlComponentFixture.EncryptionKey };
            var coordinator = new MemberOperationCoordinator();
            var roles = new YoutubeMemberRoleService(fixture.DbService, client, coordinator,
                new MemberRoleOwnershipService(fixture.DbService));
            return new YoutubeMemberService(null, client, config, fixture.DbService, null, null, null, null,
                new NotifierMetrics(Metrics.WithCustomRegistry(registry)), roles, coordinator, null,
                new YoutubeMemberAuthorizationService(fixture.DbService, config, null), null);
        }
    }
}
