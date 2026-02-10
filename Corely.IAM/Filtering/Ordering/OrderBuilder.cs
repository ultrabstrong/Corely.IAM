using System.Linq.Expressions;

namespace Corely.IAM.Filtering.Ordering;

public class OrderBuilder<T>
{
    private readonly List<OrderClause<T>> _clauses = [];

    internal OrderBuilder() { }

    public OrderBuilder<T> By<TProperty>(
        Expression<Func<T, TProperty>> property,
        SortDirection direction = SortDirection.Ascending
    )
    {
        _clauses.Clear();
        _clauses.Add(new OrderClause<T>(property, direction, IsPrimary: true));
        return this;
    }

    public OrderBuilder<T> ThenBy<TProperty>(
        Expression<Func<T, TProperty>> property,
        SortDirection direction = SortDirection.Ascending
    )
    {
        if (_clauses.Count == 0)
        {
            throw new InvalidOperationException("ThenBy must be called after By.");
        }

        _clauses.Add(new OrderClause<T>(property, direction, IsPrimary: false));
        return this;
    }

    public IReadOnlyList<OrderClause<T>> Build() => _clauses.AsReadOnly();

    public IOrderedQueryable<T> Apply(IQueryable<T> query)
    {
        if (_clauses.Count == 0)
        {
            throw new InvalidOperationException("No ordering specified. Call By() first.");
        }

        IOrderedQueryable<T>? ordered = null;

        foreach (var clause in _clauses)
        {
            if (ordered == null)
            {
                ordered =
                    clause.Direction == SortDirection.Ascending
                        ? Queryable.OrderBy(query, (dynamic)clause.PropertyExpression)
                        : Queryable.OrderByDescending(query, (dynamic)clause.PropertyExpression);
            }
            else
            {
                ordered =
                    clause.Direction == SortDirection.Ascending
                        ? Queryable.ThenBy(ordered, (dynamic)clause.PropertyExpression)
                        : Queryable.ThenByDescending(ordered, (dynamic)clause.PropertyExpression);
            }
        }

        return ordered!;
    }
}

public record OrderClause<T>(
    LambdaExpression PropertyExpression,
    SortDirection Direction,
    bool IsPrimary
);
