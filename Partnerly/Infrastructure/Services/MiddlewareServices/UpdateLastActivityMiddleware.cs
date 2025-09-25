using Partnerly.Models;
using System.Security.Claims;

namespace Partnerly.Infrastructure.Services.MiddlewareServices
{
    public class UpdateLastActivityMiddleware
    {
        private readonly RequestDelegate _next;

        public UpdateLastActivityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (Guid.TryParse(userIdStr, out Guid userId))
                {
                    if (dbContext?.Users != null)
                    {
                        var user = await dbContext.Users.FindAsync(userId);
                        if (user != null && (user.LastActivity == null || (DateTime.UtcNow - user.LastActivity.Value).TotalMinutes >= 2))
                        {
                            user.LastActivity = DateTime.UtcNow;
                            user.IsOnlayn = true;
                            dbContext.Users.Update(user);
                            dbContext.SkipValidations = true;
                            await dbContext.SaveChangesAsync();
                            dbContext.SkipValidations = false;
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}
