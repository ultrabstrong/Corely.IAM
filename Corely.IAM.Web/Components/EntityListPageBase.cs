namespace Corely.IAM.Web.Components;

public abstract class EntityListPageBase<TItem> : EntityPageBase
{
    protected List<TItem>? _items;
    protected int _skip;
    protected int _take = 25;
    protected int _totalCount;

    protected async Task OnPageChangedAsync(int newSkip)
    {
        _skip = newSkip;
        await ReloadAsync();
    }
}
