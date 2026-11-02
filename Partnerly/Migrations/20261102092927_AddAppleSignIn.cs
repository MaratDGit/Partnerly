using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Partnerly.Migrations
{
    public partial class AddAppleSignIn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppleId",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableAppleLogin",
                table: "SystemSettings",
                type: "bit",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE [SystemSettings]
SET [EnableAppleLogin] = COALESCE([EnableAppleLogin], 1);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppleId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EnableAppleLogin",
                table: "SystemSettings");
        }
    }
}
