namespace Partnerly.Infrastructure.Interfaces
{
    public interface IAuditableViewModel : IAuditableEntity
    {
        string? CreatedByUserName { get; set; }
        string? UpdatedByUserName { get; set; }
    }
}
