using Microsoft.AspNetCore.Mvc;
using Partnerly.Infrastructure.Interfaces;

namespace Partnerly.Controllers
{
    public class LogsController : _BaseController
    {
        public LogsController(IUserService userService, ICurrentUserService currentUser)
        : base(userService, currentUser)
        {
        }
    }
}
