using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
using Corely.IAM.Services;
using Microsoft.AspNetCore.Mvc;

namespace Corely.IAM.Web.Pages.Management.Accounts;

public class DetailModel(
    IRetrievalService retrievalService,
    IModificationService modificationService,
    IRegistrationService registrationService,
    IDeregistrationService deregistrationService
) : DetailPageModelBase<Account>
{
    public string AccountName { get; set; } = string.Empty;
    public List<ChildRef> Users { get; set; } = [];

    protected override string IndexPagePath => "/Management/Accounts/Index";

    public async Task<IActionResult> OnPostEditAsync(Guid id, string accountName)
    {
        var result = await modificationService.ModifyAccountAsync(
            new UpdateAccountRequest(id, accountName)
        );

        SetResultMessage(
            result.ResultCode == ModifyResultCode.Success,
            "Account updated successfully.",
            result.Message
        );

        return await ReloadAsync(id);
    }

    public async Task<IActionResult> OnPostAddUserAsync(Guid id, Guid userId)
    {
        var result = await registrationService.RegisterUserWithAccountAsync(
            new RegisterUserWithAccountRequest(userId)
        );

        SetResultMessage(
            result.ResultCode == RegisterUserWithAccountResultCode.Success,
            "User added successfully.",
            result.Message ?? "Failed to add user."
        );

        return await ReloadAsync(id);
    }

    public async Task<IActionResult> OnPostRemoveUserAsync(Guid id, Guid userId)
    {
        var result = await deregistrationService.DeregisterUserFromAccountAsync(
            new DeregisterUserFromAccountRequest(userId)
        );

        SetResultMessage(
            result.ResultCode == DeregisterUserFromAccountResultCode.Success,
            "User removed successfully.",
            result.Message ?? "Failed to remove user."
        );

        return await ReloadAsync(id);
    }

    protected override async Task<Account?> LoadItemAsync(Guid id)
    {
        var result = await retrievalService.GetAccountAsync(id, hydrate: true);
        return result.ResultCode == RetrieveResultCode.Success ? result.Item : null;
    }

    protected override void PopulateFromItem(Account item)
    {
        Id = item.Id;
        AccountName = item.AccountName;
        Users = item.Users ?? [];
    }
}
