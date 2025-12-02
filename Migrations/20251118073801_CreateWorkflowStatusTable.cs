using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZentroAPI.Migrations
{
    /// <inheritdoc />
    public partial class CreateWorkflowStatusTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "halulu_api",
                table: "quotes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                schema: "halulu_api",
                table: "quotes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "workflow_statuses",
                schema: "halulu_api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsInProgress = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    InProgressDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCheckedIn = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CheckedInDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CompletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_statuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_statuses_providers_ProviderId",
                        column: x => x.ProviderId,
                        principalSchema: "halulu_api",
                        principalTable: "providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workflow_statuses_service_requests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "halulu_api",
                        principalTable: "service_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quotes_status",
                schema: "halulu_api",
                table: "quotes",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_statuses_ProviderId",
                schema: "halulu_api",
                table: "workflow_statuses",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_statuses_RequestId",
                schema: "halulu_api",
                table: "workflow_statuses",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_statuses_RequestId_ProviderId",
                schema: "halulu_api",
                table: "workflow_statuses",
                columns: new[] { "RequestId", "ProviderId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workflow_statuses",
                schema: "halulu_api");

            migrationBuilder.DropIndex(
                name: "IX_quotes_status",
                schema: "halulu_api",
                table: "quotes");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "halulu_api",
                table: "quotes");

            migrationBuilder.DropColumn(
                name: "updated_at",
                schema: "halulu_api",
                table: "quotes");
        }
    }
}
