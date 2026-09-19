using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordStreamNotifyBot.Migrations
{
    /// <inheritdoc />
    public partial class AddYoutubeMemberVerificationDurability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 特規版已移除 YouTube 會員驗證，不再變更 youtube_member_check／guild_youtube_member_config；
            // 保留空 migration 以維持正式 DB 的 __EFMigrationsHistory 歷史。
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
