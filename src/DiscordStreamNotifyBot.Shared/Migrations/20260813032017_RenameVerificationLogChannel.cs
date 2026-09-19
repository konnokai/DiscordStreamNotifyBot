using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordStreamNotifyBot.Migrations
{
    /// <inheritdoc />
    public partial class RenameVerificationLogChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 特規版已移除會員驗證記錄頻道，guild_config 自基線即不含 log_member_status_channel_id；
            // 保留空 migration 以維持正式 DB 的 __EFMigrationsHistory 歷史。
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
