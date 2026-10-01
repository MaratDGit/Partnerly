using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq.Expressions;

namespace Partnerly.Models.DataTable
{
    public class DataTableQueryService
    {
        private readonly DataTablePropertyResolver _propertyResolver;

        public DataTableQueryService(
            DataTablePropertyResolver propertyResolver)
        {
            _propertyResolver = propertyResolver;
        }

        public async Task<DataTableResponse<T>> ExecuteAsync<T>(
            Func<IEnumerable<string>, IQueryable<T>> queryFactory,
            DataTableRequest request,
            DataTableOptions options)
        {
            request = request.Normalize();

            var fields = GetRequiredFields(options);

            var query = queryFactory(fields);

            query = ApplySearch(
                query,
                request,
                options);

            query = ApplyFilters(
                query,
                request,
                options);

            var total = await query.CountAsync();

            query = ApplySorting(
                query,
                request,
                options);

            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new DataTableResponse<T>
            {
                Items = items,
                Total = total,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        // =========================================================
        // SEARCH
        // =========================================================

        private IQueryable<T> ApplySearch<T>(
            IQueryable<T> query,
            DataTableRequest request,
            DataTableOptions options)
        {
            if (!options.Search.Enabled)
            {
                return query;
            }

            if (string.IsNullOrWhiteSpace(request.Search))
            {
                return query;
            }

            var search = request.Search.Trim();

            if (search.Length < options.Search.MinLength)
            {
                return query;
            }

            var searchableColumns = options.Columns
                .Where(x =>
                    x.Visible &&
                    x.Searchable)
                .ToList();

            if (searchableColumns.Count == 0)
            {
                return query;
            }

            var parameter = Expression.Parameter(
                typeof(T),
                "x");

            Expression? combinedExpression = null;

            foreach (var column in searchableColumns)
            {
                Expression property;
                Type propertyType;

                if (!_propertyResolver.TryResolve<T>(
                        parameter,
                        column.Field,
                        out property,
                        out propertyType))
                {
                    continue;
                }

                if (propertyType != typeof(string))
                {
                    continue;
                }

                var containsMethod = typeof(string).GetMethod(
                    nameof(string.Contains),
                    new[] { typeof(string) });

                if (containsMethod == null)
                {
                    continue;
                }

                var containsExpression = Expression.Call(
                    property,
                    containsMethod,
                    Expression.Constant(search));

                combinedExpression =
                    combinedExpression == null
                        ? containsExpression
                        : Expression.OrElse(
                            combinedExpression,
                            containsExpression);
            }

            if (combinedExpression == null)
            {
                return query;
            }

            var lambda =
                Expression.Lambda<Func<T, bool>>(
                    combinedExpression,
                    parameter);

            return query.Where(lambda);
        }

        // =========================================================
        // SORTING
        // =========================================================

        private IQueryable<T> ApplySorting<T>(
            IQueryable<T> query,
            DataTableRequest request,
            DataTableOptions options)
        {
            if (!options.Sorting.Enabled)
            {
                return query;
            }
           
            var field = request.SortField;

            if (string.IsNullOrWhiteSpace(field))
            {
                field = options.Sorting.DefaultField;
            }

            if (string.IsNullOrWhiteSpace(field))
            {
                return query;
            }

            var column = options.Columns
                .FirstOrDefault(x =>
                    string.Equals(
                        x.Field,
                        field,
                        StringComparison.OrdinalIgnoreCase));

            if (column == null || !column.Sortable)
            {
                return query;
            }

            var parameter = Expression.Parameter(
                typeof(T),
                "x");

            Expression property;
            Type propertyType;

            if (!_propertyResolver.TryResolve<T>(
                    parameter,
                    column.Field,
                    out property,
                    out propertyType))
            {
                return query;
            }

            var lambda = Expression.Lambda(
                property,
                parameter);

            var methodName =
                request.SortDirection == "desc"
                    ? "OrderByDescending"
                    : "OrderBy";

            var resultExpression = Expression.Call(
                typeof(Queryable),
                methodName,
                new Type[]
                {
                    typeof(T),
                    propertyType
                },
                query.Expression,
                Expression.Quote(lambda));

            return query.Provider
                .CreateQuery<T>(resultExpression);
        }

        // =========================================================
        // FILTERS
        // =========================================================

        private IQueryable<T> ApplyFilters<T>(
            IQueryable<T> query,
            DataTableRequest request,
            DataTableOptions options)
        {
            if (!options.Filters.Enabled)
            {
                return query;
            }

            foreach (var filter in options.Filters.Items)
            {
                string[] values;

                if (!request.TryGetFilter(
                        filter.Field,
                        out values))
                {
                    continue;
                }

                query = ApplyFilter(
                    query,
                    filter,
                    values);
            }

            return query;
        }

        private IQueryable<T> ApplyFilter<T>(
            IQueryable<T> query,
            DataTableFilter filter,
            string[] values)
        {
            if (values.Length == 0)
            {
                return query;
            }

            switch (filter.Type)
            {
                case DataTableFilterType.Text:
                    return ApplyTextFilter(
                        query,
                        filter.Field,
                        values[0]);

                case DataTableFilterType.Select:
                    return ApplySelectFilter(
                        query,
                        filter.Field,
                        values);

                case DataTableFilterType.MultiSelect:
                    return ApplyMultiSelectFilter(
                        query,
                        filter.Field,
                        values);

                case DataTableFilterType.Date:
                    return ApplyDateFilter(
                        query,
                        filter.Field,
                        values);

                case DataTableFilterType.DateRange:
                    return ApplyDateRangeFilter(
                        query,
                        filter.Field,
                        values);

                case DataTableFilterType.Number:
                    return ApplyNumberFilter(
                        query,
                        filter.Field,
                        values);

                case DataTableFilterType.NumberRange:
                    return ApplyNumberRangeFilter(
                        query,
                        filter.Field,
                        values);

                case DataTableFilterType.Boolean:
                    return ApplyBooleanFilter(
                        query,
                        filter.Field,
                        values);

                default:
                    return query;
            }
        }

        // =========================================================
        // TEXT
        // =========================================================

        private IQueryable<T> ApplyTextFilter<T>(
            IQueryable<T> query,
            string field,
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return query;
            }

            var parameter = Expression.Parameter(
                typeof(T),
                "x");

            Expression property;
            Type propertyType;

            if (!_propertyResolver.TryResolve<T>(
                    parameter,
                    field,
                    out property,
                    out propertyType))
            {
                return query;
            }

            if (propertyType != typeof(string))
            {
                return query;
            }

            var containsMethod = typeof(string).GetMethod(
                nameof(string.Contains),
                new[] { typeof(string) });

            if (containsMethod == null)
            {
                return query;
            }

            var containsExpression = Expression.Call(
                property,
                containsMethod,
                Expression.Constant(value));

            var lambda =
                Expression.Lambda<Func<T, bool>>(
                    containsExpression,
                    parameter);

            return query.Where(lambda);
        }

        // =========================================================
        // SELECT
        // =========================================================

        private IQueryable<T> ApplySelectFilter<T>(
            IQueryable<T> query,
            string field,
            string[] values)
        {
            if (values.Length == 0)
            {
                return query;
            }

            return ApplyEqualityFilter(
                query,
                field,
                values[0]);
        }

        // =========================================================
        // EQUALITY
        // =========================================================

        private IQueryable<T> ApplyEqualityFilter<T>(
            IQueryable<T> query,
            string field,
            string value)
        {
            var parameter = Expression.Parameter(
                typeof(T),
                "x");

            Expression property;
            Type propertyType;

            if (!_propertyResolver.TryResolve<T>(
                    parameter,
                    field,
                    out property,
                    out propertyType))
            {
                return query;
            }

            object? convertedValue;

            try
            {
                convertedValue = ConvertValue(
                    value,
                    propertyType);
            }
            catch
            {
                return query;
            }

            if (convertedValue == null)
            {
                return query;
            }

            var constant = Expression.Constant(
                convertedValue,
                propertyType);

            var equality = Expression.Equal(
                property,
                constant);

            var lambda =
                Expression.Lambda<Func<T, bool>>(
                    equality,
                    parameter);

            return query.Where(lambda);
        }

        // =========================================================
        // MULTI SELECT
        // =========================================================

        private IQueryable<T> ApplyMultiSelectFilter<T>(
            IQueryable<T> query,
            string field,
            string[] values)
        {
            if (values.Length == 0)
            {
                return query;
            }

            var parameter = Expression.Parameter(
                typeof(T),
                "x");

            Expression property;
            Type propertyType;

            if (!_propertyResolver.TryResolve<T>(
                    parameter,
                    field,
                    out property,
                    out propertyType))
            {
                return query;
            }

            Expression? combined = null;

            foreach (var value in values)
            {
                object? convertedValue;

                try
                {
                    convertedValue = ConvertValue(
                        value,
                        propertyType);
                }
                catch
                {
                    continue;
                }

                if (convertedValue == null)
                {
                    continue;
                }

                var constant = Expression.Constant(
                    convertedValue,
                    propertyType);

                var equality = Expression.Equal(
                    property,
                    constant);

                combined =
                    combined == null
                        ? equality
                        : Expression.OrElse(
                            combined,
                            equality);
            }

            if (combined == null)
            {
                return query;
            }

            var lambda =
                Expression.Lambda<Func<T, bool>>(
                    combined,
                    parameter);

            return query.Where(lambda);
        }

        // =========================================================
        // DATE
        // =========================================================

        private IQueryable<T> ApplyDateFilter<T>(
            IQueryable<T> query,
            string field,
            string[] values)
        {
            if (values.Length == 0)
            {
                return query;
            }

            DateTime date;

            if (!DateTime.TryParse(
                    values[0],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out date))
            {
                return query;
            }

            var parameter = Expression.Parameter(
                typeof(T),
                "x");

            Expression property;
            Type propertyType;

            if (!_propertyResolver.TryResolve<T>(
                    parameter,
                    field,
                    out property,
                    out propertyType))
            {
                return query;
            }

            var actualType =
                Nullable.GetUnderlyingType(propertyType)
                ?? propertyType;

            if (actualType != typeof(DateTime))
            {
                return query;
            }

            var start = date.Date;
            var end = start.AddDays(1);

            var startExpression =
                CreateComparisonExpression(
                    property,
                    start,
                    propertyType,
                    ExpressionType.GreaterThanOrEqual);

            var endExpression =
                CreateComparisonExpression(
                    property,
                    end,
                    propertyType,
                    ExpressionType.LessThan);

            if (startExpression == null ||
                endExpression == null)
            {
                return query;
            }

            var body = Expression.AndAlso(
                startExpression,
                endExpression);

            var lambda =
                Expression.Lambda<Func<T, bool>>(
                    body,
                    parameter);

            return query.Where(lambda);
        }

        // =========================================================
        // DATE RANGE
        // =========================================================

        private IQueryable<T> ApplyDateRangeFilter<T>(
            IQueryable<T> query,
            string field,
            string[] values)
        {
            if (values.Length == 0)
            {
                return query;
            }

            var parameter = Expression.Parameter(
                typeof(T),
                "x");

            Expression property;
            Type propertyType;

            if (!_propertyResolver.TryResolve<T>(
                    parameter,
                    field,
                    out property,
                    out propertyType))
            {
                return query;
            }

            var actualType =
                Nullable.GetUnderlyingType(propertyType)
                ?? propertyType;

            if (actualType != typeof(DateTime))
            {
                return query;
            }

            Expression? body = null;

            DateTime from;

            if (values.Length >= 1 &&
                DateTime.TryParse(
                    values[0],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out from))
            {
                var fromExpression =
                    CreateComparisonExpression(
                        property,
                        from.Date,
                        propertyType,
                        ExpressionType.GreaterThanOrEqual);

                if (fromExpression != null)
                {
                    body = fromExpression;
                }
            }

            DateTime to;

            if (values.Length >= 2 &&
                DateTime.TryParse(
                    values[1],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out to))
            {
                var toExpression =
                    CreateComparisonExpression(
                        property,
                        to.Date.AddDays(1),
                        propertyType,
                        ExpressionType.LessThan);

                if (toExpression != null)
                {
                    body = body == null
                        ? toExpression
                        : Expression.AndAlso(
                            body,
                            toExpression);
                }
            }

            if (body == null)
            {
                return query;
            }

            var lambda =
                Expression.Lambda<Func<T, bool>>(
                    body,
                    parameter);

            return query.Where(lambda);
        }

        // =========================================================
        // BOOLEAN
        // =========================================================

        private IQueryable<T> ApplyBooleanFilter<T>(
            IQueryable<T> query,
            string field,
            string[] values)
        {
            if (values.Length == 0)
            {
                return query;
            }

            bool value;

            if (!bool.TryParse(
                    values[0],
                    out value))
            {
                return query;
            }

            return ApplyEqualityFilter(
                query,
                field,
                value.ToString());
        }

        // =========================================================
        // NUMBER
        // =========================================================

        private IQueryable<T> ApplyNumberFilter<T>(
            IQueryable<T> query,
            string field,
            string[] values)
        {
            if (values.Length == 0)
            {
                return query;
            }

            return ApplyEqualityFilter(
                query,
                field,
                values[0]);
        }

        // =========================================================
        // NUMBER RANGE
        // =========================================================

        private IQueryable<T> ApplyNumberRangeFilter<T>(
            IQueryable<T> query,
            string field,
            string[] values)
        {
            if (values.Length == 0)
            {
                return query;
            }

            var parameter = Expression.Parameter(
                typeof(T),
                "x");

            Expression property;
            Type propertyType;

            if (!_propertyResolver.TryResolve<T>(
                    parameter,
                    field,
                    out property,
                    out propertyType))
            {
                return query;
            }

            Expression? body = null;

            object? from;

            if (values.Length >= 1 &&
                TryConvertNumber(
                    values[0],
                    propertyType,
                    out from))
            {
                var fromExpression =
                    CreateComparisonExpression(
                        property,
                        from,
                        propertyType,
                        ExpressionType.GreaterThanOrEqual);

                if (fromExpression != null)
                {
                    body = fromExpression;
                }
            }

            object? to;

            if (values.Length >= 2 &&
                TryConvertNumber(
                    values[1],
                    propertyType,
                    out to))
            {
                var toExpression =
                    CreateComparisonExpression(
                        property,
                        to,
                        propertyType,
                        ExpressionType.LessThanOrEqual);

                if (toExpression != null)
                {
                    body = body == null
                        ? toExpression
                        : Expression.AndAlso(
                            body,
                            toExpression);
                }
            }

            if (body == null)
            {
                return query;
            }

            var lambda =
                Expression.Lambda<Func<T, bool>>(
                    body,
                    parameter);

            return query.Where(lambda);
        }

        // =========================================================
        // COMPARISON HELPER
        // =========================================================

        private Expression? CreateComparisonExpression(
            Expression property,
            object value,
            Type propertyType,
            ExpressionType comparisonType)
        {
            var underlyingType =
                Nullable.GetUnderlyingType(propertyType);

            if (underlyingType == null)
            {
                var constant = Expression.Constant(
                    value,
                    propertyType);

                return Expression.MakeBinary(
                    comparisonType,
                    property,
                    constant);
            }

            var actualType = underlyingType;

            var valueExpression =
                Expression.Constant(
                    value,
                    actualType);

            var nullableValue =
                Expression.Convert(
                    valueExpression,
                    propertyType);

            return Expression.MakeBinary(
                comparisonType,
                property,
                nullableValue);
        }

        // =========================================================
        // VALUE CONVERSION
        // =========================================================

        private object? ConvertValue(
            string value,
            Type targetType)
        {
            var underlyingType =
                Nullable.GetUnderlyingType(targetType);

            var actualType =
                underlyingType ?? targetType;

            if (actualType.IsEnum)
            {
                return Enum.Parse(
                    actualType,
                    value,
                    true);
            }

            if (actualType == typeof(Guid))
            {
                return Guid.Parse(value);
            }

            if (actualType == typeof(DateTime))
            {
                return DateTime.Parse(
                    value,
                    CultureInfo.InvariantCulture);
            }

            if (actualType == typeof(DateOnly))
            {
                return DateOnly.Parse(
                    value,
                    CultureInfo.InvariantCulture);
            }

            if (actualType == typeof(decimal))
            {
                return decimal.Parse(
                    value,
                    CultureInfo.InvariantCulture);
            }

            return Convert.ChangeType(
                value,
                actualType,
                CultureInfo.InvariantCulture);
        }

        // =========================================================
        // NUMBER CONVERSION
        // =========================================================

        private bool TryConvertNumber(
            string value,
            Type targetType,
            out object? result)
        {
            result = null;

            try
            {
                result = ConvertValue(
                    value,
                    targetType);

                return result != null;
            }
            catch
            {
                return false;
            }
        }

        private List<string> GetRequiredFields(DataTableOptions options)
        {
            var fields = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            // Key
            if (!string.IsNullOrWhiteSpace(options.KeyField))
            {
                fields.Add(options.KeyField);
            }

            // Columns
            foreach (var column in options.Columns)
            {
                if (column.Visible ||
                    column.Searchable ||
                    column.Sortable)
                {
                    fields.Add(column.Field);
                }
            }

            // Filters
            foreach (var filter in options.Filters.Items)
            {
                if (!string.IsNullOrWhiteSpace(filter.Field))
                {
                    fields.Add(filter.Field);
                }
            }

            // Default sorting
            if (!string.IsNullOrWhiteSpace(
                options.Sorting.DefaultField))
            {
                fields.Add(
                    options.Sorting.DefaultField);
            }

            return fields.ToList();
        }
    }
}