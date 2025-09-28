using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.ViewModels;
using System.Diagnostics;

namespace Partnerly.Controllers
{
    [Authorize] // только для авторизованных
    public class DashboardController : Controller
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUser;

        public DashboardController(IUserService userService, ICurrentUserService currentUser)
        {
            _userService = userService;
            _currentUser = currentUser;
        }

        public async Task<IActionResult> Index()
        {
            var model = await GetUserProfileAsync();
            if (model == null)
            {
                return View("Error", new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    ErrorCode = ErrorMessages.UserIsBlocked,
                });
            }

            ViewData["DashboardsViewModel"] = model;

            return View();
        }

        public async Task<IActionResult> MyProfile()
        {
            var model = await GetUserProfileAsync();
            ViewData["DashboardsViewModel"] = model;
            return View();
        }

        public async Task<IActionResult> Settings()
        {
            var model = await GetUserProfileAsync();
            ViewData["DashboardsViewModel"] = model;
            return View();
        }

        public async Task<IActionResult> Payments()
        {
            var model = await GetUserProfileAsync();
            ViewData["DashboardsViewModel"] = model;
            return View();
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
