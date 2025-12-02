using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZentroAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemovePushTokenFromUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationsEnabled",
                schema: "halulu_api",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PushToken",
                schema: "halulu_api",
                table: "users");

            migrationBuilder.AlterColumn<bool>(
                name: "NotificationsEnabled",
                schema: "halulu_api",
                table: "requesters",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "NotificationsEnabled",
                schema: "halulu_api",
                table: "providers",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotificationsEnabled",
                schema: "halulu_api",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "PushToken",
                schema: "halulu_api",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "NotificationsEnabled",
                schema: "halulu_api",
                table: "requesters",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "NotificationsEnabled",
                schema: "halulu_api",
                table: "providers",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);
        }
    }
}
