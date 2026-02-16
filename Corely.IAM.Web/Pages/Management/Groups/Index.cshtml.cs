using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Microsoft.AspNetCore.Mvc;

namespace Corely.IAM.Web.Pages.Management.Groups;

public class IndexModel(
    IRetrievalService retrievalService,
    IRegistrationService registrationService,
    IDeregistrationService deregistrationService
) : ListPageModelBase<Group>
{
    public async Task<IActionResult> OnPostCreateAsync(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            SetResultMessage(false, string.Empty, "Group name is required.");
            await LoadItemsAsync();
            return Page();
        }

        var result = await registrationService.RegisterGroupAsync(
            new RegisterGroupRequest(groupName)
        );

        SetResultMessage(
            result.ResultCode == CreateGroupResultCode.Success,
            "Group created successfully.",
            result.Message ?? "Failed to create group."
        );

        await LoadItemsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid groupId)
    {
        var result = await deregistrationService.DeregisterGroupAsync(
            new DeregisterGroupRequest(groupId)
        );

        SetResultMessage(
            result.ResultCode == DeregisterGroupResultCode.Success,
            "Group deleted successfully.",
            result.Message ?? "Failed to delete group."
        );

        await LoadItemsAsync();
        return Page();
    }

    protected override async Task LoadItemsAsync()
    {
        var result = await retrievalService.ListGroupsAsync(skip: Skip, take: Take);
        if (result.ResultCode == RetrieveResultCode.Success && result.Data != null)
        {
            Items = result.Data.Items;
            TotalCount = result.Data.TotalCount;
        }
    }
}
