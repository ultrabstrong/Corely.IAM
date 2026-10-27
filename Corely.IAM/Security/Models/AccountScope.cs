using System.Linq.Expressions;
using Corely.IAM.Entities;

namespace Corely.IAM.Security.Models;

internal readonly record struct AccountScope
{
    private AccountScope(Guid id, bool isAny)
    {
        Id = id;
        IsAny = isAny;
    }

    public Guid Id { get; }
    public bool IsAny { get; }

    public static AccountScope Any { get; } = new(Guid.Empty, true);

    public static AccountScope Of(Guid accountId) => new(accountId, false);

    public static implicit operator AccountScope(Guid accountId) => Of(accountId);

    public bool Includes(Guid accountId) => IsAny || (Id != Guid.Empty && Id == accountId);

    public Expression<Func<T, bool>> ByIdPredicate<T>(Guid id)
        where T : class, IAccountOwnedEntity
    {
        var row = Expression.Parameter(typeof(T), "row");
        var values = Expression.Constant(new ScopedId(id, Id));
        var idMatches = Expression.Equal(
            Expression.Property(row, nameof(IAccountOwnedEntity.Id)),
            Expression.Field(values, nameof(ScopedId.RowId))
        );
        if (IsAny)
            return Expression.Lambda<Func<T, bool>>(idMatches, row);

        var accountMatches = Expression.Equal(
            Expression.Property(row, nameof(IAccountOwnedEntity.AccountId)),
            Expression.Field(values, nameof(ScopedId.AccountId))
        );
        var accountGiven = Expression.NotEqual(
            Expression.Field(values, nameof(ScopedId.AccountId)),
            Expression.Constant(Guid.Empty)
        );
        return Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(idMatches, Expression.AndAlso(accountGiven, accountMatches)),
            row
        );
    }

    private sealed class ScopedId(Guid rowId, Guid accountId)
    {
        public readonly Guid RowId = rowId;
        public readonly Guid AccountId = accountId;
    }
}
