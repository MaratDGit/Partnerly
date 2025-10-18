using Microsoft.AspNetCore.Mvc;
using Partnerly.Events.BaseEvents;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.ViewModels;

namespace Partnerly.Controllers
{
    public class DashboardController : _BaseController
    {
        private readonly INotificationService _notificationService;
        public DashboardController(IEventBus eventBus, IUserService userService, ICurrentUserService currentUser, ILogService logService, INotificationService notificationService)
        : base(eventBus, userService, currentUser, logService)
        {
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        public async Task<IActionResult> MyProfile()
        {
            MyProfileViewModel model = new MyProfileViewModel();
            if (_currentUser.UserId != null)
            { 
                var user = await _userService.GetUserByIDAsync(_currentUser.UserId);
                var users = await _userService.GetAllUsersAsync();
                if (user != null)
                {
                    var referrer = await _userService.GetUserByIDAsync(user.ReferrerId);
                    model.Id = user.Id;
                    model.UserName = $"{user.FirstName} {user.LastName}";
                    model.Email = user.Email;
                    model.Phone = user.Phone;
                    model.PhotoUrl = user.PhotoUrl;
                    model.RoleName = user.Role?.Name;
                    model.MyReferralCode = user.MyReferralCode;
                    model.ReferrerName = $"{referrer?.FirstName} {referrer?.LastName}";
                    model.CreatedDate = user.CreatedDate;

                    foreach (var referral in users.Where(_ => _.ReferrerId == user.Id))
                    {
                        model.Refferals.Add(new UserViewModel { FirstName = referral.FirstName, LastName = referral.LastName, PhotoUrl = referral.PhotoUrl, Id = referral.Id, CreatedDate = referral.CreatedDate, Phone = referral.Phone });
                    }
                }
            }

            return View(model);
        }

        public async Task<IActionResult> Settings()
        {
            return View();
        }

        public async Task<IActionResult> Payments()
        {
            return View();
        }
    }
}
