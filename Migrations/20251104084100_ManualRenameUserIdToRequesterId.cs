using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaluluAPI.Migrations
{
    /// <inheritdoc />
    public partial class ManualRenameUserIdToRequesterId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK_service_requests_users_UserId",
                schema: "halulu_api",
                table: "service_requests");

            // Drop existing indexes
            migrationBuilder.DropIndex(
                name: "IX_service_requests_UserId",
                schema: "halulu_api",
                table: "service_requests");

            migrationBuilder.DropIndex(
                name: "IX_service_requests_UserId_Status",
                schema: "halulu_api",
                table: "service_requests");

            // Rename column from UserId to RequesterId
            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "halulu_api",
                table: "service_requests",
                newName: "RequesterId");

            // Create new indexes with RequesterId
            migrationBuilder.CreateIndex(
                name: "IX_service_requests_RequesterId",
                schema: "halulu_api",
                table: "service_requests",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_RequesterId_Status",
                schema: "halulu_api",
                table: "service_requests",
                columns: new[] { "RequesterId", "Status" });

            // Add foreign key constraint to requesters table
            migrationBuilder.AddForeignKey(
                name: "FK_service_requests_requesters_RequesterId",
                schema: "halulu_api",
                table: "service_requests",
                column: "RequesterId",
                principalSchema: "halulu_api",
                principalTable: "requesters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop new foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK_service_requests_requesters_RequesterId",
                schema: "halulu_api",
                table: "service_requests");

            // Drop new indexes
            migrationBuilder.DropIndex(
                name: "IX_service_requests_RequesterId",
                schema: "halulu_api",
                table: "service_requests");

            migrationBuilder.DropIndex(
                name: "IX_service_requests_RequesterId_Status",
                schema: "halulu_api",
                table: "service_requests");

            // Rename column back from RequesterId to UserId
            migrationBuilder.RenameColumn(
                name: "RequesterId",
                schema: "halulu_api",
                table: "service_requests",
                newName: "UserId");

            // Recreate original indexes
            migrationBuilder.CreateIndex(
                name: "IX_service_requests_UserId",
                schema: "halulu_api",
                table: "service_requests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_UserId_Status",
                schema: "halulu_api",
                table: "service_requests",
                columns: new[] { "UserId", "Status" });

            // Add back original foreign key constraint to users table
            migrationBuilder.AddForeignKey(
                name: "FK_service_requests_users_UserId",
                schema: "halulu_api",
                table: "service_requests",
                column: "UserId",
                principalSchema: "halulu_api",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}