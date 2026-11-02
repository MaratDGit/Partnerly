using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Partnerly.Automation.Integration
{
    [Authorize]
    public class AutomationHub : Hub
    {
    }
}
