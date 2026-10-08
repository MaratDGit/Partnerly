namespace Partnerly.Infrastructure.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(Guid? id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
        IQueryable<T> QueryEntity(IEnumerable<string> fields);
        Task SaveChangesAsync();
    }
}
