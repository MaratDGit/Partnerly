using Microsoft.AspNetCore.Authentication;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using System.Security.Claims;

namespace Partnerly.Infrastructure.Services.MiddlewareServices
{
    public class RoleChangeMiddleware
    {
        private readonly RequestDelegate _next;

        public RoleChangeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var loginTimeClaim = context.User.FindFirst("LoginTime")?.Value;

                if (Guid.TryParse(userIdClaim, out var userId) && DateTime.TryParse(loginTimeClaim, out var loginTime) && dbContext?.Users != null)
                {
                    var user = await dbContext.Users.FindAsync(userId);
                    if (user?.LastRoleUpdateTime != null && user.LastRoleUpdateTime.Value.ToLocalTime() > loginTime.ToLocalTime())
                    {
                        await context.SignOutAsync();
                        context.Response.Redirect("/Account/Login");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }

}
