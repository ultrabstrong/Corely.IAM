using Microsoft.AspNetCore.Mvc;

namespace Corely.IAM.Web.Pages.Management;

public abstract class DetailPageModelBase<TItem> : ManagementPageModelBase
{
    public Guid Id { get; set; }

    protected abstract string IndexPagePath { get; }
    protected abstract Task<TItem?> LoadItemAsync(Guid id);
    protected abstract void PopulateFromItem(TItem item);

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var item = await LoadItemAsync(id);
        if (item == null)
        {
            return RedirectToPage(IndexPagePath);
        }
        PopulateFromItem(item);
        return Page();
    }

    protected async Task<IActionResult> ReloadAsync(Guid id)
    {
        var item = await LoadItemAsync(id);
        if (item != null)
        {
            PopulateFromItem(item);
        }
        return Page();
    }
}
