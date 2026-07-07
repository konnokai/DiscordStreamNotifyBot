using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordStreamNotifyBot.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelDrift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 同步既有 drift：Twitter Space 功能已自模型移除，但 EnsureCreated 不會 drop 舊表。
            // 用 IF EXISTS 確保不論目標 DB 是否仍有這些表（視當初由哪個版本的 EnsureCreated 建立）皆可安全套用。
            // 三表互無外鍵，drop 順序無關。
            migrationBuilder.Sql("DROP TABLE IF EXISTS `notice_twitter_space_channel`;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS `twitter_space`;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS `twitter_space_spider`;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notice_twitter_space_channel",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    date_added = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    discord_channel_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    guild_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    notice_twitter_space_user_id = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    notice_twitter_space_user_screen_name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    strat_twitter_space_message = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notice_twitter_space_channel", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "twitter_space",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    date_added = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    spaec_actual_start_time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    spaec_id = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    spaec_master_playlist_url = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    spaec_title = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_id = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_screen_name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_twitter_space", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "twitter_space_spider",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    date_added = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    guild_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    is_record = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_warning_user = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    user_name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_screen_name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_twitter_space_spider", x => x.user_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
