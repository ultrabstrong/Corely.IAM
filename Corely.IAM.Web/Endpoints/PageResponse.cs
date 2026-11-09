using Corely.IAM.Models;

namespace Corely.IAM.Web.Endpoints;

internal sealed record PageResponse<T>(IReadOnlyList<T> Items, int TotalCount, bool HasMore)
{
    public static PageResponse<T> From<TModel>(PagedResult<TModel> page, Func<TModel, T> map) =>
        new([.. page.Items.Select(map)], page.TotalCount, page.HasMore);
}
