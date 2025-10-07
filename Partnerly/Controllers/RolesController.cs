using Microsoft.AspNetCore.Mvc;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Infrastructure.Interfaces;
using System.Security.Claims;

namespace Partnerly.Controllers
{
    [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
    public class RolesController : _BaseController
    {
        public RolesController(IUserService userService, ICurrentUserService currentUser)
        : base(userService, currentUser)
        {
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }
    }
}
