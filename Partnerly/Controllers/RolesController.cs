using Microsoft.AspNetCore.Mvc;
using Partnerly.Infrastructure.Interfaces;

namespace Partnerly.Controllers
{
    public class RolesController : _BaseController
    {
        public RolesController(IUserService userService, ICurrentUserService currentUser)
        : base(userService, currentUser)
        {
        }
    }
}
