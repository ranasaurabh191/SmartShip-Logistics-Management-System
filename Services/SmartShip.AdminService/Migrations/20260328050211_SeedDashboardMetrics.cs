using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartShip.AdminService.Migrations
{
    /// <inheritdoc />
    public partial class SeedDashboardMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DashboardMetrics",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastUpdatedAt",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DashboardMetrics",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastUpdatedAt",
                value: new DateTime(2026, 3, 28, 5, 1, 57, 310, DateTimeKind.Utc).AddTicks(9631));
        }
    }
}
