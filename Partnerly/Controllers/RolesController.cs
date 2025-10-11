using Microsoft.AspNetCore.Mvc;
using Partnerly.Descriptors.Attributes;
using Partnerly.Descriptors.Attributes.BaseAttributes;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.ViewModels;
using System.Security.Claims;

namespace Partnerly.Controllers
{
    [ClaimAuthorize(ClaimTypes.Role, RoleTypeAttribute.Admin)]
    public class RolesController : _BaseController
    {
        protected readonly IRoleService _roleService;
        public RolesController(IUserService userService, ICurrentUserService currentUser, IRoleService roleService)
        : base(userService, currentUser)
        {
            _roleService = roleService;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        //public async Task<IActionResult> Edit(Guid id)
        //{
        //    var user = await _userService.GetUserByIDAsync(id);
        //    if (user == null) return NotFound();

        //    var userAsView = await PropertyActionsHelper.CopyPropertiesAsync(user, new UserRolesViewModel());
        //    if (userAsView == null) return NotFound();

        //    return View(userAsView);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(Guid id, UserRolesViewModel model)
        //{
        //    if (id != model.Id) return NotFound();

        //    if (ModelState.IsValid)
        //    {
        //        User? user = await _userService.GetUserByIDAsync(model.Id);
                
        //    }
        //    return View(model);
        //}

        //[HttpGet]
        //public async Task<JsonResult> GetData(string tableModel)
        //{
        //    if (tableModel == "UserRoles")
        //    {
        //        return await GetGridDataAsync<UserGridViewModel, User>("Users");
        //    }

        //    return Json(new { fields = new object[0], data = new object[0] });
        //}

        //protected override async Task<IEnumerable<TEntity>> GetEntitiesAsync<TEntity>()
        //{
        //    if (typeof(TEntity) == typeof(User))
        //        return (IEnumerable<TEntity>)await _userService.GetAllUsersAsync();

        //    return Enumerable.Empty<TEntity>();
        //}

        //protected override List<GridField> GetFields(object row)
        //{
        //    var fields = new List<GridField>();
        //    if (row is UserGridViewModel userRow)
        //    {
        //        return new List<GridField>
        //        {
        //            new GridField { FieldName = "select", DisplayName = $"", DefaultValue = false, Type = "checkbox"},
        //            new GridField { FieldName = "userName", DisplayName = $"{FieldsDisplayNames.FirstName} {FieldsDisplayNames.LastName}", LinkTemplate = "/Users/Edit/{id}" },
        //            new GridField { FieldName = "email", DisplayName = FieldsDisplayNames.Email, LinkTemplate = "/Users/Edit/{id}" },
        //            new GridField { FieldName = "myReferralCode", DisplayName = FieldsDisplayNames.ReferrerCode, LinkTemplate = "/Users/Edit/{id}" },
        //            new GridField { FieldName = "referrerName", DisplayName = FieldsDisplayNames.ReffererName, DefaultValue = "" },
        //            new GridField { FieldName = "phone", DisplayName = FieldsDisplayNames.Phone},
        //            new GridField { FieldName = "balance", DisplayName = FieldsDisplayNames.Balance, DefaultValue = "0"},
        //            new GridField { FieldName = "lastActivity", DisplayName = FieldsDisplayNames.LastActivity, Format="date:lastActivity" },
        //            new GridField { FieldName = "isOnlayn", DisplayName = FieldsDisplayNames.IsOnlayn, Type = "checkbox"},
        //            new GridField { FieldName = "isBlocked", DisplayName = FieldsDisplayNames.IsBlocked, Type = "checkbox"},
        //            new GridField { FieldName = "emailConfirmed", DisplayName = FieldsDisplayNames.EmailConfirmed, Type = "checkbox"},
        //            new GridField { FieldName = "roleName", DisplayName = FieldsDisplayNames.Role},
        //            new GridField { FieldName = "createdDate", DisplayName = FieldsDisplayNames.CreatedDate, IsSortable = true, Format="date:MM/dd/yyyy" }
        //        };
        //    }

        //    return fields;
        //}

        //protected override async Task CustomizeRowAsync(object row)
        //{
        //    if (row is UserGridViewModel userRow)
        //    {
        //        userRow.UserName = $"{userRow.FirstName} {userRow.LastName}";

        //        if (userRow.ReferrerId != null)
        //        {
        //            var Referrer = await _userService.GetUserByIDAsync(userRow.ReferrerId);
        //            userRow.ReferrerName = $"{Referrer?.FirstName} {Referrer?.LastName}";
        //        }

        //        Role? role = await _roleService.GetRoleByIDAsync(userRow.RoleId);
        //        userRow.RoleName = role?.Name;

        //        if (userRow.LastActivity != null)
        //            userRow.LastActivity = userRow.LastActivity.Value.ToLocalTime();
        //    }
        //}


        //[HttpPost]
        //public async Task<IActionResult> ExportToExcel([FromForm] string selectedIds)
        //{
        //    //var ids = JsonSerializer.Deserialize<List<Guid>>(selectedIds);

        //    //var users = await _userService.GetAllUsersAsync();
        //    //var data = users.Where(x => ids.Contains(x.Id)).ToList();

        //    //return await ExportToExcel(data, "users.xlsx");
        //}

        //[HttpPost]
        //public async Task<IActionResult> ExportAllToExcel([FromForm] string data)
        //{
        //    var allData = JsonSerializer.Deserialize<List<UserViewModel>>(data);
        //    var users = await _userService.GetAllUsersAsync();
        //    return await ExportToExcel(users, "users.xlsx");
        //}
    }
}
