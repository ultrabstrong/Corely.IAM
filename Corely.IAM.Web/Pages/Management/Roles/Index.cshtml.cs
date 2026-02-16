using Corely.IAM.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Services;
using Microsoft.AspNetCore.Mvc;

namespace Corely.IAM.Web.Pages.Management.Roles;

public class IndexModel(
    IRetrievalService retrievalService,
    IRegistrationService registrationService,
    IDeregistrationService deregistrationService
) : ListPageModelBase<Role>
{
    public async Task<IActionResult> OnPostCreateAsync(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            SetResultMessage(false, string.Empty, "Role name is required.");
            await LoadItemsAsync();
            return Page();
        }

        var result = await registrationService.RegisterRoleAsync(new RegisterRoleRequest(roleName));

        SetResultMessage(
            result.ResultCode == CreateRoleResultCode.Success,
            "Role created successfully.",
            result.Message ?? "Failed to create role."
        );

        await LoadItemsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid roleId)
    {
        var result = await deregistrationService.DeregisterRoleAsync(
            new DeregisterRoleRequest(roleId)
        );

        SetResultMessage(
            result.ResultCode == DeregisterRoleResultCode.Success,
            "Role deleted successfully.",
            result.Message ?? "Failed to delete role."
        );

        await LoadItemsAsync();
        return Page();
    }

    protected override async Task LoadItemsAsync()
    {
        var result = await retrievalService.ListRolesAsync(skip: Skip, take: Take);
        if (result.ResultCode == RetrieveResultCode.Success && result.Data != null)
        {
            Items = result.Data.Items;
            TotalCount = result.Data.TotalCount;
        }
    }
}
