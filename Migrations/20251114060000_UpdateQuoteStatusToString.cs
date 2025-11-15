using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaluluAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateQuoteStatusToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert status column from integer to varchar
            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "halulu_api",
                table: "quotes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            // Update existing integer values to string values
            migrationBuilder.Sql(@"
                UPDATE halulu_api.quotes 
                SET status = CASE 
                    WHEN status = '0' THEN 'Pending'
                    WHEN status = '1' THEN 'Accepted'
                    WHEN status = '2' THEN 'Rejected'
                    WHEN status = '3' THEN 'Expired'
                    ELSE 'Pending'
                END;
            ");

            // Ensure all records have a status
            migrationBuilder.Sql(@"
                UPDATE halulu_api.quotes 
                SET status = 'Pending' 
                WHERE status IS NULL OR status = '';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Convert string values back to integers
            migrationBuilder.Sql(@"
                UPDATE halulu_api.quotes 
                SET status = CASE 
                    WHEN status = 'Pending' THEN '0'
                    WHEN status = 'Accepted' THEN '1'
                    WHEN status = 'Rejected' THEN '2'
                    WHEN status = 'Expired' THEN '3'
                    ELSE '0'
                END;
            ");

            // Convert status column back to integer
            migrationBuilder.AlterColumn<int>(
                name: "status",
                schema: "halulu_api",
                table: "quotes",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Pending");
        }
    }
}