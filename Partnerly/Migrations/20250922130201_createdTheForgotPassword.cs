using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Partnerly.Migrations
{
    public partial class createdTheForgotPassword : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EmailTemplates",
                keyColumn: "Id",
                keyValue: new Guid("73630857-0cd2-49bf-83cf-5a823d8b5fb0"));

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

            migrationBuilder.AddColumn<int>(
                name: "ForgotPasswordTokenExpiredAtHours",
                table: "SystemSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "EmailConfirmationTokens",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "EmailConfirmationTokens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiresAt",
                table: "EmailConfirmationTokens",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenType",
                table: "EmailConfirmationTokens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "EmailTemplates",
                columns: new[] { "Id", "AttachmentsMeta", "BodyHtml", "BodyPlain", "CreatedBy", "CreatedDate", "IsDeleted", "Name", "Subject", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { new Guid("1f397aa4-1f04-40d6-8b0d-74851bf29951"), null, "<h2>Здравствуйте, {{UserName}}!</h2>\r\n                                        <p>\r\n                                            Вы запросили сброс пароля для вашей учетной записи.  \r\n                                            Чтобы создать новый пароль, перейдите по ссылке ниже:\r\n                                        </p>\r\n                                        <p>\r\n                                            <a href=\"{ { ResetPasswordLink} }\r\n                                                    \" style=\"display: inline - block; padding: 10px 20px;\r\n                                                    background - color:#0d6efd;color:#fff;text-decoration:none;border-radius:5px;\">\r\n                                               Сбросить пароль\r\n                                            </ a >\r\n                                        </ p >\r\n                                        < p >\r\n                                            Если вы не запрашивали сброс пароля, просто проигнорируйте это письмо.\r\n                                        </p>", null, new Guid("db925908-f3f1-4d0d-b326-8b2a8c97f106"), new DateTime(2025, 9, 22, 13, 2, 0, 652, DateTimeKind.Utc).AddTicks(1883), false, "FP", "Сброс пароля", new Guid("db925908-f3f1-4d0d-b326-8b2a8c97f106"), new DateTime(2025, 9, 22, 13, 2, 0, 652, DateTimeKind.Utc).AddTicks(1884) },
                    { new Guid("8ed8de30-8917-4fde-b6fa-23d770172e1d"), null, "\r\n                            <h2>Здравствуйте, {{UserName}}!</h2>\r\n                            <p>\r\n                                Подтвердите ваш email, перейдя по ссылке:\r\n                                <a href=\"{{ConfirmationLink}}\">Подтвердить</a>\r\n                            </p>", null, new Guid("db925908-f3f1-4d0d-b326-8b2a8c97f106"), new DateTime(2025, 9, 22, 13, 2, 0, 652, DateTimeKind.Utc).AddTicks(1873), false, "EC", "Подтверждение регистрации", new Guid("db925908-f3f1-4d0d-b326-8b2a8c97f106"), new DateTime(2025, 9, 22, 13, 2, 0, 652, DateTimeKind.Utc).AddTicks(1873) }
                });

            migrationBuilder.InsertData(
                table: "Logs",
                columns: new[] { "Id", "Action", "CreatedBy", "CreatedDate", "IsDeleted", "LogMessage", "Type", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("77b166db-cd93-4e80-99dc-59ee5e04f3f5"), "UC", new Guid("db925908-f3f1-4d0d-b326-8b2a8c97f106"), new DateTime(2025, 9, 22, 13, 2, 0, 652, DateTimeKind.Utc).AddTicks(1780), false, "Created the Admin user from OnModelCreating", "I", new Guid("db925908-f3f1-4d0d-b326-8b2a8c97f106"), new DateTime(2025, 9, 22, 13, 2, 0, 652, DateTimeKind.Utc).AddTicks(1781) });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsDeleted", "Name", "Type", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { new Guid("1737942c-cce1-4271-aa32-4704336edd6c"), new Guid("db925908-f3f1-4d0d-b326-8b2a8c97f106"), new DateTime(2025, 9, 22, 13, 2, 0, 652, DateTimeKind.Utc).AddTicks(1595), false, "User", "V", new Guid("db925908-f3f1-4d0d-b326-8b2a8c97f106"), new DateTime(2025, 9, 22, 13, 2, 0, 652, DateTimeKind.Utc).AddTicks(1595) },
                    { new Guid("5a407f76-41a6-425e-85cd-f17d8e3a31f5"), new Guid("db925908-f3f1-4d0d-b326-8b2a8c97f106"), new DateTime(2025, 9, 22, 13, 2, 0, 652, DateTimeKind.Utc).AddTicks(1593), false, "Employee", "U", new Guid("db925908-f3f1-4d0d-b326-8b2a8c97f106"), new DateTime(2025, 9, 22, 13, 2, 0, 652, DateTimeKind.Utc).AddTicks(1593) },
                    { new Guid("690ed1c8-457e-4555-bca7-c9d2fe60a06f"), new Guid("db925908-f3f1-4d0d-b326-8b2a8c97f106"), new DateTime(2025, 9, 22, 13, 2, 0, 652, DateTimeKind.Utc).AddTicks(1582), false, "Administrator", "D", new Guid("db925908-f3f1-4d0d-b326-8b2a8c97f106"), new DateTime(2025, 9, 22, 13, 2, 0, 652, DateTimeKind.Utc).AddTicks(1583) }
                });

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedBy", "CreatedDate", "ForgotPasswordTokenExpiredAtHours", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("db925908-f3f1-4d0d-b326-8b2a8c97f106"), new DateTime(2025, 9, 22, 13, 2, 0, 652, DateTimeKind.Utc).AddTicks(1797), 1, new Guid("db925908-f3f1-4d0d-b326-8b2a8c97f106"), new DateTime(2025, 9, 22, 13, 2, 0, 652, DateTimeKind.Utc).AddTicks(1798) });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Balance", "CreatedBy", "CreatedDate", "Email", "EmailConfirmed", "FirstName", "IsBlocked", "IsDeleted", "IsOnlayn", "LastName", "LastSignInDate", "MyReferralCode", "PasswordHash", "Phone", "PhotoUrl", "ReferrerId", "RoleId", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("db925908-f3f1-4d0d-b326-8b2a8c97f106"), null, new Guid("db925908-f3f1-4d0d-b326-8b2a8c97f106"), new DateTime(2025, 9, 22, 13, 2, 0, 652, DateTimeKind.Utc).AddTicks(1354), "marat.iigservices@gmail.com", true, "Marat", false, false, null, "Danielyan", null, "BRANCH111", "$2a$11$CHagcWFBdOAqd051jphRF.NafXCb5CwoUmoD5O/bxhuu5XkgVdEUO", "+37497111312", null, null, new Guid("690ed1c8-457e-4555-bca7-c9d2fe60a06f"), new Guid("db925908-f3f1-4d0d-b326-8b2a8c97f106"), new DateTime(2025, 9, 22, 13, 2, 0, 652, DateTimeKind.Utc).AddTicks(1359) });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EmailTemplates",
                keyColumn: "Id",
                keyValue: new Guid("1f397aa4-1f04-40d6-8b0d-74851bf29951"));

            migrationBuilder.DeleteData(
                table: "EmailTemplates",
                keyColumn: "Id",
                keyValue: new Guid("8ed8de30-8917-4fde-b6fa-23d770172e1d"));

            migrationBuilder.DeleteData(
                table: "Logs",
                keyColumn: "Id",
                keyValue: new Guid("77b166db-cd93-4e80-99dc-59ee5e04f3f5"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("1737942c-cce1-4271-aa32-4704336edd6c"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("5a407f76-41a6-425e-85cd-f17d8e3a31f5"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("db925908-f3f1-4d0d-b326-8b2a8c97f106"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("690ed1c8-457e-4555-bca7-c9d2fe60a06f"));

            migrationBuilder.DropColumn(
                name: "ForgotPasswordTokenExpiredAtHours",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "TokenType",
                table: "EmailConfirmationTokens");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "EmailConfirmationTokens",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "EmailConfirmationTokens",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiresAt",
                table: "EmailConfirmationTokens",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

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

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("dc9aa449-3278-4816-aaf5-9257e39ff602"), new DateTime(2025, 9, 22, 6, 45, 28, 11, DateTimeKind.Utc).AddTicks(9127), new Guid("dc9aa449-3278-4816-aaf5-9257e39ff602"), new DateTime(2025, 9, 22, 6, 45, 28, 11, DateTimeKind.Utc).AddTicks(9128) });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Balance", "CreatedBy", "CreatedDate", "Email", "EmailConfirmed", "FirstName", "IsBlocked", "IsDeleted", "IsOnlayn", "LastName", "LastSignInDate", "MyReferralCode", "PasswordHash", "Phone", "PhotoUrl", "ReferrerId", "RoleId", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("dc9aa449-3278-4816-aaf5-9257e39ff602"), null, new Guid("dc9aa449-3278-4816-aaf5-9257e39ff602"), new DateTime(2025, 9, 22, 6, 45, 28, 11, DateTimeKind.Utc).AddTicks(8660), "marat.iigservices@gmail.com", true, "Marat", false, false, null, "Danielyan", null, "BRANCH111", "$2a$11$mYttY3r80axkHK4znyLEi.GcCLMlX3GOgJ9/9bg8wQStQGQ4ibTzK", "+37497111312", null, null, new Guid("4efbf269-476c-4a24-abf9-77d9bd019bb1"), new Guid("dc9aa449-3278-4816-aaf5-9257e39ff602"), new DateTime(2025, 9, 22, 6, 45, 28, 11, DateTimeKind.Utc).AddTicks(8666) });
        }
    }
}
