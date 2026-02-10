using System.Linq.Expressions;

namespace Corely.IAM.Filtering.Filters;

public class BoolFilter : IFilterOperation
{
    private readonly BoolFilterOperation _operation;

    private BoolFilter(BoolFilterOperation operation)
    {
        _operation = operation;
    }

    public static BoolFilter IsTrue() => new(BoolFilterOperation.IsTrue);

    public static BoolFilter IsFalse() => new(BoolFilterOperation.IsFalse);

    public static BoolFilter IsNull() => new(BoolFilterOperation.IsNull);

    public static BoolFilter IsNotNull() => new(BoolFilterOperation.IsNotNull);

    public Expression BuildExpression(Expression property)
    {
        return _operation switch
        {
            BoolFilterOperation.IsTrue => Expression.Equal(property, Expression.Constant(true)),
            BoolFilterOperation.IsFalse => Expression.Equal(property, Expression.Constant(false)),
            BoolFilterOperation.IsNull => Expression.Equal(
                property,
                Expression.Constant(null, typeof(bool?))
            ),
            BoolFilterOperation.IsNotNull => Expression.NotEqual(
                property,
                Expression.Constant(null, typeof(bool?))
            ),
            _ => throw new NotSupportedException(
                $"Bool filter operation {_operation} is not supported"
            ),
        };
    }

    private enum BoolFilterOperation
    {
        IsTrue,
        IsFalse,
        IsNull,
        IsNotNull,
    }
}
