using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartShip.PaymentService.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueTrackingNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_TrackingNumber",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TrackingNumber",
                table: "Payments",
                column: "TrackingNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_TrackingNumber",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TrackingNumber",
                table: "Payments",
                column: "TrackingNumber");
        }
    }
}
