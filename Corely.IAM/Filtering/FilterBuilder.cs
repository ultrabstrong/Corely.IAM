using System.Collections;
using System.Linq.Expressions;
using Corely.IAM.Filtering.Filters;

namespace Corely.IAM.Filtering;

public class FilterBuilder<T>
{
    private readonly List<Expression<Func<T, bool>>> _predicates = [];
    private readonly bool _allowCollectionFilters;

    internal FilterBuilder(bool allowCollectionFilters = true)
    {
        _allowCollectionFilters = allowCollectionFilters;
    }

    // String properties
    public FilterBuilder<T> Where(Expression<Func<T, string>> property, StringFilter filter)
    {
        _predicates.Add(BuildPredicate(property, filter));
        return this;
    }

    // Comparable value type properties (int, long, float, double, decimal, DateTime, DateTimeOffset)
    public FilterBuilder<T> Where<TValue>(
        Expression<Func<T, TValue>> property,
        ComparableFilter<TValue> filter
    )
        where TValue : struct, IComparable<TValue>
    {
        _predicates.Add(BuildPredicate(property, filter));
        return this;
    }

    // Nullable comparable value type properties
    public FilterBuilder<T> Where<TValue>(
        Expression<Func<T, TValue?>> property,
        ComparableFilter<TValue> filter
    )
        where TValue : struct, IComparable<TValue>
    {
        _predicates.Add(BuildPredicate(property, filter));
        return this;
    }

    // Guid properties
    public FilterBuilder<T> Where(Expression<Func<T, Guid>> property, GuidFilter filter)
    {
        _predicates.Add(BuildPredicate(property, filter));
        return this;
    }

    // Nullable Guid properties
    public FilterBuilder<T> Where(Expression<Func<T, Guid?>> property, GuidFilter filter)
    {
        _predicates.Add(BuildPredicate(property, filter));
        return this;
    }

    // Bool properties
    public FilterBuilder<T> Where(Expression<Func<T, bool>> property, BoolFilter filter)
    {
        _predicates.Add(BuildPredicate(property, filter));
        return this;
    }

    // Nullable Bool properties
    public FilterBuilder<T> Where(Expression<Func<T, bool?>> property, BoolFilter filter)
    {
        _predicates.Add(BuildPredicate(property, filter));
        return this;
    }

    // Enum properties
    public FilterBuilder<T> Where<TEnum>(
        Expression<Func<T, TEnum>> property,
        EnumFilter<TEnum> filter
    )
        where TEnum : struct, Enum
    {
        _predicates.Add(BuildPredicate(property, filter));
        return this;
    }

    // Nullable enum properties
    public FilterBuilder<T> Where<TEnum>(
        Expression<Func<T, TEnum?>> property,
        EnumFilter<TEnum> filter
    )
        where TEnum : struct, Enum
    {
        _predicates.Add(BuildPredicate(property, filter));
        return this;
    }

    // Collection navigation property (one level deep)
    public FilterBuilder<T> Where<TChild>(
        Expression<Func<T, IEnumerable<TChild>>> collection,
        Action<FilterBuilder<TChild>> childFilter
    )
    {
        if (!_allowCollectionFilters)
        {
            throw new InvalidOperationException(
                "Nested collection filters are limited to one level deep."
            );
        }

        var childBuilder = new FilterBuilder<TChild>(allowCollectionFilters: false);
        childFilter(childBuilder);

        var childPredicate = childBuilder.Build();
        if (childPredicate == null)
        {
            return this;
        }

        // Build: parent => parent.Collection.Any(child => childPredicate)
        var parentParam = Expression.Parameter(typeof(T), "parent");
        var collectionAccess = ExpressionHelper.ReplaceParameter(
            collection.Body,
            collection.Parameters[0],
            parentParam
        );

        var anyMethod = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TChild));

        var anyCall = Expression.Call(anyMethod, collectionAccess, childPredicate);
        var lambda = Expression.Lambda<Func<T, bool>>(anyCall, parentParam);

        _predicates.Add(lambda);
        return this;
    }

    public Expression<Func<T, bool>>? Build()
    {
        if (_predicates.Count == 0)
        {
            return null;
        }

        if (_predicates.Count == 1)
        {
            return _predicates[0];
        }

        // AND all predicates together with a shared parameter
        var param = Expression.Parameter(typeof(T), "x");

        Expression? combined = null;
        foreach (var predicate in _predicates)
        {
            var rebound = ExpressionHelper.ReplaceParameter(
                predicate.Body,
                predicate.Parameters[0],
                param
            );
            combined = combined == null ? rebound : Expression.AndAlso(combined, rebound);
        }

        return Expression.Lambda<Func<T, bool>>(combined!, param);
    }

    private static Expression<Func<T, bool>> BuildPredicate<TProperty>(
        Expression<Func<T, TProperty>> propertySelector,
        IFilterOperation filter
    )
    {
        var param = propertySelector.Parameters[0];
        var memberAccess = propertySelector.Body;
        var filterExpression = filter.BuildExpression(memberAccess);
        return Expression.Lambda<Func<T, bool>>(filterExpression, param);
    }
}

internal static class ExpressionHelper
{
    public static Expression ReplaceParameter(
        Expression body,
        ParameterExpression oldParam,
        ParameterExpression newParam
    )
    {
        return new ParameterReplacer(oldParam, newParam).Visit(body);
    }

    private class ParameterReplacer(ParameterExpression oldParam, ParameterExpression newParam)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == oldParam ? newParam : base.VisitParameter(node);
        }
    }
}
