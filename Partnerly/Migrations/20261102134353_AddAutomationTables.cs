using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Partnerly.Migrations
{
    public partial class AddAutomationTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutomationConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EncryptedPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomationConnections_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AutomationTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    GraphJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomationTemplates_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AutomationRunSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Attempt = table.Column<int>(type: "int", nullable: false),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationRunSteps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Automations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    PublishedVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DraftVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaxConcurrentRuns = table.Column<int>(type: "int", nullable: false),
                    MaxRunsPerHour = table.Column<int>(type: "int", nullable: false),
                    StepTimeoutSeconds = table.Column<int>(type: "int", nullable: false),
                    RunTimeoutSeconds = table.Column<int>(type: "int", nullable: false),
                    RetryLimit = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Automations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Automations_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AutomationVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AutomationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    GraphJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GraphSchemaVersion = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomationVersions_Automations_AutomationId",
                        column: x => x.AutomationId,
                        principalTable: "Automations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AutomationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AutomationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TriggerType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TriggerReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsDryRun = table.Column<bool>(type: "bit", nullable: false),
                    Depth = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ContextJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChosenPortsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompletedNodeIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HeartbeatAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomationRuns_Automations_AutomationId",
                        column: x => x.AutomationId,
                        principalTable: "Automations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AutomationRuns_AutomationVersions_VersionId",
                        column: x => x.VersionId,
                        principalTable: "AutomationVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AutomationRuns_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AutomationTriggers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WebhookPublicId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    WebhookSecretHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationTriggers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomationTriggers_AutomationVersions_VersionId",
                        column: x => x.VersionId,
                        principalTable: "AutomationVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AutomationWakeups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AutomationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TriggerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NodeId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LeaseUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LeaseOwner = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Attempt = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationWakeups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomationWakeups_AutomationRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "AutomationRuns",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AutomationWakeups_Automations_AutomationId",
                        column: x => x.AutomationId,
                        principalTable: "Automations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AutomationWakeups_AutomationTriggers_TriggerId",
                        column: x => x.TriggerId,
                        principalTable: "AutomationTriggers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AutomationWakeups_AutomationVersions_VersionId",
                        column: x => x.VersionId,
                        principalTable: "AutomationVersions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationConnections_OwnerUserId_IsDeleted",
                table: "AutomationConnections",
                columns: new[] { "OwnerUserId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationRuns_AutomationId_Status",
                table: "AutomationRuns",
                columns: new[] { "AutomationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationRuns_IdempotencyKey",
                table: "AutomationRuns",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationRuns_OwnerUserId_CreatedDate",
                table: "AutomationRuns",
                columns: new[] { "OwnerUserId", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationRuns_VersionId",
                table: "AutomationRuns",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationRunSteps_RunId_NodeId_Attempt",
                table: "AutomationRunSteps",
                columns: new[] { "RunId", "NodeId", "Attempt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Automations_DraftVersionId",
                table: "Automations",
                column: "DraftVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Automations_IsActive_IsDeleted",
                table: "Automations",
                columns: new[] { "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Automations_OwnerUserId_IsDeleted",
                table: "Automations",
                columns: new[] { "OwnerUserId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Automations_PublishedVersionId",
                table: "Automations",
                column: "PublishedVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationTemplates_OwnerUserId",
                table: "AutomationTemplates",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationTriggers_Type_IsEnabled",
                table: "AutomationTriggers",
                columns: new[] { "Type", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationTriggers_VersionId",
                table: "AutomationTriggers",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationTriggers_WebhookPublicId",
                table: "AutomationTriggers",
                column: "WebhookPublicId",
                unique: true,
                filter: "[WebhookPublicId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationVersions_AutomationId_Number",
                table: "AutomationVersions",
                columns: new[] { "AutomationId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutomationWakeups_AutomationId",
                table: "AutomationWakeups",
                column: "AutomationId");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationWakeups_RunId",
                table: "AutomationWakeups",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationWakeups_Status_DueAtUtc",
                table: "AutomationWakeups",
                columns: new[] { "Status", "DueAtUtc" })
                .Annotation("SqlServer:Include", new[] { "LeaseUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationWakeups_TriggerId",
                table: "AutomationWakeups",
                column: "TriggerId");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationWakeups_VersionId",
                table: "AutomationWakeups",
                column: "VersionId");

            migrationBuilder.AddForeignKey(
                name: "FK_AutomationRunSteps_AutomationRuns_RunId",
                table: "AutomationRunSteps",
                column: "RunId",
                principalTable: "AutomationRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Automations_AutomationVersions_DraftVersionId",
                table: "Automations",
                column: "DraftVersionId",
                principalTable: "AutomationVersions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Automations_AutomationVersions_PublishedVersionId",
                table: "Automations",
                column: "PublishedVersionId",
                principalTable: "AutomationVersions",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AutomationVersions_Automations_AutomationId",
                table: "AutomationVersions");

            migrationBuilder.DropTable(
                name: "AutomationConnections");

            migrationBuilder.DropTable(
                name: "AutomationRunSteps");

            migrationBuilder.DropTable(
                name: "AutomationTemplates");

            migrationBuilder.DropTable(
                name: "AutomationWakeups");

            migrationBuilder.DropTable(
                name: "AutomationRuns");

            migrationBuilder.DropTable(
                name: "AutomationTriggers");

            migrationBuilder.DropTable(
                name: "Automations");

            migrationBuilder.DropTable(
                name: "AutomationVersions");
        }
    }
}
