using Corely.Common.Filtering.Ordering;

namespace Corely.IAM.Web.Components;

public abstract class EntityListPageBase<TItem> : EntityPageBase
{
    protected List<TItem>? _items;
    protected int _skip;
    protected int _take = 25;
    protected int _totalCount;
    protected string _searchText = string.Empty;
    protected string? _sortColumn;
    protected SortDirection? _sortDirection;

    private CancellationTokenSource? _debounceCts;

    protected async Task OnPageChangedAsync(int newSkip)
    {
        _skip = newSkip;
        await ReloadAsync();
    }

    protected async Task OnSearchChangedAsync()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        try
        {
            await Task.Delay(300, token);
            _skip = 0;
            await ReloadAsync();
        }
        catch (TaskCanceledException) { }
    }

    protected async Task CycleSortAsync(string column)
    {
        if (_sortColumn != column)
        {
            _sortColumn = column;
            _sortDirection = SortDirection.Ascending;
        }
        else if (_sortDirection == SortDirection.Ascending)
        {
            _sortDirection = SortDirection.Descending;
        }
        else
        {
            _sortColumn = null;
            _sortDirection = null;
        }

        _skip = 0;
        await ReloadAsync();
    }

    protected string GetSortIcon(string column)
    {
        if (_sortColumn != column)
            return "bi-chevron-expand";
        return _sortDirection == SortDirection.Ascending ? "bi-chevron-up" : "bi-chevron-down";
    }

    protected string GetSortClass(string column)
    {
        return _sortColumn == column ? "sortable active" : "sortable";
    }
}
