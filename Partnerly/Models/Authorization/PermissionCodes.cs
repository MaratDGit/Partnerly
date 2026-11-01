namespace Partnerly.Models.Authorization
{
    public static class PermissionCodes
    {
        public const string UsersView = "users.view";
        public const string UsersCreate = "users.create";
        public const string UsersEdit = "users.edit";
        public const string UsersDelete = "users.delete";
        public const string UsersManageRoles = "users.manage_roles";

        public const string RolesView = "roles.view";
        public const string RolesCreate = "roles.create";
        public const string RolesEdit = "roles.edit";
        public const string RolesDelete = "roles.delete";
        public const string RolesManagePermissions = "roles.manage_permissions";

        public const string PermissionsView = "permissions.view";
        public const string PermissionsManage = "permissions.manage";

        public const string UserGroupsView = "user_groups.view";
        public const string UserGroupsCreate = "user_groups.create";
        public const string UserGroupsEdit = "user_groups.edit";
        public const string UserGroupsDelete = "user_groups.delete";

        public const string NotificationsView = "notifications.view";
        public const string NotificationsCreate = "notifications.create";
        public const string NotificationsEdit = "notifications.edit";
        public const string NotificationsDelete = "notifications.delete";

        public const string SupportTicketsView = "support_tickets.view";
        public const string SupportTicketsCreate = "support_tickets.create";
        public const string SupportTicketsEdit = "support_tickets.edit";
        public const string SupportTicketsDelete = "support_tickets.delete";

        public const string SystemSettingsView = "system_settings.view";
        public const string SystemSettingsEdit = "system_settings.edit";

        public const string EmailTemplatesView = "email_templates.view";
        public const string EmailTemplatesCreate = "email_templates.create";
        public const string EmailTemplatesEdit = "email_templates.edit";
        public const string EmailTemplatesDelete = "email_templates.delete";

        public const string LogsView = "logs.view";

        public const string AttachmentsView = "attachments.view";
        public const string AttachmentsManage = "attachments.manage";

        public const string AiUse = "ai.use";
        public const string WebInspectorUse = "web_inspector.use";
        public const string DataTableViewIds = "datatable.view_ids";
        public const string DataTableBulkDelete = "datatable.bulk_delete";

        public static readonly string[] All =
        {
            UsersView, UsersCreate, UsersEdit, UsersDelete, UsersManageRoles,
            RolesView, RolesCreate, RolesEdit, RolesDelete, RolesManagePermissions,
            PermissionsView, PermissionsManage,
            UserGroupsView, UserGroupsCreate, UserGroupsEdit, UserGroupsDelete,
            NotificationsView, NotificationsCreate, NotificationsEdit, NotificationsDelete,
            SupportTicketsView, SupportTicketsCreate, SupportTicketsEdit, SupportTicketsDelete,
            SystemSettingsView, SystemSettingsEdit,
            EmailTemplatesView, EmailTemplatesCreate, EmailTemplatesEdit, EmailTemplatesDelete,
            LogsView,
            AttachmentsView, AttachmentsManage,
            AiUse,
            WebInspectorUse,
            DataTableViewIds,
            DataTableBulkDelete
        };
    }
}
