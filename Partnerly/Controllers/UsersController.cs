using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Events;
using Partnerly.Events.BaseEvents;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Infrastructure.Services;
using Partnerly.Models;
using Partnerly.Models.GridViews;
using Partnerly.Models.ViewModels;
using System.Security.Claims;
using System.Text.Json;

namespace Partnerly.Controllers
{
    [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin, RoleTypeAttribute.Employee)]
    public class UsersController : _BaseController
    {
        protected readonly IRoleService _roleService;
        public UsersController(IEventBus eventBus, IRoleService roleService, IUserService userService, ICurrentUserService currentUser, ILogService logService)
        : base(eventBus, userService, currentUser, logService)
        {
            _roleService = roleService;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        public async Task<IActionResult> Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                User? referrer = null;
                if (model.ReferrerCode != null)
                {
                    referrer = await _userService.GetUserByRefCodeAsync(model?.ReferrerCode);
                    if (referrer == null)
                    {
                        ModelState.AddModelError("ReferrerCode", ErrorMessages.InvalidRefferalCode);
                        return View(model);
                    }
                }

                User? existingUser = null;
                existingUser = await _userService.GetUserByEmailAsync(model?.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", ErrorMessages.UserWithEmailArleadyExist);
                    return View(model);
                }

                existingUser = await _userService.GetUserByPhoneAsync(model?.Phone);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Phone", ErrorMessages.UserWithPhoneArleadyExist);
                    return View(model);
                }

                string? fullName = model?.FullName?.Trim();
                string? firstName = null;
                string? lastName = null;

                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    string[] parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                        firstName = char.ToUpper(parts[0][0]) + parts[0][1..].ToLower();
                    if (parts.Length > 1)
                        lastName = char.ToUpper(parts[1][0]) + parts[1][1..].ToLower();
                }

                if (string.IsNullOrEmpty(firstName))
                {
                    ModelState.AddModelError("FullName", $"{FieldsDisplayNames.FirstName} {ErrorMessages.FieldRequired}");
                    return View(model);
                }
                if (string.IsNullOrEmpty(lastName))
                {
                    ModelState.AddModelError("FullName", $"{FieldsDisplayNames.LastName} {ErrorMessages.FieldRequired}");
                    return View(model);
                }

                Partnerly.Models.User newUser = new Partnerly.Models.User();
                newUser.Email = model?.Email;
                newUser.Phone = model?.Phone;
                newUser.FirstName = firstName;
                newUser.LastName = lastName;
                newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model?.Password);
                newUser.ReferrerId = referrer?.Id;
                newUser.EmailConfirmed = true;

                var result = await _userService.CreateUserAsync(newUser);
                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                    return View(model);
                }
                await _eventBus.PublishAsync(new UserRegisteredEvent(result.Data.Id, result.Data.FirstName));

                TempData["ToastMessage"] = Messages.RecordSaved;
                return RedirectToAction("Edit", new { id = result?.Data?.Id });
            }
            return View(model);
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var user = await _userService.GetUserByIDAsync(id);
            if (user == null) return NotFound();

            var userAsView = await PropertyActionsHelper.CopyPropertiesAsync(user, new UserViewModel());
            if (userAsView == null) return NotFound();

            return View(userAsView);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, UserViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                User? user = await _userService.GetUserByIDAsync(model.Id);
                if (user != null)
                {
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
                    user.Email = model.Email;
                    user.Phone = model.Phone;
                    user.EmailConfirmed = model.EmailConfirmed;
                    user.IsBlocked = model.IsBlocked;
                    user.Balance = model.Balance;

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
                    return RedirectToAction("Edit", new { id = model.Id });
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var template = await _userService.GetUserByIDAsync(id);
            if (template == null) return NotFound();

            return View();
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var user = await _userService.GetUserByIDAsync(id);
            if (user != null)
            {
                ServiceResult<User?> result = await _userService.DeleteUserAsync(id);
                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                    return View();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> GetData(string tableModel)
        {
            if (tableModel == nameof(Partnerly.Models.User))
            {
                return await GetGridDataAsync<UserGridViewModel, User>("Users");
            }

            return Json(new { fields = new object[0], data = new object[0] });
        }

        protected override async Task<IEnumerable<TEntity>> GetEntitiesAsync<TEntity>()
        {
            if (typeof(TEntity) == typeof(User))
                return (IEnumerable<TEntity>)await _userService.GetAllUsersAsync();

            return Enumerable.Empty<TEntity>();

            //var users = new List<User>();
            //var random = new Random();

            //for (int i = 1; i <= 100; i++)
            //{
            //    users.Add(new User
            //    {
            //        Id = Guid.NewGuid(),
            //        FirstName = $"User{i}",
            //        Email = $"user{i}@example.com",
            //        MyReferralCode = $"REF{i:000}",
            //        Phone = $"+1234567{random.Next(100, 999)}",
            //        Balance = Math.Round((decimal)(random.NextDouble() * 1000), 2),
            //        LastActivity = DateTime.Now.AddDays(-random.Next(0, 365)),
            //        IsOnlayn = random.Next(0, 2) == 1,
            //        IsBlocked = random.Next(0, 10) == 1, // 10% заблокированных
            //        EmailConfirmed = random.Next(0, 2) == 1,
            //        CreatedDate = DateTime.Now.AddDays(-random.Next(0, 1000))
            //    });
            //}
            //return (IEnumerable<TEntity>)users;
        }

        protected override List<GridField> GetFields(object row)
        {
            var fields = new List<GridField>();
            if (row is UserGridViewModel userRow)
            {
                return new List<GridField>
                {
                    new GridField { FieldName = "select", DisplayName = $"", DefaultValue = false, Type = "checkbox"},
                    new GridField { FieldName = "userName", DisplayName = $"{FieldsDisplayNames.FirstName} {FieldsDisplayNames.LastName}", LinkTemplate = "/Users/Edit/{id}" },
                    new GridField { FieldName = "email", DisplayName = FieldsDisplayNames.Email, LinkTemplate = "/Users/Edit/{id}" },
                    new GridField { FieldName = "myReferralCode", DisplayName = FieldsDisplayNames.ReferrerCode, LinkTemplate = "/Users/Edit/{id}" },
                    new GridField { FieldName = "referrerName", DisplayName = FieldsDisplayNames.ReffererName, DefaultValue = "" },
                    new GridField { FieldName = "phone", DisplayName = FieldsDisplayNames.Phone},
                    new GridField { FieldName = "balance", DisplayName = FieldsDisplayNames.Balance, DefaultValue = "0"},
                    new GridField { FieldName = "lastActivity", DisplayName = FieldsDisplayNames.LastActivity, Format="date:lastActivity" },
                    new GridField { FieldName = "isOnlayn", DisplayName = FieldsDisplayNames.IsOnlayn, IsFilterable = true, Type = "checkbox"},
                    new GridField { FieldName = "isBlocked", DisplayName = FieldsDisplayNames.IsBlocked, IsFilterable = true, Type = "checkbox"},
                    new GridField { FieldName = "emailConfirmed", DisplayName = FieldsDisplayNames.EmailConfirmed, IsFilterable = true, Type = "checkbox"},
                    new GridField { FieldName = "roleName", DisplayName = FieldsDisplayNames.Role, IsFilterable = true},
                    new GridField { FieldName = "createdDate", DisplayName = FieldsDisplayNames.CreatedDate, IsSortable = true, IsFilterable = true, Format="date:MM/dd/yyyy" }
                };
            }

            return fields;
        }

        protected override async Task CustomizeRowAsync(object row)
        {
            if (row is UserGridViewModel userRow)
            {
                userRow.UserName = $"{userRow.FirstName} {userRow.LastName}";

                if (userRow.ReferrerId != null)
                {
                    var Referrer = await _userService.GetUserByIDAsync(userRow.ReferrerId);
                    userRow.ReferrerName = $"{Referrer?.FirstName} {Referrer?.LastName}";
                }

                Role? role = await _roleService.GetRoleByIDAsync(userRow.RoleId);
                userRow.RoleName = role?.Name;

                if (userRow.LastActivity != null)
                    userRow.LastActivity = userRow.LastActivity.Value.ToLocalTime();
            }
        }


        [HttpPost]
        public async Task<IActionResult> ExportToExcel([FromForm] string selectedIds)
        {
            var ids = JsonSerializer.Deserialize<List<Guid>>(selectedIds);

            var users = await _userService.GetAllUsersAsync();
            var data = users.Where(x => ids.Contains(x.Id)).ToList();

            return await ExportToExcel(data, "users.xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> ExportAllToExcel([FromForm] string data)
        {
            var allData = JsonSerializer.Deserialize<List<UserViewModel>>(data);
            var users = await _userService.GetAllUsersAsync();
            return await ExportToExcel(users, "users.xlsx");
        }
    }
}
