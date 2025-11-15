using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaluluAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAgreementTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                schema: "halulu_api",
                table: "service_requests",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                schema: "halulu_api",
                table: "service_requests",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                schema: "halulu_api",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "Longitude",
                schema: "halulu_api",
                table: "service_requests");
        }
    }
}
