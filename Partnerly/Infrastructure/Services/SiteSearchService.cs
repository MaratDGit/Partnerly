using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using Partnerly.Models.Authorization;
using Partnerly.Models.ViewModels;

namespace Partnerly.Infrastructure.Services
{
    public class SiteSearchService : ISiteSearchService
    {
        private const int MaxPerGroup = 5;
        private const int MinQueryLength = 2;

        private readonly AppDbContext _context;
        private readonly IUserVisibilityService _visibility;
        private readonly ICurrentUserService _currentUser;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public SiteSearchService(
            AppDbContext context,
            IUserVisibilityService visibility,
            ICurrentUserService currentUser,
            IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _visibility = visibility;
            _currentUser = currentUser;
            _localizer = localizer;
        }

        public async Task<SiteSearchResult> SearchAsync(
            string? query,
            ISet<string> permissions,
            CancellationToken cancellationToken = default)
        {
            var result = new SiteSearchResult();
            var term = (query ?? string.Empty).Trim();
            if (term.Length < MinQueryLength)
            {
                return result;
            }

            if (term.Length > 80)
            {
                term = term[..80];
            }

            var sections = MatchSections(term, permissions);
            if (sections.Count > 0)
            {
                result.Groups.Add(new SiteSearchGroup
                {
                    Key = "sections",
                    Title = _localizer["SiteSearchSections"].Value,
                    Items = sections
                });
            }

            var visible = _visibility.VisibleUserIds();
            var viewerId = _currentUser.UserId;

            if (Has(permissions, PermissionCodes.UsersView))
            {
                var users = await _context.Set<User>().AsNoTracking()
                    .Where(u => !u.IsDeleted && visible.Contains(u.Id))
                    .Where(u =>
                        (u.FirstName != null && u.FirstName.Contains(term)) ||
                        (u.LastName != null && u.LastName.Contains(term)) ||
                        (u.Email != null && u.Email.Contains(term)) ||
                        (u.Phone != null && u.Phone.Contains(term)) ||
                        (u.MyReferralCode != null && u.MyReferralCode.Contains(term)))
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .Take(MaxPerGroup)
                    .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
                    .ToListAsync(cancellationToken);

                AddGroup(result, "users", _localizer["Users"].Value, users.Select(u => new SiteSearchItem
                {
                    Title = FormatName(u.FirstName, u.LastName, u.Email),
                    Subtitle = u.Email,
                    Url = "/Users/Edit/" + u.Id,
                    Icon = "bx bx-user"
                }).ToList());
            }

            if (Has(permissions, PermissionCodes.RolesView))
            {
                var activeText = _localizer["Active"].Value;
                var inactiveText = _localizer["Inactive"].Value;
                var roles = await _context.Set<Role>().AsNoTracking()
                    .Where(r => !r.IsDeleted && r.Name != null && r.Name.Contains(term))
                    .OrderBy(r => r.Name)
                    .Take(MaxPerGroup)
                    .Select(r => new { r.Id, r.Name, r.IsActive })
                    .ToListAsync(cancellationToken);

                AddGroup(result, "roles", _localizer["Roles"].Value, roles.Select(r => new SiteSearchItem
                {
                    Title = r.Name ?? "",
                    Subtitle = r.IsActive ? activeText : inactiveText,
                    Url = "/Roles/Edit/" + r.Id,
                    Icon = "bx bx-shield-quarter"
                }).ToList());
            }

            if (Has(permissions, PermissionCodes.UserGroupsView))
            {
                var groups = await _context.Set<UserGroup>().AsNoTracking()
                    .Where(g => !g.IsDeleted && (
                        (g.Name != null && g.Name.Contains(term)) ||
                        (g.Description != null && g.Description.Contains(term))))
                    .OrderBy(g => g.Name)
                    .Take(MaxPerGroup)
                    .Select(g => new { g.Id, g.Name, g.Description })
                    .ToListAsync(cancellationToken);

                AddGroup(result, "groups", _localizer["Groups"].Value, groups.Select(g => new SiteSearchItem
                {
                    Title = g.Name ?? "",
                    Subtitle = g.Description,
                    Url = "/UserGroups/Edit/" + g.Id,
                    Icon = "bx bx-group"
                }).ToList());
            }

            if (Has(permissions, PermissionCodes.SupportTicketsView))
            {
                var tickets = await _context.Set<SupportTicket>().AsNoTracking()
                    .Where(t => !t.IsDeleted && (
                        (t.UserId != null && visible.Contains(t.UserId.Value)) ||
                        t.AssignedTo == viewerId))
                    .Where(t =>
                        (t.TicketID != null && t.TicketID.Contains(term)) ||
                        (t.Subject != null && t.Subject.Contains(term)) ||
                        (t.Message != null && t.Message.Contains(term)))
                    .OrderByDescending(t => t.CreatedDate)
                    .Take(MaxPerGroup)
                    .Select(t => new { t.Id, t.Subject, t.TicketID })
                    .ToListAsync(cancellationToken);

                AddGroup(result, "tasks", _localizer["Tasks"].Value, tickets.Select(t => new SiteSearchItem
                {
                    Title = t.Subject ?? t.TicketID ?? "",
                    Subtitle = t.TicketID,
                    Url = "/SupportTickets/ViewCase/" + t.Id,
                    Icon = "bx bx-task"
                }).ToList());
            }

            if (Has(permissions, PermissionCodes.NotificationsView))
            {
                var notes = await _context.Set<Notification>().AsNoTracking()
                    .Where(n => !n.IsDeleted && visible.Contains(n.UserId))
                    .Where(n => n.Message != null && n.Message.Contains(term))
                    .OrderByDescending(n => n.CreatedDate)
                    .Take(MaxPerGroup)
                    .Select(n => new { n.Id, n.Message, n.Type, n.Link })
                    .ToListAsync(cancellationToken);

                AddGroup(result, "notifications", _localizer["Notifications"].Value, notes.Select(n => new SiteSearchItem
                {
                    Title = NotificationMessage.Format(n.Message),
                    Subtitle = n.Type,
                    Url = string.IsNullOrEmpty(n.Link)
                        ? "/Notification/Edit/" + n.Id
                        : n.Link,
                    Icon = "bx bx-bell"
                }).ToList());
            }

            if (Has(permissions, PermissionCodes.EmailTemplatesView))
            {
                var templates = await _context.Set<EmailTemplate>().AsNoTracking()
                    .Where(t => !t.IsDeleted && (
                        (t.Name != null && t.Name.Contains(term)) ||
                        (t.Subject != null && t.Subject.Contains(term))))
                    .OrderBy(t => t.Name)
                    .Take(MaxPerGroup)
                    .Select(t => new { t.Id, t.Name, t.Subject })
                    .ToListAsync(cancellationToken);

                AddGroup(result, "templates", _localizer["Emailtemplates"].Value, templates.Select(t => new SiteSearchItem
                {
                    Title = t.Name ?? "",
                    Subtitle = t.Subject,
                    Url = "/EmailTemplates/Edit/" + t.Id,
                    Icon = "bx bx-envelope"
                }).ToList());
            }

            if (Has(permissions, PermissionCodes.LogsView))
            {
                var logs = await _context.Set<Log>().AsNoTracking()
                    .Where(l => !l.IsDeleted && (
                        l.CreatedBy == null || visible.Contains(l.CreatedBy.Value)))
                    .Where(l =>
                        (l.Action != null && l.Action.Contains(term)) ||
                        (l.LogMessage != null && l.LogMessage.Contains(term)) ||
                        (l.Method != null && l.Method.Contains(term)))
                    .OrderByDescending(l => l.CreatedDate)
                    .Take(MaxPerGroup)
                    .Select(l => new { l.Id, l.Action, l.Type })
                    .ToListAsync(cancellationToken);

                AddGroup(result, "logs", _localizer["Logs"].Value, logs.Select(l => new SiteSearchItem
                {
                    Title = l.Action ?? "",
                    Subtitle = l.Type,
                    Url = "/LogRecords/View/" + l.Id,
                    Icon = "bx bx-file"
                }).ToList());
            }

            if (Has(permissions, PermissionCodes.AttachmentsView))
            {
                var files = await _context.Set<Attachment>().AsNoTracking()
                    .Where(a => !a.IsDeleted &&
                                a.CreatedBy != null &&
                                visible.Contains(a.CreatedBy.Value) &&
                                a.FileName.Contains(term))
                    .OrderByDescending(a => a.CreatedDate)
                    .Take(MaxPerGroup)
                    .Select(a => new { a.Id, a.FileName, a.EntityType })
                    .ToListAsync(cancellationToken);

                AddGroup(result, "attachments", _localizer["Attachments"].Value, files.Select(a => new SiteSearchItem
                {
                    Title = a.FileName,
                    Subtitle = a.EntityType,
                    Url = "/Attachments/Download/" + a.Id,
                    Icon = "bx bx-paperclip"
                }).ToList());
            }

            return result;
        }

        private List<SiteSearchItem> MatchSections(string term, ISet<string> permissions)
        {
            var items = new List<SectionCandidate>
            {
                Section(_localizer["Statistics"].Value, _localizer["Main"].Value, "/Dashboard", "bx bx-home-smile", "dashboard statistics stats home"),
                Section(_localizer["Dashboards"].Value, _localizer["Statistics"].Value, "/Dashboard#users", "bx bx-grid-alt", "dashboards kpi overview"),
                Section(_localizer["ReferralTree"].Value, _localizer["Statistics"].Value, "/Dashboard/ReferralTree", "bx bx-git-branch", "referral tree downline"),
                Section(_localizer["Myprofile"].Value, _localizer["Profile"].Value, "/Dashboard/MyProfile", "bx bx-user-circle", "profile my profile account"),
                Section(_localizer["Settings"].Value, _localizer["Profile"].Value, "/Dashboard/Settings", "bx bx-cog", "settings account password")
            };

            if (Has(permissions, PermissionCodes.WebInspectorUse))
            {
                items.Add(Section(_localizer["WebInspector"].Value, _localizer["Tools"].Value, "/WebInspector", "bx bx-globe", "web inspector tools browser"));
            }

            if (Has(permissions, PermissionCodes.SupportTicketsCreate))
            {
                items.Add(Section(_localizer["Support"].Value, _localizer["Miscellaneous"].Value, "/SupportTickets/OpenNewCase", "bx bx-support", "support ticket case help"));
            }

            if (Has(permissions, PermissionCodes.SystemSettingsView))
            {
                items.Add(Section(_localizer["Settings"].Value, _localizer["Settings"].Value, "/SystemPreferences", "bx bx-slider", "system settings preferences"));
            }

            if (Has(permissions, PermissionCodes.RolesView))
            {
                items.Add(Section(_localizer["Roles"].Value, _localizer["Settings"].Value, "/Roles", "bx bx-shield-quarter", "roles permissions"));
            }

            if (Has(permissions, PermissionCodes.UsersView))
            {
                items.Add(Section(_localizer["Users"].Value, _localizer["Settings"].Value, "/Users", "bx bx-user", "users people"));
            }

            if (Has(permissions, PermissionCodes.UserGroupsView))
            {
                items.Add(Section(_localizer["Groups"].Value, _localizer["Settings"].Value, "/UserGroups", "bx bx-group", "groups user groups"));
            }

            if (Has(permissions, PermissionCodes.SupportTicketsView))
            {
                items.Add(Section(_localizer["Tasks"].Value, _localizer["Settings"].Value, "/SupportTickets", "bx bx-task", "tasks tickets support"));
            }

            if (Has(permissions, PermissionCodes.EmailTemplatesView))
            {
                items.Add(Section(_localizer["Emailtemplates"].Value, _localizer["Settings"].Value, "/EmailTemplates", "bx bx-envelope", "email templates mail"));
            }

            if (Has(permissions, PermissionCodes.NotificationsView))
            {
                items.Add(Section(_localizer["Notifications"].Value, _localizer["Settings"].Value, "/Notification", "bx bx-bell", "notifications"));
            }

            if (Has(permissions, PermissionCodes.LogsView))
            {
                items.Add(Section(_localizer["Logs"].Value, _localizer["Settings"].Value, "/LogRecords", "bx bx-file", "logs journal audit"));
            }

            if (Has(permissions, PermissionCodes.AttachmentsView))
            {
                items.Add(Section(_localizer["Attachments"].Value, _localizer["Settings"].Value, "/Attachments", "bx bx-paperclip", "attachments files"));
            }

            return items
                .Where(item =>
                    Contains(item.Item.Title, term) ||
                    Contains(item.Item.Subtitle, term) ||
                    Contains(item.Keywords, term))
                .Select(item => item.Item)
                .Take(8)
                .ToList();
        }

        private static SectionCandidate Section(string title, string subtitle, string url, string icon, string keywords)
        {
            return new SectionCandidate
            {
                Item = new SiteSearchItem
                {
                    Title = title,
                    Subtitle = subtitle,
                    Url = url,
                    Icon = icon
                },
                Keywords = keywords
            };
        }

        private static string FormatName(string? firstName, string? lastName, string? fallback)
        {
            var name = ((firstName ?? "") + " " + (lastName ?? "")).Trim();
            return name.Length > 0 ? name : (fallback ?? "");
        }

        private static void AddGroup(
            SiteSearchResult result,
            string key,
            string title,
            List<SiteSearchItem> items)
        {
            if (items.Count == 0)
            {
                return;
            }

            result.Groups.Add(new SiteSearchGroup
            {
                Key = key,
                Title = title,
                Items = items
            });
        }

        private static bool Has(ISet<string> permissions, string code)
        {
            return permissions.Contains(code);
        }

        private static bool Contains(string? text, string term)
        {
            return !string.IsNullOrEmpty(text) &&
                   text.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        private sealed class SectionCandidate
        {
            public SiteSearchItem Item { get; init; } = null!;
            public string Keywords { get; init; } = string.Empty;
        }
    }
}
