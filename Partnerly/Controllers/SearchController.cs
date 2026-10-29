using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Partnerly.Infrastructure.Interfaces;

namespace Partnerly.Controllers
{
    [Authorize]
    public class SearchController : Controller
    {
        private readonly ISiteSearchService _search;
        private readonly IPermissionService _permissions;
        private readonly ICurrentUserService _currentUser;

        public SearchController(
            ISiteSearchService search,
            IPermissionService permissions,
            ICurrentUserService currentUser)
        {
            _search = search;
            _permissions = permissions;
            _currentUser = currentUser;
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Query(string? q, CancellationToken cancellationToken)
        {
            var codes = await _permissions.GetEffectivePermissionCodesAsync(_currentUser.UserId);
            var result = await _search.SearchAsync(q, codes, cancellationToken);
            return Json(result);
        }
    }
}
