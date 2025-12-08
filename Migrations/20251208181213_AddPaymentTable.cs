using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZentroAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "zentro_api");

            migrationBuilder.RenameTable(
                name: "workflow_statuses",
                schema: "halulu_api",
                newName: "workflow_statuses",
                newSchema: "zentro_api");

            migrationBuilder.RenameTable(
                name: "users",
                schema: "halulu_api",
                newName: "users",
                newSchema: "zentro_api");

            migrationBuilder.RenameTable(
                name: "service_requests",
                schema: "halulu_api",
                newName: "service_requests",
                newSchema: "zentro_api");

            migrationBuilder.RenameTable(
                name: "reviews",
                schema: "halulu_api",
                newName: "reviews",
                newSchema: "zentro_api");

            migrationBuilder.RenameTable(
                name: "requesters",
                schema: "halulu_api",
                newName: "requesters",
                newSchema: "zentro_api");

            migrationBuilder.RenameTable(
                name: "quotes",
                schema: "halulu_api",
                newName: "quotes",
                newSchema: "zentro_api");

            migrationBuilder.RenameTable(
                name: "providers",
                schema: "halulu_api",
                newName: "providers",
                newSchema: "zentro_api");

            migrationBuilder.RenameTable(
                name: "provider_request_status",
                schema: "halulu_api",
                newName: "provider_request_status",
                newSchema: "zentro_api");

            migrationBuilder.RenameTable(
                name: "OtpRecords",
                schema: "halulu_api",
                newName: "OtpRecords",
                newSchema: "zentro_api");

            migrationBuilder.RenameTable(
                name: "notifications",
                schema: "halulu_api",
                newName: "notifications",
                newSchema: "zentro_api");

            migrationBuilder.RenameTable(
                name: "messages",
                schema: "halulu_api",
                newName: "messages",
                newSchema: "zentro_api");

            migrationBuilder.RenameTable(
                name: "master_subcategory",
                schema: "halulu_api",
                newName: "master_subcategory",
                newSchema: "zentro_api");

            migrationBuilder.RenameTable(
                name: "master_category",
                schema: "halulu_api",
                newName: "master_category",
                newSchema: "zentro_api");

            migrationBuilder.RenameTable(
                name: "hidden_requests",
                schema: "halulu_api",
                newName: "hidden_requests",
                newSchema: "zentro_api");

            migrationBuilder.RenameTable(
                name: "agreements",
                schema: "halulu_api",
                newName: "agreements",
                newSchema: "zentro_api");

            migrationBuilder.RenameTable(
                name: "addresses",
                schema: "halulu_api",
                newName: "addresses",
                newSchema: "zentro_api");

            migrationBuilder.RenameColumn(
                name: "RequestId",
                schema: "zentro_api",
                table: "agreements",
                newName: "QuoteId");

            migrationBuilder.RenameIndex(
                name: "IX_agreements_RequestId_ProviderId",
                schema: "zentro_api",
                table: "agreements",
                newName: "IX_agreements_QuoteId_ProviderId");

            migrationBuilder.CreateTable(
                name: "payments",
                schema: "zentro_api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Method = table.Column<string>(type: "text", nullable: false),
                    TransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PaymentIntentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payments_service_requests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalSchema: "zentro_api",
                        principalTable: "service_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payments_PayeeId",
                schema: "zentro_api",
                table: "payments",
                column: "PayeeId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_PayerId",
                schema: "zentro_api",
                table: "payments",
                column: "PayerId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_ServiceRequestId",
                schema: "zentro_api",
                table: "payments",
                column: "ServiceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_Status",
                schema: "zentro_api",
                table: "payments",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payments",
                schema: "zentro_api");

            migrationBuilder.EnsureSchema(
                name: "halulu_api");

            migrationBuilder.RenameTable(
                name: "workflow_statuses",
                schema: "zentro_api",
                newName: "workflow_statuses",
                newSchema: "halulu_api");

            migrationBuilder.RenameTable(
                name: "users",
                schema: "zentro_api",
                newName: "users",
                newSchema: "halulu_api");

            migrationBuilder.RenameTable(
                name: "service_requests",
                schema: "zentro_api",
                newName: "service_requests",
                newSchema: "halulu_api");

            migrationBuilder.RenameTable(
                name: "reviews",
                schema: "zentro_api",
                newName: "reviews",
                newSchema: "halulu_api");

            migrationBuilder.RenameTable(
                name: "requesters",
                schema: "zentro_api",
                newName: "requesters",
                newSchema: "halulu_api");

            migrationBuilder.RenameTable(
                name: "quotes",
                schema: "zentro_api",
                newName: "quotes",
                newSchema: "halulu_api");

            migrationBuilder.RenameTable(
                name: "providers",
                schema: "zentro_api",
                newName: "providers",
                newSchema: "halulu_api");

            migrationBuilder.RenameTable(
                name: "provider_request_status",
                schema: "zentro_api",
                newName: "provider_request_status",
                newSchema: "halulu_api");

            migrationBuilder.RenameTable(
                name: "OtpRecords",
                schema: "zentro_api",
                newName: "OtpRecords",
                newSchema: "halulu_api");

            migrationBuilder.RenameTable(
                name: "notifications",
                schema: "zentro_api",
                newName: "notifications",
                newSchema: "halulu_api");

            migrationBuilder.RenameTable(
                name: "messages",
                schema: "zentro_api",
                newName: "messages",
                newSchema: "halulu_api");

            migrationBuilder.RenameTable(
                name: "master_subcategory",
                schema: "zentro_api",
                newName: "master_subcategory",
                newSchema: "halulu_api");

            migrationBuilder.RenameTable(
                name: "master_category",
                schema: "zentro_api",
                newName: "master_category",
                newSchema: "halulu_api");

            migrationBuilder.RenameTable(
                name: "hidden_requests",
                schema: "zentro_api",
                newName: "hidden_requests",
                newSchema: "halulu_api");

            migrationBuilder.RenameTable(
                name: "agreements",
                schema: "zentro_api",
                newName: "agreements",
                newSchema: "halulu_api");

            migrationBuilder.RenameTable(
                name: "addresses",
                schema: "zentro_api",
                newName: "addresses",
                newSchema: "halulu_api");

            migrationBuilder.RenameColumn(
                name: "QuoteId",
                schema: "halulu_api",
                table: "agreements",
                newName: "RequestId");

            migrationBuilder.RenameIndex(
                name: "IX_agreements_QuoteId_ProviderId",
                schema: "halulu_api",
                table: "agreements",
                newName: "IX_agreements_RequestId_ProviderId");
        }
    }
}
