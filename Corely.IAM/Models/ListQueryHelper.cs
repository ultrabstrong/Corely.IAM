using System.Linq.Expressions;
using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;
using Corely.DataAccess.Interfaces.Repos;

namespace Corely.IAM.Models;

internal static class ListQueryHelper
{
    public static async Task<ListResult<TModel>> ExecuteListAsync<TModel, TEntity>(
        IReadonlyRepo<TEntity> repo,
        Expression<Func<TEntity, bool>> scopePredicate,
        FilterBuilder<TModel>? filter,
        OrderBuilder<TModel>? order,
        int skip,
        int take,
        Func<TEntity, TModel> toModel,
        IReadOnlySet<Guid>? authorizedResourceIds = null
    )
        where TEntity : class
    {
        if (skip < 0)
            throw new ArgumentOutOfRangeException(nameof(skip), "Must be non-negative.");
        if (take <= 0)
            throw new ArgumentOutOfRangeException(nameof(take), "Must be positive.");

        // Folded into the predicate so the page and the count agree; filtering the results
        // afterwards would short the page and inflate the total. Null means a wildcard grant.
        if (authorizedResourceIds != null)
        {
            scopePredicate = AndAlso(
                scopePredicate,
                BuildIdInSetPredicate<TEntity>(authorizedResourceIds)
            );
        }

        var filterExpression = filter?.Build();
        Expression<Func<TEntity, bool>> predicate;
        if (filterExpression != null)
        {
            var mappedFilter = ExpressionMapper.MapPredicate<TModel, TEntity>(filterExpression);
            var param = Expression.Parameter(typeof(TEntity), "e");
            var combined = Expression.AndAlso(
                Expression.Invoke(scopePredicate, param),
                Expression.Invoke(mappedFilter, param)
            );
            predicate = Expression.Lambda<Func<TEntity, bool>>(combined, param);
        }
        else
        {
            predicate = scopePredicate;
        }

        var defaultOrder = BuildDefaultOrderExpression<TEntity>();

        var entities = await repo.QueryAsync(q =>
        {
            var query = q.Where(predicate);
            query =
                order != null
                    ? ExpressionMapper.ApplyOrder<TModel, TEntity>(query, order)
                    : query.OrderBy(defaultOrder);
            return query.Skip(skip).Take(take);
        });

        var totalCount = await repo.CountAsync(predicate);

        var items = entities.Select(toModel).ToList();
        return new ListResult<TModel>(
            RetrieveResultCode.Success,
            string.Empty,
            PagedResult<TModel>.Create(items, totalCount, skip, take)
        );
    }

    private static Expression<Func<TEntity, bool>> AndAlso<TEntity>(
        Expression<Func<TEntity, bool>> left,
        Expression<Func<TEntity, bool>> right
    )
    {
        var param = Expression.Parameter(typeof(TEntity), "e");
        return Expression.Lambda<Func<TEntity, bool>>(
            Expression.AndAlso(Expression.Invoke(left, param), Expression.Invoke(right, param)),
            param
        );
    }

    private static Expression<Func<TEntity, bool>> BuildIdInSetPredicate<TEntity>(
        IReadOnlySet<Guid> ids
    )
    {
        var idProp =
            typeof(TEntity).GetProperty("Id")
            ?? throw new InvalidOperationException(
                $"Entity type '{typeof(TEntity).Name}' does not have an 'Id' property required for authorization scoping."
            );

        // Materialised so the provider emits IN (...) rather than closing over the set.
        var idList = ids.ToList();
        var param = Expression.Parameter(typeof(TEntity), "e");
        var contains = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            [typeof(Guid)],
            Expression.Constant(idList),
            Expression.MakeMemberAccess(param, idProp)
        );
        return Expression.Lambda<Func<TEntity, bool>>(contains, param);
    }

    private static Expression<Func<TEntity, Guid>> BuildDefaultOrderExpression<TEntity>()
    {
        var idProp =
            typeof(TEntity).GetProperty("Id")
            ?? throw new InvalidOperationException(
                $"Entity type '{typeof(TEntity).Name}' does not have an 'Id' property required for default ordering."
            );
        var param = Expression.Parameter(typeof(TEntity), "e");
        var idProperty = Expression.MakeMemberAccess(param, idProp);
        return Expression.Lambda<Func<TEntity, Guid>>(idProperty, param);
    }
}
