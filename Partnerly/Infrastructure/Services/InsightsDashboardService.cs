using Microsoft.EntityFrameworkCore;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using Partnerly.Models.Authorization;
using Partnerly.Models.ViewModels;

namespace Partnerly.Infrastructure.Services
{
    public class InsightsDashboardService : IInsightsDashboardService
    {
        private readonly AppDbContext _context;
        private readonly IUserVisibilityService _visibility;
        private readonly ICurrentUserService _currentUser;

        public InsightsDashboardService(
            AppDbContext context,
            IUserVisibilityService visibility,
            ICurrentUserService currentUser)
        {
            _context = context;
            _visibility = visibility;
            _currentUser = currentUser;
        }

        public async Task<DashboardsViewModel> BuildAsync(
            ISet<string> permissions,
            CancellationToken cancellationToken = default)
        {
            var model = new DashboardsViewModel
            {
                CanUsers = Has(permissions, PermissionCodes.UsersView),
                CanRoles = Has(permissions, PermissionCodes.RolesView),
                CanTasks = Has(permissions, PermissionCodes.SupportTicketsView),
                CanGroups = Has(permissions, PermissionCodes.UserGroupsView),
                CanNotifications = Has(permissions, PermissionCodes.NotificationsView)
            };

            var visible = _visibility.VisibleUserIds();
            var now = DateTime.Now;
            var weekAgo = now.AddDays(-7);
            var monthAgo = now.AddDays(-30);
            var viewerId = _currentUser.UserId;
            var viewerIsSuper = string.Equals(
                _currentUser.RoleName?.Trim(),
                Constants.SuperRoleName,
                StringComparison.OrdinalIgnoreCase);

            if (viewerId != null)
            {
                model.MyReferrals = await _context.Set<User>().AsNoTracking()
                    .CountAsync(u => u.ReferrerId == viewerId && !u.IsDeleted, cancellationToken);
                var myTickets = _context.Set<SupportTicket>().AsNoTracking()
                    .Where(t => !t.IsDeleted && (t.UserId == viewerId || t.AssignedTo == viewerId));
                model.MyTasks = await myTickets.CountAsync(cancellationToken);
                model.MyTasksOpen = await myTickets.CountAsync(
                    t => t.Status != SupportTicketStatusAttribute.Closed, cancellationToken);
            }

            if (model.CanUsers)
            {
                var users = _context.Set<User>().AsNoTracking()
                    .Where(u => visible.Contains(u.Id));

                model.UsersTotal = await users.CountAsync(u => !u.IsDeleted, cancellationToken);
                model.UsersOnline = await users.CountAsync(u => !u.IsDeleted && u.IsOnlayn == true, cancellationToken);
                model.UsersBlocked = await users.CountAsync(u => !u.IsDeleted && u.IsBlocked == true, cancellationToken);
                model.UsersNewWeek = await users.CountAsync(u => !u.IsDeleted && u.CreatedDate >= weekAgo, cancellationToken);
                model.UsersNewMonth = await users.CountAsync(u => !u.IsDeleted && u.CreatedDate >= monthAgo, cancellationToken);
                model.UsersWithReferrer = await users.CountAsync(u => !u.IsDeleted && u.ReferrerId != null, cancellationToken);
                model.UsersWithPhone = await users.CountAsync(u => !u.IsDeleted && u.Phone != null && u.Phone != "", cancellationToken);
                model.UsersGoogle = await users.CountAsync(u => !u.IsDeleted && u.GoogleId != null && u.GoogleId != "", cancellationToken);

                var byRole = await users
                    .Where(u => !u.IsDeleted)
                    .GroupBy(u => u.Role != null ? u.Role.Name : "—")
                    .Select(g => new { Name = g.Key ?? "—", Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync(cancellationToken);

                if (!viewerIsSuper)
                    byRole = byRole.Where(x => !string.Equals(x.Name, Constants.SuperRoleName, StringComparison.OrdinalIgnoreCase)).ToList();

                model.UsersByRole = ToNamed(byRole.Select(x => (x.Name, x.Count)), model.UsersTotal);

                model.RecentUsers = await users
                    .Where(u => !u.IsDeleted)
                    .OrderByDescending(u => u.CreatedDate)
                    .Take(8)
                    .Select(u => new DashboardListItem
                    {
                        Title = ((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim(),
                        Meta = (u.Role != null ? u.Role.Name + " · " : "") + (u.Email ?? ""),
                        Badge = u.IsBlocked == true ? "Blocked" : (u.IsOnlayn == true ? "Online" : null),
                        Date = u.CreatedDate
                    })
                    .ToListAsync(cancellationToken);
            }

            if (model.CanRoles)
            {
                var roles = _context.Set<Role>().AsNoTracking().Where(r => !r.IsDeleted);
                if (!viewerIsSuper)
                    roles = roles.Where(r => r.Name != Constants.SuperRoleName);

                model.RolesTotal = await roles.CountAsync(cancellationToken);
                model.RolesActive = await roles.CountAsync(r => r.IsActive, cancellationToken);
                model.RolesInactive = await roles.CountAsync(r => !r.IsActive, cancellationToken);
                model.RolePermissions = await (
                    from rp in _context.Set<RolePermission>().AsNoTracking()
                    join r in roles on rp.RoleId equals r.Id
                    select rp.Id
                ).CountAsync(cancellationToken);
            }

            if (model.CanGroups)
            {
                var groups = _context.Set<UserGroup>().AsNoTracking().Where(g => !g.IsDeleted);
                model.GroupsTotal = await groups.CountAsync(cancellationToken);
                model.GroupsMembers = await _context.Set<UserGroupMember>().AsNoTracking()
                    .CountAsync(m => !m.IsDeleted && visible.Contains(m.UserId), cancellationToken);
                model.GroupsEmpty = await groups.CountAsync(
                    g => !_context.Set<UserGroupMember>().Any(m => m.GroupId == g.Id && !m.IsDeleted),
                    cancellationToken);
                model.GroupsNewWeek = await groups.CountAsync(g => g.CreatedDate >= weekAgo, cancellationToken);

                var topGroups = await groups
                    .Select(g => new
                    {
                        Name = g.Name ?? "—",
                        Count = g.Members.Count(m => !m.IsDeleted && visible.Contains(m.UserId))
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(8)
                    .ToListAsync(cancellationToken);
                var topMax = topGroups.Count == 0 ? 0 : topGroups.Max(x => x.Count);
                model.GroupsBySize = ToNamed(topGroups.Select(x => (x.Name, x.Count)), topMax);

                model.RecentGroups = await groups
                    .OrderByDescending(g => g.CreatedDate)
                    .Take(8)
                    .Select(g => new DashboardListItem
                    {
                        Title = g.Name ?? "",
                        Meta = g.Members.Count(m => !m.IsDeleted).ToString(),
                        Date = g.CreatedDate
                    })
                    .ToListAsync(cancellationToken);
            }

            if (model.CanTasks)
            {
                var tickets = _context.Set<SupportTicket>().AsNoTracking()
                    .Where(t => !t.IsDeleted && (
                        (t.UserId != null && visible.Contains(t.UserId.Value)) ||
                        (t.AssignedTo != null && visible.Contains(t.AssignedTo.Value))));

                model.TasksTotal = await tickets.CountAsync(cancellationToken);
                model.TasksNew = await tickets.CountAsync(t => t.Status == SupportTicketStatusAttribute.New, cancellationToken);
                model.TasksInProgress = await tickets.CountAsync(t => t.Status == SupportTicketStatusAttribute.InProgress, cancellationToken);
                model.TasksClosed = await tickets.CountAsync(t => t.Status == SupportTicketStatusAttribute.Closed, cancellationToken);
                model.TasksUnassigned = await tickets.CountAsync(t => t.AssignedTo == null, cancellationToken);
                model.TasksUnread = await tickets.CountAsync(t => t.IsRead != true, cancellationToken);
                model.TasksWeek = await tickets.CountAsync(t => t.CreatedDate >= weekAgo, cancellationToken);
                model.TasksByStatus = ToNamed(new[]
                {
                    ("New", model.TasksNew),
                    ("InProgress", model.TasksInProgress),
                    ("Closed", model.TasksClosed)
                }, model.TasksTotal);

                model.RecentTasks = await tickets
                    .OrderByDescending(t => t.CreatedDate)
                    .Take(8)
                    .Select(t => new DashboardListItem
                    {
                        Title = t.Subject ?? t.TicketID ?? "",
                        Meta = t.TicketID ?? "",
                        Badge = t.Status == SupportTicketStatusAttribute.New ? "New"
                            : t.Status == SupportTicketStatusAttribute.InProgress ? "InProgress"
                            : t.Status == SupportTicketStatusAttribute.Closed ? "Closed"
                            : t.Status,
                        Date = t.CreatedDate
                    })
                    .ToListAsync(cancellationToken);
            }

            if (model.CanNotifications)
            {
                var notes = _context.Set<Notification>().AsNoTracking()
                    .Where(n => !n.IsDeleted && visible.Contains(n.UserId));
                model.NotificationsTotal = await notes.CountAsync(cancellationToken);
                model.NotificationsUnread = await notes.CountAsync(n => !n.IsRead, cancellationToken);
                model.NotificationsWeek = await notes.CountAsync(n => n.CreatedDate >= weekAgo, cancellationToken);

                var byType = await notes
                    .GroupBy(n => n.Type ?? "—")
                    .Select(g => new { Name = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync(cancellationToken);
                model.NotificationsByType = ToNamed(
                    byType.Select(x => (TypeKey(x.Name), x.Count)),
                    model.NotificationsTotal);

                var recentNotes = await notes
                    .OrderByDescending(n => n.CreatedDate)
                    .Take(8)
                    .Select(n => new { n.Message, n.Type, n.IsRead, n.CreatedDate })
                    .ToListAsync(cancellationToken);
                model.RecentNotifications = recentNotes.Select(n =>
                {
                    var message = n.Message ?? "";
                    if (message.Length > 80)
                        message = message.Substring(0, 80) + "…";
                    return new DashboardListItem
                    {
                        Title = message,
                        Meta = TypeKey(n.Type),
                        Badge = n.IsRead ? null : "Unread",
                        Date = n.CreatedDate
                    };
                }).ToList();
            }

            return model;
        }

        private static bool Has(ISet<string> permissions, string code) =>
            permissions.Contains(code);

        private static string TypeKey(string? type) => type switch
        {
            NotificationTypeAttribute.Information => "Information",
            NotificationTypeAttribute.Warning => "Warning",
            NotificationTypeAttribute.Error => "Error",
            NotificationTypeAttribute.Success => "Success",
            _ => type ?? "—"
        };

        private static List<NamedCount> ToNamed(IEnumerable<(string Name, int Count)> rows, int total)
        {
            return rows.Select(row => new NamedCount
            {
                Name = row.Name,
                Count = row.Count,
                Percent = total <= 0 ? 0 : (int)Math.Round(row.Count * 100d / total)
            }).ToList();
        }
    }
}
