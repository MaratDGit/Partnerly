using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Partnerly.Migrations
{
    public partial class createdTheEmailServices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.CreateTable(
                name: "EmailAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAttachments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailConfirmationTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Used = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailConfirmationTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BodyHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BodyPlain = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttachmentsMeta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmailConfirmationTokenExpiredAtHours = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "EmailTemplates",
                columns: new[] { "Id", "AttachmentsMeta", "BodyHtml", "BodyPlain", "CreatedBy", "CreatedDate", "IsDeleted", "Name", "Subject", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("73630857-0cd2-49bf-83cf-5a823d8b5fb0"), null, "\r\n                            <h2>Здравствуйте, {{UserName}}!</h2>\r\n                            <p>\r\n                                Подтвердите ваш email, перейдя по ссылке:\r\n                                <a href=\"{{ConfirmationLink}}\">Подтвердить</a>\r\n                            </p>", null, new Guid("dc9aa449-3278-4816-aaf5-9257e39ff602"), new DateTime(2025, 9, 22, 6, 45, 28, 11, DateTimeKind.Utc).AddTicks(9152), false, "EC", "Подтверждение регистрации", new Guid("dc9aa449-3278-4816-aaf5-9257e39ff602"), new DateTime(2025, 9, 22, 6, 45, 28, 11, DateTimeKind.Utc).AddTicks(9152) });

            migrationBuilder.InsertData(
                table: "Logs",
                columns: new[] { "Id", "Action", "CreatedBy", "CreatedDate", "IsDeleted", "LogMessage", "Type", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("dcd670aa-3972-41ed-b718-fc68a292c289"), "UC", new Guid("dc9aa449-3278-4816-aaf5-9257e39ff602"), new DateTime(2025, 9, 22, 6, 45, 28, 11, DateTimeKind.Utc).AddTicks(9113), false, "Created the Admin user from OnModelCreating", "I", new Guid("dc9aa449-3278-4816-aaf5-9257e39ff602"), new DateTime(2025, 9, 22, 6, 45, 28, 11, DateTimeKind.Utc).AddTicks(9113) });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsDeleted", "Name", "Type", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { new Guid("34a008ff-2580-4126-b043-b84819ea7139"), new Guid("dc9aa449-3278-4816-aaf5-9257e39ff602"), new DateTime(2025, 9, 22, 6, 45, 28, 11, DateTimeKind.Utc).AddTicks(8958), false, "Employee", "U", new Guid("dc9aa449-3278-4816-aaf5-9257e39ff602"), new DateTime(2025, 9, 22, 6, 45, 28, 11, DateTimeKind.Utc).AddTicks(8959) },
                    { new Guid("4efbf269-476c-4a24-abf9-77d9bd019bb1"), new Guid("dc9aa449-3278-4816-aaf5-9257e39ff602"), new DateTime(2025, 9, 22, 6, 45, 28, 11, DateTimeKind.Utc).AddTicks(8952), false, "Administrator", "D", new Guid("dc9aa449-3278-4816-aaf5-9257e39ff602"), new DateTime(2025, 9, 22, 6, 45, 28, 11, DateTimeKind.Utc).AddTicks(8953) },
                    { new Guid("a9069bf5-2d6b-4bfa-bed8-0ca011c7346f"), new Guid("dc9aa449-3278-4816-aaf5-9257e39ff602"), new DateTime(2025, 9, 22, 6, 45, 28, 11, DateTimeKind.Utc).AddTicks(8960), false, "User", "V", new Guid("dc9aa449-3278-4816-aaf5-9257e39ff602"), new DateTime(2025, 9, 22, 6, 45, 28, 11, DateTimeKind.Utc).AddTicks(8961) }
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "EmailConfirmationTokenExpiredAtHours", "IsDeleted", "UpdatedBy", "UpdatedDate" },
                values: new object[] { 1, new Guid("dc9aa449-3278-4816-aaf5-9257e39ff602"), new DateTime(2025, 9, 22, 6, 45, 28, 11, DateTimeKind.Utc).AddTicks(9127), 24, false, new Guid("dc9aa449-3278-4816-aaf5-9257e39ff602"), new DateTime(2025, 9, 22, 6, 45, 28, 11, DateTimeKind.Utc).AddTicks(9128) });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Balance", "CreatedBy", "CreatedDate", "Email", "EmailConfirmed", "FirstName", "IsBlocked", "IsDeleted", "IsOnlayn", "LastName", "LastSignInDate", "MyReferralCode", "PasswordHash", "Phone", "PhotoUrl", "ReferrerId", "RoleId", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("dc9aa449-3278-4816-aaf5-9257e39ff602"), null, new Guid("dc9aa449-3278-4816-aaf5-9257e39ff602"), new DateTime(2025, 9, 22, 6, 45, 28, 11, DateTimeKind.Utc).AddTicks(8660), "marat.iigservices@gmail.com", true, "Marat", false, false, null, "Danielyan", null, "BRANCH111", "$2a$11$mYttY3r80axkHK4znyLEi.GcCLMlX3GOgJ9/9bg8wQStQGQ4ibTzK", "+37497111312", null, null, new Guid("4efbf269-476c-4a24-abf9-77d9bd019bb1"), new Guid("dc9aa449-3278-4816-aaf5-9257e39ff602"), new DateTime(2025, 9, 22, 6, 45, 28, 11, DateTimeKind.Utc).AddTicks(8666) });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailAttachments");

            migrationBuilder.DropTable(
                name: "EmailConfirmationTokens");

            migrationBuilder.DropTable(
                name: "EmailTemplates");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DeleteData(
                table: "Logs",
                keyColumn: "Id",
                keyValue: new Guid("dcd670aa-3972-41ed-b718-fc68a292c289"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("34a008ff-2580-4126-b043-b84819ea7139"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a9069bf5-2d6b-4bfa-bed8-0ca011c7346f"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("dc9aa449-3278-4816-aaf5-9257e39ff602"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("4efbf269-476c-4a24-abf9-77d9bd019bb1"));

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
    }
}
