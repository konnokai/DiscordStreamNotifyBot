using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordStreamNotifyBot.Migrations
{
    /// <inheritdoc />
    public partial class AddChzzkNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "max_chzzk_spider_count",
                table: "guild_config",
                type: "int unsigned",
                nullable: false,
                defaultValue: 3u);

            migrationBuilder.CreateTable(
                name: "chzzk_spider",
                columns: table => new
                {
                    channel_id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    channel_name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    channel_image_url = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    guild_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    date_added = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    initialized_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    current_stream_key = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chzzk_spider", x => x.channel_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "chzzk_streams",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    stream_key = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    channel_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    open_date_raw = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    close_date_raw = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    stream_title = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    category_name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<int>(type: "int", nullable: false),
                    last_observed_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_added = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chzzk_streams", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "notice_chzzk_stream_channels",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guild_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    discord_channel_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    notice_chzzk_channel_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    start_stream_message = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    end_stream_message = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_added = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notice_chzzk_stream_channels", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_chzzk_streams_channel_id",
                table: "chzzk_streams",
                column: "channel_id");

            migrationBuilder.CreateIndex(
                name: "ix_chzzk_streams_stream_key",
                table: "chzzk_streams",
                column: "stream_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notice_chzzk_stream_channels_guild_id_notice_chzzk_channel_id",
                table: "notice_chzzk_stream_channels",
                columns: new[] { "guild_id", "notice_chzzk_channel_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chzzk_spider");

            migrationBuilder.DropTable(
                name: "chzzk_streams");

            migrationBuilder.DropTable(
                name: "notice_chzzk_stream_channels");

            migrationBuilder.DropColumn(
                name: "max_chzzk_spider_count",
                table: "guild_config");
        }
    }
}
