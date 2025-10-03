namespace Partnerly.Infrastructure.Interfaces
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        string? RoleName { get; }
        string? RoleType { get; }
        string? Email { get; }
        string? FirstName { get; }
        string? LastName { get; }
        string? FullName { get; }
        string? ReffCode { get; }
        string? UserPhotoUrl { get; }
    }
}
