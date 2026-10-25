using Microsoft.AspNetCore.Authorization;
using Partnerly.Infrastructure.Interfaces;
using System.Security.Claims;

namespace Partnerly.Infrastructure.Authorization
{
    public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IPermissionService _permissionService;

        public PermissionAuthorizationHandler(
            IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            var userIdValue = context.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdValue, out var userId))
                return;

            var allowed = await _permissionService.HasPermissionAsync(
                userId,
                requirement.PermissionCode);

            if (allowed)
                context.Succeed(requirement);
        }
    }
}
