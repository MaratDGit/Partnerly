using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Descriptors.Messages;
using Partnerly.Events.BaseEvents;
using Partnerly.Helpers;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Infrastructure.Services;
using Partnerly.Models;
using Partnerly.Models.DataTable;
using Partnerly.Models.ViewModels;
using System.Security.Claims;

namespace Partnerly.Controllers
{
    [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin, RoleTypeAttribute.Employee)]
    public class UsersController : _BaseController
    {
        protected readonly IRoleService _roleService;
        
        public UsersController(IEventBus eventBus, IRoleService roleService, IUserService userService, ICurrentUserService currentUser, 
            ILogService logService, IStringLocalizer<SharedResource> localizer, ISystemSettingsService systemSettingsService, 
            DataTableQueryService tableService, IDataTableRegistry dataTableRegistry)
        : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService, tableService, dataTableRegistry)
        {
            _roleService = roleService;
        }

        #region Index / Create / Edit / Delete
        public IActionResult Index()
        {
            UserViewModel model = new UserViewModel();
            if (!_dataTableRegistry.Exists(DataTableNames.usersTable))
            {
                model.users = UserViewModel.CreateTableOptions(DataTableNames.usersTable, _dataTableRegistry);
            }
            else
            {
                var options = _dataTableRegistry.Get(DataTableNames.usersTable);
                model.users = options;
            }
            
            return View(model);
        }

        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
        public IActionResult Create()
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
                        ModelState.AddModelError("TermsConditions", error);
                    }
                    return View(model);
                }

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

            await PrepareViewModelAsync(userAsView);

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
                    user.Note = model.Note;

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

        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var template = await _userService.GetUserByIDAsync(id);
            if (template == null) return NotFound();


            var model = new UserViewModel
            {
                Id = template.Id
            };
            return View(model);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
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
        #endregion

        [HttpGet]
        public async Task<IActionResult> GetLookupList(string fieldName)
        {
            if (fieldName == "Name")
            {
                var query = await _userService.GetAllUsersAsync();

                var result = query
                    .Select(u => new
                    {
                        id = u.Id,
                        name = u.FirstName + " " + u.LastName,
                        email = u.Email
                    })
                    .OrderBy(u => u.name)
                    .ToList();

                return Json(result);
            }

            return Json(new { });
        }

        [HttpGet]
        public async Task<IActionResult> Data([FromQuery] DataTableRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TableID))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Table ID is required."
                });
            }

            var options = _dataTableRegistry.Get(request.TableID);

            if (options == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Unknown table."
                });
            }

            if (request.TableID == DataTableNames.usersTable)
            {
                var res = await _tableService.ExecuteAsync(
                   fields => _userService.QueryEntity(fields),
                   request,
                   options);

                return Json(res);
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Unknown table."
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInline(
        [FromBody] DataTableInlineUpdateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Id))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "User ID is required."
                });
            }

            if (!Guid.TryParse(request.Id, out var userId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid user ID."
                });
            }

            var user = await _userService.GetUserByIDAsync(userId);

            if (user == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "User not found."
                });
            }

            foreach (var field in request.Fields)
            {
                switch (field.Key)
                {
                    case "FirstName":
                        user.FirstName = field.Value;
                        break;

                    case "LastName":
                        user.LastName = field.Value;
                        break;

                    case "IsBlocked":
                        if (bool.TryParse(field.Value, out var boolValue))
                        {
                            user.IsBlocked = boolValue;
                        }
                        break;

                    case "Balance":

                        if (!decimal.TryParse(
                            field.Value,
                            out var balance))
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = "Invalid balance."
                            });
                        }

                        user.Balance = balance;
                        break;

                    default:
                        return BadRequest(new
                        {
                            success = false,
                            message = "Field is not editable: " +
                                      field.Key
                        });
                }
            }

            await _userService.UpdateUserAsync(user);

            return Json(new
            {
                success = true
            });
        }

        [HttpGet]
        public async Task<IActionResult> Export([FromQuery] DataTableRequest request, [FromQuery] string format = "csv")
        {
            if (string.IsNullOrWhiteSpace(request.TableID))
                return BadRequest(new
                {
                    success = false,
                    message = "Table ID cannot be found."
                });
            
            if (request.TableID == DataTableNames.usersTable)
            {
                return await ExportTableAsync(
                            request,
                            format,
                            fields => _userService.QueryEntity(fields),
                            "users");
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Unknown table."
                });
            }
        }
    }
}
