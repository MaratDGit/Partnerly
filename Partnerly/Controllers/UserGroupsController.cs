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
using Partnerly.Models.GridViews;
using Partnerly.Models.ViewModels;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text.Json;

namespace Partnerly.Controllers
{
    [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin, RoleTypeAttribute.Employee)]
    public class UserGroupsController : _BaseController
    {
        private readonly IUserGroupService _userGroupService;
        private readonly DataTableQueryService _tableService;
        private readonly IInlineUpdateService _inlineUpdateService;

        public UserGroupsController(IEventBus eventBus, IUserService userService, ICurrentUserService currentUser, ILogService logService, IUserGroupService userGroupService, IStringLocalizer<SharedResource> localizer, ISystemSettingsService systemSettingsService, DataTableQueryService tableService, IInlineUpdateService inlineUpdateService)
        : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService)
        {
            _userGroupService = userGroupService;
            _tableService = tableService;
            _inlineUpdateService = inlineUpdateService;
        }

        #region Index / Create / Edit / Delete
        public async Task<IActionResult> Index(UserGoupsDataTableViewModel model)
        {
            model.dataTableUserGroupsMain = CreateTableOptions();
            return View(model);
        }

        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
        public async Task<IActionResult> Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserGroupViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existing = await _userGroupService.GetByGroupNameAsync(model.Name);
                if (existing != null)
                {
                    ModelState.AddModelError("Name", $"{FieldsDisplayNames.Group} {ErrorMessages.UniqueValue}");
                    return View(model);
                }

                UserGroup group = new UserGroup();
                group.Name = model.Name;
                group.Description = model.Description;
                if (model.MembersGuids.Any())
                {
                    foreach (var member in model.MembersGuids)
                    {
                        var user = await _userService.GetUserByIDAsync(member);
                        if (user != null)
                        {
                            group.Members.Add(new UserGroupMember { UserId = user.Id });
                        }
                    }
                }
                var result = await _userGroupService.CreateUserGroupAsync(group);

                if (!result.Success)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
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
            var group = await _userGroupService.GetByIDAsync(id);
            if (group == null) return NotFound();

            var groupAsView = await PropertyActionsHelper.CopyPropertiesAsync(group, new UserGroupViewModel());
            var groupMembers = await _userGroupService.GetAllUsersByGroupIDAsync(group.Id);
            foreach (var member in groupMembers)
            {
                groupAsView.MembersGuids.Add(member.UserId.ToString());
            }
            if (groupAsView == null) return NotFound();

            await PrepareViewModelAsync(groupAsView);

            return View(groupAsView);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, UserGroupViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var group = await _userGroupService.GetByIDAsync(id);
                if (group != null)
                {
                    var existing = await _userGroupService.GetByGroupNameAsync(model.Name);
                    if (existing != null && existing.Id != group.Id)
                    {
                        ModelState.AddModelError("Name", $"{FieldsDisplayNames.Group} {ErrorMessages.UniqueValue}");
                        return View(model);
                    }

                    group.Name = model.Name;
                    group.Description = model.Description;
                    group.Note = model.Note;

                    List<UserGroupMember> members = new List<UserGroupMember>();
                    if (model.MembersGuids.Any())
                    {
                        foreach (var member in model.MembersGuids)
                        {
                            var user = await _userService.GetUserByIDAsync(member);
                            if (user != null)
                            {
                                members.Add(new UserGroupMember { UserId = user.Id, GroupId = id });
                            }
                        }
                    }
                    ServiceResult<UserGroup?> result = await _userGroupService.UpdateUserGroupAsync(group, members);
                    if (!result.Success)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error);
                        }
                        return View();
                    }
                }

                TempData["ToastMessage"] = Messages.RecordSaved;
                return RedirectToAction("Edit", new { id = model.Id });
            }
            return View(model);
        }

        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var group = await _userGroupService.GetByIDAsync(id);
            if (group == null) return NotFound();

            var model = new UserGroupViewModel
            {
                Id = group.Id
            };
            return View(model);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var group = await _userGroupService.GetByIDAsync(id);
            if (group != null)
            {
                ServiceResult<UserGroup?> result = await _userGroupService.DeleteUserGroupAsync(id);
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
                var query = await _userGroupService.GetAllUserGroupsAsync();

                var result = query
                    .Select(u => new
                    {
                        id = u.Id,
                        name = u.Name,
                        description = u.Description
                    })
                    .OrderBy(u => u.name)
                    .ToList();

                return Json(result);
            }

            return Json(new { });
        }

        [HttpGet]
        public async Task<JsonResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();

            var result = users.Select(u => new { id = u.Id, name = u.FirstName })
                .ToList();

            return Json(users);
        }

        #region Grid Related
        [HttpGet]
        public async Task<JsonResult> GetData(string tableModel)
        {
            if (tableModel == nameof(Partnerly.Models.UserGroup))
            {
                return await GetGridDataAsync<UserGroupGridViewModel, UserGroup>("UserGroups");
            }
            else if (tableModel == nameof(Partnerly.Models.User))
            {
                List<UserGridViewModel> userList = new List<UserGridViewModel>();

                var members = await _userGroupService.GetAllUsersGroupsAsync();
                var users = await _userService.GetAllUsersAsync();

                foreach (User? user in users)
                {
                    UserGridViewModel row = new UserGridViewModel
                    {
                        Id = user.Id,
                        UserName = $"{user.FirstName} {user.LastName}",
                        Email = user.Email,
                        CreatedDate = user.CreatedDate,
                    };
                    userList.Add(row);
                }

                if (userList.Any())
                {
                    return Json(new { fields = GetFields(userList.First()), data = userList });
                }
            }

            return Json(new { fields = new object[0], data = new object[0] });
        }

        protected override async Task<IEnumerable<TEntity>> GetEntitiesAsync<TEntity>()
        {
            if (typeof(TEntity) == typeof(UserGroup))
            {
                return (IEnumerable<TEntity>)await _userGroupService.GetAllUserGroupsAsync();
            }
            else if (typeof(TEntity) == typeof(Partnerly.Models.User))
            {

            }

            return Enumerable.Empty<TEntity>();
        }

        protected override List<GridAction> GetDefaultActions(object row)
        {
            if (row is UserGridViewModel)
            {
                return new List<GridAction>
                {
                };
            }
            else
            {
                return base.GetDefaultActions(row);
            }
        }

        protected override List<GridField> GetFields(object row)
        {
            var fields = new List<GridField>();
            if (row is UserGroupGridViewModel)
            {
                return new List<GridField>
                {
                    new GridField { FieldName = "select", DisplayName = $"", DefaultValue = false, Type = "checkbox"},
                    new GridField { FieldName = "name", DisplayName = FieldsDisplayNames.Name, LinkTemplate = "/UserGroups/Edit/{id}" },
                    new GridField { FieldName = "description", DisplayName = FieldsDisplayNames.Description, LinkTemplate = "/UserGroups/Edit/{id}" },
                    new GridField { FieldName = "createdDate", DisplayName = FieldsDisplayNames.CreatedDate, IsSortable = true, IsFilterable = true, Format="date:MM/dd/yyyy" }
                };
            }
            else if(row is UserGridViewModel)
            {
                return new List<GridField>
                {
                    new GridField { FieldName = "select", DisplayName = $"", DefaultValue = false, Type = "checkbox"},
                    new GridField { FieldName = "userName", DisplayName = FieldsDisplayNames.User, LinkTemplate = "/Users/Edit/{id}" },
                    new GridField { FieldName = "email", DisplayName = FieldsDisplayNames.Email },
                    new GridField { FieldName = "createdDate", DisplayName = FieldsDisplayNames.CreatedDate, IsSortable = true, IsFilterable = true, Format="date:MM/dd/yyyy" }
                };
            }

            return fields;
        }

        protected override async Task CustomizeRowAsync(object row)
        {
            if (row is UserGroupGridViewModel userGroupRow)
            {
            }
            if (row is UserGridViewModel userRow)
            {
            }
        }

        #region Export to excel
        [HttpPost]
        public async Task<IActionResult> ExportToExcel([FromForm] string selectedIds)
        {
            if (!string.IsNullOrWhiteSpace(selectedIds))
            {
                var ids = JsonSerializer.Deserialize<List<Guid>>(selectedIds);
                if (ids.Count > 0)
                {
                    var groups = await _userGroupService.GetAllUserGroupsAsync();
                    var data = groups.Where(x => ids.Contains(x.Id)).ToList();

                    return await ExportToExcel(data, "user-groups.xlsx");
                }
            }

            return BadRequest("No users selected.");
        }

        [HttpPost]
        public async Task<IActionResult> ExportAllToExcel([FromForm] string data)
        {
            var allData = JsonSerializer.Deserialize<List<UserViewModel>>(data);
            var users = await _userGroupService.GetAllUserGroupsAsync();
            return await ExportToExcel(users, "user-groups.xlsx");
        }
        #endregion
        #endregion

        [HttpGet]
        public async Task<IActionResult> Data([FromQuery] DataTableRequest request)
        {
            var options = CreateTableOptions();

            var result = await _tableService.ExecuteAsync(
               fields => _userGroupService.QueryEntity(fields),
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
                    message = "Group ID is required."
                });
            }

            if (!Guid.TryParse(request.Id, out var groupId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid Group ID."
                });
            }

            var group = await _userGroupService.GetByIDAsync(groupId);

            if (group == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "User group not found."
                });
            }

            var result = _inlineUpdateService.ApplyChanges(
                group,
                request.Fields,
                CreateTableOptions());

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }

            if (result.IsChanged)
            {
                await _userGroupService.UpdateUserGroupAsync(group, new List<UserGroupMember>());
            }

            return Ok(new
            {
                success = true,
                changed = result.IsChanged
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInline(
        [FromBody] DataTableInlineCreateRequest request)
        {
            if (request.Fields == null || request.Fields.Count == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "No fields were provided."
                });
            }

            var group = new Models.UserGroup();
            
            foreach (var field in request.Fields)
            {
                switch (field.Key)
                {
                    case nameof(Models.UserGroup.Name):

                        group.Name = field.Value?.Trim() ?? "";

                        break;

                    case nameof(Models.UserGroup.Description):

                        group.Description = field.Value?.Trim() ?? "";

                        break;

                    default:

                        return BadRequest(new
                        {
                            success = false,
                            message = "Field is not allowed: " + field.Key
                        });
                }
            }

            if (string.IsNullOrWhiteSpace(group.Name))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Group name is required."
                });
            }

            await _userGroupService.CreateUserGroupAsync(group);

            return Json(new
            {
                success = true,
                id = group.Id
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteInline(
        [FromBody] DataTableInlineDeleteRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Id))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Group ID is required."
                });
            }


            if (
                !Guid.TryParse(
                    request.Id,
                    out var groupId
                )
            )
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid Group ID."
                });
            }


            var group =
                await _userGroupService.GetByIDAsync(
                    groupId
                );


            if (group == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Group not found."
                });
            }


            await _userGroupService.DeleteUserGroupAsync(
                groupId
            );


            return Json(new
            {
                success = true
            });
        }

        [HttpGet]
        public async Task<IActionResult> Export(
        [FromQuery] DataTableRequest request,
        [FromQuery] string format = "csv")
        {
            var options =
                CreateTableOptions();

            var items =
                await _tableService.ExecuteExportAsync(
                    fields =>
                        _userGroupService.QueryEntity(fields),
                    request,
                    options
                );

            if (string.Equals(
                format,
                "excel",
                StringComparison.OrdinalIgnoreCase))
            {
                var excelExporter =
                    new DataTableExcelExporter();

                var bytes =
                    excelExporter.Export(
                        items,
                        options.Columns
                    );

                var fileName =
                    "groups-" +
                    DateTime.Now.ToString(
                        "yyyyMMdd-HHmmss"
                    ) +
                    ".xlsx";

                return File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }

            var csvExporter =
                new DataTableCsvExporter();

            var csvBytes =
                csvExporter.Export(
                    items,
                    options.Columns
                );

            var csvFileName =
                "groups-" +
                DateTime.Now.ToString(
                    "yyyyMMdd-HHmmss"
                ) +
                ".csv";

            return File(
                csvBytes,
                "text/csv; charset=utf-8",
                csvFileName
            );
        }

        private DataTableOptions CreateTableOptions()
        {
            return new DataTableOptions
            {
                Id = "userGroupsTable",
                Name = FieldsDisplayNames.Users,
                Icon = "bx bx-group",

                KeyField = "Id",
                AjaxUrl = "/UserGroups/Data",
                ExportUrl = "/UserGroups/Export",

                AutoLoad = true,
                ReloadEnabled = true,
                RememberState = true,

                InlineEditing = true,
                InlineUpdateUrl = "/UserGroups/UpdateInline",

                InlineCreateUrl = "/UserGroups/CreateInline",
                InlineDeleteUrl = "/UserGroups/DeleteInline",

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
                        Required = true,
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
                        Field = nameof(Models.UserGroup.Name),
                        Title = FieldsDisplayNames.Name,
                        Type = DataTableColumnType.Link,
                        UrlTemplate = "/UserGroups/Edit/{Id}",

                        Editable = true,
                        Visible = true,
                        Required = true,
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
                        Field = nameof(Models.UserGroup.Description),
                        Title = FieldsDisplayNames.Description,
                        Type = DataTableColumnType.Text,
                        Editable = true,
                        Resizable = true
                    },

                    new DataTableColumn
                    {
                        Field = nameof(Models.UserGroup.UpdatedDate),
                        Title = FieldsDisplayNames.UpdatedDate,
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
                            Field = nameof(Models.User.CreatedDate),
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
                        //new DataTableAction
                        //{
                        //    Name = "add",
                        //    Title = "",
                        //    Icon = "bx bx-plus",
                        //    Type = DataTableActionType.Link,
                        //    Placement = DataTableActionPlacement.Toolbar,
                        //    Url = "/UserGroups/Create",
                        //    Method = "Get",
                        //    CssClass = "data-table-grid-action"
                        //},

                        new DataTableAction
                        {
                            Name = "add",
                            Title = "",
                            Icon = "bx bx-plus",
                            Type = DataTableActionType.InlineAdd,
                            Url = "/UserGroups/CreateInline",
                            Placement = DataTableActionPlacement.Toolbar,
                            CssClass = "data-table-grid-action"
                        },

                        new DataTableAction
                        {
                            Name = "view",
                            Title = FieldsDisplayNames.View,
                            Icon = "bx bx-show",
                            Type = DataTableActionType.Link,
                            Placement = DataTableActionPlacement.Row,
                            Url = "/UserGroups/Edit/{Id}",
                            Method = "GET",
                            // CssClass = "btn btn-sm btn-outline-secondary"
                        },

                        new DataTableAction
                        {
                            Name = "edit",
                            Title = FieldsDisplayNames.Edit,
                            Icon = "bx bx-edit",
                            Type = DataTableActionType.InlineEdit,
                            Placement = DataTableActionPlacement.Row,
                            
                            //CssClass = "btn btn-sm btn-outline-primary"
                        },

                        //new DataTableAction
                        //{
                        //    Name = "delete",
                        //    Title = FieldsDisplayNames.Delete,
                        //    Icon = "bx bx-trash",
                        //    Type = DataTableActionType.Link,
                        //    Placement = DataTableActionPlacement.Row,
                        //    Url = "/UserGroups/Delete/{Id}",
                        //    Method = "GET",
                        //    //CssClass = "btn btn-sm btn-outline-danger"
                        //},

                        new DataTableAction
                        {
                            Name = "delete",
                            Title = FieldsDisplayNames.Delete,
                            Icon = "bx bx-trash",
                            Type = DataTableActionType.InlineDelete,
                            Placement = DataTableActionPlacement.Row,
                            Url = "/UserGroups/DeleteInline",
                            Method = "POST",
                            Confirm = true,
                            ConfirmMessage =
                                "Are you sure you want to delete this group?",
                            CssClass = "btn btn-sm btn-outline-danger"
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
        }
    }
}
