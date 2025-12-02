using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZentroAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderRequestStatusAndUpdateQuote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HiddenRequestIds",
                schema: "halulu_api",
                table: "providers");

            migrationBuilder.CreateTable(
                name: "hidden_requests",
                schema: "halulu_api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    HiddenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hidden_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hidden_requests_providers_ProviderId",
                        column: x => x.ProviderId,
                        principalSchema: "halulu_api",
                        principalTable: "providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_hidden_requests_service_requests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalSchema: "halulu_api",
                        principalTable: "service_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_hidden_requests_ProviderId",
                schema: "halulu_api",
                table: "hidden_requests",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_hidden_requests_ProviderId_ServiceRequestId",
                schema: "halulu_api",
                table: "hidden_requests",
                columns: new[] { "ProviderId", "ServiceRequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hidden_requests_ServiceRequestId",
                schema: "halulu_api",
                table: "hidden_requests",
                column: "ServiceRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hidden_requests",
                schema: "halulu_api");

            migrationBuilder.AddColumn<string>(
                name: "HiddenRequestIds",
                schema: "halulu_api",
                table: "providers",
                type: "jsonb",
                nullable: true);
        }
    }
}
