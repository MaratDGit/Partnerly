using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Partnerly.Migrations
{
    public partial class addedMaintenanceMode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMaintenanceMode",
                table: "SystemSettings",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMaintenanceMode",
                table: "SystemSettings");
        }
    }
}
