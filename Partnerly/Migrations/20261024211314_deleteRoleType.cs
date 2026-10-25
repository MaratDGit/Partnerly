using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Partnerly.Migrations
{
    public partial class deleteRoleType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Roles");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Roles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
