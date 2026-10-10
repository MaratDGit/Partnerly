using Partnerly.Models.DataTable;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IInlineUpdateService
    {
        InlineUpdateResult ApplyChanges<T>(
            T entity,
            IDictionary<string, string?> fields,
            DataTableOptions tableOptions);

        public InlineCreateResult Create<T>(
            IDictionary<string, string?> fields,
            DataTableOptions tableOptions)
            where T : new();
    }

    public class InlineUpdateResult
    {
        public bool Success { get; init; }

        public bool IsChanged { get; init; }

        public string? ErrorMessage { get; init; }

        public static InlineUpdateResult Changed() =>
            new()
            {
                Success = true,
                IsChanged = true
            };

        public static InlineUpdateResult NotChanged() =>
            new()
            {
                Success = true,
                IsChanged = false
            };

        public static InlineUpdateResult Error(string message) =>
            new()
            {
                Success = false,
                IsChanged = false,
                ErrorMessage = message
            };
    }

    public class InlineCreateResult
    {
        public bool Success { get; private set; }

        public bool Created { get; private set; }

        public string? ErrorMessage { get; private set; }

        public object? Entity { get; private set; }

        public static InlineCreateResult CreatedEntity(
            object entity)
        {
            return new InlineCreateResult
            {
                Success = true,
                Created = true,
                Entity = entity
            };
        }

        public static InlineCreateResult Error(
            string message)
        {
            return new InlineCreateResult
            {
                Success = false,
                Created = false,
                ErrorMessage = message
            };
        }
    }
}
