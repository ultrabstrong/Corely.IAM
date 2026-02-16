using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Services;
using Microsoft.AspNetCore.Mvc;

namespace Corely.IAM.Web.Pages.Management.Permissions;

public class IndexModel(
    IRetrievalService retrievalService,
    IRegistrationService registrationService,
    IDeregistrationService deregistrationService
) : ListPageModelBase<Permission>
{
    public async Task<IActionResult> OnPostCreateAsync(
        string resourceType,
        string? resourceId,
        bool canCreate,
        bool canRead,
        bool canUpdate,
        bool canDelete,
        bool canExecute,
        string? description
    )
    {
        if (string.IsNullOrWhiteSpace(resourceType))
        {
            SetResultMessage(false, string.Empty, "Resource type is required.");
            await LoadItemsAsync();
            return Page();
        }

        var parsedResourceId = Guid.Empty;
        if (!string.IsNullOrWhiteSpace(resourceId))
        {
            if (!Guid.TryParse(resourceId, out parsedResourceId))
            {
                SetResultMessage(false, string.Empty, "Invalid Resource ID format.");
                await LoadItemsAsync();
                return Page();
            }
        }

        var result = await registrationService.RegisterPermissionAsync(
            new RegisterPermissionRequest(
                resourceType,
                parsedResourceId,
                canCreate,
                canRead,
                canUpdate,
                canDelete,
                canExecute,
                description
            )
        );

        SetResultMessage(
            result.ResultCode == CreatePermissionResultCode.Success,
            "Permission created successfully.",
            result.Message ?? "Failed to create permission."
        );

        await LoadItemsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid permissionId)
    {
        var result = await deregistrationService.DeregisterPermissionAsync(
            new DeregisterPermissionRequest(permissionId)
        );

        SetResultMessage(
            result.ResultCode == DeregisterPermissionResultCode.Success,
            "Permission deleted successfully.",
            result.Message ?? "Failed to delete permission."
        );

        await LoadItemsAsync();
        return Page();
    }

    protected override async Task LoadItemsAsync()
    {
        var result = await retrievalService.ListPermissionsAsync(skip: Skip, take: Take);
        if (result.ResultCode == RetrieveResultCode.Success && result.Data != null)
        {
            Items = result.Data.Items;
            TotalCount = result.Data.TotalCount;
        }
    }
}
