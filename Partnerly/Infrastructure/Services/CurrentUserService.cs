using Partnerly.Descriptors.Messages;
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
        public string? RoleName =>
            _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
        public string? RoleType =>
           _httpContextAccessor.HttpContext?.User?.FindFirst(Constants.ClaimTypeRoleType)?.Value;
        public string? Email =>
           _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
        public string? FirstName =>
           _httpContextAccessor.HttpContext?.User?.FindFirst(Constants.ClaimTypeFirstName)?.Value;
        public string? LastName =>
           _httpContextAccessor.HttpContext?.User?.FindFirst(Constants.ClaimTypeLastName)?.Value;
        public string? FullName =>
           _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;
        public string? ReffCode =>
           _httpContextAccessor.HttpContext?.User?.FindFirst(Constants.ClaimTypeReffCode)?.Value;
        public string? UserPhotoUrl =>
           _httpContextAccessor.HttpContext?.User?.FindFirst(Constants.ClaimTypeUserPhotoUrl)?.Value;
    }
}
