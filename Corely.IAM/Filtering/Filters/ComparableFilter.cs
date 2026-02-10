using System.Linq.Expressions;

namespace Corely.IAM.Filtering.Filters;

public class ComparableFilter<T> : IFilterOperation
    where T : struct, IComparable<T>
{
    private readonly ComparableFilterOperation _operation;
    private readonly T? _value;
    private readonly T? _upperValue;
    private readonly T[]? _values;

    private ComparableFilter(
        ComparableFilterOperation operation,
        T? value = null,
        T? upperValue = null,
        T[]? values = null
    )
    {
        _operation = operation;
        _value = value;
        _upperValue = upperValue;
        _values = values;
    }

    public static ComparableFilter<T> Equals(T value) =>
        new(ComparableFilterOperation.Equals, value);

    public static ComparableFilter<T> NotEquals(T value) =>
        new(ComparableFilterOperation.NotEquals, value);

    public static ComparableFilter<T> GreaterThan(T value) =>
        new(ComparableFilterOperation.GreaterThan, value);

    public static ComparableFilter<T> GreaterThanOrEqual(T value) =>
        new(ComparableFilterOperation.GreaterThanOrEqual, value);

    public static ComparableFilter<T> LessThan(T value) =>
        new(ComparableFilterOperation.LessThan, value);

    public static ComparableFilter<T> LessThanOrEqual(T value) =>
        new(ComparableFilterOperation.LessThanOrEqual, value);

    public static ComparableFilter<T> Between(T lower, T upper) =>
        new(ComparableFilterOperation.Between, lower, upper);

    public static ComparableFilter<T> NotBetween(T lower, T upper) =>
        new(ComparableFilterOperation.NotBetween, lower, upper);

    public static ComparableFilter<T> In(params T[] values) =>
        new(ComparableFilterOperation.In, values: values);

    public static ComparableFilter<T> NotIn(params T[] values) =>
        new(ComparableFilterOperation.NotIn, values: values);

    public static ComparableFilter<T> IsNull() => new(ComparableFilterOperation.IsNull);

    public static ComparableFilter<T> IsNotNull() => new(ComparableFilterOperation.IsNotNull);

    public Expression BuildExpression(Expression property)
    {
        return _operation switch
        {
            ComparableFilterOperation.Equals => Expression.Equal(
                property,
                Expression.Constant(_value!.Value, typeof(T))
            ),
            ComparableFilterOperation.NotEquals => Expression.NotEqual(
                property,
                Expression.Constant(_value!.Value, typeof(T))
            ),
            ComparableFilterOperation.GreaterThan => Expression.GreaterThan(
                property,
                Expression.Constant(_value!.Value, typeof(T))
            ),
            ComparableFilterOperation.GreaterThanOrEqual => Expression.GreaterThanOrEqual(
                property,
                Expression.Constant(_value!.Value, typeof(T))
            ),
            ComparableFilterOperation.LessThan => Expression.LessThan(
                property,
                Expression.Constant(_value!.Value, typeof(T))
            ),
            ComparableFilterOperation.LessThanOrEqual => Expression.LessThanOrEqual(
                property,
                Expression.Constant(_value!.Value, typeof(T))
            ),
            ComparableFilterOperation.Between => BuildBetweenExpression(property),
            ComparableFilterOperation.NotBetween => BuildNotBetweenExpression(property),
            ComparableFilterOperation.In => BuildInExpression(property),
            ComparableFilterOperation.NotIn => Expression.Not(BuildInExpression(property)),
            ComparableFilterOperation.IsNull => Expression.Equal(
                property,
                Expression.Constant(null, typeof(T?))
            ),
            ComparableFilterOperation.IsNotNull => Expression.NotEqual(
                property,
                Expression.Constant(null, typeof(T?))
            ),
            _ => throw new NotSupportedException(
                $"Comparable filter operation {_operation} is not supported"
            ),
        };
    }

    private Expression BuildBetweenExpression(Expression property)
    {
        var gte = Expression.GreaterThanOrEqual(
            property,
            Expression.Constant(_value!.Value, typeof(T))
        );
        var lte = Expression.LessThanOrEqual(
            property,
            Expression.Constant(_upperValue!.Value, typeof(T))
        );
        return Expression.AndAlso(gte, lte);
    }

    private Expression BuildNotBetweenExpression(Expression property)
    {
        var lt = Expression.LessThan(property, Expression.Constant(_value!.Value, typeof(T)));
        var gt = Expression.GreaterThan(
            property,
            Expression.Constant(_upperValue!.Value, typeof(T))
        );
        return Expression.OrElse(lt, gt);
    }

    private Expression BuildInExpression(Expression property)
    {
        var containsMethod = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T));

        return Expression.Call(containsMethod, Expression.Constant(_values), property);
    }

    private enum ComparableFilterOperation
    {
        Equals,
        NotEquals,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Between,
        NotBetween,
        In,
        NotIn,
        IsNull,
        IsNotNull,
    }
}
