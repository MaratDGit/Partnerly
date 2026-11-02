using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Security
{
    public class AutomationAccessService : IAutomationAccessService
    {
        public AutomationAccessService(Guid currentUserId, bool canManage)
        {
            CurrentUserId = currentUserId;
            CanManage = canManage;
        }

        public Guid CurrentUserId { get; }
        public bool CanManage { get; }

        public bool CanAccess(AutomationDefinition definition)
        {
            if (definition.IsDeleted)
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
