using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZentroAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "halulu_api");

            migrationBuilder.CreateTable(
                name: "master_category",
                schema: "halulu_api",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Icon = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_master_category", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "halulu_api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProfileImage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "master_subcategory",
                schema: "halulu_api",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Icon = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_master_subcategory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_master_subcategory_master_category_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "halulu_api",
                        principalTable: "master_category",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OtpRecords",
                schema: "halulu_api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OtpCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Purpose = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OtpRecords_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "halulu_api",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "providers",
                schema: "halulu_api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceCategories = table.Column<string>(type: "text", nullable: true),
                    ExperienceYears = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Bio = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ServiceAreas = table.Column<string>(type: "text", nullable: true),
                    PricingModel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Documents = table.Column<string>(type: "text", nullable: true),
                    AvailabilitySlots = table.Column<string>(type: "text", nullable: true),
                    Rating = table.Column<decimal>(type: "numeric(3,2)", nullable: false, defaultValue: 0m),
                    Earnings = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_providers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_providers_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "halulu_api",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "requesters",
                schema: "halulu_api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PreferredCategories = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_requesters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_requesters_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "halulu_api",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_requests",
                schema: "halulu_api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingType = table.Column<string>(type: "text", nullable: false),
                    MainCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SubCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Time = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AdditionalNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_service_requests_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "halulu_api",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_master_category_IsActive",
                schema: "halulu_api",
                table: "master_category",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_master_category_Name",
                schema: "halulu_api",
                table: "master_category",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_master_subcategory_CategoryId_Name",
                schema: "halulu_api",
                table: "master_subcategory",
                columns: new[] { "CategoryId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_master_subcategory_IsActive",
                schema: "halulu_api",
                table: "master_subcategory",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_OtpRecords_Email_CreatedAt",
                schema: "halulu_api",
                table: "OtpRecords",
                columns: new[] { "Email", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OtpRecords_UserId",
                schema: "halulu_api",
                table: "OtpRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_providers_IsActive",
                schema: "halulu_api",
                table: "providers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_providers_Rating",
                schema: "halulu_api",
                table: "providers",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_providers_UserId",
                schema: "halulu_api",
                table: "providers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_requesters_IsActive",
                schema: "halulu_api",
                table: "requesters",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_requesters_UserId",
                schema: "halulu_api",
                table: "requesters",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_BookingType",
                schema: "halulu_api",
                table: "service_requests",
                column: "BookingType");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_CreatedAt",
                schema: "halulu_api",
                table: "service_requests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_Status",
                schema: "halulu_api",
                table: "service_requests",
                column: "Status");

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

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                schema: "halulu_api",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_PhoneNumber",
                schema: "halulu_api",
                table: "users",
                column: "PhoneNumber",
                unique: true,
                filter: "\"PhoneNumber\" IS NOT NULL AND \"PhoneNumber\" != ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "master_subcategory",
                schema: "halulu_api");

            migrationBuilder.DropTable(
                name: "OtpRecords",
                schema: "halulu_api");

            migrationBuilder.DropTable(
                name: "providers",
                schema: "halulu_api");

            migrationBuilder.DropTable(
                name: "requesters",
                schema: "halulu_api");

            migrationBuilder.DropTable(
                name: "service_requests",
                schema: "halulu_api");

            migrationBuilder.DropTable(
                name: "master_category",
                schema: "halulu_api");

            migrationBuilder.DropTable(
                name: "users",
                schema: "halulu_api");
        }
    }
}
