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
using Partnerly.Models.GridViews;
using Partnerly.Models.ViewModels;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace Partnerly.Controllers
{
    [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin, RoleTypeAttribute.Employee)]
    public class UsersController : _BaseController
    {
        protected readonly IRoleService _roleService;
        private readonly DataTableQueryService _tableService;
        public UsersController(IEventBus eventBus, IRoleService roleService, IUserService userService, ICurrentUserService currentUser, ILogService logService, IStringLocalizer<SharedResource> localizer, ISystemSettingsService systemSettingsService, DataTableQueryService tableService)
        : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService)
        {
            _roleService = roleService;
            _tableService = tableService;
        }

        #region Index / Create / Edit / Delete
        public async Task<IActionResult> Index()
        {
            return View();
        }

        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
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

        #region Grid Related
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
            {
                var viewRole = await _roleService.GetRoleByNameAsync(RoleTypeAttribute.User);
                var users = await _userService.GetAllUsersAsync();

                if (!base.HasPermision(RoleTypeAttribute.Delete))
                {
                    users = users.Where(_ => _.RoleId == viewRole?.Id);
                }

                return (IEnumerable<TEntity>)users;
            }

            return Enumerable.Empty<TEntity>();
        }

        protected override List<GridField> GetFields(object row)
        {
            var fields = new List<GridField>();
            if (row is UserGridViewModel userRow)
            {
                return new List<GridField>
                {
                    new GridField { FieldName = "select", DisplayName = $"", DefaultValue = false, Type = "checkbox", IsVisible = base.HasPermision(RoleTypeAttribute.Delete)},
                    new GridField { FieldName = "userName", DisplayName = $"{FieldsDisplayNames.FullName}", LinkTemplate = "/Users/Edit/{id}" },
                    new GridField { FieldName = "email", DisplayName = FieldsDisplayNames.Email, LinkTemplate = "/Users/Edit/{id}" },
                    new GridField { FieldName = "myReferralCode", DisplayName = FieldsDisplayNames.ReferrerCode, LinkTemplate = "/Users/Edit/{id}" },
                    //new GridField { FieldName = "referrerName", DisplayName = FieldsDisplayNames.ReffererName, DefaultValue = "" },
                    new GridField { FieldName = "phone", DisplayName = FieldsDisplayNames.Phone, Format = "phone:"},
                    //new GridField { FieldName = "balance", DisplayName = FieldsDisplayNames.Balance, DefaultValue = "0"},
                    new GridField { FieldName = "lastActivity", DisplayName = FieldsDisplayNames.LastActivity,  IsSortable = true, Format="date:lastActivity" },
                    new GridField { FieldName = "isOnlayn", DisplayName = FieldsDisplayNames.IsOnlayn, IsFilterable = true, Type = "checkbox", DefaultValue = false},
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

                //if (userRow.ReferrerId != null)
                //{
                //    var Referrer = await _userService.GetUserByIDAsync(userRow.ReferrerId);
                //    userRow.ReferrerName = $"{Referrer?.FirstName} {Referrer?.LastName}";
                //}

                Role? role = await _roleService.GetRoleByIDAsync(userRow.RoleId);
                userRow.RoleName = role?.Name;

                if (userRow.LastActivity != null)
                    userRow.LastActivity = userRow.LastActivity.Value.ToLocalTime();
            }
        }

        [HttpPost]
        public async Task<IActionResult> ExportToExcel([FromForm] string selectedIds)
        {
            if (!string.IsNullOrWhiteSpace(selectedIds))
            {
                var ids = JsonSerializer.Deserialize<List<Guid>>(selectedIds);
                if (ids.Count > 0)
                {
                    var users = await _userService.GetAllUsersAsync();
                    var data = users.Where(x => ids.Contains(x.Id)).ToList();

                    return await ExportToExcel(data, "users.xlsx");
                }
            }

            return BadRequest("No users selected.");
        }

        [HttpPost]
        public async Task<IActionResult> ExportAllToExcel([FromForm] string data)
        {
            var users = await _userService.GetAllUsersAsync();
            return await ExportToExcel(users, "users.xlsx");
        }
        #endregion


        public IActionResult DataTableCustom()
        {
            var options = CreateTableOptions();

            return View(options);
        }

        [HttpGet]
        public async Task<IActionResult> Data([FromQuery] DataTableRequest request)
        {
            var options = CreateTableOptions();

            var result = await _tableService.ExecuteAsync(
               fields => _userService.QueryUsers(fields),
               request,
               options);

            return Json(result);
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

        private DataTableOptions CreateTableOptions()
        {
            return new DataTableOptions
            {
                Id = "usersTable",
                Name = "Users",
                AjaxUrl = "/Users/Data",
                KeyField = "Id",

                AutoLoad = true,     
                ReloadEnabled = true,
               
                InlineEditing = true,
                InlineUpdateUrl = "/Users/UpdateInline",

                Paging = new DataTablePagingOptions
                {
                    Enabled = true,
                    PageSize = 10,
                    PageSizeOptions = new[] { 10, 25, 50, 100 },
                    ShowInfo = true,
                    ShowPageSize = false,
                },

                Search = new DataTableSearchOptions
                {
                    Enabled = true,
                    Placeholder = FieldsDisplayNames.DataTableSearch,
                    MinLength = 3,
                    DebounceMs = 300,
                },

                Rows = new DataTableRowOptions
                {
                    Selectable = true,
                    Clickable = true,
                    Hover = true,
                    Striped = false,
                },
 
                Columns = new List<DataTableColumn>
                {
                    new DataTableColumn
                    {
                        Field = "Id",
                        Title = "ID",
                        Type = DataTableColumnType.Number,
                        Visible = false,
                    },

                    new DataTableColumn
                    {
                        Field = "FirstName",
                        Title = "First Name",
                        Type = DataTableColumnType.Link,
                        UrlTemplate = "/Users/Edit/{Id}",

                        Editable = true,                  
                        Visible = true,
                        Sortable = true,
                        Searchable = true,
                        Exportable = true,
                        Width = "",
                        Format = "",
                        EmptyText = "-",
                        Alignment = DataTableAlignment.Left,
                        CssClass = "",
                    },

                    new DataTableColumn
                    {
                        Field = "LastName",
                        Title = "Last Name",
                        Type = DataTableColumnType.Text,
                        Editable = true
                    },

                    new DataTableColumn
                    {
                        Field = "Email",
                        Title = "Email",
                        Type = DataTableColumnType.Email
                    },

                    new DataTableColumn
                    {
                        Field = "IsBlocked",
                        Title = "Is Blocked",
                        Type = DataTableColumnType.Boolean,
                        Editable = true,
                        Alignment = DataTableAlignment.Center,
                    },

                    new DataTableColumn
                    {
                        Field = "Balance",
                        Title = "Balance",
                        Type = DataTableColumnType.Currency,
                        Editable = true,
                        
                        ConditionalStyles = new List<DataTableConditionalStyle> 
                        {
                            new DataTableConditionalStyle
                            {
                                Conditions = new List<DataTableConditionalCondition>
                                {
                                    new DataTableConditionalCondition
                                    {
                                        Operator = DataTableConditionalOperator.GreaterThanOrEqual,
                                        Value = "500"
                                    },
                                    new DataTableConditionalCondition
                                    {
                                        Operator = DataTableConditionalOperator.LessThan,
                                        Value = "5000"
                                    }
                                },

                                CssClass = "badge bg-label-warning fs-tiny rounded-pill ms-auto text-uppercase",
                            },
                            new DataTableConditionalStyle
                            {
                                Conditions = new List<DataTableConditionalCondition>
                                {
                                    new DataTableConditionalCondition
                                    {
                                        Operator = DataTableConditionalOperator.LessThanOrEqual,
                                        Value = "500"
                                    },
                                },

                                CssClass = "badge bg-label-danger fs-tiny rounded-pill ms-auto text-uppercase",
                            },
                            new DataTableConditionalStyle
                            {
                                Conditions = new List<DataTableConditionalCondition>
                                {
                                    new DataTableConditionalCondition
                                    {
                                        Operator = DataTableConditionalOperator.GreaterThanOrEqual,
                                        Value = "5000"
                                    },
                                },

                                CssClass = "badge bg-label-success fs-tiny rounded-pill ms-auto text-uppercase",
                            },
                        },
                        
                    },

                    new DataTableColumn
                    {
                        Field = "EmailConfirmed",
                        Title = "Email Confirmed",
                        Type = DataTableColumnType.Boolean,
                        Alignment = DataTableAlignment.Center,
                    },

                    new DataTableColumn
                    {
                        Field = "UpdatedDate",
                        Title = "Updated",
                        Type = DataTableColumnType.DateTime,
                        Format = "relative"
                    },

                    new DataTableColumn
                    {
                        Field = "CreatedDate",
                        Title = "Created",
                        Type = DataTableColumnType.DateTime,
                        Format = "dd.MM.yyyy HH:mm"
                    }
                },

                Sorting = new DataTableSortOptions
                {
                    Enabled = true,
                    DefaultField = "CreatedDate",
                    DefaultDirection = "desc"
                },

                Filters = new DataTableFilterOptions
                {
                    Enabled = true,
                    Items = new List<DataTableFilter>
                    {
                        new DataTableFilter
                        {
                            Field = "IsBlocked",
                            Label = "Blocked",
                            Type = DataTableFilterType.Boolean
                        },
                        new DataTableFilter
                        {
                            Field = "Email",
                            Label = "Email",
                            Type = DataTableFilterType.Text,
                            Placeholder = "Search email..."
                        },
                        new DataTableFilter
                        {
                            Field = "Balance",
                            Label = "Balance",
                            Type = DataTableFilterType.NumberRange
                        },
                        new DataTableFilter
                        {
                            Field = "RoleId",
                            Label = "Role",
                            Type = DataTableFilterType.MultiSelect,
                            Placeholder = "Role Name",
                            DefaultValue = "",
                            Options = new List<DataTableFilterOption>
                            {
                                new DataTableFilterOption("5DAF66B1-8140-4D48-BC12-431086FA7144", "Admin"),
                                new DataTableFilterOption("C3120D74-9C4C-44AA-8B0C-A1B2F53BF9A8", "Manager"),
                                new DataTableFilterOption("52B55F50-A21A-441E-BB59-EFAB24F907AF", "User")
                            }
                        },

                        new DataTableFilter
                        {
                            Field = "CreatedDate",
                            Label = "Created",
                            Type = DataTableFilterType.DateRange
                        }
                    }
                },

                Actions = new DataTableActionOptions
                {
                    Enabled = true,

                    Add = true,
                    AddUrl = "/Users/Create",

                    View = true,
                    ViewUrl = "/Users/View",

                    Edit = true,
                    EditUrl = "/Users/Edit",

                    Delete = true,
                    DeleteUrl = "/Users/Delete",

                    BulkDelete = true,
                    InlineAdd = true,
                    InlineDelete = true,
                },

                Export = new DataTableExportOptions
                {
                    Enabled = true,
                    Csv = true,
                    Excel = true,
                    Print = true
                }
            };
        }

    }
}
