using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Services;
using Microsoft.AspNetCore.Mvc;

namespace Corely.IAM.Web.Pages.Management.Permissions;

public class DetailModel(
    IRetrievalService retrievalService,
    IDeregistrationService deregistrationService
) : DetailPageModelBase<Permission>
{
    public string ResourceType { get; set; } = string.Empty;
    public Guid ResourceId { get; set; }
    public string? Description { get; set; }
    public bool CanCreate { get; set; }
    public bool CanRead { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExecute { get; set; }

    protected override string IndexPagePath => "/Management/Permissions/Index";

    protected override async Task<Permission?> LoadItemAsync(Guid id)
    {
        var result = await retrievalService.GetPermissionAsync(id, hydrate: true);
        return result.ResultCode == RetrieveResultCode.Success ? result.Item : null;
    }

    protected override void PopulateFromItem(Permission item)
    {
        Id = item.Id;
        ResourceType = item.ResourceType;
        ResourceId = item.ResourceId;
        Description = item.Description;
        CanCreate = item.Create;
        CanRead = item.Read;
        CanUpdate = item.Update;
        CanDelete = item.Delete;
        CanExecute = item.Execute;
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var result = await deregistrationService.DeregisterPermissionAsync(
            new DeregisterPermissionRequest(id)
        );

        if (result.ResultCode == DeregisterPermissionResultCode.Success)
        {
            return RedirectToPage(IndexPagePath);
        }

        SetResultMessage(false, string.Empty, result.Message ?? "Failed to delete permission.");
        return await ReloadAsync(id);
    }
}
