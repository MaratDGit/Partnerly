using Partnerly.Models.DataTable;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IInlineUpdateService
    {
        InlineUpdateResult ApplyChanges<T>(
            T entity,
            IDictionary<string, string?> fields,
            DataTableOptions tableOptions);
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
}
