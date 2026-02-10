using System.Linq.Expressions;
using System.Reflection;

namespace Corely.IAM.Filtering.Filters;

public class StringFilter : IFilterOperation
{
    private static readonly MethodInfo ContainsMethod = typeof(string).GetMethod(
        nameof(string.Contains),
        [typeof(string)]
    )!;

    private static readonly MethodInfo StartsWithMethod = typeof(string).GetMethod(
        nameof(string.StartsWith),
        [typeof(string)]
    )!;

    private static readonly MethodInfo EndsWithMethod = typeof(string).GetMethod(
        nameof(string.EndsWith),
        [typeof(string)]
    )!;

    private readonly StringFilterOperation _operation;
    private readonly string? _value;
    private readonly string[]? _values;

    private StringFilter(
        StringFilterOperation operation,
        string? value = null,
        string[]? values = null
    )
    {
        _operation = operation;
        _value = value;
        _values = values;
    }

    public static StringFilter Equals(string value) => new(StringFilterOperation.Equals, value);

    public static StringFilter NotEquals(string value) =>
        new(StringFilterOperation.NotEquals, value);

    public static StringFilter Contains(string value) => new(StringFilterOperation.Contains, value);

    public static StringFilter NotContains(string value) =>
        new(StringFilterOperation.NotContains, value);

    public static StringFilter StartsWith(string value) =>
        new(StringFilterOperation.StartsWith, value);

    public static StringFilter NotStartsWith(string value) =>
        new(StringFilterOperation.NotStartsWith, value);

    public static StringFilter EndsWith(string value) => new(StringFilterOperation.EndsWith, value);

    public static StringFilter NotEndsWith(string value) =>
        new(StringFilterOperation.NotEndsWith, value);

    public static StringFilter In(params string[] values) =>
        new(StringFilterOperation.In, values: values);

    public static StringFilter NotIn(params string[] values) =>
        new(StringFilterOperation.NotIn, values: values);

    public static StringFilter IsNull() => new(StringFilterOperation.IsNull);

    public static StringFilter IsNotNull() => new(StringFilterOperation.IsNotNull);

    public Expression BuildExpression(Expression property)
    {
        return _operation switch
        {
            StringFilterOperation.Equals => Expression.Equal(property, Expression.Constant(_value)),
            StringFilterOperation.NotEquals => Expression.NotEqual(
                property,
                Expression.Constant(_value)
            ),
            StringFilterOperation.Contains => Expression.Call(
                property,
                ContainsMethod,
                Expression.Constant(_value)
            ),
            StringFilterOperation.NotContains => Expression.Not(
                Expression.Call(property, ContainsMethod, Expression.Constant(_value))
            ),
            StringFilterOperation.StartsWith => Expression.Call(
                property,
                StartsWithMethod,
                Expression.Constant(_value)
            ),
            StringFilterOperation.NotStartsWith => Expression.Not(
                Expression.Call(property, StartsWithMethod, Expression.Constant(_value))
            ),
            StringFilterOperation.EndsWith => Expression.Call(
                property,
                EndsWithMethod,
                Expression.Constant(_value)
            ),
            StringFilterOperation.NotEndsWith => Expression.Not(
                Expression.Call(property, EndsWithMethod, Expression.Constant(_value))
            ),
            StringFilterOperation.In => BuildInExpression(property),
            StringFilterOperation.NotIn => Expression.Not(BuildInExpression(property)),
            StringFilterOperation.IsNull => Expression.Equal(
                property,
                Expression.Constant(null, typeof(string))
            ),
            StringFilterOperation.IsNotNull => Expression.NotEqual(
                property,
                Expression.Constant(null, typeof(string))
            ),
            _ => throw new NotSupportedException(
                $"String filter operation {_operation} is not supported"
            ),
        };
    }

    private Expression BuildInExpression(Expression property)
    {
        var containsMethod = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(string));

        return Expression.Call(containsMethod, Expression.Constant(_values), property);
    }

    private enum StringFilterOperation
    {
        Equals,
        NotEquals,
        Contains,
        NotContains,
        StartsWith,
        NotStartsWith,
        EndsWith,
        NotEndsWith,
        In,
        NotIn,
        IsNull,
        IsNotNull,
    }
}
