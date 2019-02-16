using Microsoft.EntityFrameworkCore.Migrations;

namespace CA2_Web.Data.Migrations
{
    public partial class ExternalTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    RoomId = table.Column<string>(nullable: true),
                    BookedById = table.Column<string>(nullable: true),
                    BookedByName = table.Column<string>(nullable: true),
                    SupervisedById = table.Column<string>(nullable: true),
                    SupervisedByName = table.Column<string>(nullable: true),
                    BookDate = table.Column<string>(nullable: true),
                    StartHour = table.Column<int>(nullable: false),
                    EndHour = table.Column<int>(nullable: false),
                    Expired = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    Image = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    DeviceId = table.Column<string>(nullable: true),
                    LocationId = table.Column<string>(nullable: true),
                    LocationName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "Rooms");
        }
    }
}
