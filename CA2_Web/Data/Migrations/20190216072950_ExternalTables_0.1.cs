using Microsoft.EntityFrameworkCore.Migrations;

namespace CA2_Web.Data.Migrations
{
    public partial class ExternalTables_01 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Rooms",
                newName: "Name");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Rooms",
                newName: "Description");
        }
    }
}
