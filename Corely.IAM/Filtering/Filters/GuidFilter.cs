using System.Linq.Expressions;

namespace Corely.IAM.Filtering.Filters;

public class GuidFilter : IFilterOperation
{
    private readonly GuidFilterOperation _operation;
    private readonly Guid? _value;
    private readonly Guid[]? _values;

    private GuidFilter(GuidFilterOperation operation, Guid? value = null, Guid[]? values = null)
    {
        _operation = operation;
        _value = value;
        _values = values;
    }

    public static GuidFilter Equals(Guid value) => new(GuidFilterOperation.Equals, value);

    public static GuidFilter NotEquals(Guid value) => new(GuidFilterOperation.NotEquals, value);

    public static GuidFilter In(params Guid[] values) =>
        new(GuidFilterOperation.In, values: values);

    public static GuidFilter NotIn(params Guid[] values) =>
        new(GuidFilterOperation.NotIn, values: values);

    public static GuidFilter IsNull() => new(GuidFilterOperation.IsNull);

    public static GuidFilter IsNotNull() => new(GuidFilterOperation.IsNotNull);

    public Expression BuildExpression(Expression property)
    {
        return _operation switch
        {
            GuidFilterOperation.Equals => Expression.Equal(
                property,
                Expression.Constant(_value!.Value, typeof(Guid))
            ),
            GuidFilterOperation.NotEquals => Expression.NotEqual(
                property,
                Expression.Constant(_value!.Value, typeof(Guid))
            ),
            GuidFilterOperation.In => BuildInExpression(property),
            GuidFilterOperation.NotIn => Expression.Not(BuildInExpression(property)),
            GuidFilterOperation.IsNull => Expression.Equal(
                property,
                Expression.Constant(null, typeof(Guid?))
            ),
            GuidFilterOperation.IsNotNull => Expression.NotEqual(
                property,
                Expression.Constant(null, typeof(Guid?))
            ),
            _ => throw new NotSupportedException(
                $"Guid filter operation {_operation} is not supported"
            ),
        };
    }

    private Expression BuildInExpression(Expression property)
    {
        var containsMethod = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(Guid));

        return Expression.Call(containsMethod, Expression.Constant(_values), property);
    }

    private enum GuidFilterOperation
    {
        Equals,
        NotEquals,
        In,
        NotIn,
        IsNull,
        IsNotNull,
    }
}
