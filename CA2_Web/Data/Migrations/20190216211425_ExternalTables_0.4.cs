using Microsoft.EntityFrameworkCore.Migrations;

namespace CA2_Web.Data.Migrations
{
    public partial class ExternalTables_04 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Expired",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "StartHour",
                table: "Bookings",
                newName: "TimeSlotId");

            migrationBuilder.RenameColumn(
                name: "EndHour",
                table: "Bookings",
                newName: "Status");

            migrationBuilder.AddColumn<string>(
                name: "TimeSlot",
                table: "Bookings",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeSlot",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "TimeSlotId",
                table: "Bookings",
                newName: "StartHour");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Bookings",
                newName: "EndHour");

            migrationBuilder.AddColumn<bool>(
                name: "Expired",
                table: "Bookings",
                nullable: false,
                defaultValue: false);
        }
    }
}
