using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartShip.AdminService.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DashboardMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TotalShipments = table.Column<int>(type: "int", nullable: false),
                    ActiveShipments = table.Column<int>(type: "int", nullable: false),
                    DeliveredToday = table.Column<int>(type: "int", nullable: false),
                    Exceptions = table.Column<int>(type: "int", nullable: false),
                    TotalCustomers = table.Column<int>(type: "int", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardMetrics", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "DashboardMetrics",
                columns: new[] { "Id", "ActiveShipments", "DeliveredToday", "Exceptions", "LastUpdatedAt", "TotalCustomers", "TotalShipments" },
                values: new object[] { 1, 0, 0, 0, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DashboardMetrics");
        }
    }
}
