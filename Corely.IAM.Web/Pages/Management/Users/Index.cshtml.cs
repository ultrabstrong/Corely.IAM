using Corely.IAM.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;
using Microsoft.AspNetCore.Mvc;

namespace Corely.IAM.Web.Pages.Management.Users;

public class IndexModel(
    IRetrievalService retrievalService,
    IDeregistrationService deregistrationService
) : ListPageModelBase<User>
{
    public async Task<IActionResult> OnPostRemoveAsync(Guid userId)
    {
        var result = await deregistrationService.DeregisterUserFromAccountAsync(
            new DeregisterUserFromAccountRequest(userId)
        );

        SetResultMessage(
            result.ResultCode == DeregisterUserFromAccountResultCode.Success,
            "User removed from account.",
            result.Message ?? "Failed to remove user."
        );

        await LoadItemsAsync();
        return Page();
    }

    protected override async Task LoadItemsAsync()
    {
        var result = await retrievalService.ListUsersAsync(skip: Skip, take: Take);
        if (result.ResultCode == RetrieveResultCode.Success && result.Data != null)
        {
            Items = result.Data.Items;
            TotalCount = result.Data.TotalCount;
        }
    }
}
