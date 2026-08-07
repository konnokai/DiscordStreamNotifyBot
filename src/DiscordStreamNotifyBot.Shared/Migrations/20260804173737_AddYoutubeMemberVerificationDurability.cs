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
            migrationBuilder.AlterColumn<string>(
                name: "check_yt_channel_id",
                table: "youtube_member_check",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "pending_role_removal",
                table: "youtube_member_check",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "member_check_channel_id",
                table: "guild_youtube_member_config",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "deletion_pending",
                table: "guild_youtube_member_config",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<ulong>(
                name: "previous_member_check_grant_role_id",
                table: "guild_youtube_member_config",
                type: "bigint unsigned",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_youtube_member_check_guild_id_user_id_check_yt_channel_id",
                table: "youtube_member_check",
                columns: new[] { "guild_id", "user_id", "check_yt_channel_id" },
                unique: true)
                .Annotation("MySql:IndexPrefixLength", new[] { 0, 0, 24 });

            migrationBuilder.CreateIndex(
                name: "ix_youtube_member_check_pending_role_removal_guild_id",
                table: "youtube_member_check",
                columns: new[] { "pending_role_removal", "guild_id" });

            migrationBuilder.CreateIndex(
                name: "ix_youtube_member_check_user_id_pending_role_removal",
                table: "youtube_member_check",
                columns: new[] { "user_id", "pending_role_removal" });

            migrationBuilder.CreateIndex(
                name: "ix_guild_youtube_member_config_deletion_pending_guild_id",
                table: "guild_youtube_member_config",
                columns: new[] { "deletion_pending", "guild_id" });

            migrationBuilder.CreateIndex(
                name: "ix_guild_youtube_member_config_guild_id_member_check_channel_id",
                table: "guild_youtube_member_config",
                columns: new[] { "guild_id", "member_check_channel_id" },
                unique: true)
                .Annotation("MySql:IndexPrefixLength", new[] { 0, 24 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_youtube_member_check_guild_id_user_id_check_yt_channel_id",
                table: "youtube_member_check");

            migrationBuilder.DropIndex(
                name: "ix_youtube_member_check_pending_role_removal_guild_id",
                table: "youtube_member_check");

            migrationBuilder.DropIndex(
                name: "ix_youtube_member_check_user_id_pending_role_removal",
                table: "youtube_member_check");

            migrationBuilder.DropIndex(
                name: "ix_guild_youtube_member_config_deletion_pending_guild_id",
                table: "guild_youtube_member_config");

            migrationBuilder.DropIndex(
                name: "ix_guild_youtube_member_config_guild_id_member_check_channel_id",
                table: "guild_youtube_member_config");

            migrationBuilder.DropColumn(
                name: "pending_role_removal",
                table: "youtube_member_check");

            migrationBuilder.DropColumn(
                name: "deletion_pending",
                table: "guild_youtube_member_config");

            migrationBuilder.DropColumn(
                name: "previous_member_check_grant_role_id",
                table: "guild_youtube_member_config");

            migrationBuilder.AlterColumn<string>(
                name: "check_yt_channel_id",
                table: "youtube_member_check",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "member_check_channel_id",
                table: "guild_youtube_member_config",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
