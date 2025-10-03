using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Partnerly.Descriptors.Attributes.BaseAttributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class ClaimAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _claimType;
        private readonly string[] _allowedValues;

        public ClaimAuthorizeAttribute(string claimType, params string[] allowedValues)
        {
            _claimType = claimType;
            _allowedValues = allowedValues;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
                return;
            }

            var claimValue = user.Claims.FirstOrDefault(c => c.Type == _claimType)?.Value;

            if (claimValue == null || !_allowedValues.Contains(claimValue))
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
        }
    }

}
