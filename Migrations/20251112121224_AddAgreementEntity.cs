using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaluluAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAgreementEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                schema: "halulu_api",
                table: "quotes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "agreements",
                schema: "halulu_api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterAccepted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ProviderAccepted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RequesterAcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProviderAcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinalizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agreements", x => x.Id);
                });

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
                name: "IX_agreements_ProviderId",
                schema: "halulu_api",
                table: "agreements",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_agreements_RequesterId",
                schema: "halulu_api",
                table: "agreements",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_agreements_RequestId_ProviderId",
                schema: "halulu_api",
                table: "agreements",
                columns: new[] { "RequestId", "ProviderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_agreements_Status",
                schema: "halulu_api",
                table: "agreements",
                column: "Status");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agreements",
                schema: "halulu_api");

            migrationBuilder.DropTable(
                name: "provider_request_status",
                schema: "halulu_api");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                schema: "halulu_api",
                table: "quotes");
        }
    }
}
