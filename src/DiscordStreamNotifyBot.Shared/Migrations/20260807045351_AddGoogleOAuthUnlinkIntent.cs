using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordStreamNotifyBot.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleOAuthUnlinkIntent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 特規版已移除 Google OAuth unlink 流程，不再建立 google_oauth_unlink_intent；
            // 保留空 migration 以維持正式 DB 的 __EFMigrationsHistory 歷史。
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
