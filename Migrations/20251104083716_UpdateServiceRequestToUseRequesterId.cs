using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZentroAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateServiceRequestToUseRequesterId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if constraint exists before dropping
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.table_constraints 
                              WHERE constraint_name = 'FK_service_requests_users_UserId1' 
                              AND table_schema = 'halulu_api') THEN
                        ALTER TABLE halulu_api.service_requests DROP CONSTRAINT ""FK_service_requests_users_UserId1"";
                    END IF;
                END $$;
            ");

            // Check if index exists before dropping
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_indexes 
                              WHERE indexname = 'IX_service_requests_UserId1' 
                              AND schemaname = 'halulu_api') THEN
                        DROP INDEX halulu_api.""IX_service_requests_UserId1"";
                    END IF;
                END $$;
            ");

            // Check if column exists before dropping
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns 
                              WHERE table_name = 'service_requests' 
                              AND column_name = 'UserId1' 
                              AND table_schema = 'halulu_api') THEN
                        ALTER TABLE halulu_api.service_requests DROP COLUMN ""UserId1"";
                    END IF;
                END $$;
            ");
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
