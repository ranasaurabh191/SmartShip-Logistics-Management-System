using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartShip.ShipmentService.Migrations
{
    /// <inheritdoc />
    public partial class Addpayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DistanceKm",
                table: "Shipments",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DistanceKm",
                table: "Shipments");
        }
    }
}
