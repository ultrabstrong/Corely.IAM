namespace Corely.IAM.Models;

public record PagedResult<T>(List<T> Items, int TotalCount, int CurrentPage, bool HasMore)
{
    public static PagedResult<T> Create(List<T> items, int totalCount, int skip, int take) =>
        new(items, totalCount, take > 0 ? (skip / take) + 1 : 1, skip + take < totalCount);

    public static PagedResult<T> Empty() => new([], 0, 1, false);
}
