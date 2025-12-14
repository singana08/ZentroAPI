using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZentroAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPushNotificationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_preferences",
                schema: "zentro_api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnablePushNotifications = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    NewRequests = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    QuoteResponses = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    StatusUpdates = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Messages = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Reminders = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_preferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_preferences_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "zentro_api",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "push_notification_logs",
                schema: "zentro_api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Body = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "sent"),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_push_notification_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_push_notification_logs_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "zentro_api",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_push_tokens",
                schema: "zentro_api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PushToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DeviceType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    AppVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_push_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_push_tokens_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "zentro_api",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_preferences_UserId",
                schema: "zentro_api",
                table: "notification_preferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_push_notification_logs_SentAt",
                schema: "zentro_api",
                table: "push_notification_logs",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_push_notification_logs_Status",
                schema: "zentro_api",
                table: "push_notification_logs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_push_notification_logs_UserId",
                schema: "zentro_api",
                table: "push_notification_logs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_push_tokens_PushToken",
                schema: "zentro_api",
                table: "user_push_tokens",
                column: "PushToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_push_tokens_UserId",
                schema: "zentro_api",
                table: "user_push_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_push_tokens_UserId_DeviceId",
                schema: "zentro_api",
                table: "user_push_tokens",
                columns: new[] { "UserId", "DeviceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_preferences",
                schema: "zentro_api");

            migrationBuilder.DropTable(
                name: "push_notification_logs",
                schema: "zentro_api");

            migrationBuilder.DropTable(
                name: "user_push_tokens",
                schema: "zentro_api");
        }
    }
}
