using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartShip.ShipmentService.Migrations
{
    /// <inheritdoc />
    public partial class Addpaymentoption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFragile",
                table: "Shipments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFragile",
                table: "Shipments");
        }
    }
}
