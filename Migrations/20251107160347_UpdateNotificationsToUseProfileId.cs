using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZentroAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNotificationsToUseProfileId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_notifications_users_UserId",
                schema: "halulu_api",
                table: "notifications");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "halulu_api",
                table: "notifications",
                newName: "ProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_notifications_UserId_IsRead",
                schema: "halulu_api",
                table: "notifications",
                newName: "IX_notifications_ProfileId_IsRead");

            migrationBuilder.RenameIndex(
                name: "IX_notifications_UserId_CreatedAt",
                schema: "halulu_api",
                table: "notifications",
                newName: "IX_notifications_ProfileId_CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProfileId",
                schema: "halulu_api",
                table: "notifications",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_notifications_ProfileId_IsRead",
                schema: "halulu_api",
                table: "notifications",
                newName: "IX_notifications_UserId_IsRead");

            migrationBuilder.RenameIndex(
                name: "IX_notifications_ProfileId_CreatedAt",
                schema: "halulu_api",
                table: "notifications",
                newName: "IX_notifications_UserId_CreatedAt");

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_users_UserId",
                schema: "halulu_api",
                table: "notifications",
                column: "UserId",
                principalSchema: "halulu_api",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
