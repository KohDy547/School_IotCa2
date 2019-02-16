using Microsoft.EntityFrameworkCore.Migrations;

namespace CA2_Web.Data.Migrations
{
    public partial class ExternalTables_03 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Image",
                table: "Locations");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "Locations",
                nullable: true);
        }
    }
}
