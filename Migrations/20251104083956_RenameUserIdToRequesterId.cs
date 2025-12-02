using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZentroAPI.Migrations
{
    /// <inheritdoc />
    public partial class RenameUserIdToRequesterId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This migration is redundant as the previous one already handled these drops
            // Just ensure they don't exist
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                schema: "halulu_api",
                table: "service_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_UserId1",
                schema: "halulu_api",
                table: "service_requests",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_service_requests_users_UserId1",
                schema: "halulu_api",
                table: "service_requests",
                column: "UserId1",
                principalSchema: "halulu_api",
                principalTable: "users",
                principalColumn: "Id");
        }
    }
}
