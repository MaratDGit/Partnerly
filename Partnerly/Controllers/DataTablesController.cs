using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Partnerly;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Events.BaseEvents;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Infrastructure.Services;
using Partnerly.Models;
using Partnerly.Models.Authorization;
using Partnerly.Models.DataTable;

namespace Partnerly.Controllers
{
    public class DataTablesController : _BaseController
    {
        private readonly IRoleService _roleService;
        private readonly IUserGroupService _userGroupService;
        private readonly INotificationService _notificationService;
        private readonly ISupportTicketService _supportTicketService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IAttachmentService _attachmentService;
        private readonly IPermissionService _permissionService;

        public DataTablesController(
            IEventBus eventBus,
            IUserService userService,
            ICurrentUserService currentUser,
            ILogService logService,
            IStringLocalizer<SharedResource> localizer,
            ISystemSettingsService systemSettingsService,
            DataTableQueryService tableService,
            IDataTableRegistry dataTableRegistry,
            IRoleService roleService,
            IUserGroupService userGroupService,
            INotificationService notificationService,
            ISupportTicketService supportTicketService,
            IEmailTemplateService emailTemplateService,
            IAttachmentService attachmentService,
            IPermissionService permissionService)
            : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService, tableService, dataTableRegistry)
        {
            _roleService = roleService;
            _userGroupService = userGroupService;
            _notificationService = notificationService;
            _supportTicketService = supportTicketService;
            _emailTemplateService = emailTemplateService;
            _attachmentService = attachmentService;
            _permissionService = permissionService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:datatable.bulk_delete")]
        public async Task<IActionResult> DeleteRecord([FromBody] DataTableInlineDeleteRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TableID))
                return Fail("Table ID is required.");

            var keys = MergeKeys(request.Keys, request.ParentKeys);

            return request.TableID switch
            {
                DataTableNames.usersTable => await DeleteUserAsync(request.Id),
                DataTableNames.rolesTable => await DeleteRoleAsync(request.Id),
                DataTableNames.rolePermissionsTable => await DeleteRolePermissionAsync(request.Id, keys),
                DataTableNames.userPermissionsTable => await DeleteUserPermissionAsync(request.Id, keys),
                DataTableNames.userGroupsTable => await DeleteUserGroupAsync(request.Id),
                DataTableNames.userGroupsMembersTable => await DeleteGroupMemberAsync(keys),
                DataTableNames.notificationsTable => await DeleteNotificationAsync(request.Id),
                DataTableNames.supportTicketsTable => await DeleteSupportTicketAsync(request.Id),
                DataTableNames.emailTemplatesTable => await DeleteEmailTemplateAsync(request.Id),
                DataTableNames.attachmentsTable => await DeleteAttachmentAsync(request.Id),
                DataTableNames.logsTable => await DeleteLogAsync(request.Id),
                _ => Fail(_localizer["DataTableBulkDeleteUnsupported"].Value)
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Permission:datatable.bulk_delete")]
        public async Task<IActionResult> LogBulkDelete([FromBody] DataTableBulkDeleteLogRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TableID) || request.Deleted <= 0)
                return Json(new { success = true });

            var options = _dataTableRegistry.Get(request.TableID);
            var tableName = string.IsNullOrWhiteSpace(options?.Name)
                ? request.TableID
                : options.Name;

            var message = string.Format(
                _localizer["DataTableBulkDeleteLog"].Value,
                tableName,
                request.Selected,
                request.Deleted,
                request.Remaining);

            await _logService.CreateLogAsync(
                LogActionsAttribute.DataTableBulkDeleted,
                LogTypeAttribute.Information,
                message);

            return Json(new { success = true });
        }

        private async Task<IActionResult> DeleteUserAsync(string idValue)
        {
            if (!await HasAsync(PermissionCodes.UsersDelete))
                return Denied();

            if (!TryParseId(idValue, out var id))
                return Fail(string.Format(ErrorMessages.RecordIsNullFromController, "User ID"));

            var user = await _userService.GetUserByIDAsync(id);
            if (user == null)
                return Fail(string.Format(ErrorMessages.Cannotbefound, "User"));

            if (!await CanViewUserAsync(user.Id))
                return Denied();

            return FromResult(await _userService.DeleteUserAsync(id));
        }

        private async Task<IActionResult> DeleteRoleAsync(string idValue)
        {
            if (!await HasAsync(PermissionCodes.RolesDelete))
                return Denied();

            if (!TryParseId(idValue, out var id))
                return Fail(string.Format(ErrorMessages.RecordIsNullFromController, "Role ID"));

            var role = await _roleService.GetRoleByIDAsync(id);
            if (role == null)
                return Fail(string.Format(ErrorMessages.Cannotbefound, "Role"));

            if (IsSuperRole(role) || IsSystemRole(role.Name ?? ""))
                return Fail(ErrorMessages.CannotDeleteSystemRecords);

            return FromResult(await _roleService.DeleteRoleAsync(id));
        }

        private async Task<IActionResult> DeleteRolePermissionAsync(string idValue, Dictionary<string, string?> keys)
        {
            if (!await HasAsync(PermissionCodes.RolesManagePermissions))
                return Denied();

            if (!TryGetGuid(keys, nameof(RolePermission.RoleId), out var roleId) ||
                !TryGetGuid(keys, nameof(RolePermission.PermissionId), out var permissionId))
            {
                if (!TryParseId(idValue, out var id))
                    return Fail("A valid RoleId and PermissionId are required.");

                var item = await _permissionService.QueryRolePermissionsExt()
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (item == null)
                    return Fail(string.Format(ErrorMessages.Cannotbefound, "RolePermission"));

                roleId = item.RoleId;
                permissionId = item.PermissionId;
            }

            return FromResult(await _roleService.DeletePermissionAsync(roleId, permissionId));
        }

        private async Task<IActionResult> DeleteUserPermissionAsync(string idValue, Dictionary<string, string?> keys)
        {
            if (!await HasAsync(PermissionCodes.UsersManageRoles))
                return Denied();

            if (!TryGetGuid(keys, nameof(UserPermission.UserId), out var userId) ||
                !TryGetGuid(keys, nameof(UserPermission.PermissionId), out var permissionId))
            {
                if (!TryParseId(idValue, out var id))
                    return Fail("A valid UserId and PermissionId are required.");

                var item = await _permissionService.QueryUserPermissionsExt()
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (item == null)
                    return Fail(string.Format(ErrorMessages.Cannotbefound, "UserPermission"));

                userId = item.UserId;
                permissionId = item.PermissionId;
            }

            if (!await CanViewUserAsync(userId))
                return Denied();

            return FromResult(await _userService.DeletePermissionAsync(userId, permissionId));
        }

        private async Task<IActionResult> DeleteUserGroupAsync(string idValue)
        {
            if (!await HasAsync(PermissionCodes.UserGroupsDelete))
                return Denied();

            if (!TryParseId(idValue, out var id))
                return Fail(string.Format(ErrorMessages.RecordIsNullFromController, "UserGroup ID"));

            var group = await _userGroupService.GetByIDAsync(id);
            if (group == null)
                return Fail(string.Format(ErrorMessages.Cannotbefound, "UserGroup"));

            return FromResult(await _userGroupService.DeleteUserGroupAsync(id));
        }

        private async Task<IActionResult> DeleteGroupMemberAsync(Dictionary<string, string?> keys)
        {
            if (!await HasAsync(PermissionCodes.UserGroupsEdit))
                return Denied();

            if (!TryGetGuid(keys, nameof(UserGroupMember.GroupId), out var groupId) &&
                !TryGetGuid(keys, "GroupID", out groupId))
            {
                return Fail("A valid GroupID is required.");
            }

            if (!TryGetGuid(keys, nameof(UserGroupMember.UserId), out var userId))
                return Fail("A valid UserId is required.");

            return FromResult(await _userGroupService.DeleteMemberAsync(groupId, userId));
        }

        private async Task<IActionResult> DeleteNotificationAsync(string idValue)
        {
            if (!await HasAsync(PermissionCodes.NotificationsDelete))
                return Denied();

            if (!TryParseId(idValue, out var id))
                return Fail(string.Format(ErrorMessages.RecordIsNullFromController, "Notification ID"));

            var notification = await _notificationService.GetNotificationByIDAsync(id);
            if (notification == null)
                return Fail(string.Format(ErrorMessages.Cannotbefound, "Notification"));

            if (!await CanViewUserAsync(notification.UserId))
                return Denied();

            return FromResult(await _notificationService.DeleteNotificationAsync(id));
        }

        private async Task<IActionResult> DeleteSupportTicketAsync(string idValue)
        {
            if (!await HasAsync(PermissionCodes.SupportTicketsDelete))
                return Denied();

            if (!TryParseId(idValue, out var id))
                return Fail(string.Format(ErrorMessages.RecordIsNullFromController, "SupportTicket ID"));

            var ticket = await _supportTicketService.GetSupportTicketByIDAsync(id);
            if (ticket == null)
                return Fail(string.Format(ErrorMessages.Cannotbefound, "SupportTicket"));

            if (!await CanViewSupportTicketAsync(ticket))
                return Denied();

            return FromResult(await _supportTicketService.DeleteSupportTicketAsync(id));
        }

        private async Task<IActionResult> DeleteEmailTemplateAsync(string idValue)
        {
            if (!await HasAsync(PermissionCodes.EmailTemplatesDelete))
                return Denied();

            if (!TryParseId(idValue, out var id))
                return Fail(string.Format(ErrorMessages.RecordIsNullFromController, "EmailTemplate ID"));

            var template = await _emailTemplateService.GetTemplateByIDAsync(id);
            if (template == null)
                return Fail(string.Format(ErrorMessages.Cannotbefound, "EmailTemplate"));

            if (AttributeDropdownHelper.GetValue<EmailTemplateNameAttribute>(template.Name ?? "") != null)
                return Fail(ErrorMessages.CannotDeleteSystemRecords);

            return FromResult(await _emailTemplateService.DeleteTemplateAsync(id));
        }

        private async Task<IActionResult> DeleteAttachmentAsync(string idValue)
        {
            if (!await HasAsync(PermissionCodes.AttachmentsManage))
                return Denied();

            if (!TryParseId(idValue, out var id))
                return Fail(string.Format(ErrorMessages.RecordIsNullFromController, "Attachment ID"));

            var attachment = await _attachmentService.GetByIdAsync(id);
            if (attachment == null)
                return Fail(string.Format(ErrorMessages.Cannotbefound, "Attachment"));

            if (!await CanAccessEntityAttachmentsAsync(attachment.EntityType, attachment.EntityId))
                return Denied();

            await _attachmentService.DeleteAsync(id);
            return Json(new { success = true });
        }

        private async Task<IActionResult> DeleteLogAsync(string idValue)
        {
            if (!await HasAsync(PermissionCodes.LogsView))
                return Denied();

            if (!TryParseId(idValue, out var id))
                return Fail(string.Format(ErrorMessages.RecordIsNullFromController, "Log ID"));

            var log = await _logService.GetLogByIDAsync(id);
            if (log == null)
                return Fail(string.Format(ErrorMessages.Cannotbefound, "Log"));

            if (log.CreatedBy != null && !await CanViewUserAsync(log.CreatedBy))
                return Denied();

            await _logService.DeleteLogAsync(id);
            return Json(new { success = true });
        }

        private async Task<bool> CanAccessEntityAttachmentsAsync(string entityType, Guid entityId)
        {
            if (string.Equals(entityType, "SupportTicket", StringComparison.OrdinalIgnoreCase))
            {
                var ticket = await _supportTicketService.GetSupportTicketByIDAsync(entityId);
                return await CanViewSupportTicketAsync(ticket);
            }

            if (string.Equals(entityType, "User", StringComparison.OrdinalIgnoreCase))
                return await CanViewUserAsync(entityId);

            return await HasAsync(PermissionCodes.AttachmentsView);
        }

        private Task<bool> HasAsync(string code) =>
            _permissionService.HasPermissionAsync(_currentUser.UserId, code);

        private static IActionResult Fail(string message) =>
            new BadRequestObjectResult(new { success = false, message });

        private static IActionResult Denied() =>
            new ObjectResult(new { success = false, message = ErrorMessages.NoPermissionForThisAction })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };

        private static IActionResult FromResult<T>(ServiceResult<T> result)
        {
            if (result.Success)
                return new JsonResult(new { success = true });

            var message = result.Errors != null && result.Errors.Count > 0
                ? string.Join(" ", result.Errors)
                : ErrorMessages.DefaultLogErrorMessage;

            return Fail(message);
        }

        private static bool TryParseId(string? value, out Guid id) =>
            Guid.TryParse(value, out id);

        private static Dictionary<string, string?> MergeKeys(
            Dictionary<string, string?>? keys,
            Dictionary<string, string?>? parentKeys)
        {
            var merged = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            if (parentKeys != null)
            {
                foreach (var pair in parentKeys)
                    merged[pair.Key] = pair.Value;
            }

            if (keys != null)
            {
                foreach (var pair in keys)
                    merged[pair.Key] = pair.Value;
            }

            return merged;
        }

        private static bool TryGetGuid(Dictionary<string, string?> keys, string name, out Guid value)
        {
            value = Guid.Empty;
            if (keys == null)
                return false;

            foreach (var pair in keys)
            {
                if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase) &&
                    Guid.TryParse(pair.Value, out value))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
