using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaluluAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteMessageEntitiesAndEnhanceServiceRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "halulu_api",
                table: "service_requests",
                type: "text",
                nullable: false,
                defaultValue: "Open",
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "Pending");

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedProviderId",
                schema: "halulu_api",
                table: "service_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "halulu_api",
                table: "service_requests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                schema: "halulu_api",
                table: "service_requests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "messages",
                schema: "halulu_api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiverId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_messages_service_requests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "halulu_api",
                        principalTable: "service_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quotes",
                schema: "halulu_api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quotes_providers_ProviderId",
                        column: x => x.ProviderId,
                        principalSchema: "halulu_api",
                        principalTable: "providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quotes_service_requests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "halulu_api",
                        principalTable: "service_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_AssignedProviderId",
                schema: "halulu_api",
                table: "service_requests",
                column: "AssignedProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_AssignedProviderId_Status",
                schema: "halulu_api",
                table: "service_requests",
                columns: new[] { "AssignedProviderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_messages_RequestId",
                schema: "halulu_api",
                table: "messages",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_messages_SenderId_ReceiverId",
                schema: "halulu_api",
                table: "messages",
                columns: new[] { "SenderId", "ReceiverId" });

            migrationBuilder.CreateIndex(
                name: "IX_messages_Timestamp",
                schema: "halulu_api",
                table: "messages",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_quotes_ProviderId",
                schema: "halulu_api",
                table: "quotes",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_quotes_RequestId",
                schema: "halulu_api",
                table: "quotes",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_quotes_RequestId_ProviderId",
                schema: "halulu_api",
                table: "quotes",
                columns: new[] { "RequestId", "ProviderId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_service_requests_providers_AssignedProviderId",
                schema: "halulu_api",
                table: "service_requests",
                column: "AssignedProviderId",
                principalSchema: "halulu_api",
                principalTable: "providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_service_requests_providers_AssignedProviderId",
                schema: "halulu_api",
                table: "service_requests");

            migrationBuilder.DropTable(
                name: "messages",
                schema: "halulu_api");

            migrationBuilder.DropTable(
                name: "quotes",
                schema: "halulu_api");

            migrationBuilder.DropIndex(
                name: "IX_service_requests_AssignedProviderId",
                schema: "halulu_api",
                table: "service_requests");

            migrationBuilder.DropIndex(
                name: "IX_service_requests_AssignedProviderId_Status",
                schema: "halulu_api",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "AssignedProviderId",
                schema: "halulu_api",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "halulu_api",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "Title",
                schema: "halulu_api",
                table: "service_requests");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "halulu_api",
                table: "service_requests",
                type: "text",
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "Open");
        }
    }
}
