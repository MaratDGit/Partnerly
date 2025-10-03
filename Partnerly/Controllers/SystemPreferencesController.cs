using Partnerly.Infrastructure.Interfaces;

namespace Partnerly.Controllers
{
    public class SystemPreferencesController : _BaseController
    {
        public SystemPreferencesController(IUserService userService, ICurrentUserService currentUser)
        : base(userService, currentUser)
        {
        }
    }
}
