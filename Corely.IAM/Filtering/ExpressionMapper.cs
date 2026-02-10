using System.Linq.Expressions;
using Corely.IAM.Filtering.Ordering;

namespace Corely.IAM.Filtering;

public static class ExpressionMapper
{
    public static Expression<Func<TTarget, bool>> MapPredicate<TSource, TTarget>(
        Expression<Func<TSource, bool>> source
    )
    {
        var sourceParam = source.Parameters[0];
        var targetParam = Expression.Parameter(typeof(TTarget), sourceParam.Name);
        var visitor = new PropertyRemappingVisitor(sourceParam, targetParam);
        var body = visitor.Visit(source.Body);
        return Expression.Lambda<Func<TTarget, bool>>(body, targetParam);
    }

    public static IQueryable<TTarget> ApplyOrder<TSource, TTarget>(
        IQueryable<TTarget> query,
        OrderBuilder<TSource> orderBuilder
    )
    {
        var clauses = orderBuilder.Build();
        if (clauses.Count == 0)
            return query;

        IOrderedQueryable<TTarget>? ordered = null;

        foreach (var clause in clauses)
        {
            var mappedKeySelector = MapKeySelector<TSource, TTarget>(clause.PropertyExpression);

            var source = ordered ?? (IQueryable<TTarget>)query;

            if (clause.IsPrimary)
            {
                ordered =
                    clause.Direction == SortDirection.Ascending
                        ? Queryable.OrderBy(query, (dynamic)mappedKeySelector)
                        : Queryable.OrderByDescending(query, (dynamic)mappedKeySelector);
            }
            else
            {
                ordered =
                    clause.Direction == SortDirection.Ascending
                        ? Queryable.ThenBy(ordered!, (dynamic)mappedKeySelector)
                        : Queryable.ThenByDescending(ordered!, (dynamic)mappedKeySelector);
            }
        }

        return ordered ?? query;
    }

    private static LambdaExpression MapKeySelector<TSource, TTarget>(LambdaExpression source)
    {
        var sourceParam = source.Parameters[0];
        var targetParam = Expression.Parameter(typeof(TTarget), sourceParam.Name);
        var visitor = new PropertyRemappingVisitor(sourceParam, targetParam);
        var body = visitor.Visit(source.Body);
        return Expression.Lambda(body, targetParam);
    }

    private class PropertyRemappingVisitor(
        ParameterExpression sourceParam,
        ParameterExpression targetParam
    ) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == sourceParam ? targetParam : node;

        protected override Expression VisitMember(MemberExpression node)
        {
            var visited = Visit(node.Expression!);
            if (visited.Type != node.Expression!.Type)
            {
                var prop =
                    visited.Type.GetProperty(node.Member.Name)
                    ?? throw new InvalidOperationException(
                        $"Property '{node.Member.Name}' not found on type '{visited.Type.Name}'"
                    );
                return Expression.MakeMemberAccess(visited, prop);
            }
            return base.VisitMember(node);
        }
    }
}
