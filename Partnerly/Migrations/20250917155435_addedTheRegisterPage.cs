using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Partnerly.Migrations
{
    public partial class addedTheRegisterPage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Logs",
                keyColumn: "Id",
                keyValue: new Guid("763d662a-3a06-4644-8988-208f31481c31"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("25f7559c-c56b-4c10-865d-e9dea5855523"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("ef838706-9d76-4589-bc83-31532a2c2944"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("e427ea19-fdb9-45f8-af0f-c7ddbcbc2b3a"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a7feaaed-70c4-4040-b42b-5dffba322c44"));

            migrationBuilder.AddColumn<string>(
                name: "MyReferralCode",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "Logs",
                columns: new[] { "Id", "Action", "CreatedBy", "CreatedDate", "CreatorUserId", "IsDeleted", "LogCreatorId", "LogMessage", "Type", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("324d7b45-0da7-4acb-a853-b6dc9d0ab839"), "UC", new Guid("cbe20e5c-9412-44d7-9432-56079f499766"), new DateTime(2025, 9, 17, 15, 54, 34, 700, DateTimeKind.Utc).AddTicks(8255), new Guid("cbe20e5c-9412-44d7-9432-56079f499766"), false, null, "Created the Admin user from OnModelCreating", "I", new Guid("cbe20e5c-9412-44d7-9432-56079f499766"), new DateTime(2025, 9, 17, 15, 54, 34, 700, DateTimeKind.Utc).AddTicks(8255) });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsDeleted", "Name", "Type", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { new Guid("1c04bf7c-7053-48d1-b11e-bda5066fbb03"), new Guid("cbe20e5c-9412-44d7-9432-56079f499766"), new DateTime(2025, 9, 17, 15, 54, 34, 700, DateTimeKind.Utc).AddTicks(8054), false, "User", "V", new Guid("cbe20e5c-9412-44d7-9432-56079f499766"), new DateTime(2025, 9, 17, 15, 54, 34, 700, DateTimeKind.Utc).AddTicks(8054) },
                    { new Guid("bcc61dc2-4fdc-4b11-af74-4d2dceb327b6"), new Guid("cbe20e5c-9412-44d7-9432-56079f499766"), new DateTime(2025, 9, 17, 15, 54, 34, 700, DateTimeKind.Utc).AddTicks(8052), false, "Employee", "U", new Guid("cbe20e5c-9412-44d7-9432-56079f499766"), new DateTime(2025, 9, 17, 15, 54, 34, 700, DateTimeKind.Utc).AddTicks(8052) },
                    { new Guid("bfcaef66-fddd-4d74-980f-6bac8d653110"), new Guid("cbe20e5c-9412-44d7-9432-56079f499766"), new DateTime(2025, 9, 17, 15, 54, 34, 700, DateTimeKind.Utc).AddTicks(8043), false, "Administrator", "D", new Guid("cbe20e5c-9412-44d7-9432-56079f499766"), new DateTime(2025, 9, 17, 15, 54, 34, 700, DateTimeKind.Utc).AddTicks(8044) }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Balance", "CreatedBy", "CreatedDate", "Email", "FirstName", "IsBlocked", "IsDeleted", "IsOnlayn", "LastName", "LastSignInDate", "MyReferralCode", "PasswordHash", "Phone", "PhotoUrl", "ReferrerId", "RoleId", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("cbe20e5c-9412-44d7-9432-56079f499766"), 0m, new Guid("cbe20e5c-9412-44d7-9432-56079f499766"), new DateTime(2025, 9, 17, 15, 54, 34, 700, DateTimeKind.Utc).AddTicks(7756), "marat.iigservices@gmail.com", "Marat", false, false, null, "Danielyan", null, "BRANCH111", "$2a$11$b68lYj7X7bmn5QzXoNj8u.qGLQ5HnbzQBxlsUM09l/m1/4C/WRjke", "+37497111312", null, null, new Guid("bfcaef66-fddd-4d74-980f-6bac8d653110"), new Guid("cbe20e5c-9412-44d7-9432-56079f499766"), new DateTime(2025, 9, 17, 15, 54, 34, 700, DateTimeKind.Utc).AddTicks(7761) });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Logs",
                keyColumn: "Id",
                keyValue: new Guid("324d7b45-0da7-4acb-a853-b6dc9d0ab839"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("1c04bf7c-7053-48d1-b11e-bda5066fbb03"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("bcc61dc2-4fdc-4b11-af74-4d2dceb327b6"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("cbe20e5c-9412-44d7-9432-56079f499766"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("bfcaef66-fddd-4d74-980f-6bac8d653110"));

            migrationBuilder.DropColumn(
                name: "MyReferralCode",
                table: "Users");

            migrationBuilder.InsertData(
                table: "Logs",
                columns: new[] { "Id", "Action", "CreatedBy", "CreatedDate", "CreatorUserId", "IsDeleted", "LogCreatorId", "LogMessage", "Type", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("763d662a-3a06-4644-8988-208f31481c31"), "UC", new Guid("e427ea19-fdb9-45f8-af0f-c7ddbcbc2b3a"), new DateTime(2025, 9, 16, 15, 14, 55, 683, DateTimeKind.Utc).AddTicks(3975), new Guid("e427ea19-fdb9-45f8-af0f-c7ddbcbc2b3a"), false, null, "Created the Admin user from OnModelCreating", "I", new Guid("e427ea19-fdb9-45f8-af0f-c7ddbcbc2b3a"), new DateTime(2025, 9, 16, 15, 14, 55, 683, DateTimeKind.Utc).AddTicks(3976) });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsDeleted", "Name", "Type", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { new Guid("25f7559c-c56b-4c10-865d-e9dea5855523"), new Guid("e427ea19-fdb9-45f8-af0f-c7ddbcbc2b3a"), new DateTime(2025, 9, 16, 15, 14, 55, 683, DateTimeKind.Utc).AddTicks(3759), false, "User", "V", new Guid("e427ea19-fdb9-45f8-af0f-c7ddbcbc2b3a"), new DateTime(2025, 9, 16, 15, 14, 55, 683, DateTimeKind.Utc).AddTicks(3759) },
                    { new Guid("a7feaaed-70c4-4040-b42b-5dffba322c44"), new Guid("e427ea19-fdb9-45f8-af0f-c7ddbcbc2b3a"), new DateTime(2025, 9, 16, 15, 14, 55, 683, DateTimeKind.Utc).AddTicks(3753), false, "Administrator", "D", new Guid("e427ea19-fdb9-45f8-af0f-c7ddbcbc2b3a"), new DateTime(2025, 9, 16, 15, 14, 55, 683, DateTimeKind.Utc).AddTicks(3754) },
                    { new Guid("ef838706-9d76-4589-bc83-31532a2c2944"), new Guid("e427ea19-fdb9-45f8-af0f-c7ddbcbc2b3a"), new DateTime(2025, 9, 16, 15, 14, 55, 683, DateTimeKind.Utc).AddTicks(3757), false, "Employee", "U", new Guid("e427ea19-fdb9-45f8-af0f-c7ddbcbc2b3a"), new DateTime(2025, 9, 16, 15, 14, 55, 683, DateTimeKind.Utc).AddTicks(3758) }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Balance", "CreatedBy", "CreatedDate", "Email", "FirstName", "IsBlocked", "IsDeleted", "IsOnlayn", "LastName", "LastSignInDate", "PasswordHash", "Phone", "PhotoUrl", "ReferrerId", "RoleId", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("e427ea19-fdb9-45f8-af0f-c7ddbcbc2b3a"), 0m, new Guid("e427ea19-fdb9-45f8-af0f-c7ddbcbc2b3a"), new DateTime(2025, 9, 16, 15, 14, 55, 683, DateTimeKind.Utc).AddTicks(3459), "marat.iigservices@gmail.com", "Marat", false, false, null, "Danielyan", null, "$2a$11$U.RlR6wIkTJVZf66z5qOi.hY3su4z57rvq8sdjR8C7OQUaHfpqUIS", "+37497111312", null, null, new Guid("a7feaaed-70c4-4040-b42b-5dffba322c44"), new Guid("e427ea19-fdb9-45f8-af0f-c7ddbcbc2b3a"), new DateTime(2025, 9, 16, 15, 14, 55, 683, DateTimeKind.Utc).AddTicks(3464) });
        }
    }
}
