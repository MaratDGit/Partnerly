using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Partnerly.Models;

namespace Partnerly.Descriptors.Attributes.BaseAttributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class UniqueAttribute : ValidationAttribute
    {
        private readonly Type _entityType;
        private readonly string _propertyName;
        private readonly Type _dbContextType;

        /// <summary>
        /// entityType - тип сущности для проверки уникальности
        /// propertyName - имя свойства Entity
        /// dbContextType - тип DbContext (по умолчанию AppDbContext)
        /// </summary>
        public UniqueAttribute(Type entityType, string propertyName, Type? dbContextType = null)
        {
            _entityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
            _propertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            _dbContextType = dbContextType ?? typeof(AppDbContext);
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            // Получаем DbContext из DI
            var dbContext = (DbContext?)validationContext.GetService(_dbContextType);
            if (dbContext == null)
                throw new InvalidOperationException($"DbContext типа {_dbContextType.Name} не найден в DI.");

            // Получаем DbSet<TEntity> через рефлексию
            var setMethod = typeof(DbContext).GetMethod("Set", Type.EmptyTypes)
                            ?? throw new InvalidOperationException("Метод Set<T>() не найден в DbContext");

            var genericSetMethod = setMethod.MakeGenericMethod(_entityType);
            var dbSet = genericSetMethod.Invoke(dbContext, null);
            if (dbSet == null)
                throw new InvalidOperationException("Не удалось получить DbSet для сущности " + _entityType.Name);

            // Преобразуем в IQueryable
            var queryable = ((IQueryable)dbSet).AsQueryable();

            // Создаем Expression: e => e.Property == value
            var parameter = Expression.Parameter(_entityType, "e");
            var property = Expression.Property(parameter, _propertyName);
            var constant = Expression.Constant(value);
            var equal = Expression.Equal(property, constant);

            // Если есть свойство IsDeleted, добавляем проверку IsDeleted == false
            Expression finalExpr = equal;
            var isDeletedProp = _entityType.GetProperty("IsDeleted");
            if (isDeletedProp != null && isDeletedProp.PropertyType == typeof(bool))
            {
                var isDeletedProperty = Expression.Property(parameter, isDeletedProp);
                var notDeleted = Expression.Equal(isDeletedProperty, Expression.Constant(false));
                finalExpr = Expression.AndAlso(equal, notDeleted);
            }

            // Создаем лямбду Func<TEntity, bool>
            var lambdaType = typeof(Func<,>).MakeGenericType(_entityType, typeof(bool));
            var lambda = Expression.Lambda(lambdaType, finalExpr, parameter);

            // Вызываем Any через reflection
            var anyMethod = typeof(Queryable)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(m => m.Name == "Any" && m.GetParameters().Length == 2)
                .Single()
                .MakeGenericMethod(_entityType);

            var exists = (bool)anyMethod.Invoke(null, new object[] { queryable, lambda })!;

            if (exists)
                return new ValidationResult(ErrorMessage ?? $"{_propertyName} должно быть уникальным.");

            return ValidationResult.Success;
        }
    }
}
