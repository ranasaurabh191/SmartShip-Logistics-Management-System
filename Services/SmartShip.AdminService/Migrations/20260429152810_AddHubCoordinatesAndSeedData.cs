using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartShip.AdminService.Migrations
{
    /// <inheritdoc />
    public partial class AddHubCoordinatesAndSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Hubs",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Hubs",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.UpdateData(
                table: "Hubs",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Latitude", "Longitude" },
                values: new object[] { 28.613900000000001, 77.209000000000003 });

            migrationBuilder.UpdateData(
                table: "Hubs",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Latitude", "Longitude" },
                values: new object[] { 19.076000000000001, 72.877700000000004 });

            migrationBuilder.InsertData(
                table: "Hubs",
                columns: new[] { "Id", "City", "ContactPhone", "Country", "CreatedAt", "IsActive", "Latitude", "Longitude", "Name", "State" },
                values: new object[,]
                {
                    { 101, "Bengaluru", "9800000003", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 12.9716, 77.5946, "Bangalore Hub", "Karnataka" },
                    { 102, "Hyderabad", "9800000004", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 17.385000000000002, 78.486699999999999, "Hyderabad Hub", "Telangana" },
                    { 103, "Chennai", "9800000005", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 13.082700000000001, 80.270700000000005, "Chennai Hub", "Tamil Nadu" },
                    { 104, "Kolkata", "9800000006", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 22.572600000000001, 88.363900000000001, "Kolkata Hub", "West Bengal" },
                    { 105, "Jalandhar", "9800000007", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 31.326000000000001, 75.5762, "Jalandhar Hub", "Punjab" },
                    { 106, "Lucknow", "9800000008", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 26.846699999999998, 80.946200000000005, "Lucknow Hub", "Uttar Pradesh" },
                    { 107, "Pune", "9800000009", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 18.520399999999999, 73.856700000000004, "Pune Hub", "Maharashtra" },
                    { 108, "Ahmedabad", "9800000010", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 23.022500000000001, 72.571399999999997, "Ahmedabad Hub", "Gujarat" },
                    { 109, "Jaipur", "9800000011", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 26.912400000000002, 75.787300000000002, "Jaipur Hub", "Rajasthan" },
                    { 110, "Chandigarh", "9800000012", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 30.7333, 76.779399999999995, "Chandigarh Hub", "Chandigarh" },
                    { 111, "Indore", "9800000013", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 22.7196, 75.857699999999994, "Indore Hub", "Madhya Pradesh" },
                    { 112, "Nagpur", "9800000014", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 21.145800000000001, 79.088200000000001, "Nagpur Hub", "Maharashtra" },
                    { 113, "Patna", "9800000015", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 25.609300000000001, 85.137600000000006, "Patna Hub", "Bihar" },
                    { 114, "Bhopal", "9800000016", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 23.259899999999998, 77.412599999999998, "Bhopal Hub", "Madhya Pradesh" },
                    { 115, "Kochi", "9800000017", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 9.9312000000000005, 76.267300000000006, "Kochi Hub", "Kerala" },
                    { 116, "Guwahati", "9800000018", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 26.144500000000001, 91.736199999999997, "Guwahati Hub", "Assam" },
                    { 117, "Coimbatore", "9800000019", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 11.0168, 76.955799999999996, "Coimbatore Hub", "Tamil Nadu" },
                    { 118, "Visakhapatnam", "9800000020", "India", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 17.686800000000002, 83.218500000000006, "Visakhapatnam Hub", "Andhra Pradesh" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Hubs",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "Hubs",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "Hubs",
                keyColumn: "Id",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "Hubs",
                keyColumn: "Id",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "Hubs",
                keyColumn: "Id",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "Hubs",
                keyColumn: "Id",
                keyValue: 106);

            migrationBuilder.DeleteData(
                table: "Hubs",
                keyColumn: "Id",
                keyValue: 107);

            migrationBuilder.DeleteData(
                table: "Hubs",
                keyColumn: "Id",
                keyValue: 108);

            migrationBuilder.DeleteData(
                table: "Hubs",
                keyColumn: "Id",
                keyValue: 109);

            migrationBuilder.DeleteData(
                table: "Hubs",
                keyColumn: "Id",
                keyValue: 110);

            migrationBuilder.DeleteData(
                table: "Hubs",
                keyColumn: "Id",
                keyValue: 111);

            migrationBuilder.DeleteData(
                table: "Hubs",
                keyColumn: "Id",
                keyValue: 112);

            migrationBuilder.DeleteData(
                table: "Hubs",
                keyColumn: "Id",
                keyValue: 113);

            migrationBuilder.DeleteData(
                table: "Hubs",
                keyColumn: "Id",
                keyValue: 114);

            migrationBuilder.DeleteData(
                table: "Hubs",
                keyColumn: "Id",
                keyValue: 115);

            migrationBuilder.DeleteData(
                table: "Hubs",
                keyColumn: "Id",
                keyValue: 116);

            migrationBuilder.DeleteData(
                table: "Hubs",
                keyColumn: "Id",
                keyValue: 117);

            migrationBuilder.DeleteData(
                table: "Hubs",
                keyColumn: "Id",
                keyValue: 118);

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Hubs");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Hubs");
        }
    }
}
