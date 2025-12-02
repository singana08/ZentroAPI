using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZentroAPI.Migrations
{
    /// <inheritdoc />
    public partial class CreateProviderRequestStatusTable : Migration
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

            migrationBuilder.CreateTable(
                name: "provider_request_status",
                schema: "halulu_api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_request_status", x => x.Id);
                    table.ForeignKey(
                        name: "FK_provider_request_status_providers_ProviderId",
                        column: x => x.ProviderId,
                        principalSchema: "halulu_api",
                        principalTable: "providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_provider_request_status_quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalSchema: "halulu_api",
                        principalTable: "quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_provider_request_status_service_requests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "halulu_api",
                        principalTable: "service_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_provider_request_status_ProviderId",
                schema: "halulu_api",
                table: "provider_request_status",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_provider_request_status_ProviderId_RequestId",
                schema: "halulu_api",
                table: "provider_request_status",
                columns: new[] { "ProviderId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_request_status_QuoteId",
                schema: "halulu_api",
                table: "provider_request_status",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_provider_request_status_RequestId",
                schema: "halulu_api",
                table: "provider_request_status",
                column: "RequestId");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                schema: "halulu_api",
                table: "quotes",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hidden_requests",
                schema: "halulu_api");

            migrationBuilder.DropTable(
                name: "provider_request_status",
                schema: "halulu_api");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                schema: "halulu_api",
                table: "quotes");

            migrationBuilder.AddColumn<string>(
                name: "HiddenRequestIds",
                schema: "halulu_api",
                table: "providers",
                type: "jsonb",
                nullable: true);
        }
    }
}
