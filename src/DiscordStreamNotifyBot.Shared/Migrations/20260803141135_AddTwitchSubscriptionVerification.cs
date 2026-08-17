using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordStreamNotifyBot.Migrations
{
    /// <inheritdoc />
    public partial class AddTwitchSubscriptionVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "guild_twitch_subscription_config",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guild_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    broadcaster_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    broadcaster_login = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    broadcaster_display_name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    subscriber_role_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    previous_subscriber_role_id = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    tier1role_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    tier2role_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    tier3role_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    date_added = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guild_twitch_subscription_config", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "twitch_subscription_check",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guild_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    discord_user_id = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    broadcaster_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    locale = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_checked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    pending_role_removal = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    tier = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_gift = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    last_check_time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    date_added = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_twitch_subscription_check", x => x.id);
                    table.CheckConstraint("ck_twitch_subscription_check_tier", "`tier` IS NULL OR `tier` IN ('1000', '2000', '3000')");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_guild_twitch_subscription_config_guild_id_broadcaster_id",
                table: "guild_twitch_subscription_config",
                columns: new[] { "guild_id", "broadcaster_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_twitch_subscription_check_guild_id_discord_user_id_broadcast",
                table: "twitch_subscription_check",
                columns: new[] { "guild_id", "discord_user_id", "broadcaster_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "guild_twitch_subscription_config");

            migrationBuilder.DropTable(
                name: "twitch_subscription_check");
        }
    }
}
