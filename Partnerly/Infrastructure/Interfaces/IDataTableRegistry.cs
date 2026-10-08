using Partnerly.Models.DataTable;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IDataTableRegistry
    {
        void Register(DataTableOptions options);

        DataTableOptions Get(string tableId);

        bool TryGet(string tableId, out DataTableOptions? options);

        bool Exists(string tableId);

        IReadOnlyCollection<DataTableOptions> GetAll();

        void Remove(string tableId);
    }
}