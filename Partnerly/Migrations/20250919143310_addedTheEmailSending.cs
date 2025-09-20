using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Partnerly.Migrations
{
    public partial class addedTheEmailSending : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Logs",
                keyColumn: "Id",
                keyValue: new Guid("6cce714c-2e75-4fce-bb66-ea5dedec2bae"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11a7101a-5589-433f-9dbf-529aa02dd8da"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("1a7bd6b4-859e-487b-b325-7099d20a2444"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("2070a77f-4b92-4c7a-8ccd-6b995c4da6e0"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("328b7408-573c-47e7-bb70-0a74118bb98c"));

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "Users",
                type: "bit",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Logs",
                columns: new[] { "Id", "Action", "CreatedBy", "CreatedDate", "IsDeleted", "LogMessage", "Type", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("cf75ddd6-7ba6-4c31-a69e-786e712a3a4e"), "UC", new Guid("623a973d-8fca-4044-84e8-5820412b16f7"), new DateTime(2025, 9, 19, 14, 33, 10, 316, DateTimeKind.Utc).AddTicks(4919), false, "Created the Admin user from OnModelCreating", "I", new Guid("623a973d-8fca-4044-84e8-5820412b16f7"), new DateTime(2025, 9, 19, 14, 33, 10, 316, DateTimeKind.Utc).AddTicks(4920) });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsDeleted", "Name", "Type", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { new Guid("5cbd5cb4-6d5f-4f68-99ec-905a8e1bb257"), new Guid("623a973d-8fca-4044-84e8-5820412b16f7"), new DateTime(2025, 9, 19, 14, 33, 10, 316, DateTimeKind.Utc).AddTicks(4752), false, "Administrator", "D", new Guid("623a973d-8fca-4044-84e8-5820412b16f7"), new DateTime(2025, 9, 19, 14, 33, 10, 316, DateTimeKind.Utc).AddTicks(4752) },
                    { new Guid("c17f81c1-5354-46d8-85d7-6d43c34c4b4a"), new Guid("623a973d-8fca-4044-84e8-5820412b16f7"), new DateTime(2025, 9, 19, 14, 33, 10, 316, DateTimeKind.Utc).AddTicks(4762), false, "User", "V", new Guid("623a973d-8fca-4044-84e8-5820412b16f7"), new DateTime(2025, 9, 19, 14, 33, 10, 316, DateTimeKind.Utc).AddTicks(4764) },
                    { new Guid("dd6fa52a-e238-4aaf-bcd8-2131af1029d8"), new Guid("623a973d-8fca-4044-84e8-5820412b16f7"), new DateTime(2025, 9, 19, 14, 33, 10, 316, DateTimeKind.Utc).AddTicks(4759), false, "Employee", "U", new Guid("623a973d-8fca-4044-84e8-5820412b16f7"), new DateTime(2025, 9, 19, 14, 33, 10, 316, DateTimeKind.Utc).AddTicks(4761) }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Balance", "CreatedBy", "CreatedDate", "Email", "EmailConfirmed", "FirstName", "IsBlocked", "IsDeleted", "IsOnlayn", "LastName", "LastSignInDate", "MyReferralCode", "PasswordHash", "Phone", "PhotoUrl", "ReferrerId", "RoleId", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("623a973d-8fca-4044-84e8-5820412b16f7"), null, new Guid("623a973d-8fca-4044-84e8-5820412b16f7"), new DateTime(2025, 9, 19, 14, 33, 10, 316, DateTimeKind.Utc).AddTicks(4313), "marat.iigservices@gmail.com", null, "Marat", false, false, null, "Danielyan", null, "BRANCH111", "$2a$11$N9FxGkrH3wNsmFnGGtG9TO8qlo0MlVq8ZjUEdS4fV85AYqS1Pda.a", "+37497111312", null, null, new Guid("5cbd5cb4-6d5f-4f68-99ec-905a8e1bb257"), new Guid("623a973d-8fca-4044-84e8-5820412b16f7"), new DateTime(2025, 9, 19, 14, 33, 10, 316, DateTimeKind.Utc).AddTicks(4317) });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Logs",
                keyColumn: "Id",
                keyValue: new Guid("cf75ddd6-7ba6-4c31-a69e-786e712a3a4e"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("c17f81c1-5354-46d8-85d7-6d43c34c4b4a"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("dd6fa52a-e238-4aaf-bcd8-2131af1029d8"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("623a973d-8fca-4044-84e8-5820412b16f7"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("5cbd5cb4-6d5f-4f68-99ec-905a8e1bb257"));

            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "Users");

            migrationBuilder.InsertData(
                table: "Logs",
                columns: new[] { "Id", "Action", "CreatedBy", "CreatedDate", "IsDeleted", "LogMessage", "Type", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("6cce714c-2e75-4fce-bb66-ea5dedec2bae"), "UC", new Guid("2070a77f-4b92-4c7a-8ccd-6b995c4da6e0"), new DateTime(2025, 9, 19, 6, 37, 33, 16, DateTimeKind.Utc).AddTicks(9771), false, "Created the Admin user from OnModelCreating", "I", new Guid("2070a77f-4b92-4c7a-8ccd-6b995c4da6e0"), new DateTime(2025, 9, 19, 6, 37, 33, 16, DateTimeKind.Utc).AddTicks(9772) });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsDeleted", "Name", "Type", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { new Guid("11a7101a-5589-433f-9dbf-529aa02dd8da"), new Guid("2070a77f-4b92-4c7a-8ccd-6b995c4da6e0"), new DateTime(2025, 9, 19, 6, 37, 33, 16, DateTimeKind.Utc).AddTicks(9613), false, "Employee", "U", new Guid("2070a77f-4b92-4c7a-8ccd-6b995c4da6e0"), new DateTime(2025, 9, 19, 6, 37, 33, 16, DateTimeKind.Utc).AddTicks(9614) },
                    { new Guid("1a7bd6b4-859e-487b-b325-7099d20a2444"), new Guid("2070a77f-4b92-4c7a-8ccd-6b995c4da6e0"), new DateTime(2025, 9, 19, 6, 37, 33, 16, DateTimeKind.Utc).AddTicks(9615), false, "User", "V", new Guid("2070a77f-4b92-4c7a-8ccd-6b995c4da6e0"), new DateTime(2025, 9, 19, 6, 37, 33, 16, DateTimeKind.Utc).AddTicks(9616) },
                    { new Guid("328b7408-573c-47e7-bb70-0a74118bb98c"), new Guid("2070a77f-4b92-4c7a-8ccd-6b995c4da6e0"), new DateTime(2025, 9, 19, 6, 37, 33, 16, DateTimeKind.Utc).AddTicks(9600), false, "Administrator", "D", new Guid("2070a77f-4b92-4c7a-8ccd-6b995c4da6e0"), new DateTime(2025, 9, 19, 6, 37, 33, 16, DateTimeKind.Utc).AddTicks(9601) }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Balance", "CreatedBy", "CreatedDate", "Email", "FirstName", "IsBlocked", "IsDeleted", "IsOnlayn", "LastName", "LastSignInDate", "MyReferralCode", "PasswordHash", "Phone", "PhotoUrl", "ReferrerId", "RoleId", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("2070a77f-4b92-4c7a-8ccd-6b995c4da6e0"), null, new Guid("2070a77f-4b92-4c7a-8ccd-6b995c4da6e0"), new DateTime(2025, 9, 19, 6, 37, 33, 16, DateTimeKind.Utc).AddTicks(9311), "marat.iigservices@gmail.com", "Marat", false, false, null, "Danielyan", null, "BRANCH111", "$2a$11$5H0pAEJQCYHxrnMZcMNFY.R0vBj/f4CyQby7rZBCLQCXXJt.uroum", "+37497111312", null, null, new Guid("328b7408-573c-47e7-bb70-0a74118bb98c"), new Guid("2070a77f-4b92-4c7a-8ccd-6b995c4da6e0"), new DateTime(2025, 9, 19, 6, 37, 33, 16, DateTimeKind.Utc).AddTicks(9320) });
        }
    }
}
