namespace Partnerly.Models.ViewModels
{
    public class DashboardsViewModel
    {
        public bool CanUsers { get; set; }
        public bool CanRoles { get; set; }
        public bool CanTasks { get; set; }
        public bool CanGroups { get; set; }
        public bool CanNotifications { get; set; }

        public int UsersTotal { get; set; }
        public int UsersOnline { get; set; }
        public int UsersBlocked { get; set; }
        public int UsersNewWeek { get; set; }
        public int UsersNewMonth { get; set; }
        public int UsersWithReferrer { get; set; }
        public int UsersWithPhone { get; set; }
        public int UsersGoogle { get; set; }

        public int RolesTotal { get; set; }
        public int RolesActive { get; set; }
        public int RolesInactive { get; set; }
        public int RolePermissions { get; set; }

        public int GroupsTotal { get; set; }
        public int GroupsMembers { get; set; }
        public int GroupsEmpty { get; set; }
        public int GroupsNewWeek { get; set; }

        public int TasksTotal { get; set; }
        public int TasksNew { get; set; }
        public int TasksInProgress { get; set; }
        public int TasksClosed { get; set; }
        public int TasksUnassigned { get; set; }
        public int TasksUnread { get; set; }
        public int TasksWeek { get; set; }

        public int NotificationsTotal { get; set; }
        public int NotificationsUnread { get; set; }
        public int NotificationsWeek { get; set; }
        public int MyReferrals { get; set; }
        public int MyTasks { get; set; }
        public int MyTasksOpen { get; set; }

        public List<NamedCount> UsersByRole { get; set; } = new();
        public List<NamedCount> TasksByStatus { get; set; } = new();
        public List<NamedCount> GroupsBySize { get; set; } = new();
        public List<NamedCount> NotificationsByType { get; set; } = new();
        public List<DashboardListItem> RecentUsers { get; set; } = new();
        public List<DashboardListItem> RecentTasks { get; set; } = new();
        public List<DashboardListItem> RecentGroups { get; set; } = new();
        public List<DashboardListItem> RecentNotifications { get; set; } = new();
    }

    public class NamedCount
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public int Percent { get; set; }
    }

    public class DashboardListItem
    {
        public string Title { get; set; } = string.Empty;
        public string Meta { get; set; } = string.Empty;
        public string? Badge { get; set; }
        public DateTime? Date { get; set; }
    }
}
