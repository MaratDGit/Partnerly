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
using Partnerly.Models.DataTable.ViewModels;
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
        public IActionResult Index(UserDataTableViewModel model)
        {
            model.dataTableUsersMain = CreateTableOptions();
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
            var options = _dataTableRegistry.Get(request.TableID);

            var result = await _tableService.ExecuteAsync(
               fields => _userService.QueryEntity(fields),
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

        [HttpGet]
        public async Task<IActionResult> Export([FromQuery] DataTableRequest request, [FromQuery] string format = "csv")
        {
            if (string.IsNullOrWhiteSpace(request.TableID))
                return BadRequest(new
                {
                    success = false,
                    message = "Table ID cannot be found."
                });
            
            return await ExportTableAsync(
                request,
                format,
                fields => _userService.QueryEntity(fields),
                "users");
        }

        private DataTableOptions CreateTableOptions()
        {
            var options = 
            new DataTableOptions
            {
                Id = DataTableNames.usersTable,
                Name = FieldsDisplayNames.Users,
                Icon = "bx bx-group",

                KeyField = "Id",
                AjaxUrl = "/Users/Data",
                ExportUrl = "/Users/Export",
                
                AutoLoad = true,     
                ReloadEnabled = true,
                RememberState = true,
               
                InlineEditing = true,
                InlineUpdateUrl = "/Users/UpdateInline",

                Paging = new DataTablePagingOptions
                {
                    Enabled = true,
                    PageSize = 14,
                    PageSizeOptions = new[] { 14, 25, 50, 100 },
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
                        Field = nameof(Models.User.Selected),
                        Type = DataTableColumnType.Boolean,
                        Sortable = true,
                        Searchable = false,
                        Exportable = false,
                        Editable = false,
                        Alignment = DataTableAlignment.Center,
                    },

                    new DataTableColumn
                    {
                        Field = nameof(Models.User.FirstName),
                        Title = FieldsDisplayNames.FirstName,
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
                        Resizable = true
                    },

                    new DataTableColumn
                    {
                        Field = nameof(Models.User.LastName),
                        Title = FieldsDisplayNames.LastName,
                        Type = DataTableColumnType.Text,
                        Editable = true,
                        Resizable = true
                    },

                    new DataTableColumn
                    {
                        Field = nameof(Models.User.Email),
                        Title = FieldsDisplayNames.Email,
                        Type = DataTableColumnType.Email,
                        Resizable = true
                    },

                    new DataTableColumn
                    {
                        Field = nameof(Models.User.IsBlocked),
                        Title = FieldsDisplayNames.IsBlocked,
                        Type = DataTableColumnType.Boolean,
                        Editable = true,
                        Alignment = DataTableAlignment.Center,
                        Resizable = true
                    },

                    new DataTableColumn
                    {
                        Field = nameof(Models.User.IsOnlayn),
                        Title = FieldsDisplayNames.IsOnlayn,
                        Type = DataTableColumnType.Boolean,
                        Editable = false,
                        Alignment = DataTableAlignment.Center,
                        Resizable = true
                    },

                    new DataTableColumn
                    {
                        Field = nameof(Models.User.EmailConfirmed),
                        Title = FieldsDisplayNames.EmailConfirmed,
                        Type = DataTableColumnType.Boolean,
                        Alignment = DataTableAlignment.Center,
                        Resizable = true
                    },

                    new DataTableColumn
                    {
                        Field = nameof(Models.User.Balance),
                        Title = FieldsDisplayNames.Balance,
                        Type = DataTableColumnType.Currency,
                        Editable = true,
                        Resizable = true,

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
                        Field = nameof(Models.User.LastActivity),
                        Title = FieldsDisplayNames.LastActivity,
                        Type = DataTableColumnType.DateTime,
                        Format = "relative",
                        Resizable = true
                    },

                    new DataTableColumn
                    {
                        Field = nameof(Models.User.CreatedDate),
                        Title = FieldsDisplayNames.CreatedDate,
                        Type = DataTableColumnType.DateTime,
                        Format = "dd.MM.yyyy HH:mm",
                        Resizable = true
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
                            Label = FieldsDisplayNames.IsBlocked,
                            Type = DataTableFilterType.Boolean
                        },
                        new DataTableFilter
                        {
                            Field = "IsOnlayn",
                            Label = FieldsDisplayNames.IsOnlayn,
                            Type = DataTableFilterType.Boolean
                        },
                        //new DataTableFilter
                        //{
                        //    Field = "Email",
                        //    Label = "Email",
                        //    Type = DataTableFilterType.Text,
                        //    Placeholder = "Search email..."
                        //},
                        new DataTableFilter
                        {
                            Field = "Balance",
                            Label = FieldsDisplayNames.Balance,
                            Type = DataTableFilterType.NumberRange
                        },
                        new DataTableFilter
                        {
                            Field = "RoleId",
                            Label = FieldsDisplayNames.Role,
                            Type = DataTableFilterType.MultiSelect,
                            Placeholder = "Role Name",
                            DefaultValue = "",
                            Options = new List<DataTableFilterOption>
                            {
                                new DataTableFilterOption("5DAF66B1-8140-4D48-BC12-431086FA7144", FieldsDisplayNames.Admin),
                                new DataTableFilterOption("C3120D74-9C4C-44AA-8B0C-A1B2F53BF9A8", FieldsDisplayNames.Employee),
                                new DataTableFilterOption("52B55F50-A21A-441E-BB59-EFAB24F907AF", FieldsDisplayNames.User)
                            }
                        },

                        new DataTableFilter
                        {
                            Field = "CreatedDate",
                            Label = FieldsDisplayNames.CreatedDate,
                            Type = DataTableFilterType.DateRange
                        }
                    }
                },

                Actions = new DataTableActionOptions
                {
                    Enabled = true,

                    Items = new List<DataTableAction>
                    {
                        new DataTableAction
                        {
                            Name = "add",
                            Title = "",
                            Icon = "bx bx-plus",
                            Type = DataTableActionType.Link,
                            Placement = DataTableActionPlacement.Toolbar,
                            Url = "/Users/Create",
                            Method = "Get",
                            CssClass = "data-table-grid-action"
                        },

                        new DataTableAction
                        {
                            Name = "view",
                            Title = FieldsDisplayNames.View,
                            Icon = "bx bx-show",
                            Type = DataTableActionType.Link,
                            Placement = DataTableActionPlacement.Row,
                            Url = "/Users/Edit/{Id}",
                            Method = "GET",
                            //CssClass = "data-table-grid-action"
                           // CssClass = "btn btn-sm btn-outline-secondary"
                        },

                        new DataTableAction
                        {
                            Name = "edit",
                            Title = FieldsDisplayNames.Edit,
                            Icon = "bx bx-edit",
                            Type = DataTableActionType.InlineEdit,
                            Placement = DataTableActionPlacement.Row,
                            //CssClass = "data-table-grid-action"
                            //CssClass = "btn btn-sm btn-outline-primary"
                        },

                        new DataTableAction
                        {
                            Name = "delete",
                            Title = FieldsDisplayNames.Delete,
                            Icon = "bx bx-trash",
                            Type = DataTableActionType.Link,
                            Placement = DataTableActionPlacement.Row,
                            Url = "/Users/Delete/{Id}",
                            Method = "GET",
                            //CssClass = "data-table-grid-action"
                            //CssClass = "btn btn-sm btn-outline-danger"
                        }
                    }
                },

                Export = new DataTableExportOptions
                {
                    Enabled = true,
                    Csv = true,
                    Excel = true,
                    Print = false
                }
            };

            _dataTableRegistry.Register(options);

            return options;
        }
    }
}
