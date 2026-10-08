using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.DataTable;

namespace Partnerly.Infrastructure.Services
{
    public class DataTableRegistry : IDataTableRegistry
    {
        private readonly Dictionary<string, DataTableOptions> _tables =
            new(StringComparer.OrdinalIgnoreCase);

        public void Register(DataTableOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(options.Id))
                throw new ArgumentException(
                    "DataTableOptions.Id is required.",
                    nameof(options));

            _tables[options.Id] = options;
        }

        public DataTableOptions Get(string tableId)
        {
            if (string.IsNullOrWhiteSpace(tableId))
                throw new ArgumentException(
                    "Table ID is required.",
                    nameof(tableId));

            if (!_tables.TryGetValue(tableId, out var options))
            {
                throw new KeyNotFoundException(
                    $"DataTable '{tableId}' was not registered.");
            }

            return options;
        }

        public bool TryGet(
            string tableId,
            out DataTableOptions? options)
        {
            return _tables.TryGetValue(
                tableId,
                out options);
        }

        public bool Exists(string tableId)
        {
            return _tables.ContainsKey(tableId);
        }

        public IReadOnlyCollection<DataTableOptions> GetAll()
        {
            return _tables.Values.ToList().AsReadOnly();
        }

        public void Remove(string tableId)
        {
            _tables.Remove(tableId);
        }
    }
}