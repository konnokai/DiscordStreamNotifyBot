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
            migrationBuilder.RenameColumn(
                name: "log_member_status_channel_id",
                table: "guild_config",
                newName: "verification_log_channel_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "verification_log_channel_id",
                table: "guild_config",
                newName: "log_member_status_channel_id");
        }
    }
}
