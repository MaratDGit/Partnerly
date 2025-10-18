using Microsoft.AspNetCore.Mvc;
using Partnerly.Descriptors.Messages;
using Partnerly.Events.BaseEvents;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Infrastructure.Services;
using Partnerly.Models;
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
            var user = await _userService.GetUserByIDAsync(_currentUser.UserId);
            if (user == null) return NotFound();

            var userAsView = new MySettingsViewModel()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                CreatedDate = user.CreatedDate,
                AllowSendEmails = user.AllowSendEmails,
                AllowSendNotifications = user.AllowSendNotifications,
                MyReferralCode = user.MyReferralCode,
            };

            return View(userAsView);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(Guid id, MySettingsViewModel model)
        {
            if (id != model.Id) return NotFound();

            bool passwordChanged = model.OldPassword != null || model.Password != null || model.ConfirmPassword != null;

            if (ModelState.IsValid || !passwordChanged)
            {
                User? user = await _userService.GetUserByIDAsync(model.Id);
                if (user != null)
                {
                    if (passwordChanged)
                    {
                        if (!BCrypt.Net.BCrypt.Verify(model.OldPassword, user.PasswordHash) || user.IsDeleted)
                        {
                            ModelState.AddModelError("OldPassword", ErrorMessages.IncorectPassword);
                            return View(model);
                        }
                    }

                    User? userSameEmail = await _userService.GetUserByEmailAsync(model.Email);
                    if (userSameEmail != null && userSameEmail.Id != user.Id)
                    {
                        ModelState.AddModelError("Email", ErrorMessages.UserWithEmailArleadyExist);
                        return View(model);
                    }
                    else
                    {
                        User? userSamePhone = await _userService.GetUserByPhoneAsync(model?.Phone);
                        if (userSamePhone != null && userSamePhone.Id != user.Id)
                        {
                            ModelState.AddModelError("Phone", ErrorMessages.UserWithPhoneArleadyExist);
                            return View(model);
                        }
                    }

                    user.FirstName = model.FirstName;
                    user.LastName = model.LastName;
                    user.AllowSendEmails = model.AllowSendEmails;
                    user.AllowSendNotifications = model.AllowSendNotifications;

                    if (passwordChanged)
                    {
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model?.Password);
                    }

                    ServiceResult<User?> result = await _userService.UpdateUserAsync(user);

                    if (!result.Success)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error);
                        }
                        return View(model);
                    }

                    TempData["ToastMessage"] = Messages.RecordSaved;
                    return RedirectToAction("Settings", new { });
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Payments()
        {
            return View();
        }
    }
}
