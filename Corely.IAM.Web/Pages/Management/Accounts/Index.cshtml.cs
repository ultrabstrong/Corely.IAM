using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Microsoft.AspNetCore.Mvc;

namespace Corely.IAM.Web.Pages.Management.Accounts;

public class IndexModel(
    IRetrievalService retrievalService,
    IRegistrationService registrationService,
    IDeregistrationService deregistrationService
) : ListPageModelBase<Account>
{
    public async Task<IActionResult> OnPostCreateAsync(string accountName)
    {
        if (string.IsNullOrWhiteSpace(accountName))
        {
            Message = "Account name is required.";
            MessageType = "danger";
            await LoadItemsAsync();
            return Page();
        }

        var result = await registrationService.RegisterAccountAsync(
            new RegisterAccountRequest(accountName)
        );

        SetResultMessage(
            result.ResultCode == RegisterAccountResultCode.Success,
            "Account created successfully.",
            result.Message ?? "Failed to create account."
        );

        await LoadItemsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid accountId)
    {
        var result = await deregistrationService.DeregisterAccountAsync();

        SetResultMessage(
            result.ResultCode == DeregisterAccountResultCode.Success,
            "Account deleted successfully.",
            result.Message ?? "Failed to delete account."
        );

        await LoadItemsAsync();
        return Page();
    }

    protected override async Task LoadItemsAsync()
    {
        var result = await retrievalService.ListAccountsAsync(skip: Skip, take: Take);
        if (result.ResultCode == RetrieveResultCode.Success && result.Data != null)
        {
            Items = result.Data.Items;
            TotalCount = result.Data.TotalCount;
        }
    }
}
