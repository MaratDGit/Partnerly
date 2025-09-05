using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Partnerly.Migrations
{
    public partial class createReuiredRecords : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("305d4b8c-2bed-4686-8564-78465fd4cbd3"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("4ce53a54-c1af-4a68-88af-7b6c543f7a58"));

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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsDeleted", "Name", "Type", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("4ce53a54-c1af-4a68-88af-7b6c543f7a58"), new Guid("305d4b8c-2bed-4686-8564-78465fd4cbd3"), new DateTime(2025, 9, 4, 15, 44, 50, 198, DateTimeKind.Utc).AddTicks(3239), false, "Administrator", "D", new Guid("305d4b8c-2bed-4686-8564-78465fd4cbd3"), new DateTime(2025, 9, 4, 15, 44, 50, 198, DateTimeKind.Utc).AddTicks(3240) });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Balance", "CreatedBy", "CreatedDate", "Email", "FirstName", "IsDeleted", "LastName", "PasswordHash", "Phone", "PhotoUrl", "ReferrerId", "RoleId", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("305d4b8c-2bed-4686-8564-78465fd4cbd3"), 0m, new Guid("305d4b8c-2bed-4686-8564-78465fd4cbd3"), new DateTime(2025, 9, 4, 15, 44, 50, 327, DateTimeKind.Utc).AddTicks(1422), "marat.iigservices@gmail.com", "Marat", false, "Danielyan", "$2a$11$eLNv5tU76WPYs1BjLzMW8uUEpcpSZh36DOzCOy6oITTy3IscmxXdC", "+37497111312", null, null, new Guid("4ce53a54-c1af-4a68-88af-7b6c543f7a58"), new Guid("305d4b8c-2bed-4686-8564-78465fd4cbd3"), new DateTime(2025, 9, 4, 15, 44, 50, 327, DateTimeKind.Utc).AddTicks(1427) });
        }
    }
}
