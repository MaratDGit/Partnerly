using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Partnerly.Descriptors.Messages;
using Partnerly.Events.BaseEvents;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Infrastructure.Services;
using Partnerly.Models;
using Partnerly.Models.DataTable;
using Partnerly.Models.ViewModels;

namespace Partnerly.Controllers
{
    public class DashboardController : _BaseController
    {
        public DashboardController(
            IEventBus eventBus, 
            IUserService userService, 
            ICurrentUserService currentUser, 
            ILogService logService, 
            INotificationService notificationService, 
            ISystemSettingsService systemSettingsService, 
            IStringLocalizer<SharedResource> localizer, 
            DataTableQueryService tableService, 
            IDataTableRegistry dataTableRegistry)
        : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService, tableService, dataTableRegistry)
        {

        }

        public IActionResult Index()
        {
            var model = new DashboardViewModel
            {
                // Current month
                GamesPlayed = 128,
                Victories = 74,
                Defeats = 54,

                // Previous month
                PreviousGamesPlayed = 114,
                PreviousVictories = 68,
                PreviousDefeats = 60,

                GamesByDay = new List<GamesByDayViewModel>
                {
                    new() { Date = DateTime.Today.AddDays(-6), Count = 12 },
                    new() { Date = DateTime.Today.AddDays(-5), Count = 19 },
                    new() { Date = DateTime.Today.AddDays(-4), Count = 14 },
                    new() { Date = DateTime.Today.AddDays(-3), Count = 22 },
                    new() { Date = DateTime.Today.AddDays(-2), Count = 18 },
                    new() { Date = DateTime.Today.AddDays(-1), Count = 27 },
                    new() { Date = DateTime.Today, Count = 16 }
                }
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult GetStats()
        {
            // TODO:
            // Replace these values with real database calculations
            // when the Game entity/repository is implemented.

            var gamesPlayed = 130;
            var victories = 80;
            var defeats = 50;

            var winRate = gamesPlayed == 0
                ? 0
                : Math.Round(
                    (decimal)victories / gamesPlayed * 100,
                    1
                );

            return Json(new
            {
                gamesPlayed,
                victories,
                defeats,
                winRate
            });
        }

        [HttpGet]
        public IActionResult GetGamesByPeriod(string period)
        {
            var today = DateTime.Today;

            var data = period switch
            {
                "7days" => Enumerable.Range(0, 7)
                    .Select(i => new GamesByDayViewModel
                    {
                        Date = today.AddDays(-6 + i),
                        Count = Random.Shared.Next(5, 30)
                    })
                    .ToList(),

                "30days" => Enumerable.Range(0, 30)
                    .Select(i => new GamesByDayViewModel
                    {
                        Date = today.AddDays(-29 + i),
                        Count = Random.Shared.Next(5, 30)
                    })
                    .ToList(),

                "3months" => Enumerable.Range(0, 3)
                    .Select(i =>
                    {
                        var date = new DateTime(
                            today.Year,
                            today.Month,
                            1
                        ).AddMonths(-2 + i);

                        return new GamesByDayViewModel
                        {
                            Date = date,
                            Count = Random.Shared.Next(50, 150)
                        };
                    })
                    .ToList(),

                "year" => Enumerable.Range(1, 12)
                    .Select(month => new GamesByDayViewModel
                    {
                        Date = new DateTime(
                            today.Year,
                            month,
                            1
                        ),
                        Count = Random.Shared.Next(50, 200)
                    })
                    .ToList(),

                _ => new List<GamesByDayViewModel>()
            };

            return Json(data);
        }

        public async Task<IActionResult> MyProfile()
        {
            MyProfileViewModel model = new MyProfileViewModel();
            if (_currentUser.UserId != null)
            { 
                var user = await _userService.GetUserByIDAsync(_currentUser.UserId);
                var referrals = await _userService.GetReferralsAsync(user?.Id);

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

                    if (referrals != null && referrals.Count() > 0)
                    {
                        foreach (var referral in referrals)
                        {
                            model.Refferals.Add(new UserViewModel { FirstName = referral.FirstName, LastName = referral.LastName, PhotoUrl = referral.PhotoUrl, Id = referral.Id, CreatedDate = referral.CreatedDate, Phone = referral.Phone });
                        }
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
                    else if (!string.IsNullOrEmpty(model.Phone))
                    {
                        User? userSamePhone = await _userService.GetUserByPhoneAsync(model?.Phone);
                        if (userSamePhone != null && userSamePhone.Id != user.Id)
                        {
                            ModelState.AddModelError("Phone", ErrorMessages.UserWithPhoneArleadyExist);
                            return View(model);
                        }

                        var error = ModelState["Phone"]?.Errors.FirstOrDefault()?.ErrorMessage;
                        if (error != null)
                        {
                            ModelState.AddModelError("Phone", error);
                            return View(model);
                        }
                    }
                    
                    user.FirstName = model.FirstName;
                    user.LastName = model.LastName;
                    user.Phone = model.Phone;
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
    }
}
