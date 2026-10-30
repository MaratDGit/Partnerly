using Partnerly.Infrastructure.Interfaces;

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

        public async Task InvokeAsync(HttpContext context, ISystemSettingsService settingsService)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var settings = await settingsService.GetCurrentAsync();

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
