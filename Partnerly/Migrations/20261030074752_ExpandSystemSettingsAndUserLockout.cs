using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Partnerly.Migrations
{
    public partial class ExpandSystemSettingsAndUserLockout : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessFailedCount",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEnd",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminNotificationEmail",
                table: "SystemSettings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllowedAttachmentExtensions",
                table: "SystemSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationName",
                table: "SystemSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DefaultAllowSendEmails",
                table: "SystemSettings",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DefaultAllowSendNotifications",
                table: "SystemSettings",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultLanguage",
                table: "SystemSettings",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultNewUserRoleId",
                table: "SystemSettings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailAdminOnNewRegistration",
                table: "SystemSettings",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailAdminOnNewTicket",
                table: "SystemSettings",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableEmailSending",
                table: "SystemSettings",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableGoogleLogin",
                table: "SystemSettings",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableNotifications",
                table: "SystemSettings",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAiAssistantEnabled",
                table: "SystemSettings",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LockoutDurationMinutes",
                table: "SystemSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaintenanceMessage",
                table: "SystemSettings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxAttachmentSizeMb",
                table: "SystemSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxFailedLoginAttempts",
                table: "SystemSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxProfilePhotoSizeMb",
                table: "SystemSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PersistentCookieDays",
                table: "SystemSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionExpireMinutes",
                table: "SystemSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TicketAutoAssignRoleId",
                table: "SystemSettings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TicketAutoAssignUserId",
                table: "SystemSettings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "SystemSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE [SystemSettings]
SET
    [ApplicationName] = COALESCE([ApplicationName], N'Belote'),
    [DefaultLanguage] = COALESCE([DefaultLanguage], N'en'),
    [TimeZoneId] = COALESCE([TimeZoneId], N'Asia/Yerevan'),
    [IsAiAssistantEnabled] = COALESCE([IsAiAssistantEnabled], 1),
    [EnableGoogleLogin] = COALESCE([EnableGoogleLogin], 1),
    [PersistentCookieDays] = COALESCE([PersistentCookieDays], 14),
    [SessionExpireMinutes] = COALESCE([SessionExpireMinutes], 60),
    [MaxProfilePhotoSizeMb] = COALESCE([MaxProfilePhotoSizeMb], 5),
    [MaxFailedLoginAttempts] = COALESCE([MaxFailedLoginAttempts], 0),
    [LockoutDurationMinutes] = COALESCE([LockoutDurationMinutes], 15),
    [EnableEmailSending] = COALESCE([EnableEmailSending], 1),
    [EnableNotifications] = COALESCE([EnableNotifications], 1),
    [EmailAdminOnNewRegistration] = COALESCE([EmailAdminOnNewRegistration], 0),
    [EmailAdminOnNewTicket] = COALESCE([EmailAdminOnNewTicket], 0),
    [DefaultAllowSendEmails] = COALESCE([DefaultAllowSendEmails], 1),
    [DefaultAllowSendNotifications] = COALESCE([DefaultAllowSendNotifications], 1),
    [MaxAttachmentSizeMb] = COALESCE([MaxAttachmentSizeMb], 10),
    [AllowedAttachmentExtensions] = COALESCE([AllowedAttachmentExtensions], N'jpg,jpeg,png,gif,webp,pdf,doc,docx,xls,xlsx,txt,zip')
WHERE [Id] = 1;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessFailedCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AdminNotificationEmail",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "AllowedAttachmentExtensions",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "ApplicationName",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "DefaultAllowSendEmails",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "DefaultAllowSendNotifications",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "DefaultLanguage",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "DefaultNewUserRoleId",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "EmailAdminOnNewRegistration",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "EmailAdminOnNewTicket",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "EnableEmailSending",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "EnableGoogleLogin",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "EnableNotifications",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "IsAiAssistantEnabled",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "LockoutDurationMinutes",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "MaintenanceMessage",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "MaxAttachmentSizeMb",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "MaxFailedLoginAttempts",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "MaxProfilePhotoSizeMb",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "PersistentCookieDays",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SessionExpireMinutes",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "TicketAutoAssignRoleId",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "TicketAutoAssignUserId",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "SystemSettings");
        }
    }
}
