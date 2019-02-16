using Microsoft.EntityFrameworkCore.Migrations;

namespace CA2_Web.Data.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserProperties",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    AccessLevel = table.Column<int>(nullable: false),
                    RfId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProperties", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserProperties");
        }
    }
}
