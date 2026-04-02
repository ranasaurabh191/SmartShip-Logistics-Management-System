using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartShip.PaymentService.Migrations
{
    public partial class AddSagaCorrelation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS [SagaCorrelations];");

            migrationBuilder.CreateTable(
                name: "SagaCorrelations",
                columns: table => new
                {
                    ShipmentId = table.Column<int>(nullable: false),  
                    CorrelationId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SagaCorrelations", x => x.ShipmentId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SagaCorrelations");
        }
    }
}