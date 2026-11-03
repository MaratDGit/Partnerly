using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Persistence;
using Partnerly.Descriptors.Messages;
using Partnerly.Events.BaseEvents;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Infrastructure.Services;
using Partnerly.Models;
using Partnerly.Models.Authorization;
using Partnerly.Models.DataTable;
using Partnerly.Models.ViewModels;

namespace Partnerly.Controllers
{
    public class DashboardController : _BaseController
    {
        private readonly IInsightsDashboardService _insightsDashboard;
        private readonly IAutomationStore _automationStore;
        private readonly IAutomationAccessService _automationAccess;

        public DashboardController(
            IEventBus eventBus, 
            IUserService userService, 
            ICurrentUserService currentUser, 
            ILogService logService, 
            INotificationService notificationService, 
            ISystemSettingsService systemSettingsService, 
            IStringLocalizer<SharedResource> localizer, 
            DataTableQueryService tableService, 
            IDataTableRegistry dataTableRegistry,
            IInsightsDashboardService insightsDashboard,
            IAutomationStore automationStore,
            IAutomationAccessService automationAccess)
        : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService, tableService, dataTableRegistry)
        {
            _insightsDashboard = insightsDashboard;
            _automationStore = automationStore;
            _automationAccess = automationAccess;
        }

        public async Task<IActionResult> Index()
        {
            var permissions = ViewData["Permissions"] as HashSet<string>
                ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var automation = BuildAutomationSnapshot();
            var model = new DashboardViewModel
            {
                GamesPlayed = automation.Automations,
                PreviousGamesPlayed = automation.AutomationsPrevious,
                Victories = automation.Succeeded,
                PreviousVictories = automation.SucceededPrevious,
                Defeats = automation.InQueue,
                FailedRuns = automation.Failed,
                PreviousFailedRuns = automation.FailedPrevious,
                RunsQueued = automation.Queued,
                RunsRunning = automation.Running,
                RunsWaiting = automation.Waiting,
                GamesByDay = automation.Last7Days,
                Insights = await _insightsDashboard.BuildAsync(permissions)
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult GetStats()
        {
            var automation = BuildAutomationSnapshot();
            var completed = automation.Succeeded + automation.Failed;
            var winRate = completed == 0
                ? 0
                : Math.Round((decimal)automation.Succeeded / completed * 100, 1);

            return Json(new
            {
                gamesPlayed = automation.Automations,
                victories = automation.Succeeded,
                defeats = automation.InQueue,
                winRate
            });
        }

        [HttpGet]
        public IActionResult GetGamesByPeriod(string period)
        {
            return Json(BuildRunSeries(period));
        }

        [HttpGet]
        public IActionResult Dashboards()
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ReferralTree(Guid? userId)
        {
            var resolved = await ResolveReferralTreeAsync(userId);
            if (resolved.StatusCode == 401)
                return Challenge();
            if (resolved.StatusCode == 403)
                return Forbid();
            if (resolved.StatusCode == 404 || resolved.Tree == null)
                return NotFound();

            var permissions = ViewData["Permissions"] as HashSet<string>
                ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var page = new ReferralTreePageViewModel
            {
                ViewerUserId = _currentUser.UserId ?? Guid.Empty,
                RootUserId = resolved.Tree.Id,
                RootDisplayName = resolved.Tree.DisplayName,
                CanPickUser = permissions.Contains(PermissionCodes.UsersView),
                Tree = resolved.Tree
            };

            return View(page);
        }

        [HttpGet]
        public async Task<IActionResult> GetReferralTree(Guid? userId)
        {
            var resolved = await ResolveReferralTreeAsync(userId);
            if (resolved.StatusCode == 401)
                return Unauthorized();
            if (resolved.StatusCode == 403)
                return Forbid();
            if (resolved.StatusCode == 404 || resolved.Tree == null)
                return NotFound();

            return Json(new
            {
                success = true,
                tree = resolved.Tree
            });
        }

        private async Task<(int StatusCode, ReferralTreeNodeViewModel? Tree)> ResolveReferralTreeAsync(Guid? userId)
        {
            if (_currentUser.UserId == null)
                return (401, null);

            var viewerId = _currentUser.UserId.Value;
            var rootId = userId ?? viewerId;
            var viewerIsSuper = string.Equals(
                _currentUser.RoleName?.Trim(),
                Constants.SuperRoleName,
                StringComparison.OrdinalIgnoreCase);

            var canViewRoot = rootId == viewerId || await CanViewUserAsync(rootId);
            var result = await _userService.GetReferralTreeAsync(
                rootId,
                viewerId,
                viewerIsSuper,
                canViewRoot);

            if (result.AccessDenied)
                return (403, null);

            if (result.Tree == null)
                return (404, null);

            return (200, result.Tree);
        }

        public async Task<IActionResult> MyProfile()
        {
            MyProfileViewModel model = new MyProfileViewModel();
            if (_currentUser.UserId != null)
            { 
                var user = await _userService.GetUserByIDAsync(_currentUser.UserId);

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

            return SettingsResult(userAsView);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(Guid id, MySettingsViewModel model)
        {
            model ??= new MySettingsViewModel();
            if (id == Guid.Empty)
            {
                id = model.Id;
            }

            if (id != model.Id)
            {
                return IsAjax() ? SettingsJson(false, "Not found") : NotFound();
            }

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
                            ModelState.AddModelError("OldPassword", ErrorMessages.IncorrectPassword);
                            return SettingsInvalid(model);
                        }
                    }

                    User? userSameEmail = await _userService.GetUserByEmailAsync(model.Email);
                    if (userSameEmail != null && userSameEmail.Id != user.Id)
                    {
                        ModelState.AddModelError("Email", ErrorMessages.UserWithEmailAlreadyExists);
                        return SettingsInvalid(model);
                    }
                    else if (!string.IsNullOrEmpty(model.Phone))
                    {
                        User? userSamePhone = await _userService.GetUserByPhoneAsync(model?.Phone);
                        if (userSamePhone != null && userSamePhone.Id != user.Id)
                        {
                            ModelState.AddModelError("Phone", ErrorMessages.UserWithPhoneAlreadyExists);
                            return SettingsInvalid(model);
                        }

                        var error = ModelState["Phone"]?.Errors.FirstOrDefault()?.ErrorMessage;
                        if (error != null)
                        {
                            ModelState.AddModelError("Phone", error);
                            return SettingsInvalid(model);
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
                        return SettingsInvalid(model);
                    }

                    if (IsAjax())
                    {
                        return SettingsJson(true, Messages.RecordSaved);
                    }

                    TempData["ToastMessage"] = Messages.RecordSaved;
                    return RedirectToAction("Settings", new { });
                }
            }
            return SettingsInvalid(model);
        }

        private bool IsAjax()
        {
            return string.Equals(
                Request.Headers["X-Requested-With"],
                "XMLHttpRequest",
                StringComparison.OrdinalIgnoreCase);
        }

        private IActionResult SettingsResult(MySettingsViewModel model)
        {
            if (IsAjax())
            {
                return PartialView("~/Views/Shared/Dashboard/_MySettingsPopup.cshtml", model);
            }

            ViewBag.MySettingsStandalone = true;
            return View("Settings", model);
        }

        private IActionResult SettingsJson(bool success, string? message, Dictionary<string, string>? errors = null)
        {
            return Json(new { success, message, errors });
        }

        private IActionResult SettingsInvalid(MySettingsViewModel model)
        {
            if (IsAjax())
            {
                var errors = ModelState
                    .Where(item => item.Value != null && item.Value.Errors.Count > 0)
                    .ToDictionary(
                        item => item.Key,
                        item => item.Value!.Errors[0].ErrorMessage);
                var message = errors.Values.FirstOrDefault();
                return SettingsJson(false, message, errors);
            }

            return SettingsResult(model);
        }

        private AutomationDashboardSnapshot BuildAutomationSnapshot()
        {
            var permissions = ViewData["Permissions"] as HashSet<string>
                ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!permissions.Contains(PermissionCodes.AutomationView)
                || _automationAccess.CurrentUserId == Guid.Empty)
            {
                return AutomationDashboardSnapshot.Empty;
            }

            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var defDates = VisibleAutomations().Select(item => item.CreatedDate).ToList();
            var runRows = VisibleRuns()
                .Select(item => new { item.Status, item.CreatedDate })
                .ToList();

            var succeeded = runRows.Count(item => item.Status == AutomationStatuses.Succeeded);
            var failed = runRows.Count(item => item.Status == AutomationStatuses.Failed);
            var queued = runRows.Count(item => item.Status == AutomationStatuses.Queued);
            var running = runRows.Count(item => item.Status == AutomationStatuses.Running);
            var waiting = runRows.Count(item => item.Status == AutomationStatuses.Waiting);

            return new AutomationDashboardSnapshot
            {
                Automations = defDates.Count,
                AutomationsPrevious = defDates.Count(date => date < monthStart),
                Succeeded = succeeded,
                SucceededPrevious = runRows.Count(item =>
                    item.Status == AutomationStatuses.Succeeded && item.CreatedDate < monthStart),
                Failed = failed,
                FailedPrevious = runRows.Count(item =>
                    item.Status == AutomationStatuses.Failed && item.CreatedDate < monthStart),
                Queued = queued,
                Running = running,
                Waiting = waiting,
                InQueue = queued + running + waiting,
                Last7Days = BuildRunSeries("7days", runRows.Select(item => item.CreatedDate))
            };
        }

        private IQueryable<Partnerly.Automation.Persistence.Entities.AutomationDefinition> VisibleAutomations()
        {
            var query = _automationStore.QueryDefinitions().Where(item => !item.IsDeleted);
            if (!_automationAccess.CanManage)
            {
                var owner = _automationAccess.CurrentUserId;
                query = query.Where(item => item.OwnerUserId == owner);
            }

            return query;
        }

        private IQueryable<Partnerly.Automation.Persistence.Entities.AutomationRun> VisibleRuns()
        {
            var query = _automationStore.QueryRuns();
            if (!_automationAccess.CanManage)
            {
                var owner = _automationAccess.CurrentUserId;
                query = query.Where(item => item.OwnerUserId == owner);
            }

            return query;
        }

        private List<GamesByDayViewModel> BuildRunSeries(string period, IEnumerable<DateTime>? dates = null)
        {
            var utcToday = DateTime.UtcNow.Date;
            DateTime from;
            var points = new List<DateTime>();

            switch (period)
            {
                case "30days":
                    from = utcToday.AddDays(-29);
                    points.AddRange(Enumerable.Range(0, 30).Select(i => from.AddDays(i)));
                    break;
                case "3months":
                    from = new DateTime(utcToday.Year, utcToday.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-2);
                    points.AddRange(Enumerable.Range(0, 3).Select(i => from.AddMonths(i)));
                    break;
                case "year":
                    from = new DateTime(utcToday.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    points.AddRange(Enumerable.Range(1, 12).Select(month => new DateTime(utcToday.Year, month, 1, 0, 0, 0, DateTimeKind.Utc)));
                    break;
                default:
                    from = utcToday.AddDays(-6);
                    points.AddRange(Enumerable.Range(0, 7).Select(i => from.AddDays(i)));
                    break;
            }

            var source = dates ?? VisibleRuns()
                .Where(item => item.CreatedDate >= from)
                .Select(item => item.CreatedDate)
                .ToList();

            var inRange = source.Where(date => date >= from).ToList();
            var byMonth = period == "3months" || period == "year";

            return points.Select(point => new GamesByDayViewModel
            {
                Date = point,
                Count = byMonth
                    ? inRange.Count(date => date.Year == point.Year && date.Month == point.Month)
                    : inRange.Count(date => date.Date == point.Date)
            }).ToList();
        }

        private sealed class AutomationDashboardSnapshot
        {
            public static AutomationDashboardSnapshot Empty { get; } = new()
            {
                Last7Days = Enumerable.Range(0, 7)
                    .Select(i => new GamesByDayViewModel { Date = DateTime.UtcNow.Date.AddDays(-6 + i) })
                    .ToList()
            };

            public int Automations { get; init; }
            public int AutomationsPrevious { get; init; }
            public int Succeeded { get; init; }
            public int SucceededPrevious { get; init; }
            public int Failed { get; init; }
            public int FailedPrevious { get; init; }
            public int Queued { get; init; }
            public int Running { get; init; }
            public int Waiting { get; init; }
            public int InQueue { get; init; }
            public List<GamesByDayViewModel> Last7Days { get; init; } = new();
        }
    }
}
