using System.Linq.Expressions;

namespace Corely.IAM.Filtering.Filters;

public class EnumFilter<TEnum> : IFilterOperation
    where TEnum : struct, Enum
{
    private readonly EnumFilterOperation _operation;
    private readonly TEnum? _value;
    private readonly TEnum[]? _values;

    private EnumFilter(EnumFilterOperation operation, TEnum? value = null, TEnum[]? values = null)
    {
        _operation = operation;
        _value = value;
        _values = values;
    }

    public static EnumFilter<TEnum> Equals(TEnum value) => new(EnumFilterOperation.Equals, value);

    public static EnumFilter<TEnum> NotEquals(TEnum value) =>
        new(EnumFilterOperation.NotEquals, value);

    public static EnumFilter<TEnum> In(params TEnum[] values) =>
        new(EnumFilterOperation.In, values: values);

    public static EnumFilter<TEnum> NotIn(params TEnum[] values) =>
        new(EnumFilterOperation.NotIn, values: values);

    public static EnumFilter<TEnum> IsNull() => new(EnumFilterOperation.IsNull);

    public static EnumFilter<TEnum> IsNotNull() => new(EnumFilterOperation.IsNotNull);

    public Expression BuildExpression(Expression property)
    {
        return _operation switch
        {
            EnumFilterOperation.Equals => Expression.Equal(
                property,
                Expression.Constant(_value!.Value, typeof(TEnum))
            ),
            EnumFilterOperation.NotEquals => Expression.NotEqual(
                property,
                Expression.Constant(_value!.Value, typeof(TEnum))
            ),
            EnumFilterOperation.In => BuildInExpression(property),
            EnumFilterOperation.NotIn => Expression.Not(BuildInExpression(property)),
            EnumFilterOperation.IsNull => Expression.Equal(
                property,
                Expression.Constant(null, typeof(TEnum?))
            ),
            EnumFilterOperation.IsNotNull => Expression.NotEqual(
                property,
                Expression.Constant(null, typeof(TEnum?))
            ),
            _ => throw new NotSupportedException(
                $"Enum filter operation {_operation} is not supported"
            ),
        };
    }

    private Expression BuildInExpression(Expression property)
    {
        var containsMethod = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TEnum));

        return Expression.Call(containsMethod, Expression.Constant(_values), property);
    }

    private enum EnumFilterOperation
    {
        Equals,
        NotEquals,
        In,
        NotIn,
        IsNull,
        IsNotNull,
    }
}
