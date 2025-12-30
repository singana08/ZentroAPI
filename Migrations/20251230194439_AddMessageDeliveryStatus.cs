using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZentroAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageDeliveryStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                schema: "zentro_api",
                table: "messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDelivered",
                schema: "zentro_api",
                table: "messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                schema: "zentro_api",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "IsDelivered",
                schema: "zentro_api",
                table: "messages");
        }
    }
}
