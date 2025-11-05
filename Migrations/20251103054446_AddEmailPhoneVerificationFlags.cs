using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaluluAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailPhoneVerificationFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                schema: "halulu_api",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPhoneVerified",
                schema: "halulu_api",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                schema: "halulu_api",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsPhoneVerified",
                schema: "halulu_api",
                table: "users");
        }
    }
}
