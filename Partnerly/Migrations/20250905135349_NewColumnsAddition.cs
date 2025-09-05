using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Partnerly.Migrations
{
    public partial class NewColumnsAddition : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Logs_Users_UserId",
                table: "Logs");

            migrationBuilder.DropIndex(
                name: "IX_Logs_UserId",
                table: "Logs");

            migrationBuilder.DeleteData(
                table: "Logs",
                keyColumn: "Id",
                keyValue: new Guid("c0869b53-fb1d-41f2-9ba9-dafdaade1381"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("49ed5a66-83a2-4f42-a1db-e447e8a4b23f"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("dfd999ec-f0d3-4337-865f-66d5437356a2"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("b8b2fe83-92ac-4214-ad53-606690348a65"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("df80c70b-5b1d-43bb-b807-f8a37b83ccd9"));

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Logs",
                newName: "CreatorUserId");

            migrationBuilder.AddColumn<bool>(
                name: "IsOnlayn",
                table: "Users",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSignInDate",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LogCreatorId",
                table: "Logs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogMessage",
                table: "Logs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Logs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "Logs",
                columns: new[] { "Id", "Action", "CreatedBy", "CreatedDate", "CreatorUserId", "IsDeleted", "LogCreatorId", "LogMessage", "Type", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("2da1928c-0e1e-4f27-becf-b9dbac18e256"), "UC", new Guid("fe004104-dea8-48fb-a6f5-3ce0fc1b578b"), new DateTime(2025, 9, 5, 13, 53, 49, 282, DateTimeKind.Utc).AddTicks(3164), new Guid("fe004104-dea8-48fb-a6f5-3ce0fc1b578b"), false, null, "Created the Admin user from OnModelCreating", "I", new Guid("fe004104-dea8-48fb-a6f5-3ce0fc1b578b"), new DateTime(2025, 9, 5, 13, 53, 49, 282, DateTimeKind.Utc).AddTicks(3165) });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsDeleted", "Name", "Type", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { new Guid("af849193-6e77-4cf7-a853-046499493679"), new Guid("fe004104-dea8-48fb-a6f5-3ce0fc1b578b"), new DateTime(2025, 9, 5, 13, 53, 49, 282, DateTimeKind.Utc).AddTicks(3000), false, "User", "V", new Guid("fe004104-dea8-48fb-a6f5-3ce0fc1b578b"), new DateTime(2025, 9, 5, 13, 53, 49, 282, DateTimeKind.Utc).AddTicks(3000) },
                    { new Guid("b450483f-413f-43fe-b5c4-9131bf511860"), new Guid("fe004104-dea8-48fb-a6f5-3ce0fc1b578b"), new DateTime(2025, 9, 5, 13, 53, 49, 282, DateTimeKind.Utc).AddTicks(2997), false, "Employee", "U", new Guid("fe004104-dea8-48fb-a6f5-3ce0fc1b578b"), new DateTime(2025, 9, 5, 13, 53, 49, 282, DateTimeKind.Utc).AddTicks(2998) },
                    { new Guid("fe366bdb-6092-4bcc-b604-8032eca2bdd9"), new Guid("fe004104-dea8-48fb-a6f5-3ce0fc1b578b"), new DateTime(2025, 9, 5, 13, 53, 49, 282, DateTimeKind.Utc).AddTicks(2992), false, "Administrator", "D", new Guid("fe004104-dea8-48fb-a6f5-3ce0fc1b578b"), new DateTime(2025, 9, 5, 13, 53, 49, 282, DateTimeKind.Utc).AddTicks(2993) }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Balance", "CreatedBy", "CreatedDate", "Email", "FirstName", "IsDeleted", "IsOnlayn", "LastName", "LastSignInDate", "PasswordHash", "Phone", "PhotoUrl", "ReferrerId", "RoleId", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("fe004104-dea8-48fb-a6f5-3ce0fc1b578b"), 0m, new Guid("fe004104-dea8-48fb-a6f5-3ce0fc1b578b"), new DateTime(2025, 9, 5, 13, 53, 49, 282, DateTimeKind.Utc).AddTicks(2723), "marat.iigservices@gmail.com", "Marat", false, null, "Danielyan", null, "$2a$11$3pm39sNx0jJoAJFe4WK4uuvF80Rt5ybHWJ30EBpq2NAea1z5ymQHK", "+37497111312", null, null, new Guid("fe366bdb-6092-4bcc-b604-8032eca2bdd9"), new Guid("fe004104-dea8-48fb-a6f5-3ce0fc1b578b"), new DateTime(2025, 9, 5, 13, 53, 49, 282, DateTimeKind.Utc).AddTicks(2732) });

            migrationBuilder.CreateIndex(
                name: "IX_Logs_LogCreatorId",
                table: "Logs",
                column: "LogCreatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Logs_Users_LogCreatorId",
                table: "Logs",
                column: "LogCreatorId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Logs_Users_LogCreatorId",
                table: "Logs");

            migrationBuilder.DropIndex(
                name: "IX_Logs_LogCreatorId",
                table: "Logs");

            migrationBuilder.DeleteData(
                table: "Logs",
                keyColumn: "Id",
                keyValue: new Guid("2da1928c-0e1e-4f27-becf-b9dbac18e256"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("af849193-6e77-4cf7-a853-046499493679"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("b450483f-413f-43fe-b5c4-9131bf511860"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("fe004104-dea8-48fb-a6f5-3ce0fc1b578b"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("fe366bdb-6092-4bcc-b604-8032eca2bdd9"));

            migrationBuilder.DropColumn(
                name: "IsOnlayn",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastSignInDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LogCreatorId",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "LogMessage",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Logs");

            migrationBuilder.RenameColumn(
                name: "CreatorUserId",
                table: "Logs",
                newName: "UserId");

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsDeleted", "Name", "Type", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("49ed5a66-83a2-4f42-a1db-e447e8a4b23f"), new Guid("b8b2fe83-92ac-4214-ad53-606690348a65"), new DateTime(2025, 9, 5, 9, 14, 12, 112, DateTimeKind.Utc).AddTicks(5316), false, "Employee", "U", new Guid("b8b2fe83-92ac-4214-ad53-606690348a65"), new DateTime(2025, 9, 5, 9, 14, 12, 112, DateTimeKind.Utc).AddTicks(5316) });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsDeleted", "Name", "Type", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("df80c70b-5b1d-43bb-b807-f8a37b83ccd9"), new Guid("b8b2fe83-92ac-4214-ad53-606690348a65"), new DateTime(2025, 9, 5, 9, 14, 12, 112, DateTimeKind.Utc).AddTicks(5309), false, "Administrator", "D", new Guid("b8b2fe83-92ac-4214-ad53-606690348a65"), new DateTime(2025, 9, 5, 9, 14, 12, 112, DateTimeKind.Utc).AddTicks(5310) });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsDeleted", "Name", "Type", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("dfd999ec-f0d3-4337-865f-66d5437356a2"), new Guid("b8b2fe83-92ac-4214-ad53-606690348a65"), new DateTime(2025, 9, 5, 9, 14, 12, 112, DateTimeKind.Utc).AddTicks(5318), false, "User", "V", new Guid("b8b2fe83-92ac-4214-ad53-606690348a65"), new DateTime(2025, 9, 5, 9, 14, 12, 112, DateTimeKind.Utc).AddTicks(5318) });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Balance", "CreatedBy", "CreatedDate", "Email", "FirstName", "IsDeleted", "LastName", "PasswordHash", "Phone", "PhotoUrl", "ReferrerId", "RoleId", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("b8b2fe83-92ac-4214-ad53-606690348a65"), 0m, new Guid("b8b2fe83-92ac-4214-ad53-606690348a65"), new DateTime(2025, 9, 5, 9, 14, 12, 112, DateTimeKind.Utc).AddTicks(4868), "marat.iigservices@gmail.com", "Marat", false, "Danielyan", "$2a$11$7qHZAmH9jYIufxpUFCHvqOZfJh6Bmbl/tkggdJUYq/3dBIPh02Bcm", "+37497111312", null, null, new Guid("df80c70b-5b1d-43bb-b807-f8a37b83ccd9"), new Guid("b8b2fe83-92ac-4214-ad53-606690348a65"), new DateTime(2025, 9, 5, 9, 14, 12, 112, DateTimeKind.Utc).AddTicks(4872) });

            migrationBuilder.InsertData(
                table: "Logs",
                columns: new[] { "Id", "Action", "CreatedBy", "CreatedDate", "IsDeleted", "UpdatedBy", "UpdatedDate", "UserId" },
                values: new object[] { new Guid("c0869b53-fb1d-41f2-9ba9-dafdaade1381"), "UC", new Guid("b8b2fe83-92ac-4214-ad53-606690348a65"), new DateTime(2025, 9, 5, 9, 14, 12, 112, DateTimeKind.Utc).AddTicks(5488), false, new Guid("b8b2fe83-92ac-4214-ad53-606690348a65"), new DateTime(2025, 9, 5, 9, 14, 12, 112, DateTimeKind.Utc).AddTicks(5490), new Guid("b8b2fe83-92ac-4214-ad53-606690348a65") });

            migrationBuilder.CreateIndex(
                name: "IX_Logs_UserId",
                table: "Logs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Logs_Users_UserId",
                table: "Logs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
