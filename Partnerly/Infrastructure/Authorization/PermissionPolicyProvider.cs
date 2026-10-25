using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Partnerly.Infrastructure.Authorization
{
    public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallback;

        public PermissionPolicyProvider(
            IOptions<AuthorizationOptions> options)
        {
            _fallback = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
            => _fallback.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
            => _fallback.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            const string prefix = "Permission:";

            if (!policyName.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase))
            {
                return _fallback.GetPolicyAsync(policyName);
            }

            var permissionCode = policyName[prefix.Length..];

            if (string.IsNullOrWhiteSpace(permissionCode))
                return Task.FromResult<AuthorizationPolicy?>(null);

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(
                    new PermissionRequirement(permissionCode))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
    }
}
