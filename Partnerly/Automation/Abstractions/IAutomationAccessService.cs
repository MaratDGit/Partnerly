using Partnerly.Automation.Persistence.Entities;

namespace Partnerly.Automation.Abstractions
{
    public interface IAutomationAccessService
    {
        bool CanManage { get; }
        Guid CurrentUserId { get; }
        bool CanAccess(AutomationDefinition definition);
        AutomationDefinition? VisibleOrNull(AutomationDefinition? definition);
    }
}
