namespace Partnerly.Infrastructure.Interfaces
{
    public interface IAuditableEntity
    {
        Guid? CreatedBy { get; set; }
        Guid? UpdatedBy { get; set; }
        DateTime? CreatedDate { get; set; }
        DateTime? UpdatedDate { get; set; }
    }
}
