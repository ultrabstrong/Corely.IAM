namespace Corely.IAM.Web.Pages.Management;

public abstract class ListPageModelBase<TItem> : ManagementPageModelBase
{
    public List<TItem> Items { get; set; } = [];
    public int Skip { get; set; }
    public int Take { get; set; } = 25;
    public int TotalCount { get; set; }

    public async Task OnGetAsync(int skip = 0, int take = 25)
    {
        if (skip < 0)
            skip = 0;
        if (take < 1 || take > 100)
            take = 25;
        Skip = skip;
        Take = take;
        await LoadItemsAsync();
    }

    protected abstract Task LoadItemsAsync();
}
