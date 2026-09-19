using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordStreamNotifyBot.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxSpiderCountSettingField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 特規版已移除 TwitCasting、Twitter Space 與 YouTube 會員驗證，
            // 僅保留仍在使用的 Twitch／YouTube 爬蟲數量上限欄位。
            migrationBuilder.AddColumn<uint>(
                name: "max_twitch_spider_count",
                table: "guild_config",
                type: "int unsigned",
                nullable: false,
                defaultValue: 3u);

            migrationBuilder.AddColumn<uint>(
                name: "max_you_tube_spider_count",
                table: "guild_config",
                type: "int unsigned",
                nullable: false,
                defaultValue: 3u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "max_twitch_spider_count",
                table: "guild_config");

            migrationBuilder.DropColumn(
                name: "max_you_tube_spider_count",
                table: "guild_config");
        }
    }
}
