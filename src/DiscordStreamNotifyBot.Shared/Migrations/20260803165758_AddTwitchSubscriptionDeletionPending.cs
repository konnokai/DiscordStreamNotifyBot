using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordStreamNotifyBot.Migrations
{
    /// <inheritdoc />
    public partial class AddTwitchSubscriptionDeletionPending : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 特規版已移除 Twitch 訂閱驗證，不再變更 guild_twitch_subscription_config；
            // 保留空 migration 以維持正式 DB 的 __EFMigrationsHistory 歷史。
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
