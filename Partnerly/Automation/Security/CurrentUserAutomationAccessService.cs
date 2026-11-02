using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Persistence.Entities;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.Authorization;

namespace Partnerly.Automation.Security
{
    public class CurrentUserAutomationAccessService : IAutomationAccessService
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IPermissionService _permissions;
        private bool? _canManage;

        public CurrentUserAutomationAccessService(ICurrentUserService currentUser, IPermissionService permissions)
        {
            _currentUser = currentUser;
            _permissions = permissions;
        }

        public Guid CurrentUserId => _currentUser.UserId ?? Guid.Empty;

        public bool CanManage
        {
            get
            {
                if (_canManage != null)
                {
                    return _canManage.Value;
                }

                _canManage = _permissions
                    .HasPermissionAsync(_currentUser.UserId, PermissionCodes.AutomationManage)
                    .GetAwaiter()
                    .GetResult();
                return _canManage.Value;
            }
        }

        public bool CanAccess(AutomationDefinition definition)
        {
            if (definition.IsDeleted || CurrentUserId == Guid.Empty)
            {
                return false;
            }

            return CanManage || definition.OwnerUserId == CurrentUserId;
        }

        public AutomationDefinition? VisibleOrNull(AutomationDefinition? definition)
        {
            if (definition == null || !CanAccess(definition))
            {
                return null;
            }

            return definition;
        }
    }
}
