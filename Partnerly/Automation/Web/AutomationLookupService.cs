using Microsoft.EntityFrameworkCore;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Registry;
using Partnerly.Automation.Security;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Automation.Web
{
    // Reference lists for the builder's property dropdowns (users, email
    // templates, other agents, entity fields). Read-only, no secrets.
    public class AutomationLookupService
    {
        private const int MaxItems = 200;

        private readonly AppDbContext _db;
        private readonly IUserVisibilityService _visibility;
        private readonly EntityRegistry _entities;
        private readonly IAutomationStore _store;
        private readonly IAutomationAccessService _access;

        public AutomationLookupService(
            AppDbContext db,
            IUserVisibilityService visibility,
            EntityRegistry entities,
            IAutomationStore store,
            IAutomationAccessService access)
        {
            _db = db;
            _visibility = visibility;
            _entities = entities;
            _store = store;
            _access = access;
        }

        public async Task<object> BuildAsync(CancellationToken cancellationToken)
        {
            var visibleIds = _visibility.VisibleUserIds();
            var users = await _db.Users!
                .AsNoTracking()
                .Where(user => !user.IsDeleted && visibleIds.Contains(user.Id))
                .OrderBy(user => user.Email)
                .Take(MaxItems)
                .Select(user => new { id = user.Id, email = user.Email, firstName = user.FirstName, lastName = user.LastName })
                .ToListAsync(cancellationToken);

            var templates = await _db.EmailTemplates!
                .AsNoTracking()
                .Where(item => !item.IsDeleted && item.Name != null)
                .OrderBy(item => item.Name)
                .Take(MaxItems)
                .Select(item => item.Name)
                .ToListAsync(cancellationToken);

            var automations = _store.QueryDefinitions()
                .Where(item => !item.IsDeleted && item.OwnerUserId == _access.CurrentUserId)
                .OrderBy(item => item.Name)
                .Take(MaxItems)
                .Select(item => new { id = item.Id, name = item.Name })
                .ToList();

            var httpConnections = _store.QueryConnections()
                .Where(item => !item.IsDeleted && item.OwnerUserId == _access.CurrentUserId && item.Kind == "http")
                .OrderBy(item => item.Name)
                .Select(item => new { id = item.Id, name = item.Name })
                .ToList();

            var entities = _entities.All
                .OrderBy(item => item.DisplayName)
                .Select(item => new
                {
                    key = item.Key,
                    name = item.DisplayName,
                    fields = item.Fields.Select(field => new
                    {
                        name = field.Name,
                        type = field.DataType,
                        updatable = field.Updatable,
                        filterable = field.Filterable
                    }).ToList()
                })
                .ToList();

            return new
            {
                users = users.Select(user => new
                {
                    user.id,
                    user.email,
                    name = ((user.firstName ?? string.Empty) + " " + (user.lastName ?? string.Empty)).Trim()
                }),
                emailTemplates = templates,
                automations,
                httpConnections,
                entities
            };
        }
    }
}
