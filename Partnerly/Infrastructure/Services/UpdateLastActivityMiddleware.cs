using Partnerly.Models;
using System.Security.Claims;

namespace Partnerly.Infrastructure.Services
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
            if (context.User.Identity.IsAuthenticated)
            {
                var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (Guid.TryParse(userIdStr, out Guid userId))
                {
                    var user = await dbContext.Users.FindAsync(userId);
                    if (user != null)
                    {
                        user.LastActivity = DateTime.UtcNow;
                        user.IsOnlayn = true;
                        dbContext.Users.Update(user);
                        dbContext.SaveChanges();
                    }
                }
            }

            await _next(context);
        }
    }
}
