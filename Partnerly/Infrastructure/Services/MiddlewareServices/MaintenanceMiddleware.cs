using Partnerly.Models;

namespace Partnerly.Infrastructure.Services.MiddlewareServices
{
    public class MaintenanceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly LinkGenerator _linkGenerator;
        public MaintenanceMiddleware(RequestDelegate next, LinkGenerator linkGenerator)
        {
            _next = next;
            _linkGenerator = linkGenerator;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
        {
            if (context.User.Identity?.IsAuthenticated == true && dbContext?.SystemSettings != null)
            {
                var settings = dbContext.SystemSettings.FirstOrDefault();

                if (settings != null && settings.IsMaintenanceMode == true)
                {
                    var path = context.Request.Path.Value?.ToLower();

                    if (path != null && 
                        !context.User.IsInRole(Descriptors.Messages.Constants.Admin) && 
                        !context.User.IsInRole(Descriptors.Messages.Constants.SuperRoleName))
                    {
                        if (!path.StartsWith("/home/maintenance"))
                        {
                            var maintenanceUrl = _linkGenerator.GetPathByAction(
                                action: "Maintenance",  
                                controller: "Home"    
                            );

                            context.Response.Redirect(maintenanceUrl ?? "/Home/Maintenance");
                            return;
                        }
                    }
                }
            }
            
            await _next(context);
        }
    }
}
