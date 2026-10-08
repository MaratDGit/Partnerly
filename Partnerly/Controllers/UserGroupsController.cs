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
    public class UserGroupsController : _BaseController
    {
        private readonly IUserGroupService _userGroupService;
        private readonly IInlineUpdateService _inlineUpdateService;

        public UserGroupsController(
            IEventBus eventBus, IUserService userService, ICurrentUserService currentUser, ILogService logService, 
            IUserGroupService userGroupService, IStringLocalizer<SharedResource> localizer, ISystemSettingsService systemSettingsService,
            IInlineUpdateService inlineUpdateService, DataTableQueryService tableService, IDataTableRegistry dataTableRegistry)
        : base(eventBus, userService, currentUser, logService, localizer, systemSettingsService, tableService, dataTableRegistry)
        {
            _userGroupService = userGroupService;
            _inlineUpdateService = inlineUpdateService;
        }

        #region Index / Create / Edit / Delete
        public IActionResult Index(UserGoupsDataTableViewModel model)
        {
            model.dataTableUserGroupsMain = CreateTableOptions();
            return View(model);
        }

        [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
        public IActionResult Create()
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
        public async Task<IActionResult> Data([FromQuery] DataTableRequest request)
        {
            var options = _dataTableRegistry.Get(request.TableID);

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

            var options = _dataTableRegistry.Get(request.TableID);
            var result = _inlineUpdateService.ApplyChanges(
                group,
                request.Fields,
                options);

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
                fields => _userGroupService.QueryEntity(fields),
                "groups");
        }

        private DataTableOptions CreateTableOptions()
        {
            var options = new DataTableOptions
            {
                Id = DataTableNames.userGroupsTable,
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

                //InlineCreateUrl = "/UserGroups/CreateInline",
                //InlineDeleteUrl = "/UserGroups/DeleteInline",

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

            _dataTableRegistry.Register(options);
            
            return options;
        }
    }
}
