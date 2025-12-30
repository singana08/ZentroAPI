using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZentroAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteIdToMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BiometricEnabled",
                schema: "zentro_api",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "BiometricPin",
                schema: "zentro_api",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BiometricPinExpiresAt",
                schema: "zentro_api",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralCode",
                schema: "zentro_api",
                table: "users",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReferredById",
                schema: "zentro_api",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "QuoteId",
                schema: "zentro_api",
                table: "messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceType",
                schema: "zentro_api",
                table: "master_category",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Free");

            migrationBuilder.CreateTable(
                name: "wallets",
                schema: "zentro_api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wallets_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "zentro_api",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wallet_transactions",
                schema: "zentro_api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallet_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wallet_transactions_wallets_WalletId",
                        column: x => x.WalletId,
                        principalSchema: "zentro_api",
                        principalTable: "wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "referrals",
                schema: "zentro_api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferrerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferredUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferralCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    BonusAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    FirstBookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferredUserBookingsUsed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WalletTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_referrals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_referrals_users_ReferredUserId",
                        column: x => x.ReferredUserId,
                        principalSchema: "zentro_api",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_referrals_users_ReferrerId",
                        column: x => x.ReferrerId,
                        principalSchema: "zentro_api",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_referrals_wallet_transactions_WalletTransactionId",
                        column: x => x.WalletTransactionId,
                        principalSchema: "zentro_api",
                        principalTable: "wallet_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_BiometricPin",
                schema: "zentro_api",
                table: "users",
                column: "BiometricPin",
                filter: "\"BiometricPin\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_ReferralCode",
                schema: "zentro_api",
                table: "users",
                column: "ReferralCode",
                unique: true,
                filter: "\"ReferralCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_ReferredById",
                schema: "zentro_api",
                table: "users",
                column: "ReferredById");

            migrationBuilder.CreateIndex(
                name: "IX_referrals_ReferredUserId",
                schema: "zentro_api",
                table: "referrals",
                column: "ReferredUserId");

            migrationBuilder.CreateIndex(
                name: "IX_referrals_ReferrerId",
                schema: "zentro_api",
                table: "referrals",
                column: "ReferrerId");

            migrationBuilder.CreateIndex(
                name: "IX_referrals_ReferrerId_ReferredUserId",
                schema: "zentro_api",
                table: "referrals",
                columns: new[] { "ReferrerId", "ReferredUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_referrals_Status",
                schema: "zentro_api",
                table: "referrals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_referrals_WalletTransactionId",
                schema: "zentro_api",
                table: "referrals",
                column: "WalletTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transactions_CreatedAt",
                schema: "zentro_api",
                table: "wallet_transactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transactions_ExpiresAt",
                schema: "zentro_api",
                table: "wallet_transactions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transactions_WalletId",
                schema: "zentro_api",
                table: "wallet_transactions",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_wallets_UserId",
                schema: "zentro_api",
                table: "wallets",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_users_users_ReferredById",
                schema: "zentro_api",
                table: "users",
                column: "ReferredById",
                principalSchema: "zentro_api",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_users_ReferredById",
                schema: "zentro_api",
                table: "users");

            migrationBuilder.DropTable(
                name: "referrals",
                schema: "zentro_api");

            migrationBuilder.DropTable(
                name: "wallet_transactions",
                schema: "zentro_api");

            migrationBuilder.DropTable(
                name: "wallets",
                schema: "zentro_api");

            migrationBuilder.DropIndex(
                name: "IX_users_BiometricPin",
                schema: "zentro_api",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_ReferralCode",
                schema: "zentro_api",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_ReferredById",
                schema: "zentro_api",
                table: "users");

            migrationBuilder.DropColumn(
                name: "BiometricEnabled",
                schema: "zentro_api",
                table: "users");

            migrationBuilder.DropColumn(
                name: "BiometricPin",
                schema: "zentro_api",
                table: "users");

            migrationBuilder.DropColumn(
                name: "BiometricPinExpiresAt",
                schema: "zentro_api",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ReferralCode",
                schema: "zentro_api",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ReferredById",
                schema: "zentro_api",
                table: "users");

            migrationBuilder.DropColumn(
                name: "QuoteId",
                schema: "zentro_api",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "ServiceType",
                schema: "zentro_api",
                table: "master_category");
        }
    }
}
