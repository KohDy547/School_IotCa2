using Microsoft.EntityFrameworkCore.Migrations;

namespace CA2_Web.Data.Migrations
{
    public partial class ExternalTables_02 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "UserProperties",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "UserProperties");
        }
    }
}
