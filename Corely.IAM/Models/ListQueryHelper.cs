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
        Func<TEntity, TModel> toModel
    )
        where TEntity : class
    {
        if (skip < 0)
            throw new ArgumentOutOfRangeException(nameof(skip), "Must be non-negative.");
        if (take <= 0)
            throw new ArgumentOutOfRangeException(nameof(take), "Must be positive.");

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
