namespace Partnerly.Infrastructure.Interfaces
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        string? Role { get; }
    }
}
