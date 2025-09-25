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
            var user = await _userService.GetUserByIDAsync(_currentUser.UserId);
            if (user == null || user.IsBlocked == true)
            {
                return View("Error", new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    ErrorCode = ErrorMessages.UserIsBlocked,
                });
            }

            ViewData["ProfilePhoto"] = !string.IsNullOrEmpty(user.PhotoUrl) ? user.PhotoUrl : Constants.DefaultUserProfilePhotoPath;
            ViewData["UserFirstName"] = user.FirstName;
            ViewData["UserLastName"] = user.LastName;
            ViewData["UserRefCode"] = user.MyReferralCode;

            return View();
        }
    }
}
