using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Partnerly.Migrations
{
    public partial class AddedTheLogin : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "Users",
                type: "bit",
                nullable: true);

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

        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "Users");

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
        }
    }
}
