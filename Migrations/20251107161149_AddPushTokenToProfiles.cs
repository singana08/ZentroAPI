using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaluluAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPushTokenToProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotificationsEnabled",
                schema: "halulu_api",
                table: "requesters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PushToken",
                schema: "halulu_api",
                table: "requesters",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotificationsEnabled",
                schema: "halulu_api",
                table: "providers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PushToken",
                schema: "halulu_api",
                table: "providers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationsEnabled",
                schema: "halulu_api",
                table: "requesters");

            migrationBuilder.DropColumn(
                name: "PushToken",
                schema: "halulu_api",
                table: "requesters");

            migrationBuilder.DropColumn(
                name: "NotificationsEnabled",
                schema: "halulu_api",
                table: "providers");

            migrationBuilder.DropColumn(
                name: "PushToken",
                schema: "halulu_api",
                table: "providers");
        }
    }
}
