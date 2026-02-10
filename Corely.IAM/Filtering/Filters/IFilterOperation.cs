using System.Linq.Expressions;

namespace Corely.IAM.Filtering.Filters;

public interface IFilterOperation
{
    Expression BuildExpression(Expression property);
}
