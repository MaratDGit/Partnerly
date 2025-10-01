using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.ViewModels;

namespace Partnerly.Controllers
{
    [Authorize] // только для авторизованных
    public class _BaseController : Controller
    {
        protected readonly IUserService _userService;
        protected readonly ICurrentUserService _currentUser;

        public _BaseController(IUserService userService, ICurrentUserService currentUser)
        {
            _userService = userService;
            _currentUser = currentUser;
        }
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var dashboardModel = await GetUserProfileAsync();
                if (dashboardModel != null)
                {
                    ViewData["DashboardsViewModel"] = dashboardModel;
                }
            }

            await next();
        }

        private async Task<DashboardsViewModel?> GetUserProfileAsync()
        {
            var user = await _userService.GetUserByIDAsync(_currentUser.UserId);
            if (user == null || user.IsBlocked == true)
                return null;

            return new DashboardsViewModel
            {
                UserPhotoUrl = string.IsNullOrEmpty(user.PhotoUrl)
                    ? Constants.DefaultUserProfilePhotoPath
                    : user.PhotoUrl,
                UserFirstName = user.FirstName,
                UserLastName = user.LastName,
                UserRefCode = user.MyReferralCode,
            };
        }
    }
}
