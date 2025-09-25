using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Partnerly.Migrations
{
    public partial class updateusertableOnlineStatusAutoRefreshMinute : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OnlineStatusAutoRefreshMinute",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnlineStatusAutoRefreshMinute",
                table: "SystemSettings");
        }
    }
}
