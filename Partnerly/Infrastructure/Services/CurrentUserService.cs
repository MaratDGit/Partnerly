using Partnerly.Infrastructure.Interfaces;
using System.Security.Claims;

namespace Partnerly.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId =>
            Guid.TryParse(_httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

        public string? Role =>
            _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
    }
}
